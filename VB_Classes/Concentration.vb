Imports cv = OpenCvSharp
Public Class Concentration_Basics : Inherits VBparent
    Public sideview As New Histogram_SideView2D
    Public topview As New Histogram_TopView2D
    Public ptSide As New SortedList(Of Integer, Integer)
    Public ptTop As New SortedList(Of Integer, Integer)
    Public maxSide As Single
    Public maxTop As Single
    Public avgSide As Single
    Public avgTop As Single
    Dim unsorted As New List(Of Single)
    Public drawLines As Boolean
    Dim resizeSlider As Windows.Forms.TrackBar
    Public markerColor = cv.Scalar.Yellow
    Public showHistogram As Boolean
    Public Sub New()
        resizeSlider = findSlider("Resize Factor x100")
        resizeSlider.Value = 10

        label1 = "SideView"
        label2 = "TopView"
        task.desc = "Highlight a fixed number of histogram projections where concentrations are highest"
    End Sub
    Public Sub plotHighlights(histOutput As cv.Mat, dst As cv.Mat, sideRun As Boolean)
        Dim resizeFactor = resizeSlider.Value / 100

        Dim tmp = histOutput.Resize(New cv.Size(CInt(histOutput.Width * resizeFactor), CInt(histOutput.Height * resizeFactor)), 0, 0, cv.InterpolationFlags.Nearest)
        Dim pts As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
        unsorted.Clear()
        For y = 0 To tmp.Height - 1
            For x = 0 To tmp.Width - 1
                Dim val = tmp.Get(Of Single)(y, x)
                If val > 1000 And (x = 0 Or y = tmp.Height - 1) Then val = 0 ' this eliminates the histogram entry for missing depth...
                If val > 5 Then
                    pts.Add(val, New cv.Point(CInt(x / resizeFactor), CInt(y / resizeFactor)))
                    unsorted.Add(val)
                End If
            Next
        Next

        Dim ptList = If(sideRun, ptSide, ptTop)
        For i = 0 To pts.Count - 1
            Dim pt = pts.ElementAt(i).Value
            If drawLines Then
                Dim p1 = New cv.Point(pt.X, pt.Y - task.dotSize - 3)
                Dim p2 = New cv.Point(pt.X, pt.Y + task.dotSize + 3)
                If sideRun = False Then
                    p1 = New cv.Point(pt.X - task.dotSize - 3, pt.Y)
                    p2 = New cv.Point(pt.X + task.dotSize + 3, pt.Y)
                End If
                dst.Line(p1, p2, markerColor, task.lineWidth + 2)
            Else
                dst.Circle(pt, task.dotSize + 2, markerColor, -1, task.lineType)
            End If
            Dim distance = CInt(task.maxZ * 1000 * If(sideRun, pt.X / dst1.Width, pt.Y / dst1.Height))
            If ptList.ContainsKey(distance) Then ptList(distance) += 1 Else ptList.Add(distance, 1)
        Next
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        ptSide.Clear()
        ptTop.Clear()

        sideview.Run(src)
        If standalone Or showHistogram Then dst1 = sideview.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR) Else dst1.SetTo(0)
        plotHighlights(sideview.originalHistOutput, dst1, True)
        If unsorted.Count > 0 Then
            maxSide = unsorted.Max()
            avgSide = unsorted.Average()
            If standalone Then
                setTrueText(CStr(unsorted.Count) + " points" + vbCrLf + "max = " + CStr(maxSide) + vbCrLf + "Average = " + Format(avgSide, "#0.0"))
            End If
        End If

        topview.Run(src)
        If standalone Or showHistogram Then dst2 = topview.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR) Else dst2.SetTo(0)
        plotHighlights(topview.originalHistOutput, dst2, False)
        If unsorted.Count > 0 Then
            maxTop = unsorted.Max()
            avgTop = unsorted.Average()
            If standalone Then
                setTrueText(CStr(unsorted.Count) + " points" + vbCrLf + "max = " + CStr(maxTop) + vbCrLf + "Average = " + Format(avgTop, "#0.0"), 10, dst1.Height - 100, 3)
            End If
        End If
    End Sub
