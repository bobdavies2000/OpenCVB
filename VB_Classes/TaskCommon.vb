Imports cvb = OpenCvSharp
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Public Module vbc
    Public task As VBtask
    Public allOptions As OptionsContainer
    Public recordedData As Replay_Play
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public newPoint As New cvb.Point
    Public callTrace As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Function Convert32f_To_8UC3(Input As cvb.Mat) As cvb.Mat
        Dim outMat = Input.Normalize(0, 255, cvb.NormTypes.MinMax)
        If Input.Channels() = 1 Then
            outMat.ConvertTo(outMat, cvb.MatType.CV_8U)
            Return outMat.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cvb.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function Check8uC3(ByVal input As cvb.Mat) As cvb.Mat
        Dim outMat As New cvb.Mat
        If input.Type = cvb.MatType.CV_8UC3 Then Return input
        If input.Type = cvb.MatType.CV_32F Then
            outMat = Convert32f_To_8UC3(input)
        ElseIf input.Type = cvb.MatType.CV_32SC1 Then
            input.ConvertTo(outMat, cvb.MatType.CV_32F)
            outMat = Convert32f_To_8UC3(outMat)
        ElseIf input.Type = cvb.MatType.CV_32SC3 Then
            input.ConvertTo(outMat, cvb.MatType.CV_32F)
            outMat = outMat.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            outMat = Convert32f_To_8UC3(outMat)
        ElseIf input.Type = cvb.MatType.CV_32FC3 Then
            Dim split = input.Split()
            split(0) = split(0).ConvertScaleAbs(255)
            split(1) = split(1).ConvertScaleAbs(255)
            split(2) = split(2).ConvertScaleAbs(255)
            cvb.Cv2.Merge(split, outMat)
        Else
            outMat = input.Clone
        End If
        If input.Channels() = 1 And input.Type = cvb.MatType.CV_8UC1 Then outMat = input.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Return outMat
    End Function
    Public Sub updateSettings()
        task.fpsRate = If(task.frameCount < 30, 30, task.fpsRate)
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

        Dim frameDuration = 1000 / task.fpsRate
        task.almostHeartBeat = If(task.msWatch - task.msLast + frameDuration * 1.5 > 1000, True, False)

        If (task.msWatch - task.msLast) > 1000 Then
            task.msLast = task.msWatch
            task.toggleOnOff = Not task.toggleOnOff
        End If

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
    End Sub
    Public Function FindFrm(title As String) As System.Windows.Forms.Form
        On Error Resume Next
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
End Module



Public Enum pointStyle
    unFiltered = 0
    filtered = 1
    flattened = 2
    flattenedAndFiltered = 3
End Enum




Public Enum oCase
    pointCloudAndRGB = 0
    verticalLines = 1
    drawFloor = 2
    tessalateTriangles = 3
    drawPyramid = 4
    drawCube = 5
    simplePlane = 6
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
End Enum






Public Structure mmData
    Dim minVal As Double
    Dim maxVal As Double
    Dim minLoc As cvb.Point
    Dim maxLoc As cvb.Point
End Structure





Public Structure tCell
    Dim template As cvb.Mat
    Dim searchRect As cvb.Rect
    Dim rect As cvb.Rect
    Dim center As cvb.Point2f
    Dim correlation As Single
    Dim depth As Single
    Dim strOut As String
End Structure





Public Structure gravityLine
    Dim pt1 As cvb.Point3f
    Dim pt2 As cvb.Point3f
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
    Dim pt As cvb.Point
    Dim size As Single
    Dim rotation As Single
    Dim brushNumber As Integer
End Structure





Public Class PointPair ' LineSegmentPoint in OpenCV does not use Point2f so this was built...
    Public p1 As cvb.Point2f
    Public p2 As cvb.Point2f
    Public slope As Single
    Public yIntercept As Single
    Public xIntercept As Single
    Public length As Single
    Sub New(_p1 As cvb.Point2f, _p2 As cvb.Point2f)
        p1 = _p1
        p2 = _p2
        If p1.X = p2.X Then
            slope = (p1.Y - p2.Y) / (p1.X + 0.001 - p2.X)
            yIntercept = p1.Y
        Else
            slope = (p1.Y - p2.Y) / (p1.X - p2.X)
            yIntercept = p1.Y - slope * p1.X
        End If
        length = p1.DistanceTo(p2)
    End Sub
    Sub New()
        p1 = New cvb.Point2f()
        p2 = New cvb.Point2f()
    End Sub
    Public Function edgeToEdgeLine(size As cvb.Size) As PointPair
        Dim lp As New PointPair(p1, p2)
        lp.p1 = New cvb.Point2f(0, yIntercept)
        lp.p2 = New cvb.Point2f(size.Width, size.Width * slope + yIntercept)
        xIntercept = -yIntercept / slope
        If lp.p1.Y > size.Height Then
            lp.p1.X = (size.Height - yIntercept) / slope
            lp.p1.Y = size.Height
        End If
        If lp.p1.Y < 0 Then
            lp.p1.X = xIntercept
            lp.p1.Y = 0
        End If

        If lp.p2.Y > size.Height Then
            lp.p2.X = (size.Height - yIntercept) / slope
            lp.p2.Y = size.Height
        End If
        If lp.p2.Y < 0 Then
            lp.p2.X = xIntercept
            lp.p2.Y = 0
        End If

        Return lp
    End Function
    Public Function compare(mp As PointPair) As Boolean
        If mp.p1.X = p1.X And mp.p1.Y = p1.Y And mp.p2.X = p2.X And p2.Y = p2.Y Then Return True
        Return False
    End Function
