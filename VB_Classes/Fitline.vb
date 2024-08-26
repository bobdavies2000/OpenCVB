Imports cvb = OpenCvSharp
' https://docs.opencvb.org/3.4/js_contour_features_fitLine.html
Public Class FitLine_Basics : Inherits VB_Parent
    Dim options As New Options_FitLine
    Public draw As New Draw_Lines
    Public lines As New List(Of cvb.Point) ' there are always an even number - 2 points define the line.
    Public Sub New()
        FindSlider("DrawCount").Value = 2

        labels(3) = "FitLine_Basics input"
        desc = "Show how Fitline API works.  When the lines overlap the image has a single contour and the lines are occasionally not found."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If not task.heartBeat Then Exit Sub
        options.RunOpt()

        If standaloneTest() Then
            draw.Run(src)
            dst3 = draw.dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray).Threshold(1, 255, cvb.ThresholdTypes.Binary)
            dst2 = dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Else
            lines.Clear()
        End If

        Dim contours As cvb.Point()()
        contours = cvb.Cv2.FindContoursAsArray(dst3, cvb.RetrievalModes.Tree, cvb.ContourApproximationModes.ApproxSimple)
        For i = 0 To contours.Length - 1
            Dim cnt = contours(i)
            Dim line2d = cvb.Cv2.FitLine(cnt, cvb.DistanceTypes.L2, 0, options.radiusAccuracy, options.angleAccuracy)
            Dim slope = line2d.Vy / line2d.Vx
            Dim leftY = Math.Round(-line2d.X1 * slope + line2d.Y1)
            Dim rightY = Math.Round((src.Cols - line2d.X1) * slope + line2d.Y1)
            Dim p1 = New cvb.Point(0, leftY)
            Dim p2 = New cvb.Point(src.Cols - 1, rightY)
            If standaloneTest() Then
                lines.Add(p1)
                lines.Add(p2)
            End If
            DrawLine(dst2, p1, p2, cvb.Scalar.Red)
        Next
    End Sub
End Class



Public Class FitLine_Basics3D : Inherits VB_Parent
    Dim hlines As New Hough_Lines_MT
    Public Sub New()
        desc = "Use visual lines to find 3D lines.  This algorithm is NOT working."
        labels(3) = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub houghShowLines3D(ByRef dst As cvb.Mat, segment As cvb.Line3D)
        Dim x As Double = segment.X1 * dst.Cols
        Dim y As Double = segment.Y1 * dst.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then m = 0 Else m = segment.Vy / segment.Vx ' vertical slope a no-no.
        Dim b As Double = y - m * x
        Dim pt1 As cvb.Point = New cvb.Point(x, y)
        Dim pt2 As cvb.Point
        If m = 0 Then pt2 = New cvb.Point(x, dst.Rows) Else pt2 = New cvb.Point((dst.Rows - b) / m, dst.Rows)
        dst.Line(pt1, pt2, cvb.Scalar.Red, task.lineWidth + 2, task.lineType, 0)
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If Not task.heartBeat Then Exit Sub
        hlines.Run(src)
        dst3 = hlines.dst3
        Dim mask = dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cvb.ThresholdTypes.Binary)
        dst3 = mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        src.CopyTo(dst2)

        Dim lines As New List(Of cvb.Line3D)
        Dim nullLine = New cvb.Line3D(0, 0, 0, 0, 0, 0)
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim depth = task.pcSplit(2)(roi)
            Dim fMask = mask(roi)
            Dim points As New List(Of cvb.Point3f)
            Dim rows = src.Rows, cols = src.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.Get(Of Byte)(y, x) > 0 Then
                        Dim d = depth.Get(Of Single)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cvb.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            Dim line = nullLine
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = task.depthRGB(roi).Mean()
                mean(0) = 255 - mean(0)
                dst3.Rectangle(roi, mean)
            Else
                line = cvb.Cv2.FitLine(points.ToArray, cvb.DistanceTypes.L2, 0, 0, 0.01)
            End If
            SyncLock lines
                lines.Add(line)
            End SyncLock
        End Sub)
        ' putting this in the parallel for above causes a memory leak - could not find it...
        For i = 0 To task.gridList.Count - 1
            houghShowLines3D(dst2(task.gridList(i)), lines.ElementAt(i))
        Next
    End Sub
End Class