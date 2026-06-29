Imports cv = OpenCvSharp
Public Class FeatureLess_Basics_TA : Inherits TaskParent
    Public regions As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
    Public indexList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Public brickList As New List(Of cv.Rect)
    Dim index As Integer
    Dim rect As cv.Rect
    Public mask As cv.Mat = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "CV_8U representation of the regions"
        desc = "Identify featureless grid rects."
    End Sub
    Public Function buildMap(input As cv.Mat) As cv.Mat
        mask.SetTo(0)
        For Each r In task.gridRects
            Dim val1 = input(r).Get(Of Byte)(0, 0)
            Dim val2 = dst3(r).Get(Of Byte)(0, 0)
            If val1 = 255 And val2 > 0 Then
                index = val2
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                regions.Add(count, ValidateRect(rect))
                indexList.Add(count, index)
            End If
        Next
        Return input
    End Function
    Public Function buildMapHeartBeat(input As cv.Mat) As cv.Mat
        mask.SetTo(0)
        index = 1
        For Each r In task.gridRects
            Dim val = input.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                regions.Add(count, ValidateRect(rect))
                indexList.Add(count, index)
                index += 1
            End If
        Next
        Return input
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        brickList.Clear()
        dst1.SetTo(0)
        For Each r In task.gridRects
            If task.edges.dst2(r).CountNonZero = 0 Then
                dst1(r).SetTo(255)
                brickList.Add(r)
            End If
        Next

        regions.Clear()
        indexList.Clear()
        If task.heartBeatLT Then dst3 = buildMapHeartBeat(dst1.Clone) Else dst3 = buildMap(dst1.Clone)

        index = regions.Count + 1
        For Each r In task.gridRects
            If dst3(r).Get(Of Byte)(0, 0) = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(dst3, mask, r.TopLeft, index, rect, 0, 0, flags)
                regions.Add(count, ValidateRect(rect))
                indexList.Add(count, index)
                index += 1
            End If
        Next
        dst2 = Palettize(dst3, 0)

        labels(2) = CStr(regions.Count) + " regions were found."
    End Sub
End Class





Public Class XR_FeatureLess_BasicsOld : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects."
    End Sub
    Public Function buildMap(brickList As List(Of cv.Rect), input As cv.Mat) As cv.Mat
        Dim index = 1
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In brickList
            Dim val = input.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                index += 1
            End If
        Next
        Return input
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2
        labels(3) = task.edges.labels(2)

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            dst1(r).SetTo(255)
            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        dst1 = buildMap(brickList, dst1)
        dst2 = Palettize(dst1, 0)

        labels(2) = CStr(brickList.Count) + " featureless grid regions "
    End Sub
End Class




Public Class XR_FeatureLess_Basics : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Dim redC As New RedColor_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            dst1(r).SetTo(255)
            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        redC.Run(dst1.Clone)
        dst2 = redC.dst2

        dst2.SetTo(0, dst1.InRange(0, 0))

        labels(2) = CStr(brickList.Count) + " featureless grid rects"
        labels(3) = CStr(redC.rcList.Count) + " featureless regions "
    End Sub
End Class





Public Class XR_FeatureLess_BasicsRedC : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Dim redC As New RedColor_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            dst1(r).SetTo(255)
            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        redC.Run(dst1.Clone)
        dst2 = redC.dst2

        dst2.SetTo(0, dst1.InRange(0, 0))

        labels(2) = CStr(brickList.Count) + " featureless grid rects"
        labels(3) = CStr(redC.rcList.Count) + " featureless regions "
    End Sub
End Class





Public Class XR_FeatureLess_BasicsTest : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Public regionList As New List(Of List(Of cv.Rect))
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Function buildMap(input As cv.Mat) As cv.Mat
        Dim regionIndex As Integer = regionList.Count
        regionList.Clear()

        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        Dim usedColors As New List(Of Byte)
        For Each r In brickList
            Dim val = input.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim index As Integer = dst3.Get(Of Byte)(r.Y, r.X)
                If index = 0 Or usedColors.Contains(index) Then
                    regionIndex += 1
                    index = regionIndex
                End If
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                usedColors.Add(index)
            End If
        Next
        Return input
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            dst1(r).SetTo(255)
            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        dst3 = buildMap(dst1.Clone)
        dst2 = Palettize(dst3, 0)

        labels(2) = CStr(brickList.Count) + " featureless grid rects"
        labels(3) = CStr(regionList.Count) + " featureless regions "
    End Sub
End Class





Public Class XR_FeatureLess_Depth : Inherits TaskParent
    Public fLessList As New List(Of cv.Rect)
    Public options As New Options_FeatureLess
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = src
        dst2.SetTo(0)
        ' at higher resolutions, the correlation works but the fLessThreshold does not...
        If task.workRes.Width >= 1280 Then
            If src.Channels <> 1 Then src = task.grayOriginal ' only the raw image can be used with correlation.
            Static corr As New Correlation_Basics
            corr.Run(src)
            fLessList = New List(Of cv.Rect)(corr.fLessList)
        Else
            If src.Channels <> 1 Then src = task.gray ' the motion-filtered image can be used a lower resolutions.
            fLessList.Clear()
            For Each r In task.gridRects
                Dim mm = GetMinMax(src(r))
                If mm.range < options.fLessThreshold Then
                    dst2(r).SetTo(255)
                    dst3.Rectangle(r, white, task.lineWidth)
                    fLessList.Add(r)
                End If
            Next
        End If

        'Dim index As Integer = 1
        'For Each r In task.gridRects
        '    Dim val = dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
        '    If val = 255 Then
        '        Dim floodCount = dst2.FloodFill(r.TopLeft, index)
        '        index += 1
        '        If index >= 255 Then Exit For
        '    End If
        'Next

        labels(2) = CStr(fLessList.Count) + " grid squares were found to be featureless (<gridRect>.mm.range < " +
                CStr(options.fLessThreshold) + ")"
    End Sub
