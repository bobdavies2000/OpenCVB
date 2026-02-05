Imports OpenCvSharp.ML.DTrees
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class DepthLine_Basics : Inherits TaskParent
        Public prepEdges As New RedPrep_EdgeMask
        Dim lines As New Line_Basics
        Public motionLeft As New Motion_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            If standalone Then task.gOptions.displayDst0.Checked = True
            labels(0) = "LeftView after brightness/contrast transform."
            labels(3) = "Input to Line_Basics"
            desc = "Find lines in reduced the depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then lines.lpList.Clear()

            dst0 = task.leftView

            If src.Type <> cv.MatType.CV_32FC3 Then
                prepEdges.Run(src)
            Else
                prepEdges.dst3 = src
            End If

            motionLeft.Run(task.leftView)

            lines.motionMask = motionLeft.dst3
            lines.Run(prepEdges.dst3)
            dst2 = lines.dst2
            lpList = New List(Of lpData)(lines.lpList)

            dst3 = prepEdges.dst3
            labels(2) = lines.labels(2)
        End Sub
    End Class




    Public Class DepthLine_XY : Inherits TaskParent
        Dim lineD As New DepthLine_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Find vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineD.prepEdges.reductionName = "XY Reduction"
            lineD.Run(src)

            lpList = New List(Of lpData)(lineD.lpList)
            dst0 = task.leftView
            dst2 = lineD.dst2
            dst3 = lineD.dst3
            labels = lineD.labels
        End Sub
    End Class




    Public Class DepthLine_V : Inherits TaskParent
        Dim lineD As New DepthLine_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Find vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineD.prepEdges.reductionName = "X Reduction"
            lineD.Run(src)

            lpList = New List(Of lpData)(lineD.lpList)
            dst0 = task.leftView
            dst2 = lineD.dst2
            dst3 = lineD.dst3
            labels = lineD.labels
        End Sub
    End Class




    Public Class DepthLine_H : Inherits TaskParent
        Dim lineD As New DepthLine_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Find horizontal lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineD.prepEdges.reductionName = "Y Reduction"
            lineD.Run(src)

            lpList = New List(Of lpData)(lineD.lpList)
            dst0 = task.leftView
            dst2 = lineD.dst2
            dst3 = lineD.dst3
            labels = lineD.labels
        End Sub
    End Class







    Public Class DepthLine_Common : Inherits TaskParent
        Dim lineX As New DepthLine_V
        Dim lineY As New DepthLine_H
        Dim lineXY As New DepthLine_XY
        Public lpList As New List(Of lpData)
        Public Sub New()
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Find vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineX.Run(src)
            lineY.Run(src)
            lineXY.Run(src)

            dst0 = task.leftView
            dst2.SetTo(0)
            For Each lp In lineX.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            Next
            For Each lp In lineY.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next
            labels(0) = lineX.labels(0)

            labels(2) = CStr(lineY.lpList.Count) + " horizontal (yellow), " +
                        CStr(lineX.lpList.Count) + " vertical (white) lines, " +
                        CStr(lineXY.lpList.Count) + " XY lines (various colors)"
        End Sub
    End Class







    Public Class DepthLine_HV2 : Inherits TaskParent
        Dim lineX As New DepthLine_V
        Dim lineY As New DepthLine_H
        Public lpList As New List(Of lpData)
        Public Sub New()
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Find horizontal and vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineX.Run(src)
            lineY.Run(src)

            dst0 = task.leftView

            dst2.SetTo(0)
            For Each lp In lineX.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            Next

            For Each lp In lineY.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next

            dst3 = lineX.dst3
            dst3 = dst3 Or lineY.dst3

            labels(0) = lineX.labels(0)
            labels(2) = CStr(lineY.lpList.Count) + " horizontal (yellow), " +
                        CStr(lineX.lpList.Count) + " vertical (white) lines"
        End Sub
    End Class







    Public Class DepthLine_HV : Inherits TaskParent
        Dim lineX As New RedPrep_EdgeMask
        Dim lineY As New RedPrep_EdgeMask
        Public lpList As New List(Of lpData)
        Dim lines As New Line_Basics
        Public motionLeft As New Motion_Basics
        Public Sub New()
            lineX.reductionName = "X Reduction"
            lineY.reductionName = "Y Reduction"
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            labels(3) = "Input to Line_Basics"
            desc = "Find horizontal and vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then lpList.Clear()
            lineX.Run(src)
            lineY.Run(src)

            dst3 = lineX.dst3
            dst3 = dst3 Or lineY.dst3

            dst0 = task.leftView
            motionLeft.Run(dst0)

            lines.motionMask = motionLeft.dst3
            lines.Run(dst3)

            dst2 = lines.dst2
            lpList = New List(Of lpData)(lines.lpList)

            labels(2) = lines.labels(2)
        End Sub
    End Class





    Public Class DepthLine_Points : Inherits TaskParent
        Dim lineX As New RedPrep_EdgeMask
        Dim lineY As New RedPrep_EdgeMask
        Public ptList As New List(Of cv.Point)
        Public ptDepth As New List(Of Single)
        Public ptGrid As New List(Of Integer)
        Public motionLeft As New Motion_Basics
        Public Sub New()
            lineX.reductionName = "X Reduction"
            lineY.reductionName = "Y Reduction"
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Identify the points where horizontal and vertical depth lines intersect."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then ptList.Clear()

            lineX.Run(src)
            lineY.Run(src)
            motionLeft.Run(task.leftView)

            dst0.SetTo(0)
            dst1.SetTo(0)

            dst0.SetTo(64, lineX.dst3)
            dst1.SetTo(64, lineY.dst3)

            dst3 = dst0 + dst1
            dst1 = dst3.Threshold(127, 255, cv.ThresholdTypes.Binary)

            Dim ptMat = dst1.FindNonZero()
            labels(3) = CStr(ptMat.Rows) + " Points found"

            Dim retainedList As New List(Of cv.Point)
            Dim retainedDepth As New List(Of Single)
            Dim retainedGrid As New List(Of Integer)
            For i = 0 To ptList.Count - 1
                Dim pt = ptList(i)
                If motionLeft.dst3.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    retainedList.Add(pt)
                    retainedDepth.Add(ptDepth(i))
                    retainedGrid.Add(ptGrid(i))
                End If
            Next
            Dim count = retainedList.Count

            ptList = New List(Of cv.Point)(retainedList)
            ptDepth = New List(Of Single)(retainedDepth)
            ptGrid = New List(Of Integer)(retainedGrid)
            For i = 0 To ptMat.Rows - 1
                Dim pt = ptMat.Get(Of cv.Point)(i, 0)

                ' the grid rect may have already been found.
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                If ptGrid.Contains(index) = False Then
                    If motionLeft.dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                        ptList.Add(pt)
                        Dim gr = task.gridRects(index)
                        dst1(gr).SetTo(0)
                        ptDepth.Add(task.pcSplit(2)(gr).Mean(task.depthmask(gr))(0))
                        ptGrid.Add(index)
                    End If
                End If
            Next

            dst2 = task.leftView.Clone
            For Each pt In ptList
                dst2.Circle(pt, task.DotSize, white, -1, task.lineType)
            Next
            Dim newCount = ptList.Count - count
            labels(2) = CStr(count) + " points were retained " + CStr(newCount) + " points were added."
        End Sub
    End Class
End Namespace
