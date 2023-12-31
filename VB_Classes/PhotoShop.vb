Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PhotoShop_Clahe : Inherits VBparent
    ' Contrast Limited Adaptive Histogram Equalization (CLAHE) : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Clip Limit", 1, 100, 10)
        End If
        sliders.setupTrackBar(1, "Grid Size", 1, 100, 8)
        labels(2) = "GrayScale"
        labels(3) = "CLAHE Result"
        task.desc = "Show a Contrast Limited Adaptive Histogram Equalization image (CLAHE)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src
        Dim claheObj = cv.Cv2.CreateCLAHE()
        claheObj.TilesGridSize() = New cv.Size(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        claheObj.ClipLimit = sliders.trackbar(0).Value
        claheObj.Apply(src, dst3)
    End Sub
End Class



Public Class PhotoShop_Hue : Inherits VBparent
    Public hsv_planes(2) As cv.Mat
    Public Sub New()
        labels(2) = "Hue"
        labels(3) = "Saturation"
        task.desc = "Show hue (Result1) and Saturation (Result2)."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim imghsv = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        cv.Cv2.CvtColor(src, imghsv, cv.ColorConversionCodes.RGB2HSV)
        Dim hsv_planes = imghsv.Split()

        cv.Cv2.CvtColor(hsv_planes(0), dst2, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(hsv_planes(1), dst3, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class PhotoShop_AlphaBeta : Inherits VBparent
    Public Sub New()
        task.desc = "Use alpha and beta with ConvertScaleAbs."
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 500, 300)
            sliders.setupTrackBar(1, "Brightness Beta (brightness)", -100, 100, 0)
        End If
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.ConvertScaleAbs(sliders.trackbar(0).Value / 500, sliders.trackbar(1).Value)
    End Sub
End Class








Public Class PhotoShop_Gamma : Inherits VBparent
    Dim lookupTable(255) As Byte
    Public Sub New()
        task.desc = "Use gamma with ConvertScaleAbs."
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Brightness Gamma correction", 0, 200, 100)
        End If
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static lastGamma As Integer = -1
        If lastGamma <> sliders.trackbar(0).Value Then
            lastGamma = sliders.trackbar(0).Value
            For i = 0 To lookupTable.Length - 1
                lookupTable(i) = Math.Pow(i / 255, sliders.trackbar(0).Value / 100) * 255
            Next
        End If
        dst2 = src.LUT(lookupTable)
    End Sub
End Class




Module PhotoShop_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub WhiteBalance_Close(wPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Run(wPtr As IntPtr, rgb As IntPtr, rows As Integer, cols As Integer, thresholdVal As Single) As IntPtr
    End Function
End Module





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class PhotoShop_WhiteBalance_CPP : Inherits VBparent
    Dim wPtr As IntPtr
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "White balance threshold X100", 1, 100, 10)
        End If
        wPtr = WhiteBalance_Open()
        labels(2) = "Image with auto white balance"
        labels(3) = "White pixels were altered from the original"
        task.desc = "Automate getting the right white balance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("White balance threshold X100")
        Dim thresholdVal As Single = thresholdSlider.Value / 100

        Dim rgbData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(rgbData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)

        Dim rgbPtr = WhiteBalance_Run(wPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, thresholdVal)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbPtr) ' no need to copy.  rgbPtr points to C++ data, not managed.
        Dim diff = dst2 - src
        diff = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = diff.ToMat().Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Close()
        WhiteBalance_Close(wPtr)
    End Sub
End Class





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class PhotoShop_WhiteBalance : Inherits VBparent
    Dim hist As New Histogram_Graph
    Dim whiteCPP As New PhotoShop_WhiteBalance_CPP
    Dim wPtr As IntPtr
    Public Sub New()
        hist.plotRequested = True
        labels(2) = "Image with auto white balance"
        task.desc = "Automate getting the right white balance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("White balance threshold X100")
        Dim thresholdVal = thresholdSlider.Value / 100

        Dim rgb32f As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim maxVal As Double, minVal As Double
        rgb32f.MinMaxLoc(minVal, maxVal)

        Dim planes() = rgb32f.Split()
        Dim sum32f = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        sum32f = planes(0) + planes(1) + planes(2)
        src = sum32f
        hist.RunClass(src)
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
        For i = 0 To rgb32f.Channels - 1
            planes(i) *= maxVal / mean.Item(i)
            planes(i) = planes(i).Threshold(255, 255, cv.ThresholdTypes.Trunc)
        Next

        cv.Cv2.Merge(planes, rgb32f)
        rgb32f.ConvertTo(dst2, cv.MatType.CV_8UC3)
    End Sub
    Public Sub Close()
        WhiteBalance_Close(wPtr)
    End Sub
End Class






' https://blog.csdn.net/just_sort/article/details/85982871
Public Class PhotoShop_ChangeMask : Inherits VBparent
    Dim white As New PhotoShop_WhiteBalance
    Dim whiteCPP As New PhotoShop_WhiteBalance_CPP
    Public Sub New()
        task.desc = "Create a mask for the changed pixels after white balance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static countdown = 120
        Static whiteFlag As Boolean
        If countdown = 0 Then
            countdown = 120
            whiteFlag = Not whiteFlag
        End If
        countdown -= 1

        If whiteFlag Then
            white.RunClass(src)
            dst2 = white.dst2
            labels(2) = "White balanced image - VB version"
            labels(3) = "Mask of changed pixels - VB version"
        Else
            whiteCPP.RunClass(src)
            dst2 = whiteCPP.dst2
            labels(2) = "White balanced image - C++ version"
            labels(3) = "Mask of changed pixels - C++ version"
        End If
        Dim diff = dst2 - src
        dst3 = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class PhotoShop_PlotHist : Inherits VBparent
    Dim white As New PhotoShop_ChangeMask
    Public hist1 As New Histogram_Basics
    Public hist2 As New Histogram_Basics
    Dim mat2to1 As New Mat_2to1
    Public Sub New()
        task.desc = "Plot the histogram of the before and after white balancing"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist1.RunClass(src)
        mat2to1.mat(0) = hist1.dst2

        white.RunClass(src)
        dst2 = white.dst2
        labels(2) = white.labels(2)

        hist2.RunClass(dst2)
        mat2to1.mat(1) = hist2.dst2

        mat2to1.RunClass(src)
        dst3 = mat2to1.dst2
        labels(3) = "The top is before white balance"
    End Sub
End Class








' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_Sepia : Inherits VBparent
    Public Sub New()
        task.desc = "Create a sepia image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)
        Dim tMatrix = New cv.Mat(3, 3, cv.MatType.CV_64F, {{0.393, 0.769, 0.189}, {0.349, 0.686, 0.168}, {0.272, 0.534, 0.131}})
        dst2 = dst2.Transform(tMatrix).Threshold(255, 255, cv.ThresholdTypes.Trunc)
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_Emboss : Inherits VBparent
    Public gray128 As cv.Mat
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Emboss Kernel Size", 2, 10, 2)
        End If

        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "Bottom Left"
            radio.check(1).Text = "Bottom Right"
            radio.check(2).Text = "Top Left"
            radio.check(3).Text = "Top Right"
            radio.check(0).Checked = True
        End If

        gray128 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 128)
        labels(3) = "Embossed output"
        task.desc = "Use the video stream to make it appear like an embossed paper image."
    End Sub
    Public Function kernelGenerator(size As Integer) As cv.Mat
        Dim kernel As New cv.Mat(size, size, cv.MatType.CV_8S, 0)
        For i = 0 To size - 1
            For j = 0 To size - 1
                If i < j Then kernel.Set(Of SByte)(j, i, -1) Else If i > j Then kernel.Set(Of SByte)(j, i, 1)
            Next
        Next
        Return kernel
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sizeSlider = findSlider("Emboss Kernel Size")
        Dim kernel = kernelGenerator(sizeSlider.value)

        Dim direction As Integer
        Static frm = findfrm(caller + " Radio Options")
        For direction = 0 To frm.check.length - 1
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
Public Class PhotoShop_EmbossAll : Inherits VBparent
    Dim emboss As New PhotoShop_Emboss
    Dim mats As New Mat_4to1
    Dim sizeSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Emboss threshold", 0, 255, 200)
        End If
        sizeSlider = findSlider("Emboss Kernel Size")
        sizeSlider.Value = 5

        labels(2) = "The combination of all angles"
        labels(3) = "bottom left, bottom right, top left, top right"
        task.desc = "Emboss using all the directions provided"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static threshSlider = findSlider("Emboss threshold")
        Dim kernel = emboss.kernelGenerator(sizeSlider.Value)

        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst2.Filter2D(-1, kernel)
        cv.Cv2.Add(dst3, emboss.gray128, mats.mat(0))
        mats.mat(0) = mats.mat(0).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.Y)
        dst3 = dst2.Filter2D(-1, kernel)
        cv.Cv2.Add(dst3, emboss.gray128, mats.mat(1))
        mats.mat(1) = mats.mat(1).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.X)
        dst3 = dst2.Filter2D(-1, kernel)
        cv.Cv2.Add(dst3, emboss.gray128, mats.mat(2))
        mats.mat(2) = mats.mat(2).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.Flip(kernel, kernel, cv.FlipMode.XY)
        dst3 = dst2.Filter2D(-1, kernel)
        cv.Cv2.Add(dst3, emboss.gray128, mats.mat(3))
        mats.mat(3) = mats.mat(3).Threshold(threshSlider.value, 255, cv.ThresholdTypes.Binary)

        dst2.SetTo(0)
        For i = 0 To mats.mat.Count - 1
            cv.Cv2.BitwiseOr(mats.mat(i), dst2, dst2)
        Next

        mats.RunClass(src)
        dst3 = mats.dst2
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_DuoTone : Inherits VBparent
    Public Sub New()

        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "First DuoTone Blue"
            radio.check(1).Text = "First DuoTone Green"
            radio.check(2).Text = "First DuoTone Red"
            radio.check(1).Checked = True

            radio1.Setup(caller + " ContourApproximation Mode", 4)
            radio1.check(0).Text = "Second DuoTone Blue"
            radio1.check(1).Text = "Second DuoTone Green"
            radio1.check(2).Text = "Second DuoTone Red"
            radio1.check(3).Text = "Second DuoTone None"
            radio1.check(3).Checked = True
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "DuoTone Dark if checked, Light otherwise"
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "DuoTone Exponent", 0, 50, 0)
        End If

        task.desc = "Create a DuoTone image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static expSlider = findSlider("DuoTone Exponent")
        Dim exp = 1 + expSlider.value / 100
        Dim expMat As New cv.Mat(256, 1, cv.MatType.CV_8U)
        Dim expDark As New cv.Mat(256, 1, cv.MatType.CV_8U)
        For i = 0 To expMat.Rows - 1
            expMat.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, exp), 255))
            expDark.Set(Of Byte)(0, i, Math.Min(Math.Pow(i, 2 - exp), 255))
        Next

        Dim split = src.Split()

        Dim sw1 As Integer
        Static frm = findfrm(caller + " Radio Options")
        For sw1 = 0 To frm.check.length - 1
            If frm.check(sw1).Checked Then Exit For
        Next

        Dim sw2 As Integer
        Static frm1 = findfrm(caller + " ContourApproximation Mode Radio Options")
        For sw2 = 0 To frm1.check.length - 1
            If frm1.check(sw2).Checked Then Exit For
        Next

        For i = 0 To split.Count - 1
            If i = sw1 Or i = sw2 Then
                split(i) = split(i).LUT(expMat)
            ElseIf check.Box(0).Checked Then
                split(i) = split(i).LUT(expDark)
            Else
                split(i).setto(0)
            End If
        Next

        cv.Cv2.Merge(split, dst2)
    End Sub
