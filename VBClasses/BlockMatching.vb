Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics : Inherits TaskParent
    Dim colorizer As New DepthColorizer_CPP
    Dim options As New Options_BlockMatching
    Public leftView As cv.Mat, rightView As cv.Mat
    Dim LRMeanSub As New MeanSubtraction_LeftRight
    Dim blockMatch As cv.StereoBM
    Public Sub New()
        labels(2) = "Block matching disparity colorized like depth"
        labels(3) = "Right Image (used with left image)"
        desc = "Use OpenCV's block matching on left and right views"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Options.Run()
        LRMeanSub.Run(src)

        If blockMatch Is Nothing Then blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = options.blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, algTask.leftview.Width, algTask.leftview.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, algTask.leftview.Width, algTask.leftview.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = options.numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim disparity As New cv.Mat
        blockMatch.compute(LRMeanSub.dst2, LRMeanSub.dst3, disparity)
        disparity.ConvertTo(dst1, cv.MatType.CV_32F, 1 / 16)
        dst1 = dst1.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(options.numDisparity + sideMargin, topMargin, src.Width - options.numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        cv.Cv2.Divide(options.distance, dst1(rect), dst1(rect)) ' this needs much more refinement.  The trackbar value is just an approximation.
        dst1(rect) = dst1(rect).Threshold(10, 10, cv.ThresholdTypes.Trunc)
        colorizer.Run(dst1)
        dst2(rect) = colorizer.dst2(rect)
        dst3 = algTask.rightview.Resize(src.Size())
    End Sub
    Public Sub Close()
        If blockMatch IsNot Nothing Then blockMatch.Dispose()
    End Sub
End Class