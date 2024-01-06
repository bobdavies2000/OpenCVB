Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/14770756/opencv-simpleblobdetector-filterbyinertia-meaning
Public Class Blob_Basics : Inherits VB_Algorithm
    Dim options As New Options_Blob
    Dim input As New Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New()
        blobDetector = New CS_Classes.Blob_Basics
        desc = "Isolate and list blobs with specified options"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()

        If standalone Then
            input.Run(src)
            dst2 = input.dst2
        Else
            dst2 = src
        End If
        blobDetector.RunCS(dst2, dst3, options.blobParams)
    End Sub
End Class








Public Class Blob_Input : Inherits VB_Algorithm
    Dim rectangles As New Rectangle_Rotated
    Dim circles As New Draw_Circles
    Dim ellipses As New Draw_Ellipses
    Dim poly As New Draw_Polygon
    Public Mats As New Mat_4Click
    Public updateFrequency = 30
    Public Sub New()
        findSlider("DrawCount").Value = 5
        findCheckBox("Draw filled (unchecked draw an outline)").Checked = True

        Mats.mats.lineSeparators = False

        labels(2) = "Click any quadrant below to view it on the right"
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Generate data to test Blob Detector."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        rectangles.Run(src)
        Mats.mat(0) = rectangles.dst2

        circles.Run(src)
        Mats.mat(1) = circles.dst2

        ellipses.Run(src)
        Mats.mat(2) = ellipses.dst2

        poly.Run(src)
        Mats.mat(3) = poly.dst3
        mats.Run(empty)
        dst2 = Mats.dst2
        dst3 = Mats.dst3
    End Sub
End Class




Public Class Blob_RenderBlobs : Inherits VB_Algorithm
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

            dst3.Circle(maxBlob.Centroid, task.dotSize + 3, cv.Scalar.Blue, -1, task.lineType)
            dst3.Circle(maxBlob.Centroid, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        End If
    End Sub
End Class
