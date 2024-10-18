Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Public Class Edge_Basics : Inherits VB_Parent
    Dim canny As New Edge_Canny
    Dim scharr As Edge_Scharr
    Dim binRed As Edge_BinarizedReduction
    Dim binSobel As Bin4Way_Sobel
    Dim sobel As Edge_Sobel
    Dim colorGap As Edge_ColorGap_CPP_VB
    Dim deriche As Edge_Deriche_CPP_VB
    Dim Laplacian As Edge_Laplacian
    Dim resizeAdd As Edge_ResizeAdd
    Dim regions As Edge_Regions
    Public options As New Options_Edge_Basics
    Public Sub New()
        desc = "Use Radio Buttons to select the different edge algorithms."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Select Case options.edgeSelection
            Case "Canny"
                If canny Is Nothing Then canny = New Edge_Canny
                canny.Run(src)
                dst2 = canny.dst2
            Case "Scharr"
                If scharr Is Nothing Then scharr = New Edge_Scharr
                scharr.Run(src)
                dst2 = scharr.dst3
            Case "Binarized Reduction"
                If binRed Is Nothing Then binRed = New Edge_BinarizedReduction
                binRed.Run(src)
                dst2 = binRed.dst2
            Case "Binarized Sobel"
                If binSobel Is Nothing Then binSobel = New Bin4Way_Sobel
                binSobel.Run(src)
                dst2 = binSobel.dst2
            Case "Sobel"
                If sobel Is Nothing Then sobel = New Edge_Sobel
                sobel.Run(src)
                dst2 = sobel.dst2
            Case "Color Gap"
                If colorGap Is Nothing Then colorGap = New Edge_ColorGap_CPP_VB
                colorGap.Run(src)
                dst2 = colorGap.dst2
            Case "Deriche"
                If deriche Is Nothing Then deriche = New Edge_Deriche_CPP_VB
                deriche.Run(src)
                dst2 = deriche.dst2
            Case "Laplacian"
                If Laplacian Is Nothing Then Laplacian = New Edge_Laplacian
                Laplacian.Run(src)
                dst2 = Laplacian.dst2
            Case "Resize And Add"
                If resizeAdd Is Nothing Then resizeAdd = New Edge_ResizeAdd
                resizeAdd.Run(src)
                dst2 = resizeAdd.dst2
            Case "Depth Region Boundaries"
                If regions Is Nothing Then regions = New Edge_Regions
                regions.Run(src)
                dst2 = regions.dst2
        End Select

        If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If dst2.Type <> cvb.MatType.CV_8UC1 Then dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
        labels(2) = traceName + " - selection = " + options.edgeSelection
    End Sub
End Class






Public Class Edge_DepthAndColor : Inherits VB_Parent
    Dim shadow As New Depth_Holes
    Dim canny As New Edge_Basics
    Dim dilate As New Dilate_Basics
    Public Sub New()
        FindRadio("Dilate shape: Rect").Checked = True

        FindSlider("Canny threshold1").Value = 100
        FindSlider("Canny threshold2").Value = 100

        desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow."
        labels(2) = "Edges in color and depth after dilate"
        labels(3) = "Edges in color and depth no dilate"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        shadow.Run(src)

        dst3 = If(shadow.dst3.Channels() <> 1, shadow.dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), shadow.dst3)
        dst3 += canny.dst2.Threshold(1, 255, cvb.ThresholdTypes.Binary)

        dilate.Run(dst3)
        dilate.dst2.SetTo(0, shadow.dst2)
        dst2 = dilate.dst2
    End Sub
End Class




'https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edge_Scharr : Inherits VB_Parent
    Dim options As New Options_Edges
    Public Sub New()
        labels(3) = "x field + y field in CV_32F format"
        desc = "Scharr is most accurate with 3x3 kernel."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim xField = src.Scharr(cvb.MatType.CV_32FC1, 1, 0)
        Dim yField = src.Scharr(cvb.MatType.CV_32FC1, 0, 1)
        cvb.Cv2.Add(xField, yField, dst3)
        dst3.ConvertTo(dst2, cvb.MatType.CV_8U, options.scharrMultiplier)
    End Sub
End Class






