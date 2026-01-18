Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Moments_Basics : Inherits TaskParent
        Public centroid As cv.Point2f
        Dim fore As New Foreground_KMeans
        Public scaleFactor As Integer = 1
        Public offsetPt As cv.Point
        Dim options As New Options_Kalman
        Public Sub New()
            task.kalman = New Kalman_Basics
            ReDim task.kalman.kInput(2 - 1) ' 2 elements - cv.point
            labels(2) = "Red dot = Kalman smoothed centroid"
            desc = "Compute the centroid of the provided mask file."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static center As cv.Point2f
            If standaloneTest() Then
                fore.Run(src)
                dst2 = fore.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
            Dim m = cv.Cv2.Moments(fore.dst2, True)

            If options.useKalman Then
                task.kalman.kInput(0) = m.M10 / m.M00
                task.kalman.kInput(1) = m.M01 / m.M00
                task.kalman.Run(emptyMat)
                center = New cv.Point2f(task.kalman.kOutput(0), task.kalman.kOutput(1))
            Else
                center = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
            End If
            If standaloneTest() Then DrawCircle(dst2, center, task.DotSize + 5, cv.Scalar.Red)
            centroid = New cv.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
        End Sub
    End Class





    Public Class NR_Moments_CentroidKalman : Inherits TaskParent
        Dim fore As New Foreground_KMeans
        Public Sub New()
            task.kalman = New Kalman_Basics
            ReDim task.kalman.kInput(2 - 1) ' 2 elements - cv.point
            labels(2) = "Red dot = Kalman smoothed centroid"
            desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fore.Run(src)
            dst2 = fore.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim m = cv.Cv2.Moments(fore.dst2, True)
            If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
                task.kalman.kInput(0) = m.M10 / m.M00
                task.kalman.kInput(1) = m.M01 / m.M00
                task.kalman.Run(emptyMat)
                DrawCircle(dst2, New cv.Point(task.kalman.kOutput(0), task.kalman.kOutput(1)), task.DotSize + 5, cv.Scalar.Red)
            End If
        End Sub
    End Class


End Namespace