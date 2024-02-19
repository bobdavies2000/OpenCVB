Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
' all examples in this file are from https://github.com/opencv/opencv/tree/4.x/samples
Public Class OEX_CalcBackProject_Demo1 : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "BackProjection of Hue channel", "Plot of Hue histogram"}
        vbAddAdvice(traceName + ": <place advice here on any options that are useful>")
        desc = "OpenCV Sample CalcBackProject_Demo1"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180)}

        Dim hsv As cv.Mat = task.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        cv.Cv2.CalcHist({hsv}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)
        classCount = histogram.CountNonZero
        dst0 = histogram.Normalize(0, classCount, cv.NormTypes.MinMax) ' for the backprojection.

        Dim histArray(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim peakValue = histArray.ToList.Max

        histogram = histogram.Normalize(0, 1, cv.NormTypes.MinMax)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        cv.Cv2.CalcBackProject({hsv}, {0}, dst0, dst2, ranges)

        dst3.SetTo(cv.Scalar.Red)
        Dim binW = dst2.Width / task.histogramBins
        Dim bins = dst2.Width / binW
        For i = 0 To bins - 1
            Dim h = dst2.Height * histArray(i)
            Dim r = New cv.Rect(i * binW, dst2.Height - h, binW, h)
            dst3.Rectangle(r, cv.Scalar.Black, -1)
        Next
        If task.heartBeat Then labels(3) = $"The max value below is {peakValue}"
    End Sub
End Class







Public Class OEX_CalcBackProject_Demo2 : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer = 10 ' initial value is just a guess.  It is refined after the first pass.
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.HistBinSlider.Value = 6
        labels = {"", "Mask for isolated region", "Backprojection of the hsv 2D histogram", "Mask in image context"}
        desc = "OpenCV Sample CalcBackProject_Demo2"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim count As Integer
        If task.clickPoint <> New cv.Point Then
            Dim connectivity As Integer = 8
            Dim flags = connectivity Or (255 << 8) Or cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.MaskOnly
            Dim mask2 As New cv.Mat(src.Rows + 2, src.Cols + 2, cv.MatType.CV_8U, 0)

            ' the delta between each regions value is 255 / classcount. no low or high bound needed.
            Dim delta = CInt(255 / classCount) - 1
            Dim bounds = New cv.Scalar(delta, delta, delta)
            count = cv.Cv2.FloodFill(dst2, mask2, task.clickPoint, 255, Nothing, bounds, bounds, flags)

            If count <> src.Total Then dst1 = mask2(New cv.Range(1, mask2.Rows - 1), New cv.Range(1, mask2.Cols - 1))
        End If
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}

        Dim hsv As cv.Mat = task.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        cv.Cv2.CalcHist({hsv}, {0, 1}, New cv.Mat, histogram, 2, {task.histogramBins, task.histogramBins}, ranges)
        classCount = histogram.CountNonZero
        histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        cv.Cv2.CalcBackProject({hsv}, {0, 1}, histogram, dst2, ranges)

        dst3 = src
        dst3.SetTo(cv.Scalar.White, dst1)

        setTrueText("Click anywhere to isolate that region.", 1)
    End Sub
End Class







Public Class OEX_bgfg_segm : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        desc = "OpenCV example bgfg_segm - existing BGSubtract_Basics is the same."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bgSub.Run(src)
        dst2 = bgSub.dst2
        labels(2) = bgSub.labels(2)
    End Sub
End Class







Public Class OEX_bgSub : Inherits VB_Algorithm
    Dim pBackSub As cv.BackgroundSubtractor
    Dim options As New Options_BGSubtract
    Public Sub New()
        desc = "OpenCV example bgSub"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then
            Select Case options.methodDesc
                Case "GMG"
                    pBackSub = cv.BackgroundSubtractorGMG.Create()
                Case "KNN"
                    pBackSub = cv.BackgroundSubtractorKNN.Create()
                Case "MOG"
                    pBackSub = cv.BackgroundSubtractorMOG.Create()
                Case Else ' MOG2 is the default.  Other choices map to MOG2 because OpenCVSharp doesn't support them.
                    pBackSub = cv.BackgroundSubtractorMOG2.Create()
            End Select
        End If
        pBackSub.Apply(src, dst2, options.learnRate)
    End Sub
End Class







