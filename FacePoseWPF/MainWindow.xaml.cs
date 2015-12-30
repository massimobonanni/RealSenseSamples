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
using WPFCore;

namespace FacePoseWPF
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
            IsPoseVisible = false;
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

        private float _Pitch;
        public float Pitch
        {
            get { return _Pitch; }
            set
            {
                _Pitch = value;
                NotifyPropertyChanged();
            }
        }

        private float _Roll;
        public float Roll
        {
            get { return _Roll; }
            set
            {
                _Roll = value;
                NotifyPropertyChanged();
            }
        }

        private float _Yaw;
        public float Yaw
        {
            get { return _Yaw; }
            set
            {
                _Yaw = value;
                NotifyPropertyChanged();
            }
        }

        private bool _IsPoseVisible = false;
        public bool IsPoseVisible
        {
            get { return _IsPoseVisible; }
            set
            {
                _IsPoseVisible = value;
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
            config.detection.isEnabled = false;
            config.landmarks.isEnabled = false;
            config.pose.isEnabled = true;
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

            while (!PollingTaskCancellationToken.IsCancellationRequested)
            {
                if (SenseManager.AcquireFrame().IsSuccessful())
                {
                    faceData.Update();
                    var face = faceData.QueryFaceByIndex(0);
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
            bool isPoseVisible = false;
            float pitchValue = 0, yawValue = 0, rollValue = 0;

            if (sample.color != null)
            {
                imageRGB = sample.color.GetImage();
            }

            if (face != null)
            {
                PXCMFaceData.PoseData poseData = face.QueryPose();

                PXCMFaceData.PoseEulerAngles poseAngles;
                if (poseData.QueryPoseAngles(out poseAngles))
                {
                    isPoseVisible = true;
                    pitchValue = poseAngles.pitch;
                    yawValue = poseAngles.yaw;
                    rollValue = poseAngles.roll;
                }
            }

            if (imageRGB != null)
                imageRGB.Freeze();

            Dispatcher.Invoke(() =>
            {
                this.ImageRGB = imageRGB;
                this.IsPoseVisible = isPoseVisible;
                this.Pitch = pitchValue;
                this.Roll = rollValue;
                this.Yaw = yawValue;
            });

        }
    }
}
