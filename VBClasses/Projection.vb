Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Projection_Basics : Inherits TaskParent
    Public redCellInput As New List(Of rcDataOld)
    Public rclist As New List(Of rcDataOld)
    Public viewType As String = "Top"
    Public objectList As New List(Of cv.Vec4f)
    Public showRectangles As Boolean = True
    Dim histTop As New Projection_HistTop
    Public redC As New RedColor_Basics
    Public Sub New()
        desc = "Find all the masks, rects, and counts in the input"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            histTop.Run(src)
            src = histTop.dst2

            ' redC.inputRemoved = Not histTop.dst3
            redC.Run(histTop.dst3)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            redCellInput = redC.rcList
        End If

        Dim sortedCells As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
        Dim check2 As Integer
        For i = 0 To redCellInput.Count - 1
            Dim rc = redCellInput(i)
            Dim tmp = New cv.Mat(rc.rect.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            src(rc.rect).CopyTo(tmp, rc.mask)
            rc.pixels = CInt(cv.Cv2.Sum(tmp)(0))
            sortedCells.Add(rc.pixels, rc)
            check2 += rc.pixels
        Next

        rclist.Clear()
        rclist.Add(New rcDataOld)
        For Each rc In sortedCells.Values
            rc.mapID = rclist.Count
            rclist.Add(rc)
        Next

        Dim meterDesc = "tall"
        Dim ranges = task.rangesSide
        If viewType = "Top" Then
            meterDesc = "wide"
            ranges = task.rangesTop
        End If
        objectList.Clear()
        Dim xy1 As Single, xy2 As Single, z1 As Single, z2 As Single
        If task.heartBeat Then strOut = ""
        For Each rc In rclist
            If rc.mapID = 0 Then Continue For
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
                strOut += "Object " + vbTab + CStr(rc.mapID) + vbTab + (xy2 - xy1).ToString(fmt3) + " m " + meterDesc + vbTab +
                                           z1.ToString(fmt1) + "m " + " to " + z2.ToString(fmt1) + "m from camera" + vbTab + CStr(rc.pixels) + " pixels" + vbCrLf
            End If
        Next

        If task.heartBeat Then
            Dim check1 = cv.Cv2.Sum(src)(0)
            Dim depthCount = cv.Cv2.CountNonZero(task.pcSplit(2))
            strOut += "Sum above   " + vbTab + CStr(check2) + " pixels" + " (losses from histogram ranges?)" + vbCrLf
            strOut += "Sum of src  " + vbTab + CStr(check1) + " pixels" + " (losses from RedCloud.)" + vbCrLf
            strOut += "Actual count" + vbTab + CStr(depthCount) + " pixels" + vbCrLf
        End If
        SetTrueText(strOut, 3)
        If showRectangles Then
            For i = 0 To rclist.Count - 1
            cv.Cv2.Rectangle(dst2, rclist(i).rect, task.highlight, task.lineWidth)
            Next
        End If
        labels(2) = CStr(rclist.Count) + " objects were found in the " + viewType + " view."
    End Sub
End Class









Public Class XR_Projection_Lines : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim options As New Options_Projection
    Public Sub New()
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "Lines found in the threshold output", "FeatureLess cells found", "Projections of each of the FeatureLess cells"}
        desc = "Search for surfaces among the FeatureLess regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.heartBeat Then
            dst1.SetTo(0)
            dst3.SetTo(0)
        End If
        heat.Run(src)
        If options.topCheck Then dst2 = heat.dst2 Else dst2 = heat.dst3
        cv.Cv2.Threshold(dst2, dst1, options.projectionThreshold, 255, cv.ThresholdTypes.Binary)

    End Sub
End Class








