Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading

' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_CPP
    Inherits VBparent
    Dim bgfs As IntPtr
    Public currMethod As integer = -1
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 7)
            radio.check(0).Text = "GMG"
            radio.check(1).Text = "CNT - Counting"
            radio.check(2).Text = "KNN"
            radio.check(3).Text = "MOG"
            radio.check(4).Text = "MOG2"
            radio.check(5).Text = "GSOC"
            radio.check(6).Text = "LSBP"
            radio.check(4).Checked = True ' mog2 appears to be the best...
        End If
        task.desc = "Demonstrate all the different background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static frm = findfrm("BGSubtract_Basics_CPP Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    If ocvb.frameCount > 0 Then BGSubtract_BGFG_Close(bgfs)
                    currMethod = i
                    label1 = "Method = " + frm.check(i).Text
                    bgfs = BGSubtract_BGFG_Open(currMethod)
                End If
            End If
        Next
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(bgfs, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        BGSubtract_BGFG_Close(bgfs)
    End Sub
End Class





Public Class BGSubtract_MotionDetect_MT
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)
        End If
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            For i = 0 To radio.check.Length - 1
                radio.check(i).Text = CStr(2 ^ i) + " threads"
            Next
            radio.check(0).Text = "1 thread"
            radio.check(5).Checked = True
        End If
        label2 = "Only Motion Added"
        task.desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount = 0 Then src.CopyTo(dst2)
        Dim threadData As New cv.Vec3i
        Dim width = src.Width, height = src.Height
        Static frm = findfrm("BGSubtract_MotionDetect_MT Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, width, height), New cv.Vec3i(2, width / 2, height), New cv.Vec3i(4, width / 2, height / 2),
                                           New cv.Vec3i(8, width / 4, height / 2), New cv.Vec3i(16, width / 4, height / 4), New cv.Vec3i(32, width / 8, height / 4))
                Exit For
            End If
        Next
        Dim threadCount = threadData(0)
        width = threadData(1)
        height = threadData(2)
        Dim taskArray(threadCount - 1) As System.Threading.Tasks.Task
        Dim xfactor = CInt(src.Width / width)
        Dim yfactor = Math.Max(CInt(src.Height / height), CInt(src.Width / width))
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        dst1.SetTo(0)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = System.Threading.Tasks.Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * width, height * Math.Floor(section / yfactor), width, height)
                    Dim correlation As New cv.Mat
                    cv.Cv2.MatchTemplate(src(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        src(roi).CopyTo(dst1(roi))
                        src(roi).CopyTo(dst2(roi))
                    End If
                End Sub)
        Next
        System.Threading.Tasks.Task.WaitAll(taskArray)
    End Sub
End Class




Public Class BGSubtract_Basics_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)
        End If

        task.desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()
        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = input.EmptyClone.SetTo(0)
        If ocvb.frameCount = 0 Then dst2 = input.Clone()
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        dst1.SetTo(0)
        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(input(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                Interlocked.Increment(updateCount)
                input(roi).CopyTo(dst2(roi))
            End If
            input(roi).CopyTo(dst1(roi))
        End Sub)
        label1 = "Motion added to dst2 for " + CStr(updateCount) + " segments out of " + CStr(grid.roiList.Count)
        label2 = CStr(grid.roiList.Count - updateCount) + " segments had > " + Format(correlationSlider.value / 1000, "0.0%") + " correlation"
    End Sub
End Class






Public Class BGSubtract_Depth_MT
    Inherits VBparent
    Dim bgsub As BGSubtract_Basics_MT
    Public Sub New()
        initParent()
        bgsub = New BGSubtract_Basics_MT()
        task.desc = "Detect Motion in the depth image - needs more work"
        label1 = "Depth data src"
        label2 = "Accumulated depth image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        bgsub.src = task.RGBDepth
        bgsub.Run()
        dst1 = bgsub.src
        dst2 = bgsub.dst2
        dst2.SetTo(0, task.inrange.nodepthmask)
    End Sub
End Class



Public Class BGSubtract_MOG
    Inherits VBparent
    Dim MOG As cv.BackgroundSubtractorMOG
    Public gray As New cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)
        End If

        MOG = cv.BackgroundSubtractorMOG.Create()
        task.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src
        End If
        Static learnRateSlider = findSlider("MOG Learn Rate")
        MOG.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst1 = gray
    End Sub
End Class



