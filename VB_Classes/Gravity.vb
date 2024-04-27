Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits VB_Algorithm
    Public points As New List(Of cv.Point)
    Dim resizeRatio As Integer = 1
    Public vec As New pointPair
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find all the points where depth X-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point, p2 As cv.Point)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each pt In points
            pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        dst2.Line(vec.p1, vec.p2, cv.Scalar.White, task.lineWidth, task.lineType)
        dst3.Line(vec.p1, vec.p2, cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = vbPrepareDepthInput(0) Else dst0 = src

        Dim resolution = task.quarterRes
        If dst0.Size <> resolution Then
            dst0 = dst0.Resize(resolution, cv.InterpolationFlags.Nearest)
            resizeRatio = CInt(dst2.Height / resolution.Height)
        End If

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.maxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Height / 3 To dst0.Height * 2 / 3 - 1
            Dim mm1 = vbMinMax(dst0.Row(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Row(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = vbMinMax(dst0.Row(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.X - mm2.minLoc.X) <= 1 Then points.Add(New cv.Point(mm1.minLoc.X, i))
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point
        Dim p2 As cv.Point
        If points.Count >= 2 Then
            p1 = New cv.Point(resizeRatio * points(points.Count - 1).X, resizeRatio * points(points.Count - 1).Y)
            p2 = New cv.Point(resizeRatio * points(0).X, resizeRatio * points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            points.Clear()
            vec = New pointPair
            strOut = "Gravity vector not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = New pointPair(p1, p2)
            vec = lp.edgeToEdgeLine(dst2.Size)
            If standaloneTest() Then displayResults(p1, p2)
        End If
        setTrueText(strOut, 3)
    End Sub
End Class





Public Class Gravity_Basics1 : Inherits VB_Algorithm
    Public vec As New pointPair
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Private Function findTransition(startRow As Integer, stopRow As Integer, stepRow As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For y = startRow To stopRow Step stepRow
            For x = 0 To dst0.Cols - 1
                lastVal = val
                val = dst0.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' change to sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x + Math.Abs(val) / Math.Abs(val - lastVal), y)
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= gOptions.FrameHistory.Value Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = vbPrepareDepthInput(0) Else dst0 = src

        Dim p1 = findTransition(0, dst0.Height - 1, 1)
        Dim p2 = findTransition(dst0.Height - 1, 0, -1)
        Dim lp = New pointPair(p1, p2)
        vec = lp.edgeToEdgeLine(dst2.Size)

        If p1.X >= 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(dst0.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(dst0.Get(Of Single)(p1.Y, p1.X - 1))
        End If
        setTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(vec.p1, vec.p2, 255, task.lineWidth, task.lineType)
        End If
    End Sub
End Class








Public Class Gravity_HorizonCompare : Inherits VB_Algorithm
    Dim gravity As New Gravity_Basics
    Dim horizon As New Horizon_Basics
    Public Sub New()
        desc = "Collect results from Horizon_Basics with Gravity_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gravity.Run(src)
        Dim g1 = task.gravityVec
        Dim h1 = task.horizonVec

        horizon.Run(src)
        Dim g2 = task.gravityVec
        Dim h2 = task.horizonVec

        If standaloneTest() Then
            setTrueText(strOut, 3)

            dst2.SetTo(0)
            dst2.Line(g1.p1, g1.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Line(g2.p1, g2.p2, task.highlightColor, task.lineWidth, task.lineType)

            dst2.Line(h1.p1, h1.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
            dst2.Line(h2.p1, h2.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        End If
    End Sub
End Class








Public Class Gravity_Horizon : Inherits VB_Algorithm
    Dim gravity As New Gravity_Basics
    Dim horizon As New Horizon_Basics
    Public Sub New()
        labels(2) = "Gravity vector Integer yellow and Horizon vector in red."
        desc = "Compute the gravity vector and the horizon vector separately"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gravity.Run(src)
        task.gravityVec = gravity.vec

        horizon.Run(src)
        task.horizonVec = horizon.vec
        task.horizonPresent = horizon.vecPresent

        If standaloneTest() Then
            setTrueText("Gravity vector (yellow):" + vbCrLf + gravity.strOut + vbCrLf + vbCrLf + "Horizon Vector (red): " + vbCrLf + horizon.strOut, 3)
            dst2.SetTo(0)
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        End If
    End Sub
End Class





Public Class Gravity_BasicsFail : Inherits VB_Algorithm
    Dim horizon As New Horizon_Basics
    Public vec As New pointPair
    Dim center As cv.Point
    Dim angle As Single = cv.Cv2.PI / 2
    Public Sub New()
        Dim center = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        If standalone Then labels = {"", "", "Gravity vector before rotating it back to vertical", "Gravity vector"}
        desc = "Reuse the Horizon_Basics to compute the Gravity vector - parallel but not precisely the gravity vector."
    End Sub
    Private Function ptRotate(pt As cv.Point) As cv.Point
        Dim x1 As Single = CSng((pt.X - center.X) * Math.Cos(-angle) - (pt.Y - center.Y) * Math.Sin(-angle) + center.X)
        Dim y1 As Single = CSng((pt.X - center.X) * Math.Sin(-angle) + (pt.Y - center.Y) * Math.Cos(-angle) + center.Y)
        Return New cv.Point(x1, y1)
    End Function
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = vbPrepareDepthInput(0) Else dst0 = src
        Dim M = cv.Cv2.GetRotationMatrix2D(center, angle * 57.2958, 1)
        Dim offset = (dst2.Width - dst2.Height) / 2
        Dim r = New cv.Rect(offset, 0, dst2.Height, dst2.Height)
        horizon.Run(dst0(r).WarpAffine(M, src.Size(), cv.InterpolationFlags.Nearest))

        horizon.Run(dst2)
        Dim p1 = ptRotate(horizon.vec.p1)
        Dim p2 = ptRotate(horizon.vec.p2)
        Dim lp = New pointPair(New cv.Point(p1.X + r.X, p1.Y), New cv.Point(p2.X + r.X, p2.Y))
        vec = lp.edgeToEdgeLine(dst2.Size)

        If standalone Then
            horizon.displayResults(horizon.vec.p1, horizon.vec.p2)
            dst3 = horizon.dst2

            dst2.SetTo(0)
            dst2.Line(vec.p1, vec.p2, 255, task.lineWidth, task.lineType)
        End If


        strOut = "p1 = " + vec.p1.ToString + vbCrLf + "p2 = " + vec.p2.ToString
        setTrueText(strOut, 3)
    End Sub
End Class