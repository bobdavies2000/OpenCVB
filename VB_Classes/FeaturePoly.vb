Imports cv = OpenCvSharp
Public Class FeaturePoly_Basics : Inherits VB_Parent
    Public resync As Boolean
    Public resyncCause As String
    Public resyncFrames As Integer
    Public maskChangePercent As Single

    Dim topFeatures As New FeaturePoly_TopFeatures
    Public sides As New FeaturePoly_Sides
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 30
        If dst2.Width >= 640 Then FindSlider("Resync if feature moves > X pixels").Value = 15
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Feature Polygon with perpendicular lines for center of rotation.", "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
        desc = "Build a Feature polygon with the top generation counts of the good features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.FirstPass Then sides.prevImage = src.Clone
        sides.options.RunVB()

        topFeatures.Run(src)
        dst2 = topFeatures.dst2
        sides.currPoly = New List(Of cv.Point2f)(topFeatures.poly)
        If sides.currPoly.Count < task.polyCount Then Exit Sub
        sides.Run(src)
        dst3 = sides.dst2

        For i = 0 To sides.currPoly.Count - 1
            SetTrueText(CStr(i), sides.currPoly(i), 3)
        Next
        SetTrueText("Rotate center", New cv.Point2f(sides.rotateCenter.X + 10, sides.rotateCenter.Y), 3)

        Dim causes As String = ""
        If Math.Abs(sides.rotateAngle * 57.2958) > 10 Then
            resync = True
            causes += " - Rotation angle exceeded threshold."
            sides.rotateAngle = 0
        End If
        causes += vbCrLf

        'If maskChangePercent > 0.2 Then
        '    resync = True
        '    causes += " - Difference of startFrame and current frame exceeded 20% of image size"
        'End If
        causes += vbCrLf

        If task.optionsChanged Then
            resync = True
            causes += " - Options changed"
        End If
        causes += vbCrLf

        If resyncFrames > sides.options.autoResyncAfterX Then
            resync = True
            causes += " - More than " + CStr(sides.options.autoResyncAfterX) + " frames without resync"
        End If
        causes += vbCrLf

        If Math.Abs(sides.currLengths.Sum() - sides.prevLengths.Sum()) > sides.options.removeThreshold * task.polyCount Then
            resync = True
            causes += " - The top " + CStr(task.polyCount) + " vertices have moved because of the generation counts"
        Else
            If Math.Abs(sides.prevFLineLen - sides.currFLineLen) > sides.options.removeThreshold Then
                resync = True
                causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(sides.options.removeThreshold) + " pixels"
            End If
        End If
        causes += vbCrLf

        If resync Or sides.prevPoly.Count <> task.polyCount Or task.optionsChanged Then
            sides.prevPoly = New List(Of cv.Point2f)(sides.currPoly)
            sides.prevLengths = New List(Of Single)(sides.currLengths)
            sides.prevSideIndex = sides.prevLengths.IndexOf(sides.prevLengths.Max)
            sides.prevImage = src.Clone
            resyncFrames = 0
            resyncCause = causes
        End If
        resyncFrames += 1

        strOut = "Rotation: " + Format(sides.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
        strOut += "Translation: " + CStr(CInt(sides.centerShift.X)) + ", " + CStr(CInt(sides.centerShift.Y)) + vbCrLf
        strOut += "Rotate center: " + Format(sides.rotateCenter.X, fmt0) + ", " + Format(sides.rotateCenter.Y, fmt0) + vbCrLf
        strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf + vbCrLf
        strOut += "Resync last caused by: " + vbCrLf + resyncCause

        For Each keyval In topFeatures.stable.goodCounts
            Dim pt = topFeatures.stable.basics.ptList(keyval.Value)
            Dim g = topFeatures.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            SetTrueText(CStr(g), pt)
        Next

        SetTrueText(strOut, 1)
        resync = False
    End Sub
End Class







Public Class FeaturePoly_Sides : Inherits VB_Parent
    Public currPoly As New List(Of cv.Point2f)
    Public currSideIndex As Integer
    Public currLengths As New List(Of Single)
    Public currFLineLen As Single
    Public mpCurr As PointPair

    Public prevPoly As New List(Of cv.Point2f)
    Public prevSideIndex As Integer
    Public prevLengths As New List(Of Single)
    Public prevFLineLen As Single
    Public mpPrev As PointPair

    Public prevImage As cv.Mat

    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public centerShift As cv.Point2f

    Public options As New Options_FPoly
    Dim near As New Line_Nearest
    Public rotatePoly As New Rotate_PolyQT
    Dim newPoly As List(Of cv.Point2f)
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "White is the original FPoly and yellow is the current FPoly."
        desc = "Compute the lengths of each side in a polygon"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.FirstPass Then prevImage = src.Clone
        options.RunVB()

        If standaloneTest() And task.heartBeat Then
            Random.Run(empty)
            currPoly = New List(Of cv.Point2f)(random.pointList)
        End If

        dst2.SetTo(0)
        currLengths.Clear()
        For i = 0 To currPoly.Count - 2
            currLengths.Add(currPoly(i).DistanceTo(currPoly(i + 1)))
        Next
        currSideIndex = currLengths.IndexOf(currLengths.Max)

        If task.FirstPass Then
            prevPoly = New List(Of cv.Point2f)(currPoly)
            prevLengths = New List(Of Single)(currLengths)
            prevSideIndex = prevLengths.IndexOf(prevLengths.Max)
        End If

        If prevPoly.Count = 0 Then Exit Sub

        mpPrev = New PointPair(prevPoly(prevSideIndex), prevPoly((prevSideIndex + 1) Mod task.polyCount))
        mpCurr = New PointPair(currPoly(currSideIndex), currPoly((currSideIndex + 1) Mod task.polyCount))

        prevFLineLen = mpPrev.p1.DistanceTo(mpPrev.p2)
        currFLineLen = mpCurr.p1.DistanceTo(mpCurr.p2)

        Dim d1 = mpPrev.p1.DistanceTo(mpCurr.p1)
        Dim d2 = mpPrev.p2.DistanceTo(mpCurr.p2)

        Dim newNear As PointPair
        If d1 < d2 Then
            centerShift = New cv.Point2f(mpPrev.p1.X - mpCurr.p1.X, mpPrev.p1.Y - mpCurr.p1.Y)
            rotateCenter = mpPrev.p1
            newNear = New PointPair(mpPrev.p2, mpCurr.p2)
        Else
            centerShift = New cv.Point2f(mpPrev.p2.X - mpCurr.p2.X, mpPrev.p2.Y - mpCurr.p2.Y)
            rotateCenter = mpPrev.p2
            newNear = New PointPair(mpPrev.p1, mpCurr.p1)
        End If

        Dim transPoly As New List(Of cv.Point2f)
        For i = 0 To currPoly.Count - 1
            transPoly.Add(New cv.Point2f(currPoly(i).X - centerShift.X, currPoly(i).Y - centerShift.Y))
        Next
        newNear.p1 = New cv.Point2f(newNear.p1.X - centerShift.X, newNear.p1.Y - centerShift.Y)
        newNear.p2 = New cv.Point2f(newNear.p2.X - centerShift.X, newNear.p2.Y - centerShift.Y)
        rotateCenter = New cv.Point2f(rotateCenter.X - centerShift.X, rotateCenter.Y - centerShift.Y)

        strOut = "No rotation" + vbCrLf
        rotateAngle = 0
        If d1 <> d2 Then
            If newNear.p1.DistanceTo(newNear.p2) > options.removeThreshold Then
                near.lp = mpPrev
                near.pt = newNear.p1
                near.Run(empty)
                dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, task.lineWidth + 5, task.lineType)

                Dim hypotenuse = rotateCenter.DistanceTo(near.pt)
                rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                If Single.IsNaN(rotateAngle) Then rotateAngle = 0
                strOut = "Angle is " + Format(rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            End If
        End If
        strOut += "Translation (shift) is " + Format(-centerShift.X, fmt0) + ", " + Format(-centerShift.Y, fmt0)

        If Math.Abs(rotateAngle) > 0 Then
            rotatePoly.rotateCenter = rotateCenter
            rotatePoly.rotateAngle = rotateAngle
            rotatePoly.poly.Clear()
            rotatePoly.poly.Add(newNear.p1)
            rotatePoly.Run(empty)

            If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then rotateAngle *= -1

            rotatePoly.rotateAngle = rotateAngle
            rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
            rotatePoly.Run(empty)
            newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
        End If

        vbDrawFPoly(dst2, prevPoly, cv.Scalar.White)
        vbDrawFPoly(dst2, currPoly, cv.Scalar.Yellow)
        drawFatLine(mpPrev.p1, mpPrev.p2, dst2, cv.Scalar.White)
        drawFatLine(mpCurr.p1, mpCurr.p2, dst2, cv.Scalar.Yellow)
    End Sub
End Class










Public Class FeaturePoly_BasicsOriginal : Inherits VB_Parent
    Public fPD As New fPolyData
    Public resyncImage As cv.Mat
    Public resync As Boolean
    Public resyncCause As String
    Public resyncFrames As Integer
    Public maskChangePercent As Single

    Dim topFeatures As New FeaturePoly_TopFeatures
    Public options As New Options_FPoly
    Public center As Object
    Public Sub New()
        center = New FeaturePoly_Center ' FeaturePoly_PerpendicularsTest can be used to test the perpendicular method of finding the rotate center.
        FindSlider("Feature Sample Size").Value = 30
        If dst2.Width >= 640 Then FindSlider("Resync if feature moves > X pixels").Value = 15
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Feature Polygon with perpendicular lines for center of rotation.", "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
        desc = "Build a Feature polygon with the top generation counts of the good features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.FirstPass Then resyncImage = src.Clone
        options.RunVB()

        topFeatures.Run(src)
        dst2 = topFeatures.dst2
        dst1 = topFeatures.dst3
        fPD.currPoly = New List(Of cv.Point2f)(topFeatures.poly)

        If task.optionsChanged Then fPD = New fPolyData(fPD.currPoly)
        If fPD.currPoly.Count < task.polyCount Then Exit Sub

        fPD.computeCurrLengths()
        For i = 0 To fPD.currPoly.Count - 1
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
        Next
        If task.FirstPass Then fPD.lengthPrevious = New List(Of Single)(fPD.currLength)

        center.fPD = fPD
        center.Run(src)
        fPD = center.fPD
        dst1 = (dst1 Or center.dst2).tomat
        dst0 = center.dst3

        fPD.jitterTest(dst2) ' the feature line has not really moved.

        Dim causes As String = ""
        If Math.Abs(fPD.rotateAngle * 57.2958) > 10 Then
            resync = True
            causes += " - Rotation angle exceeded threshold."
            fPD.rotateAngle = 0
        End If
        causes += vbCrLf

        If maskChangePercent > 0.2 Then
            resync = True
            causes += " - Difference of startFrame and current frame exceeded 20% of image size"
        End If
        causes += vbCrLf

        If task.optionsChanged Then
            resync = True
            causes += " - Options changed"
        End If
        causes += vbCrLf

        If resyncFrames > options.autoResyncAfterX Then
            resync = True
            causes += " - More than " + CStr(options.autoResyncAfterX) + " frames without resync"
        End If
        causes += vbCrLf

        If Math.Abs(fPD.currLength.Sum() - fPD.lengthPrevious.Sum()) > options.removeThreshold * task.polyCount Then
            resync = True
            causes += " - The top " + CStr(task.polyCount) + " vertices have moved because of the generation counts"
        Else
            If fPD.computeFLineLength() > options.removeThreshold Then
                resync = True
                causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(options.removeThreshold) + " pixels"
            End If
        End If
        causes += vbCrLf

        If resync Or fPD.prevPoly.Count <> task.polyCount Or task.optionsChanged Then
            fPD.resync()
            resyncImage = src.Clone
            resyncFrames = 0
            resyncCause = causes
        End If
        resyncFrames += 1

        vbDrawFPoly(dst2, fPD.currPoly, white)
        fPD.drawPolys(dst1, fPD.currPoly)
        For i = 0 To fPD.prevPoly.Count - 1
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
        Next

        strOut = "Rotation: " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
        strOut += "Translation: " + CStr(CInt(fPD.centerShift.X)) + ", " + CStr(CInt(fPD.centerShift.Y)) + vbCrLf
        strOut += "Rotate center: " + Format(fPD.rotateCenter.X, fmt0) + ", " + Format(fPD.rotateCenter.Y, fmt0) + vbCrLf
        strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf
        strOut += "Last resync cause(s): " + vbCrLf + resyncCause

        For Each keyval In topFeatures.stable.goodCounts
            Dim pt = topFeatures.stable.basics.ptList(keyval.Value)
            Dim g = topFeatures.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            SetTrueText(CStr(g), pt)
        Next

        SetTrueText(strOut, 1)
        dst3 = center.dst3
        labels(3) = center.labels(3)
        resync = False
    End Sub
End Class








Public Class FeaturePoly_Plot : Inherits VB_Parent
    Public fGrid As New FeaturePoly_Core
    Dim plot As New Plot_Histogram
    Public hist() As Single
    Public distDiff As New List(Of Single)
    Public Sub New()
        plot.minRange = 0
        plot.removeZeroEntry = False
        labels = {"", "", "", "anchor and companions - input to distance difference"}
        desc = "Feature Grid: compute distances between good features from frame to frame and plot the distribution"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim lastDistance = fGrid.dst0.Clone

        fGrid.Run(src)
        dst3 = fGrid.dst3

        dst3 = src.Clone
        ReDim hist(fGrid.threshold + 1)
        distDiff.Clear()
        For i = 0 To fGrid.stable.basics.facetGen.facet.facetList.Count - 1
            Dim pt = fGrid.stable.basics.ptList(i)
            Dim d = fGrid.anchor.DistanceTo(pt)
            Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
            Dim absDiff = Math.Abs(lastd - d)
            If absDiff >= hist.Length Then absDiff = hist.Length - 1
            If absDiff < fGrid.threshold Then
                hist(CInt(absDiff)) += 1
                DrawLine(dst3, fGrid.anchor, pt, task.HighlightColor)
                distDiff.Add(absDiff)
            Else
                hist(fGrid.threshold) += 1
            End If
        Next

        Dim hlist = hist.ToList
        Dim peak = hlist.Max
        Dim peakIndex = hlist.IndexOf(peak)

        Dim histMat = New cv.Mat(hist.Length, 1, cv.MatType.CV_32F, hist.ToArray)
        plot.maxValue = fGrid.stable.basics.ptList.Count
        plot.Run(histMat)
        dst2 = plot.dst2
        Dim avg = If(distDiff.Count > 0, distDiff.Average, 0)
        labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " + CStr(peakIndex) +
                        " with " + Format(peak, fmt1) + " occurances"
    End Sub
End Class








Public Class FeaturePoly_PlotWeighted : Inherits VB_Parent
    Public fPlot As New FeaturePoly_Plot
    Dim plot As New Plot_Histogram
    Dim addw As New AddWeighted_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        plot.minRange = 0
        plot.removeZeroEntry = False
        labels = {"", "Distance change from previous frame", "", "anchor and companions - input to distance difference"}
        desc = "Feature Grid: compute distances between good features from frame to frame and plot with weighting and Kalman to smooth results"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fPlot.Run(src)
        dst3 = fPlot.dst3

        Dim lastPlot As cv.Mat = plot.dst2.Clone
        If task.optionsChanged Then ReDim kalman.kInput(fPlot.hist.Length - 1)

        kalman.kInput = fPlot.hist
        kalman.Run(empty)
        fPlot.hist = kalman.kOutput

        Dim hlist = fPlot.hist.ToList
        Dim peak = hlist.Max
        Dim peakIndex = hlist.IndexOf(peak)
        Dim histMat = New cv.Mat(fPlot.hist.Length, 1, cv.MatType.CV_32F, fPlot.hist)
        plot.maxValue = fPlot.fGrid.stable.basics.ptList.Count
        plot.Run(histMat)
        addw.src2 = plot.dst2
        addw.Run(lastPlot)
        dst2 = addw.dst2
        If task.heartBeat Then
            Dim avg = If(fPlot.distDiff.Count > 0, fPlot.distDiff.Average, 0)
            labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " +
                        CStr(peakIndex) + " with " + Format(peak, fmt1) + " occurances"
        End If
    End Sub
End Class






Public Class FeaturePoly_Stablizer : Inherits VB_Parent
    Public fGrid As New FeaturePoly_Core
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Movement amount - dot is current anchor point", "SyncImage aligned to current image - slide camera left or right",
                  "current image with distance map"}
        desc = "Feature Grid: show the accumulated camera movement in X and Y (no rotation)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fGrid.Run(src.Clone)
        dst3 = fGrid.dst3
        labels(3) = fGrid.labels(2)

        Static  syncImage = src.Clone
        If fGrid.startAnchor = fGrid.anchor Then syncImage = src.Clone

        Dim shift As cv.Point2f = New cv.Point2f(fGrid.startAnchor.X - fGrid.anchor.X, fGrid.startAnchor.Y - fGrid.anchor.Y)
        Dim rect As New cv.Rect
        If shift.X < 0 Then rect.X = 0 Else rect.X = shift.X
        If shift.Y < 0 Then rect.Y = 0 Else rect.Y = shift.Y
        rect.Width = dst1.Width - Math.Abs(shift.X)
        rect.Height = dst1.Height - Math.Abs(shift.Y)

        dst1.SetTo(0)
        dst1(rect) = syncImage(rect)
        drawFatLine(fGrid.startAnchor, fGrid.anchor, dst1, cv.Scalar.White)
        drawPolkaDot(fGrid.anchor, dst1)

        Dim r = New cv.Rect(0, 0, rect.Width, rect.Height)
        If fGrid.anchor.X > fGrid.startAnchor.X Then r.X = fGrid.anchor.X - fGrid.startAnchor.X
        If fGrid.anchor.Y > fGrid.startAnchor.Y Then r.Y = fGrid.anchor.Y - fGrid.startAnchor.Y

        dst2.SetTo(0)
        dst2(r) = syncImage(rect)
    End Sub
