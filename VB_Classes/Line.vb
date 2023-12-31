Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes
Public Class Line_Basics : Inherits VBparent
    Dim ld As cv.XImgProc.FastLineDetector
    Public sortlines As New SortedList(Of Integer, cv.Vec6f)(New compareAllowIdenticalIntegerInverted)
    Public pt1List As New List(Of cv.Point)
    Public pt2List As New List(Of cv.Point)
    Public slopes As New List(Of Single)
    Public yintercepts As New List(Of Single)
    Public pixelThreshold As Integer
    Public lenSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Line length threshold in pixels", 1, 400, 20)
            sliders.setupTrackBar(1, "Depth search radius in pixels", 1, 20, 2) ' not used in Run below but externally...
            sliders.setupTrackBar(2, "x- and y-intercept search range in pixels", 1, 50, 10) ' not used in Run below but externally...
            sliders.setupTrackBar(3, "Detect lines from the last X frames", 0, 20, 10)
        End If

        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector

        lenSlider = findSlider("Line length threshold in pixels")

        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(2) = "Lines detected in the current frame"
        labels(3) = "Lines detected since camera motion threshold"
        task.desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.Clone
        If dst2.Channels <> 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)
        Dim lines = ld.Detect(src)
        pixelThreshold = lenSlider.Value

        sortlines.Clear()
        pt1List.Clear()
        pt2List.Clear()
        slopes.Clear()
        yintercepts.Clear()

        For Each v In lines
            If v(0) >= 0 And v(0) <= dst2.Cols And v(1) >= 0 And v(1) <= dst2.Rows And
               v(2) >= 0 And v(2) <= dst2.Cols And v(3) >= 0 And v(3) <= dst2.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = pt1.DistanceTo(pt2)
                If pixelLen > pixelThreshold Then
                    dst2.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    pt1List.Add(pt1)
                    pt2List.Add(pt2)
                    slopes.Add(If((pt1.X <> pt2.X), (pt1.Y - pt2.Y) / (pt1.X - pt2.X), verticalSlope))
                    yintercepts.Add(pt1.Y - slopes.ElementAt(slopes.Count - 1) * pt1.X)
                    sortlines.Add(pixelLen, New cv.Vec6f(pt1.X, pt1.Y, pt2.X, pt2.Y, pt1.DistanceTo(pt2), slopes(slopes.Count - 1)))
                End If
            End If
        Next

        If task.cameraStable = False Then dst3.SetTo(0)

        For Each line In sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst3.Line(p1, p2, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class









Public Class Line_Reduction : Inherits VBparent
    Dim lines As New Line_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        findRadio("Use simple reduction").Checked = True

        labels(2) = "Yellow > length threshold, red < length threshold"
        labels(3) = "Input image after reduction"
        task.desc = "Use the reduced rgb image as input to the line detector"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)

        lines.RunClass(reduction.dst2)
        dst2 = lines.dst2

        If task.cameraStable = False Then dst3.SetTo(0)
        For Each line In lines.sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst3.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class Line_InterceptsUI : Inherits VBparent
    Dim lines As New Line_Intercepts
    Public Sub New()
        labels(2) = "Use mouse in right image to highlight lines"
        task.desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static redRadio = findRadio("Show Top intercepts")
        Static greenRadio = findRadio("Show Bottom intercepts")
        Static yellowRadio = findRadio("Show Right intercepts")
        Static blueRadio = findRadio("Show Left intercepts")

        lines.RunClass(src)
        Dim searchRange = lines.searchRange
        dst3.SetTo(0)

        Dim red = New cv.Scalar(0, 0, 255)
        Dim green = New cv.Scalar(1, 128, 0)
        Dim yellow = New cv.Scalar(2, 255, 255)
        Dim blue = New cv.Scalar(254, 0, 0)

        Dim center = New cv.Point(dst3.Width / 2, dst3.Height / 2)
        dst3.Line(New cv.Point(0, 0), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, 0), center, red, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(0, dst2.Height), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, dst2.Height), center, yellow, task.lineWidth, cv.LineTypes.Link4)

        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        Dim pt = New cv.Point(center.X, center.Y - 30)
        cv.Cv2.FloodFill(dst3, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X, center.Y + 30)
        cv.Cv2.FloodFill(dst3, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X - 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X + 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst3.Get(Of cv.Vec3b)(task.mousePoint.Y, task.mousePoint.X)

        Dim p1 = task.mousePoint
        Static p2 As cv.Point
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cv.Point(dst3.Width / 2, 0) Else p2 = New cv.Point(dst3.Width, dst3.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color.Item0 = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
            If color.Item0 = 1 Then p2 = New cv.Point((dst3.Height - b) / m, dst3.Height) ' green
            If color.Item0 = 2 Then p2 = New cv.Point(dst3.Width, dst3.Width * m + b) ' yellow
            If color.Item0 = 254 Then p2 = New cv.Point(0, b) ' blue
            dst3.Line(center, p2, cv.Scalar.Black, task.lineWidth, task.lineType)
        End If
        dst3.Circle(center, task.dotSize, cv.Scalar.White, -1, task.lineType)

        If color.Item0 = 0 Then redRadio.checked = True
        If color.Item0 = 1 Then greenRadio.checked = True
        If color.Item0 = 2 Then yellowRadio.checked = True
        If color.Item0 = 254 Then blueRadio.checked = True

        lines.showIntercepts(p2, dst3)
        dst2 = lines.dst2
    End Sub
End Class









Public Class Line_Vertical : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Public lines As New Line_InDepthAndRGB
    Public toleranceInMMs As Single
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Error tolerance when measuring vertical lines in 3D (mm's)", 0, 300, 50)
        End If

        task.desc = "Find all the vertical lines in the IMU rectified cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static errorSlider = findSlider("Error tolerance when measuring vertical lines in 3D (mm's)")
        toleranceInMMs = errorSlider.value / 1000
        dst2 = src.Clone

        gCloud.RunClass(src)
        lines.cloudInput = gCloud.dst2
        lines.RunClass(src)

        For i = 0 To lines.z1.Count - 1
            Dim p1 = lines.z1(i)
            Dim p2 = lines.z2(i)
            If Math.Abs(p1.X - p2.X) < toleranceInMMs And Math.Abs(p1.Z - p2.Z) < toleranceInMMs Then
                dst2.Line(lines.pt1(i), lines.pt2(i), cv.Scalar.Yellow, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class








Public Class Line_Horizontal : Inherits VBparent
    Dim vLines As New Line_Vertical
    Public Sub New()
        task.desc = "Find all the horizontal lines in the IMU rectified cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.Clone

        vLines.RunClass(src)

        For i = 0 To vLines.lines.z1.Count - 1
            Dim p1 = vLines.lines.z1(i)
            Dim p2 = vLines.lines.z2(i)
            If Math.Abs(p1.Y - p2.Y) < vLines.toleranceInMMs And Math.Abs(p1.Z - p2.Z) < vLines.toleranceInMMs Then
                dst2.Line(vLines.lines.pt1(i), vLines.lines.pt2(i), cv.Scalar.Yellow, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class







Public Class Line_Intercepts : Inherits VBparent
    Dim lines As New Line_Basics
    Public pt1 As New List(Of cv.Point2f)
    Public pt2 As New List(Of cv.Point2f)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public searchRange As Integer
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1

        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "Show Top intercepts"
            radio.check(1).Text = "Show Bottom intercepts"
            radio.check(2).Text = "Show Left intercepts"
            radio.check(3).Text = "Show Right intercepts"
            radio.check(1).Checked = True
        End If

        labels(2) = "Mouse tracks top, bottom, left, or right intercepts."
        task.desc = "Consolidate RGB lines using the x- and y-intercepts"
    End Sub
    Public Sub hightLightIntercept(mousePoint As Integer, intercepts As SortedList(Of Integer, Integer), axis As Integer, dst As cv.Mat)
        For Each inter In intercepts
            If Math.Abs(mousePoint - inter.Key) < searchRange Then
                dst2.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.White, task.lineWidth + 4, task.lineType)
                dst2.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.Blue, task.lineWidth, task.lineType)
            End If
        Next
        For Each inter In intercepts
            Select Case axis
                Case 0
                    dst.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), cv.Scalar.White, task.lineWidth)
                Case 1
                    dst.Line(New cv.Point(inter.Key, dst2.Height), New cv.Point(inter.Key, dst2.Height - 10), cv.Scalar.White, task.lineWidth)
                Case 2
                    dst.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), cv.Scalar.White, task.lineWidth)
                Case 3
                    dst.Line(New cv.Point(dst2.Width, inter.Key), New cv.Point(dst2.Width - 10, inter.Key), cv.Scalar.White, task.lineWidth)
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
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static searchSlider = findSlider("x- and y-intercept search range in pixels")
        searchRange = searchSlider.value

        lines.RunClass(src)
        If lines.sortlines.Count = 0 Then Exit Sub

        dst2 = src
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
            dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
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
                    Dim xint2 = (dst2.Height - b) / m  ' x = (y - b) / m
                    Dim yint1 = b
                    Dim yint2 = m * dst2.Width + b
                    If xint1 >= 0 And xint1 <= dst2.Width Then topIntercepts.Add(xint1, i)
                    If xint2 >= 0 And xint2 <= dst2.Width Then botIntercepts.Add(xint2, i)
                    If yint1 >= 0 And yint1 <= dst2.Height Then leftIntercepts.Add(yint1, i)
                    If yint2 >= 0 And yint2 <= dst2.Height Then rightIntercepts.Add(yint2, i)
                End If
            End If
        Next

        If standalone Then showIntercepts(task.mousePoint, dst2)
    End Sub
