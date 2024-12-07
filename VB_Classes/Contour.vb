Imports cvb = OpenCvSharp
Public Class Contour_Basics : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public contourlist As New List(Of cvb.Point())
    Public allContours As cvb.Point()()
    Public options As New Options_Contours
    Public sortedList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        FindRadio("FloodFill").Checked = True
        UpdateAdvice(traceName + ": redOptions color class determines the input.  Use local options in 'Options_Contours' to further control output.")
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        color8U.Run(src)
        dst2 = color8U.dst2

        If options.retrievalMode = cvb.RetrievalModes.FloodFill Then
            dst2.ConvertTo(dst1, cvb.MatType.CV_32SC1)
            cvb.Cv2.FindContours(dst1, allContours, Nothing, cvb.RetrievalModes.FloodFill, cvb.ContourApproximationModes.ApproxSimple)
        Else
            cvb.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
        End If
        If allContours.Count <= 1 Then Exit Sub

        sortedList.Clear()
        For i = 0 To allContours.Count - 1
            If allContours(i).Length < 4 Then Continue For
            Dim count = cvb.Cv2.ContourArea(allContours(i))
            If count > 2 Then sortedList.Add(count, i)
        Next

        dst3.SetTo(0)
        contourlist.Clear()
        dst2 = color8U.dst3
        For i = 0 To sortedList.Count - 1
            Dim tour = allContours(sortedList.ElementAt(i).Value)
            contourlist.Add(tour)
            Dim vec = dst2.Get(Of cvb.Vec3b)(tour(0).Y, tour(0).X)
            Dim color = New cvb.Scalar(vec.Item0, vec.Item1, vec.Item2)
            DrawContour(dst3, tour.ToList, color, -1)
        Next
        labels(3) = $"Top {sortedList.Count} contours found"
    End Sub
End Class







Public Class Contour_General : Inherits TaskParent
    Public contourlist As New List(Of cvb.Point())
    Public allContours As cvb.Point()()
    Public options As New Options_Contours
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            If Not task.heartBeat Then Exit Sub
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Else
            If src.Channels() = 3 Then dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) Else dst2 = src
        End If

        If dst2.Type = cvb.MatType.CV_8U Then
            cvb.Cv2.FindContours(dst2, allContours, Nothing, cvb.RetrievalModes.External,
                            cvb.ContourApproximationModes.ApproxTC89KCOS)
        Else
            If dst2.Type <> cvb.MatType.CV_32S Then dst2.ConvertTo(dst2, cvb.MatType.CV_32S)
            cvb.Cv2.FindContours(dst2, allContours, Nothing, cvb.RetrievalModes.FloodFill,
                            cvb.ContourApproximationModes.ApproxTC89KCOS)
        End If

        contourlist.Clear()
        For Each c In allContours
            Dim area = cvb.Cv2.ContourArea(c)
            If area >= options.minPixels And c.Length >= minLengthContour Then contourlist.Add(c)
        Next

        dst3.SetTo(0)
        For Each ctr In allContours.ToArray
            DrawContour(dst3, ctr.ToList, cvb.Scalar.Yellow)
        Next
    End Sub
End Class





Public Class Contour_GeneralWithOptions : Inherits TaskParent
    Public contourlist As New List(Of cvb.Point())
    Public allContours As cvb.Point()()
    Public options As New Options_Contours
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            If dst2.Channels() = 3 Then
                dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
            Else
                dst2 = dst2.ConvertScaleAbs(255)
            End If
        Else
            If src.Channels() = 3 Then dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) Else dst2 = src
        End If

        If options.retrievalMode = cvb.RetrievalModes.FloodFill Then dst2.ConvertTo(dst2, cvb.MatType.CV_32SC1)
        cvb.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

        contourlist.Clear()
        For Each c In allContours
            Dim area = cvb.Cv2.ContourArea(c)
            If area >= options.minPixels And c.Length >= minLengthContour Then contourlist.Add(c)
        Next

        dst3.SetTo(0)
        For Each ctr In allContours.ToArray
            DrawContour(dst3, ctr.ToList, cvb.Scalar.Yellow)
        Next
    End Sub
End Class






