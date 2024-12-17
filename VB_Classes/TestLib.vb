Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Module TestLib
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub TestLibrary_Simple(dataPtr As IntPtr, rows As Integer, cols As Integer)
    End Sub


    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Open() As IntPtr
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("TestLibrary.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function TestLib_Run(cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr,
                                 rows As Integer, cols As Integer) As IntPtr
    End Function

End Module
Public Class TestLib_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Testing the C++ interface in TestLibrary.dll"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim inputData(src.Total * 3 - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        TestLibrary_Simple(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleInput.Free()
    End Sub
End Class





Public Class TestLib_RedCloud : Inherits TaskParent
    Public inputMask As cvb.Mat
    Public classCount As Integer
    Public rectList As New List(Of cvb.Rect)
    Public floodPoints As New List(Of cvb.Point)
    Dim color As Color8U_Basics
    Public Sub New()
        cPtr = TestLib_Open()
        inputMask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Testing RedCloud interface in TestLibrary"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels <> 1 Then
            If color Is Nothing Then color = New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = TestLib_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleMask.Free()
        handleInput.Free()
        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone

        classCount = TestLib_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC4, TestLib_Rects(cPtr))
        Dim floodPointData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC2, TestLib_FloodPoints(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim ptList(classCount * 2) As Integer
        Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length)

        rectList.Clear()
        For i = 0 To rects.Length - 4 Step 4
            rectList.Add(New cvb.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        floodPoints.Clear()
        For i = 0 To ptList.Length - 2 Step 2
            floodPoints.Add(New cvb.Point(ptList(i), ptList(i + 1)))
        Next

        If standalone Then dst3 = ShowPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = TestLib_Close(cPtr)
    End Sub
End Class