End Class








Public Class Line_NearestPoint : Inherits VBparent
    Dim rangeRect As cv.Rect
    Public pt As cv.Point2f
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public Sub New()
        Dim offset = 20
        rangeRect = New cv.Rect(offset, offset, dst2.Width - offset * 2, dst2.Height - offset * 2)
        task.desc = "Demonstrate computing the distance from a point to a line"
    End Sub
    Private Function getPoint() As cv.Point2f
        Dim x = msRNG.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
        Dim y = msRNG.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)

        Return New cv.Point2f(x, y)
    End Function
    Public Function findNearestPt(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As cv.Point2f
        Dim nearest As cv.Point2f
        Dim minX = Math.Min(p1.X, p2.X)
        Dim minY = Math.Min(p1.Y, p2.Y)
        Dim maxX = Math.Max(p1.X, p2.X)
        Dim maxY = Math.Max(p1.Y, p2.Y)

        Dim onTheLine = True
        If p1.X = p2.X Then
            nearest = New cv.Point2f(p1.X, pt.Y)
            If pt.Y < minY Or pt.Y > maxY Then onTheLine = False
        Else
            Dim m = (p1.Y - p2.Y) / (p1.X - p2.X)
            If m = 0 Then
                nearest = New cv.Point2f(pt.X, p1.Y)
                If pt.X < minX Or pt.X > maxX Then onTheLine = False
            Else
                Dim b1 = p1.Y - p1.X * m

                Dim b2 = pt.Y + pt.X / m
                Dim a1 = New cv.Point2f(0, b2)
                Dim a2 = New cv.Point2f(dst2.Width, b2 + dst2.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearest = New cv.Point2f(x, m * x + b1)

                If nearest.X < minX Or nearest.X > maxX Or nearest.Y < minY Or nearest.Y > maxY Then onTheLine = False
            End If
        End If

        If onTheLine = False Then nearest = If(pt.DistanceTo(p1) < pt.DistanceTo(p2), p1, p2)
        Return nearest
    End Function
    Public Function findDistance(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As Single
        Dim nearest = findNearestPt(p1, p2, pt)
        Return nearest.DistanceTo(pt)
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod 30 = 0 And standalone Then
            pt = getPoint()
            p1 = getPoint()
            p2 = getPoint()
        End If
        Dim nearest = findNearestPt(p1, p2, pt)
        dst2.SetTo(0)
        dst2.Circle(New cv.Point2f(pt.X, pt.Y), task.dotSize, cv.Scalar.White, -1, task.lineType)
        dst2.Line(New cv.Point2f(p1.X, p1.Y), New cv.Point2f(p2.X, p2.Y), cv.Scalar.Yellow, task.lineWidth, task.lineType)
        dst2.Line(pt, nearest, cv.Scalar.White, task.lineWidth, task.lineType)
        labels(2) = "nearest point = (" + CStr(nearest.X) + "," + CStr(nearest.Y) + ")"
    End Sub
End Class








Public Class Line_SideView : Inherits VBparent
    Dim lines As New Line_Basics
    Dim tView As New TimeView_FloodFill
    Public Sub New()
        labels(2) = "Side view of the lines detected in the RGB image"
        labels(3) = "Lines found in the RGB image view"
        task.desc = "Line in image are projected into the depth image - not yet complete..."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src)
        dst3 = lines.dst3

        Dim mask = dst3
        ' tView.src = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        'task.pointCloud.CopyTo(tView.src, mask)
        tView.RunClass(task.pointCloud)
        dst2 = tView.dst2
    End Sub
End Class











Public Class Line_LeftRightImages : Inherits VBparent
    Dim lrPalette As New Palette_LeftRightImages
    Public leftLines As New Line_TimeView
    Public rightLines As New Line_TimeView
    Public rgbLines As New Line_TimeView
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Show lines from RGB in green"
        End If

        findSlider("Line length threshold in pixels").Value = 30
        labels(2) = "Left image lines(red) with Right(blue)"
        task.desc = "Find lines in the infrared images and overlay them in a single image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static rgbCheck = findCheckBox("Show lines from RGB in green")
        lrPalette.RunClass(src)

        If task.cameraStable = False Then dst2.SetTo(cv.Scalar.White)

        leftLines.RunClass(lrPalette.dst2)
        dst2.SetTo(cv.Scalar.White)
        dst2.SetTo(cv.Scalar.Red, leftLines.dst3)

        rightLines.RunClass(lrPalette.dst3)
        dst2.SetTo(cv.Scalar.Blue, rightLines.dst3)

        If rgbCheck.checked Then
            rgbLines.RunClass(src)
            dst2.SetTo(cv.Scalar.Green, rgbLines.dst3)
        End If
    End Sub
End Class








Public Class Line_RegionsVB : Inherits VBparent
    Dim lines As New Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        findRadio("Use bitwise reduction").Checked = True
        findSlider("Bits to remove in bitwise reduction").Value = 6

        If check.Setup(caller, 2) Then
            check.Box(0).Text = "Show intermediate vertical step results."
            check.Box(1).Text = "Run horizontal without vertical step"
        End If

        task.desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static verticalCheck = findCheckBox("Show intermediate vertical step results")
        Static noVertCheck = findCheckBox("Run horizontal without vertical step")
        reduction.RunClass(src)
        dst2 = reduction.dst2
        dst3 = dst2.Clone

        lines.RunClass(src)

        Dim lineMask = lines.dst3
        dst2.SetTo(lineMatch, lineMask)
        dst3.SetTo(lineMatch, lineMask)

        Dim nextB As Byte
        Dim region As Integer = -1
        Dim indexer1 = dst2.GetGenericIndexer(Of Byte)()
        Dim indexer2 = dst3.GetGenericIndexer(Of Byte)()
        If noVertCheck.checked = False Then
            For x = 0 To dst2.Width - 1
                region = -1
                For y = 0 To dst2.Height - 1
                    nextB = indexer1(y, x)
                    If nextB = lineMatch Then
                        region = -1
                    Else
                        If region = -1 Then
                            region = nextB
                        Else
                            indexer1(y, x) = region
                        End If
                    End If
                Next
            Next
        End If

        For y = 0 To dst3.Height - 1
            region = -1
            For x = 0 To dst3.Width - 1
                nextB = indexer2(y, x)
                If nextB = lineMatch Then
                    region = -1
                Else
                    If region = -1 Then
                        If y = 0 Then
                            region = indexer1(y, x)
                        Else
                            Dim vals As New List(Of Integer)
                            Dim counts As New List(Of Integer)
                            For i = x To dst3.Width - 1
                                Dim nextVal = indexer1(y - 1, i)
                                If nextVal = lineMatch Then Exit For
                                If vals.Contains(nextVal) Then
                                    counts(vals.IndexOf(nextVal)) += 1
                                Else
                                    vals.Add(nextVal)
                                    counts.Add(1)
                                End If
                                Dim maxVal = counts.Max
                                region = vals(counts.IndexOf(maxVal))
                            Next
                        End If
                    Else
                        indexer2(y, x) = region
                    End If
                End If
            Next
        Next
        labels(2) = If(verticalCheck.checked, "Intermediate result of vertical step", "Lines detected (below) Regions detected (right image)")
        If noVertCheck.checked And verticalCheck.checked Then labels(2) = "Input to vertical step"
        If verticalCheck.checked = False Then dst2 = lines.dst2.Clone
    End Sub
End Class







Public Class Line_LUT : Inherits VBparent
    Dim lines As New Line_Basics
    Dim lut As New LUT_Basics
    Public Sub New()
        task.desc = "Compare the lines produced with the RGB and those produced after LUT"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src)
        dst2 = lines.dst2

        lut.RunClass(src)
        lines.RunClass(lut.dst2)
        dst3 = lines.dst2
    End Sub
End Class







Public Class Line_Longest : Inherits VBparent
    Dim lines As New Line_TimeViewLines
    Dim plot As New Plot_OverTime
    Public Sub New()
        plot.plotCount = 1
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "Index of line (sorted by length", 0, 100, 0)
        task.desc = "Find the longest line in RGB and use it to validate depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src.Clone)
        dst2 = src
        If lines.pt1List.Count > 0 Then
            Static indexSlider = findSlider("Index of line (sorted by length")
            If indexSlider.value >= lines.pt1List.Count Then indexSlider.value = 0
            indexSlider.maximum = lines.pt1List.Count
            Dim pt1 = lines.pt1List(indexSlider.value)
            Dim pt2 = lines.pt2list(indexSlider.value)

            dst2.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)

            Dim sq = 3
            Dim pt = pt1
            If pt1.X = pt2.X Then
                If pt1.Y > pt2.Y Then pt = pt2
            Else
                Dim slope = -(pt1.Y - pt2.Y) / (pt1.X - pt2.X)
                If slope < 0 Then
                    pt = If(pt1.X < pt2.X, pt1, pt2)
                Else
                    pt = If(pt1.Y > pt2.Y, pt1, pt2)
                End If
            End If
            Dim maskRect = New cv.Rect(Math.Max(pt.X, 0), Math.Max(pt.Y, 0), Math.Abs(pt1.X - pt2.X) + 2 * sq, Math.Abs(pt1.Y - pt2.Y) + 2 * sq)
            If maskRect.X + maskRect.Width >= dst2.Width Then maskRect.Width = dst2.Width - maskRect.X
            If maskRect.Y + maskRect.Height >= dst2.Height Then maskRect.Height = dst2.Height - maskRect.Y
            Dim lineMask = dst2(maskRect).InRange(cv.Scalar.Yellow, cv.Scalar.Yellow)

            src.Rectangle(maskRect, cv.Scalar.White, 1)

            Dim depth = task.depth32f(maskRect).Mean(lineMask).Item(0) / 1000
            Static lastXvalues As New List(Of Single)
            lastXvalues.Add(depth)
            Dim meanVal = lastXvalues.Average()
            If lastXvalues.Count > 50 Then lastXvalues.RemoveAt(0)

            setTrueText("Average Depth = " + Format(meanVal, "#0.0") + "m", (pt1.X + pt2.X) / 2 + 30, (pt1.Y + pt2.Y) / 2)

            labels(3) = "Mean (horizontal line) = " + Format(meanVal, "#0.0") + "m with " + CStr(lastXvalues.Count) + " samples."

            plot.minScale = 0
            plot.maxScale = task.maxZ
            plot.plotData = New cv.Scalar(meanVal, 0, 0)
            plot.RunClass(Nothing)
            dst3 = plot.dst2.Clone

            Dim yMean = (1 - meanVal / plot.maxScale) * dst2.Height
            dst3.Line(New cv.Point(0, yMean), New cv.Point(dst3.Width, yMean), cv.Scalar.Black, task.lineWidth)
        End If
    End Sub
