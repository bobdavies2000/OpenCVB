Imports cv = OpenCvSharp
Public Class Moments_Basics : Inherits VB_Parent
    Public centroid As cv.Point2f
    Dim foreground As New Foreground_KMeans2
    Public scaleFactor As Integer = 1
    Public offsetPt As cv.Point
    Public kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the provided mask file."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            foreground.Run(src)
            dst2 = foreground.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        Dim m = cv.Cv2.Moments(foreground.dst2, True)

        Dim center As cv.Point2f
        If gOptions.UseKalman.Checked Then
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            center = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
        Else
            center = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
        End If
        If standaloneTest() Then drawCircle(dst2,center, task.dotSize + 5, cv.Scalar.Red)
        centroid = New cv.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
    End Sub
End Class





Public Class Moments_CentroidKalman : Inherits VB_Parent
    Dim foreground As New Foreground_KMeans2
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        foreground.Run(src)
        dst2 = foreground.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim m = cv.Cv2.Moments(foreground.dst2, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            drawCircle(dst2,New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), task.dotSize + 5, cv.Scalar.Red)
        End If
    End Sub
End Class


