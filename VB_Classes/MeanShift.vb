Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits VBparent
    Public rectangleEdgeWidth As integer = 2
    Public inputRect As cv.Rect
    Public trackbox As New cv.Rect
    Public usingDrawRect As Boolean
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        task.desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then usingDrawRect = True
        If usingDrawRect Then inputRect = task.drawRect
        If inputRect.X + inputRect.Width > src.Width Then inputRect.Width = src.Width - inputRect.X
        If inputRect.Y + inputRect.Height > src.Height Then inputRect.Height = src.Height - inputRect.Y
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
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
            dst2 = backProj.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, task.lineType)
            Show_HSV_Hist(dst3, roi_hist)
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        Else
            dst2 = src
        End If
    End Sub
End Class




Public Class MeanShift_Depth : Inherits VBparent
    Dim ms As New MeanShift_Basics
    Dim blob As New Depth_ForegroundHead
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        task.desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.drawRect.Width > 0 Then
            ms.usingDrawRect = True
            ms.inputRect = New cv.Rect
        End If
        If ms.usingDrawRect Then
            ms.RunClass(src)
            dst2 = ms.dst2
            dst3 = ms.dst3
        Else
            blob.RunClass(src)
            dst2 = blob.dst2

            If blob.trustworthy Then
                ms.inputRect = blob.trustedRect
                ms.RunClass(src)
                dst3 = ms.dst3
                dst2 = ms.dst2
            Else
                dst3 = src
            End If
        End If
    End Sub
End Class



'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class MeanShift_PyrFilter : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "MeanShift Spatial Radius", 1, 100, 10)
            sliders.setupTrackBar(1, "MeanShift color Radius", 1, 100, 15)
            sliders.setupTrackBar(2, "MeanShift Max Pyramid level", 1, 8, 3)
        End If
        task.desc = "Use PyrMeanShiftFiltering to segment an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim spatialRadius = sliders.trackbar(0).Value
        Dim colorRadius = sliders.trackbar(1).Value
        Dim maxPyrLevel = sliders.trackbar(2).Value
        cv.Cv2.PyrMeanShiftFiltering(src, dst2, spatialRadius, colorRadius, maxPyrLevel)
    End Sub
End Class





'' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
'Public Class Meanshift_TopObjects : Inherits VBparent
'    Dim blob As New Blob_DepthRanges
'    Dim cams(4 - 1) As MeanShift_Basics
'    Dim mats1 As New Mat_4to1
'    Dim mats2 As New Mat_4to1
'    Dim flood As New FloodFill_Basics
'    Public Sub New()
'        If sliders.Setup(caller) Then
'            sliders.setupTrackBar(0, "How often should meanshift be reinitialized", 1, 500, 100)
'        End If
'        For i = 0 To cams.Length - 1
'            cams(i) = New MeanShift_Basics()
'            cams(i).rectangleEdgeWidth = 8
'        Next
'        task.desc = "Track up to 4 objects with meanshift"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        blob.RunClass(src)
'        dst2 = blob.dst3
'        flood.RunClass(dst2)

'        Dim updateFrequency = sliders.trackbar(0).Value
'        Dim trackBoxes As New List(Of cv.Rect)
'        For i = 0 To Math.Min(cams.Length, flood.sortedSizes.Count) - 1
'            If flood.maskSizes.Count > i Then
'                Dim camIndex = flood.sortedSizes.ElementAt(i).Value
'                If task.frameCount Mod updateFrequency = 0 Or cams(i).trackbox.Size.Width = 0 Or task.frameCount < 3 Then
'                    cams(i).inputRect = flood.rects(camIndex)
'                End If

'                cams(i).RunClass(src)
'                mats1.mat(i) = cams(i).dst2.Clone()
'                mats2.mat(i) = cams(i).dst3.Clone()
'                trackBoxes.Add(cams(i).trackbox)
'            End If
'        Next
'        mats1.RunClass(Nothing)
'        dst2 = mats1.dst2
'        mats2.RunClass(Nothing)
'        dst3 = mats2.dst2
'    End Sub
'End Class



