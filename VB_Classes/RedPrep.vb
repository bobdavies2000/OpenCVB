Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedPrep_Basics : Inherits TaskParent
    Public Sub New()
        cPtr = PrepXY_Open()
        desc = "Run the C++ PrepXY to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim inputX(task.pcSplit(0).Total * task.pcSplit(0).ElemSize - 1) As Byte
        Dim inputY(task.pcSplit(1).Total * task.pcSplit(1).ElemSize - 1) As Byte

        Marshal.Copy(task.pcSplit(0).Data, inputX, 0, inputX.Length)
        Marshal.Copy(task.pcSplit(1).Data, inputY, 0, inputY.Length)

        Dim handleX = GCHandle.Alloc(inputX, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(inputY, GCHandleType.Pinned)

        Dim imagePtr = PrepXY_Run(cPtr, handleX.AddrOfPinnedObject(), handleY.AddrOfPinnedObject(), src.Rows, src.Cols,
                                  task.xRange, task.yRange, task.histogramBins)
        handleX.Free()
        handleY.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst2.SetTo(0, task.noDepthMask)

        dst3 = ShowPalette(dst2)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = PrepXY_Close(cPtr)
    End Sub
End Class







Public Class RedPrep_VB : Inherits TaskParent
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim ranges As cv.Rangef(), zeroCount As Integer
        For i = 0 To 1
            Select Case i
                Case 0 ' X Reduction
                    dst1 = task.pcSplit(0)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange)}
                Case 1 ' Y Reduction
                    dst1 = task.pcSplit(1)
                    ranges = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange)}
            End Select

            cv.Cv2.CalcHist({dst1}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For j = 0 To histArray.Count - 1
                If histArray(j) = 0 Then zeroCount += 1
                histArray(j) = j
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst1}, {0}, histogram, dst1, ranges)

            If i = 0 Then dst3 = dst1.Clone Else dst3 += dst1
        Next

        dst3.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2.SetTo(0, task.noDepthMask)

        labels(2) = CStr(task.histogramBins * 2 - zeroCount) + " depth regions mapped (control with histogram bins.)"
    End Sub
End Class






Public Class RedPrep_Edges : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Find the edges of XY depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3

        edges.Run(dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = edges.dst2
        labels(2) = edges.labels(2)
    End Sub
End Class






Public Class RedPrep_DepthTiers : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim tiers As New Depth_Tiers
    Dim contours As New Contour_Basics
    Public Sub New()
        labels(3) = "RedPrep_Basics output define regions with common XY."
        desc = "Find the edges of XY depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst3
        dst1 = prep.dst2

        tiers.Run(src)
        dst1 += tiers.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = ShowPalette(dst1)
    End Sub
End Class