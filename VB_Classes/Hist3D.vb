Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3D_Basics : Inherits VB_Algorithm
    Dim hColor As New Hist3Dcolor_Basics
    Dim hCloud As New Hist3Dcloud_Basics
    Public classCount As Integer
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Add color and cloud 8UC1")
            radio.addRadio("Copy cloud into color 8UC1")
            radio.check(0).Checked = True
        End If

        labels = {"", "", "Sum of 8UC1 outputs of Hist3Dcolor_Basics and Hist3Dcloud_basics", ""}
        advice = "redOptions '3D Histogram Bins' "
        desc = "Build an 8UC1 image by adding Hist3Dcolor_Basics and Hist3Dcloud_Basics output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static addRadio = findRadio("Add color and cloud 8UC1")
        Dim addCloud = addRadio.checked

        hColor.Run(src)
        dst2 = hColor.dst2

        hCloud.Run(src)
        hCloud.dst2 += hColor.classCount + 1
        hCloud.dst2.SetTo(0, task.noDepthMask)

        If addCloud Then dst2 += hCloud.dst2 Else hCloud.dst2.CopyTo(dst2, task.depthMask)
        classCount = hColor.classCount + hCloud.classCount

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(3) = CStr(classCount) + " classes "
    End Sub
End Class







Public Class Hist3D_BuildHistogram : Inherits VB_Algorithm
    Public threshold As Integer
    Public classCount As Integer
    Public histArray() As Single
    Public Sub New()
        advice = "redOptions '3D Histogram Bins'" + vbCrLf
        desc = "Build a guided 3D histogram from the 3D histogram supplied in src."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            gOptions.HistBinSlider.Value = 100
            Static plot As New Histogram_Depth
            plot.Run(src)
            src = plot.histogram
        End If

        ReDim histArray(src.Total - 1)
        Marshal.Copy(src.Data, histArray, 0, histArray.Length)

        classCount = 1
        Dim index As Integer
        For i = index To histArray.Count - 1
            For index = index To histArray.Count - 1
                If histArray(index) > threshold Then Exit For
                histArray(index) = classCount
            Next
            classCount += 1
            For index = index To histArray.Count - 1
                If histArray(index) <= threshold Then Exit For
                histArray(index) = classCount
            Next

            If index >= histArray.Count Then Exit For
        Next

        Dim minClass = histArray.Min - 1
        If minClass <> 0 Then
            src -= minClass
            For i = 0 To histArray.Count - 1
                histArray(i) -= minClass
            Next
            classCount -= minClass
        End If
        dst2 = src.Clone
        Marshal.Copy(histArray, 0, dst2.Data, histArray.Length)
        labels(2) = "Histogram entries vary from " + CStr(histArray.Min) + " to " + CStr(classCount) + " inclusive"
    End Sub
End Class








Public Class Hist3D_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim hist3D As New Hist3D_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels = {"", "", "Grayscale", "dst3Label"}
        advice = "redOptions '3D Histogram Bins' "
        desc = "Run RedCloud_Basics on the combined Hist3D color/cloud output."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3D.Run(src)
        dst2 = hist3D.dst3
        labels(2) = hist3D.labels(3)

        redC.Run(hist3D.dst2)
        dst3 = redC.dst2
        labels(3) = redC.labels(2)
    End Sub
End Class








Public Class Hist3D_RedMin : Inherits VB_Algorithm
    Dim rMin As New RedColor_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        advice = "redOptions '3D Histogram Bins' "
        desc = "Use the Hist3D color classes to segment the image with RedColor_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hColor.Run(src)
        dst3 = hColor.dst3
        labels(3) = hColor.labels(3)

        rMin.Run(hColor.dst2)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class







Public Class Hist3D_DepthWithMask : Inherits VB_Algorithm
    Dim hColor As New Hist3Dcolor_Basics
    Public depthMask As New cv.Mat
    Public Sub New()
        advice = ""
        desc = "Isolate the foreground and no depth in the image and run it through Hist3D_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or showIntermediate() Then
            Static fore As New Foreground_KMeans2
            fore.Run(src)
            depthMask = fore.dst2 Or task.noDepthMask
        End If
        hColor.inputMask = depthMask
        dst0 = Not depthMask

        src.SetTo(0, dst0)

        hColor.Run(src)
        dst2 = hColor.dst2
        dst2.SetTo(0, dst0)

        dst3 = hColor.dst3
        dst3.SetTo(0, dst0)
        labels = hColor.labels
    End Sub
End Class






