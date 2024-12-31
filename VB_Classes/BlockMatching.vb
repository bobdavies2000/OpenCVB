Imports cvb = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics : Inherits TaskParent
    Dim colorizer As New Depth_Colorizer_CPP_VB
    Dim options As New Options_BlockMatching
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels(2) = "Block matching disparity colorized like depth"
        labels(3) = "Right Image (used with left image)"
        UpdateAdvice(traceName + ": click 'Show All' to see all the available options.")
        desc = "Use OpenCV's block matching on left and right views"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Options.RunOpt()

        If task.cameraName = "Azure Kinect 4K" Then
            SetTrueText("For the K4A 4 Azure camera, the left and right views are the same.")
        End If

        Static blockMatch = cvb.StereoBM.Create()
        blockMatch.BlockSize = options.blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cvb.Rect(0, 0, task.leftview.Width, task.leftview.Height)
        blockMatch.ROI2 = New cvb.Rect(0, 0, task.leftview.Width, task.leftview.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = options.numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim tmpLeft = If(task.leftview.Channels() = 3, task.leftview.CvtColor(cvb.ColorConversionCodes.BGR2Gray), task.leftview)
        Dim tmpRight = If(task.rightview.Channels() = 3, task.rightview.CvtColor(cvb.ColorConversionCodes.BGR2Gray), task.rightview)

        Dim disparity As New cvb.Mat
        blockMatch.compute(tmpLeft, tmpRight, disparity)
        disparity.ConvertTo(dst1, cvb.MatType.CV_32F, 1 / 16)
        dst1 = dst1.Threshold(0, 0, cvb.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cvb.Rect(options.numDisparity + sideMargin, topMargin, src.Width - options.numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        cvb.Cv2.Divide(options.distance, dst1(rect), dst1(rect)) ' this needs much more refinement.  The trackbar value is just an approximation.
        dst1(rect) = dst1(rect).Threshold(10, 10, cvb.ThresholdTypes.Trunc)
        colorizer.Run(dst1)
        dst2(rect) = colorizer.dst2(rect)
        dst3 = task.rightview.Resize(src.Size())
    End Sub
End Class
