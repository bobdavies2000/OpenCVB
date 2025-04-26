Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpMap As New cv.Mat
    Dim rawLines As New Line_Raw
    Public Sub New()
        lpMap = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 255)
        desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub
        If task.optionsChanged Then lpList.Clear()

        cv.Cv2.ImShow("Motion", task.motionMask)
        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingle)
        Static lastList As New List(Of lpData)
        For Each lp In lastList
            Dim noMotionTest As Boolean = True
            For Each index In lp.cellList
                Dim gc = task.gcList(index)
                If task.motionMask.Get(Of Byte)(gc.rect.TopLeft.Y, gc.rect.TopLeft.X) Then
                    noMotionTest = False
                    Exit For
                End If
            Next
            If noMotionTest Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next

        rawLines.Run(src)

        For Each lp In rawLines.lpList
            Dim motionTest As Boolean = False
            For Each index In lp.cellList
                Dim gc = task.gcList(index)
                If task.motionMask.Get(Of Byte)(gc.rect.TopLeft.Y, gc.rect.TopLeft.X) Then
                    motionTest = True
                    Exit For
                End If
            Next
            If motionTest Then sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        ' placeholder for zero so we can distinguish line 1 from the background which is 0.
        lpList.Add(New lpData(New cv.Point, New cv.Point))

        ' update lpMap from smallest to largest so the largest lines own any grid cell.
        lpMap.SetTo(0)
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            For Each index In lp.cellList
                Dim gc = task.gcList(index)
                Dim val = lpMap.Get(Of Single)(gc.rect.TopLeft.Y, gc.rect.TopLeft.X)
                If val <> 0 Then lpList(val).cellList.RemoveAt(lpList(val).cellList.IndexOf(gc.index))
                lpMap(gc.rect).SetTo(lp.index)
            Next
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2 = src.Clone
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            Next
        End If

        lastList = New List(Of lpData)(lpList)
        labels(2) = CStr(lpList.Count) + " lines were identified from the " + CStr(rawLines.lpList.Count) + " raw lines found."
    End Sub
End Class








Public Class Line_Raw : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    Dim lp = New lpData(p1, p2)
                    sortlines.Add(lp.length, lp)
                End If
            End If
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lp.p1 = validatePoint(lp.p1)
            lp.p2 = validatePoint(lp.p2)
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class








Public Class Line_RawSorted : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    Dim lp = New lpData(p1, p2)
                    sortlines.Add(lp.length, lp)
                End If
            End If
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lp.p1 = validatePoint(lp.p1)
            lp.p2 = validatePoint(lp.p2)
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class





Public Class Line_BasicsAlternative : Inherits TaskParent
    Public lines As New Line_RawSorted
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0) ' can't use 32S because calcHist won't use it...
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask.  Results are in task.lplist."
    End Sub
    Private Function getLineCounts(lpList As List(Of lpData)) As Single()
        Dim histarray(lpList.Count - 1) As Single
        If lpList.Count > 0 Then
            Dim histogram As New cv.Mat
            dst1.SetTo(0)
            For Each lp In lpList
                dst1.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth, cv.LineTypes.Link4)
            Next

            cv.Cv2.CalcHist({dst1}, {0}, task.motionMask, histogram, 1, {lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lpList.Count)})

            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
        End If

        Return histarray
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lpList.Clear()

        Dim histArray = getLineCounts(lines.lpList)
        Dim newList As New List(Of lpData)
        For i = histArray.Count - 1 To 0 Step -1
            If histArray(i) = 0 Then
                ' keep lines from the previous image NOT found in the current motion mask.
                lines.lpList(i).age += 1 ' we are keeping this line around - no motion - so bump the age.
                newList.Add(lines.lpList(i))
            End If
        Next

        If src.Channels = 1 Then lines.Run(src) Else lines.Run(task.grayStable.Clone)

        histArray = getLineCounts(task.lpList)
        For i = histArray.Count - 1 To 1 Step -1
            If histArray(i) Then
                newList.Add(task.lpList(i)) ' Add the lines in the motion mask.
            End If
        Next

        dst3.SetTo(0)
        For Each lp In newList
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
        Next

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In newList
            If lp.length > 0 Then sortlines.Add(lp.length, lp)
        Next

        task.lpList.Clear()
        ' placeholder for zero so we can distinguish line 1 from the background which is 0.
        task.lpList.Add(New lpData(New cv.Point, New cv.Point))

        dst2 = src
        For Each lp In sortlines.Values
            lp.index = task.lpList.Count
            task.lpList.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(task.lpList.Count) + " lines were found."
        labels(3) = CStr(lines.lpList.Count) + " lines were in the motion mask."
    End Sub
