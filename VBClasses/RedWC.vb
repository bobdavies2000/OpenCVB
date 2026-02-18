Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports VBClasses.Structures
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWC_Basics : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the absolute coordinates of the World Coordinates."
        End Sub
        Public Shared Function countRegions(input As cv.Mat, ByRef label As String) As List(Of Integer)
            Dim histogram As New Mat
            cv.Cv2.CalcHist({input}, {0}, task.depthmask, histogram, 1, {256}, {New cv.Rangef(0, 256)})
            Dim histArray(255) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            Dim sizeThreshold = input.Total * 0.001 ' ignore regions less than 0.1% - 1/10th of 1%
            Dim lutArray(255) As Byte
            Dim regionList As New List(Of Integer)
            For i = 1 To histArray.Count - 1
                If histArray(i) > sizeThreshold Then regionList.Add(i)
            Next

            label = CStr(regionList.Count) + " non-zero regions more than " + CStr(CInt(sizeThreshold)) + " pixels"
            Return regionList
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst2
            labels(2) = prepData.labels(2)
        End Sub
    End Class






    Public Class RedWC_X : Inherits TaskParent
        Public wcData As New RedWC_Basics
        Public Sub New()
            desc = "Assign world coordinates to each rcList entry."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcData.Run(src)
            dst2 = PaletteBlackZero(wcData.dst2)
        End Sub
    End Class





    Public Class RedWC_Y : Inherits TaskParent
        Public wcData As New RedWC_Basics
        Public Sub New()
            desc = "Assign world coordinates to each rcList entry."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcData.Run(src)
            dst2 = PaletteBlackZero(wcData.dst2)
        End Sub
    End Class







    Public Class RedWC_Validate : Inherits TaskParent
        Public wcData As New RedWC_Basics
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Identify each WC region."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcData.Run(emptyMat)
            dst2 = wcData.dst2

            Dim regionlist = RedWC_Basics.countRegions(dst2, labels(3))
            strOut = ""
            Dim count As Integer
            For i = 0 To regionlist.Count - 1
                Dim index = regionlist(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = wcData.prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod task.fOptions.ReductionSlider.Value = 0 Then
                    Dim region = CInt(mean(0) / task.fOptions.ReductionSlider.Value)
                    strOut += "region " + CStr(region) + " = " + Format(mean(0), fmt0) + "  "
                    If i Mod 3 = 0 And i > 0 Then strOut += vbCrLf
                    count += 1
                End If
            Next
            SetTrueText(strOut, 3)
            labels(2) = CStr(count) + " regions were found"
        End Sub
    End Class






    Public Class RedWC_ValidateX : Inherits TaskParent
        Dim redWC As New RedWC_X
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Identify each WC region."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redWC.Run(emptyMat)
            dst2 = redWC.wcData.dst2

            Dim regionlist = RedWC_Basics.countRegions(dst2, labels(3))
            strOut = ""
            Dim count As Integer
            For i = 0 To regionlist.Count - 1
                Dim index = regionlist(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = redWC.wcData.prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod task.fOptions.ReductionSlider.Value = 0 Then
                    Dim region = CInt(mean(0) / task.fOptions.ReductionSlider.Value)
                    strOut += "region " + CStr(region) + " = " + Format(mean(0), fmt0) + "  "
                    If i Mod 3 = 0 And i > 0 Then strOut += vbCrLf
                    count += 1
                End If
            Next
            SetTrueText(strOut, 3)
            labels(2) = CStr(Count) + " regions were found"
        End Sub
    End Class







    Public Class RedWC_ValidateY : Inherits TaskParent
        Dim redWC As New RedWC_Y
        Public Sub New()
            If standalone Then OptionParent.findRadio("Y Reduction").Checked = True
            desc = "Identify each WC region."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redWC.Run(emptyMat)
            dst2 = redWC.wcData.dst2

            Dim regionlist = RedWC_Basics.countRegions(dst2, labels(3))
            strOut = ""
            Dim count As Integer
            For i = 0 To regionlist.Count - 1
                Dim index = regionlist(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = redWC.wcData.prepData.reduced32s.Mean(dst0)
                If CInt(mean(0)) Mod task.fOptions.ReductionSlider.Value = 0 Then
                    strOut += "region " + CStr(i) + " = " + Format(mean(0), fmt0) + "  "
                    If i Mod 3 = 0 And i > 0 Then strOut += vbCrLf
                    count += 1
                End If
            Next
            SetTrueText(strOut, 3)
            labels(2) = CStr(count) + " regions were found"
        End Sub
    End Class


End Namespace