Public Class BGSubtract_MOG2
    Inherits VBparent
    Public gray As New cv.Mat
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)
        End If
        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        task.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("MOG Learn Rate")
        MOG2.Apply(input, dst1, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_GMG_KNN
    Inherits VBparent
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Learn Rate", 1, 1000, 1)
        End If

        gmg = cv.BackgroundSubtractorGMG.Create()
        knn = cv.BackgroundSubtractorKNN.Create()
        task.desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount < 120 Then
            ocvb.trueText("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(ocvb.frameCount))
        Else
            ocvb.trueText("")
        End If

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("Learn Rate")
        gmg.Apply(dst1, dst1, learnRateSlider.Value / 1000)
        knn.Apply(dst1, dst1, learnRateSlider.Value / 1000)
    End Sub
End Class





Public Class BGSubtract_MOG_RGBDepth
    Inherits VBparent
    Public gray As New cv.Mat
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MOG Learn Rate x1000", 0, 1000, 10)
        End If

        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        label1 = "Unstable depth"
        label2 = "Unstable color"
        task.desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("Learn Rate")
        MOGDepth.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGRGB.Apply(input, dst2, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_MOG_Retina
    Inherits VBparent
    Dim bgSub As BGSubtract_MOG
    Dim retina As Retina_Basics_CPP
    Public Sub New()
        initParent()
        bgSub = New BGSubtract_MOG()
        Static bgSubLearnRate = findSlider("MOG Learn Rate")
        bgSubLearnRate.Value = 100

        retina = New Retina_Basics_CPP()

        label1 = "MOG results of depth motion"
        label2 = "Difference from retina depth motion."
        task.desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        retina.src = task.RGBDepth
        retina.Run()
        bgSub.src = retina.dst2.Clone()
        bgSub.Run()
        dst1 = bgSub.dst1
        cv.Cv2.Subtract(bgSub.dst1, retina.dst2, dst2)
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion
    Inherits VBparent
    Public motion As Diff_UnstableDepthAndColor
    Public Sub New()
        initParent()
        motion = New Diff_UnstableDepthAndColor()
        task.desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        motion.src = src.Clone()
        motion.Run()
        dst1 = motion.dst1
        dst2 = motion.dst2
        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs()
        cv.Cv2.BitwiseNot(mask, mask)
        src.CopyTo(dst2, mask)
        label2 = "Image with instability filled with color data"
    End Sub
End Class






Module BGSubtract_BGFG_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Open(currMethod As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub BGSubtract_BGFG_Close(bgfs As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Run(bgfs As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module





Public Class BGSubtract_Video
    Inherits VBparent
    Dim bgfg As BGSubtract_Basics_CPP
    Dim video As Video_Basics
    Public Sub New()
        initParent()
        bgfg = New BGSubtract_Basics_CPP()

        video = New Video_Basics()
        video.srcVideo = ocvb.parms.homeDir + "Data/vtest.avi"
        task.desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        video.Run()
        dst2 = video.dst1
        bgfg.src = dst2
        bgfg.Run()
        dst1 = bgfg.dst1
    End Sub
End Class






Module BGSubtract_Synthetic_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Open(rgbPtr As IntPtr, rows As Integer, cols As Integer, fgFilename As String, amplitude As Double,
                                              magnitude As Double, wavespeed As Double, objectspeed As Double) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub BGSubtract_Synthetic_Close(synthPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Run(synthPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class BGSubtract_Synthetic_CPP
    Inherits VBparent
    Dim synthPtr As IntPtr
    Dim amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Synthetic Amplitude x100", 1, 400, 200)
            sliders.setupTrackBar(1, "Synthetic Magnitude", 1, 40, 20)
            sliders.setupTrackBar(2, "Synthetic Wavespeed x100", 1, 400, 20)
            sliders.setupTrackBar(3, "Synthetic ObjectSpeed", 1, 20, 15)
        End If
        label1 = "Synthetic background/foreground image."
        task.desc = "Generate a synthetic input to background subtraction method - Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount < 10 Then Exit Sub ' darker images at the start?
        If amplitude <> sliders.trackbar(0).Value Or magnitude <> sliders.trackbar(1).Value Or waveSpeed <> sliders.trackbar(2).Value Or
            objectSpeed <> sliders.trackbar(3).Value Then

            If ocvb.frameCount <> 0 Then BGSubtract_Synthetic_Close(synthPtr)

            amplitude = sliders.trackbar(0).Value
            magnitude = sliders.trackbar(1).Value
            waveSpeed = sliders.trackbar(2).Value
            objectSpeed = sliders.trackbar(3).Value

            Dim srcData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)

            synthPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                ocvb.parms.homeDir + "Data/baboon.jpg",
                                                amplitude / 100, magnitude, waveSpeed / 100, objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(synthPtr)
        If imagePtr <> 0 Then dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        BGSubtract_Synthetic_Close(synthPtr)
    End Sub
End Class






Public Class BGSubtract_Synthetic
    Inherits VBparent
    Dim bgfg As BGSubtract_Basics_CPP
    Dim synth As BGSubtract_Synthetic_CPP
    Public Sub New()
        initParent()
        bgfg = New BGSubtract_Basics_CPP()

        synth = New BGSubtract_Synthetic_CPP()
        task.desc = "Demonstrate background subtraction algorithms with synthetic images - Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        synth.src = src
        synth.Run()
        dst2 = synth.dst1
        bgfg.src = dst2
        bgfg.Run()
        dst1 = bgfg.dst1
    End Sub
End Class

