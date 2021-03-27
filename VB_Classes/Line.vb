Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Line_Basics
    Inherits VBparent
    Dim lines As Line_Stable
    Dim ld As cv.XImgProc.FastLineDetector
    Public sortlines As New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        initParent()
        lines = New Line_Stable
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        label1 = "Stable lines after IMU motion detection"
        task.desc = "Use the line detector on the stable lines produced by Line_Stable"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lines.src = src
        lines.Run()
        dst1 = lines.dst2

        Dim ldLines = ld.Detect(lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        sortlines.Clear()

        dst2.SetTo(0)
        For Each v In ldLines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
               v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If pixelLen > lines.pixelThreshold Then
                    dst2.Line(pt1, pt2, cv.Scalar.Yellow, lines.thickness, cv.LineTypes.AntiAlias)
                    sortlines.Add(pixelLen, New cv.Vec4f(pt1.X, pt1.Y, pt2.X, pt2.Y))
                End If
            End If
        Next
    End Sub
End Class








Public Class Line_Stable
    Inherits VBparent
    Dim ld As cv.XImgProc.FastLineDetector
    Dim stable As IMU_IscameraStable
    Public sortlines As New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
    Public thickness As Integer
    Public pixelThreshold As Integer
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Line thickness", 1, 20, 2)
            sliders.setupTrackBar(1, "Line length threshold in pixels", 1, src.Width + src.Height, 50)
            sliders.setupTrackBar(2, "Depth search radius in pixels", 1, 20, 2) ' not used in Run below but externally...
            sliders.setupTrackBar(3, "x- and y-intercept search range in pixels", 1, 50, 10) ' not used in Run below but externally...
        End If
        stable = New IMU_IscameraStable
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        label1 = "Yellow > length threshold"
        task.desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim lines = ld.Detect(src)
        Static thicknessSlider = findSlider("Line thickness")
        thickness = thicknessSlider.Value
        Static pixelSlider = findSlider("Line length threshold in pixels")
        pixelThreshold = pixelSlider.value

        sortlines.Clear()

        For Each v In lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
               v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If pixelLen > pixelThreshold Then
                    dst1.Line(pt1, pt2, cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
                    sortlines.Add(pixelLen, New cv.Vec4f(pt1.X, pt1.Y, pt2.X, pt2.Y))
                End If
            End If
        Next

        stable.Run()
        If stable.cameraStable = False Then dst2.SetTo(0)

        For Each line In sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst2.Line(p1, p2, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class







Module Line_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetectorFast_Run(image As IntPtr, rows As Integer, cols As Integer, length_threshold As Integer, distance_threshold As Single, canny_th1 As Integer, canny_th2 As Integer,
                                             canny_aperture_size As Integer, do_merge As Boolean) As Integer
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Lines() As IntPtr
    End Function

    Public Sub find3DLineSegment(dst2 As cv.Mat, _mask As cv.Mat, _depth32f As cv.Mat, aa As cv.Vec6f, maskLineWidth As Integer)
        Dim pt1 = New cv.Point(aa(0), aa(1))
        Dim pt2 = New cv.Point(aa(2), aa(3))
        Dim centerPoint = New cv.Point((aa(0) + aa(2)) / 2, (aa(1) + aa(3)) / 2)
        _mask.Line(pt1, pt2, New cv.Scalar(1), maskLineWidth, cv.LineTypes.AntiAlias)
        dst2.Line(pt1, pt2, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)

        Dim roi = New cv.Rect(Math.Min(aa(0), aa(2)), Math.Min(aa(1), aa(3)), Math.Abs(aa(0) - aa(2)), Math.Abs(aa(1) - aa(3)))

        Dim worldDepth As New List(Of cv.Vec6f)
        If roi.Width = 0 Then roi.Width = 1
        If roi.Height = 0 Then roi.Height = 1
        If roi.X + roi.Width >= _mask.Width Then roi.Width = _mask.Width - roi.X - 1
        If roi.Y + roi.Height >= _mask.Height Then roi.Height = _mask.Height - roi.Y - 1
        Dim mask = _mask(roi).Clone()
        Dim depth32f = _depth32f(roi).Clone()
        Dim totalPoints As Integer
        Dim skipPoints As Integer
        For y = 0 To roi.Height - 1
            For x = 0 To roi.Width - 1
                If mask.Get(Of Byte)(y, x) = 1 Then
                    totalPoints += 1
                    Dim w = getWorldCoordinatesD6(New cv.Point3f(x + roi.X, y + roi.Y, depth32f.Get(Of Single)(y, x)))
                    worldDepth.Add(w)
                End If
            Next
        Next
        Dim endPoints(2) As cv.Vec6f
        ' we need more than a few points...so 50
        If worldDepth.Count > 50 Then
            endPoints = segment3D(worldDepth, skipPoints)

            ' if the sample is large enough (at least 20% of possible points), then project the line for the full length of the RGB line.
            ' Note: when using RGB to determine a projected length, the line is defined by pixel coordinate for y. (z_depth = m * y_pixel + bb)
            If skipPoints / totalPoints < 0.5 Then
                If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                Dim m = (endPoints(0).Item4 - endPoints(1).Item4) / (endPoints(0).Item2 - endPoints(1).Item2)
                Dim bb = endPoints(0).Item2 - m * endPoints(0).Item4
                endPoints(0) = worldDepth(0)
                endPoints(0).Item2 = m * pt1.Y + bb
                endPoints(1) = worldDepth(worldDepth.Count - 1)
                endPoints(1).Item2 = m * pt2.Y + bb
            End If

            ' we need more than a few points...so 10
            Dim zero = New cv.Vec6f(0, 0, 0, 0, 0, 0)
            If endPoints(0) <> zero And endPoints(1) <> zero Then
                Dim b = endPoints(0)
                Dim d = endPoints(1)
                Dim lenBD = Math.Sqrt((b.Item0 - d.Item0) * (b.Item0 - d.Item0) + (b.Item1 - d.Item1) * (b.Item1 - d.Item1) + (b.Item2 - d.Item2) * (b.Item2 - d.Item2))
                cv.Cv2.PutText(dst2, Format(lenBD / 1000, "0.00") + "m", centerPoint, cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1,
                                   cv.LineTypes.AntiAlias)
                If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                cv.Cv2.PutText(dst2, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "0.00") + "y/z",
                                   New cv.Point(centerPoint.X, centerPoint.Y + 10), cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                ' show the final endpoints in xy projection.
                dst2.Circle(New cv.Point(b.Item3, b.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                dst2.Circle(New cv.Point(d.Item3, d.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Function segment3D(worldDepth As List(Of cv.Vec6f), ByRef skipPoints As Integer) As cv.Vec6f()
        ' by construction, x and y are already on a line.  Compute the average z delta.  Eliminate outliers with that average.
        Dim sum As Double = 0
        Dim midPoint As Double
        For i = 1 To worldDepth.Count - 1
            midPoint += worldDepth(i).Item2
            sum += Math.Abs(worldDepth(i).Item2 - worldDepth(i - 1).Item2)
        Next
        Dim avgDelta = sum / worldDepth.Count * 3
        midPoint /= worldDepth.Count
        Dim midIndex As Integer = -1
        ' find a point which is certain to be on the line - something close the centroid
        For i = worldDepth.Count / 4 To worldDepth.Count - 1
            If Math.Abs(worldDepth(i).Item2 - midPoint) < avgDelta Then
                midIndex = i
                Exit For
            End If
        Next
        Dim endPoints(2) As cv.Vec6f
        If midIndex > 0 Then
            endPoints(0) = worldDepth(midIndex) ' we start with a known centroid on the line.
            Dim delta As Single
            For i = midIndex - 1 To 1 Step -1
                delta = Math.Abs(endPoints(0).Item2 - worldDepth(i).Item2)
                If delta < avgDelta Then endPoints(0) = worldDepth(i) Else skipPoints += 1
            Next

            endPoints(1) = worldDepth(midIndex) ' we start with a known good point on the line.
            For i = midIndex + 1 To worldDepth.Count - 2
                delta = Math.Abs(endPoints(1).Item2 - worldDepth(i).Item2)
                If delta < avgDelta Then endPoints(1) = worldDepth(i) Else skipPoints += 1
            Next
        End If
        Return endPoints
    End Function

    Public Class CompareVec6f : Implements IComparer(Of cv.Vec6f)
        Public Function Compare(ByVal a As cv.Vec6f, ByVal b As cv.Vec6f) As Integer Implements IComparer(Of cv.Vec6f).Compare
            If a(4) > b(4) Then Return 1
            Return -1 ' never returns equal because the lines are always distinct but may have equal length
        End Function
    End Class

    ' there is a drawsegments in the contrib library but this code will operate on the full size of the image - not the small copy passed to the C++ code
    ' But, more importantly, this code uses anti-alias for the lines.  It adds the lines to a mask that may be useful with depth data.
    Public Function drawSegments(dst1 As cv.Mat, lineCount As Integer, thickness As Integer, ByRef lineMat As cv.Mat) As SortedList(Of cv.Vec6f, Integer)
        Dim sortedLines As New SortedList(Of cv.Vec6f, Integer)(New CompareVec6f)

        Dim lines(lineCount * 4 - 1) As Single
        Dim linePtr = lineDetector_Lines()
        If linePtr = 0 Then Return Nothing ' it happened!
        Marshal.Copy(linePtr, lines, 0, lines.Length)

        lineMat = New cv.Mat(lineCount, 1, cv.MatType.CV_32FC4, lines)
        Dim v6 As New cv.Vec6f
        For i = 0 To lineCount - 1
            Dim v = lineMat.Get(Of cv.Vec4f)(i)
            ' make sure that none are negative - how could any be negative?  Usually just fractionally less than zero.
            For j = 0 To 3
                If v(j) < 0 Then v(j) = 0
            Next

            v6(0) = v(0)
            v6(1) = v(1)
            v6(2) = v(2)
            v6(3) = v(3)
            v6(4) = Math.Sqrt((v(0) - v(2)) * (v(0) - v(2)) + (v(1) - v(3)) * (v(1) - v(3))) ' vector carries the length in pixels with it.
            v6(5) = 0 ' unused...

            ' add this line to the sorted list
            If sortedLines.ContainsKey(v6) Then
                sortedLines(v6) = sortedLines(v6) + 1
            Else
                sortedLines.Add(v6, 1)
            End If
        Next

        For i = sortedLines.Count - 1 To 0 Step -1
            Dim v = sortedLines.ElementAt(i).Key
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                dst1.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
            End If
        Next
        Return sortedLines
    End Function
End Module




' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class line_FLD_CPP
    Inherits VBparent
    Public sortedLines As New SortedList(Of cv.Vec6f, Integer)
    Public lineMat As New cv.Mat
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 6)
            sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
            sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
            sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
            sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
            sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
            sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "FLD - incremental merge"
            check.Box(0).Checked = True
        End If
        task.desc = "Basics for a Fast Line Detector"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        sortedLines.Clear()

        Dim length_threshold = sliders.trackbar(0).Value
        Dim distance_threshold = sliders.trackbar(1).Value / 10
        Dim canny_aperture_size = sliders.trackbar(2).Value
        If canny_aperture_size Mod 2 = 0 Then canny_aperture_size += 1
        Dim canny_th1 = sliders.trackbar(4).Value
        Dim canny_th2 = sliders.trackbar(5).Value
        Dim do_merge = check.Box(0).Checked

        src.CopyTo(dst1)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim data(src.Total - 1) As Byte

        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim lineCount = lineDetectorFast_Run(handle.AddrOfPinnedObject, src.Height, src.Width, length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge)
        handle.Free()

        Static sizeSlider = findSlider("FLD - Line Thickness")
        If lineCount > 0 Then sortedLines = drawSegments(dst1, lineCount, sizeSlider.Value, lineMat)
    End Sub
End Class






Public Class Line_LongestLine
    Inherits VBparent
    Dim lines As line_FLD_CPP
    Public Sub New()
        initParent()
        lines = New line_FLD_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
            sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)
        End If

        task.desc = "Identify planes using the lines present in the rgb image."
        label2 = ""
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run()
        src.CopyTo(dst1)

        If lines.sortedLines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
            Dim maskLineWidth As Integer = sliders.trackbar(0).Value
            Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
            find3DLineSegment(dst1, mask, task.depth32f, lines.sortedLines.ElementAt(lines.sortedLines.Count - 1).Key, maskLineWidth)
        End If
    End Sub
End Class




Public Class Line_FLD_MT
    Inherits VBparent
    Dim lines As line_FLD_CPP
    Public Sub New()
        initParent()
        lines = New line_FLD_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
            sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)
        End If
        task.desc = "Measure 3d line segments using a multi-threaded Fast Line Detector."
        label2 = ""
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run()
        src.CopyTo(dst1)

        ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
        Dim maskLineWidth As Integer = sliders.trackbar(0).Value
        Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
        Dim lineCount = Math.Max(lines.sortedLines.Count - 20, 0)
        Parallel.For(lineCount, lines.sortedLines.Count,
            Sub(i)
                find3DLineSegment(dst1, mask, task.depth32f, lines.sortedLines.ElementAt(i).Key, maskLineWidth)
            End Sub)
        label1 = "Showing the " + CStr(Math.Min(lines.sortedLines.Count, 20)) + " longest lines out of " + CStr(lines.sortedLines.Count)
    End Sub
End Class



'Public Class Line_3D_FitLineZ
'    Inherits VBparent
'    Dim linesFLD As line_FLD_CPP
'    Public Sub New()
'        initParent()
'        linesFLD = New line_FLD_CPP()

'        If findfrm(caller + " Slider Options") Is Nothing Then
'            sliders.Setup(caller)
'            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 3)
'            sliders.setupTrackBar(1, "Point count threshold", 5, 500, 50)
'            sliders.setupTrackBar(2, "Update frequency (in frames)", 1, 100, 1)
'        End If
'        If findfrm(caller + " CheckBox Options") Is Nothing Then
'            check.Setup(caller, 2)
'            check.Box(0).Text = "Fitline using x and z (unchecked it will use y and z)"
'            check.Box(1).Text = "Display only the longest line"
'            check.Box(1).Checked = True
'        End If

'        task.desc = "Use Fitline with the sparse Z data and X or Y (in RGB pixels)."
'        label2 = ""
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then task.intermediateObject = Me
'        If task.frameCount Mod sliders.trackbar(2).Value Then Exit Sub
'        Dim useX As Boolean = check.Box(0).Checked
'        linesFLD.src = src
'        linesFLD.Run()
'        src.CopyTo(dst1)

'        Dim sortedlines As SortedList(Of cv.Vec6f, Integer)
'        sortedlines = linesFLD.sortedLines

'        If sortedlines.Count > 0 Then
'            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will likely get full length.
'            Dim maskLineWidth As Integer = sliders.trackbar(0).Value
'            Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)

'            Dim longestLineOnly As Boolean = check.Box(1).Checked
'            Dim pointCountThreshold = sliders.trackbar(1).Value
'            Parallel.For(0, sortedlines.Count,
'                Sub(i)
'                    If longestLineOnly And i < sortedlines.Count - 1 Then Exit Sub
'                    Dim aa = sortedlines.ElementAt(i).Key
'                    Dim pt1 = New cv.Point(aa(0), aa(1))
'                    Dim pt2 = New cv.Point(aa(2), aa(3))
'                    dst1.Line(pt1, pt2, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
'                    mask.Line(pt1, pt2, New cv.Scalar(i), maskLineWidth, cv.LineTypes.AntiAlias)

'                    Dim roi = New cv.Rect(Math.Min(aa(0), aa(2)), Math.Min(aa(1), aa(3)), Math.Abs(aa(0) - aa(2)), Math.Abs(aa(1) - aa(3)))

'                    Dim worldDepth As New List(Of cv.Vec6f)
'                    If roi.Width = 0 Then roi.Width = 1
'                    If roi.Height = 0 Then roi.Height = 1
'                    If roi.X + roi.Width >= mask.Width Then roi.Width = mask.Width - roi.X - 1
'                    If roi.Y + roi.Height >= mask.Height Then roi.Height = mask.Height - roi.Y - 1

'                    Dim _mask = mask(roi).Clone()
'                    Dim points As New List(Of cv.Point2f)
'                    For y = 0 To roi.Height - 1
'                        For x = 0 To roi.Width - 1
'                            If _mask.Get(Of Byte)(y, x) = i Then
'                                Dim w = getWorldCoordinatesD6(New cv.Point3f(x + roi.X, y + roi.Y, task.depth32f.Get(Of Single)(y, x)))
'                                points.Add(New cv.Point(If(useX, w.Item0, w.Item1), w.Item2))
'                                worldDepth.Add(w)
'                            End If
'                        Next
'                    Next

'                    ' without a sufficient number of points, the results can vary widely.
'                    If points.Count < pointCountThreshold Then Exit Sub

'                    Dim line = cv.Cv2.FitLine(points, cv.DistanceTypes.L2, 1, 0.01, 0.01)
'                    Dim mm = line.Vy / line.Vx
'                    Dim bb = line.Y1 - mm * line.X1
'                    Dim endPoints(2) As cv.Vec6f
'                    Dim lastW = worldDepth.Count - 1
'                    endPoints(0) = worldDepth(0)
'                    endPoints(1) = worldDepth(lastW)
'                    endPoints(0).Item2 = bb + mm * If(useX, worldDepth(0).Item3, worldDepth(0).Item4)
'                    endPoints(1).Item2 = bb + mm * If(useX, worldDepth(lastW).Item3, worldDepth(lastW).Item4)

'                    Dim b = endPoints(0)
'                    Dim d = endPoints(1)
'                    Dim lenBD = Math.Sqrt((b.Item0 - d.Item0) * (b.Item0 - d.Item0) + (b.Item1 - d.Item1) * (b.Item1 - d.Item1) + (b.Item2 - d.Item2) * (b.Item2 - d.Item2))

'                    Dim ptIndex = (i / sortedlines.Count) * (worldDepth.Count - 1)
'                    Dim textPoint = New cv.Point(worldDepth(ptIndex).Item3, worldDepth(ptIndex).Item4)
'                    If textPoint.X > mask.Width - 50 Then textPoint.X = mask.Width - 50
'                    If textPoint.Y > mask.Height - 50 Then textPoint.Y = mask.Height - 50
'                    cv.Cv2.PutText(dst1, Format(lenBD / 1000, "#0.00") + "m", textPoint, cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
'                    If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
'                    cv.Cv2.PutText(dst1, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "#0.00") + If(useX, "x/z", "y/z"),
'                                    New cv.Point(textPoint.X, textPoint.Y + 10), cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

'                    ' show the final endpoints in xy projection.
'                    dst1.Circle(New cv.Point(b.Item3, b.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
'                    dst1.Circle(New cv.Point(d.Item3, d.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
'                End Sub)
'        End If
'    End Sub
'End Class




' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class line_FLD
    Inherits VBparent
    Public lines As New List(Of cv.Vec4f)
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 6)
            sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
            sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
            sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
            sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
            sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
            sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "FLD - incremental merge"
            check.Box(0).Checked = True
        End If
        task.desc = "A Fast Line Detector"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        lines.Clear()

        Dim length_threshold = sliders.trackbar(0).Value
        Dim distance_threshold = sliders.trackbar(1).Value / 10
        Dim canny_aperture_size = sliders.trackbar(2).Value
        If canny_aperture_size Mod 2 = 0 Then canny_aperture_size += 1
        Dim canny_th1 = sliders.trackbar(4).Value
        Dim canny_th2 = sliders.trackbar(5).Value
        Dim do_merge = check.Box(0).Checked

        src.CopyTo(dst1)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim cols = src.Width
        Dim rows = src.Height
        Dim data(src.Total - 1) As Byte

        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim lineCount = lineDetectorFast_Run(handle.AddrOfPinnedObject, rows, cols, length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge)
        handle.Free()

        If lineCount > 0 Then
            Dim pts(4 * lineCount - 1) As Single
            Dim linePtr = lineDetector_Lines()
            If linePtr <> 0 Then
                Marshal.Copy(linePtr, pts, 0, pts.Length)
                For i = 0 To lineCount - 1
                    lines.Add(New cv.Vec4f(pts(i), pts(i + 1), pts(i + 2), pts(i + 3)))
                Next
            End If
        End If
        If standalone Or task.intermediateReview = caller Then
            For j = 0 To lines.Count - 1 Step 4
                Dim v = lines(j)
                Dim pt1 = New cv.Point(v(0), v(1))
                dst1.Line(New cv.Point(v(0), v(1)), New cv.Point(v(2), v(3)), cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class







Public Class Line_Reduction
    Inherits VBparent
    Dim lDetect As Line_Basics
    Dim reduction As Reduction_Basics
    Dim stable As IMU_IscameraStable
    Public Sub New()
        initParent()
        stable = New IMU_IscameraStable
        lDetect = New Line_Basics()

        reduction = New Reduction_Basics()
        Dim simpleRadio = findRadio("Use simple reduction")
        simpleRadio.Checked = True

        label1 = "Yellow > length threshold, red < length threshold"
        label2 = "Input image after reduction"
        task.desc = "Use the reduced rgb image as input to the line detector"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        reduction.src = src
        reduction.Run()

        lDetect.src = reduction.dst1
        lDetect.Run()
        dst1 = lDetect.dst1

        stable.Run()
        If stable.cameraStable = False Then dst2.SetTo(0)

        For Each line In lDetect.sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst2.Line(p1, p2, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class







Public Class Line_HighlightSlope
    Inherits VBparent
    Dim lines As Line_Intercepts
    Public Sub New()
        initParent()
        lines = New Line_Intercepts
        label1 = "Use mouse in right image to highlight lines"
        task.desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        lines.src = src
        lines.Run()
        Dim searchRange = lines.searchRange
        dst2.SetTo(0)

        Dim red = New cv.Scalar(0, 0, 255)
        Dim green = New cv.Scalar(1, 128, 0)
        Dim yellow = New cv.Scalar(2, 255, 255)
        Dim blue = New cv.Scalar(254, 0, 0)

        Dim center = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        dst2.Line(New cv.Point(0, 0), center, blue, 1, cv.LineTypes.Link4)
        dst2.Line(New cv.Point(dst1.Width, 0), center, red, 1, cv.LineTypes.Link4)
        dst2.Line(New cv.Point(0, dst1.Height), center, blue, 1, cv.LineTypes.Link4)
        dst2.Line(New cv.Point(dst1.Width, dst1.Height), center, yellow, 1, cv.LineTypes.Link4)

        Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
        Dim pt = New cv.Point(center.X, center.Y - 30)
        cv.Cv2.FloodFill(dst2, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X, center.Y + 30)
        cv.Cv2.FloodFill(dst2, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X - 30, center.Y)
        cv.Cv2.FloodFill(dst2, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X + 30, center.Y)
        cv.Cv2.FloodFill(dst2, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst2.Get(Of cv.Vec3b)(task.mousePoint.Y, task.mousePoint.X)

        Dim p1 = task.mousePoint
        Static p2 As cv.Point
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cv.Point(dst2.Width / 2, 0) Else p2 = New cv.Point(dst2.Width, dst2.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color.Item0 = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
            If color.Item0 = 1 Then p2 = New cv.Point((dst2.Height - b) / m, dst2.Height) ' green
            If color.Item0 = 2 Then p2 = New cv.Point(dst2.Width, dst2.Width * m + b) ' yellow
            If color.Item0 = 254 Then p2 = New cv.Point(0, b) ' blue
            dst2.Line(center, p2, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
        End If
        dst2.Circle(center, task.dotSize, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)


        Static redRadio = findRadio("Show Top intercepts")
        Static greenRadio = findRadio("Show Bottom intercepts")
        Static yellowRadio = findRadio("Show Right intercepts")
        Static blueRadio = findRadio("Show Left intercepts")
        If color.Item0 = 0 Then redRadio.checked = True
        If color.Item0 = 1 Then greenRadio.checked = True
        If color.Item0 = 2 Then yellowRadio.checked = True
        If color.Item0 = 254 Then blueRadio.checked = True

        lines.showIntercepts(p2, dst2)
        dst1 = lines.dst1
    End Sub
End Class







Public Class Line_ConfirmedDepth
    Inherits VBparent
    Dim lines As Line_Basics
    Public pt1 As New List(Of cv.Point2f)
    Public pt2 As New List(Of cv.Point2f)
    Public z1 As New List(Of cv.Point3f) ' the point cloud values corresponding to pt1 and pt2
    Public z2 As New List(Of cv.Point3f)
    Public cloudInput As cv.Mat
    Public Sub New()
        initParent()
        lines = New Line_Basics
        label1 = "Lines defined in RGB"
        label2 = "Lines in RGB confirmed in the point cloud"
        task.desc = "Find the RGB lines and confirm they are present in the cloud data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static thickSlider = findSlider("Line thickness")
        Dim thickness = thickSlider.value
        lines.src = src
        lines.Run()
        dst1 = lines.dst1

        If lines.sortlines.Count = 0 Then Exit Sub
        Dim lineList = New List(Of cv.Rect)
        If cloudInput Is Nothing Then cloudInput = task.pointCloud
        Dim split = cloudInput.Split()
        dst2.SetTo(0)
        pt1.Clear()
        pt2.Clear()
        z1.Clear()
        z2.Clear()
        For Each nl In lines.sortlines
            Dim p1 = New cv.Point2f(nl.Value.Item0, nl.Value.Item1)
            Dim p2 = New cv.Point2f(nl.Value.Item2, nl.Value.Item3)

            Dim minXX = Math.Min(p1.X, p2.X)
            Dim minYY = Math.Min(p1.Y, p2.Y)
            Dim w = Math.Abs(p1.X - p2.X)
            Dim h = Math.Abs(p1.Y - p2.Y)
            Dim r = New cv.Rect(minXX, minYY, If(w > 0, w, 2), If(h > 0, h, 2))
            Dim mask = New cv.Mat(New cv.Size(w, h), cv.MatType.CV_8U, 0)
            mask.Line(New cv.Point(CInt(p1.X - r.X), CInt(p1.Y - r.Y)), New cv.Point(CInt(p2.X - r.X), CInt(p2.Y - r.Y)), 255, thickness, cv.LineTypes.Link4)
            Dim mean = cloudInput(r).Mean(mask)

            If mean <> New cv.Scalar Then
                Dim min As Double, max As Double, Loc(4 - 1) As cv.Point
                cv.Cv2.MinMaxLoc(split(0)(r), min, max, Loc(0), Loc(1), mask)

                cv.Cv2.MinMaxLoc(split(1)(r), min, max, Loc(2), Loc(3), mask)
                Dim len1 = Loc(0).DistanceTo(Loc(1))
                Dim len2 = Loc(2).DistanceTo(Loc(3))
                If len1 > len2 Then
                    p1 = New cv.Point(Loc(0).X + r.X, Loc(0).Y + r.Y)
                    p2 = New cv.Point(Loc(1).X + r.X, Loc(1).Y + r.Y)
                Else
                    p1 = New cv.Point(Loc(2).X + r.X, Loc(2).Y + r.Y)
                    p2 = New cv.Point(Loc(3).X + r.X, Loc(3).Y + r.Y)
                End If
                If p1.DistanceTo(p2) > 1 Then
                    dst2.Line(p1, p2, cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
                    pt1.Add(p1)
                    pt2.Add(p2)
                    z1.Add(cloudInput.Get(Of cv.Point3f)(p1.Y, p1.X))
                    z2.Add(cloudInput.Get(Of cv.Point3f)(p2.Y, p2.X))
                End If
            End If
        Next
    End Sub
End Class









Public Class Line_Vertical
    Inherits VBparent
    Dim gCloud As Depth_PointCloud_IMU
    Public lines As Line_ConfirmedDepth
    Public thickness As Integer
    Public toleranceInMMs As Single
    Public Sub New()
        initParent()
        gCloud = New Depth_PointCloud_IMU
        lines = New Line_ConfirmedDepth


        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Error tolerance when measuring vertical lines in 3D (mm's)", 0, 300, 50)
        End If

        task.desc = "Find all the vertical lines in the IMU rectified cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static thickSlider = findSlider("Line thickness")
        Static errorSlider = findSlider("Error tolerance when measuring vertical lines in 3D (mm's)")
        toleranceInMMs = errorSlider.value / 1000
        thickness = thickSlider.value
        dst1 = src.Clone

        gCloud.Run()
        lines.cloudInput = gCloud.dst1
        lines.src = src
        lines.Run()

        For i = 0 To lines.z1.Count - 1
            Dim p1 = lines.z1(i)
            Dim p2 = lines.z2(i)
            If Math.Abs(p1.X - p2.X) < toleranceInMMs And Math.Abs(p1.Z - p2.Z) < toleranceInMMs Then
                dst1.Line(lines.pt1(i), lines.pt2(i), cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class








Public Class Line_Horizontal
    Inherits VBparent
    Dim vLines As Line_Vertical
    Public Sub New()
        initParent()
        vLines = New Line_Vertical
        task.desc = "Find all the horizontal lines in the IMU rectified cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = src.Clone

        vLines.src = src
        vLines.Run()

        For i = 0 To vLines.lines.z1.Count - 1
            Dim p1 = vLines.lines.z1(i)
            Dim p2 = vLines.lines.z2(i)
            If Math.Abs(p1.Y - p2.Y) < vLines.toleranceInMMs And Math.Abs(p1.Z - p2.Z) < vLines.toleranceInMMs Then
                dst1.Line(vLines.lines.pt1(i), vLines.lines.pt2(i), cv.Scalar.Yellow, vLines.thickness, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class







Public Class Line_Intercepts
    Inherits VBparent
    Dim lines As Line_Basics
    Public pt1 As New List(Of cv.Point2f)
    Public pt2 As New List(Of cv.Point2f)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public searchRange As Integer
    Public thickNess As Integer
    Public Sub New()
        initParent()
        lines = New Line_Basics
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = 1

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "Show Top intercepts"
            radio.check(1).Text = "Show Bottom intercepts"
            radio.check(2).Text = "Show Left intercepts"
            radio.check(3).Text = "Show Right intercepts"
            radio.check(1).Checked = True
        End If

        label1 = "Mouse tracks top, bottom, left, or right intercepts."
        task.desc = "Consolidate RGB lines using the x- and y-intercepts"
    End Sub
    Public Sub hightLightIntercept(mousePoint As Integer, intercepts As SortedList(Of Integer, Integer), axis As Integer, dst As cv.Mat)
        For Each inter In intercepts
            If Math.Abs(mousePoint - inter.Key) < searchRange Then
                dst1.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.White, thickNess + 4, cv.LineTypes.AntiAlias)
                dst1.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.Blue, thickNess, cv.LineTypes.AntiAlias)
            End If
        Next
        For Each inter In intercepts
            Select Case axis
                Case 0
                    dst.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), cv.Scalar.White, task.lineSize)
                Case 1
                    dst.Line(New cv.Point(inter.Key, dst1.Height), New cv.Point(inter.Key, dst1.Height - 10), cv.Scalar.White, task.lineSize)
                Case 2
                    dst.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), cv.Scalar.White, task.lineSize)
                Case 3
                    dst.Line(New cv.Point(dst1.Width, inter.Key), New cv.Point(dst1.Width - 10, inter.Key), cv.Scalar.White, task.lineSize)
            End Select
        Next
    End Sub
    Public Sub showIntercepts(mousePoint As cv.Point, dst As cv.Mat)
        Static topRadio = findRadio("Show Top intercepts")
        Static botRadio = findRadio("Show Bottom intercepts")
        Static leftRadio = findRadio("Show Left intercepts")
        Static rightRadio = findRadio("Show Right intercepts")

        For i = 0 To 3
            Dim radio = Choose(i + 1, topRadio, botRadio, leftRadio, rightRadio)
            Dim intercepts = Choose(i + 1, topIntercepts, botIntercepts, leftIntercepts, rightIntercepts)
            Dim pt = Choose(i + 1, mousePoint.X, mousePoint.X, mousePoint.Y, mousePoint.Y)
            If radio.checked Then hightLightIntercept(pt, intercepts, i, dst)
        Next
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static thickSlider = findSlider("Line thickness")
        Static searchSlider = findSlider("x- and y-intercept search range in pixels")
        thickNess = thickSlider.value
        searchRange = searchSlider.value

        lines.src = src
        lines.Run()
        If lines.sortlines.Count = 0 Then Exit Sub

        dst1 = src
        pt1.Clear()
        pt2.Clear()
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        For i = 0 To lines.sortlines.Count - 1
            Dim nl = lines.sortlines.ElementAt(i).Value
            Dim p1 = New cv.Point2f(nl.Item0, nl.Item1)
            Dim p2 = New cv.Point2f(nl.Item2, nl.Item3)

            Dim minXX = Math.Min(p1.X, p2.X)
            If p1.X <> minXX Then ' leftmost point is always in pt1
                Dim tmp = p1
                p1 = p2
                p2 = tmp
            End If

            pt1.Add(p1)
            pt2.Add(p2)
            dst1.Line(p1, p2, cv.Scalar.Yellow, thickNess, cv.LineTypes.AntiAlias)
            If p1.X = p2.X Then
                topIntercepts.Add(p1.X, i)
                botIntercepts.Add(p1.X, i)
            Else
                Dim m = (p1.Y - p2.Y) / (p1.X - p2.X)
                Dim b = p1.Y - p1.X * m
                If m = 0 Then
                    leftIntercepts.Add(p1.Y, i)
                    rightIntercepts.Add(p1.Y, i)
                Else
                    Dim xint1 = -b / m
                    Dim xint2 = (dst1.Height - b) / m  ' x = (y - b) / m
                    Dim yint1 = b
                    Dim yint2 = m * dst1.Width + b
                    If xint1 >= 0 And xint1 <= dst1.Width Then topIntercepts.Add(xint1, i)
                    If xint2 >= 0 And xint2 <= dst1.Width Then botIntercepts.Add(xint2, i)
                    If yint1 >= 0 And yint1 <= dst1.Height Then leftIntercepts.Add(yint1, i)
                    If yint2 >= 0 And yint2 <= dst1.Height Then rightIntercepts.Add(yint2, i)
                End If
            End If
        Next

        If standalone Then showIntercepts(task.mousePoint, dst1)
    End Sub
End Class








Public Class Line_LeftRightImages
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "description"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
    End Sub
End Class