End Class






Public Structure coinPoints
    Dim p1 As cvb.Point
    Dim p2 As cvb.Point
    Dim p3 As cvb.Point
    Dim p4 As cvb.Point
End Structure






Public Structure matchRect
    Dim p1 As cvb.Point
    Dim p2 As cvb.Point
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
    Public color As cvb.Vec3b
End Class





Public Class fPolyData
    Public prevPoly As New List(Of cvb.Point2f)
    Public lengthPrevious As New List(Of Single)
    Public polyPrevSideIndex As Integer

    Public rotateCenter As cvb.Point2f
    Public rotateAngle As Single
    Public centerShift As cvb.Point2f
    Public currPoly As New List(Of cvb.Point2f)
    Public currLength As New List(Of Single)
    Dim jitterCheck As cvb.Mat
    Dim lastJitterPixels As Integer
    Public featureLineChanged As Boolean
    Sub New()
        prevPoly = New List(Of cvb.Point2f)
        currPoly = New List(Of cvb.Point2f)
        polyPrevSideIndex = 0
    End Sub
    Sub New(_currPoly As List(Of cvb.Point2f))
        prevPoly = New List(Of cvb.Point2f)(_currPoly)
        currPoly = New List(Of cvb.Point2f)(_currPoly)
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
        prevPoly = New List(Of cvb.Point2f)(currPoly)
        jitterCheck.SetTo(0)
    End Sub
    Public Function prevmp() As PointPair
        Return New PointPair(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Function currmp() As PointPair
        If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
        Return New PointPair(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Sub DrawPolys(dst As cvb.Mat, currPoly As List(Of cvb.Point2f), parent As Object)
        parent.DrawFPoly(dst, prevPoly, cvb.Scalar.White)
        parent.DrawFPoly(dst, currPoly, cvb.Scalar.Yellow)
        parent.DrawFatLine(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cvb.Scalar.Yellow)
        parent.DrawFatLine(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cvb.Scalar.White)
    End Sub
    Public Sub jitterTest(dst As cvb.Mat, parent As Object) ' return true if there is nothing to change
        If jitterCheck Is Nothing Then jitterCheck = New cvb.Mat(dst.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        Dim polymp = currmp()
        parent.DrawLine(jitterCheck, polymp.p1, polymp.p2, 255, task.lineWidth)
        Dim jitterPixels = jitterCheck.CountNonZero
        If jitterPixels = lastJitterPixels Then featureLineChanged = True Else featureLineChanged = False
        lastJitterPixels = jitterPixels
    End Sub
End Class





Public Structure vec5f
    Dim f1 As Single
    Dim f2 As Single
    Dim f3 As Single
    Dim f4 As Single
    Dim f5 As Single
    Public Sub New(_f1 As Single, _f2 As Single, _f3 As Single, _f4 As Single, _f5 As Single)
        f1 = _f1
        f2 = _f2
        f3 = _f3
        f4 = _f4
        f5 = _f5
    End Sub
End Structure





Public Structure vec8f
    Dim f1 As Single
    Dim f2 As Single
    Dim f3 As Single
    Dim f4 As Single
    Dim f5 As Single
    Dim f6 As Single
    Dim f7 As Single
    Dim f8 As Single
    Public Sub New(_f1 As Single, _f2 As Single, _f3 As Single, _f4 As Single, _f5 As Single, _f6 As Single, _f7 As Single, _f8 As Single)
        f1 = _f1
        f2 = _f2
        f3 = _f3
        f4 = _f4
        f5 = _f5
        f6 = _f6
        f7 = _f7
        f8 = _f8
    End Sub
End Structure






Public Class rcData
    Public rect As cvb.Rect
    Public mask As cvb.Mat
    Public pixels As Integer
    Public floodPoint As cvb.Point

    Public color As New cvb.Vec3b
    Public naturalColor As New cvb.Vec3b
    Public naturalGray As Integer
    Public exactMatch As Boolean
    Public pointMatch As Boolean

    Public depthPixels As Integer
    Public depthMask As cvb.Mat
    Public depthMean As cvb.Scalar
    Public depthStdev As cvb.Scalar

    Public colorMean As cvb.Scalar
    Public colorStdev As cvb.Scalar

    Public minVec As cvb.Point3f
    Public maxVec As cvb.Point3f
    Public minLoc As cvb.Point
    Public maxLoc As cvb.Point

    Public maxDist As cvb.Point
    Public maxDStable As cvb.Point ' keep maxDist the same if it is still on the cell.

    Public index As Integer
    Public indexLast As Integer

    Public nab As Integer
    Public container As Integer

    Public contour As New List(Of cvb.Point)
    Public motionFlag As Boolean
    Public motionPixels As Integer

    Public nearestFeature As cvb.Point2f
    Public features As New List(Of cvb.Point)
    Public featurePair As New List(Of PointPair)
    Public matchCandidatesSorted As New SortedList(Of Integer, Integer)
    Public matchCandidates As New List(Of Integer)

    ' transition these...
    Public nabs As New List(Of Integer)
    Public hull As New List(Of cvb.Point)
    Public eq As cvb.Vec4f ' plane equation
    Public contour3D As New List(Of cvb.Point3f)
    Public Sub New()
        index = 0
        mask = New cvb.Mat(1, 1, cvb.MatType.CV_8U)
        depthMask = mask
        rect = New cvb.Rect(0, 0, 1, 1)
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
End Enum