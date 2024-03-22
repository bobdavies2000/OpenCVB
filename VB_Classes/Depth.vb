Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_Basics : Inherits VB_Algorithm
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        vbAddAdvice(traceName + ": use global option to control 'Max Depth'.")
        desc = "Colorize the depth data into task.depthRGB"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.pcSplit(2)

        task.pcSplit(2) = task.pcSplit(2).Threshold(task.maxZmeters, task.maxZmeters, cv.ThresholdTypes.Trunc)
        task.maxDepthMask = task.pcSplit(2).ConvertScaleAbs().InRange(task.maxZmeters, task.maxZmeters)
        If standalone Then dst3 = task.maxDepthMask
        setTrueText(task.gMat.strOut, 3)

        colorizer.Run(task.pcSplit(2))
        task.depthRGB = colorizer.dst2
    End Sub
End Class







Public Class Depth_Display : Inherits VB_Algorithm
    Public Sub New()
        gOptions.displayDst0.Checked = True
        gOptions.displayDst1.Checked = True
        labels = {"task.pcSplit(2)", "task.pointcloud", "task.depthMask", "task.noDepthMask"}
        desc = "Display the task.pcSplit(2), task.pointcloud, task.depthMask, and task.noDepthMask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = task.pcSplit(2)
        dst1 = task.pointCloud
        dst2 = task.depthMask
        dst3 = task.noDepthMask
    End Sub
End Class







Public Class Depth_Flatland : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Region Count", 1, 250, 10)
        labels(3) = "Grayscale version"
        desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static regionSlider = findSlider("Region Count")
        Dim reductionFactor As Single = regionSlider.Maximum - regionSlider.Value
        dst2 = task.depthRGB / reductionFactor
        dst2 *= reductionFactor
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Depth_FirstLastDistance : Inherits VB_Algorithm
    Public Sub New()
        desc = "Monitor the first and last depth distances"
    End Sub
    Private Sub identifyMinMax(pt As cv.Point, text As String)
        dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        setTrueText(text, pt, 2)

        dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        setTrueText(text, pt, 3)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim mm As mmData = vbMinMax(task.pcSplit(2), task.depthMask)
        task.depthRGB.CopyTo(dst2)

        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = "Min Depth " + Format(mm.minVal, fmt1) + "m"
        identifyMinMax(mm.minLoc, labels(2))

        labels(3) = "Max Depth " + Format(mm.maxVal, fmt1) + "m"
        identifyMinMax(mm.maxLoc, labels(3))
    End Sub
End Class