End Class






' https://github.com/spmallick/learnopencv/tree/master/
Public Class PhotoShop_Brightness : Inherits VBparent
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Brightness Value", 0, 255, 100)
        End If

        labels(2) = "RGB straight to HSV"
        task.desc = "Implement the traditional brightness effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static brightnessSlider = findSlider("Brightness Value")
        Dim brightness As Single = brightnessSlider.value / 100

        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim hsv64 As New cv.Mat
        dst2.ConvertTo(hsv64, cv.MatType.CV_64F)
        Dim split = hsv64.Split()

        split(1) *= brightness
        split(1) = split(1).Threshold(255, 255, cv.ThresholdTypes.Trunc)

        split(2) *= brightness
        split(2) = split(2).Threshold(255, 255, cv.ThresholdTypes.Trunc)

        cv.Cv2.Merge(split, hsv64)
        hsv64.ConvertTo(dst3, cv.MatType.CV_8UC3)
        dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        labels(3) = "Brightness level = " + CStr(brightnessSlider.value)
    End Sub
End Class





Public Class PhotoShop_UnsharpMask : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "sigma", 1, 2000, 100)
            sliders.setupTrackBar(1, "threshold", 0, 255, 5)
            sliders.setupTrackBar(2, "Shift Amount", 0, 5000, 1000)
        End If
        task.desc = "Sharpen an image - Painterly Effect"
        labels(3) = "Unsharp mask (difference from Blur)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim blurred As New cv.Mat
        Dim sigma As Double = sliders.trackbar(0).Value / 100
        Dim threshold As Double = sliders.trackbar(1).Value
        Dim amount As Double = sliders.trackbar(2).Value / 1000
        cv.Cv2.GaussianBlur(src, dst3, New cv.Size(), sigma, sigma)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(src, dst3, diff)
        diff = diff.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        dst2 = src * (1 + amount) + diff * (-amount)
        diff.CopyTo(dst3)
    End Sub
