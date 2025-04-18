﻿Imports cv = OpenCvSharp
Public Class HighVis_Basics : Inherits TaskParent
    Dim info As New GridCell_Info
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display all the grid cells that have good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each gc In task.gcList
            If gc.highlyVisible Then
                If gc.correlation > 0 And gc.corrHistory.Count = task.historyCount Then
                    dst1(gc.rect).SetTo((gc.correlation + 1) * 127)
                Else
                    dst1(gc.rect).SetTo(0)
                End If
            End If
        Next

        dst0 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim mm = GetMinMax(dst1, dst0)
        dst2 = ShowPaletteDepth((dst1 - mm.minVal) * mm.maxVal / (mm.maxVal - mm.minVal))
        labels(2) = task.gCell.labels(2)

        info.Run(src)
        SetTrueText(info.strOut, 3)
    End Sub
End Class





Public Class HighVis_LineBasics : Inherits TaskParent
    Dim LRViews As New LeftRight_Lines
    Public leftLines As New List(Of lpData)
    Public Sub New()
        desc = "Find lines that are visible in both the left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRViews.Run(src)
        dst2 = LRViews.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        leftLines.Clear()
        For Each lp In LRViews.leftLines
            If lp.depth = 0 Then Continue For
            Dim gc = task.gcList(lp.gcIndex(0))
            If lp.highlyVisible Then
                leftLines.Add(lp)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(LRViews.leftLines.Count) + " lines are present and " + CStr(leftLines.Count) +
                        " were marked highly visible in the left and right images."
        End If
    End Sub
End Class





Public Class HighVis_Lines : Inherits TaskParent
    Dim highVis As New HighVis_LineBasics
    Dim intrinsics As New Intrinsics_Basics
    Public Sub New()
        desc = "Find lines that are highly visible in the left image and copy them to the right image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        highVis.Run(src)
        dst2 = highVis.dst2

        Dim leftLines = New List(Of lpData)(highVis.leftLines)
        Dim rightCount As Integer
        For Each lp In leftLines
            If lp.pcMeans(1).Item(2) = 0 Or lp.pcMeans(2).Item(2) = 0 Then Continue For ' end points must have depth so skip them if not.

            Dim gc = task.gcList(lp.gcIndex(0)) ' what is the range of depth values in the left grid cell.
            If gc.mm.maxVal = gc.mm.minVal > 0.1 Then Continue For ' too much variability in depth - not reliable for translation.

            rightCount += 1

            Dim ptRight() As cv.Point = {lp.p1, lp.p2}
            For i = 0 To 1
                If task.rgbLeftAligned Then
                    ptRight(i) = intrinsics.translatePixel(task.pointCloud.Get(Of cv.Point3f)(gc.rect.TopLeft.Y, gc.rect.TopLeft.X))
                End If
                ptRight(i).X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / lp.pcMeans(i + 1).Item(2)
            Next

            dst3.Line(ptRight(0), ptRight(1), task.highlight, task.lineWidth, task.lineType)
        Next


        'If task.heartBeat Then
        '    labels(2) = CStr(LRViews.leftLines.Count) + " lines present and " + CStr(leftCount) + " were highly visible in the left image."
        '    labels(3) = CStr(LRViews.rightLines.Count) + " lines present in the right image and " + CStr(rightCount) +
        '                " were highly visible in the right image."
        'End If
    End Sub
End Class