End Class





Public Class XR_FeatureLess_DepthMotion : Inherits TaskParent
    Public fLessRaw As New FeatureLess_DepthFull
    Public rectList As New List(Of cv.Rect)
    Public fLessNot As New List(Of cv.Rect)
    Public ptList As New HashSet(Of cv.Point)
    Public Sub New()
        desc = "A features grid rect cannot change if there has been no motion in that grid rect."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLessRaw.Run(src)
        If task.optionsChanged Then
            dst2 = fLessRaw.dst3.Clone
            rectList = New List(Of cv.Rect)(fLessRaw.brickList)
        End If

        ptList.Clear()
        Dim newList As New List(Of cv.Rect)
        ' remove any grid rects that had motion.
        For Each r In rectList
            Dim val = task.motion.motionMask.Get(Of Byte)(r.Y, r.X)
            If val <> 0 Then dst2(r).SetTo(0) Else newList.Add(r)
            ptList.Add(r.TopLeft)
        Next

        For Each r In fLessRaw.brickList
            Dim val = task.motion.motionMask.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
            If val = 0 Then
                If ptList.Contains(r.TopLeft) = False Then
                    newList.Add(r)
                    dst2(r).SetTo(255)
                End If
            End If
        Next

        fLessNot.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X) = 0 Then fLessNot.Add(r)
        Next

        If newList.Count > 0 Then rectList = New List(Of cv.Rect)(newList)

        labels(2) = fLessRaw.labels(2)
    End Sub
End Class




Public Class FeatureLess_Correlation : Inherits TaskParent
    Dim corr As New Correlation_Basics
    Public fLessList As New List(Of cv.Rect)
    Public Sub New()
        desc = "Measure the correlation of all grid squares except where there is motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray

        corr.Run(src)
        dst2 = corr.dst2
        dst3 = corr.dst3
        fLessList = New List(Of cv.Rect)(corr.fLessList)

        labels(2) = corr.labels(2)
    End Sub
End Class





Public Class XR_FeatureLess_Correlation : Inherits TaskParent
    Public fLessList As New List(Of cv.Rect)
    Dim smallGrid As New Grid_SquaresOnly
    Public options As New Options_FeatureLess
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        ' couldn't put this in the constructor because motion is a task algorithm.
        If task.firstPass Then smallGrid.Run(src) ' create the grid squares for the small resolution.
        Dim input As cv.Mat
        If src.Channels <> 1 Then input = task.gray Else input = src

        ' why do this resize?  Because the options.flessThreshold works at smallRes but not larger resolutions.
        If task.workRes.Height > 270 Then input = input.Resize(task.smallRes, 0, 0, cv.InterpolationFlags.Nearest)

        Dim mask As New cv.Mat(input.Size, cv.MatType.CV_8U, 0)
        For Each r In smallGrid.gridRects
            r = ValidateRect(r)
            Dim mm = GetMinMax(input(r))
            If mm.range < options.fLessThreshold Then mask(r).SetTo(255)
        Next

        dst2 = mask.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)

        fLessList.Clear()
        For Each r In task.gridRects
            If dst2.Get(Of Byte)(r.Y, r.X) Then fLessList.Add(r)
        Next

        If standaloneTest() Then
            dst3 = task.gray.Clone
            For Each r In fLessList
                dst3.Rectangle(r, white, task.lineWidth)
            Next

            Dim index = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            Dim mm = GetMinMax(task.gray(task.gridRects(index)))
            SetTrueText("Click on any grid rect to see its grayscale range." + vbCrLf +
                            "Min gray = " + Format(mm.minVal, fmt0) + vbCrLf +
                            "Max Gray = " + Format(mm.maxVal, fmt0) + vbCrLf +
                            "Range = " + Format(mm.range, fmt0) + vbCrLf + vbCrLf, 1)
        End If

        labels(3) = CStr(fLessList.Count) + " grid squares were found to be featureless (range < " +
                        CStr(options.fLessThreshold) + ")"

    End Sub
End Class




Public Class XR_FeatureLess_Correlations : Inherits TaskParent
    Dim corr As New Correlation_BasicsPlot
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corr.Run(src)
        dst3 = corr.dst3
        labels(3) = corr.labels(3)

        dst2.SetTo(0)
        For i = 0 To corr.cList.Count - 1
            Dim r = task.gridRects(i)
            If corr.cList(i) < corr.maxCorrelation Then
                dst2(r).SetTo(255)
                If standaloneTest() Then src.Rectangle(r, white, task.lineWidth)
            End If
        Next
    End Sub
End Class






Public Class XR_FeatureLess_Contours : Inherits TaskParent
    Dim edgeline As New EdgeLine_Basics
    Dim contours As New Contour_Basics
    Public Sub New()
        desc = "Use Contour_Basics to get the contour data for the top contours by size."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels = 1 Then edgeline.Run(src) Else edgeline.Run(task.gray)
        If src.Type <> cv.MatType.CV_8U Then
            contours.Run(edgeline.dst2)
            dst2 = contours.dst2
            labels = contours.labels
        Else
            contours.Run(src)
            dst2 = contours.dst2
            labels = contours.labels
        End If
    End Sub
End Class








Public Class XR_FeatureLess_Sobel : Inherits TaskParent
    Dim edges As New Edge_Sobel
    Dim options As New Options_Sobel()
    Public Sub New()
        desc = "Use Sobel edges to define featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(options.distanceThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class XR_FeatureLess_UniquePixels : Inherits TaskParent
    Dim fless As New Hough_FeatureLessTopX
    Dim sort As New Sort_1Channel
    Public Sub New()
        If standalone Then OptionParent.FindSlider("Threshold for sort input").Value = 0
        labels = {"", "Gray scale input to sort/remove dups", "Unique pixels", ""}
        desc = "Find the unique gray pixels for the featureless regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        sort.Run(dst2)
        dst3 = sort.dst2
    End Sub
