Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Vertical_Basics : Inherits TaskParent
        Dim lines As New LineSeg_Core
        Dim lpList As New List(Of lpData)
        Public Sub New()
            desc = "Find all the vertical and horizontal lines in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim sortScores As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
            Dim lpLong = task.longestLine
            For i = -5 To 5
                Dim angle = task.verticalizeAngle + 0.02 * i
                dst3 = GravityRGB_Basics.rotateRGB(task.gray(lpLong.rect), angle)
                lines.Run(dst3)
                For Each lp In lines.lpList
                    If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then sortScores.Add(lp.length, i)
                Next
            Next
            Dim optimalAngle = task.verticalizeAngle + 0.01 * sortScores.Values(0)

            If task.heartBeat Then dst2.SetTo(0)
            lpList.Clear()
            For Each lp In lines.lpList
                If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then
                    Line(dst2, lp.p1, lp.p2, white, task.lineWidth)
                    lpList.Add(lp)
                End If
            Next

            labels(2) = CStr(lpList.Count) + " vertical lines were found."
            labels(3) = "Optimal angle is " + optimalAngle.ToString("0.000") + " optimal index = " + CStr(sortScores.Values(0))
        End Sub
    End Class




    Public Class Vertical_Explore : Inherits TaskParent
        Dim lines As New LineSeg_Core
        Dim lpList As New List(Of lpData)
        Dim sortScores As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        Public Sub New()
            desc = "Find all the vertical and horizontal lines in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static index As Integer = -100
            Static startAngle As Double
            If task.heartBeat Then startAngle = task.verticalizeAngle
            Dim angle = startAngle + 0.01 * index
            dst3 = GravityRGB_Basics.rotateRGB(task.gray, angle)
            lines.Run(dst3)

            dst2.SetTo(0)
            lpList.Clear()
            For Each lp In lines.lpList
                If Math.Abs(lp.p1.X - lp.p2.X) <= 1 Then
                    Line(dst2, lp.p1, lp.p2, white, task.lineWidth)
                    lpList.Add(lp)
                    sortScores.Add(lp.length, index)
                End If
            Next
            labels(2) = CStr(lpList.Count) + " vertical lines were found.  Angle = " + angle.ToString("0.000")
            Dim optimalAngle = startAngle + 0.01 * sortScores.Values(0)
            labels(3) = "Optimal angle is " + optimalAngle.ToString("0.000") + " starting angle = " +
                        startAngle.ToString("0.000") + " optimal index = " + CStr(sortScores.Values(0))
            index += 1
            If index >= 100 Then index = -100
            If sortScores.Count >= 200 Then sortScores.RemoveAt(0)
        End Sub
    End Class





    Public Class Vertical_Longest : Inherits TaskParent
        Public longestLine As lpData
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            labels = {"", "", "Longest line", "longest line aligned with Gravity"}
            desc = "Rotate the longest line's lp.rect to gravity with verticalizeAngle and run Line_Basics_TA on it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If longestLine Is Nothing Then longestLine = task.longestLine
            Dim lp As lpData = longestLine
            dst2.SetTo(0)
            Line(dst2, lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)

            dst1 = GravityRGB_Basics.rotateRGB(dst2(lp.rect), task.verticalizeAngle)

            Dim aspectRect = lp.rect.Width / CSng(lp.rect.Height)
            Dim aspect = dst3.Width / CSng(dst3.Height)
            Dim r As cv.Rect
            If aspectRect > aspect Then
                r = New cv.Rect(0, 0, dst3.Width, CInt(lp.rect.Height * dst3.Width / CSng(lp.rect.Width)))
            Else
                r = New cv.Rect(0, 0, CInt(lp.rect.Width * dst3.Height / CSng(lp.rect.Height)), dst3.Height)
            End If
            r = ValidateRect(r)

            Resize(dst1, dst0(r), r.Size)

            Dim topX As Integer, botX As Integer
            For x = 0 To r.Width - 1
                If dst0.Row(0).Get(Of Byte)(0, x) Then
                    topX = x
                    Exit For
                End If
            Next
            For x = 0 To r.Width - 1
                If dst0.Row(dst0.Height - 1).Get(Of Byte)(0, x) Then
                    botX = x
                    Exit For
                End If
            Next

            dst3.SetTo(0)
            lp = New lpData(New cv.Point(topX, 0), New cv.Point(botX, dst3.Height - 1))
            Line(dst3, lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link4)
            SetTrueText(CStr(topX), New cv.Point(topX + 4, 0), 3)
            SetTrueText(CStr(botX), New cv.Point(botX + 4, dst3.Height - 10), 3)
        End Sub
    End Class




    Public Class Vertical_Gravity : Inherits TaskParent
        Dim gravity As New GravityRGB_Basics
        Dim lines As New Line_Core
        Public Sub New()
            desc = "Cursor.ai: Find longest vertical line after rotating using the IMU data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            gravity.Run(task.gray)
            lines.Run(gravity.dst3)

            Dim sortList As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            For Each lpC In lines.lpList
                If Math.Abs(lpC.p1.X - lpC.p2.X) <= 2 Then sortList.Add(lpC.length, lpC)
            Next
            If sortList.Count = 0 Then
                SetTrueText("No near-vertical lines found.", 3)
                Return
            End If

            Dim lp = sortList.Values(0)

            ' If the line is almost vertical but p1.X and p2.X still differ by 1 or 2 pixels,
            ' nudge verticalizeAngle by the rotation needed to make p1.X = p2.X.
            Dim dx = lp.p2.X - lp.p1.X
            Dim absDx = Math.Abs(dx)
            If absDx >= 1 AndAlso absDx <= 2 Then
                Dim dy = lp.p2.Y - lp.p1.Y
                If dy = 0 Then dy = 1
                Dim deltaAngle = Math.Atan2(dx, dy) * RadToDeg
                task.verticalizeAngle += deltaAngle
                strOut = "Adjusted verticalizeAngle by " + deltaAngle.ToString(fmt3) + " deg" + vbCrLf +
                         "dx=" + dx.ToString(fmt1) + "  new verticalizeAngle=" + task.verticalizeAngle.ToString(fmt3)
            Else
                strOut = "No adjustment (dx=" + dx.ToString(fmt1) + ")" + vbCrLf +
                         "verticalizeAngle=" + task.verticalizeAngle.ToString(fmt3)
            End If

            If standalone Then
                labels(2) = CStr(sortList.Count) + " vertical lines found.  The longest EP line is shown."
                dst3.SetTo(0)
                dst3(lp.rect).SetTo(0)
                Line(dst3, lp.ptE1, lp.ptE2, white, task.lineWidth, cv.LineTypes.Link4)
                SetTrueText(CStr(lp.p1.X), New cv.Point(lp.p1.X + 4, 0), 3)
                SetTrueText(CStr(lp.p2.X), New cv.Point(lp.p2.X + 4, dst3.Height - 10), 3)
                SetTrueText(strOut, 2)
            End If
        End Sub
    End Class




    Public Class Vertical_Image : Inherits TaskParent
        Dim vert As New Vertical_Gravity
        Public Sub New()
            desc = "Display the verticalized image after the IMU angle has been tweaked by Vertical_Gravity"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            vert.Run(task.gray)
            dst2 = GravityRGB_Basics.rotateRGB(task.color, task.verticalizeAngle)
        End Sub
    End Class

End Namespace
