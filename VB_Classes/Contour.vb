Imports cv = OpenCvSharp
Public Class Contour_Basics : Inherits TaskParent
    Public options As New Options_Contours
    Public classCount As Integer
    Public contourList As New List(Of contourData)
    Public Sub New()
        OptionParent.findRadio("List").Checked = True
        labels(3) = "Input to OpenCV's FindContours"
        desc = "General purpose contour finder"
    End Sub
    Public Shared Function sortContours(allContours As cv.Point()(), maxContours As Integer) As List(Of contourData)
        Dim sortedList As New SortedList(Of Integer, contourData)(New compareAllowIdenticalIntegerInverted)
        Dim tourMat As New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        For Each tour In allContours
            Dim contour = New contourData
            contour.pixels = cv.Cv2.ContourArea(tour)
            contour.points = New List(Of cv.Point)(tour)
            If contour.pixels > task.color.Total * 3 / 4 Then Continue For
            If tour.Count < 4 Then Continue For

            contour.rect = contour.buildRect(tour)
            contour.center = New cv.Point(contour.rect.TopLeft.X + contour.rect.Width / 2,
                                          contour.rect.TopLeft.Y + contour.rect.Height / 2)

            If contour.rect.Width = 0 Or contour.rect.Height = 0 Then Continue For

            tourMat(contour.rect).SetTo(0)
            Dim listOfPoints = New List(Of List(Of cv.Point))({tour.ToList})
            cv.Cv2.DrawContours(tourMat, listOfPoints, 0, New cv.Scalar(sortedList.Count), -1, cv.LineTypes.Link8)
            contour.mask = tourMat(contour.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
            contour.depth = task.pcSplit(2)(contour.rect).Mean(task.depthMask(contour.rect))(0)
            contour.mm = GetMinMaxShared(task.pcSplit(2)(contour.rect), contour.mask)
            sortedList.Add(contour.pixels, contour)
        Next

        Dim contourList As New List(Of contourData)({New contourData})
        For Each contour In sortedList.Values
            contour.index = contourList.Count
            contourList.Add(contour)
            If contourList.Count >= maxContours Then Exit For
        Next
        Return contourList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = task.edges.dst2

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
        Else
            cv.Cv2.FindContours(dst3, allContours, Nothing, options.retrievalMode, mode)
        End If
        If allContours.Count <= 1 Then Exit Sub

        contourList = sortContours(allContours, 255)
        task.contours.contourMap.SetTo(0)
        For Each contour In contourList
            task.contours.contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next
        classCount = contourList.Count

        dst2 = ShowPalette(task.contours.contourMap)
        If task.toggleOn Then
            For Each contour In contourList
                dst2.Rectangle(contour.rect, task.highlight, task.lineWidth)
            Next
        End If

        labels(2) = CStr(contourList.Count) + " contours were found"
    End Sub
End Class







Public Class Contour_Bricks : Inherits TaskParent
    Public Sub New()
        desc = "Display the full and partial bricks in each contour"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.contours.contourMap)
        dst2.SetTo(white, task.contours.contourMap.Threshold(0, 255, cv.ThresholdTypes.BinaryInv))

        Static pt = task.ClickPoint
        If task.mouseClickFlag Then pt = task.ClickPoint
        Dim index = task.contours.contourMap.Get(Of Byte)(pt.Y, pt.X)
        task.contourD = task.contours.contourList(index)

        labels(2) = task.contours.labels(2)
    End Sub
End Class







