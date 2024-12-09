Imports cvb = OpenCvSharp
'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class PyrFilter_Basics : Inherits TaskParent
    Dim options As New Options_PyrFilter
    Public Sub New()
        desc = "Use PyrMeanShiftFiltering to segment an image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        cvb.Cv2.PyrMeanShiftFiltering(src, dst2, options.spatialRadius, options.colorRadius, options.maxPyramid)
    End Sub
End Class







Public Class PyrFilter_RedCloud : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim pyr As New PyrFilter_Basics
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", "PyrFilter output before reduction"}
        desc = "Use RedColor to segment the output of PyrFilter"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        pyr.Run(src)
        dst3 = pyr.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        reduction.Run(dst3)

        task.redC.Run(reduction.dst2)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)
    End Sub
End Class