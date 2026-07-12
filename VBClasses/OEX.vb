Imports System.IO
Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' all examples in this file are from https://github.com/opencv/opencv/tree/4.x/samples
Public Class OEX_CalcBackProject_Demo1 : Inherits TaskParent
    Public histogram As New Mat
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "BackProjection of Hue channel", "Plot of Hue histogram"}
        desc = "OpenCV Sample CalcBackProject_Demo1"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim ranges() As Rangef = New Rangef() {New Rangef(0, 180)}

        Dim hsv As New Mat
        CvtColor(task.color, hsv, ColorConversionCodes.BGR2HSV)
        CalcHist({hsv}, {0}, New Mat, histogram, 1, {task.histogramBins}, ranges)
        classCount = CountNonZero(histogram)
        Normalize(histogram, dst0, 0, classCount, NormTypes.MinMax) ' for the backprojection.

        Dim histArray(histogram.Total - 1) As Single
        histogram.GetArray(Of Single)(histArray)

        Dim peakValue = histArray.ToList.Max

        Normalize(histogram, histogram, 0, 1, NormTypes.MinMax)
        histogram.GetArray(Of Single)(histArray)

        CalcBackProject({hsv}, {0}, dst0, dst2, ranges)

        dst3.SetTo(Scalar.Red)
        Dim binW = dst2.Width / task.histogramBins
        Dim bins = dst2.Width / binW
        For i = 0 To bins - 1
            Dim h = dst2.Height * histArray(i)
            Dim r = New cv.Rect(i * binW, dst2.Height - h, binW, h)
            Rectangle(dst3, r, Scalar.Black, -1)
        Next
        labels(3) = $"The max value below is {peakValue}"
    End Sub
End Class







Public Class XR_OEX_CalcBackProject_Demo2 : Inherits TaskParent
    Public histogram As New Mat
    Public classCount As Integer = 10 ' initial value is just a guess.  It is refined after the first pass.
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.gOptions.setHistogramBins(6)
        labels = {"", "Mask for isolated region", "Backprojection of the hsv 2D histogram", "Mask in image context"}
        desc = "OpenCV Sample CalcBackProject_Demo2"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim count As Integer
        If task.clickPoint <> newPoint Then
            Dim connectivity As Integer = 8
            Dim flags = connectivity Or (255 << 8) Or FloodFillFlags.FixedRange Or FloodFillFlags.MaskOnly
            Dim mask2 As New Mat(src.Rows + 2, src.Cols + 2, MatType.CV_8U, Scalar.All(0))

            ' the delta between each regions value is 255 / classcount. no low or high bound needed.
            Dim delta = 255 \ classCount - 1
            Dim bounds = New Scalar(delta, delta, delta)
            count = FloodFill(dst2, mask2, task.clickPoint, 255, Nothing, bounds, bounds, flags)

            If count <> src.Total Then dst1 = mask2(New Range(1, mask2.Rows - 1), New Range(1, mask2.Cols - 1))
        End If
        Dim ranges() As Rangef = New Rangef() {New Rangef(0, 180), New Rangef(0, 256)}

        Dim hsv As New Mat
        CvtColor(task.color, hsv, ColorConversionCodes.BGR2HSV)
        CalcHist({hsv}, {0, 1}, New Mat, histogram, 2, {task.histogramBins, task.histogramBins}, ranges)
        classCount = CountNonZero(histogram)
        Normalize(histogram, histogram, 0, 255, NormTypes.MinMax)
        CalcBackProject({hsv}, {0, 1}, histogram, dst2, ranges)

        dst3 = src
        dst3.SetTo(white, dst1)

        SetTrueText("Click anywhere to isolate that region.", 1)
    End Sub
End Class







Public Class XR_OEX_bgfg_segm : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        desc = "OpenCV example bgfg_segm - existing BGSubtract_Basics is the same."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bgSub.Run(src)
        dst2 = bgSub.dst2
        labels(2) = bgSub.labels(2)
    End Sub
End Class







