Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Public Module vbc
    Private _task As AlgorithmTask
    ''' <summary>Shared algorithm task state for the current session.</summary>
    Public Property task As AlgorithmTask
        Get
            Return _task
        End Get
        Set(value As AlgorithmTask)
            _task = value
        End Set
    End Property

    Public Const maxSlope As Integer = 100000
    Public Const PixelsPerRad As Single = 60.0F
    Public Const RadToDeg As Double = 57.295779513082323
    Public Const AngleThreshold As Single = 2.0F
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public ReadOnly white As New Scalar(255, 255, 255)
    Public ReadOnly black As New Scalar(0, 0, 0)
    Public ReadOnly grayColor As New Scalar(127, 127, 127)
    Public ReadOnly yellow As New Scalar(0, 255, 255)
    Public ReadOnly purple As New Scalar(255, 0, 255)
    Public ReadOnly teal As New Scalar(255, 255, 0)
    Public ReadOnly red As New Scalar(0, 0, 255), green As New Scalar(0, 255, 0)
    Public ReadOnly blue As New Scalar(255, 0, 0)

    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Function vecToScalar(c As Vec3b) As Scalar
        Return New Scalar(c(0), c(1), c(2))
    End Function
    Public Function ScalarToVec(c As Scalar) As Vec3b
        Return New Vec3b(c(0), c(1), c(2))
    End Function
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

        Dim xp1 = New Point2f(0, yIntercept)
        Dim xp2 = New Point2f(w, w * lp.slope + yIntercept)
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

        If xp1.Y = task.color.Height Then xp1.Y -= 1
        If xp2.Y = task.color.Height Then xp2.Y -= 1
        Return New lpData(xp1, xp2)
    End Function
    Public Function GetMinMax(mat As Mat, Optional mask As Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            MinMaxLoc(mat, mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            MinMaxLoc(mat, mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If

        If Double.IsInfinity(mm.maxVal) Then
            Console.WriteLine("IsInfinity encountered in getMinMax.")
            mm.maxVal = 0 ' skip ...
        End If
        mm.range = mm.maxVal - mm.minVal
        Return mm
    End Function
    Public Function getMinMaxDrawRect(mat As Mat) As mmData
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then mat = mat(task.drawRect)
        Return GetMinMax(mat)
    End Function
    ' alternative optional parameter: ApproxTC89L1 or ApproxNone
    Public Function ContourBuild(mask As Mat, Optional approxMode As contourApproximationModes = contourApproximationModes.ApproxNone) As List(Of cv.Point)
        Dim allContours As cv.Point()() = Nothing
        FindContours(mask, allContours, Nothing, RetrievalModes.External, approxMode)

        Dim tourCount As New List(Of Integer)
        For Each tour In allContours
            tourCount.Add(tour.Length)
        Next
        If tourCount.Count > 0 Then
            Return New List(Of cv.Point)(allContours(tourCount.IndexOf(tourCount.Max)).ToList)
        End If
        Return New List(Of cv.Point)
    End Function
    Public Function validatePoint(pt As cv.Point2f) As cv.Point2f
        If CInt(pt.X) < 0 Then pt.X = 0
        If CInt(pt.X) >= task.color.Width Then pt.X = task.color.Width - 1
        If CInt(pt.Y) < 0 Then pt.Y = 0
        If CInt(pt.Y) >= task.color.Height Then pt.Y = task.color.Height - 1

        Return pt
    End Function
    Public Function ValidateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X + r.Width >= task.workRes.Width * ratio Then r.Width = task.workRes.Width * ratio - r.X - 1
        If r.Y + r.Height >= task.workRes.Height * ratio Then r.Height = task.workRes.Height * ratio - r.Y - 1
        If r.X >= task.workRes.Width * ratio Then r.X = task.workRes.Width - 1
        If r.Y >= task.workRes.Height * ratio Then r.Y = task.workRes.Height - 1
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        Return r
    End Function
    Public Function validateRect(r As cv.Rect, width As Integer, height As Integer) As cv.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > width Then r.X = width - 1
        If r.Y > height Then r.Y = height - 1
        If r.X + r.Width >= width Then r.Width = width - r.X - 1
        If r.Y + r.Height >= height Then r.Height = height - r.Y - 1
        Return r
    End Function
End Module