Imports System.Globalization

Public Class DeviceModelToImageConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim model As PXCMCapture.DeviceModel
        If [Enum].TryParse(value.ToString(), model) Then
            Select Case model
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_DS4
                    Return "Images/R200.png"
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_F200
                    Return "Images/F200.png"
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_SR300
                    Return "Images/F200.png"
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_GENERIC
                    Return "Images/WebCam.png"
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM
                    Return "Images/F200.png"
                Case PXCMCapture.DeviceModel.DEVICE_MODEL_R200
                    Return "Images/R200.png"
                Case Else
                    Return "Images/WebCam.png"
            End Select
        End If
        Return Nothing
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