Public Class Contour_Regions : Inherits TaskParent
    Public contourList As New List(Of cv.Point())
    Public areaList As New List(Of Integer) ' point counts for each contour in contourList above.
    Public options As New Options_Contours
    Public Sub New()
        dst0 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        OptionParent.findRadio("FloodFill").Checked = True
        labels(3) = "Input to OpenCV's FindContours"
        desc = "General purpose contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
        Else
            cv.Cv2.FindContours(dst3, allContours, Nothing, options.retrievalMode, mode)
        End If
        If allContours.Count <= 1 Then Exit Sub

        Dim sortedList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To allContours.Count - 1
            If allContours(i).Length < 4 Then Continue For
            Dim count = cv.Cv2.ContourArea(allContours(i))
            If count > src.Total * 3 / 4 Then Continue For
            sortedList.Add(count, i)
        Next

        dst0.SetTo(0)
        contourList.Clear()
        areaList.Clear()
        For i = 0 To Math.Min(sortedList.Count, 255) - 1
            Dim ele = sortedList.ElementAt(i)
            contourList.Add(allContours(ele.Value))
            areaList.Add(ele.Key)
            DrawContour(dst0, allContours(ele.Value).ToList, contourList.Count, -1, cv.LineTypes.Link8)
        Next
        dst2 = ShowPalette(dst0)
        labels(2) = $"Top {contourList.Count} contours in contourList from the " + CStr(sortedList.Count) + " found."
    End Sub
End Class







Public Class Contour_Features : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public Sub New()
        labels(3) = "Each of the feature points with their correlation coefficien"
        desc = "Show contours and features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.contours.contourMap)
        feat.Run(src)

        dst3.SetTo(0)
        For Each pt In task.features
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
            dst3.Circle(pt, task.DotSize, task.highlight, -1)
            Dim index = task.bbo.brickMap.Get(Of Single)(pt.Y, pt.X)
            Dim brick = task.bbo.brickList(index)
            SetTrueText(Format(brick.correlation, fmt1), pt, 3)
        Next
        labels(2) = "There are " + CStr(task.contours.contourList.Count) + " contours and " +
                    CStr(task.features.Count) + " features."
    End Sub
End Class







Public Class Contour_BrickPoints : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public Sub New()
        desc = "Show contours and Brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2.Clone
        ptBrick.Run(task.grayStable)

        For Each pt In ptBrick.intensityFeatures
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class






Public Class Contour_Delaunay : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        desc = "Use Delaunay to track maxDist point."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.contours.dst2
        labels(3) = task.contours.labels(2)

        delaunay.inputPoints.Clear()
        Dim maxList As New List(Of cv.Point2f)
        For Each contour In task.contours.contourList
            maxList.Add(GetMaxDist(contour.mask, contour.rect))
            delaunay.inputPoints.Add(GetMaxDist(contour.mask, contour.rect))
        Next

        delaunay.Run(emptyMat)
        dst2 = delaunay.dst2.Clone

        For Each pt In maxList
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class







