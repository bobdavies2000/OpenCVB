Imports cv = OpenCvSharp
Imports VB_Classes.TaskParent
Public Module vbc
    Public task As VBtask
    Public taskReady As Boolean
    Public allOptions As OptionsContainer
    Public recordedData As Replay_Play
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public newPoint As New cv.Point
    Public msRNG As New System.Random
    Public white As New cv.Scalar(255, 255, 255), black As New cv.Scalar(0, 0, 0)
    Public grayColor As New cv.Scalar(127, 127, 127)
    Public yellow As New cv.Scalar(0, 255, 255), purple As New cv.Scalar(255, 0, 255)
    Public teal As New cv.Scalar(255, 255, 0)
    Public red As New cv.Scalar(0, 0, 255), green As New cv.Scalar(0, 255, 0)
    Public blue As New cv.Scalar(255, 0, 0)

    Public zero3f As New cv.Point3f
    Public newVec4f As New cv.Vec4f
    Public emptyMat As New cv.Mat
    Public saveVecColors(0) As cv.Vec3b
    Public saveScalarColors(0) As cv.Scalar
    Public saveFixedPalette As Boolean
    Public saveDepthColorMap As cv.Mat
    Public saveDepthColorList As New List(Of cv.Vec3b)
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Function vecToScalar(c As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(c(0), c(1), c(2))
    End Function
    Public Function DisplayCells() As cv.Mat
        Dim dst As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)

        For Each rc In task.rcList
            dst(rc.rect).SetTo(rc.color, rc.mask)
        Next

        Return dst
    End Function
    Public Function RebuildRCMap(sortedCells As SortedList(Of Integer, rcData)) As cv.Mat
        task.rcList.Clear()
        task.rcList.Add(New rcData) ' placeholder rcData so map is correct.
        task.rcMap.SetTo(0)
        Static saveColorSetting = task.redOptions.trackingIndex
        For Each rc In sortedCells.Values
            rc.index = task.rcList.Count

            If saveColorSetting <> task.redOptions.trackingIndex Then rc.color = black
            Select Case task.redOptions.trackingIndex
                Case trackColor.meanColor
                    Dim colorStdev As cv.Scalar
                    cv.Cv2.MeanStdDev(task.color(rc.rect), rc.color, colorStdev, rc.mask)
                Case trackColor.tracking
                    If rc.color = black Then rc.color = task.scalarColors(rc.index)
                Case trackColor.colorWithDepth
                    If rc.depth > task.MaxZmeters Then rc.depth = task.MaxZmeters
                    Dim index = CInt(255 * rc.depth / task.MaxZmeters)
                    rc.color = task.depthColorList(index)
            End Select

            task.rcList.Add(rc)
            task.rcMap(rc.rect).SetTo(rc.index, rc.mask)

            If rc.index >= 255 Then Exit For
        Next
        saveColorSetting = task.redOptions.trackingIndex
        Return DisplayCells()
    End Function
    Public Function RebuildRCMap(rcList As List(Of rcData)) As cv.Mat
        task.rcMap.SetTo(0)
        Dim dst As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        For Each rc In rcList
            task.rcMap(rc.rect).SetTo(rc.index, rc.mask)
            dst(rc.rect).SetTo(rc.color, rc.mask)
            If rc.index >= 255 Then Exit For
        Next
        Return dst
    End Function
    Public Function Convert32f_To_8UC3(Input As cv.Mat) As cv.Mat
        Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
        If Input.Channels() = 1 Then
            outMat.ConvertTo(outMat, cv.MatType.CV_8U)
            Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function Check8uC3(ByVal input As cv.Mat) As cv.Mat
        Dim outMat As New cv.Mat
        If input.Type = cv.MatType.CV_8UC3 Then Return input
        If input.Type = cv.MatType.CV_32F Then
            outMat = Convert32f_To_8UC3(input)
        ElseIf input.Type = cv.MatType.CV_32SC1 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = Convert32f_To_8UC3(outMat)
        ElseIf input.Type = cv.MatType.CV_32SC3 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = outMat.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            outMat = Convert32f_To_8UC3(outMat)
        ElseIf input.Type = cv.MatType.CV_32FC3 Then
            outMat = input.ConvertScaleAbs(255)
        Else
            outMat = input.Clone
        End If
        If input.Channels() = 1 And input.Type = cv.MatType.CV_8UC1 Then outMat = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Return outMat
    End Function
    Public Sub updateSettings()
        task.fpsAlgorithm = If(task.frameCount < 30, 30, task.fpsAlgorithm)
        If task.myStopWatch Is Nothing Then task.myStopWatch = Stopwatch.StartNew()

        ' update the time measures
        task.msWatch = task.myStopWatch.ElapsedMilliseconds

        task.quarterBeat = False
        task.midHeartBeat = False
        task.heartBeat = False
        Dim ms = (task.msWatch - task.msLast) / 1000
        For i = 0 To task.quarter.Count - 1
            If task.quarter(i) = False And ms > Choose(i + 1, 0.25, 0.5, 0.75, 1.0) Then
                task.quarterBeat = True
                If i = 1 Then task.midHeartBeat = True
                If i = 3 Then task.heartBeat = True
                task.quarter(i) = True
            End If
        Next
        If task.heartBeat Then ReDim task.quarter(4)

        If task.frameCount = 0 Then task.heartBeat = True

        Static heartBeatCount As Integer = 5
        If task.heartBeat Then
            heartBeatCount += 1
            If heartBeatCount >= 5 Then
                task.heartBeatLT = True
                heartBeatCount = 0
            End If
        End If

        Dim frameDuration = 1000 / task.fpsAlgorithm
        task.almostHeartBeat = If(task.msWatch - task.msLast + frameDuration * 1.5 > 1000, True, False)

        If (task.msWatch - task.msLast) > 1000 Then task.msLast = task.msWatch
        If task.heartBeatLT Then task.toggleOn = Not task.toggleOn

        If task.paused Then
            task.midHeartBeat = False
            task.almostHeartBeat = False
        End If

        task.histogramBins = task.gOptions.HistBinBar.Value
        task.lineWidth = task.gOptions.LineWidth.Value
        task.DotSize = task.gOptions.DotSizeSlider.Value

        task.MaxZmeters = task.gOptions.maxDepth
        task.metersPerPixel = task.MaxZmeters / task.dst2.Height ' meters per pixel in projections - side and top.
        task.debugSyncUI = task.gOptions.debugSyncUI.Checked
        task.depthDiffMeters = task.gOptions.DepthDiffSlider.Value / 100

        task.rcPixelThreshold = 0 ' task.gOptions.DebugSlider.Value / 1000
    End Sub

    Public Function findRectFromLine(lp As lpData) As cv.Rect
        Dim rect = New cv.Rect(lp.p1.X, lp.p1.Y, Math.Abs(lp.p1.X - lp.p2.X), Math.Abs(lp.p1.Y - lp.p2.Y))
        If lp.p1.Y > lp.p2.Y Then rect = New cv.Rect(lp.p1.X, lp.p2.Y, rect.Width, rect.Height)
        If rect.Width < 2 Then rect.Width = 2
        If rect.Height < 2 Then rect.Height = 2
        Return rect
    End Function


    Public Function findEdgePoints(lp As lpData) As lpData
        ' compute the edge to edge line - might be useful...
        Dim yIntercept = lp.p1.Y - lp.slope * lp.p1.X
        Dim w = task.cols, h = task.rows

        Dim xp1 = New cv.Point2f(0, yIntercept)
        Dim xp2 = New cv.Point2f(w, w * lp.slope + yIntercept)
        Dim xIntercept = -yIntercept / lp.slope
        If xp1.Y > h Then
            xp1.X = (h - yIntercept) / lp.slope
            xp1.Y = h
        End If
        If xp1.Y < 0 Then
            xp1.X = xIntercept
            xp1.Y = 0
        End If

        If xp2.Y > h Then
            xp2.X = (h - yIntercept) / lp.slope
            xp2.Y = h
        End If
        If xp2.Y < 0 Then
            xp2.X = xIntercept
            xp2.Y = 0
        End If
        Return New lpData(xp1, xp2)
    End Function
