Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports WPFCore

#Const POLLING = True

Class MainWindow
    Implements INotifyPropertyChanged

    Private SenseManager As PXCMSenseManager

    Private TaskCancellationTokenSource As CancellationTokenSource


#Region "Proprieta'"
    Private _ImageRGB As WriteableBitmap
    Public Property ImageRGB As WriteableBitmap
        Get
            Return _ImageRGB
        End Get
        Set(value As WriteableBitmap)
            _ImageRGB = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ImageRGB"))
        End Set
    End Property

    Private _ImageIR As WriteableBitmap
    Public Property ImageIR As WriteableBitmap
        Get
            Return _ImageIR
        End Get
        Set(value As WriteableBitmap)
            _ImageIR = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ImageIR"))
        End Set
    End Property

    Private _ImageDepth As WriteableBitmap
    Public Property ImageDepth As WriteableBitmap
        Get
            Return _ImageDepth
        End Get
        Set(value As WriteableBitmap)
            _ImageDepth = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ImageDepth"))
        End Set
    End Property
#End Region

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.DataContext = Me

        SenseManager = PXCMSenseManager.CreateInstance()

        SenseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720)
        SenseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 640, 480)
        SenseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_IR, 640, 480)
        InitializeCamera()

#If POLLING Then
        InitializeCamera()
        ConfigurePollingTask()
#Else
        ConfigureHandler()
        ConfigurePollingTask()
#End If
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If (TaskCancellationTokenSource IsNot Nothing) Then TaskCancellationTokenSource.Cancel()
        SenseManager.Close()
        SenseManager.Dispose()
    End Sub


#Region "Loop acquisizione"
    Private Sub InitializeCamera()

        If SenseManager.Init().IsError() Then
            MessageBox.Show("Errore nell'inizializzazione della camera")
            Close()
        End If
    End Sub

    Private PollingTask As Task
    Private PollingTaskCancellationToken As CancellationToken

    Private Sub ConfigurePollingTask()
        TaskCancellationTokenSource = New CancellationTokenSource()
        PollingTaskCancellationToken = TaskCancellationTokenSource.Token
        PollingTask = New Task(AddressOf PollingCode)
        PollingTask.Start()
    End Sub

    Private Sub PollingCode()
        While Not PollingTaskCancellationToken.IsCancellationRequested
            If SenseManager.AcquireFrame().IsSuccessful() Then
                Dim sample = SenseManager.QuerySample()
                ElaborateSample(sample)
                SenseManager.ReleaseFrame()
            End If
        End While
    End Sub
#End Region


#Region "Gestione con Handler"
    Private Sub ConfigureHandler()
        Dim handler = New PXCMSenseManager.Handler()
        handler.onNewSample = AddressOf OnNewSample
        If SenseManager.Init(handler).IsError() Then
            MessageBox.Show("Errore nell'inizializzazione della camera")
            Close()
        End If
    End Sub

    Private Function OnNewSample(mid As Integer, sample As PXCMCapture.Sample) As pxcmStatus
        ElaborateSample(sample)
        Return pxcmStatus.PXCM_STATUS_NO_ERROR
    End Function

    Private HandlerTask As Task

    Private Sub ConfigureHandlerTask()
        HandlerTask = New Task(AddressOf HandlerCode)
        HandlerTask.Start()
    End Sub

    Private Sub HandlerCode()
        SenseManager.StreamFrames(True)
    End Sub
#End Region

    Private Sub ElaborateSample(ByVal sample As PXCMCapture.Sample)
        If sample Is Nothing Then Return

        Dim imageRGB As WriteableBitmap = Nothing
        Dim imageIR As WriteableBitmap = Nothing
        Dim imageDepth As WriteableBitmap = Nothing

        If sample.color IsNot Nothing Then
            imageRGB = sample.color.GetImage()
            imageRGB.Freeze()
        End If
        If sample.ir IsNot Nothing Then
            imageIR = sample.ir.GetImage()
            imageIR.Freeze()
        End If
        If sample.depth IsNot Nothing Then
            imageDepth = sample.depth.GetImage()
            imageDepth.Freeze()
        End If

        Dispatcher.Invoke(Sub()
                              Me.ImageRGB = imageRGB
                              Me.ImageIR = imageIR
                              Me.ImageDepth = imageDepth
                          End Sub)
    End Sub

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
End Class
