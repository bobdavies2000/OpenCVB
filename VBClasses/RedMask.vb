Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedMask_Basics : Inherits TaskParent
    Implements IDisposable
    Public mdList As New List(Of maskData)
    Public classCount As Integer
    Public Sub New()
        cPtr = RedMask_Open()
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = Mat_Basics.srcMustBe8U(src)

        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim minSize As Integer = dst2.Total * 0.001
        Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, minSize)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

        classCount = RedMask_Count(cPtr)
        If classCount <= 1 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))
        Dim rects(classCount - 1) As cv.Rect
        rectData.GetArray(Of cv.Rect)(rects)

        Dim rectlist = rects.ToList
        mdList.Clear()
        mdList.Add(New maskData) ' add a placeholder for zero...
        For i = 0 To classCount - 1
            Dim md As New maskData
            md.rect = rectlist(i)
            If md.rect.Size = dst2.Size Then Continue For
            md.mask = dst2(md.rect).InRange(i + 1, i + 1)
            md.contour = ContourBuild(md.mask)
            DrawTour(md.mask, md.contour, 255, -1)
            md.pixels = md.mask.CountNonZero
            md.maxDist = Distance_Basics.GetMaxDist(md)
            md.mm = GetMinMax(task.pcSplit(2)(md.rect), task.depthmask(md.rect))
            md.index = mdList.Count
            mdList.Add(md)
        Next

        classCount = mdList.Count

        dst3 = Palettize(dst2)

        labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class




Public Class NR_RedMask_Redraw : Inherits TaskParent
    Public redMask As New RedMask_Basics
    Public Sub New()
        desc = "Redraw the image using the mean color of each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMask.Run(src)

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim color As cv.Scalar
        Dim emptyVec As New cv.Vec3b
        For Each md In redMask.mdList
            Dim c = src.Get(Of cv.Vec3b)(md.maxDist.Y, md.maxDist.X)
            color = New cv.Scalar(c.Item0, c.Item1, c.Item2)
            dst2(md.rect).SetTo(color, md.mask)
        Next
        labels(2) = redMask.labels(2)
    End Sub
End Class






Public Class RedMask_Color : Inherits TaskParent
    Dim cellGen As New RedMask_ToRedColor
    Dim redMask As New RedMask_Basics
    Public rclist As New List(Of rcData)
    Public rcMap As New cv.Mat ' redColor map 
    Dim contours As New Contour_Basics
    Public inputRemoved As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        If src.Type <> cv.MatType.CV_8U Then
            If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics_TA" Then
                dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Else
                dst1 = Mat_Basics.srcMustBe8U(src)
            End If
        Else
            dst1 = src
        End If

        If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
        redMask.Run(dst1 + 1)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        rclist.Clear()
        For Each md In redMask.mdList
            Dim rc = New rcData(md.mask, md.rect, rclist.Count + 1)
            rc.buildMaxDist()
            rclist.Add(rc)
        Next

        dst2 = redMask.dst2
        dst3 = Palettize(dst2)
        labels(2) = redMask.labels(2)
        labels(3) = CStr(rclist.Count) + " loosely defined cells found"

        dst2.ConvertTo(rcMap, cv.MatType.CV_32S)
        strOut = RedUtil_Basics.selectCell(rcMap, rclist)
        SetTrueText(strOut, 1)
    End Sub
End Class





Public Class NR_RedMask_CellDepthHistogram : Inherits TaskParent
    Dim plot As New PlotBar_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
        If task.rcD Is Nothing Then
            SetTrueText("Select any cell", 1)
            Exit Sub
        End If
        If task.heartBeat Then
            Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                vbc.DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
        End If
        dst3 = plot.dst2
    End Sub
End Class




Public Class RedMask_CPP : Inherits TaskParent
    Implements IDisposable
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public identifyCount As Integer = 255
    Public Sub New()
        cPtr = RedMask_Open()
        desc = "Run the C++ RedCloud Interface With Or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = Mat_Basics.srcMustBe8U(src)

        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, 0)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

        classCount = Math.Min(RedMask_Count(cPtr), identifyCount * 2)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))
        Dim rects(classCount - 1) As cv.Rect
        rectData.GetArray(Of cv.Rect)(rects)

        rectList = rects.ToList
        If standaloneTest() Then dst3 = Palettize(dst2)

        labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
        labels(3) = "Palette version of the data In dst2 With " + CStr(classCount) + " regions."
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class





