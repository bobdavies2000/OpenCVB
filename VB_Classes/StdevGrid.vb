﻿Imports cv = OpenCvSharp
Public Class StdevGrid_Basics : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        gOptions.AddWeightedSlider.Value = 70
        If standalone Then gOptions.GridSize.Value = 4
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use the task.gridList roi's to compute the stdev for each roi.  If small (<10), mark as featureLess."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim stdevList0 As New List(Of Single)
        Dim stdevList1 As New List(Of Single)
        Dim stdevList2 As New List(Of Single)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        For Each roi In task.gridList
            cv.Cv2.MeanStdDev(src(roi), mean, stdev)
            stdevList0.Add(stdev(0))
            stdevList1.Add(stdev(1))
            stdevList2.Add(stdev(2))
        Next

        Dim avg0 = stdevList0.Average
        Dim avg1 = stdevList1.Average
        Dim avg2 = stdevList2.Average
        dst3.SetTo(0)
        For i = 0 To stdevList0.Count - 1
            Dim roi = task.gridList(i)
            If stdevList0(i) < avg0 And stdevList1(i) < avg1 And stdevList2(i) < avg2 Then
                dst3.Rectangle(roi, cv.Scalar.White, -1)
            End If
        Next
        labels(3) = "Stdev average X/Y/Z = " + CInt(stdevList0.Average).ToString + ", " + CInt(stdevList1.Average).ToString + ", " + CInt(stdevList2.Average).ToString

        addw.src2 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst2 = addw.dst2
    End Sub
End Class







Public Class StdevGrid_Canny : Inherits VB_Algorithm
    Dim canny As New Edge_Canny
    Dim devGrid As New StdevGrid_Basics
    Public Sub New()
        desc = "Create the stdev grid with the input image, then create the stdev grid for the canny output, then combine them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        devGrid.Run(src Or dst3)
        dst2 = devGrid.dst2
    End Sub
End Class








Public Class StdevGrid_Sorted : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Dim gridLow As New Grid_LowRes
    Dim gridQuarter As New Grid_QuarterRes
    Dim myRes = task.lowRes
    Public sortedStd As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingle)
    Public bgrList As New List(Of cv.Vec3b)
    Public roiList As New List(Of cv.Rect)
    Public categories() As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        gOptions.GridSize.Value = If(task.lowRes = myRes, 4, 8)
        labels(2) = "Use the AddWeighted slider to observe where stdev is above average."
        desc = "Sort the roi's by the sum of their bgr stdev's to find the least volatile regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static gridList As New List(Of cv.Rect)
        If task.optionsChanged Then
            If myRes = task.lowRes Then
                gridLow.Run(src)
                gridList = gridLow.gridList
            Else
                gridQuarter.Run(src)
                gridList = gridQuarter.gridList
            End If
        End If

        Dim srcSmall As cv.Mat = src.Resize(myRes)
        Dim meanS As cv.Scalar, stdev As cv.Scalar
        sortedStd.Clear()
        Dim ratio = src.Width / myRes.Width
        bgrList.Clear()
        roiList.Clear()
        ReDim categories(8)
        For i = 0 To gridList.Count - 1
            Dim roi = gridList(i)
            Dim tmp As cv.Mat = srcSmall(roi)
            cv.Cv2.MeanStdDev(tmp, meanS, stdev)
            If ratio <> 1 Then roi = New cv.Rect(roi.X * ratio, roi.Y * ratio, roi.Width * ratio, roi.Height * ratio)
            sortedStd.Add(stdev(0) + stdev(1) + stdev(2), roi)
            Dim colorIndex As Integer = 1
            Dim mean As cv.Vec3i = New cv.Vec3i(CInt(meanS(0)), CInt(meanS(1)), CInt(meanS(2)))
            If mean(1) = mean(2) Then
                colorIndex = 2
            ElseIf mean(0) = mean(2) Then
                colorIndex = 3
            ElseIf mean(0) = mean(1) Then
                colorIndex = 4
            ElseIf mean(0) > mean(1) And mean(0) > mean(2) Then
                colorIndex = 5
            ElseIf mean(1) > mean(0) And mean(1) > mean(2) Then
                colorIndex = 6
            ElseIf mean(2) > mean(0) And mean(2) > mean(0) Then
                colorIndex = 7
            ElseIf mean(0) = mean(1) And mean(1) = mean(2) Then
                colorIndex = 8
            End If

            Dim color = Choose(colorIndex, black, yellow, purple, teal, blue, green, red, white)
            categories(colorIndex) += 1
            bgrList.Add(color)
            roiList.Add(roi)
        Next
        Dim avg = sortedStd.Keys.Average

        Dim count As Integer, maskVal = 255
        If standalone = False Then maskVal = 1
        dst2.SetTo(0)
        For i = 0 To sortedStd.Count - 1
            Dim nextStdev = sortedStd.ElementAt(i).Key
            If nextStdev < avg Then
                Dim roi = sortedStd.ElementAt(i).Value
                dst2(roi).SetTo(maskVal)
                count += 1
            End If
        Next
        dst2 = dst2.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)

        If standaloneTest() Then
            addw.src2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            addw.Run(src)
            dst3 = addw.dst2
        End If

        labels(3) = $"{count} roi's or " + Format(count / sortedStd.Count, "0%") + " have an average stdev sum of " +
                    Format(avg, fmt1) + " or less"
    End Sub
End Class







Public Class StdevGrid_ColorSplit : Inherits VB_Algorithm
    Dim devGrid As New StdevGrid_Sorted
    Public Sub New()
        desc = "Split each pixel into one of 7 categories - yellow, purple, teal, blue, green, red, black or white "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        devGrid.Run(src)

        For i = 0 To devGrid.bgrList.Count - 1
            Dim roi = devGrid.roiList(i)
            Dim color = devGrid.bgrList(i)
            dst2(roi).SetTo(color)
        Next

        strOut = "Categories:" + vbCrLf
        For i = 1 To devGrid.categories.Count - 1
            Dim colorName = Choose(i, "black", "yellow", "purple", "teal", "blue", "green", "red", "white")
            strOut += colorName + vbTab + CStr(devGrid.categories(i)) + vbCrLf
        Next
        setTrueText(strOut, 3)
    End Sub
End Class