End Class








Public Class FeaturePoly_StartPoints : Inherits VB_Parent
    Public startPoints As New List(Of cv.Point2f)
    Public goodPoints As New List(Of cv.Point2f)
    Dim fGrid As New FeaturePoly_Core
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, 255)
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Track the feature grid points back to the last sync point"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static  thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Dim threshold = thresholdSlider.Value
        Dim maxShift = fGrid.anchor.DistanceTo(fGrid.startAnchor) + threshold

        fGrid.Run(src)
        dst2 = fGrid.dst3
        Static  facets As New List(Of List(Of cv.Point))
        Dim lastPoints = dst0.Clone
        If fGrid.startAnchor = fGrid.anchor Or goodPoints.Count < 5 Then
            startPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
            facets = New List(Of List(Of cv.Point))(fGrid.goodFacets)
        End If

        dst0.SetTo(255)
        If standaloneTest() Then dst1.SetTo(0)
        Dim mpList As New List(Of PointPair)
        goodPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
        Dim facet As New List(Of cv.Point)
        Dim usedGood As New List(Of Integer)
        For i = 0 To goodPoints.Count - 1
            Dim pt = goodPoints(i)
            Dim startPoint = lastPoints.Get(Of Byte)(pt.Y, pt.X)
            If startPoint = 255 And i < 256 Then startPoint = i
            If startPoint < startPoints.Count And usedGood.Contains(startPoint) = False Then
                usedGood.Add(startPoint)
                facet = facets(startPoint)
                dst0.FillConvexPoly(facet, startPoint, cv.LineTypes.Link4)
                If standaloneTest() Then dst1.FillConvexPoly(facet, task.scalarColors(startPoint), task.lineType)
                mpList.Add(New PointPair(startPoints(startPoint), pt))
            End If
        Next

        ' dst3.SetTo(0)
        For Each mp In mpList
            If mp.p1.DistanceTo(mp.p2) <= maxShift Then DrawLine(dst1, mp.p1, mp.p2, cv.Scalar.Yellow)
            DrawCircle(dst1, mp.p1, task.DotSize, cv.Scalar.Yellow)
        Next
        dst1.Line(fGrid.anchor, fGrid.startAnchor, cv.Scalar.White, task.lineWidth + 1, task.lineType)
    End Sub
