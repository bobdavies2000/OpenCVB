Imports NAudio.Wave
Imports cv = OpenCvSharp
Public Class Color_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Public classifier As Object
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        labels(3) = "vbPalette output of dst2 at left"
        vbAddAdvice(traceName + ": redOptions 'Color Source' control which color source is used.")
        desc = "Classify pixels by color using a variety of techniques"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Or classifier Is Nothing Then
            Select Case redOptions.colorInputIndex
                Case 0 ' "BackProject_Full"
                    Static backP As New BackProject_Full
                    classifier = backP
                Case 1 ' "KMeans_Basics"
                    Static km As New KMeans_Basics
                    classifier = km
                Case 2 ' "LUT_Basics"
                    Static lut As New LUT_Basics
                    classifier = lut
                Case 3 ' "Reduction_Basics"
                    Static reduction As New Reduction_Basics
                    classifier = reduction
                Case 4 ' "Hist3DColor_Basics"
                    Static hColor As New Hist3Dcolor_Basics
                    classifier = hColor
                Case 5 ' "Binarize_Split4"
                    Static binar4 As New Binarize_Split4
                    classifier = binar4
                Case 6 ' "Binarize_SplitDepth"
                    Static binar As New Binarize_SplitDepth
                    classifier = binar
                Case 7 ' "Binarize_DepthTiers"
                    Static binarTiers As New Binarize_DepthTiers
                    classifier = binarTiers
            End Select
        End If

        dst1 = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)

        classifier.run(dst1)

        If task.heartBeat Then
            dst2 = classifier.dst2.clone
        ElseIf task.motionDetected Then
            classifier.dst2(task.motionRect).copyto(dst2(task.motionRect))
        End If

        classCount = classifier.classCount

        If task.maxDepthMask.Rows > 0 Then
            classCount += 1
            dst2.SetTo(classCount, task.maxDepthMask)
        End If

        dst3 = classifier.dst3
        labels(2) = "Color_Basics: method = " + classifier.tracename + " produced " + CStr(classCount) + " pixel classifications"
    End Sub
End Class






Public Class Color_Grayscale : Inherits VB_Algorithm
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use OpenCV to create grayscale image")
            check.Box(0).Checked = True
        End If
        labels = {"", "", "Color_Grayscale", ""}
        desc = "Manually create a grayscale image.  The only reason for this example is to show how slow it can be to do the work manually in VB.Net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static grayCheck = findCheckBox("Use OpenCV to create grayscale image")
        If grayCheck.Checked Then
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            Parallel.For(0, src.Rows,
                Sub(y)
                    For x = 0 To src.Cols - 1
                        Dim cc = src.Get(Of cv.Vec3b)(y, x)
                        dst2.Set(Of Byte)(y, x, CByte((cc(0) * 1140 + cc(1) * 5870 + cc(2) * 2989) / 10000))
                    Next
                End Sub)
        End If
    End Sub
End Class






Public Class Color_Depth : Inherits VB_Algorithm
    Public reduction As New Reduction_Basics
    Public depth As New Depth_InRange
    Public classCount As Integer
    Public Sub New()
        gOptions.LineType.SelectedIndex = 1 ' linetype = link4
        labels = {"", "", "Color Reduction Edges", "Depth Range Edges"}
        desc = "Add depth regions edges to the color Reduction image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2
        classCount = reduction.classCount

        depth.Run(src)
        dst2.SetTo(0, depth.dst3)
        dst3.SetTo(0)
        dst3.SetTo(cv.Scalar.White, depth.dst3)
    End Sub
End Class








Public Class Color_KMeans : Inherits VB_Algorithm
    Public km0 As New KMeans_Basics
    Public km1 As New KMeans_Basics
    Public km2 As New KMeans_Basics
    Dim options As New Options_ColorFormat
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels(0) = "Recombined channels in other images."
        desc = "Run KMeans on each of the 3 color channels"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst0 = options.dst2

        Dim split = dst0.Split()

        km0.Run(split(0))
        dst1 = km0.dst2 * 255 / km0.classCount

        km1.Run(split(1))
        dst2 = km1.dst2 * 255 / km0.classCount

        km2.Run(split(2))
        dst3 = km2.dst2 * 255 / km0.classCount

        For i = 1 To 3
            labels(i) = options.colorFormat + " channel " + CStr(i - 1)
        Next
    End Sub
End Class









Public Class Color_RedHue : Inherits VB_Algorithm
    Dim options As New Options_CamShift
    Public Sub New()
        vbAddAdvice(traceName + ": This mask of red hue areas is available for use.")
        labels = {"", "", "Pixels with Red Hue", ""}
        desc = "Find all the reddish pixels in the image - indicate some life form."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mask = hsv.InRange(options.camSBins, New cv.Scalar(180, 255, options.camMax))
        dst2.SetTo(0)
        src.CopyTo(dst2, mask)
    End Sub
End Class








' https://stackoverflow.com/questions/40233986/python-is-there-a-function-or-formula-to-find-the-complementary-colour-of-a-rgb
Public Class Color_Complementary : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Current image in complementary colors", "HSV version of the current image but hue is flipped to complementary value."}
        desc = "Display the current image in complementary colors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim split = hsv.Split()
        split(0) += 90 Mod 180
        cv.Cv2.Merge(split, dst3)
        dst2 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
    End Sub
End Class








