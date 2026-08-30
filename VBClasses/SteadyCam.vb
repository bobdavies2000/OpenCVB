Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class SteadyCam_Basics_TA : Inherits TaskParent
        Dim match As New Match_CenterRect
        Public shiftXY As cv.Point
        Public M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
        Public inverseM As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
        Public Sub New()
            match.displayRequest = True
            desc = "Use Match_CenterRect to determine the offset due to camera motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayOriginal
            match.Run(src)
            dst2 = match.dst2
            dst3 = match.dst3
            labels = match.labels
            shiftXY = match.shiftXY
            M = match.M
            InvertAffineTransform(M, inverseM)
        End Sub
    End Class







    Public Class SteadyCam_BasicsOriginal : Inherits TaskParent
        Dim match As New Match_Basics
        Public shiftXY As cv.Point2f
        Public forceRecenter As Boolean
        Dim centerRect As cv.Rect
        Dim kalman As New Kalman_Basics
        Public Sub New()
            desc = "Cursor.ai: Match the image center using Match_Basics to find X/Y shift; dst3 is gray shifted to align (black edges where missing)."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayOriginal

            If task.heartBeatLT Or forceRecenter Or task.optionsChanged Then
                forceRecenter = False

                centerRect = Rectangle_Basics.centerRect(dst2.Size, 3)
                match.template = src(centerRect).Clone
                shiftXY = New cv.Point2f(0, 0)
                If standaloneTest() Then dst3 = src.Clone
                Exit Sub
            End If

            match.Run(src)
            If standaloneTest() Then
                dst2 = Match_Basics.showCorrelationMat(match.correlationMat, match.mm.minVal, dst2.Size)
                Rectangle(dst2, centerRect, white, task.lineWidth)
                Circle(dst2, match.newCenter, task.DotSize, black, -1, task.lineType)
            End If

            If centerRect.Contains(match.newCenter) = False Then forceRecenter = True

            shiftXY = New cv.Point2f(dst2.Width \ 2 - match.newCenter.X, dst2.Height \ 2 - match.newCenter.Y)

            ' turn off kalman filtering with the debugCheckbox - or just comment out this conditional because kalman looks valuable.
            If task.gOptions.DebugCheckBox.Checked = False Then
                kalman.kInput = {shiftXY.X, shiftXY.Y}
                kalman.Run(emptyMat)
                shiftXY = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
            End If

            Dim M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
            M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, shiftXY.X)
            M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, shiftXY.Y)

            If standaloneTest() Then
                ' Shift gray so content stays locked to the template frame; 
                WarpAffine(src, dst3, M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

                labels(2) = "corr=" + match.correlation.ToString(fmt3) + "  shift=" + shiftXY.ToString
                labels(3) = "Aligned gray; missing data is black."
            End If
        End Sub
    End Class






    Public Class SteadyCam_Kalman : Inherits TaskParent
        Dim kalman As New Kalman_Basics
        Public Sub New()
            desc = "Use Kalman to smooth the behavior of ShiftXY in SteadyCam_Basics_TA"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim shiftXY = task.steadyCam.shiftXY
            kalman.kInput = {shiftXY.X, shiftXY.Y}
            kalman.Run(emptyMat)
            shiftXY = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
            Dim M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
            M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, shiftXY.X)
            M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, shiftXY.Y)

            ' Shift gray so content stays locked to the template frame; 
            WarpAffine(src, dst3, M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            dst2 = task.steadyCam.dst2
            labels = task.steadyCam.labels
        End Sub
    End Class






    Public Class SteadyCam_WarpAffine : Inherits TaskParent
        Dim steady As New SteadyCam_Basics_TA
        Public Sub New()
            desc = "Cursor.ai: Use SteadyCam_Basics shift output as WarpAffine input, then rotate with verticalizeAngle."
            labels = {"", "", "SteadyCam_Basics match", "Original gray with rotation + shift"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            steady.Run(src)
            dst2 = steady.dst2

            Dim angle = task.verticalizeAngle
            Dim center = New Point2f(steady.dst3.Width / 2.0F, steady.dst3.Height / 2.0F)
            Dim M = GetRotationMatrix2D(center, -angle, 1.0)

            ' Rotate the shift-aligned SteadyCam_Basics output so dst3 reflects both shift and rotation.
            WarpAffine(steady.dst3, dst3, M, steady.dst3.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            labels(2) = steady.labels(2)
            labels(3) = "angle=" + angle.ToString(fmt2) + " deg after shiftXY "
        End Sub
    End Class





    Public Class SteadyCam_LongestLine : Inherits TaskParent
        Dim matcher As New Line_Match
        Public shiftXY As cv.Point2f
        Public rotateAngle As Double
        Public forceRecenter As Boolean
        Dim snapshot As cv.Mat
        Dim refLine As lpData
        Public Sub New()
            desc = "Cursor.ai: Snapshot gray at heartBeatLT; match the current image to that snapshot using the longest line."
            labels = {"", "", "Longest line match vs heartBeatLT snapshot", "Current gray aligned to snapshot"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            src = task.grayOriginal
            If task.lines.lpList.Count = 0 Then
                dst2 = src.Clone
                dst3 = src.Clone
                labels(2) = "No lines found"
                Exit Sub
            End If

            Dim lp = task.lines.lpList(0)
            If task.heartBeatLT Or forceRecenter Or snapshot Is Nothing Or refLine Is Nothing Then
                forceRecenter = False
                snapshot = src.Clone
                refLine = New lpData(lp.p1, lp.p2)
                matcher.lp = refLine
                matcher.goodCorrelation = False
                matcher.Run(src)
                shiftXY = New cv.Point2f(0, 0)
                rotateAngle = 0
                dst2 = matcher.dst2
                Line(dst2, refLine.p1, refLine.p2, task.highlight, task.lineWidth + 1, task.lineType)
                dst3 = src.Clone
                labels(2) = "heartBeatLT snapshot  len=" + refLine.length.ToString(fmt1) + "  angle=" + refLine.angle.ToString(fmt2)
                labels(3) = "Snapshot gray (identity until next frame)"
                Exit Sub
            End If

            matcher.Run(src)
            If matcher.goodCorrelation = False Then
                forceRecenter = True
                dst2 = matcher.dst2
                dst3 = src.Clone
                labels(2) = matcher.labels(2)
                labels(3) = "Low correlation — recapture longest line on next frame"
                Exit Sub
            End If

            Dim matched = matcher.lp
            shiftXY = New cv.Point2f(refLine.ptCenter.X - matched.ptCenter.X, refLine.ptCenter.Y - matched.ptCenter.Y)
            rotateAngle = refLine.angle - matched.angle
            If Math.Abs(rotateAngle) > 15 Then forceRecenter = True

            Dim M = GetRotationMatrix2D(matched.ptCenter, -rotateAngle, 1.0)
            M.Set(Of Double)(0, 2, M.Get(Of Double)(0, 2) + shiftXY.X)
            M.Set(Of Double)(1, 2, M.Get(Of Double)(1, 2) + shiftXY.Y)
            WarpAffine(src, dst3, M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            dst2 = matcher.dst2
            Line(dst2, refLine.p1, refLine.p2, Scalar.Yellow, task.lineWidth, task.lineType)
            Line(dst2, matched.p1, matched.p2, task.highlight, task.lineWidth + 1, task.lineType)

            labels(2) = matcher.labels(2)
            labels(3) = "shift=" + shiftXY.ToString + "  rotate=" + rotateAngle.ToString(fmt2) + " deg"
        End Sub
    End Class




    Public Class SteadyCam_FindLines : Inherits TaskParent
        Dim steady As New SteadyCam_WarpAffine
        Dim lines As New Line_Basics
        Public Sub New()
            desc = "Find the lines in the SteadyCam_Lines output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            steady.Run(task.gray)
            dst2 = steady.dst3

            lines.Run(dst2)
            dst3 = lines.dst2
        End Sub
    End Class
End Namespace