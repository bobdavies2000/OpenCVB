Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_Flatland : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Region Count", 1, 250, 10)
        End If
        label2 = "Grayscale version"
        task.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        dst1 = task.RGBDepth / reductionFactor
        dst1 *= reductionFactor
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Depth_FirstLastDistance : Inherits VBparent
    Public Sub New()
        task.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim mask = task.depth32f.Threshold(1, 20000, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        cv.Cv2.MinMaxLoc(task.depth32f, minVal, maxVal, minLoc, maxLoc, mask)
        task.RGBDepth.CopyTo(dst1)
        task.RGBDepth.CopyTo(dst2)
        label1 = "Min Depth " + CStr(minVal) + " mm"
        dst1.Circle(minLoc, 10, cv.Scalar.White, -1, task.lineType)
        label2 = "Max Depth " + CStr(maxVal) + " mm"
        dst2.Circle(maxLoc, 10, cv.Scalar.White, -1, task.lineType)
    End Sub
End Class





Public Class Depth_HolesRect : Inherits VBparent
    Dim shadow As New Depth_Holes
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "shadowRect Min Size", 1, 20000, 2000)
        End If
        task.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run(src As cv.Mat)
        Static sizeSlider = findSlider("shadowRect Min Size")
        shadow.Run(src)

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        Dim minSize = sizeSlider.value
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim size = minRect.Size.Width * minRect.Size.Height
            If size > minSize Then
                Dim nextColor = New cv.Scalar(task.vecColors(i Mod 255).Item0, task.vecColors(i Mod 255).Item1, task.vecColors(i Mod 255).Item2)
                drawRotatedRectangle(minRect, dst1, nextColor)
                If contours(i).Length >= 5 Then
                    minEllipse(i) = cv.Cv2.FitEllipse(contours(i))
                End If
            End If
        Next
        cv.Cv2.AddWeighted(dst1, 0.5, task.RGBDepth, 0.5, 0, dst1)
    End Sub
End Class







Public Class Depth_FlatData : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FlatData Region Count", 1, 250, 200)
        End If
        label1 = "Reduced resolution RGBDepth"
        task.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim gray As New cv.Mat
        Dim gray8u As New cv.Mat

        Dim depthMask As cv.Mat = task.depthMask
        gray = task.depth32f.Normalize(0, 255, cv.NormTypes.MinMax, -1, depthMask)
        gray.ConvertTo(gray8u, cv.MatType.CV_8U)

        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        gray8u = gray8u / reductionFactor
        gray8u *= reductionFactor

        dst1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class








