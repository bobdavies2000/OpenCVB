Imports cvb = OpenCvSharp
Public Class ImShow_Basics : Inherits VB_Parent
    Public Sub New()
        desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cvb.Cv2.ImShow("color", src)
    End Sub
End Class







Public Class ImShow_WaitKey : Inherits VB_Parent
    Dim feat As New Feature_Stable
    Public Sub New()
        desc = "You can use the HighGUI WaitKey call to pause an algorithm and review output one frame at a time."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        cvb.Cv2.ImShow("Hit space bar to advance to the next frame", feat.dst2)
        cvb.Cv2.WaitKey(1000) ' No need for waitkey with imshow in OpenCVB - finishing a buffer is the same thing so waitkey just delays by 1 second here.
        dst2 = feat.dst2
    End Sub
End Class







Public Class ImShow_CV32FC3 : Inherits VB_Parent
    Public Sub New()
        desc = "Experimenting with how to show an 32fc3 Mat file."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cvb.Cv2.ImShow("Point cloud", task.pointCloud)
        dst2 = task.pointCloud.Clone
    End Sub
End Class