' https://stackoverflow.com/questions/40233986/python-is-there-a-function-or-formula-to-find-the-complementary-colour-of-a-rgb
Public Class Color_ComplementaryTest : Inherits VB_Algorithm
    Dim images As New Image_Basics
    Dim comp As New Color_Complementary
    Public Sub New()
        labels = {"", "", "Original Image", "Color_Complementary version looks identical to the correct version at the link above "}
        desc = "Create the complementary images for Gilles Tran's 'Glasses' image for comparison"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        images.fileNameForm.filename.Text = task.homeDir + "Data/Glasses by Gilles Tran.png"
        images.Run(empty)
        dst2 = images.dst2

        comp.Run(dst2)
        dst3 = comp.dst2
    End Sub
End Class







' https://github.com/BhanuPrakashNani/Image_Processing/tree/master/Est.%20Transformation
Public Class Color_InRange : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Original", "After InRange processing"}
        desc = "Use inRange to isolate colors from the background"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = cv.Cv2.ImRead(task.homeDir + "Data/1.jpg", cv.ImreadModes.Grayscale)
        dst1 = dst2.InRange(105, 165) ' should make this a slider and experiment further...
        dst3 = dst2.Clone
        dst3.SetTo(0, dst1)
    End Sub
End Class








Public Class Color_TopX_VB : Inherits VB_Algorithm
    Dim topX As New Hist3Dcolor_TopXColors
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Top X pixels", 2, 32, 16)
        desc = "Classify every BGR pixel into some common colors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src
        input = input.Resize(task.lowRes, cv.InterpolationFlags.Nearest)

        Static topXSlider = findSlider("Top X pixels")
        topX.mapTopX = topXSlider.value
        topX.Run(input)

        Dim top As New List(Of cv.Vec3b)
        For Each pt In topX.topXPixels
            top.Add(New cv.Vec3b(pt.X, pt.Y, pt.Z))
        Next

        dst2 = input.Clone
        For y = 0 To input.Rows - 1
            For x = 0 To input.Cols - 1
                Dim distances As New List(Of Single)
                For Each pt In top
                    Dim vec = input.Get(Of cv.Vec3b)(y, x)
                    distances.Add(distance3D(pt, New cv.Vec3b(vec.Item0, vec.Item1, vec.Item2)))
                Next
                Dim best = top(distances.IndexOf(distances.Min))
                dst2.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(best.Item0, best.Item1, best.Item2))
            Next
        Next
        labels(2) = "The BGR image mapped to " + CStr(topX.mapTopX) + " colors"
    End Sub
End Class







' https://github.com/AjinkyaChavan9/RGB-Color-Classifier-with-Deep-Learning-using-Keras-and-Tensorflow
Public Class Color_Common_VB : Inherits VB_Algorithm
    Dim common As New List(Of cv.Vec3b)
    Dim commonScalar As List(Of cv.Scalar) = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.Yellow, cv.Scalar.Pink, cv.Scalar.Purple, cv.Scalar.Brown,
                                              cv.Scalar.Gray, cv.Scalar.Black, cv.Scalar.White}.ToList
    Public Sub New()
        For Each c In commonScalar
            common.Add(New cv.Vec3b(c(0), c(1), c(2)))
        Next
        desc = "Classify every BGR pixel into some common colors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim distances As New List(Of Single)
                For Each pt In common
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
                    distances.Add(distance3D(pt, New cv.Vec3b(vec.Item0, vec.Item1, vec.Item2)))
                Next
                Dim best = common(distances.IndexOf(distances.Min))
                dst2.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(best.Item0, best.Item1, best.Item2))
            Next
        Next
        labels(2) = "The BGR image mapped to " + CStr(common.Count) + " common colors"
    End Sub
End Class







Public Class Color_Smoothing : Inherits VB_Algorithm
    Dim frames As New History_Basics
    Public Sub New()
        labels = {"", "", "Averaged BGR image over the last X frames", ""}
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32FC3, 0)
        desc = "Merge that last X BGR frames to smooth out differences."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        frames.Run(src)
        dst2 = frames.dst2
        labels(2) = "The image below is the average of " + CStr(frames.saveFrames.Count) + " the last BGR frames"
    End Sub
End Class







Public Class Color_Denoise : Inherits VB_Algorithm
    Dim denoise As New Denoise_Pixels
    Public Sub New()
        denoise.standalone = True
        desc = "Remove single pixels between identical pixels for all color classifiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        denoise.Run(src)
        dst2 = denoise.dst2
        dst3 = denoise.dst3
        setTrueText(denoise.strOut, 2)
    End Sub
End Class






Public Class Color_MotionFiltered : Inherits VB_Algorithm
    Dim colorClass As New Color_Basics
    Public classCount As Integer
    Public Sub New()
        desc = "Prepare a Color_Basics image using the task.motionRect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst3 = task.motionColor.dst2
        colorClass.Run(task.motionColor.dst2)
        dst2 = colorClass.dst3
        classCount = colorClass.classCount
    End Sub
End Class







Public Class Color_Hue : Inherits VB_Algorithm
    Public Sub New()
        desc = "Isolate those regions in the image that have a reddish hue."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim loBins As cv.Scalar = New cv.Scalar(0, 40, 32)
        Dim hiBins As cv.Scalar = New cv.Scalar(180, 255, 255)
        dst2 = hsv.InRange(loBins, hiBins)
    End Sub
End Class







Public Class Color_BlackAndWhite : Inherits VB_Algorithm
    Dim options As New Options_StdevGrid
    Public Sub New()
        labels = {"", "", "Mask to identify all 'black' regions", "Mask identifies all 'white' regions"}
        desc = "Create masks for black and white"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst1.Threshold(options.minThreshold, 255, cv.ThresholdTypes.BinaryInv)
        dst3 = dst1.Threshold(options.maxThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class
