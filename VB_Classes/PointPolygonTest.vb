Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class PointPolygonTest_Basics : Inherits VB_Algorithm
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        desc = "PointPolygonTest will decide what is inside and what is outside."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            rotatedRect.Run(src)
            src = rotatedRect.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If

        dst2 = src.Clone
        Dim contours As cv.Point()()
        cv.Cv2.FindContours(src, contours, Nothing, RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        For i = 0 To dst1.Rows - 1
            For j = 0 To dst1.Cols - 1
                Dim distance = cv.Cv2.PointPolygonTest(contours(0), New cv.Point(j, i), True)
                dst1.Set(Of Single)(i, j, distance)
            Next
        Next

        Dim mm = vbMinMax(dst1)
        mm.minVal = Math.Abs(mm.minVal)
        mm.maxVal = Math.Abs(mm.maxVal)

        Dim blue As New cv.Vec3b(0, 0, 0)
        Dim red As New cv.Vec3b(0, 0, 0)
        For i = 0 To src.Rows - 1
            For j = 0 To src.Cols - 1
                Dim val = dst1.Get(Of Single)(i, j)
                If val < 0 Then
                    blue(0) = 255 - Math.Abs(val) * 255 / mm.minVal
                    dst3.Set(Of cv.Vec3b)(i, j, blue)
                ElseIf val > 0 Then
                    red(2) = 255 - val * 255 / mm.maxVal
                    dst3.Set(Of cv.Vec3b)(i, j, red)
                Else
                    dst3.Set(Of cv.Vec3b)(i, j, white)
                End If
            Next
        Next
    End Sub
End Class
