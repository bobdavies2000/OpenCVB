Imports cv = OpenCvSharp
Public Class MotionCam_Basics : Inherits TaskParent
    Public top As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
    Public left As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
    Public right As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
    Public bottom As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
    Dim edgeList As New List(Of SortedList(Of Single, Integer))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find all the line edge points and display them."
    End Sub
    Public Sub buildDisplay(lpList As List(Of lpData), offset1 As Integer, offset2 As Integer, color As cv.Scalar)
        Dim pt As cv.Point2f
        For i = 0 To 3
            Dim sortList = Choose(i + 1, top, left, right, bottom)
            Dim index As Integer = 0
            For Each ele In sortList
                Dim lp = lpList(ele.Value)

                Select Case i
                    Case 0 ' top
                        pt = New cv.Point2f(ele.Key, offset2)
                    Case 1 ' left
                        pt = New cv.Point2f(offset2, ele.Key)
                    Case 2 ' right
                        pt = New cv.Point2f(dst2.Width - 10 - offset1, ele.Key)
                    Case 3 ' bottom
                        pt = New cv.Point2f(ele.Key, dst2.Height - 10 - offset1)
                End Select

                SetTrueText(CStr(index), pt)
                dst2.Circle(pt, task.DotSize, color, -1, task.lineType)
                index += 1
            Next
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.lineRGB.dst2
        labels(1) = "The top " + CStr(task.lineRGB.lpList.Count) + " longest lines in the image."
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
        buildDisplay(lpList, 0, 0, task.highlight)

        Dim count = top.Count + left.Count + right.Count + bottom.Count
        labels(2) = CStr(count) + " edge points of the top longest lines in the image are shown."
    End Sub
End Class






Public Class MotionCam_MatchLast : Inherits TaskParent
    Dim motion As New MotionCam_Basics
    Public Sub New()
        desc = "Find the common trends in the image edge points of the top, left, right, and bottom."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)

        Static lastTop As New SortedList(Of Single, Integer)(motion.top)
        Static lastLeft As New SortedList(Of Single, Integer)(motion.left)
        Static lastRight As New SortedList(Of Single, Integer)(motion.right)
        Static lastBottom As New SortedList(Of Single, Integer)(motion.bottom)

        ' buildDisplay(0, task.highlight)

        Dim histogram(motion.top.Count) As Single
        For Each pt In motion.top

        Next
    End Sub
End Class
