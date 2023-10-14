Imports cv = OpenCvSharp

Public Class Hull_Basics : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public inputPoints As New List(Of cv.Point2f)
    Public hull As New List(Of cv.Point)
    Public useRandomPoints As Boolean
    Public Sub New()
        labels = {"", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", ""}
        If standalone Then random.range = New cv.Rect(100, 100, 50, 50)
        desc = "Given a list of points, create a hull that encloses them."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If (standalone And heartBeat()) Or (useRandomPoints And heartBeat()) Then
            random.Run(Nothing)
            dst2 = random.dst2
            inputPoints = New List(Of cv.Point2f)(random.pointList)
        End If
        Dim hull2f = cv.Cv2.ConvexHull(inputPoints, True)
        hull = vbFloat2Int(hull2f.ToList)
        vbDrawContour(dst2, hull, cv.Scalar.Yellow)
    End Sub
End Class