End Class








Public Class FeaturePoly_Triangle : Inherits VB_Parent
    Dim triangle As New Area_MinTriangle_CPP
    Dim fGrid As New FeaturePoly_Core
    Public Sub New()
        desc = "Find the minimum triangle that contains the feature grid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fGrid.Run(src)
        dst2 = fGrid.dst2

        triangle.srcPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
        triangle.Run(empty)
        dst3 = triangle.dst2
    End Sub
End Class







Public Class FeaturePoly_TopFeatures : Inherits VB_Parent
    Public stable As New Stable_BasicsCount
    Public poly As New List(Of cv.Point2f)
    Public options As New Options_FPoly
    Public Sub New()
        desc = "Get the top features and validate them"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        stable.Run(src)
        dst2 = stable.dst2
        poly.Clear()
        For Each keyVal In stable.goodCounts
            Dim pt = stable.basics.ptList(keyVal.Value)
            Dim g = stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            SetTrueText(CStr(g), pt)
            If poly.Count < task.polyCount Then poly.Add(pt)
        Next

        For i = 0 To poly.Count - 2
            DrawLine(dst2, poly(i), poly(i + 1), cv.Scalar.White)
        Next
    End Sub
End Class










Public Class FeaturePoly_WarpAffinePoly : Inherits VB_Parent
    Dim rotatePoly As New Rotate_PolyQT
    Dim warp As New WarpAffine_BasicsQT
    Dim fPoly As New FeaturePoly_BasicsOriginal
    Public Sub New()
        labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygon with rotation and shift - should be aligned"}
        desc = "Rotate and shift just the Feature polygon as indicated by FeaturePoly_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fPoly.Run(src)
        Dim polyPrev = fPoly.fPD.prevPoly
        Dim poly = New List(Of cv.Point2f)(fPoly.fPD.currPoly)

        dst2.SetTo(0)
        dst3.SetTo(0)

        vbDrawFPoly(dst2, polyPrev, white)

        warp.rotateCenter = fPoly.fPD.rotateCenter
        warp.rotateAngle = fPoly.fPD.rotateAngle
        warp.Run(dst2)
        dst3 = warp.dst2

        rotatePoly.rotateAngle = fPoly.fPD.rotateAngle
        rotatePoly.rotateCenter = fPoly.fPD.rotateCenter
        rotatePoly.poly = New List(Of cv.Point2f)(poly)
        rotatePoly.Run(empty)

        If fPoly.fPD.polyPrevSideIndex >= rotatePoly.poly.Count Then fPoly.fPD.polyPrevSideIndex = 0

        Dim offset = New cv.Point2f(rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).X - polyPrev(fPoly.fPD.polyPrevSideIndex).X,
                                    rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).Y - polyPrev(fPoly.fPD.polyPrevSideIndex).Y)

        Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
        If offset.X < 0 Then r1.X = 0
        If offset.Y < 0 Then r1.Y = 0

        Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
        If offset.X > 0 Then r2.X = 0
        If offset.Y > 0 Then r2.Y = 0

        dst3(r1) = dst2(r2)
        dst3 = dst3 - dst2

        vbDrawFPoly(dst3, rotatePoly.poly, cv.Scalar.Yellow)
        vbDrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)

        SetTrueText(fPoly.strOut, 3)
    End Sub
