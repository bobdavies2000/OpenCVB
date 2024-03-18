Imports cv = OpenCvSharp
Public Class HeatMap_Basics : Inherits VB_Algorithm
    Public topframes As New History_Basics
    Public sideframes As New History_Basics
    Public histogramTop As New cv.Mat
    Public histogramSide As New cv.Mat
    Dim options As New Options_HeatMap
    Public Sub New()
        desc = "Highlight concentrations of depth pixels in the side view"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, histogramTop, 2, task.bins2D, task.rangesTop)
        histogramTop.Row(0).SetTo(0)

        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, histogramSide, 2, task.bins2D, task.rangesSide)
        histogramSide.Col(0).SetTo(0)

        topframes.Run(histogramTop)
        dst0 = topframes.dst2

        sideframes.Run(histogramSide)
        dst1 = sideframes.dst2

        dst2 = vbPalette(dst0.ConvertScaleAbs())
        dst3 = vbPalette(dst1.ConvertScaleAbs())
        labels(2) = "Top view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
        labels(3) = "Side view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class








Public Class HeatMap_Grid : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public Sub New()
        gOptions.GridSize.Value = 5
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Histogram mask for top-down view - original histogram in dst0", "Histogram mask for side view - original histogram in dst1"}
        desc = "Apply a grid to the HeatMap_OverTime to isolate objects."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        heat.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim maxCount1 As Integer, maxCount2 As Integer
        Dim sync1 As New Object, sync2 As New Object
        If gOptions.UseMultiThreading.Checked Then
            Parallel.ForEach(task.gridList,
            Sub(roi)
                Dim count1 = heat.histogramTop(roi).CountNonZero
                dst2(roi).SetTo(count1)
                If count1 > maxCount1 Then
                    SyncLock sync1
                        maxCount1 = count1
                    End SyncLock
                End If

                Dim count2 = heat.histogramSide(roi).CountNonZero
                dst3(roi).SetTo(count2)
                If count2 > maxCount2 Then
                    SyncLock sync2
                        maxCount2 = count2
                    End SyncLock
                End If
            End Sub)
        Else
            For Each roi In task.gridList
                Dim count1 = heat.histogramTop(roi).CountNonZero
                dst2(roi).SetTo(count1)
                If count1 > maxCount1 Then maxCount1 = count1

                Dim count2 = heat.histogramSide(roi).CountNonZero
                dst3(roi).SetTo(count2)
                If count2 > maxCount2 Then maxCount2 = count2
            Next
        End If
        dst2 *= 255 / maxCount1
        dst3 *= 255 / maxCount2
    End Sub
End Class









Public Class HeatMap_HotNot : Inherits VB_Algorithm
    Dim heat As New HeatMap_Hot
    Public Sub New()
        labels = {"", "", "Mask of cool areas in the heat map - top view", "Mask of cool areas in the heat map - side view"}
        desc = "Isolate points with low histogram values in side and top views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst0 = heat.dst2.ConvertScaleAbs
        dst1 = heat.dst3.ConvertScaleAbs
        dst2 = heat.dst0.SetTo(0, dst0)
        dst3 = heat.dst1.SetTo(0, dst1)
    End Sub
End Class






Public Class HeatMap_Hot : Inherits VB_Algorithm
    Dim histTop As New Histogram2D_Top
    Dim histSide As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Mask of hotter areas for the Top View", "Mask of hotter areas for the Side View"}
        desc = "Isolate masks for just the hotspots in the heat map"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        histTop.Run(src)
        dst2 = histTop.histogram

        histSide.Run(src)
        dst3 = histSide.histogram

        Dim mmTop = vbMinMax(dst2)
        Dim mmSide = vbMinMax(dst3)
        If task.heartBeat Then labels(2) = CStr(mmTop.maxVal) + " max count " + CStr(dst2.CountNonZero) + " pixels in the top down view"
        If task.heartBeat Then labels(3) = CStr(mmSide.maxVal) + " max count " + CStr(dst3.CountNonZero) + " pixels in the side view"
    End Sub
End Class








