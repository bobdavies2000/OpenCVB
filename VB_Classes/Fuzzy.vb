Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Public Class Fuzzy_Basics : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim options As New Options_Contours
    Public contours As cvb.Point()()
    Public sortContours As New SortedList(Of Integer, cvb.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        Dim floodRadio = FindRadio("FloodFill")
        If floodRadio.Enabled Then floodRadio.Enabled = False ' too much special handling - cv_32SC1 image 
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.redOptions.setBitReductionBar(32)
        cPtr = Fuzzy_Open()
        FindRadio("CComp").Checked = True
        labels = {"", "Solid regions", "8-Bit output of Fuzzy_Basics", "Fuzzy edges"}
        desc = "That which is not solid is fuzzy"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        reduction.Run(src)
        dst0 = reduction.dst2
        If dst0.Channels() <> 1 Then dst0 = dst0.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim dataSrc(dst0.Total) As Byte
        Marshal.Copy(dst0.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Fuzzy_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst0.Rows, dst0.Cols)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(dst0.Rows, dst0.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone
        dst3 = dst2.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv)

        Dim tmp As New cvb.Mat
        If options.retrievalMode = cvb.RetrievalModes.CComp Or options.retrievalMode = cvb.RetrievalModes.FloodFill Then
            dst3.ConvertTo(tmp, cvb.MatType.CV_32S)
        Else
            dst3.ConvertTo(tmp, cvb.MatType.CV_8U)
        End If
        contours = cvb.Cv2.FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)

        sortContours.Clear()
        For i = 0 To contours.Length - 1
            ' get this region's ID
            Dim maskID As Integer = 0
            Dim pt = contours(i)(0)
            For y = pt.Y - 1 To pt.Y + 1
                For x = pt.X - 1 To pt.X + 1
                    If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                        Dim val = dst2.Get(Of Byte)(y, x)
                        If val <> 0 Then
                            maskID = val
                            Exit For
                        End If
                    End If
                Next
                If maskID <> 0 Then Exit For
            Next
            sortContours.Add(contours(i).Length, New cvb.Point(i, maskID))
        Next

        dst1 = ShowPalette(dst2 * 255 / reduction.classCount)
        dst1.SetTo(0, dst3)
        labels(1) = "There were " + CStr(sortContours.Count) + " contour > 100 points."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Fuzzy_Close(cPtr)
    End Sub
End Class






Public Class Fuzzy_Filter : Inherits TaskParent
    Dim kernel As cvb.Mat
    Dim reduction As New Reduction_Basics
    Public contours As cvb.Point()()
    Public sortContours As New SortedList(Of Integer, cvb.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Dim options As New Options_Contours
    Public Sub New()
        Dim array() As Single = {1, 1, 1, 1, 1, 1, 1, 1, 1}
        kernel = cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32F, array)
        kernel *= 1 / 9
        desc = "Use a 2D filter to find smooth areas"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        reduction.Run(src)

        Dim src32f As New cvb.Mat
        reduction.dst2.ConvertTo(src32f, cvb.MatType.CV_32F)
        dst2 = src32f.Filter2D(-1, kernel)
        dst3 = dst2.Subtract(src32f)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv)
        dst3.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv)

        If options.retrievalMode = cvb.RetrievalModes.FloodFill Then
            Dim tmp As New cvb.Mat
            dst3.ConvertTo(tmp, cvb.MatType.CV_32S)
            contours = cvb.Cv2.FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)
        Else
            contours = cvb.Cv2.FindContoursAsArray(dst3, options.retrievalMode, options.ApproximationMode)
        End If

        sortContours.Clear()
        For i = 0 To contours.Length - 1
            Dim maskID As Integer = 0
            Dim pt = contours(i)(0)
            For y = pt.Y - 1 To pt.Y + 1
                For x = pt.X - 1 To pt.X + 1
                    If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                        Dim val = reduction.dst2.Get(Of Byte)(y, x)
                        If val <> 0 Then
                            maskID = val
                            Exit For
                        End If
                    End If
                Next
                If maskID <> 0 Then Exit For
            Next
            sortContours.Add(contours(i).Length, New cvb.Point(i, maskID))
        Next

        dst2 = ShowPalette(reduction.dst2 * 255 / reduction.classCount)
        dst2.SetTo(0, dst3)
    End Sub
End Class








Public Class Fuzzy_ContoursDepth : Inherits TaskParent
    Public fuzzyD As New Fuzzy_Basics
    Public Sub New()
        desc = "Use contours to outline solids in the depth data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fuzzyD.Run(task.depthRGB)
        dst2 = fuzzyD.dst1
    End Sub
