Imports cvb = OpenCvSharp

Public Class Artifacts_LowRes : Inherits VB_Parent
    Dim options As New Options_Resize
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpFillOutliers").Enabled = False
        desc = "Build a low-res image to start the process of finding artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim pct = options.resizePercent
        dst2 = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Artifacts_Reduction : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Build a lowRes image after reduction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst3 = reduction.dst3

        lowRes.Run(dst3)
        dst2 = lowRes.dst2
    End Sub
End Class
