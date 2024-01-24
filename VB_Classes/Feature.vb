Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_Basics : Inherits VB_Algorithm
    Dim Brisk As cv.BRISK
    Public featurePoints As New List(Of cv.Point2f)
    Public options As New Options_Features
    Public Sub New()
        Brisk = cv.BRISK.Create()
        findSlider("Feature Sample Size").Value = 400
        vbAddAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        featurePoints.Clear()
        Dim sampleSize = options.fOptions.featurePoints
        If options.useBRISK Then
            Dim keyPoints = Brisk.Detect(src).ToList
            For Each kp In keyPoints
                If kp.Size >= options.minDistance Then featurePoints.Add(kp.Pt)
            Next
        Else
            featurePoints = cv.Cv2.GoodFeaturesToTrack(src, sampleSize, options.quality, options.minDistance, Nothing, 7, True, 3).ToList
        End If

        Dim color = If(dst2.Channels = 3, cv.Scalar.Yellow, cv.Scalar.White)
        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize, color, -1, task.lineType)
        Next

        labels(2) = "Found " + CStr(featurePoints.Count) + " points with quality = " + CStr(options.quality) +
                    " and minimum distance = " + CStr(options.minDistance)
    End Sub
End Class









' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_BasicsOld : Inherits VB_Algorithm
    Public featurePoints As New List(Of cv.Point2f)
    Public options As New Options_Features
    Public Sub New()
        vbAddAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sampleSize = options.fOptions.featurePoints
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, sampleSize, options.quality, options.minDistance, Nothing, 7, True, 3)

        featurePoints.Clear()
        For i = 0 To features.Length - 1
            Dim pt = features(i)
            featurePoints.Add(pt)
            dst2.Circle(pt, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
        Next

        labels(2) = "Found " + CStr(featurePoints.Count) + " points with quality = " + CStr(options.quality) +
                    " and minimum distance = " + CStr(options.minDistance)
    End Sub
End Class







Public Class Feature_ShiTomasi : Inherits VB_Algorithm
    Dim harris As New Corners_HarrisDetector
    Dim shiTomasi As New Corners_ShiTomasi_CPP
    Public Sub New()
        findSlider("Corner normalize threshold").Value = 15

        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Harris features")
            radio.addRadio("Shi-Tomasi features")
            radio.check(1).Checked = True
        End If

        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static typeRadio = findRadio("Harris features")
        If typeRadio.checked Then
            harris.Run(task.leftView)
            dst2 = harris.dst2.Clone

            harris.Run(task.rightView)
            dst3 = harris.dst2
        Else
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        End If
    End Sub
End Class








Public Class Feature_Tracer : Inherits VB_Algorithm
    Dim features As New Feature_PointsDelaunay
    Public goodList As New List(Of List(Of cv.Point2f)) ' stable points only
    Public Sub New()
        labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
        desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Match."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        features.Run(src)
        dst3 = features.dst2

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(features.feat.featurePoints)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                task.color.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                Dim c = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
                dst2.Circle(pt, task.dotSize + 1, c, -1, task.lineType)
            Next
        Next
        labels(2) = CStr(features.feat.featurePoints.Count) + " features were identified in the image."
    End Sub
End Class









Public Class Feature_CellGrid : Inherits VB_Algorithm
    Dim feat As New Feature_BasicsKNN
    Public cellPopulation As New List(Of Integer) ' count the feature population of each roi
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        desc = "Track the GoodFeatures in each Grid_Basics cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then dst0.SetTo(0)

        dst2 = src

        feat.Run(dst2)
        For Each pt In feat.featurePoints
            Dim val = dst0.Get(Of Byte)(pt.Y, pt.X)
            dst0.Set(Of Byte)(pt.Y, pt.X, If(val = 255, val, val + 1))
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        dst3 = feat.dst2

        cellPopulation.Clear()
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            Dim features = dst0(roi).Sum()
            cellPopulation.Add(features(0))
        Next
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class









Public Class Feature_CellFinder : Inherits VB_Algorithm
    Dim floodCells As New Feature_CellGrid
    Public bestCells As New List(Of cv.Rect)
    Public bestLeftCell As cv.Rect
    Public bestRightCell As cv.Rect
    Public Sub New()
        labels = {"", "", "Input image with marked features - best cells are highlighted", ""}
        desc = "Find 2 cells with the most features but not on the edge (too likely impacted with camera motion)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        floodCells.Run(src)
        dst2 = floodCells.dst2

        Static popSort As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        popSort.Clear()
        For i = 0 To floodCells.cellPopulation.Count - 1
            Dim roi = task.gridList(i)
            If roi.X > 0 And (roi.X + roi.Width) < dst2.Width And roi.Y > 0 And (roi.Y + roi.Height) < dst2.Height Then
                setTrueText(CStr(floodCells.cellPopulation(i)), New cv.Point(roi.X, roi.Y), 3)
                popSort.Add(floodCells.cellPopulation(i), roi)
            End If
        Next

        bestLeftCell = New cv.Rect(-1, -1, 0, 0)
        bestRightCell = New cv.Rect(-1, -1, 0, 0)

        ' if the current best's are toward the top, then just stick with the current ones'.  
        For i = 0 To popSort.Count / 2
            Dim roi = popSort.ElementAt(i).Value
            For Each best In bestCells
                If best = roi Then
                    If roi.X < dst2.Width / 3 Then bestLeftCell = roi ' leftmost cell
                    If roi.X > dst2.Width * 2 / 3 Then bestRightCell = roi ' rightmost cell...
                End If
            Next
            If bestLeftCell.X <> -1 And bestRightCell.X <> -1 Then Exit For
        Next

        For i = 0 To popSort.Count - 1
            If bestLeftCell.X <> -1 And bestRightCell.X <> -1 Then Exit For
            Dim roi = popSort.ElementAt(i).Value
            If roi.X < dst2.Width / 3 Then bestLeftCell = roi ' leftmost cell
            If roi.X > dst2.Width * 2 / 3 Then bestRightCell = roi ' rightmost cell...
        Next

        bestCells.Clear()
        bestCells.Add(bestLeftCell)
        bestCells.Add(bestRightCell)

        dst2.Rectangle(bestLeftCell, task.highlightColor, task.lineWidth + 1)
        dst2.Rectangle(bestRightCell, task.highlightColor, task.lineWidth + 1)

        dst3.SetTo(0)
        dst3.Rectangle(bestLeftCell, task.highlightColor, task.lineWidth + 1)
        dst3.Rectangle(bestRightCell, task.highlightColor, task.lineWidth + 1)
    End Sub
End Class





Public Class Feature_PointsKNN : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public knn As New KNN_Basics
    Public Sub New()
        findSlider("Distance").Value = 30
        labels(2) = "Track Good features 1:1 using KNN_One_to_One"
        desc = "Find good features and track them from one image to the next using KNN 1:1 correspondence."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2

        knn.queries.Clear()
        For Each pt In feat.featurePoints
            knn.queries.Add(pt)
        Next

        knn.Run(src)
        dst2 = src
        dst2 += knn.dst2
    End Sub
End Class







Public Class Feature_PointsDelaunay : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public Sub New()
        labels = {"Good features highlighted", "", "", "Delaunay map of good features - format CV_8U"}
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use Delaunay with the points provided by GoodFeaturesToTrack."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.paused Then Exit Sub
        feat.Run(src)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        For Each pt In feat.featurePoints
            subdiv.Insert(pt)
        Next

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)
        If facets.Count = 0 Then Exit Sub

        Dim ifacet() As cv.Point
        Dim incr As Integer = 255 / facets.Count
        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Count - 1)
            For j = 0 To facets(i).Count - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next

            Dim index = If(heartBeat(), i * incr, dst3.Get(Of Byte)(feat.featurePoints(i).Y, feat.featurePoints(i).X))
            vbDrawContour(dst3, ifacet.ToList, index, -1)
        Next

        dst2 = vbPalette(dst3)
        labels(2) = CStr(feat.featurePoints.Count) + " features were identified in the image."
    End Sub
