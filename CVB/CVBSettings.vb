Imports System.IO
Imports Newtonsoft.Json

Namespace CVB
    Public Class CVBSettings
        Public FormLeft As Integer = 0
        Public FormTop As Integer = 0
        Public FormWidth As Integer = 1867
        Public FormHeight As Integer = 1134
        Public algorithm As String
    End Class

    Public Class CVBSettingsIO
        Private jsonFileName As String
        Public Sub New(fileName As String)
            jsonFileName = fileName
        End Sub

        Public Function Load() As CVBSettings
            Dim fileInfo As New FileInfo(jsonFileName)
            If fileInfo.Exists Then
                Try
                    Using streamReader As New StreamReader(jsonFileName)
                        Dim json = streamReader.ReadToEnd()
                        If json <> "" Then
                            Return JsonConvert.DeserializeObject(Of CVBSettings)(json)
                        End If
                    End Using
                Catch ex As Exception
                    ' If deserialization fails, return default settings
                End Try
            End If
            ' Return default settings if file doesn't exist or deserialization fails
            Return New CVBSettings()
        End Function

        Public Sub Save(settings As CVBSettings)
            Try
                Using streamWriter As New StreamWriter(jsonFileName)
                    Dim serializer As New JsonSerializer With {.Formatting = Formatting.Indented}
                    serializer.Serialize(streamWriter, settings)
                End Using
            Catch ex As Exception
                ' Log error if needed, but don't throw
            End Try
        End Sub
    End Class
End Namespace

