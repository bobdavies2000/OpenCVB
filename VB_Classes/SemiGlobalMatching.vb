Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SemiGlobalMatching_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Open(rows As Integer, cols As Integer, disparityRange As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Run(SemiGlobalMatchingPtr As IntPtr, leftPtr As IntPtr, rightPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
End Module





' https://github.com/epiception/SGM-Census
Public Class SemiGlobalMatching_CPP : Inherits VB_Algorithm
    ReadOnly leftData(1 - 1) As Byte
    Dim rightData(1 - 1) As Byte
    Public Sub New()
        desc = "Find depth using the semi-global matching algorithm."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        'If task.frameCount < 10 Then Exit Sub
        'If task.cameraName = "Azure Kinect 4K" Then
        '    setTrueText("The left and right views are identical with the Microsoft K4A 4 Azure camera.")
        '    Exit Sub
        'End If

        'If leftData.Length <> src.Total Then
        '    ReDim leftData(src.Total - 1)
        '    ReDim rightData(src.Total - 1)
        '    cPtr = SemiGlobalMatching_Open(src.Rows, src.Cols, 3)
        'End If

        'Marshal.Copy(task.leftview.Data, leftData, 0, leftData.Length)
        'Marshal.Copy(task.rightview.Data, rightData, 0, rightData.Length)

        'Dim handleLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)
        'Dim handleRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)
        'Dim imagePtr = SemiGlobalMatching_Run(cPtr, handleLeft.AddrOfPinnedObject(), handleRight.AddrOfPinnedObject(),
        '                                      task.leftview.Rows, task.leftview.Cols)
        'handleLeft.Free()
        'handleRight.Free()

        'Dim dst2 = New cv.Mat(task.leftview.Rows, task.leftview.Cols, cv.MatType.CV_8U, imagePtr)
        setTrueText("This algorithm runs but always returns zero - a bug in the C++ algorithm as ported?" + vbCrLf +
                      "Needs work but investing further is not needed - we have disparity from the device.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = SemiGlobalMatching_Close(cPtr)
    End Sub
End Class


