Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineDepth_Basics : Inherits TaskParent
        Dim prepEdges As New RedPrep_EdgeMask
        Dim lines As New Line_Basics
        Dim motionLeft As New Motion_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            If standalone Then task.gOptions.displayDst0.Checked = True
            lines.removeOverlappingLines = False
            labels(3) = "Input to Line_Basics"
            desc = "Find lines in reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then lines.lpList.Clear()

            prepEdges.Run(src)

            dst0 = task.leftView
            motionLeft.Run(dst0)

            lines.motionMask = motionLeft.dst3
            lines.Run(prepEdges.dst3)
            dst2 = lines.dst2

            dst3 = prepEdges.dst3
            labels(2) = lines.labels(2)
        End Sub
    End Class
End Namespace