Public Class Contour_LineRGB : Inherits TaskParent
    Public Sub New()
        desc = "Identify contour by its Lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)

        For Each lp In task.lineRGB.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class Contour_RotatedRects : Inherits TaskParent
    Public rotatedRect As New Rectangle_Rotated
    Dim basics As New Contour_Regions
    Public Sub New()
        labels(3) = "Find contours of several rotated rects"
        desc = "Demo options on FindContours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim imageInput As New cv.Mat
        rotatedRect.Run(src)
        imageInput = rotatedRect.dst2
        If imageInput.Channels() = 3 Then
            dst2 = imageInput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
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
    Dim image As cv.Mat
    Public Sub New()
        labels = {"", "", "Identified horizontal lines - why is scale factor necessary?", "Identified vertical lines"}
        image = cv.Cv2.ImRead(task.HomeDir + "Data/invoice.jpg")
        Dim dstSize = New cv.Size(dst2.Height * dst2.Width / image.Height, dst2.Height)
        Dim dstRect = New cv.Rect(0, 0, image.Width, dst2.Height)
        image = image.Resize(dstSize)
        desc = "Remove the lines from an invoice image"
    End Sub
    Private Function scaleTour(tour()() As cv.Point) As cv.Point()()
        For i = 0 To tour.Count - 1
            Dim tmpTour = New List(Of cv.Point)
            For Each pt In tour(i)
                tmpTour.Add(New cv.Point(pt.X * options.scaleFactor, pt.Y))
            Next
            tour(i) = tmpTour.ToArray
        Next
        Return tour
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = image.Resize(dst2.Size)
        dst3 = dst2.Clone
        Dim gray = image.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(options.widthHeight, 1))
        Dim removedH As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedH, cv.MorphTypes.Open, hkernel,, options.iterations)
        Dim tour = cv.Cv2.FindContoursAsArray(removedH, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        tour = scaleTour(tour)
        For i = 0 To tour.Count - 1
            cv.Cv2.DrawContours(dst2, tour, i, cv.Scalar.Black, task.lineWidth)
        Next

        Dim vkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(1, options.widthHeight))
        Dim removedV As New cv.Mat
        thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)
        cv.Cv2.MorphologyEx(thresh, removedV, cv.MorphTypes.Open, vkernel,, options.iterations)
        tour = cv.Cv2.FindContoursAsArray(removedV, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        tour = scaleTour(tour)
        For i = 0 To tour.Count - 1
            cv.Cv2.DrawContours(dst3, tour, i, cv.Scalar.Black, task.lineWidth)
        Next
    End Sub
End Class










Public Class Contour_SidePoints : Inherits TaskParent
    Public vecLeft As cv.Vec3f, vecRight As cv.Vec3f, vecTop As cv.Vec3f, vecBot As cv.Vec3f
    Public ptLeft As cv.Point, ptRight As cv.Point, ptTop As cv.Point, ptBot As cv.Point
    Public sides As New Profile_Basics
    Public Sub New()
        desc = "Find the left/right and top/bottom sides of a contour"
    End Sub
    Private Function vec3fToString(v As cv.Vec3f) As String
        Return Format(v(0), fmt3) + vbTab + Format(v(1), fmt3) + vbTab + Format(v(2), fmt3)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rcD

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
                DrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
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
    Dim contour As New Contour_Regions
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Kmeans foreground output", "Contour of foreground"}
        desc = "Build a contour for the foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        km.Run(task.pcSplit(2))
        dst2 = km.dst2

        contour.Run(dst2)
        dst3.SetTo(0)
        For Each ctr In contour.contourList
            DrawContour(dst3, New List(Of cv.Point)(ctr), 255, -1)
        Next
    End Sub
End Class







Public Class Contour_Outline : Inherits TaskParent
    Public rc As New rcData
    Public Sub New()
        desc = "Create a simplified contour of the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        Dim ptList As List(Of cv.Point) = rc.contour

        dst3.SetTo(0)

        Dim newContour As New List(Of cv.Point)
        rc = task.rcD
        If rc.contour.Count = 0 Then Exit Sub
        Dim p1 As cv.Point, p2 As cv.Point
        newContour.Add(p1)
        For i = 0 To rc.contour.Count - 2
            p1 = rc.contour(i)
            p2 = rc.contour(i + 1)
            dst3(rc.rect).Line(p1, p2, white, task.lineWidth + 1)
            newContour.Add(p2)
        Next
        rc.contour = New List(Of cv.Point)(newContour)
        dst3(rc.rect).Line(rc.contour(rc.contour.Count - 1), rc.contour(0), white, task.lineWidth + 1)

        labels(2) = "Input points = " + CStr(rc.contour.Count)
    End Sub
End Class







Public Class Contour_SelfIntersect : Inherits TaskParent
    Public rc As New rcData
    Public Sub New()
        desc = "Search the contour points for duplicates indicating the contour is self-intersecting."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedC(src, labels(2))
            rc = task.rcD
            DrawContour(dst2(rc.rect), rc.contour, white, -1)
        End If

        Dim selfInt As Boolean
        Dim ptList As New List(Of String)
        dst3 = rc.mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In rc.contour
            Dim ptStr = Format(pt.X, "0000") + Format(pt.Y, "0000")
            If ptList.Contains(ptStr) Then
                Dim pct = ptList.Count / rc.contour.Count
                If pct > 0.1 And pct < 0.9 Then
                    selfInt = True
                    DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Red)
                End If
            End If
            ptList.Add(ptStr)
        Next
        labels(3) = If(selfInt, "Self intersecting - red shows where", "Not self-intersecting")
    End Sub
