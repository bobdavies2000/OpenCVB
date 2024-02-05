Imports OpenCvSharp
Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Shapes
Imports System.Drawing.Drawing2D
Imports System.Text.RegularExpressions
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports OpenCvSharp.Internal.Vectors
Imports CS_Classes
Imports OpenCvSharp.ML
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
            Dim mask2 As New Mat(src.Rows + 2, src.Cols + 2, cv.MatType.CV_8U, 0)

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






Public Class OEX_Points_Classifier : Inherits VB_Algorithm
    Dim options As New Options_Classifier
    Public Sub New()
        gOptions.DebugCheckBox.Checked = True
        cPtr = OEX_Points_Classifier_Open()
        desc = "OpenCV Example Points_Classifier"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then gOptions.DebugCheckBox.Checked = True
        Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, options.sampleCount, options.methodIndex, dst2.Rows, dst2.Cols,
                                                    If(gOptions.DebugCheckBox.Checked, 1, 0))
        gOptions.DebugCheckBox.Checked = False
        dst1 = New cv.Mat(dst0.Rows, dst0.Cols, cv.MatType.CV_32S, imagePtr)

        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = vbPalette(dst0 * 255 / 2)
        imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.dotSize)
        dst3 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)

        setTrueText("Click the global DebugCheckBox to get another set of points.", 3)
    End Sub
    Public Sub Close()
        OEX_Points_Classifier_Close(cPtr)
    End Sub
End Class

Module OEX_Points_Classifier_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_Points_Classifier_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OEX_Points_Classifier_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_ShowPoints(cPtr As IntPtr, imgRows As Integer, imgCols As Integer, dotSize As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_Points_Classifier_RunCPP(cPtr As IntPtr, count As Integer, methodIndex As Integer,
                                                 imgRows As Integer, imgCols As Integer, resetInput As Integer) As IntPtr
    End Function
End Module







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
