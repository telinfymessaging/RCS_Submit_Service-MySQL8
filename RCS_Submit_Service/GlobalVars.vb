Imports System.IO
Imports System.Net
Imports Newtonsoft.Json

Module GlobalVars
    Public objSyncLock As Object = New Object()
    Public strMySQLConn As String = ""
    Public IsOSWindows As Boolean = False
    Public objSyncLockRCS_client As Object = New Object()
    Public strRCSBusinessURL As String = "https://asia-rcsbusinessmessaging.googleapis.com"

    Public Function getError(strErrorMsg As WebException) As String
        Try
            Dim resp = New StreamReader(strErrorMsg.Response.GetResponseStream()).ReadToEnd()
            Dim obj = JsonConvert.DeserializeObject(resp)
            Return obj.ToString()
        Catch ex As Exception
            addLog(strErrorMsg.ToString())
            addLog(ex.ToString())
            Return ""
        End Try
    End Function

End Module
