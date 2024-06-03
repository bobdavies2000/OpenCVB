Imports cv = OpenCvSharp
Public Class ImShow_Basics : Inherits VB_Parent
    Public Sub New()
        desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        cv.Cv2.ImShow("color", src)
    End Sub
End Class







Public Class ImShow_WaitKey : Inherits VB_Parent
    Dim vDemo As New Voronoi_Basics
    Public Sub New()
        desc = "You can use the HighGUI WaitKey call to pause an algorithm and review output one frame at a time."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        vDemo.Run(src)
        cv.Cv2.ImShow("Hit space bar to advance to the next frame", vDemo.dst2)
        cv.Cv2.WaitKey(1000) ' It will halt the test all run if 0 but 0 is the useful value for debugging interactively.
        dst2 = vDemo.dst2
    End Sub
End Class







Public Class ImShow_CV32FC3 : Inherits VB_Parent
    Public Sub New()
        desc = "Experimenting with how to show an 32fc3 Mat file."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cv.Cv2.ImShow("Point cloud", task.pointCloud)
        dst2 = task.pointCloud.Clone
    End Sub
End Class
