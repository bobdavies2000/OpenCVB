Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes
Public Class Line_Basics : Inherits VBparent
    Dim ld As cv.XImgProc.FastLineDetector
    Public sortlines As New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
    Public pt1List As New List(Of cv.Point)
    Public pt2List As New List(Of cv.Point)
    Public thickness As Integer
    Public pixelThreshold As Integer
    Public lenSlider As Windows.Forms.TrackBar
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 5)
            sliders.setupTrackBar(0, "Line thickness", 1, 20, 2)
            sliders.setupTrackBar(1, "Line length threshold in pixels", 1, dst1.Width + dst1.Height, 40)
            sliders.setupTrackBar(2, "Depth search radius in pixels", 1, 20, 2) ' not used in Run below but externally...
            sliders.setupTrackBar(3, "x- and y-intercept search range in pixels", 1, 50, 10) ' not used in Run below but externally...
            sliders.setupTrackBar(4, "Detect lines from the last X frames", 0, 20, 10)
        End If

        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector

        lenSlider = findSlider("Line length threshold in pixels")

        label1 = "Lines detected in the current frame"
        label2 = "Lines detected since camera motion threshold"
        task.desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static thicknessSlider = findSlider("Line thickness")
        dst1 = src.Clone
        If dst1.Channels <> 3 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim lines = ld.Detect(src)
        thickness = thicknessSlider.Value
        pixelThreshold = lenSlider.Value

        sortlines.Clear()
        pt1List.Clear()
        pt2List.Clear()

        For Each v In lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
               v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = pt1.DistanceTo(pt2)
                If pixelLen > pixelThreshold Then
                    dst1.Line(pt1, pt2, cv.Scalar.Yellow, thickness, task.lineType)
                    pt1List.Add(pt1)
                    pt2List.Add(pt2)
                    sortlines.Add(pixelLen, New cv.Vec4f(pt1.X, pt1.Y, pt2.X, pt2.Y))
                End If
            End If
        Next

        If task.cameraStable = False Then dst2.SetTo(0)

        For Each line In sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst2.Line(p1, p2, cv.Scalar.Yellow, 2, task.lineType)
        Next
    End Sub
End Class









Public Class Line_Reduction : Inherits VBparent
    Dim lDetect As New Line_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        findRadio("Use simple reduction").Checked = True

        label1 = "Yellow > length threshold, red < length threshold"
        label2 = "Input image after reduction"
        task.desc = "Use the reduced rgb image as input to the line detector"
    End Sub
    Public Sub Run(src As cv.Mat)
        reduction.Run(src)

        lDetect.Run(reduction.dst1)
        dst1 = lDetect.dst1

        If task.cameraStable = False Then dst2.SetTo(0)
        For Each line In lDetect.sortlines
            Dim p1 = New cv.Point(line.Value.Item0, line.Value.Item1)
            Dim p2 = New cv.Point(line.Value.Item2, line.Value.Item3)
            dst2.Line(p1, p2, cv.Scalar.Yellow, 1, task.lineType)
        Next
    End Sub
End Class







Public Class Line_InterceptsUI : Inherits VBparent
    Dim lines As New Line_Intercepts
    Public Sub New()
        label1 = "Use mouse in right image to highlight lines"
        task.desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static redRadio = findRadio("Show Top intercepts")
        Static greenRadio = findRadio("Show Bottom intercepts")
        Static yellowRadio = findRadio("Show Right intercepts")
        Static blueRadio = findRadio("Show Left intercepts")

        lines.Run(src)
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
            dst2.Line(center, p2, cv.Scalar.Black, 1, task.lineType)
        End If
        dst2.Circle(center, task.dotSize, cv.Scalar.White, -1, task.lineType)

        If color.Item0 = 0 Then redRadio.checked = True
        If color.Item0 = 1 Then greenRadio.checked = True
        If color.Item0 = 2 Then yellowRadio.checked = True
        If color.Item0 = 254 Then blueRadio.checked = True

        lines.showIntercepts(p2, dst2)
        dst1 = lines.dst1
    End Sub
