Imports cv = OpenCvSharp
Public Class MotionLine_Basics : Inherits TaskParent
    Dim diff As New Diff_RGBAccum
    Dim lineHistory As New List(Of List(Of lpData))
    Public Sub New()
        labels(3) = "Wave at the camera to see results - "
        desc = "Track lines that are the result of motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        If task.heartBeat Then dst3 = src
        lineHistory.Add(task.lineRGB.lpList)
        For Each lplist In lineHistory
            For Each lp In lplist
                DrawLine(dst3, lp.p1, lp.p2)
            Next
        Next
        If lineHistory.Count > task.gOptions.FrameHistory.Value Then lineHistory.RemoveAt(0)

        labels(2) = CStr(task.lineRGB.lpList.Count) + " lines were found in the diff output"
    End Sub
End Class