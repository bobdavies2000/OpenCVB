Imports System.Windows.Markup
Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits VB_Algorithm
    Public xData As cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Private Function findTransition(startRow As Integer, stopRow As Integer, stepRow As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        For y = startRow To stopRow Step stepRow
            For x = 0 To xData.Cols - 1
                lastVal = val
                val = xData.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' insert sub-pixel accuracy here but we might want to avoid it.
                    Dim pt = New cv.Point2f(x + Math.Abs(val) / Math.Abs(val - lastVal), y)
                    'Dim pt = New cv.Point2f(CInt(x + Math.Abs(val) / Math.Abs(val - lastVal)), y)
                    Return pt
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Sub RunVB(src As cv.Mat)
        If gOptions.gravityPointCloud.Checked Then
            xData = task.pcSplit(0)
        Else
            Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
            Dim split = pc.Split()
            xData = split(0)
        End If

        Dim p1 = findTransition(0, xData.Height - 1, 1)
        Dim p2 = findTransition(xData.Height - 1, 0, -1)
        Dim lp = New pointPair(p1, p2)
        task.gravityVec = lp.edgeToEdgeLine(dst2.Size)

        If p1.X > 0 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(xData.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(xData.Get(Of Single)(p1.Y, p1.X - 1))
        End If
        setTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, 255, task.lineWidth, task.lineType)
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, 255, task.lineWidth, task.lineType)
        End If
    End Sub
End Class








Public Class Gravity_HorizonCompare : Inherits VB_Algorithm
    Dim gravity As New Gravity_Basics
    Dim horizon As New Horizon_Basics
    Public Sub New()
        desc = "Compare the results of Horizon_Basics with Gravity_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gravity.Run(src)
        Dim g1 = task.gravityVec
        Dim h1 = task.horizonVec

        horizon.Run(src)
        Dim g2 = task.gravityVec
        Dim h2 = task.horizonVec

        If standaloneTest() Then
            setTrueText(strOut, 3)

            dst2.SetTo(0)
            dst2.Line(g1.p1, g1.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Line(g2.p1, g2.p2, task.highlightColor, task.lineWidth, task.lineType)

            dst2.Line(h1.p1, h1.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
            dst2.Line(h2.p1, h2.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        End If
    End Sub
End Class








Public Class Gravity_Horizon : Inherits VB_Algorithm
    Dim perp As New Line_Perpendicular
    Dim gravity As New Gravity_Basics
    Dim horizon As New Horizon_Basics
    Public Sub New()
        labels(2) = "Gravity vector Integer yellow and Horizon vector in red."
        desc = "Compute the gravity vector and the horizon vector separately"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gravity.Run(src)
        Dim g1 = task.gravityVec

        horizon.Run(src)
        Dim h2 = task.horizonVec
        task.gravityVec = g1

        If standaloneTest() Then
            setTrueText("Gravity vector (yellow):" + vbCrLf + gravity.strOut + vbCrLf + vbCrLf + "Horizon Vector (red): " + vbCrLf + horizon.strOut, 3)
            dst2.SetTo(0)
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        End If
    End Sub
End Class