Public Class Projection_ObjectIsolate : Inherits TaskParent
    Public top As New Projection_ViewTop
    Public side As New Projection_ViewSide
    Dim options As New Options_Projection
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        side.objects.showRectangles = False
        desc = "Using the top down view, create a histogram for Y-values of the largest object."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        top.Run(src)
        dst3 = top.dst2
        labels(3) = top.labels(2)

        If options.index < top.objects.objectList.Count Then
            Dim lower = New cv.Scalar(top.objects.objectList(options.index)(0), -100, top.objects.objectList(options.index)(2))
            Dim upper = New cv.Scalar(top.objects.objectList(options.index)(1), +100, top.objects.objectList(options.index)(3))
                          cv.Cv2.InRange(task.pointCloud, lower, upper, dst0)

            dst1.SetTo(0)
            task.pointCloud.CopyTo(dst1, dst0)
            side.Run(dst1)
            dst2 = side.histSide.dst3
            labels(2) = side.labels(2)
        End If
    End Sub
End Class







Public Class Projection_Object : Inherits TaskParent
    Dim top As New Projection_ViewTop
    Dim side As New Projection_ViewSide
    Public Sub New()
        task.gOptions.DebugSlider.Value = 0 ' pick the biggest object...
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        top.objects.showRectangles = False
        desc = "Using the top down view, create a histogram for Y-values of the largest object."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        top.Run(src)
        dst3 = top.dst2
        labels(3) = top.labels(2)

        Dim index = task.gOptions.DebugSlider.Value
        If index < top.objects.objectList.Count Then
            Dim lower = New cv.Scalar(top.objects.objectList(index)(0), -100, top.objects.objectList(index)(2))
            Dim upper = New cv.Scalar(top.objects.objectList(index)(1), +100, top.objects.objectList(index)(3))
            Dim mask As New cv.Mat
            cv.Cv2.InRange(task.pointCloud, lower, upper, mask)

            Dim rc = top.objects.rclist(task.gOptions.DebugSlider.Value + 1) ' the biggest by default...
            dst0.SetTo(0)
            cv.Cv2.Threshold(top.histTop.dst2(rc.rect), dst0(rc.rect), 0, 255, cv.ThresholdTypes.Binary)
            Dim _cvtInline As New cv.Mat
            cv.Cv2.CvtColor(dst3, _cvtInline, cv.ColorConversionCodes.BGR2GRAY)
            dst0.SetTo(0,_cvtInline)

            dst1.SetTo(0)
            task.pointCloud.CopyTo(dst1, mask)
            side.Run(dst1)
            dst2 = side.dst2
            labels(2) = side.labels(2)
        End If
    End Sub
End Class






Public Class XR_Projection_Floor : Inherits TaskParent
    Dim isolate As New Projection_ObjectIsolate
    Dim objSlider As TrackBar
    Public Sub New()
        objSlider = OptionParent.FindSlider("Index of object")
        desc = "Isolate just the floor."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        isolate.Run(src)
        dst2 = isolate.dst2
        dst3 = isolate.dst3
        labels(2) = isolate.labels(2)
        labels(3) = isolate.labels(3)

        If objSlider.Value + 1 > isolate.side.objects.rclist.Count Then Exit Sub
        Dim rc = isolate.top.objects.rclist(objSlider.Value + 1) ' the biggest by default...
        Dim rowList As New List(Of Integer)
        For y = 0 To rc.rect.Height - 1
        rowList.Add(cv.Cv2.CountNonZero(dst2(rc.rect).Row(y)) + rc.rect.Y)
        Next

        Dim maxRow = rowList.Max
        Dim ranges = task.rangesSide
        Dim floor = (ranges(0).End - ranges(0).Start) * maxRow / dst2.Height + ranges(0).Start
    End Sub
End Class











