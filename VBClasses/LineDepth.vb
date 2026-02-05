Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineDepth_Basics : Inherits TaskParent
        Public prepEdges As New RedPrep_EdgeMask
        Dim lines As New Line_Basics
        Dim motionLeft As New Motion_Basics
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




    Public Class LineDepth_XY : Inherits TaskParent
        Dim lineD As New LineDepth_Basics
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




    Public Class LineDepth_Vertical : Inherits TaskParent
        Dim lineD As New LineDepth_Basics
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




    Public Class LineDepth_Horizontal : Inherits TaskParent
        Dim lineD As New LineDepth_Basics
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







    Public Class LineDepth_Common : Inherits TaskParent
        Dim lineX As New LineDepth_Vertical
        Dim lineY As New LineDepth_Horizontal
        Dim lineXY As New LineDepth_XY
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
            dst2 = lineXY.dst2.Clone
            For Each lp In lineX.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            Next
            For Each lp In lineY.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class







    Public Class LineDepth_HorizontalVertical : Inherits TaskParent
        Dim lineX As New LineDepth_Vertical
        Dim lineY As New LineDepth_Horizontal
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
            dst2.SetTo(0)
            For Each lp In lineX.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            Next
            For Each lp In lineY.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class
End Namespace
