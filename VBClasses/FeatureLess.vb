Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureLess_Basics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst1.Size, MatType.CV_8U, 0)
        Dim rcListLast As New List(Of rcData)
        Public fLessBricks As New List(Of cv.Rect)
        Public Sub New()
            desc = "Identify featureless regions."
        End Sub
        Public Shared Function MyFinder(myList As List(Of rcData), id As Integer) As rcData
            If id = 0 Then Return Nothing
            For Each rc In myList
                If rc.ID = id Then Return myList(rc.index)
            Next
            Return Nothing
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.algorithmActive Then
                SetTrueText(traceName + " is a task algorithm that has already run", 3)
                dst2 = task.fless.dst2
                labels(2) = task.fless.labels(2)
                trueData = New List(Of TrueText)(task.fless.trueData)
                Exit Sub ' this is a task.algorithm that has already run...
            End If

            rcListLast = New List(Of rcData)(rcList)

            rcMap.SetTo(0)
            fLessBricks.Clear()
            For Each r In task.gridRects
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
                If r.Height <> task.gridWH Or r.Width <> task.gridWH Then Continue For ' odd sizes.
                rcMap(r).SetTo(255)
                fLessBricks.Add(r)
            Next

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(rcMap.Width + 2, rcMap.Height + 2), MatType.CV_8U, 0)
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim rc As rcData
            Dim index As Integer = 1
            Dim flags = FloodFillFlags.FixedRange Or (255 << 8)
            For Each r In fLessBricks
                If rcMap.Get(Of Byte)(r.Y, r.X) = 255 Then
                    Dim count = FloodFill(rcMap, mask, r.TopLeft, index, rect, 0, 0, flags)
                    ' it is difficult to track cells that are narrow...
                    If rect.Width > task.gridWH And rect.Height > task.gridWH Then
                        rc = New rcData(rcMap(rect), rect, index)
                        sortList.Add(rc.pixels, rc)
                    End If
                End If
            Next

            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            rcMap.SetTo(0)
            For Each rc In sortList.Values
                rc.index = rcList.Count
                Dim pt = WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.inverseM)
                pt = validatePoint(pt)
                rc.ID = task.delaunayFLess.idMap.Get(Of Byte)(pt.Y, pt.X)

                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rc.pixels = CountNonZero(rc.mask)
                rcList.Add(rc)
            Next

            For Each rc In rcList
                If rc.index = 0 Then Continue For
                If rc.motionPixels = 0 Then
                    Dim rclast = MyFinder(rcListLast, rc.ID)
                    If rclast IsNot Nothing Then
                        rc.age = rclast.age + 1
                        If rc.age >= 1000 Then rc.age = 10
                        rc.maxDStable = rclast.maxDStable
                    End If
                End If
                ' SetTrueText(CStr(rc.age), rc.maxDStable)
            Next

            dst2 = Palettize(rcMap, 0)

            For Each rc In rcList
                If rc.index = 0 Then Continue For
                SetTrueText(CStr(rc.ID), rc.maxDist)
            Next

            Dim rcNext = MyFinder(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
            If rcNext IsNot Nothing Then
                task.rcD = rcNext
                Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
                SetTrueText(task.rcD.displayCell, 3)
            Else
                If rcList.Count > 1 Then task.rcD = rcList(1)
            End If

            labels(2) = CStr(rcList.Count - 1) + " featureless regions found.  Age is shown."
        End Sub
    End Class





    Public Class XR_FeatureLess_Core : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless gridrects that also have depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastIndex = dst1.Clone
            Dim rcLastList = New List(Of rcData)(rcList)

            dst1.SetTo(0)
            For i = 0 To task.gridRects.Count - 1
                Dim r = task.gridRects(i)
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
                If r.Height <> task.gridWH Or r.Width <> task.gridWH Then Continue For ' odd sizes.
                dst1(r).SetTo(255)
            Next

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(dst1.Width + 2, dst1.Height + 2), MatType.CV_8U, 0)
            Dim nextList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim flags = FloodFillFlags.FixedRange Or (255 << 8)
            For Each r In task.gridRects
                If dst1.Get(Of Byte)(r.Y, r.X) = 255 Then
                    Dim count = FloodFill(dst1, mask, r.TopLeft, nextList.Count + 1, rect, 0, 0, flags)
                    If count = 0 Or rect.Width = 0 Or rect.Height = 0 Then Continue For
                    Dim rc = New rcData(mask(rect), rect, 255) With {.pixels = count}
                    nextList.Add(rc.pixels, rc)
                End If
            Next

            rcList = New List(Of rcData)(nextList.Values)

            Dim usedList As New List(Of Integer)
            Dim nextFree As Integer = 1
            For Each rcPrev In rcLastList
                If rcPrev.index >= nextFree Then nextFree = rcPrev.index + 1
            Next
            If nextFree > 255 Then nextFree = 1

            For Each rc In rcList
                Dim floodIndex = rc.ID
                InRange(dst1(rc.rect), floodIndex, floodIndex, rc.mask)
                If CountNonZero(rc.mask) > 0 Then rc.maxDist = rc.buildMaxDist(rc.mask)

                Dim prevIndex = lastIndex.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)

                Dim rcLast As rcData = Nothing
                If prevIndex > 0 Then
                    For Each rcPrev In rcLastList
                        If rcPrev.index = prevIndex Then
                            rcLast = rcPrev
                            Exit For
                        End If
                    Next
                End If

                If rcLast IsNot Nothing AndAlso rc.rect.IntersectsWith(rcLast.rect) AndAlso
                   usedList.Contains(prevIndex) = False Then
                    rc.ID = prevIndex
                    rc.age = rcLast.age + 1
                    If rc.age >= 1000 Then rc.age = 100
                Else
                    While usedList.Contains(nextFree) Or nextFree > 255
                        nextFree += 1
                        If nextFree > 255 Then nextFree = 1
                        If usedList.Count >= 255 Then Exit While
                    End While
                    rc.ID = nextFree
                    nextFree += 1
                End If
                usedList.Add(rc.ID)
                dst1(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            For i = 0 To rcList.Count - 1
                rcList(i).index = i
            Next

            dst2 = Palettize(dst1, 0)

            If standaloneTest() Then
                For Each rc In rcList
                    SetTrueText(CStr(rc.age), rc.maxDist)
                Next
            End If
            labels(2) = CStr(rcList.Count) + " featureless regions found with max index = " + CStr(nextFree - 1)
        End Sub
    End Class




    Public Class XR_FeatureLess_BasicsOld : Inherits TaskParent
        Public regions As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        Public indexList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Public brickList As New List(Of cv.Rect)
        Dim index As Integer
        Dim rect As cv.Rect
        Public mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
            labels(3) = "CV_8U representation of the regions"
            desc = "Identify featureless grid rects."
        End Sub
        Public Function buildMap(input As Mat) As Mat
            mask.SetTo(0)
            For Each r In task.gridRects
                Dim val1 = input(r).Get(Of Byte)(0, 0)
                Dim val2 = dst3(r).Get(Of Byte)(0, 0)
                If val1 = 255 And val2 > 0 Then
                    index = val2
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                    regions.Add(count, ValidateRect(rect))
                    indexList.Add(count, index)
                End If
            Next
            Return input
        End Function
        Public Function buildMapHeartBeat(input As Mat) As Mat
            mask.SetTo(0)
            index = 1
            For Each r In task.gridRects
                Dim val = input.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
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
                If CountNonZero(task.edges.dst2(r)) = 0 Then
                    dst1(r).SetTo(255)
                    brickList.Add(r)
                End If
            Next

            regions.Clear()
            indexList.Clear()
            If heartbeatLTX() Then dst3 = buildMapHeartBeat(dst1.Clone) Else dst3 = buildMap(dst1.Clone)

            index = regions.Count + 1
            For Each r In task.gridRects
                If dst3(r).Get(Of Byte)(0, 0) = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(dst3, mask, r.TopLeft, index, rect, 0, 0, flags)
                    regions.Add(count, ValidateRect(rect))
                    indexList.Add(count, index)
                    index += 1
                End If
            Next
            dst2 = Palettize(dst3, 0)

            labels(2) = CStr(regions.Count) + " regions were found."
        End Sub
    End Class





    Public Class XR_FeatureLess_BasicsCore : Inherits TaskParent
        Public brickList As New List(Of cv.Rect)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless grid rects."
        End Sub
        Public Shared Function buildMap(brickList As List(Of cv.Rect), input As Mat) As Mat
            Dim index = 1
            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            For Each r In brickList
                Dim val = input.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
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
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
                dst1(r).SetTo(255)
                brickList.Add(r)
            Next
            Dim countRects = brickList.Count

            dst1 = buildMap(brickList, dst1)
            dst2 = Palettize(dst1, 0)

            labels(2) = CStr(brickList.Count) + " featureless grid regions "
        End Sub
    End Class





    Public Class XR_FeatureLess_BasicsOldTest : Inherits TaskParent
        Public brickList As New List(Of cv.Rect)
        Public regionList As New List(Of List(Of cv.Rect))
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless grid rects using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Function buildMap(input As Mat) As Mat
            Dim regionIndex As Integer = regionList.Count
            regionList.Clear()

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            Dim usedColors As New List(Of Byte)
            For Each r In brickList
                Dim val = input.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim index As Integer = dst3.Get(Of Byte)(r.Y, r.X)
                    If index = 0 Or usedColors.Contains(index) Then
                        regionIndex += 1
                        index = regionIndex
                    End If
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
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
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
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
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
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
                        Rectangle(dst3, r, white, task.lineWidth)
                        fLessList.Add(r)
                    End If
                Next
            End If

            'Dim index As Integer = 1
            'For Each r In task.gridRects
            '    Dim val = dst2.Get(Of Byte)(r.Y, r.X)
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
        Public rcList As New List(Of rcData)
        Public fLessNot As New List(Of cv.Rect)
        Public ptList As New HashSet(Of cv.Point)
        Public Sub New()
            desc = "A features grid rect cannot change if there has been no motion in that grid rect."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then
                dst2 = task.fless.dst3.Clone
                rcList = New List(Of rcData)(task.fless.rcList)
            End If

            ptList.Clear()
            Dim newList As New List(Of rcData)
            ' remove any grid rects that had motion.
            For Each rc In rcList
                Dim val = task.motion.motionMask.Get(Of Byte)(rc.rect.Y, rc.rect.X)
                If val <> 0 Then dst2(rc.rect).SetTo(0) Else newList.Add(rc)
                ptList.Add(rc.rect.TopLeft)
            Next

            For Each rc In task.fless.rcList
                Dim val = task.motion.motionMask.Get(Of Byte)(rc.rect.Y, rc.rect.X)
                If val = 0 Then
                    If ptList.Contains(rc.rect.TopLeft) = False Then
                        newList.Add(rc)
                        dst2(rc.rect).SetTo(255)
                    End If
                End If
            Next

            fLessNot.Clear()
            For i = 0 To task.gridRects.Count - 1
                Dim r = task.gridRects(i)
                If dst2.Get(Of Byte)(r.Y, r.X) = 0 Then fLessNot.Add(r)
            Next

            If newList.Count > 0 Then rcList = New List(Of rcData)(newList)

            labels(2) = task.fless.labels(2)
        End Sub
    End Class




    Public Class XR_FeatureLess_Correlation1 : Inherits TaskParent
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
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            ' couldn't put this in the constructor because motion is a task algorithm.
            If task.firstPass Then smallGrid.Run(src) ' create the grid squares for the small resolution.
            Dim input As Mat
            If src.Channels <> 1 Then input = task.gray Else input = src

            ' why do this resize?  Because the options.flessThreshold works at smallRes but not larger resolutions.
            If task.workRes.Height > 270 Then Resize(input, input, task.smallRes, 0, 0, InterpolationFlags.Nearest)

            Dim mask As New Mat(input.Size, MatType.CV_8U, 0)
            For Each r In smallGrid.gridRects
                r = ValidateRect(r)
                Dim mm = GetMinMax(input(r))
                If mm.range < options.fLessThreshold Then mask(r).SetTo(255)
            Next

            Resize(mask, dst2, src.Size, 0, 0, InterpolationFlags.Nearest)

            fLessList.Clear()
            For Each r In task.gridRects
                If dst2.Get(Of Byte)(r.Y, r.X) Then fLessList.Add(r)
            Next

            If standaloneTest() Then
                dst3 = task.gray.Clone
                For Each r In fLessList
                    Rectangle(dst3, r, white, task.lineWidth)
                Next

                Dim index = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
                Dim mm = GetMinMax(task.gray(task.gridRects(index)))
                SetTrueText("Click on any grid rect to see its grayscale range." + vbCrLf +
                            "Min gray = " + mm.minVal.ToString(fmt0) + vbCrLf +
                            "Max Gray = " + mm.maxVal.ToString(fmt0) + vbCrLf +
                            "Range = " + mm.range.ToString(fmt0) + vbCrLf + vbCrLf, 1)
            End If

            labels(3) = CStr(fLessList.Count) + " grid squares were found to be featureless (range < " +
                        CStr(options.fLessThreshold) + ")"

        End Sub
    End Class




    Public Class XR_FeatureLess_Correlations : Inherits TaskParent
        Dim corr As New Correlation_BasicsPlot
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
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
                    If standaloneTest() Then Rectangle(src, r, white, task.lineWidth)
                End If
            Next
        End Sub
    End Class






    Public Class XR_FeatureLess_Contours : Inherits TaskParent
        Dim edgeline As New EdgeLine_Basics
        Dim contours As New Contour_Finder
        Public Sub New()
            desc = "Use Contour_Finder to get the contour data for the top contours by size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels = 1 Then edgeline.Run(src) Else edgeline.Run(task.gray)
            If src.Type <> MatType.CV_8U Then
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
            Threshold(Not edges.dst2, dst2, options.distanceThreshold, 255, ThresholdTypes.Binary)
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
            CvtColor(fless.dst2, dst2, ColorConversionCodes.BGR2GRAY)
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
            CvtColor(fLessTopX.dst2, dst2, ColorConversionCodes.BGR2GRAY)

            sort3.Run(fLessTopX.dst2)
            dst3 = sort3.dst2
        End Sub
    End Class






    Public Class XR_FeatureLess_Histogram : Inherits TaskParent
        Dim backP As New XR_BackProject_FeatureLess
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
                        Dim floodCount = FloodFill(mask, pt, regionCount)
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

            Dim label As New Mat

            InRange(mask, maxIndex + 1, maxIndex + 1, label)
            Dim nonZ = CountNonZero(label)
            labels(3) = "Largest FeatureLess Region (" + CStr(nonZ) + " " + (nonZ / label.Total).ToString("#0.0%") + " pixels)"
            dst3.SetTo(white, label)
        End Sub
    End Class







    Public Class XR_FeatureLess_History : Inherits TaskParent
        Dim frames As New History_Basics
        Dim fLess As New XR_FeatureLess_Correlation
        Public Sub New()
            labels(3) = "The brighter the grid square, the more recent appearance."
            desc = "Accumulate the edges over a span of X images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.grayOriginal)

            Threshold(fLess.dst2, dst2, 0, 255, ThresholdTypes.Binary)

            frames.Run(dst2)
            dst3 = frames.dst2

            Dim countCurr = CountNonZero(dst2)
            Dim countAll = CountNonZero(dst3)
            Dim sqPixels = task.gridWH * task.gridWH
            labels(2) = "Current frame: " + (countCurr / sqPixels).ToString(fmt0) + " grid squares and " +
                        (countAll / sqPixels).ToString(fmt0) + " of all grid squares."
        End Sub
    End Class








    Public Class XR_FeatureLess_LeftRight : Inherits TaskParent
        Dim fLessL As New FeatureLess_Basics
        Dim fLessR As New FeatureLess_Basics
        Public Sub New()
            labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
            desc = "Find the featureless regions of the left and right images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLessL.Run(task.leftView)
            dst2 = fLessL.dst2
            labels(2) = fLessL.labels(2)

            fLessR.Run(task.rightView)
            dst3 = fLessR.dst2
            labels(3) = fLessR.labels(2)
        End Sub
    End Class





    Public Class XR_FeatureLess_Stabilized : Inherits TaskParent
        Dim diff As New Diff_Simple
        Public Sub New()
            labels(2) = "CV_8U version of the featureless cells."
            desc = "Double-check that any differences from the previous fLess output occurred because of motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.fless.rcMap
            labels(3) = task.fless.labels(2)

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
            dst3 = Palettize(dst2, 0)
        End Sub
    End Class





    Public Class XR_FeatureLess_Lines1 : Inherits TaskParent
        Dim fLess As New XR_FeatureLess_DepthFull
        Dim ranges() As Rangef = New Rangef() {New Rangef(0, 256)}
        Public lpList As New List(Of lpData)
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Find and display lines that are between featureless regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.grayOriginal)
            dst2 = fLess.dst2
            labels(2) = fLess.labels(2)

            dst1.SetTo(0)
            task.lines.dst1.CopyTo(dst1, fLess.dst1)

            Dim histogram As New Mat
            CalcHist({dst1}, {0}, New Mat, histogram, 1, {256}, ranges)

            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            dst3.SetTo(0)
            lpList.Clear()
            For i = 1 To Math.Min(task.lines.lpList.Count, histArray.Length) - 1
                If histArray(i) > 0 Then
                    Dim lp = task.lines.lpList(i)
                    Line(dst3, lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
                    lpList.Add(lp)
                End If
            Next

            labels(2) = fLess.labels(2)
            labels(3) = CStr(lpList.Count) + " lines intersected with featureless grid rects."
        End Sub
    End Class





    Public Class XR_FeatureLess_Cells : Inherits TaskParent
        Dim saveColorMap As Mat
        Dim fLess As New XR_FeatureLess_Correlation
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
            Dim mask = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim flags As FloodFillFlags = FloodFillFlags.Link4
            Dim countList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each r In fLess.fLessList
                Dim val = dst2.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim count = FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                    countList.Add(count, index)
                    index += 1
                End If
            Next

            ' this will color the regions in size order.
            For i = 0 To countList.Count - 1
                Dim countIndex = countList.Values.ElementAt(i)
                task.colorMap.Set(Of Vec3b)(countIndex, 0, saveColorMap.Get(Of Vec3b)(i, 0))
            Next
            dst3 = Palettize(dst2, 0)
            labels(2) = CStr(index - 1) + " featureless regions were found below (8UC1)."
        End Sub
    End Class





    Public Class XR_FeatureLess_NotImages : Inherits TaskParent
        Dim fLess As New XR_FeatureLess_DepthFull
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
        Dim fLess As New XR_FeatureLess_DepthFull
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
            desc = "Isolate features in the not of the featureless regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray.Clone)

            dst2.SetTo(0)
            task.color.CopyTo(dst2, Not fLess.dst2)

            feat.Run(dst2)

            For Each pt In feat.ptlist
                Circle(dst2, pt, task.DotSize + 2, task.highlight, -1)
            Next

            For Each lp In task.lines.lpList
                Line(dst2, lp.p1, lp.p2, black, task.lineWidth)
            Next
            Dim count = task.gridRects.Count - fLess.brickList.Count
            labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
        End Sub
    End Class






    Public Class XR_FeatureLess_FeatureLines : Inherits TaskParent
        Dim fLess As New XR_FeatureLess_DepthFull
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
            desc = "Use lines to further divide featureless from features."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray.Clone)
            dst1 = fLess.dst3.Clone
            dst3 = fLess.dst3.Clone

            dst2.SetTo(0)
            task.color.CopyTo(dst2, Not dst1)

            For i = 1 To task.gridRects.Count - 1
                Dim r1 = task.gridRects(i - 1)
                Dim r2 = task.gridRects(i)
                Dim p1 = r1.TopLeft
                Dim p2 = r2.TopLeft
                If p1.X < p2.X Then
                    Dim val1 = dst1.Get(Of Byte)(p1.Y, p1.X)
                    Dim val2 = dst1.Get(Of Byte)(p2.Y, p2.X)
                    If val1 = 255 And val2 = 0 Then
                        ' line present?
                        If CountNonZero(task.lines.dst1(r2)) Then
                            task.lines.dst0(r2).CopyTo(dst1(r2))
                            InRange(dst3(r2), 0, 0, dst3(r2))
                            dst3(r2).SetTo(0, task.lines.dst1(r2))
                        End If
                    End If

                    If val1 = 0 And val2 = 255 Then
                        ' line present?
                        If CountNonZero(task.lines.dst1(r1)) Then
                            task.lines.dst0(r1).CopyTo(dst1(r1))
                            InRange(dst3(r1), 0, 0, dst3(r1))
                            dst3(r1).SetTo(0, task.lines.dst1(r1))
                        End If
                    End If
                End If
            Next

            For Each lp In task.lines.lpList
                Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next
            Dim count = task.gridRects.Count - fLess.brickList.Count
            labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
        End Sub
    End Class






    Public Class XR_FeatureLess_Predict : Inherits TaskParent
        Dim ml As New ML_RandomForest
        Public clusters() As Single
        Dim ranges() As Rangef
        Public bpArray() As Single
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
            desc = "Use edges, depth, color, and location to predict featureless regions."
        End Sub
        Public Function backProjectHist(histogram As Mat, histInput As Mat, edgeCounts As List(Of Integer)) As Single()
            Dim backP As New Mat
            CalcBackProject({histInput}, {0, 1}, histogram, backP, ranges)
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
                Dim edgeCount = CountNonZero(task.edges.dst2(r))
                edgeCounts.Add(edgeCount)

                flat(index) = Mean(src(r))(0)
                flat(index + 1) = Mean(task.pcSplit(2)(r), task.depthmask(r))(0)
                If edgeCount = 0 Then
                    colorDepth.Add(flat(index))
                    colorDepth.Add(flat(index + 1))
                End If
                flat(index + 2) = edgeCount
                'flat(index + 3) = CSng(r.X)
                'flat(index + 4) = CSng(r.Y)
                index += inputVariableCount
            Next

            ml.testMat = Mat.FromPixelData(rectCount, inputVariableCount, MatType.CV_32F, flat)
            Dim mmX = GetMinMax(task.gray)
            ranges = {New Rangef(mmX.minVal - 0.01, 255.01), New Rangef(0, task.MaxZmeters)}

            Dim trainLabels(rectCount - 1) As Single
            If heartbeatLTX() Then
                Dim histogram As New Mat
                Dim histInput = Mat.FromPixelData(colorDepth.Count \ 2, 1, MatType.CV_32FC2, colorDepth.ToArray())
                CalcHist({histInput}, {0, 1}, New Mat(), histogram, 2, {task.histogramBins, task.histogramBins}, ranges)

                Threshold(histogram, histogram, 0, 255, ThresholdTypes.Binary)

                Dim floodIndex As Integer = 1
                For y = 0 To histogram.Height - 1
                    For x = 0 To histogram.Width - 1
                        Dim pt = New cv.Point(x, y)
                        Dim val = histogram.Get(Of Single)(y, x)
                        If val = 255 Then
                            FloodFill(histogram, pt, floodIndex)
                            floodIndex += 1
                            If floodIndex >= 254 Then Exit For
                        End If
                    Next
                Next

                Dim gridList = backProjectHist(histogram, histInput, edgeCounts)
                ml.trainResponse = Mat.FromPixelData(rectCount, 1, MatType.CV_32F, gridList)
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






    Public Class XR_FeatureLess_Features : Inherits TaskParent
        Dim fLess As New XR_FeatureLess_DepthFull
        Public featureList As New List(Of Single)
        Public idList As New List(Of Single)
        Public inputVariableCount As Integer = 5
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            desc = "Expanded floodfill usage for the featureLess image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray.Clone)
            dst2 = fLess.dst2

            Dim rect As New cv.Rect
            Dim mask = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim index As Integer = 1
            featureList.Clear()
            idList.Clear()
            rcList.Clear()
            For Each r In task.gridRects
                If dst2.Get(Of Byte)(r.Y, r.X) = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim Count = FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                    Dim rc = New rcData(mask(rect), rect, index)
                    rcList.Add(rc)

                    idList.Add(CSng(index))

                    featureList.Add(Mean(src(rect), rc.mask)(0))
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



    Public Class XR_FeatureLess_IndexKNN : Inherits TaskParent
        Dim feat As New XR_FeatureLess_Features
        Dim knn As New KNN_IndividualQuery
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            knn.dimension = feat.inputVariableCount
            desc = "Predict the index for each featureLess region using the features Mat."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastImage = feat.dst2.Clone
            Dim lastList = New List(Of rcData)(feat.rcList)

            feat.Run(task.gray.Clone)
            dst2 = feat.dst2
            labels(2) = feat.labels(2)

            Dim nVal = knn.dimension
            Dim queries = Mat.FromPixelData(feat.featureList.Count \ nVal, nVal, MatType.CV_32F, feat.featureList.ToArray)

            knn.trainMat = Mat.FromPixelData(queries.Rows, nVal, MatType.CV_32F, feat.featureList.ToArray)

            If feat.featureList.Count = 0 Then Exit Sub ' nothing to work with...

            knn.Run(emptyMat)

            Static maxDistList As New List(Of cv.Point)
            For Each rc In feat.rcList
                Dim lastIndex = lastImage.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
                If lastIndex >= 0 And lastIndex < lastList.Count Then
                    rc.age += 1
                    If rc.age >= 1000 Then rc.age = 100
                End If
            Next

            dst3.SetTo(0)
            For Each rc In feat.rcList
                dst3(rc.rect).SetTo(task.scalarColors(rc.ID Mod 255), rc.mask)
                SetTrueText(CStr(rc.age), rc.maxDist, 3)
            Next

            maxDistList.Clear()
            For Each rc In feat.rcList
                Rectangle(dst3, rc.rect, task.highlight, task.lineWidth)
                maxDistList.Add(rc.maxDist)
            Next

            'strOut = Utility_Basics.selectCell(dst2, feat.rcList)
            'SetTrueText(strOut, 1)

            labels(2) = CStr(knn.trainMat.Rows) + " featureless clusters were found."
        End Sub
    End Class







    Public Class XR_FeatureLess_ClustersHist2D : Inherits TaskParent
        Public fLess As New XR_FeatureLess_DepthFull
        Public histArray(task.histogramBins * task.histogramBins - 1) As Single
        Public features As New Mat
        Public bpArray() As Single
        Public Sub New()
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
            desc = "Isolate clusters using a 2D histogram"
        End Sub
        Public Function backProjectHistArray(histogram As Mat, ranges() As Rangef) As Mat
            Dim backP As New Mat
            CalcBackProject({features}, {0, 1}, histogram, backP, ranges)
            ReDim bpArray(histogram.Rows * histogram.Cols - 1)
            backP.GetArray(Of Single)(bpArray)

            Dim dst As New Mat(task.workRes, MatType.CV_8U, 0)
            For i = 0 To fLess.brickList.Count - 1
                dst(fLess.brickList(i)).SetTo(bpArray(i))
            Next

            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            Dim mmX = GetMinMax(fLess.dst1)
            Dim ranges() As Rangef = {New Rangef(mmX.minVal - 0.01, 255.01), New Rangef(0, task.MaxZmeters)}

            Dim depthMat = task.pcSplit(2).Clone
            depthMat.SetTo(0, Not fLess.dst1)

            Dim grayMat As New Mat
            fLess.dst2.ConvertTo(grayMat, MatType.CV_32F)

            Dim grayMats() As Mat = {grayMat, depthMat}
            Merge(grayMats, features)

            Dim histogram As New Mat
            Dim bins = task.histogramBins
            CalcHist({features}, {0, 1}, New Mat(), histogram, 2, {bins, bins}, ranges)

            Threshold(histogram, histogram, 0, 255, ThresholdTypes.Binary)
            Dim floodIndex As Integer = 1
            For y = 0 To histogram.Height - 1
                For x = 0 To histogram.Width - 1
                    Dim pt = New cv.Point(x, y)
                    Dim val = histogram.Get(Of Single)(y, x)
                    If val = 255 Then
                        FloodFill(histogram, pt, floodIndex)
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







    Public Class XR_FeatureLess_XLines : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Dim fLess As New XR_FeatureLess_BasicsOld
        Public Sub New()
            desc = "Find horizontal and vertical lines through the center of featureless grid rects."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)
            dst1 = fLess.dst3
            dst2 = fLess.dst2

            lpList.Clear()
            For y = task.gridWH To dst1.Height - 1 Step task.gridWH
                Dim p1 = New cv.Point, p2 = New cv.Point
                Dim val1 As Integer, val2 As Integer
                For x = 0 To dst1.Width - 1 Step task.gridWH
                    val1 = dst1.Get(Of Byte)(y, x)
                    val2 = If(x + task.gridWH >= dst2.Width, 0, dst1.Get(Of Byte)(y, x + task.gridWH))
                    If val1 <> val2 Or (x = 0 And val1 <> 0) Then
                        If val1 = 0 Then p1 = New cv.Point(x + task.gridWH, y)
                        If val1 <> 0 And (x = 0 And val1 <> 0) Then p1 = New cv.Point(CInt(x), CInt(y))
                        If val2 = 0 Then p2 = New cv.Point(x + task.gridWH - 1, y)
                        If p1 <> New cv.Point And p2 <> New cv.Point Then
                            Dim lp As New lpData(p1, p2) With {.fLessID = If(val1 = 0, val2, val1)}
                            lpList.Add(lp)
                            Line(dst2, p1, p2, white, task.lineWidth)
                            p1 = New cv.Point
                            p2 = New cv.Point
                        End If
                    End If
                Next
            Next
            labels(2) = CStr(lpList.Count) + " horizontal lines encountered with depth"
        End Sub
    End Class





    Public Class XR_FeatureLess_YLines : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Dim fLess As New XR_FeatureLess_BasicsOld
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            desc = "Find horizontal and vertical lines through the center of featureless grid rects."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)
            dst1 = fLess.dst3
            dst2 = fLess.dst2

            lpList.Clear()
            For x = task.gridWH To dst1.Width - 1 Step task.gridWH
                Dim p1 = New cv.Point, p2 = New cv.Point
                Dim val1 As Integer, val2 As Integer
                For y = 0 To dst1.Height - 1 Step task.gridWH
                    val1 = dst1.Get(Of Byte)(y, x)
                    val2 = If(y + task.gridWH >= dst2.Height, 0, dst1.Get(Of Byte)(y + task.gridWH, x))
                    If val1 <> val2 Or (y = 0 And val1 <> 0) Then
                        If val1 = 0 Then p1 = New cv.Point(x, y + task.gridWH)
                        If val1 <> 0 And (y = 0 And val1 <> 0) Then p1 = New cv.Point(CInt(x), CInt(y))
                        If val2 = 0 Then p2 = New cv.Point(x, y + task.gridWH - 1)
                        If p1 <> New cv.Point And p2 <> New cv.Point Then
                            Dim lp As New lpData(p1, p2) With {.fLessID = If(val1 = 0, val2, val1)}
                            lpList.Add(lp)
                            Line(dst2, p1, p2, white, task.lineWidth)
                            p1 = New cv.Point
                            p2 = New cv.Point
                        End If
                    End If
                Next
            Next

            labels(2) = CStr(lpList.Count) + " horizontal lines encountered with depth"
        End Sub
    End Class





    Public Class XR_FeatureLess_Lines : Inherits TaskParent
        Dim xLines As New XR_FeatureLess_XLines
        Dim yLines As New XR_FeatureLess_YLines
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
                        Line(dst3, lp.p1, lp.p2, white, task.lineWidth)
                    End If
                Next
                For Each lp In yLines.lpList
                    If lp.fLessID = selectedRegion Then
                        Line(dst3, lp.p1, lp.p2, white, task.lineWidth)
                    End If
                Next
            End If
        End Sub
    End Class





    Public Class XR_FeatureLess_BasicsOldTest2 : Inherits TaskParent
        Public brickList As New List(Of cv.Rect)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless grid rects."
        End Sub
        Public Shared Function buildMap(brickList As List(Of cv.Rect), input As Mat) As Mat
            Dim index = 1
            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            For Each r In brickList
                Dim val = input.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
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
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
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






    Public Class XR_FeatureLess_BrickList : Inherits TaskParent
        Public brickList As New List(Of cv.Rect)
        Dim index As Integer
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless grid rects."
        End Sub
        Public Function buildMap() As Mat
            Dim input = dst1.Clone
            index = 1
            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            For Each r In task.gridRects
                Dim val = input.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(input, mask, r.TopLeft, index, rect, 0, 0, flags)
                    index += 1
                End If
            Next
            Return input
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1.SetTo(0)
            brickList.Clear()
            For Each r In task.gridRects
                If CountNonZero(task.edges.dst2(r)) = 0 Then
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





    Public Class XR_FeatureLess_DepthFull : Inherits TaskParent
        Public brickList As New List(Of cv.Rect)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify featureless gridrects that also have depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            labels(3) = task.edges.labels(2)

            dst1.SetTo(0)
            brickList.Clear()
            For i = 0 To task.gridRects.Count - 1
                Dim r = task.gridRects(i)
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
                If CountNonZero(task.depthmask(r)) = 0 Then Continue For
                dst1(r).SetTo(255)

                brickList.Add(r)
            Next
            Dim countRects = brickList.Count

            Dim index = 1
            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(dst1.Width + 2, dst1.Height + 2), MatType.CV_8U, 0)
            For Each r In brickList
                Dim val = dst1.Get(Of Byte)(r.Y, r.X)
                If val = 255 Then
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                    Dim count = FloodFill(dst1, mask, r.TopLeft, index, rect, 0, 0, flags)
                    index += 1
                End If
            Next

            dst2 = Palettize(dst1, 0)

            labels(2) = CStr(brickList.Count) + " featureless grid regions with " + CStr(countRects) + " input grid rects"
        End Sub
    End Class





    Public Class FeatureLess_OrderKNN : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim knn As New KNN_Minimal
        Dim dimension As Integer = 5
        Public Sub New()
            desc = "Try to keep the featureless region index consistent from frame to frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.fless.dst2
            labels(2) = task.fless.labels(2)

            rcList = task.fless.rcList

            Static queries As New List(Of Single)
            Dim trainInput = New List(Of Single)(queries)
            queries.Clear()
            For Each rc In rcList
                Dim color As cv.Scalar = Mean(task.color(rc.rect), rc.mask)
                queries.Add(color(0))
                queries.Add(color(1))
                queries.Add(color(2))
                queries.Add(rc.pixels)
                queries.Add(rc.depth)
            Next

            If task.firstPass Then
                trainInput = New List(Of Single)(queries)
                task.clickPoint = rcList(1).maxDist
            End If

            knn.queryMat = Mat.FromPixelData(queries.Count \ dimension, dimension, MatType.CV_32F, queries.ToArray)
            knn.trainMat = Mat.FromPixelData(trainInput.Count \ dimension, dimension, MatType.CV_32F, trainInput.ToArray)
            knn.Run(emptyMat)

            For Each rc In rcList
                If rc.ID = 0 Then Continue For
                rc.ID = knn.result(rc.index, 0)
                SetTrueText(CStr(rc.ID), rc.maxDist)
            Next
        End Sub
    End Class




    Public Class FeatureLess_Ordered : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim tracked As New List(Of rcData)
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Keep the rc.ID assigned the first time each FeatureLess_Basics cell appears."
        End Sub
        Private Function indexUsed(index As Integer) As Boolean
            For Each rcPrev In tracked
                If rcPrev.index = index Then Return True
            Next
            Return False
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim claimed As New List(Of Integer)
            rcList.Clear()
            rcList.Add(task.fless.rcList(0))
            For Each rc In task.fless.rcList
                If rc.ID = 0 Then Continue For

                Dim matched As rcData = Nothing
                For Each rcPrev In tracked
                    If claimed.Contains(rcPrev.index) Then Continue For

                    If rc.rect.IntersectsWith(rcPrev.rect) Then
                        matched = rcPrev
                        Exit For
                    End If
                Next

                If matched IsNot Nothing Then
                    rc.ID = matched.index
                    rc.age = matched.age + 1
                    tracked(tracked.IndexOf(matched)) = rc
                Else
                    Dim nextFree As Integer = 1
                    While indexUsed(nextFree)
                        nextFree += 1
                    End While
                    rc.ID = nextFree
                    tracked.Add(rc)
                End If
                claimed.Add(rc.ID)
                rcList.Add(rc)
            Next

            dst1.SetTo(0)
            For Each rc In rcList
                If rc.ID = 0 Then Continue For
                dst1(rc.rect).SetTo(rc.ID, rc.mask)
                SetTrueText(CStr(rc.ID), rc.maxDist)
            Next
            dst2 = Palettize(dst1, 0)

            labels(2) = CStr(rcList.Count - 1) + " featureless cells with stable indices"
        End Sub
    End Class




    Public Class XR_FeatureLess_Delaunay : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim delaunay As New Delaunay_Basics
        Public Sub New()
            desc = "Create a map for the featureless cells with Delaunay triangulation"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rcList = task.fless.rcList
            dst2 = task.fless.dst2
            labels(2) = task.fless.labels(2)

            If task.heartBeatLT Or task.optionsChanged Then
                delaunay.ptList.Clear()
                For Each rc In rcList
                    delaunay.ptList.Add(rc.maxDist)
                Next

                delaunay.Run(emptyMat)
                dst1 = delaunay.dst1.Clone
                labels(3) = delaunay.labels(2)

                For Each rc In rcList
                    If rc.ID = 0 Then Continue For
                    dst1(rc.rect).SetTo(rc.ID, rc.mask)
                Next
            End If

            dst3 = Palettize(dst1, 0)

            For Each rc In rcList
                If rc.ID = 0 Then Continue For
                DrawContours(dst3(rc.rect), {rc.contour}, 0, task.highlight, task.lineWidth, task.lineType)
                Circle(dst3, rc.maxDist, task.DotSize, task.highlight, -1)
            Next
        End Sub
    End Class



    Public Class XR_FeatureLess_TrackerOriginal : Inherits TaskParent
        Dim fLessD As New XR_FeatureLess_Delaunay
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Build a Delaunay map on heartbeatLT then use it to keep the index consistent."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLessD.Run(src)

            dst3 = fLessD.dst3
            labels(2) = fLessD.labels(2)
            dst1 = fLessD.dst1.Clone

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData())
            For Each rc In fLessD.rcList
                If rc.ID = 0 Then Continue For
                Dim nextID = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rcMap(rc.rect).SetTo(nextID, rc.mask)
                rcList.Add(rc)
            Next

            dst2 = Palettize(rcMap, 0)
        End Sub
    End Class




    Public Class FeatureLess_Matching : Inherits TaskParent
        Dim match As New Match_Basics
        Dim rect As New cv.Rect(0, 0, dst2.Width, dst2.Height)
        Public originalCenter As cv.Point = Nothing
        Public newCenter As cv.Point = Nothing
        Dim rcMatch As rcData
        Dim snapshot As cv.Mat = Nothing
        Public M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
        Public verticalizeAngle As Double
        Public Sub New()
            verticalizeAngle = task.verticalizeAngle
            task.fOptions.MatchCorrSlider.Value = 85
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            labels(1) = "Path of the rectangle center - match.newCenter."
            desc = "Match a featureless cell to the previous image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.fless.rcList.Count = 0 Then Exit Sub

            rcMatch = Nothing
            For Each rc In task.fless.rcList
                If rc.ID = 0 Then Continue For
                If rc.rect.IntersectsWith(rect) Then
                    rcMatch = rc
                    Exit For
                End If
            Next
            If rcMatch Is Nothing Then rcMatch = task.fless.rcList(1)

            If heartbeatLTX() Or rcMatch.motionPixels / rcMatch.pixels >= 50 Or
                match.correlation * 100 < task.fOptions.MatchCorrSlider.Value Then
                dst1.SetTo(0)
                dst3.SetTo(0)
                rect = rcMatch.rect
                Dim w = task.gridWH
                rect = ValidateRect(New cv.Rect(rect.X - w, rect.Y - w,
                                                rect.Width + 2 * w, rect.Height + 2 * w))
                If rect.Width > dst2.Width \ 2 Then rect.Width \= 2
                If rect.Height > dst2.Height \ 2 Then rect.Height \= 2
                originalCenter = New cv.Point(rect.X + rect.Width \ 2, rect.Y + rect.Height \ 2)
                match.template = task.gray(rect).Clone
                dst3(rect) = task.gray(rect)
                Rectangle(dst3, rect, white, task.lineWidth)
                snapshot = task.grayOriginal.Clone
                verticalizeAngle = task.verticalizeAngle
            End If

            match.Run(task.gray)
            labels(2) = "Template is shown below after shift.  Correlation: " +
                        match.correlation.ToString("F3")

            newCenter = match.newCenter
            If standaloneTest() Then
                Circle(dst1, newCenter, task.DotSize, white, -1)
                Line(dst1, originalCenter, newCenter, grayColor, task.lineWidth, task.lineType)
            End If

            Dim shiftXY = New cv.Point2f()
            M = task.steadyCam.M
            M.Set(Of Double)(0, 2, shiftXY.X)
            M.Set(Of Double)(1, 2, shiftXY.Y)

            WarpAffine(snapshot, dst2, M, snapshot.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
        End Sub
    End Class





    Public Class FeatureLess_Gravity : Inherits TaskParent
        Dim match As New FeatureLess_Matching
        Public Sub New()
            desc = "Run FeatureLess_Matching to get shift and use gravity to rotate the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            match.Run(task.gray)
            dst2 = match.dst2
            labels(2) = match.labels(2)

            Dim rotateAngle As Double = task.verticalizeAngle - match.verticalizeAngle
            Static rotateCenter As New Point2f(dst2.Width / 2, dst2.Height / 2)

            Dim M = GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
            WarpAffine(dst2, dst3, M, dst2.Size(), InterpolationFlags.Nearest)
            labels(3) = "Image at left rotated by " + rotateAngle.ToString("F2") +
                        " degrees to verticalize the image."
        End Sub
    End Class





    Public Class FeatureLess_DisectionOriginal : Inherits TaskParent
        ' Each cell adds the horizontal line depths, then the vertical line depths.
        Public depthList As New List(Of List(Of Single))
        Public Sub New()
            desc = "Cursor.ai: Disect featureless cells with a horizontal and vertical line through maxDist."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.fless.dst2.Clone
            labels(2) = task.fless.labels(2)

            depthList.Clear()
            For Each rc In task.fless.rcList
                If rc.ID = 0 Then Continue For

                Dim lineMask As New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
                Dim pt = New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
                Line(lineMask, New cv.Point(0, pt.Y), New cv.Point(lineMask.Width - 1, pt.Y), 255, 1)
                Line(lineMask, New cv.Point(pt.X, 0), New cv.Point(pt.X, lineMask.Height - 1), 255, 1)
                BitwiseAnd(lineMask, rc.mask, lineMask)

                Dim hDepth As New List(Of Single)
                For x = 0 To lineMask.Width - 1
                    If lineMask.Get(Of Byte)(pt.Y, x) = 0 Then Continue For
                    hDepth.Add(task.pcSplit(2).Get(Of Single)(rc.maxDist.Y, rc.rect.X + x))
                Next

                Dim vDepth As New List(Of Single)
                For y = 0 To lineMask.Height - 1
                    If lineMask.Get(Of Byte)(y, pt.X) = 0 Then Continue For
                    vDepth.Add(task.pcSplit(2).Get(Of Single)(rc.rect.Y + y, rc.maxDist.X))
                Next
                depthList.Add(hDepth)
                depthList.Add(vDepth)

                Dim drawMask As New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
                Line(drawMask, New cv.Point(0, pt.Y), New cv.Point(drawMask.Width - 1, pt.Y), 255, task.lineWidth)
                Line(drawMask, New cv.Point(pt.X, 0), New cv.Point(pt.X, drawMask.Height - 1), 255, task.lineWidth)
                BitwiseAnd(drawMask, rc.mask, drawMask)
                dst2(rc.rect).SetTo(task.highlight, drawMask)
            Next
        End Sub
    End Class




    Public Class FeatureLess_Disection : Inherits TaskParent
        ' Each cell adds the horizontal line depths, then the vertical line depths.
        Public depthList As New List(Of List(Of Single))
        Public ptList As New List(Of List(Of cv.Point))
        Public indexList As New List(Of Integer)
        Public Sub New()
            desc = "Cursor.ai: Disect featureless cells with a horizontal and vertical line through maxDist."
        End Sub
        Private Shared Sub depthAlongLine(rc As rcData, p1 As cv.Point, p2 As cv.Point,
                                   depths As List(Of Single), pts As List(Of cv.Point))
            Dim iter As New LineIterator(task.pcSplit(2), p1, p2, PixelConnectivity.Connectivity8, False)
            For Each pt In iter
                Dim depth = pt.GetValue(Of Single)()
                If depth <= 0 OrElse Single.IsFinite(depth) = False Then Continue For
                Dim x = pt.Pos.X - rc.rect.X
                Dim y = pt.Pos.Y - rc.rect.Y
                If x < 0 Or y < 0 Or x >= rc.mask.Width Or y >= rc.mask.Height Then Continue For
                If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                depths.Add(depth)
                pts.Add(pt.Pos)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.fless.dst2.Clone
            labels(2) = task.fless.labels(2)

            depthList.Clear()
            ptList.Clear()
            indexList.Clear()
            For Each rc In task.fless.rcList
                If rc.ID = 0 Or rc.mask.Width < 2 Or rc.mask.Height < 2 Then Continue For

                Dim pt = New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
                pt.X = Math.Max(0, Math.Min(rc.mask.Width - 1, pt.X))
                pt.Y = Math.Max(0, Math.Min(rc.mask.Height - 1, pt.Y))
                Dim cx = Math.Max(0, Math.Min(dst2.Width - 1, rc.rect.X + pt.X))
                Dim cy = Math.Max(0, Math.Min(dst2.Height - 1, rc.rect.Y + pt.Y))

                Dim h1 = New cv.Point(Math.Max(0, rc.rect.X), cy)
                Dim h2 = New cv.Point(Math.Min(dst2.Width - 1, rc.rect.X + rc.rect.Width - 1), cy)
                Dim v1 = New cv.Point(cx, Math.Max(0, rc.rect.Y))
                Dim v2 = New cv.Point(cx, Math.Min(dst2.Height - 1, rc.rect.Y + rc.rect.Height - 1))

                Dim hDepth As New List(Of Single), hPts As New List(Of cv.Point)
                Dim vDepth As New List(Of Single), vPts As New List(Of cv.Point)
                depthAlongLine(rc, h1, h2, hDepth, hPts)
                depthAlongLine(rc, v1, v2, vDepth, vPts)
                depthList.Add(hDepth)
                depthList.Add(vDepth)
                ptList.Add(hPts)
                ptList.Add(vPts)
                indexList.Add(rc.ID)

                Dim drawMask As New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
                Line(drawMask, New cv.Point(0, pt.Y), New cv.Point(drawMask.Width - 1, pt.Y), 255, task.lineWidth)
                Line(drawMask, New cv.Point(pt.X, 0), New cv.Point(pt.X, drawMask.Height - 1), 255, task.lineWidth)
                BitwiseAnd(drawMask, rc.mask, drawMask)
                dst2(rc.rect).SetTo(task.highlight, drawMask)
            Next
        End Sub
        Private Shared Function signedTerm(value As Double) As String
            If value < 0 Then Return " - " + Math.Abs(value).ToString(fmt3) + "t"
            Return " + " + value.ToString(fmt3) + "t"
        End Function
        Private Shared Function axisEquation(name As String, origin As Double, direction As Double) As String
            If Math.Abs(direction) < 0.0001 Then Return name + " = " + origin.ToString(fmt3)
            If origin < 0 Then
                Return "(" + name + " + " + Math.Abs(origin).ToString(fmt3) + ") / " + direction.ToString(fmt3)
            End If
            Return "(" + name + " - " + origin.ToString(fmt3) + ") / " + direction.ToString(fmt3)
        End Function
        Public Shared Function LineEquation(depths As List(Of Single), pts As List(Of cv.Point),
                                            kind As String, cellIndex As Integer) As String
            If depths Is Nothing OrElse pts Is Nothing OrElse Math.Min(depths.Count, pts.Count) < 2 Then
                Return "Cell " + CStr(cellIndex) + " does not have enough " + kind + " depth points to fit a 3D line."
            End If

            Dim cloud As New List(Of Point3f)
            For i = 0 To Math.Min(depths.Count, pts.Count) - 1
                cloud.Add(Cloud_Basics.worldCoordinates(pts(i), depths(i)))
            Next
            Dim line = FitLine(cloud, DistanceTypes.L2, 0, 0.01, 0.01)

            Dim strOut = "Cell " + CStr(cellIndex) + " " + kind + " 3D line" + vbCrLf
            strOut += CStr(cloud.Count) + " points, meters" + vbCrLf + vbCrLf
            strOut += "x = " + line.X1.ToString(fmt3) + signedTerm(line.Vx) + vbCrLf
            strOut += "y = " + line.Y1.ToString(fmt3) + signedTerm(line.Vy) + vbCrLf
            strOut += "z = " + line.Z1.ToString(fmt3) + signedTerm(line.Vz) + vbCrLf + vbCrLf
            strOut += axisEquation("x", line.X1, line.Vx) + " =" + vbCrLf
            strOut += axisEquation("y", line.Y1, line.Vy) + " =" + vbCrLf
            strOut += axisEquation("z", line.Z1, line.Vz)
            Return strOut
        End Function
        Public Shared Sub ShowClickedLine(alg As TaskParent, disect As FeatureLess_Disection, vertical As Boolean)
            alg.dst2 = disect.dst2.Clone
            alg.labels(2) = disect.labels(2)
            alg.dst3.SetTo(0)

            Static selected As Integer = -1
            If selected < 0 Or task.firstPass Or (task.mouseClickFlag And task.mousePicTag = 2) Then
                Dim clickPt = task.clickPoint
                If clickPt.X >= 0 And clickPt.Y >= 0 And clickPt.X < task.fless.dst1.Width And
                   clickPt.Y < task.fless.dst1.Height Then
                    selected = task.fless.dst1.Get(Of Byte)(clickPt.Y, clickPt.X)
                End If
            End If

            Dim kind = If(vertical, "vertical", "horizontal")
            If selected <= 0 Then
                alg.labels(3) = "Click a featureless cell in dst2."
                alg.SetTrueText(alg.labels(3), 3)
                Exit Sub
            End If

            Dim slot = disect.indexList.IndexOf(selected)
            If slot < 0 Then
                alg.labels(3) = "Cell " + CStr(selected) + " has no " + kind + " line."
                alg.SetTrueText(alg.labels(3), 3)
                Exit Sub
            End If

            Dim listIndex = slot * 2 + If(vertical, 1, 0)
            Dim depths = disect.depthList(listIndex)
            Dim pts = disect.ptList(listIndex)

            For Each rc In task.fless.rcList
                If rc.ID <> selected Then Continue For
                Rectangle(alg.dst2, rc.rect, white, task.lineWidth)
                If pts.Count >= 2 Then
                    Line(alg.dst2, pts(0), pts(pts.Count - 1), white, task.lineWidth + 1, task.lineType)
                End If
                Circle(alg.dst2, rc.maxDist, task.DotSize, white, -1, task.lineType)
                Exit For
            Next

            alg.labels(3) = "3D " + kind + " line for cell " + CStr(selected) + "."
            alg.SetTrueText(LineEquation(depths, pts, kind, selected), 3)
        End Sub
    End Class




    Public Class FeatureLess_DisectHLine : Inherits TaskParent
        Dim disect As New FeatureLess_Disection
        Public Sub New()
            desc = "Cursor.ai: Fit a 3D line to the horizontal depth samples of the clicked featureless cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            disect.Run(src)
            FeatureLess_Disection.ShowClickedLine(Me, disect, False)
        End Sub
    End Class




    Public Class FeatureLess_DisectVLine : Inherits TaskParent
        Dim disect As New FeatureLess_Disection
        Public Sub New()
            desc = "Cursor.ai: Fit a 3D line to the vertical depth samples of the clicked featureless cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            disect.Run(src)
            FeatureLess_Disection.ShowClickedLine(Me, disect, True)
        End Sub
    End Class




    Public Class FeatureLess_DisectionPlane : Inherits TaskParent
        Public disect As New FeatureLess_Disection
        ' planeList(i) matches disect.indexList(i): ax + by + cz = d
        Public planeList As New List(Of Vec4f)
        ' Parallel to planeList. Held entries keep the last equation and depth while the cell has no motion.
        Public anchorDepth As New List(Of Single)
        Public heldStill As New List(Of Boolean)
        Private Class stillPlane
            Public plane As Vec4f
            Public depth As Single
            Public maxDist As cv.Point
        End Class
        Private stillList As New List(Of stillPlane)
        Public Sub New()
            desc = "Cursor.ai: Build a plane equation from a cell's horizontal/vertical lines."
        End Sub
        Private Shared Function worldCloud(depths As List(Of Single), pts As List(Of cv.Point)) As List(Of Point3f)
            Dim cloud As New List(Of Point3f)
            If depths Is Nothing OrElse pts Is Nothing Then Return cloud
            For i = 0 To Math.Min(depths.Count, pts.Count) - 1
                If depths(i) <= 0 OrElse Single.IsFinite(depths(i)) = False Then Continue For
                cloud.Add(Cloud_Basics.worldCoordinates(pts(i), depths(i)))
            Next
            Return cloud
        End Function
        Private Shared Function crossProduct(v1 As Point3f, v2 As Point3f) As Point3f
            Return New Point3f(v1.Y * v2.Z - v1.Z * v2.Y,
                               v1.Z * v2.X - v1.X * v2.Z,
                               v1.X * v2.Y - v1.Y * v2.X)
        End Function
        Private Shared Function formatPlane(a As Double, b As Double, c As Double, d As Double) As String
            Dim s1 = If(b < 0, " - ", " + ")
            Dim s2 = If(c < 0, " - ", " + ")
            Return a.ToString(fmt3) + "x" + s1 + Math.Abs(b).ToString(fmt3) + "y" + s2 +
                   Math.Abs(c).ToString(fmt3) + "z = " + d.ToString(fmt3)
        End Function
        Private Shared Function anchorPoint(rc As rcData, hCloud As List(Of Point3f), vCloud As List(Of Point3f)) As Point3f
            If rc IsNot Nothing AndAlso rc.maxDist.X >= 0 AndAlso rc.maxDist.Y >= 0 AndAlso
               rc.maxDist.X < task.pcSplit(2).Cols AndAlso rc.maxDist.Y < task.pcSplit(2).Rows Then
                Dim depth = task.pcSplit(2).Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                If depth > 0 AndAlso Single.IsFinite(depth) Then Return Cloud_Basics.worldCoordinates(rc.maxDist, depth)
            End If

            Dim count As Integer
            Dim x As Double, y As Double, z As Double
            For Each cloud In {hCloud, vCloud}
                If cloud Is Nothing Then Continue For
                For Each p In cloud
                    If p.Z <= 0 OrElse Single.IsFinite(p.Z) = False Then Continue For
                    x += p.X
                    y += p.Y
                    z += p.Z
                    count += 1
                Next
            Next
            If count > 0 Then Return New Point3f(CSng(x / count), CSng(y / count), CSng(z / count))
            If rc IsNot Nothing AndAlso rc.depth > 0 AndAlso Single.IsFinite(rc.depth) Then
                Return Cloud_Basics.worldCoordinates(rc.maxDist, rc.depth)
            End If
            Return New Point3f
        End Function
        Private Shared Function flatPlane(cellIndex As Integer, anchor As Point3f, reason As String) As (plane As Vec4f, depth As Single, text As String)
            Dim plane = New Vec4f(0, 0, 1, anchor.Z)
            Dim text = "Cell " + CStr(cellIndex) + " plane" + vbCrLf
            text += reason + vbCrLf + vbCrLf
            text += formatPlane(plane(0), plane(1), plane(2), plane(3)) + vbCrLf + vbCrLf
            text += "normal = (0, 0, 1)"
            Return (plane, anchor.Z, text)
        End Function
        Private Shared Function heldText(cellIndex As Integer, plane As Vec4f, depth As Single) As String
            Dim text = "Cell " + CStr(cellIndex) + " plane held (no motion)" + vbCrLf
            text += "depth = " + depth.ToString(fmt3) + vbCrLf + vbCrLf
            text += formatPlane(plane(0), plane(1), plane(2), plane(3))
            Return text
        End Function
        Private Function findStill(rc As rcData, used As Boolean()) As Integer
            If rc Is Nothing OrElse rc.mask Is Nothing OrElse used Is Nothing Then Return -1
            Dim best = -1
            Dim bestDist = Integer.MaxValue
            For i = 0 To stillList.Count - 1
                If used(i) Then Continue For
                Dim x = stillList(i).maxDist.X - rc.rect.X
                Dim y = stillList(i).maxDist.Y - rc.rect.Y
                If x < 0 OrElse y < 0 OrElse x >= rc.mask.Width OrElse y >= rc.mask.Height Then Continue For
                If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                Dim dx = stillList(i).maxDist.X - rc.maxDist.X
                Dim dy = stillList(i).maxDist.Y - rc.maxDist.Y
                Dim dist = dx * dx + dy * dy
                If dist < bestDist Then
                    bestDist = dist
                    best = i
                End If
            Next
            Return best
        End Function
        Private Shared Function buildPlane(cellIndex As Integer, rc As rcData, hDepths As List(Of Single), hPts As List(Of cv.Point),
                                    vDepths As List(Of Single), vPts As List(Of cv.Point)) As (plane As Vec4f, depth As Single, text As String)
            Dim hCloud = worldCloud(hDepths, hPts)
            Dim vCloud = worldCloud(vDepths, vPts)
            Dim anchor = anchorPoint(rc, hCloud, vCloud)
            If anchor.Z <= 0 Then
                Return (New Vec4f, 0, "Cell " + CStr(cellIndex) + " has no depth.")
            End If
            If hCloud.Count < 2 Or vCloud.Count < 2 Then
                Return flatPlane(cellIndex, anchor, "Constant depth: one dissecting line has too few depth samples.")
            End If

            Dim hLine = FitLine(hCloud, DistanceTypes.L2, 0, 0.01, 0.01)
            Dim vLine = FitLine(vCloud, DistanceTypes.L2, 0, 0.01, 0.01)
            Dim normal = crossProduct(New Point3f(hLine.Vx, hLine.Vy, hLine.Vz),
                                      New Point3f(vLine.Vx, vLine.Vy, vLine.Vz))
            Dim magnitude = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z)
            If magnitude < 0.001 OrElse Double.IsFinite(magnitude) = False Then
                Return flatPlane(cellIndex, anchor, "Constant depth: the dissecting lines are parallel.")
            End If

            normal = New Point3f(normal.X / magnitude, normal.Y / magnitude, normal.Z / magnitude)
            If Single.IsFinite(normal.X) = False OrElse Single.IsFinite(normal.Y) = False OrElse
               Single.IsFinite(normal.Z) = False Then
                Return flatPlane(cellIndex, anchor, "Constant depth: the plane fit was not finite.")
            End If
            If normal.Z < 0 Then normal = New Point3f(-normal.X, -normal.Y, -normal.Z)

            Dim d = normal.X * anchor.X + normal.Y * anchor.Y + normal.Z * anchor.Z
            Dim plane = New Vec4f(normal.X, normal.Y, normal.Z, d)
            Dim text = "Cell " + CStr(cellIndex) + " plane" + vbCrLf
            text += CStr(hCloud.Count) + " horizontal points, " + CStr(vCloud.Count) + " vertical points, meters" + vbCrLf + vbCrLf
            text += formatPlane(plane(0), plane(1), plane(2), plane(3)) + vbCrLf + vbCrLf
            text += "normal = (" + normal.X.ToString(fmt3) + ", " + normal.Y.ToString(fmt3) + ", " + normal.Z.ToString(fmt3) + ")"
            Return (plane, anchor.Z, text)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            disect.Run(src)
            dst2 = disect.dst2.Clone
            labels(2) = disect.labels(2)
            If task.firstPass Or task.optionsChanged Then stillList.Clear()
            planeList.Clear()
            anchorDepth.Clear()
            heldStill.Clear()

            Dim nextStill As New List(Of stillPlane)
            Dim used As Boolean() = Nothing
            If stillList.Count > 0 Then used = New Boolean(stillList.Count - 1) {}

            Static selected As Integer = -1
            If selected < 0 Or task.firstPass Or (task.mouseClickFlag And task.mousePicTag = 2) Then
                Dim clickPt = task.clickPoint
                If clickPt.X >= 0 And clickPt.Y >= 0 And clickPt.X < task.fless.dst1.Width And
                   clickPt.Y < task.fless.dst1.Height Then
                    selected = task.fless.dst1.Get(Of Byte)(clickPt.Y, clickPt.X)
                End If
            End If

            Dim selectedText = ""
            For i = 0 To disect.indexList.Count - 1
                Dim cell = disect.indexList(i)
                Dim rc As rcData = Nothing
                For Each candidate In task.fless.rcList
                    If candidate.index = cell Then
                        rc = candidate
                        Exit For
                    End If
                Next

                Dim resultPlane As Vec4f
                Dim resultDepth As Single
                Dim resultText As String = ""
                Dim hold = False
                If rc IsNot Nothing AndAlso rc.motionPixels = 0 Then
                    Dim heldSlot = findStill(rc, used)
                    If heldSlot >= 0 AndAlso stillList(heldSlot).depth > 0 Then
                        used(heldSlot) = True
                        hold = True
                        resultPlane = stillList(heldSlot).plane
                        resultDepth = stillList(heldSlot).depth
                        resultText = heldText(cell, resultPlane, resultDepth)
                    End If
                End If
                If hold = False Then
                    Dim built = buildPlane(cell, rc, disect.depthList(i * 2), disect.ptList(i * 2),
                                            disect.depthList(i * 2 + 1), disect.ptList(i * 2 + 1))
                    resultPlane = built.plane
                    resultDepth = built.depth
                    resultText = built.text
                End If

                planeList.Add(resultPlane)
                anchorDepth.Add(resultDepth)
                heldStill.Add(hold)
                If rc IsNot Nothing AndAlso resultDepth > 0 Then
                    Dim entry As New stillPlane With {.plane = resultPlane, .depth = resultDepth,
                                                      .maxDist = rc.maxDist}
                    nextStill.Add(entry)
                End If
                If cell <> selected Then Continue For

                selectedText = resultText
                If rc Is Nothing Then Continue For
                Rectangle(dst2, rc.rect, white, task.lineWidth)
                Dim hPts = disect.ptList(i * 2)
                Dim vPts = disect.ptList(i * 2 + 1)
                If hPts.Count >= 2 Then Line(dst2, hPts(0), hPts(hPts.Count - 1), white, task.lineWidth + 1, task.lineType)
                If vPts.Count >= 2 Then Line(dst2, vPts(0), vPts(vPts.Count - 1), white, task.lineWidth + 1, task.lineType)
                Circle(dst2, rc.maxDist, task.DotSize, white, -1, task.lineType)
            Next

            If selected <= 0 Then
                labels(3) = "Click a featureless cell in dst2."
                SetTrueText(labels(3), 3)
            ElseIf selectedText = "" Then
                labels(3) = "Cell " + CStr(selected) + " has no dissecting lines."
                SetTrueText(labels(3), 3)
            Else
                labels(3) = "Plane equation for cell " + CStr(selected) + "."
                SetTrueText(selectedText, 3)
            End If
            stillList = nextStill
        End Sub
    End Class





    Public Class FeatureLess_SteadyCam : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public idMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Track the FeatureLess cells points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            idMap = task.delaunayFLess.idMap
            dst0.SetTo(0)
            dst3.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            For Each rc In task.fless.rcList
                Dim ptx = WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.M)
                ptx = validatePoint(ptx)
                Line(dst3, rc.maxDist, ptx, white, task.lineWidth)
                rc.ID = idMap.Get(Of Byte)(ptx.Y, ptx.X)
                rcList.Add(rc)
                dst0(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = Palettize(dst0, 0)

            WarpAffine(task.delaunayFLess.dst2, dst3, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            labels(2) = CStr(task.redColor.rcList.Count) + " were mapped to " + CStr(task.delaunayFLess.ptList.Count) + " cells"
            labels(3) = CStr(task.redColor.rcList.Count) + " cells were found"
        End Sub
    End Class




    Public Class FeatureLess_ReMap : Inherits TaskParent
        Public Sub New()
            desc = "Update the featureless idMap using SteadyCam M."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            WarpAffine(task.fless.dst2, dst2, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, black)
            dst3 = task.steadyCam.dst2
        End Sub
    End Class





    Public Class FeatureLess_Tracker : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            ' Hungarian Assignment algorithm - visual solution.
            desc = "Use the maxDist point with task.SteadyCam.M to track the FeatureLess_Basics cells." +
                   "  No cost function needed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = task.steadyCam.dst2
            If task.fless.rcList.Count = 0 Then Exit Sub

            If task.heartBeatLT Or task.delaunayFLess.snapshotList.Count < 2 Then dst3 = task.fless.dst3

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData())
            Dim usedList As New List(Of Integer)
            For Each rcPrev In task.delaunayFLess.snapshotList
                If rcPrev.index = 0 Then Continue For
                Dim pt = WarpAffine_Basics.WarpPoint(rcPrev.maxDist, task.steadyCam.inverseM)
                For Each rc In task.fless.rcList
                    If rc.ID = 0 Then Continue For
                    If rc.rect.Contains(pt) Then
                        If usedList.Contains(rc.ID) = False Then
                            rc.ID = rcPrev.index
                            rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                            rcList.Add(rc)
                            usedList.Add(rc.ID)
                            Exit For
                        End If
                    End If
                Next
            Next

            Dim unusedCount = task.fless.rcList.Count - usedList.Count
            Dim nextSlot As Integer = task.fless.rcList.Count
            For Each rc In task.fless.rcList
                If rc.ID = 0 Then Continue For
                If usedList.Contains(rc.ID) = False Then
                    rc.ID = nextSlot
                    nextSlot += 1
                    rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                    rcList.Add(rc)
                End If
            Next

            If standaloneTest() Then
                dst2 = Palettize(rcMap, 0)

                For Each rc In task.delaunayFLess.snapshotList
                    If rc.index = 0 Then Continue For
                    If rc.ID <> 0 Then
                        Dim pt = WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.inverseM)
                        Circle(dst2, pt, task.DotSize + 2, black, -1)
                        Circle(dst2, pt, task.DotSize, task.highlight, -1)
                    End If
                Next
            End If

            strOut = "In dst2 at left: " + vbCrLf + vbCrLf
            strOut += "- A circle with no cell shows a cell that is no longer present." + vbCrLf
            strOut += "- A cell with no circle is a new cell." + vbCrLf
            strOut += "- All circles represent the new location of a cell found at the time of the heartbeat snapshot." + vbCrLf
            strOut += "- Multiple circles in a cell represent cells that have expanded and covered other snapshot cells." + vbCrLf
            SetTrueText(strOut, 3)

            labels(2) = CStr(rcList.Count - 1) + " cells were tracked and " + CStr(unusedCount) + " could not be matched."
            labels(3) = CStr(task.delaunayFLess.snapshotList.Count) + " cells found in the heartbeat snapshot that is used to identify and track cells."
        End Sub
    End Class
End Namespace
