Class MainWindow

    Private Session As PXCMSession = Nothing

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CreateSession()
        lstDevices.ItemsSource = GetAllDevices(Session)
    End Sub

    Private Sub CreateSession()
        Session = PXCMSession.CreateInstance()
    End Sub


    Private Function GetAllDevices(session As PXCMSession) As IEnumerable(Of CameraInfo)
        Dim deviceList = New List(Of CameraInfo)

        ' Preparo il filtro con cui ricercare i sensori di tipo device
        Dim implDescTemplate = New PXCMSession.ImplDesc()
        implDescTemplate.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR
        implDescTemplate.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE

        Dim implIndex = 0
        Dim implDesc As PXCMSession.ImplDesc = Nothing

        ' eseguo una query per recuperare l'i-esima implDesc che soddisfa il template
        While session.QueryImpl(implDescTemplate, implIndex, implDesc).IsSuccessful()

            Dim capture As PXCMCapture = Nothing
            ' recupero le info del device la cui implDesc ho recuperato
            If session.CreateImpl(implDesc, capture).IsSuccessful() Then
                Dim deviceInfoIndex = 0
                Dim deviceInfo As PXCMCapture.DeviceInfo = Nothing

                'eseguo la query per recuperare l'i-esima device info legata alla captture (ce ne dovrebbe essere una sola)
                While capture.QueryDeviceInfo(deviceInfoIndex, deviceInfo).IsSuccessful()
                    deviceList.Add(New CameraInfo(deviceInfo))
                    deviceInfoIndex += 1
                End While
            End If
            implIndex += 1
        End While

        Return deviceList
    End Function

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Session.Dispose()
    End Sub
End Class
