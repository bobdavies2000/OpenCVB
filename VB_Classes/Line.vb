Imports cvb = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Dim ld As cvb.XImgProc.FastLineDetector
    Public lpList As New List(Of PointPair)
    Public lineColor = cvb.Scalar.White
    Public Sub New()
        ld = cvb.XImgProc.CvXImgProc.CreateFastLineDetector
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) Else dst2 = src.Clone
        If dst2.Type <> cvb.MatType.CV_8U Then dst2.ConvertTo(dst2, cvb.MatType.CV_8U)

        Dim lines = ld.Detect(dst2)

        Dim sortByLen As New SortedList(Of Single, PointPair)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= dst2.Cols And v(1) >= 0 And v(1) <= dst2.Rows And
               v(2) >= 0 And v(2) <= dst2.Cols And v(3) >= 0 And v(3) <= dst2.Rows Then
                Dim p1 = New cvb.Point(v(0), v(1))
                Dim p2 = New cvb.Point(v(2), v(3))
                Dim lp = New PointPair(p1, p2)
                sortByLen.Add(lp.length, lp)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        lpList.Clear()
        For Each lp In sortByLen.Values
            lpList.Add(lp)
            DrawLine(dst2, lp.p1, lp.p2, lineColor)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class






Public Class Line_SubsetRect : Inherits TaskParent
    Dim ld As cvb.XImgProc.FastLineDetector
    Public sortByLen As New SortedList(Of Single, PointPair)(New compareAllowIdenticalSingleInverted)
    Public mpList As New List(Of PointPair)
    Public ptList As New List(Of cvb.Point2f)
    Public subsetRect As cvb.Rect
    Public lineColor = cvb.Scalar.White
    Public Sub New()
        subsetRect = New cvb.Rect(0, 0, dst2.Width, dst2.Height)
        ld = cvb.XImgProc.CvXImgProc.CreateFastLineDetector
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) Else dst2 = src.Clone
        If dst2.Type <> cvb.MatType.CV_8U Then dst2.ConvertTo(dst2, cvb.MatType.CV_8U)

        Dim lines = ld.Detect(dst2(subsetRect))

        sortByLen.Clear()
        mpList.Clear()
        ptList.Clear()
        For Each v In lines
            If v(0) >= 0 And v(0) <= dst2.Cols And v(1) >= 0 And v(1) <= dst2.Rows And
               v(2) >= 0 And v(2) <= dst2.Cols And v(3) >= 0 And v(3) <= dst2.Rows Then
                Dim p1 = New cvb.Point(v(0) + subsetRect.X, v(1) + subsetRect.Y)
                Dim p2 = New cvb.Point(v(2) + subsetRect.X, v(3) + subsetRect.Y)
                Dim lp = New PointPair(p1, p2)
                mpList.Add(lp)
                ptList.Add(p1)
                ptList.Add(p2)
                sortByLen.Add(lp.length, lp)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        For Each lp In sortByLen.Values
            DrawLine(dst2, lp.p1, lp.p2, lineColor)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        labels(2) = CStr(mpList.Count) + " lines were detected in the current frame"
    End Sub
End Class






