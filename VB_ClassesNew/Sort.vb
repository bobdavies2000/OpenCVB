Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Sort_Basics : Inherits TaskParent
    Dim options As New Options_Sort
    Public Sub New()
        desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        If options.radio5.Checked Then
            src = src.Reshape(1, src.Rows * src.Cols)
            options.sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Descending
        End If
        If options.radio4.Checked Then
            src = src.Reshape(1, src.Rows * src.Cols)
            options.sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Ascending
        End If
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Sort(options.sortOption)
        If options.radio4.Checked Or options.radio5.Checked Then dst2 = dst2.Reshape(1, dst0.Rows)
    End Sub
End Class







Public Class Sort_RectAndMask : Inherits TaskParent
    Dim sort As New Sort_Basics
    Public mask As cv.Mat
    Public rect As cv.Rect
    Public Sub New()
        labels(3) = "Original input to sort"
        If standalone Then task.drawRect = New cv.Rect(10, 10, 50, 5)
        desc = "Sort the grayscale image portion in a rect while allowing for a mask."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim tmpRect = If(rect = New cv.Rect, task.drawRect, rect)
        dst1 = src(tmpRect).Clone
        If mask IsNot Nothing Then
            mask = mask.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            dst1.SetTo(0, mask)
        End If
        sort.Run(dst1)
        dst2 = sort.dst2.Reshape(1, dst1.Rows)
        dst2 = dst2.Resize(dst3.Size)
        If standaloneTest() Then dst3 = src(tmpRect).Resize(dst3.Size)
    End Sub
End Class





Public Class Sort_MLPrepTest_CPP : Inherits TaskParent
    Public reduction As New Reduction_Basics
    Public MLTestData As New cv.Mat
    Public Sub New()
        cPtr = Sort_MLPrepTest_Open()
        desc = "Prepare the grayscale image and row to predict depth"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        reduction.Run(src)

        Dim dataSrc(reduction.dst2.Total * reduction.dst2.ElemSize) As Byte
        Marshal.Copy(reduction.dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Sort_MLPrepTest_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        MLTestData = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32FC2, imagePtr).Clone
        Dim split = MLTestData.Split()
        dst2 = split(0)
        dst3 = split(1)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Sort_MLPrepTest_Close(cPtr)
    End Sub
End Class










Public Class Sort_1Channel : Inherits TaskParent
    Dim sort As New Sort_Basics
    Dim dups As New ML_RemoveDups_CPP
    Public rangeStart As New List(Of Integer)
    Public rangeEnd As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        OptionParent.findRadio("Sort all pixels descending").Checked = True
        If standalone Then task.gOptions.GridSlider.Value = 10
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "Mask used to isolate the gray scale input to sort", "Sorted thresholded data", "Output of sort - no duplicates"}
        desc = "Take some 1-channel input, sort it, and provide the list of unique elements"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Static thresholdSlider =OptionParent.FindSlider("Threshold for sort input")

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)
        sort.Run(dst2)

        Dim pixelsPerBlock = CInt(dst3.Total / dst2.Rows)
        Dim sq = Math.Sqrt(pixelsPerBlock)
        task.gOptions.GridSlider.Value = CInt(Math.Min(sq, 10))

        dst0 = sort.dst2.Reshape(1, dst2.Rows)

        dups.Run(dst0)
        dst3.SetTo(255)
        Dim inputCount = dups.dst3.CountNonZero
        Dim testVals As New List(Of Integer)
        For i = 0 To Math.Min(inputCount, task.gridRects.Count) - 1
            Dim roi = task.gridRects(i)
            Dim val = CInt(dups.dst3.Get(Of Byte)(0, i))
            testVals.Add(val)
            dst3(roi).SetTo(val)
        Next

        If testVals.Count = 0 Then Exit Sub

        rangeStart.Clear()
        rangeEnd.Clear()
        rangeStart.Add(testVals(0))
        For i = 0 To testVals.Count - 2
            If Math.Abs(testVals(i) - testVals(i + 1)) > 1 Then
                rangeEnd.Add(testVals(i))
                rangeStart.Add(testVals(i + 1))
            End If
        Next
        rangeEnd.Add(testVals(testVals.Count - 1))
        labels(3) = " The number of unique entries = " + CStr(inputCount) + " were spread across " + CStr(rangeStart.Count) + " ranges"
    End Sub
End Class