End Class







Public Class XR_FeatureLess_Unique3Pixels : Inherits TaskParent
    Dim fLessTopX As New Hough_FeatureLessTopX
    Dim sort3 As New Sort_3Channel
    Public Sub New()
        desc = "Find the unique 3-channel pixels for the featureless regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLessTopX.Run(src)
        dst2 = fLessTopX.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sort3.Run(fLessTopX.dst2)
        dst3 = sort3.dst2
    End Sub
End Class






Public Class XR_FeatureLess_Histogram : Inherits TaskParent
    Dim backP As New BackProject_FeatureLess
    Public Sub New()
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels = backP.labels
    End Sub
End Class










Public Class XR_FeatureLess_DCT : Inherits TaskParent
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(3) = "Largest FeatureLess Region"
        desc = "Use DCT to find featureless regions."
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        dct.Run(src)
        dst2 = dct.dst2
        dst3 = dct.dst3

        Dim mask = dst2.Clone()
        Dim objectSize As New List(Of Integer)
        Dim regionCount = 1
        For y = 0 To mask.Rows - 1
            For x = 0 To mask.Cols - 1
                If mask.Get(Of Byte)(y, x) = 255 Then
                    Dim pt As New cv.Point(x, y)
                    Dim floodCount = mask.FloodFill(pt, regionCount)
                    objectSize.Add(floodCount)
                    regionCount += 1
                End If
            Next
        Next

        Dim maxSize As Integer, maxIndex As Integer
        For i = 0 To objectSize.Count - 1
            If maxSize < objectSize.ElementAt(i) Then
                maxSize = objectSize.ElementAt(i)
                maxIndex = i
            End If
        Next

        Dim label = mask.InRange(maxIndex + 1, maxIndex + 1)
        Dim nonZ = label.CountNonZero()
        labels(3) = "Largest FeatureLess Region (" + CStr(nonZ) + " " + Format(nonZ / label.Total, "#0.0%") + " pixels)"
        dst3.SetTo(white, label)
    End Sub
End Class







Public Class XR_FeatureLess_History : Inherits TaskParent
    Dim frames As New History_Basics
    Dim fLess As New FeatureLess_Correlation
    Public Sub New()
        labels(3) = "The brighter the grid square, the more recent appearance."
        desc = "Accumulate the edges over a span of X images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal)

        dst2 = fLess.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        frames.Run(dst2)
        dst3 = frames.dst2

        Dim countCurr = dst2.CountNonZero
        Dim countAll = dst3.CountNonZero
        Dim sqPixels = task.gridWH * task.gridWH
        labels(2) = "Current frame: " + Format(countCurr / sqPixels, fmt0) + " grid squares and " +
                        Format(countAll / sqPixels, fmt0) + " of all grid squares."
    End Sub
End Class








Public Class FeatureLess_LeftRight : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Public Sub New()
        labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
        desc = "Find the featureless regions of the left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.leftView)
        dst2 = fLess.dst2.Clone

        fLess.Run(task.rightView)
        dst3 = fLess.dst2.Clone
    End Sub
End Class






Public Class FeatureLess_RedColor : Inherits TaskParent
    Public redC As New RedCloud_Flood_CPP
    Public fLess As New FeatureLess_DepthFull
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use the FeatureLess_DepthFull output as input to RedColor_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal.Clone)
        dst3 = fLess.dst2
        labels(3) = fLess.labels(2)

        redC.Run(dst3)

        dst2.SetTo(0)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        strOut = redC.strOut
        SetTrueText(strOut, 1)
    End Sub
End Class




Public Class FeatureLess_Stabilized : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Dim diff As New Diff_Simple
    Public Sub New()
        desc = "Double-check that any differences from the previous fLess output occurred because of motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst1

        Static fLessLast = dst2.Clone

        diff.lastFrame = fLessLast
        diff.Run(dst2)

        For Each r In task.gridRects
            Dim val = diff.dst2.Get(Of Byte)(r.Y, r.X)
            If val > 0 Then
                Dim gridIndex = task.gridMap.Get(Of Integer)(r.Y, r.X)
                If task.motion.motionMask.Get(Of Byte)(r.Y, r.X) = 0 Then
                    dst2(task.gridRects(gridIndex)).SetTo(0)
                End If
            End If
        Next

        fLessLast = dst2.Clone
    End Sub
End Class





Public Class XR_FeatureLess_Lines : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 255)}
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Find and display lines that are between featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal)
        dst2 = fLess.dst2
        labels(2) = fLess.labels(2)

        dst1.SetTo(0)
        task.lines.dst1.CopyTo(dst1, fLess.dst2)

        Dim histogram As New cv.Mat
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {256}, ranges)

        Dim histArray(histogram.Rows - 1) As Single
        histogram.GetArray(Of Single)(histArray)

        dst3.SetTo(0)
        lpList.Clear()
        For i = 1 To Math.Min(task.lines.lpList.Count, histArray.Count) - 1
            If histArray(i) > 0 Then
                Dim lp = task.lines.lpList(i)
                dst3.Line(lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
                lpList.Add(lp)
            End If
        Next

        labels(2) = fLess.labels(2)
        labels(3) = CStr(lpList.Count) + " lines intersected with featureless grid rects."
    End Sub
End Class





Public Class FeatureLess_Cells : Inherits TaskParent
    Dim saveColorMap As cv.Mat
    Dim fLess As New FeatureLess_Correlation
    Public Sub New()
        saveColorMap = task.colorMap.Clone
        labels(3) = "Region Colors are ordered by size."
        desc = "Group the featureless grid squares"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal)

        dst2 = fLess.dst2.Clone
        labels(2) = fLess.labels(2)

        Dim index = 1
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4
        Dim countList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each r In fLess.fLessList
            Dim val = dst2.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim count = cv.Cv2.FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                countList.Add(count, index)
                index += 1
            End If
        Next

        ' this will color the regions in size order.
        For i = 0 To countList.Count - 1
            Dim countIndex = countList.Values.ElementAt(i)
            task.colorMap.Set(Of cv.Vec3b)(countIndex, 0, saveColorMap.Get(Of cv.Vec3b)(i, 0))
        Next
        dst3 = Palettize(dst2, 0)
        labels(2) = CStr(index - 1) + " featureless regions were found below (8UC1)."
    End Sub