End Class








Public Class Line_RawSubset : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(ValidateRect(subsetRect)))

        Dim lpDataList As New List(Of lpData)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y))
                Dim p2 = New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    lpDataList.Add(New lpData(p1, p2))
                End If
            End If
        Next

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpDataList
            sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class








Public Class Line_Intercepts : Inherits TaskParent
    Public extended As New LongLine_ExtendTest
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Dim longLine As New XO_LongLine_BasicsEx
    Public options As New Options_Intercepts
    Public intercept As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public interceptArray = {topIntercepts, botIntercepts, leftIntercepts, rightIntercepts}
    Public Sub New()
        labels(2) = "Highlight line x- and y-intercepts.  Move mouse over the image."
        desc = "Show lines with similar y-intercepts"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In task.lpList
            Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
            If lp.p1.X <> minXX Then ' leftmost point is always in p1
                Dim tmp = lp.p1
                lp.p1 = lp.p2
                lp.p2 = tmp
            End If

            p1List.Add(lp.p1)
            p2List.Add(lp.p2)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)

            Dim saveP1 = lp.p1, saveP2 = lp.p2

            Dim emps = lp.BuildLongLine(lp)
            If emps.p1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
            If emps.p1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
            If emps.p1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

            If emps.p2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
            If emps.p2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
            If emps.p2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
            index += 1
        Next

        If standaloneTest() Then
            For Each inter In intercept
                If Math.Abs(options.mouseMovePoint - inter.Key) < options.interceptRange Then
                    DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cv.Scalar.Blue)
                End If
            Next
        End If
    End Sub
End Class









Public Class Line_VerticalHorizontal : Inherits TaskParent
    Dim verts As New Line_Vertical
    Dim horiz As New Line_Horizontal
    Public vertList As New List(Of lpData)
    Public horizList As New List(Of lpData)
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        labels(3) = "Vertical lines are in yellow and horizontal lines in red."
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        verts.Run(src)
        horiz.Run(src)

        Dim vList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
        Dim hList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)

        dst3.SetTo(0)
        For Each lp In verts.vertList
            vList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            DrawLine(dst3, lp.p1, lp.p2, task.highlight)
        Next

        For Each lp In horiz.horizList
            hList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Red)
        Next

        vertList = New List(Of lpData)(vList.Values)
        horizList = New List(Of lpData)(hList.Values)
        labels(2) = "Number of lines identified (vertical/horizontal): " + CStr(vList.Count) + "/" + CStr(hList.Count)
    End Sub
End Class










Public Class Line_Perpendicular : Inherits TaskParent
    Public input As lpData
    Public output As lpData
    Dim midPoint As cv.Point2f
    Public Sub New()
        labels = {"", "", "White is the original line, red dot is midpoint, yellow is perpendicular line", ""}
        desc = "Find the line perpendicular to the line created by the points provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then input = task.gravityVec
        dst2.SetTo(0)
        DrawLine(dst2, input.p1, input.p2, white)

        output = task.gravityHorizon.computePerp(input)
        DrawCircle(dst2, midPoint, task.DotSize + 2, cv.Scalar.Red)
        DrawLine(dst2, output.p1, output.p2, cv.Scalar.Yellow)
    End Sub
End Class







