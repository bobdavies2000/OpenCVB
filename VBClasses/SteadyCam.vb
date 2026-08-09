Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class SteadyCam_Basics : Inherits TaskParent
        Dim match As New Match_Basics
        Public shiftXY As cv.Point2f
        Public Sub New()
            desc = "Cursor.ai: Match the image center using Match_Basics to find X/Y shift; dst3 is gray shifted to align (black edges where missing)."
            labels = {"", "", "Match correlation", "Shift-aligned gray (black edges)"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            src = task.grayOriginal

            Static template As cv.Mat = Nothing
            Static center As cv.Point
            Static rect As cv.Rect = Nothing
            If task.heartBeat Then
                Dim padx = dst2.Width / 3
                Dim pady = dst2.Height / 3
                rect = ValidateRect(New cv.Rect(padx, pady, dst2.Width - padx * 2, dst2.Height - pady * 2))
                template = src(rect).Clone
                center = New cv.Point(dst2.Width \ 2, dst2.Height \ 2)
                shiftXY = New cv.Point2f(0, 0)
                dst3 = src.Clone
                Exit Sub
            End If

            match.template = template
            match.Run(src)
            dst2 = Match_Basics.showCorrelationMat(match.correlationMat, match.mm.minVal)
            Circle(dst2, match.newCenter, task.DotSize, black, -1, task.lineType)

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
            desc = "Use SteadyCam_Basics shift output as WarpAffine input, then rotate with verticalizeAngle; dst3 has rotation + shift."
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
End Namespace