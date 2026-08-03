Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class GravityRGB_Basics : Inherits TaskParent
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "Original RGB", "RGB rotated with IMU gravity data"}
            desc = "Cursor.ai: Rotate the RGB image using the same IMU gravity data used by Cloud_Gravity."
        End Sub
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
            dst3 = rotateRGB(src, task.verticalizeAngle)

            strOut = "Same IMU gravity data used by Cloud_Gravity (gMatrix):" + vbCrLf +
                 "verticalizeAngle = " + task.verticalizeAngle.ToString(fmt2) + " deg" + vbCrLf +
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





    Public Class GravityRGB_Line : Inherits TaskParent
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




    Public Class GravityRGB_Motion : Inherits TaskParent
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





    Public Class GravityRGB_Vertical : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public Sub New()
            desc = "Cursor.ai: Find all lines in task.lines.lpList that are nearly parallel to the IMU gravity vector."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lpList.Clear()
            dst2 = task.color.Clone
            dst3 = task.color.Clone

            If task.lpGravity Is Nothing Then
                SetTrueText("task.lpGravity is not available.", 2)
                labels(2) = "No gravity vector"
                Return
            End If

            Dim gAngle = task.lpGravity.angle
            Line(dst3, task.lpGravity.ptE1, task.lpGravity.ptE2, task.highlight, task.lineWidth + 1, task.lineType)

            For Each lp In task.lines.lpList
                If Math.Abs(gAngle - lp.angle) < AngleThreshold Then
                    Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                    lpList.Add(lp)
                End If
            Next

            If lpList.Count = 0 Then
                labels(2) = "No lines parallel to gravity (of " + CStr(task.lines.lpList.Count) + ")"
            Else
                labels(2) = CStr(lpList.Count) + " of " + CStr(task.lines.lpList.Count) +
                            " lines nearly parallel to gravity (within " + CStr(AngleThreshold) + " deg)"
            End If
            labels(3) = "lpGravity angle = " + gAngle.ToString(fmt2) + " deg"
        End Sub
    End Class





    Public Class GravityRGB_Rotate : Inherits TaskParent
        Dim vert As New GravityRGB_Vertical
        Dim lastRotateAngle As Double
        Dim angleReady As Boolean
        Public avgAngle As Double
        Public rotateAngle As Double
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Cursor.ai: Average GravityRGB_Vertical angles-to-vertical and rotate task.color."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then angleReady = False

            vert.Run(src)
            dst2 = task.color

            Dim angleDiff As Double
            If vert.lpList.Count = 0 Then
                angleDiff = task.verticalizeAngle
                If Math.Abs(angleDiff) > 90 Then angleDiff = angleDiff Mod 90
                avgAngle = If(task.lpGravity Is Nothing, 90.0, CDbl(task.lpGravity.angle))
            Else
                Dim sumAngle As Double = 0
                Dim sumToVertical As Double = 0
                For Each lp In vert.lpList
                    sumAngle += lp.angle
                    ' Signed angle from image vertical (90 deg), same sign as verticalizeAngle / Vertical_Gravity.
                    If lp.angle >= 0 Then
                        sumToVertical += 90 - lp.angle
                    Else
                        sumToVertical += -90 - lp.angle
                    End If
                Next
                avgAngle = sumAngle / vert.lpList.Count
                angleDiff = sumToVertical / vert.lpList.Count
                If Math.Abs(angleDiff) > 90 Then angleDiff = angleDiff Mod 90
            End If

            ' Local previous angle — IMU overwrites task.verticalizeAngle every frame.
            If Not angleReady Then
                lastRotateAngle = angleDiff
                angleReady = True
            End If

            ' Hold only for tiny jitter; otherwise take the new estimate so dst3 stays true to vertical.
            If Math.Abs(angleDiff - lastRotateAngle) < 0.1 Then
                rotateAngle = lastRotateAngle
            Else
                rotateAngle = angleDiff
            End If
            lastRotateAngle = rotateAngle
            task.verticalizeAngle = rotateAngle

            dst3 = GravityRGB_Basics.rotateRGB(task.color, rotateAngle)

            If vert.lpList.Count = 0 Then
                labels(3) = "No vertical lines - fallback rotateAngle = " + rotateAngle.ToString(fmt2) + " deg"
                SetTrueText("No GravityRGB_Vertical lines; using verticalizeAngle", 3)
                If task.heartBeat Then
                    labels(2) = "No vertical lines - no avg/vertical diff"
                End If
                Return
            End If

            strOut = "Vertical lines = " + CStr(vert.lpList.Count) + vbCrLf +
                     "Average line angle = " + avgAngle.ToString(fmt2) + " deg" + vbCrLf +
                     "Average angle to vertical = " + rotateAngle.ToString(fmt2) + " deg" + vbCrLf +
                     "verticalizeAngle = " + task.verticalizeAngle.ToString(fmt3) + " deg"
            SetTrueText(strOut, 1)
            labels(3) = "Rotated by " + rotateAngle.ToString(fmt2) + " deg (avg of " +
                        CStr(vert.lpList.Count) + " vertical lines)"
            If task.heartBeat Then
                labels(2) = "Avg angle " + avgAngle.ToString(fmt2) + " deg, vertical = " +
                            angleDiff.ToString(fmt2) + " deg"
            End If
        End Sub
    End Class

End Namespace