End Class









Public Class Feature_Line : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim lineDisp As New Line_DisplayInfo
    Dim options As New Options_Features
    Dim match As New Match_tCell
    Public tcells As List(Of tCell)
    Public Sub New()
        Dim tc As tCell
        tcells = New List(Of tCell)({tc, tc})
        labels = {"", "", "Longest line present.", ""}
        desc = "Find and track a line using the end points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
        Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

        Dim threshold = options.fOptions.correlationThreshold
        Dim correlationTest = tcells(0).correlation <= threshold Or tcells(1).correlation <= threshold
        lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
        If task.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or lineDisp.distance < distanceThreshold Then
            lineDisp.myHighLightColor = If(lineDisp.myHighLightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            Dim rSize = options.fOptions.matchCellSize
            lines.subsetRect = New cv.Rect(rSize * 3, rSize * 3, src.Width - rSize * 6, src.Height - rSize * 6)
            lines.Run(src.Clone)

            If lines.mpList.Count = 0 Then
                setTrueText("No lines found.", 3)
                Exit Sub
            End If
            Dim mp = lines.mpList(lines.sortLength.ElementAt(0).Value)

            tcells(0) = match.createCell(src, 0, mp.p1)
            tcells(1) = match.createCell(src, 0, mp.p2)
        End If

        dst2 = src.Clone
        For i = 0 To tcells.Count - 1
            match.tCells(0) = tcells(i)
            match.Run(src)
            tcells(i) = match.tCells(0)
            setTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y))
            setTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y), 3)
        Next

        lineDisp.tcells = New List(Of tCell)(tcells)
        lineDisp.Run(src)
        dst2 = lineDisp.dst2
        setTrueText(lineDisp.strOut, New cv.Point(10, 40), 3)
    End Sub
End Class







Public Class Feature_VerticalVerify : Inherits VB_Algorithm
    Dim linesVH As New Feature_LinesVH
    Public verify As New IMU_VerticalVerify
    Public Sub New()
        desc = "Select a line or group of lines and track the result"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        linesVH.Run(src)

        verify.gCells = New List(Of gravityLine)(linesVH.gCells)
        verify.Run(src)
        dst2 = verify.dst2
    End Sub
End Class