Module DepthXYZ_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Open(ppx As Single, ppy As Single, fx As Single, fy As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_XYZ_OpenMP_Close(DepthXYZPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Run(DepthXYZPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
End Module







Public Class Depth_MeanStdev_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 40

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MeanStdev Max Depth Range", 1, 20000, 3500)
            sliders.setupTrackBar(1, "MeanStdev Frame Series", 1, 100, 5)
        End If
        task.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)

        Dim maxDepth = sliders.trackbar(0).Value
        Dim meanCount = sliders.trackbar(1).Value

        Static lastMeanCount As Integer
        If grid.roiList.Count <> meanSeries.Rows Or meanCount <> lastMeanCount Then
            meanSeries = New cv.Mat(grid.roiList.Count, meanCount, cv.MatType.CV_32F, 0)
            lastMeanCount = meanCount
        End If

        Dim mask As New cv.Mat, tmp16 As New cv.Mat
        cv.Cv2.InRange(task.depth32f, 1, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        Dim outOfRangeMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, outOfRangeMask)

        cv.Cv2.MinMaxLoc(task.depth32f, minVal, maxVal, minLoc, maxLoc, mask)

        Dim meanIndex = task.frameCount Mod meanCount
        Dim meanValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Dim stdValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(task.depth32f(roi), mean, stdev, mask(roi))
            meanSeries.Set(Of Single)(i, meanIndex, mean)
            If task.frameCount >= meanCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues.Set(Of Single)(i, 0, mean)
                stdValues.Set(Of Single)(i, 0, stdev)
            End If
        End Sub)

        If task.frameCount >= meanCount Then
            Dim minStdVal As Double, maxStdVal As Double
            Dim meanmask = meanValues.Threshold(1, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minLoc, maxLoc, meanmask)
            Dim stdMask = stdValues.Threshold(0.001, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minLoc, maxLoc, stdMask)

            Parallel.For(0, grid.roiList.Count,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                dst2(roi).SetTo(255 * (stdValues.Get(Of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                dst2(roi).SetTo(0, outOfRangeMask(roi))

                dst1(roi).SetTo(255 * (meanValues.Get(Of Single)(i, 0) - minVal) / (maxVal - minVal))
                dst1(roi).SetTo(0, outOfRangeMask(roi))
            End Sub)
            cv.Cv2.BitwiseOr(dst2, grid.gridMask, dst2)
            label2 = "Stdev for each ROI (normalized): Min " + Format(minStdVal, "#0.0") + " Max " + Format(maxStdVal, "#0.0")
        End If
        label1 = "Mean for each ROI (normalized): Min " + Format(minVal, "#0.0") + " Max " + Format(maxVal, "#0.0")
    End Sub
End Class






Public Class Depth_MeanStdevPlot : Inherits VBparent
    Dim plot1 As New Plot_OverTime
    Dim plot2 As New Plot_OverTime
    Public Sub New()
        plot1.dst1 = dst1
        plot1.maxScale = 2000
        plot1.plotCount = 1

        plot2.dst1 = dst2
        plot2.maxScale = 1000
        plot2.plotCount = 1

        task.desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Sub Run(src As cv.Mat)

        Dim mean As Single = 0, stdev As Single = 0
        Dim depthMask As cv.Mat = task.depthMask
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev, depthMask)

        If mean > plot1.maxScale Then plot1.maxScale = mean + 1000 - (mean + 1000) Mod 1000
        If stdev > plot2.maxScale Then plot2.maxScale = stdev + 1000 - (stdev + 1000) Mod 1000

        plot1.plotData = New cv.Scalar(mean, 0, 0)
        plot1.Run(src)
        dst1 = plot1.dst1

        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run(src)
        dst2 = plot2.dst1

        label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
End Class




Public Class Depth_Uncertainty : Inherits VBparent
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Uncertainty threshold", 1, 255, 100)
        End If
        label2 = "Mask of areas with unstable depth"
        task.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(src As cv.Mat)
        retina.Run(task.RGBDepth)
        dst1 = retina.dst1
        dst2 = retina.dst2.Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette : Inherits VBparent
    Dim customColorMap As New cv.Mat
    Public Sub New()

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        task.desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim depthNorm = (task.depth32f * 255 / (task.maxDepth - task.minDepth)).ToMat ' do the normalize manually to use the min and max Depth (more stable)
        depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
        dst1 = Palette_Custom_Apply(depthNorm.CvtColor(cv.ColorConversionCodes.GRAY2BGR), customColorMap).SetTo(0, task.noDepthMask)
    End Sub
End Class




Module Depth_Colorizer_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, histSize As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, histSize As Integer) As IntPtr
    End Function
End Module




Public Class Depth_Colorizer_CPP : Inherits VBparent
    Dim dcPtr As IntPtr
    Public Sub New()
        dcPtr = Depth_Colorizer_Open()
        task.desc = "Display Depth image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then
            If standalone Or task.intermediateReview = caller Then src = task.depth32f Else dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        End If
        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        Depth_Colorizer_Close(dcPtr)
    End Sub
End Class







Public Class Depth_ColorizerFastFade_CPP : Inherits VBparent
    Dim dcPtr As IntPtr
    Public Sub New()
        dcPtr = Depth_Colorizer2_Open()
        label2 = "No depth mask from Depth_InRange"
        task.desc = "Display depth data with InRange.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(src As cv.Mat)

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        dst2 = task.noDepthMask

        Dim depthData(input.Total * input.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(input.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer2_Run(dcPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, task.maxDepth)
        handleSrc.Free()

        If imagePtr <> 0 Then
            dst1 = New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_8UC3, imagePtr)
            If standalone Or task.intermediateReview = caller Then dst1.SetTo(0, dst2)
        End If
    End Sub
    Public Sub Close()
        Depth_Colorizer2_Close(dcPtr)
    End Sub
End Class




' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_ColorizerVB : Inherits VBparent
    Public Sub New()
        task.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(src As cv.Mat)
        src = task.depth32f
        Dim nearColor = New Byte() {0, 255, 255}
        Dim farColor = New Byte() {255, 0, 0}

        Dim histogram(256 * 256 - 1) As Integer
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = Math.Truncate(src.Get(Of Single)(y, x))
                If pixel Then histogram(pixel) += 1
            Next
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1) + 1
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) = (histogram(i) << 8) / histogram(256 * 256 - 1)
        Next

        Dim stride = src.Width * 3
        Dim rgbdata(stride * src.Height) As Byte
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = Math.Truncate(src.Get(Of Single)(y, x))
                If pixel Then
                    Dim t = histogram(pixel)
                    rgbdata(x * 3 + 0 + y * stride) = ((256 - t) * nearColor(0) + t * farColor(0)) >> 8
                    rgbdata(x * 3 + 1 + y * stride) = ((256 - t) * nearColor(1) + t * farColor(1)) >> 8
                    rgbdata(x * 3 + 2 + y * stride) = ((256 - t) * nearColor(2) + t * farColor(2)) >> 8
                End If
            Next
        Next
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbdata)
    End Sub
End Class





