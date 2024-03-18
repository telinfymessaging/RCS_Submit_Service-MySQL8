#Region "Imports"
Imports MySql.Data.MySqlClient
Imports System.IO
#End Region


Module DBFunctions

    Public Sub RCS_Log(ByVal logdesc As String)

        Dim con As MySqlConnection = New MySqlConnection(strMySQLConn)

        Try
            con.Open()
            Dim cmd As MySqlCommand = New MySqlCommand("RCS_Log", con)
            cmd.CommandType = CommandType.StoredProcedure

            cmd.Parameters.AddWithValue("@logDesc", logdesc)
            cmd.Parameters("@logDesc").Direction = ParameterDirection.Input
            cmd.Parameters("@logDesc").DbType = DbType.String


            cmd.ExecuteNonQuery()
            Exit Sub
        Catch ex As Exception
            Try
                Dim swriter As StreamWriter
                Dim strDirPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)
                'strDirPath = strDirPath.Replace("file:/", "")
                Dim os As OperatingSystem = Environment.OSVersion
                If os.VersionString.Contains("Windows") = False Then
                    strDirPath = strDirPath.Replace("file:/", "/")
                Else
                    strDirPath = strDirPath.Replace("file:\", "")
                    IsOSWindows = True
                End If
                swriter = File.AppendText(strDirPath & "/Log" & Now().ToString("yyyyMMdd") & ".txt")
                swriter.WriteLine(ex.ToString())
                swriter.WriteLine(logdesc)
                swriter.Close()
            Catch ex1 As Exception
                '
            End Try
        Finally
            Try
                If con.State = ConnectionState.Open Then con.Close()
            Catch ex As Exception
                '
            End Try
        End Try
        Try
            Dim swriter As StreamWriter
            Dim strDirPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)
            'strDirPath = strDirPath.Replace("file:/", "")
            Dim os As OperatingSystem = Environment.OSVersion
            If os.VersionString.Contains("Windows") = False Then
                strDirPath = strDirPath.Replace("file:/", "/")
            Else
                strDirPath = strDirPath.Replace("file:\", "")
                IsOSWindows = True
            End If
            swriter = File.AppendText(strDirPath & "/Log" & Now().ToString("yyyyMMdd") & ".txt")
            swriter.WriteLine(logdesc)
            swriter.Close()
        Catch ex1 As Exception
            '
        End Try
    End Sub
    Public Sub addLog(ByVal strMsg As String)
        Dim eventLog As EventLog = New EventLog("Application")
        eventLog.Source = "Application"
        Try
            SyncLock objSyncLock

                Try
                    Call RCS_Log(strMsg)
                    Exit Sub
                Catch ex As Exception
                    eventLog.WriteEntry(ex.ToString(), EventLogEntryType.Information, 101)
                End Try
                Dim swriter As StreamWriter
                Dim strDirPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)
                If IsOSWindows = False Then
                    strDirPath = strDirPath.Replace("file:/", "/")
                Else
                    strDirPath = strDirPath.Replace("file:\", "")
                End If
                swriter = File.AppendText(strDirPath & "\logs\" & Now().ToString("yyyyMMdd") & ".txt")
                swriter.WriteLine(Now() & "===" & strMsg)
                swriter.Close()
            End SyncLock
        Catch ex As Exception
            EventLog.WriteEntry(ex.ToString(), EventLogEntryType.Information, 101)
        End Try
    End Sub
    Public Function Get_RCS_data(ByVal n As UInt32) As DataSet

        Dim con As MySqlConnection = New MySqlConnection(strMySQLConn)
        Dim da As MySqlDataAdapter = New MySqlDataAdapter
        Dim ds As DataSet = New DataSet

        Try

            con.Open()

            Dim cmd As MySqlCommand = New MySqlCommand("get_rcs_data", con)
            cmd.CommandType = CommandType.StoredProcedure


            cmd.Parameters.AddWithValue("@n", n)
            cmd.Parameters("@n").DbType = DbType.Int32
            cmd.Parameters("@n").Direction = ParameterDirection.Input


            da.SelectCommand = cmd
            da.Fill(ds)
            Return ds

        Catch eTheardAbort As Threading.ThreadAbortException
            Return ds
        Catch ex As Exception
            addLog(ex.ToString)
            Return ds
        Finally
            If con.State = ConnectionState.Open Then con.Close()
        End Try
    End Function

    Public Function Delete_RCS_data(ByVal pID As UInt32, pJob_ID As UInt32, strSubmitResponse As String, pServerMessage As String) As DataSet

        Dim con As MySqlConnection = New MySqlConnection(strMySQLConn)
        Dim da As MySqlDataAdapter = New MySqlDataAdapter
        Dim ds As DataSet = New DataSet

        Try

            con.Open()

            Dim cmd As MySqlCommand = New MySqlCommand("delete_t_rcs", con)
            cmd.CommandType = CommandType.StoredProcedure


            cmd.Parameters.AddWithValue("@pID", pID)
            cmd.Parameters("@pID").DbType = DbType.UInt32
            cmd.Parameters("@pID").Direction = ParameterDirection.Input

            cmd.Parameters.AddWithValue("@pJob_ID", pJob_ID)
            cmd.Parameters("@pJob_ID").DbType = DbType.UInt32
            cmd.Parameters("@pJob_ID").Direction = ParameterDirection.Input

            cmd.Parameters.AddWithValue("@rcs_resp_data", strSubmitResponse)
            cmd.Parameters("@rcs_resp_data").DbType = DbType.String
            cmd.Parameters("@rcs_resp_data").Direction = ParameterDirection.Input



            da.SelectCommand = cmd
            da.Fill(ds)
            Return ds

        Catch eTheardAbort As Threading.ThreadAbortException
            Return ds
        Catch ex As Exception
            addLog("JSON Data:" & pServerMessage)
            'addLog(ex.ToString)
            Return ds
        Finally
            If con.State = ConnectionState.Open Then con.Close()
        End Try
    End Function

End Module
