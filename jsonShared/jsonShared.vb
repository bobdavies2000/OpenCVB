Imports cv = OpenCvSharp
Namespace jsonShared
    Public Class Settings
        Public algorithm As String = "AddWeighted_Basics"
        Public algorithmHistory As New List(Of String)

        Public allOptionsLeft As Integer = 0
        Public allOptionsTop As Integer = 0
        Public allOptionsWidth As Integer = MainFormWidth
        Public allOptionsHeight As Integer = MainFormHeight

        Public cameraName As String = "" ' "StereoLabs ZED 2/2i"
        Public cameraPresent As List(Of Boolean)
        Public cameraFound As Boolean
        Public cameraSupported As List(Of Boolean)

        Public captureRes As New cv.Size(640, 480)

        Public desiredFPS As Integer = 60
        Public displayRes As cv.Size = New cv.Size(320, 240)

        Public FPSPaintTarget As Integer = 30
        Public Image_Basics_Name As String = ""

        Public MainFormLeft As Integer = 0
        Public MainFormTop As Integer = 0
        Public MainFormWidth As Integer = 1867
        Public MainFormHeight As Integer = 1134

        Public plyFileName As String = ""
        Public resolutionsSupported As List(Of Boolean)

        Public sharpGLLeft As Integer = 0
        Public sharpGLTop As Integer = 0
        Public sharpGLWidth As Integer = 300
        Public sharpGLHeight As Integer = 500

        Public ShowAllOptions As Boolean
        Public testAllDuration As Integer = 5

        Public TreeViewLeft As Integer = 0
        Public TreeViewTop As Integer = 0
        Public TreeViewWidth As Integer = 515
        Public TreeViewHeight As Integer = 500

        Public VideoFileName As String = ""
        Public workRes As New cv.Size(320, 240)
    End Class
    Public Interface ISettingsUpdater
        Sub UpdateSetting(key As String, value As Object)
    End Interface
End Namespace