' https://www.learnopencvb.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Edge_Preserving : Inherits VB_Parent
    Dim options As New Options_Edges
    Public Sub New()
        labels(3) = "Edge preserving blur for BGR depth image above"
        desc = "OpenCV's edge preserving filter."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If options.recurseCheck Then
            cvb.Cv2.EdgePreservingFilter(src, dst2, cvb.EdgePreservingMethods.RecursFilter, options.EP_Sigma_s, options.EP_Sigma_r)
        Else
            cvb.Cv2.EdgePreservingFilter(src, dst2, cvb.EdgePreservingMethods.NormconvFilter, options.EP_Sigma_s, options.EP_Sigma_r)
        End If
        If options.recurseCheck Then
            cvb.Cv2.EdgePreservingFilter(task.depthRGB, dst3, cvb.EdgePreservingMethods.RecursFilter, options.EP_Sigma_s, options.EP_Sigma_r)
        Else
            cvb.Cv2.EdgePreservingFilter(task.depthRGB, dst3, cvb.EdgePreservingMethods.NormconvFilter, options.EP_Sigma_s, options.EP_Sigma_r)
        End If
    End Sub
End Class







'  https://docs.opencvb.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
Public Class Edge_RandomForest_CPP_VB : Inherits VB_Parent
    Dim rgbData() As Byte
    Dim options As New Options_Edges2
    Public Sub New()
        desc = "Detect edges using structured forests - Opencv Contrib"
        ReDim rgbData(dst2.Total * dst2.ElemSize - 1)
        labels(3) = "Thresholded Edge Mask (use slider to adjust)"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.frameCount < 100 Then SetTrueText("On the first call only, it takes a few seconds to load the randomForest model.", New cvb.Point(10, 100))

        ' why not do this in the constructor?  Because the message is held up by the lengthy process of loading the model.
        If task.frameCount = 5 Then
            Dim modelInfo = New FileInfo(task.HomeDir + "Data/model.yml.gz")
            cPtr = Edge_RandomForest_Open(modelInfo.FullName)
        End If
        If task.frameCount > 5 Then ' the first images are skipped so the message above can be displayed.
            Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)
            Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
            Dim imagePtr = Edge_RandomForest_Run(cPtr, handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleRGB.Free()

            dst3 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Threshold(options.edgeRFthreshold, 255, cvb.ThresholdTypes.Binary)
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Edge_RandomForest_Close(cPtr)
    End Sub
End Class








Public Class Edge_DCTfrequency : Inherits VB_Parent
    Dim options As New Options_Edges2
    Public Sub New()
        labels(3) = "Mask for the isolated frequencies"
        desc = "Find edges by removing all the highest frequencies."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim gray = task.depthRGB.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cvb.Mat
        Dim src32f As New cvb.Mat
        gray.ConvertTo(src32f, cvb.MatType.CV_32F, 1 / 255)
        cvb.Cv2.Dct(src32f, frequencies, cvb.DctFlags.None)

        Dim roi As New cvb.Rect(0, 0, options.removeFrequencies, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        labels(2) = "Highest " + CStr(options.removeFrequencies) + " frequencies removed from RGBDepth"

        cvb.Cv2.Dct(frequencies, src32f, cvb.DctFlags.Inverse)
        src32f.ConvertTo(dst2, cvb.MatType.CV_8UC1, 255)
        dst3 = dst2.Threshold(options.dctThreshold, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class







' https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
Public Class Edge_Deriche_CPP_VB : Inherits VB_Parent
    Dim options As New Options_Edges3
    Public Sub New()
        cPtr = Edge_Deriche_Open()
        labels(3) = "Image enhanced with Deriche results"
        desc = "Edge detection using the Deriche X and Y gradients"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If src.Channels = 1 Then src = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Edge_Deriche_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.alpha, options.omega)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC3, imagePtr).Clone
        dst3 = src Or dst2
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Edge_Deriche_Close(cPtr)
    End Sub
End Class









Public Class Edge_DCTinput : Inherits VB_Parent
    Dim edges As New Edge_Basics
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(2) = "Canny edges produced from original grayscale image"
        labels(3) = "Edges produced with featureless regions cleared"
        desc = "Use the featureless regions to enhance the edge detection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        edges.Run(src)
        dst2 = edges.dst2.Clone

        dct.Run(src)
        Dim tmp = src.SetTo(cvb.Scalar.White, dct.dst2)
        edges.Run(tmp)
        dst3 = edges.dst2
    End Sub
End Class







Public Class Edge_Consistent : Inherits VB_Parent
    Dim edges As New Bin4Way_Sobel
    Dim saveFrames As New List(Of cvb.Mat)
    Public Sub New()
        FindSlider("Sobel kernel Size").Value = 5
        desc = "Edges that are consistent for x number of frames"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then saveFrames = New List(Of cvb.Mat)

        edges.Run(src)

        Dim tmp = If(edges.dst2.Channels() = 1, edges.dst2.Clone, edges.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        saveFrames.Add(tmp)
        If saveFrames.Count > task.frameHistoryCount Then saveFrames.RemoveAt(0)

        dst2 = saveFrames(0)
        For i = 1 To saveFrames.Count - 1
            dst2 = saveFrames(i) And dst2
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, Not edges.dst3)
    End Sub
End Class






Public Class Edge_BinarizedReduction : Inherits VB_Parent
    Dim edges As New Bin4Way_Sobel
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Visualize the impact of reduction on Edge_BinarizeSobel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst3 = reduction.dst2
        edges.Run(dst3)
        dst2 = edges.mats.mat(0)
    End Sub
End Class








Public Class Edge_BinarizedBrightness : Inherits VB_Parent
    Dim edges As New Edge_Basics
    Dim bright As New Brightness_Basics
    Public Sub New()
        FindRadio("Binarized Sobel").Checked = True
        desc = "Visualize the impact of brightness on Bin4Way_Sobel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bright.Run(src)
        dst2 = bright.dst3
        edges.Run(bright.dst3)
        dst3 = edges.dst2
        labels(3) = edges.labels(2)
    End Sub
End Class










Public Class Edge_SobelLRBinarized : Inherits VB_Parent
    Dim edges As New Bin4Way_Sobel
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels = {"", "", "Horizontal Sobel - Left View", "Horizontal Sobel - Right View"}
        desc = "Isolate edges in the left and right views."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.mouseClickFlag Then task.mouseClickFlag = False ' preempt use of quadrants.

        edges.Run(task.rightView)
        If standaloneTest() Then
            addw.src2 = edges.dst3
            addw.Run(task.rightView)
            dst3 = addw.dst2.Clone
        Else
            dst3 = edges.dst3.Clone
        End If

        edges.Run(task.leftView)
        If standaloneTest() Then
            addw.src2 = edges.dst3
            addw.Run(task.leftView)
            dst2 = addw.dst2
        Else
            dst2 = edges.dst3
        End If
    End Sub
End Class








Public Class Edge_Matching : Inherits VB_Parent
    Dim match As New Match_Basics
    Dim redRects As New List(Of Integer)
    Dim options As New Options_EdgeMatching
    Public Sub New()
        desc = "Match edges in the left and right views to determine distance"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = task.leftView
        dst3 = task.rightView

        Dim maxLocs(task.gridRects.Count - 1) As Integer
        Dim highlights As New List(Of Integer)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim width = If(roi.X + roi.Width + options.searchDepth < dst2.Width, roi.Width + options.searchDepth, dst2.Width - roi.X - 1)
            Dim searchROI = New cvb.Rect(roi.X, roi.Y, width, roi.Height)
            match.template = dst3(roi)
            match.Run(dst2(searchROI))

            maxLocs(i) = match.matchRect.X
            If match.correlation > options.threshold Or redRects.Contains(i) Then
                highlights.Add(i)
                SetTrueText(Format(match.correlation, fmt2), New cvb.Point(roi.X, roi.Y), 3)
            End If
        Next

        If options.overlayChecked Then
            dst2.SetTo(255, task.gridMask)
            dst3.SetTo(255, task.gridMask)
        End If

        dst2 = If(dst2.Channels() = 3, dst2, dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR))
        dst3 = If(dst3.Channels() = 3, dst3, dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR))
        If options.highlightChecked Then
            labels(2) = "Matched grid segments in dst3 with disparity"
            For Each i In highlights
                Dim roi = task.gridRects(i)
                dst3.Rectangle(roi, cvb.Scalar.Red, 2)
                roi.X += maxLocs(i)
                dst2.Rectangle(roi, cvb.Scalar.Red, 2)
                SetTrueText(CStr(maxLocs(i)), New cvb.Point(roi.X, roi.Y), 2)
            Next
        Else
            labels(2) = "Click in dst3 to highlight segment in dst2"
            If options.clearChecked Then
                redRects.Clear()
                task.gridROIclicked = 0
                options.clearChecked = False
            End If
            If task.gridROIclicked Then
                If redRects.Contains(task.gridROIclicked) = False Then redRects.Add(task.gridROIclicked)
                For Each i In redRects
                    Dim roi = task.gridRects(i)
                    dst3.Rectangle(roi, cvb.Scalar.Red, 2)
                    roi.X += maxLocs(i)
                    dst2.Rectangle(roi, cvb.Scalar.Red, 2)
                    SetTrueText(CStr(maxLocs(i)), New cvb.Point(roi.X, roi.Y), 2)
                Next
            End If
        End If
        labels(3) = "Grid segments > " + Format(options.threshold, "#0%") + " correlation coefficient"
    End Sub
