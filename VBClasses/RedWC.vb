Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWC_Basics : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the absolute coordinates of the World Coordinates."
        End Sub
        Public Shared Function countRegions(input As cv.Mat, ByRef label As String) As List(Of Integer)
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({input}, {0}, task.depthmask, histogram, 1, {256}, {New cv.Rangef(0, 256)})
            Dim histArray() As Single = Nothing
            histogram.GetArray(Of Single)(histArray)

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
        Public regionList As New List(Of Integer)
        Public wcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
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
            wcMap.SetTo(0)
            For i = 0 To regionlist.Count - 1
                Dim index = regionlist(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = wcData.prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod task.fOptions.ReductionSlider.Value = 0 Then
                    Dim region = CInt(mean(0) / task.fOptions.ReductionSlider.Value)
                    wcMap.SetTo(region, dst0)
                    If region = 0 Then
                        strOut += vbCrLf + If(i Mod 3 = 2 Or i Mod 3 = 1, vbCrLf, "")
                        strOut += "region " + Format(region, "00") + " mean " + Format(mean(0), fmt0) + vbCrLf
                        strOut += vbCrLf
                    Else
                        strOut += "region " + Format(region, "00") + " mean " + Format(mean(0), fmt0) + vbTab
                        If i Mod 3 = 2 Then strOut += vbCrLf
                    End If
                    count += 1
                End If
            Next
            SetTrueText(strOut, 3)
            labels(2) = CStr(count) + " regions were found"
        End Sub
    End Class






    Public Class RedWC_ValidateX : Inherits TaskParent
        Dim wcValidate As New RedWC_Validate
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Identify each WC region."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcValidate.Run(emptyMat)
            dst2 = wcValidate.wcData.dst2
            labels(2) = wcValidate.labels(2)

            SetTrueText(wcValidate.strOut, 3)
        End Sub
    End Class




    Public Class RedWC_ValidateY : Inherits TaskParent
        Dim wcValidate As New RedWC_Validate
        Public Sub New()
            If standalone Then OptionParent.findRadio("Y Reduction").Checked = True
            desc = "Identify each WC region."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcValidate.Run(emptyMat)
            dst2 = wcValidate.wcData.dst2
            labels(2) = wcValidate.labels(2)

            SetTrueText(wcValidate.strOut, 3)
        End Sub
    End Class





    Public Class RedWC_RedCloud : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim wcData As New RedWC_Basics
        Dim wcMapX As cv.Mat
        Dim wcMapY As cv.Mat
        Public rcList As New List(Of rcData)
        Public Sub New()
            desc = "Assign world coordinates to each RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static reductionX = OptionParent.findRadio("X Reduction")
            Static reductionY = OptionParent.findRadio("Y Reduction")

            reductionX.checked = True
            wcData.Run(emptyMat)
            wcMapX = wcData.dst2.Clone

            reductionY.checked = True
            wcData.Run(emptyMat)
            wcMapY = wcData.dst2.Clone

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            rcList.Clear()
            Dim reduction = task.fOptions.ReductionSlider.Value
            For Each rc In redC.rcList
                Dim x = wcMapY.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                Dim y = wcMapX.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.region = New cv.Point(CInt(x / reduction), CInt(y / reduction))
                rcList.Add(rc)
            Next

            If standalone Then
                Dim region As Integer = Math.Abs(task.gOptions.DebugSlider.Value)
                labels(2) = "The highlighted cells are in X region " + CStr(region)
                dst3.SetTo(0)
                For Each rc In rcList
                    If rc.region.X = region Then
                        dst2(rc.rect).SetTo(white, rc.mask)
                        dst3(rc.rect).SetTo(white, rc.mask)
                    End If
                Next
            End If
        End Sub
    End Class

End Namespace