Public Class Line_InterceptsUI : Inherits TaskParent
    Dim lines As New Line_Intercepts
    Dim p2 As cvb.Point
    Dim redRadio As System.Windows.Forms.RadioButton
    Dim greenRadio As System.Windows.Forms.RadioButton
    Dim yellowRadio As System.Windows.Forms.RadioButton
    Dim blueRadio As System.Windows.Forms.RadioButton
    Public Sub New()
        redRadio = FindRadio("Show Top intercepts")
        greenRadio = FindRadio("Show Bottom intercepts")
        yellowRadio = FindRadio("Show Right intercepts")
        blueRadio = FindRadio("Show Left intercepts")
        labels(2) = "Use mouse in right image to highlight lines"
        desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst3.SetTo(0)

        Dim red = New cvb.Scalar(0, 0, 255)
        Dim green = New cvb.Scalar(1, 128, 0)
        Dim yellow = New cvb.Scalar(2, 255, 255)
        Dim blue = New cvb.Scalar(254, 0, 0)

        Dim center = New cvb.Point(dst3.Width / 2, dst3.Height / 2)
        dst3.Line(New cvb.Point(0, 0), center, blue, task.lineWidth, cvb.LineTypes.Link4)
        dst3.Line(New cvb.Point(dst2.Width, 0), center, red, task.lineWidth, cvb.LineTypes.Link4)
        dst3.Line(New cvb.Point(0, dst2.Height), center, blue, task.lineWidth, cvb.LineTypes.Link4)
        dst3.Line(New cvb.Point(dst2.Width, dst2.Height), center, yellow, task.lineWidth, cvb.LineTypes.Link4)

        Dim mask = New cvb.Mat(New cvb.Size(dst2.Width + 2, dst2.Height + 2), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        Dim pt = New cvb.Point(center.X, center.Y - 30)
        cvb.Cv2.FloodFill(dst3, mask, pt, red, New cvb.Rect, 1, 1, cvb.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cvb.Point(center.X, center.Y + 30)
        cvb.Cv2.FloodFill(dst3, mask, pt, green, New cvb.Rect, 1, 1, cvb.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cvb.Point(center.X - 30, center.Y)
        cvb.Cv2.FloodFill(dst3, mask, pt, blue, New cvb.Rect, 1, 1, cvb.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cvb.Point(center.X + 30, center.Y)
        cvb.Cv2.FloodFill(dst3, mask, pt, yellow, New cvb.Rect, 1, 1, cvb.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst3.Get(Of cvb.Vec3b)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim p1 = task.mouseMovePoint
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cvb.Point(dst3.Width / 2, 0) Else p2 = New cvb.Point(dst3.Width, dst3.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color(0) = 0 Then p2 = New cvb.Point(-b / m, 0) ' red zone
            If color(0) = 1 Then p2 = New cvb.Point((dst3.Height - b) / m, dst3.Height) ' green
            If color(0) = 2 Then p2 = New cvb.Point(dst3.Width, dst3.Width * m + b) ' yellow
            If color(0) = 254 Then p2 = New cvb.Point(0, b) ' blue
            DrawLine(dst3, center, p2, cvb.Scalar.Black)
        End If
        DrawCircle(dst3, center, task.DotSize, cvb.Scalar.White)

        If color(0) = 0 Then redRadio.checked = True
        If color(0) = 1 Then greenRadio.checked = True
        If color(0) = 2 Then yellowRadio.checked = True
        If color(0) = 254 Then blueRadio.checked = True

        lines.hightLightIntercept(dst3)
        dst2 = lines.dst2
    End Sub
End Class







Public Class Line_Intercepts : Inherits TaskParent
    Public extended As New LongLine_Extend
    Public lines As New Line_Basics
    Public p1List As New List(Of cvb.Point2f)
    Public p2List As New List(Of cvb.Point2f)
    Dim longLine As New LongLine_Basics
    Public options As New Options_Intercepts
    Public intercept As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public interceptArray = {topIntercepts, botIntercepts, leftIntercepts, rightIntercepts}
    Public Sub New()
        labels(2) = "Highlight line x- and y-intercepts.  Move mouse over the image."
        desc = "Show lines with similar y-intercepts"
    End Sub
    Public Sub hightLightIntercept(dst As cvb.Mat)
        For Each inter In intercept
            If Math.Abs(options.mouseMovePoint - inter.Key) < options.interceptRange Then
                DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cvb.Scalar.White)
                DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cvb.Scalar.Blue)
            End If
        Next
        For Each inter In intercept
            Select Case options.selectedIntercept
                Case 0
                    dst.Line(New cvb.Point(inter.Key, 0), New cvb.Point(inter.Key, 10), cvb.Scalar.White, task.lineWidth)
                Case 1
                    dst.Line(New cvb.Point(inter.Key, dst2.Height), New cvb.Point(inter.Key, dst2.Height - 10), cvb.Scalar.White, task.lineWidth)
                Case 2
                    dst.Line(New cvb.Point(0, inter.Key), New cvb.Point(10, inter.Key), cvb.Scalar.White, task.lineWidth)
                Case 3
                    dst.Line(New cvb.Point(dst2.Width, inter.Key), New cvb.Point(dst2.Width - 10, inter.Key), cvb.Scalar.White, task.lineWidth)
            End Select
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        lines.Run(src)
        If lines.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In lines.lpList
            Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
            If lp.p1.X <> minXX Then ' leftmost point is always in p1
                Dim tmp = lp.p1
                lp.p1 = lp.p2
                lp.p2 = tmp
            End If

            p1List.Add(lp.p1)
            p2List.Add(lp.p2)
            DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Yellow)

            Dim saveP1 = lp.p1, saveP2 = lp.p2

            Dim emps = longLine.BuildLongLine(lp)
            If emps.p1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
            If emps.p1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

            If emps.p2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
            If emps.p2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
            index += 1
        Next

        If standaloneTest() Then hightLightIntercept(dst2)
    End Sub
End Class






Public Class Line_LeftRightImages : Inherits TaskParent
    Public leftLines As New Line_TimeView
    Public rightLines As New Line_TimeView
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels(2) = "Left image lines(red) with Right(blue)"
        desc = "Find lines in the infrared images and overlay them in a single image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.cameraStable = False Then dst2.SetTo(cvb.Scalar.White)

        leftLines.Run(task.leftView)
        dst2.SetTo(cvb.Scalar.White)
        dst2.SetTo(cvb.Scalar.Red, leftLines.dst3)

        rightLines.Run(task.rightView)
        dst2.SetTo(cvb.Scalar.Blue, rightLines.dst3)

        dst0 = task.leftView
        dst1 = task.rightView
    End Sub
End Class










Public Class Line_InDepthAndBGR : Inherits TaskParent
    Dim lines As New Line_Basics
    Public p1List As New List(Of cvb.Point2f)
    Public p2List As New List(Of cvb.Point2f)
    Public z1List As New List(Of cvb.Point3f) ' the point cloud values corresponding to p1 and p2
    Public z2List As New List(Of cvb.Point3f)
    Public Sub New()
        labels(2) = "Lines defined in BGR"
        labels(3) = "Lines in BGR confirmed in the point cloud"
        desc = "Find the BGR lines and confirm they are present in the cloud data."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        If lines.lpList.Count = 0 Then Exit Sub

        Dim lineList = New List(Of cvb.Rect)
        If task.motionFlag Or task.optionsChanged Then dst3.SetTo(0)
        p1List.Clear()
        p2List.Clear()
        z1List.Clear()
        z2List.Clear()
        For Each lp In lines.lpList
            Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
            Dim minYY = Math.Min(lp.p1.Y, lp.p2.Y)
            Dim w = Math.Abs(lp.p1.X - lp.p2.X)
            Dim h = Math.Abs(lp.p1.Y - lp.p2.Y)
            Dim r = New cvb.Rect(minXX, minYY, If(w > 0, w, 2), If(h > 0, h, 2))
            Dim mask = New cvb.Mat(New cvb.Size(w, h), cvb.MatType.CV_8U, cvb.Scalar.All(0))
            mask.Line(New cvb.Point(CInt(lp.p1.X - r.X), CInt(lp.p1.Y - r.Y)), New cvb.Point(CInt(lp.p2.X - r.X), CInt(lp.p2.Y - r.Y)), 255, task.lineWidth, cvb.LineTypes.Link4)
            Dim mean = task.pointCloud(r).Mean(mask)

            If mean <> New cvb.Scalar Then
                Dim mmX = GetMinMax(task.pcSplit(0)(r), mask)
                Dim mmY = GetMinMax(task.pcSplit(1)(r), mask)
                Dim len1 = mmX.minLoc.DistanceTo(mmX.maxLoc)
                Dim len2 = mmY.minLoc.DistanceTo(mmY.maxLoc)
                If len1 > len2 Then
                    lp.p1 = New cvb.Point(mmX.minLoc.X + r.X, mmX.minLoc.Y + r.Y)
                    lp.p2 = New cvb.Point(mmX.maxLoc.X + r.X, mmX.maxLoc.Y + r.Y)
                Else
                    lp.p1 = New cvb.Point(mmY.minLoc.X + r.X, mmY.minLoc.Y + r.Y)
                    lp.p2 = New cvb.Point(mmY.maxLoc.X + r.X, mmY.maxLoc.Y + r.Y)
                End If
                If lp.p1.DistanceTo(lp.p2) > 1 Then
                    DrawLine(dst3, lp.p1, lp.p2, cvb.Scalar.Yellow)
                    p1List.Add(lp.p1)
                    p2List.Add(lp.p2)
                    z1List.Add(task.pointCloud.Get(Of cvb.Point3f)(lp.p1.Y, lp.p1.X))
                    z2List.Add(task.pointCloud.Get(Of cvb.Point3f)(lp.p2.Y, lp.p2.X))
                End If
            End If
        Next
    End Sub
End Class








Public Class Line_PointSlope : Inherits TaskParent
    Dim extend As New LongLine_Extend
    Dim lines As New Line_Basics
    Dim knn As New KNN_BasicsN
    Public bestLines As New List(Of PointPair)
    Const lineCount As Integer = 3
    Const searchCount As Integer = 100
    Public Sub New()
        knn.options.knnDimension = 5 ' slope, p1.x, p1.y, p2.x, p2.y
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "TrainInput to KNN", "Tracking these lines", "Query inputs to KNN"}
        desc = "Find the 3 longest lines in the image and identify them from frame to frame using the point and slope."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = src

        If bestLines.Count < lineCount Or task.heartBeat Then
            dst3.SetTo(0)
            bestLines.Clear()
            knn.queries.Clear()
            For Each lp In lines.lpList
                bestLines.Add(lp)
                For j = 0 To knn.options.knnDimension - 1
                    knn.queries.Add(Choose(j + 1, lp.slope, lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
                Next
                DrawLine(dst3, lp.p1, lp.p2, task.HighlightColor)
                If bestLines.Count >= lineCount Then Exit For
            Next
        End If

        dst1.SetTo(0)
        knn.trainInput.Clear()
        For Each lp In lines.lpList
            For j = 0 To knn.options.knnDimension - 1
                knn.trainInput.Add(Choose(j + 1, lp.slope, lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
            Next
            dst1.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth + 1, task.lineType)
        Next
        If knn.trainInput.Count = 0 Then
            SetTrueText("There were no lines detected!  Were there any unusual settings for this run?", 3)
            Exit Sub
        End If

        knn.Run(empty)
        If knn.result Is Nothing Then Exit Sub
        Dim nextLines As New List(Of PointPair)
        Dim usedBest As New List(Of Integer)
        Dim index As Integer
        For i = 0 To knn.result.GetUpperBound(0)
            For j = 0 To knn.result.GetUpperBound(1)
                index = knn.result(i, j)
                If usedBest.Contains(index) = False Then Exit For
            Next
            usedBest.Add(index)

            If index * knn.options.knnDimension + 4 < knn.trainInput.Count Then
                Dim mps = New PointPair(New cvb.Point2f(knn.trainInput(index * knn.options.knnDimension + 0), knn.trainInput(index * knn.options.knnDimension + 1)),
                          New cvb.Point2f(knn.trainInput(index * knn.options.knnDimension + 2), knn.trainInput(index * knn.options.knnDimension + 3)))
                mps.slope = knn.trainInput(index * knn.options.knnDimension)
                nextLines.Add(mps)
            End If
        Next

        bestLines = New List(Of PointPair)(nextLines)
        For Each ptS In bestLines
            DrawLine(dst2, ptS.p1, ptS.p2, task.HighlightColor)
            DrawLine(dst1, ptS.p1, ptS.p2, cvb.Scalar.Red)
        Next
    End Sub
End Class








Public Class Line_Movement : Inherits TaskParent
    Public p1 As cvb.Point
    Public p2 As cvb.Point
    Dim gradientColors(100) As cvb.Scalar
    Dim kalman As New Kalman_Basics
    Dim frameCount As Integer
    Public Sub New()
        kalman.kOutput = {0, 0, 0, 0}

        Dim color1 = cvb.Scalar.Yellow, color2 = cvb.Scalar.Blue
        Dim f As Double = 1.0
        For i = 0 To gradientColors.Length - 1
            gradientColors(i) = New cvb.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2))
            f -= 1 / gradientColors.Length
        Next

        labels = {"", "", "Line Movement", ""}
        desc = "Show the movement of the line provided"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            Static k1 = p1
            Static k2 = p2
            If k1.DistanceTo(p1) = 0 And k2.DistanceTo(p2) = 0 Then
                k1 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                k2 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                dst2.SetTo(0)
            End If
            kalman.kInput = {k1.X, k1.Y, k2.X, k2.Y}
            kalman.Run(empty)
            p1 = New cvb.Point(kalman.kOutput(0), kalman.kOutput(1))
            p2 = New cvb.Point(kalman.kOutput(2), kalman.kOutput(3))
        End If
        frameCount += 1
        DrawLine(dst2, p1, p2, gradientColors(frameCount Mod gradientColors.Count))
    End Sub
End Class







Public Class Line_GCloud : Inherits TaskParent
    Public lines As New Line_Basics
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_Features
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        angleSlider = FindSlider("Angle tolerance in degrees")
        labels(2) = "Line_GCloud - Blue are vertical lines using the angle thresholds."
        desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
    End Sub
    Public Function updateGLine(src As cvb.Mat, gc As gravityLine, p1 As cvb.Point, p2 As cvb.Point) As gravityLine
        gc.tc1.center = p1
        gc.tc2.center = p2
        gc.tc1 = match.createCell(src, gc.tc1.correlation, p1)
        gc.tc2 = match.createCell(src, gc.tc2.correlation, p2)
        gc.tc1.strOut = Format(gc.tc1.correlation, fmt2) + vbCrLf + Format(gc.tc1.depth, fmt2) + "m"
        gc.tc2.strOut = Format(gc.tc2.correlation, fmt2) + vbCrLf + Format(gc.tc2.depth, fmt2) + "m"

        Dim mean = task.pointCloud(gc.tc1.rect).Mean(task.depthMask(gc.tc1.rect))
        gc.pt1 = New cvb.Point3f(mean(0), mean(1), mean(2))
        gc.tc1.depth = gc.pt1.Z
        mean = task.pointCloud(gc.tc2.rect).Mean(task.depthMask(gc.tc2.rect))
        gc.pt2 = New cvb.Point3f(mean(0), mean(1), mean(2))
        gc.tc2.depth = gc.pt2.Z

        gc.len3D = distance3D(gc.pt1, gc.pt2)
        If gc.pt1 = New cvb.Point3f Or gc.pt2 = New cvb.Point3f Then
            gc.len3D = 0
        Else
            gc.arcX = Math.Asin((gc.pt1.X - gc.pt2.X) / gc.len3D) * 57.2958
            gc.arcY = Math.Abs(Math.Asin((gc.pt1.Y - gc.pt2.Y) / gc.len3D) * 57.2958)
            If gc.arcY > 90 Then gc.arcY -= 90
            gc.arcZ = Math.Asin((gc.pt1.Z - gc.pt2.Z) / gc.len3D) * 57.2958
        End If

        Return gc
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim maxAngle = angleSlider.Value

        dst2 = src.Clone
        lines.Run(src.Clone)

        sortedVerticals.Clear()
        sortedHorizontals.Clear()
        For Each lp In lines.lpList
            Dim gc As gravityLine
            gc = updateGLine(src, gc, lp.p1, lp.p2)
            allLines.Add(lp.p1.DistanceTo(lp.p2), gc)
            If Math.Abs(90 - gc.arcY) < maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Blue)
            End If
            If Math.Abs(gc.arcY) <= maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Yellow)
            End If
        Next

        labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " + Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
    End Sub
End Class







Public Class Line_DisplayInfo : Inherits TaskParent
    Public tcells As New List(Of tCell)
    Dim canny As New Edge_Basics
    Dim blur As New Blur_Basics
    Public distance As Integer
    Public maskCount As Integer
    Dim myCurrentFrame As Integer = -1
    Public Sub New()
        dst1 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(2) = "When running standaloneTest(), a pair of random points is used to test the algorithm."
        desc = "Display the line provided in mp"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src
        If standaloneTest() And task.heartBeat Then
            Dim tc As tCell
            tcells.Clear()
            For i = 0 To 2 - 1
                tc.center = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                tcells.Add(tc)
            Next
        End If
        If tcells.Count < 2 Then Exit Sub

        If myCurrentFrame < task.frameCount Then
            canny.Run(src)
            blur.Run(canny.dst2)
            myCurrentFrame = task.frameCount
        End If
        dst1.SetTo(0)
        Dim p1 = tcells(0).center
        Dim p2 = tcells(1).center
        DrawLine(dst1, p1, p2, 255)

        dst3.SetTo(0)
        blur.dst2.Threshold(1, 255, cvb.ThresholdTypes.Binary).CopyTo(dst3, dst1)
        distance = p1.DistanceTo(p2)
        maskCount = dst3.CountNonZero

        For Each tc In tcells
            'dst2.Rectangle(tc.rect, myHighlightColor)
            'dst2.Rectangle(tc.searchRect, cvb.Scalar.White, task.lineWidth)
            SetTrueText(tc.strOut, New cvb.Point(tc.rect.X, tc.rect.Y))
        Next

        strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
        DrawLine(dst2, p1, p2, task.HighlightColor)

        strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                  "Correlation coefficient is shown with the depth in meters."
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class Line_Perpendicular : Inherits TaskParent
    Public p1 As cvb.Point2f ' first input point
    Public p2 As cvb.Point2f ' second input point
    Public r1 As cvb.Point2f ' first output point (perpendicalar to input)
    Public r2 As cvb.Point2f ' second output point (perpendicalar to input) 
    Public Sub New()
        labels = {"", "", "White is the original line, red dot is midpoint, yellow is perpendicular line", ""}
        desc = "Find the line perpendicular to the line created by the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static externalUse = If(p1 = New cvb.Point2f, False, True)
        If task.heartBeat Or externalUse Then
            If standaloneTest() Then
                p1 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                p2 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If
            dst2.SetTo(0)
            DrawLine(dst2, p1, p2, cvb.Scalar.White)

            Dim slope As Single
            If p1.X = p2.X Then slope = 100000 Else slope = (p1.Y - p2.Y) / (p1.X - p2.X)
            Dim midPoint = New cvb.Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)

            DrawCircle(dst2, midPoint, task.DotSize + 2, cvb.Scalar.Red)
            Dim m = If(slope = 0, 100000, -1 / slope)

            Dim b = midPoint.Y - m * midPoint.X
            r1 = New cvb.Point2f(-b / m, 0)
            r2 = New cvb.Point2f((dst2.Height - b) / m, dst2.Height)
            DrawLine(dst2, r1, r2, cvb.Scalar.Yellow)
        End If
    End Sub