End Class









Public Class Line_TimeViewLines : Inherits VBparent
    Dim lines As New Line_TimeView
    Dim basics As New Line_Basics
    Public pt1List As New List(Of cv.Point)
    Public pt2list As New List(Of cv.Point)
    Public Sub New()
        labels(2) = "Lines from the latest Line_TimeLine"
        labels(3) = "Lines (green) Vertical (blue) Horizontal (Red)"
        task.desc = "Find slope and y-intercept of lines over time."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src)
        If lines.pixelcount = 0 Then Exit Sub
        basics.RunClass(lines.dst3)

        Dim sortlines As New SortedList(Of Single, cv.Vec6f)(New compareAllowIdenticalSingleInverted)
        pt1List.Clear()
        pt2list.Clear()

        For i = 0 To basics.slopes.Count - 1
            Dim pt1 = basics.pt1List(i)
            Dim pt2 = basics.pt2List(i)
            sortlines.Add(pt1.DistanceTo(pt2), New cv.Vec6f(pt1.X, pt1.Y, pt2.X, pt2.Y, basics.yintercepts(i), basics.slopes(i)))
        Next

        dst2 = lines.dst3
        dst3.SetTo(cv.Scalar.White)
        Dim index = lines.lineIndex
        For Each sl In sortlines
            Dim v = sl.Value
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst3.Line(pt1, pt2, cv.Scalar.Green, task.lineWidth, task.lineType)
            pt1List.Add(pt1)
            pt2list.Add(pt2)
            If v.Item5 = verticalSlope Then
                dst3.Line(pt1, pt2, cv.Scalar.Blue, task.lineWidth + task.lineWidth, task.lineType)
            Else
                If v.Item5 = 0 Then
                    dst3.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + task.lineWidth, task.lineType)
                End If
            End If
        Next
    End Sub