End Class





Public Class FeatureLess_NotImages : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Public Sub New()
        labels(3) = "All regions in the image with features."
        desc = "Provide masks for both the featureless and non-featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal)
        dst0 = fLess.dst2
        labels(2) = fLess.labels(2)

        dst1 = Not dst0

        dst2.SetTo(0)
        dst3.SetTo(0)

        src.CopyTo(dst2, dst0)
        src.CopyTo(dst3, dst1)
    End Sub
End Class






Public Class XR_FeatureLess_FeaturesOld : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fLess As New FeatureLess_DepthFull
    Public Sub New()
        desc = "Isolate features in the not of the featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray.Clone)

        dst2.SetTo(0)
        task.color.CopyTo(dst2, Not fLess.dst2)

        feat.Run(dst2)

        For Each pt In feat.features
            dst2.Circle(pt, task.DotSize + 2, task.highlight, -1)
        Next

        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, black, task.lineWidth)
        Next
        Dim count = task.gridRects.Count - fLess.brickList.Count
        labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
    End Sub
End Class






Public Class FeatureLess_FeatureLines : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Public Sub New()
        desc = "Use lines to further divide featureless from features."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray.Clone)
        dst1 = fLess.dst3.Clone
        dst3 = fLess.dst3.Clone

        dst2.SetTo(0)
        task.color.CopyTo(dst2, Not dst1)

        For i = 1 To task.gridRects.Count - 1
            If i = 9 Then Dim k = 0
            Dim r1 = task.gridRects(i - 1)
            Dim r2 = task.gridRects(i)
            Dim p1 = r1.TopLeft
            Dim p2 = r2.TopLeft
            If p1.X < p2.X Then
                Dim val1 = dst1.Get(Of Byte)(p1.Y, p1.X)
                Dim val2 = dst1.Get(Of Byte)(p2.Y, p2.X)
                If val1 = 255 And val2 = 0 Then
                    ' line present?
                    If task.lines.dst3(r2).CountNonZero Then
                        task.lines.dst0(r2).CopyTo(dst1(r2))
                        dst3(r2) = dst3(r2).InRange(0, 0)
                        dst3(r2).SetTo(0, task.lines.dst3(r2))
                    End If
                End If

                If val1 = 0 And val2 = 255 Then
                    ' line present?
                    If task.lines.dst3(r1).CountNonZero Then
                        task.lines.dst0(r1).CopyTo(dst1(r1))
                        dst3(r1) = dst3(r1).InRange(0, 0)
                        dst3(r1).SetTo(0, task.lines.dst3(r1))
                    End If
                End If

                'If val1 = 0 And val2 > 0 Then
                '    ' line present?
                '    If task.lines.dst3(r1).CountNonZero Then
                '        dst1(r2) = dst1(r2).InRange(0, 0)
                '    End If
                'End If
            End If
        Next

        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, black, task.lineWidth)
        Next
        Dim count = task.gridRects.Count - fLess.brickList.Count
        labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
    End Sub
End Class






Public Class FeatureLess_ToList : Inherits TaskParent
    Dim clusters As New FeatureLess_ClusterFlood
    Public clusterX As New List(Of List(Of Integer))
    Public clusterY As New List(Of List(Of Integer))
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create a RedCloud rcList from FeatureLess_Cluster output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastMap As cv.Mat = rcMap.Clone
        Dim rcLastList As New List(Of rcData)(rcList)

        clusters.Run(task.gray)
        dst2 = clusters.dst2.Clone
        labels(2) = clusters.labels(3)

        If clusters.floodPoints.Count <= 1 Then Exit Sub ' nothing to work on...

        clusterX.Clear()
        clusterY.Clear()
        For i = 0 To clusters.floodPoints.Count ' adding one extra for the zero class...
            clusterX.Add(New List(Of Integer))
            clusterY.Add(New List(Of Integer))
        Next

        rcList.Clear()
        rcMap.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim cIndex = dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
            If cIndex = 0 Then Continue For

            clusterX(cIndex).Add(r.TopLeft.X)
            clusterY(cIndex).Add(r.TopLeft.Y)
        Next

        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 1 To clusterX.Count - 1
            Dim minX = clusterX(i).Min
            Dim minY = clusterY(i).Min
            Dim w = clusterX(i).Max - minX + task.gridWH
            Dim h = clusterY(i).Max - minY + task.gridWH
            Dim rect = ValidateRect(New cv.Rect(minX, minY, w, h))

            Dim pt = New cv.Point(clusterX(i)(0), clusterY(i)(0))
            Dim val = dst2.Get(Of Byte)(pt.Y, pt.X)
            Dim rc = New rcData(dst2(rect), rect, val)
            sortList.Add(rc.pixels, rc)
        Next

        rcList.Clear()
        For Each rc In sortList.Values
            If task.heartBeat Or True Then
                rc.mapID = rcList.Count + 1
            Else
                Dim lastIndex = lastMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                rc.mapID = rcList.Count + 1
                If lastIndex > 0 And lastIndex < rcLastList.Count Then
                    rc.mapID = lastIndex
                    rc.age = rcLastList(lastIndex).age + 1
                End If
            End If
            rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
            rcList.Add(rc)
        Next

        dst3 = Palettize(rcMap, 0)

        strOut = Utility_Basics.selectCell(rcMap, rcList)
        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class FeatureLess_ClusterFlood : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Public floodPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Identify the clusters in the FeatureLess_DepthFull output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray)
        dst2 = fLess.dst1.Clone
        labels(2) = fLess.labels(2)

        floodPoints.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim val = dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
            If val = 255 Then
                Dim floodCount = dst2.FloodFill(r.TopLeft, floodPoints.Count + 1)
                floodPoints.Add(r.TopLeft)
                If floodPoints.Count >= 254 Then Exit For
            End If
        Next

        If standaloneTest() Then dst3 = Palettize(dst2, 0)

        If standalone Then
            Static clusterID As Byte
            clusterID = dst2.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)

            If clusterID > 0 Then
                dst0 = dst2.Clone
                dst0.FloodFill(task.clickPoint, 255)
                dst0 = dst0.Threshold(254, 255, cv.ThresholdTypes.Binary)
                task.color.SetTo(white, dst0)
            End If
        End If
        labels(3) = CStr(floodPoints.Count - 1) + " clusters were found "
    End Sub