End Class









Public Class Line_CellsVertHoriz : Inherits TaskParent
    Dim lines As New FeatureLine_Finder
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels(2) = "RedCloud_Hulls output with lines highlighted"
        desc = "Identify the lines created by the RedCloud Cells and separate vertical from horizontal"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        lines.Run(dst2.Clone)
        dst3 = src
        For i = 0 To lines.sortedHorizontals.Count - 1
            Dim index = lines.sortedHorizontals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cvb.Scalar.Yellow)
        Next
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim index = lines.sortedVerticals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cvb.Scalar.Blue)
        Next
        labels(3) = CStr(lines.sortedVerticals.Count) + " vertical and " + CStr(lines.sortedHorizontals.Count) + " horizontal lines identified in the RedCloud output"
    End Sub
End Class






Public Class Line_Cells : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Identify all lines in the RedCloud_Basics cell boundaries"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        lines.Run(dst2.Clone)
        dst3 = lines.dst3
        labels(2) = CStr(lines.lpList.Count / 2) + " lines identified"
    End Sub
End Class





Public Class Line_ViewSide : Inherits TaskParent
    Public autoY As New OpAuto_YRange
    Public lines As New Line_Basics
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Lines found in the hotspots of the Side View."}
        desc = "Find lines in the hotspots for the side view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histSide.Run(src)

        autoY.Run(histSide.histogram)
        dst2 = histSide.histogram.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2.Clone)
        dst3 = lines.dst3
    End Sub
