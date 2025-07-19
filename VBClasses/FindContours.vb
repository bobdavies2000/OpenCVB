Imports cv = OpenCvSharp
Public Class FindContours_Basics : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim findRect As New FindMinRect_Basics
    Dim options As New Options_MinArea
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use minRectArea to busy areas in an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        edges.Run(task.grayStable)

        Dim contours = cv.Cv2.FindContoursAsArray(edges.dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim sortedTours As New SortedList(Of Integer, Tuple(Of cv.RotatedRect, Integer))(New compareAllowIdenticalInteger)
        For i = 0 To contours.Count - 1
            findRect.inputContour = contours(i)
            findRect.Run(emptyMat)
            Dim rr = findRect.minRect
            If rr.BoundingRect.Width > options.minSize And rr.BoundingRect.Height > options.minSize Then
                Dim tuple = New Tuple(Of cv.RotatedRect, Integer)(rr, i)
                sortedTours.Add(rr.Size.Width * rr.Size.Height, tuple)
            End If
        Next

        Dim lastDst3 = dst3.Clone
        dst2.SetTo(0)
        dst1.SetTo(0)
        For i = 0 To sortedTours.Values.Count - 1
            Dim tuple = sortedTours.Values(i)
            DrawRotatedRect(tuple.Item1, dst2, 255)
            DrawContour(dst2, contours(tuple.Item2).ToList, 0, task.lineWidth, cv.LineTypes.Link4)
            DrawContour(dst1, contours(tuple.Item2).ToList, (i Mod 254) + 1, task.lineWidth, cv.LineTypes.Link4)
        Next

        dst3 = ShowPalette(dst1)
        labels(2) = "There were " + CStr(sortedTours.Count) + " contours found with width and height greater than " + CStr(options.minSize)
    End Sub
End Class