End Class









Public Class Contour_Largest : Inherits TaskParent
    Public bestContour As New List(Of cv.Point)
    Public allContours As cv.Point()()
    Public options As New Options_Contours
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        labels = {"", "", "Input to FindContours", "Largest single contour in the input image."}
        desc = "Create a mask from the largest contour of the input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standaloneTest() Then
            If task.heartBeat Then
                rotatedRect.Run(src)
                dst2 = rotatedRect.dst2
            End If
        Else
            dst2 = src
        End If

        If dst2.Channels() <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            dst2.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
            dst1.ConvertTo(dst3, cv.MatType.CV_8UC1)
        Else
            cv.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
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
    Public options As New Options_Contours
    Public Sub New()
        desc = "Compare findContours options - ApproxSimple, ApproxNone, etc."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = runRedC(src, labels(2))

        Dim tmp = task.rcD.mask.Clone

        Dim allContours As cv.Point()()
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then tmp.ConvertTo(tmp, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(tmp, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

        dst3.SetTo(0)
        cv.Cv2.DrawContours(dst3(task.rcD.rect), allContours, -1, cv.Scalar.Yellow)
    End Sub
End Class







Public Class Contour_Smoothing : Inherits TaskParent
    Dim options As New Options_Contours2
    Public Sub New()
        labels(3) = "The white outline is the truest contour while the red is the selected approximation."
        desc = "Compare contours of the selected cell. Cells are offset to help comparison."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        Dim rc = task.rcD

        dst1.SetTo(0)
        dst3.SetTo(0)

        Dim bestContour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)
        DrawContour(dst3(rc.rect), bestContour, white, task.lineWidth + 3)

        Dim approxContour = ContourBuild(rc.mask, options.ApproximationMode)
        DrawContour(dst3(rc.rect), approxContour, cv.Scalar.Red)

        If task.heartBeat Then labels(2) = "Contour points count reduced from " + CStr(bestContour.Count) +
                                           " to " + CStr(approxContour.Count)
    End Sub
End Class






Public Class Contour_FromPoints : Inherits TaskParent
    Dim random As New Random_Basics
    Public Sub New()
        OptionParent.FindSlider("Random Pixel Count").Value = 3
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a contour from some random points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            random.Run(src)
            dst2.SetTo(0)
            For Each p1 In random.PointList
                For Each p2 In random.PointList
                    DrawLine(dst2, p1, p2, white)
                Next
            Next
        End If

        Dim hullPoints = cv.Cv2.ConvexHull(random.PointList.ToArray, True).ToList

        Dim hull As New List(Of cv.Point)
        For Each pt In hullPoints
            hull.Add(New cv.Point(pt.X, pt.Y))
        Next

        dst3.SetTo(0)
        DrawContour(dst3, hull, white, -1)
    End Sub
End Class





Public Class Contour_GeneralWithOptions : Inherits TaskParent
    Public contourlist As New List(Of cv.Point())
    Public allContours As cv.Point()()
    Public options As New Options_Contours
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            If Not task.heartBeat Then Exit Sub
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            If dst2.Channels() = 3 Then
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
            Else
                dst2 = dst2.ConvertScaleAbs(255)
            End If
        Else
            dst2 = task.gray
        End If

        If options.retrievalMode = cv.RetrievalModes.FloodFill Then dst2.ConvertTo(dst2, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

        contourlist.Clear()
        For Each c In allContours
            contourlist.Add(c)
        Next

        dst3.SetTo(0)
        For Each ctr In allContours.ToArray
            DrawContour(dst3, ctr.ToList, cv.Scalar.Yellow)
        Next
    End Sub
End Class







Public Class Contour_General : Inherits TaskParent
    Public contourlist As New List(Of cv.Point())
    Public allContours As cv.Point()()
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If Not task.heartBeat Then Exit Sub
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            If src.Type = cv.MatType.CV_8U Then dst2 = src Else dst2 = task.grayStable
        End If

        If dst2.Type = cv.MatType.CV_8U Then
            cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxTC89KCOS)
        Else
            If dst2.Type <> cv.MatType.CV_32S Then dst2.ConvertTo(dst2, cv.MatType.CV_32S)
            cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxTC89KCOS)
        End If

        contourlist.Clear()
        For Each c In allContours
            Dim area = cv.Cv2.ContourArea(c)
            contourlist.Add(c)
            If contourlist.Count >= 255 Then Exit For
        Next

        dst3.SetTo(0)
        For Each ctr In allContours.ToArray
            DrawContour(dst3, ctr.ToList, cv.Scalar.Yellow)
        Next
    End Sub
