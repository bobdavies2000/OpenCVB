Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics
    Inherits VBparent
    Public rectangleEdgeWidth As integer = 2
    Public inputRect As cv.Rect
    Public trackbox As New cv.Rect
    Public usingDrawRect As Boolean
    Public Sub New()
        initParent()
        label1 = "Draw anywhere to start mean shift tracking."
        task.desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track.  Tracker Algorithm"
		task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then usingDrawRect = True
        If usingDrawRect Then inputRect = task.drawRect
        If inputRect.X + inputRect.Width > src.Width Then inputRect.Width = src.Width - inputRect.X
        If inputRect.Y + inputRect.Height > src.Height Then inputRect.Height = src.Height - inputRect.Y
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As integer = {0, 1, 2}
        Dim hsize() As integer = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Static roi_hist As New cv.Mat
        If inputRect.Width > 0 And inputRect.Height > 0 Then
            If usingDrawRect Then trackbox = task.drawRect Else trackbox = inputRect
            Dim maskROI = hsv(inputRect).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist(New cv.Mat() {hsv(inputRect)}, ch, maskROI, roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            If usingDrawRect Then task.drawRectClear = True
        End If
        If trackbox.Width <> 0 Then
            Dim backProj As New cv.Mat
            cv.Cv2.CalcBackProject(New cv.Mat() {hsv}, ch, roi_hist, backProj, ranges)
            cv.Cv2.MeanShift(backProj, trackbox, cv.TermCriteria.Both(10, 1))
            dst1 = backProj.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst1.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, task.lineType)
            Show_HSV_Hist(dst2, roi_hist)
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        Else
            dst1 = src
        End If
    End Sub
End Class




Public Class MeanShift_Depth
    Inherits VBparent
    Dim ms As MeanShift_Basics
    Dim blob As Depth_ForegroundHead
    Public Sub New()
        initParent()
        ms = New MeanShift_Basics()
        blob = New Depth_ForegroundHead()
        label1 = "Draw anywhere to start mean shift tracking."
        task.desc = "Use depth to start mean shift algorithm.  Tracker Algorithm"
		task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.drawRect.Width > 0 Then
            ms.usingDrawRect = True
            ms.inputRect = New cv.Rect
        End If
        If ms.usingDrawRect Then
            ms.src = src
            ms.Run()
            dst1 = ms.dst1
            dst2 = ms.dst2
        Else
            blob.Run()
            dst1 = blob.dst1

            If blob.trustworthy Then
                ms.src = src
                ms.inputRect = blob.trustedRect
                ms.Run()
                dst2 = ms.dst2
                dst1 = ms.dst1
            Else
                dst2 = src
            End If
        End If
    End Sub
End Class



'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class MeanShift_PyrFilter
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MeanShift Spatial Radius", 1, 100, 10)
            sliders.setupTrackBar(1, "MeanShift color Radius", 1, 100, 15)
            sliders.setupTrackBar(2, "MeanShift Max Pyramid level", 1, 8, 3)
        End If
        task.desc = "Use PyrMeanShiftFiltering to segment an image."
		task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim spatialRadius = sliders.trackbar(0).Value
        Dim colorRadius = sliders.trackbar(1).Value
        Dim maxPyrLevel = sliders.trackbar(2).Value
        cv.Cv2.PyrMeanShiftFiltering(src, dst1, spatialRadius, colorRadius, maxPyrLevel)
    End Sub
End Class





' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Meanshift_TopObjects
    Inherits VBparent
    Dim blob As Blob_DepthClusters
    Dim cams(4 - 1) As MeanShift_Basics
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Public Sub New()
        initParent()
        mats1 = New Mat_4to1()

        mats2 = New Mat_4to1()

        blob = New Blob_DepthClusters()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "How often should meanshift be reinitialized", 1, 500, 100)
        End If
        For i = 0 To cams.Length - 1
            cams(i) = New MeanShift_Basics()
            cams(i).rectangleEdgeWidth = 8
        Next
        task.desc = "Track - tracking algorithm"
		task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        blob.src = src
        blob.Run()

        Dim updateFrequency = sliders.trackbar(0).Value
        Dim trackBoxes As New List(Of cv.Rect)
        For i = 0 To Math.Min(cams.Length, blob.flood.sortedSizes.Count) - 1
            If blob.flood.maskSizes.Count > i Then
                Dim camIndex = blob.flood.sortedSizes.ElementAt(i).Value
                If task.frameCount Mod updateFrequency = 0 Or cams(i).trackbox.Size.Width = 0 Or task.frameCount < 3 Then
                    cams(i).inputRect = blob.flood.rects(camIndex)
                End If

                cams(i).src = src
                cams(i).Run()
                mats1.mat(i) = cams(i).dst1.Clone()
                mats2.mat(i) = cams(i).dst2.Clone()
                trackBoxes.Add(cams(i).trackbox)
            End If
        Next
        mats1.Run()
        dst1 = mats1.dst1
        mats2.Run()
        dst2 = mats2.dst1
    End Sub
End Class