Public Class Contour_RotatedRects : Inherits TaskParent
    Public rotatedRect As New Rectangle_Rotated
    Dim basics As New Contour_General
    Public Sub New()
        labels(3) = "Find contours of several rotated rects"
        desc = "Demo options on FindContours."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim imageInput As New cvb.Mat
        rotatedRect.Run(src)
        imageInput = rotatedRect.dst2
        If imageInput.Channels() = 3 Then
            dst2 = imageInput.CvtColor(cvb.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
        Else
            dst2 = imageInput.ConvertScaleAbs(255)
        End If

        basics.Run(dst2)
        dst2 = basics.dst2
        dst3 = basics.dst3
    End Sub
End Class









' https://github.com/SciSharp/SharpCV/blob/master/src/Sharpcvb.Examples/Program.cs
Public Class Contour_RemoveLines : Inherits TaskParent
    Dim options As New Options_Morphology
    Dim image As cvb.Mat
    Public Sub New()
        UpdateAdvice(traceName + ": use the local options in 'Morphology width/height to show impact'")
        labels = {"", "", "Identified horizontal lines - why is scale factor necessary?", "Identified vertical lines"}
        image = cvb.Cv2.ImRead(task.HomeDir + "Data/invoice.jpg")
        Dim dstSize = New cvb.Size(dst2.Height * dst2.Width / image.Height, dst2.Height)
        Dim dstRect = New cvb.Rect(0, 0, image.Width, dst2.Height)
        image = image.Resize(dstSize)
        desc = "Remove the lines from an invoice image"
    End Sub
    Private Function scaleTour(tour()() As cvb.Point) As cvb.Point()()
        For i = 0 To tour.Count - 1
            Dim tmpTour = New List(Of cvb.Point)
            For Each pt In tour(i)
                tmpTour.Add(New cvb.Point(pt.X * options.scaleFactor, pt.Y))
            Next
            tour(i) = tmpTour.ToArray
        Next
        Return tour
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = image.Resize(dst2.Size)
        dst3 = dst2.Clone
        Dim gray = image.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv Or cvb.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cvb.Cv2.GetStructuringElement(cvb.MorphShapes.Rect, New cvb.Size(options.widthHeight, 1))
        Dim removedH As New cvb.Mat
        cvb.Cv2.MorphologyEx(thresh, removedH, cvb.MorphTypes.Open, hkernel,, options.iterations)
        Dim tour = cvb.Cv2.FindContoursAsArray(removedH, cvb.RetrievalModes.External, cvb.ContourApproximationModes.ApproxSimple)
        tour = scaleTour(tour)
        For i = 0 To tour.Count - 1
            cvb.Cv2.DrawContours(dst2, tour, i, cvb.Scalar.Black, task.lineWidth)
        Next

        Dim vkernel = cvb.Cv2.GetStructuringElement(cvb.MorphShapes.Rect, New cvb.Size(1, options.widthHeight))
        Dim removedV As New cvb.Mat
        thresh = gray.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv Or cvb.ThresholdTypes.Otsu)
        cvb.Cv2.MorphologyEx(thresh, removedV, cvb.MorphTypes.Open, vkernel,, options.iterations)
        tour = cvb.Cv2.FindContoursAsArray(removedV, cvb.RetrievalModes.External, cvb.ContourApproximationModes.ApproxSimple)
        tour = scaleTour(tour)
        For i = 0 To tour.Count - 1
            cvb.Cv2.DrawContours(dst3, tour, i, cvb.Scalar.Black, task.lineWidth)
        Next
    End Sub
End Class










Public Class Contour_Edges : Inherits TaskParent
    Dim edges As New Edge_ResizeAdd
    Dim contour As New Contour_General
    Dim lastImage = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
    Public Sub New()
        desc = "Create contours for Edge_MotionAccum"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst2 = edges.dst2

        contour.Run(dst2)

        dst3.SetTo(0)
        Dim colors As New List(Of cvb.Vec3b)
        Dim color As cvb.Scalar
        For Each c In contour.allContours
            If c.Count > minLengthContour Then
                Dim vec = lastImage.Get(Of cvb.Vec3b)(c(0).Y, c(0).X)
                If vec = black Or colors.Contains(vec) Then
                    color = New cvb.Scalar(msRNG.Next(10, 240), msRNG.Next(10, 240), msRNG.Next(10, 240)) ' trying to avoid extreme colors... 
                Else
                    color = New cvb.Scalar(vec(0), vec(1), vec(2))
                End If
                colors.Add(vec)
                DrawContour(dst3, c.ToList, color, -1)
            End If
        Next
        lastImage = dst3.Clone
    End Sub