End Class






Public Class Line_ViewTop : Inherits TaskParent
    Public autoX As New OpAuto_XRange
    Public lines As New Line_Basics
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Lines found in the hotspots of the Top View."}
        desc = "Find lines in the hotspots for the Top View."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histTop.Run(src)

        autoX.Run(histTop.histogram)
        dst2 = histTop.histogram.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2)
        dst3 = lines.dst3
    End Sub
End Class






Public Class Line_FromContours : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim lines As New Line_Basics
    Dim contours As New Contour_Gray
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics" ' to enable sliders.
        lines.lineColor = cvb.Scalar.Red
        UpdateAdvice("Use the reduction sliders in the redoptions to control contours and subsequent lines found.")
        desc = "Find the lines in the contours."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        contours.Run(reduction.dst2)
        dst2 = contours.dst2.Clone
        lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, cvb.Scalar.White)
        Next
    End Sub
End Class








Public Class Line_ColorClass : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Lines for the current color class", "Color Class input"}
        desc = "Review lines in all the different color classes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)
        dst1 = color8U.dst2

        lines.Run(dst1 * 255 / color8U.classCount)
        dst2 = lines.dst2
        dst3 = lines.dst3

        labels(1) = "Input to Line_Basics"
        labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
    End Sub
End Class







