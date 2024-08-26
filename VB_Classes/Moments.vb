Imports cvb = OpenCvSharp
Public Class Moments_Basics : Inherits VB_Parent
    Public centroid As cvb.Point2f
    Dim fore As New Foreground_KMeans
    Public scaleFactor As Integer = 1
    Public offsetPt As cvb.Point
    Public kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cvb.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the provided mask file."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            fore.Run(src)
            dst2 = fore.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        End If
        Dim m = cvb.Cv2.Moments(fore.dst2, True)

        Dim center As cvb.Point2f
        If task.gOptions.UseKalman.Checked Then
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            center = New cvb.Point2f(kalman.kOutput(0), kalman.kOutput(1))
        Else
            center = New cvb.Point2f(m.M10 / m.M00, m.M01 / m.M00)
        End If
        If standaloneTest() Then DrawCircle(dst2,center, task.DotSize + 5, cvb.Scalar.Red)
        centroid = New cvb.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
    End Sub
End Class





Public Class Moments_CentroidKalman : Inherits VB_Parent
    Dim fore As New Foreground_KMeans
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cvb.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fore.Run(src)
        dst2 = fore.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Dim m = cvb.Cv2.Moments(fore.dst2, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            DrawCircle(dst2,New cvb.Point(kalman.kOutput(0), kalman.kOutput(1)), task.DotSize + 5, cvb.Scalar.Red)
        End If
    End Sub
End Class


