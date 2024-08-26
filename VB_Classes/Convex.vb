Imports cvb = OpenCvSharp
Public Class Convex_Basics : Inherits VB_Parent
    Public hull() As cvb.Point
    Dim options As New Options_Convex
    Public Sub New()
        UpdateAdvice(traceName + ": use the local options to control the number of points.")
        desc = "Surround a set of random points with a convex hull"
        labels = {"", "", "Convex Hull - red dot is center and the black dots are the input points", ""}
    End Sub
    Public Function buildRandomHullPoints() As List(Of cvb.Point)
        Dim pad = 4
        Dim w = dst2.Width - dst2.Width / pad
        Dim h = dst2.Height - dst2.Height / pad

        Dim hullList As New List(Of cvb.Point)
        For i = 0 To options.hullCount - 1
            hullList.Add(New cvb.Point2f(msRNG.Next(dst2.Width / pad, w), msRNG.Next(dst2.Height / pad, h)))
        Next
        Return hullList
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim hullList = task.rc.contour
        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            hullList = buildRandomHullPoints()
        End If

        If hullList.Count = 0 Then
            SetTrueText("No points were provided.  Update hullList before running.")
            Exit Sub
        End If

        hull = cvb.Cv2.ConvexHull(hullList.ToArray, True)

        dst2.SetTo(0)

        Dim pMat As cvb.Mat = cvb.Mat.FromPixelData(hull.Count, 1, cvb.MatType.CV_32SC2, hull)
        Dim sum = pMat.Sum()
        DrawContour(dst2, hullList, cvb.Scalar.White, -1)

        For i = 0 To hull.Count - 1
            DrawLine(dst2, hull(i), hull((i + 1) Mod hull.Count), cvb.Scalar.White)
        Next
    End Sub
End Class






Public Class Convex_RedCloud : Inherits VB_Parent
    Dim convex As New Convex_Basics
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Selected contour - line shows hull with white is contour.  Click to select another contour.", "RedCloud cells"}
        desc = "Get lots of odd shapes from the RedCloud_Basics output and use ConvexHull to simplify them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        If task.rc.contour IsNot Nothing Then
            convex.Run(src)

            dst3.SetTo(0)
            dst3(task.rc.rect) = convex.dst2(New cvb.Rect(0, 0, task.rc.rect.Width, task.rc.rect.Height))
            DrawCircle(dst3,task.rc.maxDist, task.DotSize, cvb.Scalar.White)
        End If
    End Sub
End Class







' https://stackoverflow.com/questions/31354150/opencv-convexity-defects-drawing
Public Class Convex_Defects : Inherits VB_Parent
    Dim contours As New Contour_Largest
    Public Sub New()
        dst2 = cvb.Cv2.ImRead(task.HomeDir + "Data/star2.png").Threshold(200, 255, cvb.ThresholdTypes.Binary).Resize(task.WorkingRes)
        dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray)

        labels = {"", "", "Input to the ConvexHull and ConvexityDefects", "Yellow = ConvexHull, Red = ConvexityDefects, Yellow dots are convexityDefect 'Far' points"}
        desc = "Find the convexityDefects in the image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        contours.Run(dst2.Clone)
        Dim c = contours.bestContour.ToArray
        dst3 = dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Dim hull = cvb.Cv2.ConvexHull(c, False)
        Dim hullIndices = cvb.Cv2.ConvexHullIndices(c, False)
        DrawContour(dst3, hull.ToList, task.HighlightColor)

        Dim defects = cvb.Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList)
        For Each v In defects
            dst3.Line(c(v(0)), c(v(2)), cvb.Scalar.Red, task.lineWidth + 1, task.lineType)
            dst3.Line(c(v(1)), c(v(2)), cvb.Scalar.Red, task.lineWidth + 1, task.lineType)
            DrawCircle(dst3,c(v(2)), task.DotSize + 2, task.HighlightColor)
        Next
    End Sub
End Class






Public Class Convex_RedCloudDefects : Inherits VB_Parent
    Dim convex As New Convex_RedCloud
    Dim contours As New Contour_Largest
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Hull outline in green, lines show defects.", "Output of RedCloud_Basics"}
        desc = "Find the convexityDefects in the selected RedCloud cell"
    End Sub
    Public Function betterContour(c As List(Of cvb.Point), defects() As cvb.Vec4i) As List(Of cvb.Point)
        Dim lastV As Integer = -1
        Dim newC As New List(Of cvb.Point)
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
    Public Sub RunAlg(src As cvb.Mat)
        convex.Run(src)
        dst1 = convex.redC.dst2
        labels(1) = convex.redC.labels(2)
        dst3 = convex.dst3

        Dim rc = task.rc
        If rc.mask Is Nothing Then Exit Sub

        dst2 = rc.mask.Resize(dst2.Size(), 0, 0, cvb.InterpolationFlags.Nearest)
        contours.Run(dst2)
        Dim c = contours.bestContour

        Dim hull = cvb.Cv2.ConvexHull(c, False)
        Dim hullIndices = cvb.Cv2.ConvexHullIndices(c, False)
        dst2.SetTo(0)
        DrawContour(dst2, hull.ToList, rc.color, -1)

        Try
            Dim defects = cvb.Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList)
            rc.contour = betterContour(c, defects)
        Catch ex As Exception
            SetTrueText("Convexity defects failed due to self-intersection.", 3)
        End Try

        DrawContour(dst2, rc.contour, cvb.Scalar.Red)
    End Sub
End Class





