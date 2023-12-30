Imports cv = OpenCvSharp
Public Class Contour_Basics : Inherits VB_Algorithm
    Public contourlist As New List(Of cv.Point())
    Public allContours As cv.Point()()
    Dim rotatedRect As New Rectangle_Rotated
    Public options As New Options_Contours
    Public Sub New()
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone
        If standalone Then
            If heartBeat() = False Then Exit Sub
            rotatedRect.Run(src)
            dst2 = rotatedRect.dst2
            If dst2.Channels = 3 Then
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
            Else
                dst2 = dst2.ConvertScaleAbs(255)
            End If
        Else
            If src.Channels = 3 Then dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If

        If options.retrievalMode = cv.RetrievalModes.FloodFill Then dst2.ConvertTo(dst2, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)

        contourlist.Clear()
        Dim minPixels = gOptions.minPixelsSlider.Value
        For Each c In allContours
            Dim area = cv.Cv2.ContourArea(c)
            If area >= minPixels And c.Length >= options.minLength Then contourlist.Add(c)
        Next

        dst3.SetTo(0)
        For Each ctr In allContours.ToArray
            vbDrawContour(dst3, ctr.ToList, cv.Scalar.Yellow)
        Next
    End Sub
End Class






Public Class Contour_RotatedRects : Inherits VB_Algorithm
    Public rotatedRect As New Rectangle_Rotated
    Dim basics As New Contour_Basics
    Public Sub New()
        labels(3) = "Find contours of several rotated rects"
        desc = "Demo options on FindContours."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim imageInput As New cv.Mat
        rotatedRect.Run(src)
        imageInput = rotatedRect.dst2
        If imageInput.Channels = 3 Then
            dst2 = imageInput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
        Else
            dst2 = imageInput.ConvertScaleAbs(255)
        End If

        basics.Run(dst2)
        dst2 = basics.dst2
        dst3 = basics.dst3
    End Sub
End Class










' https://github.com/SciSharp/SharpCV/blob/master/src/SharpCV.Examples/Program.cs
Public Class Contour_RemoveLines : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Morphology width/height", 1, 100, 20)
            sliders.setupTrackBar("MorphologyEx iterations", 1, 5, 1)
        End If
        labels(2) = "Original image"
        labels(3) = "Original with horizontal/vertical lines removed"
        desc = "Remove the lines from an invoice image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static morphSlider = findSlider("Morphology width/height")
        Static morphExSlider = findSlider("MorphologyEx iterations")
        Dim tmp = cv.Cv2.ImRead(task.homeDir + "Data/invoice.jpg")
        Dim dstSize = New cv.Size(src.Height / tmp.Height * src.Width, src.Height)
        Dim dstRect = New cv.Rect(0, 0, dstSize.Width, src.Height)
        tmp = tmp.Resize(dstSize)
        dst2 = tmp.Resize(dst2.Size)
        Dim gray = tmp.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(CInt(morphSlider.Value), 1))
        Dim removedH As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedH, cv.MorphTypes.Open, hkernel,, morphExSlider.Value)
        Dim cnts = cv.Cv2.FindContoursAsArray(removedH, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, task.lineWidth)
        Next

        Dim vkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(1, CInt(morphSlider.Value)))
        Dim removedV As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedV, cv.MorphTypes.Open, vkernel,, morphExSlider.Value)
        cnts = cv.Cv2.FindContoursAsArray(removedV, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, task.lineWidth)
        Next

        dst3 = tmp.Resize(dst3.Size)
        cv.Cv2.ImShow("Altered image at original resolution", tmp)
    End Sub
End Class










Public Class Contour_Edges : Inherits VB_Algorithm
    Dim edges As New Edge_ResizeAdd
    Dim contour As New Contour_Basics
    Public Sub New()
        desc = "Create contours for Edge_MotionAccum"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        dst2 = edges.dst2

        contour.Run(dst2)

        dst3.SetTo(0)
        Static lastImage = New cv.Mat(dst3.Size, cv.MatType.CV_8UC3, 0)
        Dim colors As New List(Of cv.Vec3b)
        Dim color As cv.Scalar
        For Each c In contour.allContours
            If c.Count > contour.options.minLength Then
                Dim vec = lastImage.Get(Of cv.Vec3b)(c(0).Y, c(0).X)
                If vec = black Or colors.Contains(vec) Then
                    color = New cv.Scalar(msRNG.Next(10, 240), msRNG.Next(10, 240), msRNG.Next(10, 240)) ' trying to avoid extreme colors... 
                Else
                    color = New cv.Scalar(vec(0), vec(1), vec(2))
                End If
                colors.Add(vec)
                vbDrawContour(dst3, c.ToList, color, -1)
            End If
        Next
        lastImage = dst3.Clone
    End Sub