Public Class OEX_BasicLinearTransforms : Inherits VB_Algorithm
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        findSlider("Alpha (contrast)").Value = 2
        findSlider("Beta (brightness)").Value = 40
        desc = "OpenCV Example BasicLinearTransforms - NOTE: much faster than BasicLinearTransformTrackBar"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static alphaSlider = findSlider("Alpha (contrast)")
        Static betaSlider = findSlider("Beta (brightness)")
        src.ConvertTo(dst2, -1, alphaSlider.value, betaSlider.value)
    End Sub
End Class







Public Class OEX_BasicLinearTransformsTrackBar : Inherits VB_Algorithm
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        findSlider("Alpha (contrast)").Value = 2
        findSlider("Beta (brightness)").Value = 40
        desc = "OpenCV Example BasicLinearTransformTrackBar"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static alphaSlider = findSlider("Alpha (contrast)")
        Static betaSlider = findSlider("Beta (brightness)")
        Dim alpha = alphaSlider.value
        Dim beta = betaSlider.value
        For y As Integer = 0 To src.Rows - 1
            For x As Integer = 0 To src.Cols - 1
                Dim vec = src.Get(Of cv.Vec3b)(y, x)
                vec(0) = Math.Min(vec(0) * alpha + beta, 255)
                vec(1) = Math.Min(vec(1) * alpha + beta, 255)
                vec(2) = Math.Min(vec(2) * alpha + beta, 255)
                dst2.Set(Of cv.Vec3b)(y, x, vec)
            Next
        Next
    End Sub
End Class








Public Class OEX_delaunay2 : Inherits VB_Algorithm
    Dim active_facet_color As New cv.Scalar(0, 0, 255)
    Dim delaunay_color As New cv.Scalar(255, 255, 255)
    Dim points As New List(Of cv.Point2f)
    Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Next triangle list being built.  Latest entry is in red.", "The completed voronoi facets"}
        desc = "OpenCV Example delaunay2"
    End Sub
    Public Shared Sub locatePoint(img As cv.Mat, subdiv As cv.Subdiv2D, pt As cv.Point, activeColor As cv.Scalar)
        Dim e0 As Integer = 0
        Dim vertex As Integer = 0

        subdiv.Locate(pt, e0, vertex)

        If e0 > 0 Then
            Dim e As Integer = e0
            Do
                Dim org As cv.Point, dst As cv.Point
                If subdiv.EdgeOrg(e, org) > 0 AndAlso subdiv.EdgeDst(e, dst) > 0 Then
                    img.Line(org, dst, activeColor, task.lineWidth + 3, task.lineType, 0)
                End If

                e = subdiv.GetEdge(e, cv.Subdiv2D.NEXT_AROUND_LEFT)
            Loop While e <> e0
        End If

        img.Circle(pt, task.dotSize, activeColor, -1, task.lineType)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.quarterBeat Then
            If points.Count < 10 Then
                dst2.SetTo(0)
                Dim pt = New cv.Point2f(msRNG.Next(0, dst2.Width - 10) + 5, msRNG.Next(0, dst2.Height - 10) + 5)
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
                    dst2.Line(pts(0), pts(1), delaunay_color, task.lineWidth, task.lineType)
                    dst2.Line(pts(1), pts(2), delaunay_color, task.lineWidth, task.lineType)
                    dst2.Line(pts(2), pts(0), delaunay_color, task.lineWidth, task.lineType)
                Next
            Else
                dst1 = dst2.Clone

                Dim facets = New cv.Point2f()() {Nothing}
                Dim centers() As cv.Point2f
                subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

                Dim ifacet As New List(Of cv.Point)
                Dim ifacets As New List(Of List(Of cv.Point))({ifacet})

                For i = 0 To facets.Count - 1
                    ifacet.Clear()
                    ifacet.AddRange(facets(i).Select(Function(p) New cv.Point(p.X, p.Y)))

                    Dim color = task.vecColors(i Mod 255)
                    dst3.FillConvexPoly(ifacet, color, 8, 0)

                    ifacets(0) = ifacet
                    cv.Cv2.Polylines(dst3, ifacets, True, New cv.Vec3b, task.lineWidth, task.lineType)
                    dst3.Circle(centers(i), 3, New cv.Vec3b, -1, task.lineType)
                Next

                points.Clear()
                subdiv = New cv.Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
            End If
        End If
    End Sub
End Class







