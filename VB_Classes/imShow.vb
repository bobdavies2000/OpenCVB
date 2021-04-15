Imports cv = OpenCvSharp
Public Class ImShow_Basics : Inherits VBparent
    Public Sub New()
        task.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
        ' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        cv.Cv2.ImShow("color", src)
    End Sub
End Class







Public Class ImShow_WaitKey : Inherits VBparent
    Dim vDemo As Voronoi_Basics
    Public Sub New()
        vDemo = New Voronoi_Basics()

        task.desc = "You can use the HighGUI WaitKey call to pause an algorithm and review output one frame at a time."
        ' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        vDemo.Run(src)
        cv.Cv2.ImShow("Hit space bar to advance to the next frame", vDemo.dst1)
        cv.Cv2.WaitKey(1000) ' It will halt the test all run if 0 but 0 is the useful value for debugging interactively.
        dst1 = vDemo.dst1
    End Sub
End Class

