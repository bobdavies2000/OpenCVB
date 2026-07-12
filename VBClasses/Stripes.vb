Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Stripes_Basics : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(0)
        Dim depth32f As cv.Mat = src * 1000
        Dim depth32S As New cv.Mat
        depth32f.ConvertTo(depth32S, cv.MatType.CV_32S)

        Dim reduction = task.fOptions.ReductionColor.Value
        Dim mm = GetMinMax(depth32S, task.depthmask)
        dst2 = Abs(depth32S) / reduction
        Dim maxVal = Math.Min(Math.Abs(mm.minVal), mm.maxVal) ' symmetric around 0
        If maxVal = 0 Then maxVal = mm.maxVal ' symmetric around 0 except for Z where all values are above 0
        classCount = maxVal \ reduction

        dst3 = Palettize(dst2)
        mm = GetMinMax(dst2, task.depthmask)
        dst2 *= 255 / mm.maxVal
    End Sub
End Class






Public Class Stripes_CloudX : Inherits TaskParent
    Dim stripes As New Stripes_Basics
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stripes.Run(task.pcSplit(0))
        dst2 = stripes.dst2
        dst3 = stripes.dst3
    End Sub
End Class






Public Class Stripes_CloudY : Inherits TaskParent
    Dim stripes As New Stripes_Basics
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stripes.Run(task.pcSplit(1))
        dst2 = stripes.dst2
        dst3 = stripes.dst3
    End Sub
End Class






Public Class Stripes_CloudZ : Inherits TaskParent
    Dim stripes As New Stripes_Basics
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stripes.Run(task.pcSplit(2))
        dst2 = stripes.dst2
        dst3 = stripes.dst3
    End Sub
End Class





Public Class Stripes_XYZ : Inherits TaskParent
    Dim stripeX As New Stripes_CloudX
    Dim stripeY As New Stripes_CloudY
    Dim stripeZ As New Stripes_CloudZ
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Stripes in the X-direction", "Stripes in the Y-direction", "Stripes in the Z-direction"}
        desc = "Outline stripes in all 3 dimensions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stripeX.Run(task.pcSplit(0))
        dst1 = stripeX.dst3.Clone
        stripeY.Run(task.pcSplit(1))
        dst2 = stripeY.dst3.Clone
        stripeZ.Run(task.pcSplit(2))
        dst3 = stripeZ.dst3
    End Sub
End Class






Public Class XR_Stripes_Histogram : Inherits TaskParent
    Dim stripes As New Stripes_XYZ
    Public Sub New()
        desc = "Show a histogram for the output of stripes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stripes.Run(src)
        CvtColor(stripes.dst1, dst1, cv.ColorConversionCodes.BGR2GRAY)
        CvtColor(stripes.dst2, dst2, cv.ColorConversionCodes.BGR2GRAY)
        CvtColor(stripes.dst3, dst3, cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