End Module



Public Enum pointStyle
    unFiltered = 0
    filtered = 1
    flattened = 2
    flattenedAndFiltered = 3
End Enum




Public Enum oCase
    drawPointCloudRGB = 0
    drawLineAndCloud = 1
    drawFloor = 2
    trianglesAndColor = 3
    drawPyramid = 4
    drawCube = 5
    quadBasics = 6
    minMaxBlocks = 7
    drawTiles = 8
    drawCell = 9
    drawCells = 10
    floorStudy = 11
    data3D = 12
    sierpinski = 13
    polygonCell = 14
    Histogram3D = 15
    pcPoints = 16
    pcLines = 17
    pcPointsAlone = 18
    drawLines = 19
    drawAvgPointCloudRGB = 20
End Enum






Public Structure mmData
    Dim minVal As Double
    Dim maxVal As Double
    Dim minLoc As cv.Point
    Dim maxLoc As cv.Point
    Dim range As Double
End Structure





Public Structure tCell
    Dim template As cv.Mat
    Dim searchRect As cv.Rect
    Dim rect As cv.Rect
    Dim center As cv.Point2f
    Dim correlation As Single
    Dim depth As Single
    Dim strOut As String
End Structure





Public Structure gravityLine
    Dim pt1 As cv.Point3f
    Dim pt2 As cv.Point3f
    Dim len3D As Single
    Dim imageAngle As Single
    Dim arcX As Single
    Dim arcY As Single
    Dim arcZ As Single
    Dim tc1 As tCell
    Dim tc2 As tCell