Public Class Depth_ColorizerVB_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Depth", 0, 1000, 0)
            sliders.setupTrackBar(1, "Max Depth", 1001, 10000, 4000)
        End If
        task.desc = "Colorize depth manually with multi-threading."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim minDepth = sliders.trackbar(0).Value
        Dim maxDepth = sliders.trackbar(1).Value
        Dim histSize = maxDepth - minDepth

        Dim dimensions() = New Integer() {histSize}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minDepth, maxDepth)}

        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, hist, 1, dimensions, ranges)

        Dim histogram(histSize - 1) As Single
        Marshal.Copy(hist.Data, histogram, 0, histogram.Length)
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1)
        Next

        Dim maxHist = histogram(histSize - 1)
        If maxHist > 0 Then
            Parallel.ForEach(grid.roiList,
           Sub(roi)
               Dim depth = src(roi)
               Dim rgbdata(src.Total) As cv.Vec3b
               Dim rgbIndex As Integer
               For y = 0 To depth.Rows - 1
                   For x = 0 To depth.Cols - 1
                       Dim pixel = Math.Truncate(depth.Get(Of Single)(y, x))
                       If pixel > 0 And pixel < histSize Then
                           Dim t = histogram(pixel) / maxHist
                           rgbdata(rgbIndex) = New cv.Vec3b(((1 - t) * nearColor(0) + t * farColor(0)) * 255,
                                                            ((1 - t) * nearColor(1) + t * farColor(1)) * 255,
                                                            ((1 - t) * nearColor(2) + t * farColor(2)) * 255)
                       End If
                       rgbIndex += 1
                   Next
               Next
               dst1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
           End Sub)

        End If
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class Depth_Colorizer_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public Sub New()
        task.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim range = task.maxDepth - task.minDepth
        Parallel.ForEach(grid.roiList,
         Sub(roi)
             Dim depth = src(roi)
             Dim stride = depth.Width * 3
             Dim rgbdata(stride * depth.Height) As Byte
             For y = 0 To depth.Rows - 1
                 For x = 0 To depth.Cols - 1
                     Dim pixel = depth.Get(Of Single)(y, x)
                     If pixel > task.minDepth And pixel <= task.maxDepth Then
                         Dim t = (pixel - task.minDepth) / range
                         rgbdata(x * 3 + 0 + y * stride) = ((1 - t) * nearColor(0) + t * farColor(0)) * 255
                         rgbdata(x * 3 + 1 + y * stride) = ((1 - t) * nearColor(1) + t * farColor(1)) * 255
                         rgbdata(x * 3 + 2 + y * stride) = ((1 - t) * nearColor(2) + t * farColor(2)) * 255
                     End If
                 Next
             Next
             dst1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
         End Sub)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class Depth_LocalMinMax_MT : Inherits VBparent
    Public grid As New Thread_Grid
    Public minPoint(0) As cv.Point2f
    Public maxPoint(0) As cv.Point2f
    Public Sub New()
        label1 = "Red is min distance, blue is max distance"
        task.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        Dim mask = task.depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If standalone Or task.intermediateReview = caller Then
            src.CopyTo(dst1)
            dst1.SetTo(cv.Scalar.White, grid.gridMask)
        End If

        If minPoint.Length <> grid.roiList.Count Then
            ReDim minPoint(grid.roiList.Count - 1)
            ReDim maxPoint(grid.roiList.Count - 1)
        End If
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            cv.Cv2.MinMaxLoc(task.depth32f(roi), minVal, maxVal, minLoc, maxLoc, mask(roi))
            If minLoc.X < 0 Or minLoc.Y < 0 Then minLoc = New cv.Point2f(0, 0)
            minPoint(i) = New cv.Point(minLoc.X + roi.X, minLoc.Y + roi.Y)
            maxPoint(i) = New cv.Point(maxLoc.X + roi.X, maxLoc.Y + roi.Y)

            cv.Cv2.Circle(dst1(roi), minLoc, 5, cv.Scalar.Red, -1, task.lineType)
            cv.Cv2.Circle(dst1(roi), maxLoc, 5, cv.Scalar.Blue, -1, task.lineType)
        End Sub)
    End Sub
End Class





Public Class Depth_LocalMinMax_Kalman_MT : Inherits VBparent
    Dim kalman As New Kalman_Basics
    Public grid As New Thread_Grid
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 128
        findSlider("ThreadGrid Height").Value = 90
        grid.Run(Nothing)

        ReDim kalman.kInput(grid.roiList.Count * 4 - 1)

        label1 = "Red is min distance, blue is max distance"
        task.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        If grid.roiList.Count * 4 <> kalman.kInput.Length Then
            If kalman IsNot Nothing Then kalman.Dispose()
            kalman = New Kalman_Basics()
            ReDim kalman.kInput(grid.roiList.Count * 4 - 1)
        End If

        dst1 = src.Clone()
        dst1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim depth32f As cv.Mat = task.depth32f
        Dim depthmask As cv.Mat = task.depthMask

        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minLoc As cv.Point, maxLoc As cv.Point
            cv.Cv2.MinMaxLoc(depth32f(roi), minVal, maxVal, minLoc, maxLoc, depthmask(roi))
            If minLoc.X < 0 Or minLoc.Y < 0 Then minLoc = New cv.Point2f(0, 0)
            kalman.kInput(i * 4) = minLoc.X
            kalman.kInput(i * 4 + 1) = minLoc.Y
            kalman.kInput(i * 4 + 2) = maxLoc.X
            kalman.kInput(i * 4 + 3) = maxLoc.Y
        End Sub)

        kalman.Run(src)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        Dim radius = 5
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim ptmin = New cv.Point2f(kalman.kOutput(i * 4) + roi.X, kalman.kOutput(i * 4 + 1) + roi.Y)
            Dim ptmax = New cv.Point2f(kalman.kOutput(i * 4 + 2) + roi.X, kalman.kOutput(i * 4 + 3) + roi.Y)
            ptmin = validatePoint2f(ptmin)
            ptmax = validatePoint2f(ptmax)
            subdiv.Insert(ptmin)
            cv.Cv2.Circle(dst1, ptmin, radius, cv.Scalar.Red, -1, task.lineType)
            cv.Cv2.Circle(dst1, ptmax, radius, cv.Scalar.Blue, -1, task.lineType)
        Next
        paint_voronoi(task.scalarColors, dst2, subdiv)
    End Sub