Public Class OEX_MeanShift : Inherits VB_Algorithm
    Dim term_crit As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180)}
    Public histogram As New cv.Mat
    Public Sub New()
        labels(3) = "Draw a rectangle around the region of interest"
        desc = "OpenCV Example MeanShift"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static trackWindow As cv.Rect
        Dim roi = If(task.drawRect.Width > 0, task.drawRect, New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv As cv.Mat = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        dst2 = src
        If task.optionsChanged Then
            trackWindow = roi
            Dim mask As New cv.Mat
            cv.Cv2.InRange(hsv, New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255), mask)
            cv.Cv2.CalcHist({hsv(roi)}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)
            histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        End If
        cv.Cv2.CalcBackProject({hsv}, {0}, histogram, dst3, ranges)
        If trackWindow.Width <> 0 Then
            cv.Cv2.MeanShift(dst3, trackWindow, cv.TermCriteria.Both(10, 1))
            src.Rectangle(trackWindow, cv.Scalar.White, task.lineWidth, task.lineType)
        End If
    End Sub
End Class







Public Class OEX_PointPolygonTest_demo : Inherits VB_Algorithm
    Dim pointPoly As New PointPolygonTest_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "OpenCV Example PointPolygonTest_demo - it became PointPolygonTest_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim r As Integer = dst2.Height / 4
        Dim vert As New List(Of cv.Point)
        vert.Add(New cv.Point2f(3 * r / 2 + dst2.Width / 4, 1.34 * r))
        vert.Add(New cv.Point2f(1 * r + dst2.Width / 4, 2 * r))
        vert.Add(New cv.Point2f(3 * r / 2 + dst2.Width / 4, 2.866 * r))
        vert.Add(New cv.Point2f(5 * r / 2 + dst2.Width / 4, 2.866 * r))
        vert.Add(New cv.Point2f(3 * r + dst2.Width / 4, 2 * r))
        vert.Add(New cv.Point2f(5 * r / 2 + dst2.Width / 4, 1.34 * r))

        dst2.SetTo(0)
        For i As Integer = 0 To vert.Count - 1
            dst2.Line(vert(i), vert((i + 1) Mod 6), cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        pointPoly.Run(dst2)
        dst3 = pointPoly.dst3
    End Sub
End Class








Public Class OEX_Remap : Inherits VB_Algorithm
    Dim remap As New Remap_Basics
    Public Sub New()
        desc = "The OpenCV Remap example became the Remap_Basics algorithm."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        remap.Run(src)
        dst2 = remap.dst2
        labels(2) = remap.labels(2)
    End Sub
End Class





Public Class OEX_Sobel_Demo : Inherits VB_Algorithm
    Dim sobel As New Edge_Sobel
    Public Sub New()
        desc = "OpenCV Example Sobel_Demo became Edge_Sobel algorithm."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sobel.Run(src)
        dst2 = sobel.dst2
        dst3 = sobel.dst3
        labels = sobel.labels
    End Sub
End Class







Public Class OEX_Threshold : Inherits VB_Algorithm
    Dim threshold As New Threshold_Basics
    Public Sub New()
        desc = "OpenCV Example Threshold became Threshold_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        threshold.Run(src)
        dst2 = threshold.dst2
        dst3 = threshold.dst3
        labels = threshold.labels
    End Sub
End Class








Public Class OEX_Threshold_Inrange : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Hue low", 0, 180, 90)
            sliders.setupTrackBar("Hue high", 0, 180, 180)
            sliders.setupTrackBar("Saturation low", 0, 255, 50)
            sliders.setupTrackBar("Saturation high", 0, 255, 150)
            sliders.setupTrackBar("Value low", 0, 255, 50)
            sliders.setupTrackBar("Value high", 0, 255, 150)
        End If

        desc = "OpenCV Example Threshold_Inrange"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static hueLowSlider = findSlider("Hue low")
        Static hueHighSlider = findSlider("Hue high")
        Static satLowSlider = findSlider("Saturation low")
        Static satHighSlider = findSlider("Saturation high")
        Static valLowSlider = findSlider("Value low")
        Static valHighSlider = findSlider("Value high")
        Dim lows As New cv.Scalar(hueLowSlider.value, satLowSlider.value, valLowSlider.value)
        Dim highs As New cv.Scalar(hueHighSlider.value, satHighSlider.value, valHighSlider.value)

        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        dst2 = hsv.InRange(lows, highs)
    End Sub
End Class







Public Class OEX_Points_Classifier : Inherits VB_Algorithm
    Dim basics As New Classifier_Basics
    Public Sub New()
        desc = "OpenCV Example Points_Classifier became Classifier_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3
        labels = basics.labels
        setTrueText("Click the global DebugCheckBox to get another set of points.", 2)
    End Sub
End Class






Public Class OEX_GoodFeaturesToTrackDemo : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "OpenCV Example GoodFeaturesToTrackDemo - now Feature_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)
    End Sub
End Class












Public Class OEX_Core_Reduce : Inherits VB_Algorithm
    Public Sub New()
        desc = "Use OpenCV's reduce API to create row/col sums, averages, and min/max."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            Dim m As New cv.Mat(3, 2, cv.MatType.CV_32F, New Single() {1, 2, 3, 4, 5, 6})
            Dim col_sum As New cv.Mat, row_sum As New cv.Mat
            cv.Cv2.Reduce(m, col_sum, 0, cv.ReduceTypes.Sum, cv.MatType.CV_32F)
            cv.Cv2.Reduce(m, row_sum, 1, cv.ReduceTypes.Sum, cv.MatType.CV_32F)

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

            Dim col_average As New cv.Mat, row_average As New cv.Mat, col_min As New cv.Mat
            Dim col_max As New cv.Mat, row_min As New cv.Mat, row_max As New cv.Mat

            cv.Cv2.Reduce(m, col_average, 0, cv.ReduceTypes.Avg, cv.MatType.CV_32F)
            cv.Cv2.Reduce(m, row_average, 1, cv.ReduceTypes.Avg, cv.MatType.CV_32F)

            cv.Cv2.Reduce(m, col_min, 0, cv.ReduceTypes.Min, cv.MatType.CV_32F)
            cv.Cv2.Reduce(m, row_min, 1, cv.ReduceTypes.Min, cv.MatType.CV_32F)

            cv.Cv2.Reduce(m, col_max, 0, cv.ReduceTypes.Max, cv.MatType.CV_32F)
            cv.Cv2.Reduce(m, row_max, 1, cv.ReduceTypes.Max, cv.MatType.CV_32F)

            'col_average = [3, 4]
            'row_average = [1.5, 3.5, 5.5]
            'col_min = [  1,   2]
            'row_min = [  1,   3,   5]
            'col_max = [  5,   6]
            'row_max = [  2,   4,   6]
        End If
        setTrueText(strOut, 2)
    End Sub
End Class






Public Class OEX_Core_Split : Inherits VB_Algorithm
    Public Sub New()
        desc = "OpenCV Example Core_Split"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim d As New cv.Mat(2, 2, cv.MatType.CV_8UC3, New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12})

        Dim channels = d.Split()

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

        setTrueText(strOut, 2)
    End Sub
