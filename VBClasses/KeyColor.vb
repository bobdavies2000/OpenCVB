Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Namespace VBClasses
    Public Class KeyColor_Basics : Inherits TaskParent
        Dim rcList As New List(Of rcData)
        Dim rcMap As New Mat(task.workRes, MatType.CV_32F, 0)
        Dim edgeline As New EdgeLine_KeyColorOnly
        Dim options As New Options_Contours
        Public Sub New()
            OptionParent.findRadio("FloodFill").Checked = True
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify the key colors using contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim lastResult = dst2.Clone

            edgeline.Run(task.gray)
            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = RetrievalModes.FloodFill Then
                Dim dst As New Mat(task.workRes, MatType.CV_8U, 0)
                edgeline.dst2.ConvertTo(dst, MatType.CV_32SC1)
                FindContours(dst, allContours, Nothing, RetrievalModes.FloodFill, mode)
            Else
                FindContours(edgeline.dst2, allContours, Nothing, options.retrievalMode, mode)
            End If

            Dim sortedList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New Mat(task.workRes, MatType.CV_8U, 0)
            Dim minSize = src.Total * 0.01 ' we are only interested in contours with more than X% of the pixels.
            For Each ptArray In allContours
                Dim rc As New rcData With {.rect = Contour_Core.buildRect(ptArray)}

                tourMat(rc.rect).SetTo(0)
                rc.contour = ptArray.ToList
                Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
                DrawContours(tourMat, listOfPoints, 0, New Scalar(sortedList.Count), -1, LineTypes.Link8)
                Threshold(tourMat(rc.rect), rc.mask, 0, 255, ThresholdTypes.Binary)
                rc.maxDist = rc.buildMaxDist(rc.mask)
                rc.pixels = ContourArea(ptArray)
                If rc.pixels >= minSize Then sortedList.Add(rc.pixels, rc)
            Next

            rcMap.SetTo(0)
            rcList.Clear()
            For i = 1 To sortedList.Values.Count - 1
                Dim rc = sortedList.Values(i)
                rcMap(rc.rect).SetTo(i, rc.mask)
                rc.index = i
                rcList.Add(rc)
            Next

            dst2 = Palettize(rcMap)

            Static clickPoint As cv.Point
            If task.mouseClickFlag Then clickPoint = task.clickPoint
            Dim clickIndex As Integer = rcMap.Get(Of Single)(clickPoint.Y, clickPoint.X)
            SetTrueText(RedC_Basics.displayCell(rcList, clickIndex - 1), 3)
        End Sub
    End Class





    Public Class XR_KeyColor_Contours : Inherits TaskParent
        Public keyList As New List(Of keyData)
        Public keyMap As New Mat(task.workRes, MatType.CV_8U, 0)
        Dim edgeline As New EdgeLine_KeyColorOnly
        Dim options As New Options_Contours
        Public Sub New()
            OptionParent.findRadio("FloodFill").Checked = True
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Identify the key colors using contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim lastResult = dst2.Clone

            edgeline.Run(task.gray)
            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = RetrievalModes.FloodFill Then
                Dim dst As New Mat(task.workRes, MatType.CV_8U, 0)
                edgeline.dst2.ConvertTo(dst, MatType.CV_32SC1)
                FindContours(dst, allContours, Nothing, RetrievalModes.FloodFill, mode)
            Else
                FindContours(edgeline.dst2, allContours, Nothing, options.retrievalMode, mode)
            End If

            Dim sortedList As New SortedList(Of Integer, keyData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New Mat(task.workRes, MatType.CV_8U, 0)
            Dim minSize = src.Total * 0.01 ' we are only interested in contours with more than X% of the pixels.
            For Each ptArray In allContours
                Dim tour As New keyData With {.rect = Contour_Core.buildRect(ptArray)}
                If tour.rect.Width = 0 Or tour.rect.Height = 0 Then Continue For

                tourMat(tour.rect).SetTo(0)
                tour.contour = ptArray.ToList
                Dim listOfPoints = New List(Of List(Of cv.Point))({tour.contour})
                DrawContours(tourMat, listOfPoints, 0, New Scalar(sortedList.Count), -1, LineTypes.Link8)
                Threshold(tourMat(tour.rect), tour.mask, 0, 255, ThresholdTypes.Binary)
                tour.maxDist = keyData.GetMaxDistContour(tour)
                tour.pixels = ContourArea(ptArray)
                If tour.pixels >= minSize Then sortedList.Add(tour.pixels, tour)
            Next

            keyMap.SetTo(0)
            keyList.Clear()
            For i = 1 To sortedList.Values.Count - 1
                Dim tour = sortedList.Values(i)
                keyMap(tour.rect).SetTo(i, tour.mask)
                tour.index = i
                keyList.Add(tour)
            Next

            dst2 = Palettize(keyMap)
        End Sub
    End Class




    Public Class XR_KeyColor_OverDepth : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim keyColors As New XR_KeyColor_Contours
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Overlay the KeyColor_Contours cells on the reduced depth results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            keyColors.Run(task.gray)

            dst1.SetTo(0)
            For i = 1 To keyColors.keyList.Count - 1
                Dim key = keyColors.keyList(i)
                dst1(key.rect).SetTo(key.index, key.mask)
            Next

            dst3 = Palettize(dst1, 0)

            For Each key In keyColors.keyList
            Circle(dst3, key.maxDist, task.DotSize, task.highlight, -1)
            Next
            labels(3) = CStr(keyColors.keyList.Count - 1) + " regions were found with more than 1% of the image."
        End Sub
    End Class




    Public Class XR_KeyColor_OverColor : Inherits TaskParent
        Dim redC As New RedColor_Basics
        Dim keyColors As New XR_KeyColor_Contours
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Overlay the KeyColor_Contours cells on the reduced color results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
        Dim _redC_cvt As New Mat
        CvtColor(keyColors.dst2, _redC_cvt, ColorConversionCodes.BGR2GRAY)
        redC.Run(_redC_cvt)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            keyColors.Run(task.gray)

            dst1.SetTo(0)
            For i = 1 To keyColors.keyList.Count - 1
                Dim key = keyColors.keyList(i)
                dst1(key.rect).SetTo(key.index, key.mask)
            Next

            dst3 = Palettize(dst1, 0)

            For Each key In keyColors.keyList
            Circle(dst3, key.maxDist, task.DotSize, task.highlight, -1)
            Next
            labels(3) = CStr(keyColors.keyList.Count - 1) + " regions were found with more than 1% of the image."
        End Sub
    End Class




    Public Class XR_KeyColor_Straight : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim keyColors As New XR_KeyColor_Contours
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Convert the keyList into an rcList"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rcList.Clear()
            rcMap.SetTo(0)

            keyColors.Run(task.gray)

            keyColors.keyList.RemoveAt(0)
            For Each key In keyColors.keyList
                Dim rc = New rcData(key.mask, key.rect, -1) With {.mapID = rcList.Count + 1, .contour = key.contour}
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
            Next

            dst2 = Palettize(rcMap, 0)
            labels(2) = CStr(rcList.Count - 1) + " cells were found."
        End Sub
    End Class

End Namespace
