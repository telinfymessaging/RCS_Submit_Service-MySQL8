#Region "Import"
Imports System.Net
Imports System.IO
Imports System
Imports System.Net.Http
Imports Newtonsoft.Json

#End Region
Module Main

#Region "Vars"
    Dim intProcessCnt As UInt32 = 0
    Dim objRCSClient() As RCS_Client
#End Region

#Region "RCS Data Structure"
    Structure RCSDataStructure
        Dim id As UInt32
        Dim user_id As UInt32
        Dim job_id As UInt32
        Dim rcs_data As String
        Dim phone_number As UInt64
        Dim Bearer As String
    End Structure

#End Region

#Region "JsonStructure"
    Public Class fileURLReturn
        Public Property name As String
    End Class
#End Region

#Region "JsonSendTextStructure"
    Public Class ContentMessage
        Public Property text As String
    End Class

    Public Class sendTextCls
        Public Property contentMessage As ContentMessage
    End Class
#End Region

#Region "JsonRichCardStructure"
    Public Class ContentInfo
        Public Property fileUrl As String
        Public Property forceRefresh As String
    End Class

    Public Class Media
        Public Property height As String
        'Public Property contentInfo As ContentInfo
        Public Property fileName As String
    End Class

    Public Class Reply
        Public Property text As String
        Public Property postbackData As String
    End Class

    Public Class Suggestion
        Public Property reply As Reply
    End Class

    Public Class CardContent
        Public Property title As String
        Public Property description As String
        Public Property media As Media
        Public Property suggestions As Suggestion()
    End Class

    Public Class StandaloneCard
        Public Property thumbnailImageAlignment As String
        Public Property cardOrientation As String
        Public Property cardContent As CardContent
    End Class

    Public Class RichCard
        Public Property standaloneCard As StandaloneCard
    End Class

    Public Class ContentMessageRichCard
        Public Property richCard As RichCard
    End Class

    Public Class richCardCls
        Public Property contentMessage As ContentMessageRichCard
    End Class

