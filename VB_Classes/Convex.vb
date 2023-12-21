Imports cv = OpenCvSharp
Public Class Convex_Basics : Inherits VB_Algorithm
    Public hull() As cv.Point
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Hull random points", 4, 20, 10)
        desc = "Surround a set of random points with a convex hull"
        labels = {"", "", "Convex Hull - red dot is center and the black dots are the input points", ""}
    End Sub
    Public Function buildRandomHullPoints() As List(Of cv.Point)
        Static hullSlider = findSlider("Hull random points")
        Dim Count = hullSlider.Value
        Dim pad = 4
        Dim w = dst2.Width - dst2.Width / pad
        Dim h = dst2.Height - dst2.Height / pad

        Dim hullList As New List(Of cv.Point)
        For i = 0 To Count - 1
            hullList.Add(New cv.Point2f(msRNG.Next(dst2.Width / pad, w), msRNG.Next(dst2.Height / pad, h)))
        Next
        Return hullList
    End Function
    Public Sub RunVB(src As cv.Mat)
        Dim hullList = task.rcSelect.contour
        If standalone Then
            If heartBeat() = False Then Exit Sub
            hullList = buildRandomHullPoints()
        End If

        If hullList.Count = 0 Then
            setTrueText("No points were provided.  Update hullList before running.")
            Exit Sub
        End If

        hull = cv.Cv2.ConvexHull(hullList.ToArray, True)

        dst2.SetTo(0)

        Dim pMat As New cv.Mat(hull.Count, 1, cv.MatType.CV_32SC2, hull)
        Dim sum = pMat.Sum()
        vbDrawContour(dst2, hullList, cv.Scalar.White, -1)

        For i = 0 To hull.Count - 1
            dst2.Line(hull(i), hull((i + 1) Mod hull.Count), cv.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class






Public Class Convex_RedCloud : Inherits VB_Algorithm
    Dim convex As New Convex_Basics
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Selected contour - line shows hull with white is contour.  Click to select another contour.", "RedCloud cells"}
        desc = "Get lots of odd shapes from the RedCloud_Basics output and use ConvexHull to simplify them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        If task.rcSelect.contour IsNot Nothing Then
            convex.Run(src)

            dst3.SetTo(0)
            dst3(task.rcSelect.rect) = convex.dst2(New cv.Rect(0, 0, task.rcSelect.rect.Width, task.rcSelect.rect.Height))
            dst3.Circle(task.rcSelect.maxDist, task.dotSize, cv.Scalar.White, -1, task.lineType)
        End If
    End Sub
End Class







' https://stackoverflow.com/questions/31354150/opencv-convexity-defects-drawing
Public Class Convex_Defects : Inherits VB_Algorithm
    Dim contours As New Contour_Largest
    Public Sub New()
        dst2 = cv.Cv2.ImRead(task.homeDir + "Data/star2.png").Threshold(200, 255, cv.ThresholdTypes.Binary).Resize(task.workingRes)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        labels = {"", "", "Input to the ConvexHull and ConvexityDefects", "Yellow = ConvexHull, Red = ConvexityDefects, Yellow dots are convexityDefect 'Far' points"}
        desc = "Find the convexityDefects in the image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        contours.Run(dst2.Clone)
        Dim c = contours.bestContour.ToArray
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim hull = cv.Cv2.ConvexHull(c, False)
        Dim hullIndices = cv.Cv2.ConvexHullIndices(c, False)
        vbDrawContour(dst3, hull.ToList, task.highlightColor)

        Dim defects = cv.Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList)
        For Each v In defects
            dst3.Line(c(v(0)), c(v(2)), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            dst3.Line(c(v(1)), c(v(2)), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            dst3.Circle(c(v(2)), task.dotSize + 2, task.highlightColor, -1, task.lineType)
        Next
    End Sub
End Class







Public Class Convex_RedCloudDefects : Inherits VB_Algorithm
    Dim convex As New Convex_RedCloud
    Dim contours As New Contour_Largest
    Dim resize As New Resize_Preserve
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Hull outline in green, lines show defects.", "Output of RedCloud_Basics"}
        desc = "Find the convexityDefects in the selected RedCloud cell"
    End Sub
    Public Function betterContour(c As List(Of cv.Point), defects() As cv.Vec4i) As List(Of cv.Point)
        Dim lastV As Integer = -1
        Dim newC As New List(Of cv.Point)
        For Each v In defects
            If v(0) <> lastV And lastV >= 0 Then
                For i = lastV To v(0) - 1
                    newC.Add(c(i))
                Next
            End If
            newC.Add(c(v(0)))
            newC.Add(c(v(2)))
            newC.Add(c(v(1)))
            lastV = v(1)
        Next
        If defects.Count > 0 Then
            If lastV <> defects(0)(0) Then
                For lastV = lastV To c.Count - 1
                    newC.Add(c(lastV))
                Next
            End If
            newC.Add(c(defects(0)(0)))
        End If
        Return newC
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Resize Percentage (%)")
        Dim percent = (100 - percentSlider.value) / 100
        Dim percentOffset = (100 - percentSlider.value) / 100 / 2
        Dim rect = New cv.Rect(dst2.Width * percentOffset, dst2.Height * percentOffset, dst2.Width * percent, dst2.Height * percent)

        convex.Run(src)
        dst0 = convex.redC.dst0
        dst1 = convex.redC.dst2
        dst3 = convex.dst3
        labels(1) = convex.redC.labels(2)

        Dim rc = task.rcSelect
        If rc.mask Is Nothing Then Exit Sub

        resize.Run(rc.mask.Resize(dst2.Size))
        contours.Run(resize.dst2)
        Dim c = contours.bestContour

        Dim hull = cv.Cv2.ConvexHull(c, False)
        Dim hullIndices = cv.Cv2.ConvexHullIndices(c, False)
        dst2.SetTo(0)
        vbDrawContour(dst2, hull.ToList, rc.color, -1)

        Dim defects = cv.Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList)
        rc.contour = betterContour(c, defects)

        vbDrawContour(dst2, rc.contour, cv.Scalar.Red)
    End Sub
End Class