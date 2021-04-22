Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject_Basics : Inherits VBparent
    Public hist As Histogram_Basics
    Public Sub New()
        hist = New Histogram_Basics
        label1 = "Move mouse to backproject each histogram column"
        task.desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub Run(src As cv.Mat)
        hist.Run(src)
        dst1 = hist.dst1

        Dim barWidth = dst1.Width / task.histogramBins
        Dim barRange = 255 / task.histogramBins
        Dim histIndex = Math.Floor(task.mousePoint.X / barWidth)

        Dim minRange = If(histIndex = task.histogramBins, 255 - barRange, histIndex * barRange)
        Dim maxRange = If(histIndex = task.histogramBins, 255, (histIndex + 1) * barRange)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
        dst2 = src
        If maxRange = 255 Then dst2.SetTo(cv.Scalar.Black, mask) Else dst2.SetTo(cv.Scalar.White, mask)
        Dim count = hist.histogram.Get(Of Single)(histIndex, 0)
        label2 = "Backprojecting " + CStr(CInt(minRange)) + " to " + CStr(CInt(maxRange)) + " with " +
                 Format(count, "#0") + " (" + Format(count / dst1.Total, "0.0%") + ") samples"
        dst1.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst1.Height), cv.Scalar.Yellow, task.lineSize)
    End Sub
End Class







Public Class BackProject_Full : Inherits VBparent
    Public hist As Histogram_Basics
    Public Sub New()
        hist = New Histogram_Basics
        label1 = "Move mouse to backproject each histogram column"
        task.desc = "Backproject the entire histogram."
    End Sub
    Public Sub Run(src As cv.Mat)
        hist.Run(src)
        dst1 = hist.dst1

        Dim barWidth = dst1.Width / task.histogramBins
        Dim barRange = 255 / task.histogramBins
        Dim histIndex = Math.Floor(task.mousePoint.X / barWidth)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
    End Sub
End Class








Public Class BackProject_Masks : Inherits VBparent
    Dim lines As Line_Basics
    Dim hist As Histogram_Basics
    Public Sub New()
        hist = New Histogram_Basics
        lines = New Line_Basics
        task.desc = "Create all the backprojection masks from a histogram"
    End Sub
    Public Function maskLineDetect(gray As cv.Mat, histogram As cv.Mat, histIndex As Integer) As List(Of cv.Vec4f)
        Dim barWidth = dst1.Width / histogram.Rows
        Dim barRange = 255 / histogram.Rows

        Dim minRange = If(histIndex = histogram.Rows, 255 - barRange, histIndex * barRange)
        Dim maxRange = If(histIndex = histogram.Rows, 255, (histIndex + 1) * barRange)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim mask = New cv.Mat
        cv.Cv2.CalcBackProject({gray}, {0}, histogram, mask, ranges)
        lines.Run(mask)
        Dim masklines As New List(Of cv.Vec4f)
        For i = 0 To lines.sortlines.Count - 1
            masklines.Add(lines.sortlines.ElementAt(i).Value)
        Next
        Return masklines
    End Function
    Public Sub Run(src As cv.Mat)
        hist.Run(src)
        dst1 = hist.dst1

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim allLines As New List(Of cv.Vec4f)
        For i = 0 To task.histogramBins - 1
            Dim masklines = maskLineDetect(gray, hist.histogram, i)
            For j = 0 To masklines.Count - 1
                allLines.Add(masklines(j))
            Next
        Next
        dst2 = src
        For i = 0 To allLines.Count - 1
            Dim v = allLines(i)
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst2.Line(pt1, pt2, cv.Scalar.Yellow, task.lineSize, task.lineType)
        Next
    End Sub
End Class







Public Class BackProject_MasksLines : Inherits VBparent
    Dim lines As BackProject_Masks
    Dim hist As Histogram_Basics
    Public Sub New()
        hist = New Histogram_Basics
        lines = New BackProject_Masks
        task.desc = "Inspect the lines from individual backprojection masks from a histogram"
    End Sub
    Public Sub Run(src As cv.Mat)
        hist.Run(src)
        dst1 = hist.dst1

        Dim barWidth = dst1.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mousePoint.X / barWidth)

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim allLines = lines.maskLineDetect(gray, hist.histogram, histIndex)
        dst2 = src
        For i = 0 To allLines.Count - 1
            Dim v = allLines(i)
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst2.Line(pt1, pt2, cv.Scalar.Yellow, task.lineSize, task.lineType)
        Next
        dst1.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst1.Height), cv.Scalar.Yellow, task.lineSize)
    End Sub
End Class











Public Class BackProject_Surfaces : Inherits VBparent
    Public pcValid As Motion_MinMaxPointCloud
    Dim hist As Histogram_Basics
    Dim mats As Mat_2to1
    Public Sub New()
        mats = New Mat_2to1
        hist = New Histogram_Basics
        pcValid = New Motion_MinMaxPointCloud

        label1 = "Top=differences in X, Bot=differences in Y"
        task.desc = "Find solid surfaces using the pointcloud X and Y differences"
    End Sub
    Public Sub Run(src As cv.Mat)
        pcValid.Run(src)
        Dim mask = pcValid.dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)

        Dim split = pcValid.dst2.Split()
        Dim xDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)
        Dim yDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)

        Dim r1 = New cv.Rect(0, 0, dst1.Width - 1, dst1.Height - 1)
        Dim r2 = New cv.Rect(1, 1, dst1.Width - 1, dst1.Height - 1)

        cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1))

        xDiff.SetTo(0, mask)
        yDiff.SetTo(0, mask)

        Dim xMat = xDiff.ConvertScaleAbs(255)

        hist.Run(xMat)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(1, 2)}
        cv.Cv2.CalcBackProject({xMat}, {0}, hist.histogram, mats.mat(0), ranges)

        Dim yMat = yDiff.ConvertScaleAbs(255)
        hist.Run(yMat)
        cv.Cv2.CalcBackProject({yMat}, {0}, hist.histogram, mats.mat(1), ranges)

        mats.Run(Nothing)
        dst1 = mats.dst1

        cv.Cv2.BitwiseOr(mats.mat(0), mats.mat(1), dst2)
        label2 = "Likely smooth surfaces " + CStr(task.frameCount)
    End Sub
End Class