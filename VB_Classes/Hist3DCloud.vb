Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3Dcloud_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public histogram1D As New cv.Mat
    Public histArray() As Single
    Public classCount As Integer
    Public maskInput As New cv.Mat
    Public simK As New Hist3D_BuildHistogram
    Public Sub New()
        labels(2) = "dst2 = backprojection of pointcloud (8UC1 format)."
        advice = "redOptions '3D Histogram Bins'" + vbCrLf + "redOptions - Histogram Channels"
        desc = "Build a 3D histogram from the pointcloud and backproject it to segment the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim bins = redOptions.HistBinSlider.Value

        cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, {bins, bins, bins}, redOptions.rangesCloud)

        ReDim histArray(redOptions.bins3D - 1)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim threshold = src.Total * 0.001
        For i = 0 To histArray.Count - 1
            If histArray(i) > threshold Then Exit For
            histArray(i) = 0
        Next
        histogram = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, histArray)

        simK.Run(histogram)
        histogram = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, simK.histArray)
        classCount = simK.classCount

        cv.Cv2.CalcBackProject({src}, {2}, histogram, dst2, {redOptions.rangesCloud(redOptions.rangesCloud.Count - 1)})
        dst2 = dst2.ConvertScaleAbs

        dst2.SetTo(0, task.noDepthMask)
        dst2.SetTo(classCount, task.maxDepthMask)
        dst3 = vbPalette(dst2 * 255 / classCount)

        labels(2) = simK.labels(2) + " with " + CStr(redOptions.bins3D) + " histogram bins"
        labels(3) = "LastClassCount/classCount = " + CStr(classCount) + "/" + CStr(classCount)
    End Sub
End Class








Public Class Hist3Dcloud_DepthSplit : Inherits VB_Algorithm
    Dim hist As List(Of Histogram_Kalman)
    Dim hist2d As List(Of Histogram2D_Cloud)
    Dim mats1 As New Mat_4Click
    Dim mats2 As New Mat_4Click
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        hist = New List(Of Histogram_Kalman)({New Histogram_Kalman, New Histogram_Kalman, New Histogram_Kalman})
        hist2d = New List(Of Histogram2D_Cloud)({New Histogram2D_Cloud, New Histogram2D_Cloud, New Histogram2D_Cloud})
        labels(2) = "Histograms (Kalman) for X (upper left), Y (upper right) and Z.  UseZeroDepth removes 0 (no depth) entries."
        labels(3) = "X to Y histogram (upper left), X to Z (upper right), and Y to Z (bottom)."
        advice = "redOptions '3D Histogram Bins'" + vbCrLf + "redOptions - Histogram Channels"
        desc = "Plot the 3 histograms of the depth data dimensions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For i = 0 To 2
            hist(i).Run(task.pcSplit(i))
            mats1.mat(i) = hist(i).dst2.Clone

            If i = 0 Then redOptions.channels = {0, 1}
            If i = 1 Then redOptions.channels = {0, 2}
            If i = 2 Then redOptions.channels = {1, 2}
            hist2d(i).Run(task.pointCloud)
            mats2.mat(i) = hist2d(i).histogram.ConvertScaleAbs
        Next

        mats1.Run(empty)
        dst2 = mats1.dst2
        dst3 = mats1.mat(mats1.quadrant)

        mats2.Run(empty)
        dst1 = mats2.dst2
    End Sub
End Class







Public Class Hist3Dcloud_Highlights : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        advice = "redOptions '3D Histogram Bins'" + vbCrLf + "redOptions - Histogram Channels"
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = redOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3Dcloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins,
                                     rx.Item(0), ry.Item(0), rz.Item(0),
                                     rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        histogram = New cv.Mat(redOptions.bins3D, 1, cv.MatType.CV_32F, dstPtr)

        ranges = New cv.Rangef() {New cv.Rangef(rx(0), rx(1)), New cv.Rangef(ry(0), ry(1)), New cv.Rangef(rz(0), rz(1))}

        Dim samples(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, samples, 0, samples.Length)

        Dim sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)

        For i = 0 To samples.Count - 1
            sortedHist.Add(samples(i), i)
        Next

        For i = 0 To sortedHist.Count - 1
            Dim key = sortedHist.ElementAt(i)
            Dim val = key.Value
            samples(val) = i + 1
        Next

        Marshal.Copy(samples, 0, histogram.Data, samples.Length)
        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        Static maskval As Integer
        If heartBeat() Then maskval += 1

        If sortedHist.ElementAt(maskval).Key = 0 Then maskval = 0
        Dim index = sortedHist.ElementAt(maskval).Value

        dst3 = dst2.InRange(index, index)
        labels(3) = "There were " + CStr(sortedHist.ElementAt(maskval).Key) + " samples in bin " + CStr(index)
    End Sub
End Class





Public Class Hist3Dcloud_BP_Filter : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Dim options As New Options_HistXD
    Public Sub New()
        redOptions.HistBinSlider.Value = 16
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        labels(2) = "Mask of the pointcloud image after backprojection that removes 'blowback' pixels"
        advice = "redOptions '3D Histogram Bins'" + vbCrLf + "redOptions - Histogram Channels"
        desc = "Backproject a 3D pointcloud histogram after thresholding the bins with the small samples."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim bins = redOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim histInput(src.Total * 3 - 1) As Single
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim imagePtr = BackProjectCloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins, options.threshold3D,
                                            rx.Item(0), ry.Item(0), rz.Item(0),
                                            rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        dst2 = New cv.Mat(dst2.Height, dst2.Width, cv.MatType.CV_8U, imagePtr)
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, dst2)
    End Sub
End Class








Public Class Hist3Dcloud_PlotHist1D : Inherits VB_Algorithm
    Dim hcloud As New Hist3Dcloud_Basics
    Dim plot As New Plot_Histogram
    Public histogram As cv.Mat
    Public histArray() As Single
    Dim simK As New Hist3D_BuildHistogram
    Public Sub New()
        plot.removeZeroEntry = False
        labels(2) = "The 3D histogram of the pointcloud data stream - note the number of gaps"
        advice = "redOptions '3D Histogram Bins'" + vbCrLf + "redOptions - Histogram Channels"
        desc = "Present the 3D histogram as a typical histogram bar chart."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        hcloud.Run(src)
        ReDim histArray(hcloud.histogram.Total - 1)
        Marshal.Copy(hcloud.histogram.Data, histArray, 0, histArray.Length)

        histogram = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, histArray)
        plot.Run(histogram)
        dst2 = plot.dst2

        simK.Run(histogram)
        labels(3) = simK.labels(2)
    End Sub
End Class
