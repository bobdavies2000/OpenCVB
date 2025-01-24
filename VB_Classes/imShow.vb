Imports cv = OpenCvSharp
Public Class ImShow_Basics : Inherits TaskParent
    Public Sub New()
        desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        cv.Cv2.ImShow("color", src)
    End Sub
End Class







Public Class ImShow_WaitKey : Inherits TaskParent
    Public Sub New()
        desc = "You can use the HighGUI WaitKey call to pause an algorithm and review output one frame at a time."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runFeature(src)
        cv.Cv2.ImShow("Hit space bar to advance to the next frame", dst2)
        cv.Cv2.WaitKey(1000) ' No need for waitkey with imshow in OpenCVB - finishing a buffer is the same thing so waitkey just delays by 1 second here.
    End Sub
End Class







Public Class ImShow_CV32FC3 : Inherits TaskParent
    Public Sub New()
        desc = "Experimenting with how to show an 32fc3 Mat file."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        cv.Cv2.ImShow("Point cloud", task.pointCloud)
        dst2 = task.pointCloud.Clone
    End Sub
End Class
