Imports System.Runtime.InteropServices
Imports System.Windows.Documents
Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class FeatureLess_Basics : Inherits TaskParent
    Public rectList As New List(Of cv.Rect)
    Public rectIndex As New List(Of Integer)
    Dim edges As New Edge_Canny
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        edges.Run(src)
        labels(3) = edges.labels(2)

        dst3 = src
        dst2.SetTo(0)
        rectList.Clear()
        rectIndex.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If edges.dst2(r).CountNonZero > 0 Then Continue For
            dst2(r).SetTo(255)
            dst3.Rectangle(r, white, task.lineWidth)
            rectList.Add(r)
            rectIndex.Add(i)
        Next

        labels(2) = CStr(rectList.Count) + " featureless grid squares"
    End Sub
End Class





Public Class NR_FeatureLess_Basics : Inherits TaskParent
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





Public Class FeatureLess_BasicsMotion : Inherits TaskParent
    Public fLessRaw As New FeatureLess_Basics
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
            rectList = New List(Of cv.Rect)(fLessRaw.rectList)
        End If

        ptList.Clear()
        Dim newList As New List(Of cv.Rect)
        ' remove any grid rects that had motion.
        For Each r In rectList
            Dim val = task.motion.motionMask.Get(Of Byte)(r.Y, r.X)
            If val <> 0 Then dst2(r).SetTo(0) Else newList.Add(r)
            ptList.Add(r.TopLeft)
        Next

        For Each r In fLessRaw.rectList
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





Public Class NR_FeatureLess_Correlation : Inherits TaskParent
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




Public Class NR_FeatureLess_Correlations : Inherits TaskParent
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






Public Class NR_FeatureLess_Contours : Inherits TaskParent
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







Public Class NR_FeatureLess_Canny : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim options As New Options_Sobel()
    Public Sub New()
        desc = "Use Canny edges to define featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(options.distanceThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class NR_FeatureLess_Sobel : Inherits TaskParent
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







Public Class NR_FeatureLess_UniquePixels : Inherits TaskParent
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







Public Class NR_FeatureLess_Unique3Pixels : Inherits TaskParent
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






Public Class NR_FeatureLess_Histogram : Inherits TaskParent
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










Public Class NR_FeatureLess_DCT : Inherits TaskParent
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







Public Class NR_FeatureLess_History : Inherits TaskParent
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
        Dim sqPixels = task.brickEdgeLen * task.brickEdgeLen
        labels(2) = "Current frame: " + Format(countCurr / sqPixels, fmt0) + " grid squares and " +
                        Format(countAll / sqPixels, fmt0) + " of all grid squares."
    End Sub
End Class








Public Class FeatureLess_LeftRight : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
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
    Public fLess As New FeatureLess_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use the FeatureLess_Basics output as input to RedColor_Basics"
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
    Dim fLess As New FeatureLess_Basics
    Dim diff As New Diff_Simple
    Public Sub New()
        desc = "Double-check that any differences from the previous fLess output occurred because of motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2

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





Public Class FeatureLess_Lines : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
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
        Dim minSize = task.brickEdgeLen * task.brickEdgeLen
        Dim countList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each r In fLess.fLessList
            Dim val = dst2.Get(Of Byte)(r.Y, r.X)
            If val = 255 Then
                Dim count = cv.Cv2.FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                If count > minSize Then
                    countList.Add(count, index)
                    index += 1
                Else
                    dst2(r).SetTo(0)
                End If
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
    Dim fLess As New FeatureLess_Basics
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






Public Class FeatureLess_FeaturesOld : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fLess As New FeatureLess_Basics
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
        Dim count = task.gridRects.Count - fLess.rectList.Count
        labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
    End Sub
End Class






