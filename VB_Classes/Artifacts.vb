Imports cvb = OpenCvSharp

Public Class Artifacts_LowRes : Inherits VB_Parent
    Dim options As New Options_Resize
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        desc = "Build a low-res image to start the process of finding artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim pct = options.resizePercent
        dst3 = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst2 = dst3.Resize(New cvb.Size(src.Width / pct, src.Height / pct))
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






Public Class Artifacts_Features : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim feat As New Feature_Basics
    Public Sub New()
        FindSlider("Resize Percentage (%)").Value = 20
        task.gOptions.SetDotSize(1)
        desc = "Find features in a low res image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        dst2 = lowRes.dst2

        feat.Run(lowRes.dst3)
        dst3 = feat.dst2
    End Sub
End Class