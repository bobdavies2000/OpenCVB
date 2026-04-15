Imports System.Runtime.InteropServices
Imports VBClasses
Imports cv = OpenCvSharp
Public Class RedMask_Basics : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim redMask As New RedMask_MapAndList
    Dim fLess As New FeatureLess_BasicsRaw
    Dim knn As New KNN_N3Basics
    Public Sub New()
        knn.queries.Add(New cv.Point3f(0, 0, 0)) ' we only need one entry in the queries.
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use KNN to identify the previous cell for each current cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst3
        labels(2) = fLess.labels(2)

        redMask.Run(dst2)

        Dim rcListLast As New List(Of rcData)(rcList)
        Dim lastColorMat = dst3.Clone

        knn.trainInput.Clear()
        For Each rc In redMask.rcList
            knn.trainInput.Add(New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels))
        Next

        dst3.SetTo(0)
        For Each rc In redMask.rcList
            knn.queries(0) = New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels)
            knn.Run(emptyMat)
            Dim lastIndex = knn.result(0, 0)
            If rcListLast.Count > 0 And lastIndex < rcListLast.Count Then
                Dim rcLast = rcListLast(lastIndex)
                rc.indexLast = rcLast.index
                rc.maxDStable = rcLast.maxDist
            End If

            Dim lastColor = lastColorMat.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If lastColor <> black Then
                If lastColor <> task.vecColors(rc.index) Then rc.color = lastColor
            Else
                rc.color = task.scalarColors(rc.index)
            End If
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        strOut = RedUtil_Basics.selectCell(redMask.dst2, redMask.rcList)
        SetTrueText(strOut, 1)
        If task.rcD IsNot Nothing Then task.clickPoint = task.rcD.maxDist

        Dim usedColors As New List(Of cv.Scalar)
        rcList.Clear()
        For Each rc In redMask.rcList
            If rc.indexLast > 0 Then
                Dim rcLast = rcListLast(rc.indexLast - 1)
                Dim gridIndex = rc.gridIndex
                Dim lastGridIndex = rcLast.gridIndex
                If task.gridNabes(gridIndex).Contains(lastGridIndex) Then
                    rc.age = rcLast.age + 1
                    If rc.age > 1000 Then rc.age = 2
                End If
            End If

            If usedColors.Contains(rc.color) Then
                rc.color = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            End If
            usedColors.Add(rc.color)

            rcList.Add(rc)
        Next

        rcMap = redMask.dst2.Clone

        labels(3) = CStr(redMask.rcList.Count) + " cells were identified."
    End Sub
End Class





Public Class NR_RedMask_Basics : Inherits TaskParent
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
    Public redMask As New NR_RedMask_Basics
    Public Sub New()
        desc = "Redraw the image using the mean color of each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMask.Run(src)

        src = task.gray
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
    Dim redMask As New NR_RedMask_Basics
    Public rclist As New List(Of rcData)
    Public rcMap As New cv.Mat ' redColor map 
    Dim contours As New Contour_Basics
    Public inputRemoved As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then contours.Run(src)
        If src.Type <> cv.MatType.CV_8U Then
            If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
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
    Public redMask As New NR_RedMask_Basics
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
    Public redMask As New NR_RedMask_Basics
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
            If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
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





Public Class RedMask_Test : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim redCore As New RedMask_CPP
    Dim fLess As New FeatureLess_BasicsRaw
    Public fLessGridRects As New List(Of List(Of Integer))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use KNN to identify the previous cell for each current cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst3
        labels(2) = fLess.labels(2)

        redCore.Run(dst2)
        Dim classcount = redCore.classCount

        Dim fLessNew As New List(Of List(Of Integer))
        For i = 0 To classcount
            fLessNew.Add(New List(Of Integer))
        Next

        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If dst2.Get(Of Byte)(r.Y, r.X) = 255 Then
                Dim index = redCore.dst2.Get(Of Byte)(r.Y, r.X)
                If index > 0 Then fLessNew(index).Add(i)
            End If
        Next

        Dim SortList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To fLessNew.Count - 1
            SortList.Add(fLessNew(i).Count, i)
        Next

        fLessGridRects.Clear()
        For Each index In SortList.Values
            If fLessNew(index).Count > 0 Then fLessGridRects.Add(fLessNew(index))
        Next

        dst1.SetTo(0)
        For i = 0 To fLessGridRects.Count - 1
            For j = 0 To fLessGridRects(i).Count - 1
                dst1(task.gridRects(fLessGridRects(i)(j))).SetTo(i + 1)
            Next
        Next

        dst3 = Palettize(dst1, 0)
        labels(3) = CStr(fLessGridRects.Count) + " cells were found."
    End Sub
End Class






