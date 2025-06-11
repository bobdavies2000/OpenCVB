Imports cv = OpenCvSharp
Public Class MotionCam_Basics : Inherits TaskParent
    Public top As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public left As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public right As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public bottom As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find all the line edge points and display them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.lineRGB.dst2
        top.Clear()
        left.Clear()
        right.Clear()
        bottom.Clear()

        Dim lpList = task.lineRGB.lpList
        For Each lp In lpList
            If lp.ep1.X = 0 Then left.Add(lp.ep1.Y, lp.index)
            If lp.ep1.Y = 0 Then top.Add(lp.ep1.X, lp.index)
            If lp.ep2.X = 0 Then left.Add(lp.ep2.Y, lp.index)
            If lp.ep2.Y = 0 Then top.Add(lp.ep2.X, lp.index)

            If lp.ep1.X = dst2.Width Then right.Add(lp.ep1.X, lp.index)
            If lp.ep1.Y = dst2.Height Then bottom.Add(lp.ep1.X, lp.index)
            If lp.ep2.X = dst2.Width Then right.Add(lp.ep2.Y, lp.index)
            If lp.ep2.Y = dst2.Height Then bottom.Add(lp.ep2.X, lp.index)
        Next

        dst2 = src.Clone
        For Each ele In top
            dst2.Circle(New cv.Point(ele.Key, 0), task.DotSize, task.highlight, -1, task.lineType)
        Next
        For Each ele In left
            dst2.Circle(New cv.Point(0, ele.Key), task.DotSize, task.highlight, -1, task.lineType)
        Next
        For Each ele In right
            dst2.Circle(New cv.Point(dst2.Width - 1, ele.Key), task.DotSize, task.highlight, -1, task.lineType)
        Next
        For Each ele In bottom
            dst2.Circle(New cv.Point(ele.Key, dst2.Height - 1), task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class






Public Class MotionCam_Trend : Inherits TaskParent
    Dim motion As New MotionCam_Basics
    Public Sub New()
        desc = "Find the common trends in the image edge points of the top, left, right, and bottom."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)
        Dim histogram(motion.top.Count) As Single
        For Each pt In motion.top

        Next
    End Sub
End Class
