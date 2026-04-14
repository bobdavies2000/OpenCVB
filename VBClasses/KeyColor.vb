Imports OpenCvSharp
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class KeyColor_Basics : Inherits TaskParent
        Dim keyList As New List(Of keyData)
        Dim keyMap As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
        Dim edgeline As New EdgeLine_KeyColorOnly
        Dim options As New Options_Contours
        Public Sub New()
            OptionParent.findRadio("FloodFill").Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Identify the key colors using contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim lastResult = dst2.Clone

            edgeline.Run(task.gray)
            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then
                Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                edgeline.dst2.ConvertTo(dst, cv.MatType.CV_32SC1)
                cv.Cv2.FindContours(dst, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            Else
                cv.Cv2.FindContours(edgeline.dst2, allContours, Nothing, options.retrievalMode, mode)
            End If

            Dim sortedList As New SortedList(Of Integer, keyData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
            Dim minSize = src.Total * 0.01 ' we are only interested in contours with more than X% of the pixels.
            For Each ptArray In allContours
                Dim tour = New keyData
                tour.rect = tour.buildRect(ptArray)
                If tour.rect.Width = 0 Or tour.rect.Height = 0 Then Continue For

                tourMat(tour.rect).SetTo(0)
                tour.contour = ptArray.ToList
                Dim listOfPoints = New List(Of List(Of cv.Point))({tour.contour})
                cv.Cv2.DrawContours(tourMat, listOfPoints, 0, New cv.Scalar(sortedList.Count), -1, cv.LineTypes.Link8)
                tour.mask = tourMat(tour.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
                tour.maxDist = tour.GetMaxDistContour(tour)
                tour.pixels = cv.Cv2.ContourArea(ptArray)
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




    Public Class KeyColor_Reduction : Inherits TaskParent
        Dim reduction As New Reduction_BasicsParmInput
        Public Sub New()
            reduction.reductionFactor = 50
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Identify the key colors using contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(task.gray)
            dst2 = reduction.dst3
        End Sub
    End Class



    Public Class NR_KeyColor_Contours : Inherits TaskParent
        Public keyList As New List(Of keyData)
        Public keyMap As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
        Dim edgeline As New EdgeLine_KeyColorOnly
        Dim options As New Options_Contours
        Public Sub New()
            OptionParent.findRadio("FloodFill").Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Identify the key colors using contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim lastResult = dst2.Clone

            edgeline.Run(task.gray)
            Dim allContours As cv.Point()() = Nothing

            Dim mode = options.options2.ApproximationMode
            If options.retrievalMode = cv.RetrievalModes.FloodFill Then
                Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                edgeline.dst2.ConvertTo(dst, cv.MatType.CV_32SC1)
                cv.Cv2.FindContours(dst, allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            Else
                cv.Cv2.FindContours(edgeline.dst2, allContours, Nothing, options.retrievalMode, mode)
            End If

            Dim sortedList As New SortedList(Of Integer, keyData)(New compareAllowIdenticalIntegerInverted)
            Dim tourMat As New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
            Dim minSize = src.Total * 0.01 ' we are only interested in contours with more than X% of the pixels.
            For Each ptArray In allContours
                Dim tour = New keyData
                tour.rect = tour.buildRect(ptArray)
                If tour.rect.Width = 0 Or tour.rect.Height = 0 Then Continue For

                tourMat(tour.rect).SetTo(0)
                tour.contour = ptArray.ToList
                Dim listOfPoints = New List(Of List(Of cv.Point))({tour.contour})
                cv.Cv2.DrawContours(tourMat, listOfPoints, 0, New cv.Scalar(sortedList.Count), -1, cv.LineTypes.Link8)
                tour.mask = tourMat(tour.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
                tour.maxDist = tour.GetMaxDistContour(tour)
                tour.pixels = cv.Cv2.ContourArea(ptArray)
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




    Public Class NR_KeyColor_OverDepth : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim keyColors As New NR_KeyColor_Contours
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
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
                dst3.Circle(key.maxDist, task.DotSize, task.highlight, -1)
            Next
            labels(3) = CStr(keyColors.keyList.Count - 1) + " regions were found with more than 1% of the image."
        End Sub
    End Class




    Public Class NR_KeyColor_OverColor : Inherits TaskParent
        Dim redC As New RedColor_Basics
        Dim keyColors As New NR_KeyColor_Contours
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Overlay the KeyColor_Contours cells on the reduced color results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(keyColors.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
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
                dst3.Circle(key.maxDist, task.DotSize, task.highlight, -1)
            Next
            labels(3) = CStr(keyColors.keyList.Count - 1) + " regions were found with more than 1% of the image."
        End Sub
    End Class




    Public Class NR_KeyColor_Straight : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim keyColors As New NR_KeyColor_Contours
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Convert the keyList into an rcList"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rcList.Clear()
            rcMap.SetTo(0)

            keyColors.Run(task.gray)

            keyColors.keyList.RemoveAt(0)
            For Each key In keyColors.keyList
                Dim rc = New rcData(key.mask, key.rect, -1)
                rc.index = rcList.Count + 1
                rc.contour = key.contour
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            dst2 = Palettize(rcMap, 0)
            labels(2) = CStr(rcList.Count - 1) + " cells were found."
        End Sub
    End Class




    Public Class KeyColor_Delaunay : Inherits TaskParent
        Public redMask As New RedMask_Basics
        Dim delaunay As New Delaunay_Basics
        Public facetList As New List(Of List(Of cv.Point))
        Dim subdiv As New cv.Subdiv2D
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Use the key color maxDist points as input to delaunay."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMask.Run(dst2)
            dst2 = redMask.dst3
            labels(2) = redMask.labels(3)

            strOut = RedUtil_Basics.selectCell(redMask.rcMap, redMask.rcList)
            SetTrueText(strOut, 1)

            Dim inputPoints As New List(Of cv.Point2f)
            subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
            subdiv.Insert(inputPoints)

            'For Each rc In redMask.rcList
            '    inputPoints.Add()
            'Next

            Dim facets = New cv.Point2f()() {Nothing}
            subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

            facetList.Clear()
            For i = 0 To facets.Length - 1
                Dim nextFacet As New List(Of cv.Point)
                For j = 0 To facets(i).Length - 1
                    nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
                Next

                dst3.FillConvexPoly(nextFacet, i, cv.LineTypes.Link4)
                facetList.Add(nextFacet)
            Next

            dst3.ConvertTo(dst1, cv.MatType.CV_8U)
            dst2 = Palettize(dst1)
        End Sub
    End Class
End Namespace