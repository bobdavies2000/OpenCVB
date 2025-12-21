Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class PhotoShop_Clahe : Inherits TaskParent
        ' Contrast Limited Adaptive Histogram Equalization (CLAHE) : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Clip Limit", 1, 100, 10)
                sliders.setupTrackBar("Grid Size", 1, 100, 8)
            End If

            labels(2) = "GrayScale"
            labels(3) = "CLAHE Result"
            desc = "Show a Contrast Limited Adaptive Histogram Equalization image (CLAHE)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static clipSlider = OptionParent.FindSlider("Clip Limit")
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst2 = src
            Dim claheObj = cv.Cv2.CreateCLAHE()
            claheObj.TilesGridSize() = New cv.Size(CInt(taskAlg.brickSize), CInt(taskAlg.brickSize))
            claheObj.ClipLimit = clipSlider.Value
            claheObj.Apply(src, dst3)
            claheObj.Dispose()
        End Sub
    End Class



    Public Class PhotoShop_HSV : Inherits TaskParent
        Public hsv_planes(2) As cv.Mat
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "", "HSV (8UC3)", "Hue (8uC1)"}
            desc = "HSV 8UC3 is in dst2, dst3 is Hue in 8UC1, and dst1 is Saturation 8UC1."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            cv.Cv2.CvtColor(src, dst2, cv.ColorConversionCodes.RGB2HSV)
            Dim hsv_planes = dst2.Split()

            cv.Cv2.CvtColor(hsv_planes(0), dst3, cv.ColorConversionCodes.GRAY2BGR)
            cv.Cv2.CvtColor(hsv_planes(1), dst1, cv.ColorConversionCodes.GRAY2BGR)
        End Sub
    End Class






    Public Class PhotoShop_AlphaBeta : Inherits TaskParent
        Public Sub New()
            desc = "Use alpha and beta with ConvertScaleAbs."
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Alpha (contrast)", 0, 500, 300)
                sliders.setupTrackBar("Brightness Beta", -100, 100, 0)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static alphaSlider = OptionParent.FindSlider("Alpha (contrast)")
            Static betaSlider = OptionParent.FindSlider("Brightness Beta")
            dst2 = src.ConvertScaleAbs(alphaSlider.Value / 500, betaSlider.Value)
        End Sub
    End Class








    Public Class PhotoShop_Gamma : Inherits TaskParent
        Dim lookupTable(255) As Byte
        Dim lastGamma As Integer = -1
        Public Sub New()
            desc = "Use gamma correction."
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Brightness Gamma correction", 0, 200, 50)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static gammaSlider = OptionParent.FindSlider("Brightness Gamma correction")
            If lastGamma <> gammaSlider.Value Then
                lastGamma = gammaSlider.Value
                For i = 0 To lookupTable.Length - 1
                    lookupTable(i) = Math.Pow(i / 255, gammaSlider.Value / 100) * 255
                Next
            End If
            dst2 = src.LUT(lookupTable)
        End Sub
    End Class







    ' https://blog.csdn.net/just_sort/article/details/85982871
    Public Class PhotoShop_WhiteBalancePlot : Inherits TaskParent
        Dim hist As New Hist_Graph
        Dim whiteCPP As New PhotoShop_WhiteBalance
        Public Sub New()
            hist.plotRequested = True
            labels = {"", "", "Image with auto white balance", "Histogram of pixel values"}
            desc = "Automate getting the right white balance"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static thresholdSlider = OptionParent.FindSlider("White balance threshold X100")
            Dim thresholdVal = thresholdSlider.Value / 100

            Dim rgb32f As New cv.Mat
            src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
            Dim maxVal As Double, minVal As Double
            rgb32f.MinMaxLoc(minVal, maxVal)

            Dim planes() = rgb32f.Split()
            Dim sum32f = New cv.Mat(src.Size(), cv.MatType.CV_32F)
            sum32f = planes(0) + planes(1) + planes(2)
            src = sum32f
            hist.Run(src)
            dst3 = hist.dst2

            Dim sum As Single
            Dim threshold As Integer
            For i = hist.histRaw(0).Rows - 1 To 0 Step -1
                sum += hist.histRaw(0).Get(Of Single)(i, 0)
                If sum > src.Rows * src.Cols * thresholdVal Then
                    threshold = i
                    Exit For
                End If
            Next

            Dim mask = sum32f.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(1)

            Dim mean = rgb32f.Mean(mask)
            For i = 0 To rgb32f.Channels() - 1
                planes(i) *= maxVal / mean(i)
                planes(i) = planes(i).Threshold(255, 255, cv.ThresholdTypes.Trunc)
            Next

            cv.Cv2.Merge(planes, rgb32f)
            rgb32f.ConvertTo(dst2, cv.MatType.CV_8UC3)
        End Sub
    End Class






    ' https://blog.csdn.net/just_sort/article/details/85982871
    Public Class PhotoShop_ChangeMask : Inherits TaskParent
        Dim whiteBal As New PhotoShop_WhiteBalance
        Public Sub New()
            OptionParent.FindSlider("White balance threshold X100").Value = 3
            desc = "Create a mask for the changed pixels after white balance"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            whiteBal.Run(src)
            dst2 = whiteBal.dst2
            labels(2) = "White balanced image"
            labels(3) = "Mask of changed pixels"
            Dim diff = dst2 - src
            dst3 = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class





    ' https://blog.csdn.net/just_sort/article/details/85982871
    Public Class PhotoShop_PlotHist : Inherits TaskParent
        Dim whiteBal As New PhotoShop_ChangeMask
        Public hist1 As New Hist_Basics
        Public hist2 As New Hist_Basics
        Dim mat2to1 As New Mat_2to1
        Public Sub New()
            hist1.plotHist.addLabels = False
            hist2.plotHist.addLabels = False
            desc = "Plot the histogram of the before and after white balancing"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist1.Run(src)
            mat2to1.mat(0) = hist1.dst2

            whiteBal.Run(src)
            dst2 = whiteBal.dst2
            labels(2) = whiteBal.labels(2)

            hist2.Run(dst2)
            mat2to1.mat(1) = hist2.dst2

            mat2to1.Run(src)
            dst3 = mat2to1.dst2
            labels(3) = "The top is before white balance"
        End Sub
    End Class








    ' https://github.com/spmallick/learnopencv/tree/master/
    Public Class PhotoShop_Sepia : Inherits TaskParent
        Public Sub New()
            desc = "Create a sepia image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)
            Dim tMatrix = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_64F, {{0.393, 0.769, 0.189}, {0.349, 0.686, 0.168}, {0.272, 0.534, 0.131}})
            dst2 = dst2.Transform(tMatrix).Threshold(255, 255, cv.ThresholdTypes.Trunc)
        End Sub
    End Class







    ' https://github.com/spmallick/learnopencv/tree/master/
    Public Class PhotoShop_Emboss : Inherits TaskParent
        Public gray128 As cv.Mat
        Public Sub New()

            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Emboss Kernel Size", 2, 10, 2)
            End If

            If radio.Setup(traceName) Then
                radio.addRadio("Bottom Left")
                radio.addRadio("Bottom Right")
                radio.addRadio("Top Left")
                radio.addRadio("Top Right")
                radio.check(0).Checked = True
            End If

            gray128 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 128)
            labels(3) = "Embossed output"
            desc = "Use the video stream to make it appear like an embossed paper image."
        End Sub
        Public Function kernelGenerator(size As Integer) As cv.Mat
            Dim kernel As cv.Mat = New cv.Mat(size, size, cv.MatType.CV_8S, cv.Scalar.All(0))
            For i = 0 To size - 1
                For j = 0 To size - 1
                    If i < j Then kernel.Set(Of SByte)(j, i, -1) Else If i > j Then kernel.Set(Of SByte)(j, i, 1)
                Next
            Next
            Return kernel
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static sizeSlider = OptionParent.FindSlider("Emboss Kernel Size")
            Dim kernel = kernelGenerator(sizeSlider.Value)

            Dim direction As Integer
            Static frm = OptionParent.findFrm(traceName + " Radio Buttons")
            For direction = 0 To frm.check.Count - 1
                If frm.check(direction).Checked Then Exit For
            Next

            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Select Case direction
                Case 0 ' do nothing!
                Case 1 ' flip vertically
                    cv.Cv2.Flip(kernel, kernel, cv.FlipMode.Y)
                Case 2 ' flip horizontally
                    cv.Cv2.Flip(kernel, kernel, cv.FlipMode.X)
                Case 3 ' flip horizontally and vertically
                    cv.Cv2.Flip(kernel, kernel, cv.FlipMode.XY)
            End Select

            dst3 = dst2.Filter2D(-1, kernel)
            cv.Cv2.Add(dst3, gray128, dst3)
        End Sub
    End Class






    ' https://github.com/spmallick/learnopencv/tree/master/
    Public Class PhotoShop_EmbossAll : Inherits TaskParent
        Dim emboss As New PhotoShop_Emboss
        Dim mats As New Mat_4to1
        Dim sizeSlider As TrackBar
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Emboss threshold", 0, 255, 200)
            End If
            sizeSlider = OptionParent.FindSlider("Emboss Kernel Size")
            sizeSlider.Value = 5

            labels(2) = "The combination of all angles"
            labels(3) = "bottom left, bottom right, top left, top right"
            desc = "Emboss using all the directions provided"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static threshSlider = OptionParent.FindSlider("Emboss threshold")
            Dim kernel = emboss.kernelGenerator(sizeSlider.Value)

            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst3 = dst2.Filter2D(-1, kernel)
            cv.Cv2.Add(dst3, emboss.gray128, mats.mat(0))
            mats.mat(0) = mats.mat(0).Threshold(threshSlider.Value, 255, cv.ThresholdTypes.Binary)

            cv.Cv2.Flip(kernel, kernel, cv.FlipMode.Y)
            dst3 = dst2.Filter2D(-1, kernel)
            cv.Cv2.Add(dst3, emboss.gray128, mats.mat(1))
            mats.mat(1) = mats.mat(1).Threshold(threshSlider.Value, 255, cv.ThresholdTypes.Binary)

            cv.Cv2.Flip(kernel, kernel, cv.FlipMode.X)
            dst3 = dst2.Filter2D(-1, kernel)
            cv.Cv2.Add(dst3, emboss.gray128, mats.mat(2))
            mats.mat(2) = mats.mat(2).Threshold(threshSlider.Value, 255, cv.ThresholdTypes.Binary)

            cv.Cv2.Flip(kernel, kernel, cv.FlipMode.XY)
            dst3 = dst2.Filter2D(-1, kernel)
            cv.Cv2.Add(dst3, emboss.gray128, mats.mat(3))
            mats.mat(3) = mats.mat(3).Threshold(threshSlider.Value, 255, cv.ThresholdTypes.Binary)

            dst2.SetTo(0)
            For i = 0 To mats.mat.Count - 1
                dst2 = mats.mat(i) Or dst2
            Next

            mats.Run(emptyMat)
            dst3 = mats.dst2
        End Sub
    End Class







    ' https://github.com/spmallick/learnopencv/tree/master/
    Public Class PhotoShop_DuoTone : Inherits TaskParent
        Dim options As New Options_Photoshop
        Public Sub New()

            If radio.Setup(traceName) Then
                radio.addRadio("First DuoTone Blue")
                radio.addRadio("First DuoTone Green")
                radio.addRadio("First DuoTone Red")
                radio.check(1).Checked = True
            End If

            If check.Setup(traceName) Then check.addCheckBox("DuoTone Dark if checked, Light otherwise")

            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("DuoTone Exponent", 0, 50, 0)
            End If

            desc = "Create a DuoTone image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static duoCheck = OptionParent.findCheckBox("DuoTone Dark if checked, Light otherwise")
            options.Run()
            Static expSlider = OptionParent.FindSlider("DuoTone Exponent")
            Dim exp = 1 + expSlider.Value / 100
            Dim expMat As New cv.Mat(256, 1, cv.MatType.CV_8U)
            Dim expDark As New cv.Mat(256, 1, cv.MatType.CV_8U)
            For i = 0 To expMat.Rows - 1
                expMat.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, exp), 255))
                expDark.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, 2 - exp), 255))
            Next

            Dim split = src.Split()

            Dim switch1 As Integer
            Static frm = OptionParent.findFrm(traceName + " Radio Buttons")
            For switch1 = 0 To frm.check.Count - 1
                If frm.check(switch1).Checked Then Exit For
            Next

            For i = 0 To split.Count - 1
                If i = switch1 Or i = options.switchColor Then
                    split(i) = split(i).LUT(expMat)
                ElseIf duoCheck.Checked Then
                    split(i) = split(i).LUT(expDark)
                Else
                    split(i).SetTo(0)
                End If
            Next

            cv.Cv2.Merge(split, dst2)
        End Sub
    End Class





    Public Class PhotoShop_UnsharpMask : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("sigma", 1, 2000, 100)
                sliders.setupTrackBar("Photoshop Threshold", 0, 255, 5)
                sliders.setupTrackBar("Shift Amount", 0, 5000, 1000)
            End If
            desc = "Sharpen an image"
            labels(3) = "Unsharp mask (difference from Blur)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static sigmaSlider = OptionParent.FindSlider("sigma")
            Static thresholdSlider = OptionParent.FindSlider("Photoshop Threshold")
            Static shiftSlider = OptionParent.FindSlider("Shift Amount")

            Dim blurred As New cv.Mat
            Dim amount As Double = shiftSlider.Value / 1000
            cv.Cv2.GaussianBlur(src, dst3, New cv.Size(), sigmaSlider.Value / 100, sigmaSlider.Value / 100)

            Dim diff As New cv.Mat
            cv.Cv2.Absdiff(src, dst3, diff)
            diff = diff.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
            dst2 = src * (1 + amount) + diff * (-amount)
            diff.CopyTo(dst3)
        End Sub
    End Class








    ' https://www.learnopencvb.com/non-photorealistic-rendering-using-opencv-python-c/
    Public Class PhotoShop_SharpenStylize : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Stylize Sigma_s", 0, 200, 60)
                sliders.setupTrackBar("Stylize Sigma_r X100", 1, 100, 7)
            End If
            desc = "Stylize an image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static sSlider = OptionParent.FindSlider("Stylize Sigma_s")
            Static rSlider = OptionParent.FindSlider("Stylize Sigma_r X100")
            cv.Cv2.Stylization(src, dst2, sSlider.Value, rSlider.Value / rSlider.maximum)
        End Sub
    End Class







    ' https://www.learnopencvb.com/non-photorealistic-rendering-using-opencv-python-c/
    Public Class PhotoShop_Pencil_Basics : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Pencil Sigma_s", 0, 200, 60)
                sliders.setupTrackBar("Pencil Sigma_r X100", 1, 100, 7)
                sliders.setupTrackBar("Pencil Shade Factor X100", 1, 200, 40)
            End If

            desc = "Convert image to a pencil sketch"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static sSlider = OptionParent.FindSlider("Pencil Sigma_s")
            Static rSlider = OptionParent.FindSlider("Pencil Sigma_r X100")
            Static shadeSlider = OptionParent.FindSlider("Pencil Shade Factor X100")
            cv.Cv2.PencilSketch(src, dst3, dst2, sSlider.Value, rSlider.Value / rSlider.maximum, shadeSlider.Value / 1000)
        End Sub
    End Class






    ' https://cppsecrets.com/users/2582658986657266505064717765737646677977/Convert-photo-to-sketch-using-python.php?fbclid=IwAR3pOtiqxeOPiqouii7tmN9Q7yA5vG4dFdXGqA0XgZqcMB87w5a1PEMzGOw
    Public Class PhotoShop_Pencil_Manual : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Blur kernel size", 2, 100, 10)
            End If

            If radio.Setup(traceName) Then
                radio.addRadio("Pencil grayscale image")
                radio.addRadio("Pencil grayscale inverted image")
                radio.addRadio("Pencil blur image")
                radio.check(0).Checked = True
            End If
            desc = "Break down the process of converting an image to a sketch"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim grayinv As New cv.Mat
            grayinv = Not src
            Static kernelSlider = OptionParent.FindSlider("Blur kernel size")
            Dim ksize As Integer = kernelSlider.Value Or 1
            Dim blur = grayinv.Blur(New cv.Size(ksize, ksize), New cv.Point(ksize / 2, ksize / 2))
            cv.Cv2.Divide(src, 255 - blur, dst2, 256)

            Dim index As Integer = -1
            Static frm = OptionParent.findFrm(traceName + " Radio Buttons")
            For index = 0 To frm.check.Count - 1
                If radio.check(index).Checked Then Exit For
            Next
            labels(3) = "Intermediate result: " + Choose(index + 1, "grayscale image", "grayscale inverted image", "blur image")
            dst3 = Choose(index + 1, src, grayinv, blur)
        End Sub
    End Class







    Public Class PhotoShop_Vignetting : Inherits TaskParent
        Dim vignet As New Vignetting_Basics
        Public Sub New()
            labels(2) = "Vignetted image.  Click anywhere to establish a different center."
            desc = "Inject vignetting into an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            vignet.Run(src)
            dst2 = vignet.dst2
        End Sub
    End Class






    ' https://www.learnopencvb.com/non-photorealistic-rendering-using-opencv-python-c/
    Public Class PhotoShop_SharpenDetail : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("DetailEnhance Sigma_s", 0, 200, 60)
                sliders.setupTrackBar("DetailEnhance Sigma_r X100", 1, 100, 7)
            End If
            desc = "Enhance detail on an image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static sSigmaSlider = OptionParent.FindSlider("DetailEnhance Sigma_s")
            Static rSigmaSlider = OptionParent.FindSlider("DetailEnhance Sigma_r X100")

            If src.Channels <> 3 Then src = taskAlg.color.Clone
            cv.Cv2.DetailEnhance(src, dst2, sSigmaSlider.Value, rSigmaSlider.Value / rSigmaSlider.Maximum)
        End Sub
    End Class





    ' https://blog.csdn.net/just_sort/article/details/85982871
    Public Class PhotoShop_WhiteBalance : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("White balance threshold X100", 1, 100, 50)
            cPtr = WhiteBalance_Open()
            labels = {"", "", "Image with white balance applied", "White pixels were altered from the original"}
            desc = "Automate getting the right white balance"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static thresholdSlider = OptionParent.FindSlider("White balance threshold X100")
            Dim thresholdVal As Single = thresholdSlider.Value / 100

            If src.Channels <> 3 Then src = taskAlg.color.Clone
            Dim rgbData(src.Total * src.ElemSize - 1) As Byte
            Dim handleSrc = GCHandle.Alloc(rgbData, GCHandleType.Pinned) ' pin it for the duration...
            Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)

            Dim imagePtr = WhiteBalance_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, thresholdVal)
            handleSrc.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
            If standaloneTest() Then
                Dim diff = dst2 - src
                diff = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                dst3 = diff.ToMat().Threshold(1, 255, cv.ThresholdTypes.Binary)
            End If
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = WhiteBalance_Close(cPtr)
        End Sub
    End Class
End Namespace