End Class









Public Class Contour_SidePoints : Inherits TaskParent
    Public vecLeft As cvb.Vec3f, vecRight As cvb.Vec3f, vecTop As cvb.Vec3f, vecBot As cvb.Vec3f
    Public ptLeft As cvb.Point, ptRight As cvb.Point, ptTop As cvb.Point, ptBot As cvb.Point
    Public sides As New Profile_Basics
    Public Sub New()
        desc = "Find the left/right and top/bottom sides of a contour"
    End Sub
    Private Function vec3fToString(v As cvb.Vec3f) As String
        Return Format(v(0), fmt3) + vbTab + Format(v(1), fmt3) + vbTab + Format(v(2), fmt3)
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rc

        If sides.corners.Count > 0 And task.heartBeat Then
            ptLeft = sides.corners(1)
            ptRight = sides.corners(2)
            ptTop = sides.corners(3)
            ptBot = sides.corners(4)

            vecLeft = sides.corners3D(1)
            vecRight = sides.corners3D(2)

            vecTop = sides.corners3D(3)
            vecBot = sides.corners3D(4)

            If rc.contour.Count > 0 Then
                dst3.SetTo(0)
                DrawContour(dst3(rc.rect), rc.contour, cvb.Scalar.Yellow)
                DrawLine(dst3, ptLeft, ptRight, white)
                DrawLine(dst3, ptTop, ptBot, white)
            End If
            If task.heartBeat Then
                strOut = "X     " + vbTab + "Y     " + vbTab + "Z " + vbTab + " 3D location (units=meters)" + vbCrLf
                strOut += vec3fToString(vecLeft) + vbTab + " Left side average (blue)" + vbCrLf
                strOut += vec3fToString(vecRight) + vbTab + " Right side average (red)" + vbCrLf
                strOut += vec3fToString(vecTop) + vbTab + " Top side average (green)" + vbCrLf
                strOut += vec3fToString(vecBot) + vbTab + " Bottom side average (white)" + vbCrLf + vbCrLf
                strOut += "The contour may show points further away but they don't have depth."
            End If
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Contour_Foreground : Inherits TaskParent
    Dim km As New Foreground_KMeans
    Dim contour As New Contour_General
    Public Sub New()
        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Kmeans foreground output", "Contour of foreground"}
        desc = "Build a contour for the foreground"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        km.Run(task.pcSplit(2))
        dst2 = km.dst2

        contour.Run(dst2)
        dst3.SetTo(0)
        For Each ctr In contour.contourlist
            DrawContour(dst3, New List(Of cvb.Point)(ctr), 255, -1)
        Next
    End Sub
End Class













Public Class Contour_Sorted : Inherits TaskParent
    Dim contours As New Contour_GeneralWithOptions
    Dim sortedContours As New SortedList(Of Integer, cvb.Point())(New compareAllowIdenticalIntegerInverted)
    Dim sortedByArea As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Dim diff As New Diff_Basics
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Dim options As New Options_Contours
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Contours in the detected motion", "Diff output - detected motion"}
        task.gOptions.pixelDiffThreshold = 25
        desc = "Display the contours from largest to smallest in the motion output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src)
        erode.Run(diff.dst2) ' remove solo points.

        contours.Run(diff.dst2)
        dst2 = contours.dst2
        dst1 = contours.dst2.Clone

        sortedByArea.Clear()
        sortedContours.Clear()
        dst3.SetTo(0)
        For i = 0 To contours.contourlist.Count - 1
            Dim area = cvb.Cv2.ContourArea(contours.contourlist(i))
            If area > options.minPixels And contours.contourlist(i).Length > minLengthContour Then
                sortedByArea.Add(area, i)
                sortedContours.Add(area, cvb.Cv2.ApproxPolyDP(contours.contourlist(i),
                                                             contours.options.epsilon, True))
                DrawContour(dst3, contours.contourlist(i).ToList, white, -1)
            End If
        Next

        dilate.Run(dst3)
        dst3 = dilate.dst2

        Dim beforeCount = dst1.CountNonZero
        dst1.SetTo(0, dst3)
        Dim afterCount = dst1.CountNonZero
        SetTrueText("Before dilate: " + CStr(beforeCount) + vbCrLf + "After dilate " + CStr(afterCount) + vbCrLf + "Removed = " + CStr(beforeCount - afterCount), 1)

        SetTrueText("The motion detected produced " + CStr(sortedContours.Count) + " contours after filtering for length and area.", 3)
    End Sub
