Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_CPP : Inherits VBparent
    Dim bgfs As IntPtr
    Public currMethod As integer = -1
    Public Sub New()
        If radio.Setup(caller, 7) Then
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
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frm = findfrm("BGSubtract_Basics_CPP Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    If task.frameCount > 0 Then BGSubtract_BGFG_Close(bgfs)
                    currMethod = i
                    labels(2) = "Method = " + frm.check(i).Text
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
            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        BGSubtract_BGFG_Close(bgfs)
    End Sub
End Class





Public Class BGSubtract_MotionDetect_MT : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)
        End If

        If radio.Setup(caller, 6) Then
            For i = 0 To radio.check.Length - 1
                radio.check(i).Text = CStr(2 ^ i) + " threads"
            Next
            radio.check(0).Text = "1 thread"
            radio.check(5).Checked = True
        End If

        labels(3) = "Only Motion Added"
        task.desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static correlationSlider = findSlider("Correlation Threshold")
        Static frm = findfrm(caller + " Radio Options")
        Dim threadData As New cv.Vec3i
        Dim width = src.Width, height = src.Height
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, width, height), New cv.Vec3i(2, width / 2, height), New cv.Vec3i(4, width / 2, height / 2),
                                           New cv.Vec3i(8, width / 4, height / 2), New cv.Vec3i(16, width / 4, height / 4), New cv.Vec3i(32, width / 8, height / 4))
                Exit For
            End If
        Next

        If task.frameCount = 0 Then src.CopyTo(dst3)
        Dim threadCount = threadData(0)
        width = threadData(1)
        height = threadData(2)
        Dim taskArray(threadCount - 1) As System.Threading.Tasks.Task
        Dim xfactor = CInt(src.Width / width)
        Dim yfactor = Math.Max(CInt(src.Height / height), CInt(src.Width / width))
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        dst2.SetTo(0)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = System.Threading.Tasks.Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * width, height * Math.Floor(section / yfactor), width, height)
                    Dim correlation As New cv.Mat
                    cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        src(roi).CopyTo(dst2(roi))
                        src(roi).CopyTo(dst3(roi))
                    End If
                End Sub)
        Next
        System.Threading.Tasks.Task.WaitAll(taskArray)
    End Sub
End Class




Public Class BGSubtract_Basics_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public Sub New()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)

        task.desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        grid.RunClass(Nothing)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.frameCount < 5 Then dst3 = src.Clone

        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                End If
            End Sub)
        dst2 = src
        labels(2) = "Motion added to dst3 for " + CStr(updateCount) + " segments out of " + CStr(grid.roiList.Count)
        labels(3) = CStr(grid.roiList.Count - updateCount) + " segments out of " + CStr(grid.roiList.Count) + " had > " +
                         Format(correlationSlider.value / 1000, "0.0%") + " correlation"
    End Sub
End Class






Public Class BGSubtract_Depth_MT : Inherits VBparent
    Dim bgsub As New BGSubtract_Basics_MT
    Public Sub New()
        task.desc = "Detect Motion in the depth image - needs more work"
        labels(2) = "Depth data src"
        labels(3) = "Accumulated depth image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        bgsub.RunClass(task.RGBDepth)
        dst2 = task.RGBDepth
        dst3 = bgsub.dst3
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class



Public Class BGSubtract_MOG : Inherits VBparent
    Dim MOG As cv.BackgroundSubtractorMOG
    Public gray As New cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)
        End If

        MOG = cv.BackgroundSubtractorMOG.Create()
        task.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static learnRateSlider = findSlider("MOG Learn Rate")
        If src.Channels = 3 Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src
        End If
        MOG.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst2 = gray
    End Sub
End Class



