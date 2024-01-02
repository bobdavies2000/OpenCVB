Imports cv = OpenCvSharp
Public Class Background_RedMin : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Dim hist3D As New Hist3D_DepthTier
    Public minCells As New List(Of rcPrep)
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "Output of Hist3D_DepthTier, input to RedMin_Basics"
        advice = "redOptions '3D Histogram Bins' " + vbCrLf + "redOptions other 'Histogram 3D Options'"
        desc = "Run the foreground through RedCloud_Basics "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3D.Run(src)
        dst3 = hist3D.dst3

        rMin.minCore.inputMask = hist3D.dst0
        rMin.Run(hist3D.dst2)
        dst2 = rMin.dst3

        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)

        labels(2) = rMin.labels(3)
    End Sub
End Class