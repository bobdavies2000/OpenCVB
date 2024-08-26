Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/epiception/SGM-Census
Public Class SemiGlobalMatching_CPP_VB : Inherits VB_Parent
    Dim leftData(0) As Byte
    Dim rightData(0) As Byte
    Public Sub New()
        desc = "Find depth using the semi-global matching algorithm."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.frameCount < 10 Then Exit Sub
        If task.cameraName = "Azure Kinect 4K" Then
            SetTrueText("The left and right views are identical with the Microsoft K4A 4 Azure camera.")
            Exit Sub
        End If

        'If leftData.Length <> src.Total Then
        '    ReDim leftData(src.Total - 1)
        '    ReDim rightData(src.Total - 1)
        '    cPtr = SemiGlobalMatching_Open(src.Rows, src.Cols, 3)
        'End If

        'Marshal.Copy(task.leftView.Data, leftData, 0, leftData.Length)
        'Marshal.Copy(task.rightView.Data, rightData, 0, rightData.Length)

        'Dim handleLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)
        'Dim handleRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)
        'Dim imagePtr = SemiGlobalMatching_Run(cPtr, handleLeft.AddrOfPinnedObject(), handleRight.AddrOfPinnedObject(),
        '                                      task.leftView.Rows, task.leftView.Cols)
        'handleLeft.Free()
        'handleRight.Free()

        'Dim dst2 = New cvb.Mat(task.leftView.Rows, task.leftView.Cols, cvb.MatType.CV_8U, imagePtr)
        SetTrueText("This algorithm runs but always returns zero - a bug in the C++ algorithm as ported?" + vbCrLf +
                    "Needs work but investing further is not needed - we have disparity from the device.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = SemiGlobalMatching_Close(cPtr)
    End Sub
End Class


