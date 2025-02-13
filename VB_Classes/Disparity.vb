Imports cv = OpenCvSharp
Public Class Disparity_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public rightView As cv.Mat
    Public rect As cv.Rect
    Public matchRect As cv.Rect
    Public Sub New()
        task.ClickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        optiBase.FindSlider("Depth Cell Size").Value = 32
        desc = "Given a depth cell, find the match in the right view image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        For Each idd In task.iddList
            If idd.lRect.Height >= 8 Then dst2.Rectangle(idd.lRect, 255, task.lineWidth)
        Next
        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        rect = task.dCell.grid.gridRectsAll(index)

        match.template = dst2(rect)
        Dim maxDisparity As Integer = 128
        match.searchRect = New cv.Rect(Math.Max(0, rect.X - maxDisparity), rect.Y,
                                 rect.BottomRight.X - rect.X + maxDisparity, rect.Height)
        If standalone Then rightView = task.rightView
        match.Run(rightView)
        dst3 = rightView
        matchRect = match.matchRect
        If match.searchRect.Height >= 8 Then
            dst2.Rectangle(rect, 255, task.lineWidth)
            dst3.Rectangle(match.searchRect, 255, task.lineWidth)
            dst3.Rectangle(match.matchRect, 255, task.lineWidth + 1)
        Else
            dst2(rect).CopyTo(dst3(match.matchRect))
            dst2.Rectangle(rect, 255, task.lineWidth)
        End If
        labels(3) = "Correlation = " + Format(match.correlation, fmt3) + " with disparity = " +
                     CStr(rect.X - match.matchRect.X) + " pixels"
    End Sub
End Class






Public Class Disparity_Manual : Inherits TaskParent
    Public correlations As New List(Of Single), means As New List(Of Single), stdevs As New List(Of Single)
    Public searchRect As cv.Rect, rect As cv.Rect
    Public matchRect As cv.Rect
    Public bestCorrelation As Single, MeanDiff As Single, StdevDiff As Single
    Public rightView As cv.Mat
    Public Sub New()
        If Math.Abs(task.workingRes.Width - 672) < 10 Then task.gOptions.LineWidth.Value = 2
        optiBase.FindSlider("Depth Cell Size").Value = 32
        labels(2) = "Select a depth cell to find its match in the right view."
        desc = "Given a depth cell, find the match in the right view image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim leftInput As cv.Mat, rightInput As cv.Mat
        If rightView Is Nothing Then
            leftInput = task.leftView
            rightInput = task.rightView
        Else
            rightInput = rightView
            leftInput = src
        End If

        dst2 = leftInput
        For Each idd In task.iddList
            dst2.Rectangle(idd.lRect, 255, task.lineWidth)
        Next

        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        rect = task.dCell.grid.gridRectsAll(index)
        dst2.Rectangle(rect, 255, task.lineWidth + 1)

        Dim correlation As New cv.Mat
        Dim opt = cv.TemplateMatchModes.CCoeffNormed
        correlations.Clear()
        means.Clear()
        stdevs.Clear()

        Dim meanT As Single, stdevT As Single, mean As Single, stdev As Single
        rect = ValidateRect(rect)
        cv.Cv2.MeanStdDev(dst2(rect), meanT, stdevT)

        dst3 = rightInput.Clone
        Dim rr = rect
        Dim maxDisparity As Integer = 128
        For i = 1 To maxDisparity
            rr.X -= 1
            If rr.X <= 0 Then Exit For
            If dst3(rr).CountNonZero = 0 Then Continue For
            cv.Cv2.MeanStdDev(dst3(rect), mean, stdev)
            means.Add(Math.Abs(mean - meanT))
            stdevs.Add(Math.Abs(stdev - stdevT))
            cv.Cv2.MatchTemplate(dst2(rect), dst3(rr), correlation, opt)
            correlations.Add(correlation.Get(Of Single)(0, 0))
            cv.Cv2.MatchTemplate(task.pcSplit(2)(rect), task.pcSplit(2)(rr), correlation, opt)
        Next
        If correlations.Count = 0 Then
            labels(2) = "The selected cell has no data - skipping..."
            Exit Sub
        End If
        bestCorrelation = correlations.Max

        searchRect = New cv.Rect(rect.X - maxDisparity, rect.Y,
                                 rect.BottomRight.X - rect.X + maxDisparity, rect.Height)

        dst3.Rectangle(searchRect, 255, task.lineWidth)
        Dim indexCorr = correlations.IndexOf(bestCorrelation)
        matchRect = New cv.Rect(rect.X - indexCorr, rect.Y, rect.Width, rect.Height)
        dst3.Rectangle(matchRect, 255, task.lineWidth + 1)

        MeanDiff = means(indexCorr)
        StdevDiff = stdevs(indexCorr)

        If task.heartBeat Then
            labels(3) = "Max correlation = " + Format(bestCorrelation, fmt3) + "  " +
                        "Pixel disparity = " + CStr(index) + "  " +
                        "Mean difference at Max correlation = " + Format(MeanDiff, fmt3) + "  " +
                        "Stdev difference at Max correlation = " + Format(StdevDiff, fmt3)
        End If
    End Sub
