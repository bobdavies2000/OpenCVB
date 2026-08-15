Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class SteadyCam_Basics : Inherits TaskParent
        Dim match As New Match_Basics
        Public shiftXY As cv.Point2f
        Public forceRecenter As Boolean
        Dim safeCenterRect As cv.Rect
        Public Sub New()
            desc = "Cursor.ai: Match the image center using Match_Basics to find X/Y shift; dst3 is gray shifted to align (black edges where missing)."
            labels = {"", "", "Match correlation", "Shift-aligned gray (black edges)"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayOriginal

            Static template As cv.Mat = Nothing
            Static center As cv.Point
            Static rect As cv.Rect = Nothing
            If task.heartBeatLT Or forceRecenter Then
                forceRecenter = False

                rect = Mat_Basics.buildCenterRect(dst2.Width \ 3, dst2.Height \ 3)
                template = src(rect).Clone
                center = New cv.Point(dst2.Width \ 2, dst2.Height \ 2)
                shiftXY = New cv.Point2f(0, 0)
                dst3 = src.Clone

                rect.X = task.gridWH * 5
                rect.Y = task.gridWH * 2
                Dim x = (dst2.Width - match.correlationMat.Width) / 2 + rect.X
                Dim y = (dst2.Height - match.correlationMat.Height) / 2 + rect.Y
                safeCenterRect = New cv.Rect(x, y, match.correlationMat.Width - rect.X * 2, match.correlationMat.Height - rect.Y * 2)

                Exit Sub
            End If

            match.template = template
            match.Run(src)
            dst2 = Match_Basics.showCorrelationMat(match.correlationMat, match.mm.minVal)

            Rectangle(dst2, safeCenterRect, white, task.lineWidth)

            Circle(dst2, match.newCenter, task.DotSize, black, -1, task.lineType)
            If safeCenterRect.Contains(match.newCenter) = False Then forceRecenter = True

            shiftXY = New cv.Point2f(center.X - match.newCenter.X, center.Y - match.newCenter.Y)
            Dim M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
            M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, shiftXY.X)
            M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, shiftXY.Y)

            ' Shift gray so content stays locked to the template frame; uncovered edges are black.
            WarpAffine(src, dst3, M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            labels(2) = "corr=" + match.correlation.ToString(fmt3) + "  shift=" + shiftXY.ToString
            labels(3) = "Aligned gray; black = missing after shift"
        End Sub
    End Class




    Public Class SteadyCam_WarpAffine : Inherits TaskParent
        Dim steady As New SteadyCam_Basics
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
            labels(3) = "angle=" + angle.ToString(fmt2) + " deg  shift=" + steady.shiftXY.ToString
        End Sub
    End Class




    Public Class SteadyCam_Lines : Inherits TaskParent
        Dim steady As New SteadyCam_Basics
        Dim refLine As lpData
        Public rotateAngle As Double
        Dim forceRecenter As Boolean
        Dim matcher As New Line_Match
        Public Sub New()
            desc = "Shift task.lines.dst3 with SteadyCam_Basics, then rotate by longest-line angle delta vs previous."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.lines.lpList.Count = 0 Then Exit Sub
            If src.Channels <> 1 Then src = task.grayOriginal

            steady.Run(src)
            Dim linesImg = task.lines.dst3

            ' Shift the current lines image left/right (and up/down) with SteadyCam_Basics.shiftXY.
            Dim Mshift As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
            Mshift.Set(Of Double)(0, 0, 1) : Mshift.Set(Of Double)(0, 1, 0) : Mshift.Set(Of Double)(0, 2, steady.shiftXY.X)
            Mshift.Set(Of Double)(1, 0, 0) : Mshift.Set(Of Double)(1, 1, 1) : Mshift.Set(Of Double)(1, 2, steady.shiftXY.Y)
            WarpAffine(linesImg, dst1, Mshift, linesImg.Size, InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(0))

            Dim lp = task.lines.lpList(0) ' longest line
            matcher.lp = lp
            matcher.Run(src)
            Line(task.color, lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)

            Dim indexLast = matcher.refreshCount.Count - 1
            Dim resetNeeded = steady.forceRecenter Or forceRecenter Or matcher.refreshCount(indexLast) > 0
            If task.heartBeatLT Or task.optionsChanged Or resetNeeded Then
                forceRecenter = False
                refLine = New lpData(lp.p1, lp.p2)
                rotateAngle = 0
                dst3 = dst1
                labels(3) = "Reference longest line reset  shift=" + steady.shiftXY.ToString
                Exit Sub
            End If

            ' Rotation needed so current longest line matches the previous reference angle.
            rotateAngle = refLine.angle - lp.angle
            If Math.Abs(rotateAngle) > 15 Then
                steady.forceRecenter = True
                forceRecenter = True
            End If

            Dim center = New Point2f(lp.ptCenter.X + steady.shiftXY.X, lp.ptCenter.Y + steady.shiftXY.Y)
            Dim Mrot = GetRotationMatrix2D(center, -rotateAngle, 1.0)
            WarpAffine(dst1, dst3, Mrot, dst1.Size, InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(0))

            dst2 = dst1.Clone

            Dim lpNew = New lpData(New cv.Point(CInt(lp.p1.X + steady.shiftXY.X), CInt(lp.p1.Y + steady.shiftXY.Y)),
                                   New cv.Point(CInt(lp.p2.X + steady.shiftXY.X), CInt(lp.p2.Y + steady.shiftXY.Y)))
            Line(dst2, lpNew.p1, lpNew.p2, task.highlight, task.lineWidth + 1, task.lineType)

            labels(2) = steady.labels(2) + "  Rectangle shows the limits before forcing a reset."
            labels(3) = "rotate=" + rotateAngle.ToString(fmt2) + " deg  shift=" + steady.shiftXY.ToString +
                        "  lp.age=" + CStr(lp.age)
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