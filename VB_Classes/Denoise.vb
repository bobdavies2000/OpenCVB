Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Denoise_Basics_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Basics_Open(frameCount As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Denoise_Basics_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Denoise_Basics_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module







Public Class Denoise_Basics_CPP : Inherits VBparent
    Dim cPtr As IntPtr
    Public Sub New()
        cPtr = Denoise_Basics_Open(5)
        labels(2) = "Resized input image - very small"
        labels(3) = "Output image - very small"
        task.desc = "Denoise example is not working properly.  Full size images take a really long time.  Not sure what the problem is with the output."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Resize(New cv.Size(src.Width / 8, src.Height / 8))
        Dim srcData(dst2.Total * dst2.ElemSize) As Byte
        Marshal.Copy(dst2.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Denoise_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, dst2.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(dst2.Total * dst2.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst3 = New cv.Mat(dst2.Rows, dst2.Cols, If(dst2.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), dstData)
        End If
    End Sub
    Public Sub Close()
        Denoise_Basics_Close(cPtr)
    End Sub
End Class