End Class





Public Class XR_FeatureLess_Predict : Inherits TaskParent
    Dim ml As New ML_RandomForest
    Public clusters() As Single
    Dim ranges() As cv.Rangef
    Public bpArray() As Single
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use edges, depth, color, and location to predict featureless regions."
    End Sub
    Public Function backProjectHist(histogram As cv.Mat, histInput As cv.Mat, edgeCounts As List(Of Integer)) As Single()
        Dim backP As New cv.Mat
        cv.Cv2.CalcBackProject({histInput}, {0, 1}, histogram, backP, ranges)
        ReDim bpArray(histogram.Rows * histogram.Cols - 1)
        backP.GetArray(Of Single)(bpArray)

        Dim gridList(task.gridRects.Count - 1) As Single
        Dim bpIndex As Integer
        For i = 0 To task.gridRects.Count - 1
            If edgeCounts(i) = 0 Then
                gridList(i) = bpArray(bpIndex)
                bpIndex += 1
            End If
        Next

        Return gridList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rectCount = task.gridRects.Count
        Dim inputVariableCount As Integer = 3
        Dim flat(rectCount * inputVariableCount - 1) As Single
        Dim colorDepth As New List(Of Single)
        Dim edgeCounts As New List(Of Integer)
        Dim index As Integer
        For Each r In task.gridRects
            Dim edgeCount = task.edges.dst2(r).CountNonZero
            edgeCounts.Add(edgeCount)

            flat(index) = src(r).Mean()(0)
            flat(index + 1) = task.pcSplit(2)(r).Mean(task.depthmask(r))(0)
            If edgeCount = 0 Then
                colorDepth.Add(flat(index))
                colorDepth.Add(flat(index + 1))
            End If
            flat(index + 2) = edgeCount
            'flat(index + 3) = CSng(r.TopLeft.X)
            'flat(index + 4) = CSng(r.TopLeft.Y)
            index += inputVariableCount
        Next

        ml.testMat = cv.Mat.FromPixelData(rectCount, inputVariableCount, cv.MatType.CV_32F, flat)
        Dim mmX = GetMinMax(task.gray)
        ranges = {New cv.Rangef(mmX.minVal - 0.01, 255.01), New cv.Rangef(0, task.MaxZmeters)}

        Dim trainLabels(rectCount - 1) As Single
        If task.heartBeatLT Then
            Dim histogram As New cv.Mat
            Dim histInput = cv.Mat.FromPixelData(colorDepth.Count / 2, 1, cv.MatType.CV_32FC2, colorDepth.ToArray)
            Dim bins = task.histogramBins
            cv.Cv2.CalcHist({histInput}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, ranges)

            histogram = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)

            Dim floodIndex As Integer = 1
            For y = 0 To histogram.Height - 1
                For x = 0 To histogram.Width - 1
                    Dim pt = New cv.Point(x, y)
                    Dim val = histogram.Get(Of Single)(y, x)
                    If val = 255 Then
                        histogram.FloodFill(pt, floodIndex)
                        floodIndex += 1
                        If floodIndex >= 254 Then Exit For
                    End If
                Next
            Next

            Dim gridList = backProjectHist(histogram, histInput, edgeCounts)
            ml.trainResponse = cv.Mat.FromPixelData(rectCount, 1, cv.MatType.CV_32F, gridList)
            ml.trainMat = ml.testMat.Clone
            ml.predictions = ml.trainResponse.Clone

            dst0.SetTo(0)
            For j = 0 To rectCount - 1
                Dim nextVal = CInt(ml.predictions.Get(Of Single)(j, 0))
                If nextVal > 0 Then dst0(task.gridRects(j)).SetTo(nextVal)
            Next
            dst1 = Palettize(dst0, 0)
        End If

        ml.Run(emptyMat)

        dst3.SetTo(0)
        Dim maxClass As Integer
        For j = 0 To rectCount - 1
            Dim nextVal = CInt(ml.predictions.Get(Of Single)(j, 0))
            If nextVal > maxClass Then maxClass = nextVal
            If nextVal > 0 Then dst3(task.gridRects(j)).SetTo(nextVal)
        Next

        dst2 = Palettize(dst3, 0)
        labels(2) = CStr(maxClass) + " grid cell clusters found with prediction."
    End Sub
