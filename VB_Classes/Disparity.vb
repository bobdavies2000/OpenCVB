Imports  cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Disparity_Basics : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "8-bit disparity", "32-bit float disparity"}
        desc = "Get the 8-bit representation of disparity"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.parms.cameraName <> ActiveTask.algParms.camNames.OakDCamera Then
            dst3 = task.depth32f.Normalize(0, task.histogramBins, cv.NormTypes.MinMax)
            dst3.ConvertTo(dst2, cv.MatType.CV_8U)
        Else
            dst2 = task.disparity
        End If
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = "8-bit disparity ranging up to " + CStr(CInt(vbMinMax(dst2).maxVal))
    End Sub
End Class



Public Class Disparity_Histogram : Inherits VB_Algorithm
    Public backP As New BackProject_Image
    Public dDisp As New Disparity_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.checked = true
        backP.hist.noZeroEntry = True
        backP.useInrange = True
        desc = "Create a histogram using the disparity image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dDisp.Run(Nothing)
        src = dDisp.dst2.Clone

        backP.Run(src)
        dst2 = backP.dst2
        dst1 = backP.dst3
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.White, backP.mask)
        dst1.SetTo(255, backP.mask)
        labels = backP.labels
        labels(1) = "Disparity with selected column highlighted"
    End Sub
End Class





Public Class Disparity_HistogramKeyboard : Inherits VB_Algorithm
    Dim dispHist As New Disparity_Histogram
    Dim keys As New Keyboard_Basics
    Public Sub New()
        desc = "Get keyboard input to the disparity histogram display"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        keys.Run(src)
        Dim keyIn = New List(Of String)(keys.keyInput)
        Dim incrX = dst1.Width / task.histogramBins

        If keyIn.Count Then
            task.mouseMovePointUpdated = True
            For i = 0 To keyIn.Count - 1
                Select Case keyIn(i)
                    Case "Left"
                        task.mouseMovePoint.X -= incrX
                    Case "Right"
                        task.mouseMovePoint.X += incrX
                End Select
            Next
        End If

        dispHist.Run(src)
        dst2 = dispHist.dst2
        dst3 = dispHist.dst3

        ' this is intended to provide a natural behavior for the left and right arrow keys.  The Keyboard_Basics Keyboard Options text box must be active.
        If task.frameCount = 30 Then
            Dim hwnd = FindWindow(Nothing, "OpenCVB Algorithm Options")
            SetForegroundWindow(hwnd)
        End If
        labels = dispHist.labels
    End Sub
End Class