Public Class XR_OEX_bgSub : Inherits TaskParent
    Implements IDisposable
    Dim pBackSub As BackgroundSubtractor
    Dim options As New Options_BGSubtract
    Public Sub New()
        desc = "OpenCV example bgSub"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.optionsChanged Then
            If pBackSub IsNot Nothing Then pBackSub.Dispose()
            Select Case options.methodDesc
                Case "GMG"
                    pBackSub = BackgroundSubtractorGMG.Create()
                Case "KNN"
                    pBackSub = BackgroundSubtractorKNN.Create()
                Case "MOG"
                    pBackSub = BackgroundSubtractorMOG.Create()
                Case Else ' MOG2 is the default.  Other choices map to MOG2 because OpenCVSharp doesn't support them.
                    pBackSub = BackgroundSubtractorMOG2.Create()
            End Select
        End If
        pBackSub.Apply(src, dst2, options.learnRate)
    End Sub
    Protected Overrides Sub Finalize()
        If pBackSub IsNot Nothing Then pBackSub.Dispose()
    End Sub
End Class







Public Class XR_OEX_BasicLinearTransforms : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        desc = "OpenCV Example BasicLinearTransforms - NOTE: much faster than BasicLinearTransformTrackBar"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        src.ConvertTo(dst2, -1, options.brightness, options.contrast)
    End Sub
End Class






Public Class XR_OEX_BasicLinearTransformsTrackBar : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        desc = "OpenCV Example BasicLinearTransformTrackBar - much slower than XR_OEX_BasicLinearTransforms"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        For y As Integer = 0 To src.Rows - 1
            For x As Integer = 0 To src.Cols - 1
                Dim vec = src.Get(Of Vec3b)(y, x)
                vec(0) = Math.Max(Math.Min(vec(0) * options.brightness + options.contrast, 255), 0)
                vec(1) = Math.Max(Math.Min(vec(1) * options.brightness + options.contrast, 255), 0)
                vec(2) = Math.Max(Math.Min(vec(2) * options.brightness + options.contrast, 255), 0)
                dst2.Set(Of Vec3b)(y, x, vec)
            Next
        Next
    End Sub
End Class








Public Class XR_OEX_delaunay2 : Inherits TaskParent
    Dim active_facet_color As New Scalar(0, 0, 255)
    Dim delaunay_color As New Scalar(255, 255, 255)
    Dim points As New List(Of Point2f)
    Dim subdiv As New Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Next triangle list being built.  Latest entry is in red.", "The completed voronoi facets"}
        desc = "OpenCV Example delaunay2"
    End Sub
    Public Sub locatePoint(img As Mat, subdiv As Subdiv2D, pt As cv.Point, activeColor As Scalar)
        Dim e0 As Integer = 0
        Dim vertex As Integer = 0

        subdiv.Locate(pt, e0, vertex)

        If e0 > 0 Then
            Dim e As Integer = e0
            Do
                Dim org As cv.Point, dst As cv.Point
                If subdiv.EdgeOrg(e, org) > 0 And subdiv.EdgeDst(e, dst) > 0 Then
                    Line(img, org, dst, activeColor, task.lineWidth + 3, task.lineType, 0)
                End If

                e = subdiv.GetEdge(e, Subdiv2D.NEXT_AROUND_LEFT)
            Loop While e <> e0
        End If

        Circle(img, pt, task.DotSize, activeColor, -1, task.lineType)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.quarterBeat Then
            If points.Count < 10 Then
                dst2.SetTo(0)
                Dim pt = New Point2f(msRNG.Next(0, dst2.Width - 10) + 5, msRNG.Next(0, dst2.Height - 10) + 5)
                points.Add(pt)
                locatePoint(dst2, subdiv, pt, active_facet_color)
                subdiv.Insert(pt)

                Dim triangleList = subdiv.GetTriangleList()
                Dim pts(3 - 1) As cv.Point
                For i = 0 To triangleList.Count - 1
                    Dim t = triangleList(i)
                    pts(0) = New cv.Point(Math.Round(t(0)), Math.Round(t(1)))
                    pts(1) = New cv.Point(Math.Round(t(2)), Math.Round(t(3)))
                    pts(2) = New cv.Point(Math.Round(t(4)), Math.Round(t(5)))
                    Line(dst2, pts(0), pts(1), delaunay_color, task.lineWidth, task.lineType)
                    Line(dst2, pts(1), pts(2), delaunay_color, task.lineWidth, task.lineType)
                    Line(dst2, pts(2), pts(0), delaunay_color, task.lineWidth, task.lineType)
                Next
            Else
                dst1 = dst2.Clone

                Dim facets = New Point2f()() {Nothing}
                Dim centers() As Point2f = Nothing
                subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

                Dim ifacet As New List(Of cv.Point)
                Dim ifacets As New List(Of List(Of cv.Point))({ifacet})

                For i = 0 To facets.Count - 1
                    ifacet.Clear()
                    ifacet.AddRange(facets(i).Select(Function(p) New cv.Point(p.X, p.Y)))

                    Dim color = task.vecColors(i Mod 256)
                    FillConvexPoly(dst3, ifacet, color, 8, 0)

                    ifacets(0) = ifacet
                    Polylines(dst3, ifacets, True, New Vec3b, task.lineWidth, task.lineType)
                    Circle(dst3, centers(i), 3, New Vec3b, -1, task.lineType)
                Next

                points.Clear()
                subdiv = New Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
            End If
        End If
    End Sub
