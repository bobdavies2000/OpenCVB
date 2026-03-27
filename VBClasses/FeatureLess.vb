Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureLess_Basics : Inherits TaskParent
        Dim corr As New Correlation_Basics
        Public rectList As New List(Of cv.Rect)
        Public Sub New()
            desc = "Measure the correlation of all grid squares except where there is motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            corr.Run(src)
            dst2 = corr.dst2
            dst3 = corr.dst3
            rectList = New List(Of cv.Rect)(corr.rectList)

            labels(3) = corr.labels(3)
        End Sub
    End Class




    Public Class FeatureLess_Threshold : Inherits TaskParent
        Public rectList As New List(Of cv.Rect)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            rectList.Clear()
            dst2 = src
            ' Currently task.fLessThreshold is just a fixed number.  It works but must be tested further.
            For Each r In task.gridRects
                Dim mm = GetMinMax(src(r))
                If mm.range < task.fLessThreshold Then
                    rectList.Add(r)
                    dst2.Rectangle(r, 255, task.lineWidth)
                End If
            Next

            labels(2) = CStr(rectList.Count) + " grid squares were found to be featureless (gridRects(i).mm.range < " +
                        CStr(task.fLessThreshold) + ")"
        End Sub
    End Class





    Public Class NR_FeatureLess_Basics : Inherits TaskParent
        Public rectList As New List(Of cv.Rect)
        Dim smallGrid As New Grid_SquaresOnly
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' couldn't put this in the constructor because motion is a task algorithm.
            If task.firstPass Then smallGrid.Run(src) ' create the grid squares for the small resolution.
            Dim input As cv.Mat
            If src.Channels <> 1 Then input = task.gray Else input = src

            ' why do this resize?  Because the flessThreshold works at smallRes but not larger resolutions.
            If task.workRes.Height > 270 Then input = input.Resize(task.smallRes, 0, 0, cv.InterpolationFlags.Nearest)

            Dim mask As New cv.Mat(input.Size, cv.MatType.CV_8U, 0)
            For Each r In smallGrid.gridRects
                Dim mm = GetMinMax(input(r))
                If mm.range < task.fLessThreshold Then mask(r).SetTo(255)
            Next

            dst2 = mask.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)

            rectList.Clear()
            For Each r In task.gridRects
                If dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X) Then rectList.Add(r)
            Next

            If standaloneTest() Then
                dst3 = task.gray.Clone
                For Each r In rectList
                    dst3.Rectangle(r, white, task.lineWidth)
                Next

                Dim index = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
                Dim mm = GetMinMax(task.gray(task.gridRects(index)))
                SetTrueText("Click on any grid rect to see its grayscale range." + vbCrLf +
                            "Min gray = " + Format(mm.minVal, fmt0) + vbCrLf +
                            "Max Gray = " + Format(mm.maxVal, fmt0) + vbCrLf +
                            "Range = " + Format(mm.range, fmt0) + vbCrLf + vbCrLf, 1)
            End If

            labels(3) = CStr(rectList.Count) + " grid squares were found to be featureless (range < " +
                        CStr(task.fLessThreshold) + ")"

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
        Dim edgeline As New EdgeLine_Basics_TA
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Use Contour_Basics to get the contour data for the top contours by size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels = 1 Then edgeline.Run(src) Else edgeline.Run(task.grayStable)
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
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            labels(3) = "The brighter the grid square, the more recent appearance."
            desc = "Accumulate the edges over a span of X images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

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








    Public Class NR_FeatureLess_LeftRight : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
            desc = "Find the featureless regions of the left and right images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.toggleOn Then
                dst2 = task.leftView
                dst3 = task.rightView
            Else
                fLess.Run(task.leftView)
                dst2 = fLess.dst2.Clone

                fLess.Run(task.rightView)
                dst3 = fLess.dst2.Clone
            End If
        End Sub
    End Class






    Public Class FeatureLess_Not : Inherits TaskParent
        Dim feat As New Feature_General
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            desc = "Use the FeatureLess mask to reduce the input to feature searches."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            dst2.SetTo(0)
            src.CopyTo(dst2, Not fless.dst2)

            feat.Run(dst2)
            feat.dst2.CopyTo(dst3)
            Dim count = task.gridRects.Count - fLess.rectList.Count
            labels(2) = "Current frame: " + CStr(count) + " grid squares had features"
        End Sub
    End Class





    Public Class FeatureLess_Cells : Inherits TaskParent
        Dim saveColorMap As cv.Mat
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            saveColorMap = task.colorMap.Clone
            labels(3) = "Region Colors are ordered by size."
            desc = "Group the featureless grid squares"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            dst2 = fLess.dst2.Clone
            labels(2) = fLess.labels(2)

            Dim index = 1
            Dim rect As cv.Rect
            Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4
            Dim minSize = task.brickEdgeLen * task.brickEdgeLen
            Dim countList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each r In fLess.rectList
                Dim val = dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
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





    Public Class FeatureLess_RedColor : Inherits TaskParent
        Dim redC As New RedColor_Basics
        Dim fLess As New FeatureLess_Threshold
        Public Sub New()
            task.gOptions.GridSlider.Value = 4
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Use the featureLess_Basics output as input to RedColor_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            dst2 = fLess.dst2.Clone
            dst1 = dst2.InRange(0, 0)
            labels(2) = fLess.labels(3)

            redC.Run(dst2)
            dst3 = redC.dst2
            dst3.SetTo(0, dst1)
            labels(3) = redC.labels(2)

            strOut = RedUtil_Basics.selectCell(redC.rcMap, redC.rcList)
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class FeatureLess_Stabilized : Inherits TaskParent
        Dim fLess As New FeatureLess_Threshold
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
                Dim val = diff.dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
                If val > 0 Then
                    Dim gridIndex = task.gridMap.Get(Of Integer)(r.TopLeft.Y, r.TopLeft.X)
                    If task.motion.motionMask.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X) = 0 Then
                        dst2(task.gridRects(gridIndex)).SetTo(0)
                    End If
                End If
            Next

            fLessLast = dst2.Clone
        End Sub
    End Class
End Namespace