End Class








' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edge_RGB : Inherits VB_Parent
    Dim sobel As New Edge_Sobel
    Public Sub New()
        desc = "Combine the edges from all 3 channels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim img32f As New cvb.Mat
        src.ConvertTo(img32f, cvb.MatType.CV_32FC3)
        Dim split = img32f.Split()
        For i = 0 To 3 - 1
            split(i) = split(i).Normalize(0, 255, cvb.NormTypes.MinMax)
        Next
        cvb.Cv2.Merge(split, img32f)
        img32f.ConvertTo(dst2, cvb.MatType.CV_8UC3)
        For i = 0 To 3 - 1
            sobel.Run(split(i))
            split(i) = 255 - sobel.dst2
        Next
        cvb.Cv2.Merge(split, dst2)
    End Sub
End Class







' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edge_HSV : Inherits VB_Parent
    Dim edges As New Edge_RGB
    Public Sub New()
        desc = "Combine the edges from all 3 HSV channels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim hsv = src.CvtColor(cvb.ColorConversionCodes.BGR2HSV)
        edges.Run(hsv)
        dst2 = edges.dst2
    End Sub
End Class








Public Class Edge_SobelLR : Inherits VB_Parent
    Dim sobel As New Edge_Sobel
    Public Sub New()
        FindSlider("Sobel kernel Size").Value = 3
        desc = "Find the edges in the LeftViewimages."
        labels = {"", "", "Edges in Left Image", "Edges in Right Image (except on Kinect 4 Azure)"}
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        sobel.Run(task.rightView)
        dst3 = sobel.dst2.Clone()

        sobel.Run(task.leftView)
        dst2 = sobel.dst2
    End Sub
