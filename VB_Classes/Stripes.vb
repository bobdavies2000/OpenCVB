Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Stripes_Basics : Inherits TaskParent
    Dim classCount As Integer
    Public Sub New()
        task.redOptions.ReductionSliders.Enabled = True
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim reductionVal = task.redOptions.SimpleReductionBar.Value

        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(0)
        Dim depth32f As cv.Mat = src * 1000
        Dim depth32S As New cv.Mat
        depth32f.ConvertTo(depth32S, cv.MatType.CV_32S)

        Dim mm = GetMinMax(depth32S, task.depthMask)
        dst2 = cv.Cv2.Abs(depth32S) / reductionVal
        Dim maxVal = Math.Min(Math.Abs(mm.minVal), mm.maxVal) ' symmetric around 0
        If maxVal = 0 Then maxVal = mm.maxVal ' symmetric around 0 except for Z where all values are above 0
        classCount = CInt(maxVal / reductionVal)

        dst3 = ShowPalette(dst2 * 255 / classCount)
        mm = GetMinMax(dst2, task.depthMask)
        dst2 *= 255 / mm.maxVal
    End Sub
End Class






Public Class Stripes_CloudX : Inherits TaskParent
    Dim stripes As New Stripes_Basics
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        stripes.run(task.pcSplit(0))
        dst2 = stripes.dst2
        dst3 = stripes.dst3
    End Sub
End Class






Public Class Stripes_CloudY : Inherits TaskParent
    Dim stripes As New Stripes_Basics
    Public Sub New()
        desc = "Create stripes throughout the image with reduction"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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
    Public Overrides sub RunAlg(src As cv.Mat)
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
    Public Overrides sub RunAlg(src As cv.Mat)
        stripeX.Run(task.pcSplit(0))
        dst1 = stripeX.dst3.Clone
        stripeY.Run(task.pcSplit(1))
        dst2 = stripeY.dst3.Clone
        stripeZ.Run(task.pcSplit(2))
        dst3 = stripeZ.dst3
    End Sub
End Class






Public Class Stripes_Histogram : Inherits TaskParent
    Dim stripes As New Stripes_XYZ
    Public Sub New()
        desc = "Show a histogram for the output of stripes"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        stripes.Run(src)
        dst1 = stripes.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = stripes.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = stripes.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
