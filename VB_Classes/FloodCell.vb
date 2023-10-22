Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class FloodCell_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Public sizes As New List(Of Integer)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public inputMask As cv.Mat
    Public Sub New()
        cPtr = FloodCell_Open()
        If standalone Then gOptions.PixelDiffThreshold.Value = 0
        gOptions.HistBinSlider.Value = 20 ' adjust histogram bins lower if regions are > 255
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static guided As New GuidedBP_Depth
            guided.Run(src)
            src = guided.backProject
        End If

        Dim handlemask As GCHandle
        Dim maskPtr As IntPtr
        If inputMask IsNot Nothing Then
            Dim MaskData(inputMask.Total - 1) As Byte
            handlemask = GCHandle.Alloc(MaskData, GCHandleType.Pinned)
            Marshal.Copy(inputMask.Data, MaskData, 0, MaskData.Length)
            maskPtr = handlemask.AddrOfPinnedObject()
        End If

        Dim inputData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), maskPtr, src.Rows, src.Cols, src.Type,
                                     task.minPixels, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()
        If maskPtr <> 0 Then handlemask.Free()

        dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        classCount = FloodCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " regions found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FloodCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FloodCell_Rects(cPtr))
        Dim inputRects As New List(Of cv.Rect)
        Dim inputSizes As New List(Of Integer)
        For i = 0 To classCount - 2
            inputRects.Add(rectData.Get(Of cv.Rect)(i, 0))
            inputSizes.Add(sizeData.Get(Of Integer)(i, 0))
        Next

        rects.Clear()
        masks.Clear()
        sizes.Clear()
        For i = 0 To inputRects.Count - 1
            If inputSizes(i) = 0 Then Continue For
            Dim r = validateRect(inputRects(i))
            rects.Add(r)
            sizes.Add(inputSizes(i))
            Dim mask = dst3(r).InRange(i + 1, i + 1)
            masks.Add(mask)
        Next

        dst2 = vbPalette(dst3 * 255 / classCount)
        If standalone Then dst2.SetTo(0, task.noDepthMask)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class






Module GuidedBP_Cell_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Run(
                cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer,
                type As Integer, minPixels As Integer, diff As Integer) As IntPtr
    End Function
End Module






Public Class FloodCell_Color : Inherits VB_Algorithm
    Dim fCell As New FloodCell_Basics
    Public Sub New()
        desc = "Floodfill an image so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static reduction As New Reduction_Basics
        reduction.Run(src)
        fCell.Run(reduction.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
    End Sub
End Class