Public Class Line_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        FindSlider("Canny Aperture").Value = 7
        labels = {"", "", "Straight lines in Canny output", "Input to Line_Basics"}
        desc = "Find lines in the Canny output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        dst3 = canny.dst2.Clone

        lines.Run(canny.dst2)
        dst2 = lines.dst3
    End Sub
End Class












Public Class Line_TimeViewLines : Inherits TaskParent
    Dim lines As New Line_TimeView
    Public lpList As New List(Of PointPair)
    Public Sub New()
        labels(2) = "Lines from the latest Line_TimeLine"
        labels(3) = "Vertical (blue) Horizontal (Red) Other (Green)"
        desc = "Find slope and y-intercept of lines over time."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        If lines.pixelcount = 0 Then Exit Sub

        lpList.Clear()

        dst2 = lines.dst3
        dst3.SetTo(cvb.Scalar.White)
        Dim index = lines.frameList.Count - 1 ' the most recent.
        For Each lp In lines.lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, cvb.Scalar.Green)
            lpList.Add(lp)
            If lp.slope = 0 Then
                dst3.Line(lp.p1, lp.p2, cvb.Scalar.Red, task.lineWidth * 2 + 1, task.lineType)
            End If
        Next
    End Sub
End Class







Public Class Line_TimeView : Inherits TaskParent
    Public frameList As New List(Of List(Of PointPair))
    Public lines As New Line_Basics
    Public pixelcount As Integer
    Public mpList As New List(Of PointPair)
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Collect lines over time"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)

        If task.optionsChanged Or task.motionFlag Then frameList.Clear()
        Dim nextMpList = New List(Of PointPair)(lines.lpList)
        frameList.Add(nextMpList)

        dst2 = src
        dst3.SetTo(0)
        mpList.Clear()
        Dim lineTotal As Integer
        For i = 0 To frameList.Count - 1
            lineTotal += frameList(i).Count
            For Each lp In frameList(i)
                DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Yellow)
                DrawLine(dst3, lp.p1, lp.p2, cvb.Scalar.White)
                mpList.Add(lp)
            Next
        Next

        If frameList.Count >= task.frameHistoryCount Then frameList.RemoveAt(0)
        pixelcount = dst3.CountNonZero
        labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelcount / 1000, "#.0") + "k pixels"
    End Sub