Public Class NR_RedMark_Features : Inherits TaskParent
    Dim options As New Options_RedCloudFeatures
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Display And validate the keyPoints for each RedCloud cell"
    End Sub
    Private Function vbNearFar(factor As Single) As cv.Vec3b
        Dim nearYellow As New cv.Vec3b(255, 0, 0)
        Dim farBlue As New cv.Vec3b(0, 255, 255)
        If Single.IsNaN(factor) Then Return New cv.Vec3b
        If factor > 1 Then factor = 1
        If factor < 0 Then factor = 0
        Return New cv.Vec3b(((1 - factor) * farBlue(0) + factor * nearYellow(0)),
                                ((1 - factor) * farBlue(1) + factor * nearYellow(1)),
                                ((1 - factor) * farBlue(2) + factor * nearYellow(2)))
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = runRedList(src, labels(2))
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
        If task.rcD Is Nothing Then
            SetTrueText("Select any cell", 1)
            Exit Sub
        End If

        Dim rc = task.rcD

        dst0 = task.color
        Dim correlationMat As New cv.Mat, correlationXtoZ As Single, correlationYtoZ As Single
        dst3.SetTo(0)
        Select Case options.selection
            Case 0
                Dim pt = rc.maxDist
                dst2.Circle(pt, task.DotSize, task.highlight, -1, cv.LineTypes.AntiAlias)
                labels(3) = "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
            Case 1
                dst3(rc.rect).SetTo(vbNearFar((rc.wcMean(2)) / task.MaxZmeters), rc.mask)
                labels(3) = "rc.wcMean(2) Is highlighted in dst2"
                labels(3) = "Mean depth for the cell Is " + Format(rc.wcMean(2), fmt3)
            Case 2
                cv.Cv2.MatchTemplate(task.pcSplit(0)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationXtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation X to Z Is yellow, low correlation X to Z Is blue"
            Case 3
                cv.Cv2.MatchTemplate(task.pcSplit(1)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationYtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue"
        End Select
        If options.selection = 2 Or options.selection = 3 Then
            dst3(rc.rect).SetTo(vbNearFar(If(options.selection = 2, correlationXtoZ, correlationYtoZ) + 1), rc.mask)
            SetTrueText("(" + Format(correlationXtoZ, fmt3) + ", " + Format(correlationYtoZ, fmt3) + ")",
                                rc.rect.TopLeft, 3)
        End If
        DrawTour(dst0(rc.rect), rc.contour, cv.Scalar.Yellow)
        SetTrueText(labels(3), 3)
        labels(2) = "Highlighted feature = " + options.labelName
    End Sub
End Class




Public Class NR_RedMask_Consistent : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.fOptions.ColorDiffSlider.Value = 1
        desc = "Remove RedColor results that are inconsistent with the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim count As Integer
        For Each rc In redC.rcList
            If rc.age > 1 Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " cells matched the previous generation."
    End Sub
End Class



Public Class NR_RedMask_Hue : Inherits TaskParent
    Dim hue As New Color8U_Hue
    Dim redMask As New RedMask_Color
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Mask of the areas with Hue"
        desc = "Run RedCloud on just the red hue regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hue.Run(src)
        dst3 = hue.dst2

        redMask.inputRemoved = Not dst3
        redMask.Run(src)
        dst2 = redMask.dst3
        labels(2) = redMask.labels(2)

        SetTrueText(redMask.strOut, 1)
    End Sub
End Class





Public Class NR_RedMask_FourColor : Inherits TaskParent
    Dim binar4 As New Bin4Way_Regions
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst3 = Palettize(binar4.dst2)

        redC.Run(binar4.dst2)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
    End Sub
End Class





Public Class RedMask_ToRedColor : Inherits TaskParent
    Public redMask As New RedMask_Basics
    Public mdList As New List(Of maskData)
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public Sub New()
        desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMask.Run(src)
        dst2 = redMask.dst3
        labels(2) = redMask.labels(3)

        Dim rcMapLast = rcMap.Clone
        Dim rcListLast As New List(Of rcData)(rcList)
        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        rcMap.SetTo(0)
        For i = 0 To redMask.mdList.Count - 1
            Dim rc = New rcData(redMask.mdList(i).mask, redMask.mdList(i).rect, sortedCells.Count)
            rc.mask = redMask.mdList(i).mask
            If rc.rect.Size = dst2.Size Then Continue For ' RedMask_List can find a cell this big.  
            ' DrawTour(rc.mask, rc.contour, 255, -1)
            rc.pixels = redMask.mdList(i).mask.CountNonZero
            rc.age = 1
            rc.index = sortedCells.Count + 1
            sortedCells.Add(rc.pixels, rc)

            rcMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        rcList = New List(Of rcData)(sortedCells.Values)
        labels(2) = CStr(rcList.Count) + " total cells "

        SetTrueText(redMask.strOut, 3)
    End Sub
End Class






Public Class RedMask_List : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public cellGen As New RedMask_ToRedColor
    Public redMask As New RedMask_Basics
    Public contours As New Contour_Basics
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rclist As New List(Of rcData)
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Shared Function DisplayCells(mdList As List(Of maskData)) As cv.Mat
        Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)

        For Each md In mdList
            dst(md.rect).SetTo(task.scalarColors(md.index Mod 255), md.mask)
        Next

        Return dst
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        If src.Type <> cv.MatType.CV_8U Then
            If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics_TA" Then
                dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Else
                dst1 = Mat_Basics.srcMustBe8U(src)
            End If
        Else
            dst1 = src
        End If

        If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
        redMask.Run(dst1)
        dst2 = redMask.dst3
        labels(2) = redMask.labels(3)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        rcMap.SetTo(0)
        rclist.Clear()
        For Each md In redMask.mdList
            Dim rc = New rcData(md.mask, md.rect, md.index)
            rc.index = rclist.Count + 1
            rclist.Add(rc)
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        strOut = RedUtil_Basics.selectCell(rcMap, rclist)
        SetTrueText(strOut, 1)
    End Sub
End Class
