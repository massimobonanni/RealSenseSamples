using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FaceTrackingWPF;
using System.Collections.Generic;
using System.Linq;

namespace FaceTrackingWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private PXCMSenseManager SenseManager;

        private PXCMFaceModule FaceModule;

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
            ConfigurePollingTask();
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

            var config = FaceModule.CreateActiveConfiguration();
            config.detection.isEnabled = true;
            config.landmarks.isEnabled = false;
            config.pose.isEnabled = false;
            config.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_FARTHEST_TO_CLOSEST;

            if (config.ApplyChanges().IsSuccessful())
            {
                if (SenseManager.Init().IsError())
                {
                    MessageBox.Show("Errore nell'inizializzazione della camera");
                    Close();
                }
            }

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
            List<PXCMFaceData.Face> faces = new List<PXCMFaceData.Face>();

            while (!PollingTaskCancellationToken.IsCancellationRequested)
            {
                if (SenseManager.AcquireFrame().IsSuccessful())
                {
                    faceData.Update();
                    faces.Clear();
                    for (int i = 0; i < faceData.QueryNumberOfDetectedFaces(); i++)
                    {
                        faces.Add(faceData.QueryFaceByIndex(i));
                    }

                    var sample = SenseManager.QuerySample();
                    ElaborateSample(sample, faces);
                    SenseManager.ReleaseFrame();
                }
            }
        }
        #endregion

        private IEnumerable<Color> TrackingColors = new List<Color>() { Colors.Yellow, Colors.Blue, Colors.Green, Colors.Red, Colors.White, Colors.Fuchsia };

        private Color GetColorByIndex(int index)
        {
            var colorIndex = index % TrackingColors.Count();
            return TrackingColors.ElementAt(colorIndex);
        }

        private void ElaborateSample(PXCMCapture.Sample sample, IEnumerable<PXCMFaceData.Face> faces)
        {
            if (sample == null) return;

            WriteableBitmap imageRGB = null;

            if (sample.color != null)
            {
                imageRGB = GetImage(sample.color);
            }

            if (faces != null && faces.Any())
            {
                for (int i = 0; i < faces.Count(); i++)
                {
                    var face = faces.ElementAt(i);
                    var detectionData = face.QueryDetection();
                    PXCMRectI32 faceBound;
                    if (detectionData != null && detectionData.QueryBoundingRect(out faceBound))
                    {
                        imageRGB.DrawRectangle(faceBound.x, faceBound.y,
                            faceBound.x + faceBound.w, faceBound.y + faceBound.h,
                            GetColorByIndex(i), 4);
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
