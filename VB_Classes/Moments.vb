Imports cv = OpenCvSharp
Public Class Moments_Basics
    Inherits VBparent
    Public inputMask As cv.Mat
    Public centroid As cv.Point2f
    Dim foreground As kMeans_Depth_FG_BG
    Public scaleFactor As Integer = 1
    Public offsetPt As cv.Point
    Public kalman As Kalman_Basics
    Public useKalman As Boolean = True
    Public Sub New()
        initParent()

        If standalone Then foreground = New kMeans_Depth_FG_BG()

        kalman = New Kalman_Basics()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point

        label1 = "Red dot = Kalman smoothed centroid"
        task.desc = "Compute the centroid of the provided mask file."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            foreground.Run()
            dst1 = foreground.dst1
            inputMask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        Dim m = cv.Cv2.Moments(inputMask, True)

        Dim center As cv.Point2f
        If kalman.check.Box(0).Checked Or useKalman Then
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run()
            center = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
        Else
            center = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
        End If
        If standalone or task.intermediateReview = caller Then dst1.Circle(center, 10, cv.Scalar.Red, -1, task.lineType)
        centroid = New cv.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
    End Sub
End Class





Public Class Moments_CentroidKalman
    Inherits VBparent
    Dim foreground As kMeans_Depth_FG_BG
    Dim kalman As Kalman_Basics
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics()
        ReDim kalman.kInput(2 - 1) ' 2 elements - cv.point

        foreground = New kMeans_Depth_FG_BG()

        label1 = "Red dot = Kalman smoothed centroid"
        task.desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        foreground.Run()
        dst1 = foreground.dst1
        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run()
            dst1.Circle(New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class