End Class





Public Class Contour_Basics_FloodFill : Inherits TaskParent
    Public options As New Options_Contours
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "FloodFill retrieval mode contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst2 = ShowPalette(contourMap)

        labels(2) = "FloodFill found the " + CStr(contourList.Count) + " largest contours of the " +
                    CStr(allContours.Count) + " found.  "
    End Sub
End Class





Public Class Contour_Basics_CComp : Inherits TaskParent
    Public options As New Options_Contours
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "CComp retrieval mode contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst1 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.CComp, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst2 = ShowPalette(contourMap)

        labels(2) = "CComp found the " + CStr(contourList.Count) + " largest contours of the " +
                    CStr(allContours.Count) + " found.  "
    End Sub
End Class





Public Class Contour_Basics_Tree : Inherits TaskParent
    Public options As New Options_Contours
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "Tree retrieval mode contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst1 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.Tree, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst2 = ShowPalette(contourMap)

        labels(2) = "Tree found the " + CStr(contourList.Count) + " largest contours of the " +
                    CStr(allContours.Count) + " found.  "
    End Sub
End Class





Public Class Contour_Basics_List : Inherits TaskParent
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public options As New Options_Contours
    Public Sub New()
        desc = "List retrieval mode contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst3, allContours, Nothing, cv.RetrievalModes.List, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst2 = ShowPalette(contourMap)

        If task.heartBeat Then
            labels(2) = "Contour_Basics_List found the " + CStr(contourList.Count) + " largest color contours of the " +
                        CStr(allContours.Count) + " found.  "
        End If
    End Sub
End Class





