Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.redOptions.TrackingColor.Checked = True
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
        desc = "Show the gcmap (grid cells) and fpMap (features points) "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lTrack.Run(src)
        dst2 = lTrack.dst2
        dst1 = lTrack.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = lTrack.labels(2)

        Dim count As Integer
        dst3.SetTo(0)
        Dim histarray(task.rcList.Count - 1) As Single
        Dim histogram As New cv.Mat
        For Each gc In task.gcList
            cv.Cv2.CalcHist({task.rcMap(gc.rect)}, {0}, emptyMat, histogram, 1, {task.rcList.Count},
                             New cv.Rangef() {New cv.Rangef(1, task.rcList.Count)})

            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
            ' if multiple lines intersect a grid rect, choose the largest redcloud cell containing them.
            ' The largest will be the index of the first non-zero histogram entry.
            For j = 1 To histarray.Count - 1
                If histarray(j) > 0 Then
                    Dim rc = task.rcList(j)
                    dst3(gc.rect).SetTo(rc.color)
                    ' dst3(gc.rect).SetTo(0, Not dst1(gc.rect))
                    count += 1
                    Exit For
                End If
            Next
        Next

        labels(3) = "The redCloud cells are completely covered by " + CStr(count) + " grid cells"
    End Sub
End Class






Public Class LineTrack_Depth : Inherits TaskParent
    Public Sub New()
        desc = "Track lines and separate them by depth value."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)
        Dim lp As New lpData
        For Each lp In task.lpList
            Dim gc = task.gcList(task.gcMap.Get(Of Single)(lp.center.Y, lp.center.X))
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 2, task.lineType)
        Next
    End Sub
End Class