End Class






Public Class Contour_Outline : Inherits TaskParent
    Public rc As New rcData
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Create a simplified contour of the selected cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        Dim ptList As List(Of cvb.Point) = rc.contour

        dst3.SetTo(0)

        Dim newContour As New List(Of cvb.Point)
        rc = task.rc
        If rc.contour.Count = 0 Then Exit Sub
        Dim p1 As cvb.Point, p2 As cvb.Point
        newContour.Add(p1)
        For i = 0 To rc.contour.Count - 2
            p1 = rc.contour(i)
            p2 = rc.contour(i + 1)
            dst3(rc.rect).Line(p1, p2, white, task.lineWidth + 1)
            newContour.Add(p2)
        Next
        rc.contour = New List(Of cvb.Point)(newContour)
        dst3(rc.rect).Line(rc.contour(rc.contour.Count - 1), rc.contour(0), white, task.lineWidth + 1)

        labels(2) = "Input points = " + CStr(rc.contour.Count)
    End Sub
End Class







Public Class Contour_SelfIntersect : Inherits TaskParent
    Public rc As New rcData
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Search the contour points for duplicates indicating the contour is self-intersecting."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            rc = task.rc
            DrawContour(dst2(rc.rect), rc.contour, white, -1)
            labels(2) = redC.labels(2)
        End If

        Dim selfInt As Boolean
        Dim ptList As New List(Of String)
        dst3 = rc.mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        For Each pt In rc.contour
            Dim ptStr = Format(pt.X, "0000") + Format(pt.Y, "0000")
            If ptList.Contains(ptStr) Then
                Dim pct = ptList.Count / rc.contour.Count
                If pct > 0.1 And pct < 0.9 Then
                    selfInt = True
                    DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Red)
                End If
            End If
            ptList.Add(ptStr)
        Next
        labels(3) = If(selfInt, "Self intersecting - red shows where", "Not self-intersecting")
    End Sub
End Class









