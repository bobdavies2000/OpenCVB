Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics : Inherits VBparent
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Blockmatch max disparity", 2, 5, 2)
            sliders.setupTrackBar(1, "Blockmatch block size", 5, 255, 15)
            sliders.setupTrackBar(2, "Blockmatch distance factor (approx) X1000", 1, 100, 20)
        End If
        task.desc = "Use OpenCV's block matching on left and right views"
        labels(2) = "Block matching disparity colorized like depth"
        labels(3) = "Right Image (used with left image)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam Then
            setTrueText("For the Kinect 4 Azure camera, the left and right views are the same.")
        End If

        If task.intermediateName <> "" Then
            dst2.SetTo(0)
            task.intermediateName = ""
            setTrueText("The intermediate results are the same for BlockMatching_Basics", 10, 200)
            Exit Sub
        End If
        Dim numDisparity = sliders.trackbar(0).Value * 16 ' must be a multiple of 16
        Dim blockSize = sliders.trackbar(1).Value
        If blockSize Mod 2 = 0 Then blockSize += 1 ' must be odd

        Static blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, task.leftView.Width, task.leftView.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, task.leftView.Width, task.leftView.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim disparity As New cv.Mat
        blockMatch.compute(task.leftView, task.rightView, disparity)
        Dim ctmp As New cv.Mat
        disparity.ConvertTo(ctmp, cv.MatType.CV_32F, 1 / 16)
        ctmp = ctmp.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(numDisparity + sideMargin, topMargin, src.Width - numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        Dim distance = sliders.trackbar(2).Value * 1000
        cv.Cv2.Divide(distance, ctmp(rect), ctmp(rect)) ' this needs much more refinement.  The trackbar3 value is just an approximation.
        ctmp(rect) = ctmp(rect).Threshold(10000, 10000, cv.ThresholdTypes.Trunc)
        colorizer.RunClass(ctmp)
        dst2(rect) = colorizer.dst2(rect)
        dst3 = task.rightView.Resize(src.Size())
    End Sub
End Class
