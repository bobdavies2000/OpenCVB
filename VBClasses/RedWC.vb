Imports System.Windows
Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWC_Basics : Inherits TaskParent
        Public indexer As New Indexer_Basics
        Public redC As New RedCloud_FloodFill
        Dim element As New cv.Mat
        Public Sub New()
            element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(3, 3))
            desc = "Assign abstract world coordinates to each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            indexer.Run(src)
            dst1 = indexer.dst3.Dilate(element, Nothing, 1)

            redC.Run(dst1)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            dst2.SetTo(0, dst1)
            redC.rcMap.SetTo(0, dst1)

            strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            If strOut <> "" Then SetTrueText(strOut, 3) Else SetTrueText("Click on any cell", 3)
        End Sub
    End Class







    Public Class RedWC_BasicsOld : Inherits TaskParent
        Public redC As New RedCloud_FloodFill
        Public indexer As New Indexer_Basics
        Dim wcDataX As New RedWC_Core
        Dim wcDataY As New RedWC_Core
        Public rcList As New List(Of rcData)
        Public rcTranslate As New List(Of cv.Point)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Assign world coordinates to each RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            wcDataX.prepData.presetReductionName = "X Reduction"
            wcDataX.Run(emptyMat)
            strOut = CStr(wcDataX.regionList.Count) + " non-zero vertical regions" + vbCrLf

            indexer.Run(wcDataX.dst2)
            dst1 = indexer.dst2
            dst1.SetTo(0, indexer.dst3)

            wcDataX.prepData.presetReductionName = "Y Reduction"
            wcDataY.Run(emptyMat)
            strOut += CStr(wcDataY.regionList.Count) + " non-zero horizontal regions" + vbCrLf

            indexer.Run(wcDataY.dst2)
            dst1 = indexer.dst2
            dst1.SetTo(0, indexer.dst3)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            dst2.SetTo(0, indexer.dst3)

            rcList.Clear()
            For Each rc In redC.rcList
                Dim x = wcDataX.wcMap.Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                Dim y = wcDataY.wcMap.Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                rc.wc = New cv.Point(CInt(x), CInt(y))
                rcList.Add(rc)
            Next

            rcTranslate.Clear()
            For Each rc In rcList
                rcTranslate.Add(rc.wc)
            Next

            If standaloneTest() Then
                Dim index = redC.rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
                If index > 0 And index < rcList.Count Then
                    Dim rcClick = rcList(index - 1)
                    strOut += rcClick.displayCell()
                    dst2(rcClick.rect).SetTo(white, rcClick.mask)
                    dst3.SetTo(0)
                    dst3(rcClick.rect).SetTo(white, rcClick.mask)
                End If
            End If
            If standaloneTest() Then dst2.SetTo(0, task.noDepthMask)
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class RedWC_Core : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public wcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Public regionList As New List(Of Integer)
        Public Sub New()
            desc = "Prepare the absolute world coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst2

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
            wcMap.SetTo(255)
            For i = 0 To regionList.Count - 1
                Dim index = regionList(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod task.reduction = 0 Then
                    Dim region = CInt(mean(0) / task.reduction)
                    wcMap.SetTo(region, dst0)
                End If
            Next

            dst3 = PaletteBlackZero(wcMap)
            dst3.SetTo(0, task.noDepthMask)
        End Sub
    End Class








    Public Class RedWC_Validate : Inherits TaskParent
        Public wcData As New RedWC_Core
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
            For i = 0 To regionList.Count - 1
                Dim index = regionList(i)
                dst0 = dst2.InRange(index, index)
                Dim mean = wcData.prepData.reduced32s.Mean(dst0)

                If CInt(mean(0)) Mod task.reduction = 0 Then
                    Dim region = CInt(mean(0) / task.reduction)
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





    Public Class RedWC_Neighbors : Inherits TaskParent
        Dim redWC As New RedWC_BasicsOld
        Public Sub New()
            task.gOptions.displayDst1.Checked = True
            desc = "Identify the neighbors of the cell that was clicked"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redWC.Run(src)
            dst2 = redWC.dst2
            labels(2) = redWC.labels(2)

            Dim index = redWC.redC.rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            dst3.SetTo(0)
            If index > 0 Then
                Dim rcCenter = redWC.rcList(index - 1)
                labels(3) = "The highlighted cell neighbors for rc = " + CStr(rcCenter.index)
                SetTrueText(rcCenter.displayCell, 1)
                For y = -1 To 1
                    For x = -1 To 1
                        Dim region = New cv.Point(rcCenter.wc.X + x, rcCenter.wc.Y + y)
                        index = redWC.rcTranslate.IndexOf(region)
                        If index >= 0 Then
                            Dim rc = redWC.rcList(index)
                            dst2(rc.rect).SetTo(white, rc.mask)
                            dst3(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                Next
            End If
        End Sub
    End Class
End Namespace