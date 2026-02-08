Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
'https://github.com/omidsakhi/cv-hmm
Namespace VBClasses
    Public Class NR_HMM_Example_CPP : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            If tsk.testAllRunning = False Then cPtr = HMM_Open()
            labels(2) = "Text output with explanation will appear in the Visual Studio output."
            desc = "Simple test of Hidden Markov Model - text output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.testAllRunning Then
                SetTrueText("When NR_HMM_Example_CPP is run repeatedly as part of a 'Test All', it can run out of OpenCL memory.")
                Exit Sub
            End If
            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = HMM_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
            handleSrc.Free()

            SetTrueText("Review the 'Visual Studio's Immediate Window' output to see the sharedResults.images..", 3)
            If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, IIf(src.Channels() = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), imagePtr).Clone
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = HMM_Close(cPtr)
        End Sub
    End Class
End Namespace