Public Class Feature_LinesVH : Inherits VB_Algorithm
    Public gCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New Line_GCloud
    Dim options As New Options_Features
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Vertical lines")
            radio.addRadio("Horizontal lines")
            radio.check(0).Checked = True
        End If
        labels(3) = "More readable than dst1 - index, correlation, length (meters), and ArcY"
        desc = "Find and track all the horizontal or vertical lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim rsize = options.fOptions.matchCellSize
        gLines.lines.subsetRect = New cv.Rect(rsize * 3, rsize * 3, src.Width - rsize * 6, src.Height - rsize * 6)
        gLines.Run(src)

        Static vertRadio = findRadio("Vertical lines")
        Dim sortedLines = If(vertRadio.checked, gLines.sortedVerticals, gLines.sortedHorizontals)
        If sortedLines.Count = 0 Then
            setTrueText("There were no vertical lines found.", 3)
            Exit Sub
        End If

        Dim gc As gravityLine
        gCells.Clear()
        match.tCells.Clear()
        For i = 0 To sortedLines.Count - 1
            gc = sortedLines.ElementAt(i).Value

            If i = 0 Then
                dst1.SetTo(0)
                gc.tc1.template.CopyTo(dst1(gc.tc1.rect))
                gc.tc2.template.CopyTo(dst1(gc.tc2.rect))
            End If

            match.tCells.Clear()
            match.tCells.Add(gc.tc1)
            match.tCells.Add(gc.tc2)

            match.Run(src)
            Dim threshold = options.fOptions.correlationThreshold
            If match.tCells(0).correlation >= threshold And match.tCells(1).correlation >= threshold Then
                gc.tc1 = match.tCells(0)
                gc.tc2 = match.tCells(1)
                gc = gLines.updateGLine(src, gc, gc.tc1.center, gc.tc2.center)
                If gc.len3D > 0 Then gCells.Add(gc)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        For i = 0 To gCells.Count - 1
            Dim tc As tCell
            gc = gCells(i)
            Dim p1 As cv.Point2f, p2 As cv.Point2f
            For j = 0 To 2 - 1
                tc = Choose(j + 1, gc.tc1, gc.tc2)
                If j = 0 Then p1 = tc.center Else p2 = tc.center
            Next
            setTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 2)
            setTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 3)

            dst2.Line(p1, p2, myHighLightColor, task.lineWidth, task.lineType)
            dst3.Line(p1, p2, myHighLightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class










Public Class Feature_Lines_Tutorial1 : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of cv.Point2f)
        Dim raw3D As New List(Of cv.Point3f)
        For i = 0 To lines.ptList.Count - 1 Step 2
            Dim p1 = lines.ptList(i)
            Dim p2 = lines.ptList(i + 1)

            If task.pcSplit(2).Get(Of Single)(p1.Y, p1.X) > 0 And task.pcSplit(2).Get(Of Single)(p2.Y, p2.X) > 0 Then
                raw2D.Add(p1)
                raw2D.Add(p2)
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(p2.Y, p2.X))
            End If
        Next

        dst3 = src
        For i = 0 To raw2D.Count - 2 Step 2
            dst3.Line(raw2D(i), raw2D(i + 1), task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = "Starting with " + Format(lines.ptList.Count / 2, "000") + " lines, there are " +
                                       Format(raw3D.Count / 2, "000") + " with depth data."
    End Sub
End Class







Public Class Feature_Lines_Tutorial2 : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim gMat As New IMU_GMatrix
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5)
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = findSlider("Area kernel size for depth")
        Dim k = kernelSlider.Value - 1
        Dim kernel = kernelSlider.Value * 2 - 1

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of cv.Point2f)
        Dim raw3D As New List(Of cv.Point3f)
        For i = 0 To lines.ptList.Count - 1 Step 2
            Dim p1 = lines.ptList(i), p2 = lines.ptList(i + 1)
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, p1, p2)
                Dim rect = validateRect(New cv.Rect(pt.x - k, pt.y - k, kernel, kernel))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next
            If pt1.Z > 0 And pt2.Z > 0 Then
                raw2D.Add(p1)
                raw2D.Add(p2)
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(p2.Y, p2.X))
            End If
        Next

        dst3 = src
        For i = 0 To raw2D.Count - 2 Step 2
            dst3.Line(raw2D(i), raw2D(i + 1), task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = "Starting with " + Format(lines.ptList.Count / 2, "000") + " lines, there are " +
                                       Format(raw3D.Count / 2, "000") + " with depth data."
        If raw3D.Count = 0 Then
            setTrueText("No vertical or horizontal lines were found")
        Else
            gMat.Run(empty)
            task.gMatrix = gMat.gMatrix
            Dim matLines3D As cv.Mat = (New cv.Mat(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix
        End If
    End Sub
End Class








Public Class Feature_LongestVerticalKNN : Inherits VB_Algorithm
    Dim gLines As New Line_GCloud
    Dim longest As New Feature_Longest
    Public Sub New()
        labels(3) = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines."
        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN."
    End Sub
    Private Function testLastPair(lastPair As linePoints, gc As gravityLine) As Boolean
        Dim distance1 = lastPair.p1.DistanceTo(lastPair.p2)
        Dim p1 = gc.tc1.center
        Dim p2 = gc.tc2.center
        If distance1 < 0.75 * p1.DistanceTo(p2) Then Return True ' it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
        Return False
    End Function
    Public Sub RunVB(src As cv.Mat)
        gLines.Run(src)
        If gLines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were present", 3)
            Exit Sub
        End If

        dst3 = src.Clone
        Dim index As Integer

        If testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value) Then longest.knn.lastPair = New linePoints
        For Each gl In gLines.sortedVerticals
            If index >= 10 Then Exit For

            Dim gc = gl.Value
            Dim p1 = gc.tc1.center
            Dim p2 = gc.tc2.center
            If longest.knn.lastPair.compare(New linePoints) Then longest.knn.lastPair = New linePoints(p1, p2)
            Dim pt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
            setTrueText(CStr(index) + vbCrLf + Format(gc.arcY, fmt1), pt, 3)
            index += 1

            dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            longest.knn.trainInput.Add(p1)
            longest.knn.trainInput.Add(p2)
        Next

        longest.Run(src)
        dst2 = longest.dst2
    End Sub
End Class







Public Class Feature_Longest : Inherits VB_Algorithm
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match As New Match_Basics
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub

    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone

        knn.Run(src.Clone)
        Static p1 As cv.Point, p2 As cv.Point
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2

        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect).Clone
        match.Run(src)
        If match.correlation >= options.fOptions.correlationThreshold Then
            dst3 = match.dst0.Resize(dst3.Size)
            dst2.Line(p1, p2, myHighLightColor, task.lineWidth, task.lineType)
            dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
            rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            myHighLightColor = If(myHighLightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New linePoints(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class










Public Class Feature_tCellTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Dim tracker As New Feature_Points
    Dim match As New Match_tCell
    Public tcells As New List(Of tCell)
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X regions with goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim monitorCount = options.fOptions.featurePoints
        Dim minCorrelation = options.fOptions.correlationThreshold

        strOut = ""
        If tcells.Count < tracker.feat.featurePoints.Count / 3 Or tcells.Count < 2 Or task.optionsChanged Then
            myHighLightColor = If(myHighLightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            tracker.Run(src)
            tcells.Clear()
            For Each pt In tracker.feat.featurePoints
                tcells.Add(match.createCell(src, 0, pt))
            Next
            strOut += "------------------" + vbCrLf + vbCrLf
        End If

        dst2 = src.Clone

        Dim newCells As New List(Of tCell)
        For Each tc In tcells
            match.tCells(0) = tc
            match.Run(src)
            If match.tCells(0).correlation >= minCorrelation Then
                tc = match.tCells(0)
                setTrueText(Format(tc.correlation, fmt3), tc.center)
                If standalone Then strOut += Format(tc.correlation, fmt3) + ", "
                dst2.Circle(tc.center, task.dotSize, myHighLightColor, -1, task.lineType)
                dst2.Rectangle(tc.rect, myHighLightColor, task.lineWidth, task.lineType)
                newCells.Add(tc)
            End If
        Next

        If standalone Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        tcells = New List(Of tCell)(newCells)
        labels(2) = "Of the " + CStr(tracker.feat.featurePoints.Count) + " input cells " + CStr(newCells.Count) + " cells were tracked with correlation above " +
                    Format(minCorrelation, fmt1)
    End Sub
End Class









Public Class Feature_PointTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public feat As New Feature_Basics
    Dim mPoints As New Match_Points
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim minCorrelation = options.fOptions.correlationThreshold
        Dim rSize = options.fOptions.matchCellSize
        Dim radius = rSize / 2

        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            myHighLightColor = If(myHighLightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            mPoints.ptx.Clear()
            feat.Run(src)
            For Each pt In feat.featurePoints
                mPoints.ptx.Add(pt)
                Dim rect = validateRect(New cv.Rect(pt.X - radius, pt.Y - radius, rSize, rSize))
            Next
            strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
        End If
        mPoints.Run(src)

        dst2 = src.Clone
        For i = mPoints.ptx.Count - 1 To 0 Step -1
            If mPoints.correlation(i) > minCorrelation Then
                dst2.Circle(mPoints.ptx(i), task.dotSize, myHighLightColor, -1, task.lineType)
                strOut += Format(mPoints.correlation(i), fmt3) + ", "
            Else
                mPoints.ptx.RemoveAt(i)
            End If
        Next
        If standalone Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        labels(2) = "Of the " + CStr(feat.featurePoints.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(minCorrelation, fmt2)
    End Sub
End Class






Public Class Feature_LongestV_Tutorial1 : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Public Sub New()
        desc = "Use Feature_Lines to find all the vertical lines and show the longest."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class Feature_LongestV_Tutorial2 : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Dim knn As New KNN_Core4D
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Public Sub New()
        desc = "Use Feature_Lines to find all the vertical lines.  Use KNN_Core4D to track each line."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst1 = lines.dst3

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim match3D As New List(Of cv.Point3f)
        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim sIndex = lines.sortedVerticals.ElementAt(i).Value
            Dim x1 = lines.lines2D(sIndex)
            Dim x2 = lines.lines2D(sIndex + 1)
            Dim vec = If(x1.Y < x2.Y, New cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y), New cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y))
            If knn.queries.Count = 0 Then
                myHighLightColor = If(myHighLightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
                knn.queries.Add(vec)
            End If
            knn.trainInput.Add(vec)
            match3D.Add(lines.lines3D(sIndex))
            match3D.Add(lines.lines3D(sIndex + 1))
        Next

        Dim saveVec = knn.queries(0)
        knn.Run(empty)

        Dim index = knn.result(0, 0)
        Dim p1 = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
        Dim p2 = New cv.Point2f(knn.trainInput(index)(2), knn.trainInput(index)(3))
        pt1 = match3D(index * 2)
        pt2 = match3D(index * 2 + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

        Static lastLength = lines.sorted2DV.ElementAt(0).Key
        Dim bestLength = lines.sorted2DV.ElementAt(0).Key
        Static lengthReject As Integer
        knn.queries.Clear()
        If lastLength > 0.5 * bestLength Then
            knn.queries.Add(New cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y))
            lastLength = p1.DistanceTo(p2)
        Else
            lengthReject += 1
            lastLength = bestLength
        End If
        labels(3) = "Length rejects = " + Format(lengthReject / task.frameCount, "0%")
    End Sub
End Class






Public Class Feature_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public lines2D As New List(Of cv.Point2f)
    Public lines3D As New List(Of cv.Point3f)
    Public sorted2DV As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedVerticals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5)
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, 5)
        End If
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static angleSlider = findSlider("Angle tolerance in degrees")
        Static kernelSlider = findSlider("Area kernel size for depth")
        Dim tolerance = angleSlider.Value
        Dim k = kernelSlider.Value - 1
        Dim kernel = kernelSlider.Value * 2 - 1
        dst3 = src.Clone

        lines2D.Clear()
        lines3D.Clear()
        sorted2DV.Clear()
        sortedVerticals.Clear()
        sortedHorizontals.Clear()

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of cv.Point2f)
        Dim raw3D As New List(Of cv.Point3f)
        For i = 0 To lines.ptList.Count - 1 Step 2
            Dim p1 = lines.ptList(i), p2 = lines.ptList(i + 1)
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, p1, p2)
                Dim rect = validateRect(New cv.Rect(pt.x - k, pt.y - k, kernel, kernel))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next

            If pt1.Z > 0 And pt2.Z > 0 And pt1.Z < 4 And pt2.Z < 4 Then ' points more than X meters away are not accurate...
                raw2D.Add(p1)
                raw2D.Add(p2)
                raw3D.Add(pt1)
                raw3D.Add(pt2)
            End If
        Next

        If raw3D.Count = 0 Then
            setTrueText("No vertical or horizontal lines were found")
        Else
            Dim matLines3D As cv.Mat = (New cv.Mat(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix

            For i = 0 To raw2D.Count - 2 Step 2
                Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                Dim len3D = distance3D(pt1, pt2)
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                If Math.Abs(arcY - 90) < tolerance Then
                    dst3.Line(raw2D(i), raw2D(i + 1), cv.Scalar.Blue, task.lineWidth, task.lineType)
                    sortedVerticals.Add(len3D, lines3D.Count)
                    sorted2DV.Add(raw2D(i).DistanceTo(raw2D(i + 1)), lines2D.Count)
                    If pt1.Y > pt2.Y Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i))
                        lines2D.Add(raw2D(i + 1))
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i + 1))
                        lines2D.Add(raw2D(i))
                    End If
                End If
                If Math.Abs(arcY) < tolerance Then
                    dst3.Line(raw2D(i), raw2D(i + 1), cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    sortedHorizontals.Add(len3D, lines3D.Count)
                    If pt1.X < pt2.X Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i))
                        lines2D.Add(raw2D(i + 1))
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i + 1))
                        lines2D.Add(raw2D(i))
                    End If
                End If
            Next
        End If
        labels(2) = "Starting with " + Format(lines.ptList.Count / 2, "000") + " lines, there are " +
                                       Format(lines3D.Count / 2, "000") + " with depth data."
        labels(3) = "There were " + CStr(sortedVerticals.Count) + " vertical lines (blue) and " + CStr(sortedHorizontals.Count) + " horizontal lines (yellow)"
    End Sub
