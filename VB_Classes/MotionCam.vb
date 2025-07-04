﻿Imports cv = OpenCvSharp
Public Class MotionCam_Basics : Inherits TaskParent
    Public edgeList As New List(Of SortedList(Of Single, Integer))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find all the line edge points and display them."
    End Sub
    Public Sub buildDisplay(edgePoints As List(Of SortedList(Of Single, Integer)), lpList As List(Of lpData),
                            offset1 As Integer, color As cv.Scalar)
        Dim pt As cv.Point2f
        Dim index As Integer
        For Each sortlist In edgePoints
            Dim ptIndex As Integer = 0
            For Each ele In sortlist
                Dim lp = lpList(ele.Value)

                Select Case index
                    Case 0 ' top
                        pt = New cv.Point2f(ele.Key, offset1)
                    Case 1 ' left
                        pt = New cv.Point2f(offset1, ele.Key)
                    Case 2 ' right
                        pt = New cv.Point2f(dst2.Width - 10 - offset1, ele.Key)
                    Case 3 ' bottom
                        pt = New cv.Point2f(ele.Key, dst2.Height - 10 - offset1)
                End Select

                'SetTrueText(CStr(ptIndex), pt)
                dst2.Circle(pt, task.DotSize, color, -1, task.lineType)
                ptIndex += 1
            Next
            index += 1
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.lineRGB.dst2
        labels(1) = "The top " + CStr(task.lineRGB.lpList.Count) + " longest lines in the image."

        Dim top As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim left As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim right As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim bottom As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)

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

        edgeList.Clear()
        For i = 0 To 3
            Dim sortList = Choose(i + 1, top, left, right, bottom)
            edgeList.Add(sortList)
        Next

        dst2 = src.Clone
        buildDisplay(edgeList, lpList, 0, task.highlight)

        labels(2) = CStr(task.lineRGB.lpList.Count * 2) + " edge points of the top " + CStr(task.lineRGB.lpList.Count) +
                    " longest lines in the image are shown."
    End Sub
End Class






Public Class MotionCam_MatchLast : Inherits TaskParent
    Dim motion As New MotionCam_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the common trends in the image edge points of the top, left, right, and bottom."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)
        dst1 = motion.dst1
        labels(1) = motion.labels(1)

        Static edgeList As New List(Of SortedList(Of Single, Integer))(motion.edgeList)
        Static lpLastList As New List(Of lpData)(task.lineRGB.lpList)

        For i = 0 To edgeList.Count - 1
            If edgeList(i).Count = motion.edgeList(i).Count Then
                For j = 0 To edgeList(i).Count - 1
                    If edgeList(i).ElementAt(j).Key <> motion.edgeList(i).ElementAt(j).Key Then Dim k = 0
                Next
            Else
                Dim k = 0
            End If
        Next

        motion.buildDisplay(edgeList, lpLastList, 20, white)
        dst2 = motion.dst2
        trueData = motion.trueData

        edgeList = New List(Of SortedList(Of Single, Integer))(motion.edgeList)
        lpLastList = New List(Of lpData)(task.lineRGB.lpList)

        labels(2) = motion.labels(2) + "  White points are for the previous frame"
    End Sub
End Class
