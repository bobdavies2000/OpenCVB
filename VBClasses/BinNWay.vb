Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class BinNWay_Basics : Inherits TaskParent
        Dim options As New Options_BinNWay
        Dim binSplit(0) As Integer
        Public classCount As Integer
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Run RedColor for each gradation from light to dark."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            classCount = options.gradations

            If task.optionsChanged Then
                ReDim binSplit(classCount)
                Dim incr = 255 / classCount
                For i = 0 To binSplit.Length - 1
                    binSplit(i) = i * incr
                Next
                labels(2) = CStr(classCount) + " separate RedColor inputs combined"
            End If

            For i = 0 To classCount - 1
                Dim tmp As New Mat
                InRange(task.gray, binSplit(i), binSplit(i + 1), tmp)
                Threshold(tmp, tmp, 0, 255, ThresholdTypes.Binary)
                dst2.SetTo(i + 1, tmp)
            Next

            dst3 = Palettize(dst2)

            If standalone Then
                Static redC As New RedC_Basics
                redC.Run(dst2)
                labels(2) = redC.labels(2)
                dst2 = redC.dst2

                SetTrueText(redC.strOut, 1)
            End If
            labels(3) = CStr(classCount) + " classes in the motion-adjusted gray image."
        End Sub
    End Class
End Namespace