Public Class Projection_Cell : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim heatCell As New HeatMap_Basics
    Dim redC As New RedColor_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32FC3, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Top View projection of the selected cell", "RedColor_Basics output - select a cell to project at right and above", "Side projection of the selected cell"}
        desc = "Create a top and side projection of the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        heat.Run(src)
        dst1 = heat.dst2.Clone
        dst3 = heat.dst3.Clone

        If redC.rcList.Count > 0 Then
            Dim rc = redC.rcList(0) ' just pick the biggest cell for now...
            dst0.SetTo(0)
            task.pointCloud(rc.rect).CopyTo(dst0(rc.rect), rc.mask)
        End If
        heatCell.Run(dst0)
        Dim maskTop As New cv.Mat
        cv.Cv2.CvtColor(heatCell.dst2, maskTop, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Threshold(maskTop, maskTop, 0, 255, cv.ThresholdTypes.Binary)
        Dim maskSide As New cv.Mat
        cv.Cv2.CvtColor(heatCell.dst3, maskSide, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Threshold(maskSide, maskSide, 0, 255, cv.ThresholdTypes.Binary)
        dst1.SetTo(white, maskTop)
        dst3.SetTo(white, maskSide)
    End Sub
End Class







Public Class XR_Projection_Derivative : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim heatDeriv As New HeatMap_Basics
    Dim deriv As New Derivative_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create a top and side projection the best derivative data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        deriv.Run(src)
        dst1 = deriv.dst3
        labels(1) = deriv.labels(3)

        heat.Run(task.pointCloud)
        dst2 = heat.dst2
        dst3 = heat.dst3
        labels(2) = heat.labels(2)
        labels(3) = heat.labels(3)

        Dim pc As New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst1)

        heatDeriv.Run(pc)
        Dim top As New cv.Mat
        cv.Cv2.CvtColor(heatDeriv.dst2, top, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Threshold(top, top, 0, 255, cv.ThresholdTypes.Binary)
        Dim side As New cv.Mat
        cv.Cv2.CvtColor(heatDeriv.dst3, side, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Threshold(side, side, 0, 255, cv.ThresholdTypes.Binary)
        dst2.SetTo(cv.Scalar.White, top)
        dst3.SetTo(cv.Scalar.White, side)
    End Sub
End Class









Public Class Projection_ViewTop : Inherits TaskParent
    Public histTop As New Projection_HistTop
    Public objects As New Projection_Basics
    Public Sub New()
        desc = "Find all the masks, rects, and counts in the top down view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)

        objects.redC.Run(histTop.dst3)

        objects.redCellInput = objects.redC.rcList
        objects.dst2 = objects.redC.dst2
        objects.labels(2) = objects.redC.labels(2)
        objects.Run(histTop.dst2)

        dst2 = objects.dst2
        labels(2) = objects.redC.labels(2)
        SetTrueText(objects.strOut, 3)
    End Sub
End Class








Public Class Projection_ViewSide : Inherits TaskParent
    Public histSide As New Projection_HistSide
    Public objects As New Projection_Basics
    Public Sub New()
        objects.viewType = "Side"
        desc = "Find all the masks, rects, and counts in the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)

        objects.redC.Run(histSide.dst3)

        objects.redCellInput = objects.redC.rcList
        objects.dst2 = objects.redC.dst2
        objects.labels(2) = objects.redC.labels(2)
        objects.Run(histSide.dst2)

        dst2 = objects.dst2
        labels(2) = objects.redC.labels(2)
        SetTrueText(objects.strOut, 3)
    End Sub
End Class







Public Class Projection_HistSide : Inherits TaskParent
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Top view with histogram counts", "ZY (Side View) - mask"}
        desc = "Create a 2D side view for ZY histogram of depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
        histogram.Col(0).SetTo(0)

        cv.Cv2.ConvertScaleAbs(histogram, dst2)
        Dim _thr1 As New cv.Mat
        cv.Cv2.Threshold(histogram, dst3, task.projectionThreshold, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.ConvertScaleAbs(dst3, dst3)
    End Sub
End Class






Public Class Projection_HistTop : Inherits TaskParent
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Top view with histogram counts", "XZ (Top View) - mask"}
        desc = "Create a 2D top view for XZ histogram of depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
        histogram.Row(0).SetTo(0)

        cv.Cv2.ConvertScaleAbs(histogram, dst2)
        cv.Cv2.Threshold(histogram, dst3, task.projectionThreshold, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.ConvertScaleAbs(dst3, dst3)
    End Sub
End Class
