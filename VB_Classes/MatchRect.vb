Imports System.Windows
Imports cv = OpenCvSharp
Public Class MatchRect_Basics : Inherits VB_Algorithm
    Public match As New Match_Basics
    Public rectInput As New cv.Rect
    Public rectOutput As New cv.Rect
    Public Sub New()
        desc = "Track a RedCloud rectangle using MatchTemplate.  Click on a cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static rectSave As New cv.Rect
        If task.optionsChanged Or (standalone And task.heartBeat) Then match.correlation = 0
        If match.correlation < match.options.correlationThreshold Or rectSave <> rectInput Or task.mouseClickFlag Then
            If standalone Then
                Static redC As New RedCloud_Basics
                redC.Run(src)
                dst2 = redC.dst2
                labels(2) = redC.labels(2)
                rectInput = task.rc.rect
            End If
            rectSave = rectInput
            match.template = src(rectInput).Clone
        End If

        setSelectedContour()

        match.Run(src)
        rectOutput = match.matchRect

        If standalone Then
            If task.heartBeat Then dst3.SetTo(0)
            dst3.Rectangle(rectOutput, task.highlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class
