Imports cvb = OpenCvSharp
Public Class Erode_Basics : Inherits VB_Parent
    Public options As New Options_Erode
    Public Sub New()
        UpdateAdvice(traceName + ": use local options to control erosion.")
        desc = "Erode the image provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        If options.noshape Or options.iterations = 0 Then dst2 = src Else dst2 = src.Erode(options.element, Nothing, options.iterations)

        If standaloneTest() Then
            dst3 = task.depthRGB.Erode(options.element, Nothing, options.iterations)
            labels(3) = "Eroded Depth " + CStr(options.iterations) + " times"
        End If
        labels(2) = "Eroded BGR " + CStr(-options.iterations) + " times"
    End Sub
End Class








Public Class Erode_CloudXY : Inherits VB_Parent
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Dim erodeMask As New Erode_Basics
    Public Sub New()
        FindSlider("Dilate Iterations").Value = 2
        FindRadio("Erode shape: Ellipse").Checked = True
        labels = {"", "", "Eroded point cloud X", "Erode point cloud Y"}
        desc = "Erode depth and then find edges"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static dilateSlider = FindSlider("Dilate Iterations")
        Static erodeSlider = FindSlider("Erode Iterations")
        erodeMask.Run(task.depthMask)
        dst1 = Not erodeMask.dst2

        dilate.Run(task.pcSplit(0))
        Dim mm as mmData = GetMinMax(dilate.dst2, erodeMask.dst2)
        dst2 = (dilate.dst2 - mm.minVal) / (mm.maxVal - mm.minVal)
        dst2.SetTo(0, dst1)

        erode.Run(task.pcSplit(1))
        mm = GetMinMax(dilate.dst2, erodeMask.dst2)
        dst3 = (erode.dst2 - mm.minVal) / (mm.maxVal - mm.minVal)
        dst3.SetTo(0, dst1)
    End Sub
End Class






Public Class Erode_DepthSeed : Inherits VB_Parent
    Dim erode As New Erode_Basics
    Dim options As New Options_Erode
    Public Sub New()
        desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        cvb.Cv2.Erode(task.pcSplit(2), dst0, erode.options.element)
        dst0 = task.pcSplit(2) - dst0
        dst3 = dst0.LessThan(options.flatDepth).ToMat
        dst1 = task.pcSplit(2).GreaterThan(0).ToMat
        dst1.SetTo(0, task.pcSplit(2).GreaterThan(task.MaxZmeters))
        dst3 = dst3 And dst1
        dst2.SetTo(0)
        task.depthRGB.CopyTo(dst2, dst3)
    End Sub
End Class









Public Class Erode_Dilate : Inherits VB_Parent
    Dim options As New Options_Dilate
    Public Sub New()
        desc = "Erode and then dilate with MorphologyEx on the input image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        cvb.Cv2.MorphologyEx(src, dst2, cvb.MorphTypes.Close, options.element)
        cvb.Cv2.MorphologyEx(dst2, dst2, cvb.MorphTypes.Open, options.element)
    End Sub
End Class

