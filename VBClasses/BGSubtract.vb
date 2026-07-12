Imports System.IO
Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics : Inherits TaskParent
    Implements IDisposable
    Public options As New Options_BGSubtract
    Public Sub New()
        cPtr = BGSubtract_BGFG_Open(options.currMethod)
        desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.optionsChanged Then
            BGSubtract_BGFG_Close(cPtr)
            cPtr = BGSubtract_BGFG_Open(options.currMethod)
        End If

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, options.learnRate)
        handleSrc.Free()

        dst2 = Mat.FromPixelData(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr)
        labels(2) = options.methodDesc
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class





' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class XR_BGSubtract_Basics_QT : Inherits TaskParent
    Implements IDisposable
    Dim learnRate As Double
    Public Sub New()
        Dim learnRate = If(dst2.Width >= 1280, 0.5, 0.1) ' learn faster with large images (slower frame rate)
        cPtr = BGSubtract_BGFG_Open(4) ' MOG2 is the default method when running in QT mode.
        desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
        handleSrc.Free()

        dst2 = Mat.FromPixelData(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr)
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class









Public Class BGSubtract_MOG2 : Inherits TaskParent
    Implements IDisposable
    Dim MOG2 As BackgroundSubtractorMOG2
    Dim options As New Options_BGSubtract
    Public Sub New()
        MOG2 = BackgroundSubtractorMOG2.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If src.Channels() <> 1 Then src = task.gray
        MOG2.Apply(src, dst2, options.learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If MOG2 IsNot Nothing Then MOG2.Dispose()
    End Sub
End Class







Public Class XR_BGSubtract_MOG2_QT : Inherits TaskParent
    Implements IDisposable
    Dim MOG2 As BackgroundSubtractorMOG2
    Public Sub New()
        MOG2 = BackgroundSubtractorMOG2.Create()
        desc = "Subtract background using a mixture of Gaussians - the QT version"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray
        Dim learnRate = If(dst2.Width >= 1280, 0.5, 0.1) ' learn faster with large images (slower frame rate)
        MOG2.Apply(src, dst2, learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If MOG2 IsNot Nothing Then MOG2.Dispose()
    End Sub
End Class







Public Class XR_BGSubtract_MotionDetect : Inherits TaskParent
    Dim options As New Options_MotionDetect
    Public Sub New()
        labels(3) = "Only Motion Added"
        desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

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
                        Dim correlation As New Mat
                        If roi.X + roi.Width > dst3.Width Then roi.Width = dst3.Width - roi.X - 1
                        If roi.Y + roi.Height > dst3.Height Then roi.Height = dst3.Height - roi.Y - 1
                        MatchTemplate(src(roi), dst3(roi), correlation, TemplateMatchModes.CCoeffNormed)
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




Public Class BGSubtract_MOG : Inherits TaskParent
    Implements IDisposable
    Dim MOG As BackgroundSubtractorMOG
    Dim options As New Options_BGSubtract
    Public Sub New()
        MOG = BackgroundSubtractorMOG.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If src.Channels() <> 1 Then src = task.gray
        MOG.Apply(src, dst2, options.learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If MOG IsNot Nothing Then MOG.Dispose()
    End Sub
End Class






Public Class XR_BGSubtract_GMG_KNN : Inherits TaskParent
    Implements IDisposable
    Dim gmg As BackgroundSubtractorGMG
    Dim knn As BackgroundSubtractorKNN
    Dim options As New Options_BGSubtract
    Public Sub New()
        gmg = BackgroundSubtractorGMG.Create()
        knn = BackgroundSubtractorKNN.Create()
        desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If task.frameCount < 120 Then
            SetTrueText("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(task.frameCount))
        Else
            SetTrueText("")
        End If

        gmg.Apply(task.gray, dst2, options.learnRate)
        knn.Apply(dst2, dst2, options.learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If gmg IsNot Nothing Then gmg.Dispose()
        If knn IsNot Nothing Then knn.Dispose()
    End Sub
End Class






Public Class XR_BGSubtract_MOG_RGBDepth : Inherits TaskParent
    Implements IDisposable
    Public grayMat As New Mat
    Dim options As New Options_BGSubtract
    Dim MOGDepth As BackgroundSubtractorMOG
    Dim MOGRGB As BackgroundSubtractorMOG
    Public Sub New()
        MOGDepth = BackgroundSubtractorMOG.Create()
        MOGRGB = BackgroundSubtractorMOG.Create()
        labels = {"", "", "Unstable depth", "Unstable color (if there is motion)"}
        desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        CvtColor(task.depthRGB, grayMat, ColorConversionCodes.BGR2GRAY)
        MOGDepth.Apply(grayMat, grayMat, options.learnRate)
        CvtColor(grayMat, dst2, ColorConversionCodes.GRAY2BGR)

        MOGRGB.Apply(task.gray, dst3, options.learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If MOGDepth IsNot Nothing Then MOGDepth.Dispose()
        If MOGRGB IsNot Nothing Then MOGRGB.Dispose()
    End Sub
End Class





Public Class XR_BGSubtract_DepthOrColorMotion : Inherits TaskParent
    Public motion As New Diff_UnstableDepthAndColor
    Public Sub New()
        desc = "Detect motion with both depth and color changes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)
        dst2 = motion.dst2
        dst3 = motion.dst3
        src.CopyTo(dst3, Not dst2)
        labels(3) = "Image with instability filled with color data"
    End Sub
End Class






Public Class XR_BGSubtract_Video : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Dim video As New Video_Basics
    Public Sub New()
        video.options.fileInfo = New FileInfo(task.homeDir + "opencv/Samples/Data/vtest.avi")
        desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        video.Run(src)
        dst3 = video.dst2
        bgSub.Run(dst3)
        dst2 = bgSub.dst2
    End Sub
End Class









Public Class BGSubtract_Synthetic_CPP : Inherits TaskParent
    Implements IDisposable
    Dim options As New Options_BGSubtractSynthetic
    Public Sub New()
        labels(2) = "Synthetic background/foreground image."
        desc = "Generate a synthetic input to background subtraction method"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If task.optionsChanged Then
            If Not task.firstPass Then BGSubtract_Synthetic_Close(cPtr)

            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)

            cPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                 task.homeDir + "opencv/Samples/Data/baboon.jpg",
                                                 options.amplitude / 100, options.magnitude, options.waveSpeed / 100, options.objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(cPtr)
        If imagePtr <> 0 Then dst2 = Mat.FromPixelData(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = BGSubtract_Synthetic_Close(cPtr)
    End Sub
End Class






Public Class BGSubtract_Synthetic : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Dim synth As New BGSubtract_Synthetic_CPP
    Public Sub New()
        desc = "Demonstrate background subtraction algorithms with synthetic images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        synth.Run(src)
        dst3 = synth.dst2
        bgSub.Run(dst3)
        dst2 = bgSub.dst2
    End Sub
End Class







Public Class XR_BGSubtract_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        desc = "Use BGSubtract with the output of a reduction"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        Dim mm = GetMinMax(reduction.dst2)
        dst2 = Palettize(reduction.dst2)

        bgSub.Run(dst2)
        dst3 = bgSub.dst2.Clone

        labels(3) = "Count nonzero = " + CStr(CountNonZero(dst3))
    End Sub
End Class

