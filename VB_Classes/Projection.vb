Imports cv = OpenCvSharp
Imports System.Windows.Forms
Public Class Projection_Basics : Inherits VB_Parent
    Public redCellInput As New List(Of rcData)
    Public redCells As New List(Of rcData)
    Public viewType As String = "Top"
    Public objectList As New List(Of cv.Vec4f)
    Public showRectangles As Boolean = True
    Dim histTop As New Projection_HistTop
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Find all the masks, rects, and counts in the input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            histTop.Run(src)
            src = histTop.dst2

            redC.inputMask = Not histTop.dst3
            redC.Run(histTop.dst3)
            redCellInput = task.redCells
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim check2 As Integer
        For i = 0 To redCellInput.Count - 1
            Dim rc = redCellInput(i)
            Dim tmp = New cv.Mat(rc.rect.Size(), cv.MatType.CV_32F, 0)
            src(rc.rect).CopyTo(tmp, rc.mask)
            rc.pixels = tmp.Sum()
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
        Dim meterDesc = "tall"
        Dim ranges = task.rangesSide
        If viewType = "Top" Then
            meterDesc = "wide"
            ranges = task.rangesTop
        End If
        objectList.Clear()
        Dim xy1 As Single, xy2 As Single, z1 As Single, z2 As Single
        If task.heartBeat Then strOut = ""
        For Each rc In redCells
            If rc.index = 0 Then Continue For
            If rc.index <= task.redOptions.identifyCount Then
                If viewType = "Side" Then
                    xy1 = (ranges(0).End - ranges(0).Start) * rc.rect.Y / dst2.Height + ranges(0).Start
                    xy2 = (ranges(0).End - ranges(0).Start) * (rc.rect.Y + rc.rect.Height) / dst2.Height + ranges(0).Start
                    z1 = (ranges(1).End - ranges(1).Start) * rc.rect.X / dst2.Width
                    z2 = (ranges(1).End - ranges(1).Start) * (rc.rect.X + rc.rect.Width) / dst2.Width
                Else
                    xy1 = (ranges(1).End - ranges(1).Start) * rc.rect.X / dst2.Width + ranges(1).Start
                    xy2 = (ranges(1).End - ranges(1).Start) * (rc.rect.X + rc.rect.Width) / dst2.Width + ranges(1).Start
                    z1 = (ranges(0).End - ranges(0).Start) * rc.rect.Y / dst2.Height
                    z2 = (ranges(0).End - ranges(0).Start) * (rc.rect.Y + rc.rect.Height) / dst2.Height
                End If
                objectList.Add(New cv.Vec4f(xy1, xy2, z1, z2))
                If task.heartBeat Then
                    strOut += "Object " + vbTab + CStr(rc.index) + vbTab + Format(xy2 - xy1, fmt3) + " m " + meterDesc + vbTab +
                               Format(z1, fmt1) + "m " + " to " + Format(z2, fmt1) + "m from camera" + vbTab + CStr(rc.pixels) + " pixels" + vbCrLf
                End If
            Else
                otherCount += rc.pixels
            End If
        Next

        If task.heartBeat Then
            Dim check1 = src.Sum()(0)
            Dim depthCount = task.pcSplit(2).CountNonZero
            strOut += CStr(redCells.Count - task.redOptions.identifyCount - 1) + " other objects " + vbTab + CStr(otherCount) + " pixels" + vbCrLf
            strOut += "Sum above   " + vbTab + CStr(check2) + " pixels" + " (losses from histogram ranges?)" + vbCrLf
            strOut += "Sum of src  " + vbTab + CStr(check1) + " pixels" + " (losses from RedCloud.)" + vbCrLf
            strOut += "Actual count" + vbTab + CStr(depthCount) + " pixels" + vbCrLf
        End If
        SetTrueText(strOut, 3)
        If showRectangles Then
            For i = 0 To Math.Min(redCells.Count, task.redOptions.identifyCount) - 1
                dst2.Rectangle(redCells(i).rect, task.HighlightColor, task.lineWidth)
            Next
        End If
        labels(2) = CStr(redCells.Count) + " objects were found in the " + viewType + " view."
    End Sub
End Class







Public Class Projection_HistSide : Inherits VB_Parent
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Top view with histogram counts", "ZY (Side View) - mask"}
        UpdateAdvice(traceName + ": redOptions 'Project threshold' affects how many regions are isolated.")
        desc = "Create a 2D side view for ZY histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
        histogram.Col(0).SetTo(0)

        dst2 = histogram.ConvertScaleAbs
        dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class Projection_HistTop : Inherits VB_Parent
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Top view with histogram counts", "XZ (Top View) - mask"}
        UpdateAdvice(traceName + ": redOptions 'Project threshold' affects how many regions are isolated.")
        desc = "Create a 2D top view for XZ histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
        histogram.Row(0).SetTo(0)

        dst2 = histogram.ConvertScaleAbs
        dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class








Public Class Projection_Lines : Inherits VB_Parent
    Dim heat As New HeatMap_Basics
    Dim lines As New Line_Basics
    Dim options As New Options_Projection
    Public Sub New()
        FindCheckBox("Top View (Unchecked Side View)").Checked = False
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        labels = {"", "Lines found in the threshold output", "FeatureLess cells found", "Projections of each of the FeatureLess cells"}
        desc = "Search for surfaces among the FeatureLess regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.heartBeat Then
            dst1.SetTo(0)
            dst3.SetTo(0)
        End If
        heat.Run(src)
        If options.topCheck Then dst2 = heat.dst2 Else dst2 = heat.dst3
        dst1 = dst2.Threshold(options.projectionThreshold, 255, cv.ThresholdTypes.Binary)

        lines.Run(dst1)
        dst3 += lines.dst3
    End Sub
