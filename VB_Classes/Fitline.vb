Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/js_contour_features_fitLine.html
Public Class Fitline_Basics : Inherits VB_Algorithm
    Dim options As New Options_FitLine
    Public draw As New Draw_Lines
    Public lines As New List(Of cv.Point) ' there are always an even number - 2 points define the line.
    Public Sub New()
        findSlider("DrawCount").Value = 2

        labels(3) = "FitLine_Basics input"
        desc = "Show how Fitline API works.  When the lines overlap the image has a single contour and the lines are occasionally not found."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.heartBeat = False Then Exit Sub
        options.RunVB()

        If standalone Then
            draw.Run(src)
            dst3 = draw.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst2 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            lines.Clear()
        End If

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(dst3, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To contours.Length - 1
            Dim cnt = contours(i)
            Dim line2d = cv.Cv2.FitLine(cnt, cv.DistanceTypes.L2, 0, options.radiusAccuracy, options.angleAccuracy)
            Dim slope = line2d.Vy / line2d.Vx
            Dim leftY = Math.Round(-line2d.X1 * slope + line2d.Y1)
            Dim rightY = Math.Round((src.Cols - line2d.X1) * slope + line2d.Y1)
            Dim p1 = New cv.Point(0, leftY)
            Dim p2 = New cv.Point(src.Cols - 1, rightY)
            If standalone Then
                lines.Add(p1)
                lines.Add(p2)
            End If
            dst2.Line(p1, p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Class



Public Class Fitline_3DBasics_MT : Inherits VB_Algorithm
    Dim hlines As New Hough_Lines_MT
    Public Sub New()
        desc = "Use visual lines to find 3D lines.  This algorithm is NOT working."
        labels(3) = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.heartBeat = False Then Exit Sub
        hlines.Run(src)
        dst3 = hlines.dst3
        Dim mask = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst3 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        src.CopyTo(dst2)

        Dim lines As New List(Of cv.Line3D)
        Dim nullLine = New cv.Line3D(0, 0, 0, 0, 0, 0)
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim depth = task.pcSplit(2)(roi)
            Dim fMask = mask(roi)
            Dim points As New List(Of cv.Point3f)
            Dim rows = src.Rows, cols = src.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.Get(Of Byte)(y, x) > 0 Then
                        Dim d = depth.Get(Of Single)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cv.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            Dim line = nullLine
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = task.depthRGB(roi).Mean()
                mean(0) = 255 - mean(0)
                dst3.Rectangle(roi, mean, -1, task.lineType)
            Else
                line = cv.Cv2.FitLine(points.ToArray, cv.DistanceTypes.L2, 0, 0, 0.01)
            End If
            SyncLock lines
                lines.Add(line)
            End SyncLock
        End Sub)
        ' putting this in the parallel for above causes a memory leak - could not find it...
        For i = 0 To task.gridList.Count - 1
            houghShowLines3D(dst2(task.gridList(i)), lines.ElementAt(i))
        Next
        dst2.SetTo(cv.scalar.white, task.gridMask)
    End Sub
End Class