End Class







Public Class Concentration_BothViews : Inherits VBparent
    Public histC As New Concentration_Basics
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Use Lines instead of Dots to show concentration points - uncheck will use Dots"
            check.Box(1).Text = "Show histogram data (white)"
        End If
        task.desc = "Monitor the histogram concentration points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static dotCheck = findCheckBox("Use Lines instead of Dots to show concentration points - uncheck will use Dots")
        Static histCheck = findCheckBox("Show histogram data (white)")
        histC.drawLines = dotCheck.checked
        histC.showHistogram = histCheck.checked
        histC.Run(src)
        dst1 = histC.dst1
        dst2 = histC.dst2
    End Sub
End Class









Public Class Concentration_Peaks : Inherits VBparent
    Dim plot As New Plot_OverTime
    Dim dots As New Concentration_BothViews
    Dim mats As New Mat_4to1
    Public Sub New()
        plot.plotCount = 1
        plot.minScale = 0

        label1 = "SideView, TopView, plot - Grab Y rotate slider..."
        label2 = "Average is blue"
        task.desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static rotateSlider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")
        Static maxAverage As Single
        Static peakRotation As Integer

        dots.Run(src)
        task.ttTextData.Clear()

        mats.mat(0) = dots.dst1
        mats.mat(1) = dots.dst2
        If maxAverage < dots.histC.avgSide Then
            maxAverage = dots.histC.avgSide
            peakRotation = rotateSlider.value
        End If
        plot.plotData = New cv.Scalar(dots.histC.avgSide, 0, 0)
        plot.maxScale = 30
        plot.Run(Nothing)
        dst2 = plot.dst1

        mats.Run(Nothing)
        dst1 = mats.dst1

        Static saveRotation = rotateSlider.value
        Static automateRotate As Boolean = True
        If automateRotate Then rotateSlider.value += 1
        If Math.Abs(saveRotation - rotateSlider.value) <> 1 And saveRotation <> 90 Then automateRotate = False
        saveRotation = rotateSlider.value
        If rotateSlider.value >= 90 Then
            maxAverage = 0
            peakRotation = -90
            rotateSlider.value = -90
        End If

        label2 = "Peak average = " + CStr(CInt(maxAverage)) + " at " + CStr(peakRotation) + " degrees"
    End Sub
End Class







Public Class Concentration_PeakLines : Inherits VBparent
    Dim lines As New Line_Basics
    Dim mats As New Mat_4to1
    Public histC As New Concentration_Basics
    Public optimalRotation As Integer
    Public Sub New()
        histC.drawLines = True
        histC.markerColor = cv.Scalar.Gray
        label1 = "Grab the Y-axis rotation slider to manually review peaks."
        task.desc = "Rotate around Y-axis to find peak line length"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static rotateSlider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")
        Static maxLength As Single
        Static peakRotation As Integer = -91

        histC.Run(src)
        mats.mat(0) = histC.dst1
        mats.mat(1) = histC.dst2

        lines.Run(histC.dst1)
        dst2 = lines.dst1

        If lines.sortlines.Count > 0 Then
            Dim nextLen = CInt(lines.sortlines.ElementAt(0).Value.Item4)
            If maxLength < nextLen Then
                maxLength = nextLen
                peakRotation = rotateSlider.value
            End If
        End If

        label2 = "Longest line = " + CStr(maxLength) + " pixels at " + CStr(If(optimalRotation = -91, peakRotation, optimalRotation)) + " degrees"

        Static saveRotation = rotateSlider.value
        Static automateRotate As Boolean = True
        If automateRotate Then rotateSlider.value += 1
        If Math.Abs(saveRotation - rotateSlider.value) <> 1 And saveRotation <> 90 Then automateRotate = False
        saveRotation = rotateSlider.value
        If rotateSlider.value >= 90 Then
            optimalRotation = peakRotation
            maxLength = 0
            peakRotation = -90
            rotateSlider.value = -90
        End If

        mats.Run(Nothing)
        dst1 = mats.dst1
    End Sub
End Class