End Class







Public Class Edge_ColorGap_CPP_VB : Inherits VB_Parent
    Dim gap As New Edge_ColorGap_VB
    Public Sub New()
        cPtr = Edge_ColorGap_Open()
        desc = "Using grayscale image to identify color gaps which imply an edge - C++ version"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static distanceSlider = FindSlider("Input pixel distance")
        Static diffSlider = FindSlider("Input pixel difference")
        Dim diff = diffSlider.Value

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim dataSrc(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Edge_ColorGap_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, distanceSlider.Value And 254, diff)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone
        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst2)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Edge_ColorGap_Close(cPtr)
    End Sub
End Class










Public Class Edge_ColorGap_VB : Inherits VB_Parent
    Dim options As New Options_Edges3
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()

        labels = {"", "Vertical and Horizontal edges", "Vertical edges", "Horizontal edges"}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Using grayscale image to identify color gaps which imply an edge - VB edition"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst2.SetTo(0)
        Dim half = options.gapDistance / 2
        Dim pix1 As Integer, pix2 As Integer
        For y = 0 To dst1.Height - 1
            For x = options.gapDistance To dst1.Width - options.gapDistance - 1
                pix1 = src.Get(Of Byte)(y, x)
                pix2 = src.Get(Of Byte)(y, x + options.gapDistance)
                If Math.Abs(pix1 - pix2) >= options.gapdiff Then dst2.Set(Of Byte)(y, x + half, 255)
            Next
        Next

        dst3.SetTo(0)
        For y = options.gapDistance To dst1.Height - options.gapDistance - 1
            For x = 0 To dst1.Width - 1
                pix1 = src.Get(Of Byte)(y, x)
                pix2 = src.Get(Of Byte)(y + options.gapDistance, x)
                If Math.Abs(pix1 - pix2) >= options.gapdiff Then dst3.Set(Of Byte)(y + half, x, 255)
            Next
        Next

        dst1 = dst2 Or dst3
    End Sub
End Class







Public Class Edge_DepthGap_Native : Inherits VB_Parent
    Dim options As New Options_DepthEdges
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()

        labels = {"", "Vertical and Horizontal edges", "Vertical edges", "Horizontal edges"}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Using dpeth image to identify gaps which imply an edge"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = task.pcSplit(2)
        dst2.SetTo(0)
        Dim half = options.depthDist / 2
        Dim pix1 As Single, pix2 As Single
        For y = 0 To src.Height - 1
            For x = options.depthDist To src.Width - options.depthDist - 1
                pix1 = src.Get(Of Single)(y, x)
                pix2 = src.Get(Of Single)(y, x + options.depthDist)
                If Math.Abs(pix1 - pix2) >= options.mmDepthDiff Then dst2.Set(Of Byte)(y, x + half, 255)
            Next
        Next

        dst3.SetTo(0)
        For y = options.depthDist To src.Height - options.depthDist - 1
            For x = 0 To src.Width - 1
                pix1 = src.Get(Of Single)(y, x)
                pix2 = src.Get(Of Single)(y + options.depthDist, x)
                If Math.Abs(pix1 - pix2) >= options.mmDepthDiff Then dst3.Set(Of Byte)(y + half, x, 255)
            Next
        Next

        dst1 = dst2 Or dst3
    End Sub
End Class