Public Class FeatureLess_FeatureLines : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
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
        Dim count = task.gridRects.Count - fLess.rectList.Count
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
                rc.index = rcList.Count + 1
            Else
                Dim lastIndex = lastMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                rc.index = rcList.Count + 1
                If lastIndex > 0 And lastIndex < rcLastList.Count Then
                    rc.index = lastIndex
                    rc.age = rcLastList(lastIndex).age + 1
                End If
            End If
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            rcList.Add(rc)
        Next

        dst3 = Palettize(rcMap, 0)

        strOut = RedUtil_Basics.selectCell(rcMap, rcList)
        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class FeatureLess_ClusterFlood : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Public floodPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Identify the clusters in the FeatureLess_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray)
        dst2 = fLess.dst2.Clone
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







'Public Class FeatureLess_ClustersHist2D : Inherits TaskParent
'    Public fLess As New FeatureLess_Basics
'    Public histArray(task.histogramBins * task.histogramBins - 1) As Single
'    Public features As New cv.Mat
'    Public bpArray() As Single
'    Public Sub New()
'        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
'        desc = "Isolate clusters using a 2D histogram"
'    End Sub
'    Public Function backProjectHistArray(histogram As cv.Mat, ranges() As cv.Rangef) As cv.Mat
'        Dim backP As New cv.Mat
'        cv.Cv2.CalcBackProject({features}, {0, 1}, histogram, backP, ranges)
'        ReDim bpArray(histogram.Rows * histogram.Cols - 1)
'        backP.GetArray(Of Single)(bpArray)

'        Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
'        For i = 0 To fLess.rectList.Count - 1
'            dst(fLess.rectList(i)).SetTo(bpArray(i))
'        Next

'        Return dst
'    End Function
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        fLess.Run(task.gray)

'        Dim mmX = GetMinMax(fLess.grayMat)
'        Dim ranges() As cv.Rangef = {New cv.Rangef(mmX.minVal - 0.01, 255.01), New cv.Rangef(0, task.MaxZmeters)}

'        cv.Cv2.Merge({fLess.grayMat, fLess.depthMat}, features)

'        Dim histogram As New cv.Mat
'        Dim bins = task.histogramBins
'        cv.Cv2.CalcHist({features}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, ranges)

'        histogram = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
'        Dim floodIndex As Integer = 1
'        For y = 0 To histogram.Height - 1
'            For x = 0 To histogram.Width - 1
'                Dim pt = New cv.Point(x, y)
'                Dim val = histogram.Get(Of Single)(y, x)
'                If val = 255 Then
'                    histogram.FloodFill(pt, floodIndex)
'                    floodIndex += 1
'                    If floodIndex >= 254 Then Exit For
'                End If
'            Next
'        Next

'        histogram.GetArray(Of Single)(histArray)

'        dst3 = backProjectHistArray(histogram, ranges)
'        Dim clusterCount = GetMinMax(dst3).maxVal - 1
'        dst2 = Palettize(dst3, 0)

'        labels(2) = CStr(clusterCount) + " clusters were found for the " + "X scale is mean grayscale color and the Y scale is mean depth."
'        labels(3) = CStr(clusterCount) + " clusters were identified."
'    End Sub
'End Class




Public Class FeatureLess_Predict : Inherits TaskParent
    Dim ml As New ML_RandomForest
    Dim edges As New Edge_Canny
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
        If src.Channels <> 1 Then src = task.gray
        edges.Run(src)

        Dim rectCount = task.gridRects.Count
        Dim inputVariableCount As Integer = 3
        Dim flat(rectCount * inputVariableCount - 1) As Single
        Dim colorDepth As New List(Of Single)
        Dim edgeCounts As New List(Of Integer)
        Dim index As Integer
        For Each r In task.gridRects
            Dim edgeCount = edges.dst2(r).CountNonZero
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
        Dim mmX = GetMinMax(src)
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






