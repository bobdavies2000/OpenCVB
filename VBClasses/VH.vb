Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class VH_Basics : Inherits TaskParent
        Dim lines As New LineSeg_Core
        Dim lpList As New List(Of lpData)
        Public Sub New()
            desc = "Find all the vertical and horizontal lines in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim sortScores As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
            Dim lpLong = task.longestLine
            For i = -5 To 5
                Dim angle = task.verticalizeAngle + 0.02 * i
                dst3 = Cloud_GravityRGB.rotateRGB(task.gray(lpLong.rect), angle)
                lines.Run(dst3)
                For Each lp In lines.lpList
                    If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then sortScores.Add(lp.length, i)
                Next
            Next
            Dim optimalAngle = task.verticalizeAngle + 0.01 * sortScores.Values(0)

            If task.heartBeat Then dst2.SetTo(0)
            lpList.Clear()
            For Each lp In lines.lpList
                If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then
                    Line(dst2, lp.p1, lp.p2, white, task.lineWidth)
                    lpList.Add(lp)
                End If
            Next

            labels(2) = CStr(lpList.Count) + " vertical lines were found."
            labels(3) = "Optimal angle is " + optimalAngle.ToString("0.000") + " optimal index = " + CStr(sortScores.Values(0))
        End Sub
    End Class




    Public Class VH_Explore : Inherits TaskParent
        Dim lines As New LineSeg_Core
        Dim lpList As New List(Of lpData)
        Dim sortScores As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        Public Sub New()
            desc = "Find all the vertical and horizontal lines in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static index As Integer = -100
            Static startAngle As Double
            If task.heartBeat Then startAngle = task.verticalizeAngle
            Dim angle = startAngle + 0.01 * index
            dst3 = Cloud_GravityRGB.rotateRGB(task.gray, angle)
            lines.Run(dst3)

            dst2.SetTo(0)
            lpList.Clear()
            For Each lp In lines.lpList
                If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then
                    Line(dst2, lp.p1, lp.p2, white, task.lineWidth)
                    lpList.Add(lp)
                    sortScores.Add(lp.length, index)
                End If
            Next
            labels(2) = CStr(lpList.Count) + " vertical lines were found.  Angle = " + angle.ToString("0.000")
            Dim optimalAngle = startAngle + 0.01 * sortScores.Values(0)
            labels(3) = "Optimal angle is " + optimalAngle.ToString("0.000") + " starting angle = " +
                        startAngle.ToString("0.000") + " optimal index = " + CStr(sortScores.Values(0))
            index += 1
            If index >= 100 Then index = -100
            If sortScores.Count >= 200 Then sortScores.RemoveAt(0)
        End Sub
    End Class
End Namespace