End Class






' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class PhotoShop_SharpenDetail : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "DetailEnhance Sigma_s", 0, 200, 60)
            sliders.setupTrackBar(1, "DetailEnhance Sigma_r X100", 1, 100, 7)
        End If
        task.desc = "Enhance detail on an image - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        cv.Cv2.DetailEnhance(src, dst2, sigma_s, sigma_r)
    End Sub
End Class







' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class PhotoShop_SharpenStylize : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Stylize Sigma_s", 0, 200, 60)
            sliders.setupTrackBar(1, "Stylize Sigma_r X100", 1, 100, 7)
        End If
        task.desc = "Stylize an image - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        cv.Cv2.Stylization(src, dst2, sigma_s, sigma_r)
    End Sub
End Class







' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class PhotoShop_Pencil_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Pencil Sigma_s", 0, 200, 60)
            sliders.setupTrackBar(1, "Pencil Sigma_r", 1, 100, 7)
            sliders.setupTrackBar(2, "Pencil Shade Factor X100", 1, 200, 40)
        End If

        task.desc = "Convert image to a pencil sketch - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        Dim shadowFactor = sliders.trackbar(2).Value / 1000
        cv.Cv2.PencilSketch(src, dst3, dst2, sigma_s, sigma_r, shadowFactor)
    End Sub