Public Class FeatureLess_PredictIndex : Inherits TaskParent
    Dim feat As New FeatureLess_Features
    Dim ml As New ML_RandomForest
    Public Sub New()
        desc = "Predict the index for each featureLess region using the features Mat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.gray)
        dst2 = feat.dst2
        dst1 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        labels(2) = feat.labels(2)

        Dim features = cv.Mat.FromPixelData(feat.idList.Count, feat.inputVariableCount, cv.MatType.CV_32F, feat.featureList.ToArray)

        Dim singleEntry(feat.inputVariableCount * 2 - 1) As Single
        Static saveIDList As List(Of Single)
        Static savefeatures As List(Of Single)
        If task.heartBeatLT Then
            savefeatures = feat.featureList
            saveIDList = feat.idList
            ml.trainMat = features
            ml.trainResponse = cv.Mat.FromPixelData(feat.idList.Count, 1, cv.MatType.CV_32F, feat.idList.ToArray)
            ml.predictions = ml.trainResponse.Clone
            dst3 = feat.dst3
        Else
            ' ml.testMat = features

            For i = 0 To feat.inputVariableCount * 2 - 1
                singleEntry(i) = Math.Floor(feat.featureList(i))
            Next

            ml.testMat = cv.Mat.FromPixelData(2, feat.inputVariableCount, cv.MatType.CV_32F, singleEntry)
        End If

        ml.Run(emptyMat)

        Dim tmp(ml.testMat.Rows - 1) As Single
        Dim predictions(ml.testMat.Rows - 1) As Integer

        Marshal.Copy(ml.predictions.Data, tmp, 0, tmp.Length)
        For i = 0 To predictions.Count - 1
            predictions(i) = CInt(tmp(i))
        Next

        'Dim colors(255) As cv.Vec3b
        'Dim maxClass As Integer
        'For i = 0 To ml.predictions.Rows - 1
        '    colors(i) = task.vecColors(predictions(i))
        '    If predictions(i) > maxClass Then maxClass = predictions(i)
        'Next

        'Dim colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, colors)
        'cv.Cv2.ApplyColorMap(dst2, dst3, colorMap)
        'dst3.SetTo(0, dst1)
    End Sub
End Class





Public Class FeatureLess_Features : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
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
        Dim minSize = task.gridWH * task.gridWH
        For Each r In task.gridRects
            If dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X) = 255 Then
                Dim flags = cv.FloodFillFlags.FixedRange Or (index << 8)
                Dim Count = cv.Cv2.FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                If Count > minSize Then
                    Dim rc = New rcData(mask(rect), rect, index)
                    rcList.Add(rc)

                    idList.Add(CSng(index))

                    featureList.Add(src(rect).Mean(rc.mask)(0))
                    featureList.Add(rc.wGrid.Z)

                    featureList.Add(rc.pixels)
                    featureList.Add(rc.maxDist.X)
                    featureList.Add(rc.maxDist.Y)
                    index += 1
                Else
                    dst2(r).SetTo(0)
                End If
            End If
        Next

        dst3 = Palettize(dst2, 0)

        labels(2) = CStr(index) + " regions were found."
    End Sub
End Class



Public Class FeatureLess_IndexKNN : Inherits TaskParent
    Dim feat As New FeatureLess_Features
    Dim knn As New KNN_NNBasicsRaw
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        knn.dimension = feat.inputVariableCount
        desc = "Predict the index for each featureLess region using the features Mat."
    End Sub
    Private Function foundLast(rc As rcData, lrc As rcData) As rcData
        rc.age = lrc.age + 1
        If rc.age >= 1000 Then rc.age = 10
        rc.index = rc.indexLast
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
        knn.Run(emptyMat)

        Static maxDistList As New List(Of cv.Point)
        For Each rc In feat.rcList
            Dim lastIndex = lastImage.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
            If lastIndex >= 0 And lastIndex < lastList.Count Then
                rc.indexLast = lastIndex
                rc = foundLast(rc, lastList(lastIndex))
            Else
                'If maxDistList.Contains(rc.maxDStable) Then
                '    rc.indexLast = maxDistList.IndexOf(rc.maxDStable)
                '    rc = foundLast(rc, lastList(rc.indexLast))
                'End If
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
            maxDistList.Add(rc.maxDStable)
            If task.heartBeat Then rc.color = task.scalarColors(rc.index + 1)
        Next

        strOut = RedUtil_Basics.selectCell(dst2, feat.rcList)
        SetTrueText(strOut, 1)

        labels(2) = CStr(knn.trainMat.Rows) + " featureless clusters were found."
    End Sub
End Class