End Class





Public Class OEX_Filter2D : Inherits VB_Algorithm
    Public Sub New()
        desc = "OpenCV Example Filter2D demo - Use a varying kernel to show the impact."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static ddepth As cv.MatType = -1, anchor = New cv.Point(-1, -1), kernelSize As Integer = 3, ind = 0

        If task.heartBeat Then ind += 1
        kernelSize = 3 + 2 * (ind Mod 5)
        Dim kernel As cv.Mat = New cv.Mat(kernelSize, kernelSize, cv.MatType.CV_32F, 1 / (kernelSize * kernelSize))

        dst2 = src.Filter2D(ddepth, kernel, anchor, 0, cv.BorderTypes.Default)
        setTrueText("Kernel size = " + CStr(kernelSize), 3)
    End Sub
End Class





Public Class OEX_FitEllipse : Inherits VB_Algorithm
    Dim img As cv.Mat
    Public Sub New()
        Dim fileInputName As New FileInfo(task.homeDir + "opencv/samples/data/ellipses.jpg")
        img = cv.Cv2.ImRead(fileInputName.FullName)

        If sliders.Setup(traceName) Then sliders.setupTrackBar("FitEllipse threshold", 0, 100, 50)
        cPtr = OEX_FitEllipse_Open()
        desc = "OEX Example fitellipse"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("FitEllipse threshold")
        Dim threshold = thresholdSlider.value

        Dim cppData(img.Total * img.ElemSize - 1) As Byte
        Marshal.Copy(img.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = OEX_FitEllipse_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), img.Rows, img.Cols, img.Channels)
        handleSrc.Free()

        dst2 = New cv.Mat(img.Rows, img.Cols, If(img.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), imagePtr).Clone
    End Sub
    Public Sub Close()
        OEX_FitEllipse_Close(cPtr)
    End Sub
End Class

Module OEX_FitEllipse_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_FitEllipse_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OEX_FitEllipse_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_FitEllipse_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module