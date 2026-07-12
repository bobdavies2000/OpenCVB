Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class ImShow_Basics : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
        If src.Width > 0 Then ImShow("color", src)
    End Sub
    Protected Overrides Sub Finalize()
        If task.testAllRunning = False Then DestroyWindow("color")
    End Sub
End Class





Public Class ImShow_CV32FC3 : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        desc = "Experimenting with how to show an 32fc3 Mat file."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
        ImShow("Point cloud", task.pointCloud)
        dst2 = task.pointCloud.Clone
    End Sub
    Protected Overrides Sub Finalize()
        DestroyWindow("Point cloud")
    End Sub
End Class
