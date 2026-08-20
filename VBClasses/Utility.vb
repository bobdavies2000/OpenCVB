Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Enum causes
        lastCellFound
        indexLastGood
        indexLastBelowZero
        indexLastAboveCount
        intersectLastRectFailed
        optionsChange
        maxDistOutsideOfLastRect
        colorSync
        wGridNotInLastList
    End Enum
    Public Class Utility_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Provide a home for some shared utility functions."
        End Sub
        Public Shared Function getFontsize() As Single
            Dim fontSize As Single
            Select Case task.workRes.Width
                Case 1920
                    fontSize = 3.5
                Case 1280
                    fontSize = 2.5
                Case 960
                    fontSize = 1.5
                Case 672
                    fontSize = 1.5
                Case 640
                    fontSize = 1.5
                Case 480
                    fontSize = 1.2
                Case 240
                    fontSize = 1.2
                Case 336
                    fontSize = 1.0
                Case 320
                    fontSize = 1.0
                Case 168
                    fontSize = 0.5
                Case 160
                    fontSize = 1.0
            End Select
            Return fontSize
        End Function
        Public Shared Function getThickness() As Integer
            Dim fontThickness As Integer = 1
            Select Case task.workRes.Width
                Case 1920
                    fontThickness = 4
                Case 1280
                    fontThickness = 2
            End Select
            Return fontThickness
        End Function
        Public Shared Function ComputeHullCentroid(hull As Point(), rcD As rcData) As Point
            Dim area As Double = 0
            Dim cx As Double = 0
            Dim cy As Double = 0

            For i = 0 To hull.Length - 1
                Dim p1 = hull(i)
                Dim p2 = hull((i + 1) Mod hull.Length)

                Dim cross = p1.X * p2.Y - p2.X * p1.Y

                area += cross
                cx += (p1.X + p2.X) * cross
                cy += (p1.Y + p2.Y) * cross
            Next

            area /= 2.0
            cx /= (6.0 * area)
            cy /= (6.0 * area)

            Return New Point(rcD.rect.X + cx, rcD.rect.Y + cy)
        End Function
        Public Shared Sub AddPlotScale(dst As Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
            Dim fontSize = getFontsize()
            Dim fontThickness = getThickness()

            Dim spacer = dst.Height / (lineCount + 1)
            Dim spaceVal = (maxVal - minVal) / (lineCount + 1)
            If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
            For i = 0 To lineCount
                Dim p1 = New cv.Point(0, spacer * i)
                Dim p2 = New cv.Point(dst.Width, spacer * i)
                Line(dst, p1, p2, white, fontThickness)
                Dim nextVal = (maxVal - spaceVal * i)
                Dim nextText = If(maxVal > 1000, (nextVal / 1000).ToString("N2") + "k", nextVal.ToString(fmt1))
                Dim p3 = New cv.Point(0, p1.Y + 12)
                PutText(dst, nextText, p3, HersheyFonts.HersheyPlain, fontSize, white, fontThickness, task.lineType)
            Next
        End Sub
        Public Shared Function Magnify(src As cv.Mat) As cv.Mat
            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
                Dim dst As cv.Mat = src(task.drawRect)
                Return dst
            End If
            Return src
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("Utility_Basics is to make some small 'Shared' utilities available.)", 3)
        End Sub
    End Class
End Namespace