End Class





Public Class Depth_ColorMap : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Depth ColorMap Alpha X100", 1, 100, 5)
            sliders.setupTrackBar(1, "Depth ColorMap Beta", 1, 100, 3)
        End If
        task.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim alpha = sliders.trackbar(0).Value / 100
        Dim beta = sliders.trackbar(1).Value
        cv.Cv2.ConvertScaleAbs(task.depth32f, src, alpha, beta)
        src += 1
        task.palette.Run(src)
        dst1 = task.palette.dst1
        dst1.SetTo(0, task.noDepthMask)
        dst2 = task.palette.dst2
    End Sub
End Class






Public Class Depth_NotMissing : Inherits VBparent
    Public mog As New BGSubtract_Basics_CPP
    Public Sub New()
        label2 = "Stable (non-zero) Depth"
        task.desc = "Collect X frames, compute stable depth using the RGB and Depth image."
    End Sub
    Public Sub Run(src As cv.Mat)
        If standalone Or task.intermediateReview = caller Then src = task.RGBDepth
        mog.Run(src)
        dst1 = mog.dst1
        cv.Cv2.BitwiseNot(mog.dst1, dst2)
        label1 = "Unstable Depth" + " using " + mog.radio.check(mog.currMethod).Text + " method"
        Dim zeroDepth = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(1)
        dst2.SetTo(0, zeroDepth)
    End Sub
End Class








Public Class Depth_Median : Inherits VBparent
    Dim median As New Math_Median_CDF
    Public Sub New()
        median.rangeMax = 10000
        median.rangeMin = 1 ' ignore depth of zero as it is not known.
        task.desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub Run(src As cv.Mat)
        median.Run(task.depth32f)

        Dim mask As cv.Mat
        mask = task.depth32f.LessThan(median.medianVal)
        task.RGBDepth.CopyTo(dst1, mask)

        dst1.SetTo(0, task.noDepthMask)

        label1 = "Median Depth < " + Format(median.medianVal, "#0.0")

        cv.Cv2.BitwiseNot(mask, mask)
        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst2, mask)
        dst2.SetTo(0, task.noDepthMask)
        label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
End Class






