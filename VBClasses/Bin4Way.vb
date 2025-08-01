﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Bin4Way_Basics : Inherits TaskParent
    Dim mats As New Mat_4to1
    Dim binary As New Bin4Way_SplitMean
    Dim diff(3) As Diff_Basics
    Dim labelStr(3) As String, points(3) As cv.Point
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        For i = 0 To diff.Count - 1
            diff(i) = New Diff_Basics
        Next
        labels = {"", "Quartiles for selected roi.  Click in dst1 to see different roi.", "4 brightness levels - darkest to lightest",
                      "Quartiles for the selected grid element, darkest to lightest"}
        desc = "Highlight the contours for each grid element with stats for each."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Static index As Integer = task.grid.gridMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        If task.mousePicTag = 1 Then index = task.grid.gridMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        Dim roiSave = If(index < task.gridRects.Count, task.gridRects(index), New cv.Rect)

        If task.optionsChanged Then index = 0

        Dim matList(3) As cv.Mat
        For i = 0 To matList.Count - 1
            mats.mat(i) = New cv.Mat(mats.mat(i).Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            binary.mats.mat(i) = New cv.Mat(binary.mats.mat(i).Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        Next

        Dim quadrant As Integer
        binary.Run(task.gray)
        binary.mats.Run(emptyMat)
        dst2 = binary.mats.dst2
        dst1 = binary.mats.dst3 * 0.5
        matList = binary.mats.mat
        quadrant = binary.mats.quadrant

        dst0.SetTo(0)
        For i = 0 To diff.Count - 1
            diff(i).Run(binary.mats.mat(i))
            dst0 = dst0 Or diff(i).dst2
        Next

        Dim counts(3, task.gridRects.Count) As Integer
        Dim contourCounts As New List(Of List(Of Integer))
        Dim means As New List(Of List(Of Single))

        Dim allContours As cv.Point()() = Nothing
        For i = 0 To counts.GetUpperBound(0)
            For j = 0 To task.gridRects.Count - 1
                Dim roi = task.gridRects(j)
                Dim tmp = matList(i)(roi)
                cv.Cv2.FindContours(tmp, allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
                If i = 0 Then
                    contourCounts.Add(New List(Of Integer))
                    means.Add(New List(Of Single))
                End If
                contourCounts(j).Add(allContours.Count)
                means(j).Add(task.gray(roi).Mean(tmp)(0))
                If i = quadrant Then SetTrueText(CStr(allContours.Count), roi.TopLeft, 1)
                counts(i, j) = allContours.Count
            Next
        Next

        Dim bump = 3
        Dim ratio = dst2.Height / task.gridRects(0).Height
        For i = 0 To matList.Count - 1
            Dim tmp As cv.Mat = matList(i)(roiSave) * 0.5
            Dim nextCount = tmp.CountNonZero
            Dim tmpVolatile As cv.Mat = dst0(roiSave) And tmp
            tmp.SetTo(255, tmpVolatile)
            dst0(roiSave).CopyTo(tmp, tmpVolatile)
            Dim r = New cv.Rect(0, 0, tmp.Width * ratio, tmp.Height * ratio)
            mats.mat(i)(r) = tmp.Resize(New cv.Size(r.Width, r.Height))

            If task.heartBeat Then
                Dim plus = mats.mat(i)(r).Width / 2
                points(i) = Choose(i + 1, New cv.Point(bump + plus, bump), New cv.Point(bump + dst2.Width / 2 + plus, bump),
                                          New cv.Point(bump + plus, bump + dst2.Height / 2),
                                          New cv.Point(bump + dst2.Width / 2 + plus, bump + dst2.Height / 2))
                labelStr(i) = (CStr(nextCount) + " pixels" + vbCrLf + CStr(contourCounts(index)(i)) + " contours" + vbCrLf +
                               Format(means(index)(i), fmt0) + " mean" + vbCrLf + CStr(tmpVolatile.CountNonZero) + " volatile")
            End If
        Next

        For i = 0 To labelStr.Count - 1
            SetTrueText(labelStr(i), points(i), 3)
        Next

        mats.Run(emptyMat)
        dst3 = mats.dst2

        dst1.Rectangle(roiSave, white, task.lineWidth)
        task.color.Rectangle(roiSave, white, task.lineWidth)
    End Sub
End Class








Public Class Bin4Way_Canny : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim binary As New Bin4Way_SplitMean
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        desc = "Find edges from each of the binarized images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)

        binary.Run(src)

        edges.Run(binary.mats.mat(0))  ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.Run(binary.mats.mat(1))  ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(1) Or mats.mat(3)

        edges.Run(binary.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(2) Or mats.mat(3)
        mats.Run(emptyMat)
        dst2 = mats.dst2
        If mats.dst3.Channels() = 3 Then
            labels(3) = "Combo of first 3 below.  Click quadrants in dst2."
            dst3 = mats.mat(3)
        Else
            dst3 = mats.dst3
        End If
    End Sub
End Class






Public Class Bin4Way_Sobel : Inherits TaskParent
    Dim edges As New Edge_Sobel
    Dim binary As New Bin4Way_SplitMean
    Public mats As New Mat_4to1
    Public Sub New()
        OptionParent.FindSlider("Sobel kernel Size").Value = 5
        labels(2) = "Edges between halves, lightest, darkest, and the combo"
        labels(3) = "Click any quadrant in dst2 to view it in dst3"
        desc = "Collect Sobel edges from binarized images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(src)

        edges.Run(binary.mats.mat(0)) ' the light and dark halves
        mats.mat(0) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        edges.Run(binary.mats.mat(1)) ' the lightest of the light half
        mats.mat(1) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(1) Or mats.mat(3)

        edges.Run(binary.mats.mat(3))  ' the darkest of the dark half
        mats.mat(2) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(3) = mats.mat(2) Or mats.mat(3)

        mats.Run(emptyMat)
        dst3 = mats.dst2
        dst2 = mats.mat(1)
    End Sub
End Class








Public Class Bin4Way_Unstable1 : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim diff As New Diff_Basics
    Public Sub New()
        desc = "Find the unstable pixels in the binary image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        diff.Run(binary.dst3)
        dst3 = diff.dst2
        If task.heartBeat Then labels(3) = "There are " + CStr(dst3.CountNonZero) + " unstable pixels"
    End Sub
End Class







Public Class Bin4Way_UnstableEdges : Inherits TaskParent
    Dim canny As New Edge_Basics
    Dim blur As New Blur_Basics
    Dim unstable As New Bin4Way_Unstable
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        desc = "Find unstable pixels but remove those that are also edges."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        canny.Run(src)
        blur.Run(canny.dst2)
        dst1 = blur.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        unstable.Run(src)
        dst2 = unstable.dst2
        dst3 = unstable.dst3

        If task.gOptions.DebugCheckBox.Checked = False Then dst3.SetTo(0, dst1)
    End Sub
End Class







Public Class Bin4Way_UnstablePixels : Inherits TaskParent
    Dim unstable As New Bin4Way_UnstableEdges
    Public gapValues As New List(Of Byte)
    Public Sub New()
        desc = "Identify the unstable grayscale pixel values "
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        unstable.Run(task.gray)
        dst2 = unstable.dst3

        Dim points = dst2.FindNonZero()
        If points.Rows = 0 Then Exit Sub
        Dim pts(points.Rows * 2 - 1) As Integer
        Marshal.Copy(points.Data, pts, 0, pts.Length)

        Dim pixels As New List(Of Byte)
        Dim pixelSort As New SortedList(Of Byte, Integer)(New compareByte)
        For i = 0 To pts.Count - 1 Step 2
            Dim val = task.gray.Get(Of Byte)(pts(i + 1), pts(i))
            If pixels.Contains(val) = False Then
                pixelSort.Add(val, 1)
                pixels.Add(val)
            End If
        Next

        Dim gapThreshold = 2
        gapValues.Clear()
        strOut = "These are the ranges of grayscale bytes where there is fuzziness." + vbCrLf
        Dim lastIndex As Integer, lastGap As Integer
        For Each index In pixelSort.Keys
            If Math.Abs(lastIndex - index) > gapThreshold Then
                strOut += vbCrLf
                gapValues.Add((index + lastGap) / 2)
                lastGap = index
                For i = index + 1 To pixelSort.Keys.Count - 1
                    If pixelSort.Keys.ElementAt(i) - lastGap > gapThreshold Then Exit For
                    lastGap = i
                Next
            End If
            strOut += CStr(index) + vbTab
            lastIndex = index
        Next
        If gapValues.Count < 4 Then
            gapValues.Add((255 + lastGap) / 2)
        End If

        strOut += vbCrLf + vbCrLf + "The best thresholds for this image to avoid fuzziness are: " + vbCrLf
        For Each index In gapValues
            strOut += CStr(index) + vbTab
        Next
        SetTrueText(strOut, 3)
        If task.heartBeat Then labels(3) = "There are " + CStr(dst2.CountNonZero) + " unstable pixels"
    End Sub
End Class







Public Class Bin4Way_SplitValley : Inherits TaskParent
    Dim binary As New Binarize_Simple
    Dim valley As New HistValley_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        labels(2) = "A 4-way split - darkest (upper left) to lightest (lower right)"
        desc = "Binarize an image using the valleys provided by HistValley_Basics"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(task.gray)
        Dim mask = binary.dst2.Clone

        If task.heartBeat Then valley.Run(task.gray)

        mats.mat(0) = task.gray.InRange(0, valley.valleys(1) - 1)
        mats.mat(1) = task.gray.InRange(valley.valleys(1), valley.valleys(2) - 1)
        mats.mat(2) = task.gray.InRange(valley.valleys(2), valley.valleys(3) - 1)
        mats.mat(3) = task.gray.InRange(valley.valleys(3), 255)

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
        labels(3) = mats.labels(3)
    End Sub
End Class







Public Class Bin4Way_UnstablePixels1 : Inherits TaskParent
    Dim hist As New Hist_Basics
    Dim unstable As New Bin4Way_UnstableEdges
    Public gapValues As New List(Of Byte)
    Dim boundaries(4) As Byte
    Public Sub New()
        task.gOptions.setHistogramBins(255)
        desc = "Identify the unstable grayscale pixel values "
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hist.Run(task.gray)

        unstable.Run(task.gray)
        dst2 = unstable.dst3

        Dim points = dst2.FindNonZero()
        If points.Rows = 0 Then Exit Sub
        Dim pts(points.Rows * 2 - 1) As Integer
        Marshal.Copy(points.Data, pts, 0, pts.Length)

        Dim pixels As New List(Of Byte)
        Dim pixelSort As New SortedList(Of Byte, Integer)(New compareByte)
        For i = 0 To pts.Count - 1 Step 2
            Dim val = task.gray.Get(Of Byte)(pts(i + 1), pts(i))
            If pixels.Contains(val) = False Then
                pixelSort.Add(val, 1)
                pixels.Add(val)
            End If
        Next

        boundaries(0) = 0 * 255 / 4
        boundaries(1) = 1 * 255 / 4
        boundaries(2) = 2 * 255 / 4
        boundaries(3) = 3 * 255 / 4
        boundaries(4) = 255
        Dim gapThreshold = 2, lastIndex As Integer, bIndex As Integer = 1
        strOut = "These are the ranges of grayscale bytes where there is fuzziness." + vbCrLf
        For i = 0 To pixelSort.Keys.Count - 1
            Dim index = pixelSort.ElementAt(i).Key
            If Math.Abs(lastIndex - index) > gapThreshold Then
                strOut += vbCrLf
                If bIndex < boundaries.Count Then
                    boundaries(bIndex) = index
                    bIndex += 1
                End If
            End If
            strOut += CStr(index) + vbTab
            lastIndex = index
        Next

        gapValues.Clear()
        For i = 1 To boundaries.Count - 1
            Dim minVal = Byte.MaxValue
            Dim minIndex = 0
            For j = boundaries(i - 1) To boundaries(i) - 1
                If hist.histArray(j) < minVal Then
                    minVal = hist.histArray(j)
                    minIndex = j
                End If
            Next
            gapValues.Add(minIndex)
        Next
        strOut += vbCrLf + vbCrLf + "The best thresholds for this image to avoid fuzziness are: " + vbCrLf
        For Each index In gapValues
            strOut += CStr(index) + vbTab
        Next
        SetTrueText(strOut, 3)
        If task.heartBeat Then labels(3) = "There are " + CStr(dst2.CountNonZero) + " unstable pixels"
    End Sub
End Class






Public Class Bin4Way_SplitGaps : Inherits TaskParent
    Dim unstable As New Bin4Way_UnstablePixels
    Public mats As New Mat_4Click
    Dim diff(3) As Diff_Basics
    Public Sub New()
        For i = 0 To diff.Count - 1
            diff(i) = New Diff_Basics
            mats.mat(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        Next
        If standalone Then task.gOptions.displaydst1.checked = true
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "A 4-way split - darkest (upper left) to lightest (lower right)"
        desc = "Separate the quartiles of the image using the fuzzy grayscale pixel values"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        unstable.Run(task.gray)

        Dim lastVal As Integer = 255
        For i = Math.Min(mats.mat.Count, unstable.gapValues.Count) - 1 To 0 Step -1
            mats.mat(i) = task.gray.InRange(unstable.gapValues(i), lastVal)
            lastVal = unstable.gapValues(i)
        Next

        dst1.SetTo(0)
        For i = 0 To diff.Count - 1
            diff(i).Run(mats.mat(i))
            dst1 = dst1 Or diff(i).dst2
        Next
        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
        If task.heartBeat Then labels(1) = "There are " + CStr(dst1.CountNonZero) + " unstable pixels"
    End Sub
End Class








Public Class Bin4Way_RegionsLeftRight : Inherits TaskParent
    Dim binaryLeft As New Bin4Way_SplitMean
    Dim binaryRight As New Bin4Way_SplitMean
    Public classCount = 4 ' 4-way split
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Left in in 4 colors", "Right image in 4 colors"}
        desc = "Add the 4-way split of left and right views."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binaryLeft.Run(src)

        dst0.SetTo(1, binaryLeft.mats.mat(0))
        dst0.SetTo(2, binaryLeft.mats.mat(1))
        dst0.SetTo(3, binaryLeft.mats.mat(2))
        dst0.SetTo(4, binaryLeft.mats.mat(3))

        dst2 = ShowPalette(dst0)

        binaryRight.Run(task.rightView)

        dst1.SetTo(1, binaryRight.mats.mat(0))
        dst1.SetTo(2, binaryRight.mats.mat(1))
        dst1.SetTo(3, binaryRight.mats.mat(2))
        dst1.SetTo(4, binaryRight.mats.mat(3))

        dst3 = ShowPalette(dst1)
    End Sub
End Class







Public Class Bin4Way_Regions1 : Inherits TaskParent
    Dim binary As New Binarize_Simple
    Public mats As New Mat_4Click
    Public classCount = 4 ' 4-way split 
    Public Sub New()
        labels(2) = "A 4-way split - darkest (upper left) to lightest (lower right)"
        desc = "Binarize an image and split it into quartiles using peaks."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(task.gray)
        Dim mask = binary.dst2.Clone

        Dim midColor = binary.meanScalar(0)
        Dim topColor = cv.Cv2.Mean(task.gray, mask)(0)
        Dim botColor = cv.Cv2.Mean(task.gray, Not mask)(0)
        mats.mat(0) = task.gray.InRange(0, botColor)
        mats.mat(1) = task.gray.InRange(botColor, midColor)
        mats.mat(2) = task.gray.InRange(midColor, topColor)
        mats.mat(3) = task.gray.InRange(topColor, 255)

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
        labels(3) = mats.labels(3)
    End Sub
End Class






Public Class Bin4Way_BasicsColors : Inherits TaskParent
    Dim bin4 As New Bin4Way_Basics
    Dim color8U As New Color8U_Basics
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        desc = "Test Bin4Way_Basics with different src inputs."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        bin4.Run(color8U.dst3)
        dst1 = bin4.dst1
        dst2 = bin4.dst2
        dst3 = bin4.dst3
        labels = bin4.labels
        trueData = bin4.trueData
    End Sub
End Class





Public Class Bin4Way_Unstable : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim diff(3) As Diff_Basics
    Public Sub New()
        For i = 0 To diff.Count - 1
            diff(i) = New Diff_Basics
        Next
        labels(2) = "Image separated into 4 levels - darkest to lightest"
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the unstable pixels in the binary image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        dst3.SetTo(0)
        For i = 0 To diff.Count - 1
            diff(i).Run(binary.mats.mat(i))
            dst3 = dst3 Or diff(i).dst2
        Next
        If task.heartBeat Then labels(3) = "There are " + CStr(dst3.CountNonZero) + " unstable pixels"
    End Sub
End Class





Public Class Bin4Way_BasicsRed : Inherits TaskParent
    Public mats As New Mat_4to1
    Dim hist As New Hist_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(255)
        labels(3) = "Grayscale histogram of the image with markers showing where each quarter of the samples are."
        desc = "Implement a 4-way split similar to the Bin3Way_Basics algorithm."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim bins = task.histogramBins
        hist.Run(task.gray)
        dst3 = hist.dst2

        Dim histArray = hist.histArray
        Dim fraction As Integer = dst2.Total / 4
        Dim accums As New List(Of Integer)({0, 0, 0, 0})
        Dim quartiles As New List(Of Integer)({0, 0, 0, 0})
        Dim index As Integer
        For i = 0 To histArray.Count - 1
            accums(index) += histArray(i)
            If accums(index) >= fraction Then
                quartiles(index) = i
                index += 1
            End If
        Next

        For i = 0 To quartiles.Count - 1
            Dim offset = quartiles(i) / bins * dst3.Width
            DrawLine(dst3, New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), white)
        Next

        mats.mat(0) = task.gray.InRange(0, quartiles(0) - 1)
        mats.mat(1) = task.gray.InRange(quartiles(0), quartiles(1) - 1)
        mats.mat(2) = task.gray.InRange(quartiles(1), quartiles(2) - 1)
        mats.mat(3) = task.gray.InRange(quartiles(2), 255)

        If standaloneTest() Then
            mats.Run(emptyMat)
            dst2 = mats.dst2
        End If
    End Sub
End Class









Public Class Bin4Way_RedCloud : Inherits TaskParent
    Dim bin2 As New Bin4Way_BasicsRed
    Dim flood As New Flood_BasicsMask
    Dim cellMaps(3) As cv.Mat, rcList(3) As List(Of rcData)
    Dim options As New Options_Bin2WayRedCloud
    Public Sub New()
        flood.showSelected = False
        desc = "Identify the lightest and darkest regions separately and then combine the rcData."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        dst3 = runRedC(src, labels(3))

        If task.optionsChanged Then
            For i = 0 To rcList.Count - 1
                rcList(i) = New List(Of rcData)
                cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            Next
        End If

        bin2.Run(src)

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = options.startRegion To options.endRegion
            task.redC.rcMap = cellMaps(i)
            task.redC.rcList = rcList(i)
            flood.inputRemoved = Not bin2.mats.mat(i)
            flood.Run(bin2.mats.mat(i))
            cellMaps(i) = task.redC.rcMap.Clone
            rcList(i) = New List(Of rcData)(task.redC.rcList)
            For Each rc In task.redC.rcList
                If rc.index = 0 Then Continue For
                sortedCells.Add(rc.pixels, rc)
            Next
        Next

        dst2 = RebuildRCMap(sortedCells)

        If task.heartBeat Then labels(2) = CStr(task.redC.rcList.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class







Public Class Bin4Way_Regions : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Public classCount As Integer = 4 ' 4-way split 
    Public Sub New()
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        rebuildMats()
        labels = {"", "", "CV_8U version of dst3 with values ranging from 1 to 4", "Palettized version of dst2"}
        desc = "Add the 4-way split of images to define the different regions."
    End Sub
    Private Sub rebuildMats()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For i = 0 To binary.mats.mat.Count - 1
            binary.mats.mat(i) = New cv.Mat(dst1.Size, cv.MatType.CV_8UC1, 0)
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(src)

        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        dst2.SetTo(1, binary.mats.mat(0))
        dst2.SetTo(2, binary.mats.mat(1))
        dst2.SetTo(3, binary.mats.mat(2))
        dst2.SetTo(4, binary.mats.mat(3))

        If standaloneTest() Then dst3 = ShowPalette(dst2)
    End Sub
End Class








Public Class Bin4Way_SplitMean : Inherits TaskParent
    Public binary As New Binarize_Simple
    Public mats As New Mat_4Click
    Dim botColor As cv.Scalar, midColor As cv.Scalar, topColor As cv.Scalar
    Public Sub New()
        labels(2) = "A 4-way split - darkest (upper left) to lightest (lower right)"
        desc = "Binarize an image and split it into quartiles using peaks."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(task.gray)
        Dim mask = binary.dst2.Clone

        If task.heartBeat Then
            midColor = binary.meanScalar(0)
            topColor = cv.Cv2.Mean(task.gray, mask)(0)
            botColor = cv.Cv2.Mean(task.gray, Not mask)(0)
        End If

        mats.mat(0) = task.gray.InRange(0, botColor)
        mats.mat(1) = task.gray.InRange(botColor, midColor)
        mats.mat(2) = task.gray.InRange(midColor, topColor)
        mats.mat(3) = task.gray.InRange(topColor, 255)

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
        labels(3) = mats.labels(3)
    End Sub
End Class