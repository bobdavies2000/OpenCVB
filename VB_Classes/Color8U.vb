Imports cv = OpenCvSharp
Public Class Color8U_Basics : Inherits TaskParent
    Public classCount As Integer
    Public classifier As Object
    Dim colorMethods(10) As Object
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        labels(3) = "ShowPalette output of dst2 at left"
        desc = "Classify pixels by color using a variety of techniques"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim index = task.redOptions.colorInputIndex
        If task.optionsChanged Or classifier Is Nothing Then
            Select Case index
                Case 0
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New BackProject_Full
                Case 1
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New Bin4Way_Regions
                Case 2
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New Binarize_DepthTiers
                Case 3
                    If colorMethods(index) Is Nothing Then colorMethods(index) = task.edges ' New EdgeLine_Basics
                Case 4
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New Hist3Dcolor_Basics
                Case 5
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New KMeans_Basics
                Case 6
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New LUT_Basics
                Case 7
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New Reduction_Basics
                Case 8
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New PCA_NColor_CPP
                Case 9
                    If colorMethods(index) Is Nothing Then colorMethods(index) = New MeanSubtraction_Gray
            End Select
            classifier = colorMethods(index)
        End If

        If task.redOptions.colorInputName <> "EdgeLine_Basics" Then ' edgeLine_Basics is already running on each frame.
            If task.redOptions.colorInputName = "PCA_NColor_CPP" Then ' requires RGB input.
                classifier.Run(src.Clone)
            Else
                classifier.Run(task.grayStable.Clone)
            End If
        End If

        If task.optionsChanged Then dst2 = classifier.dst2.clone Else classifier.dst2.copyto(dst2, task.motionMask)
        classCount = classifier.classCount

        dst3 = ShowPalette(dst2)
        labels(2) = "Color_Basics: method = " + classifier.tracename + " produced " + CStr(classCount) +
                    " pixel classifications"
    End Sub
End Class







Public Class Color8U_Sweep : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public classCount As Integer
    Public Sub New()
        desc = "Sweep through all the Color8U_Basics algorithms..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then
            Dim index = task.redOptions.ColorSource.SelectedIndex + 1
            If index >= task.redOptions.ColorSource.Items.Count Then index = 0
            task.redOptions.ColorSource.SelectedIndex = index
            task.redOptions.Sync()
        End If

        color8u.Run(src)
        classCount = color8u.classCount
        dst2 = ShowPalette(color8u.dst2)

        strOut = "Current color source = " + task.redOptions.colorInputName
        SetTrueText(strOut, 2)
    End Sub
End Class





Public Class Color8U_Grayscale : Inherits TaskParent
    Dim options As New Options_Grayscale8U
    Public classCount = 255
    Public Sub New()
        labels = {"", "", "Color_Grayscale", ""}
        desc = "Manually create a grayscale image.  The only reason for this example is to show how slow it can be to do the work manually in VB.Net"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Channels = 1 Then
            dst2 = src ' nothing to do...
            dst3 = src ' nothing to do...
            Exit Sub
        End If

        If options.useOpenCV Then
            dst2 = task.gray
        Else
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
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






Public Class Color8U_Depth : Inherits TaskParent
    Public reduction As New Reduction_Basics
    Public depth As New Depth_InRange
    Public classCount As Integer
    Public Sub New()
        task.gOptions.LineType.SelectedIndex = 1 ' linetype = link4
        labels = {"", "", "Color Reduction Edges", "Depth Range Edges"}
        desc = "Add depth regions edges to the color Reduction image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2
        classCount = reduction.classCount

        depth.Run(src)
        dst2.SetTo(0, depth.dst3)
        dst3.SetTo(0)
        dst3.SetTo(white, depth.dst3)
    End Sub
End Class








Public Class Color8U_KMeans : Inherits TaskParent
    Public km0 As New KMeans_Basics
    Public km1 As New KMeans_Basics
    Public km2 As New KMeans_Basics
    Public colorFmt As New Color_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.displaydst1.checked = True
        labels(0) = "Recombined channels in other images."
        desc = "Run KMeans on each of the 3 color channels"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        colorFmt.Run(src)
        dst0 = colorFmt.dst2

        Dim split = dst0.Split()

        km0.Run(split(0))
        dst1 = km0.dst2 * 255 / km0.classCount

        km1.Run(split(1))
        dst2 = km1.dst2 * 255 / km0.classCount

        km2.Run(split(2))
        dst3 = km2.dst2 * 255 / km0.classCount

        For i = 1 To 3
            labels(i) = colorFmt.options.colorFormat + " channel " + CStr(i - 1)
        Next
    End Sub
End Class









Public Class Color8U_RedHue : Inherits TaskParent
    Dim options As New Options_CamShift
    Public Sub New()
        labels = {"", "", "Pixels with Red Hue", ""}
        desc = "Find all the reddish pixels in the image - indicate some life form."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mask = hsv.InRange(options.camSBins, New cv.Scalar(180, 255, options.camMax))
        dst2.SetTo(0)
        src.CopyTo(dst2, mask)
    End Sub
End Class








' https://stackoverflow.com/questions/40233986/python-is-there-a-function-or-formula-to-find-the-complementary-colour-of-a-rgb
Public Class Color8U_Complementary : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Current image in complementary colors", "HSV version of the current image but hue is flipped to complementary value."}
        desc = "Display the current image in complementary colors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim split = hsv.Split()
        split(0) += 90 Mod 180
        cv.Cv2.Merge(split, dst3)
        dst2 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
    End Sub