Public Class Depth_HolesRect : Inherits VB_Algorithm
    Dim shadow As New Depth_Holes
    Public Sub New()
        labels(2) = "The 10 largest contours in the depth holes."
        desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        shadow.Run(src)

        Dim contours As cv.Point()()
        If shadow.dst3.Channels = 3 Then shadow.dst3 = shadow.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        contours = cv.Cv2.FindContoursAsArray(shadow.dst3, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim sortContours As New SortedList(Of Integer, List(Of cv.Point))(New compareAllowIdenticalIntegerInverted)
        For Each c In contours
            sortContours.Add(c.Length, c.ToList)
        Next
        dst3.SetTo(0)
        For i = 0 To Math.Min(sortContours.Count, 10) - 1
            Dim contour = sortContours.ElementAt(i).Value
            Dim minRect = cv.Cv2.MinAreaRect(contour)
            Dim nextColor = New cv.Scalar(task.vecColors(i Mod 256)(0), task.vecColors(i Mod 256)(1), task.vecColors(i Mod 256)(2))
            drawRotatedRectangle(minRect, dst2, nextColor)
            vbDrawContour(dst3, contour.ToList, cv.Scalar.White, task.lineWidth)
        Next
        cv.Cv2.AddWeighted(dst2, 0.5, task.depthRGB, 0.5, 0, dst2)
    End Sub
End Class








Public Class Depth_MeanStdev_MT : Inherits VB_Algorithm
    Dim meanSeries As New cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Rows, dst3.Cols, cv.MatType.CV_8U, 0)
        desc = "Collect a time series of depth mean and stdev to highlight where depth is unstable."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then meanSeries = New cv.Mat(task.gridList.Count, task.frameHistoryCount, cv.MatType.CV_32F, 0)

        Dim index = task.frameCount Mod task.frameHistoryCount
        Dim meanValues(task.gridList.Count - 1) As Single
        Dim stdValues(task.gridList.Count - 1) As Single
        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.pcSplit(2)(roi), mean, stdev, task.depthMask(roi))
            meanSeries.Set(Of Single)(i, index, mean)
            If task.frameCount >= task.frameHistoryCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues(i) = mean
                stdValues(i) = stdev
            End If
        End Sub)

        If task.frameCount >= task.frameHistoryCount Then
            Dim means As New cv.Mat(task.gridList.Count, 1, cv.MatType.CV_32F, meanValues.ToArray)
            Dim stdevs As New cv.Mat(task.gridList.Count, 1, cv.MatType.CV_32F, stdValues.ToArray)
            Dim meanmask = means.Threshold(1, task.maxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            Dim mm As mmData = vbMinMax(means, meanmask)
            Dim stdMask = stdevs.Threshold(0.001, task.maxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            Dim mmStd = vbMinMax(stdevs, stdMask)

            Static maxMeanVal As Single, maxStdevVal As Single
            maxMeanVal = Math.Max(maxMeanVal, mm.maxVal)
            maxStdevVal = Math.Max(maxStdevVal, mmStd.maxVal)

            Parallel.For(0, task.gridList.Count,
            Sub(i)
                Dim roi = task.gridList(i)
                dst3(roi).SetTo(255 * stdevs.Get(Of Single)(i, 0) / maxStdevVal)
                dst3(roi).SetTo(0, task.noDepthMask(roi))

                dst2(roi).SetTo(255 * means.Get(Of Single)(i, 0) / maxMeanVal)
                dst2(roi).SetTo(0, task.noDepthMask(roi))
            End Sub)

            If task.heartBeat Then
                maxMeanVal = 0
                maxStdevVal = 0
            End If

            If standaloneTest() Then
                For i = 0 To task.gridList.Count - 1
                    Dim roi = task.gridList(i)
                    setTrueText(Format(meanValues(i), fmt3) + vbCrLf + Format(stdValues(i), fmt3), New cv.Point(roi.X, roi.Y), 3)
                Next
            End If

            dst3 = dst3 Or task.gridMask
            labels(2) = "The regions where the depth is volatile are brighter.  Stdev min " + Format(mmStd.minVal, fmt3) + " Stdev Max " + Format(mmStd.maxVal, fmt3)
            labels(3) = "Mean/stdev for each ROI: Min " + Format(mm.minVal, fmt3) + " Max " + Format(mm.maxVal, fmt3)
        End If
    End Sub
End Class








Public Class Depth_MeanStdevPlot : Inherits VB_Algorithm
    Dim plot1 As New Plot_OverTimeSingle
    Dim plot2 As New Plot_OverTimeSingle
    Public Sub New()
        desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        Dim depthMask As cv.Mat = task.depthMask
        cv.Cv2.MeanStdDev(task.pcSplit(2), mean, stdev, depthMask)

        plot1.plotData = mean(0)
        plot1.Run(src)
        dst2 = plot1.dst2

        plot2.plotData = stdev(0)
        plot2.Run(src)
        dst3 = plot2.dst2

        labels(2) = "Plot of mean depth = " + Format(mean(0), fmt1) + " min = " + Format(plot1.min, fmt2) + " max = " + Format(plot1.max, fmt2)
        labels(3) = "Plot of depth stdev = " + Format(stdev(0), fmt1) + " min = " + Format(plot2.min, fmt2) + " max = " + Format(plot2.max, fmt2)
    End Sub
End Class




Public Class Depth_Uncertainty : Inherits VB_Algorithm
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Uncertainty threshold", 1, 255, 100)
        labels(3) = "Mask of areas with stable depth"
        desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Uncertainty threshold")
        retina.Run(task.depthRGB)
        dst2 = retina.dst2
        dst3 = retina.dst3.Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette : Inherits VB_Algorithm
    Dim customColorMap As New cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gColor.gradientWidth = 255
        gColor.Run(empty)
        customColorMap = gColor.gradient

        Dim mult = 255 / task.maxZmeters
        Dim depthNorm = (task.pcSplit(2) * mult).ToMat
        depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
        Dim ColorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, customColorMap.Data())
        cv.Cv2.ApplyColorMap(src, dst2, ColorMap)
    End Sub
End Class








Public Class Depth_Colorizer_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Depth_Colorizer_Open()
        desc = "Display depth data with InRange.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.maxZmeters)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Depth_Colorizer_Close(cPtr)
    End Sub
End Class






Public Class Depth_LocalMinMax_MT : Inherits VB_Algorithm
    Public minPoint(0) As cv.Point2f
    Public maxPoint(0) As cv.Point2f
    Public Sub New()
        labels = {"", "", "Highlight (usually yellow) is min distance, red is max distance",
                  "Highlight is min, red is max.  Lines would indicate planes are present."}
        desc = "Find min and max depth in each segment."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            src.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, task.gridMask)
        End If

        If minPoint.Length <> task.gridList.Count Then
            ReDim minPoint(task.gridList.Count - 1)
            ReDim maxPoint(task.gridList.Count - 1)
        End If

        If task.heartBeat Then dst3.SetTo(0)
        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            Dim mm As mmData = vbMinMax(task.pcSplit(2)(roi), task.depthMask(roi))
            If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
            minPoint(i) = New cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y)
            maxPoint(i) = New cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y)

            dst2(roi).Circle(mm.minLoc, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2(roi).Circle(mm.maxLoc, task.dotSize, cv.Scalar.Red, -1, task.lineType)

            Dim p1 = New cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y)
            Dim p2 = New cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y)
            dst3.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Circle(p2, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        End Sub)
    End Sub
End Class