End Class







Public Class Line_TimeView : Inherits VBparent
    Public ptList1() As List(Of cv.Point)
    Public ptList2() As List(Of cv.Point)
    Public lineIndex As Integer = -1
    Dim lines As New Line_Basics
    Public pixelcount As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        task.desc = "Collect lines over time"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frameSlider = findSlider("Detect lines from the last X frames")
        Static lineCount As Integer

        lines.RunClass(src)

        If lineCount <> frameSlider.value Then
            lineCount = frameSlider.value
            ReDim ptList1(lineCount - 1)
            ReDim ptList2(lineCount - 1)
            lineIndex = lineCount
        End If

        lineIndex += 1
        If lineIndex >= lineCount Then lineIndex = 0

        ptList1(lineIndex) = New List(Of cv.Point)(lines.pt1List)
        ptList2(lineIndex) = New List(Of cv.Point)(lines.pt2List)

        dst2 = src
        dst3.SetTo(0)
        Dim lineTotal As Integer
        For i = 0 To ptList1.Count - 1
            If ptList1(i) IsNot Nothing Then
                lineTotal += ptList1(i).Count
                For j = 0 To ptList1(i).Count - 1
                    dst2.Line(ptList1(i)(j), ptList2(i)(j), cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    dst3.Line(ptList1(i)(j), ptList2(i)(j), cv.Scalar.White, task.lineWidth, task.lineType)
                Next
            End If
        Next

        pixelcount = dst3.CountNonZero()
        labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelCount / 1000, "#.0") + "k pixels"
    End Sub
