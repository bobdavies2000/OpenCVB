Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/22132510/opencv-approxpolydp-for-edge-maps-Not-contours
' https://docs.opencvb.org/4.x/js_contour_features_approxPolyDP.html
Namespace VBClasses
    Public Class ApproxPoly_Basics : Inherits TaskParent
        Dim contour As New Contour_Largest
        Dim rotatedRect As New Rectangle_Rotated
        Dim options As New Options_ApproxPoly
        Public Sub New()
            labels = {"", "", "Input to the ApproxPolyDP", "Output of ApproxPolyDP"}
            desc = "Using the input contours, create ApproxPoly output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standaloneTest() Then
                If tsk.heartBeat Then rotatedRect.Run(src)
                dst2 = rotatedRect.dst2
            End If

            contour.Run(dst2)
            dst2 = contour.dst2

            If contour.allContours.Count > 0 Then
                Dim nextContour As cv.Point()
                nextContour = cv.Cv2.ApproxPolyDP(contour.bestContour, options.epsilon, options.closedPoly)
                dst3.SetTo(0)
                DrawTour(dst3, nextContour.ToList, cv.Scalar.Yellow)
            Else
                labels(2) = "No contours found"
            End If
        End Sub
    End Class









    ' https://stackoverflow.com/questions/22132510/opencv-approxpolydp-for-edge-maps-Not-contours
    Public Class NR_ApproxPoly_FindandDraw : Inherits TaskParent
        Dim rotatedRect As New Rectangle_Rotated
        Public allContours As cv.Point()()
        Public Sub New()
            labels(2) = "FindandDraw input"
            labels(3) = "FindandDraw output - note the change in line width where ApproxPoly differs from DrawContours"
            desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)

            dst0.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
            dst3.SetTo(0)

            Dim nextContour As cv.Point()
            Dim contours As New List(Of cv.Point())
            For Each tour In allContours
                nextContour = cv.Cv2.ApproxPolyDP(tour, 3, True)
                If nextContour.Count > 2 Then contours.Add(nextContour)
            Next

            cv.Cv2.DrawContours(dst3, contours, -1, New cv.Scalar(0, 255, 255), tsk.lineWidth, tsk.lineType)
        End Sub
    End Class







    Public Class NR_ApproxPoly_Hull : Inherits TaskParent
        Dim hull As New Hull_Basics
        Dim aPoly As New ApproxPoly_Basics
        Public Sub New()
            hull.useRandomPoints = True
            labels = {"", "", "Original Hull", "Hull after ApproxPoly"}
            desc = "Use ApproxPolyDP on a hull to show impact of options (which appears to be minimal - what is wrong?)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hull.Run(src)
            dst2 = hull.dst2

            aPoly.Run(dst2)
            dst3 = aPoly.dst2
        End Sub
    End Class

End Namespace