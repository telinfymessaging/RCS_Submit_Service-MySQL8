
#Region "Imports"
Imports System.Net
Imports Newtonsoft.Json
Imports System.Text

Imports System.Net.Sockets
Imports System.Net.Security
Imports System.Security.Authentication
Imports System.IO

#End Region

Public Class RCS_Client

#Region "VARS"
    Dim lIntFetchRecords As UInt32 = 10
    Dim mServerMessage As String = ""

#End Region

#Region "RCS Data Structure"
    Structure RCSDataStructure
        Dim id As UInt32
        Dim user_id As UInt32
        Dim job_id As UInt32
        Dim rcs_data As String
        Dim phone_number As UInt64
        Dim Bearer As String
        Dim Relative_URL As String
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
        Public suggestions As New ArrayList
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
        'Public Property suggestions As Suggestion()
        Public Property suggestions As New ArrayList
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
        Public Property suggestions As New ArrayList
    End Class

    Public Sub New(pIntFetchRecords As UInt32)
        lIntFetchRecords = pIntFetchRecords
    End Sub

    Public Sub RCS_Main()
        Dim i As UInt32 = 0, ds As DataSet = Nothing, intRowCnt As UInt32 = 0
        Dim objRCSStruct As RCSDataStructure = New RCSDataStructure
        While 1
            Try
                i = 0
                SyncLock objSyncLockRCS_client
                    ds = Get_RCS_data(lIntFetchRecords)
                End SyncLock
                Try
                    intRowCnt = ds.Tables(0).Rows.Count
                    'If intRowCnt > 0 Then
                    '    'addLog("Rows Fetched are " & intRowCnt)
                    'End If

                Catch ex As Exception
                    intRowCnt = 0
                    addLog(ex.ToString())
                End Try
                If ds.Tables.Count > 0 Then
                    While i < intRowCnt
                        With objRCSStruct
                            .id = ds.Tables(0).Rows(i).Item("id")
                            .job_id = ds.Tables(0).Rows(i).Item("job_id")
                            .phone_number = ds.Tables(0).Rows(i).Item("phone_number")
                            .rcs_data = ds.Tables(0).Rows(i).Item("rcs_data")
                            .user_id = ds.Tables(0).Rows(i).Item("user_id")
                            .Bearer = ds.Tables(0).Rows(i).Item("Bearer")
                            .Relative_URL = ds.Tables(0).Rows(i).Item("relative_url")
                        End With
                        Dim t As Threading.Thread = New Threading.Thread(AddressOf SubmitRCS)
                        t.Start(objRCSStruct)
                        'SubmitRCS(objRCSStruct)
                        'Threading.Thread.Sleep(100)
                        i = i + 1
                        If i >= intRowCnt Then
                            Exit While
                        End If
                    End While
                End If
            Catch ex As Exception
                addLog(ex.ToString())
            Finally
                Threading.Thread.Sleep(1000)
            End Try
        End While
    End Sub

    Private Sub SubmitRCS(ByVal objRCSDataStructure As RCSDataStructure)

        Try
            'Dim response As String = RunClient(strRCSBusinessURL, "upload.video.google.com", objRCSDataStructure.Bearer, "/v1/phones/+" & objRCSDataStructure.phone_number & "/agentMessages?messageId=" & objRCSDataStructure.job_id, objRCSDataStructure.rcs_data)
            Dim response As String = RunClient(strRCSBusinessURL, "upload.video.google.com", objRCSDataStructure.Bearer, objRCSDataStructure.Relative_URL, objRCSDataStructure.rcs_data)
            If response.Length > 0 Then
                Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, response, mServerMessage)
            End If
            Exit Sub

            Dim objRCS_Data As RCS_Data = JsonConvert.DeserializeObject(Of RCS_Data)(objRCSDataStructure.rcs_data)
            Dim strResponse As String = ""
            Dim strJobID As String = CStr(objRCSDataStructure.job_id)
            Dim IsWebException As Boolean = False
            With objRCS_Data
                If .text.Length > 0 And .image.imageUrl.Length = 0 Then
                    strJobID = strJobID & "a"
                    strResponse = sendText(strJobID, "+" & objRCSDataStructure.phone_number, objRCSDataStructure.Bearer, .text, .suggestions)
                    If strResponse.Length > 0 Then
                        Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, strResponse, mServerMessage)
                    Else
                        Exit Sub
                    End If

                End If
                If .image.imageUrl.Length > 0 Then
                    strJobID = strJobID & "b"
                    strResponse = sendRichCard(strJobID, "+" & objRCSDataStructure.phone_number, .image.imageUrl, objRCSDataStructure.Bearer, .image.fileID, .image.title, .text, .suggestions)
                    If strResponse.Length > 0 Then
                        Call Delete_RCS_data(objRCSDataStructure.id, objRCSDataStructure.job_id, strResponse, mServerMessage)
                    End If
                End If
            End With
        Catch ex As Exception
            addLog(ex.ToString())
        Finally
            'addLog("Submit Ended at " & Now())
        End Try
    End Sub

    Private Function sendText(strJobID As String, strPhoneNumber As String, strBearer As String, strText As String, pSuggestions As ArrayList) As String
        Try
            Dim startDT As Date = Now()
            Dim strJsonData As String = ""
            Dim obj As sendTextCls = New sendTextCls()
            Dim objText As ContentMessage = New ContentMessage()

            objText.text = strText
            objText.suggestions = pSuggestions
            obj.contentMessage = objText


            strJsonData = JsonConvert.SerializeObject(obj)


            'Dim client As WebClient = New WebClient()
            'client.Headers.Add("Content-Type", "application/json")
            'client.Headers.Add("Authorization", "Bearer " & strBearer)
            'Dim response As String = client.UploadString(strRCSBusinessURL & "/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, strJsonData)
            addLog("Seconds: " & (Now() - startDT).TotalSeconds)
            Dim response As String = RunClient(strRCSBusinessURL, "upload.video.google.com", strBearer, "/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, strJsonData)

            Return response
        Catch ex1 As WebException
            addLog(ex1.ToString())
            Return getError(ex1)
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        End Try

    End Function

    Private Function sendRichCard(strJobID As String, strPhoneNumber As String, strFileURL As String, strBearer As String, strFilename As String, strTitle As String, strDescription As String, pSuggestions As ArrayList)

        Try
            Dim startDT As Date = Now()
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

            Dim objCardContent As New CardContent()
            With objCardContent
                .title = strTitle
                .description = strDescription
                .media = objMedia
                .suggestions = pSuggestions
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

            'Dim client As WebClient = New WebClient()
            'client.Headers.Add("Content-Type", "application/json")
            'client.Headers.Add("Authorization", "Bearer " & strBearer)

            'Dim response As String = client.UploadString(strRCSBusinessURL & "/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, "post", strJsonData)            
            addLog("Seconds: " & (Now() - startDT).TotalSeconds)
            Dim response As String = RunClient(strRCSBusinessURL, "upload.video.google.com", strBearer, "/v1/phones/" & strPhoneNumber & "/agentMessages?messageId=" & strJobID, strJsonData)

            Return response
        Catch ex1 As WebException
            addLog(ex1.ToString())
            Return getError(ex1)
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        End Try
    End Function

    Function RunClient(ByVal machineName As String, ByVal serverName As String, strBearer As String, strPostURL As String, strBody As String) As String
        Dim dt As Date = Now()
        machineName = machineName.Replace("https://", "")
        Dim client As New TcpClient(machineName, 443)

        If client.Connected = False Then
            Return ""
        End If
        'addLog("Client connected.")


        Dim sslStream As New SslStream(client.GetStream(), False, New RemoteCertificateValidationCallback(AddressOf ValidateServerCertificate), Nothing)

        Try
            sslStream.AuthenticateAsClient(serverName)
        Catch e As AuthenticationException

            addLog(e.Message)

            If e.InnerException IsNot Nothing Then

                addLog(e.InnerException.Message)

            End If

            addLog("Authentication failed - closing the connection.")

            client.Close()

            Return ""

        End Try

        Try
            ' Encode a test message into a byte array.

            ' Signal the end of the message using the "<EOF>".
            Dim strdata As String = "POST " & strPostURL & " HTTP/1.1
Host: " & machineName & "
Authorization: Bearer " & strBearer & "
Content-Type: application/json
Content-Length: " & strBody.Length() & "

" & strBody
            'addLog(strdata)
            Dim messsage As Byte() = Encoding.UTF8.GetBytes(strdata)



            ' Send hello message to the server.

            sslStream.Write(messsage)

            sslStream.Flush()

            ' Read message from the server.

            Dim serverMessage As String = ReadMessage(sslStream)

            mServerMessage = serverMessage

            'serverMessage = serverMessage.Substring(serverMessage.IndexOf(vbCrLf & vbCrLf) + 4)

            'Dim iFirstPos As UInt16 = serverMessage.IndexOf(Chr(123))
            ''Dim iLastPos As UInt16 = serverMessage.IndexOf(vbLf & vbCrLf & Chr(0))
            ''serverMessage = serverMessage.Substring(iFirstPos, iLastPos - iFirstPos)
            'serverMessage = serverMessage.Substring(iFirstPos).Replace(vbLf & vbCrLf & Chr(0), "")

            'serverMessage = serverMessage.Split(vbCrLf)(0)

            'addLog("RCS Seconds: " & (Now() - dt).TotalSeconds)
            client.Close()
            Return serverMessage
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        End Try
    End Function

    Function ReadMessage(ByVal sslStream As SslStream) As String

        Dim buffer(4096) As Byte

        Dim messageData As StringBuilder = New StringBuilder

        Dim bytes As Integer = -1
        Dim tmpStr As String = ""
        sslStream.ReadTimeout = 1000 * 30
        Try
            Do

                bytes = sslStream.Read(buffer, 0, buffer.Length)
                Dim decoder As Decoder = Encoding.UTF8.GetDecoder

                Dim chars(decoder.GetCharCount(buffer, 0, bytes)) As Char

                decoder.GetChars(buffer, 0, bytes, chars, 0)


                messageData.Append(chars)
                tmpStr = messageData.ToString()
                If tmpStr.Substring(tmpStr.Length - 4) = vbLf & vbCrLf & Chr(0) Then
                    Exit Do
                End If
            Loop While Not (bytes = 0)


            Return messageData.ToString
        Catch eIO As IOException
            Return messageData.ToString
        Catch ex As Exception
            addLog(ex.ToString())
            Return ""
        Finally
            Try
                sslStream.Close()
            Catch ex As Exception
                '
            End Try
        End Try
    End Function
    Private Function ValidateServerCertificate() As Boolean
        Return True
    End Function

End Class