Public Class Contour_Largest : Inherits TaskParent
    Public bestContour As New List(Of cvb.Point)
    Public allContours As cvb.Point()()
    Public options As New Options_Contours
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        UpdateAdvice(traceName + ": use the local options in 'Options_Contours'")
        labels = {"", "", "Input to FindContours", "Largest single contour in the input image."}
        desc = "Create a mask from the largest contour of the input."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If standaloneTest() Then
            If task.heartBeat Then
                rotatedRect.Run(src)
                dst2 = rotatedRect.dst2
            End If
        Else
            dst2 = src
        End If

        If dst2.Channels() <> 1 Then dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If options.retrievalMode = cvb.RetrievalModes.FloodFill Then
            dst2.ConvertTo(dst1, cvb.MatType.CV_32SC1)
            cvb.Cv2.FindContours(dst1, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
            dst1.ConvertTo(dst3, cvb.MatType.CV_8UC1)
        Else
            cvb.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
        End If

        Dim maxCount As Integer, maxIndex As Integer
        If allContours.Count = 0 Then Exit Sub
        For i = 0 To allContours.Count - 1
            Dim len = CInt(allContours(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        bestContour = allContours(maxIndex).ToList
        If standaloneTest() Then
            dst3.SetTo(0)
            If maxIndex >= 0 And maxCount >= 2 Then
                DrawContour(dst3, allContours(maxIndex).ToList, white)
            End If
        End If
    End Sub
End Class







Public Class Contour_Compare : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public options As New Options_Contours
    Public Sub New()
        desc = "Compare findContours options - ApproxSimple, ApproxNone, etc."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim tmp = task.rc.mask.Clone

        Dim allContours As cvb.Point()()
        If options.retrievalMode = cvb.RetrievalModes.FloodFill Then tmp.ConvertTo(tmp, cvb.MatType.CV_32SC1)
        cvb.Cv2.FindContours(tmp, allContours, Nothing, cvb.RetrievalModes.External, options.ApproximationMode)

        dst3.SetTo(0)
        cvb.Cv2.DrawContours(dst3(task.rc.rect), allContours, -1, cvb.Scalar.Yellow)
    End Sub
End Class









Public Class Contour_RedCloudCorners : Inherits TaskParent
    Public corners(4 - 1) As cvb.Point
    Public rc As New rcData
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            rc = task.rc
        End If

        dst3.SetTo(0)
        DrawCircle(dst3, rc.maxDist, task.DotSize, white)
        Dim center As New cvb.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
        Dim maxDistance(4 - 1) As Single
        For i = 0 To corners.Length - 1
            corners(i) = center ' default is the center - a triangle shape can omit a corner
        Next
        If rc.contour Is Nothing Then Exit Sub
        For Each pt In rc.contour
            Dim quad As Integer
            If pt.X - center.X >= 0 And pt.Y - center.Y <= 0 Then quad = 0 ' upper right quadrant
            If pt.X - center.X >= 0 And pt.Y - center.Y >= 0 Then quad = 1 ' lower right quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y >= 0 Then quad = 2 ' lower left quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y <= 0 Then quad = 3 ' upper left quadrant
            Dim dist = center.DistanceTo(pt)
            If dist > maxDistance(quad) Then
                maxDistance(quad) = dist
                corners(quad) = pt
            End If
        Next

        DrawContour(dst3(rc.rect), rc.contour, white)
        For i = 0 To corners.Count - 1
            DrawLine(dst3(rc.rect), center, corners(i), white)
        Next
    End Sub
End Class







Public Class Contour_RedCloudEdges : Inherits TaskParent
    Dim redC As New RedCloud_Cells
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "EdgeDraw_Basics output", "", "Pixels below are both cell boundaries and edges."}
        desc = "Intersect the cell contours and the edges in the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        labels(2) = redC.redC.labels(2) + " - Contours only.  Click anywhere to select a cell"

        dst2.SetTo(0)
        For Each rc In task.redCells
            DrawContour(dst2(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        edges.Run(src)
        dst1 = edges.dst2

        dst3 = dst1 And dst2
    End Sub
End Class






Public Class Contour_RedCloud : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Show all the contours found in the RedCloud output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3.SetTo(0)
        For Each rc In task.redCells
            DrawContour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class Contour_CompareToFeatureless : Inherits TaskParent
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "Contour_WholeImage output", "FeatureLess_Basics output"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        contour.Run(src)
        dst2 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2
    End Sub
End Class







Public Class Contour_Smoothing : Inherits TaskParent
    Dim options As New Options_Contours2
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "The white outline is the truest contour while the red is the selected approximation."
        desc = "Compare contours of the selected cell. Cells are offset to help comparison."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        Dim rc = task.rc

        dst1.SetTo(0)
        dst3.SetTo(0)

        Dim bestContour = ContourBuild(rc.mask, cvb.ContourApproximationModes.ApproxNone)
        DrawContour(dst3(rc.rect), bestContour, white, task.lineWidth + 3)

        Dim approxContour = ContourBuild(rc.mask, options.ApproximationMode)
        DrawContour(dst3(rc.rect), approxContour, cvb.Scalar.Red)

        If task.heartBeat Then labels(2) = "Contour points count reduced from " + CStr(bestContour.Count) +
                                           " to " + CStr(approxContour.Count)
    End Sub
End Class







Public Class Contour_RC_AddContour : Inherits TaskParent
    Public contour As New List(Of cvb.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If myFrameCount <> task.frameCount Then
            options.RunOpt() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            reduction.Run(src)
            src = reduction.dst2
        End If

        Dim allContours As cvb.Point()()
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        cvb.Cv2.FindContours(src, allContours, Nothing, cvb.RetrievalModes.External, options.ApproximationMode)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To allContours.Count - 1
            Dim len = CInt(allContours(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        dst2 = src
        If allContours.Count = 0 Then Exit Sub
        Dim contour = New List(Of cvb.Point)(allContours(maxIndex).ToList)
        DrawContour(dst2, contour, 255, task.lineWidth)
    End Sub
End Class







Public Class Contour_Gray : Inherits TaskParent
    Public contour As New List(Of cvb.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If myFrameCount <> task.frameCount Then
            options.RunOpt() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics"
            reduction.Run(src)
            src = reduction.dst2
        End If

        Dim allContours As cvb.Point()()
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        cvb.Cv2.FindContours(src, allContours, Nothing, cvb.RetrievalModes.External, options.ApproximationMode)
        If allContours.Count = 0 Then Exit Sub

        dst2 = src
        For Each tour In allContours
            DrawContour(dst2, tour.ToList, white, task.lineWidth)
        Next
        labels(2) = $"There were {allContours.Count} contours found."
    End Sub
End Class









Public Class Contour_WholeImage : Inherits TaskParent
    Dim contour As New Contour_Basics
    Public Sub New()
        FindSlider("Max contours").Value = 20
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find the top X contours by size and display them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        contour.Run(src)
        Dim sortedContours As New SortedList(Of Integer, List(Of cvb.Point))(New compareAllowIdenticalIntegerInverted)
        For Each tour In contour.contourlist
            sortedContours.Add(tour.Length, tour.ToList)
        Next

        dst2.SetTo(0)
        For i = 0 To sortedContours.Count - 1
            Dim tour = sortedContours.ElementAt(i).Value
            DrawContour(dst2, tour, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class Contour_DepthTiers : Inherits TaskParent
    Public options As New Options_Contours
    Public optionsTiers As New Options_DepthTiers
    Public classCount As Integer
    Public contourlist As New List(Of cvb.Point())
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        FindRadio("FloodFill").Checked = True
        UpdateAdvice(traceName + ": redOptions color class determines the input.  Use local options in 'Options_Contours' to further control output.")
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.pcSplit(2).ConvertTo(dst1, cvb.MatType.CV_32S, 100 / optionsTiers.cmPerTier, 1)

        Dim allContours As cvb.Point()()
        cvb.Cv2.FindContours(dst1, allContours, Nothing, cvb.RetrievalModes.FloodFill, cvb.ContourApproximationModes.ApproxSimple)
        If allContours.Count <= 1 Then Exit Sub

        Dim sortedList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To allContours.Count - 1
            If allContours(i).Length < 4 Then Continue For
            Dim count = cvb.Cv2.ContourArea(allContours(i))
            If count < options.minPixels Then Continue For
            If count > 2 Then sortedList.Add(count, i)
        Next

        dst2.SetTo(0)
        contourlist.Clear()
        For i = 0 To sortedList.Count - 1
            Dim tour = allContours(sortedList.ElementAt(i).Value)
            Dim val = dst2.Get(Of Byte)(tour(0).Y, tour(0).X)
            If val = 0 Then
                Dim index = dst1.Get(Of Integer)(tour(0).Y, tour(0).X)
                contourlist.Add(tour)
                DrawContour(dst2, tour.ToList, index, -1)
            End If
        Next

        dst2.SetTo(1, dst2.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv))
        classCount = task.MaxZmeters * 100 / optionsTiers.cmPerTier

        If standaloneTest() Then dst3 = ShowPalette(dst2 * 255 / classCount)
        labels(3) = $"All depth pixels are assigned a tier with {classCount} contours."
    End Sub
End Class





Public Class Contour_FromPoints : Inherits TaskParent
    Dim contour As New Contour_Basics
    Dim random As New Random_Basics
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 3
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Create a contour from some random points"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            random.Run(src)
            dst2.SetTo(0)
            For Each p1 In random.PointList
                For Each p2 In random.PointList
                    DrawLine(dst2, p1, p2, white)
                Next
            Next
        End If

        Dim hullPoints = cvb.Cv2.ConvexHull(random.PointList.ToArray, True).ToList

        Dim hull As New List(Of cvb.Point)
        For Each pt In hullPoints
            hull.Add(New cvb.Point(pt.X, pt.Y))
        Next

        dst3.SetTo(0)
        DrawContour(dst3, hull, white, -1)
    End Sub
End Class
