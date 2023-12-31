Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3D_Basics : Inherits VB_Algorithm
    Dim hColor As New Hist3Dcolor_Basics
    Dim hCloud As New Hist3Dcloud_Basics
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "Sum of 8UC1 outputs of Hist3Dcolor_Basics and Hist3Dcloud_basics", ""}
        advice = "Primary: redOptions '3D Histogram Bins' "
        desc = "Build an 8UC1 image by adding Hist3Dcolor_Basics and Hist3Dcloud_Basics output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hColor.Run(src)
        hCloud.Run(src)

        hCloud.dst2 += hColor.classCount + 1
        dst2 = hColor.dst2 + hCloud.dst2
        classCount = hColor.classCount + hCloud.classCount

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(3) = CStr(classCount) + " classes "
    End Sub
End Class






Public Class Hist3D_BasicsCopy : Inherits VB_Algorithm
    Dim hColor As New Hist3Dcolor_Basics
    Dim hCloud As New Hist3Dcloud_Basics
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "Merge of 8UC1 outputs of Hist3Dcolor_Basics and Hist3Dcloud_basics", ""}
        advice = "Primary: redOptions '3D Histogram Bins' "
        desc = "Build an 8UC1 image by adding Hist3Dcolor_Basics and Hist3Dcloud_Basics output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst2

        hCloud.Run(src)
        hCloud.dst2 += hColor.classCount + 1

        hCloud.dst2.CopyTo(dst2, task.depthMask)
        classCount = hColor.classCount + hCloud.classCount + 1

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(3) = CStr(classCount) + " classes "
    End Sub
End Class






Public Class Hist3D_BuildHistogram : Inherits VB_Algorithm
    Public threshold As Integer
    Public classCount As Integer
    Public histArray() As Single
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins'" + vbCrLf
        desc = "Build a simulated (guided) 3D histogram from the 3D histogram supplied in src."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then
            Static plot1D As New Hist3Dcloud_PlotHist1D
            plot1D.Run(src)
            src = plot1D.histogram
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
        advice = "Primary: redOptions '3D Histogram Bins' "
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