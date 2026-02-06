Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineEnds_Basics : Inherits TaskParent
        Dim match As New LineEnds_Correlation
        Public correlations As New List(Of Single)
        Public Sub New()
            taskA.featureOptions.MatchCorrSlider.Value = 90
            desc = "Track each of the lines found in Line_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone
            dst3 = taskA.rightView
            correlations.Clear()
            For Each lp In taskA.lines.lpList
                match.lpInput = lp
                match.Run(src)
                correlations.Add(match.p1Correlation)
                correlations.Add(match.p2Correlation)
                If match.p1Correlation > taskA.fCorrThreshold And match.p2Correlation > taskA.fCorrThreshold Then
                    DrawLine(dst2, lp.p1, lp.p2)
                End If
                dst2.Rectangle(lp.rect, taskA.highlight, taskA.lineWidth)
                DrawLine(dst2, lp.p1, lp.p2)
                Exit For ' only evaluating the longest line for now...
            Next

            labels(2) = match.labels(2)
            dst3 = taskA.lines.dst3
            labels(3) = taskA.lines.labels(3)
        End Sub
    End Class





    Public Class NR_LineEnds_Concat : Inherits TaskParent
        Public lpInput As lpData
        Dim match As New Match_Basics
        Public correlation As Single
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            labels(3) = "Correlation measures how similar the previous template is to the current one."
            desc = "Concatenate the end point templates to return a single correlation to the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.optionsChanged Then
                dst2.SetTo(0)
                dst3.SetTo(0)
            End If

            If standalone Then lpInput = taskA.lines.lpList(0)

            Dim nabeRect1 = taskA.gridNabeRects(taskA.gridMap.Get(Of Integer)(lpInput.p1.Y, lpInput.p1.X))
            Dim nabeRect2 = taskA.gridNabeRects(taskA.gridMap.Get(Of Integer)(lpInput.p2.Y, lpInput.p2.X))
            cv.Cv2.HConcat(src(nabeRect1), src(nabeRect2), match.template)
            Static templateLast = match.template.Clone

            match.Run(templateLast)
            correlation = match.correlation

            If standaloneTest() Then
                Static rFit As New Rectangle_Fit
                rFit.Run(match.template)
                dst2 = rFit.dst2.Clone
                labels(2) = "Correlation = " + Format(correlation, fmt3)

                rFit.Run(templateLast)
                dst3 = rFit.dst2

                dst1 = src.Clone
                DrawRect(dst1, nabeRect1)
                DrawRect(dst1, nabeRect2)
            End If

            templateLast = match.template.Clone
        End Sub
    End Class





    Public Class LineEnds_Correlation : Inherits TaskParent
        Public lpInput As lpData
        Dim match As New Match_Basics
        Public p1Correlation As Single
        Public p2Correlation As Single
        Public Sub New()
            desc = "Compare area around end points of a line to the previous image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lpInput = taskA.lines.lpList(0)
            Static lastImage = taskA.gray.Clone

            Dim rect = taskA.gridRects(lpInput.p1GridIndex)
            match.template = taskA.gray(rect)
            match.Run(lastImage(taskA.gridNabeRects(lpInput.p1GridIndex)))
            p1Correlation = match.correlation

            rect = taskA.gridRects(lpInput.p2GridIndex)
            match.template = taskA.gray(rect)
            match.Run(lastImage(taskA.gridNabeRects(lpInput.p2GridIndex)))
            p2Correlation = match.correlation

            lastImage = taskA.gray.Clone

            If standaloneTest() Then
                dst2 = src.Clone
                DrawLine(dst2, lpInput, taskA.highlight)
            End If
            labels(2) = "Rect for p1 has correlation " + Format(p1Correlation, fmt3) +
                    " to the previous image while " +
                    "rect for p2 has " + Format(p2Correlation, fmt3)
        End Sub
    End Class
End Namespace