End Class








Public Class Line_RegionsVB : Inherits TaskParent
    Dim lines As New Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        task.redOptions.setBitReductionBar(6)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show intermediate vertical step results.")
            check.addCheckBox("Run horizontal without vertical step")
        End If

        desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static noVertCheck = FindCheckBox("Run horizontal without vertical step")
        Static verticalCheck = FindCheckBox("Show intermediate vertical step results.")
        reduction.Run(src)
        dst2 = reduction.dst2
        dst3 = dst2.Clone

        lines.Run(src)

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










Public Class Line_Verticals : Inherits TaskParent
    Public lines As New Line_Basics
    Public options As New Options_Features
    Public verticals As New List(Of gravityLine)
    Public maxAngleX As Integer
    Public maxAngleZ As Integer
    Dim gMat As New IMU_GMatrix
    Dim cellSlider As System.Windows.Forms.TrackBar
    Dim angleXSlider As System.Windows.Forms.TrackBar
    Dim angleZSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        cellSlider = FindSlider("MatchTemplate Cell Size")
        angleXSlider = FindSlider("X angle tolerance in degrees")
        angleZSlider = FindSlider("Z angle tolerance in degrees")
        desc = "Capture all vertical and horizontal lines."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        maxAngleX = angleXSlider.Value
        maxAngleZ = angleZSlider.Value
        Dim radius = CInt(cellSlider.Value / 2)
        lines.Run(src.Clone)

        If lines.lpList.Count = 0 Then Exit Sub ' nothing to work with...
        Dim lines2 As New List(Of cvb.Point2f)
        Dim lines3 As New List(Of cvb.Point3f)
        For Each lp In lines.lpList
            lines2.Add(New cvb.Point2f(lp.p1.X, lp.p1.Y))
            lines2.Add(New cvb.Point2f(lp.p2.X, lp.p2.Y))
            For j = 0 To 2 - 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                lines3.Add(task.pointCloud.Get(Of cvb.Point3f)(pt.y, pt.x))
            Next
        Next

        dst2 = src.Clone

        gMat.Run(empty)

        Dim points As cvb.Mat = cvb.Mat.FromPixelData(lines3.Count, 3, cvb.MatType.CV_32F, lines3.ToArray)
        Dim gPoints As cvb.Mat = (points * gMat.gMatrix).ToMat

        verticals.Clear()
        For i = 0 To gPoints.Rows - 1 Step 2
            Dim vert As gravityLine
            vert.tc1.center = lines2(i)
            vert.tc2.center = lines2(i + 1)
            vert.pt1 = gPoints.Get(Of cvb.Point3f)(i + 0, 0)
            vert.pt2 = gPoints.Get(Of cvb.Point3f)(i + 1, 0)
            vert.len3D = distance3D(vert.pt1, vert.pt2)
            Dim arcX = Math.Asin((vert.pt1.X - vert.pt2.X) / vert.len3D) * 57.2958
            Dim arcZ = Math.Asin((vert.pt1.Z - vert.pt2.Z) / vert.len3D) * 57.2958
            If Math.Abs(arcX) <= maxAngleX And Math.Abs(arcZ) <= maxAngleZ Then
                SetTrueText(Format(arcX, fmt1) + " X" + vbCrLf + Format(arcZ, fmt1) + " Z", lines2(i), 2)
                SetTrueText(Format(arcX, fmt1) + " X" + vbCrLf + Format(arcZ, fmt1) + " Z", lines2(i), 3)
                DrawLine(dst2, lines2(i), lines2(i + 1), task.HighlightColor)
                verticals.Add(vert)
            End If
        Next
        labels(2) = CStr(verticals.Count) + " vertical lines were found.  Total lines found = " + CStr(lines.lpList.Count)
    End Sub
