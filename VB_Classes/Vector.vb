Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/37492663/how-to-use-magnitude-and-absdiff-opencv-functions-to-compute-distances
Public Class Vector_Magnitude : Inherits VB_Parent
    Public Sub New()
        desc = "Compute Euclidian and Manhattan Distance on a single vector."
        labels(2) = "Vector Magnitude"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim cVector() As Single = {1, 4, 4, 8}
        strOut = "p1 = (" + CStr(cVector(0)) + ", " + CStr(cVector(1)) + ")" + vbTab + " p2 = (" + CStr(cVector(2)) + ", " + CStr(cVector(3)) + ")" + vbCrLf + vbCrLf
        Dim coordinates As New cv.Mat(1, 4, cv.MatType.CV_32F, cVector)
        Dim diff_x = coordinates.Col(0) - coordinates.Col(2)
        Dim diff_y = coordinates.Col(1) - coordinates.Col(3)

        ' sqrt((x2 - x1)^2 + (y2 - y1)^2)
        Dim euclidean_distance As New cv.Mat
        cv.Cv2.Magnitude(diff_x, diff_y, euclidean_distance)
        strOut += "euclidean_distance = " + CStr(euclidean_distance.Get(Of Single)(0, 0)) + vbCrLf + vbCrLf

        Dim manhattan_distance = cv.Cv2.Abs(diff_x) + cv.Cv2.Abs(diff_y)
        strOut += "manhattan_distance = " + CStr(manhattan_distance.ToMat.Get(Of Single)(0, 0)) + vbCrLf + vbCrLf

        ' Another way to compute L1 distance, with absdiff
        ' abs(x2 - x1) + abs(y2 - y1)
        Dim points1 = coordinates(cv.Range.All(), New cv.Range(0, 2))
        Dim points2 = coordinates(cv.Range.All(), New cv.Range(2, 4))
        Dim other_manhattan_distance As New cv.Mat
        cv.Cv2.Absdiff(points1, points2, other_manhattan_distance)
        other_manhattan_distance = other_manhattan_distance.Col(0) + other_manhattan_distance.Col(1)
        strOut += "other_manhattan_distance = " + CStr(other_manhattan_distance.Get(Of Single)(0, 0))
        SetTrueText(strOut)
    End Sub
End Class



