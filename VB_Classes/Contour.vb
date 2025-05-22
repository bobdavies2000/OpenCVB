Imports cv = OpenCvSharp
Public Class Contour_Basics : Inherits TaskParent
    Public options As New Options_Contours
    Public Sub New()
        dst0 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        task.tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        OptionParent.findRadio("FloodFill").Checked = True
        labels(3) = "Input to OpenCV's FindContours"
        desc = "General purpose contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_8U Then
            Static color8U As New Color8U_Basics
            color8U.Run(src)
            dst3 = color8U.dst2
        Else
            dst3 = src
        End If

        Dim allContours As cv.Point()()
        Dim mode = options.options2.ApproximationMode
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
        Else
            cv.Cv2.FindContours(dst3, allContours, Nothing, options.retrievalMode, mode)
        End If
        If allContours.Count <= 1 Then Exit Sub

        Dim sortedList As New SortedList(Of Integer, tourData)(New compareAllowIdenticalIntegerInverted)
        For Each tour In allContours
            Dim td = New tourData
            ' tour = New List(Of cv.Point)(tour)
            td.pixels = cv.Cv2.ContourArea(tour)
            If td.pixels > src.Total * 3 / 4 Then Continue For
            If tour.Count < 4 Then Continue For

            Dim minX As Single = tour.Min(Function(p) p.X)
            Dim maxX As Single = tour.Max(Function(p) p.X)
            Dim minY As Single = tour.Min(Function(p) p.Y)
            Dim maxY As Single = tour.Max(Function(p) p.Y)

            td.rect = ValidateRect(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
            If td.rect.Width = 0 Or td.rect.Height = 0 Then Continue For

            td.maxDist = GetMaxDist(td.mask, td.rect)

            dst0(td.rect).SetTo(0)
            DrawContour(dst0, tour.ToList, sortedList.Count, -1, cv.LineTypes.Link8)
            td.mask = dst0(td.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)

            sortedList.Add(td.pixels, td)
        Next

        task.tourList.Clear()
        task.tourList.Add(New tourData)
        task.tourMap.SetTo(0)
        For Each td In sortedList.Values
            td.index = task.tourList.Count
            task.tourMap(td.rect).SetTo(td.index, td.mask)
            task.tourList.Add(td)
            If task.tourList.Count >= options.maxContours Then Exit For
        Next

        dst2 = ShowPalette(task.tourMap)

        Static pt = task.ClickPoint
        If task.mouseClickFlag Then pt = task.ClickPoint
        Dim index = task.tourMap.Get(Of Byte)(pt.Y, pt.X)
        task.tourD = task.tourList(index)

        labels(2) = CStr(task.tourList.Count) + " largest contours of the " + CStr(sortedList.Count) + " found."
    End Sub
End Class







Public Class Contour_BasicsOld : Inherits TaskParent
    Public contourList As New List(Of cv.Point())
    Public areaList As New List(Of Integer) ' point counts for each contour in contourList above.
    Public options As New Options_Contours
    Dim color8U As New Color8U_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        OptionParent.findRadio("FloodFill").Checked = True
        labels(3) = "Input to OpenCV's FindContours"
        desc = "General purpose contour finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_8U Then
            color8U.Run(src)
            dst3 = color8U.dst2
        Else
            dst3 = src
        End If

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
        For i = 0 To Math.Min(sortedList.Count, options.maxContours) - 1
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
        desc = "Show contours and features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2.Clone
        feat.Run(src)

        For Each pt In task.features
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class







Public Class Contour_Bricks : Inherits TaskParent
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







Public Class Contour_Info : Inherits TaskParent
    Public Sub New()
        desc = "Provide details about the selected contour's tourList entry."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            dst2 = task.contours.dst2
            labels(2) = task.contours.labels(2)
        End If

        Dim td = task.tourD

        strOut = vbCrLf + vbCrLf
        strOut += "Index = " + CStr(td.index) + vbCrLf
        strOut += "Number of pixels in the mask: " + CStr(td.pixels) + vbCrLf
        strOut += "maxDist = " + td.maxDist.ToString + vbCrLf
        dst2.Rectangle(td.rect, task.highlight, task.lineWidth)
        dst2.Circle(td.maxDist, task.DotSize, task.highlight, -1)

        SetTrueText(strOut, 3)
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
        For Each td In task.tourList
            delaunay.inputPoints.Add(td.maxDist)
        Next

        delaunay.Run(emptyMat)
        dst2 = delaunay.dst2.Clone

        For Each td In task.tourList
            dst2.Circle(td.maxDist, task.DotSize, task.highlight, -1)
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

        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class
Public Class Contour_RotatedRects : Inherits TaskParent
    Public rotatedRect As New Rectangle_Rotated
    Dim basics As New Contour_BasicsOld
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
    Dim contour As New Contour_BasicsOld
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









Public Class Contour_RedCloudCorners : Inherits TaskParent
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedC(src, labels(2))
            rc = task.rcD
        End If

        dst3.SetTo(0)
        DrawCircle(dst3, rc.maxDist, task.DotSize, white)
        Dim center As New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
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
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "EdgeLine_Basics output", "", "Pixels below are both cell boundaries and edges."}
        desc = "Intersect the cell contours and the edges in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedC(src)
        labels(2) = task.redC.labels(2) + " - Contours only.  Click anywhere to select a cell"

        dst2.SetTo(0)
        For Each rc In task.rcList
            DrawContour(dst2(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        dst3 = task.edges.dst2 And dst2
    End Sub
End Class








Public Class Contour_CompareToFeatureless : Inherits TaskParent
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "Contour_WholeImage output", "FeatureLess_Basics output"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contour.Run(src)
        dst2 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst3 ' .Threshold(0, 255, cv.ThresholdTypes.Binary)
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











Public Class Contour_WholeImage : Inherits TaskParent
    Dim contour As New Contour_BasicsOld
    Public Sub New()
        OptionParent.FindSlider("Max contours").Value = 20
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        desc = "Outline all the color contours found in the whole image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contour.Run(src)
        Dim sortedContours As New SortedList(Of Integer, List(Of cv.Point))(New compareAllowIdenticalIntegerInverted)
        For Each tour In contour.contourList
            sortedContours.Add(tour.Length, tour.ToList)
        Next

        dst2.SetTo(0)
        For i = 0 To sortedContours.Count - 1
            Dim tour = sortedContours.ElementAt(i).Value
            DrawContour(dst2, tour, 255, task.lineWidth)
        Next
        labels(2) = CStr(sortedContours.Count) + " contours are shown below."
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











'Public Class Contour_Edges : Inherits TaskParent
'    Dim edges As New Edge_ResizeAdd
'    Dim contour As New Contour_General
'    Dim lastImage = New cv.Mat(New cv.Size(task.dst2.Width, task.dst2.Height), cv.MatType.CV_8UC3, cv.Scalar.All(0))
'    Public Sub New()
'        desc = "Create contours for Edge_MotionAccum"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        edges.Run(src)
'        dst2 = edges.dst2

'        contour.Run(dst2)

'        dst3.SetTo(0)
'        Dim colors As New List(Of cv.Vec3b)
'        Dim color As cv.Scalar
'        For Each c In contour.allContours
'            If c.Count > minLengthContour Then
'                Dim vec = lastImage.Get(Of cv.Vec3b)(c(0).Y, c(0).X)
'                If vec = black.ToVec3b Or colors.Contains(vec) Then
'                    color = New cv.Scalar(msRNG.Next(10, 240), msRNG.Next(10, 240), msRNG.Next(10, 240)) ' trying to avoid extreme colors... 
'                Else
'                    color = New cv.Scalar(vec(0), vec(1), vec(2))
'                End If
'                colors.Add(vec)
'                DrawContour(dst3, c.ToList, color, -1)
'            End If
'        Next
'        lastImage = dst3.Clone
'    End Sub
'End Class





'Public Class Contour_GeneralWithOptions : Inherits TaskParent
'    Public contourlist As New List(Of cv.Point())
'    Public allContours As cv.Point()()
'    Public options As New Options_Contours
'    Dim rotatedRect As New Rectangle_Rotated
'    Public Sub New()
'        labels = {"", "", "FindContour input", "Draw contour output"}
'        desc = "General purpose contour finder"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        options.Run()

'        If standaloneTest() Then
'            If Not task.heartBeat Then Exit Sub
'            rotatedRect.Run(src)
'            dst2 = rotatedRect.dst2
'            If dst2.Channels() = 3 Then
'                dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
'            Else
'                dst2 = dst2.ConvertScaleAbs(255)
'            End If
'        Else
'            dst2 = task.gray
'        End If

'        If options.retrievalMode = cv.RetrievalModes.FloodFill Then dst2.ConvertTo(dst2, cv.MatType.CV_32SC1)
'        cv.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

'        contourlist.Clear()
'        For Each c In allContours
'            contourlist.Add(c)
'        Next

'        dst3.SetTo(0)
'        For Each ctr In allContours.ToArray
'            DrawContour(dst3, ctr.ToList, cv.Scalar.Yellow)
'        Next
'    End Sub
'End Class







'Public Class Contour_General : Inherits TaskParent
'    Public contourlist As New List(Of cv.Point())
'    Public allContours As cv.Point()()
'    Public options As New Options_Contours
'    Dim rotatedRect As New Rectangle_Rotated
'    Public Sub New()
'        labels = {"", "", "FindContour input", "Draw contour output"}
'        desc = "General purpose contour finder"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        If standalone Then
'            If Not task.heartBeat Then Exit Sub
'            rotatedRect.Run(src)
'            dst2 = rotatedRect.dst2
'            dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Else
'            dst2 = task.gray
'        End If

'        If dst2.Type = cv.MatType.CV_8U Then
'            cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxTC89KCOS)
'        Else
'            If dst2.Type <> cv.MatType.CV_32S Then dst2.ConvertTo(dst2, cv.MatType.CV_32S)
'            cv.Cv2.FindContours(dst2, allContours, Nothing, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxTC89KCOS)
'        End If

'        contourlist.Clear()
'        For Each c In allContours
'            Dim area = cv.Cv2.ContourArea(c)
'            contourlist.Add(c)
'            If contourlist.Count >= options.maxContours Then Exit For
'        Next

'        dst3.SetTo(0)
'        For Each ctr In allContours.ToArray
'            DrawContour(dst3, ctr.ToList, cv.Scalar.Yellow)
'        Next
'    End Sub
'End Class












'Public Class Contour_Sorted : Inherits TaskParent
'    Dim contours As New Contour_GeneralWithOptions
'    Dim sortedContours As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
'    Dim sortedByArea As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
'    Dim diff As New Diff_Basics
'    Dim erode As New Erode_Basics
'    Dim dilate As New Dilate_Basics
'    Dim options As New Options_Contours
'    Public Sub New()
'        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
'        If standalone Then task.gOptions.displaydst1.checked = True
'        labels = {"", "", "Contours in the detected motion", "Diff output - detected motion"}
'        task.gOptions.pixelDiffThreshold = 25
'        desc = "Display the contours from largest to smallest in the motion output"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        diff.Run(src)
'        erode.Run(diff.dst2) ' remove solo points.

'        contours.Run(diff.dst2)
'        dst2 = contours.dst2
'        dst1 = contours.dst2.Clone

'        sortedByArea.Clear()
'        sortedContours.Clear()
'        dst3.SetTo(0)
'        For i = 0 To contours.contourlist.Count - 1
'            If area > options.minPixels And contours.contourlist(i).Length > minLengthContour Then
'                sortedByArea.Add(area, i)
'                sortedContours.Add(area, cv.Cv2.ApproxPolyDP(contours.contourlist(i), contours.options.epsilon, True))
'                DrawContour(dst3, contours.contourlist(i).ToList, white, -1)
'            End If
'        Next

'        dilate.Run(dst3)
'        dst3 = dilate.dst2

'        Dim beforeCount = dst1.CountNonZero
'        dst1.SetTo(0, dst3)
'        Dim afterCount = dst1.CountNonZero
'        SetTrueText("Before dilate: " + CStr(beforeCount) + vbCrLf + "After dilate " + CStr(afterCount) + vbCrLf + "Removed = " + CStr(beforeCount - afterCount), 1)

'        SetTrueText("The motion detected produced " + CStr(sortedContours.Count) + " contours after filtering for length and area.", 3)
'    End Sub
'End Class






'Public Class Contour_DepthTiers : Inherits TaskParent
'    Public options As New Options_Contours
'    Public optionsTiers As New Options_DepthTiers
'    Public classCount As Integer
'    Public contourlist As New List(Of cv.Point())
'    Public Sub New()
'        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
'        OptionParent.findRadio("FloodFill").Checked = True
'        labels = {"", "", "FindContour input", "Draw contour output"}
'        desc = "General purpose contour finder"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        options.Run()

'        task.pcSplit(2).ConvertTo(dst1, cv.MatType.CV_32S, 100 / optionsTiers.cmPerTier, 1)

'        Dim allContours As cv.Point()()
'        cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
'        If allContours.Count <= 1 Then Exit Sub

'        Dim sortedList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
'        For i = 0 To allContours.Count - 1
'            If allContours(i).Length < 4 Then Continue For
'            Dim count = cv.Cv2.ContourArea(allContours(i))
'            If count < options.minPixels Then Continue For
'            If count > 2 Then sortedList.Add(count, i)
'        Next

'        dst2.SetTo(0)
'        contourlist.Clear()
'        For i = 0 To sortedList.Count - 1
'            Dim tour = allContours(sortedList.ElementAt(i).Value)
'            Dim val = dst2.Get(Of Byte)(tour(0).Y, tour(0).X)
'            If val = 0 Then
'                Dim index = dst1.Get(Of Integer)(tour(0).Y, tour(0).X)
'                contourlist.Add(tour)
'                DrawContour(dst2, tour.ToList, index, -1)
'            End If
'        Next

'        dst2.SetTo(1, dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv))
'        classCount = task.MaxZmeters * 100 / optionsTiers.cmPerTier

'        If standaloneTest() Then dst3 = ShowPalette(dst2)
'        labels(3) = $"All depth pixels are assigned a tier with {classCount} contours."
'    End Sub
'End Class

