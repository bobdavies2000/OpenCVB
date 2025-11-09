Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PolyLine_Basics : Inherits TaskParent
    Dim edgeLine As New EdgeLine_Basics
    Dim rcList As New List(Of rcData)
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        labels(1) = "CV_8U edges - input to PalleteBlackZero"
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Retain edges where there was no motion."
    End Sub
    Private Sub rcDataDraw(rc As rcData)
        Static nextList = New List(Of List(Of cv.Point))
        Dim n = rc.contour.Count - 1
        nextList.Clear()
        nextList.Add(rc.contour)
        cv.Cv2.Polylines(dst2(rc.rect), nextList, False, cv.Scalar.All(rc.index), task.lineWidth, task.lineType)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat
        Dim histarray(edgeLine.rcList.Count - 1) As Single
        If task.motionRect.Width = 0 Then Exit Sub ' no change!

        Dim newList As New List(Of rcData)
        dst2.SetTo(0)
        If edgeLine.rcList.Count Then
            Dim ranges1 = New cv.Rangef() {New cv.Rangef(0, edgeLine.rcList.Count)}
            cv.Cv2.CalcHist({dst2(task.motionRect)}, {0}, New cv.Mat, histogram,
                            1, {edgeLine.rcList.Count}, ranges1)
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) = 0 Then
                    Dim rc = edgeLine.rcList(i - 1)
                    rc.index = newList.Count + 1
                    newList.Add(rc)

                    rcDataDraw(rc)
                End If
            Next
        End If
        Dim removed = edgeLine.rcList.Count - newList.Count

        edgeLine.Run(src)
        ReDim histarray(edgeLine.classCount - 1)

        Dim ranges2 = New cv.Rangef() {New cv.Rangef(0, edgeLine.classCount)}
        cv.Cv2.CalcHist({edgeLine.dst2(task.motionRect)}, {0}, New cv.Mat, histogram,
                        1, {edgeLine.classCount}, ranges2)
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        Dim count As Integer
        For Each rc In edgeLine.rcList
            If histarray(rc.index - 1) > 0 And rc.contour.Count > 0 Then
                count += 1
                rc.index = newList.Count + 1
                newList.Add(rc)

                rcDataDraw(rc)
            End If
        Next

        dst2.ConvertTo(dst1, cv.MatType.CV_8U)
        dst3 = PaletteBlackZero(dst1)

        rcList = New List(Of rcData)(newList)

        labels(2) = CStr(edgeLine.classCount) + " lines found. " +
                    CStr(removed) + " removed and " + CStr(count) + " added " +
                    " to rcList after filtering for motion."
    End Sub
End Class






Public Class PolyLine_IEnumerableExample : Inherits TaskParent
    Dim options As New Options_PolyLines
    Public Sub New()
        desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

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
Public Class PolyLine_Random : Inherits TaskParent
    Dim zoom As New Pixel_Zoom
    Public Sub New()
        labels(2) = "To zoom move the mouse over the image"
        desc = "Create a random procedural image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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