End Class










Public Class Projection_Cell : Inherits VB_Parent
    Dim heat As New HeatMap_Basics
    Dim heatCell As New HeatMap_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32FC3, 0)
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.unFiltered.Checked = True
        labels = {"", "Top View projection of the selected cell", "RedCloud_Basics output - select a cell to project at right and above", "Side projection of the selected cell"}
        desc = "Create a top and side projection of the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        heat.Run(src)
        dst1 = heat.dst2.Clone
        dst3 = heat.dst3.Clone

        Dim rc = task.rc

        dst0.SetTo(0)
        task.pointCloud(rc.rect).CopyTo(dst0(rc.rect), rc.mask)

        heatCell.Run(dst0)
        Dim maskTop = heatCell.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim maskSide = heatCell.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        If maskTop.CountNonZero = 0 And maskSide.CountNonZero = 0 Then SetTrueText("The selected cell has no depth data.", 3)
        dst1.SetTo(cv.Scalar.White, maskTop)
        dst3.SetTo(cv.Scalar.White, maskSide)
    End Sub
End Class








Public Class Projection_Top : Inherits VB_Parent
    Public histTop As New Projection_HistTop
    Dim redC As New RedCloud_Basics
    Public objects As New Projection_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        histTop.Run(src)

        redC.inputMask = Not histTop.dst3
        redC.Run(histTop.dst3)

        objects.redCellInput = task.redCells
        objects.dst2 = redC.dst2
        objects.labels(2) = redC.labels(2)
        objects.Run(histTop.dst2)

        dst2 = objects.dst2
        labels(2) = redC.labels(2)
        SetTrueText(objects.strOut, 3)
    End Sub
End Class








Public Class Projection_Side : Inherits VB_Parent
    Public histSide As New Projection_HistSide
    Dim redC As New RedCloud_Basics
    Public objects As New Projection_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        objects.viewType = "Side"
        desc = "Find all the masks, rects, and counts in the side view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        histSide.Run(src)

        redC.inputMask = Not histSide.dst3
        redC.Run(histSide.dst3)

        objects.redCellInput = task.redCells
        objects.dst2 = redC.dst2
        objects.labels(2) = redC.labels(2)
        objects.Run(histSide.dst2)

        dst2 = objects.dst2
        labels(2) = redC.labels(2)
        SetTrueText(objects.strOut, 3)
    End Sub
End Class







Public Class Projection_ObjectIsolate : Inherits VB_Parent
    Public top As New Projection_Top
    Public side As New Projection_Side
    Dim options As New Options_Projection
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        side.objects.showRectangles = False
        desc = "Using the top down view, create a histogram for Y-values of the largest object."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        top.Run(src)
        dst3 = top.dst2
        labels(3) = top.labels(2)

        If options.index < top.objects.objectList.Count Then
            Dim lower = New cv.Scalar(top.objects.objectList(options.index)(0), -100, top.objects.objectList(options.index)(2))
            Dim upper = New cv.Scalar(top.objects.objectList(options.index)(1), +100, top.objects.objectList(options.index)(3))
            dst0 = task.pointCloud.InRange(lower, upper)

            dst1.SetTo(0)
            task.pointCloud.CopyTo(dst1, dst0)
            side.Run(dst1)
            dst2 = side.histSide.dst3
            labels(2) = side.labels(2)
        End If
    End Sub
End Class







Public Class Projection_Object : Inherits VB_Parent
    Dim top As New Projection_Top
    Dim side As New Projection_Side
    Public Sub New()
        task.gOptions.setDebugSlider(0) ' pick the biggest object...
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        top.objects.showRectangles = False
        desc = "Using the top down view, create a histogram for Y-values of the largest object."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        top.Run(src)
        dst3 = top.dst2
        labels(3) = top.labels(2)

        Dim index = task.gOptions.debugSliderValue
        If index < top.objects.objectList.Count Then
            Dim lower = New cv.Scalar(top.objects.objectList(index)(0), -100, top.objects.objectList(index)(2))
            Dim upper = New cv.Scalar(top.objects.objectList(index)(1), +100, top.objects.objectList(index)(3))
            Dim mask = task.pointCloud.InRange(lower, upper)

            Dim rc = top.objects.redCells(task.gOptions.debugSliderValue + 1) ' the biggest by default...
            dst0.SetTo(0)
            dst0(rc.rect) = top.histTop.dst2(rc.rect).Threshold(0, 255, cv.ThresholdTypes.Binary)
            dst0.SetTo(0, dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            dst1.SetTo(0)
            task.pointCloud.CopyTo(dst1, mask)
            side.Run(dst1)
            dst2 = side.dst2
            labels(2) = side.labels(2)
        End If
    End Sub
End Class






Public Class Projection_Floor : Inherits VB_Parent
    Dim isolate As New Projection_ObjectIsolate
    Dim objSlider As TrackBar
    Public Sub New()
        objSlider = FindSlider("Index of object")
        desc = "Isolate just the floor."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        isolate.Run(src)
        dst2 = isolate.dst2
        dst3 = isolate.dst3
        labels(2) = isolate.labels(2)
        labels(3) = isolate.labels(3)

        If objSlider.value + 1 >= isolate.side.objects.redCells.Count Then Exit Sub
        Dim rc = isolate.top.objects.redCells(objSlider.Value + 1) ' the biggest by default...
        Dim rowList As New List(Of Integer)
        For y = 0 To rc.rect.Height - 1
            rowList.Add(dst2(rc.rect).Row(y).CountNonZero() + rc.rect.Y)
        Next

        Dim maxRow = rowList.Max
        Dim ranges = task.rangesSide
        Dim floor = (ranges(0).End - ranges(0).Start) * maxRow / dst2.Height + ranges(0).Start
    End Sub
End Class
