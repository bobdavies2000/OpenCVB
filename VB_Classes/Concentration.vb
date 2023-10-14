Imports cv = OpenCvSharp
Public Class Concentration_Basics : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public Sub New()
        labels = {"", "", "SideView", "TopView"}
        findCheckBox("Show Frustrum").Checked = False
        gOptions.FrameHistory.Value = 1 ' no history needed...
        desc = "Highlight where concentrations are highest in the Top and Side Views"
    End Sub
    Public Sub plotHighlights(histogram As cv.Mat, ByRef dst As cv.Mat, dstIndex As Integer)
        Static percentSlider = findSlider("Show top concentration %")

        Dim pts As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
        Dim avg As Double, Max As Single
        For y = 0 To histogram.Height - 1
            For x = 0 To histogram.Width - 1
                Dim val = histogram.Get(Of Single)(y, x)
                If val > 0 Then pts.Add(val, New cv.Point(x, y))
                avg += val
                If Max < val Then Max = val
            Next
        Next

        If pts.Count = 0 Then Exit Sub

        avg /= pts.Count
        For i = 0 To pts.Count * percentSlider.value / 100
            Dim pt = pts.ElementAt(i).Value
            dst.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        labels(dstIndex) = CStr(pts.Count) + " total points, max = " + Format(Max, fmt0) + ", Average = " + Format(Avg, fmt0)
    End Sub
    Public Sub RunVB(src as cv.Mat)
        heat.Run(src)

        dst2 = heat.dst2
        plotHighlights(heat.dst0, dst2, 2)

        dst3 = heat.dst3
        plotHighlights(heat.dst1, dst3, 3)
    End Sub
End Class








Public Class Concentration_PeakLines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public basics As New Concentration_Basics
    Public optimalRotation As Integer
    Public Sub New()
        labels(2) = "Grab the Y-axis rotation slider to manually review peaks."
        desc = "Rotate around Y-axis to find peak line length"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static rotateSlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static maxLength As Single
        Static peakRotation As Integer = -91

        basics.Run(src)
        dst2 = dst2 Or basics.dst2

        lines.Run(basics.dst2)

        For Each line In lines.sortLength
            Dim mps = lines.mpList(line.Value)
            dst3.Line(mps.p1, mps.p2, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        If lines.sortLength.Count > 0 Then
            Dim nextLen = lines.sortLength.ElementAt(0).Key
            If maxLength < nextLen Then
                maxLength = nextLen
                peakRotation = rotateSlider.Value
            End If
        End If

        labels(3) = "Longest line = " + CStr(maxLength) + " pixels at " + CStr(If(optimalRotation = -91, peakRotation, optimalRotation)) + " degrees"

        Static saveRotation = rotateSlider.Value
        Static automateRotate As Boolean = True
        If automateRotate Then rotateSlider.Value += 1
        If Math.Abs(saveRotation - rotateSlider.Value) <> 1 And saveRotation <> 90 Then automateRotate = False
        saveRotation = rotateSlider.Value
        If rotateSlider.Value >= 90 Then
            optimalRotation = peakRotation
            maxLength = 0
            peakRotation = -90
            rotateSlider.Value = -90
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If
    End Sub
End Class