End Class







Public Class Line_Verts : Inherits TaskParent
    Dim verts As New Line_Verticals
    Dim match As New Match_tCell
    Public verticals As New List(Of gravityLine)
    Dim gMat As New IMU_GMatrix
    Public Sub New()
        labels(3) = "Numbers below are: correlation coefficient, distance in meters, angle from vertical in the X-direction, angle from vertical in the Z-direction"
        desc = "Find the list of vertical lines and track them until most are lost, then recapture the vertical lines again."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        If verticals.Count < 2 Or verticals.Count < verts.verticals.Count / 3 Or task.optionsChanged Then
            verts.Run(src)
            For Each vert In verts.verticals
                vert.tc1 = match.createCell(src, 0, vert.tc1.center)
                vert.tc2 = match.createCell(src, 0, vert.tc2.center)
                verticals.Add(vert)
            Next
        End If

        dst2 = src.Clone
        Dim lines2 As New List(Of cvb.Point2f)
        Dim lines3 As New List(Of cvb.Point3f)
        Dim newVerts As New List(Of gravityLine)
        For i = 0 To verticals.Count - 1
            Dim vert = verticals(i)

            match.tCells.Clear()
            match.tCells.Add(vert.tc1)
            match.tCells.Add(vert.tc2)
            match.Run(src)
            vert.tc1 = match.tCells(0)
            vert.tc2 = match.tCells(1)

            Dim correlationMin = verts.options.correlationMin
            If vert.tc1.correlation >= correlationMin And vert.tc2.correlation >= correlationMin Then
                lines2.Add(vert.tc1.center)
                lines2.Add(vert.tc2.center)
                lines3.Add(task.pointCloud.Get(Of cvb.Point3f)(vert.tc1.center.Y, vert.tc1.center.X))
                lines3.Add(task.pointCloud.Get(Of cvb.Point3f)(vert.tc2.center.Y, vert.tc2.center.X))
            End If

            newVerts.Add(vert)
        Next
        If lines3.Count Then
            gMat.Run(empty)

            Dim points As cvb.Mat = cvb.Mat.FromPixelData(lines3.Count, 3, cvb.MatType.CV_32F, lines3.ToArray)
            Dim gPoints As cvb.Mat = (points * gMat.gMatrix).ToMat

            verticals.Clear()
            For i = 0 To gPoints.Rows - 1 Step 2
                Dim vert = newVerts(i / 2)
                vert.pt1 = gPoints.Get(Of cvb.Point3f)(i + 0, 0)
                vert.pt2 = gPoints.Get(Of cvb.Point3f)(i + 1, 0)
                vert.len3D = distance3D(vert.pt1, vert.pt2)
                Dim arcX = Math.Asin((vert.pt1.X - vert.pt2.X) / vert.len3D) * 57.2958
                Dim arcZ = Math.Asin((vert.pt1.Z - vert.pt2.Z) / vert.len3D) * 57.2958
                If Math.Abs(arcX) <= verts.maxAngleX And Math.Abs(arcZ) <= verts.maxAngleZ Then
                    SetTrueText(vert.tc1.strOut, New cvb.Point(vert.tc1.rect.X, vert.tc1.rect.Y))
                    SetTrueText(vert.tc1.strOut + vbCrLf + Format(arcX, fmt1) + " X" + vbCrLf + Format(arcZ, fmt1) + " Z",
                                New cvb.Point(vert.tc1.rect.X, vert.tc1.rect.Y), 3)
                    DrawLine(dst2, vert.tc1.center, vert.tc2.center, task.HighlightColor)
                    verticals.Add(vert)
                End If
            Next
        End If
        labels(2) = "Starting with " + CStr(verts.verticals.Count) + " there are " + CStr(verticals.Count) + " lines remaining"
    End Sub
End Class








