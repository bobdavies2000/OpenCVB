Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Moments_Basics : Inherits TaskParent
    Public centroid As cv.Point2f
    Dim fore As New XR_Foreground_KMeans
    Public scaleFactor As Integer = 1
    Public offsetPt As cv.Point
    Dim options As New Options_Kalman
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the provided mask file."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static center As cv.Point2f
        If standaloneTest() Then
            fore.Run(src)
            CvtColor(fore.dst2, dst2, cv.ColorConversionCodes.GRAY2BGR)
        End If
        Dim m = cv.Cv2.Moments(fore.dst2, True)

        If options.useKalman Then
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(emptyMat)
            center = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
        Else
            center = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
        End If
        If standaloneTest() Then Circle(dst2, center, task.DotSize + 5, cv.Scalar.Red, -1, task.lineType)
        centroid = New cv.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
    End Sub
End Class





Public Class XR_Moments_CentroidKalman : Inherits TaskParent
    Dim fore As New XR_Foreground_KMeans
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point
        labels(2) = "Red dot = Kalman smoothed centroid"
        desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fore.Run(src)
        CvtColor(fore.dst2, dst2, cv.ColorConversionCodes.GRAY2BGR)
        Dim m = cv.Cv2.Moments(fore.dst2, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(emptyMat)
            Circle(dst2, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), task.DotSize + 5, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class


