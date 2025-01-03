Imports cv = OpenCvSharp
Public Class MatchRect_Basics : Inherits TaskParent
    Public match As New Match_Basics
    Public rectInput As New cv.Rect
    Public rectOutput As New cv.Rect
    Dim rectSave As New cv.Rect
    Public Sub New()
        desc = "Track a RedCloud rectangle using MatchTemplate.  Click on a cell."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If task.optionsChanged Then match.correlation = 0
        If match.correlation < match.options.correlationMin Or rectSave <> rectInput Or task.mouseClickFlag Then
            If standalone Then
                If task.firstPass Then task.redC = New RedCloud_Basics
                task.redC.Run(src)
                dst2 = task.redC.dst2
                labels(2) = task.redC.labels(2)
                rectInput = task.rc.rect
            End If
            rectSave = rectInput
            match.template = src(rectInput).Clone
        End If

        match.Run(src)
        rectOutput = match.matchRect

        If standalone Then
            If task.heartBeat Then dst3.SetTo(0)
            dst3.Rectangle(rectOutput, task.HighlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class




Public Class MatchRect_RedCloud : Inherits TaskParent
    Dim matchRect As New MatchRect_Basics
    Public Sub New()
        task.redC = New RedCloud_Basics
        desc = "Track a RedCloud cell using MatchTemplate."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)
        task.ClickPoint = task.rc.maxDist

        If task.heartBeat Then matchRect.rectInput = task.rc.rect

        matchRect.Run(src)
        If standalone Then
            If task.heartBeat Then dst3.SetTo(0)
            dst3.Rectangle(matchRect.rectOutput, task.HighlightColor, task.lineWidth, task.lineType)
        End If
        labels(2) = "MatchLine correlation = " + Format(matchRect.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
    End Sub
End Class