Public Class Line_Nearest : Inherits TaskParent
    Public pt As cvb.Point2f ' How close is this point to the input line?
    Public lp As New PointPair ' the input line.
    Public nearPoint As cvb.Point2f
    Public onTheLine As Boolean
    Public distance As Single
    Public Sub New()
        labels(2) = "Yellow line is input line, white dot is the input point, and the white line is the nearest path to the input line."
        desc = "Find the nearest point on a line"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() And task.heartBeat Then
            lp.p1 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lp.p2 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            pt = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        Dim minX = Math.Min(lp.p1.X, lp.p2.X)
        Dim minY = Math.Min(lp.p1.Y, lp.p2.Y)
        Dim maxX = Math.Max(lp.p1.X, lp.p2.X)
        Dim maxY = Math.Max(lp.p1.Y, lp.p2.Y)

        onTheLine = True
        If lp.p1.X = lp.p2.X Then
            nearPoint = New cvb.Point2f(lp.p1.X, pt.Y)
            If pt.Y < minY Or pt.Y > maxY Then onTheLine = False
        Else
            Dim m = (lp.p1.Y - lp.p2.Y) / (lp.p1.X - lp.p2.X)
            If m = 0 Then
                nearPoint = New cvb.Point2f(pt.X, lp.p1.Y)
                If pt.X < minX Or pt.X > maxX Then onTheLine = False
            Else
                Dim b1 = lp.p1.Y - lp.p1.X * m

                Dim b2 = pt.Y + pt.X / m
                Dim a1 = New cvb.Point2f(0, b2)
                Dim a2 = New cvb.Point2f(dst2.Width, b2 + dst2.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearPoint = New cvb.Point2f(x, m * x + b1)

                If nearPoint.X < minX Or nearPoint.X > maxX Or nearPoint.Y < minY Or nearPoint.Y > maxY Then onTheLine = False
            End If
        End If

        Dim distance1 = Math.Sqrt(Math.Pow(pt.X - lp.p1.X, 2) + Math.Pow(pt.Y - lp.p1.Y, 2))
        Dim distance2 = Math.Sqrt(Math.Pow(pt.X - lp.p2.X, 2) + Math.Pow(pt.Y - lp.p2.Y, 2))
        If onTheLine = False Then nearPoint = If(distance1 < distance2, lp.p1, lp.p2)
        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Yellow)
            DrawLine(dst2, pt, nearPoint, cvb.Scalar.White)
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.White)
        End If
        distance = Math.Sqrt(Math.Pow(pt.X - nearPoint.X, 2) + Math.Pow(pt.Y - nearPoint.Y, 2))
    End Sub
End Class





' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Line_Intersection : Inherits TaskParent
    Public p1 As cvb.Point2f, p2 As cvb.Point2f, p3 As cvb.Point2f, p4 As cvb.Point2f
    Public intersectionPoint As cvb.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            p1 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p3 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p4 = New cvb.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        intersectionPoint = IntersectTest(p1, p2, p3, p4, New cvb.Rect(0, 0, src.Width, src.Height))

        dst2.SetTo(0)
        dst2.Line(p1, p2, cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        dst2.Line(p3, p4, cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        If intersectionPoint <> New cvb.Point2f Then
            DrawCircle(dst2, intersectionPoint, task.DotSize + 4, cvb.Scalar.White)
            labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
        Else
            labels(2) = "Parallel!!!"
        End If
        If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
            labels(2) += " (off screen)"
        End If
    End Sub
End Class






Public Class Line_Gravity : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim nearest As New Line_Nearest
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim pixelDiff = task.gOptions.pixelDiffThreshold

        dst2 = src.Clone
        lines.Run(src)
        If standaloneTest() Then dst3 = lines.dst2

        nearest.lp = task.gravityVec
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, cvb.Scalar.White)
        For Each lp In lines.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.gravityVec.p1, task.gravityVec.p2, New cvb.Rect(0, 0, src.Width, src.Height))
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance
            'DrawLine(dst2,nearest.nearPoint, lp.p1, cvb.Scalar.Red)

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance
            'DrawLine(dst2,nearest.nearPoint, lp.p2, cvb.Scalar.Red)

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, task.HighlightColor)
            End If
        Next

        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cvb.Scalar.White)
        nearest.lp = task.horizonVec
        For Each lp In lines.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.horizonVec.p1, task.horizonVec.p2, New cvb.Rect(0, 0, src.Width, src.Height))
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.Red)
            End If
        Next
        labels(2) = "Slope for gravity is " + Format(task.gravityVec.slope, fmt1) + ".  Slope for horizon is " + Format(task.horizonVec.slope, fmt1)
    End Sub
End Class







Public Class Line_KNN : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim swarm As New Swarm_Basics
    Public Sub New()
        FindSlider("Connect X KNN points").Value = 1
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use KNN to find the other line end points nearest to each endpoint and connect them with a line."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        swarm.options.RunOpt()
        lines.Run(src)
        dst2 = lines.dst2

        dst3.SetTo(0)
        swarm.knn.queries.Clear()
        For Each lp In lines.lpList
            swarm.knn.queries.Add(lp.p1)
            swarm.knn.queries.Add(lp.p2)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        swarm.knn.trainInput = New List(Of cvb.Point2f)(swarm.knn.queries)
        swarm.knn.Run(empty)

        swarm.DrawLines(dst3)
        labels(2) = lines.labels(2)
    End Sub
End Class