End Class










Public Class FeaturePoly_RotatePoints : Inherits VB_Parent
    Dim rotatePoly As New Rotate_PolyQT
    Public poly As New List(Of cv.Point2f)
    Public polyPrev As New List(Of cv.Point2f)
    Public rotateAngle As Single
    Public rotateCenter As cv.Point2f
    Public polyPrevSideIndex As Integer
    Public centerShift As cv.Point2f
    Public Sub New()
        labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygons with rotation and shift - should be aligned"}
        desc = "Rotate and shift just the Feature polygon as indicated by FeaturePoly_Basics"
    End Sub
    Public Function shiftPoly(polyPrev As List(Of cv.Point2f), poly As List(Of cv.Point2f)) As cv.Point2f
        rotatePoly.rotateAngle = rotateAngle
        rotatePoly.rotateCenter = rotateCenter
        rotatePoly.poly = New List(Of cv.Point2f)(poly)
        rotatePoly.Run(empty)

        Dim totalX = rotatePoly.poly(polyPrevSideIndex).X - polyPrev(polyPrevSideIndex).X
        Dim totalY = rotatePoly.poly(polyPrevSideIndex).Y - polyPrev(polyPrevSideIndex).Y

        Return New cv.Point2f(totalX, totalY)
    End Function
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            SetTrueText(traceName + " is meant only to run with FeaturePoly_Basics to validate the translation")
            Exit Sub
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)

        Dim rotateAndShift As New List(Of cv.Point2f)
        centerShift = shiftPoly(polyPrev, poly)
        vbDrawFPoly(dst2, polyPrev, white)
        vbDrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)
        For i = 0 To polyPrev.Count - 1
            Dim p1 = New cv.Point2f(rotatePoly.poly(i).X - centerShift.X, rotatePoly.poly(i).Y - centerShift.Y)
            Dim p2 = New cv.Point2f(rotatePoly.poly((i + 1) Mod task.polyCount).X - centerShift.X,
                                    rotatePoly.poly((i + 1) Mod task.polyCount).Y - centerShift.Y)
            rotateAndShift.Add(p1)
            SetTrueText(CStr(i), rotatePoly.poly(i), 2)
            SetTrueText(CStr(i), polyPrev(i), 2)
        Next
        vbDrawFPoly(dst3, polyPrev, white)
        vbDrawFPoly(dst3, rotateAndShift, cv.Scalar.Yellow)

        strOut = "After Rotation: " + Format(rotatePoly.rotateAngle, fmt0) + " degrees " +
                 "After Translation (shift) of: " + Format(centerShift.X, fmt0) + ", " + Format(centerShift.Y, fmt0) + vbCrLf +
                 "Center of Rotation: " + Format(rotateCenter.X, fmt0) + ", " + Format(rotateCenter.Y, fmt0) + vbCrLf +
                 "If the algorithm is working properly, the white and yellow Feature polygons below " + vbCrLf +
                 "should match in size and location."
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class FeaturePoly_WarpAffineImage : Inherits VB_Parent
    Dim warp As New WarpAffine_BasicsQT
    Dim fPoly As New FeaturePoly_BasicsOriginal
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Use OpenCV's WarpAffine to rotate and translate the starting image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fPoly.Run(src)

        warp.rotateCenter = fPoly.fPD.rotateCenter
        warp.rotateAngle = fPoly.fPD.rotateAngle
        warp.Run(fPoly.resyncImage.Clone)
        dst2 = warp.dst2
        dst1 = fPoly.dst1

        Dim offset = fPoly.fPD.centerShift

        Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
        If offset.X < 0 Then r1.X = 0
        If offset.Y < 0 Then r1.Y = 0

        Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
        If offset.X > 0 Then r2.X = 0
        If offset.Y > 0 Then r2.Y = 0

        dst3(r1) = dst2(r2)
        dst3 = src - dst2

        Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        Dim diffCount = changed.CountNonZero
        strOut = fPoly.strOut
        strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " +
                           Format(diffCount / dst3.Total, "0%")

        SetTrueText(strOut, 1)
    End Sub
