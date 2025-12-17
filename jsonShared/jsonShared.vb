Imports cv = OpenCvSharp

Namespace jsonShared
    Public Class Settings
        Public cameraName As String = "StereoLabs ZED 2/2I"
        Public cameraPresent As List(Of Boolean)
        Public cameraFound As Boolean
        Public resolutionsSupported As List(Of Boolean)
        Public cameraSupported As List(Of Boolean)
        Public camera640x480Support As List(Of Boolean)
        Public camera1920x1080Support As List(Of Boolean)

        Public MainFormLeft As Integer = 0
        Public MainFormTop As Integer = 0
        Public MainFormWidth As Integer = 1867
        Public MainFormHeight As Integer = 1134

        Public TreeViewLeft As Integer = 0
        Public TreeViewTop As Integer = 0
        Public TreeViewWidth As Integer = 300
        Public TreeViewHeight As Integer = 500

        Public sharpGLLeft As Integer = 0
        Public sharpGLTop As Integer = 0
        Public sharpGLWidth As Integer = 300
        Public sharpGLHeight As Integer = 500

        Public allOptionsLeft As Integer = 0
        Public allOptionsTop As Integer = 0
        Public allOptionsWidth As Integer = MainFormWidth
        Public allOptionsHeight As Integer = MainFormHeight

        Public algorithm As String
        Public algorithmHistory As New List(Of String)

        Public workRes As New cv.Size(320, 240)
        Public captureRes As New cv.Size(640, 480)
        Public displayRes As cv.Size
        Public FPSdisplay As Integer

        Public desiredFPS As Integer = 60
        Public testAllDuration As Integer = 5
        Public ShowAllOptions As Boolean
        Public Image_Basics_Name As String = ""
        Public plyFileName As String = ""
        Public VideoFileName As String = ""
    End Class
    Public Interface ISettingsUpdater
        Sub UpdateSetting(key As String, value As Object)
    End Interface
End Namespace