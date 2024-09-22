Imports System.Windows
Imports cvb = OpenCvSharp
Public Class MatchRect_Basics : Inherits VB_Parent
    Public match As New Match_Basics
    Public rectInput As New cvb.Rect
    Public rectOutput As New cvb.Rect
    Dim rectSave As New cvb.Rect
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Track a RedCloud rectangle using MatchTemplate.  Click on a cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then match.correlation = 0
        If match.correlation < match.options.correlationMin Or rectSave <> rectInput Or task.mouseClickFlag Then
            If standalone Then
                redC.Run(src)
                dst2 = redC.dst2
                labels(2) = redC.labels(2)
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




Public Class MatchRect_RedCloud : Inherits VB_Parent
    Dim matchRect As New MatchRect_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Track a RedCloud cell using MatchTemplate."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
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
