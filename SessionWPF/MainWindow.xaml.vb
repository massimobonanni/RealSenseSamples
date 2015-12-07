Class MainWindow

    Private Session As PXCMSession
    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Session = PXCMSession.CreateInstance()


        Dim version As PXCMSession.ImplVersion = Session.QueryVersion()
        Console.WriteLine("version {0}.{1}", version.major, version.minor)




        Dim coordinate As PXCMSession.CoordinateSystem = Session.QueryCoordinateSystem()






        Dim result As PXCMSession.ImplDesc = Nothing
        Dim template = New PXCMSession.ImplDesc() With {.vendor = &H8086}
        Dim modules = New List(Of String)()
        Dim implIndex = 0
        While (True)
            If Session.QueryImpl(template, implIndex, result).IsError() Then Exit While
            modules.Add(result.friendlyName)
            implIndex += 1
        End While





        'Dim senseManager As PXCMSenseManager = Session.CreateSenseManager()
        'If senseManager IsNot Nothing Then
        '    MessageBox.Show("SenseManager creato con successo!")
        'Else
        '    MessageBox.Show("Errore nella creazione del SenseManager!")
        'End If

        'Dim senseManager As PXCMSenseManager = Nothing
        'If Session.CreateImpl(Of PXCMSenseManager)(senseManager).IsSuccessful() Then
        '    MessageBox.Show("SenseManager creato con successo!")
        'Else
        '    MessageBox.Show("Errore nella creazione del SenseManager!")
        'End If


        Dim captureManager As PXCMCaptureManager = Session.CreateCaptureManager()
        If captureManager IsNot Nothing Then
            Dim desc As PXCMSession.ImplDesc = Nothing
            If Session.QueryModuleDesc(captureManager, desc).IsSuccessful() Then
                Console.WriteLine("friendlyName {0}", desc.friendlyName)
                Console.WriteLine("version {0}.{1}", desc.version.major, desc.version.minor)
                Console.WriteLine("vendor {0:X}", desc.vendor)
            End If
        End If









        Dim cuid = PXCMSenseManager.CUID
        Dim anotherSenseManager As PXCMBase = Nothing
        If Session.CreateImpl(cuid, anotherSenseManager) = pxcmStatus.PXCM_STATUS_NO_ERROR Then
            MessageBox.Show(anotherSenseManager.GetType().FullName)
        Else
            MessageBox.Show("Errore nella CreateImpl!")
        End If

    End Sub
End Class
