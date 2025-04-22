Imports cv = OpenCvSharp
Public Class FindMinRect_Basics : Inherits TaskParent
    Public minRect As cv.RotatedRect
    Dim options As New Options_MinArea
    Public inputPoints As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            options.Run()
            inputPoints = quickRandomPoints(options.numPoints)
        End If

        minRect = cv.Cv2.MinAreaRect(inputPoints.ToArray)

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.Red)
            Next
            DrawRotatedOutline(minRect, dst2, cv.Scalar.Yellow)
        End If
    End Sub
End Class






Public Class FindMinRect_Motion : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        optiBase.FindSlider("MOG Learn Rate X1000").Value = 100 ' low threshold to maximize motion
        desc = "Use minRectArea to encompass detected motion"
        labels(2) = "MinRectArea of MOG motion"
    End Sub
    Private Function motionRectangles(gray As cv.Mat, colors() As cv.Vec3b) As cv.Mat
        Return gray
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        bgSub.Run(src)
        dst1 = bgSub.dst2
        Dim contours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim contourCount As Integer
        For Each tour In contours
            Dim minRect = cv.Cv2.MinAreaRect(tour)
            If minRect.BoundingRect.Width > 1 And minRect.BoundingRect.Height > 1 Then
                contourCount += 1
                DrawRotatedRect(minRect, dst1, contourCount Mod 256)
            End If
        Next
        dst2 = ShowPalette(dst1)
        labels(2) = "There were " + CStr(contourCount) + " contours found"
        SetTrueText("Wave at the camera to see algorithm working...", 3)
    End Sub
End Class





Public Class FindMinRect_Contours : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use minRectArea to busy areas in an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim offset = 2
        dst1(New cv.Rect(0, 0, dst2.Width - offset, dst2.Height - offset)) = task.grayStable(New cv.Rect(offset, offset, dst2.Width - offset, dst2.Height - offset))
        dst1 = dst1 And task.grayStable
        Dim contours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim countTours As Integer
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            If minRect.BoundingRect.Width > 1 And minRect.BoundingRect.Height > 1 Then
                DrawRotatedRect(minRect, dst1, i Mod 256)
                countTours += 1
            End If
        Next
        dst2 = ShowPalette(dst1)
        labels(2) = "There were " + CStr(countTours) + " contours found in the image."
    End Sub
End Class
