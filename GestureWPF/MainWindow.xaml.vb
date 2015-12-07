Imports System.ComponentModel
Imports System.Threading
Imports System.Collections.ObjectModel

#Const POLLING = True


Class MainWindow
    Implements INotifyPropertyChanged

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        Gestures = New ObservableCollection(Of String)()

    End Sub

    Private SenseManager As PXCMSenseManager
    Private HandModule As PXCMHandModule

    Private TaskCancellationTokenSource As CancellationTokenSource

#Region "Proprieta'"
    Private _Gestures As ObservableCollection(Of String)
    Public Property Gestures As ObservableCollection(Of String)
        Get
            Return _Gestures
        End Get
        Set(value As ObservableCollection(Of String))
            _Gestures = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Gestures"))
        End Set
    End Property
#End Region

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.DataContext = Me

        SenseManager = PXCMSenseManager.CreateInstance()
        SenseManager.EnableHand()

        InitializeCamera()
        ConfigurePollingTask()
    End Sub

    Private Sub InitializeCamera()
        HandModule = SenseManager.QueryHand()

        Dim config = HandModule.CreateActiveConfiguration()
        config.EnableAllGestures()
        If config.ApplyChanges().IsSuccessful() Then
            If SenseManager.Init().IsError() Then
                MessageBox.Show("Errore nell'inizializzazione della camera")
                Close()
            End If
        End If
    End Sub

#Region "Loop acquisizione"

    Private PollingTask As Task
    Private PollingTaskCancellationToken As CancellationToken

    Private Sub ConfigurePollingTask()
        TaskCancellationTokenSource = New CancellationTokenSource()
        PollingTaskCancellationToken = TaskCancellationTokenSource.Token
        PollingTask = New Task(AddressOf PollingCode)
        PollingTask.Start()
    End Sub

    Private Sub PollingCode()
        Dim handData = HandModule.CreateOutput()
        While Not PollingTaskCancellationToken.IsCancellationRequested
            If SenseManager.AcquireFrame().IsSuccessful() Then
                handData.Update()
                ElaborateSample(handData)
                SenseManager.ReleaseFrame()
            End If
        End While
    End Sub
#End Region

#Region "Interfaccia INotifyPropertyChanged"
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
#End Region

    Private Sub ElaborateSample(handData As PXCMHandData)
        Dim index = 0
        Dim gestureData As PXCMHandData.GestureData = Nothing
        Dim numberOfGesture As Integer = handData.QueryFiredGesturesNumber()
        For handIndex = 0 To numberOfGesture - 1
            If handData.QueryFiredGestureData(handIndex, gestureData).IsSuccessful() Then
                If gestureData.state = PXCMHandData.GestureStateType.GESTURE_STATE_START Then
                    DisplayGesture(gestureData)
                End If
            End If
        Next
    End Sub

    Private Sub DisplayGesture(gestureData As PXCMHandData.GestureData)
        Dispatcher.Invoke(Sub()
                              Gestures.Add(String.Format("{0:HH\:mm\:ss} - {1} - {2}",
                                                         DateTime.Now, gestureData.name, gestureData.state))
                          End Sub)
    End Sub


End Class