Public Class Sort_3Channel : Inherits TaskParent
    Dim sort As New Sort_Basics
    Dim dups As New ML_RemoveDups_CPP
    Dim bgra As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        OptionParent.findRadio("Sort all pixels descending").Checked = True
        labels = {"", "The BGRA input to sort - shown here as 1-channel CV_32S format", "Output of sort - no duplicates", "Input before removing the dups - use slider to increase/decrease the amount of data"}
        desc = "Take some 3-channel input, convert it to BGRA, sort it as integers, and provide the list of unique elements"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Static thresholdSlider =OptionParent.FindSlider("Threshold for sort input")
        Dim inputMask = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standaloneTest() Then inputMask = inputMask.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)

        bgra = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA)
        dst1 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_32S, bgra.Data)

        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32S, 0)
        dst1.CopyTo(dst0, inputMask)
        sort.Run(dst0)
        dst2 = sort.dst2.Reshape(1, dst2.Rows)
        Dim tmp = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC4, dst2.Data)
        dst3 = tmp.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        'dups.Run(dst2)
        'dst2 = dups.dst2
    End Sub
End Class











Public Class Sort_Integer : Inherits TaskParent
    Dim sort As New Sort_Basics
    Public data(dst2.Total - 1) As Integer
    Public vecList As New List(Of Integer)
    Public Sub New()
        OptionParent.findRadio("Sort all pixels ascending").Checked = True
        labels = {"", "Mask used to isolate the gray scale input to sort", "Sorted thresholded data", "Output of sort - no duplicates"}
        desc = "Take some 1-channel input, sort it, and provide the list of unique elements"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            Dim split = src.Split()
            Dim zero As New cv.Mat(split(0).Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            cv.Cv2.Merge({split(0), split(1), split(2), zero}, src)
            Marshal.Copy(src.Data, data, 0, data.Length)
            src = New cv.Mat(src.Size(), cv.MatType.CV_32S, 0)
            Marshal.Copy(data, 0, src.Data, data.Length)
        End If

        sort.Run(src)
        Marshal.Copy(sort.dst2.Data, data, 0, data.Length)

        vecList.Clear()
        vecList.Add(data(0))
        For i = 1 To data.Count - 1
            If data(i - 1) <> data(i) Then vecList.Add(data(i))
        Next
        labels(2) = "There were " + CStr(vecList.Count) + " unique 8UC3 pixels in the input."
    End Sub
End Class






Public Class Sort_GrayScale1 : Inherits TaskParent
    Dim sort As New Sort_Integer
    Dim pixels(2)() As Byte
    Public Sub New()
        desc = "Sort the grayscale image but keep the 8uc3 pixels with each gray entry."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray(src.Total - 1) As Byte
        Marshal.Copy(dst1.Data, gray, 0, gray.Length)

        Dim split = src.Split()
        For i = 0 To 2
            If task.firstPass Then ReDim pixels(i)(src.Total - 1)
            Marshal.Copy(split(i).Data, pixels(i), 0, pixels(i).Length)
        Next

        Dim input(gray.Count - 1) As UInteger
        For i = 0 To gray.Length - 1
            input(i) = pixels(0)(i) * 65536 + pixels(1)(i) * 256 + pixels(2)(i)
        Next

        sort.Run(cv.Mat.FromPixelData(gray.Length, 1, cv.MatType.CV_32S, input))

        Dim sorted(gray.Length - 1) As UInteger

        Dim unique As New List(Of UInteger)
        unique.Add(sort.data(0))
        For i = 1 To sort.data.Count - 1
            If sort.data(i - 1) <> sort.data(i) Then unique.Add(sort.data(i))
        Next
        labels(2) = "There were " + CStr(unique.Count) + " distinct pixels in the image."
    End Sub
End Class






Public Class Sort_GrayScale : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Dim pixels(2)() As Byte
    Public Sub New()
        desc = "Sort the grayscale image but keep the 8uc3 pixels with each gray entry."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim split = src.Split()
        For i = 0 To 2
            If task.firstPass Then ReDim pixels(i)(src.Total - 1)
            Marshal.Copy(split(i).Data, pixels(i), 0, pixels(i).Length)
        Next

        Dim totals(255) As Single
        Dim lut(255) As cv.Vec3b
        For i = 0 To src.Total - 1
            Dim index = CInt(0.299 * pixels(2)(i) + 0.587 * pixels(1)(i) + 0.114 * pixels(0)(i))
            totals(index) += 1
            If totals(index) = 1 Then lut(index) = New cv.Vec3b(pixels(0)(i), pixels(1)(i), pixels(2)(i))
        Next

        Dim histogram As cv.Mat = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_32F, totals)
        plot.Run(histogram)
        dst2 = plot.dst2
    End Sub
End Class