#End Region
    Public Class Image
        Public Property imageUrl As String
        Public Property title As String
        Public Property fileID As String
    End Class

    Public Class RCS_Data
        Public Property image As Image
        Public Property text As String
    End Class

    Public Sub Start_Main()
        Dim ds As New DataSet
        Dim tProcessThread() As Threading.Thread = Nothing, intRowCnt As UInt32 = 0, i As UInt32 = 0
        Dim intFetchRecords As UInt32 = 0
        Dim objRCSStruct As RCSDataStructure = New RCSDataStructure
        Try
            Exit Sub
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

            ReDim tProcessThread(intProcessCnt - 1)
            For i = 0 To intProcessCnt - 1
                tProcessThread(i) = New Threading.Thread(AddressOf tSubmitRCS)
            Next
            addLog("Process Started......@1 " & Now())
            Try
                While 1
                    Try
                        i = 0
                        ds = Get_RCS_data(intFetchRecords)
                        Try
                            intRowCnt = ds.Tables(0).Rows.Count
                        Catch ex As Exception
                            intRowCnt = 0
                            addLog(ex.ToString())
                        End Try
                        If ds.Tables.Count > 0 Then
                            While i < intRowCnt
                                For ax = 0 To intProcessCnt - 1
                                    If tProcessThread(ax).ThreadState = 8 Or tProcessThread(ax).ThreadState = 16 Then
                                        tProcessThread(ax) = New Threading.Thread(AddressOf tSubmitRCS)
                                        With objRCSStruct
                                            .id = ds.Tables(0).Rows(i).Item("id")
                                            .job_id = ds.Tables(0).Rows(i).Item("job_id")
                                            .phone_number = ds.Tables(0).Rows(i).Item("phone_number")
                                            .rcs_data = ds.Tables(0).Rows(i).Item("rcs_data")
                                            .user_id = ds.Tables(0).Rows(i).Item("user_id")
                                            .Bearer = ds.Tables(0).Rows(i).Item("Bearer")
                                        End With
                                        tProcessThread(ax).Start(objRCSStruct)
                                        i = i + 1
                                        If i >= intRowCnt Then Exit While
                                    End If
                                Next
                                Threading.Thread.Sleep(10)
                            End While
                        Else
                            Threading.Thread.Sleep(10)
                        End If
                    Catch ex As Exception
                        addLog(ex.ToString())
                        Threading.Thread.Sleep(30000)
                    End Try
                    Threading.Thread.Sleep(10000)
                End While
            Catch ex As Exception
                addLog(ex.ToString())
            End Try
        Catch ex As Exception
            addLog(ex.ToString())
            End
        End Try
    End Sub

    Private Sub tSubmitRCS(ByVal objRCSDataStructure As RCSDataStructure)
        Try
            Dim objRCS_Data As RCS_Data = JsonConvert.DeserializeObject(Of RCS_Data)(objRCSDataStructure.rcs_data)
            Dim strResponse As String = ""
            Dim strJobID As String = CStr(objRCSDataStructure.job_id)
            Dim IsWebException As Boolean = False
            With objRCS_Data
                If .text.Length > 0 Then
                    strJobID = strJobID & "a"
                    strResponse = sendText(strJobID, "+" & objRCSDataStructure.phone_number, objRCSDataStructure.Bearer, .text, IsWebException)
                    If strResponse.Length > 0 Then
                        Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, strResponse)
                    End If

                End If
                If .image.imageUrl.Length > 0 And IsWebException = False Then
                    strJobID = strJobID & "b"
                    strResponse = sendRichCard(strJobID, "+" & objRCSDataStructure.phone_number, .image.imageUrl, objRCSDataStructure.Bearer, .image.fileID, IsWebException)
                    'Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, strResponse)
                    If strResponse.Length > 0 Then
                        Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, strResponse)
                    End If
                End If
            End With

        Catch ex As Exception
            addLog(ex.ToString())
        End Try
    End Sub

    Private Function sendText(strJobID As String, strPhoneNumber As String, strBearer As String, strText As String, ByRef pIsWebException As Boolean) As String
        Try
            pIsWebException = False
            Dim strJsonData As String = ""
            Dim obj As sendTextCls = New sendTextCls()
            Dim objText As ContentMessage = New ContentMessage()

            objText.text = strText
            obj.contentMessage = objText

            strJsonData = JsonConvert.SerializeObject(obj)

            'addLog(strJsonData)
            Dim client As WebClient = New WebClient()
            client.Headers.Add("Content-Type", "application/json")
            client.Headers.Add("Authorization", "Bearer " & strBearer)

            Dim response As String = client.UploadString("https://asia-rcsbusinessmessaging.googleapis.com/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, strJsonData)
            Return response
        Catch ex1 As WebException
            pIsWebException = True
            Return getError(ex1)
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        End Try

    End Function

    Private Function sendRichCard(strJobID As String, strPhoneNumber As String, strFileURL As String, strBearer As String, strFilename As String, ByRef pIsWebException As Boolean)
        Try
            pIsWebException = False
            Dim objContentInfo As ContentInfo = New ContentInfo()
            With objContentInfo
                .fileUrl = strFileURL
                .forceRefresh = "False"
            End With
            Dim objMedia As Media = New Media()
            With objMedia
                .height = "TALL"
                '.contentInfo = objContentInfo
                .fileName = strFilename
            End With
            'Dim objReply As Reply = New Reply()
            'With objReply
            '.text = Nothing
            '.postbackData = Nothing
            'End With
            'Dim objSuggestion() As Suggestion()
            'With objSuggestion
            '.reply = objReply
            'End With
            Dim objCardContent As New CardContent()
            With objCardContent
                .title = ""
                .description = ""
                .media = objMedia
                .suggestions = Nothing
            End With
            Dim objStandaloneCard As StandaloneCard = New StandaloneCard()
            With objStandaloneCard
                .thumbnailImageAlignment = "RIGHT"
                .cardOrientation = "VERTICAL"
                .cardContent = objCardContent
            End With
            Dim objRichCard As RichCard = New RichCard()
            objRichCard.standaloneCard = objStandaloneCard
            Dim objContentMessageRichCard As ContentMessageRichCard = New ContentMessageRichCard()
            objContentMessageRichCard.richCard = objRichCard
            Dim objrichCardCls As richCardCls = New richCardCls()
            objrichCardCls.contentMessage = objContentMessageRichCard

            Dim strJsonData As String = JsonConvert.SerializeObject(objrichCardCls)
            strJsonData = strJsonData.Replace("null", "[]")
            'Console.WriteLine(strJsonData)
            'Console.WriteLine("https://asia-rcsbusinessmessaging.googleapis.com/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID)
            'Return strJsonData
            Dim client As WebClient = New WebClient()
            client.Headers.Add("Content-Type", "application/json")
            client.Headers.Add("Authorization", "Bearer " & strBearer)

            Dim response As String = client.UploadString("https://asia-rcsbusinessmessaging.googleapis.com/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, "post", strJsonData)
            Return response
        Catch ex1 As WebException
            pIsWebException = True
            Return getError(ex1)
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        End Try
    End Function

End Module