End Class






Public Class Disparity_MatchMean : Inherits TaskParent
    Dim disparity As New Disparity_Manual
    Public Sub New()
        desc = "Find and display the best cell with the smallest mean difference to the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        disparity.Run(src)
        dst2 = disparity.dst2
        dst3 = disparity.dst3

        Dim rect = disparity.rect
        Dim r = rect

        Dim index = disparity.means.IndexOf(disparity.MeanDiff)
        r.X = rect.X - index

        dst3.Rectangle(r, 255, task.lineWidth)
        If task.heartBeat Then
            labels(3) = "Correlation at best Mean = " + Format(disparity.bestCorrelation, fmt3) + "  " +
                        "Mean difference = " + Format(disparity.MeanDiff, fmt3) + "  " +
                        "Stdev difference at best mean = " + Format(disparity.StdevDiff, fmt3)
        End If
    End Sub
End Class





Public Class Disparity_MatchStdev : Inherits TaskParent
    Dim disparity As New Disparity_Manual
    Public Sub New()
        desc = "Find and display the best cell with the smallest stdev difference to the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        disparity.Run(src)
        dst2 = disparity.dst2
        dst3 = disparity.dst3

        Dim rect = disparity.rect
        Dim r = rect

        Dim index = disparity.stdevs.IndexOf(disparity.StdevDiff)
        r.X = rect.X - index

        dst3.Rectangle(r, 255, task.lineWidth)
        If task.heartBeat Then
            labels(3) = "Correlation at best Stdev = " + Format(disparity.bestCorrelation, fmt3) + "  " +
                        "Mean difference at Stdev mean = " + Format(disparity.MeanDiff, fmt3) + "  " +
                        "Stdev difference = " + Format(disparity.StdevDiff, fmt3)
        End If
    End Sub
End Class







Public Class Disparity_Features : Inherits TaskParent
    Dim featNo As New Feature_NoMotion
    Public Sub New()
        optiBase.findRadio("GoodFeatures (ShiTomasi) grid").Checked = True
        desc = "Use features in depth cells to confirm depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        featNo.Run(task.leftView)
        dst2 = featNo.dst2.Clone
        labels(2) = featNo.labels(2)

        'For Each r In task.dCell.gridRects
        '    dst2.Rectangle(r, 255, task.lineWidth)
        'Next

        featNo.Run(task.rightView)
        dst3 = featNo.dst2
        labels(3) = featNo.labels(2)
    End Sub
End Class







Public Class Disparity_Edges : Inherits TaskParent
    Dim edges As New EdgeDraw_Basics
    Dim disparity As New Disparity_Basics
    Public Sub New()
        desc = "Use features in depth cells to confirm depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.leftView)
        dst2 = edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2.Clone

        disparity.rightView = dst3
        disparity.Run(dst2)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels
    End Sub
