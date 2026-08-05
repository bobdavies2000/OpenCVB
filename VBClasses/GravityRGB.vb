Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class GravityRGB_Basics : Inherits TaskParent
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "Original RGB", "RGB rotated with IMU gravity data"}
            desc = "Cursor.ai: Rotate the RGB image using the same IMU gravity data used by Cloud_Gravity."
        End Sub
        Public Shared Function WarpPoint(pt As Point2f, M As Mat) As Point2f
            Dim xOut = M.Get(Of Double)(0, 0) * pt.X + M.Get(Of Double)(0, 1) * pt.Y + M.Get(Of Double)(0, 2)
            Dim yOut = M.Get(Of Double)(1, 0) * pt.X + M.Get(Of Double)(1, 1) * pt.Y + M.Get(Of Double)(1, 2)
            Return New Point2f(CSng(xOut), CSng(yOut))
        End Function
        Public Shared Function rotateRGB(src As Mat, angle As Double) As cv.Mat
            If Math.Abs(angle) > 90 Then angle = angle Mod 90
            Dim center = New Point2f(src.Width / 2.0F, src.Height / 2.0F)
            Dim M = GetRotationMatrix2D(center, -angle, 1)
            Dim dst As New Mat(src.Size, src.Type)
            WarpAffine(src, dst, M, src.Size(), InterpolationFlags.Cubic)
            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = task.gray
            dst2 = src

            Dim rotateAngle = task.verticalizeAngle
            Static lastRotateAngle = task.verticalizeAngle
            If task.imuBasics.noCameraMotion Then rotateAngle = lastRotateAngle
            lastRotateAngle = rotateAngle

            dst3 = rotateRGB(src, rotateAngle)

            strOut = "verticalizeAngle = " + task.verticalizeAngle.ToString(fmt2) + " deg" + vbCrLf +
                     "noCameraMotion = " + CStr(task.imuBasics.noCameraMotion) + vbCrLf +
                     "accRadians X = " + task.accRadians.X.ToString(fmt3) + vbCrLf +
                     "accRadians Y = " + task.accRadians.Y.ToString(fmt3) + vbCrLf +
                     "accRadians Z = " + task.accRadians.Z.ToString(fmt3)
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class XR_GravityRGB_RotateRGB : Inherits TaskParent
        Public bestAngle As Double
        Public angleOffset As Double
        Dim center As cv.Point2f
        Public Sub New()
            center = New Point2f(dst2.Width / 2, dst2.Height / 2)
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "Inverse WarpAffine result", "Lines rotated at corrected gravity angle",
                  "AbsDiff of inverse vs original lines (jagged residual)"}
            desc = "Cursor.ai: Correct the gravity WarpAffine using jagged edges in task.lines.dst3: rotateRGB, inverse WarpAffine, compare to original."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lines = task.lines.dst3

            Dim reconstructed As New Mat
            Dim diff As New Mat
            Dim sortScores As New SortedList(Of Integer, Double)(New compareAllowIdenticalInteger)

            ' Search near the IMU gravity angle for the rotation that leaves the least jagged residual
            ' after rotateRGB + inverse WarpAffine compared to the original lines image.
            For i = -100 To 100
                Dim angle = task.verticalizeAngle + i * 0.01
                dst3 = GravityRGB_Basics.rotateRGB(lines, angle)

                ' Inverse of rotateRGB's WarpAffine (forward used GetRotationMatrix2D(center, -angle, 1))
                Dim Minv = GetRotationMatrix2D(center, angle, 1)
                WarpAffine(dst3, reconstructed, Minv, lines.Size(), InterpolationFlags.Nearest)

                Absdiff(reconstructed, lines, diff)
                Threshold(diff, diff, 0, 255, ThresholdTypes.Binary)
                sortScores.Add(CountNonZero(diff), angle)
            Next

            bestAngle = sortScores.Values(0)
            angleOffset = bestAngle - task.verticalizeAngle

            dst2 = GravityRGB_Basics.rotateRGB(lines, bestAngle)

            Dim MinvBest = GetRotationMatrix2D(center, bestAngle, 1)
            WarpAffine(dst2, dst1, MinvBest, lines.Size(), InterpolationFlags.Nearest)
            Absdiff(dst1, lines, dst3)
            Threshold(dst3, dst3, 0, 255, ThresholdTypes.Binary)

            strOut = "IMU verticalizeAngle = " + task.verticalizeAngle.ToString(fmt3) + " deg" + vbCrLf +
                 "Corrected bestAngle = " + bestAngle.ToString(fmt3) + " deg" + vbCrLf +
                 "angleOffset = " + angleOffset.ToString(fmt3) + " deg" + vbCrLf +
                 "Jagged residual pixels = " + CStr(CountNonZero(dst3))
            SetTrueText(strOut, 1)
            labels(2) = "Best gravity angle = " + bestAngle.ToString("0.000") + " (offset " + angleOffset.ToString("0.000") + ")"
        End Sub
    End Class





    Public Class XR_GravityRGB_IMUFixup : Inherits TaskParent
        Public bestAngle As Double
        Public angleOffset As Double
        Dim center As cv.Point2f
        Public Sub New()
            center = New Point2f(dst2.Width / 2, dst2.Height / 2)
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "Inverse WarpAffine result", "", "AbsDiff of inverse vs original lines (jagged residual)"}
            desc = "Cursor.ai: Correct the gravity WarpAffine using jagged edges in task.lines.dst3: rotateRGB, inverse WarpAffine, compare to original."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lines = task.lines.dst3

            Dim reconstructed As New Mat
            Dim diff As New Mat
            Dim sortScores As New SortedList(Of Integer, Double)(New compareAllowIdenticalInteger)

            ' Search near the IMU gravity angle for the rotation that leaves the least jagged residual
            ' after rotateRGB + inverse WarpAffine compared to the original lines image.
            For i = -100 To 100
                Dim angle = task.verticalizeAngle + i * 0.01
                dst3 = GravityRGB_Basics.rotateRGB(lines, angle)

                ' Inverse of rotateRGB's WarpAffine (forward used GetRotationMatrix2D(center, -angle, 1))
                Dim Minv = GetRotationMatrix2D(center, angle, 1)
                WarpAffine(dst3, reconstructed, Minv, lines.Size(), InterpolationFlags.Nearest)

                Absdiff(reconstructed, lines, diff)
                Threshold(diff, diff, 0, 255, ThresholdTypes.Binary)
                sortScores.Add(CountNonZero(diff), angle)
            Next

            bestAngle = sortScores.Values(0)
            angleOffset = bestAngle - task.verticalizeAngle

            dst2 = GravityRGB_Basics.rotateRGB(lines, bestAngle)

            Dim MinvBest = GetRotationMatrix2D(center, bestAngle, 1)
            WarpAffine(dst2, dst1, MinvBest, lines.Size(), InterpolationFlags.Nearest)
            Absdiff(dst1, lines, dst3)
            Threshold(dst3, dst3, 0, 255, ThresholdTypes.Binary)

            strOut = "IMU verticalizeAngle = " + task.verticalizeAngle.ToString(fmt3) + " deg" + vbCrLf +
                         "Corrected bestAngle = " + bestAngle.ToString(fmt3) + " deg" + vbCrLf +
                         "angleOffset = " + angleOffset.ToString(fmt3) + " deg" + vbCrLf +
                         "Jagged residual pixels = " + CStr(CountNonZero(dst3))
            SetTrueText(strOut, 1)
            labels(2) = "Best gravity angle = " + bestAngle.ToString("0.000") + " (offset " + angleOffset.ToString("0.000") + ")"
        End Sub
    End Class





    Public Class XR_GravityRGB_Line : Inherits TaskParent
        Dim lines As New Line_Core
        Dim para As New Line_Parallel
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find the lines in the gravity-rotated grayscale image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = GravityRGB_Basics.rotateRGB(task.gray, task.verticalizeAngle)

            lines.Run(dst2)

            Static lineHistory As New List(Of cv.Mat)({lines.dst2})
            dst1 = lineHistory(0)
            lineHistory.Add(lines.dst2.Clone)
            For i = 1 To lineHistory.Count - 1
                dst1 = dst1 Or lineHistory(i)
            Next
            If lineHistory.Count > task.fOptions.FrameHistoryCount.Value Then lineHistory.RemoveAt(0)

            para.lpList = lines.lpList
            para.Run(task.gray)

            Dim indexList As List(Of Integer) = Nothing
            For Each intersections In para.interList.Values
                For Each index In intersections
                    Dim lp = lines.lpList(index)
                    If Math.Abs(lp.p1.X - lp.p2.X) <= 2 Then
                        indexList = intersections
                        Exit For
                    End If
                Next
                If indexList IsNot Nothing Then Exit For
            Next

            If indexList Is Nothing Then Exit Sub ' no parallel vertical lines found
            If task.heartBeat Then dst3.SetTo(0)
            For Each index In indexList
                Dim lp = lines.lpList(index)
                Line(dst3, lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.AntiAlias)
            Next

        End Sub
    End Class





    Public Class XR_GravityRGB_Motion : Inherits TaskParent
        Public Sub New()
            labels(3) = "Gravity rotation on every frame - contrast with the dst2 image."
            desc = "Rotate just the motion grid cells using the gravity warpaffine."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static motionMaskUsed As New List(Of Integer)

            dst3 = GravityRGB_Basics.rotateRGB(task.gray, task.verticalizeAngle)
            dst1 = GravityRGB_Basics.rotateRGB(task.motion.motionMask, task.verticalizeAngle)
            If task.heartBeatLT Or task.imuBasics.noCameraMotion = False Then
                dst2 = dst3.Clone
                motionMaskUsed.Add(0)
            Else
                dst3.CopyTo(dst2, dst1)
                motionMaskUsed.Add(1)
            End If

            If motionMaskUsed.Count > 100 Then motionMaskUsed.RemoveAt(0)

            labels(2) = (motionMaskUsed.Average).ToString("0.0%") + " of the frames used the motion mask"
        End Sub
    End Class






    Public Class XR_GravityRGB_Rotate : Inherits TaskParent
        Dim vert As New GravityRGB_Vertical
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Cursor.ai: Average GravityRGB_Vertical angles-to-vertical and rotate task.color."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static lastRotateAngle As Double
            Dim avgAngle As Double
            If task.optionsChanged Or vert.lpList.Count = 0 Then
                avgAngle = task.verticalizeAngle
                If Math.Abs(avgAngle) > 90 Then avgAngle = avgAngle Mod 90
                lastRotateAngle = avgAngle
            End If

            vert.Run(src)
            dst2 = vert.dst2

            If avgAngle = 0 Then avgAngle = vert.avgAngleToVertical

            ' Hold only for tiny jitter; otherwise take the new estimate so dst3 stays true to vertical.
            Dim rotateAngle = If(Math.Abs(avgAngle - lastRotateAngle) < 1, lastRotateAngle, avgAngle)
            lastRotateAngle = rotateAngle

            dst3 = GravityRGB_Basics.rotateRGB(task.color, rotateAngle)

            If vert.lpList.Count = 0 Then
                labels(3) = "No vertical lines - fallback rotateAngle = " + rotateAngle.ToString(fmt2) + " deg"
                SetTrueText("No GravityRGB_Vertical lines; using verticalizeAngle", 3)
                If task.heartBeat Then
                    labels(2) = "No vertical lines - no avg/vertical diff"
                End If
                Return
            End If

            If task.heartBeat Then
                strOut = "Vertical lines identified = " + CStr(vert.lpList.Count) + vbCrLf +
                             "Average line angle to vertical = " + avgAngle.ToString(fmt2) + " deg" + vbCrLf +
                             "verticalizeAngle = " + task.verticalizeAngle.ToString(fmt3) + " deg"
                labels(3) = "Rotated by " + rotateAngle.ToString(fmt2) + " deg (avg of " +
                            CStr(vert.lpList.Count) + " vertical lines)"
                labels(2) = "Avg angle " + avgAngle.ToString(fmt2) + " deg, vertical = " +
                            avgAngle.ToString(fmt2) + " deg"
            End If
            SetTrueText(strOut, 1)

            lastRotateAngle = If(Math.Abs(avgAngle - lastRotateAngle) < 10, lastRotateAngle, avgAngle)
        End Sub
    End Class





    Public Class XR_GravityRGB_Compare : Inherits TaskParent
        Dim rotate As New XR_GravityRGB_Rotate
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Compare the results of using the vertical lines to just using the IMU"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rotate.Run(emptyMat)
            dst1 = rotate.dst3
            dst3 = GravityRGB_Basics.rotateRGB(task.color, task.verticalizeAngle)

            Absdiff(dst1, dst3, dst2)

        End Sub
    End Class




    Public Class XR_GravityRGB_LineRotate : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
            desc = "Cursor.ai: Rotate each line in task.lines.lpList with GravityRGB_Basics.rotateRGB and draw on 8UC1 dst2."
        End Sub
        Private Shared Function WarpPoint(pt As Point2f, M As Mat) As Point2f
            Dim xOut = M.Get(Of Double)(0, 0) * pt.X + M.Get(Of Double)(0, 1) * pt.Y + M.Get(Of Double)(0, 2)
            Dim yOut = M.Get(Of Double)(1, 0) * pt.X + M.Get(Of Double)(1, 1) * pt.Y + M.Get(Of Double)(1, 2)
            Return New Point2f(CSng(xOut), CSng(yOut))
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' Rebuild lpList from rotated endpoints (same transform as rotateRGB).
            lpList.Clear()
            dst2.SetTo(0)
            Dim angle = task.verticalizeAngle
            If Math.Abs(angle) > 90 Then angle = angle Mod 90
            Dim center = New Point2f(dst2.Width / 2.0F, dst2.Height / 2.0F)
            Dim M = GetRotationMatrix2D(center, -angle, 1)
            For Each lp In task.lines.lpList
                Dim p1 = WarpPoint(lp.p1, M)
                Dim p2 = WarpPoint(lp.p2, M)
                If Math.Abs(p1.X - p2.X) < 2 Then
                    lpList.Add(New lpData(p1, p2))
                    Line(dst2, p1, p2, white, task.lineWidth, task.lineType)
                End If
            Next

            labels(2) = CStr(task.lines.lpList.Count) + " lines rotated by " +
                        task.verticalizeAngle.ToString(fmt2) + " deg"
        End Sub
    End Class





    Public Class XR_GravityRGB_UsingVerticalLine : Inherits TaskParent
        Dim vert As New GravityRGB_Vertical
        Public rotateAngle As Double
        Public Sub New()
            desc = "Cursor.ai: Rotate GravityRGB_Vertical lines by avgAngleToVertical, average residual to make p1.X = p2.X, rotate task.gray."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            vert.Run(src)
            dst2 = task.gray

            Dim angle = CDbl(vert.avgAngleToVertical)
            If Math.Abs(angle) > 90 Then angle = angle Mod 90
            Dim center = New Point2f(dst2.Width / 2.0F, dst2.Height / 2.0F)
            Dim M = GetRotationMatrix2D(center, -angle, 1)

            rotateAngle = task.verticalizeAngle
            If vert.lpList.Count > 0 Then
                Dim lp = task.lines.lpList(0)
                Line(dst2, lp.p1, lp.p2, white)
                Dim p1 = GravityRGB_Basics.WarpPoint(lp.p1, M)
                Dim p2 = GravityRGB_Basics.WarpPoint(lp.p2, M)
                If p1.X <> p2.X Then
                    Dim dx = p2.X - p1.X
                    Dim dy = p2.Y - p1.Y
                    If dy = 0 Then dy = 1
                    Dim delta As Single = Math.Atan2(dx, dy) * RadToDeg
                    Static lastDelta = delta
                    ' the delta jumps more than 5 degrees, it is bogus.  Just use the last in that case.
                    If Math.Abs(lastDelta - delta) > 5 Then delta = lastDelta Else lastDelta = delta
                    rotateAngle += delta
                End If
            End If

            Static lastRotateAngle = rotateAngle
            If task.imuBasics.noCameraMotion Then rotateAngle = lastRotateAngle
            lastRotateAngle = rotateAngle
            dst3 = GravityRGB_Basics.rotateRGB(task.gray, rotateAngle)

            If task.heartBeat Then
                labels(2) = CStr(vert.lpList.Count) + " vertical lines, avgAngleToVertical = " +
                            vert.avgAngleToVertical.ToString(fmt2) + " deg"
                labels(3) = "Rotated gray by " + rotateAngle.ToString(fmt2) + " deg"
            End If
        End Sub
    End Class





    Public Class GravityRGB_Vertical : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public avgAngleToVertical As Single
        Public Sub New()
            desc = "Cursor.ai: Find all lines in task.lines.lpList that are nearly parallel to the IMU gravity vector."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.gray.Clone
            dst3 = task.gray.Clone

            If task.lpGravity Is Nothing Then
                SetTrueText("task.lpGravity is not available.", 2)
                labels(2) = "No gravity vector"
                Return
            End If

            Dim gAngle = task.lpGravity.angle
            Line(dst3, task.lpGravity.ptE1, task.lpGravity.ptE2, white, task.lineWidth + 1, task.lineType)

            Dim lpSorted As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            For Each lp In task.lines.lpList
                If Math.Abs(gAngle - lp.angle) < AngleThreshold Then
                    Line(dst2, lp.p1, lp.p2, white, task.lineWidth, task.lineType)
                    lpSorted.Add(lp.length, lp)
                End If
            Next

            lpList = New List(Of lpData)(lpSorted.Values)

            Dim angleList As New List(Of Single)
            For Each lp In lpList
                angleList.Add(If(lp.angle >= 0, 90 - lp.angle, -90 - lp.angle))
            Next
            avgAngleToVertical = If(angleList.Count = 0, task.verticalizeAngle, angleList.Average())
            If Math.Abs(avgAngleToVertical) > 90 Then avgAngleToVertical = avgAngleToVertical Mod 90

            If lpList.Count = 0 Then
                labels(2) = "No lines parallel to gravity (of " + CStr(task.lines.lpList.Count) + ")"
            Else
                labels(2) = CStr(lpList.Count) + " of " + CStr(task.lines.lpList.Count) +
                            " lines nearly parallel to gravity (within " + CStr(AngleThreshold) + " deg)"
            End If
            labels(3) = "lpGravity angle = " + gAngle.ToString(fmt2) + " deg"
        End Sub
    End Class
End Namespace