Public Class Contour_Basics_External : Inherits TaskParent
    Public contourList As New List(Of contourData)
    Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public options As New Options_Contours
    Public Sub New()
        desc = "External retrieval mode contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst1 = srcMustBe8U(src)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.List, mode)
        If allContours.Count <= 1 Then Exit Sub

        contourList = Contour_Basics.sortContours(allContours, 255)
        contourMap.SetTo(0)
        For Each contour In contourList
            contourMap(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst2 = ShowPalette(contourMap)

        labels(2) = "External found the " + CStr(contourList.Count) + " largest contours of the " +
                    CStr(allContours.Count) + " found.  "
    End Sub
End Class








Public Class Contour_Info : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        desc = "Provide details about the selected contour's contourList entry."
    End Sub
    Public Shared Sub setContourSelection(contourlist As List(Of contourData), contourMap As cv.Mat)
        Static pt = task.ClickPoint
        If task.mouseClickFlag Then pt = task.ClickPoint
        Dim index = contourMap.Get(Of Byte)(pt.Y, pt.X)
        If pt = New cv.Point Or index = 0 Then index = 1
        task.contourD = contourlist(index)
        task.color(task.contourD.rect).SetTo(cv.Scalar.White, task.contourD.mask)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            dst2 = ShowPalette(task.contours.contourMap)
            labels(2) = task.contours.labels(2)
        End If

        setContourSelection(task.contours.contourList, task.contours.contourMap)

        Dim contour = task.contourD

        strOut = vbCrLf + vbCrLf
        strOut += "Index = " + CStr(contour.index) + vbCrLf
        strOut += "Depth = " + Format(contour.depth, fmt1) + vbCrLf
        strOut += "Range (m) = " + Format(contour.mm.range, fmt1) + vbCrLf
        strOut += "Number of pixels in the mask: " + CStr(contour.pixels) + vbCrLf

        dst0 = src
        dst0(contour.rect).SetTo(white, contour.mask)

        dst2.Rectangle(contour.rect, task.highlight, task.lineWidth)
        dst2.Circle(contour.center, task.DotSize, task.highlight, -1)

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Contour_Depth : Inherits TaskParent
    Dim backP As New BackProject_Basics_Depth
    Dim options As New Options_Contours
    Public depthContourList As New List(Of contourData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "ShowPalette output of the depth contours in dst2"
        desc = "Isolate the contours in the output of BackProject_Basics_Depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        backP.Run(src)

        Dim allcontours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        dst0 = backP.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.FindContours(dst0, allcontours, Nothing, cv.RetrievalModes.List, mode)
        If allContours.Count <= 1 Then Exit Sub

        depthContourList = Contour_Basics.sortContours(allcontours, 255)
        dst2.SetTo(0)
        For Each contour In depthContourList
            dst2(contour.rect).SetTo(contour.index, contour.mask)
            If contour.index < 6 And contour.index > 0 Then
                Dim str = CStr(contour.index) + " ID" + vbCrLf + CStr(contour.pixels) + " pixels" + vbCrLf +
                            Format(contour.depth, fmt3) + "m depth" + vbCrLf + Format(contour.mm.range, fmt3) + " range in m"
                SetTrueText(str, contour.center, 2)
            End If
        Next

        Static saveTrueData As New List(Of TrueText)
        If task.heartBeatLT Then
            saveTrueData = New List(Of TrueText)(trueData)
        Else
            trueData = New List(Of TrueText)(saveTrueData)
        End If

        dst3 = ShowPalette(dst2)
        labels(2) = "CV_8U format of the " + CStr(depthContourList.Count) + " depth contours"
    End Sub
End Class









Public Class Contour_InfoDepth : Inherits TaskParent
    Public Sub New()
        desc = "Provide details about the selected contour's contourList entry."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            dst2 = ShowPalette(task.contours.contourMap)
            labels(2) = task.contours.labels(2)
        End If

        Static pt = task.ClickPoint
        If task.mouseClickFlag Then pt = task.ClickPoint
        Dim index = task.contours.contourMap.Get(Of Byte)(pt.Y, pt.X)
        task.contourD = task.contours.contourList(index)

        Dim contour = task.contourD

        strOut = vbCrLf + vbCrLf
        strOut += "Index = " + CStr(contour.index) + vbCrLf
        strOut += "Depth = " + Format(contour.depth, fmt1) + vbCrLf
        strOut += "Range (m) = " + Format(contour.mm.range, fmt1) + vbCrLf
        strOut += "Number of pixels in the mask: " + CStr(contour.pixels) + vbCrLf

        dst2.Rectangle(contour.rect, task.highlight, task.lineWidth)
        dst2.Circle(contour.center, task.DotSize, task.highlight, -1)

        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Contour_RedCloud : Inherits TaskParent
    Dim prep As New RedCloud_PrepOutline
    Public options As New Options_Contours
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use the RedCloud_PrepData as input to contours_basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        prep.Run(src)
        prep.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        labels(2) = prep.labels(2)

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.List, mode)
        If allContours.Count <= 1 Then Exit Sub

        Dim contourList = Contour_Basics.sortContours(allContours, 255 - task.histogramBins * 2)
        dst1.SetTo(0)
        For Each contour In contourList
            dst1(contour.rect).SetTo(contour.index, contour.mask)
        Next

        dst3 = ShowPalette(dst1)
        labels(3) = CStr(contourList.Count) + " contours found"
    End Sub
End Class
