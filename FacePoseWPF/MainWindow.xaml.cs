using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FacePoseWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PXCMSenseManager SenseManager;

        private PXCMFaceModule FaceModule;

        private PXCMFaceData FaceData;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region UI Properties
        private WriteableBitmap _ImageRGB;
        public WriteableBitmap ImageRGB
        {
            get { return _ImageRGB; }
            set
            {
                _ImageRGB = value;
                NotifyPropertyChanged();
            }
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;

            SenseManager = PXCMSenseManager.CreateInstance();

            SenseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720);
            SenseManager.EnableFace();
            InitializeCamera();
            //ConfigurePollingTask();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (TaskCancellationTokenSource != null)
                TaskCancellationTokenSource.Cancel();
            SenseManager.Close();
            SenseManager.Dispose();
        }


        #region Inizializzazione e acquisizione
        private void InitializeCamera()
        {
            FaceModule = SenseManager.QueryFace();
            FaceData = FaceModule.CreateOutput();

            var config = FaceModule.CreateActiveConfiguration();
            config.detection.isEnabled = false;
            config.landmarks.isEnabled = true;
            config.pose.isEnabled = true;
            config.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_FARTHEST_TO_CLOSEST;

            if (config.ApplyChanges().IsSuccessful())
            {
                var handler = new PXCMSenseManager.Handler();
                handler.onModuleProcessedFrame  = OnModuleProcessedFrame;
                if (SenseManager.Init(handler).IsError())
                {
                    MessageBox.Show("Errore nell'inizializzazione della camera");
                    Close();
                }
            }

            SenseManager.StreamFrames(true);

            config.Dispose();
        }

        private Task PollingTask;
        private CancellationTokenSource TaskCancellationTokenSource;
        private CancellationToken PollingTaskCancellationToken;

        private void ConfigurePollingTask()
        {
            TaskCancellationTokenSource = new CancellationTokenSource();
            PollingTaskCancellationToken = TaskCancellationTokenSource.Token;
            PollingTask = new Task(PollingCode);
            PollingTask.Start();
        }

        private void PollingCode()
        {
            PXCMFaceData faceData = FaceModule.CreateOutput();

            while (!PollingTaskCancellationToken.IsCancellationRequested)
            {
                if (SenseManager.AcquireFrame().IsSuccessful())
                {
                    faceData.Update();
                    var face = faceData.QueryFaceByIndex(0);
                    var sample = SenseManager.QuerySample();
                    ElaborateSample(sample, face);
                    SenseManager.ReleaseFrame();
                }
            }
        }
        #endregion


        pxcmStatus OnModuleProcessedFrame(Int32 mid, PXCMBase module, PXCMCapture.Sample sample)
        {
            //// check if the callback is from the hand tracking module
            if (mid == PXCMFaceModule.CUID)
            {
                // Retrieve the current hand data
                FaceData.Update();
                var face = FaceData.QueryFaceByIndex(0);
                ElaborateSample(sample, face);
            }

            return pxcmStatus.PXCM_STATUS_NO_ERROR;
        }

        private void ElaborateSample(PXCMCapture.Sample sample, PXCMFaceData.Face face)
        {
            if (sample == null) return;

            WriteableBitmap imageRGB = null;

            if (sample.color != null)
            {
                imageRGB = GetImage(sample.color);
            }

            if (face != null)
            {

                PXCMFaceData.LandmarksData landmarkData = face.QueryLandmarks();
                PXCMFaceData.LandmarkPoint[] landmarkPoints = null;
                if (landmarkData.QueryPoints(out landmarkPoints))
                {
                    foreach (var point in landmarkPoints)
                    {
                        imageRGB.FillEllipseCentered((int)point.image.x, (int)point.image.y, 4, 4, Colors.White);
                    }
                }

            }

            if (imageRGB != null)
                imageRGB.Freeze();

            Dispatcher.Invoke(() =>
            {
                this.ImageRGB = imageRGB;
            });

            Process.GetCurrentProcess();
        }

        private WriteableBitmap GetImage(PXCMImage image)
        {
            PXCMImage.ImageData imageData = null;
            WriteableBitmap returnImage = null;
            int width = 0;
            int height = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ,
                                   PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32,
                                   out imageData).IsSuccessful())
            {
                width = Convert.ToInt32(imageData.pitches[0] / 4);
                height = image.info.height;
                returnImage = imageData.ToWritableBitmap(width, height, 96, 96);
                image.ReleaseAccess(imageData);
            }
            return returnImage;
        }
    }
}
