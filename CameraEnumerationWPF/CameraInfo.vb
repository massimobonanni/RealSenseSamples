Public Class CameraInfo

    Public Sub New()

    End Sub

    Public Sub New(deviceInfo As PXCMCapture.DeviceInfo)
        If deviceInfo Is Nothing Then Throw New ArgumentNullException("deviceInfo")
        UID = deviceInfo.duid
        Firmware = New Version(deviceInfo.firmware(0), deviceInfo.firmware(1), deviceInfo.firmware(2), deviceInfo.firmware(3))
        Model = deviceInfo.model
        Name = deviceInfo.name
        Orientation = deviceInfo.orientation
        SerialNumber = deviceInfo.serial
        Streams = deviceInfo.streams
    End Sub

    Public Property UID As Integer
    Public Property Firmware As Version
    Public Property Model As PXCMCapture.DeviceModel
    Public Property Name As String
    Public Property Orientation As PXCMCapture.DeviceOrientation
    Public Property SerialNumber As String
    Public Property Streams As PXCMCapture.StreamType

End Class