Public Class HeatMap_Cell : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim heat As New HeatMap_Hot
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Display the heat map for the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)

        dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud(task.rc.rect).CopyTo(dst0(task.rc.rect), task.rc.mask)

        heat.Run(dst0)
        dst1 = heat.dst2
        dst3 = heat.dst3

        labels(1) = heat.labels(2)
        labels(3) = heat.labels(3)

        identifyCells(flood.redCells)
    End Sub
End Class








Public Class HeatMap_Objects : Inherits VB_Algorithm
    Dim guided As New GuidedBP_Basics
    Public Sub New()
        redOptions.ProjectionThreshold.Value = 1
        desc = "This is just a placeholder to make it easy to find the GuidedBP_Basics which shows objects in top/side views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)
        dst2 = guided.dst2
        dst3 = guided.dst3
        labels = guided.labels
    End Sub
End Class







Public Class HeatMap_Top1 : Inherits VB_Algorithm
    Dim histTop As New Histogram2D_Top
    Dim redC As New RedCloud_BasicsMask
    Dim redCells As New List(Of rcData)
    Public Sub New()
        redOptions.ProjectionThreshold.Value = 1
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            histTop.Run(src)

            redC.inputMask = Not histTop.dst3
            redC.Run(histTop.dst3)
            redCells.Clear()
            For Each rc In redC.redCells
                For i = 0 To redC.redCells.Count - 1
                    Dim rcBig = redC.redCells(i)
                    If rcBig.rect.Contains(rc.rect) Then
                        rcBig.rect = rcBig.rect.Union(rc.rect)
                        redCells.Add(rcBig)
                        Exit For
                    End If
                Next
            Next
        End If

        dst2 = redC.dst2
        For Each rc In redCells
            dst2.Rectangle(rc.rect, task.highlightColor, task.lineWidth)
            If rc.index < redOptions.identifyCount Then setTrueText(CStr(rc.index), New cv.Point(rc.rect.X - 10, rc.rect.Y))
        Next
        labels(2) = CStr(redCells.Count) + " objects were found in the top view."
    End Sub
End Class








Public Class HeatMap_Side : Inherits VB_Algorithm
    Dim histSide As New Histogram2D_Side
    Dim redC As New RedCloud_BasicsMask
    Dim redCells As New List(Of rcData)
    Public Sub New()
        redOptions.ProjectionThreshold.Value = 1
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            Dim depthCount = task.pcSplit(2).CountNonZero
            histSide.Run(src)
            dst3 = histSide.dst2

            redC.inputMask = Not histSide.dst3
            redC.Run(histSide.dst3)

            For i = 0 To redC.redCells.Count - 1
                Dim rc = redC.redCells(i)
                Dim tmp = New cv.Mat(rc.rect.Size, cv.MatType.CV_32F, 0)
                dst3(rc.rect).CopyTo(tmp, rc.mask)
                redC.redCells(i).pixels = tmp.Sum()
            Next

            For i = 0 To redC.redCells.Count - 1
                Dim rcBig = redC.redCells(i)
                For j = i + 1 To redC.redCells.Count - 1
                    Dim rc = redC.redCells(j)
                    If rc.pixels = 0 Then Continue For
                    If rcBig.rect.Contains(rc.rect) Then
                        rcBig.rect = rcBig.rect.Union(rc.rect)
                        redC.redCells(i).pixels += rc.pixels
                        redC.redCells(j).pixels = 0
                    End If
                Next
            Next

            Dim check1 = dst3.Sum()(0)

            Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim check2 As Integer
            For Each rc In redC.redCells
                sortedCells.Add(rc.pixels, rc)
                check2 += rc.pixels
            Next

            redCells.Clear()
            redCells.Add(New rcData)
            For Each rc In sortedCells.Values
                rc.index = redCells.Count
                redCells.Add(rc)
            Next

            Dim otherCount As Integer
            strOut = ""
            For Each rc In redCells
                If rc.index = 0 Then Continue For
                If rc.index <= redOptions.identifyCount Then
                    dst2.Rectangle(rc.rect, task.highlightColor, task.lineWidth)
                    Dim y1 = (task.rangesSide(0).End - task.rangesSide(0).Start) * rc.rect.Y / dst2.Height
                    Dim y2 = (task.rangesSide(0).End - task.rangesSide(0).Start) * (rc.rect.Y + rc.rect.Height) / dst2.Height
                    Dim z1 = (task.rangesSide(1).End - task.rangesSide(1).Start) * rc.rect.X / dst2.Width
                    Dim z2 = (task.rangesSide(1).End - task.rangesSide(1).Start) * (rc.rect.X + rc.rect.Width) / dst2.Width
                    strOut += "Object " + vbTab + CStr(rc.index) + vbTab + Format(y2 - y1, fmt3) + " m high " + vbTab +
                               Format(z1, fmt1) + "m to " + Format(z2, fmt1) + "m from camera" + vbTab + CStr(rc.pixels) + " pixels" + vbCrLf
                Else
                    otherCount += rc.pixels
                End If
            Next

            strOut += "Other  " + vbTab + CStr(otherCount) + " pixels" + vbCrLf
            strOut += "Total  " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check1 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check2 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
        End If
        setTrueText(strOut, 3)

        dst2 = redC.dst2
        labels(2) = CStr(redCells.Count) + " objects were found in the top view."
    End Sub