End Class







Public Class Line_InDepthAndRGB : Inherits VBparent
    Dim lines As New Line_Basics
    Public pt1 As New List(Of cv.Point2f)
    Public pt2 As New List(Of cv.Point2f)
    Public z1 As New List(Of cv.Point3f) ' the point cloud values corresponding to pt1 and pt2
    Public z2 As New List(Of cv.Point3f)
    Public cloudInput As cv.Mat
    Public Sub New()
        labels(2) = "Lines defined in RGB"
        labels(3) = "Lines in RGB confirmed in the point cloud"
        task.desc = "Find the RGB lines and confirm they are present in the cloud data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src)
        dst2 = lines.dst2

        If lines.sortlines.Count = 0 Then Exit Sub
        Dim lineList = New List(Of cv.Rect)
        If cloudInput Is Nothing Then cloudInput = task.pointCloud
        Dim split = cloudInput.Split()
        If task.cameraStable = False Then dst3.SetTo(0)
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
            mask.Line(New cv.Point(CInt(p1.X - r.X), CInt(p1.Y - r.Y)), New cv.Point(CInt(p2.X - r.X), CInt(p2.Y - r.Y)), 255, task.lineWidth, cv.LineTypes.Link4)
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
                    dst3.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    pt1.Add(p1)
                    pt2.Add(p2)
                    z1.Add(cloudInput.Get(Of cv.Point3f)(p1.Y, p1.X))
                    z2.Add(cloudInput.Get(Of cv.Point3f)(p2.Y, p2.X))
                End If
            End If
        Next
    End Sub