End Class







Public Class XR_OEX_MeanShift : Inherits TaskParent
    Dim term_crit As New TermCriteria(CriteriaTypes.Eps + CriteriaTypes.Count, 10, 1.0)
    Dim ranges() As Rangef = New Rangef() {New Rangef(0, 180)}
    Public histogram As New Mat
    Dim trackWindow as cv.Rect
    Public Sub New()
        labels(3) = "Draw a rectangle around the region of interest"
        desc = "OpenCV Example MeanShift"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim roi = If(task.drawRect.Width > 0, task.drawRect, New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv As New Mat
        CvtColor(src, hsv, ColorConversionCodes.BGR2HSV)
        dst2 = src
        If task.optionsChanged Then
            trackWindow = roi
            Dim mask As New Mat
            InRange(hsv, New Scalar(0, 60, 32), New Scalar(180, 255, 255), mask)
            CalcHist({hsv(roi)}, {0}, New Mat, histogram, 1, {task.histogramBins}, ranges)
            Normalize(histogram, histogram, 0, 255, NormTypes.MinMax)
        End If
        CalcBackProject({hsv}, {0}, histogram, dst3, ranges)
        If trackWindow.Width <> 0 Then
            MeanShift(dst3, trackWindow, TermCriteria.Both(10, 1))
            DrawRect(src, trackWindow, white)
        End If
    End Sub
End Class





Public Class OEX_PointPolygon : Inherits TaskParent
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        desc = "PointPolygonTest will decide what is inside and what is outside."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            rotatedRect.Run(src)
            CvtColor(rotatedRect.dst2, src, ColorConversionCodes.BGR2GRAY)
        End If

        dst2 = src.Clone
        Dim contours As cv.Point()() = Nothing
        FindContours(src, contours, Nothing, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple)

        dst1 = New Mat(dst1.Size(), MatType.CV_32F, Scalar.All(0))
        For i = 0 To dst1.Rows - 1
            For j = 0 To dst1.Cols - 1
                Dim distance = PointPolygonTest(contours(0), New cv.Point(j, i), True)
                dst1.Set(Of Single)(i, j, distance)
            Next
        Next

        Dim mm = GetMinMax(dst1)
        mm.minVal = Math.Abs(mm.minVal)
        mm.maxVal = Math.Abs(mm.maxVal)

        Dim blue As New Vec3b(0, 0, 0)
        Dim red As New Vec3b(0, 0, 0)
        For i = 0 To src.Rows - 1
            For j = 0 To src.Cols - 1
                Dim val = dst1.Get(Of Single)(i, j)
                If val < 0 Then
                    blue(0) = 255 - Math.Abs(val) * 255 / mm.minVal
                    dst3.Set(Of Vec3b)(i, j, blue)
                ElseIf val > 0 Then
                    red(2) = 255 - val * 255 / mm.maxVal
                    dst3.Set(Of Vec3b)(i, j, red)
                Else
                    dst3.Set(Of Vec3b)(i, j, white.ToVec3b)
                End If
            Next
        Next
    End Sub
End Class






Public Class XR_OEX_PointPolygon_demo : Inherits TaskParent
    Dim pointPoly As New OEX_PointPolygon
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "OpenCV Example PointPolygonTest_demo - it became PointPolygonTest_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim r As Integer = dst2.Height / 4
        Dim vert As New List(Of cv.Point)
        vert.Add(New Point2f(3 * r / 2 + dst2.Width / 4, 1.34 * r))
        vert.Add(New Point2f(1 * r + dst2.Width / 4, 2 * r))
        vert.Add(New Point2f(3 * r / 2 + dst2.Width / 4, 2.866 * r))
        vert.Add(New Point2f(5 * r / 2 + dst2.Width / 4, 2.866 * r))
        vert.Add(New Point2f(3 * r + dst2.Width / 4, 2 * r))
        vert.Add(New Point2f(5 * r / 2 + dst2.Width / 4, 1.34 * r))

        dst2.SetTo(0)
        For i As Integer = 0 To vert.Count - 1
            Line(dst2, vert(i), vert((i + 1) Mod 6), white, task.lineWidth, task.lineType)
        Next

        pointPoly.Run(dst2)
        dst3 = pointPoly.dst3
    End Sub
End Class








Public Class XR_OEX_Remap : Inherits TaskParent
    Dim remap As New Remap_Basics
    Public Sub New()
        desc = "The OpenCV Remap example became the Remap_Basics algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        remap.Run(src)
        dst2 = remap.dst2
        labels(2) = remap.labels(2)
    End Sub
End Class








Public Class XR_OEX_Threshold : Inherits TaskParent
    Dim threshold As New Threshold_Basics
    Public Sub New()
        desc = "OpenCV Example Threshold became Threshold_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        threshold.Run(src)
        dst2 = threshold.dst2
        dst3 = threshold.dst3
        labels = threshold.labels
    End Sub
End Class








Public Class XR_OEX_Threshold_Inrange : Inherits TaskParent
    Dim options As New Options_OEX
    Public Sub New()
        desc = "OpenCV Example Threshold_Inrange"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim hsv As New Mat
        CvtColor(src, hsv, ColorConversionCodes.BGR2HSV)
        InRange(hsv, options.lows, options.highs, dst2)
    End Sub
End Class







Public Class XR_OEX_Points_Classifier : Inherits TaskParent
    Dim basics As New Classifier_Basics_CPP
    Public Sub New()
        desc = "OpenCV Example Points_Classifier became Classifier_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3
        labels = basics.labels
        SetTrueText("Click the global DebugCheckBox to get another set of points.", 2)
    End Sub
End Class







Public Class XR_OEX_Core_Reduce : Inherits TaskParent
    Public Sub New()
        desc = "Use OpenCV's reduce API to create row/col sums, averages, and min/max."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim m As Mat = Mat.FromPixelData(3, 2, MatType.CV_32F, New Single() {1, 2, 3, 4, 5, 6})
            Dim col_sum As New Mat, row_sum As New Mat
            Reduce(m, col_sum, 0, ReduceTypes.Sum, MatType.CV_32F)
            Reduce(m, row_sum, 1, ReduceTypes.Sum, MatType.CV_32F)

            strOut = "Original Mat" + vbCrLf
            For y = 0 To m.Rows - 1
                For x = 0 To m.Cols - 1
                    strOut += CStr(m.Get(Of Single)(y, x)) + ", "
                Next
                strOut += vbCrLf
            Next

            strOut += vbCrLf + "col_sum" + vbCrLf
            For i = 0 To m.Cols - 1
                strOut += CStr(col_sum.Get(Of Single)(0, i)) + ", "
            Next

            strOut += vbCrLf + "row_sum" + vbCrLf
            For i = 0 To m.Rows - 1
                strOut += CStr(row_sum.Get(Of Single)(0, i)) + ", "
            Next

            'm =
            '[  1,   2;
            '   3,   4;
            '   5,   6]
            'col_sum = [9, 12]
            'row_sum = [3, 7, 11]

            Dim col_average As New Mat, row_average As New Mat, col_min As New Mat
            Dim col_max As New Mat, row_min As New Mat, row_max As New Mat

            Reduce(m, col_average, 0, ReduceTypes.Avg, MatType.CV_32F)
            Reduce(m, row_average, 1, ReduceTypes.Avg, MatType.CV_32F)

            Reduce(m, col_min, 0, ReduceTypes.Min, MatType.CV_32F)
            Reduce(m, row_min, 1, ReduceTypes.Min, MatType.CV_32F)

            Reduce(m, col_max, 0, ReduceTypes.Max, MatType.CV_32F)
            Reduce(m, row_max, 1, ReduceTypes.Max, MatType.CV_32F)

            'col_average = [3, 4]
            'row_average = [1.5, 3.5, 5.5]
            'col_min = [  1,   2]
            'row_min = [  1,   3,   5]
            'col_max = [  5,   6]
            'row_max = [  2,   4,   6]
        End If
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class XR_OEX_Core_Split : Inherits TaskParent
    Public Sub New()
        desc = "OpenCV Example Core_Split"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim d As Mat = Mat.FromPixelData(2, 2, MatType.CV_8UC3, New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12})

        Dim channels = Split(d)

        Dim samples(d.Total * d.ElemSize - 1) As Byte
        Marshal.Copy(d.Data, samples, 0, samples.Length)

        strOut = "Original 2x2 Mat"
        For i = 0 To samples.Count - 1
            strOut += samples(i).ToString + ", "
        Next
        strOut += vbCrLf

        For i = 0 To 2
            strOut += "Channels " + CStr(i) + vbCrLf
            For y = 0 To channels(i).Rows - 1
                For x = 0 To channels(i).Cols - 1
                    strOut += channels(i).Get(Of Byte)(y, x).ToString + ", "
                Next
                strOut += vbCrLf
            Next
        Next

        SetTrueText(strOut, 2)
    End Sub
