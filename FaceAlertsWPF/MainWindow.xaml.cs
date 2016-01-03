using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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



namespace FaceAlertsWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private enum Mode
        {
            Polling,
            Events
        }

        private Mode RetrieveMode = Mode.Polling;

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

        private ObservableCollection<string> _Alerts = new ObservableCollection<string>();
        public ObservableCollection<string> Alerts
        {
            get { return _Alerts; }
            set
            {
                _Alerts = value;
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
            config.EnableAllAlerts();
            if (RetrieveMode == Mode.Events)
            {
                config.strategy = PXCMFaceConfiguration.TrackingStrategyType.STRATEGY_FARTHEST_TO_CLOSEST;
                PXCMFaceConfiguration.OnFiredAlertDelegate alertHandler = new PXCMFaceConfiguration.OnFiredAlertDelegate(OnAlertHandler);
                config.SubscribeAlert(alertHandler);
            }
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

        private void OnAlertHandler(PXCMFaceData.AlertData alertData)
        {
            DisplayAlertData(alertData);
        }

        private void DisplayAlertData(PXCMFaceData.AlertData alertData)
        {
            Dispatcher.Invoke(() =>
            {
                this.Alerts.Insert(0, $"[{alertData.timeStamp}] - FaceId: {alertData.faceId} - {alertData.label }");
            });
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
                    if (RetrieveMode == Mode.Polling)
                    {
                        PXCMFaceData.AlertData alertData = null;
                        for (int i = 0; i < faceData.QueryFiredAlertsNumber(); i++)
                        {
                            if (faceData.QueryFiredAlertData(i, out alertData).IsSuccessful())
                                DisplayAlertData(alertData);
                        }
                    }
                    var sample = SenseManager.QuerySample();
                    ElaborateSample(sample);
                    if (!PollingTaskCancellationToken.IsCancellationRequested) SenseManager.ReleaseFrame();
                }
            }
        }
        #endregion


        private void ElaborateSample(PXCMCapture.Sample sample)
        {
            if (sample == null) return;

            WriteableBitmap imageRGB = null;

            if (sample.color != null)
            {
                imageRGB = sample.color.GetImage();
                if (imageRGB != null)
                {
                    imageRGB.Freeze();
                    Dispatcher.Invoke(() =>
                    {
                        this.ImageRGB = imageRGB;
                    });
                }
            }
        }
    }
}
