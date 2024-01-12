Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Sort_Basics : Inherits VB_Algorithm
    Dim options As New Options_Sort
    Public Sub New()
        desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If options.radio5.Checked Then
            src = src.Reshape(1, src.Rows * src.Cols)
            options.sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Descending
        End If
        If options.radio4.Checked Then
            src = src.Reshape(1, src.Rows * src.Cols)
            options.sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Ascending
        End If
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Sort(options.sortOption)
        If options.radio4.Checked Or options.radio5.Checked Then dst2 = dst2.Reshape(1, dst0.Rows)
    End Sub
End Class







Public Class Sort_RectAndMask : Inherits VB_Algorithm
    Dim sort As New Sort_Basics
    Public mask As cv.Mat
    Public rect As cv.Rect
    Public Sub New()
        labels(3) = "Original input to sort"
        If standalone Then task.drawRect = New cv.Rect(100, 100, 100, 100)
        desc = "Sort the grayscale image portion in a rect while allowing for a mask."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim tmpRect = If(rect = New cv.Rect, task.drawRect, rect)
        dst1 = src(tmpRect).Clone
        If mask IsNot Nothing Then
            mask = mask.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            dst1.SetTo(0, mask)
        End If
        sort.Run(dst1)
        dst2 = sort.dst2.Reshape(1, dst1.Rows)
        dst2 = dst2.Resize(dst3.Size)
        If standalone Then dst3 = src(tmpRect).Resize(dst3.Size)
    End Sub
End Class





Public Class Sort_MLPrepTest_CPP : Inherits VB_Algorithm
    Public reduction As New Reduction_Basics
    Public MLTestData As New cv.Mat
    Public Sub New()
        cPtr = Sort_MLPrepTest_Open()
        desc = "Prepare the grayscale image and row to predict depth"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        reduction.Run(src)

        Dim dataSrc(reduction.dst2.Total * reduction.dst2.ElemSize) As Byte
        Marshal.Copy(reduction.dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Sort_MLPrepTest_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        MLTestData = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32FC2, imagePtr).Clone
        Dim split = MLTestData.Split()
        dst2 = split(0)
        dst3 = split(1)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Sort_MLPrepTest_Close(cPtr)
    End Sub
End Class









Public Class Sort_3Channel : Inherits VB_Algorithm
    Dim sort As New Sort_Basics
    Dim dups As New ML_RemoveDups_CPP
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        findRadio("Sort all pixels descending").Checked = True
        labels = {"", "The BGRA input to sort - shown here as 1-channel CV_32S format", "Output of sort - no duplicates", "Input before removing the dups - use slider to increase/decrease the amount of data"}
        desc = "Take some 3-channel input, convert it to BGRA, sort it as integers, and provide the list of unique elements"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Threshold for sort input")
        Dim inputMask = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone Then inputMask = inputMask.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)

        Static bgra As cv.Mat
        bgra = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA)
        dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_32S, bgra.Data)

        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        dst1.CopyTo(dst0, inputMask)
        sort.Run(dst0)
        dst2 = sort.dst2.Reshape(1, dst2.Rows)
        Dim tmp = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC4, dst2.Data)
        dst3 = tmp.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        dups.Run(dst2)
        dst2 = dups.dst2
    End Sub
End Class









Public Class Sort_1Channel : Inherits VB_Algorithm
    Dim sort As New Sort_Basics
    Dim dups As New ML_RemoveDups_CPP
    Public rangeStart As New List(Of Integer)
    Public rangeEnd As New List(Of Integer)
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        findRadio("Sort all pixels descending").Checked = True
        If standalone Then gOptions.GridSize.Value = 10
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Mask used to isolate the gray scale input to sort", "Sorted thresholded data", "Output of sort - no duplicates"}
        desc = "Take some 1-channel input, sort it, and provide the list of unique elements"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Threshold for sort input")

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)
        sort.Run(dst2)

        Dim pixelsPerBlock = CInt(dst3.Total / dst2.Rows)
        Dim sq = Math.Sqrt(pixelsPerBlock)
        gOptions.GridSize.Value = CInt(Math.Min(sq, 10))

        dst0 = sort.dst2.Reshape(1, dst2.Rows)

        dups.Run(dst0)
        dst3.SetTo(255)
        Dim inputCount = dups.dst3.CountNonZero
        Dim testVals As New List(Of Integer)
        For i = 0 To Math.Min(inputCount, task.gridList.Count) - 1
            Dim roi = task.gridList(i)
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