End Class






Public Class FeatureLess_Features : Inherits TaskParent
    Dim fLess As New FeatureLess_DepthFull
    Public featureList As New List(Of Single)
    Public idList As New List(Of Single)
    Public inputVariableCount As Integer = 5
    Public rcList As New List(Of rcData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Expanded floodfill usage for the featureLess image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray.Clone)
        dst2 = fLess.dst2

        Dim rect As New cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        Dim index As Integer = 1
        featureList.Clear()
        idList.Clear()
        rcList.Clear()
        For Each r In task.gridRects
            If dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X) = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim Count = cv.Cv2.FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                Dim rc = New rcData(mask(rect), rect, index)
                rcList.Add(rc)

                idList.Add(CSng(index))

                featureList.Add(src(rect).Mean(rc.mask)(0))
                featureList.Add(rc.wGrid.Z)

                featureList.Add(rc.pixels)
                featureList.Add(rc.maxDist.X)
                featureList.Add(rc.maxDist.Y)
                index += 1
            End If
        Next

        dst3 = Palettize(dst2, 0)

        labels(2) = CStr(index) + " regions were found."
    End Sub
End Class



Public Class FeatureLess_IndexKNN : Inherits TaskParent
    Dim feat As New FeatureLess_Features
    Dim knn As New KNN_IndividualQuery
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        knn.dimension = feat.inputVariableCount
        desc = "Predict the index for each featureLess region using the features Mat."
    End Sub
    Private Function foundLast(rc As rcData, lrc As rcData) As rcData
        rc.age = lrc.age + 1
        If rc.age >= 1000 Then rc.age = 10
        rc.mapID = rc.indexLast
        rc.color = lrc.color
        Return rc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastImage = feat.dst2.Clone
        Dim lastList = New List(Of rcData)(feat.rcList)

        feat.Run(task.gray.Clone)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        Dim nVal = knn.dimension
        Dim queries = cv.Mat.FromPixelData(feat.featureList.Count \ nVal, nVal, cv.MatType.CV_32F, feat.featureList.ToArray)

        knn.trainMat = cv.Mat.FromPixelData(queries.Rows, nVal, cv.MatType.CV_32F, feat.featureList.ToArray)

        If feat.featureList.Count = 0 Then Exit Sub ' nothing to work with...

        knn.Run(emptyMat)

        Static maxDistList As New List(Of cv.Point)
        For Each rc In feat.rcList
            Dim lastIndex = lastImage.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
            If lastIndex >= 0 And lastIndex < lastList.Count Then
                rc.indexLast = lastIndex
                rc = foundLast(rc, lastList(lastIndex))
            End If
        Next

        dst3.SetTo(0)
        For Each rc In feat.rcList
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            SetTrueText(CStr(rc.age), rc.maxDist, 3)
        Next

        maxDistList.Clear()
        For Each rc In feat.rcList
            dst3.Rectangle(rc.rect, task.highlight, task.lineWidth)
            maxDistList.Add(rc.maxDist)
            If task.heartBeat Then rc.color = task.scalarColors(rc.mapID + 1)
        Next

        strOut = Utility_Basics.selectCell(dst2, feat.rcList)
        SetTrueText(strOut, 1)

        labels(2) = CStr(knn.trainMat.Rows) + " featureless clusters were found."
    End Sub
End Class







Public Class XR_FeatureLess_ClustersHist2D : Inherits TaskParent
    Public fLess As New FeatureLess_DepthFull
    Public histArray(task.histogramBins * task.histogramBins - 1) As Single
    Public features As New cv.Mat
    Public bpArray() As Single
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Isolate clusters using a 2D histogram"
    End Sub
    Public Function backProjectHistArray(histogram As cv.Mat, ranges() As cv.Rangef) As cv.Mat
        Dim backP As New cv.Mat
        cv.Cv2.CalcBackProject({features}, {0, 1}, histogram, backP, ranges)
        ReDim bpArray(histogram.Rows * histogram.Cols - 1)
        backP.GetArray(Of Single)(bpArray)

        Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
        For i = 0 To fLess.brickList.Count - 1
            dst(fLess.brickList(i)).SetTo(bpArray(i))
        Next

        Return dst
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray)

        Dim mmX = GetMinMax(fLess.dst1)
        Dim ranges() As cv.Rangef = {New cv.Rangef(mmX.minVal - 0.01, 255.01), New cv.Rangef(0, task.MaxZmeters)}

        Dim depthMat = task.pcSplit(2).Clone
        depthMat.SetTo(0, Not fLess.dst1)

        Dim grayMat As New cv.Mat
        fLess.dst2.ConvertTo(grayMat, cv.MatType.CV_32F)

        cv.Cv2.Merge({grayMat, depthMat}, features)

        Dim histogram As New cv.Mat
        Dim bins = task.histogramBins
        cv.Cv2.CalcHist({features}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, ranges)

        histogram = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim floodIndex As Integer = 1
        For y = 0 To histogram.Height - 1
            For x = 0 To histogram.Width - 1
                Dim pt = New cv.Point(x, y)
                Dim val = histogram.Get(Of Single)(y, x)
                If val = 255 Then
                    histogram.FloodFill(pt, floodIndex)
                    floodIndex += 1
                    If floodIndex >= 254 Then Exit For
                End If
            Next
        Next

        histogram.GetArray(Of Single)(histArray)

        dst3 = backProjectHistArray(histogram, ranges)
        Dim clusterCount = GetMinMax(dst3).maxVal - 1
        dst2 = Palettize(dst3, 0)

        labels(2) = CStr(clusterCount) + " clusters were found for the " + "X scale is mean grayscale color and the Y scale is mean depth."
        labels(3) = CStr(clusterCount) + " clusters were identified."
    End Sub
