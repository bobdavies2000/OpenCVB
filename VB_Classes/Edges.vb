Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
'https://docs.opencv.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edges_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Canny threshold1", 1, 255, 50)
            sliders.setupTrackBar(1, "Canny threshold2", 1, 255, 50)
            sliders.setupTrackBar(2, "Canny Aperture", 3, 7, 3)
        End If

        task.desc = "Show canny edge detection with varying thresholds"
        labels(2) = "Canny using L1 Norm"
        labels(3) = "Canny using L2 Norm"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static t1Slider = findSlider("Canny threshold1")
        Static t2Slider = findSlider("Canny threshold2")
        Static apertureSlider = findSlider("Canny Aperture")
        Dim threshold1 As Integer = t1Slider.value
        Dim threshold2 As Integer = t2Slider.Value
        Dim apTmp = apertureSlider.value
        Dim aperture = If(apTmp Mod 2, apTmp, apTmp + 1)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = src.Canny(threshold1, threshold2, aperture, False)
        dst3 = src.Canny(threshold1, threshold2, aperture, True)
    End Sub
End Class






Public Class Edges_DepthAndColor : Inherits VBparent
    Dim shadow As New Depth_Holes
    Dim canny As New Edges_Basics
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        dilate.radio.check(2).Checked = True

        canny.sliders.trackbar(0).Value = 100
        canny.sliders.trackbar(1).Value = 100

        task.desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow."
        labels(2) = "Edges in color and depth after dilate"
        labels(3) = "Edges in color and depth no dilate"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        canny.RunClass(src)
        shadow.RunClass(src)

        dst3 = If(shadow.dst3.Channels <> 1, shadow.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY), shadow.dst3)
        dst3 += canny.dst2.Threshold(1, 255, cv.ThresholdTypes.Binary)

        dilate.RunClass(dst3)
        dilate.dst2.SetTo(0, shadow.dst2)
        dst2 = dilate.dst2
    End Sub
End Class







'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Edges_Laplacian : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Gaussian Kernel", 1, 32, 7)
            sliders.setupTrackBar(1, "Laplacian Kernel", 1, 32, 5)
        End If
        labels(3) = "Laplacian of Depth Image"
        task.desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gaussiankernelSize = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value - 1)
        Dim laplaciankernelSize = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value - 1)

        dst2 = src.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst2 = dst2.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst2 = dst2.ConvertScaleAbs()

        dst3 = task.RGBDepth.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst3 = dst3.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst3 = dst3.ConvertScaleAbs()
    End Sub
End Class



'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Scharr : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Scharr multiplier X100", 1, 500, 50)
        End If
        labels(3) = "x field + y field in CV_32F format"
        task.desc = "Scharr is most accurate with 3x3 kernel."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim xField = gray.Scharr(cv.MatType.CV_32FC1, 1, 0)
        Dim yField = gray.Scharr(cv.MatType.CV_32FC1, 0, 1)
        cv.Cv2.Add(xField, yField, dst3)
        dst3.ConvertTo(dst2, cv.MatType.CV_8U, sliders.trackbar(0).Value / 100)
    End Sub
End Class






' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Edges_Preserving : Inherits VBparent
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Edge RecurseFilter"
            radio.check(1).Text = "Edge NormconvFilter"
            radio.check(0).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Edge Sigma_s", 0, 200, 10)
            sliders.setupTrackBar(1, "Edge Sigma_r", 1, 100, 40)
        End If
        labels(3) = "Edge preserving blur for RGB depth image above"
        task.desc = "OpenCV's edge preserving filter."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(src, dst2, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(src, dst2, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst3, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst3, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
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
Public Class Edges_RandomForest_CPP : Inherits VBparent
    Dim rgbData() As Byte
    Dim EdgesPtr As IntPtr
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Edges RF Threshold", 1, 255, 35)
        End If

        task.desc = "Detect edges using structured forests - Opencv Contrib"
        ReDim rgbData(dst2.Total * dst2.ElemSize - 1)
        labels(3) = "Thresholded Edge Mask (use slider to adjust)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount < 100 Then setTrueText("On the first call only, it takes a few seconds to load the randomForest model.", 10, 100)

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

            dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, gray8u).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
    Public Sub Close()
        Edges_RandomForest_Close(EdgesPtr)
    End Sub
End Class






Public Class Edges_ResizeAdd : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Border Vertical in Pixels", 1, 20, 5)
            sliders.setupTrackBar(1, "Border Horizontal in Pixels", 1, 20, 5)
            sliders.setupTrackBar(2, "Threshold for Pixel Difference", 1, 50, 16)
        End If
        task.desc = "Find edges using a resize, subtract, and threshold."
        labels(2) = "Edges found with just resizing"
        labels(3) = "Found edges added to grayscale image source."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim newFrame = gray(New cv.Range(sliders.trackbar(0).Value, gray.Rows - sliders.trackbar(0).Value),
                            New cv.Range(sliders.trackbar(1).Value, gray.Cols - sliders.trackbar(1).Value))
        newFrame = newFrame.Resize(gray.Size())
        cv.Cv2.Absdiff(gray, newFrame, dst2)
        dst2 = dst2.Threshold(sliders.trackbar(2).Value, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.Add(gray, dst2, dst3)
    End Sub
End Class







Public Class Edges_DCTfrequency : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Remove Frequencies < x", 0, 100, 32)
            sliders.setupTrackBar(1, "Threshold after Removal", 1, 255, 20)
        End If

        labels(3) = "Mask for the isolated frequencies"
        task.desc = "Find edges by removing all the highest frequencies."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.trackbar(0).Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        labels(2) = "Highest " + CStr(sliders.trackbar(0).Value) + " frequencies removed from RGBDepth"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(dst2, cv.MatType.CV_8UC1, 255)
        dst3 = dst2.Threshold(sliders.trackbar(1).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







' https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
Public Class Edges_Deriche_CPP : Inherits VBparent
    Dim Edges_Deriche As IntPtr
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Deriche Alpha", 1, 400, 100)
            sliders.setupTrackBar(1, "Deriche Omega", 1, 1000, 100)
        End If
        Edges_Deriche = Edges_Deriche_Open()
        labels(3) = "Image enhanced with Deriche results"
        task.desc = "Edge detection using the Deriche X and Y gradients - Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        End If
        cv.Cv2.BitwiseOr(src, dst2, dst3)
    End Sub
    Public Sub Close()
        Edges_Deriche_Close(Edges_Deriche)
    End Sub
End Class









Public Class Edges_DCTinput : Inherits VBparent
    Dim edges As New Edges_Basics
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(2) = "Canny edges produced from original grayscale image"
        labels(3) = "Edges produced with featureless regions cleared"
        task.desc = "Use the featureless regions to enhance the edge detection"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        edges.RunClass(src)
        dst2 = edges.dst2.Clone

        dct.RunClass(src)
        Dim tmp = src.SetTo(cv.Scalar.White, dct.dst2)
        edges.RunClass(tmp)
        dst3 = edges.dst2
    End Sub
End Class








Public Class Edges_BinarizedCanny : Inherits VBparent
    Dim edges As New Edges_Basics
    Dim binarize As Binarize_Recurse
    Dim mats As New Mat_4Click
    Public Sub New()
        binarize = New Binarize_Recurse
        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        task.desc = "Collect edges from binarized images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        binarize.RunClass(src)

        edges.RunClass(binarize.mats.mat(0))  ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.RunClass(binarize.mats.mat(1))  ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.RunClass(binarize.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        mats.RunClass(src)
        dst2 = mats.dst2
        If mats.dst3.Channels = 3 Then
            labels(3) = "Combo of first 3 below.  Click quadrants in dst2."
            dst3 = mats.mat(3)
        Else
            dst3 = mats.dst3
        End If
    End Sub
End Class








Public Class Edges_BinarizedBrightness : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim bright As New PhotoShop_Brightness
    Public Sub New()
        task.desc = "Visualize the impact of brightness on Edges_BinarizeSobel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        bright.RunClass(src)
        dst2 = bright.dst3
        edges.RunClass(bright.dst3)
        dst3 = edges.dst3
    End Sub
End Class







Public Class Edges_BinarizedReduction : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim reduction As New Reduction_Basics
    Public Sub New()
        task.desc = "Visualize the impact of reduction on Edges_BinarizeSobel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)
        dst2 = reduction.dst2
        edges.RunClass(dst2)
        dst3 = edges.dst3
    End Sub
End Class








Public Class Edges_Depth : Inherits VBparent
    Dim dMax As New Depth_SmoothMax
    Dim sobel As New Edges_Sobel
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 14
        task.desc = "Use Depth_SmoothMax to find edges in Depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        dMax.RunClass(task.depth32f)
        dst2 = dMax.dst2

        sobel.RunClass(dMax.dst3)
        dst3 = sobel.dst2
    End Sub
End Class









Public Class Edges_FeaturesOnly : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim featLess As New Featureless_Basics
    Public Sub New()
        labels(2) = "Output of Edges_BinarizedSobel"
        labels(3) = "dst2 with featureless areas removed."
        task.desc = "Removing the featureless regions after a binarized sobel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        task.mouseClickFlag = False ' edges calls a mat_4clicks algorithm.
        edges.RunClass(src)
        dst2 = edges.dst3

        featLess.RunClass(src)
        dst3 = dst2.Clone
        dst3.SetTo(0, featLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
    End Sub
End Class









Public Class Edges_Consistent : Inherits VBparent
    Dim edges As New Edges_FeaturesOnly
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Edges present n frames", 1, 30, 10)
        End If

        task.desc = "Edges that are consistent for x number of frames"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static nFrameSlider = findSlider("Edges present n frames")
        Dim nFrames = nFrameSlider.value

        Static saveFrameCount = nFrames
        Static saveFrames As New List(Of cv.Mat)
        If saveFrameCount <> nFrames Then
            saveFrames = New List(Of cv.Mat)
            saveFrameCount = nFrames
        End If

        edges.RunClass(src)

        saveFrames.Add(edges.dst3.Clone)
        If saveFrames.Count > nFrames Then saveFrames.RemoveAt(0)

        dst2 = saveFrames(0)
        For i = 1 To saveFrames.Count - 1
            cv.Cv2.BitwiseAnd(saveFrames(i), dst2, dst2)
        Next
    End Sub
End Class








Public Class Edges_Stdev : Inherits VBparent
    Dim stdev As New Math_Stdev
    Dim edges As New Edges_BinarizedSobel
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 14

        labels(2) = "Edges in High Stdev areas"
        labels(3) = "Mask of low stdev areas"
        task.desc = "Edges where stdev is above a threshold"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        stdev.RunClass(src)
        edges.RunClass(src)
        dst2 = edges.dst3
        dst2.SetTo(0, stdev.lowStdevMask)
        dst3 = stdev.lowStdevMask
    End Sub
End Class








Public Class Edges_BlackSquare : Inherits VBparent
    Dim std As New Math_Stdev
    Dim edges As New Edges_BinarizedSobel
    Dim addW As New AddWeighted_Basics
    Public Sub New()
        task.desc = "Visualize the impact of Sobel on a black square"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        std.RunClass(src)

        edges.RunClass(std.dst3)
        dst2 = edges.dst3

        addW.src2 = std.dst3
        addW.RunClass(dst2)
        dst3 = addW.dst2

        'Dim mask = std.dst3.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        'dst3 = dst2.Clone.SetTo(0, mask)
    End Sub
End Class






Public Class Edges_Combo : Inherits VBparent
    Dim edges1 As New Edges_BinarizedCanny
    Dim edges2 As New Edges_BinarizedSobel
    Public Sub New()
        labels(2) = "Sobel = red, Canny = yellow - they are identical"
        task.desc = "Combine the results of binarized canny and sobel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        edges1.RunClass(src)
        edges2.RunClass(src)

        dst2 = task.color
        dst2.SetTo(cv.Scalar.Red, edges2.dst3)
        dst2.SetTo(cv.Scalar.Yellow, edges1.dst3)
    End Sub
End Class








'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_SobelHorizontal : Inherits VBparent
    Dim edges As New Edges_Sobel
    Public Sub New()
        edges.horizontalOnly = True
        task.desc = "Find edges with Sobel only in the horizontal direction"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
        edges.RunClass(src)

        dst2 = edges.dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class Edges_SobelLRBinarized : Inherits VBparent
    Dim red As New LeftRight_Basics
    Dim edges As New Edges_BinarizedSobel
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels(2) = "Horizontal Sobel - Left View"
        labels(3) = "Horizontal Sobel - Right View"
        task.desc = "Isolate edges in the left and right views."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mouseClickFlag Then task.mouseClickFlag = False ' preempt use of quadrants.
        red.RunClass(src)

        edges.RunClass(red.dst3)
        If standalone Then
            addw.src2 = edges.dst3
            addw.RunClass(red.dst3)
            dst3 = addw.dst2
        Else
            dst3 = edges.dst3
        End If

        edges.RunClass(red.dst2)
        If standalone Then
            addw.src2 = edges.dst3
            addw.RunClass(red.dst2)
            dst2 = addw.dst2
        Else
            dst2 = edges.dst3
        End If
    End Sub
End Class








Public Class Edges_BinarizedSobel : Inherits VBparent
    Dim edges As New Edges_Sobel
    Dim binarize As Binarize_Recurse
    Public mats As New Mat_4Click
    Public Sub New()
        binarize = New Binarize_Recurse
        findSlider("Sobel kernel Size").Value = 5

        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        labels(3) = "Click any quadrant in dst2 to enlarge it in dst3"
        task.desc = "Collect Sobel edges from binarized images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mouseClickFlag And task.mousePicTag = RESULT_DST2 Then setMyActiveMat()

        binarize.RunClass(src)

        edges.RunClass(binarize.mats.mat(0)) ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.RunClass(binarize.mats.mat(1)) ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.RunClass(binarize.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        If standalone Or task.intermediateActive Then mats.RunClass(src)
        dst2 = mats.dst2
        If mats.dst3.Channels = 3 Then
            labels(3) = "BitwiseOr of images 1-3 at left.  Click dst2."
            dst3 = mats.mat(3).Threshold(0, 255, cv.ThresholdTypes.Binary)
        Else
            dst3 = mats.mat(quadrantIndex)
        End If
    End Sub
End Class










Public Class Edges_Matching : Inherits VBparent
    Dim match As New MatchTemplate_Basics
    Dim red As New LeftRight_Basics
    Dim grid As New Thread_Grid
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Search depth in pixels", 1, 256, 256)
            sliders.setupTrackBar(1, "Correlation threshold for display X100", 1, 100, 80)
        End If

        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Overlay thread grid"
            check.Box(1).Text = "Highlight all grid entries above threshold"
            check.Box(2).Text = "Clear selected highlights (if Highlight all grid entries is unchecked)"
            check.Box(1).Checked = True
        End If

        task.desc = "Match edges in the left and right views to determine distance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static overlayCheck = findCheckBox("Overlay thread grid")
        Static highlightCheck = findCheckBox("Highlight all grid entries above threshold")
        Static clearCheck = findCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
        Static redRects As New List(Of Integer)
        Static thresholdSlider = findSlider("Correlation threshold for display X100")
        Static searchSlider = findSlider("Search depth in pixels")
        Dim threshold = thresholdSlider.value / 100
        Dim searchDepth = searchSlider.value

        grid.RunClass(Nothing)

        red.RunClass(src)
        dst2 = red.dst2
        dst3 = red.dst3

        Dim matchOption = match.checkRadio()
        Dim fsize = task.fontSize / 3
        Dim maxLocs(grid.roiList.Count - 1) As Integer
        Dim highlights As New List(Of Integer)
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim width = If(roi.X + roi.Width + searchDepth < dst2.Width, roi.Width + searchDepth, dst2.Width - roi.X - 1)
            Dim searchROI = New cv.Rect(roi.X, roi.Y, width, roi.Height)
            match.searchArea = dst3(roi)
            match.template = dst2(searchROI)
            match.RunClass(src)
            Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
            match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
            maxLocs(i) = maxLoc.X
            If maxVal > threshold Or redRects.Contains(i) Then
                highlights.Add(i)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                dst3.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                cv.Cv2.PutText(dst3, Format(maxVal, "#0.00"), pt, task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        Next

        If overlayCheck.checked Then
            dst2.SetTo(255, grid.gridMask)
            dst3.SetTo(255, grid.gridMask)
        End If

        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If highlightCheck.checked Then
            labels(2) = "Matched grid segments in dst3 with disparity"
            For Each i In highlights
                Dim roi = grid.roiList(i)
                dst3.Rectangle(roi, cv.Scalar.Red, 2)
                roi.X += maxLocs(i)
                dst2.Rectangle(roi, cv.Scalar.Red, 2)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                cv.Cv2.PutText(dst2, CStr(maxLocs(i)), pt, task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        Else
            labels(2) = "Click in dst3 to highlight segment in dst2"
            If clearCheck.checked Then
                redRects.Clear()
                grid.mouseClickROI = 0
                clearCheck.checked = False
            End If
            If grid.mouseClickROI Then
                If redRects.Contains(grid.mouseClickROI) = False Then redRects.Add(grid.mouseClickROI)
                For Each i In redRects
                    Dim roi = grid.roiList(i)
                    dst3.Rectangle(roi, cv.Scalar.Red, 2)
                    roi.X += maxLocs(i)
                    dst2.Rectangle(roi, cv.Scalar.Red, 2)
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                    cv.Cv2.PutText(dst2, CStr(maxLocs(i)), pt, task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
                Next
            End If
        End If
        labels(3) = "Grid segments > " + Format(threshold, "#0%") + " correlation coefficient"
    End Sub
End Class









Public Class Edges_MotionOverlay : Inherits VBparent
    Dim diff As New Diff_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Displacement in the X direction (in pixels)", 0, 100, 7)
            sliders.setupTrackBar(1, "Displacement in the Y direction (in pixels)", 0, 100, 11)
        End If

        labels(3) = "AbsDiff output of offset with original"
        task.desc = "Find edges by displacing the current RGB image in any direction and diff it with the original."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static xSlider = findSlider("Displacement in the X direction (in pixels)")
        Static ySlider = findSlider("Displacement in the Y direction (in pixels)")
        Dim xDisp = xSlider.value
        Dim yDisp = ySlider.value

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        diff.lastFrame = src.Clone
        Dim rect1 = New cv.Rect(xDisp, yDisp, dst2.Width - xDisp - 1, dst2.Height - yDisp - 1)
        Dim rect2 = New cv.Rect(0, 0, dst2.Width - xDisp - 1, dst2.Height - yDisp - 1)
        diff.lastFrame(rect2) = src(rect1).Clone

        diff.RunClass(src)
        dst2 = diff.dst2
        dst3 = diff.dst3
        dst3.SetTo(0, task.noDepthMask)
        labels(2) = "Src offset (x,y) = (" + CStr(xDisp) + "," + CStr(yDisp) + ")"
    End Sub
End Class







' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edges_RGB : Inherits VBparent
    Dim sobel As New Edges_Sobel
    Public Sub New()
        findCheckBox("Threshold Sobel Results").Checked = False
        task.desc = "Combine the edges from all 3 channels.  Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim img32f As New cv.Mat
        src.ConvertTo(img32f, cv.MatType.CV_32FC3)
        Dim split = img32f.Split()
        For i = 0 To 3 - 1
            split(i) = split(i).Normalize(0, 255, cv.NormTypes.MinMax)
        Next
        cv.Cv2.Merge(split, img32f)
        img32f.ConvertTo(dst2, cv.MatType.CV_8UC3)
        For i = 0 To 3 - 1
            sobel.RunClass(split(i))
            split(i) = 255 - sobel.dst2
        Next
        cv.Cv2.Merge(split, dst2)
    End Sub
End Class







' https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
Public Class Edges_HSV : Inherits VBparent
    Dim edges As New Edges_RGB
    Public Sub New()
        findSlider("Threshold to zero pixels below this value").Value = 25
        task.desc = "Combine the edges from all 3 HSV channels.  Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        edges.RunClass(hsv)
        dst2 = edges.dst2
    End Sub
End Class








Public Class Edges_SobelLR : Inherits VBparent
    Dim red As New LeftRight_Basics
    Dim sobel As New Edges_Sobel
    Public Sub New()
        sobel.sliders.trackbar(0).Value = 5

        task.desc = "Find the edges in the LeftViewimages."
        labels(2) = "Edges in Left Image"
        labels(3) = "Edges in Right Image (except on Kinect)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        red.RunClass(src)
        Dim leftView = red.dst2
        sobel.RunClass(red.dst3)
        dst3 = sobel.dst2.Clone()

        sobel.RunClass(leftView)
        dst2 = sobel.dst2
    End Sub
End Class






'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Sobel : Inherits VBparent
    Public grayX As cv.Mat
    Public grayY As cv.Mat
    Public horizontalOnly As Boolean
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Sobel kernel Size", 1, 32, 4)
            sliders.setupTrackBar(1, "Threshold to zero pixels below this value", 0, 255, 100)
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Threshold Sobel Results"
            check.Box(0).Checked = True
        End If

        task.desc = "Show Sobel edge detection with varying kernel sizes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
        Static thresholdCheck = findCheckBox("Threshold Sobel Results")
        Static ksizeSlider = findSlider("Sobel kernel Size")
        Dim kernelSize = If(ksizeSlider.Value Mod 2, ksizeSlider.Value, ksizeSlider.Value - 1)
        dst2 = New cv.Mat(src.Rows, src.Cols, src.Type)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        grayX = src.Sobel(cv.MatType.CV_32F, 1, 0, kernelSize)
        If horizontalOnly = False Then
            grayY = src.Sobel(cv.MatType.CV_32F, 0, 1, kernelSize)
            If standalone Then
                addw.src2 = grayY
                addw.RunClass(grayX)
                dst2 = addw.dst2.ConvertScaleAbs()
            Else
                dst2 = (grayY + grayX).ToMat.ConvertScaleAbs()
            End If
        Else
            dst2 = grayX.ConvertScaleAbs()
        End If
        If thresholdCheck.checked Then
            dst2 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Tozero).Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
End Class






Public Class Edges_SobelCustomV : Inherits VBparent
    Public Sub New()
        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        task.desc = "Show Sobel edge detection a custom vertical kernel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = src.Filter2D(cv.MatType.CV_32F, New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {1, 0, -1, 2, 0, -2, 1, 0, -1}))
        dst1.ConvertTo(dst2, src.Type)
        dst1 = src.Filter2D(cv.MatType.CV_32F, New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {3, 0, -3, 10, 0, -10, 3, 0, -3}))
        dst1.ConvertTo(dst3, src.Type)
    End Sub
End Class







Public Class Edges_SobelCustomH : Inherits VBparent
    Public Sub New()
        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        task.desc = "Show Sobel edge detection a custom horizontal kernel"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = src.Filter2D(cv.MatType.CV_32F, New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {1, 2, 1, 0, 0, 0, -1, -2, -1}))
        dst1.ConvertTo(dst2, src.Type)
        dst1 = src.Filter2D(cv.MatType.CV_32F, New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {3, 10, 3, 0, 0, 0, -3, -10, -3}))
        dst1.ConvertTo(dst3, src.Type)
    End Sub
End Class








Public Class Edges_SobelCustom : Inherits VBparent
    Dim addw As New AddWeighted_Basics
    Dim edgesV As New Edges_SobelCustomV
    Dim edgesH As New Edges_SobelCustomH
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Horizontal Edges"
            check.Box(1).Text = "Vertical Edges"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        labels = {"", "", "Sobel Custom 1", "Sobel Custom 2"}
        task.desc = "Show Sobel edge detection with custom horizont and vertical kernels"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static horizCheck = findCheckBox("Horizontal Edges")
        Static vertCheck = findCheckBox("Vertical Edges")
        If horizCheck.checked Then
            edgesH.RunClass(src)
            dst2 = edgesH.dst2
            dst3 = edgesH.dst3
        End If

        If vertCheck.checked Then edgesV.RunClass(src)

        If horizCheck.checked And vertCheck.checked Then
            addw.src2 = edgesV.dst2
            addw.RunClass(dst2)
            dst2 = addw.dst2

            addw.src2 = edgesV.dst3
            addw.RunClass(dst3)
            dst3 = addw.dst2
        ElseIf vertCheck.checked Then
            dst2 = edgesV.dst2.Clone
            dst3 = edgesV.dst3.Clone
        End If
    End Sub
End Class








Public Class Edges_SobelCustomLeftRight : Inherits VBparent
    Dim custom As New Edges_SobelCustom
    Public Sub New()
        usingdst0 = True
        usingdst1 = True
        labels = {"Left Image Custom 1", "Left Image Custom 2", "Right Image Custom 1", "Right Image Custom 2"}
        task.desc = "Show Sobel edge detection for both left and right images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        custom.RunClass(task.leftView)
        dst0 = custom.dst2.Clone
        dst1 = custom.dst3.Clone

        custom.RunClass(task.rightView)
        dst2 = custom.dst2
        dst3 = custom.dst3
    End Sub
End Class