End Class





Public Class XR_OEX_Filter2D : Inherits TaskParent
    Dim ddepth As MatType = -1, anchor = New cv.Point(-1, -1), kernelSize As Integer = 3, ind = 0
    Public Sub New()
        desc = "OpenCV Example Filter2D demo - Use a varying kernel to show the impact."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)

        If task.heartBeat Then ind += 1
        kernelSize = 3 + 2 * (ind Mod 5)
        Dim kernel As Mat = New Mat(kernelSize, kernelSize, MatType.CV_32F, Scalar.All(1 / (kernelSize * kernelSize)))

        Filter2D(src, dst2, ddepth, kernel, anchor, 0, BorderTypes.Default)
        SetTrueText("Kernel size = " + CStr(kernelSize), 3)
    End Sub
End Class





Public Class XR_OEX_FitEllipse : Inherits TaskParent
    Implements IDisposable
    Dim img As New Mat
    Dim options As New Options_FitEllipse
    Public Sub New()
        Dim fileInputName As New FileInfo(task.homeDir + "opencv/samples/data/ellipses.jpg")
        Dim tmp As Mat = ImRead(fileInputName.FullName)
        CvtColor(tmp, img, ColorConversionCodes.BGR2GRAY)

        cPtr = OEX_FitEllipse_Open()
        desc = "OEX Example fitellipse"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim cppData(img.Total * img.ElemSize - 1) As Byte
        Marshal.Copy(img.Data, cppData, 0, cppData.Length)

        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = OEX_FitEllipse_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), img.Rows, img.Cols,
                                                 options.threshold, options.fitType)
        handleSrc.Free()

        dst2 = Mat.FromPixelData(img.Rows + 4, img.Cols + 4, MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Protected Overrides Sub Finalize()
        OEX_FitEllipse_Close(cPtr)
    End Sub
End Class

Module OEX_FitEllipse_CPP_Module
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_FitEllipse_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OEX_FitEllipse_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_FitEllipse_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer,
                                              threshold As Integer, fitType As Integer) As IntPtr
    End Function
End Module


