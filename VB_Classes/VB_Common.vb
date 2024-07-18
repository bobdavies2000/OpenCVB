Imports cv = OpenCvSharp
Imports System.Drawing
Imports System.IO.Pipes
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Module VB_Common
    Public task As VBtask
    Public allOptions As OptionsContainer
    Public openGL_hwnd As IntPtr
    Public openGLPipe As NamedPipeServerStream
    Public recordedData As Replay_Play
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public newPoint As New cv.Point
    Public callTrace As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()
    Public Function FindFrm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Function GetNormalize32f(Input As cv.Mat) As cv.Mat
        Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
        If Input.Channels() = 1 Then
            outMat.ConvertTo(outMat, cv.MatType.CV_8U)
            Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function MakeSureImage8uC3(ByVal input As cv.Mat) As cv.Mat
        Dim outMat As New cv.Mat
        If input.Type = cv.MatType.CV_8UC3 Then Return input
        If input.Type = cv.MatType.CV_32F Then
            outMat = GetNormalize32f(input)
        ElseIf input.Type = cv.MatType.CV_32SC1 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = GetNormalize32f(outMat)
        ElseIf input.Type = cv.MatType.CV_32SC3 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = outMat.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            outMat = GetNormalize32f(outMat)
        ElseIf input.Type = cv.MatType.CV_32FC3 Then
            Dim split = input.Split()
            split(0) = split(0).ConvertScaleAbs(255)
            split(1) = split(1).ConvertScaleAbs(255)
            split(2) = split(2).ConvertScaleAbs(255)
            cv.Cv2.Merge(split, outMat)
        Else
            outMat = input.Clone
        End If
        If input.Channels() = 1 And input.Type = cv.MatType.CV_8UC1 Then outMat = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
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
        task.metersPerPixel = task.MaxZmeters / task.WorkingRes.Height ' meters per pixel in projections - side and top.
        task.debugSyncUI = task.gOptions.debugSyncUI.Checked
    End Sub
End Module







Public Structure mmData
    Dim minVal As Double
    Dim maxVal As Double
    Dim minLoc As cv.Point
    Dim maxLoc As cv.Point
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





Public Class PointPair
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public slope As Single
    Public yIntercept As Single
    Public xIntercept As Single
    Public length As Single
    Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
        p1 = _p1
        p2 = _p2
        If CInt(p1.X) = CInt(p2.X) Then If p1.X < p2.X Then p2.X += 1 Else p1.X += 1 ' shift it so we can be sane.
        slope = (p1.Y - p2.Y) / (p1.X - p2.X)
        yIntercept = p1.Y - slope * p1.X
        length = p1.DistanceTo(p2)
    End Sub
    Sub New()
        p1 = New cv.Point2f()
        p2 = New cv.Point2f()
    End Sub
    Public Function edgeToEdgeLine(size As cv.Size) As PointPair
        Dim lp As New PointPair(p1, p2)
        lp.p1 = New cv.Point2f(0, yIntercept)
        lp.p2 = New cv.Point2f(size.Width, size.Width * slope + yIntercept)
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
    Public Function prevmp() As PointPair
        Return New PointPair(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Function currmp() As PointPair
        If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
        Return New PointPair(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Sub DrawPolys(dst As cv.Mat, currPoly As List(Of cv.Point2f), parent As Object)
        parent.DrawFPoly(dst, prevPoly, cv.Scalar.White)
        parent.DrawFPoly(dst, currPoly, cv.Scalar.Yellow)
        parent.DrawFatLine(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.Yellow)
        parent.DrawFatLine(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.White)
    End Sub
    Public Sub jitterTest(dst As cv.Mat, parent As Object) ' return true if there is nothing to change
        If jitterCheck Is Nothing Then jitterCheck = New cv.Mat(dst.Size(), cv.MatType.CV_8U, 0)
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
    Public rect As cv.Rect
    Public mask As cv.Mat
    Public pixels As Integer
    Public floodPoint As cv.Point

    Public color As New cv.Vec3b
    Public naturalColor As New cv.Vec3b
    Public naturalGray As Integer
    Public exactMatch As Boolean
    Public pointMatch As Boolean

    Public depthPixels As Integer
    Public depthMask As cv.Mat
    Public depthMean As cv.Scalar
    Public depthStdev As cv.Scalar

    Public colorMean As cv.Scalar
    Public colorStdev As cv.Scalar

    Public minVec As cv.Point3f
    Public maxVec As cv.Point3f
    Public minLoc As cv.Point
    Public maxLoc As cv.Point

    Public maxDist As cv.Point
    Public maxDStable As cv.Point ' keep maxDist the same if it is still on the cell.

    Public index As Integer
    Public indexLast As Integer

    Public nab As Integer
    Public container As Integer

    Public contour As New List(Of cv.Point)
    Public motionFlag As Boolean
    Public motionPixels As Integer

    Public nearestFeature As cv.Point2f
    Public features As New List(Of cv.Point)
    Public featurePair As New List(Of PointPair)
    Public matchCandidatesSorted As New SortedList(Of Integer, Integer)
    Public matchCandidates As New List(Of Integer)

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