Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Polylines_Basics : Inherits TaskParent
    Public edgeList As New List(Of List(Of cv.Point))
    Dim eSeg As New EdgeLines_Raw
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Retain edges where there was no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat
        Dim lastList As New List(Of List(Of cv.Point))(edgeList)
        Dim histarray(lastList.Count - 1) As Single

        edgeList.Clear()
        edgeList.Add(New List(Of cv.Point)) ' placeholder for zero
        If lastList.Count > 0 Then
            Dim ranges1 = New cv.Rangef() {New cv.Rangef(0, lastList.Count)}
            cv.Cv2.CalcHist({dst1}, {0}, task.motionMask, histogram, 1, {lastList.Count}, ranges1)
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) = 0 Then edgeList.Add(lastList(i))
            Next
        End If

        eSeg.Run(src)

        ReDim histarray(eSeg.segments.Count - 1)

        eSeg.dst3.ConvertTo(dst1, cv.MatType.CV_32F)

        Dim ranges2 = New cv.Rangef() {New cv.Rangef(0, eSeg.segments.Count)}
        cv.Cv2.CalcHist({dst1}, {0}, task.motionMask, histogram, 1, {eSeg.segments.Count}, ranges2)
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        For i = 1 To histarray.Count - 1
            If histarray(i) > 0 Then edgeList.Add(eSeg.segments(i - 1))
        Next

        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 1 To edgeList.Count - 1
            Dim nextList = New List(Of List(Of cv.Point))
            nextList.Add(edgeList(i))
            Dim n = edgeList(i).Count
            If n > 0 Then
                Dim distance As Double = Math.Sqrt((edgeList(i)(0).X - edgeList(i)(n - 1).X) * (edgeList(i)(0).X - edgeList(i)(n - 1).X) +
                                                   (edgeList(i)(0).Y - edgeList(i)(n - 1).Y) * (edgeList(i)(0).Y - edgeList(i)(n - 1).Y))
                Dim drawClosed = distance < 10
                cv.Cv2.Polylines(dst1, nextList, drawClosed, i, task.lineWidth, cv.LineTypes.Link4)
                cv.Cv2.Polylines(dst2, nextList, drawClosed, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        Next
        dst3 = ShowPalette(dst1 * 255 / eSeg.segments.Count)

        If task.heartBeat Then
            labels(2) = CStr(eSeg.segments.Count) + " lines found in EdgeLines C++ in the latest image and " +
                        CStr(edgeList.Count) + " resulted after filtering with the motion mask."
            labels(3) = "The " + CStr(eSeg.segments.Count) + " segments found in the current image are colored with the index of each segment"
        End If
    End Sub
End Class






Public Class Polylines_IEnumerableExample : Inherits TaskParent
    Dim options As New Options_PolyLines
    Public Sub New()
        desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Dim points = Enumerable.Range(0, options.polyCount).Select(Of cv.Point)(
            Function(i)
                Return New cv.Point(CInt(msRNG.Next(0, src.Width)), CInt(msRNG.Next(0, src.Height)))
            End Function).ToList
        Dim pts As New List(Of List(Of cv.Point))
        pts.Add(points)

        dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        ' NOTE: when there are 2 points, there will be 1 line.
        dst2.Polylines(pts, options.polyClosed, white, task.lineWidth, task.lineType)
    End Sub
End Class





' VB.Net implementation of the browse example in Opencvb.
' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
Public Class Polylines_Random : Inherits TaskParent
    Dim zoom As New Pixel_Zoom
    Public Sub New()
        labels(2) = "To zoom move the mouse over the image"
        desc = "Create a random procedural image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.frameCount Mod (task.fpsAlgorithm * 3) = 0 Then ' every x frames.
            Dim h = src.Height, w = src.Width
            Dim autorand As New Random
            Dim points2f(10000) As cv.Point2f
            Dim pts As New List(Of List(Of cv.Point))
            Dim points As New List(Of cv.Point)
            points2f(0) = New cv.Point2f(autorand.NextDouble() - 0.5, autorand.NextDouble() - 0.5)
            For i = 1 To points2f.Count - 1
                points2f(i) = New cv.Point2f(autorand.NextDouble() - 0.5 + points2f(i - 1).X, autorand.NextDouble() - 0.5 + points2f(i - 1).Y)
                points.Add(New cv.Point(CInt(points2f(i).X * 10 + w / 2), CInt(points2f(i).Y * 10 + h / 2)))
            Next
            pts.Add(points)

            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst2.Polylines(pts, False, white, task.lineWidth, task.lineType)
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        zoom.Run(dst2)
        dst3 = zoom.dst2
    End Sub
End Class


