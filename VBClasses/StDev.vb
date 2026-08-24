Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class StDev_Highest : Inherits TaskParent
        Public Sub New()
            If standalone Then setLargeGridSize()
            dst3 = New Mat(dst3.Size, MatType.CV_32F, 0)
            desc = "What is the Stdev for each cell "
        End Sub
        Public Shared Sub setLargeGridSize()
            Dim val As Integer = task.workRes.Width / 10
            If task.gOptions.GridSlider.Maximum < val Then task.gOptions.GridSlider.Maximum = val
            task.gOptions.GridSlider.Value = task.workRes.Width \ 10
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            dst3.SetTo(0)
            Dim stDevs As New List(Of Double)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            For Each roi In task.gridRects
                If roi.Width <> roi.Height Then Continue For
                MeanStdDev(src(roi), mean, stdev, Nothing)
                stDevs.Add(stdev(0))
                dst3(roi).SetTo(stdev)

                If task.gridWH > 16 Then SetTrueText(stdev(0).ToString("#0.0"), roi.TopLeft, 2)
            Next

            ConvertScaleAbs(dst3, dst2, 255 / (stDevs.Max - stDevs.Min), stDevs.Min)
            dst2 = ShowAddweighted(src, dst2, labels(1))

            labels(2) = "Lighter = higher stdev. Range: " + stDevs.Max.ToString("0.0") + " to " + stDevs.Min.ToString("0.0")
        End Sub
    End Class




    Public Class StDev_Range : Inherits TaskParent
        Public Sub New()
            If standalone Then setLargeGridSize()
            dst3 = New Mat(dst3.Size, MatType.CV_32F, 0)
            labels(2) = "Values are the cell range - (max - min)"
            desc = "What is the range for each cell"
        End Sub
        Public Shared Sub setLargeGridSize()
            Dim val As Integer = task.workRes.Width / 10
            If task.gOptions.GridSlider.Maximum < val Then task.gOptions.GridSlider.Maximum = val
            task.gOptions.GridSlider.Value = task.workRes.Width \ 10
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            dst3.SetTo(0)
            Dim ranges As New List(Of Double)
            For Each roi In task.gridRects
                If roi.Width <> roi.Height Then Continue For
                Dim mm = getMinMaxDrawRect(src(roi))
                ranges.Add(mm.range)
                dst3(roi).SetTo(mm.range)

                If task.gridWH > 16 Then SetTrueText(mm.range.ToString("#0"), roi.TopLeft, 2)
            Next

            ConvertScaleAbs(dst3, dst2, 255 / (ranges.Max - ranges.Min), ranges.Min)
            dst2 = src
            dst2.SetTo(white, task.gridMask)
            labels(3) = "Lighter = higher Range. Range: " + ranges.Max.ToString("0.0") + " to " + ranges.Min.ToString("0.0")
        End Sub
    End Class
End Namespace