Imports cv = OpenCvSharp
Public Class Contours_Basics : Inherits VBparent
    Public rotatedRect As New Rectangle_Rotated
    Public options As New Contours_Options
    Public contourlist As New List(Of cv.Point())
    Public centroids As New List(Of cv.Point)
    Public sortedContours As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
    Public contours0 As cv.Point()()
    Public Sub New()
        labels(3) = "Contours_Basics with centroid in red"
        task.desc = "Demo options on FindContours."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static areaSlider = findSlider("Contour minimum area")
        Static epsilonSlider = findSlider("Contour epsilon (arc length percent)")
        options.RunClass(Nothing)

        If standalone Then
            Dim imageInput As New cv.Mat
            rotatedRect.RunClass(src)
            imageInput = rotatedRect.dst2
            If imageInput.Channels = 3 Then
                dst2 = imageInput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
            Else
                dst2 = imageInput.ConvertScaleAbs(255)
            End If
        Else
            If src.Channels = 3 Then dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst2 = src
        End If

        If options.retrievalMode = cv.RetrievalModes.FloodFill Then
            Dim img32sc1 As New cv.Mat
            dst2.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
            contours0 = cv.Cv2.FindContoursAsArray(img32sc1, options.retrievalMode, options.ApproximationMode)
            img32sc1.ConvertTo(dst2, cv.MatType.CV_8UC1)
        Else
            contours0 = cv.Cv2.FindContoursAsArray(dst2, options.retrievalMode, options.ApproximationMode)
        End If

        Dim minArea = areaSlider.value
        Dim epsilon = epsilonSlider.value
        If standalone Then
            dst3.SetTo(0)
            Dim cnt = contours0.ToArray
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then
                cv.Cv2.DrawContours(dst3, cnt, -1, cv.Scalar.Yellow, -1, task.lineType)
            Else
                cv.Cv2.DrawContours(dst3, cnt, -1, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
            End If

            For i = 0 To contours0.Length - 1 Step 2
                Dim m = cv.Cv2.Moments(contours0(i), False)
                Dim pt = New cv.Point(m.M10 / m.M00, m.M01 / m.M00)

                Dim area = cv.Cv2.ContourArea(contours0(i))
                If area > minArea Then
                    contourlist.Add(cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    dst3.Circle(pt, task.dotSize, cv.Scalar.Red, -1, task.lineType)
                    cv.Cv2.PutText(dst3, Format(area / 1000, "#0") + "k pixels", New cv.Point(pt.X + task.dotSize, pt.Y), cv.HersheyFonts.HersheyComplexSmall, task.fontSize, cv.Scalar.White)
                Else
                    cv.Cv2.PutText(dst3, "too small", New cv.Point(pt.X + task.dotSize, pt.Y), cv.HersheyFonts.HersheyComplexSmall, task.fontSize, cv.Scalar.White)
                End If
            Next
        Else
            centroids.Clear()
            sortedContours.Clear()
            contourlist.Clear()

            For i = 0 To contours0.Length - 1 Step 2
                Dim m = cv.Cv2.Moments(contours0(i), False)
                Dim pt = New cv.Point(m.M10 / m.M00, m.M01 / m.M00)
                Dim area = cv.Cv2.ContourArea(contours0(i))
                If area > minArea Then
                    sortedContours.Add(area, cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    contourlist.Add(cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    centroids.Add(pt)
                End If
            Next
        End If
    End Sub
End Class







Public Class Contours_Options : Inherits VBparent
    Public retrievalMode As cv.RetrievalModes
    Public ApproximationMode As cv.ContourApproximationModes
    Public Sub New()
        If radio.Setup(caller + " Retrieval Mode", 5) Then
            radio.check(0).Text = "CComp"
            radio.check(1).Text = "External"
            radio.check(2).Text = "FloodFill"
            radio.check(3).Text = "List"
            radio.check(4).Text = "Tree"
            radio.check(2).Checked = True

            radio1.Setup(caller + " ContourApproximation Mode", 4)
            radio1.check(0).Text = "ApproxNone"
            radio1.check(1).Text = "ApproxSimple"
            radio1.check(2).Text = "ApproxTC89KCOS"
            radio1.check(3).Text = "ApproxTC89L1"
            radio1.check(1).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Contour minimum area", 0, 50000, 1000)
            sliders.setupTrackBar(1, "Contour epsilon (arc length percent)", 0, 100, 3)
        End If
        task.desc = "Options for use with the find/draw contours."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frm = findfrm(caller + " Retrieval Mode Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        Static frm1 = findfrm(caller + " ContourApproximation Mode Radio Options")
        For i = 0 To frm1.check.length - 1
            If frm1.check(i).Checked Then
                ApproximationMode = Choose(i + 1, cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                                              cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1)
                Exit For
            End If
        Next
        If standalone Or task.intermediateActive Then
            setTrueText("There is no output for the contours_options - just options to set.")
        End If
    End Sub
End Class





Public Class Contours_RGB : Inherits VBparent
    Public Sub New()
        task.desc = "Find and draw the contour of the largest foreground RGB contour."
        labels(3) = "Background"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim img = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img.SetTo(0, task.noDepthMask)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Integer
        Dim maxNodes As Integer
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next

        If contours0.Length = 0 Then Exit Sub
        If contours0(maxIndex).Length = 0 Then Exit Sub

        Dim hull() = cv.Cv2.ConvexHull(contours0(maxIndex), True)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For i = 0 To hull.Count - 1
            points.Add(New cv.Point(hull(i).X, hull(i).Y))
        Next
        listOfPoints.Add(points)
        dst2.SetTo(0)
        cv.Cv2.DrawContours(dst2, listOfPoints, 0, New cv.Scalar(255, 0, 0), -1)
        cv.Cv2.DrawContours(dst2, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
        dst3.SetTo(0)
        src.CopyTo(dst3, task.noDepthMask)
    End Sub
End Class








' https://github.com/SciSharp/SharpCV/blob/master/src/SharpCV.Examples/Program.cs
Public Class Contours_RemoveLines : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Morphology width/height", 1, 100, 20)
            sliders.setupTrackBar(1, "MorphologyEx iterations", 1, 5, 1)
        End If
        labels(2) = "Original image"
        labels(3) = "Original with horizontal/vertical lines removed"
        task.desc = "Remove the lines from an invoice image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim tmp = cv.Cv2.ImRead(task.parms.homeDir + "Data/invoice.jpg")
        Dim dstSize = New cv.Size(src.Height / tmp.Height * src.Width, src.Height)
        Dim dstRect = New cv.Rect(0, 0, dstSize.Width, src.Height)
        dst2(dstRect) = tmp.Resize(dstSize)
        Dim gray = tmp.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(sliders.trackbar(0).Value, 1))
        Dim removedH As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedH, cv.MorphTypes.Open, hkernel,, sliders.trackbar(1).Value)
        Dim cnts = cv.Cv2.FindContoursAsArray(removedH, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, task.lineWidth)
        Next

        Dim vkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(1, sliders.trackbar(0).Value))
        Dim removedV As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedV, cv.MorphTypes.Open, vkernel,, sliders.trackbar(1).Value)
        cnts = cv.Cv2.FindContoursAsArray(removedV, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, task.lineWidth)
        Next

        dst3(dstRect) = tmp.Resize(dstSize)
        cv.Cv2.ImShow("Altered image at original resolution", tmp)
    End Sub
End Class





Public Class Contours_Depth : Inherits VBparent
    Public contours As New List(Of cv.Point)
    Public Sub New()
        task.desc = "Find and draw the contour of the depth foreground."
        labels(2) = "DepthContour input"
        labels(3) = "DepthContour output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = task.noDepthMask
        dst3.SetTo(0)
        Dim input As cv.Mat = task.depthMask
        Dim contours0 = cv.Cv2.FindContoursAsArray(input, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Integer
        Dim maxNodes As Integer
        For i = 0 To contours0.Length - 1
            Dim c = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < c.Length Then
                maxIndex = i
                maxNodes = c.Length
            End If
        Next
        If contours0.Length Then
            cv.Cv2.DrawContours(dst3, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
            contours.Clear()
            For Each ct In contours0(maxIndex)
                contours.Add(ct)
            Next
        End If
    End Sub
End Class






Public Class Contours_Prediction : Inherits VBparent
    Dim outline As New Contours_Depth
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1)
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Predict the nth point ahead of the current point", 1, 100, 1)
        End If
        labels(2) = "Original contour image"
        labels(3) = "Image after smoothing with Kalman_Basics"
        task.desc = "Predict the next contour point with Kalman to smooth the outline"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        outline.RunClass(src)
        dst2 = outline.dst3
        dst3.SetTo(0)
        Dim stepSize = sliders.trackbar(0).Value
        Dim len = outline.contours.Count
        If len > 0 Then
            kalman.kInput = {outline.contours(0).X, outline.contours(0).Y}
            kalman.RunClass(src)
            Dim origin = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
            For i = 0 To outline.contours.Count - 1 Step stepSize
                Dim pt1 = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
                kalman.kInput = {outline.contours(i Mod len).X, outline.contours(i Mod len).Y}
                kalman.RunClass(src)
                Dim pt2 = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
                dst3.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            Next
            dst3.Line(New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), origin, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        End If
        labels(2) = "There were " + CStr(outline.contours.Count) + " points in this contour"
    End Sub
End Class








Public Class Contours_FindandDraw : Inherits VBparent
    Dim rotatedRect As New Rectangle_Rotated
    Public Sub New()
        findSlider("DrawCount").Value = 5
        labels(2) = "FindandDraw input"
        labels(3) = "FindandDraw output"
        task.desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rotatedRect.RunClass(src)
        dst2 = rotatedRect.dst2
        Dim img = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        Dim tmp As New cv.Mat
        img.ConvertTo(tmp, cv.MatType.CV_32SC1)
        Dim contours0 = cv.Cv2.FindContoursAsArray(tmp, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
        Dim contours As New List(Of cv.Point())
        dst3.SetTo(0)
        For j = 0 To contours0.Length - 1
            Dim nextContour = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
            If nextContour.Length > 2 Then contours.Add(nextContour)
        Next

        cv.Cv2.DrawContours(dst3, contours.ToArray, -1, New cv.Scalar(0, 255, 255), task.lineWidth + 1, task.lineType)
    End Sub
End Class










Public Class Contours_Binarized : Inherits VBparent
    Dim sobel As New Edges_Sobel
    Public basics As New Contours_Basics
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 3

        labels(2) = "Sobel output of grayscale input"
        labels(3) = "DrawContours output after FindContours"
        task.desc = "Find contours using Edges after image is binarized"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sobel.RunClass(src)
        dst2 = sobel.dst2

        basics.RunClass(dst2.Clone)

        Dim cntList = basics.sortedContours
        If cntList.Count = 0 Then Exit Sub ' there were no lines?
        Dim incr = If(cntList.Count > 255, 1, CInt(255 / cntList.Count))
        dst3.SetTo(0)
        Static lastFrame = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To cntList.Count - 1
            Dim lPoints = New List(Of List(Of cv.Point))
            lPoints.Add(cntList.ElementAt(i).Value.ToList)
            cv.Cv2.DrawContours(CType(dst3, cv.InputOutputArray), lPoints, 0, task.scalarColors(i Mod 255), -1)
        Next

        lastFrame = dst3.Clone
    End Sub
End Class
