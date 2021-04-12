
Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module HMM_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub HMM_Close(HMMPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Run(HMMPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer, channels As integer) As IntPtr
    End Function
End Module



'https://github.com/omidsakhi/cv-hmm
Public Class HMM_Example_CPP
    Inherits VBparent
    Dim HMM As IntPtr = 0
    Public Sub New()
        initParent()
        If task.parms.testAllRunning = False Then HMM = HMM_Open()
        label1 = "Text output with explanation will appear in the Visual Studio output."
        task.desc = "Simple test of Hidden Markov Model - text output"
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.parms.testAllRunning Then
            task.trueText("When HMM_Example_CPP is run repeatedly as part of a 'Test All', it can run out of OpenCL memory.")
            Exit Sub
        End If
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = HMM_Run(HMM, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, IIf(src.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), imagePtr)
    End Sub
    Public Sub Close()
        If HMM <> 0 Then HMM_Close(HMM)
    End Sub
End Class



