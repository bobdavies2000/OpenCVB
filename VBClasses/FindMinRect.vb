Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FindMinRect_Basics : Inherits TaskParent
        Public minRect As cv.RotatedRect
        Dim options As New Options_MinArea
        Public inputPoints As New List(Of cv.Point2f)
        Public inputContour() As cv.Point
        Public Sub New()
            desc = "Find minimum containing rectangle for a set of points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                If Not task.heartBeat Then Exit Sub
                options.Run()
                inputPoints = Rectangle_EnclosingPoints.quickRandomPoints(options.numPoints)
            End If

            If inputPoints.Count = 0 Then
                minRect = cv.Cv2.MinAreaRect(inputContour)
            Else
                minRect = cv.Cv2.MinAreaRect(inputPoints.ToArray)
            End If
            If standaloneTest() Then
                dst2.SetTo(0)
                For Each pt In inputPoints
                    DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.Red)
                Next
                Draw_Arc.DrawRotatedOutline(minRect, dst2, cv.Scalar.Yellow)
            End If
        End Sub
    End Class






    Public Class NR_FindMinRect_Motion : Inherits TaskParent
        Dim bgSub As New BGSubtract_Basics
        Public Sub New()
            OptionParent.FindSlider("MOG Learn Rate X1000").Value = 100 ' low threshold to maximize motion
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
                    Rectangle_Basics.DrawRotatedRect(minRect, dst1, contourCount Mod 256)
                End If
            Next
            dst2 = PaletteFull(dst1)
            labels(2) = "There were " + CStr(contourCount) + " contours found"
            SetTrueText("Wave at the camera to see algorithm working...", 3)
        End Sub
    End Class
End Namespace