End Class









Public Class Contour_SidePoints : Inherits VB_Algorithm
    Public vecLeft As cv.Vec3f, vecRight As cv.Vec3f, vecTop As cv.Vec3f, vecBot As cv.Vec3f
    Public ptLeft As cv.Point, ptRight As cv.Point, ptTop As cv.Point, ptBot As cv.Point
    Public sides As New Profile_Basics
    Public Sub New()
        desc = "Find the left/right and top/bottom sides of a contour"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rcSelect

        If sides.corners.Count > 0 And heartBeat() Then
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
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
                dst3.Line(ptLeft, ptRight, cv.Scalar.White, task.lineWidth, task.lineType)
                dst3.Line(ptTop, ptBot, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
            If heartBeat() Then
                strOut = "X     " + vbTab + "Y     " + vbTab + "Z " + vbTab + " 3D location (units=meters)" + vbCrLf
                strOut += vec3fToString(vecLeft) + vbTab + " Left side average (blue)" + vbCrLf
                strOut += vec3fToString(vecRight) + vbTab + " Right side average (red)" + vbCrLf
                strOut += vec3fToString(vecTop) + vbTab + " Top side average (green)" + vbCrLf
                strOut += vec3fToString(vecBot) + vbTab + " Bottom side average (white)" + vbCrLf + vbCrLf
                strOut += "The contour may show points further away but they don't have depth."
            End If
        End If
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class Contour_Foreground : Inherits VB_Algorithm
    Dim km As New Foreground_KMeans2
    Dim contour As New Contour_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Kmeans foreground output", "Contour of foreground"}
        desc = "Build a contour for the foreground"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(task.pcSplit(2))
        dst2 = km.dst2

        contour.Run(dst2)
        dst3.SetTo(0)
        For Each ctr In contour.contourlist
            vbDrawContour(dst3, New List(Of cv.Point)(ctr), 255, -1)
        Next
    End Sub
End Class









Public Class Contour_Depth : Inherits VB_Algorithm
    Dim depth As New Depth_InRange
    Public Sub New()
        desc = "Find and draw the contours of each depth segment."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depth.Run(src)
        dst2 = vbPalette(depth.dst2 * 255 / depth.classCount)
        dst3 = depth.dst3
        labels = depth.labels
    End Sub
End Class













Public Class Contour_Sorted : Inherits VB_Algorithm
    Dim contours As New Contour_Basics
    Dim sortedContours As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
    Dim sortedByArea As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
    Dim diff As New Diff_Basics
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Contours in the detected motion", "Diff output - detected motion"}
        gOptions.PixelDiffThreshold.Value = 25
        desc = "Display the contours from largest to smallest in the motion output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        erode.Run(diff.dst3) ' remove solo points.

        contours.Run(diff.dst3)
        dst2 = contours.dst2
        dst1 = contours.dst2.Clone

        sortedByArea.Clear()
        sortedContours.Clear()
        dst3.SetTo(0)
        Dim minPixels = gOptions.minPixelsSlider.Value
        For i = 0 To contours.contourlist.Count - 1
            Dim area = cv.Cv2.ContourArea(contours.contourlist(i))
            If area > minPixels And contours.contourlist(i).Length > contours.options.minLength Then
                sortedByArea.Add(area, i)
                sortedContours.Add(area, cv.Cv2.ApproxPolyDP(contours.contourlist(i), contours.options.epsilon, True))
                vbDrawContour(dst3, contours.contourlist(i).ToList, cv.Scalar.White, -1)
            End If
        Next

        dilate.Run(dst3)
        dst3 = dilate.dst2

        Dim beforeCount = dst1.CountNonZero
        dst1.SetTo(0, dst3)
        Dim afterCount = dst1.CountNonZero
        setTrueText("Before dilate: " + CStr(beforeCount) + vbCrLf + "After dilate " + CStr(afterCount) + vbCrLf + "Removed = " + CStr(beforeCount - afterCount), 1)

        setTrueText("The motion detected produced " + CStr(sortedContours.Count) + " contours after filtering for length and area.", 3)
    End Sub
End Class






Public Class Contour_Outline : Inherits VB_Algorithm
    Public rc As New rcData
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Create a simplified contour of the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        Dim ptList As List(Of cv.Point) = rc.contour

        dst3.SetTo(0)

        Dim newContour As New List(Of cv.Point)
        rc = task.rcSelect
        If rc.contour.Count = 0 Then Exit Sub
        Dim p1 As cv.Point, p2 As cv.Point
        newContour.Add(p1)
        For i = 0 To rc.contour.Count - 2
            p1 = rc.contour(i)
            p2 = rc.contour(i + 1)
            dst3(rc.rect).Line(p1, p2, cv.Scalar.White, task.lineWidth + 1)
            newContour.Add(p2)
        Next
        rc.contour = New List(Of cv.Point)(newContour)
        dst3(rc.rect).Line(rc.contour(rc.contour.Count - 1), rc.contour(0), cv.Scalar.White, task.lineWidth + 1)

        labels(2) = "Input points = " + CStr(rc.contour.Count)
    End Sub
End Class







Public Class Contour_SelfIntersect : Inherits VB_Algorithm
    Public rc As New rcData
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Search the contour points for duplicates indicating the contour is self-intersecting."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            rc = task.rcSelect
            vbDrawContour(dst2(rc.rect), rc.contour, cv.Scalar.White, -1)
            labels(2) = redC.labels(2)
        End If

        Dim selfInt As Boolean
        Dim ptList As New List(Of String)
        If heartBeat() Then dst1.SetTo(0)
        For Each pt In rc.contour
            Dim ptStr = Format(pt.X, "0000") + Format(pt.Y, "0000")
            If ptList.Contains(ptStr) Then
                Dim pct = ptList.Count / rc.contour.Count
                If pct > 0.1 And pct < 0.9 Then
                    selfInt = True
                    dst1(rc.rect).SetTo(cv.Scalar.White, rc.mask)
                    dst3 = rc.mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                    dst3.Circle(pt, task.dotSize, cv.Scalar.Red, -1, task.lineType)
                End If
            End If
            ptList.Add(ptStr)
        Next
        labels(3) = If(selfInt, "Self intersecting", "Not self-intersecting")
    End Sub
End Class









Public Class Contour_Largest : Inherits VB_Algorithm
    Public bestContour As New List(Of cv.Point)
    Public allContours As cv.Point()()
    Public options As New Options_Contours
    Public Sub New()
        labels = {"", "", "Input to FindContours", "Largest single contour in the input image."}
        desc = "Create a mask from the largest contour of the input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If standalone Then
            Static rotatedRect As New Rectangle_Rotated
            If heartBeat() Then
                rotatedRect.Run(src)
                dst2 = rotatedRect.dst2
            End If
        Else
            dst2 = src
        End If

        If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
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
        If standalone Then
            dst3.SetTo(0)
            If maxIndex >= 0 And maxCount >= 2 Then
                cv.Cv2.DrawContours(dst3, allContours, maxIndex, cv.Scalar.White, -1, task.lineType)
            End If
        End If
    End Sub
End Class







Public Class Contour_Compare : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public options As New Options_Contours
    Public Sub New()
        desc = "Compare findContours options - ApproxSimple, ApproxNone, etc."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim tmp = task.rcSelect.mask.Clone

        Dim allContours As cv.Point()()
        If options.retrievalMode = cv.RetrievalModes.FloodFill Then tmp.ConvertTo(tmp, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(tmp, allContours, Nothing, cv.RetrievalModes.External, options.ApproximationMode)

        dst3.SetTo(0)
        cv.Cv2.DrawContours(dst3(task.rcSelect.rect), allContours, -1, cv.Scalar.Yellow, -1, task.lineType)
    End Sub
End Class







Public Class Contour_Smoothing : Inherits VB_Algorithm
    Dim options As New Options_Contours2
    Dim redC As New RedCloud_FeatureLess
    Public Sub New()
        labels(2) = "Use the options to change how the contour is smoothed."
        desc = "Create a simplified contour of the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        redC.Run(src)
        dst2 = redC.dst2

        Dim rc = task.rcSelect

        dst1.SetTo(0)
        dst3.SetTo(0)

        Dim poorContour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)
        vbDrawContour(dst1(rc.rect), poorContour, cv.Scalar.Yellow)

        Dim newContour = contourBuild(rc.mask, options.ApproximationMode)
        vbDrawContour(dst3(rc.rect), newContour, cv.Scalar.Yellow)

        If heartBeat() Then labels(3) = "Contour points count reduced from " + CStr(poorContour.Count) + " to " + CStr(newContour.Count)
    End Sub
End Class









Public Class Contour_RedCloudCorners : Inherits VB_Algorithm
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            rc = task.rcSelect
        End If

        dst3.SetTo(0)
        dst3.Circle(rc.maxDist, task.dotSize, cv.Scalar.White, task.lineWidth)
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

        vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White)
        For i = 0 To corners.Count - 1
            dst3(rc.rect).Line(center, corners(i), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class Contour_RedCloudEdges : Inherits VB_Algorithm
    Dim redC As New RedColor_Cells
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "EdgeDraw_Basics output", "", "Pixels below are both cell boundaries and edges."}
        desc = "Intersect the cell contours and the edges in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        labels(2) = redC.redC.labels(2) + " - Contours only.  Click anywhere to select a cell"

        dst2.SetTo(0)
        For Each rc In redC.redC.redCells
            vbDrawContour(dst2(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        edges.Run(src)
        dst1 = edges.dst2

        dst3 = dst1 And dst2
    End Sub
End Class






Public Class Contour_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Show all the contours found in the RedCloud output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3.SetTo(0)
        For Each rc In redC.redCells
            vbDrawContour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class Contour_Image : Inherits VB_Algorithm
    Dim colorClass As New Color_Basics
    Public contourlist As New List(Of cv.Point())
    Public allContours As cv.Point()()
    Public options As New Options_Contours
    Public Sub New()
        findRadio("FloodFill").Checked = True
        labels = {"", "", "FindContour input", "Draw contour output"}
        desc = "General purpose contour finder"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        colorClass.Run(src)
        dst2 = colorClass.dst2.Clone

        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            dst2.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, allContours, Nothing, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
            dst1.ConvertTo(dst3, cv.MatType.CV_8UC1)
        Else
            cv.Cv2.FindContours(dst2, allContours, Nothing, options.retrievalMode, options.ApproximationMode)
        End If

        contourlist.Clear()
        For Each ctr In allContours.ToArray
            contourlist.Add(ctr)
            If ctr.Length > options.minLength Then vbDrawContour(dst3, ctr.ToList, cv.Scalar.White)
        Next

        If standalone Or testIntermediate(traceName) Then
            dst2 = vbPalette(dst2 * 255 / colorClass.classCount)
        End If
    End Sub
End Class






Public Class Contour_WholeImage : Inherits VB_Algorithm
    Dim contour As New Contour_Image
    Public Sub New()
        redOptions.DesiredCellSlider.Value = 20
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find the top X contours by size and display them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        contour.Run(src)
        Dim sortedContours As New SortedList(Of Integer, List(Of cv.Point))(New compareAllowIdenticalIntegerInverted)
        For Each tour In contour.contourlist
            sortedContours.Add(tour.Length, tour.ToList)
        Next

        dst2.SetTo(0)
        For i = 1 To sortedContours.Count - 1 ' toss the contour around whole image - element 0.
            Dim tour = sortedContours.ElementAt(i).Value
            vbDrawContour(dst2, tour, 255, task.lineWidth)
            If i >= redOptions.DesiredCellSlider.Value Then Exit For
        Next
    End Sub
End Class






Public Class Contour_CompareToFeatureless : Inherits VB_Algorithm
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "Contour_WholeImage output", "FeatureLess_Basics output"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        contour.Run(src)
        dst2 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2
    End Sub
End Class