End Class






Public Class Line_DupDepthOptions : Inherits VBparent
    Public lines As New Line_Basics
    Public addw As New AddWeighted_Basics
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1
        task.desc = "Options for the Line_DupDepth algorithms."
    End Sub
    Public Function drawLinesV() As cv.Mat
        Dim latest As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To lines.pt1List.Count - 1
            Dim pt1 = lines.pt1List(i)
            Dim pt2 = lines.pt2List(i)
            If pt1.X = pt2.X Then latest.Line(pt1, pt2, cv.Scalar.White, task.lineWidth, cv.LineTypes.Link4)
        Next
        Return latest
    End Function
    Public Function drawLinesH() As cv.Mat
        Dim latest As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To lines.pt1List.Count - 1
            Dim pt1 = lines.pt1List(i)
            Dim pt2 = lines.pt2List(i)
            If pt1.Y = pt2.Y Then latest.Line(pt1, pt2, cv.Scalar.White, task.lineWidth, cv.LineTypes.Link4)
        Next
        Return latest
    End Function
    Public Function avgRect() As String
        Static means As New List(Of Single)
        Static saveDrawRect = task.drawRect
        If saveDrawRect <> task.drawRect Then
            saveDrawRect = task.drawRect
            means.Clear()
        End If
        Dim mean = task.depth32f(task.drawRect).Mean(task.depthMask(task.drawRect))
        Dim nextVal = mean.Item(0) / 1000
        means.Add(nextVal)
        Dim meanCount = 100
        If means.Count > meanCount Then means.RemoveAt(0)
        Dim avg = means.Average()
        Static meanMin = avg, meanMax = avg
        If means.Count = 1 Then
            meanMin = avg
            meanMax = avg
        End If
        If meanMin > avg Then meanMin = avg
        If meanMax < avg Then meanMax = avg
        Return "Average (" + CStr(meanCount) + " frames)=" + Format(avg, "#.000") + " Min=" + Format(meanMin, "#0.000") + " Max=" + Format(meanMax, "#.000")
    End Function
    Public Function showDepthData()
        Dim str As String = ""
        str += "x = " + CStr(task.drawRect.X) + " y = " + CStr(task.drawRect.Y) + vbCrLf
        Dim w = task.drawRect.Width
        Dim h = task.drawRect.Height
        If w = 0 Or w > screenDWidth Then w = screenDWidth ' standard screen amount...
        If h = 0 Or h > screenDHeight Then h = screenDHeight ' standard screen amount...
        For y = task.drawRect.Y To Math.Min(dst2.Height, task.drawRect.Y + h) - 1
            For x = task.drawRect.X To Math.Min(dst2.Width, task.drawRect.X + w) - 1
                str += Format(task.depth32f.Get(Of Single)(y, x), "0000") + " "
            Next
            str += vbCrLf
        Next
        Return str
    End Function
    Public Function getCloudData() As List(Of cv.Vec3f)
        Dim vals As New List(Of cv.Vec3f)
        For y = task.drawRect.Y To task.drawRect.Y + task.drawRect.Height - 1
            For x = task.drawRect.X To task.drawRect.X + task.drawRect.Width - 1
                vals.Add(task.pointCloud.Get(Of cv.Vec3f)(y, x))
            Next
        Next
        Return vals
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        setTrueText("Line_DupDepthOptions has no output - just consolidates all the options and functions needed for Line_DupDepth algorithms.")
    End Sub
