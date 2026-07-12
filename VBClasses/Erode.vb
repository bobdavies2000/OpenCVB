Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Erode_Basics : Inherits TaskParent
    Public options As New Options_Erode
    Public Sub New()
        desc = "Erode the image provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If options.noshape Or options.iterations = 0 Then dst2 = src Else Erode(src, dst2, options.element, Nothing, options.iterations)

        If standaloneTest() Then
            Erode(task.depthRGB, dst3, options.element, Nothing, options.iterations)
            labels(3) = "Eroded Depth " + CStr(options.iterations) + " times"
        End If
        labels(2) = "Eroded BGR " + CStr(-options.iterations) + " times"
    End Sub
End Class








Public Class XR_Erode_CloudXY : Inherits TaskParent
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Dim erodeMask As New Erode_Basics
    Public Sub New()
        OptionParent.FindSlider("Dilate Iterations").Value = 2
        OptionParent.findRadio("Erode shape: Ellipse").Checked = True
        labels = {"", "", "Eroded cv.Point cloud X", "Erode cv.Point cloud Y"}
        desc = "Erode depth and then find edges"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static dilateSlider = OptionParent.FindSlider("Dilate Iterations")
        Static erodeSlider = OptionParent.FindSlider("Erode Iterations")
        erodeMask.Run(task.depthmask)
        dst1 = Not erodeMask.dst2

        dilate.Run(task.pcSplit(0))
        Dim mm As mmData = GetMinMax(dilate.dst2, erodeMask.dst2)
        dst2 = (dilate.dst2 - mm.minVal) / (mm.maxVal - mm.minVal)
        dst2.SetTo(0, dst1)

        erode.Run(task.pcSplit(1))
        mm = GetMinMax(dilate.dst2, erodeMask.dst2)
        dst3 = (erode.dst2 - mm.minVal) / (mm.maxVal - mm.minVal)
        dst3.SetTo(0, dst1)
    End Sub
End Class






Public Class XR_Erode_DepthSeed : Inherits TaskParent
    Dim options As New Options_Erode
    Public Sub New()
        desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Erode(task.pcSplit(2), dst0, options.element)
        dst0 = task.pcSplit(2) - dst0
        dst3 = dst0.LessThan(options.flatDepth).ToMat
        dst1 = task.pcSplit(2).GreaterThan(0).ToMat
        dst1.SetTo(0, task.pcSplit(2).GreaterThan(task.MaxZmeters))
        dst3 = dst3 And dst1
        dst2.SetTo(0)
        task.depthRGB.CopyTo(dst2, dst3)
    End Sub
End Class









Public Class XR_Erode_Dilate : Inherits TaskParent
    Dim options As New Options_Dilate
    Public Sub New()
        desc = "Erode and then dilate with MorphologyEx on the input image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        MorphologyEx(src, dst2, MorphTypes.Close, options.element)
        MorphologyEx(dst2, dst2, MorphTypes.Open, options.element)
    End Sub
End Class