End Class







Public Class HeatMap_Top : Inherits VB_Algorithm
    Dim histTop As New Histogram2D_Top
    Dim redC As New RedCloud_BasicsMask
    Dim redCells As New List(Of rcData)
    Public Sub New()
        redOptions.ProjectionThreshold.Value = 1
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            Dim depthCount = task.pcSplit(2).CountNonZero
            histTop.Run(src)
            dst3 = histTop.dst2

            redC.inputMask = Not histTop.dst3
            redC.Run(histTop.dst3)

            For i = 0 To redC.redCells.Count - 1
                Dim rc = redC.redCells(i)
                Dim tmp = New cv.Mat(rc.rect.Size, cv.MatType.CV_32F, 0)
                dst3(rc.rect).CopyTo(tmp, rc.mask)
                redC.redCells(i).pixels = tmp.Sum()
            Next

            For i = 0 To redC.redCells.Count - 1
                Dim rcBig = redC.redCells(i)
                For j = i + 1 To redC.redCells.Count - 1
                    Dim rc = redC.redCells(j)
                    If rc.pixels = 0 Then Continue For
                    If rcBig.rect.Contains(rc.rect) Then
                        rcBig.rect = rcBig.rect.Union(rc.rect)
                        redC.redCells(i).pixels += rc.pixels
                        redC.redCells(j).pixels = 0
                    End If
                Next
            Next

            Dim check1 = dst3.Sum()(0)

            Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim check2 As Integer
            For Each rc In redC.redCells
                sortedCells.Add(rc.pixels, rc)
                check2 += rc.pixels
            Next

            redCells.Clear()
            redCells.Add(New rcData)
            For Each rc In sortedCells.Values
                rc.index = redCells.Count
                redCells.Add(rc)
            Next

            Dim otherCount As Integer
            strOut = ""
            For Each rc In redCells
                If rc.index = 0 Then Continue For
                If rc.index <= redOptions.identifyCount Then
                    dst2.Rectangle(rc.rect, task.highlightColor, task.lineWidth)
                    Dim xy1 = (task.rangesSide(0).End - task.rangesSide(0).Start) * rc.rect.Y / dst2.Height
                    Dim xy2 = (task.rangesSide(0).End - task.rangesSide(0).Start) * (rc.rect.Y + rc.rect.Height) / dst2.Height
                    Dim z1 = (task.rangesSide(1).End - task.rangesSide(1).Start) * rc.rect.X / dst2.Width
                    Dim z2 = (task.rangesSide(1).End - task.rangesSide(1).Start) * (rc.rect.X + rc.rect.Width) / dst2.Width
                    strOut += "Object " + vbTab + CStr(rc.index) + vbTab + Format(xy2 - xy1, fmt3) + " m " + vbTab +
                               Format(z1, fmt1) + "m to " + Format(z2, fmt1) + "m from camera" + vbTab + CStr(rc.pixels) + " pixels" + vbCrLf
                Else
                    otherCount += rc.pixels
                End If
            Next

            strOut += "Other  " + vbTab + CStr(otherCount) + " pixels" + vbCrLf
            strOut += "Total  " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check1 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check2 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
        End If
        setTrueText(strOut, 3)

        dst2 = redC.dst2
        labels(2) = CStr(redCells.Count) + " objects were found in the top view."
    End Sub
