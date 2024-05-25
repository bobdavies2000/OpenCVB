Imports Newtonsoft.Json
Imports cv = OpenCvSharp
Imports System.IO

Namespace jsonClass

    Public Class ApplicationStorage
        Public algorithm As String
        Public algorithmGroup As String

        Public cameraIndex As Integer
        Public cameraName As String
        Public cameraPresent As List(Of Boolean)
        Public resolutionsSupported As List(Of Boolean)
        Public cameraSupported As List(Of Boolean)
        Public camera640x480Support As List(Of Boolean)
        Public camera1920x1080Support As List(Of Boolean)

        Public locationMain As cv.Vec4f
        Public locationPixelViewer As cv.Vec4f
        Public locationOpenGL As cv.Vec4f
        Public locationOptions As cv.Vec4f

        Public myntSDKready As Boolean
        Public zedSDKready As Boolean
        Public oakDSDKready As Boolean

        Public snap640 As Boolean
        Public snap320 As Boolean
        Public snapCustom As Boolean

        Public workingRes As cv.Size
        Public workingResIndex As Integer
        Public captureRes As cv.Size
        Public displayRes As cv.Size

        Public testAllDuration As Integer
        Public showConsoleLog As Boolean

        Public defaultFontSize As Integer
        Public defaultFontName As String

        Public treeButton As Boolean

        Public PixelViewerButton As Boolean

        Public fontInfo As Font
        Public desiredFPS As Integer

    End Class

    Public Class FileOperations
        Public jsonFileName As String
        Public Sub Save(storageList As List(Of ApplicationStorage))
            Using streamWriter = File.CreateText(jsonFileName)
                Dim serializer = New JsonSerializer With {.Formatting = Formatting.Indented}
                serializer.Serialize(streamWriter, storageList)
            End Using
        End Sub
        Public Function Load() As List(Of ApplicationStorage)
            Dim fileInfo As New FileInfo(jsonFileName)
            If fileInfo.Exists Then
                Using streamReader = New StreamReader(jsonFileName)
                    Dim json = streamReader.ReadToEnd()
                    Return JsonConvert.DeserializeObject(Of List(Of ApplicationStorage))(json)
                End Using
            End If
            Dim empty As New List(Of ApplicationStorage)

            Dim emptyApp As New ApplicationStorage
            emptyApp.cameraName = ""
            emptyApp.cameraIndex = 0
            emptyApp.workingRes = New cv.Size(320, 240)
            emptyApp.snap640 = True
            emptyApp.testAllDuration = 5
            emptyApp.showConsoleLog = False
            emptyApp.treeButton = True
            emptyApp.algorithmGroup = "<All VB.Net>"
            SaveSetting("OpenCVB", "OpenGLtaskX", "OpenGLtaskX", 0)
            SaveSetting("OpenCVB", "OpenGLtaskY", "OpenGLtaskY", 0)
            SaveSetting("OpenCVB", "OpenGLtaskWidth", "OpenGLtaskWidth", 512)

            empty.Add(emptyApp)
            Return empty
        End Function
    End Class
End Namespace