End Structure





Public Structure DNAentry
    Dim color As Byte
    Dim pt As cv.Point
    Dim size As Single
    Dim rotation As Single
    Dim brushNumber As Integer
End Structure






Public Structure coinPoints
    Dim p1 As cv.Point
    Dim p2 As cv.Point
    Dim p3 As cv.Point
    Dim p4 As cv.Point
End Structure






Public Structure matchRect
    Dim p1 As cv.Point
    Dim p2 As cv.Point
    Dim correlation1 As Single
    Dim correlation2 As Single
End Structure




Public Structure mlData
    Dim row As Single
    Dim col As Single
    Dim red As Single
    Dim green As Single
    Dim blue As Single
End Structure






Public Class roiData
    Public depth As Single
    Public color As cv.Vec3b
End Class





Public Class fPolyData
    Public prevPoly As New List(Of cv.Point2f)
    Public lengthPrevious As New List(Of Single)
    Public polyPrevSideIndex As Integer

    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public centerShift As cv.Point2f
    Public currPoly As New List(Of cv.Point2f)
    Public currLength As New List(Of Single)
    Dim jitterCheck As cv.Mat
    Dim lastJitterPixels As Integer
    Public featureLineChanged As Boolean
    Sub New()
        prevPoly = New List(Of cv.Point2f)
        currPoly = New List(Of cv.Point2f)
        polyPrevSideIndex = 0
    End Sub
    Sub New(_currPoly As List(Of cv.Point2f))
        prevPoly = New List(Of cv.Point2f)(_currPoly)
        currPoly = New List(Of cv.Point2f)(_currPoly)
        polyPrevSideIndex = 0
    End Sub
    Public Function computeCurrLengths() As Single
        currLength = New List(Of Single)
        Dim polymp = currmp()
        Dim d = polymp.p1.DistanceTo(polymp.p2)
        For i = 0 To currPoly.Count - 1
            d = currPoly(i).DistanceTo(currPoly((i + 1) Mod task.polyCount))
            currLength.Add(d)
        Next
        If lengthPrevious Is Nothing Then lengthPrevious = New List(Of Single)(currLength)
        Return d
    End Function
    Public Function computeFLineLength() As Single
        Return Math.Abs(currLength(polyPrevSideIndex) - lengthPrevious(polyPrevSideIndex))
    End Function
    Public Sub resync()
        lengthPrevious = New List(Of Single)(currLength)
        polyPrevSideIndex = lengthPrevious.IndexOf(lengthPrevious.Max)
        prevPoly = New List(Of cv.Point2f)(currPoly)
        jitterCheck.SetTo(0)
    End Sub
    Public Function prevmp() As lpData
        Return New lpData(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Function currmp() As lpData
        If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
        Return New lpData(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Sub DrawPolys(dst As cv.Mat, currPoly As List(Of cv.Point2f), parent As Object)
        parent.DrawFPoly(dst, prevPoly, cv.Scalar.White)
        parent.DrawFPoly(dst, currPoly, cv.Scalar.Yellow)
        parent.DrawFatLine(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.Yellow)
        parent.DrawFatLine(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.White)
    End Sub
    Public Sub jitterTest(dst As cv.Mat, parent As Object) ' return true if there is nothing to change
        If jitterCheck Is Nothing Then jitterCheck = New cv.Mat(dst.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        Dim polymp = currmp()
        parent.DrawLine(jitterCheck, polymp.p1, polymp.p2, 255, task.lineWidth)
        Dim jitterPixels = jitterCheck.CountNonZero
        If jitterPixels = lastJitterPixels Then featureLineChanged = True Else featureLineChanged = False
        lastJitterPixels = jitterPixels
    End Sub
End Class





Public Class gcData
    Public rect As cv.Rect ' rectange under the cursor in the color image.
    Public lRect As New cv.Rect ' when the left camera is not automatically aligned with the color image - some cameras don't do this.
    Public rRect As New cv.Rect ' The rect in the right image matching the left image rect.

    Public center As cv.Point ' center of the rectangle
    Public depth As Single
    Public depthStdev As Single
    Public age As Integer
    Public color As cv.Vec3f
    Public mm As mmData ' min and max values of the depth data.
    Public corners As New List(Of cv.Point3f)
    Public correlation As Single
    Public features As New List(Of cv.Point)
    Public index As Integer
End Class





Public Class triangleData
    Public color As cv.Point3f
    Public facets(3) As cv.Point3f
End Class




Public Class maskData
    Public rect As cv.Rect
    Public mask As cv.Mat
    Public contour As New List(Of cv.Point)
    Public index As Integer
    Public maxDist As cv.Point
    Public pixels As Integer
    Public depthMean As Single
    Public mm As mmData ' min/max/loc
    Public Sub New()
        mask = New cv.Mat(1, 1, cv.MatType.CV_8U)
        rect = New cv.Rect(0, 0, 1, 1)
    End Sub
End Class





Public Class rcData
    Public rect As cv.Rect
    Public mask As cv.Mat
    Public pixels As Integer
    Public age As Integer

    Public color As cv.Scalar

    Public mdList As New List(Of maskData)

    Public depthPixels As Integer
    Public depthMask As cv.Mat
    Public depth As Single

    Public mmX As mmData
    Public mmY As mmData
    Public mmZ As mmData

    Public maxDist As cv.Point
    Public maxDStable As cv.Point ' keep maxDist the same if it is still on the cell.

    Public index As Integer
    Public indexLast As Integer

    Public container As Integer

    Public contour As New List(Of cv.Point)

    Public ptFacets As New List(Of cv.Point)
    Public ptList As New List(Of cv.Point)

    ' transition these...
    Public nabs As New List(Of Integer)
    Public hull As New List(Of cv.Point)
    Public eq As cv.Vec4f ' plane equation
    Public contour3D As New List(Of cv.Point3f)
    Public Sub New()
        index = 0
        mask = New cv.Mat(1, 1, cv.MatType.CV_8U)
        depthMask = mask
        rect = New cv.Rect(0, 0, 1, 1)
    End Sub
End Class




Public Class rangeData
    Public index As Integer
    Public pixels As Integer
    Public start As Integer
    Public ending As Integer
    Public Sub New(_index As Integer, _start As Integer, _ending As Integer, _pixels As Integer)
        index = _index
        pixels = _pixels
        start = _start
        ending = _ending
    End Sub
End Class







Public Enum gifTypes
    gifdst0 = 0
    gifdst1 = 1
    gifdst2 = 2
    gifdst3 = 3
    openCVBwindow = 4
    openGLwindow = 5
    EntireScreen = 6
End Enum






Public Class lpData ' LineSegmentPoint in OpenCV does not use Point2f so this was built...
    Public center As cv.Point ' the point to use when identifying this line
    Public age As Integer
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public slope As Single
    Public depth As Single
    Public length As Single
    Public color As cv.Vec3f
    Public index As Integer
    Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
        p1 = _p1
        p2 = _p2
        If p1.X > p2.X Then
            p1 = _p2
            p2 = _p1
        End If
        p1 = New cv.Point2f(p1.X, p1.Y)
        p2 = New cv.Point2f(p2.X, p2.Y)

        If p1.X = p2.X Then
            slope = (p1.Y - p2.Y) / (p1.X + 0.001 - p2.X)
        Else
            slope = (p1.Y - p2.Y) / (p1.X - p2.X)
        End If

        center = New cv.Point(CInt((p1.X + p2.X) / 2), CInt((p1.Y + p2.Y) / 2))
        length = p1.DistanceTo(p2)
        age = 1
        Dim index = task.gcMap.Get(Of Integer)(center.Y, center.X)
        depth = task.gcList(index).depth
        color = task.gcList(index).color
    End Sub
    Sub New()
        p1 = New cv.Point2f()
        p2 = New cv.Point2f()
    End Sub
    Public Function perpendicularPoints(pt As cv.Point2f, distance As Integer) As (cv.Point, cv.Point)
        Dim perpSlope = -1 / slope
        Dim angleRadians As Double = Math.Atan(perpSlope)
        Dim xShift = distance * Math.Cos(angleRadians)
        Dim yShift = distance * Math.Sin(angleRadians)
        Dim p1 = New cv.Point(pt.X + xShift, pt.Y + yShift)
        Dim p2 = New cv.Point(pt.X - xShift, pt.Y - yShift)
        If p1.X < 0 Then p1.X = 0
        If p1.X >= task.color.Width Then p1.X = task.color.Width - 1
        If p1.Y < 0 Then p1.Y = 0
        If p1.Y >= task.color.Height Then p1.Y = task.color.Height - 1
        If p2.X < 0 Then p2.X = 0
        If p2.X >= task.color.Width Then p2.X = task.color.Width - 1
        If p2.Y < 0 Then p2.Y = 0
        If p2.Y >= task.color.Height Then p2.Y = task.color.Height - 1
        center = New cv.Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
        Return (p1, p2)
    End Function
    Public Function compare(mp As lpData) As Boolean
        If mp.p1.X = p1.X And mp.p1.Y = p1.Y And mp.p2.X = p2.X And p2.Y = p2.Y Then Return True
        Return False
    End Function
End Class





Public Class fcsData ' feature coordinate system (Line centers are the input)
    Public index As Integer
    Public age As Integer
    Public pt As cv.Point
    Sub New()

    End Sub
End Class






Public Class fpXData ' feature point -  excessive - trim this to fpData...
    Public index As Integer
    Public indexLast As Integer = -1
    Public age As Integer
    Public ID As Single
    Public travelDistance As Single
    Public periph As Boolean
    Public mask As cv.Mat
    Public rect As cv.Rect
    Public facet2f As List(Of cv.Point2f)
    Public facets As List(Of cv.Point)
    Public pt As cv.Point
    Public ptHistory As List(Of cv.Point)
    Public ptCenter As cv.Point
    Public center As cv.Point
    Public rcIndex As Integer
    Public nabeList As List(Of Integer)
    Public nabeRect As cv.Rect
    Public depthMean As Single
    Public depthMin As Single
    Public depthMax As Single
    Public gcIndex As Integer

    Public colorTracking As cv.Scalar
    Sub New()
        mask = New cv.Mat
        facet2f = New List(Of cv.Point2f)
        facets = New List(Of cv.Point)
        ptHistory = New List(Of cv.Point)
        nabeList = New List(Of Integer)
    End Sub
End Class