Public Class Line_Info : Inherits TaskParent
    Public Sub New()
        task.gOptions.DebugSlider.Value = 1 ' because the 0th element is a placeholder at 0,0
        labels(3) = "The selected line with details."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display details about the line selected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = task.lines.labels(2) + " - Use the global option 'DebugSlider' to select a line."

        If task.lpList.Count <= 1 Then Exit Sub
        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In task.lpList
                dst2.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
                DrawCircle(dst2, lp.p1, task.DotSize, task.highlight)
            Next
        End If
        If task.firstPass Then
            task.lpD = task.lpList(1)
        Else
            Dim index = task.lpMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
            task.lpD = task.lpList(index)
        End If

        strOut = "Use the global options 'DebugSlider' to select the line for display " + vbCrLf + vbCrLf
        strOut += CStr(task.lpList.Count) + " lines found " + vbCrLf + vbCrLf

        dst2.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

        strOut += "Line ID = " + CStr(task.lpD.index) + vbCrLf + vbCrLf
        strOut += "gcMap element = " + CStr(task.lpD.index) + vbCrLf
        strOut += "Age = " + CStr(task.lpD.age) + vbCrLf

        strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf + vbCrLf
        strOut += "Slope = " + Format(task.lpD.m, fmt3) + vbCrLf
        strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf

        For Each index In task.lpD.cellList
            If index = task.lpD.cellList.Last Then
                strOut += CStr(index)
            Else
                strOut += CStr(index) + ", "
            End If
        Next
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Line_Horizontal : Inherits TaskParent
    Public horizList As New List(Of lpData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        Dim p1 = task.horizonVec.p1, p2 = task.horizonVec.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        horizList.Clear()
        dst3.SetTo(0)
        For Each lp In task.lpList
            If lp.p1.X > lp.p2.X Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.Y - lp.p1.Y
            If lp.p1.X < lp.p2.X Then sideOpposite = lp.p1.Y - lp.p2.Y
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.X - lp.p2.X)) * 57.2958

            If Math.Abs(angle - hAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                horizList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(horizList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
    End Sub
End Class






Public Class Line_ViewLeftRight : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim linesRaw As New Line_RawSorted
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(task.leftView)
        dst2.SetTo(0)
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
        Next
        labels(2) = lines.labels(2)

        linesRaw.Run(task.rightView)
        dst3 = linesRaw.dst2
        labels(3) = linesRaw.labels(2)
    End Sub
End Class






Public Class Line_Vertical : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        dst3 = task.lines.dst2

        Dim p1 = task.gravityVec.p1, p2 = task.gravityVec.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        vertList.Clear()
        For Each lp In task.lpList
            If lp.p1.Y > lp.p2.Y Then lp = New lpData(lp.p2, lp.p1)

            sideOpposite = lp.p2.X - lp.p1.X
            If lp.p1.Y < lp.p2.Y Then sideOpposite = lp.p1.X - lp.p2.X
            Dim angle = Math.Atan(sideOpposite / Math.Abs(lp.p1.Y - lp.p2.Y)) * 57.2958

            If Math.Abs(angle - gAngle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                vertList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(vertList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
    End Sub
End Class






Public Class Line_GCloud : Inherits TaskParent
    Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
    Public options As New Options_LineFinder
    Dim match As New Match_tCell
    Dim angleSlider As System.Windows.Forms.TrackBar
    Dim lines As New Line_RawSorted
    Public Sub New()
        angleSlider = optiBase.FindSlider("Angle tolerance in degrees")
        labels(2) = "Line_GCloud - Blue are vertical lines using the angle thresholds."
        desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
    End Sub
    Public Function updateGLine(src As cv.Mat, gc As gravityLine, p1 As cv.Point, p2 As cv.Point) As gravityLine
        gc.tc1.center = p1
        gc.tc2.center = p2
        gc.tc1 = match.createCell(src, gc.tc1.correlation, p1)
        gc.tc2 = match.createCell(src, gc.tc2.correlation, p2)
        gc.tc1.strOut = Format(gc.tc1.correlation, fmt2) + vbCrLf + Format(gc.tc1.depth, fmt2) + "m"
        gc.tc2.strOut = Format(gc.tc2.correlation, fmt2) + vbCrLf + Format(gc.tc2.depth, fmt2) + "m"

        Dim mean = task.pointCloud(gc.tc1.rect).Mean(task.depthMask(gc.tc1.rect))
        gc.pt1 = New cv.Point3f(mean(0), mean(1), mean(2))
        gc.tc1.depth = gc.pt1.Z
        mean = task.pointCloud(gc.tc2.rect).Mean(task.depthMask(gc.tc2.rect))
        gc.pt2 = New cv.Point3f(mean(0), mean(1), mean(2))
        gc.tc2.depth = gc.pt2.Z

        gc.len3D = distance3D(gc.pt1, gc.pt2)
        If gc.pt1 = New cv.Point3f Or gc.pt2 = New cv.Point3f Then
            gc.len3D = 0
        Else
            gc.arcX = Math.Asin((gc.pt1.X - gc.pt2.X) / gc.len3D) * 57.2958
            gc.arcY = Math.Abs(Math.Asin((gc.pt1.Y - gc.pt2.Y) / gc.len3D) * 57.2958)
            If gc.arcY > 90 Then gc.arcY -= 90
            gc.arcZ = Math.Asin((gc.pt1.Z - gc.pt2.Z) / gc.len3D) * 57.2958
        End If

        Return gc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim maxAngle = angleSlider.Value

        dst2 = src.Clone
        lines.Run(src.Clone)

        sortedVerticals.Clear()
        sortedHorizontals.Clear()
        For Each lp In task.lpList
            Dim gc As gravityLine
            gc = updateGLine(src, gc, lp.p1, lp.p2)
            allLines.Add(lp.p1.DistanceTo(lp.p2), gc)
            If Math.Abs(90 - gc.arcY) < maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Blue)
            End If
            If Math.Abs(gc.arcY) <= maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), gc)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            End If
        Next

        labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " +
                    Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
    End Sub
End Class







' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Line_Intersection : Inherits TaskParent
    Public p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        intersectionPoint = IntersectTest(p1, p2, p3, p4)
        intersectionPoint = IntersectTest(New lpData(p1, p2), New lpData(p3, p4))

        dst2.SetTo(0)
        dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        dst2.Line(p3, p4, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        If intersectionPoint <> New cv.Point2f Then
            DrawCircle(dst2, intersectionPoint, task.DotSize + 4, white)
            labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
        Else
            labels(2) = "Parallel!!!"
        End If
        If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
            labels(2) += " (off screen)"
        End If
    End Sub
End Class






Public Class Line_CellList : Inherits TaskParent
    Public lp As lpData
    Public Sub New()
        desc = "Create the cellList for a given line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeatLT Then
                lp = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                dst2.SetTo(0)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
                Dim rotatedRect = cv.Cv2.MinAreaRect({lp.p1, lp.p2})
                dst2.Rectangle(rotatedRect.BoundingRect, task.highlight, task.lineWidth)
            End If
        End If


        lp.cellList.Clear()
        If lp.p1.X = lp.p2.X Then
            ' handle the special case of slope 0
            Dim x = lp.p1.X
            For y = Math.Min(lp.p1.Y, lp.p2.Y) To Math.Max(lp.p1.Y, lp.p2.Y) Step task.cellSize
                Dim index = task.gcMap.Get(Of Single)(y, x)
                lp.cellList.Add(index)
                dst2.Rectangle(task.gcList(index).rect, task.highlight, task.lineWidth)
            Next
        Else
            For x = Math.Min(lp.p1.X, lp.p2.X) To Math.Max(lp.p1.X, lp.p2.X)
                Dim y = lp.m * x + lp.b
                Dim index = task.gcMap.Get(Of Single)(y, x)
                dst2.Rectangle(task.gcList(index).rect, task.highlight, task.lineWidth)
                If lp.cellList.Contains(index) = False Then lp.cellList.Add(index)
            Next
        End If
        labels(2) = CStr(lp.cellList.Count) + " grid cells will cover the line."
    End Sub
End Class






Public Class Line_CellListValidate : Inherits TaskParent
    Public lp As lpData
    Public Sub New()
        desc = "Validate the cellList for a given line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeatLT Then
                lp = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                dst2.SetTo(0)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            End If
        End If
        For Each index In lp.cellList
            dst2.Rectangle(task.gcList(index).rect, task.highlight, task.lineWidth)
        Next
        labels(2) = CStr(lp.cellList.Count) + " grid cells will cover the line."
    End Sub
End Class