End Class







Public Class Line_ConfirmedDepth : Inherits VBparent
    Dim lines As New Line_Basics
    Public pt1 As New List(Of cv.Point2f)
    Public pt2 As New List(Of cv.Point2f)
    Public z1 As New List(Of cv.Point3f) ' the point cloud values corresponding to pt1 and pt2
    Public z2 As New List(Of cv.Point3f)
    Public cloudInput As cv.Mat
    Public Sub New()
        label1 = "Lines defined in RGB"
        label2 = "Lines in RGB confirmed in the point cloud"
        task.desc = "Find the RGB lines and confirm they are present in the cloud data."
    End Sub
    Public Sub Run(src As cv.Mat)
        Static thickSlider = findSlider("Line thickness")
        Dim thickness = thickSlider.value

        lines.Run(src)
        dst1 = lines.dst1

        If lines.sortlines.Count = 0 Then Exit Sub
        Dim lineList = New List(Of cv.Rect)
        If cloudInput Is Nothing Then cloudInput = task.pointCloud
        Dim split = cloudInput.Split()
        If task.cameraStable = False Then dst2.SetTo(0)
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
                    dst2.Line(p1, p2, cv.Scalar.Yellow, thickness, task.lineType)
                    pt1.Add(p1)
                    pt2.Add(p2)
                    z1.Add(cloudInput.Get(Of cv.Point3f)(p1.Y, p1.X))
                    z2.Add(cloudInput.Get(Of cv.Point3f)(p2.Y, p2.X))
                End If
            End If
        Next
    End Sub
End Class









Public Class Line_Vertical : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Public lines As New Line_ConfirmedDepth
    Public thickness As Integer
    Public toleranceInMMs As Single
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Error tolerance when measuring vertical lines in 3D (mm's)", 0, 300, 50)
        End If

        task.desc = "Find all the vertical lines in the IMU rectified cloud"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static thickSlider = findSlider("Line thickness")
        Static errorSlider = findSlider("Error tolerance when measuring vertical lines in 3D (mm's)")
        toleranceInMMs = errorSlider.value / 1000
        thickness = thickSlider.value
        dst1 = src.Clone

        gCloud.Run(src)
        lines.cloudInput = gCloud.dst1
        lines.Run(src)

        For i = 0 To lines.z1.Count - 1
            Dim p1 = lines.z1(i)
            Dim p2 = lines.z2(i)
            If Math.Abs(p1.X - p2.X) < toleranceInMMs And Math.Abs(p1.Z - p2.Z) < toleranceInMMs Then
                dst1.Line(lines.pt1(i), lines.pt2(i), cv.Scalar.Yellow, thickness, task.lineType)
            End If
        Next
    End Sub
End Class








Public Class Line_Horizontal : Inherits VBparent
    Dim vLines As New Line_Vertical
    Public Sub New()
        task.desc = "Find all the horizontal lines in the IMU rectified cloud"
    End Sub
    Public Sub Run(src As cv.Mat)
        dst1 = src.Clone

        vLines.Run(src)

        For i = 0 To vLines.lines.z1.Count - 1
            Dim p1 = vLines.lines.z1(i)
            Dim p2 = vLines.lines.z2(i)
            If Math.Abs(p1.Y - p2.Y) < vLines.toleranceInMMs And Math.Abs(p1.Z - p2.Z) < vLines.toleranceInMMs Then
                dst1.Line(vLines.lines.pt1(i), vLines.lines.pt2(i), cv.Scalar.Yellow, vLines.thickness, task.lineType)
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
    Public thickNess As Integer
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1

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
                dst1.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.White, thickNess + 4, task.lineType)
                dst1.Line(pt1(inter.Value), pt2(inter.Value), cv.Scalar.Blue, thickNess, task.lineType)
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
    Public Sub Run(src As cv.Mat)
        Static thickSlider = findSlider("Line thickness")
        Static searchSlider = findSlider("x- and y-intercept search range in pixels")
        thickNess = thickSlider.value
        searchRange = searchSlider.value

        lines.Run(src)
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
            dst1.Line(p1, p2, cv.Scalar.Yellow, thickNess, task.lineType)
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