End Class






Public Class Line_DupDepthV : Inherits VBparent
    Public dOptions As New Line_DupDepthOptions
    Dim cloud As New PointCloud_NeighborV
    Public Sub New()
        labels(2) = "Move mouse over the image to see the depth data"
        labels(3) = "Draw a rectangle around lines to get stats"
        task.desc = "Detect lines in the PointCloud_NeighborV output where linear patterns show where duplicate depth values are neighbors."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        cloud.RunClass(src)

        dOptions.lines.RunClass(cloud.dst2)
        dOptions.addw.src2 = dOptions.drawLinesV()

        dOptions.addw.RunClass(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = dOptions.addw.dst2

        setTrueText(dOptions.showDepthData(), 10, 40, 3)
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then labels(3) = dOptions.avgRect()
    End Sub
End Class







Public Class Line_DupDepthH : Inherits VBparent
    Public dOptions As New Line_DupDepthOptions
    Dim cloud As New PointCloud_NeighborH
    Public Sub New()
        labels(2) = "Move mouse over the image to see the depth data"
        labels(3) = "Draw a rectangle around lines to get stats"
        task.desc = "Detect lines in the PointCloud_NeighborH output where linear patterns show where duplicate depth values are neighbors."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        cloud.RunClass(src)

        dOptions.lines.RunClass(cloud.dst2)
        dOptions.addw.src2 = dOptions.drawLinesH()

        dOptions.addw.RunClass(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = dOptions.addw.dst2

        setTrueText(dOptions.showDepthData(), 10, 40, 3)
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then labels(3) = dOptions.avgRect()
    End Sub
End Class







Public Class Line_DupDepth : Inherits VBparent
    Dim dupH As New Line_DupDepthH
    Dim dupV As New Line_DupDepthV
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(2) = "OR dupDephH and DupDepthV"
        task.desc = "Merge the horizontal and vertical lines with duplicate depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dupH.RunClass(src)
        dupV.RunClass(src)
        task.ttTextData.Clear()
        cv.Cv2.BitwiseOr(dupH.dOptions.addw.src2, dupV.dOptions.addw.src2, dst2)
    End Sub
End Class







Public Class Line_DupLongestH : Inherits VBparent
    Dim dupH As New Line_DupDepthH
    Public longestP1 As cv.Point
    Public longestP2 As cv.Point
    Dim longestLen As Integer
    Public Sub New()
        labels(2) = "Longest line is highlighted.  dst3 shows values"
        task.desc = "Use the depth along the longest line in Line_DupDepthH to find line in 3D."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dupH.RunClass(src)
        dst2 = dupH.dst2

        Dim latest As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        longestLen = 0
        For i = 0 To dupH.dOptions.lines.pt1List.Count - 1
            Dim pt1 = dupH.dOptions.lines.pt1List(i)
            Dim pt2 = dupH.dOptions.lines.pt2List(i)
            Dim len = Math.Abs(pt1.X - pt2.X)
            If len > longestLen Then
                Dim val1 = task.depth32f.Get(Of Single)(pt1.Y, pt1.X)
                If val1 > 0 Then
                    longestP1 = If(pt1.X < pt2.X, pt1, pt2)
                    longestP2 = If(pt1.X > pt2.X, pt1, pt2)
                    longestLen = len
                End If
            End If
        Next

        task.ttTextData.Clear()

        task.drawRect = New cv.Rect(longestP1.X, longestP1.Y, longestP2.X - longestP1.X, 1)
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            dst2.Rectangle(task.drawRect, cv.Scalar.White, task.lineWidth + 2)

            setTrueText(dupH.dOptions.showDepthData(), 10, 40, 3)
            labels(3) = dupH.dOptions.avgRect()

            Dim lineData = dupH.dOptions.getCloudData()
            Dim lineMat = New cv.Mat(lineData.Count, 1, cv.MatType.CV_32FC3, lineData.ToArray())
            Dim meanVec = lineMat.Mean()
            Dim split = lineMat.Split()
            For i = 0 To 3 - 1
                split(i).MinMaxLoc(minVal, maxVal)
                Dim prefix = Choose(i + 1, "X", "Y", "Z")
                setTrueText(prefix + " mean = " + Format(meanVec.Item(i) * 1000, "0000") + " minVal = " + Format(minVal * 1000, "0000") + " maxVal = " + Format(maxVal * 1000, "0000"), 10, 100 + i * 25, 3)
            Next
        End If
    End Sub
End Class






Public Class Line_KMeans : Inherits VBparent
    Dim km As New KMeans_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        task.desc = "Detect lines in the KMeans output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.RunClass(src)
        dst2 = km.dst2
        lines.RunClass(km.dst2)
        dst3 = lines.dst2
    End Sub
End Class





'Public Class Line_KMeansFuzzy : Inherits VBparent
'    Dim km As New KMeans_Fuzzy
'    Dim lines As New Line_Basics
'    Public Sub New()
'        task.desc = "Detect lines in the KMeans fuzzy output"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        km.RunClass(src)
'        dst2 = km.dst2
'        lines.RunClass(km.dst3)
'        dst3 = lines.dst2
'    End Sub
'End Class