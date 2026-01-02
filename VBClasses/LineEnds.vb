Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineEnds_Basics : Inherits TaskParent
        Dim match As New LineEnds_Correlation
        Public correlations As New List(Of Single)
        Public Sub New()
            task.featureOptions.MatchCorrSlider.Value = 90
            desc = "Track each of the lines found in Line_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone
            dst3 = task.rightView
            correlations.Clear()
            For Each lp In task.lines.lpList
                match.lpInput = lp
                match.Run(src)
                correlations.Add(match.p1Correlation)
                correlations.Add(match.p2Correlation)
                If match.p1Correlation > task.fCorrThreshold And match.p2Correlation > task.fCorrThreshold Then
                    DrawLine(dst2, lp.p1, lp.p2)
                End If
                dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
                DrawLine(dst2, lp.p1, lp.p2)
                Exit For ' only evaluating the longest line for now...
            Next

            labels(2) = match.labels(2)
            dst3 = task.lines.dst3
            labels(3) = task.lines.labels(3)
        End Sub
    End Class





    Public Class LineEnds_Concat : Inherits TaskParent
        Public lpInput As lpData
        Dim match As New Match_Basics
        Public correlation As Single
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(3) = "Correlation measures how similar the previous template is to the current one."
            desc = "Concatenate the end point templates to return a single correlation to the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then
                dst2.SetTo(0)
                dst3.SetTo(0)
            End If

            If standalone Then lpInput = task.lineLongest

            Dim nabeRect1 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p1.Y, lpInput.p1.X))
            Dim nabeRect2 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p2.Y, lpInput.p2.X))
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
            If standalone Then lpInput = task.lineLongest
            Static lastImage = task.gray.Clone

            Dim rect = task.gridRects(lpInput.p1GridIndex)
            match.template = task.gray(rect)
            match.Run(lastImage(task.gridNabeRects(lpInput.p1GridIndex)))
            p1Correlation = match.correlation

            rect = task.gridRects(lpInput.p2GridIndex)
            match.template = task.gray(rect)
            match.Run(lastImage(task.gridNabeRects(lpInput.p2GridIndex)))
            p2Correlation = match.correlation

            lastImage = task.gray.Clone

            If standaloneTest() Then
                dst2 = src.Clone
                DrawLine(dst2, lpInput, task.highlight)
            End If
            labels(2) = "Rect for p1 has correlation " + Format(p1Correlation, fmt3) +
                    " to the previous image while " +
                    "rect for p2 has " + Format(p2Correlation, fmt3)
        End Sub
    End Class
End Namespace