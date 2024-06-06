Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/14770756/opencv-simpleblobdetector-filterbyinertia-meaning
Public Class Blob_Input : Inherits VB_Parent
    Dim rotatedRect As New Rectangle_Rotated
    Dim circles As New Draw_Circles
    Dim ellipses As New Draw_Ellipses
    Dim poly As New Draw_Polygon
    Public Mats As New Mat_4Click
    Public updateFrequency = 30
    Public Sub New()
        FindSlider("DrawCount").Value = 5
        findCheckBox("Draw filled (unchecked draw an outline)").Checked = True

        Mats.mats.lineSeparators = False

        labels(2) = "Click any quadrant below to view it on the right"
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Generate data to test Blob Detector."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rotatedRect.Run(src)
        Mats.mat(0) = rotatedRect.dst2

        circles.Run(src)
        Mats.mat(1) = circles.dst2

        ellipses.Run(src)
        Mats.mat(2) = ellipses.dst2

        poly.Run(src)
        Mats.mat(3) = poly.dst3
        Mats.Run(empty)
        dst2 = Mats.dst2
        dst3 = Mats.dst3
    End Sub
End Class




Public Class Blob_RenderBlobs : Inherits VB_Parent
    Dim input As New Blob_Input
    Public Sub New()
        labels(2) = "Input blobs"
        labels(3) = "Largest blob, centroid in yellow"
        desc = "Use connected components to find blobs."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.frameCount Mod input.updateFrequency = 0 Then
            input.Run(src)
            dst2 = input.dst2
            Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.Binary)
            Dim labelView = dst2.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            cc.RenderBlobs(labelView)

            For Each b In cc.Blobs.Skip(1)
                dst2.Rectangle(b.Rect, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            Next

            Dim maxBlob = cc.GetLargestBlob()
            dst3.SetTo(0)
            cc.FilterByBlob(dst2, dst3, maxBlob)

            dst3.Circle(New cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.dotSize + 3, cv.Scalar.Blue, -1, task.lineType)
            drawCircle(dst3, New cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.dotSize, cv.Scalar.Yellow)
        End If
    End Sub
End Class