End Class








' https://www.google.com/search?q=geometry+find+the+center+of+rotation&rlz=1C1CHBF_enUS838US838&oq=geometry+find+the+center+of+rotation&aqs=chrome..69i57j0i22i30j0i390l3.9576j0j4&sourceid=chrome&ie=UTF-8#kpvalbx=_rgg1Y9rbGM3n0PEP-ae4oAc_34
Public Class FeaturePoly_Perpendiculars : Inherits VB_Parent
    Public altCenterShift As cv.Point2f
    Public fPD As fPolyData
    Public rotatePoints As New FeaturePoly_RotatePoints
    Dim near As New Line_Nearest
    Public Sub New()
        labels = {"", "", "Output of FeaturePoly_Basics", "Center of rotation is where the extended lines intersect"}
        desc = "Find the center of rotation using the perpendicular lines from polymp and FLine (feature line) in FeaturePoly_Basics"
    End Sub
    Private Function findrotateAngle(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As Single
        near.lp = New PointPair(p1, p2)
        near.pt = pt
        near.Run(empty)
        DrawLine(dst2, pt, near.nearPoint, cv.Scalar.Red)
        Dim d1 = fPD.rotateCenter.DistanceTo(pt)
        Dim d2 = fPD.rotateCenter.DistanceTo(near.nearPoint)
        Dim angle = Math.Asin(near.nearPoint.DistanceTo(pt) / If(d1 > d2, d1, d2))
        If Single.IsNaN(angle) Then Return 0
        Return angle
    End Function
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().")
            Exit Sub
        End If

        Static  kalman As New Kalman_Basics
        Static  perp1 As New Line_Perpendicular
        Static  perp2 As New Line_Perpendicular

        dst2.SetTo(0)
        perp1.p1 = fPD.currPoly(fPD.polyPrevSideIndex)
        perp1.p2 = fPD.currPoly((fPD.polyPrevSideIndex + 1) Mod task.polyCount)
        perp1.Run(empty)

        DrawLine(dst2, perp1.r1, perp1.r2, cv.Scalar.Yellow)

        perp2.p1 = fPD.prevPoly(fPD.polyPrevSideIndex)
        perp2.p2 = fPD.prevPoly((fPD.polyPrevSideIndex + 1) Mod task.polyCount)
        perp2.Run(empty)
        DrawLine(dst2, perp2.r1, perp2.r2, white)

        fPD.rotateCenter = IntersectTest(perp2.r1, perp2.r2, perp1.r1, perp1.r2, New cv.Rect(0, 0, src.Width, src.Height))
        If fPD.rotateCenter = New cv.Point2f Then
            fPD.rotateAngle = 0
        Else
            DrawCircle(dst2, fPD.rotateCenter, task.DotSize + 2, cv.Scalar.Red)
            fPD.rotateAngle = findrotateAngle(perp2.r1, perp2.r2, perp1.r1)
        End If
        If fPD.rotateAngle = 0 Then fPD.rotateCenter = New cv.Point2f

        altCenterShift = New cv.Point2f(fPD.currPoly(fPD.polyPrevSideIndex).X - fPD.prevPoly(fPD.polyPrevSideIndex).X,
                                        fPD.currPoly(fPD.polyPrevSideIndex).Y - fPD.prevPoly(fPD.polyPrevSideIndex).Y)

        kalman.kInput = {fPD.rotateAngle}
        kalman.Run(empty)
        fPD.rotateAngle = kalman.kOutput(0)

        rotatePoints.poly = fPD.currPoly
        rotatePoints.polyPrev = fPD.prevPoly
        rotatePoints.polyPrevSideIndex = fPD.polyPrevSideIndex
        rotatePoints.rotateAngle = fPD.rotateAngle
        rotatePoints.Run(src)
        fPD.centerShift = rotatePoints.centerShift
        dst3 = rotatePoints.dst3
    End Sub
End Class








Public Class FeaturePoly_PerpendicularsTest : Inherits VB_Parent
    Dim center As New FeaturePoly_Perpendiculars
    Dim fPoly As New FeaturePoly_BasicsOriginal
    Public Sub New()
        fPoly.center = center
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Test the perpendicular method of finding the rotate center of the Feature Polygon"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fPoly.Run(src)
        dst1 = fPoly.dst1
        dst2 = fPoly.dst2
        dst3 = fPoly.dst3
    End Sub
End Class








Public Class FeaturePoly_PerpendicularsImage : Inherits VB_Parent
    Dim center As New FeaturePoly_Perpendiculars
    Dim fImage As New FeaturePoly_Image
    Public Sub New()
        fImage.fpoly.center = center
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Rotate the image using the perpendicular method of finding the rotate center"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fImage.Run(src)
        dst1 = fImage.dst1
        dst2 = fImage.dst2
        dst3 = fImage.dst3
    End Sub
End Class









Public Class FeaturePoly_Image : Inherits VB_Parent
    Public fpoly As New FeaturePoly_BasicsOriginal
    Dim rotate As New Rotate_BasicsQT
    Public resync As Boolean
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
        desc = "Rotate and shift the image as indicated by FeaturePoly_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        fpoly.Run(src)
        dst1 = fpoly.dst1

        If fpoly.resync = False Then
            If fpoly.fPD.featureLineChanged = False Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                rotate.rotateAngle = fpoly.fPD.rotateAngle
                rotate.rotateCenter = fpoly.fPD.rotateCenter
                rotate.Run(fpoly.resyncImage)
                dst0 = rotate.dst2

                Dim offset As cv.Point2f = fpoly.fPD.centerShift

                Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
                r1 = ValidateRect(r1)
                If offset.X < 0 Then r1.X = 0
                If offset.Y < 0 Then r1.Y = 0

                Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
                r2.Width = r1.Width
                r2.Height = r1.Height
                If r2.X < 0 Or r2.X >= dst2.Width Then Exit Sub ' wedged...
                If r2.Y < 0 Or r2.Y >= dst2.Height Then Exit Sub ' wedged...
                If offset.X > 0 Then r2.X = 0
                If offset.Y > 0 Then r2.Y = 0

                Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
                rotate.Run(mask2)
                mask2 = rotate.dst2

                Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
                mask(r1).SetTo(255)
                mask(r1) = mask2(r2)
                mask = Not mask

                dst2(r1) = dst0(r2)
                dst3 = input - dst2
                dst3.SetTo(0, mask)
            End If

            Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Dim diffCount = changed.CountNonZero
            resync = fpoly.resync
            fpoly.maskChangePercent = diffCount / dst3.Total
            strOut = fpoly.strOut
            strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")

        Else
            dst2 = fpoly.resyncImage.Clone
            dst3.SetTo(0)
        End If

        SetTrueText(strOut, 1)
    End Sub
End Class








Public Class FeaturePoly_ImageMask : Inherits VB_Parent
    Public fImage As New FeaturePoly_Image
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.pixelDiffThreshold = 10
        desc = "Build the image mask of the differences between the current frame and resync image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fImage.Run(src)
        dst2 = fImage.dst3
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        labels = fImage.labels
        dst1 = fImage.fpoly.dst1
        SetTrueText(fImage.strOut, 1)
    End Sub
End Class







Public Class FeaturePoly_PointCloud : Inherits VB_Parent
    Public fMask As New FeaturePoly_ImageMask
    Public fPolyCloud As cv.Mat
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Update changed point cloud pixels as indicated by the FeaturePoly_ImageMask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fMask.Run(src)
        If fMask.fImage.fpoly.resync Or task.FirstPass Then fPolyCloud = task.pointCloud.Clone
        dst1 = fMask.dst1
        dst2 = fMask.dst2
        dst3 = fMask.dst3
        task.pointCloud.CopyTo(fPolyCloud, dst3)

        SetTrueText(fMask.fImage.strOut, 1)
    End Sub
End Class







Public Class FeaturePoly_ResyncCheck : Inherits VB_Parent
    Dim fPoly As New FeaturePoly_BasicsOriginal
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        desc = "If there was no resync, check the longest side of the feature polygon (Feature Line) for unnecessary jitter."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fPoly.Run(src)
        dst2 = fPoly.dst1
        SetTrueText(fPoly.strOut, 2)

        Static  lastPixelCount As Integer
        If fPoly.resync Then
            dst3.SetTo(0)
            lastPixelCount = 0
        End If

        If fPoly.fPD.currPoly.Count < 2 Then Exit Sub ' polygon not found...

        Dim polymp = fPoly.fPD.currmp()
        DrawLine(dst3, polymp.p1, polymp.p2, 255)

        Dim pixelCount = dst3.CountNonZero
        SetTrueText(Format(Math.Abs(lastPixelCount - pixelCount)) + " pixels ", 3)
        lastPixelCount = pixelCount
    End Sub
End Class








Public Class FeaturePoly_Center : Inherits VB_Parent
    Public rotatePoly As New Rotate_PolyQT
    Dim near As New Line_Nearest
    Public fPD As fPolyData
    Dim newPoly As List(Of cv.Point2f)
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Layout of feature polygons after just translation - red line is used in sine computation",
                      "Layout of the starting (white) and current (yellow) feature polygons",
                      "Layout of feature polygons after rotation and translation"}
        desc = "Manually rotate and translate the current feature polygon to a previous feature polygon."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            SetTrueText(traceName + " is called by FeaturePoly_Basics to get the rotate center and angle." + vbCrLf +
                        "It does not produce any output when run standaloneTest().")
            Exit Sub
        End If

        Static  thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Dim threshold = thresholdSlider.Value

        Dim sindex1 = fPD.polyPrevSideIndex
        Dim sIndex2 = (sindex1 + 1) Mod task.polyCount

        Dim mp1 = fPD.currmp()
        Dim mp2 = fPD.prevmp()
        Dim d1 = mp1.p1.DistanceTo(mp2.p1)
        Dim d2 = mp1.p2.DistanceTo(mp2.p2)
        Dim newNear As PointPair
        If d1 < d2 Then
            fPD.centerShift = New cv.Point2f(mp1.p1.X - mp2.p1.X, mp1.p1.Y - mp2.p1.Y)
            fPD.rotateCenter = mp1.p1
            newNear = New PointPair(mp1.p2, mp2.p2)
        Else
            fPD.centerShift = New cv.Point2f(mp1.p2.X - mp2.p2.X, mp1.p2.Y - mp2.p2.Y)
            fPD.rotateCenter = mp1.p2
            newNear = New PointPair(mp1.p1, mp2.p1)
        End If

        Dim transPoly As New List(Of cv.Point2f)
        For i = 0 To fPD.currPoly.Count - 1
            transPoly.Add(New cv.Point2f(fPD.currPoly(i).X - fPD.centerShift.X, fPD.currPoly(i).Y - fPD.centerShift.Y))
        Next
        newNear.p1 = New cv.Point2f(newNear.p1.X - fPD.centerShift.X, newNear.p1.Y - fPD.centerShift.Y)
        newNear.p2 = New cv.Point2f(newNear.p2.X - fPD.centerShift.X, newNear.p2.Y - fPD.centerShift.Y)
        fPD.rotateCenter = New cv.Point2f(fPD.rotateCenter.X - fPD.centerShift.X, fPD.rotateCenter.Y - fPD.centerShift.Y)

        dst1.SetTo(0)
        fPD.drawPolys(dst1, transPoly)
        SetTrueText("Rotate center", fPD.rotateCenter, 1)

        strOut = "No rotation" + vbCrLf
        fPD.rotateAngle = 0
        If d1 <> d2 Then
            If newNear.p1.DistanceTo(newNear.p2) > threshold Then
                near.lp = New PointPair(fPD.prevPoly(sindex1), fPD.prevPoly(sIndex2))
                near.pt = newNear.p1
                near.Run(empty)
                dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, task.lineWidth + 5, task.lineType)

                Dim hypotenuse = fPD.rotateCenter.DistanceTo(near.pt)
                fPD.rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                If Single.IsNaN(fPD.rotateAngle) Then fPD.rotateAngle = 0
                strOut = "Angle is " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            End If
        End If
        strOut += "Translation (shift) is " + Format(-fPD.centerShift.X, fmt0) + ", " + Format(-fPD.centerShift.Y, fmt0)

        If Math.Abs(fPD.rotateAngle) > 0 Then
            rotatePoly.rotateCenter = fPD.rotateCenter
            rotatePoly.rotateAngle = fPD.rotateAngle
            rotatePoly.poly.Clear()
            rotatePoly.poly.Add(newNear.p1)
            rotatePoly.Run(empty)

            If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then fPD.rotateAngle *= -1

            rotatePoly.rotateAngle = fPD.rotateAngle
            rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
            rotatePoly.Run(empty)

            newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
        End If
        dst3.SetTo(0)
        fPD.drawPolys(dst3, fPD.currPoly)
        SetTrueText(strOut, 2)
    End Sub