End Class





Public Class FeatureLess_DepthFull : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless gridrects that also have depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        labels(3) = task.edges.labels(2)

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            If task.depthmask(r).CountNonZero = 0 Then Continue For
            dst1(r).SetTo(255)

            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        Dim index = 1
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In brickList
            Dim val = dst1.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(dst1, mask, r.TopLeft, index, rect, 0, 0, flags)
                index += 1
            End If
        Next

        dst2 = Palettize(dst1, 0)

        labels(2) = CStr(brickList.Count) + " featureless grid regions with " + CStr(countRects) + " input grid rects"
    End Sub
End Class






Public Class FeatureLess_Depth : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Public Sub New()
        desc = "Same as FeatureLess_Basics but remove those grid rects with no depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        brickList.Clear()
        For Each r In task.fLess.brickList
            If task.depthmask(r).CountNonZero = 0 Then Continue For
            brickList.Add(r)
            dst1(r).SetTo(255)
        Next

        dst2 = task.fLess.dst2
        labels(2) = task.fLess.labels(2)
    End Sub
End Class





Public Class FeatureLess_XLines : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        desc = "Find horizontal and vertical lines through the center of featureless grid rects."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray

        dst1 = task.fLess.dst3
        dst2 = task.fLess.dst2

        lpList.Clear()
        For y = task.gridWH To dst1.Height - 1 Step task.gridWH
            Dim p1 = newPoint, p2 = newPoint
            Dim val1 As Integer, val2 As Integer
            For x = 0 To dst1.Width - 1 Step task.gridWH
                val1 = dst1.Get(Of Byte)(y, x)
                val2 = If(x + task.gridWH >= dst2.Width, 0, dst1.Get(Of Byte)(y, x + task.gridWH))
                If val1 <> val2 Or (x = 0 And val1 <> 0) Then
                    If val1 = 0 Then p1 = New cv.Point(x + task.gridWH, y)
                    If val1 <> 0 And (x = 0 And val1 <> 0) Then p1 = New cv.Point(x, y)
                    If val2 = 0 Then p2 = New cv.Point(x + task.gridWH - 1, y)
                    If p1 <> newPoint And p2 <> newPoint Then
                        Dim lp = New lpData(p1, p2)
                        lp.fLessID = If(val1 = 0, val2, val1)
                        lpList.Add(lp)
                        dst2.Line(p1, p2, white, task.lineWidth)
                        p1 = newPoint
                        p2 = newPoint
                    End If
                End If
            Next
        Next
        labels(2) = CStr(lpList.Count) + " horizontal lines encountered with depth"
    End Sub
End Class





Public Class FeatureLess_YLines : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find horizontal and vertical lines through the center of featureless grid rects."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray

        dst1 = task.fLess.dst3
        dst2 = task.fLess.dst2

        lpList.Clear()
        For x = task.gridWH To dst1.Width - 1 Step task.gridWH
            Dim p1 = newPoint, p2 = newPoint
            Dim val1 As Integer, val2 As Integer
            For y = 0 To dst1.Height - 1 Step task.gridWH
                val1 = dst1.Get(Of Byte)(y, x)
                val2 = If(y + task.gridWH >= dst2.Height, 0, dst1.Get(Of Byte)(y + task.gridWH, x))
                If val1 <> val2 Or (y = 0 And val1 <> 0) Then
                    If val1 = 0 Then p1 = New cv.Point(x, y + task.gridWH)
                    If val1 <> 0 And (y = 0 And val1 <> 0) Then p1 = New cv.Point(x, y)
                    If val2 = 0 Then p2 = New cv.Point(x, y + task.gridWH - 1)
                    If p1 <> newPoint And p2 <> newPoint Then
                        Dim lp = New lpData(p1, p2)
                        lp.fLessID = If(val1 = 0, val2, val1)
                        lpList.Add(lp)
                        dst2.Line(p1, p2, white, task.lineWidth)
                        p1 = newPoint
                        p2 = newPoint
                    End If
                End If
            Next
        Next

        labels(2) = CStr(lpList.Count) + " horizontal lines encountered with depth"
    End Sub
End Class





Public Class FeatureLess_Lines : Inherits TaskParent
    Dim xLines As New FeatureLess_XLines
    Dim yLines As New FeatureLess_YLines
    Public Sub New()
        desc = "Access the line data to get info about the featureless region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        xLines.Run(task.gray)
        dst2 = xLines.dst2
        labels(2) = xLines.labels(2)

        yLines.Run(task.gray)
        dst2 = yLines.dst2
        labels(2) = yLines.labels(2)

        Static selectedRegion As Integer = -1
        If task.mouseClickFlag Then
            selectedRegion = xLines.dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        End If

        If selectedRegion > 0 Then
            dst3.SetTo(0)
            For Each lp In xLines.lpList
                If lp.fLessID = selectedRegion Then
                    dst3.Line(lp.p1, lp.p2, white, task.lineWidth)
                End If
            Next
            For Each lp In yLines.lpList
                If lp.fLessID = selectedRegion Then
                    dst3.Line(lp.p1, lp.p2, white, task.lineWidth)
                End If
            Next
        End If
    End Sub
End Class





Public Class XR_FeatureLess_BasicsTest2 : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects."
    End Sub
    Public Function buildMap(brickList As List(Of cv.Rect), input As cv.Mat) As cv.Mat
        Dim index = 1
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In brickList
            Dim val = input.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                index += 1
            End If
        Next
        Return input
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2
        labels(3) = task.edges.labels(2)

        dst1.SetTo(0)
        brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If task.edges.dst2(r).CountNonZero > 0 Then Continue For
            dst1(r).SetTo(255)
            brickList.Add(r)
        Next
        Dim countRects = brickList.Count

        If task.heartBeatLT = False Then
            For Each r In brickList
                Dim val = dst3.Get(Of Byte)(r.Y, r.X)
                If val = 0 Then dst1(r).SetTo(0)
            Next
        End If
        dst3 = buildMap(brickList, dst1.Clone)
        dst2 = Palettize(dst3, 0)

        labels(2) = CStr(brickList.Count) + " featureless grid regions "
    End Sub