End Class








Public Class Fuzzy_NeighborProof : Inherits TaskParent
    Dim fuzzy As New Fuzzy_Basics
    Dim proofFailed As Boolean = False
    Public Sub New()
        desc = "Prove that every contour point has at one and only one neighbor with the mask ID and that the rest are zero"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If proofFailed Then Exit Sub
        fuzzy.Run(src)
        dst2 = fuzzy.dst1
        For i = 0 To fuzzy.contours.Length - 1
            Dim len = fuzzy.contours(i).Length
            For j = 0 To len - 1
                Dim pt = fuzzy.contours(i)(j)
                Dim maskID As Integer = 0
                For y = Math.Max(0, pt.Y - 1) To pt.Y + 1
                    For x = Math.Max(0, pt.X - 1) To pt.X + 1
                        If x < src.Width And y < src.Height Then
                            Dim val = dst2.Get(Of Byte)(y, x)
                            If val <> 0 Then maskID = val
                            If maskID <> 0 And val <> 0 And maskID <> val Then
                                MsgBox("Proof has failed!  There is more than one mask ID identified by this contour point.")
                                proofFailed = True
                                Exit Sub
                            End If
                        End If
                    Next
                Next
            Next
        Next
        SetTrueText("Results are valid." + vbCrLf + "Mask ID's for all contour points in each region identified only one region.", New cvb.Point(10, 50), 3)
    End Sub
End Class








Public Class Fuzzy_TrackerDepth : Inherits TaskParent
    Public fuzzy As New Fuzzy_Basics
    Public centroids As New List(Of cvb.Point)
    Public rects As New List(Of cvb.Rect)
    Public layoutColor As New List(Of Integer)
    Public highlightPoint As cvb.Point
    Public highlightRect As cvb.Rect
    Public highlightRegion = -1
    Dim options As New Options_TrackerDepth
    Public Sub New()
        desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        fuzzy.Run(task.depthRGB)
        dst2 = fuzzy.dst1

        centroids.Clear()
        rects.Clear()
        layoutColor.Clear()
        Dim minX As Double, maxX As Double
        Dim minY As Double, maxY As Double
        For Each vec In fuzzy.sortContours.Values
            Dim contours = fuzzy.contours(vec(0))
            Dim points = cvb.Mat.FromPixelData(contours.Length, 1, cvb.MatType.CV_32SC2, contours.ToArray)
            Dim center = points.Sum()
            points = cvb.Mat.FromPixelData(contours.Length, 2, cvb.MatType.CV_32S, contours.ToArray)
            points.Col(0).MinMaxIdx(minX, maxX)
            points.Col(1).MinMaxIdx(minY, maxY)

            Dim rect = New cvb.Rect(minX, minY, maxX - minX, maxY - minY)
            If rect.Width * rect.Height > options.minRectSize Then
                Dim centroid = New cvb.Point2f(center(0) / contours.Length, center(1) / contours.Length)
                centroids.Add(centroid)
                rects.Add(rect)
                layoutColor.Add(vec(1))
                If options.displayRect Then
                    DrawCircle(dst2, centroid, task.DotSize + 3, cvb.Scalar.Yellow)
                    DrawCircle(dst2, centroid, task.DotSize, cvb.Scalar.Red)
                    dst2.Rectangle(rect, cvb.Scalar.Yellow, 2)
                End If
            End If
        Next

        labels(2) = CStr(fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class







Public Class Fuzzy_TrackerDepthClick : Inherits TaskParent
    Public tracker As New Fuzzy_TrackerDepth
    Public highlightPoint As cvb.Point
    Public highlightRect As cvb.Rect
    Public highlightRegion = -1
    Public regionMask As cvb.Mat
    Public Sub New()
        desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        tracker.Run(src)
        dst2 = tracker.dst2

        If highlightRegion < 0 Then SetTrueText("Click any color region to get more details and track it", New cvb.Point(10, 50), 3)

        dst3 = tracker.fuzzy.dst1
        If task.mouseClickFlag Then
            highlightPoint = task.ClickPoint
            highlightRegion = tracker.fuzzy.dst2.Get(Of Byte)(highlightPoint.Y, highlightPoint.X)
        End If
        If highlightRegion >= 0 Then
            regionMask = tracker.fuzzy.dst2.InRange(highlightRegion, highlightRegion + 1)
            dst3.SetTo(cvb.Scalar.Yellow, regionMask)
        End If
        labels(2) = CStr(tracker.fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class