End Class






Public Class Feature_ArcY : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Public Sub New()
        desc = "Use Feature_Lines data to identify the longest lines and show its angle."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

        Dim pt1 = lines.lines3D(index)
        Dim pt2 = lines.lines3D(index + 1)
        Dim len3D = distance3D(pt1, pt2)
        Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
        setTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
    End Sub
End Class






Public Class Feature_ArcYAll : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Dim flow As New Font_FlowText
    Public Sub New()
        flow.dst = 3
        desc = "Use Feature_Lines data to collect vertical lines and measure accuracy of each."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        dst3.SetTo(0)
        Static arcLongAverage As New List(Of Single)
        Dim arcList As New List(Of Single)
        flow.msgs.Add("ID" + vbTab + "length" + vbTab + "distance")
        For i = 0 To Math.Min(10, lines.sortedVerticals.Count) - 1
            Dim index = lines.sortedVerticals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index)
            Dim p2 = lines.lines2D(index + 1)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            setTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
            dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

            Dim pt1 = lines.lines3D(index)
            Dim pt2 = lines.lines3D(index + 1)
            Dim len3D = distance3D(pt1, pt2)
            If len3D > 0 Then
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                arcList.Add(arcY)
                flow.msgs.Add(Format(arcY, fmt3) + vbTab + Format(len3D, fmt3) + "m " + vbTab + Format(pt1.Z, fmt1) + "m")
            End If
        Next
        If standalone Then flow.Run(empty)

        Static firstAverage As New List(Of Single)
        Static firstBest As Integer
        Dim mostAccurate = arcList(0)
        firstAverage.Add(mostAccurate)
        For Each arc In arcList
            If arc > mostAccurate Then
                mostAccurate = arc
                Exit For
            End If
        Next
        If mostAccurate = arcList(0) Then firstBest += 1

        Dim avg = arcList.Average()
        arcLongAverage.Add(avg)
        labels(3) = "arcY avg = " + Format(avg, fmt1) + ", long term average = " + Format(arcLongAverage.Average, fmt1) +
                    ", first was best " + Format(firstBest / task.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
        If arcLongAverage.Count > 1000 Then
            arcLongAverage.RemoveAt(0)
            firstAverage.RemoveAt(0)
        End If
    End Sub
End Class







' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_BasicsKNN : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Public featurePoints As New List(Of cv.Point2f)
    Public feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        knn.queries = New List(Of cv.Point2f)(feat.featurePoints)
        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(empty)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = feat.featurePoints(i)
            If pt.DistanceTo(qPt) > feat.options.distanceThreshold Then knn.trainInput(trainIndex) = feat.featurePoints(i)
        Next
        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class






Public Class Feature_BasicsValidated : Inherits VB_Algorithm
    Public centers As New List(Of cv.Point2f)
    Dim templates As New List(Of cv.Mat)
    Dim drawRects As New List(Of cv.Rect)
    Dim match As New Match_Basics
    Dim feat As New Feature_BasicsKNN
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Correlation threshold X100", 0, 100, 70)
            sliders.setupTrackBar("Minimum number of points (or resync with good features.)", 1, 20, 10)
        End If

        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Find good features and track them with matchTemplate."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Correlation threshold X100")
        Static minSlider = findSlider("Minimum number of points (or resync with good features.)")
        Dim minCorrelation = thresholdSlider.Value / 100
        Dim minPoints = minSlider.Value

        feat.Run(src)
        Dim rSize = feat.feat.options.fOptions.matchCellSize

        src.CopyTo(dst2)
        Dim nextTemplates As New List(Of cv.Mat)
        Dim nextRects As New List(Of cv.Rect)
        If templates.Count < minPoints Then
            For i = 0 To feat.featurePoints.Count - 1
                Dim pt = feat.featurePoints(i)
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                Dim r = validateRect(New cv.Rect(pt.X - rSize, pt.Y - rSize, rSize * 2, rSize * 2))
                nextTemplates.Add(src(r).Clone)
                nextRects.Add(r)
            Next
        Else
            nextTemplates = New List(Of cv.Mat)(templates)
            nextRects = New List(Of cv.Rect)(drawRects)
        End If

        templates.Clear()
        drawRects.Clear()
        dst3 = src.Clone
        dst1.SetTo(0)
        For i = 0 To nextTemplates.Count - 1
            match.template = nextTemplates(i)
            match.drawRect = nextRects(i)
            match.Run(src)
            If match.correlation > minCorrelation Then
                templates.Add(nextTemplates(i))
                drawRects.Add(nextRects(i))
                dst1.Circle(match.matchCenter, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                setTrueText(Format(match.correlation, fmt3), match.matchCenter, 1)
                dst3.Circle(match.matchCenter, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                setTrueText(Format(match.correlation, fmt3), match.matchCenter, 3)
            End If
        Next
        labels(2) = feat.labels(2)
    End Sub
End Class











' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_Grid : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Public corners As New List(Of cv.Point2f)
    Public options As New Options_Features
    Public Sub New()
        findSlider("Feature Sample Size").Value = 1
        desc = "Find good features to track in each roi of the task.gridList"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        corners.Clear()

        For Each roi In task.gridList
            Dim sampleSize = options.fOptions.featurePoints
            Dim features = cv.Cv2.GoodFeaturesToTrack(src(roi), sampleSize, options.quality, options.minDistance, Nothing, 7, True, 3)
            For Each pt In features
                corners.Add(New cv.Point2f(roi.X + pt.X, roi.Y + pt.Y))
            Next
        Next

        knn.queries = New List(Of cv.Point2f)(corners)
        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(empty)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = corners(i)
            If pt.DistanceTo(qPt) > options.distanceThreshold Then knn.trainInput(trainIndex) = corners(i)
        Next

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In corners
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next
        labels(2) = "Found " + CStr(corners.Count) + " points with quality = " + CStr(options.quality) +
                    " and minimum distance = " + CStr(options.minDistance)
    End Sub
End Class











Public Class Feature_GoodFeatureTrace : Inherits VB_Algorithm
    Dim feat As New Feature_BasicsKNN
    Public Sub New()
        findSlider("Distance threshold (pixels)").Value = 1
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Track the GoodFeatures"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        Static frameList As New List(Of cv.Mat)
        If task.optionsChanged Then frameList.Clear()

        dst0.SetTo(0)
        For Each pt In feat.featurePoints
            dst0.Set(Of Byte)(pt.Y, pt.X, 1)
            task.color.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        frameList.Add(dst0.Clone)
        If frameList.Count >= task.frameHistoryCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 = dst1.Add(dst0)
        dst2 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        dst3 = src
        dst3.SetTo(task.highlightColor, dst2)
    End Sub
End Class


















Public Class Feature_TraceKNN : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Dim feat As New Feature_Basics
    Public mpList As New List(Of linePoints)
    Public Sub New()
        findSlider("Feature Sample Size").Value = 200
        findSlider("Distance threshold (pixels)").Value = 20
        desc = "Track the GoodFeatures across a frame history and connect the first and last good.corners in the history."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        Static cornerHistory As New List(Of List(Of cv.Point2f))
        If task.optionsChanged Then cornerHistory.Clear()

        Dim histCount = task.frameHistoryCount
        cornerHistory.Add(New List(Of cv.Point2f)(feat.featurePoints))

        Dim lastIndex = cornerHistory.Count - 1
        knn.trainInput = New List(Of cv.Point2f)(cornerHistory.ElementAt(0))
        knn.queries = New List(Of cv.Point2f)(cornerHistory.ElementAt(lastIndex))
        knn.Run(empty)

        dst2.SetTo(0)
        mpList.Clear()
        Dim distanceThreshold = feat.options.distanceThreshold
        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = knn.queries(i)
            If pt.DistanceTo(qPt) > distanceThreshold Then Continue For
            dst2.Line(pt, qPt, cv.Scalar.White, task.lineWidth, task.lineType)
            mpList.Add(New linePoints(pt, qPt))
        Next
        strOut = CStr(mpList.Count) + " points were matched from the first to the " + CStr(lastIndex) + "th  (last) set of corners."
        setTrueText(strOut, 3)
        If cornerHistory.Count >= histCount Then cornerHistory.RemoveAt(0)
    End Sub
End Class







' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Feature_Sift : Inherits VB_Algorithm
    Dim siftCS As New CS_Classes.CS_SiftBasics
    Dim options As New Options_Sift
    Public Sub New()
        desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim doubleSize As New cv.Mat(dst2.Rows, dst2.Cols * 2, cv.MatType.CV_8UC3)
        siftCS.RunCS(task.leftView, task.rightView, doubleSize, options.useBFMatcher, options.pointCount)

        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

        labels(2) = If(options.useBFMatcher, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class







Public Class Feature_Sift_MT : Inherits VB_Algorithm
    Dim siftCS As New CS_Classes.CS_SiftBasics
    Dim siftBasics As New Feature_Sift
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Dim grid As New Grid_Rectangles
    Public Sub New()
        findSlider("Grid Cell Width").Maximum = dst2.Cols * 2
        findSlider("Grid Cell Width").Value = dst2.Cols * 2
        findSlider("Grid Cell Height").Value = 10

        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static bfRadio = findRadio("Use BF Matcher")
        grid.Run(src)

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim left = task.leftView(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = task.rightView(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.RunCS(left, right, dstTmp, bfRadio.Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst2 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst3 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

        labels(2) = If(bfRadio.Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class







'https://docs.opencv.org/4.x/da/df5/tutorial_py_sift_intro.html
Public Class Feature_SiftPoints : Inherits VB_Algorithm
    Dim sift As New CS_Classes.CS_SiftPoints
    Dim options As New Options_Sift
    Public stablePoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Keypoints found in SIFT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        sift.RunCS(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), options.pointCount)

        Dim newPoints As New List(Of cv.Point)
        For i = 0 To sift.keypoints.Count - 1
            Dim pt = sift.keypoints(i).Pt
            dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            newPoints.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
        Next

        dst3 = src.Clone
        Static history = New List(Of List(Of cv.Point))
        If task.optionsChanged Then history.clear()
        history.Add(newPoints)
        stablePoints.Clear()
        For Each pt In newPoints
            Dim missing = False
            For Each ptList In history
                If ptList.Contains(pt) = False Then
                    missing = True
                    Exit For
                End If
            Next
            If missing = False Then
                dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                stablePoints.Add(pt)
            End If
        Next
        If history.count >= task.frameHistoryCount Then history.removeat(0)
        labels(3) = "Sift keypoints that are present in the last " + CStr(task.frameHistoryCount) + "  frames."
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits VB_Algorithm
    Public corners As New List(Of cv.Point2f)
    Public feat As New Feature_Basics
    Public Sub New()
        findSlider("Feature Sample Size").Value = 200
        findSlider("Min Distance to next").Value = 1
        desc = "Find good features across multiple frames."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = src.Clone

        Static cornerHistory As New List(Of List(Of cv.Point2f))

        cornerHistory.Add(New List(Of cv.Point2f)(feat.featurePoints))
        If cornerHistory.Count > task.frameHistoryCount Then cornerHistory.RemoveAt(0)

        corners.Clear()
        For Each cList In cornerHistory
            For Each pt In cList
                corners.Add(New cv.Point(pt.X, pt.Y))
                dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
        Next

        labels(2) = "Found " + CStr(corners.Count) + " points with quality = " + CStr(feat.options.quality) +
                    " and minimum distance = " + CStr(feat.options.minDistance)
    End Sub
End Class






Public Class Feature_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Good features", "History of good features"}
        desc = "Get the features in a reduction grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = src

        feat.Run(reduction.dst2)
        If task.heartBeat Then dst3.SetTo(0)
        For Each pt In feat.featurePoints
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class






Public Class Feature_Agast : Inherits VB_Algorithm
    Dim ptCount(1) As Integer
    Public featurePoints As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Agast Threshold", 1, 200, 10)

        cPtr = Agast_Open()
        vbAddAdvice(traceName + ": Agast has no options right now...")
        desc = "Use the Agast Feature Detector in the OpenCV Contrib"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Agast Threshold")

        Dim input As cv.Mat, useResize As Boolean, resizeFactor = 4
        If src.Width >= 1280 Then
            input = src.Resize(New cv.Size(task.workingRes.Width / resizeFactor, task.workingRes.Height / resizeFactor))
            useResize = True
        Else
            input = src
        End If

        Dim dataSrc(input.Total * input.ElemSize - 1) As Byte
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim handleCount = GCHandle.Alloc(ptCount, GCHandleType.Pinned)
        Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols,
                                 handleCount.AddrOfPinnedObject(), thresholdSlider.value)
        handleSrc.Free()
        handleCount.Free()

        Dim ptMat = New cv.Mat(ptCount(0), 1, cv.MatType.CV_32FC2, imagePtr).Clone
        featurePoints.Clear()
        If standalone Then dst2 = input

        For i = 0 To ptMat.Rows - 1
            Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
            If useResize Then pt = New cv.Point(pt.X * resizeFactor, pt.Y * resizeFactor)
            featurePoints.Add(pt)
            If standalone Or showIntermediate() Then dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        If task.midHeartBeat Then
            labels(2) = CStr(featurePoints.Count) + " features found"
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class






Public Class Feature_StableGood : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim stable As New Feature_StableAgast
    Public stablePoints As New List(Of cv.Point2f)
    Public generations As New List(Of Integer)
    Public Sub New()
        labels(3) = "The raw Feature_Basics output"
        desc = "Age out the unstable feature points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src.Clone)
        dst3 = feat.dst2
        dst2 = src

        Dim candidates = stable.ageGenerations(feat.featurePoints)

        stablePoints.Clear()
        generations.Clear()
        For i = 0 To Math.Min(candidates.Count, stable.desiredCount) - 1
            If stable.generations(i) >= stable.threshold Then
                stablePoints.Add(candidates(i))
                generations.Add(stable.generations(i))
                dst2.Circle(candidates(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
            End If
        Next
        labels(2) = CStr(stablePoints.Count) + " stable points were present at least " +
                    CStr(CInt(stable.threshold)) + " generations"
    End Sub
End Class








Public Class Feature_Points : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        dst3.SetTo(0)

        For Each pt In feat.featurePoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(feat.featurePoints.Count) + " targets were present with " + CStr(feat.options.fOptions.featurePoints) + " requested."
    End Sub
End Class





Public Class Feature_StableAgast : Inherits VB_Algorithm
    Dim agast As New Feature_Agast
    Public threshold As Integer
    Public desiredCount As Integer
    Public stablePoints As New List(Of cv.Point2f)
    Public generations As New List(Of Integer)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Generation Threshold", 1, 20, 10)
            sliders.setupTrackBar("Desired Count", 1, 500, 200)
        End If
        desc = "Age out the unstable feature points."
    End Sub
    Public Function ageGenerations(inputPoints As List(Of cv.Point2f)) As List(Of cv.Point2f)
        Static genSlider = findSlider("Generation Threshold")
        Static countSlider = findSlider("Desired Count")
        threshold = genSlider.value
        desiredCount = countSlider.value

        If task.optionsChanged Then
            stablePoints.Clear()
            generations.Clear()
        End If

        Dim prevGen As New List(Of Integer)(generations)
        For Each pt In inputPoints
            If stablePoints.Contains(pt) Then
                Dim index = stablePoints.IndexOf(pt)
                generations(index) += 1
            Else
                stablePoints.Add(pt)
                generations.Add(1)
            End If
        Next

        Dim removeCount As Integer
        For i = prevGen.Count - 1 To 0 Step -1
            If prevGen(i) = generations(i) Then
                generations.RemoveAt(i)
                stablePoints.RemoveAt(i)
                removeCount += 1
            End If
        Next
        Return stablePoints
    End Function
    Public Sub RunVB(src As cv.Mat)
        agast.Run(src.Clone)
        dst3 = agast.dst2
        dst2 = src

        ageGenerations(agast.featurePoints)

        Dim displayCount As Integer
        For i = 0 To Math.Min(stablePoints.Count, desiredCount) - 1
            If generations(i) >= threshold Then
                Dim pt = stablePoints(i)
                dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
                displayCount += 1
            End If
        Next
        labels(2) = CStr(displayCount) + " stable points were present at least " + CStr(CInt(threshold)) + " generations"
    End Sub
End Class





Public Class Feature_StableSorted : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public desiredCount As Integer
    Public stablePoints As New List(Of cv.Point2f)
    Public generations As New List(Of Integer)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired Count", 1, 500, 200)
        desc = "Display the top X feature points ordered by generations they were present."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static countSlider = findSlider("Desired Count")
        desiredCount = countSlider.value

        feat.Run(src.Clone)
        dst3 = feat.dst2
        dst2 = src

        If task.optionsChanged Then
            stablePoints.Clear()
            generations.Clear()
        End If

        For i = 0 To feat.featurePoints.Count - 1
            Dim pt = New cv.Point2f(Math.Round(feat.featurePoints(i).X), Math.Round(feat.featurePoints(i).Y))
            If stablePoints.Contains(pt) Then
                generations(i) += 1
            Else
                stablePoints.Add(pt)
                generations.Add(1)
            End If
        Next

        Dim sortByGen As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To stablePoints.Count - 1
            sortByGen.Add(generations(i), stablePoints(i))
        Next

        Dim displayCount As Integer
        stablePoints.Clear()
        generations.Clear()
        For i = 0 To Math.Min(sortByGen.Count, desiredCount * 2) - 1
            Dim ele = sortByGen.ElementAt(i)
            Dim pt = ele.Value
            If displayCount < desiredCount Then
                dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
                displayCount += 1
            End If
            stablePoints.Add(ele.Value)
            generations.Add(ele.Key)
        Next
        labels(2) = "The most stable " + CStr(displayCount) + " points are highlighted below"
        labels(3) = "Output of Feature_Basics" + CStr(feat.featurePoints.Count) + " points found"
    End Sub
End Class