End Class









Public Class HeatMap_ObjectList : Inherits VB_Algorithm
    Public redCellInput As New List(Of rcData)
    Public redCells As New List(Of rcData)
    Public viewType As String = "Top"
    Public Sub New()
        redOptions.ProjectionThreshold.Value = 1
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            If standalone Then
                Static histTop As New Histogram2D_Top
                histTop.Run(src)
                src = histTop.dst2

                Static redc As New RedCloud_BasicsMask
                redc.inputMask = Not histTop.dst3
                redC.Run(histTop.dst3)
                redCellInput = redc.redCells
                dst2 = redc.dst2
                labels(2) = redc.labels(2)
            End If
            Dim depthCount = task.pcSplit(2).CountNonZero

            For i = 0 To redCellInput.Count - 1
                Dim rc = redCellInput(i)
                Dim tmp = New cv.Mat(rc.rect.Size, cv.MatType.CV_32F, 0)
                src(rc.rect).CopyTo(tmp, rc.mask)
                redCellInput(i).pixels = tmp.Sum()
            Next

            For i = 0 To redCellInput.Count - 1
                Dim rcBig = redCellInput(i)
                For j = i + 1 To redCellInput.Count - 1
                    Dim rc = redCellInput(j)
                    If rc.pixels = 0 Then Continue For
                    If rcBig.rect.Contains(rc.rect) Then
                        rcBig.rect = rcBig.rect.Union(rc.rect)
                        redCellInput(i).pixels += rc.pixels
                        redCellInput(j).pixels = 0
                    End If
                Next
            Next

            Dim check1 = src.Sum()(0)

            Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim check2 As Integer
            For Each rc In redCellInput
                sortedCells.Add(rc.pixels, rc)
                check2 += rc.pixels
            Next

            redCells.Clear()
            redCells.Add(New rcData)
            For Each rc In sortedCells.Values
                rc.index = redCells.Count
                redCells.Add(rc)
            Next

            Dim otherCount As Integer
            strOut = ""
            Dim meterDesc = "high"
            If viewType = "Top" Then meterDesc = "wide"
            For Each rc In redCells
                If rc.index = 0 Then Continue For
                If rc.index <= redOptions.identifyCount Then
                    dst2.Rectangle(rc.rect, task.highlightColor, task.lineWidth)
                    Dim xy1 = (task.rangesSide(0).End - task.rangesSide(0).Start) * rc.rect.Y / dst2.Height
                    Dim xy2 = (task.rangesSide(0).End - task.rangesSide(0).Start) * (rc.rect.Y + rc.rect.Height) / dst2.Height
                    Dim z1 = (task.rangesSide(1).End - task.rangesSide(1).Start) * rc.rect.X / dst2.Width
                    Dim z2 = (task.rangesSide(1).End - task.rangesSide(1).Start) * (rc.rect.X + rc.rect.Width) / dst2.Width
                    strOut += "Object " + vbTab + CStr(rc.index) + vbTab + Format(xy2 - xy1, fmt3) + " m " + meterDesc + vbTab +
                               Format(z1, fmt1) + "m " + " to " + Format(z2, fmt1) + "m from camera" + vbTab + CStr(rc.pixels) + " pixels" + vbCrLf
                Else
                    otherCount += rc.pixels
                End If
            Next

            strOut += "Other  " + vbTab + CStr(otherCount) + " pixels" + vbCrLf
            strOut += "Total  " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check1 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
            strOut += "Check2 " + vbTab + CStr(depthCount) + " pixels" + vbCrLf
        End If
        setTrueText(strOut, 3)

        labels(2) = CStr(redCells.Count) + " objects were found in the " + viewType + " view."
    End Sub
End Class