Public Class Hist3D_Pixel : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public histArray() As Single
    Public classCount As Integer
    Public Sub New()
        advice = "redOptions '3D Histogram Bins' "
        desc = "Classify each pixel using a 3D histogram backprojection."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 3 Then src = task.color
        Dim bins = redOptions.HistBinSlider.Value
        cv.Cv2.CalcHist({src}, {0, 1, 2}, New cv.Mat, histogram, 3, {bins, bins, bins}, redOptions.rangesBGR)

        ReDim histArray(histogram.Total - 1)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        For i = 0 To histArray.Count - 1
            histArray(i) = i + 1
        Next

        classCount = redOptions.bins3D
        Marshal.Copy(histArray, 0, histogram.Data, histArray.Length)

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)
        dst3 = If(classCount < 256, vbPalette(dst2 * 255 / classCount), vbPalette(dst2))
    End Sub
End Class








Public Class Hist3D_PixelCells : Inherits VB_Algorithm
    Dim pixel As New Hist3D_Pixel
    Dim rMin As New RedColor_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        advice = ""
        desc = "After classifying each pixel, backproject each RedMin cell using the same 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst2
        labels(2) = rMin.labels(3)

        pixel.Run(src)

        dst0.SetTo(0)
        For Each cell In rMin.minCells
            cv.Cv2.CalcBackProject({src(cell.rect)}, {0, 1, 2}, pixel.histogram, dst1(cell.rect), redOptions.rangesBGR)
            dst1(cell.rect).CopyTo(dst0(cell.rect), cell.mask)
        Next

        dst3 = vbPalette(dst0 * 255 / redOptions.bins3D)
    End Sub
End Class








Public Class Hist3D_PixelClassify : Inherits VB_Algorithm
    Dim pixel As New Hist3D_Pixel
    Dim rMin As New RedColor_Basics
    Public Sub New()
        advice = ""
        desc = "Classify each pixel with a 3D histogram backprojection and run RedColor_Basics on the output."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pixel.Run(src)

        rMin.Run(pixel.dst2)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class






Public Class Hist3D_PixelDiffMask : Inherits VB_Algorithm
    Dim pixel As New Hist3D_Pixel
    Dim rMin As New RedColor_Basics
    Public Sub New()
        advice = ""
        desc = "Build better image segmentation - remove unstable pixels from 3D color histogram backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pixel.Run(src)
        Static lastImage As cv.Mat = pixel.dst2.Clone
        cv.Cv2.Absdiff(lastImage, pixel.dst2, dst3)
        dst2 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastImage = pixel.dst2.Clone

        rMin.minCore.inputMask = dst2
        rMin.Run(pixel.dst2)
        dst3 = rMin.dst3.Clone
        labels(3) = rMin.labels(3)
    End Sub
End Class






Public Class Hist3D_RedMinGrid : Inherits VB_Algorithm
    Dim rMin As New RedMin_PixelVectors
    Dim hVector As New Hist3Dcolor_Vector
    Public Sub New()
        gOptions.GridSize.Value = 8
        advice = ""
        desc = "Build RedMin pixel vectors and then measure each grid element's distance to those vectors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst2
        dst3 = dst2.InRange(0, 0)
        If rMin.pixelVector.Count = 0 Then Exit Sub
        dst1.SetTo(0)
        dst0 = rMin.rMin.dst2
        For Each roi In task.gridList
            If dst3(roi).CountNonZero Then
                Dim candidates As New List(Of Integer)
                For y = 0 To roi.Height - 1
                    For x = 0 To roi.Width - 1
                        Dim val = dst0(roi).Get(Of Byte)(y, x)
                        If val = 0 Then Continue For
                        If candidates.Contains(val) = False Then candidates.Add(val)
                    Next
                Next
                If candidates.Count > 1 Then
                    hVector.inputMask = dst3(roi)
                    hVector.Run(src(roi))
                    Dim distances As New List(Of Double)
                    For Each index In candidates
                        Dim vec = rMin.pixelVector(index - 1)
                        distances.Add(distanceN(vec, hVector.histArray))
                    Next
                    Dim cell = rMin.minCells(candidates(distances.IndexOf(distances.Min)) - 1)
                    dst1(roi).SetTo(cell.color, dst3(roi))
                    dst2(roi).SetTo(cell.color, dst3(roi))
                ElseIf candidates.Count = 1 Then
                    Dim cell = rMin.minCells(candidates(0) - 1)
                    dst1(roi).SetTo(cell.color, dst3(roi))
                    dst2(roi).SetTo(cell.color, dst3(roi))
                End If
            End If
        Next
    End Sub
End Class