End Class






Public Class FeatureLess_Tracker : Inherits TaskParent
    Public regions As New List(Of (count As Integer, r As cv.Rect))
    Dim overlap As New FeatureLess_Overlap
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Track featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
        Dim index As Integer

        overlap.Run(emptyMat)

        regions.Clear()
        dst0 = task.fLess.dst1.Clone
        Dim newCandidates As New List(Of cv.Rect)
        Dim floodRects As New List(Of cv.Rect)
        For Each r In task.gridRects
            If overlap.dst3(r).Get(Of Byte)(0, 0) = 255 Then
                index = dst3(r).Get(Of Byte)(0, 0)
                If index > 0 And dst0(r).Get(Of Byte)(0, 0) = 255 Then
                    Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = cv.Cv2.FloodFill(dst0, mask, r.TopLeft, index, rect, 0, 0, flags)
                    regions.Add((count, ValidateRect(rect)))
                    floodRects.Add(r)
                End If
            ElseIf overlap.dst2(r).Get(Of Byte)(0, 0) = 255 Then
                newCandidates.Add(r)
            End If
        Next

        Dim indexNew = regions.Count + 1
        For Each r In newCandidates
            If dst3(r).Get(Of Byte)(0, 0) = 0 And dst0(r).Get(Of Byte)(0, 0) = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (indexNew << 8)
                Dim count = cv.Cv2.FloodFill(dst0, mask, r.TopLeft, indexNew, rect, 0, 0, flags)
                regions.Add((count, ValidateRect(rect)))
                indexNew += 1
            End If
        Next

        dst2 = Palettize(dst0, 0)
        labels(2) = CStr(regions.Count) + " regions were found."

        dst1 = task.fLess.dst1.Clone
        If task.heartBeatLT Then dst3 = task.fLess.dst3.Clone

        For Each r In floodRects
            dst2.Rectangle(r, task.highlight, task.lineWidth)
        Next
    End Sub
End Class





Public Class FeatureLess_Overlap : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Grid rects that did not overlap", "Grid rects that overlapped."}
        desc = "Compare the current and previous featureless regions and define overlap and not overlap."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = task.fLess.dst1.Clone

        dst2 = dst0.Clone
        dst2.SetTo(0, dst1)
        dst3 = dst0 And dst1

        dst1 = task.fLess.dst1.Clone
    End Sub
End Class




Public Class FeatureLess_BrickList : Inherits TaskParent
    Public brickList As New List(Of cv.Rect)
    Dim index As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless grid rects."
    End Sub
    Public Function buildMap() As cv.Mat
        Dim input = dst1.Clone
        index = 1
        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In task.gridRects
            Dim val = input.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim count = cv.Cv2.FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                index += 1
            End If
        Next
        Return input
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        brickList.Clear()
        For Each r In task.gridRects
            If task.edges.dst2(r).CountNonZero = 0 Then
                dst1(r).SetTo(255)
                brickList.Add(r)
            End If
        Next

        dst3 = buildMap()
        brickList.Clear()
        For Each r In task.gridRects
            Dim val = dst1.Get(Of Byte)(r.Y, r.X)
            If val > 0 Then brickList.Add(r)
        Next
        dst2 = Palettize(dst3, 0)
        labels(2) = CStr(index) + " regions were found."
    End Sub
End Class






Public Class FeatureLess_ReductionTest : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Dim rcList As New List(Of rcData)
    Public Sub New()
        desc = "Identify each featureless region by index."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8u.Run(task.gray)
        dst1 = task.fLess.dst3
        dst2 = color8u.dst3
        dst3 = task.fLess.mask

        rcList.Clear()
        For i = 0 To task.fLess.regions.Count - 1
            Dim r = task.fLess.regions.Values(i)
            rcList.Add(New rcData(dst1(r), r, task.fLess.indexList.Values(i)))
        Next

        Dim rcIndex = Math.Abs(task.gOptions.DebugSlider.Value)
        If rcIndex < rcList.Count Then
            Dim rc = rcList(rcIndex)
            DrawTour(dst2(rc.rect), rc.contour, task.highlight, task.lineWidth)
            DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        End If
    End Sub
End Class






Public Class FeatureLess_CalcHist : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Dim histMapList As New List(Of (Index As Integer, histList As List(Of Integer)))
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        desc = "Find the LUT values in each featureless region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8u.Run(task.gray)
        dst1 = task.fLess.dst3
        dst2 = color8u.dst3

        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 255)}
        histMapList.Clear()
        For i = 0 To task.fLess.regions.Count - 1
            Dim r = task.fLess.regions.Values(i)
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({dst1(r)}, {0}, New cv.Mat, histogram, 1, {256}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)
            Dim histList As New List(Of Integer)
            For j = 0 To histArray.Count - 1
                If histArray(j) > 0 Then histList.Add(j)
            Next
            histMapList.Add((task.fLess.indexList.Values(i), histList))
        Next

        dst0.SetTo(0)
        For Each tup In histMapList
            Dim lutArray As Byte() = Enumerable.Repeat(CByte(0), 256).ToArray()
            For Each index In tup.histList
                lutArray(index) = tup.Index
            Next

            dst0 += color8u.dst2.LUT(lutArray)
        Next

        dst3 = Palettize(dst0)
    End Sub
End Class