Public Class Edge_DepthGap_CPP_VB : Inherits VB_Parent
    Dim options As New Options_DepthEdges
    Public Sub New()
        cPtr = Edge_DepthGap_Open()
        desc = "Create edges wherever depth differences are greater than x"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Type <> cvb.MatType.CV_32FC1 Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Edge_DepthGap_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.mmDepthDiff)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Edge_DepthGap_Close(cPtr)
    End Sub
End Class








Public Class Edge_CannyMin : Inherits VB_Parent
    Dim canny As New Edge_Canny
    Public Sub New()
        FindSlider("Canny threshold1").Value = 200
        FindSlider("Canny threshold2").Value = 200
        desc = "Set the max thresholds for Canny to get the minimum number of edge pixels"
        labels(2) = "Essential lines in the image - minimum number of pixels in Canny output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        dst2 = canny.dst2
    End Sub
End Class











Public Class Edge_CannyLeftRight : Inherits VB_Parent
    Dim canny As New Edge_Canny
    Public Sub New()
        FindSlider("Canny threshold1").Value = 200
        FindSlider("Canny threshold2").Value = 200
        labels = {"", "", "Essential lines in the left image", "Essential lines in the right image"}
        desc = "Set the max thresholds for Canny to get the minimum number of edge pixels for the left and right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(task.leftView)
        dst2 = canny.dst2.Clone

        canny.Run(task.rightView)
        dst3 = canny.dst2
    End Sub
End Class








Public Class Edge_Reduction : Inherits VB_Parent
    Dim reduction As New Reduction_Basics
    Dim edge As New Edge_Basics
    Public Sub New()
        task.redOptions.setSimpleReductionBar(1)
        labels = {"", "", "Edges in the Reduction output", "Reduction_Basics output"}
        desc = "Find edges in the reduction image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst3 = reduction.dst2

        edge.Run(dst3)
        dst2 = edge.dst2
    End Sub
End Class







Public Class Edge_Regions : Inherits VB_Parent
    Dim tiers As New Depth_Tiers
    Dim edge As New Edge_Basics
    Public Sub New()
        FindSlider("Canny threshold2").Value = 30
        labels = {"", "", "Edge_Canny output for the depth regions", "Identified regions "}
        desc = "Find the edges for the depth tiers."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        tiers.Run(src)
        dst3 = tiers.dst3

        edge.Run(dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        dst2 = edge.dst2
    End Sub
End Class








'https://docs.opencvb.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edge_Canny : Inherits VB_Parent
    Dim options As New Options_Canny
    Public Sub New()
        labels = {"", "", "Canny using L1 Norm", "Canny using L2 Norm"}
        desc = "Show canny edge detection with varying thresholds"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If src.Channels() <> cvb.MatType.CV_8U Then src.ConvertTo(src, cvb.MatType.CV_8U)
        dst2 = src.Canny(options.threshold1, options.threshold2, options.aperture, True)

        dst3.SetTo(0)
        src.CopyTo(dst3, dst2)
    End Sub
End Class








'https://docs.opencvb.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edge_CannyHistory : Inherits VB_Parent
    Dim options As New Options_Canny
    Public Sub New()
        labels = {"", "", "Canny using L1 Norm", ""}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Show canny edge over the last X frame (see global option 'FrameHistory')"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        dst2 = src.Canny(options.threshold1, options.threshold2, options.aperture, True)
        Static frameList As New List(Of cvb.Mat)
        If task.optionsChanged Then frameList.Clear()
        frameList.Add(dst2)
        dst3.SetTo(0)
        For Each m In frameList
            dst3 = dst3 Or m
        Next
        If frameList.Count >= task.frameHistoryCount Then frameList.RemoveAt(0)
    End Sub
End Class






Public Class Edge_ResizeAdd : Inherits VB_Parent
    Dim options As New Options_Edges4
    Public Sub New()
        desc = "Find edges using a resize, subtract, and threshold."
        labels(2) = "Edges found with just resizing"
        labels(3) = "Found edges added to grayscale image source."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim gray = src
        If src.Channels() = 3 Then gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim newFrame = gray(New cvb.Range(options.vertPixels, gray.Rows - options.vertPixels),
                            New cvb.Range(options.horizPixels, gray.Cols - options.horizPixels))
        newFrame = newFrame.Resize(gray.Size(), 0, 0, cvb.InterpolationFlags.Nearest)
        cvb.Cv2.Absdiff(gray, newFrame, dst2)
        dst2 = dst2.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
        cvb.Cv2.Add(gray, dst2, dst3)
    End Sub
End Class







Public Class Edge_CannyCombined : Inherits VB_Parent
    Dim canny As New Edge_CannyHistory
    Dim edges As New Edge_ResizeAdd
    Public Sub New()
        desc = "Combine the results of Edge_ResizeAdd and Canny"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        edges.Run(canny.dst2)
        dst2 = canny.dst2 Or edges.dst2
    End Sub
End Class






Public Class Edge_SobelCustomV : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        desc = "Show Sobel edge detection a custom vertical kernel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst1 = src.Filter2D(cvb.MatType.CV_32F, cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32FC1, New Single() {1, 0, -1, 2, 0, -2, 1, 0, -1}))
        dst1.ConvertTo(dst2, src.Type)
        dst1 = src.Filter2D(cvb.MatType.CV_32F, cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32FC1, New Single() {3, 0, -3, 10, 0, -10, 3, 0, -3}))
        dst1.ConvertTo(dst3, src.Type)
    End Sub
