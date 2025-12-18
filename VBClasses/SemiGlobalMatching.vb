Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/epiception/SGM-Census
Namespace VBClasses
    Public Class SemiGlobalMatching_CPP : Inherits TaskParent
        Dim leftData(0) As Byte
        Dim rightData(0) As Byte
        Public Sub New()
            desc = "Find depth using the semi-global matching algorithm."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If algTask.frameCount < 10 Then Exit Sub

            'If leftData.Length <> src.Total Then
            '    ReDim leftData(src.Total - 1)
            '    ReDim rightData(src.Total - 1)
            '    cPtr = SemiGlobalMatching_Open(src.Rows, src.Cols, 3)
            'End If

            'Marshal.Copy(algTask.leftView.Data, leftData, 0, leftData.Length)
            'Marshal.Copy(algTask.rightView.Data, rightData, 0, rightData.Length)

            'Dim handleLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)
            'Dim handleRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)
            'Dim imagePtr = SemiGlobalMatching_Run(cPtr, handleLeft.AddrOfPinnedObject(), handleRight.AddrOfPinnedObject(),
            '                                      algTask.leftView.Rows, algTask.leftView.Cols)
            'handleLeft.Free()
            'handleRight.Free()

            'Dim dst2 = New cv.Mat(algTask.leftView.Rows, algTask.leftView.Cols, cv.MatType.CV_8U, imagePtr)
            SetTrueText("This algorithm runs but always returns zero - a bug in the C++ algorithm as ported?" + vbCrLf +
                        "Needs work but investing further is not needed - we have disparity from the device.", 3)
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = SemiGlobalMatching_Close(cPtr)
        End Sub
    End Class
End Namespace