Imports cv = OpenCvSharp
'https://security.stackexchange.com/questions/42428/Is-generating-random-numbers-using-a-smartphone-camera-a-good-idea
Public Class Photon_Basics : Inherits VB_Parent
    Dim hist As New Hist_Basics
    Public Sub New()
        labels = {"", "", "Points where B, G, or R differ from the previous image", "Histogram showing distribution of absolute value of differences"}
        desc = "With no motion the camera values will show the random photon differences.  Are they random?"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static lastImage As cv.Mat = src
        cv.Cv2.Absdiff(src, lastImage, dst1)

        dst0 = dst1.Reshape(1, dst1.Rows * 3)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        If dst0.CountNonZero > 0 Then
            dst2 = dst1.Clone
            hist.Run(dst0)
            dst3 = hist.dst2
        End If

        lastImage = src
    End Sub
End Class









Public Class Photon_Test : Inherits VB_Parent
    Dim reduction As New Reduction_Basics
    Dim counts(4 - 1) As List(Of Integer)
    Dim mats As New Mat_4to1
    Public Sub New()
        If standaloneTest() Then task.gOptions.displayDst1.Checked = True
        For i = 0 To counts.Count - 1
            counts(i) = New List(Of Integer)
        Next
        labels = {"", "", "5 color levels from reduction (black not shown)", "Selected distribution"}
        desc = ""
    End Sub
    Public Sub RunVB(src as cv.Mat)
        task.redOptions.SimpleReductionSlider.Value = 64 ' for now...
        Dim reduce = 64

        reduction.Run(src)
        dst1 = reduction.dst2

        Dim testCount = dst2.Width - 1
        strOut = ""
        For i = 0 To counts.Length - 1
            mats.mat(i) = dst1.InRange(reduce * i, reduce * i)
            counts(i).Add(mats.mat(i).CountNonZero)
            If counts(i).Count > testCount Then counts(i).RemoveAt(0)
            strout += "for " + CStr(i * reduce) + " average = " + Format(counts(i).Average, "###,##0") + " min = " + Format(counts(i).Min, "###,##0.0") + " max = " +
                      Format(counts(i).Max, "###,##0.0") + vbCrLf
        Next
        setTrueText(strout, 3)
        mats.Run(empty)
        dst2 = mats.dst2

        Dim colWidth = dst2.Width / testCount
        dst3.SetTo(0)
        For i = 0 To counts(0).Count - 1
            Dim colTop = 0
            For j = 0 To counts.Length - 1
                Dim h = CInt((dst2.Height - 1) * (counts(j)(i) / dst2.Total)) ' extra parens to avoid overflow at high res.
                Dim r = New cv.Rect(colWidth * i, colTop, colWidth, h)
                If h > 0 Then dst3(r).SetTo(Choose(j + 1, cv.Scalar.Red, cv.Scalar.LightGreen, cv.Scalar.Blue, cv.Scalar.Yellow))
                colTop += h
            Next
        Next
    End Sub
End Class










'https://security.stackexchange.com/questions/42428/Is-generating-random-numbers-using-a-smartphone-camera-a-good-idea
Public Class Photon_Subtraction : Inherits VB_Parent
    Dim hist As New Hist_Basics
    Public Sub New()
        labels = {"", "", "Points where B, G, or R differ", "Histogram showing distribution of differences"}
        desc = "Same as Photon_Basics but without ignoring sign."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        src = src.Reshape(1, src.Rows * 3)
        src.ConvertTo(src, cv.MatType.CV_32F)

        Static lastImage As cv.Mat = src
        Dim subOutput As New cv.Mat
        cv.Cv2.Subtract(src, lastImage, subOutput)
        Dim histInput = subOutput.Add(cv.Scalar.All(100)).ToMat

        hist.Run(histInput)
        dst2 = hist.dst2

        subOutput = subOutput.Reshape(3, dst2.Height)
        dst1 = subOutput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        If dst1.CountNonZero Then dst3 = dst1.Clone ' occasionally the image returned is identical to the last.  hmmm...
        lastImage = src
    End Sub
End Class