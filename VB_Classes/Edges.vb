Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
'https://docs.opencv.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edges_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Canny threshold1", 1, 255, 50)
            sliders.setupTrackBar(1, "Canny threshold2", 1, 255, 50)
            sliders.setupTrackBar(2, "Canny Aperture", 3, 7, 3)
        End If

        task.desc = "Show canny edge detection with varying thresholds"
        label1 = "Canny using L1 Norm"
        label2 = "Canny using L2 Norm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim threshold1 As Integer = sliders.trackbar(0).Value
        Dim threshold2 As Integer = sliders.trackbar(1).Value
        Dim aperture = If(sliders.trackbar(2).Value Mod 2, sliders.trackbar(2).Value, sliders.trackbar(2).Value + 1)

        Dim input = src
        If input.Channels = 3 Then input = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst1 = input.Canny(threshold1, threshold2, aperture, False)
        dst2 = input.Canny(threshold1, threshold2, aperture, True)
    End Sub
End Class






Public Class Edges_DepthAndColor
    Inherits VBparent
    Dim shadow As Depth_Holes
    Dim canny As Edges_Basics
    Dim dilate As DilateErode_Basics
    Public Sub New()
        initParent()
        dilate = New DilateErode_Basics()
        dilate.radio.check(2).Checked = True

        canny = New Edges_Basics()
        canny.sliders.trackbar(0).Value = 100
        canny.sliders.trackbar(1).Value = 100

        shadow = New Depth_Holes()

        task.desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow."
        label1 = "Edges in color and depth after dilate"
        label2 = "Edges in color and depth no dilate"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        canny.src = src
        canny.Run()
        shadow.Run()

        dst2 = shadow.dst2
        dst2 += canny.dst1.Threshold(1, 255, cv.ThresholdTypes.Binary)

        dilate.src = dst2
        dilate.Run()
        dilate.dst1.SetTo(0, shadow.holeMask)
        dst1 = dilate.dst1
    End Sub
End Class







'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Edges_Laplacian
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Gaussian Kernel", 1, 32, 7)
            sliders.setupTrackBar(1, "Laplacian Kernel", 1, 32, 5)
        End If
        label2 = "Laplacian of Depth Image"
        task.desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim gaussiankernelSize = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value - 1)
        Dim laplaciankernelSize = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value - 1)

        dst1 = src.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst1 = dst1.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst1 = dst1.ConvertScaleAbs()

        dst2 = task.RGBDepth.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst2 = dst2.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst2 = dst2.ConvertScaleAbs()
    End Sub
End Class



'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Scharr
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Scharr multiplier X100", 1, 500, 50)
        End If
        label2 = "x field + y field in CV_32F format"
        task.desc = "Scharr is most accurate with 3x3 kernel."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim xField = gray.Scharr(cv.MatType.CV_32FC1, 1, 0)
        Dim yField = gray.Scharr(cv.MatType.CV_32FC1, 0, 1)
        cv.Cv2.Add(xField, yField, dst2)
        dst2.ConvertTo(dst1, cv.MatType.CV_8U, sliders.trackbar(0).Value / 100)
    End Sub
End Class






' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Edges_Preserving
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Edge RecurseFilter"
            radio.check(1).Text = "Edge NormconvFilter"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Edge Sigma_s", 0, 200, 10)
            sliders.setupTrackBar(1, "Edge Sigma_r", 1, 100, 40)
        End If
        label2 = "Edge preserving blur for RGB depth image above"
        task.desc = "OpenCV's edge preserving filter."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(src, dst1, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(src, dst1, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst2, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst2, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
    End Sub
End Class






Module Edges_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Open(modelFileName As String) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edges_RandomForest_Close(Edges_RandomForestPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Run(Edges_RandomForestPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_Deriche_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edges_Deriche_Close(Edges_DerichePtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_Deriche_Run(Edges_DerichePtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, alpha As Single, omega As Single) As IntPtr
    End Function
End Module







'  https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
Public Class Edges_RandomForest_CPP
    Inherits VBparent
    Dim rgbData() As Byte
    Dim EdgesPtr As IntPtr
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Edges RF Threshold", 1, 255, 35)
        End If

        task.desc = "Detect edges using structured forests - Opencv Contrib"
        ReDim rgbData(src.Total * src.ElemSize - 1)
        label2 = "Thresholded Edge Mask (use slider to adjust)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount < 100 Then task.trueText("On the first call only, it takes a few seconds to load the randomForest model.", 10, 100)

        ' why not do this in the constructor?  Because the message is held up by the lengthy process of loading the model.
        If task.frameCount = 5 Then
            Dim modelInfo = New FileInfo(task.parms.homeDir + "Data/model.yml.gz")
            EdgesPtr = Edges_RandomForest_Open(modelInfo.FullName)
        End If
        If task.frameCount > 5 Then ' the first images are skipped so the message above can be displayed.
            Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)
            Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
            Dim gray8u = Edges_RandomForest_Run(EdgesPtr, handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleRGB.Free() ' free the pinned memory...

            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, gray8u).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
    Public Sub Close()
        Edges_RandomForest_Close(EdgesPtr)
    End Sub
End Class






Public Class Edges_ResizeAdd
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Border Vertical in Pixels", 1, 20, 5)
            sliders.setupTrackBar(1, "Border Horizontal in Pixels", 1, 20, 5)
            sliders.setupTrackBar(2, "Threshold for Pixel Difference", 1, 50, 16)
        End If
        task.desc = "Find edges using a resize, subtract, and threshold."
        label1 = "Edges found with just resizing"
        label2 = "Found edges added to grayscale image source."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim newFrame = gray(New cv.Range(sliders.trackbar(0).Value, gray.Rows - sliders.trackbar(0).Value),
                            New cv.Range(sliders.trackbar(1).Value, gray.Cols - sliders.trackbar(1).Value))
        newFrame = newFrame.Resize(gray.Size())
        cv.Cv2.Absdiff(gray, newFrame, dst1)
        dst1 = dst1.Threshold(sliders.trackbar(2).Value, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.Add(gray, dst1, dst2)
    End Sub
End Class







Public Class Edges_DCTfrequency
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Remove Frequencies < x", 0, 100, 32)
            sliders.setupTrackBar(1, "Threshold after Removal", 1, 255, 20)
        End If

        label2 = "Mask for the isolated frequencies"
        task.desc = "Find edges by removing all the highest frequencies."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.trackbar(0).Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        label1 = "Highest " + CStr(sliders.trackbar(0).Value) + " frequencies removed from RGBDepth"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(dst1, cv.MatType.CV_8UC1, 255)
        dst2 = dst1.Threshold(sliders.trackbar(1).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







' https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
Public Class Edges_Deriche_CPP
    Inherits VBparent
    Dim Edges_Deriche As IntPtr
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Deriche Alpha", 1, 400, 100)
            sliders.setupTrackBar(1, "Deriche Omega", 1, 1000, 100)
        End If
        Edges_Deriche = Edges_Deriche_Open()
        label2 = "Image enhanced with Deriche results"
        task.desc = "Edge detection using the Deriche X and Y gradients - Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim alpha = sliders.trackbar(0).Value / 100
        Dim omega = sliders.trackbar(1).Value / 1000
        Dim imagePtr = Edges_Deriche_Run(Edges_Deriche, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, alpha, omega)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total * src.ElemSize() - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        End If
        cv.Cv2.BitwiseOr(src, dst1, dst2)
    End Sub
    Public Sub Close()
        Edges_Deriche_Close(Edges_Deriche)
    End Sub
End Class








Public Class Edges_Palette
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        edges = New Edges_Basics
        palette = New Palette_Basics
        Dim randomRadio = findRadio("Random - use slider to adjust")
        randomRadio.Checked = True
        Dim paletteSlider = findSlider("Number of color transitions (Used only with Random)")
        paletteSlider.Value = 2

        label1 = "Edges found using grayscale image"
        label2 = "Edges found after palettizing grayscale image"
        task.desc = "Use palette to help canny find more edges"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst1.Clone

        palette.src = src
        palette.Run()
        edges.src = palette.dst1
        edges.Run()
        dst2 = edges.dst1.Clone
    End Sub
End Class








Public Class Edges_DCTinput
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim dct As DCT_FeatureLess
    Public Sub New()
        initParent()
        edges = New Edges_Basics
        dct = New DCT_FeatureLess
        label1 = "Canny edges produced from original grayscale image"
        label2 = "Edges produced with featureless regions cleared"
        task.desc = "Use the featureless regions to enhance the edge detection"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        edges.src = src
        edges.Run()
        dst1 = edges.dst1.Clone

        dct.src = src
        dct.Run()
        edges.src = src.SetTo(cv.Scalar.White, dct.dst1)
        edges.Run()
        dst2 = edges.dst1
    End Sub
End Class








Public Class Edges_BinarizedCanny
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim binarize As Binarize_Recurse
    Dim mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click
        binarize = New Binarize_Recurse
        edges = New Edges_Basics
        label1 = "Edges between halves, lightest, darkest, and the combo"
        task.desc = "Collect edges from binarized images"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        binarize.src = src
        binarize.Run()

        edges.src = binarize.mats.mat(0) ' the light and dark halves
        edges.Run()
        mats.mat(0) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.src = binarize.mats.mat(1) ' the lightest of the light half
        edges.Run()
        mats.mat(1) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.src = binarize.mats.mat(3) ' the darkest of the dark half
        edges.Run()
        mats.mat(2) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        mats.Run()
        dst1 = mats.dst1
        If mats.dst2.Channels = 3 Then
            label2 = "Combo of first 3 below.  Click quadrants in dst1."
            dst2 = mats.mat(3)
        Else
            dst2 = mats.dst2
        End If
    End Sub
End Class








Public Class Edges_BinarizedBrightness
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim bright As PhotoShop_Brightness
    Public Sub New()
        initParent()

        edges = New Edges_BinarizedSobel
        bright = New PhotoShop_Brightness

        task.desc = "Visualize the impact of brightness on Edges_BinarizeSobel"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        bright.src = src
        bright.Run()
        dst1 = bright.dst2

        edges.src = bright.dst2
        edges.Run()
        dst2 = edges.dst2
    End Sub
End Class







Public Class Edges_BinarizedReduction
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()

        edges = New Edges_BinarizedSobel
        reduction = New Reduction_Basics

        task.desc = "Visualize the impact of reduction on Edges_BinarizeSobel"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        reduction.src = src
        reduction.Run()
        dst1 = reduction.dst1

        edges.src = dst1
        edges.Run()
        dst2 = edges.dst2
    End Sub
End Class








Public Class Edges_Depth
    Inherits VBparent
    Dim dMax As Depth_SmoothMax
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        dMax = New Depth_SmoothMax
        sobel = New Edges_Sobel
        Dim kernelSlider = findSlider("Sobel kernel Size")
        kernelSlider.Value = 14
        task.desc = "Use Depth_SmoothMax to find edges in Depth"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        dMax.src = task.depth32f
        dMax.Run()
        dst1 = dMax.dst1

        sobel.src = dMax.dst2
        sobel.Run()
        dst2 = sobel.dst1
    End Sub
End Class









Public Class Edges_FeaturesOnly
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim featLess As Featureless_Basics
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        featLess = New Featureless_Basics
        label1 = "Output of Edges_BinarizedSobel"
        label2 = "dst1 with featureless areas removed."
        task.desc = "Removing the featureless regions after a binarized sobel"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        task.mouseClickFlag = False ' edges calls a mat_4clicks algorithm.
        edges.src = src
        edges.Run()
        dst1 = edges.dst2

        featLess.src = src
        featLess.Run()
        dst2 = dst1.Clone
        dst2.SetTo(0, featLess.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
    End Sub
End Class









Public Class Edges_Consistent
    Inherits VBparent
    Dim edges As Edges_FeaturesOnly
    Public Sub New()
        initParent()
        edges = New Edges_FeaturesOnly

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Edges present n frames", 1, 30, 10)
        End If

        task.desc = "Edges that are consistent for x number of frames"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Static nFrameSlider = findSlider("Edges present n frames")
        Dim nFrames = nFrameSlider.value

        Static saveFrameCount = nFrames
        Static saveFrames As New List(Of cv.Mat)
        If saveFrameCount <> nFrames Then
            saveFrames = New List(Of cv.Mat)
            saveFrameCount = nFrames
        End If

        edges.src = src
        edges.Run()

        saveFrames.Add(edges.dst2.Clone)
        If saveFrames.Count > nFrames Then saveFrames.RemoveAt(0)

        dst1 = saveFrames(0)
        For i = 1 To saveFrames.Count - 1
            cv.Cv2.BitwiseAnd(saveFrames(i), dst1, dst1)
        Next
    End Sub
End Class








Public Class Edges_Stdev
    Inherits VBparent
    Dim stdev As Math_Stdev
    Dim edges As Edges_BinarizedSobel
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        stdev = New Math_Stdev
        Dim kernelSlider = findSlider("Sobel kernel Size")
        kernelSlider.Value = 3

        label1 = "Edges in High Stdev areas"
        label2 = "Mask of low stdev areas"
        task.desc = "Edges where stdev is above a threshold"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        stdev.src = src
        stdev.Run()

        edges.src = src
        edges.Run()
        dst1 = edges.dst2

        dst1.SetTo(0, stdev.lowStdevMask)
        dst2 = stdev.lowStdevMask
    End Sub
End Class








Public Class Edges_BlackSquare
    Inherits VBparent
    Dim std As Math_Stdev
    Dim edges As Edges_BinarizedSobel
    Dim addW As AddWeighted_Basics
    Public Sub New()
        initParent()
        addW = New AddWeighted_Basics
        edges = New Edges_BinarizedSobel
        std = New Math_Stdev
        task.desc = "Visualize the impact of Sobel on a black square"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        std.src = src
        std.Run()

        edges.src = std.dst2
        edges.Run()
        dst1 = edges.dst2

        addW.src = dst1
        addW.src2 = std.dst2
        addW.Run()
        dst2 = addW.dst1

        'Dim mask = std.dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        'dst2 = dst1.Clone.SetTo(0, mask)
    End Sub
End Class






Public Class Edges_Combo
    Inherits VBparent
    Dim edges1 As Edges_BinarizedCanny
    Dim edges2 As Edges_BinarizedSobel
    Public Sub New()
        initParent()
        edges1 = New Edges_BinarizedCanny
        edges2 = New Edges_BinarizedSobel

        label1 = "Sobel = red, Canny = yellow - they are identical"
        task.desc = "Combine the results of binarized canny and sobel"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        edges1.src = src
        edges1.Run()

        edges2.src = src
        edges2.Run()

        dst1 = task.color
        dst1.SetTo(cv.Scalar.Red, edges2.dst2)
        dst1.SetTo(cv.Scalar.Yellow, edges1.dst2)
    End Sub
End Class







Public Class Edges_SobelLR
    Inherits VBparent
    Dim red As LeftRightView_Basics
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        red = New LeftRightView_Basics()
        sobel = New Edges_Sobel()
        sobel.sliders.trackbar(0).Value = 5

        task.desc = "Find the edges in the LeftViewimages."
        label1 = "Edges in Left Image"
        label2 = "Edges in Right Image (except on Kinect)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        red.Run()
        Dim leftView = red.dst1
        sobel.src = red.dst2
        sobel.Run()
        dst2 = sobel.dst1.Clone()

        sobel.src = leftView
        sobel.Run()
        dst1 = sobel.dst1
    End Sub
End Class






'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Sobel
    Inherits VBparent
    Public grayX As cv.Mat
    Public grayY As cv.Mat
    Public horizontalOnly As Boolean
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()
        If standalone Then addw = New AddWeighted_Basics
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sobel kernel Size", 1, 32, 3)
            sliders.setupTrackBar(1, "Threshold to zero pixels below this value", 0, 255, 100)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Threshold Sobel Results"
            check.Box(1).Text = "Use Sobel Results"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        task.desc = "Show Sobel edge detection with varying kernel sizes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static ksizeSlider = findSlider("Sobel kernel Size")
        Dim kernelSize = If(ksizeSlider.Value Mod 2, ksizeSlider.Value, ksizeSlider.Value - 1)
        dst1 = New cv.Mat(src.Rows, src.Cols, src.Type)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        grayX = src.Sobel(cv.MatType.CV_32F, 1, 0, kernelSize)
        If horizontalOnly = False Then
            grayY = src.Sobel(cv.MatType.CV_32F, 0, 1, kernelSize)
            If standalone Then
                addw.src = grayX
                addw.src2 = grayY
                addw.Run()
                dst1 = addw.dst1.ConvertScaleAbs()
            Else
                dst1 = (grayY + grayX).ToMat.ConvertScaleAbs()
            End If
        Else
            dst1 = grayX.ConvertScaleAbs()
        End If
        Static thresholdCheck = findCheckBox("Threshold Sobel Results")
        If thresholdCheck.checked Then
            Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
            dst1 = dst1.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Tozero).Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
End Class







'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_SobelHorizontal
    Inherits VBparent
    Dim edges As Edges_Sobel
    Public Sub New()
        initParent()
        edges = New Edges_Sobel
        edges.horizontalOnly = True
        task.desc = "Find edges with Sobel only in the horizontal direction"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        edges.src = src
        edges.Run()

        Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
        dst1 = edges.dst1.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class Edges_SobelLRBinarized
    Inherits VBparent
    Dim red As LeftRightView_Basics
    Dim edges As Edges_BinarizedSobel
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()
        If standalone Then
            addw = New AddWeighted_Basics
            Dim weightSlider = findSlider("Weight")
            weightSlider.Value = 75
        End If
        edges = New Edges_BinarizedSobel
        red = New LeftRightView_Basics
        Dim brightSlider = findSlider("Infrared Brightness")
        brightSlider.Value = 1

        label1 = "Horizontal Sobel - Left View"
        label2 = "Horizontal Sobel - Right View"
        task.desc = "Isolate edges in the left and right views."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If task.mouseClickFlag Then task.mouseClickFlag = False ' preempt use of quadrants.
        red.Run()

        edges.src = red.dst2
        edges.Run()
        If standalone Then
            addw.src = red.dst2
            addw.src2 = edges.dst2
            addw.Run()
            dst2 = addw.dst1
        Else
            dst2 = edges.dst2
        End If

        edges.src = red.dst1
        edges.Run()
        If standalone Then
            addw.src = red.dst1
            addw.src2 = edges.dst2
            addw.Run()
            dst1 = addw.dst1
        Else
            dst1 = edges.dst2
        End If
    End Sub
End Class








Public Class Edges_BinarizedSobel
    Inherits VBparent
    Dim edges As Edges_Sobel
    Dim binarize As Binarize_Recurse
    Public mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click

        binarize = New Binarize_Recurse

        edges = New Edges_Sobel

        Dim kernelSlider = findSlider("Sobel kernel Size")
        kernelSlider.Value = 5

        label1 = "Edges between halves, lightest, darkest, and the combo"
        label2 = "Click any quadrant in dst1 to enlarge it in dst2"
        task.desc = "Collect Sobel edges from binarized images"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()

        binarize.src = src
        binarize.Run()

        edges.src = binarize.mats.mat(0) ' the light and dark halves
        edges.Run()
        mats.mat(0) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.src = binarize.mats.mat(1) ' the lightest of the light half
        edges.Run()
        mats.mat(1) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.src = binarize.mats.mat(3) ' the darkest of the dark half
        edges.Run()
        mats.mat(2) = edges.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        mats.Run()
        dst1 = mats.dst1
        If mats.dst2.Channels = 3 Then
            label2 = "Bitwise or of images 1-3 at left.  Click dst1."
            dst2 = mats.mat(3).Threshold(0, 255, cv.ThresholdTypes.Binary)
        Else
            dst2 = mats.mat(quadrantIndex)
        End If
    End Sub
End Class










Public Class Edges_Matching
    Inherits VBparent
    Dim match As MatchTemplate_Basics
    Dim red As LeftRightView_Basics
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()

        match = New MatchTemplate_Basics
        grid = New Thread_Grid
        red = New LeftRightView_Basics
        Dim brightSlider = findSlider("Infrared Brightness")
        brightSlider.Value = 1

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Search depth in pixels", 1, 256, 256)
            sliders.setupTrackBar(1, "Correlation threshold for display X100", 1, 100, 80)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Overlay thread grid"
            check.Box(1).Text = "Highlight all grid entries above threshold"
            check.Box(2).Text = "Clear selected highlights (if Highlight all grid entries is unchecked)"
            check.Box(1).Checked = True
        End If

        task.desc = "Match edges in the left and right views to determine distance"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        grid.Run()

        red.Run()
        dst1 = red.dst1
        dst2 = red.dst2

        Static redRects As New List(Of Integer)
        Static thresholdSlider = findSlider("Correlation threshold for display X100")
        Static searchSlider = findSlider("Search depth in pixels")
        Dim threshold = thresholdSlider.value / 100
        Dim searchDepth = searchSlider.value

        Dim matchOption = match.checkRadio()
        Dim fsize = task.fontSize / 3
        Dim maxLocs(grid.roiList.Count - 1) As Integer
        Dim highlights As New List(Of Integer)
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim width = If(roi.X + roi.Width + searchDepth < dst1.Width, roi.Width + searchDepth, dst1.Width - roi.X - 1)
            Dim searchROI = New cv.Rect(roi.X, roi.Y, width, roi.Height)
            match.searchArea = dst2(roi)
            match.template = dst1(searchROI)
            match.Run()
            Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
            match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
            maxLocs(i) = maxLoc.X
            If maxVal > threshold Or redRects.Contains(i) Then
                highlights.Add(i)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                cv.Cv2.PutText(dst2, Format(maxVal, "#0.00"), pt, task.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            End If
        Next

        Static overlayCheck = findCheckBox("Overlay thread grid")
        If overlayCheck.checked Then
            dst1.SetTo(255, grid.gridMask)
            dst2.SetTo(255, grid.gridMask)
        End If

        Static highlightCheck = findCheckBox("Highlight all grid entries above threshold")
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If highlightCheck.checked Then
            label1 = "Matched grid segments in dst2 with disparity"
            For Each i In highlights
                Dim roi = grid.roiList(i)
                dst2.Rectangle(roi, cv.Scalar.Red, 2)
                roi.X += maxLocs(i)
                dst1.Rectangle(roi, cv.Scalar.Red, 2)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                dst1.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                cv.Cv2.PutText(dst1, CStr(maxLocs(i)), pt, task.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        Else
            label1 = "Click in dst2 to highlight segment in dst1"
            Static clearCheck = findCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
            If clearCheck.checked Then
                redRects.Clear()
                grid.mouseClickROI = 0
                clearCheck.checked = False
            End If
            If grid.mouseClickROI Then
                If redRects.Contains(grid.mouseClickROI) = False Then redRects.Add(grid.mouseClickROI)
                For Each i In redRects
                    Dim roi = grid.roiList(i)
                    dst2.Rectangle(roi, cv.Scalar.Red, 2)
                    roi.X += maxLocs(i)
                    dst1.Rectangle(roi, cv.Scalar.Red, 2)
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    dst1.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                    cv.Cv2.PutText(dst1, CStr(maxLocs(i)), pt, task.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                Next
            End If
        End If
        label2 = "Grid segments > " + Format(threshold, "#0%") + " correlation coefficient"
    End Sub
End Class









Public Class Edges_MotionOverlay
    Inherits VBparent
    Dim diff As Diff_Basics
    Public Sub New()
        initParent()

        diff = New Diff_Basics
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Displacement in the X direction (in pixels)", 0, 100, 7)
            sliders.setupTrackBar(1, "Displacement in the Y direction (in pixels)", 0, 100, 11)
        End If

        label2 = "AbsDiff output of offset with original"
        task.desc = "Find edges by displacing the current RGB image in any direction and diff it with the original."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Static xSlider = findSlider("Displacement in the X direction (in pixels)")
        Static ySlider = findSlider("Displacement in the Y direction (in pixels)")
        Dim xDisp = xSlider.value
        Dim yDisp = ySlider.value

        Dim input As cv.Mat = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        diff.lastFrame = input.Clone
        Dim rect1 = New cv.Rect(xDisp, yDisp, dst1.Width - xDisp - 1, dst1.Height - yDisp - 1)
        Dim rect2 = New cv.Rect(0, 0, dst1.Width - xDisp - 1, dst1.Height - yDisp - 1)
        diff.lastFrame(rect2) = input(rect1).Clone

        diff.src = input
        diff.Run()
        dst1 = diff.dst1
        dst2 = diff.dst2
        dst2.SetTo(0, task.noDepthMask)
        label1 = "Src offset (x,y) = (" + CStr(xDisp) + "," + CStr(yDisp) + ")"
    End Sub
End Class







' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edges_RGB
    Inherits VBparent
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel
        Dim thresholdCheck = findCheckBox("Threshold Sobel Results")
        thresholdCheck.Checked = False
        task.desc = "Combine the edges from all 3 channels.  Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Dim img32f As New cv.Mat
        src.ConvertTo(img32f, cv.MatType.CV_32FC3)
        Dim split = img32f.Split()
        For i = 0 To 3 - 1
            split(i) = split(i).Normalize(0, 255, cv.NormTypes.MinMax)
        Next
        cv.Cv2.Merge(split, img32f)
        img32f.ConvertTo(dst1, cv.MatType.CV_8UC3)
        For i = 0 To 3 - 1
            sobel.src = split(i)
            sobel.Run()
            split(i) = 255 - sobel.dst1
        Next
        cv.Cv2.Merge(split, dst1)
    End Sub
End Class







' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edges_HSV
    Inherits VBparent
    Dim edges As Edges_RGB
    Public Sub New()
        initParent()
        edges = New Edges_RGB
        Dim thresholdSlider = findSlider("Threshold to zero pixels below this value")
        thresholdSlider.Value = 25
        task.desc = "Combine the edges from all 3 HSV channels.  Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        edges.src = hsv
        edges.Run()
        dst1 = edges.dst1
    End Sub
End Class