'Public Class Sort_FeatureLess : Inherits VB_Algorithm
'    Public fLess As New FeatureLess_Rects
'    Public sort As New Sort_1Channel
'    Public Sub New()
'        findSlider("Threshold for sort input").Value = 0
'        desc = "Sort all the featureless grayscale pixels."
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        fLess.Run(src)

'        dst2.SetTo(0)
'        src += 1 ' pull darkest regions away from zero
'        src.CopyTo(dst2, fLess.dst2)

'        sort.Run(dst2)
'        dst3 = sort.dst3

'        dst3(New cv.Rect(0, dst2.Height / 2, dst2.Width, dst2.Height / 2)).SetTo(0)
'        If heartBeat() Then
'            strOut = ""
'            For i = sort.rangeStart.Count - 1 To 0 Step -1
'                strOut += "Range " + CStr(sort.rangeStart.Count - i) + " from " + CStr(sort.rangeEnd(i)) + " to " + CStr(sort.rangeStart(i)) + vbCrLf
'            Next
'            labels(3) = sort.labels(3)
'        End If
'        setTrueText(strOut, New cv.Point(5, dst2.Height / 2 + 5), 3)
'    End Sub
'End Class








'Public Class Sort_Blur : Inherits VB_Algorithm
'    Dim sort As New Sort_FeatureLess
'    Dim blur As New Blur_Basics
'    Public Sub New()
'        desc = "Use Sort_FeatureLess with a blur as well"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        blur.Run(src)

'        sort.Run(blur.dst2)

'        dst2 = sort.dst2
'        dst3 = sort.dst3
'        setTrueText(sort.strOut, New cv.Point(5, dst2.Height / 2 + 5), 3)
'        labels = sort.labels
'    End Sub
'End Class








'Public Class Sort_Reduction : Inherits VB_Algorithm
'    Public sort As New Sort_FeatureLess
'    Public reduction As New Reduction_Basics
'    Dim addw As New AddWeighted_Basics
'    Public Sub New()
'        dst2 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
'        desc = "Use Sort_FeatureLess with Reduction"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        addw.src2 = src.Clone

'        reduction.Run(src)

'        sort.Run(reduction.dst2)

'        dst3 = reduction.dst2
'        dst2 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
'        Dim incr = CInt(255 / sort.fLess.rects.Count)
'        For i = 0 To sort.fLess.rects.Count - 1
'            Dim r = sort.fLess.rects(i)
'            Dim tmp = dst3(r).InRange(sort.fLess.rectColor(i) - 1, sort.fLess.rectColor(i))
'            dst2(r).SetTo(i * incr + 1, tmp)
'        Next
'        addw.Run(vbPalette(dst2))
'        dst2 = addw.dst2

'        labels(2) = "There were " + CStr(sort.fLess.rects.Count) + " regions"
'    End Sub
'End Class








'Public Class Sort_Inrange : Inherits VB_Algorithm
'    Dim sReduce As New Sort_Reduction
'    Public Sub New()
'        If sliders.Setup(traceName) Then sliders.setupTrackBar("Selected Range", 0, 10, 1)
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        desc = "Use Inrange with the Sort_Reduction ranges"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Static rangeSlider = findSlider("Selected Range")
'        Dim rangeIndex = rangeSlider.value

'        sReduce.Run(src)
'        dst3 = sReduce.dst3
'        labels = sReduce.labels

'        Dim rStart = sReduce.sort.sort.rangeStart
'        Dim rEnd = sReduce.sort.sort.rangeEnd
'        If rangeIndex < rStart.Count Then
'            Dim lo = rStart(rangeIndex)
'            Dim hi = rEnd(rangeIndex)
'            dst2 = sReduce.reduction.dst2.InRange(lo - 1, hi)
'        End If
'        setTrueText(sReduce.sort.strOut, New cv.Point(5, dst2.Height / 2 + 5), 3)
'    End Sub
'End Class
