Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics : Inherits TaskParent
    Dim colorizer As New Depth_Colorizer_CPP
    Dim options As New Options_BlockMatching
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels(2) = "Block matching disparity colorized like depth"
        labels(3) = "Right Image (used with left image)"
        desc = "Use OpenCV's block matching on left and right views"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.RunOpt()

        If task.cameraName = "Azure Kinect 4K" Then
            SetTrueText("For the K4A 4 Azure camera, the left and right views are the same.")
        End If

        Static blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = options.blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, task.leftview.Width, task.leftview.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, task.leftview.Width, task.leftview.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = options.numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim tmpLeft = If(task.leftview.Channels() = 3, task.leftview.CvtColor(cv.ColorConversionCodes.BGR2Gray), task.leftview)
        Dim tmpRight = If(task.rightview.Channels() = 3, task.rightview.CvtColor(cv.ColorConversionCodes.BGR2Gray), task.rightview)

        Dim disparity As New cv.Mat
        blockMatch.compute(tmpLeft, tmpRight, disparity)
        disparity.ConvertTo(dst1, cv.MatType.CV_32F, 1 / 16)
        dst1 = dst1.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(options.numDisparity + sideMargin, topMargin, src.Width - options.numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        cv.Cv2.Divide(options.distance, dst1(rect), dst1(rect)) ' this needs much more refinement.  The trackbar value is just an approximation.
        dst1(rect) = dst1(rect).Threshold(10, 10, cv.ThresholdTypes.Trunc)
        colorizer.Run(dst1)
        dst2(rect) = colorizer.dst2(rect)
        dst3 = task.rightview.Resize(src.Size())
    End Sub
End Class







'Public Class BlockMatching_Grid : Inherits TaskParent
'    Dim colorizer As New Depth_Colorizer_CPP
'    Dim options As New Options_BlockMatching
'    Dim lowRes As New LowRes_LeftRight
'    Public Sub New()
'        desc = "Match the low resolution left and right images."
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        options.RunOpt()
'        lowRes.Run(src)

'        Static blockMatch = cv.StereoBM.Create()
'        blockMatch.BlockSize = options.blockSize
'        blockMatch.MinDisparity = 0
'        blockMatch.ROI1 = New cv.Rect(0, 0, task.cols, task.rows)
'        blockMatch.ROI2 = New cv.Rect(0, 0, task.cols, task.rows)
'        blockMatch.PreFilterCap = 31
'        blockMatch.NumDisparities = options.numDisparity
'        blockMatch.TextureThreshold = 10
'        blockMatch.UniquenessRatio = 15
'        blockMatch.SpeckleWindowSize = 100
'        blockMatch.SpeckleRange = 32
'        blockMatch.Disp12MaxDiff = 1

'        Dim tmpLeft = If(lowRes.dst2.Channels() = 3, lowRes.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY), task.leftView)
'        Dim tmpRight = If(lowRes.dst3.Channels() = 3, lowRes.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY), task.rightView)

'        Dim disparity As New cv.Mat
'        blockMatch.compute(tmpLeft, tmpRight, disparity)
'        disparity.ConvertTo(dst1, cv.MatType.CV_32F, 1 / 16)
'        dst1 = dst1.Threshold(0, 0, cv.ThresholdTypes.Tozero)
'        Dim topMargin = 10, sideMargin = 8
'        Dim rect = New cv.Rect(options.numDisparity + sideMargin, topMargin, src.Width - options.numDisparity - sideMargin * 2, src.Height - topMargin * 2)
'        ' this needs much more refinement.  The trackbar value is just an approximation.
'        cv.Cv2.Divide(options.distance, dst1(rect), dst1(rect))
'        dst1(rect) = dst1(rect).Threshold(10, 10, cv.ThresholdTypes.Trunc)
'        colorizer.Run(dst1)
'        dst2(rect) = colorizer.dst2(rect)
'    End Sub
'End Class
