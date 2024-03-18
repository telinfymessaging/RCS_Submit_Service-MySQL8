Public Class RCS_Submit_Service

#Region "VARs"
    Dim intProcessCnt As UInt32 = 0
    Dim objRCSClient() As RCS_Client
#End Region

    'Dim t As Threading.Thread = New Threading.Thread(AddressOf Start_Main)
    Protected Overrides Sub OnStart(ByVal args() As String)
        ' Add code here to start your service. This method should set things
        ' in motion so your service can do its work.
        't.Start()

        Dim tProcessThread() As Threading.Thread = Nothing
        Dim intFetchRecords As UInt32 = 0

        Try
            Dim doc As New System.Xml.XmlDocument
            Dim strDirPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)
            'strDirPath = strDirPath.Replace("file:\", "")
            Dim os As OperatingSystem = Environment.OSVersion
            If os.VersionString.Contains("Windows") = False Then
                strDirPath = strDirPath.Replace("file:/", "/")
            Else
                strDirPath = strDirPath.Replace("file:\", "")
                IsOSWindows = True
            End If
            doc.Load(strDirPath & "\DBConfig.xml")
            Dim list = doc.GetElementsByTagName("MySQLDBConnString")
            For Each item As System.Xml.XmlElement In list
                strMySQLConn = item.InnerText
            Next

            Dim list1 = doc.GetElementsByTagName("ProcessCnt")
            For Each item As System.Xml.XmlElement In list1
                intProcessCnt = item.InnerText
            Next
            Dim list2 = doc.GetElementsByTagName("FetchRecords")
            For Each item As System.Xml.XmlElement In list2
                intFetchRecords = item.InnerText
            Next
            Dim list3 = doc.GetElementsByTagName("RCSUrl")
            For Each item As System.Xml.XmlElement In list3
                strRCSBusinessURL = item.InnerText
            Next
            addLog("URL: " & strRCSBusinessURL)
            ReDim tProcessThread(intProcessCnt - 1)
            For i = 0 To intProcessCnt - 1
                ReDim Preserve objRCSClient(i)
                objRCSClient(i) = New RCS_Client(intFetchRecords)
                tProcessThread(i) = New Threading.Thread(AddressOf objRCSClient(i).RCS_Main)
                tProcessThread(i).Start()
            Next
            addLog(objRCSClient.Length() & " RCSes Client Services Started")
        Catch ex As Exception
            addLog(ex.ToString())
            End
        End Try

    End Sub

    Protected Overrides Sub OnStop()
        ' Add code here to perform any tear-down necessary to stop your service.
        End
    End Sub

End Class
