Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Contour_Basics : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(task.workRes, cv.MatType.CV_32F, 0)
        Public sortContours As New Contour_Sort
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            labels(3) = "Input to OpenCV's FindContours"
            desc = "General purpose contour finder"
        End Sub
        Public Function selectContour() As contourData
            Dim tour As New contourData
            Dim id = contourMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            For Each task.contourD In contourList
                If id = task.contourD.ID Then Exit For
            Next

            For Each tour In contourList
                If tour.ID = id Then Exit For
            Next
            task.color(tour.rect).SetTo(cv.Scalar.White, tour.mask)
            Return task.contourD
        End Function
        Public Shared Function buildContours(input As cv.Mat) As cv.Point()()
            Static options As New Options_Contours
            options.Run()

            Dim mm = GetMinMax(input)
            If mm.maxVal < 255 Then input = (input - mm.minVal) * 255 / (mm.maxVal - mm.minVal)

            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then
                Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                input.ConvertTo(dst, cv.MatType.CV_32SC1)
                cv.Cv2.FindContours(dst, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            Else
                cv.Cv2.FindContours(input, allContours, Nothing, options.retrievalMode, mode)
            End If
            Return allContours
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type = cv.MatType.CV_8U Then
                dst3 = src
            Else
                edgeline.Run(task.grayStable)
                dst3 = edgeline.dst2
            End If

            sortContours.allContours = buildContours(dst3)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2

            classCount = contourList.Count

            labels(2) = CStr(contourList.Count) + " contours were found"
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            edgeline.Dispose()
        End Sub
    End Class






    Public Class Contour_Regions : Inherits TaskParent
        Public contourList As New List(Of cv.Point())
        Public areaList As New List(Of Integer) ' point counts for each contour in contourList above.
        Public options As New Options_Contours
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            dst0 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            OptionParent.findRadio("FloodFill").Checked = True
            labels(3) = "Input to OpenCV's FindContours"
            desc = "General purpose contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            edgeline.Run(task.grayStable)
            dst3 = edgeline.dst2

            Dim allContours As cv.Point()() = Nothing
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
                DrawTour(dst0, allContours(ele.Value).ToList, contourList.Count, -1, cv.LineTypes.Link8)
            Next
            dst2 = PaletteFull(dst0)
            labels(2) = $"Top {contourList.Count} contours in contourList from the " + CStr(sortedList.Count) + " found."
        End Sub
    End Class








    Public Class NR_Contour_Features : Inherits TaskParent
        Dim contours As New Contour_Basics
        Public Sub New()
            If task.feat Is Nothing Then task.feat = New Feature_Basics
            labels(3) = "Each of the feature points with their correlation coefficien"
            desc = "Show contours and features"
        End Sub
        Public Shared Function getCorrelation(lrect As cv.Rect) As Single
            Dim correlationMat As New cv.Mat
            Dim rRect As cv.Rect
            Dim depth = task.pcSplit(2)(lrect).Mean(task.depthmask(lrect))
            If depth = 0 Then Return 0 ' no correlation if there is no depth...
            rRect = lrect
            rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth.Val0
            If rRect.X < 0 Or rRect.X + rRect.Width >= task.color.Width Then
                Return 0 ' no correlation if there is no depth...
            End If

            cv.Cv2.MatchTemplate(task.leftView(lrect), task.rightView(rRect), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            Return correlationMat.Get(Of Single)(0, 0)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst2 = contours.dst2

            dst3.SetTo(0)
            For Each pt In task.feat.features
                DrawCircle(dst2, pt)
                DrawCircle(dst3, pt)
                Dim rect = task.gridRects(task.gridMap.Get(Of Integer)(pt.Y, pt.X))
                Dim correlation = getCorrelation(rect)
                SetTrueText(Format(correlation, fmt1), pt, 3)
            Next
            labels(2) = "There are " + CStr(contours.contourList.Count) + " contours and " +
                    CStr(task.feat.features.Count) + " features."
        End Sub
    End Class







    Public Class NR_Contour_BrickPoints : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Show contours and Brick points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst2 = contours.dst2.Clone
            bPoint.Run(task.grayStable)

            For Each pt In bPoint.ptList
                DrawCircle(dst2, pt)
            Next
        End Sub
    End Class






    Public Class NR_Contour_Delaunay : Inherits TaskParent
        Dim delaunay As New Delaunay_Basics
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Use Delaunay to track maxDist point."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst3 = contours.dst2
            labels(3) = contours.labels(2)

            delaunay.inputPoints.Clear()
            Dim maxList As New List(Of cv.Point2f)
            For Each contour In contours.contourList
                maxList.Add(Distance_Basics.GetMaxDist(contour.mask, contour.rect))
                delaunay.inputPoints.Add(Distance_Basics.GetMaxDist(contour.mask, contour.rect))
            Next

            delaunay.Run(emptyMat)
            dst2 = delaunay.dst2.Clone

            For Each pt In maxList
                DrawCircle(dst2, pt)
            Next
        End Sub
    End Class







    Public Class NR_Contour_LineRGB : Inherits TaskParent
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Identify contour by its Lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst2 = contours.dst2
            labels(2) = contours.labels(2)

            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineWidth)
            Next
        End Sub
    End Class







    Public Class NR_Contour_RotatedRects : Inherits TaskParent
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
    Public Class NR_Contour_RemoveLines : Inherits TaskParent
        Dim options As New Options_Morphology
        Dim image As cv.Mat
        Public Sub New()
            labels = {"", "", "Identified horizontal lines - why is scale factor necessary?", "Identified vertical lines"}
            image = cv.Cv2.ImRead(task.homeDir + "Data/invoice.jpg")
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
            Dim rc = task.oldrcD

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
                    DrawTour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
                    dst3.Line(ptLeft, ptRight, white, task.lineWidth, task.lineWidth)
                    dst3.Line(ptTop, ptBot, white, task.lineWidth, task.lineWidth)
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







    Public Class NR_Contour_Foreground : Inherits TaskParent
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
                DrawTour(dst3, New List(Of cv.Point)(ctr), 255, -1)
            Next
        End Sub
    End Class








    Public Class NR_Contour_SelfIntersect : Inherits TaskParent
        Public rc As New oldrcData
        Public Sub New()
            desc = "Search the contour points for duplicates indicating the contour is self-intersecting."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                dst2 = runRedList(src, labels(2))
                rc = task.oldrcD
                DrawTour(dst2(rc.rect), rc.contour, white, -1)
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
                    DrawTour(dst3, allContours(maxIndex).ToList, white)
                End If
            End If
        End Sub
    End Class







    Public Class NR_Contour_Compare : Inherits TaskParent
        Public options As New Options_Contours
        Public Sub New()
            desc = "Compare findContours options - ApproxSimple, ApproxNone, etc."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = runRedList(src, labels(2))

            Dim tmp = task.oldrcD.mask.Clone

            Dim allContours As cv.Point()() = Nothing
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then tmp.ConvertTo(tmp, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(tmp, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

            dst3.SetTo(0)
            cv.Cv2.DrawContours(dst3(task.oldrcD.rect), allContours, -1, cv.Scalar.Yellow)
        End Sub
    End Class







    Public Class NR_Contour_Smoothing : Inherits TaskParent
        Dim options As New Options_Contours2
        Public Sub New()
            labels(3) = "The white outline is the truest contour while the red is the selected approximation."
            desc = "Compare contours of the selected cell. Cells are offset to help comparison."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            Dim rc = task.oldrcD

            dst1.SetTo(0)
            dst3.SetTo(0)

            Dim bestContour = ContourBuild(rc.mask)
            DrawTour(dst3(rc.rect), bestContour, white, task.lineWidth + 3)

            Dim approxContour = ContourBuild(rc.mask, options.ApproximationMode)
            DrawTour(dst3(rc.rect), approxContour, cv.Scalar.Red)

            If task.heartBeat Then labels(2) = "Contour points count reduced from " + CStr(bestContour.Count) +
                                           " to " + CStr(approxContour.Count)
        End Sub
    End Class






    Public Class NR_Contour_FromPoints : Inherits TaskParent
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
                        dst2.Line(p1, p2, white, task.lineWidth, task.lineWidth)
                    Next
                Next
            End If

            Dim hullPoints = cv.Cv2.ConvexHull(random.PointList.ToArray, True).ToList

            Dim hull As New List(Of cv.Point)
            For Each pt In hullPoints
                hull.Add(New cv.Point(pt.X, pt.Y))
            Next

            dst3.SetTo(0)
            DrawTour(dst3, hull, white, -1)
        End Sub
    End Class





    Public Class NR_NF_Contour_GeneralWithOptions : Inherits TaskParent
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
                DrawTour(dst3, ctr.ToList, cv.Scalar.Yellow)
            Next
        End Sub
    End Class







    Public Class NR_Contour_General : Inherits TaskParent
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
                DrawTour(dst3, ctr.ToList, cv.Scalar.Yellow)
            Next
        End Sub
    End Class








    Public Class NR_NF_Contour_InfoDepth : Inherits TaskParent
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Provide details about the selected contour's contourList entry."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If standalone Then
                dst2 = contours.dst2
                labels(2) = contours.labels(2)
            End If

            task.contourD = contours.selectContour()

            strOut = vbCrLf + vbCrLf
            Dim index = contours.contourList.IndexOf(task.contourD)
            strOut += "Index = " + CStr(index) + vbCrLf
            strOut += "Depth = " + Format(task.contourD.depth, fmt1) + vbCrLf
            strOut += "Range (m) = " + Format(task.contourD.mm.range, fmt1) + vbCrLf
            strOut += "Number of pixels in the mask: " + CStr(task.contourD.pixels) + vbCrLf

            DrawRect(dst2, task.contourD.rect)
            DrawCircle(dst2, task.contourD.maxDist)

            SetTrueText(strOut, 3)
        End Sub
    End Class









    Public Class NR_Contour_Lines : Inherits TaskParent
        Dim hulls As New Contour_Hulls
        Public Sub New()
            desc = "Build a list of the lines in the output of Contour_Hulls"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hulls.Run(src)
            dst3 = hulls.dst3
            labels(3) = hulls.labels(3)

            dst2 = src
            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineWidth)
            Next
            labels(2) = task.lines.labels(2)
        End Sub
    End Class





    Public Class NR_Contour_Isolate : Inherits TaskParent
        Dim contours As New Contour_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Display the contours with and without the selected contour - determines if the contour is needed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst2 = contours.dst2
            labels(2) = contours.labels(2)

            task.contourD = contours.selectContour()

            dst1.SetTo(0)
            Dim indexD = contours.contourList.IndexOf(task.contourD)
            For Each contour In contours.contourList
                Dim index = contours.contourList.IndexOf(contour)
                If index = indexD Then Continue For
                dst1(contour.rect).SetTo(index + 1, contour.mask)
            Next

            dst3 = PaletteFull(dst1)

            For Each contour In contours.contourList
                DrawCircle(dst3, contour.maxDist)
            Next
        End Sub
    End Class








    Public Class Contour_Hulls : Inherits TaskParent
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim contours As New Contour_Basics_List
        Public Sub New()
            desc = "Add hulls and improved contours using ConvexityDefects to each contour cell"
        End Sub
        Public Function getSelectedHull() As contourData
            Dim id = contourMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
            For Each task.contourD In contours.contourList
                If id = task.contourD.ID Then Exit For
            Next
            Return task.contourD
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            labels(2) = contours.labels(2)
            dst2 = contours.dst2

            contourMap.SetTo(0)
            contourList.Clear()
            For Each tour In contours.contourList
                tour.hull = cv.Cv2.ConvexHull(tour.points.ToArray, True).ToList
                Dim index = contours.contourList.IndexOf(tour)
                DrawTour(contourMap, tour.hull, tour.ID Mod 255, -1)
                contourList.Add(tour)
            Next

            dst3 = PaletteFull(contourMap)
            If task.heartBeat Then labels(3) = CStr(contourList.Count) + " hulls"
        End Sub
    End Class





    Public Class NR_Contour_EdgePoints : Inherits TaskParent
        Dim plot As New Plot_OverTimeSingle
        Dim rangeVal As Integer = 3
        Dim contours As New Contour_Basics
        Public Sub New()
            plot.useFixedRange = True
            plot.max = rangeVal
            plot.min = -rangeVal
            task.gOptions.DebugSlider.Value = 50
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            desc = "Find the edge points for each contour rect"
        End Sub
        Public Shared Function EdgePointOffset(ptIn As cv.Point, offset As Integer) As cv.Point
            Dim pt = New cv.Point(ptIn.X, ptIn.Y)
            If pt.X = 0 Then pt.X += offset
            If pt.Y = 0 Then pt.Y += offset
            If pt.X = 0 Then pt.X += offset
            If pt.Y = 0 Then pt.Y += offset
            If pt.X = task.workRes.Width - 1 Then pt.X -= offset
            If pt.Y = task.workRes.Height - 1 Then pt.Y -= offset
            If pt.X = task.workRes.Width - 1 Then pt.X -= offset
            If pt.Y = task.workRes.Height - 1 Then pt.Y -= offset
            Return pt
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            dst0 = dst1.Clone
            dst2 = task.lines.dst3
            labels(2) = task.lines.labels(2)

            Dim ptList As New List(Of cv.Point)
            dst1.SetTo(0)
            Dim maxLines = 3 ' task.gOptions.DebugSlider.Value * 2
            For Each contour In contours.contourList
                Dim index = contours.contourList.IndexOf(contour)
                If index < 3 Then Continue For
                ptList.Add(New cv.Point(0, contour.rect.TopLeft.Y))
                ptList.Add(New cv.Point(contour.rect.TopLeft.X, 0))

                Dim ptIndex = ptList.Count - 1
                For i = 0 To 1
                    Dim pt = ptList(ptIndex - i)
                    dst1.Set(Of Single)(pt.Y, pt.X, index + 1)
                    DrawCircle(dst2, pt)
                Next
                If ptList.Count >= maxLines Then Exit For
            Next

            Dim distancesTop As New List(Of Integer)
            For x = 0 To dst1.Width - 1
                Dim contourIndex = dst1.Get(Of Single)(0, x)
                If contourIndex <> 0 Then
                    For xx = Math.Max(0, x - rangeVal) To Math.Min(dst2.Width - 1, x + rangeVal)
                        Dim lastIndex = dst0.Get(Of Single)(0, xx)
                        If lastIndex = contourIndex Then distancesTop.Add(xx - x)
                    Next
                End If
            Next

            plot.plotData = 0
            If distancesTop.Count > 0 Then plot.plotData = distancesTop.Average
            plot.Run(src)
            dst3 = plot.dst2

            labels(3) = "Top count = " + CStr(distancesTop.Count)
        End Sub
    End Class






    Public Class NR_Contour_Depth : Inherits TaskParent
        Dim prep As New RedPrep_Depth ' only interested in XY reduction.
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Find the contours in the cloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            contours.Run(prep.dst2)
            dst2 = contours.dst2
            labels(2) = contours.labels(2)
        End Sub
    End Class






    Public Class Contour_RotateRect : Inherits TaskParent
        Dim edges As New Edge_Basics
        Dim findRect As New FindMinRect_Basics
        Dim options As New Options_MinArea
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Use minRectArea to busy areas in an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            edges.Run(task.grayStable)

            Dim contours = cv.Cv2.FindContoursAsArray(edges.dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            Dim sortedTours As New SortedList(Of Integer, Tuple(Of cv.RotatedRect, Integer))(New compareAllowIdenticalInteger)
            For i = 0 To contours.Count - 1
                findRect.inputContour = contours(i)
                findRect.Run(emptyMat)
                Dim rr = findRect.minRect
                If rr.BoundingRect.Width > options.minSize And rr.BoundingRect.Height > options.minSize Then
                    Dim tuple = New Tuple(Of cv.RotatedRect, Integer)(rr, i)
                    sortedTours.Add(rr.Size.Width * rr.Size.Height, tuple)
                End If
            Next

            Dim lastDst3 = dst3.Clone
            dst2.SetTo(0)
            dst1.SetTo(0)
            For i = 0 To sortedTours.Values.Count - 1
                Dim tuple = sortedTours.Values(i)
                Rectangle_Basics.DrawRotatedRect(tuple.Item1, dst2, 255)
                DrawTour(dst2, contours(tuple.Item2).ToList, 0, task.lineWidth, cv.LineTypes.Link4)
                DrawTour(dst1, contours(tuple.Item2).ToList, (i Mod 254) + 1, task.lineWidth, cv.LineTypes.Link4)
            Next

            dst3 = PaletteBlackZero(dst1)
            labels(2) = "There were " + CStr(sortedTours.Count) + " contours found with width and height greater than " + CStr(options.minSize)
        End Sub
    End Class






    Public Class NR_Contour_Info : Inherits TaskParent
        Dim contours As New Contour_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst0.Checked = True
            desc = "Provide details about the selected contour's contourList entry."
        End Sub
        Public Shared Function contourDesc(contourMap As cv.Mat, contourList As List(Of contourData)) As String
            Dim tour As New contourData
            Static pt = task.ClickPoint
            If task.mouseClickFlag Then pt = task.ClickPoint
            Dim id = contourMap.Get(Of Integer)(pt.Y, pt.X)
            Dim idFound As Boolean
            For Each tour In contourList
                If tour.ID = id Then
                    idFound = True
                    Exit For
                End If
            Next
            If idFound = False Then tour = contourList(0)
            task.color(tour.rect).SetTo(cv.Scalar.White, tour.mask)

            Dim cDesc As String = ""
            cDesc += "ID = " + CStr(tour.ID) + " (grid index of maxDist)" + vbCrLf
            cDesc += "Depth = " + Format(tour.depth, fmt1) + " m" + vbCrLf
            cDesc += "Range = " + Format(tour.mm.range, fmt1) + " m" + vbCrLf
            cDesc += "Number of pixels in the mask: " + CStr(tour.pixels) + vbCrLf

            cDesc += "MaxDist point = " + CStr(tour.maxDist.X) + ", " + CStr(tour.maxDist.Y) + vbCrLf
            Return cDesc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If standalone Then
                dst2 = contours.dst2
                labels(2) = contours.labels(2)
            End If
            If contours.contourList.Count = 0 Then Exit Sub
            strOut = contourDesc(contours.contourMap, contours.contourList)
            dst0 = src
            dst0(task.contourD.rect).SetTo(white, task.contourD.mask)

            dst2.Rectangle(task.contourD.rect, task.highlight, task.lineWidth)

            SetTrueText(strOut, 3)
        End Sub
    End Class









    Public Class Contour_Sort : Inherits TaskParent
        Public allContours As cv.Point()()
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(task.workRes, cv.MatType.CV_32S, 0)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Sort the contours by size and prepare the contour map"
        End Sub
        Public Shared Function GetMaxDistContour(ByRef contour As contourData) As cv.Point
            Dim mask = contour.mask.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += contour.rect.X
            mm.maxLoc.Y += contour.rect.Y
            Return mm.maxLoc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText(traceName + " is not run standalone.  Use the Contour_SortTest to see how to add " +
                        traceName + " to another algorithm.")
                Exit Sub
            End If

            Dim sortedList As New SortedList(Of Integer, contourData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
            For Each ptArray In allContours
                Dim tour = New contourData
                tour.pixels = cv.Cv2.ContourArea(ptArray)
                If tour.pixels < 5 Then Continue For
                tour.points = New List(Of cv.Point)(ptArray)
                If tour.pixels > task.color.Total * 3 / 4 Then Continue For ' toss this contour - it covers everything...

                tour.rect = tour.buildRect(ptArray)
                If tour.rect.Width = 0 Or tour.rect.Height = 0 Then Continue For

                tourMat(tour.rect).SetTo(0)
                Dim listOfPoints = New List(Of List(Of cv.Point))({ptArray.ToList})
                cv.Cv2.DrawContours(tourMat, listOfPoints, 0, New cv.Scalar(sortedList.Count), -1, cv.LineTypes.Link8)
                tour.mask = tourMat(tour.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
                tour.depth = task.pcSplit(2)(tour.rect).Mean(task.depthMask(tour.rect))(0)
                tour.mm = GetMinMax(task.pcSplit(2)(tour.rect), tour.mask)
                tour.maxDist = GetMaxDistContour(tour)
                tour.ID = task.gridMap.Get(Of Integer)(tour.maxDist.Y, tour.maxDist.X)
                If tour.ID = 0 Then tour.ID = 1 ' stay away from zero...
                tour.age = 1
                sortedList.Add(tour.pixels, tour)
            Next

            Dim contourLast As New List(Of contourData)(contourList)
            Dim contourMapLast = contourMap.Clone

            dst2.SetTo(0)
            contourList.Clear()
            dst1.SetTo(0)
            contourMap.SetTo(0)
            For i = sortedList.Values.Count - 1 To 0 Step -1
                Dim tour = sortedList.Values(i)
                Dim idLast = CInt(contourMapLast.Get(Of Integer)(tour.maxDist.Y, tour.maxDist.X))
                For Each tourLast In contourLast
                    If idLast = tourLast.ID And idLast > 0 Then
                        tour.age = tourLast.age + 1
                        Exit For
                    End If
                Next

                contourList.Add(tour)
                contourMap(tour.rect).SetTo(tour.ID, tour.mask)
                dst1(tour.rect).SetTo(tour.ID Mod 255, tour.mask)
            Next

            dst2 = PaletteBlackZero(dst1)
            Dim matched As Integer
            For Each tour In contourList
                If tour.age > 1 Then matched += 1
            Next

            If contourList.Count > 0 Then
                strOut = NR_Contour_Info.contourDesc(contourMap, contourList)
                If standaloneTest() Then SetTrueText(strOut, 3)
            End If

            If task.heartBeat Then
                labels(2) = "Matched " + CStr(matched) + "/" + CStr(contourList.Count) + " contours to the previous generation"
            End If
        End Sub
    End Class






    Public Class Contour_SortNew : Inherits TaskParent
        Public allContours As cv.Point()()
        Public rcList As New List(Of contourData)
        Public rcMap As New cv.Mat(task.workRes, cv.MatType.CV_32S, 0)
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Sort the contours by size and prepare the contour map"
        End Sub
        Public Shared Function GetMaxDistContour(ByRef contour As contourData) As cv.Point
            Dim mask = contour.mask.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += contour.rect.X
            mm.maxLoc.Y += contour.rect.Y
            Return mm.maxLoc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(task.grayStable)
            If standalone Then
                allContours = Contour_Basics.buildContours(edgeline.dst2)
            End If

            Dim sortedList As New SortedList(Of Integer, contourData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
            For Each ptArray In allContours
                Dim tour = New contourData
                tour.pixels = cv.Cv2.ContourArea(ptArray)
                If tour.pixels < 5 Then Continue For
                tour.points = New List(Of cv.Point)(ptArray)
                If tour.pixels > task.color.Total * 3 / 4 Then Continue For ' toss this contour - it covers everything...

                tour.rect = tour.buildRect(ptArray)
                If tour.rect.Width = 0 Or tour.rect.Height = 0 Then Continue For

                tourMat(tour.rect).SetTo(0)
                Dim listOfPoints = New List(Of List(Of cv.Point))({ptArray.ToList})
                cv.Cv2.DrawContours(tourMat, listOfPoints, 0, New cv.Scalar(sortedList.Count), -1, cv.LineTypes.Link8)
                tour.mask = tourMat(tour.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
                tour.depth = task.pcSplit(2)(tour.rect).Mean(task.depthMask(tour.rect))(0)
                tour.mm = GetMinMax(task.pcSplit(2)(tour.rect), tour.mask)
                tour.maxDist = GetMaxDistContour(tour)
                tour.ID = task.gridMap.Get(Of Integer)(tour.maxDist.Y, tour.maxDist.X)
                If tour.ID = 0 Then tour.ID = 1 ' stay away from zero...
                tour.age = 1
                sortedList.Add(tour.pixels, tour)
            Next

            Dim contourLast As New List(Of contourData)(rcList)
            Dim rcMapLast = rcMap.Clone

            dst2.SetTo(0)
            rcList.Clear()
            dst1.SetTo(0)
            rcMap.SetTo(0)
            For i = sortedList.Values.Count - 1 To 0 Step -1
                Dim tour = sortedList.Values(i)
                Dim idLast = CInt(rcMapLast.Get(Of Integer)(tour.maxDist.Y, tour.maxDist.X))
                For Each tourLast In contourLast
                    If idLast = tourLast.ID And idLast > 0 Then
                        tour.age = tourLast.age + 1
                        Exit For
                    End If
                Next

                rcList.Add(tour)
                rcMap(tour.rect).SetTo(tour.ID, tour.mask)
                dst1(tour.rect).SetTo(tour.ID Mod 255, tour.mask)
            Next

            dst2 = PaletteBlackZero(dst1)
            Dim matched As Integer
            For Each tour In rcList
                If tour.age > 1 Then matched += 1
            Next

            If rcList.Count > 0 Then
                strOut = NR_Contour_Info.contourDesc(rcMap, rcList)
                If standaloneTest() Then SetTrueText(strOut, 3)
            End If

            If task.heartBeat Then
                labels(2) = "Matched " + CStr(matched) + "/" + CStr(rcList.Count) + " contours to the previous generation"
            End If
        End Sub
    End Class





    Public Class Contour_Basics_List : Inherits TaskParent
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Public sortContours As New Contour_SortNew
        Public options As New Options_Contours
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            labels(3) = "Details for the selected contour."
            task.featureOptions.Color8USource.SelectedItem = "EdgeLine_Basics"
            desc = "List retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels <> 1 Then edgeline.Run(task.grayStable) Else edgeline.Run(src)
            dst3 = edgeline.dst2

            sortContours.allContours = Contour_Basics.buildContours(dst3)
            sortContours.Run(src)

            contourList = sortContours.rcList
            contourMap = sortContours.rcMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
            strOut = sortContours.strOut
        End Sub
    End Class



    Public Class NR_Contour_BasicsOld : Inherits TaskParent
        Public classCount As Integer
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(task.workRes, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            labels(3) = "Input to OpenCV's FindContours"
            desc = "General purpose contour finder"
        End Sub
        Public Shared Function selectContour(contours As Contour_Basics_List) As contourData
            Dim tour As New contourData
            Dim id = contours.contourMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
            For Each task.contourD In contours.contourList
                If id = task.contourD.ID Then Exit For
            Next

            For Each tour In contours.contourList
                If tour.ID = id Then Exit For
            Next
            task.color(tour.rect).SetTo(cv.Scalar.White, tour.mask)
            Return task.contourD
        End Function
        Public Shared Function buildContours(input As cv.Mat) As cv.Point()()
            Static options As New Options_Contours
            options.Run()

            Dim mm = GetMinMax(input)
            If mm.maxVal < 255 Then input = (input - mm.minVal) * 255 / (mm.maxVal - mm.minVal)

            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then
                Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                input.ConvertTo(dst, cv.MatType.CV_32SC1)
                cv.Cv2.FindContours(dst, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            Else
                cv.Cv2.FindContours(input, allContours, Nothing, options.retrievalMode, mode)
            End If
            Return allContours
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(task.grayStable)
            If src.Type = cv.MatType.CV_8U Then dst3 = src Else dst3 = edgeline.dst2

            sortContours.allContours = buildContours(dst3)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2

            classCount = contourList.Count

            labels(2) = CStr(contourList.Count) + " contours were found"
        End Sub
    End Class

End Namespace