End Class








' https://stackoverflow.com/questions/40233986/python-is-there-a-function-or-formula-to-find-the-complementary-colour-of-a-rgb
Public Class Color8U_ComplementaryTest : Inherits TaskParent
    Dim images As New Image_Basics
    Dim comp As New Color8U_Complementary
    Public Sub New()
        labels = {"", "", "Original Image", "Color_Complementary version looks identical to the correct version at the link above "}
        desc = "Create the complementary images for Gilles Tran's 'Glasses' image for comparison"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        images.options.fileNameForm.filename.Text = task.HomeDir + "Data/Glasses by Gilles Tran.png"
        images.Run(src)
        dst2 = images.dst2

        comp.Run(dst2)
        dst3 = comp.dst2
    End Sub
End Class







' https://github.com/BhanuPrakashNani/Image_Processing/tree/master/Est.%20Transformation
Public Class Color8U_InRange : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Original", "After InRange processing"}
        desc = "Use inRange to isolate colors from the background"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = cv.Cv2.ImRead(task.HomeDir + "Data/1.jpg", cv.ImreadModes.Grayscale)
        dst1 = dst2.InRange(105, 165) ' should make this a slider and experiment further...
        dst3 = dst2.Clone
        dst3.SetTo(0, dst1)
    End Sub
End Class








Public Class Color8U_TopX : Inherits TaskParent
    Dim topX As New Hist3Dcolor_TopXColors
    Dim options As New Options_Color8UTopX
    Public Sub New()
        desc = "Classify every BGR pixel into some common colors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        topX.mapTopX = options.topXcount
        topX.Run(src)

        Dim top As New List(Of cv.Vec3b)
        For Each pt In topX.topXPixels
            top.Add(New cv.Vec3b(pt.X, pt.Y, pt.Z))
        Next

        dst2 = src.Clone
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim distances As New List(Of Single)
                For Each pt In top
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
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
Public Class Color8U_Common : Inherits TaskParent
    Dim common As New List(Of cv.Vec3b)
    Dim commonScalar As List(Of cv.Scalar) = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.Yellow, cv.Scalar.Pink, cv.Scalar.Purple, cv.Scalar.Brown,
                                              cv.Scalar.Gray, cv.Scalar.Black, white}.ToList
    Public Sub New()
        For Each c In commonScalar
            common.Add(New cv.Vec3b(c(0), c(1), c(2)))
        Next
        desc = "Classify every BGR pixel into some common colors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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







Public Class Color8U_Smoothing : Inherits TaskParent
    Dim frames As New History_Basics
    Public Sub New()
        labels = {"", "", "Averaged BGR image over the last X frames", ""}
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32FC3, 0)
        desc = "Merge that last X BGR frames to smooth out differences."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        frames.Run(src)
        dst2 = frames.dst2
        labels(2) = "The image below is the average of " + CStr(frames.saveFrames.Count) + " the last BGR frames"
    End Sub
End Class







Public Class Color8U_Hue : Inherits TaskParent
    Public Sub New()
        desc = "Isolate those regions in the image that have a reddish hue."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim loBins As cv.Scalar = New cv.Scalar(0, 40, 32)
        Dim hiBins As cv.Scalar = New cv.Scalar(180, 255, 255)
        dst2 = hsv.InRange(loBins, hiBins)
    End Sub
End Class







Public Class Color8U_BlackAndWhite : Inherits TaskParent
    Dim options As New Options_StdevGrid
    Public Sub New()
        labels = {"", "", "Mask to identify all 'black' regions", "Mask identifies all 'white' regions"}
        desc = "Create masks for black and white"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst1 = task.gray
        dst2 = dst1.Threshold(options.minThreshold, 255, cv.ThresholdTypes.BinaryInv)
        dst3 = dst1.Threshold(options.maxThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Color8U_MotionFiltered : Inherits TaskParent
    Dim color8U As New Color8U_Sweep
    Public classCount As Integer
    Dim motion As New Motion_BGSub
    Public Sub New()
        desc = "Prepare a Color8U_Basics image using the task.motionMask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.motionMask.CountNonZero Then
            src.SetTo(0, Not task.motionMask)
            color8U.Run(src)
            dst2 = color8U.dst3
            dst2.CopyTo(dst3, task.motionMask)
            dst2.SetTo(0, Not task.motionMask)
            classCount = color8U.classCount
        End If
        If task.heartBeatLT Then dst3.SetTo(0)
        labels(2) = color8U.strOut
    End Sub
End Class





Public Class Color8U_Edges : Inherits TaskParent
    Dim color8u As New Color8U_Sweep
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Find edges in the Color8U_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8u.Run(src)
        dst2 = color8u.dst3

        edges.Run(dst2)
        dst3 = edges.dst2
        labels(2) = color8u.strOut
    End Sub
End Class






Public Class Color8U_LeftRight : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public Sub New()
        task.gOptions.UseMotionMask.Checked = False
        desc = "Create a color transformation for both the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8u.Run(task.leftView)
        dst2 = color8u.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = color8u.labels(2)

        color8u.Run(task.rightView)
        dst3 = color8u.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(3) = color8u.labels(2)
    End Sub
End Class