End Class







Public Class Edge_SobelCustomH : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        desc = "Show Sobel edge detection a custom horizontal kernel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst1 = src.Filter2D(cvb.MatType.CV_32F, cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32FC1, New Single() {1, 2, 1, 0, 0, 0, -1, -2, -1}))
        dst1.ConvertTo(dst2, src.Type)
        dst1 = src.Filter2D(cvb.MatType.CV_32F, cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32FC1, New Single() {3, 10, 3, 0, 0, 0, -3, -10, -3}))
        dst1.ConvertTo(dst3, src.Type)
    End Sub
End Class








Public Class Edge_SobelCustom : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Dim edgesV As New Edge_SobelCustomV
    Dim edgesH As New Edge_SobelCustomH
    Dim options As New Options_Edges4
    Public Sub New()
        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        desc = "Show Sobel edge detection with custom horizont and vertical kernels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If options.horizonCheck Then
            edgesH.Run(src)
            dst2 = edgesH.dst2
            dst3 = edgesH.dst3
        End If

        If options.verticalCheck Then edgesV.Run(src)

        If options.horizonCheck And options.verticalCheck Then
            addw.src2 = edgesV.dst2
            addw.Run(dst2)
            dst2 = addw.dst2

            addw.src2 = edgesV.dst3
            addw.Run(dst3)
            dst3 = addw.dst2
        ElseIf options.verticalCheck Then
            dst2 = edgesV.dst2.Clone
            dst3 = edgesV.dst3.Clone
        End If
    End Sub
End Class








Public Class Edge_SobelCustomLeftRight : Inherits VB_Parent
    Dim custom As New Edge_SobelCustom
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"Left Image Custom 1", "Left Image Custom 2", "Right Image Custom 1", "Right Image Custom 2"}
        desc = "Show Sobel edge detection for both left and right images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        custom.Run(task.leftView)
        dst0 = custom.dst2.Clone
        dst1 = custom.dst3.Clone

        custom.Run(task.rightView)
        dst2 = custom.dst2
        dst3 = custom.dst3
    End Sub
End Class








Public Class Edge_BackProjection : Inherits VB_Parent
    Dim valley As New HistValley_OptionsAuto
    Dim canny As New Edge_Basics
    Public Sub New()
        labels(3) = "Canny edges in grayscale (red) and edges in back projection (blue)"
        desc = "Find the edges in the HistValley_FromPeaks backprojection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        dst1 = canny.dst2.Clone

        valley.Run(src)
        dst2 = valley.dst1

        canny.Run(valley.dst1)

        Dim offset = 1
        Dim r1 = New cvb.Rect(offset, offset, dst2.Width - offset - 1, dst2.Height - offset - 1)
        Dim r2 = New cvb.Rect(0, 0, dst2.Width - offset - 1, dst2.Height - offset - 1)
        dst3.SetTo(cvb.Scalar.White)
        dst3(r1).SetTo(cvb.Scalar.Blue, canny.dst2(r2))
        dst3.SetTo(cvb.Scalar.Red, dst1)
        labels(2) = valley.labels(3)
    End Sub
End Class









'https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edge_Sobel : Inherits VB_Parent
    Public addw As New AddWeighted_Basics
    Dim options As New Options_Sobel
    Public Sub New()
        desc = "Show Sobel edge detection with varying kernel sizes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst0 = src.Sobel(cvb.MatType.CV_32F, 1, 0, options.kernelSize)
        If options.horizontalDerivative And options.verticalDerivative Then
            dst1 = src.Sobel(cvb.MatType.CV_32F, 0, 1, options.kernelSize)
            If standaloneTest() Then
                addw.src2 = dst1
                addw.Run(dst0)
                dst2 = addw.dst2.ConvertScaleAbs()
            Else
                dst2 = (dst1 + dst0).ToMat.ConvertScaleAbs()
            End If
        Else
            dst2 = dst0.ConvertScaleAbs()
        End If
    End Sub
