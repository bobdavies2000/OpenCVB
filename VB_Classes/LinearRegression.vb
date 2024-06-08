Imports cv = OpenCvSharp
Public Class LinearRegression_Basics : Inherits VB_Parent
    Public x As New List(Of Single)
    Public y As New List(Of Single)
    Public p1 As cv.Point, p2 As cv.Point
    Public Sub New()
        desc = "A simple example of using OpenCV's linear regression."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            x = New List(Of Single)({1, 2, 3, 4, 5})
            y = New List(Of Single)({2, 4, 5, 4, 5})
        End If
        If x.Count = 0 Then Exit Sub ' nothing supplied - happens when the horizon is off the image.

        Dim meanX = x.Average()
        Dim meanY = y.Average()

        Dim numerator As Single, denominator As Single
        For i = 0 To x.Count - 1
            numerator += (x(i) - meanX) * (y(i) - meanY)
            denominator += Math.Pow(x(i) - meanX, 2)
        Next

        Dim m = numerator / denominator
        Dim c = meanY - m * meanX

        p1 = New cv.Point(0, CInt(c))
        p2 = New cv.Point(dst2.Width, CInt(m * dst2.Width + c))
        dst2.SetTo(0)
        DrawLine(dst2, p1, p2, cv.Scalar.White)

        For i = 0 To x.Count - 1
            Dim pt As New cv.Point(x(i), y(i))
            DrawCircle(dst2,pt, task.dotSize, cv.Scalar.Red)
        Next
    End Sub
End Class






Public Class LinearRegression_Test : Inherits VB_Parent
    Dim regress As New LinearRegression_Basics
    Public Sub New()
        desc = "A simple example of using OpenCV's linear regression."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim x As New List(Of Single)({1, 2, 3, 4, 5})
        Dim y As New List(Of Single)({2, 4, 5, 4, 5})

        regress.x.Clear()
        regress.y.Clear()
        For i = 0 To x.Count - 1
            regress.x.Add(x(i))
            regress.y.Add(y(i))
        Next

        regress.Run(Nothing)
        dst2 = regress.dst2
    End Sub
End Class







Public Class LinearRegression_Random : Inherits VB_Parent
    Dim regress As New LinearRegression_Basics
    Dim random As New Random_Basics
    Public Sub New()
        desc = "A simple example of using OpenCV's linear regression."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        random.Run(Nothing)

        regress.x.Clear()
        regress.y.Clear()
        For i = 0 To random.pointList.Count - 1
            regress.x.Add(random.pointList(i).X)
            regress.y.Add(random.pointList(i).Y)
        Next

        regress.Run(Nothing)
        dst2 = regress.dst2
    End Sub
End Class