End Class








Public Class FeaturePoly_EdgeRemoval : Inherits VB_Parent
    Dim fMask As New FeaturePoly_ImageMask
    Dim edges As New Edge_All
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Remove edges from the FeaturePoly_ImageMask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fMask.Run(src)
        dst2 = fMask.dst3

        edges.Run(src)
        dst1 = edges.dst2

        dst3 = dst2 And Not dst1
    End Sub
End Class














Public Class FeaturePoly_ImageNew : Inherits VB_Parent
    Public fpoly As New FeaturePoly_Basics
    Dim rotate As New Rotate_BasicsQT
    Public resync As Boolean
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
        desc = "Rotate and shift the image as indicated by FeaturePoly_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        fpoly.Run(src)
        dst1 = fpoly.dst3

        If fpoly.resync = False Then
            ' If fpoly.sides.featureLineChanged = False Then
            dst2.SetTo(0)
            dst3.SetTo(0)
            rotate.rotateAngle = fpoly.sides.rotateAngle
            rotate.rotateCenter = fpoly.sides.rotateCenter
            rotate.Run(fpoly.sides.prevImage)
            dst0 = rotate.dst2

            Dim offset As cv.Point2f = fpoly.sides.centerShift

            Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
            If offset.X < 0 Then r1.X = 0
            If offset.Y < 0 Then r1.Y = 0

            Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
            If offset.X > 0 Then r2.X = 0
            If offset.Y > 0 Then r2.Y = 0

            Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
            rotate.Run(mask2)
            mask2 = rotate.dst2

            Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
            mask(r1).SetTo(255)
            mask(r1) = mask2(r2)
            mask = Not mask

            dst2(r1) = dst0(r2)
            dst3 = input - dst2
            dst3.SetTo(0, mask)
            ' End If

            Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Dim diffCount = changed.CountNonZero
            resync = fpoly.resync
            fpoly.maskChangePercent = diffCount / dst3.Total
            strOut = fpoly.strOut
            strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")
        Else
            dst2 = fpoly.sides.prevImage.Clone
            dst3.SetTo(0)
        End If

        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class FeaturePoly_LeftRight : Inherits VB_Parent
    Dim leftPoly As New FeaturePoly_Basics
    Dim rightPoly As New FeaturePoly_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"Left image", "Right image", "FPoly output for left image", "FPoly output for right image"}
        desc = "Measure camera motion through the left and right images using FPoly"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = task.leftView
        dst1 = task.rightView
        leftPoly.Run(task.leftView)
        dst2 = leftPoly.dst3
        SetTrueText(leftPoly.strOut, 2)

        rightPoly.Run(task.rightView)
        dst3 = rightPoly.dst3
        SetTrueText(rightPoly.strOut, 3)
    End Sub
