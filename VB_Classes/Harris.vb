Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module Harris_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Harris_Features_Close(Harris_FeaturesPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As integer, cols As integer, threshold As Single,
                                        neighborhood As Int16, aperture As Int16, HarrisParm As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Harris_Detector_Close(Harris_FeaturesPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As integer, cols As integer, qualityLevel As Double,
                                        count As IntPtr) As IntPtr
    End Function
End Module



' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Harris_Features_CPP : Inherits VBparent
    Dim srcData() As Byte
    Dim Harris_Features As IntPtr
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 5)
            sliders.setupTrackBar(0, "Harris Threshold", 1, 100, 1)
            sliders.setupTrackBar(1, "Harris Neighborhood", 1, 41, 21)
            sliders.setupTrackBar(2, "Harris aperture", 1, 31, 21)
            sliders.setupTrackBar(3, "Harris Parameter", 1, 100, 1)
            sliders.setupTrackBar(4, "Weight for dst1 X100", 1, 100, 50)
        End If
        task.desc = "Use Harris feature detectors to identify interesting points."

        ReDim srcData(dst1.Total - 1)
        Harris_Features = Harris_Features_Open()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim threshold = sliders.trackbar(0).Value / 10000
        Dim neighborhood = sliders.trackbar(1).Value
        If neighborhood Mod 2 = 0 Then neighborhood += 1
        Dim aperture = sliders.trackbar(2).Value
        If aperture Mod 2 = 0 Then aperture += 1
        Dim HarrisParm = sliders.trackbar(3).Value / 100
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(Harris_Features, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, threshold,
                                           neighborhood, aperture, HarrisParm)
        handleSrc.Free() ' free the pinned memory...

        Dim gray32f = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)
        gray32f.ConvertTo(dst1, cv.MatType.CV_8U)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim weight = sliders.trackbar(4).Value / 100
        cv.Cv2.AddWeighted(dst1, weight, task.color, 1 - weight, 0, dst2)
        label2 = "RGB overlaid with Harris result. Weight = " + Format(weight, "0%")
    End Sub
    Public Sub Close()
        Harris_Features_Close(Harris_Features)
    End Sub
End Class




' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Harris_Detector_CPP : Inherits VBparent
    Dim srcData() As Byte
    Dim ptCount(1) As integer
    Dim Harris_Detector As IntPtr
    Public FeaturePoints As New List(Of cv.Point2f)
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Harris qualityLevel", 1, 100, 2)
        End If
        task.desc = "Use Harris detector to identify interesting points."

        ReDim srcData(dst1.Total - 1)
        Harris_Detector = Harris_Detector_Open()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim qualityLevel = sliders.trackbar(0).Value / 100

        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim handleCount = GCHandle.Alloc(ptCount, GCHandleType.Pinned)
        Dim ptPtr = Harris_Detector_Run(Harris_Detector, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, qualityLevel, handleCount.AddrOfPinnedObject())
        handleSrc.Free()
        handleCount.Free()
        If ptCount(0) > 1 And ptPtr <> 0 Then
            Dim pts((ptCount(0) - 1) * 2 - 1) As integer
            Marshal.Copy(ptPtr, pts, 0, ptCount(0))
            Dim ptMat = New cv.Mat(ptCount(0), 2, cv.MatType.CV_32S, pts)
            If standalone or task.intermediateReview = caller Then src.CopyTo(dst1)
            FeaturePoints.Clear()
            For i = 0 To ptMat.Rows - 1
                FeaturePoints.Add(New cv.Point2f(ptMat.Get(of integer)(i, 0), ptMat.Get(of integer)(i, 1)))
                If standalone or task.intermediateReview = caller Then dst1.Circle(FeaturePoints(i), 3, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If
    End Sub
    Public Sub Close()
        Harris_Detector_Close(Harris_Detector)
    End Sub
End Class