Public Class BGSubtract_MOG2 : Inherits VBparent
    Public gray As New cv.Mat
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)
        End If
        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        task.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static learnRateSlider = findSlider("MOG Learn Rate")
        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOG2.Apply(input, dst2, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_GMG_KNN : Inherits VBparent
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Learn Rate", 1, 1000, 1)
        End If

        gmg = cv.BackgroundSubtractorGMG.Create()
        knn = cv.BackgroundSubtractorKNN.Create()
        task.desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static learnRateSlider = findSlider("Learn Rate")
        If task.frameCount < 120 Then
            setTrueText("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(task.frameCount))
        Else
            setTrueText("")
        End If

        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gmg.Apply(dst2, dst2, learnRateSlider.Value / 1000)
        knn.Apply(dst2, dst2, learnRateSlider.Value / 1000)
    End Sub
End Class





Public Class BGSubtract_MOG_RGBDepth : Inherits VBparent
    Public gray As New cv.Mat
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "MOG Learn Rate x1000", 0, 1000, 10)
        End If

        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        labels(2) = "Unstable depth"
        labels(3) = "Unstable color"
        task.desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static learnRateSlider = findSlider("Learn Rate")
        gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGDepth.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGRGB.Apply(input, dst3, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_MOG_Retina : Inherits VBparent
    Dim bgSub As New BGSubtract_MOG
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        findSlider("MOG Learn Rate").Value = 100
        labels(2) = "MOG results of depth motion"
        labels(3) = "Difference from retina depth motion."
        task.desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        retina.RunClass(task.RGBDepth)
        bgSub.RunClass(retina.dst3.Clone())
        dst2 = bgSub.dst2
        cv.Cv2.Subtract(bgSub.dst2, retina.dst3, dst3)
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion : Inherits VBparent
    Public motion As New Diff_UnstableDepthAndColor
    Public Sub New()
        task.desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        motion.RunClass(src)
        dst2 = motion.dst2
        dst3 = motion.dst3
        Dim mask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs()
        cv.Cv2.BitwiseNot(mask, mask)
        src.CopyTo(dst3, mask)
        labels(3) = "Image with instability filled with color data"
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





Public Class BGSubtract_Video : Inherits VBparent
    Dim bgfg As New BGSubtract_Basics_CPP
    Dim video As New Video_Basics
    Public Sub New()
        video.srcVideo = task.parms.homeDir + "Data/vtest.avi"
        task.desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        video.RunClass(src)
        dst3 = video.dst2
        bgfg.RunClass(dst3)
        dst2 = bgfg.dst2
    End Sub
End Class









Public Class BGSubtract_Synthetic_CPP : Inherits VBparent
    Dim synthPtr As IntPtr
    Dim amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Synthetic Amplitude x100", 1, 400, 200)
            sliders.setupTrackBar(1, "Synthetic Magnitude", 1, 40, 20)
            sliders.setupTrackBar(2, "Synthetic Wavespeed x100", 1, 400, 20)
            sliders.setupTrackBar(3, "Synthetic ObjectSpeed", 1, 20, 15)
        End If
        labels(2) = "Synthetic background/foreground image."
        task.desc = "Generate a synthetic input to background subtraction method - Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount < 10 Then Exit Sub ' darker images at the start?
        If amplitude <> sliders.trackbar(0).Value Or magnitude <> sliders.trackbar(1).Value Or waveSpeed <> sliders.trackbar(2).Value Or
            objectSpeed <> sliders.trackbar(3).Value Then

            If task.frameCount <> 0 Then BGSubtract_Synthetic_Close(synthPtr)

            amplitude = sliders.trackbar(0).Value
            magnitude = sliders.trackbar(1).Value
            waveSpeed = sliders.trackbar(2).Value
            objectSpeed = sliders.trackbar(3).Value

            Dim srcData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)

            synthPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                task.parms.homeDir + "Data/baboon.jpg",
                                                amplitude / 100, magnitude, waveSpeed / 100, objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(synthPtr)
        If imagePtr <> 0 Then dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        BGSubtract_Synthetic_Close(synthPtr)
    End Sub
End Class






Public Class BGSubtract_Synthetic : Inherits VBparent
    Dim bgfg As New BGSubtract_Basics_CPP
    Dim synth As New BGSubtract_Synthetic_CPP
    Public Sub New()
        task.desc = "Demonstrate background subtraction algorithms with synthetic images - Painterly"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        synth.RunClass(src)
        dst3 = synth.dst2
        bgfg.RunClass(dst3)
        dst2 = bgfg.dst2
    End Sub
End Class







Public Class BGSubtract_Reduction : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim bgfg As New BGSubtract_Basics_CPP
    Public Sub New()
        task.desc = "Use BGSubtract with the output of a reduction"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)

        task.palette.RunClass(reduction.dst2.Clone)
        dst2 = task.palette.dst2

        bgfg.RunClass(dst2)
        dst3 = bgfg.dst2.Clone

        labels(3) = "Count nonzero = " + CStr(dst3.CountNonZero)
    End Sub
End Class