End Class







Public Class Disparity_LowRes : Inherits TaskParent
    Dim lowres As New LowRes_LeftRight
    Dim disparity As New Disparity_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use features in depth cells to confirm depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView
        lowres.Run(src)
        disparity.rightView = lowres.dst3
        disparity.Run(lowres.dst2)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels

        task.color.Rectangle(disparity.rect, 255, task.lineWidth)
        dst1.Rectangle(disparity.matchRect, 255, task.lineWidth)
    End Sub
End Class







Public Class Disparity_Validate : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "To validate Disparity_Basics, build the right view from the left view.  Should always match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim w = dst2.Width / 5
        Dim r1 = New cv.Rect(w, 0, dst2.Width - w, dst2.Height)
        Dim r2 = New cv.Rect(0, 0, r1.Width, dst2.Height)
        dst3.SetTo(0)
        task.leftView(r1).CopyTo(dst3(r2))
        disparity.rightView = dst3
        disparity.Run(task.leftView)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels
    End Sub
End Class







Public Class Disparity_RedMask : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Dim leftCells As New LeftRight_RedLeftGray
    Dim rightCells As New LeftRight_RedRightGray
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "To validate Disparity_Basics, just shift the left image right.  Should always match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView
        leftCells.Run(src)
        rightCells.Run(src)

        disparity.rightView = rightCells.dst2
        disparity.Run(leftCells.dst2)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels

        task.color.Rectangle(disparity.rect, 255, task.lineWidth)
        dst1.Rectangle(disparity.matchRect, 255, task.lineWidth)
    End Sub
End Class





'Z = B * f / disparity  - we are using here: disparity = B * f / Z
'where:

' Z Is the depth in meters
' B Is the baseline in meters
' f Is the focal length in pixels
' disparity Is the disparity in pixels
' The baseline Is the distance between the two cameras in a stereo setup.
' The focal length Is the distance between the camera's lens and the sensor. The disparity is the difference in the x-coordinates of the same point in the left and right images.

' For example, if the baseline Is 0.5 meters, the focal length Is 1000 pixels, And the disparity Is 100 pixels, then the depth Is

' Z = 0.5 * 1000 / 100 = 5 meters
' The Function() relating depth To disparity Is only valid For a calibrated stereo setup.
Public Class Disparity_Inverse : Inherits TaskParent
    Public Sub New()
        task.drawRect = New cv.Rect(dst2.Width / 2 - 10, dst2.Height / 2 - 10, 20, 20)
        desc = "Use the depth to find the disparity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView
        ' assuming StereoLabs Zed 2i camera for now.
        ' disparity = B * f / depth
        Dim camInfo = task.calibData
        If task.drawRect.Width > 0 Then
            Dim white As New cv.Vec3b(255, 255, 255)
            For y = 0 To task.drawRect.Height - 1
                For x = 0 To task.drawRect.Width - 1
                    Dim depth = task.pcSplit(2)(task.drawRect).Get(Of Single)(y, x)
                    If depth > 0 Then
                        Dim disp = 0.12 * camInfo.fx / depth
                        dst3(task.drawRect).Set(Of cv.Vec3b)(y, x - disp, white)
                    End If
                Next
            Next
        End If
    End Sub
End Class








Public Class Disparity_Color8u : Inherits TaskParent
    Dim color8u As New Color8U_LeftRight
    Dim disparity As New Disparity_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.ClickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Measure the impact of the color8u transforms on the depth cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView.Clone
        color8u.Run(src)

        dst2 = color8u.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        disparity.rightView = color8u.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        disparity.Run(dst2)
        dst3 = disparity.dst3
        labels = disparity.labels

        task.color.Rectangle(disparity.rect, 255, task.lineWidth)
        dst1.Rectangle(disparity.matchRect, 255, task.lineWidth)

        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        Dim rect = task.dCell.grid.gridRectsAll(index)
        dst2.Rectangle(rect, 255, task.lineWidth + 1)
    End Sub
End Class