Public Class Line_Sift_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim siftCS As New CS_SiftBasics
    Dim siftBasics As Sift_Basics
    Dim lrView As New Line_LeftRightImages
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Maximum = task.color.Cols * 2
        gridWidthSlider.Value = task.color.Cols * 2 ' we are just taking horizontal slices of the image.
        gridHeightSlider.Value = 10

        grid.Run(Nothing)

        siftBasics = New Sift_Basics
        findRadio("Use Flann Matcher").Enabled = False

        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        label1 = "Left image - lines connect SIFT dots"
        label2 = "Right image - note inaccurate results"
        task.desc = "Using the lines highlighted in left/right infrared images, find corresponding lines."
    End Sub
    Public Sub Run(src As cv.Mat)
        grid.Run(Nothing)

        lrView.Run(src)
        dst1 = lrView.dst1
        dst2 = lrView.dst2

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim left = lrView.dst1(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = lrView.dst2(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.Run(left, right, dstTmp, siftBasics.radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst1 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst2 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))
    End Sub
End Class








Public Class Line_NearestPoint : Inherits VBparent
    Dim rangeRect As cv.Rect
    Public Sub New()
        Dim offset = 20
        rangeRect = New cv.Rect(offset, offset, dst1.Width - offset * 2, dst1.Height - offset * 2)
        task.desc = "Demonstrate computing the distance from a point to a line"
    End Sub
    Private Function getPoint() As cv.Point2f
        Dim x = msRNG.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
        Dim y = msRNG.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)

        Return New cv.Point2f(x, y)
    End Function
    Public Function findNearest(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As cv.Point2f
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
                Dim a2 = New cv.Point2f(dst1.Width, b2 + dst1.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearest = New cv.Point2f(x, m * x + b1)

                If nearest.X < minX Or nearest.X > maxX Or nearest.Y < minY Or nearest.Y > maxY Then onTheLine = False
            End If
        End If

        If onTheLine = False Then nearest = If(pt.DistanceTo(p1) < pt.DistanceTo(p2), p1, p2)
        Return nearest
    End Function
    Public Function findDistance(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As Single
        Dim nearest = findNearest(p1, p2, pt)
        Return nearest.DistanceTo(pt)
    End Function
    Public Sub Run(src As cv.Mat)

        If task.frameCount Mod 30 = 0 And standalone Then
            Dim pt = getPoint()
            Dim p1 = getPoint()
            Dim p2 = getPoint()
            Dim nearest = findNearest(p1, p2, pt)
            dst1.SetTo(0)
            dst1.Circle(New cv.Point2f(pt.X, pt.Y), task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst1.Line(New cv.Point2f(p1.X, p1.Y), New cv.Point2f(p2.X, p2.Y), cv.Scalar.Yellow, 1, task.lineType)
            dst1.Line(pt, nearest, cv.Scalar.White, 1, task.lineType)
            label1 = "nearest point = (" + CStr(nearest.X) + "," + CStr(nearest.Y) + ")"
        End If
    End Sub
End Class








Public Class Line_SideView : Inherits VBparent
    Dim lines As New Line_Basics
    Dim tView As New TimeView_FloodFill
    Public Sub New()
        label1 = "Side view of the lines detected in the RGB image"
        label2 = "Lines found in the RGB image view"
        task.desc = "Line in image are projected into the depth image"
    End Sub
    Public Sub Run(src As cv.Mat)

        lines.Run(src)
        dst2 = lines.dst2

        Dim mask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ' tView.src = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)
        'task.pointCloud.CopyTo(tView.src, mask)
        tView.Run(task.pointCloud)

        dst1 = tView.dst2
    End Sub
End Class







Public Class Line_TimeView : Inherits VBparent
    Public pt1List() As List(Of cv.Point)
    Public pt2List() As List(Of cv.Point)
    Dim lines As New Line_Basics
    Public Sub New()
        task.desc = "Collect lines over time"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static frameSlider = findSlider("Detect lines from the last X frames")
        Static lineCount As Integer
        Static lineIndex As Integer

        lines.Run(src)

        If lineCount <> frameSlider.value Then
            lineCount = frameSlider.value
            ReDim pt1List(lineCount - 1)
            ReDim pt2List(lineCount - 1)
            lineIndex = 0
        End If

        pt1List(lineIndex) = New List(Of cv.Point)(lines.pt1List)
        pt2List(lineIndex) = New List(Of cv.Point)(lines.pt2List)

        dst1 = src
        dst2.SetTo(0)
        Dim lineTotal As Integer
        For i = 0 To pt1List.Count - 1
            If pt1List(i) IsNot Nothing Then
                lineTotal += pt1List(i).Count
                For j = 0 To pt1List(i).Count - 1
                    dst1.Line(pt1List(i)(j), pt2List(i)(j), cv.Scalar.Yellow, task.lineSize, task.lineType)
                    dst2.Line(pt1List(i)(j), pt2List(i)(j), cv.Scalar.Yellow, task.lineSize, task.lineType)
                Next
            End If
        Next

        Dim pixelCount = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()

        lineIndex += 1
        If lineIndex >= lineCount Then lineIndex = 0
        label2 = "There were " + CStr(lineTotal) + " lines detected with " + Format(pixelCount / 1000, "#.0") + "k pixels"
    End Sub
End Class








Public Class Line_Regions : Inherits VBparent
    Dim lines As New Line_TimeView
    Dim reduction As New Reduction_Basics
    Public Sub New()
        label1 = "Lines detected (below) Regions detected (right image)"
        findRadio("Use bitwise reduction").Checked = True
        findSlider("Bits to remove in bitwise reduction").Value = 6
        task.desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Sub Run(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst1

        lines.Run(src)

        Const lineMatch = 254
        dst2.SetTo(lineMatch, lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst1 = dst2.Clone

        Dim indexer1 = dst1.GetGenericIndexer(Of Byte)()
        Dim indexer2 = dst2.GetGenericIndexer(Of Byte)()
        Dim nextB As Byte
        Dim region As Byte
        Dim noRegion = True

        For x = 0 To dst1.Width - 1
            noRegion = True
            For y = 0 To dst1.Height - 1
                nextB = indexer1(y, x)
                If nextB = lineMatch Then
                    noRegion = True
                Else
                    If noRegion Then
                        region = nextB
                        noRegion = False
                    Else
                        indexer1(y, x) = region
                    End If
                End If
            Next
        Next

        For y = 0 To dst2.Height - 1
            noRegion = True
            For x = 0 To dst2.Width - 1
                nextB = indexer2(y, x)
                If nextB = lineMatch Then
                    noRegion = True
                Else
                    If noRegion Then
                        region = indexer1(y, x)
                        noRegion = False
                    Else
                        indexer2(y, x) = region
                    End If
                End If
            Next
        Next

        dst1 = lines.dst1
    End Sub
End Class










Public Class Line_LeftRightImages : Inherits VBparent
    Dim lrPalette As New Palette_LeftRightImages
    Public leftLines As New Line_TimeView
    Public rightLines As New Line_TimeView
    Public rgbLines As New Line_TimeView
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Show lines from RGB in green"
        End If

        findSlider("Line length threshold in pixels").Value = 30
        label1 = "Left image lines(red) with Right(blue)"
        task.desc = "Find lines in the infrared images and overlay them in a single image"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static rgbCheck = findCheckBox("Show lines from RGB in green")
        lrPalette.Run(src)

        If task.cameraStable = False Then dst1.SetTo(cv.Scalar.White)

        leftLines.Run(lrPalette.dst1)
        dst1.SetTo(cv.Scalar.White)
        dst1.SetTo(cv.Scalar.Red, leftLines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        rightLines.Run(lrPalette.dst2)
        dst1.SetTo(cv.Scalar.Blue, rightLines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        If rgbCheck.checked Then
            rgbLines.Run(src)
            dst1.SetTo(cv.Scalar.Green, rgbLines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        End If
    End Sub
End Class