Public Class Depth_MinMaxToVoronoi : Inherits VB_Algorithm
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(task.gridList.Count * 4 - 1)

        labels = {"", "", "Red is min distance, blue is max distance", "Voronoi representation of min and max points for each cell."}
        desc = "Find min and max depth in each roi and create a voronoi representation using the min and max points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then ReDim kalman.kInput(task.gridList.Count * 4 - 1)

        dst2 = src.Clone()
        dst2.SetTo(cv.Scalar.White, task.gridMask)

        Dim depthmask As cv.Mat = task.depthMask

        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            Dim mm As mmData = vbMinMax(task.pcSplit(2)(roi), depthmask(roi))
            If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
            kalman.kInput(i * 4) = mm.minLoc.X
            kalman.kInput(i * 4 + 1) = mm.minLoc.Y
            kalman.kInput(i * 4 + 2) = mm.maxLoc.X
            kalman.kInput(i * 4 + 3) = mm.maxLoc.Y
        End Sub)

        kalman.Run(src)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            Dim ptmin = New cv.Point2f(kalman.kOutput(i * 4) + roi.X, kalman.kOutput(i * 4 + 1) + roi.Y)
            Dim ptmax = New cv.Point2f(kalman.kOutput(i * 4 + 2) + roi.X, kalman.kOutput(i * 4 + 3) + roi.Y)
            ptmin = validatePoint2f(ptmin)
            ptmax = validatePoint2f(ptmax)
            subdiv.Insert(ptmin)
            dst2.Circle(ptmin, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst2.Circle(ptmax, task.dotSize, cv.Scalar.Blue, -1, task.lineType)
        Next
        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class





Public Class Depth_ColorMap : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Depth ColorMap Alpha X100", 1, 100, 5)
            sliders.setupTrackBar("Depth ColorMap Beta", 1, 100, 3)
        End If
        desc = "Display the depth as a color map"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static alphaSlider = findSlider("Depth ColorMap Alpha X100")
        Static betaSlider = findSlider("Depth ColorMap Beta")
        cv.Cv2.ConvertScaleAbs(task.pcSplit(2) * 1000, dst1, alphaSlider.Value / 100, betaSlider.Value)
        dst1 += 1
        dst2 = vbPalette(dst1)
        dst2.SetTo(0, task.noDepthMask)
        dst3 = task.palette.dst3
    End Sub
End Class






Public Class Depth_NotMissing : Inherits VB_Algorithm
    Public bgSub As New BGSubtract_Basics
    Public Sub New()
        labels(3) = "Stable (non-zero) Depth"
        desc = "Collect X frames, compute stable depth using the BGR and Depth image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then src = task.depthRGB
        bgSub.Run(src)
        dst2 = bgSub.dst2
        dst3 = Not bgSub.dst2
        labels(2) = "Unstable Depth" + " using " + bgSub.options.methodDesc + " method"
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Depth_Median : Inherits VB_Algorithm
    Dim median As New Math_Median_CDF
    Public Sub New()
        median.rangeMax = task.maxZmeters
        median.rangeMin = 0
        desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        median.Run(task.pcSplit(2))

        Dim mask As cv.Mat
        mask = task.pcSplit(2).LessThan(median.medianVal)
        task.pcSplit(2).CopyTo(dst2, mask)

        dst2.SetTo(0, task.noDepthMask)

        labels(2) = "Median Depth < " + Format(median.medianVal, fmt1)

        dst3.SetTo(0)
        task.depthRGB.CopyTo(dst3, Not mask)
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = "Median Depth > " + Format(median.medianVal, fmt1)
    End Sub
End Class






Public Class Depth_SmoothingMat : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold in millimeters", 1, 1000, 10)
        labels(3) = "Depth pixels after smoothing"
        desc = "Use depth rate of change to smooth the depth values in close range"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold in millimeters")
        Static lastDepth = task.pcSplit(2)

        If standaloneTest() Then src = task.pcSplit(2)
        Dim rect = If(task.drawRect.Width <> 0, task.drawRect, New cv.Rect(0, 0, src.Width, src.Height))

        cv.Cv2.Subtract(lastDepth, task.pcSplit(2), dst2)

        Dim mmThreshold = CSng(thresholdSlider.Value / 1000)
        dst2 = dst2.Threshold(mmThreshold, 0, cv.ThresholdTypes.TozeroInv).Threshold(-mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(task.pcSplit(2), dst2, dst3)
        lastDepth = task.pcSplit(2)

        labels(2) = "Smoothing Mat: range to " + CStr(task.maxZmeters) + " meters"
    End Sub
End Class





Public Class Depth_Smoothing : Inherits VB_Algorithm
    Dim smooth As New Depth_SmoothingMat
    Dim reduction As New Reduction_Basics
    Public reducedDepth As New cv.Mat
    Public mats As New Mat_4to1
    Public colorize As New Depth_ColorMap
    Public Sub New()
        redOptions.BitwiseReduction.Checked = True
        labels(3) = "Mask of depth that is smooth"
        desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        smooth.Run(task.pcSplit(2))
        Dim input = smooth.dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        input.ConvertTo(mats.mat(0), cv.MatType.CV_8UC1)
        Dim tmp As New cv.Mat
        cv.Cv2.Add(smooth.dst3, smooth.dst2, tmp)
        mats.mat(1) = tmp.Normalize(0, 255, cv.NormTypes.MinMax).ConvertScaleAbs()

        reduction.Run(task.pcSplit(2))
        reduction.dst2.ConvertTo(reducedDepth, cv.MatType.CV_32F)
        colorize.Run(reducedDepth)
        dst2 = colorize.dst2
        mats.Run(empty)
        dst3 = mats.dst2
        labels(2) = smooth.labels(2)
    End Sub
End Class







Public Class Depth_HolesOverTime : Inherits VB_Algorithm
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of images to retain", 0, 30, 10)
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Latest hole mask"
        desc = "Integrate memory holes over time to identify unstable depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static countSlider = findSlider("Number of images to retain")
        Static saveCount = countSlider.Value
        If saveCount <> countSlider.Value Then
            images.Clear()
            dst0.SetTo(0)
        End If
        saveCount = countSlider.Value

        dst3 = task.noDepthMask
        dst1 = dst3.Threshold(0, 1, cv.ThresholdTypes.Binary)
        images.Add(dst1)

        dst0 += dst1
        dst2 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Depth holes integrated over the past " + CStr(images.Count) + " images"
        If images.Count >= saveCount Then
            dst0 -= images(0)
            images.RemoveAt(0)
        End If
    End Sub
End Class








Public Class Depth_Holes : Inherits VB_Algorithm
    Dim element As New cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Amount of dilation of borderMask", 1, 10, 1)
            sliders.setupTrackBar("Amount of dilation of holeMask", 0, 10, 0)
        End If
        labels(3) = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        desc = "Identify holes in the depth image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static borderSlider = findSlider("Amount of dilation of borderMask")
        Static holeSlider = findSlider("Amount of dilation of holeMask")
        dst2 = task.pcSplit(2).Threshold(0.01, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2 = dst2.Dilate(element, Nothing, holeSlider.Value)
        dst3 = dst2.Dilate(element, Nothing, borderSlider.Value)
        dst3 = dst3 Xor dst2
        If standaloneTest() Then task.depthRGB.CopyTo(dst3, dst3)
    End Sub
End Class







'Public Class Depth_Fusion : Inherits VB_Algorithm
'    Dim dMax As New Depth_StableMax
'    Public Sub New()
'        desc = "Fuse the depth from the previous x frames."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static fuseFrames As New List(Of cv.Mat)

'        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

'        If task.optionsChanged Then fuseFrames = New List(Of cv.Mat)

'        fuseFrames.Add(src.Clone)
'        If fuseFrames.Count > task.frameHistoryCount Then fuseFrames.RemoveAt(0)

'        If fuseFrames.Count = 0 Then Exit Sub
'        dst2 = fuseFrames(0).Clone
'        For i = 1 To fuseFrames.Count - 1
'            cv.Cv2.Max(fuseFrames(i), dst2, dst2)
'        Next
'    End Sub
'End Class









Public Class Depth_Dilate : Inherits VB_Algorithm
    Dim dilate As New Dilate_Basics
    Public Sub New()
        desc = "Dilate the depth data to fill holes."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dilate.Run(task.pcSplit(2))
        dst2 = dilate.dst2
    End Sub
End Class








Public Class Depth_ForegroundHead : Inherits VB_Algorithm
    Dim fgnd As New Depth_ForegroundBlob
    Public kalman As New Kalman_Basics
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New()
        labels(2) = "Blue is current, red is kalman, green is trusted"
        desc = "Use Depth_ForeGround to find the foreground blob.  Then find the probable head of the person in front of the camera."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fgnd.Run(src)

        trustworthy = False
        If fgnd.dst2.CountNonZero And fgnd.maxIndex >= 0 Then
            Dim rectSize = 50
            If src.Width > 1000 Then rectSize = 250
            Dim xx = fgnd.blobLocation(fgnd.maxIndex).X - rectSize / 2
            Dim yy = fgnd.blobLocation(fgnd.maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > src.Width Then xx = src.Width - rectSize
            dst2 = fgnd.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            kalman.kInput = {xx, yy, rectSize, rectSize}
            kalman.Run(src)
            Dim nextRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
            dst2.Rectangle(kRect, cv.Scalar.Red, 2)
            dst2.Rectangle(nextRect, cv.Scalar.Blue, 2)
            If Math.Abs(kRect.X - nextRect.X) < rectSize / 4 And Math.Abs(kRect.Y - nextRect.Y) < rectSize / 4 Then
                trustedRect = validateRect(kRect)
                trustworthy = True
                dst2.Rectangle(trustedRect, cv.Scalar.Green, 5)
            End If
        End If
    End Sub
End Class









Public Class Depth_RGBShadow : Inherits VB_Algorithm
    Public Sub New()
        desc = "Merge the BGR and Depth Shadow"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Depth_BGSubtract : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        labels = {"", "", "Latest task.noDepthMask", "BGSubtract output for the task.noDepthMask"}
        desc = "Create a mask for the missing depth across multiple frame"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.noDepthMask

        bgSub.Run(dst2)
        dst3 = bgSub.dst2
    End Sub
End Class









Public Class Depth_Averaging : Inherits VB_Algorithm
    Public avg As New Math_ImageAverage
    Public colorize As New Depth_Colorizer_CPP
    Public Sub New()
        labels(3) = "32-bit format depth data"
        desc = "Take the average depth at each pixel but eliminate any pixels that had zero depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        avg.Run(src)

        dst3 = avg.dst2
        colorize.Run(dst3)
        dst2 = colorize.dst2
    End Sub
End Class








Public Class Depth_MaxMask : Inherits VB_Algorithm
    Dim contour As New Contour_General
    Public Sub New()
        labels = {"", "", "Depth that is too far", "Contour of depth that is too far..."}
        desc = "Display the task.maxDepthMask and its contour containing depth that is greater than maxdepth (global setting)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src

        dst2.SetTo(cv.Scalar.White, task.maxDepthMask)
        contour.Run(task.maxDepthMask)
        dst3.SetTo(0)
        For Each c In contour.allContours
            Dim hull = cv.Cv2.ConvexHull(c, True).ToList
            vbDrawContour(dst3, hull, cv.Scalar.White, -1)
        Next
    End Sub
End Class







Public Class Depth_ForegroundOverTime : Inherits VB_Algorithm
    Dim options As New Options_ForeGround
    Dim fore As New Depth_Foreground
    Dim contours As New Contour_Largest
    Public Sub New()
        labels = {"", "", "Foreground objects", "Edges for the Foreground Objects"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        task.frameHistoryCount = 5
        desc = "Create a fused foreground mask over x number of frames (task.frameHistoryCount)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastFrames As New List(Of cv.Mat)
        options.RunVB()

        If task.optionsChanged Then lastFrames.Clear()

        fore.Run(src)
        lastFrames.Add(fore.dst3)
        dst2.SetTo(0)
        For Each m In lastFrames
            dst2 += m
        Next
        If lastFrames.Count >= task.frameHistoryCount Then lastFrames.RemoveAt(0)

        contours.Run(dst2)
        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each ctr In contours.allContours
            If ctr.Length >= options.minSizeContour Then
                vbDrawContour(dst2, ctr.ToList, cv.Scalar.White, -1)
                vbDrawContour(dst3, ctr.ToList, cv.Scalar.White)
            End If
        Next
    End Sub
End Class





Public Class Depth_ForegroundBlob : Inherits VB_Algorithm
    Dim options As New Options_ForeGround
    Public blobLocation As New List(Of cv.Point)
    Public maxIndex As Integer
    Public Sub New()
        labels(2) = "Mask for the largest foreground blob"
        desc = "Use InRange to define foreground and find the largest blob in the foreground"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        cv.Cv2.InRange(task.pcSplit(2), 0.01, options.maxForegroundDepthInMeters, dst2)
        dst3 = dst2.Clone

        ' find the largest blob and use that to define the foreground object.
        Dim blobSize As New List(Of Integer)
        blobLocation.Clear()

        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim nextByte = dst2.Get(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = dst2.FloodFill(New cv.Point(x, y), 0)
                    If count > 10 Then
                        blobSize.Add(count)
                        blobLocation.Add(New cv.Point(x, y))
                    End If
                End If
            Next
        Next
        If blobSize.Count > 0 Then
            Dim maxBlob As Integer
            maxIndex = -1
            For i = 0 To blobSize.Count - 1
                If maxBlob < blobSize(i) Then
                    maxBlob = blobSize(i)
                    maxIndex = i
                End If
            Next
            dst3.FloodFill(blobLocation(maxIndex), 250)
            cv.Cv2.InRange(dst3, 250, 250, dst2)
            dst2.SetTo(0, task.noDepthMask)
            labels(3) = "Mask of all depth pixels < " + Format(options.maxForegroundDepthInMeters, "0.0") + "m"
        End If
    End Sub
End Class








Public Class Depth_Foreground : Inherits VB_Algorithm
    Dim options As New Options_ForeGround
    Dim contours As New Contour_Largest
    Public Sub New()
        labels(2) = "Foreground objects"
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask for the objects in the foreground"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim dst1 = task.pcSplit(2).Threshold(options.maxForegroundDepthInMeters, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dst1.SetTo(0, task.noDepthMask)

        contours.Run(dst1)
        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each ctr In contours.allContours
            If ctr.Length >= options.minSizeContour Then
                vbDrawContour(dst2, ctr.ToList, cv.Scalar.White, -1)
                vbDrawContour(dst3, ctr.ToList, cv.Scalar.White)
            End If
        Next
    End Sub
End Class










Public Class Depth_Grid : Inherits VB_Algorithm
    Public Sub New()
        gOptions.GridSize.Value = 4
        labels = {"", "", "White regions below are likely depth edges where depth changes rapidly", "Depth 32f display"}
        desc = "Find boundaries in depth to separate featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst3 = task.pcSplit(2)
        dst2 = task.gridMask.Clone
        For Each roi In task.gridList
            Dim mm As mmData = vbMinMax(dst3(roi))
            If Math.Abs(mm.minVal - mm.maxVal) > 0.1 Then dst2(roi).SetTo(cv.Scalar.White)
        Next
    End Sub
End Class









Public Class Depth_InRange : Inherits VB_Algorithm
    Dim options As New Options_ForeGround
    Dim contours As New Contour_Largest
    Public classCount As Integer = 1
    Public Sub New()
        labels = {"", "", "Looks empty! But the values are there - 0 to classcount.  Run standaloneTest() to see the palette output for this", "Edges between the depth regions."}
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        dst3 = New cv.Mat(dst0.Size, cv.MatType.CV_8U)
        desc = "Create the selected number of depth ranges "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim regMats As New List(Of cv.Mat)
        ' regMats.Add(task.noDepthMask)
        For i = 0 To options.numberOfRegions - 1
            Dim upperBound = (i + 1) * options.depthPerRegion
            If i = options.numberOfRegions - 1 Then upperBound = 1000
            regMats.Add(task.pcSplit(2).InRange(i * options.depthPerRegion, upperBound))
            If i = 0 Then regMats(0).SetTo(0, task.noDepthMask)
        Next

        dst2 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst3.SetTo(0)
        classCount = 1
        For i = 0 To regMats.Count - 1
            contours.Run(regMats(i))
            For Each ctr In contours.allContours
                If ctr.Length >= options.minSizeContour Then
                    vbDrawContour(dst2, ctr.ToList, classCount, -1)
                    classCount += 1
                    vbDrawContour(dst3, ctr.ToList, cv.Scalar.White)
                End If
            Next
        Next

        dst0 = src.Clone
        dst0.SetTo(cv.Scalar.White, dst3)

        If standaloneTest() Then
            dst2 = vbPalette(dst2 * 255 / classCount)
        End If
        If task.heartBeat Then labels(2) = Format(classCount, "000") + " regions were found"
    End Sub
End Class












Public Class Depth_Regions : Inherits VB_Algorithm
    Public classCount As Integer = 5
    Public Sub New()
        desc = "Separate the scene into a specified number of regions by depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = task.pcSplit(2).Threshold(gOptions.MaxDepth.Value, gOptions.MaxDepth.Value, cv.ThresholdTypes.Binary)
        dst0 = (task.pcSplit(2) / gOptions.MaxDepth.Value) * 255 / classCount
        dst0.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2.SetTo(0, task.noDepthMask)

        If standaloneTest() Then dst3 = vbPalette(dst2)
        labels(2) = CStr(classCount) + " regions defined in the depth data"
    End Sub
End Class








Public Class Depth_TierCount : Inherits VB_Algorithm
    Public valley As New HistValley_DepthOld
    Public classCount As Integer
    Public Sub New()
        labels = {"", "Histogram of the depth data with instantaneous valley lines", "", ""}
        desc = "Determine the 'K' value for the best number of clusters for the depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(src)
        dst2 = valley.dst2

        Static kValues As New List(Of Integer)
        kValues.Add(valley.valleyOrder.Count)

        classCount = CInt(kValues.Average)
        If kValues.Count > task.frameHistoryCount * 10 Then kValues.RemoveAt(0)

        setTrueText("'K' value = " + CStr(classCount) + " after averaging.  Instanteous value = " +
                    CStr(valley.valleyOrder.Count), 3)
        labels(2) = "There are " + CStr(classCount)
    End Sub
End Class








Public Class Depth_Colorizer_VB : Inherits VB_Algorithm
    Public Sub New()
        desc = "Colorize the depth based on the near and far colors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        If src.Size <> task.lowRes Then src = src.Resize(task.lowRes, cv.InterpolationFlags.Nearest)

        dst2 = New cv.Mat(task.lowRes, cv.MatType.CV_8UC3, 0)
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = src.Get(Of Single)(y, x)
                If pixel > 0 And pixel <= task.maxZmeters Then
                    Dim t = pixel / task.maxZmeters
                    Dim color = New cv.Vec3b(((1 - t) * nearColor(0) + t * farColor(0)),
                                             ((1 - t) * nearColor(1) + t * farColor(1)),
                                             ((1 - t) * nearColor(2) + t * farColor(2)))
                    dst2.Set(Of cv.Vec3b)(y, x, color)
                End If
            Next
        Next
    End Sub
End Class







Public Class Depth_PunchIncreasing : Inherits VB_Algorithm
    Public depth As New Depth_PunchDecreasing
    Public Sub New()
        depth.Increasing = True
        desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depth.Run(src)
        dst2 = depth.dst2
    End Sub
End Class








Public Class Depth_PunchDecreasing : Inherits VB_Algorithm
    Public Increasing As Boolean
    Dim fore As New Depth_Foreground
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold in millimeters", 0, 1000, 8)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fore.Run(src)
        dst1.SetTo(0)
        task.pcSplit(2).CopyTo(dst1, fore.dst2)

        Static lastDepth = dst1
        Static mmSlider = findSlider("Threshold in millimeters")
        Dim mmThreshold = mmSlider.Value / 1000
        If Increasing Then
            cv.Cv2.Subtract(dst1, lastDepth, dst2)
        Else
            cv.Cv2.Subtract(lastDepth, dst1, dst2)
        End If
        dst2 = dst2.Threshold(mmThreshold, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastDepth = dst1.Clone
    End Sub
End Class






Public Class Depth_PunchBlob : Inherits VB_Algorithm
    Dim depthDec As New Depth_PunchDecreasing
    Dim depthInc As New Depth_PunchDecreasing
    Dim contours As New Contour_General
    Public Sub New()
        desc = "Identify the punch with a rectangle around the largest blob"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depthInc.Run(src)
        dst2 = depthInc.dst2

        Dim mm As mmData = vbMinMax(dst2)
        dst2.ConvertTo(dst1, cv.MatType.CV_8U)
        contours.Run(dst1)
        dst3 = contours.dst3

        Static lastContoursCount As Integer
        Static punchCount As Integer
        Static showMessage As Integer
        If contours.contourlist.Count > 0 Then showMessage = 30

        If showMessage = 30 And lastContoursCount = 0 Then punchCount += 1
        lastContoursCount = contours.contourlist.Count
        labels(3) = CStr(punchCount) + " Punches Thrown"

        If showMessage > 0 Then
            setTrueText("Punched!!!", New cv.Point(10, 100), 3)
            showMessage -= 1
        End If

        Static showWarningInfo As Integer
        If contours.contourlist.Count > 3 Then showWarningInfo = 100

        If showWarningInfo Then
            showWarningInfo -= 1
            setTrueText("Too many contours!  Reduce the Max Depth.", New cv.Point(10, 130), 3)
        End If
    End Sub
End Class








Public Class Depth_PunchBlobNew : Inherits VB_Algorithm
    Dim depthDec As New Depth_PunchDecreasing
    Dim depthInc As New Depth_PunchDecreasing
    Dim contours As New Contour_General
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for punch", 0, 255, 250)
        desc = "Identify a punch using both depth and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold for punch")
        Dim threshold = thresholdSlider.value

        Static lastColor As cv.Mat = task.color.Clone

        dst2 = task.color.Clone
        dst2 -= lastColor
        dst3 = dst2.Threshold(0, New cv.Scalar(threshold, threshold, threshold), cv.ThresholdTypes.Binary).ConvertScaleAbs

        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        lastColor = task.color.Clone
    End Sub
End Class








Public Class Depth_Contour : Inherits VB_Algorithm
    Dim contour As New Contour_General
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(2) = "task.depthMask contour"
        desc = "Create and display the task.depthMask output as a contour."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        contour.Run(task.depthMask)

        dst2.SetTo(0)
        For Each tour In contour.contourlist
            vbDrawContour(dst2, tour.ToList, 255, -1)
        Next
    End Sub
End Class









Public Class Depth_Outline : Inherits VB_Algorithm
    Dim contour As New Contour_General
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Contour separating depth from no depth"
        desc = "Provide a line that separates depth from no depth throughout the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then src = task.depthMask
        contour.Run(src)

        dst2.SetTo(0)
        For Each tour In contour.contourlist
            vbDrawContour(dst2, tour.ToList, 255, task.lineWidth)
        Next

        If standaloneTest() Then
            If task.heartBeat Then dst3.SetTo(0)
            dst3 = dst3 Or dst2
        End If
    End Sub
End Class






Public Class Depth_StableAverage : Inherits VB_Algorithm
    Dim dAvg As New Depth_Averaging
    Dim extrema As New Depth_StableMinMax
    Public Sub New()
        findRadio("Use farthest distance").Checked = True
        desc = "Use Depth_StableMax to remove the artifacts from the Depth_Averaging"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static unchangedRadio = findRadio("Use unchanged depth input")
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        extrema.Run(src)

        If unchangedRadio.checked Then
            dst2 = extrema.dst2
            dst3 = extrema.dst3
        Else
            dAvg.Run(extrema.dst3)
            dst2 = dAvg.dst2
            dst3 = dAvg.dst3
        End If
    End Sub
End Class






Public Class Depth_MinMaxNone : Inherits VB_Algorithm
    Public options As New Options_MinMaxNone
    Public Sub New()
        desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim split() As cv.Mat
        If src.Type = cv.MatType.CV_32FC3 Then split = src.Split() Else split = task.pcSplit

        Static filtered As Integer
        If task.heartBeat Then
            dst3 = split(2)
            filtered = 0
        End If
        labels(2) = "Point cloud unchanged"
        If options.useMax Then
            labels(2) = "Point cloud maximum values at each pixel"
            cv.Cv2.Max(split(2), dst3, split(2))
        End If
        If options.useMin Then
            labels(2) = "Point cloud minimum values at each pixel"
            Dim saveMat = split(2).Clone
            cv.Cv2.Min(split(2), dst3, split(2))
            Dim mask = split(2).InRange(0, 0.1)
            saveMat.CopyTo(split(2), mask)
        End If
        cv.Cv2.Merge(split, dst2)
        dst3 = split(2)
        filtered += 1
        labels(2) += " after " + CStr(filtered) + " images"
    End Sub
End Class







Public Class Depth_StableMin : Inherits VB_Algorithm
    Public stableMin As cv.Mat
    Dim colorize As New Depth_Colorizer_CPP
    Public Sub New()
        gOptions.unFiltered.Checked = True
        labels = {"", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle."}
        desc = "To reduce z-Jitter, use the closest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        If task.heartBeat Then
            stableMin = src.Clone
            dst3.SetTo(0)
        ElseIf task.motionDetected Then
            src(task.motionRect).CopyTo(stableMin(task.motionRect))
            If src.Type <> stableMin.Type Then src.ConvertTo(src, stableMin.Type)
            stableMin.CopyTo(src, task.noDepthMask)
            cv.Cv2.Min(src, stableMin, stableMin)
        End If

        colorize.Run(stableMin)
        dst2 = colorize.dst2
    End Sub
End Class






Public Class Depth_StableMax : Inherits VB_Algorithm
    Public stableMax As cv.Mat
    Dim colorize As New Depth_Colorizer_CPP
    Public Sub New()
        gOptions.unFiltered.Checked = True
        labels = {"", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle."}
        desc = "To reduce z-Jitter, use the farthest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        If task.heartBeat Then
            stableMax = src.Clone
            dst3.SetTo(0)
        ElseIf task.motionDetected Then
            src(task.motionRect).CopyTo(stableMax(task.motionRect))
            If src.Type <> stableMax.Type Then src.ConvertTo(src, stableMax.Type)
            stableMax.CopyTo(src, task.noDepthMask)
            cv.Cv2.Min(src, stableMax, stableMax)
        End If

        colorize.Run(stableMax)
        dst2 = colorize.dst2
    End Sub
End Class







Public Class Depth_StableMinMax : Inherits VB_Algorithm
    Dim colorize As New Depth_Colorizer_CPP
    Public dMin As New Depth_StableMin
    Public dMax As New Depth_StableMax
    Dim options As New Options_MinMaxNone
    Public Sub New()
        gOptions.unFiltered.Checked = True
        labels(2) = "Depth map colorized"
        labels(3) = "32-bit StableDepth"
        desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)
        If task.optionsChanged Then dst3 = task.pcSplit(2)

        If options.useMax Then
            dMax.Run(src)
            dst3 = dMax.stableMax
            dst2 = dMax.dst2
        ElseIf options.useMin Then
            dMin.Run(src)
            dst3 = dMin.stableMin
            dst2 = dMin.dst2
        ElseIf options.useNone Then
            dst3 = task.pcSplit(2)
            dst2 = task.depthRGB
        End If
    End Sub
End Class









Public Class Depth_WorldXYZ_MT : Inherits VB_Algorithm
    Public depthUnitsMeters = False
    Public Sub New()
        labels(3) = "dst3 = pointcloud"
        desc = "Create OpenGL point cloud from depth data (slow)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        dst3 = New cv.Mat(src.Size(), cv.MatType.CV_32FC3, 0)
        If depthUnitsMeters = False Then src = (src * 0.001).ToMat
        Dim multX = task.pointCloud.Width / src.Width
        Dim multY = task.pointCloud.Height / src.Height
        Parallel.ForEach(task.gridList,
              Sub(roi)
                  Dim xy As New cv.Point3f
                  For y = roi.Y To roi.Y + roi.Height - 1
                      For x = roi.X To roi.X + roi.Width - 1
                          xy.X = x * multX
                          xy.Y = y * multY
                          xy.Z = src.Get(Of Single)(y, x)
                          If xy.Z <> 0 Then
                              Dim xyz = getWorldCoordinates(xy)
                              dst3.Set(Of cv.Point3f)(y, x, xyz)
                          End If
                      Next
                  Next
              End Sub)
        setTrueText("OpenGL data prepared.")
    End Sub
End Class










Public Class Depth_WorldXYZ : Inherits VB_Algorithm
    Public depthUnitsMeters = False
    Public Sub New()
        labels(3) = "dst3 = pointcloud"
        desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)
        If depthUnitsMeters = False Then src = (src * 0.001).ToMat
        dst2 = New cv.Mat(src.Size(), cv.MatType.CV_32FC3, 0)
        Dim xy As New cv.Point3f
        For xy.Y = 0 To dst2.Height - 1
            For xy.X = 0 To dst2.Width - 1
                xy.Z = src.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim xyz = getWorldCoordinates(xy)
                    dst2.Set(Of cv.Point3f)(xy.Y, xy.X, xyz)
                End If
            Next
        Next
        setTrueText("OpenGL data prepared and in dst2.", 3)
    End Sub
End Class








Public Class Depth_World : Inherits VB_Algorithm
    Dim template As New Math_Template
    Public Sub New()
        labels = {"", "", "Merged templates and depth32f - should be similar to upper right image", ""}
        desc = "Build the (approximate) point cloud using camera intrinsics - see CameraOakD.vb for comparable calculations"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If firstPass Then template.Run(empty) ' intrinsics arrive with the first buffers.

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        cv.Cv2.Multiply(template.dst2, src, dst0)
        dst0 *= 1 / task.calibData.fx

        cv.Cv2.Multiply(template.dst3, src, dst1)
        dst1 *= 1 / task.calibData.fy

        cv.Cv2.Merge({dst0, dst1, src}, dst2)
        If standaloneTest() Then
            Static colorizer As New Depth_Colorizer_CPP
            colorizer.Run(dst2)
            dst2 = colorizer.dst2
        End If
    End Sub
End Class









Public Class Depth_TiersZ : Inherits VB_Algorithm
    Public classCount As Integer
    Dim options As New Options_Contours
    Public Sub New()
        vbAddAdvice(traceName + ": gOptions 'Max Depth (meters)' and local options for cm's per tier.")
        desc = "Create a reduced image of the depth data to define tiers of similar values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static cmSlider = findSlider("cm's per tier")
        Dim cmTier = cmSlider.value

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        dst1 = (src * 100 / cmTier).toMat
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        classCount = task.maxZmeters * 100 / cmTier + 1

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(2) = $"{classCount} regions found."
    End Sub
End Class