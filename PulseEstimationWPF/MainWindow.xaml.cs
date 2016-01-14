using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using WPFCore;

namespace PulseEstimationWPF
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

        private int _HeartRate;
        public int HeartRate
        {
            get { return _HeartRate; }
            set
            {
                _HeartRate = value;
                NotifyPropertyChanged();
            }
        }

        private bool _IsHeartRateRetrieved;
        public bool IsHeartRateRetrieved
        {
            get { return _IsHeartRateRetrieved; }
            set
            {
                _IsHeartRateRetrieved = value;
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
            config.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_CLOSEST_TO_FARTHEST;

            var pulseConfig = config.QueryPulse();
            pulseConfig.Enable();

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

            while (!PollingTaskCancellationToken.IsCancellationRequested)
            {
                if (SenseManager.AcquireFrame().IsSuccessful())
                {
                    faceData.Update();
                    var face = faceData.QueryFaceByID(0);
                    var sample = SenseManager.QuerySample();
                    ElaborateSample(sample, face);
                    if (!PollingTaskCancellationToken.IsCancellationRequested) SenseManager.ReleaseFrame();
                }
            }
        }
        #endregion



        private void ElaborateSample(PXCMCapture.Sample sample, PXCMFaceData.Face face)
        {
            if (sample == null) return;

            WriteableBitmap imageRGB = null;
            float heartRate =-1;
            if (sample.color != null)
            {
                imageRGB = sample.color.GetImage();
            }

            if (face != null)
            {

                PXCMFaceData.DetectionData detectionData = face.QueryDetection();
                PXCMRectI32 faceBound;
                if (detectionData != null && detectionData.QueryBoundingRect(out faceBound))
                {
                    imageRGB.DrawRectangle(faceBound.x, faceBound.y,
                        faceBound.x + faceBound.w, faceBound.y + faceBound.h,
                        Colors.Red, 4);
                }

                PXCMFaceData.PulseData pulseData = face.QueryPulse();
                if (pulseData != null)
                    heartRate = pulseData.QueryHeartRate();
                else
                    heartRate = -1;

            }

            if (imageRGB != null)
                imageRGB.Freeze();

            Dispatcher.Invoke(() =>
            {
                this.ImageRGB = imageRGB;
                HeartRate =(int) Math.Round(heartRate,0);
                IsHeartRateRetrieved = heartRate > 0;
            });

        }

    }
}