End Class






'https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edge_SobelHorizontal : Inherits VB_Parent
    Dim edges As New Edge_Sobel
    Public Sub New()
        FindCheckBox("Vertical Derivative").Checked = False
        FindCheckBox("Horizontal Derivative").Checked = True
        desc = "Find edges with Sobel only in the horizontal direction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static thresholdSlider = FindSlider("Threshold to zero pixels below this value")
        edges.Run(src)

        dst2 = edges.dst2.Threshold(thresholdSlider.Value, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Edge_MotionFrames : Inherits VB_Parent
    Dim edges As New Edge_Basics
    Dim frames As New History_Basics
    Public Sub New()
        labels = {"", "", "The multi-frame edges output", "The Edge_Canny output for the last frame only"}
        FindSlider("Canny threshold1").Value = 50
        FindSlider("Canny threshold2").Value = 50
        desc = "Collect edges over several frames controlled with global frame history"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst3 = edges.dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        frames.Run(edges.dst2)
        dst2 = frames.dst2
    End Sub
End Class







Public Class Edge_MotionOverlay : Inherits VB_Parent
    Dim options As New Options_EdgeOverlay
    Public Sub New()
        labels(3) = "AbsDiff output of offset with original"
        desc = "Find edges by displacing the current BGR image in any direction and diff it with the original."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Static offsetImage As cvb.Mat = src.Clone
        Dim rect1 = New cvb.Rect(options.xDisp, options.yDisp, dst2.Width - options.xDisp - 1, dst2.Height - options.yDisp - 1)
        Dim rect2 = New cvb.Rect(0, 0, dst2.Width - options.xDisp - 1, dst2.Height - options.yDisp - 1)
        offsetImage(rect2) = src(rect1).Clone

        cvb.Cv2.Absdiff(src, offsetImage, dst0)
        dst2 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
        labels(2) = "Src offset (x,y) = (" + CStr(options.xDisp) + "," + CStr(options.yDisp) + ")"
    End Sub
End Class







Public Class Edge_RedCloud : Inherits VB_Parent
    Dim canny As New Edge_Basics
    Dim redC As New RedCloud_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Canny Edges (0), RedCloud output (1), RedCloud Edges(2), 0 And'd with 2"
        labels(3) = "Cell boundaries that are also real edges."
        task.redOptions.setIdentifyCells(False)
        desc = "Identify cell boundaries that are also edges."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        mats.mat(0) = canny.dst2

        redC.Run(src)
        mats.mat(1) = redC.dst2

        canny.Run(redC.dst2)
        mats.mat(2) = canny.dst2

        mats.mat(3) = mats.mat(2).SetTo(0, Not mats.mat(0))

        mats.Run(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class Edge_Color8U : Inherits VB_Parent
    Public colorMethods(10 - 1)
    Dim canny As New Edge_Basics
    Dim options As New Options_ColorMethod
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        labels = {"", "", "The 'OR' of each selected method", "The 'AND' of each selected method"}

        desc = "Find edges in a variety of Color8U algorithms then find the edges common to all."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.FirstPass Then
            Dim frmCheck = FindFrm("Options_ColorMethod CheckBoxes")
            frmCheck.Left = task.gOptions.Width / 2
        End If
        options.RunOpt()

        For i = 0 To colorMethods.Count - 1
            If options.check.Box(i).Checked Then
                If colorMethods(i) Is Nothing Then
                    Select Case i
                        Case 0
                            colorMethods(i) = New BackProject_Full()
                        Case 1
                            colorMethods(i) = New BackProject2D_Full()
                        Case 2
                            colorMethods(i) = New Bin4Way_Regions()
                        Case 3
                            colorMethods(i) = New Binarize_DepthTiers()
                        Case 4
                            colorMethods(i) = New FeatureLess_Groups()
                        Case 5
                            colorMethods(i) = New Hist3Dcolor_Basics()
                        Case 6
                            colorMethods(i) = New KMeans_Basics()
                        Case 7
                            colorMethods(i) = New LUT_Basics()
                        Case 8
                            colorMethods(i) = New Reduction_Basics()
                        Case 9
                            colorMethods(i) = New PCA_NColor_CPP_VB()
                    End Select
                End If
            End If
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim initdst3 As Boolean
        For i = 0 To colorMethods.Count - 1
            If options.check.Box(i).Checked Then
                colorMethods(i).run(src)
                If options.check.Box(i).Text = "FeatureLess_Groups" Then
                    canny.dst2 = colorMethods(i).dst2
                Else
                    canny.Run(colorMethods(i).dst3)
                End If
                dst2 = dst2 Or canny.dst2
                If initdst3 = False Then
                    dst3 = canny.dst2
                    initdst3 = True
                Else
                    dst3 = dst3 And canny.dst2
                End If
            End If
        Next
    End Sub
End Class






Public Class Edge_CannyAccum : Inherits VB_Parent
    Dim canny As New Edge_Basics
    Dim accum As New AddWeighted_Accumulate
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        desc = "Accumulate Canny edges to highlight all real edges better."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        accum.Run(canny.dst2)
        dst2 = accum.dst2
        labels(2) = "Accumulated canny edges."
    End Sub
End Class





Public Class Edge_CloudSegments : Inherits VB_Parent
    Dim segments As New Hist_CloudSegments
    Dim edges As New Edge_Sobel
    Public Sub New()
        desc = "Build edges from the point cloud segments from Hist_Cloud - simplistic approach"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        segments.Run(src)
        dst3 = segments.dst3
        edges.Run(dst3)
        dst2 = segments.dst3
    End Sub
End Class




Public Class Edge_DiffX_CPP_VB : Inherits VB_Parent
    Public segments As New Hist_CloudSegments
    Dim edges As New Edge_Sobel
    Public Sub New()
        task.redOptions.XReduction.Checked = True
        cPtr = Edge_DiffX_Open()
        desc = "Ignore edges with zero - in C++ because it needs to be optimized."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        segments.reductionVal = "X"
        segments.Run(src)
        src = segments.dst1 ' the byte version of the segmented image.

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Edge_DiffX_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr)
        dst3 = segments.dst3
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cvb.Scalar.White)
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, cvb.Scalar.White)
    End Sub
    Public Sub Close()
        Edge_DiffX_Close(cPtr)
    End Sub