End Class








Public Class FeaturePoly_Core : Inherits VB_Parent
    Public stable As New Stable_GoodFeatures
    Public anchor As cv.Point2f
    Public startAnchor As cv.Point2f
    Public goodPoints As New List(Of cv.Point2f)
    Public goodFacets As New List(Of List(Of cv.Point))
    Public threshold As Integer
    Dim options As New Options_FPoly
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Maximum shift to trigger resync", 1, 100, 50)
            sliders.setupTrackBar("Anchor point max movement", 1, 10, 5)
        End If
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32F, 0)
        FindSlider("Feature Sample Size").Value = 20
        labels = {"", "Distance change from previous frame", "", "Feature Grid with anchor"}
        desc = "Feature Grid: compute distances between good features from frame to frame"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static  thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Static  shiftSlider = FindSlider("Maximum shift to trigger resync")
        Static  anchorSlider = FindSlider("Anchor point max movement")
        Dim maxShift = shiftSlider.Value
        threshold = thresholdSlider.Value

        stable.Run(src)
        dst3 = stable.basics.dst3

        Dim lastDistance = dst0.Clone
        anchor = stable.basics.anchorPoint
        Static  lastAnchor = anchor
        If lastAnchor.distanceto(anchor) > anchorSlider.Value Then lastDistance.SetTo(0)

        dst0.SetTo(0)
        goodPoints.Clear()
        goodFacets.Clear()
        dst2.SetTo(0)
        For i = 0 To stable.basics.facetGen.facet.facetList.Count - 1
            Dim facet = stable.basics.facetGen.facet.facetList(i)
            Dim pt = stable.basics.ptList(i)
            Dim d = anchor.DistanceTo(pt)
            dst0.FillConvexPoly(facet, d, task.lineType)
            Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
            Dim absDiff = Math.Abs(lastd - d)
            If absDiff < threshold Then
                goodPoints.Add(pt)
                goodFacets.Add(facet)
                SetTrueText(Format(absDiff, fmt1), pt, 2)
                DrawLine(dst3, anchor, pt, task.HighlightColor)
                dst2.Set(Of cv.Vec3b)(pt.Y, pt.X, white)
            End If
        Next

        Dim shift As cv.Point2f = New cv.Point2f(startAnchor.X - anchor.X, startAnchor.Y - anchor.Y)
        If goodPoints.Count = 0 Or Math.Abs(shift.X) > maxShift Or Math.Abs(shift.Y) > maxShift Then startAnchor = anchor
        labels(2) = "Distance change (after threshholding) since last reset = " + shift.ToString
        lastAnchor = anchor
    End Sub
End Class