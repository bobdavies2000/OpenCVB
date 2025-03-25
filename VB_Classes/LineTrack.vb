Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.redOptions.TrackingColor.Checked = True
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)

        dst1.SetTo(0)
        For Each lp In task.lpList
            dst1.Line(lp.p1, lp.p2, 255, task.lineWidth + 1, cv.LineTypes.Link8)
        Next

        dst2 = runRedC(dst1, labels(2), Not dst1)

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst3, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst3, lp.center, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class






Public Class LineTrack_Map : Inherits TaskParent
    Dim lTrack As New LineTrack_Basics
    Public Sub New()
        task.gOptions.CrossHairs.Checked = False
        desc = "Show the gcmap (grid cells) and lpMap (lines) "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lTrack.Run(src)
        dst2 = lTrack.dst2
        labels(2) = lTrack.labels(2)

        Dim count As Integer
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            'Dim histarray(task.rcList.Count - 1) As Single
            'If task.rcList.Count > 0 Then
            '    Dim histogram As New cv.Mat
            '    cv.Cv2.CalcHist({task.lpMap}, {0}, task.motionMask, histogram, 1, {lpList.Count}, New cv.Rangef() {New cv.Rangef(1, lpList.Count)})

            '    Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
            'End If

            Dim mm = GetMinMax(task.rcMap(rect), task.depthMask(rect))
            If mm.minVal = 0 Then Dim k = 0
            If mm.maxVal > 0 Then
                ' if multiple lines intersect a grid rect, choose the largest redcloud cell containing them.
                ' mm.minval is the index of the largest.
                Dim index = If(mm.minVal = 0, mm.maxVal, mm.minVal)
                Dim rc = task.rcList(index)
                task.rcList(mm.minVal).gridCells.Add(i)
                dst3(rect).SetTo(rc.color)
                count += 1
            End If
        Next

        labels(3) = "The redCloud cells are completely covered by " + CStr(count) + " grid cells"
    End Sub
End Class