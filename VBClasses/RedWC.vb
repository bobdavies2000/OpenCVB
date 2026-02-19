Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWC_Basics : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public wcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Public regionList As New List(Of Integer)
        Public Sub New()
            If standalone Then OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the absolute coordinates of the World Coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst2

            Dim reduction = task.fOptions.ReductionSlider.Value
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({dst2}, {0}, task.depthmask, histogram, 1, {256}, {New cv.Rangef(0, 256)})
            Dim histArray() As Single = Nothing
            histogram.GetArray(Of Single)(histArray)

            Dim sizeThreshold = dst2.Total * 0.001 ' ignore regions less than 0.1% - 1/10th of 1%
            Dim lutArray(255) As Byte
            regionList.Clear()
            For i = 1 To histArray.Count - 1
                If histArray(i) > sizeThreshold Then regionList.Add(i)
            Next

            labels(2) = CStr(regionList.Count) + " non-zero regions > " + CStr(CInt(sizeThreshold)) + " pixels"
            Dim count As Integer
            wcMap.SetTo(0)
            For i = 0 To regionList.Count - 1
                Dim index = regionList(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod reduction = 0 Then
                    Dim region = CInt(mean(0) / reduction)
                    wcMap.SetTo(region, dst0)
                    count += 1
                End If
            Next

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

            regionList = wcData.regionList
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
        Dim wcDataX As New RedWC_Basics
        Dim wcDataY As New RedWC_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            desc = "Assign world coordinates to each RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static reductionX = OptionParent.findRadio("X Reduction")
            Static reductionY = OptionParent.findRadio("Y Reduction")

            reductionX.checked = True
            wcDataX.Run(emptyMat)

            reductionY.checked = True
            wcDataY.Run(emptyMat)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            rcList.Clear()
            Dim reduction = task.fOptions.ReductionSlider.Value
            For Each rc In redC.rcList
                ' NOTE: X and Y are flipped here but for good reason.
                Dim x = wcDataY.wcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                Dim y = wcDataX.wcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
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