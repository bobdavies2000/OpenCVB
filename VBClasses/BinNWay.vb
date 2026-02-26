Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class BinNWay_Basics : Inherits TaskParent
        Dim options As New Options_BinNWay
        Dim binSplit(0) As Integer
        Dim redC As New RedColor_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Run RedColor for each gradation from light to dark."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.optionsChanged Then
                ReDim binSplit(options.gradations)
                Dim incr = 255 / options.gradations
                For i = 0 To binSplit.Count - 1
                    binSplit(i) = i * incr
                Next
                labels(2) = CStr(options.gradations) + " separate RedColor inputs combined"
            End If

            For i = 0 To options.gradations - 1
                Dim tmp = task.grayStable.InRange(binSplit(i), binSplit(i + 1))
                tmp = tmp.Threshold(0, 255, cv.ThresholdTypes.Binary)
                dst1.SetTo(i + 1, tmp)
            Next

            dst3 = PaletteFull(dst1)

            If standalone Then
                redC.Run(dst1)
                labels(2) = redC.labels(2)
                dst2 = redC.dst2

                strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
                SetTrueText(strOut, 1)
            End If
            labels(3) = CStr(options.gradations) + " of the motion-adjusted gray image."
        End Sub
    End Class
End Namespace