End Class





Public Class Edge_DiffY_CPP_VB : Inherits VB_Parent
    Public segments As New Hist_CloudSegments
    Dim edges As New Edge_Sobel
    Public Sub New()
        task.redOptions.YReduction.Checked = True
        cPtr = Edge_DiffY_Open()
        desc = "Ignore edges with zero - in C++ because it needs to be optimized."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        segments.reductionVal = "Y"
        segments.Run(src)
        src = segments.dst1 ' the byte version of the segmented image.

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Edge_DiffY_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr)
        dst3 = segments.dst3
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cvb.Scalar.White)
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, cvb.Scalar.White)
    End Sub
    Public Sub Close()
        Edge_DiffY_Close(cPtr)
    End Sub
End Class





Public Class Edge_DiffZ_CPP_VB : Inherits VB_Parent
    Public segments As New Hist_CloudSegments
    Dim edges As New Edge_Sobel
    Public Sub New()
        task.redOptions.ZReduction.Checked = True
        cPtr = Edge_DiffY_Open()
        desc = "Ignore edges with zero - in C++ because it needs to be optimized."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        segments.reductionVal = "Z"
        segments.Run(src)
        src = segments.dst1 ' the byte version of the segmented image.

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Edge_DiffY_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr)
        dst3 = segments.dst3
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cvb.Scalar.White)
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, cvb.Scalar.White)
    End Sub
    Public Sub Close()
        Edge_DiffY_Close(cPtr)
    End Sub
End Class







Public Class Edge_DiffXYZ : Inherits VB_Parent
    Dim diffX As New Edge_DiffX_CPP_VB
    Dim diffY As New Edge_DiffY_CPP_VB
    Dim diffZ As New Edge_DiffZ_CPP_VB
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Combine the edges found in Edge_DiffX/Y/Z and Edge_DiffY of the cloud XY values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diffX.Run(src)
        mats.mat(0) = diffX.dst3

        diffY.Run(src)
        mats.mat(1) = diffY.dst3

        diffZ.Run(src)
        mats.mat(2) = diffZ.dst3

        mats.mat(3) = diffX.dst2 Or diffY.dst2 ' diffz is too much...

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







'https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Edge_LaplacianColor : Inherits VB_Parent
    Dim options As New Options_LaplacianKernels
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then labels(3) = "Laplacian of DepthRGB"
        desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.GaussianBlur(New cvb.Size(options.gaussiankernelSize,
                                             options.gaussiankernelSize), 0, 0)
        dst2 = dst2.Laplacian(cvb.MatType.CV_8U, options.LaplaciankernelSize, 1, 0).ConvertScaleAbs()
        dst2 = dst2.Threshold(options.threshold, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class







Public Class Edge_Laplacian : Inherits VB_Parent
    Dim options As New Options_LaplacianKernels
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If src.Channels <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Laplacian(cvb.MatType.CV_8U, options.LaplaciankernelSize, 1, 0).ConvertScaleAbs()
        dst2 = dst2.Threshold(options.threshold, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class