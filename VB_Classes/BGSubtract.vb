Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics : Inherits VB_Parent
    Public options As New Options_BGSubtract
    Public Sub New()
        cPtr = BGSubtract_BGFG_Open(options.currMethod)
        UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then
            BGSubtract_BGFG_Close(cPtr)
            cPtr = BGSubtract_BGFG_Open(options.currMethod)
        End If

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, options.learnRate)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
        labels(2) = options.methodDesc
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class





' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_QT : Inherits VB_Parent
    Dim learnRate As Double
    Public Sub New()
        Dim learnRate = If(dst2.Width >= 1280, 0.5, 0.1) ' learn faster with large images (slower frame rate)
        cPtr = BGSubtract_BGFG_Open(4) ' MOG2 is the default method when running in QT mode.
        desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class






Public Class BGSubtract_MOG2 : Inherits VB_Parent
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Dim options As New Options_BGSubtract
    Public Sub New()
        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        MOG2.Apply(src, dst2, options.learnRate)
    End Sub
End Class






Public Class BGSubtract_MOG2_QT : Inherits VB_Parent
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New()
        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        desc = "Subtract background using a mixture of Gaussians - the QT version"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        Dim learnRate = If(dst2.Width >= 1280, 0.5, 0.1) ' learn faster with large images (slower frame rate)
        MOG2.Apply(src, dst2, learnRate)
    End Sub
End Class







Public Class BGSubtract_MotionDetect : Inherits VB_Parent
    Dim options As New Options_MotionDetect
    Public Sub New()
        labels(3) = "Only Motion Added"
        desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Or task.frameCount < 10 Then src.CopyTo(dst3)
        Dim threadCount = options.threadData(0)
        Dim width = options.threadData(1), height = options.threadData(2)
        Dim taskArray(threadCount - 1) As System.Threading.Tasks.Task
        Dim xfactor = CInt(src.Width / width)
        Dim yfactor = Math.Max(CInt(src.Height / height), CInt(src.Width / width))
        dst2.SetTo(0)
        Dim motionFound As Boolean
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = System.Threading.Tasks.Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * width, height * Math.Floor(section / yfactor), width, height)
                    Dim correlation As New cv.Mat
                    If roi.X + roi.Width > dst3.Width Then roi.Width = dst3.Width - roi.X - 1
                    If roi.Y + roi.Height > dst3.Height Then roi.Height = dst3.Height - roi.Y - 1
                    cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If options.CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        src(roi).CopyTo(dst2(roi))
                        src(roi).CopyTo(dst3(roi))
                        motionFound = True
                    End If
                End Sub)
        Next
        System.Threading.Tasks.Task.WaitAll(taskArray)
        If motionFound = False Then SetTrueText("No motion detected in any of the regions")
    End Sub
End Class




Public Class BGSubtract_MOG : Inherits VB_Parent
    Dim MOG As cv.BackgroundSubtractorMOG
    Dim options As New Options_BGSubtract
    Public Sub New()
        MOG = cv.BackgroundSubtractorMOG.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        MOG.Apply(src, dst2, options.learnRate)
    End Sub
End Class






Public Class BGSubtract_GMG_KNN : Inherits VB_Parent
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Dim options As New Options_BGSubtract
    Public Sub New()
        gmg = cv.BackgroundSubtractorGMG.Create()
        knn = cv.BackgroundSubtractorKNN.Create()
        desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If task.frameCount < 120 Then
            SetTrueText("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(task.frameCount))
        Else
            SetTrueText("")
        End If

        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        gmg.Apply(dst2, dst2, options.learnRate)
        knn.Apply(dst2, dst2, options.learnRate)
    End Sub
End Class






Public Class BGSubtract_MOG_RGBDepth : Inherits VB_Parent
    Public grayMat As New cv.Mat
    Dim options As New Options_BGSubtract
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New()
        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        labels = {"", "", "Unstable depth", "Unstable color (if there is motion)"}
        desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        grayMat = task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        MOGDepth.Apply(grayMat, grayMat, options.learnRate)
        dst2 = grayMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        MOGRGB.Apply(src, dst3, options.learnRate)
    End Sub
End Class



Public Class BGSubtract_MOG_Retina : Inherits VB_Parent
    Dim bgSub As New BGSubtract_MOG
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        labels = {"", "", "MOG results of depth motion", "Difference from retina depth motion."}
        desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        retina.Run(task.depthRGB)
        bgSub.Run(retina.dst3.Clone())
        dst2 = bgSub.dst2
        cv.Cv2.Subtract(bgSub.dst2, retina.dst3, dst3)
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion : Inherits VB_Parent
    Public motion As New Diff_UnstableDepthAndColor
    Public Sub New()
        desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst2 = motion.dst2
        dst3 = motion.dst3
        Dim mask = dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray).ConvertScaleAbs()
        src.CopyTo(dst3, Not mask)
        labels(3) = "Image with instability filled with color data"
    End Sub
End Class






Public Class BGSubtract_Video : Inherits VB_Parent
    Dim bgSub As New BGSubtract_Basics
    Dim video As New Video_Basics
    Public Sub New()
        video.srcVideo = task.HomeDir + "opencv/Samples/Data/vtest.avi"
        desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        video.Run(src)
        dst3 = video.dst2
        bgSub.Run(dst3)
        dst2 = bgSub.dst2
    End Sub
End Class









Public Class BGSubtract_Synthetic_CPP : Inherits VB_Parent
    Dim options As New Options_BGSubtractSynthetic
    Public Sub New()
        labels(2) = "Synthetic background/foreground image."
        desc = "Generate a synthetic input to background subtraction method"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If task.optionsChanged Then
            If Not task.FirstPass Then BGSubtract_Synthetic_Close(cPtr)

            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)

            cPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                             task.HomeDir + "opencv/Samples/Data/baboon.jpg",
                                             options.amplitude / 100, options.magnitude, options.waveSpeed / 100, options.objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(cPtr)
        If imagePtr <> 0 Then dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_Synthetic_Close(cPtr)
    End Sub
End Class






Public Class BGSubtract_Synthetic : Inherits VB_Parent
    Dim bgSub As New BGSubtract_Basics
    Dim synth As New BGSubtract_Synthetic_CPP
    Public Sub New()
        desc = "Demonstrate background subtraction algorithms with synthetic images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        synth.Run(src)
        dst3 = synth.dst2
        bgSub.Run(dst3)
        dst2 = bgSub.dst2
    End Sub
End Class







Public Class BGSubtract_Reduction : Inherits VB_Parent
    Dim reduction As New Reduction_Basics
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        desc = "Use BGSubtract with the output of a reduction"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        Dim mm = GetMinMax(reduction.dst2)
        dst2 = ShowPalette(reduction.dst2 * 255 / mm.maxval)

        bgSub.Run(dst2)
        dst3 = bgSub.dst2.Clone

        labels(3) = "Count nonzero = " + CStr(dst3.CountNonZero)
    End Sub
End Class

