Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class ImShow_Basics : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
            If src.Width > 0 Then cv.Cv2.ImShow("color", src)
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            cv.Cv2.DestroyWindow("color")
        End Sub
    End Class





    Public Class ImShow_CV32FC3 : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            desc = "Experimenting with how to show an 32fc3 Mat file."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.testAllRunning Then Exit Sub ' when testing, this can occasionally fail - mysterious.
            cv.Cv2.ImShow("Point cloud", taskA.pointCloud)
            dst2 = taskA.pointCloud.Clone
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            cv.Cv2.DestroyWindow("Point cloud")
        End Sub
    End Class
End Namespace