Public Class NR_RedMask_KNN : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim redCore As New RedMask_CPP
    Dim fLess As New FeatureLess_BasicsRaw
    Dim knn As New KNN_N3Basics
    Public fLessGridRects As New List(Of List(Of Integer))
    Public Sub New()
        knn.queries.Add(New cv.Point3f)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use KNN to identify the previous cell for each current cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst3
        labels(2) = fLess.labels(2)

        redCore.Run(dst2)
        Dim classcount = redCore.classCount

        Dim rcListLast As New List(Of rcData)(rcList)

        fLessGridRects.Clear()
        For i = 0 To classcount
            fLessGridRects.Add(New List(Of Integer))
        Next
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim index = redCore.dst2.Get(Of Byte)(r.Y, r.X)
            If index > 0 Then fLessGridRects(index).Add(i)
        Next

        knn.trainInput.Clear()
        For Each rc In rcList
            knn.trainInput.Add(New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels))
        Next

        dst3.SetTo(0)
        rcMap.SetTo(0)
        rcList.Clear()
        For i = 0 To classcount - 1
            Dim r = redCore.rects(i)
            Dim mask255 = redCore.dst2(r).InRange(i + 1, i + 1)
            Dim mask As New cv.Mat(mask255.Size, cv.MatType.CV_8U, 0)
            redCore.dst2(r).CopyTo(mask, mask255)
            Dim rc As New rcData(mask, r, i + 1)
            rc.color = task.scalarColors((rcList.Count + 1) Mod 255)

            knn.queries(0) = New cv.Point3f(rc.maxDist.X, rc.maxDist.Y, rc.pixels)
            knn.Run(emptyMat)
            If knn.trainInput.Count > 0 Then
                Dim index = knn.result(0, 0)
                If rcListLast.Count > 0 Then
                    Dim rcLast = rcListLast(index)
                    If rcLast.rect.IntersectsWith(task.gridRects(rc.gridIndex)) Then
                        Dim gridList = task.gridNabes(rcLast.gridIndex)
                        rc.color = rcLast.color
                        rc.age = rcLast.age + 1
                        If rc.age > 1000 Then rc.age = 2
                    End If
                End If
            End If

            rc.index = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            rcList.Add(rc)
        Next

        strOut = RedUtil_Basics.selectCell(rcMap, rcList)
        SetTrueText(strOut, 1)
        If task.rcD IsNot Nothing Then task.clickPoint = task.rcD.maxDist

        For Each rc In rcList
            dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            SetTrueText(CStr(rc.index) + ", " + CStr(rc.age), rc.maxDist, 3)
        Next

        labels(3) = "Palette version of the data in dst2 with " + CStr(classcount) + " regions."
    End Sub
End Class






Public Class RedMask_CPP : Inherits TaskParent
    Implements IDisposable
    Public classCount As Integer
    Public rects() As cv.Rect
    Public Sub New()
        cPtr = RedMask_Open()
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = Mat_Basics.srcMustBe8U(src) Else dst1 = src

        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim minSize As Integer = dst2.Total * 0.001
        Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, minSize)
        handleInput.Free()

        dst2 = cv.Mat.FromPixelData(dst0.Rows + 2, dst0.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

        classCount = RedMask_Count(cPtr)
        If classCount <= 1 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr)).Clone
        ReDim rects(classCount - 1)
        rectData.GetArray(Of cv.Rect)(rects)

        If standaloneTest() Then dst3 = Palettize(dst2)

        labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class




Public Class RedMask_MapAndList : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Dim redCore As New RedMask_CPP
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = Mat_Basics.srcMustBe8U(src) Else dst1 = src

        redCore.Run(dst1)
        Dim classcount = redCore.classCount
        If classcount <= 1 Then Exit Sub ' no data to process.

        dst2.SetTo(0)
        rcList.Clear()
        For i = 0 To classcount - 1
            Dim rc As New rcData
            rc.rect = redCore.rects(i)
            rc.mask = redCore.dst2(rc.rect).InRange(i + 1, i + 1)
            rc = New rcData(rc.mask, rc.rect, -1)
            rc.index = rcList.Count + 1

            rc.contour = ContourBuild(rc.mask)
            Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
            rc.mask = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
            cv.Cv2.DrawContours(rc.mask, listOfPoints, 0, cv.Scalar.All(rc.index), -1, cv.LineTypes.Link4)

            rc.color = task.scalarColors(rc.index Mod 255)
            dst2(rc.rect).SetTo(rc.index, rc.mask)
            rcList.Add(rc)
        Next

        If standaloneTest() Then dst3 = Palettize(dst2, 0)

        labels(2) = "CV_8U result with " + CStr(classcount) + " regions."
        labels(3) = "Palette version of the data in dst2 with " + CStr(classcount) + " regions."
    End Sub
End Class





Public Class RedMask_Delaunay : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Dim redMask As New RedMask_Basics
    Dim facetList As New List(Of List(Of cv.Point))
    Dim rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "The colors below match the color of the corresponding featureless region in dst2."
        desc = "Fill the delaunay map with the index for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redMask.Run(src)
        dst2 = redMask.dst3
        labels(2) = redMask.labels(3)

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))

        Dim inputPoints As New List(Of cv.Point2f)
        For Each rc In redMask.rcList
            inputPoints.Add(rc.maxDist)
        Next
        subdiv.Insert(inputPoints)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            Dim rc = redMask.rcList(i)
            rcMap.FillConvexPoly(nextFacet, rc.index, cv.LineTypes.Link4)
            If standaloneTest() Then dst3.FillConvexPoly(nextFacet, rc.color, cv.LineTypes.Link4)
            facetList.Add(nextFacet)
        Next

        'For Each rc In redMask.rcList
        '    DrawTour(dst3(rc.rect), rc.contour, task.highlight, task.lineWidth)
        'Next
        strOut = RedUtil_Basics.DelaunaySelect(rcMap, redMask.rcList)
        SetTrueText(strOut, 1)
    End Sub
End Class