Public Class Depth_SmoothingMat : Inherits VBparent
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold in millimeters", 1, 1000, 10)
        End If
        label2 = "Depth pixels after smoothing"
        task.desc = "Use depth rate of change to smooth the depth values beyond close range"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold in millimeters")
        Static lastDepth = task.depth32f

        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim rect = If(task.drawRect.Width <> 0, task.drawRect, New cv.Rect(0, 0, src.Width, src.Height))

        cv.Cv2.Subtract(lastDepth, task.depth32f, dst1)

        Dim mmThreshold = CSng(thresholdSlider.Value)
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.TozeroInv).Threshold(-mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(task.depth32f, dst1, dst2)
        lastDepth = task.depth32f

        label1 = "Smoothing Mat: range from " + CStr(task.minDepth) + " to +" + CStr(task.maxDepth)
    End Sub
End Class





Public Class Depth_Smoothing : Inherits VBparent
    Dim smooth As New Depth_SmoothingMat
    Dim reduction As New Reduction_Basics
    Public reducedDepth As New cv.Mat
    Public mats As New Mat_4to1
    Public colorize As New Depth_ColorMap
    Public Sub New()
        findRadio("Use bitwise reduction").Checked = True
        label2 = "Mask of depth that is smooth"
        task.desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work"
    End Sub
    Public Sub Run(src As cv.Mat)
        smooth.Run(task.depth32f)
        Dim input = smooth.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        input.ConvertTo(mats.mat(0), cv.MatType.CV_8UC1)
        Dim tmp As New cv.Mat
        cv.Cv2.Add(smooth.dst2, smooth.dst1, tmp)
        mats.mat(1) = tmp.InRange(0, 255)

        reduction.Run(task.depth32f)
        reduction.dst1.ConvertTo(reducedDepth, cv.MatType.CV_32F)
        colorize.Run(reducedDepth)
        dst1 = colorize.dst1
        mats.Run(Nothing)
        dst2 = mats.dst1
        label1 = smooth.label1
    End Sub
End Class










Public Class Depth_Edges : Inherits VBparent
    Dim edges As New Edges_Laplacian
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold for depth disparity", 0, 255, 200)
        End If
        task.desc = "Find edges in depth data"
    End Sub
    Public Sub Run(src As cv.Mat)
        edges.Run(src)
        dst1 = edges.dst2
        dst2 = edges.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_HolesOverTime : Inherits VBparent
    Dim recentImages As New List(Of cv.Mat)
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of images to retain", 0, 30, 3)
        End If
        label2 = "Latest hole mask"
        task.desc = "Integrate memory holes over time to identify unstable depth"
    End Sub
    Public Sub Run(src As cv.Mat)
        recentImages.Add(task.noDepthMask.Clone) ' To see the value of clone, remove it temporarily.  Only the most recent depth holes are added in.

        dst2 = task.noDepthMask
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For Each img In recentImages
            cv.Cv2.BitwiseOr(dst1, img, dst1)
        Next
        label1 = "Depth holes integrated over the past " + CStr(recentImages.Count) + " images"
        If recentImages.Count >= sliders.trackbar(0).Value Then recentImages.RemoveAt(0)
    End Sub
End Class








Public Class Depth_Holes : Inherits VBparent
    Public holeMask As New cv.Mat
    Dim element As New cv.Mat
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Amount of dilation of borderMask", 1, 10, 1)
            sliders.setupTrackBar(1, "Amount of dilation of holeMask", 0, 10, 0)
        End If
        label2 = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        task.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run(src As cv.Mat)
        holeMask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        holeMask = holeMask.Dilate(element, Nothing, sliders.trackbar(1).Value)
        dst1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst2 = holeMask.Dilate(element, Nothing, sliders.trackbar(0).Value)
        cv.Cv2.BitwiseXor(dst2, holeMask, dst2)
        If standalone Or task.intermediateReview = caller Then task.RGBDepth.CopyTo(dst2, dst2)
    End Sub
End Class










Public Class Depth_WorldXYZ : Inherits VBparent
    Public depthUnitsMeters = False
    Public Sub New()
        label2 = "dst2 = pointcloud"
        task.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f
        If depthUnitsMeters = False Then input = (input * 0.001).ToMat
        Dim xy As New cv.Point3f
        dst2 = New cv.Mat(task.pointCloud.Size(), cv.MatType.CV_32FC3, 0)
        For xy.Y = 0 To dst2.Height - 1
            For xy.X = 0 To dst2.Width - 1
                xy.Z = input.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim xyz = getWorldCoordinates(xy)
                    dst2.Set(Of cv.Point3f)(xy.Y, xy.X, xyz)
                End If
            Next
        Next
        If standalone Or task.intermediateReview = caller Then task.trueText("OpenGL data prepared.")
    End Sub
End Class






Public Class Depth_WorldXYZ_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public depthUnitsMeters = False
    Public Sub New()
        label2 = "dst2 = pointcloud"
        task.desc = "Create OpenGL point cloud from depth data (slow)"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.depth32f
        grid.Run(Nothing)

        dst2 = New cv.Mat(task.pointCloud.Size(), cv.MatType.CV_32FC3, 0)
        If depthUnitsMeters = False Then src = (src * 0.001).ToMat
        Dim multX = task.pointCloud.Width / src.Width
        Dim multY = task.pointCloud.Height / src.Height
        Parallel.ForEach(grid.roiList,
              Sub(roi)
                  Dim xy As New cv.Point3f
                  For y = roi.Y To roi.Y + roi.Height - 1
                      For x = roi.X To roi.X + roi.Width - 1
                          xy.X = x * multX
                          xy.Y = y * multY
                          xy.Z = src.Get(Of Single)(y, x)
                          If xy.Z <> 0 Then
                              Dim xyz = getWorldCoordinates(xy)
                              dst2.Set(Of cv.Point3f)(y, x, xyz)
                          End If
                      Next
                  Next
              End Sub)
        If standalone Or task.intermediateReview = caller Then task.trueText("OpenGL data prepared.")
    End Sub
End Class






Public Class Depth_Foreground : Inherits VBparent
    Public blobLocation As New List(Of cv.Point)
    Public maxIndex As Integer
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Max Range for foreground depth in mm's", 200, 2000, 1200)
            sliders.setupTrackBar(1, "Number of frames to fuse", 1, 20, 10)
        End If

        label1 = "Mask for the largest foreground blob"
        task.desc = "Use InRange to define foreground and find the largest blob in the foreground"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static depthSlider = findSlider("Max Range for foreground depth in mm's")

        cv.Cv2.InRange(task.depth32f, 0, depthSlider.value, dst1)
        dst2 = dst1.Clone

        ' find the largest blob and use that define that to be the foreground object.
        Dim blobSize As New List(Of Integer)
        blobLocation.Clear()

        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim nextByte = dst1.Get(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = dst1.FloodFill(New cv.Point(x, y), 0)
                    If count > 10 Then
                        blobSize.Add(count)
                        blobLocation.Add(New cv.Point(x, y))
                    End If
                End If
            Next
        Next
        Dim maxBlob As Integer
        maxIndex = -1
        For i = 0 To blobSize.Count - 1
            If maxBlob < blobSize.Item(i) Then
                maxBlob = blobSize.Item(i)
                maxIndex = i
            End If
        Next
        dst2.FloodFill(blobLocation(maxIndex), 250)
        cv.Cv2.InRange(dst2, 250, 250, dst1)
        dst1.SetTo(0, task.noDepthMask)
        label2 = "Mask of all depth pixels < " + Format(depthSlider.value / 1000, "0.0") + "m"
    End Sub
End Class






Public Class Depth_ForegroundOverTime : Inherits VBparent
    Dim fore As New Depth_Foreground
    Public Sub New()
        label1 = "Pixels that are consistently present"
        label2 = "Latest foreground frame"
        task.desc = "Create a fused foreground mask over x number of frames"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static countSlider = findSlider("Number of frames to fuse")
        Static lastFrames As New List(Of cv.Mat)
        Static saveCount As Integer
        Static sumFrame As New cv.Mat

        If saveCount <> countSlider.value Then
            lastFrames.Clear()
            saveCount = countSlider.value
            sumFrame = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        End If

        fore.Run(src)
        lastFrames.Add((fore.dst1 * 1 / 255).ToMat)
        cv.Cv2.Add(sumFrame, lastFrames(lastFrames.Count - 1), sumFrame)
        If lastFrames.Count >= saveCount Then
            sumFrame = sumFrame.Subtract(lastFrames(0)).ToMat
            lastFrames.RemoveAt(0)
        End If
        dst1 = sumFrame.Threshold(saveCount - 2, 255, cv.ThresholdTypes.Binary)
        dst2 = fore.dst1
    End Sub
End Class






Public Class Depth_InRange : Inherits VBparent
    Public depthMask As New cv.Mat
    Public noDepthMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public Sub New()
        label1 = "Depth values that are in-range"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(src As cv.Mat)
        dst1 = task.depth32f
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Depth_LowQualityMask : Inherits VBparent
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        findRadio("Dilate/Erode shape: Ellipse").Checked = True

        label2 = "Dilated zero depth - reduces flyout particles"
        task.desc = "Monitor motion in the mask where depth is zero"
    End Sub
    Public Sub Run(src As cv.Mat)
        dst1 = task.noDepthMask
        dilate.Run(dst1)
        dst2 = dilate.dst1
    End Sub
End Class









Public Class Depth_PunchDecreasing : Inherits VBparent
    Public Increasing As Boolean
    Dim fore As New Depth_Foreground
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold in millimeters", 0, 1000, 8)
        End If

        task.desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub Run(src As cv.Mat)
        fore.Run(src)
        Dim depth32f As New cv.Mat
        task.depth32f.CopyTo(depth32f, fore.dst1)

        Static lastDepth = depth32f
        Static mmSlider = findSlider("Threshold in millimeters")
        Dim mmThreshold = mmSlider.Value
        If Increasing Then
            cv.Cv2.Subtract(depth32f, lastDepth, dst1)
        Else
            cv.Cv2.Subtract(lastDepth, depth32f, dst1)
        End If
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastDepth = depth32f.Clone
    End Sub
End Class





Public Class Depth_PunchIncreasing : Inherits VBparent
    Public depth As New Depth_PunchDecreasing
    Public Sub New()
        depth.Increasing = True
        task.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run(src As cv.Mat)
        depth.Run(src)
        dst1 = depth.dst1
    End Sub
End Class






Public Class Depth_PunchBlob : Inherits VBparent
    Dim depthDec As New Depth_PunchDecreasing
    Dim depthInc As New Depth_PunchDecreasing
    Dim contours As New Contours_Basics
    Public Sub New()
        findSlider("Contour minimum area").Value = 5000
        task.desc = "Identify the punch with a rectangle around the largest blob"
    End Sub
    Public Sub Run(src As cv.Mat)
        depthInc.Run(src)
        dst1 = depthInc.dst1

        contours.Run(dst1)
        dst2 = contours.dst2

        Static lastContoursCount As Integer
        Static punchCount As Integer
        Static showMessage As Integer
        If contours.contourlist.Count > 0 Then showMessage = 30

        If showMessage = 30 And lastContoursCount = 0 Then punchCount += 1
        lastContoursCount = contours.contourlist.Count
        label2 = CStr(punchCount) + " Punches Thrown"

        If showMessage > 0 Then
            task.trueText("Punched!!!", 10, 100, 3)
            showMessage -= 1
        End If

        Static showWarningInfo As Integer
        If contours.contourlist.Count > 3 Then showWarningInfo = 100

        If showWarningInfo Then
            showWarningInfo -= 1
            task.trueText("Too many contours!  Reduce the Max Depth.", 10, 130, 3)
        End If
    End Sub
End Class









' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_PointCloud_IMU : Inherits VBparent
    Public Mask As New cv.Mat
    Public imu As New IMU_GVector
    Public gMatrix(,) As Single
    Public Sub New()
        task.desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        Static angleYslider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud.Clone

        imu.Run(src)
        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        '[cos(a) -sin(a)    0]
        '[sin(a)  cos(a)    0]
        '[0       0         1] rotate the point cloud around
        '  the x-axis.
        If xCheckbox.Checked Then
            cz = Math.Cos(task.angleZ)
            sz = Math.Sin(task.angleZ)
        End If

        '[1       0         0      ] rotate the point cloud around the z-axis.
        '[0       cos(a)    -sin(a)]
        '[0       sin(a)    cos(a) ]
        If zCheckbox.Checked Then
            cx = Math.Cos(task.angleX)
            sx = Math.Sin(task.angleX)
        End If

        '[cx -sx    0]  [1  0   0 ] 
        '[sx  cx    0]  [0  cz -sz]
        '[0   0     1]  [0  sz  cz]
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

        Dim angleY = angleYslider.value
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(angleY * cv.Cv2.PI / 180)
        sy = Math.Sin(angleY * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        gMatrix = gM
        If xCheckbox.Checked Or zCheckbox.Checked Or angleY <> 0 Then
            Dim gMat = New cv.Mat(3, 3, cv.MatType.CV_32F, gMatrix)
            Dim gInput = input.Reshape(1, input.Rows * input.Cols)
            Dim gOutput = (gInput * gMat).ToMat
            dst1 = gOutput.Reshape(3, input.Rows)
            label1 = "dst1 = pointcloud after rotation"
        Else
            dst1 = input.Clone
            label1 = "dst1 = pointcloud without rotation"
        End If

        task.pixelsPerMeter = dst1.Width / task.maxZ
    End Sub
End Class






Public Class Depth_SmoothAverage : Inherits VBparent
    Dim dMin As New Depth_SmoothMin
    Dim dMax As New Depth_SmoothMax
    Dim colorize As New Depth_ColorizerFastFade_CPP
    Public Sub New()
        label1 = "InRange average depth (low quality depth removed)"
        label2 = "32-bit format average stable depth"
        task.desc = "To reduce z-Jitter, use the average depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run(src As cv.Mat)
        dMax.Run(task.depth32f)
        dst1 = dMax.dMin.dst1

        cv.Cv2.AddWeighted(dMax.dMin.stableMin, 0.5, dMax.stableMax, 0.5, 0, dst2)
    End Sub
End Class







Public Class Depth_SmoothMin : Inherits VBparent
    Public stableMin As cv.Mat
    Public motion As New Motion_Basics
    Dim colorize As New Depth_ColorizerFastFade_CPP
    Public Sub New()
        label1 = "InRange depth with low quality depth removed."
        label2 = "Motion in the RGB image. Depth updated in rectangle."
        task.desc = "To reduce z-Jitter, use the closest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.depth32f

        motion.Run(task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

        If motion.resetAll Or stableMin Is Nothing Then
            stableMin = src.Clone
        Else
            For Each rect In motion.intersect.enclosingRects
                If rect.Width And rect.Height Then src(rect).CopyTo(stableMin(rect))
                cv.Cv2.Min(src, stableMin, stableMin)
            Next
        End If

        If motion.intersect.inputRects.Count > 0 Then
            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In motion.intersect.inputRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            For Each rect In motion.intersect.enclosingRects
                dst2.Rectangle(rect, cv.Scalar.Red, 2)
            Next
        End If

        colorize.Run(stableMin)
        dst1 = colorize.dst1
    End Sub
End Class






Public Class Depth_SmoothMax : Inherits VBparent
    Public dMin As New Depth_SmoothMin
    Dim colorize As New Depth_ColorizerFastFade_CPP
    Public stableMax As cv.Mat
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use SmoothMin to find zero depth pixels"
        End If

        label1 = "InRange depth with low quality depth removed."
        label2 = "32-bit format StableMax"
        task.desc = "To reduce z-Jitter, use the farthest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static dMinCheck = findCheckBox("Use SmoothMin to find zero depth pixels")
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.depth32f

        dMin.Run(src)

        If dMin.motion.resetAll Or stableMax Is Nothing Then
            stableMax = src.Clone
        Else
            For Each rect In dMin.motion.intersect.enclosingRects
                If rect.Width And rect.Height Then src(rect).CopyTo(stableMax(rect))
                cv.Cv2.Max(src, stableMax, stableMax)
            Next

            If dMinCheck.checked Then
                Dim zeroMask As New cv.Mat
                dMin.stableMin.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertTo(zeroMask, cv.MatType.CV_8U)
                stableMax.SetTo(0, zeroMask)
            End If
        End If

        colorize.Run(stableMax)
        dst1 = colorize.dst1
        dst2 = stableMax
    End Sub
End Class









Public Class Depth_Averaging : Inherits VBparent
    Public avg As New Math_ImageAverage
    Public colorize As New Depth_Colorizer_CPP
    Public Sub New()
        label2 = "32-bit format depth data"
        task.desc = "Take the average depth at each pixel but eliminate any pixels that had zero depth."
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        avg.Run(src)

        dst2 = avg.dst1
        colorize.Run(dst2)
        dst1 = colorize.dst1
    End Sub
End Class







Public Class Depth_SmoothMinMax : Inherits VBparent
    Dim colorize As New Depth_ColorizerFastFade_CPP
    Public dMin As New Depth_SmoothMin
    Public dMax As New Depth_SmoothMax
    Public resetAll As Boolean
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(1).Checked = True
        End If

        label1 = "Depth map colorized"
        label2 = "32-bit StableDepth"
        task.desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.depth32f

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(caller + " Radio Options")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        Static saveRadioVal = -1
        If radioVal <> saveRadioVal Then
            saveRadioVal = radioVal
            dst2 = task.depth32f
            resetAll = True
        Else
            Select Case saveRadioVal
                Case 0
                    dMax.Run(src)
                    dst2 = dMax.stableMax
                    dst1 = dMax.dst1
                    resetAll = dMax.dMin.motion.resetAll
                Case 1
                    dMin.Run(src)
                    dst2 = dMin.stableMin
                    dst1 = dMin.dst1
                    resetAll = dMin.motion.resetAll
                Case 2
                    dst2 = task.depth32f
                    colorize.Run(dst2)
                    dst1 = colorize.dst1
                    resetAll = True
            End Select
        End If
    End Sub
End Class








Public Class Depth_AveragingStable : Inherits VBparent
    Dim dAvg As New Depth_Averaging
    Dim extrema As New Depth_SmoothMinMax
    Public Sub New()
        findRadio("Use farthest distance").Checked = True
        task.desc = "Use Depth_SmoothMax to remove the artifacts from the Depth_Averaging"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static noAvgRadio = findRadio("Use unchanged depth input")
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        extrema.Run(src)

        If noAvgRadio.checked = False Then
            dAvg.Run(extrema.dst2)
            dst1 = dAvg.dst1
            dst2 = dAvg.dst2
        Else
            dst1 = extrema.dst1
            dst2 = extrema.dst2
        End If
    End Sub
End Class








Public Class Depth_Fusion : Inherits VBparent
    Dim dMax As New Depth_SmoothMax
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of frames to fuse", 1, 300, 5)
        End If

        task.desc = "Fuse the depth from the previous x frames."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static fuseSlider = findSlider("Number of frames to fuse")
        Dim fuseCount = fuseSlider.value
        Static saveFuseCount = fuseCount
        Static fuseFrames As New List(Of cv.Mat)

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f

        If saveFuseCount <> fuseCount Then
            fuseFrames = New List(Of cv.Mat)
            saveFuseCount = fuseCount
        End If

        fuseFrames.Add(input.Clone)
        If fuseFrames.Count > fuseCount Then fuseFrames.RemoveAt(0)

        dst1 = fuseFrames(0).Clone
        For i = 1 To fuseFrames.Count - 1
            cv.Cv2.Max(fuseFrames(i), dst1, dst1)
        Next
    End Sub
End Class









Public Class Depth_Dilate : Inherits VBparent
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        task.desc = "Dilate the depth data to fill holes."
    End Sub
    Public Sub Run(src As cv.Mat)
        dilate.Run(task.depth32f)
        dst1 = dilate.dst1
    End Sub
End Class








Public Class Depth_ForegroundHead : Inherits VBparent
    Dim fgnd As New Depth_Foreground
    Public kalman As New Kalman_Basics
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New()
        label1 = "Blue is current, red is kalman, green is trusted"
        task.desc = "Use Depth_ForeGround to find the foreground blob.  Then find the probable head of the person in front of the camera."
    End Sub
    Public Sub Run(src As cv.Mat)
        fgnd.Run(src)

        trustworthy = False
        If fgnd.dst1.CountNonZero() And fgnd.maxIndex >= 0 Then
            Dim rectSize = 50
            If src.Width > 1000 Then rectSize = 250
            Dim xx = fgnd.blobLocation.Item(fgnd.maxIndex).X - rectSize / 2
            Dim yy = fgnd.blobLocation.Item(fgnd.maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > src.Width Then xx = src.Width - rectSize
            dst1 = fgnd.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            kalman.kInput = {xx, yy, rectSize, rectSize}
            kalman.Run(src)
            Dim nextRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
            dst1.Rectangle(kRect, cv.Scalar.Red, 2)
            dst1.Rectangle(nextRect, cv.Scalar.Blue, 2)
            If Math.Abs(kRect.X - nextRect.X) < rectSize / 4 And Math.Abs(kRect.Y - nextRect.Y) < rectSize / 4 Then
                trustedRect = validateRect(kRect)
                trustworthy = True
                dst1.Rectangle(trustedRect, cv.Scalar.Green, 5)
            End If
        End If
    End Sub
End Class









Public Class Depth_RGBShadow : Inherits VBparent
    Public Sub New()
        task.desc = "Merge the RGB and Depth Shadow"
    End Sub
    Public Sub Run(src As cv.Mat)
        dst1 = src
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class