End Class






' https://cppsecrets.com/users/2582658986657266505064717765737646677977/Convert-photo-to-sketch-using-python.php?fbclid=IwAR3pOtiqxeOPiqouii7tmN9Q7yA5vG4dFdXGqA0XgZqcMB87w5a1PEMzGOw
Public Class PhotoShop_Pencil_Manual : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Blur kernel size", 2, 100, 10)
        End If

        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "Pencil grayscale image"
            radio.check(1).Text = "Pencil grayscale inverted image"
            radio.check(2).Text = "Pencil blur image"
            radio.check(0).Checked = True
        End If
        task.desc = "Break down the process of converting an image to a sketch - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayinv As New cv.Mat
        cv.Cv2.BitwiseNot(src, grayinv)
        Static kernelSlider = findSlider("Blur kernel size")
        Dim ksize As Integer = kernelSlider.Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim blur = grayinv.Blur(New cv.Size(ksize, ksize), New cv.Point(ksize / 2, ksize / 2))
        cv.Cv2.Divide(src, 255 - blur, dst2, 256)

        Dim index As Integer = -1
        Static frm = findfrm(caller + " Radio Options")
        For index = 0 To frm.check.length - 1
            If radio.check(index).Checked Then Exit For
        Next
        labels(3) = "Intermediate result: " + Choose(index + 1, "grayscale image", "grayscale inverted image", "blur image")
        dst3 = Choose(index + 1, src, grayinv, blur)
    End Sub
End Class

