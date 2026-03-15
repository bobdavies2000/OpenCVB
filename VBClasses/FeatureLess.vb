Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureLess_Basics : Inherits TaskParent
        Public rectList As New List(Of cv.Rect)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            dst3 = src
            dst2.SetTo(0)
            Dim count As Integer
            Dim rangeThreshold As Integer = 30
            rectList.Clear()
            For Each gSq In task.gSquares
                Dim mm = GetMinMax(src(gSq))
                If mm.range < rangeThreshold Then
                    dst2(gSq).SetTo(255)
                    count += 1
                    dst3.Rectangle(gSq, white, task.lineWidth)
                    rectList.Add(gSq)
                End If
            Next
            labels(3) = CStr(count) + " grid squares were found to be featureless (range < " + CStr(rangeThreshold) + ")"
        End Sub
    End Class




    Public Class FeatureLess_Correlations : Inherits TaskParent
        Dim corr As New Correlation_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Identify featureless squares using the gray scale range - see 'Correlation_Basics'."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then dst3 = src
            corr.Run(src)
            labels(3) = corr.labels(3)

            dst2.SetTo(0)
            For i = 0 To corr.cList.Count - 1
                Dim gSq = task.gSquares(i)
                If corr.cList(i) < corr.corrThreshold Then
                    dst2(gSq).SetTo(255)
                    If standaloneTest() Then src.Rectangle(gSq, white, task.lineWidth)
                End If
            Next
        End Sub
    End Class




    Public Class FeatureLess_Compare : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Dim corr As New Correlation_Basics
        Public Sub New()
            labels(3) = "The red squares below are differences from the correlation calculation"
            desc = "Compare the correlation results with the range threshold results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)
            dst2 = fLess.dst3

            corr.Run(src)
            dst3 = corr.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            For i = 0 To corr.cList.Count - 1
                Dim correlation = corr.cList(i)
                If correlation < corr.corrThreshold Then
                    Dim gSq = task.gSquares(i)
                    Dim val = fLess.dst2.Get(Of Byte)(gSq.TopLeft.Y, gSq.TopLeft.X)
                    If val = 0 Then dst3.Rectangle(gSq, red, -1)
                End If
            Next
        End Sub
    End Class





    Public Class NR_FeatureLess_Basics : Inherits TaskParent
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            If task.contours Is Nothing Then task.contours = New Contour_Basics_List
            desc = "Use Contour_Basics to get the contour data for the top contours by size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels = 1 Then edgeline.Run(src) Else edgeline.Run(task.grayStable)
            If src.Type <> cv.MatType.CV_8U Then
                task.contours.Run(edgeline.dst2)
                dst2 = task.contours.dst2
                labels = task.contours.labels
            Else
                task.contours.Run(src)
                dst2 = task.contours.dst2
                labels = task.contours.labels
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
        Dim fless As New Hough_FeatureLessTopX
        Dim sort3 As New Sort_3Channel
        Public Sub New()
            desc = "Find the unique 3-channel pixels for the featureless regions"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fless.Run(src)
            dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            sort3.Run(fless.dst2)
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
        Dim fLess As New FeatureLess_Basics
        Dim frames As New History_Basics
        Public Sub New()
            desc = "Accumulate the edges over a span of X images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)
            dst2 = fLess.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

            frames.Run(dst2)
            dst3 = frames.dst2
        End Sub
    End Class







    Public Class NR_FeatureLess_Groups : Inherits TaskParent
        Dim redCPP As New RedList_CPP
        Dim fless As New FeatureLess_Basics
        Public classCount As Integer
        Public Sub New()
            desc = "Group RedCloud cells by the value of their featureless maxDist"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fless.Run(src)
            dst1 = fless.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            labels(2) = fless.labels(2)

            If task.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, task.motionRGB.motionMask)
            redCPP.Run(dst2 - 1)
            classCount = redCPP.classCount
            dst3 = PaletteFull(redCPP.dst2)
            labels(3) = CStr(classCount) + " featureless regions were found."
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
                fLess.Run(task.leftView)
                dst2 = fLess.dst2.Clone

                fLess.Run(task.rightView)
                dst3 = fLess.dst2.Clone
            Else
                dst2 = task.leftView
                dst3 = task.rightView
            End If
        End Sub
    End Class





    Public Class FeatureLess_RedColor : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Dim redC As New RedColor_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Use the featureLess_Basics output as input to RedColor_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)
            dst2 = fLess.dst2
            labels(2) = fLess.labels(3)

            redC.Run(dst2)
            dst3 = redC.dst2
            labels(3) = redC.labels(2)

            For Each rc In redC.rcList
                dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            Next

            If standaloneTest() Then
                strOut = RedUtil_Basics.selectCell(redC.rcMap, redC.rcList)
                If redC.rcList.Count > 0 And task.rcD Is Nothing Then
                    task.clickPoint = redC.rcList(0).maxDist
                End If
                SetTrueText(strOut, 1)
            End If
        End Sub
    End Class





    Public Class FeatureLess_Not : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Dim feat As New Feature_General
        Public Sub New()
            desc = "Use the FeatureLess mask to reduce the input to feature searches."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)
            dst2.SetTo(0)
            src.CopyTo(dst2, Not fLess.dst2)

            feat.Run(dst2)
            feat.dst2.CopyTo(dst3)
        End Sub
    End Class





    Public Class FeatureLess_Cells : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            desc = "Create a list of cells with a list of "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = fLess.dst2.Clone ' save last map

            fLess.Run(src)
            dst2 = fLess.dst2
            labels(2) = fLess.labels(2)

            Dim index = 1
            Dim rect As cv.Rect
            Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4
            Dim minSize = task.brickSize * task.brickSize
            Dim cellCount As Integer
            Dim usedList As New List(Of Integer)
            For Each r In fLess.rectList
                Dim valLast = dst1.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
                Dim val = dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
                If val = 255 Then
                    If valLast <> 0 And usedList.Contains(valLast) = False Then index = valLast
                    usedList.Add(index)
                    Dim count = cv.Cv2.FloodFill(dst2, mask, r.TopLeft, index, rect, 0, 0, flags)
                    If count > minSize Then
                        index += 1
                        cellCount += 1
                    Else
                        dst2(r).SetTo(0)
                    End If
                End If
            Next

            dst3 = PaletteBlackZero(dst2)
            labels(2) = CStr(cellCount - 1) + " featureless regions were found."
        End Sub
    End Class
End Namespace