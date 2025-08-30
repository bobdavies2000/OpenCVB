﻿Imports cv = OpenCvSharp
Public Class Convex_Basics : Inherits TaskParent
    Public hull() As cv.Point
    Dim options As New Options_Convex
    Public Sub New()
        desc = "Surround a set of random points with a convex hull"
        labels = {"", "", "Convex Hull - red dot is center and the black dots are the input points", ""}
    End Sub
    Public Function buildRandomHullPoints() As List(Of cv.Point)
        Dim pad = 4
        Dim w = dst2.Width - dst2.Width / pad
        Dim h = dst2.Height - dst2.Height / pad

        Dim hullList As New List(Of cv.Point)
        For i = 0 To options.hullCount - 1
            hullList.Add(New cv.Point2f(msRNG.Next(dst2.Width / pad, w), msRNG.Next(dst2.Height / pad, h)))
        Next
        Return hullList
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim hullList = task.rcD.contour
        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            hullList = buildRandomHullPoints()
        End If

        If hullList.Count = 0 Then
            SetTrueText("No points were provided.  Update hullList before running.")
            Exit Sub
        End If

        hull = cv.Cv2.ConvexHull(hullList.ToArray, True)

        dst2.SetTo(0)

        Dim pMat As cv.Mat = cv.Mat.FromPixelData(hull.Count, 1, cv.MatType.CV_32SC2, hull)
        Dim sum = pMat.Sum()
        DrawContour(dst2, hullList, white, -1)

        For i = 0 To hull.Count - 1
            DrawLine(dst2, hull(i), hull((i + 1) Mod hull.Count), white)
        Next
    End Sub
End Class






Public Class Convex_RedColor : Inherits TaskParent
    Dim convex As New Convex_Basics
    Public Sub New()
        labels = {"", "", "Selected contour - line shows hull with white is contour.  Click to select another contour.", "RedCloud cells"}
        desc = "Get lots of odd shapes from the RedColor_Basics output and use ConvexHull to simplify them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        If task.rcD.contour IsNot Nothing Then
            convex.Run(src)

            dst3.SetTo(0)
            dst3(task.rcD.rect) = convex.dst2(New cv.Rect(0, 0, task.rcD.rect.Width, task.rcD.rect.Height))
            DrawCircle(dst3, task.rcD.maxDist, task.DotSize, white)
        End If
    End Sub
End Class







' https://stackoverflow.com/questions/31354150/opencv-convexity-defects-drawing
Public Class Convex_Defects : Inherits TaskParent
    Dim contours As New Contour_Largest
    Public Sub New()
        dst2 = cv.Cv2.ImRead(task.HomeDir + "Data/star2.png").Threshold(200, 255,
                            cv.ThresholdTypes.Binary).Resize(New cv.Size(task.workRes.Width, task.workRes.Height))
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray)

        labels = {"", "", "Input to the ConvexHull and ConvexityDefects", "Yellow = ConvexHull, Red = ConvexityDefects, Yellow dots are convexityDefect 'Far' points"}
        desc = "Find the convexityDefects in the image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        contours.Run(dst2.Clone)
        Dim c = contours.bestContour.ToArray
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim hull = cv.Cv2.ConvexHull(c, False)
        Dim hullIndices = cv.Cv2.ConvexHullIndices(c, False)
        DrawContour(dst3, hull.ToList, task.highlight)

        Dim defects = cv.Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList)
        For Each v In defects
            dst3.Line(c(v(0)), c(v(2)), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            dst3.Line(c(v(1)), c(v(2)), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            DrawCircle(dst3,c(v(2)), task.DotSize + 2, task.highlight)
        Next
    End Sub
End Class






Public Class Convex_RedColorDefects : Inherits TaskParent
    Dim contours As New Contour_Largest
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Hull outline in yellow, red is hull with defects removed.  Select any cell in the upper right..."
        labels(3) = "Original mask that produces the hull at left"
        desc = "Find the convexityDefects in the selected RedCloud cell"
    End Sub
    Public Shared Function betterContour(c As List(Of cv.Point), defects() As cv.Vec4i) As List(Of cv.Point)
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
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = runRedC(src, labels(1))

        Dim rc = task.rcD
        If rc.mask Is Nothing Then Exit Sub

        Dim sz = New cv.Size(dst2.Height * rc.mask.Width / rc.mask.Height, dst2.Height)
        If rc.mask.Width > rc.mask.Height Then
            sz = New cv.Size(dst2.Width, dst2.Height * rc.mask.Height / rc.mask.Width)
        End If
        dst0 = rc.mask.Resize(sz, 0, 0, cv.InterpolationFlags.Nearest)
        Dim r = New cv.Rect(0, 0, dst0.Width, dst0.Height)
        dst3.SetTo(0)
        dst3(r).SetTo(white, dst0)
        contours.Run(dst3)
        Dim c = contours.bestContour

        Dim hull = cv.Cv2.ConvexHull(c, False)
        Dim hullIndices = cv.Cv2.ConvexHullIndices(c, False)
        dst2.SetTo(0)
        DrawContour(dst2, hull.ToList, task.highlight, -1)

        Try
            Dim defects = cv.Cv2.ConvexityDefects(c, hullIndices.ToList)
            rc.contour = betterContour(c, defects)
        Catch ex As Exception
            SetTrueText("Convexity defects failed due to self-intersection.", 3)
        End Try

        DrawContour(dst2, rc.contour, cv.Scalar.Red)
    End Sub
End Class





