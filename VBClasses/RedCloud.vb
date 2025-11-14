Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Public redSweep As New RedCloud_Sweep
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public percentImage As Single
    Public Sub New()
        task.redCloud = Me
        desc = "Build contours for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        labels(3) = redSweep.labels(3)
        labels(2) = redSweep.labels(2) + If(standalone, "  Number is cell age", "")

        Dim rcListLast = New List(Of rcData)(rcList)
        Dim rcMapLast As cv.Mat = rcMap.Clone

        rcList.Clear()
        Dim r2 As cv.Rect
        rcMap.SetTo(0)
        dst2.SetTo(0)
        For Each rc In redSweep.rcList
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If indexLast > 0 Then
                indexLast -= 1 ' index is 1 less than the rcMap value
                r2 = rcListLast(indexLast).rect
            End If
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                Dim lrc = rcListLast(indexLast)
                If rc.rect.Contains(lrc.maxDist) Then
                    Dim row = lrc.maxDist.Y - lrc.rect.Y
                    Dim col = lrc.maxDist.X - lrc.rect.X
                    If row < rc.mask.Height And col < rc.mask.Width Then
                        If rc.mask.Get(Of Byte)(row, col) Then ' more doublechecking...
                            rc.maxDist = lrc.maxDist
                            rc.depth = lrc.depth
                        End If
                    End If
                End If

                rc.age = lrc.age + 1
                If rc.age > 1000 Then rc.age = 2

                If task.motionRect.Contains(rc.maxDist) Then rc.age = 1

                rc.color = lrc.color
            End If
            rc.index = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            rcList.Add(rc)
        Next

        If standalone Then
            For Each rc In rcList
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
            Next
        End If

        If standaloneTest() Then
            RedCloud_Cell.selectCell(rcMap, rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 3)
        End If
    End Sub
End Class




Public Class RedCloud_Sweep : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Shared Function sweepImage(input As cv.Mat) As List(Of rcData)
        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minSize As Integer = input.Total * 0.001
        Dim rc As rcData = Nothing
        Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim minCount As Integer
        For y = 0 To input.Height - 1
            For x = 0 To input.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If input.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    Dim count = cv.Cv2.FloodFill(input, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minSize Then
                            rc = New rcData(input(rect), rect, index)
                            If rc.index < 0 Then Continue For
                            newList.Add(rc.pixels, rc)
                            index += 1
                        Else
                            minCount += 1
                        End If
                    End If
                End If
            Next
        Next
        Return New List(Of rcData)(newList.Values)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = prepEdges.dst2.Clone

        rcList = sweepImage(dst3)

        Dim index As Integer
        dst2.SetTo(0)
        rcMap.SetTo(0)
        For Each rc In rcList
            index += 1
            rc.index = index
            rc.color = task.vecColors(rc.index Mod 255)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next

        labels(2) = CStr(rcList.Count) + " RedCloud cells were identified."

        Static unchanged As Integer
        If task.motionRect.Width = 0 Then
            unchanged += 1
            labels(3) = "RedCloud cells were unchanged " + CStr(unchanged) + " times since last heartBeatLT"
        End If
        If task.heartBeatLT Then unchanged = 0
    End Sub
End Class







Public Class RedCloud_CellDepthHistogram : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2))

        RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell
        SetTrueText(strOut, 1)

        If task.rcD Is Nothing Then
            labels(3) = "Select a RedCloud cell to see the histogram"
            Exit Sub
        End If

        Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
        depth.SetTo(0, task.noDepthMask(task.rcD.rect))
        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(depth)
        labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

        Dim incr = dst2.Width / task.MaxZmeters
        For i = 1 To CInt(task.MaxZmeters - 1)
            Dim x = incr * i
            DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
        Next
        dst3 = plot.dst2
    End Sub
End Class





Public Class RedCloud_CellMask : Inherits TaskParent
    Dim redMotion As New XO_RedCloud_Motion
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask that outlines all the RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMotion.Run(src)

        dst3.SetTo(0)
        For Each rc In task.redCloud.rcList
            Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
            cv.Cv2.DrawContours(dst3(rc.rect), listOfPoints, 0, white, task.lineWidth, cv.LineTypes.Link8)
        Next

        dst2 = task.redCloud.redSweep.dst1
    End Sub
End Class





Public Class RedCloud_Cell : Inherits TaskParent
    Public Sub New()
        desc = "Display the output of a RedCloud cell."
    End Sub
    Public Shared Sub selectCell(rcMap As cv.Mat, rcList As List(Of rcData))
        If rcList.Count > 0 Then
            Dim clickIndex = rcMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X) - 1
            If clickIndex >= 0 And clickIndex < rcList.Count Then
                task.rcD = rcList(clickIndex)
            Else
                Dim ages As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
                For Each pc In rcList
                    ages.Add(pc.age, pc.index - 1)
                Next
                task.rcD = rcList(ages.ElementAt(0).Value)
            End If
            If task.rcD.rect.Contains(task.ClickPoint) Then
                task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                Exit Sub
            End If
        End If
        task.rcD = Nothing
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then dst2 = runRedCloud(src, labels(2))

        selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class RedCloud_Small : Inherits TaskParent
    Dim minRes As cv.Size
    Public Sub New()
        Select Case CStr(task.cols) + "x" + CStr(task.rows)
            Case "1920x1080", "960x540", "480x270"
                minRes = New cv.Size(480, 270)
            Case "1280x720", "640x360", "320x180"
                minRes = New cv.Size(320, 180)
            Case "640x480", "320x240", "160x120"
                minRes = New cv.Size(160, 120)
            Case "960x600", "480x300", "240x150"
                minRes = New cv.Size(240, 150)
            Case "672x376", "336x188", "168x94"
                minRes = New cv.Size(168, 94)
        End Select
        desc = "Run RedCloud at the smallest resolution and resize. NOT WORTH IT!"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim minRect = New cv.Rect(0, 0, minRes.Width, minRes.Height)
        If src.Size <> minRes Then src = task.pointCloud.Resize(minRes) Else src = task.pointCloud
        dst1 = runRedCloud(src, labels(2))(minRect)
        dst2 = dst1.Resize(task.workRes)

        If task.firstPass Then
            OptionParent.FindSlider("Reduction Target").Value = 400
        End If

        Dim ratio = CInt(task.workRes.Width / minRes.Width)
        task.redCloud.rcMap.SetTo(0)
        For Each rc In task.redCloud.rcList
            Dim r = rc.rect
            rc.rect = New cv.Rect(r.X * ratio, r.Y * ratio, r.Width * ratio, r.Height * ratio)
            Dim maskSize = New cv.Size(rc.rect.Width, rc.rect.Height)
            rc.mask = rc.mask.Resize(maskSize)
            task.redCloud.rcMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
        SetTrueText(strOut, 3)
    End Sub
End Class

