Imports cv = OpenCvSharp
Imports System.IO
Imports OpenCvSharp
Imports System.Runtime.InteropServices

Public Class MyFrameSource
    Inherits cv.FrameSource
    Public origFrame_ As New cv.Mat
    Public Sub New(fs As cv.FrameSource)

    End Sub
    Public Sub SetFrame(frame As cv.Mat)
        frame.CopyTo(origFrame_)
    End Sub
    Public Overrides Sub NextFrame(frame As OutputArray)
        frame = origFrame_.Clone()
    End Sub

    Public Overrides Sub Reset()
        Throw New NotImplementedException()
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/gpu/super_resolution.cpp
' https://www.csharpcodi.com/vs2/?source=4752/opencvsharp_samples/SamplesCS/Samples/SuperResolutionSample.cs
Public Class SuperRes_Basics : Implements IDisposable
    Dim myfs As MyFrameSource
    Dim fs As cv.FrameSource
    Public Sub New(ocvb As AlgorithmData)
        fs = cv.FrameSource.CreateFrameSource_Empty()
        myfs = New MyFrameSource(fs)
        ocvb.desc = "Enhance resolution with SuperRes API in OpenCV"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color.Clone()
        'fs.NextFrame(src)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        fs.Dispose()
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/gpu/super_resolution.cpp
' https://www.csharpcodi.com/vs2/?source=4752/opencvsharp_samples/SamplesCS/Samples/SuperResolutionSample.cs
Public Class SuperRes_Video : Implements IDisposable
    Public videoOptions As New OptionsVideoName
    Dim fs As cv.FrameSource
    Dim sr As cv.SuperResolution
    Public Sub New(ocvb As AlgorithmData)
        videoOptions.fileinfo = New FileInfo(task.parms.HomeDir + "Data/CarsDrivingUnderBridge.mp4")
        videoOptions.Show()
        fs = cv.FrameSource.CreateFrameSource_Video(videoOptions.fileinfo.FullName)
        fs.NextFrame(ocvb.result2) ' skip the first frame.
        sr = cv.SuperResolution.CreateBTVL1()
        sr.SetInput(fs)
        ocvb.desc = "Enhance resolution with SuperRes API in OpenCV"
        ocvb.label1 = "Original Video"
        ocvb.label2 = "SuperRes video"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = videoOptions.nextImage()
        Dim normalFrame As New cv.Mat
        Dim srFrame As New cv.Mat
        'Dim test = New FileStorage(task.parms.HomeDir + "Data/test.txt", FileStorage.Mode.Write)
        'sr.Write(test)
        'sr.NextFrame(ocvb.result2)
        'fs.NextFrame(normalFrame)
        'If normalFrame.Empty() Then Exit Sub
        'sr.NextFrame(srFrame)
        'cv.Cv2.ImShow("srFrame", srFrame)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        videoOptions.Dispose()
        fs.Dispose()
        sr.Dispose()
    End Sub
End Class




Module SuperRes_CPP_Module
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperRes_Open() As IntPtr
    End Function
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SuperRes_Close(SuperResPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperRes_Run(SuperResPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module


Public Class SuperRes_CPP : Implements IDisposable
    Dim SuperRes As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        SuperRes = SuperRes_Open()
        ocvb.desc = "description of class"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color
        Dim srcData(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = SuperRes_Run(SuperRes, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(src.Rows, src.Cols, IIf(src.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        SuperRes_Close(SuperRes)
    End Sub
End Class


