Imports cv = OpenCvSharp
Imports System.Threading
Public Class Texture_Basics : Inherits TaskParent
    Dim ellipse As New Draw_Ellipses
    Public texture As New cv.Mat
    Public tRect As cv.Rect
    Dim texturePop As Integer
    Public tChange As Boolean ' if the texture hasn't changed this will be false.
    Public Sub New()
        task.gOptions.GridSlider.Value = Math.Min(CInt(dst2.Width / 8), task.gOptions.GridSlider.Maximum)

        desc = "find the best sample 256x256 texture of a mask"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Or src.Channels() <> 1 Then
            ellipse.Run(src)
            dst2 = ellipse.dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray)
            dst2 = dst2.ConvertScaleAbs(255)
            dst3 = ellipse.dst2.Clone
            dst3.SetTo(cv.Scalar.Yellow, task.gridMask)
        Else
            dst2 = src
        End If

        tChange = True
        If texturePop > 0 Then
            Dim nextCount = dst2(tRect).CountNonZero
            If nextCount >= texturePop * 0.95 Then tChange = False
        End If
        If tChange Then
            Dim sortcounts As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
            For Each roi In task.gridRects
                sortcounts.Add(dst2(roi).CountNonZero, roi)
            Next
            If standaloneTest() Then dst3.Rectangle(sortcounts.ElementAt(0).Value, white, 2)
            tRect = sortcounts.ElementAt(0).Value
            texture = task.color(tRect)
            texturePop = dst2(tRect).CountNonZero
        End If
        If standaloneTest() Then dst3.Rectangle(tRect, white, 2)
    End Sub
End Class
