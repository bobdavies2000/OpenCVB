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

            If src.Type <> cv.MatType.CV_32FC3 Then
                prepEdges.Run(src)
            Else
                prepEdges.dst3 = src
            End If

            dst0 = task.leftView
            motionLeft.Run(dst0)

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
            dst0 = lineD.dst0
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
            dst0 = lineD.dst0
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
            dst0 = lineD.dst0
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

            dst0 = lineXY.dst0
            dst2 = lineXY.dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
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

            dst0 = lineX.dst0
            dst2 = lineX.dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
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
            OptionParent.FindSlider("Reduction Target").Value = 200
            If standalone Then task.gOptions.displayDst0.Checked = True
            labels(3) = "Input to Line_Basics"
            desc = "Find horizontal and vertical lines in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then lpList.Clear()
            lineX.prep.reductionName = "X Reduction"
            lineX.Run(src)
            lineY.prep.reductionName = "Y Reduction"
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
        Public lpList As New List(Of lpData)
        Public Sub New()
            lineX.reductionName = "X Reduction"
            lineY.reductionName = "Y Reduction"
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Identify the points where horizontal and vertical depth lines intersect."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then lpList.Clear()

            lineX.Run(src)
            lineY.Run(src)

            dst0.SetTo(0)
            dst1.SetTo(0)

            dst0.SetTo(64, lineX.dst3)
            dst1.SetTo(64, lineY.dst3)

            dst3 = dst0 + dst1
            dst2 = dst3.Threshold(128, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class

End Namespace
