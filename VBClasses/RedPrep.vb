Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedPrep_Basics : Inherits TaskParent
        Dim prepEdges As New RedPrep_Edges_CPP
        Public options As New Options_RedCloud
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Reduction transform for the point cloud"
        End Sub
        Private Function reduceChan(chan As cv.Mat, noDepthmask As cv.Mat) As cv.Mat
            chan *= task.reductionTarget
            Dim mm As mmData = GetMinMax(chan)
            Dim dst32f As New cv.Mat
            If Math.Abs(mm.minVal) > mm.maxVal Then
                mm.minVal = -mm.maxVal
                chan.ConvertTo(dst32f, cv.MatType.CV_32F)
                Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
                mask.ConvertTo(mask, cv.MatType.CV_8U)
                dst32f.SetTo(mm.minVal, mask)
            End If
            chan = (chan - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            chan.ConvertTo(chan, cv.MatType.CV_8U)
            chan.SetTo(0, noDepthmask)
            Return chan
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone

            Dim pc32S As New cv.Mat
            src.ConvertTo(pc32S, cv.MatType.CV_32SC3, 1000 / task.reductionTarget)
            Dim split = pc32S.Split()

            dst2.SetTo(0)
            Dim saveNoDepth As cv.Mat = Nothing
            If src.Size <> task.workRes Then
                saveNoDepth = task.noDepthMask.Clone
                task.noDepthMask = task.noDepthMask.Resize(src.Size)
            End If
            If options.PrepX Then
                prepEdges.Run(reduceChan(split(0), task.noDepthMask))
                If dst2.Size <> src.Size Then
                    dst2 = dst2.Resize(src.Size)
                    dst2 = dst2 Or prepEdges.dst3
                Else
                    dst2 = dst2 Or prepEdges.dst3
                End If
            End If

            If options.PrepY Then
                prepEdges.Run(reduceChan(split(1), task.noDepthMask))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepZ Then
                prepEdges.Run(reduceChan(split(2), task.noDepthMask))
                dst2 = dst2 Or prepEdges.dst3
            End If

            ' this is not as good as the operations above.
            'prepEdges.Run(reduceChan(split(0) + split(1) + split(2)))
            'dst2 = prepEdges.dst3

            ' this rectangle prevents bleeds at the image edges.  It is necessary.  Test without it to see the impact.
            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width, dst2.Height), 255, 2)

            If src.Size <> task.workRes Then task.noDepthMask = saveNoDepth.Clone
            labels(2) = "Using reduction factor = " + CStr(task.reductionTarget)
        End Sub
    End Class







    Public Class RedPrep_Depth : Inherits TaskParent
        Dim options As New Options_HistPointCloud
        Public Sub New()
            cPtr = PrepXY_Open()
            desc = "Run the C++ PrepXY to create a list of mask, rect, and other info about image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim inputX(task.pcSplit(0).Total * task.pcSplit(0).ElemSize - 1) As Byte
            Dim inputY(task.pcSplit(1).Total * task.pcSplit(1).ElemSize - 1) As Byte

            Marshal.Copy(task.pcSplit(0).Data, inputX, 0, inputX.Length)
            Marshal.Copy(task.pcSplit(1).Data, inputY, 0, inputY.Length)

            Dim handleX = GCHandle.Alloc(inputX, GCHandleType.Pinned)
            Dim handleY = GCHandle.Alloc(inputY, GCHandleType.Pinned)

            Dim imagePtr = PrepXY_Run(cPtr, handleX.AddrOfPinnedObject(), handleY.AddrOfPinnedObject(), src.Rows, src.Cols,
                                  task.xRange, task.yRange, task.histogramBins)
            handleX.Free()
            handleY.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst2.SetTo(0, task.noDepthMask)

            dst3 = PaletteBlackZero(dst2)
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = PrepXY_Close(cPtr)
        End Sub
    End Class








    Public Class RedPrep_VB : Inherits TaskParent
        Public Sub New()
            desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram As New cv.Mat

            Dim ranges As cv.Rangef() = Nothing, zeroCount As Integer
            For i = 0 To 1
                Select Case i
                    Case 0 ' X Reduction
                        dst1 = task.pcSplit(0)
                        ranges = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange)}
                    Case 1 ' Y Reduction
                        dst1 = task.pcSplit(1)
                        ranges = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange)}
                End Select

                cv.Cv2.CalcHist({dst1}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                For j = 0 To histArray.Count - 1
                    If histArray(j) = 0 Then zeroCount += 1
                    histArray(j) = j
                Next

                histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
                cv.Cv2.CalcBackProject({dst1}, {0}, histogram, dst1, ranges)

                If i = 0 Then dst3 = dst1.Clone Else dst3 += dst1
            Next

            dst3.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2.SetTo(0, task.noDepthMask)

            labels(2) = CStr(task.histogramBins * 2 - zeroCount) + " depth regions mapped (control with histogram bins.)"
        End Sub
    End Class






    Public Class RedPrep_DepthEdges : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Find the edges of XY depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst3 = prep.dst3

            edges.Run(dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = edges.dst2
            labels(2) = edges.labels(2)
        End Sub
    End Class






    Public Class RedPrep_DepthTiers : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Dim tiers As New Depth_Tiers
        Public Sub New()
            labels(3) = "RedPrep_Depth output define regions with common XY."
            desc = "Find the edges of XY depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst3 = prep.dst3
            dst1 = prep.dst2

            tiers.Run(src)
            dst1 += tiers.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            dst2 = PaletteFull(dst1)
        End Sub
    End Class




    Public Class RedPrep_ReductionChoices : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public options As New Options_RedCloud
        Public options1 As New Options_HistPointCloud
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Reduction transform for the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            options1.Run()

            Dim split() = {New cv.Mat, New cv.Mat, New cv.Mat}
            Dim reduceAmt = task.reductionTarget
            task.pcSplit(0).ConvertTo(split(0), cv.MatType.CV_32S, 1000 / reduceAmt)
            task.pcSplit(1).ConvertTo(split(1), cv.MatType.CV_32S, 1000 / reduceAmt)
            task.pcSplit(2).ConvertTo(split(2), cv.MatType.CV_32S, 1000 / reduceAmt)

            Select Case task.reductionName
                Case "X Reduction"
                    dst0 = split(0) * reduceAmt
                Case "Y Reduction"
                    dst0 = split(1) * reduceAmt
                Case "Z Reduction"
                    dst0 = split(2) * reduceAmt
                Case "XY Reduction"
                    dst0 = (split(0) + split(1)) * reduceAmt
                Case "XZ Reduction"
                    dst0 = (split(0) + split(2)) * reduceAmt
                Case "YZ Reduction"
                    dst0 = (split(1) + split(2)) * reduceAmt
                Case "XYZ Reduction"
                    dst0 = (split(0) + split(1) + split(2)) * reduceAmt
            End Select

            Dim mm As mmData = GetMinMax(dst0)
            Dim dst32f As New cv.Mat
            If Math.Abs(mm.minVal) > mm.maxVal Then
                mm.minVal = -mm.maxVal
                dst0.ConvertTo(dst32f, cv.MatType.CV_32F)
                Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
                mask.ConvertTo(mask, cv.MatType.CV_8U)
                dst32f.SetTo(mm.minVal, mask)
            End If
            dst2 = (dst0 - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)

            dst2.SetTo(0, task.noDepthMask)

            If standaloneTest() Then
                mm = GetMinMax(dst2)
                plot.createHistogram = True
                plot.removeZeroEntry = False
                plot.maxRange = mm.maxVal
                plot.Run(dst2)
                dst1 = plot.dst2

                For i = 0 To plot.histArray.Count - 1
                    plot.histArray(i) = i
                Next
            End If
            dst3 = PaletteBlackZero(dst2)

            labels(2) = "Using reduction factor = " + CStr(reduceAmt)
        End Sub
    End Class





    Public Class RedPrep_EdgeMask : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Get the edges in the RedPrep_ReductionChoices output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2
            labels(2) = prep.labels(2)

            dst3.SetTo(0)
            For y = 1 To dst2.Height - 2
                For x = 1 To dst2.Width - 2
                    Dim pix1 = dst2.Get(Of Byte)(y, x)
                    Dim pix2 = dst2.Get(Of Byte)(y, x + 1)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)

                    pix2 = dst2.Get(Of Byte)(y + 1, x)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)

                    pix2 = dst2.Get(Of Byte)(y + 1, x + 1)
                    If pix1 <> 0 And pix2 <> 0 And pix1 <> pix2 Then dst3.Set(Of Byte)(y, x, 255)
                Next
            Next

            dst2.SetTo(0, dst3)
        End Sub
    End Class







    Public Class RedPrep_Edges_CPP : Inherits TaskParent
        Public Sub New()
            cPtr = RedPrep_CPP_Open()
            desc = "Isolate each depth region"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static prep As New RedPrep_ReductionChoices
                prep.Run(src)
                dst2 = prep.dst2
                labels(2) = prep.labels(2)
            Else
                dst2 = src
            End If

            Dim cppData(dst2.Total - 1) As Byte
            Marshal.Copy(dst2.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = RedPrep_CPP_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols)
            handleSrc.Free()

            dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Clone
            If src.Size <> task.noDepthMask.Size Then
                dst3.SetTo(255, task.noDepthMask.Resize(src.Size))
                dst2 = dst2.Resize(src.Size)
                dst2.SetTo(0, dst3)
            Else
                dst3.SetTo(255, task.noDepthMask)
                dst2.SetTo(0, dst3)
            End If
        End Sub
        Public Sub Close()
            RedPrep_CPP_Close(cPtr)
        End Sub
    End Class




    Public Class RedPrep_CloudAndColor : Inherits TaskParent
        Dim prepEdges As New RedPrep_Edges_CPP
        Public options As New Options_RedCloud
        Dim redSimple As New RedColor_Basics
        Dim edges As New EdgeLine_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Reduction transform for the point cloud"
        End Sub
        Private Function reduceChan(chan As cv.Mat) As cv.Mat
            chan *= task.reductionTarget
            Dim mm As mmData = GetMinMax(chan)
            Dim dst32f As New cv.Mat
            If Math.Abs(mm.minVal) > mm.maxVal Then
                mm.minVal = -mm.maxVal
                chan.ConvertTo(dst32f, cv.MatType.CV_32F)
                Dim mask = dst32f.Threshold(mm.minVal, mm.minVal, cv.ThresholdTypes.BinaryInv)
                mask.ConvertTo(mask, cv.MatType.CV_8U)
                dst32f.SetTo(mm.minVal, mask)
            End If
            chan = (chan - mm.minVal) * 255 / (mm.maxVal - mm.minVal)
            chan.ConvertTo(chan, cv.MatType.CV_8U)
            chan.SetTo(0, task.noDepthMask)
            Return chan
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim pc32S As New cv.Mat
            task.pointCloud.ConvertTo(pc32S, cv.MatType.CV_32SC3, 1000 / task.reductionTarget)
            Dim split = pc32S.Split()

            dst2.SetTo(0)
            If options.PrepX Then
                prepEdges.Run(reduceChan(split(0)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepY Then
                prepEdges.Run(reduceChan(split(1)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            If options.PrepZ Then
                prepEdges.Run(reduceChan(split(2)))
                dst2 = dst2 Or prepEdges.dst3
            End If

            redSimple.Run(src)
            edges.Run(redSimple.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst3 = edges.dst2
            dst3.CopyTo(dst2, task.noDepthMask)

            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, task.lineWidth)
            labels(2) = "Using reduction factor = " + CStr(task.reductionTarget)
        End Sub
    End Class





    Public Class RedPrep_EdgesX : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in Y").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Z").Checked = False
            desc = "Find X depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            labels(2) = edges.labels(2)

            dst2.SetTo(0, task.noDepthMask)
        End Sub
    End Class






    Public Class RedPrep_EdgesY : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in X").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Z").Checked = False
            desc = "Find Y depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            labels(2) = edges.labels(2)

            dst2.SetTo(0, task.noDepthMask)
        End Sub
    End Class






    Public Class RedPrep_EdgesZ : Inherits TaskParent
        Dim edges As New RedPrep_Basics
        Public Sub New()
            OptionParent.FindCheckBox("Prep Edges in X").Checked = False
            OptionParent.FindCheckBox("Prep Edges in Y").Checked = False
            desc = "Find Z depth edges in the pointcloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            dst2.SetTo(0, task.noDepthMask)

            labels(2) = edges.labels(2)
        End Sub
    End Class









    Public Class XO_RedList_LikelyFlatSurfaces : Inherits TaskParent
        Dim verts As New Plane_Basics
        Public vCells As New List(Of oldrcData)
        Public hCells As New List(Of oldrcData)
        Public Sub New()
            labels(1) = "RedCloud output"
            desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            runRedList(src, labels(2))
            verts.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)

            vCells.Clear()
            hCells.Clear()
            For Each rc In task.redList.oldrclist
                If rc.depth >= task.MaxZmeters Then Continue For
                Dim tmp As cv.Mat = verts.dst2(rc.rect) And rc.mask
                If tmp.CountNonZero / rc.pixels > 0.5 Then
                    DrawTour(dst2(rc.rect), rc.contour, rc.color, -1)
                    vCells.Add(rc)
                End If
                tmp = verts.dst3(rc.rect) And rc.mask
                Dim count = tmp.CountNonZero
                If count / rc.pixels > 0.5 Then
                    DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)
                    hCells.Add(rc)
                End If
            Next

            Dim rcX = task.oldrcD
            SetTrueText("mean depth = " + Format(rcX.depth, "0.0"), 3)
        End Sub
    End Class





    Public Class XO_RedList_MostlyColor : Inherits TaskParent
        Public Sub New()
            labels(3) = "Cells that have more than 50% depth data."
            desc = "Identify cells that have more than 50% depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst3.SetTo(0)
            For Each rc In task.redList.oldrclist
                If rc.depthPixels / rc.pixels > 0.5 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
            Next
        End Sub
    End Class








    Public Class XO_RedList_Motion : Inherits TaskParent
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "If a RedCloud cell has no motion, it is preserved."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.motionBasics.motionList.Count = 0 Then Exit Sub ' full image stable means nothing needs to be done...
            runRedList(src, labels(2))
            If task.redList.oldrclist.Count = 0 Then Exit Sub

            Static rcLastList As New List(Of oldrcData)(task.redList.oldrclist)

            Dim count As Integer
            dst1.SetTo(0)
            task.redList.oldrclist.RemoveAt(0)
            'Dim newList As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)
            Dim newList As New List(Of oldrcData), tmp As New cv.Mat
            Dim countMaxD As Integer, countMissedMaxD As Integer
            For Each rc In task.redList.oldrclist
                tmp = task.motionMask(rc.rect) And rc.mask
                If tmp.CountNonZero = 0 Then
                    If rc.indexLast <> 0 And rc.indexLast < rcLastList.Count Then
                        Dim lrc = rcLastList(rc.indexLast)
                        If lrc.maxDStable = rc.maxDStable Then
                            countMaxD += 1
                            rc = lrc
                        Else
                            countMissedMaxD += 1
                            Continue For
                        End If
                    End If
                    Dim testCell = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    If testCell = 0 Then newList.Add(rc)
                Else
                    count += 1
                    newList.Add(rc)
                End If
                dst1(rc.rect).SetTo(255, rc.mask)
            Next
            labels(3) = CStr(count) + " of " + CStr(task.redList.oldrclist.Count) + " redCloud cells had motion." +
                    "  There were " + CStr(countMaxD) + " maxDstable matches and " + CStr(countMissedMaxD) + " misses"

            task.redList.oldrclist.Clear()
            task.redList.oldrclist.Add(New oldrcData)
            For Each rc In newList
                rc.index = task.redList.oldrclist.Count
                task.redList.oldrclist.Add(rc)
            Next

            rcLastList = New List(Of oldrcData)(task.redList.oldrclist)

            dst3.SetTo(0)
            For Each rc In task.redList.oldrclist
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            Next

            dst2 = RebuildRCMap(task.redList.oldrclist.ToList)
            XO_RedList_Basics.setSelectedCell()
        End Sub
    End Class






    Public Class XO_RedList_OnlyColorHist3D : Inherits TaskParent
        Dim hColor As New Hist3Dcolor_Basics
        Public Sub New()
            desc = "Use the backprojection of the 3D RGB histogram as input to RedList_Basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            runRedList(src, labels(3))
            hColor.Run(src)
            dst2 = hColor.dst3
            labels(2) = hColor.labels(3)

            dst3 = task.redList.rcMap
            dst3.SetTo(0, task.noDepthMask)
            labels(3) = task.redList.labels(2)
        End Sub
    End Class






    Public Class XO_RedList_OnlyColorAlt : Inherits TaskParent
        Public Sub New()
            desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            runRedList(src, labels(3))

            Dim lastCells As New List(Of oldrcData)(task.redList.oldrclist)
            Dim lastMap As cv.Mat = task.redList.rcMap.Clone
            Dim lastColors As cv.Mat = dst3.Clone

            Dim newCells As New List(Of oldrcData)
            task.redList.rcMap.SetTo(0)
            dst3.SetTo(0)
            Dim usedColors = New List(Of cv.Scalar)({black})
            Dim unmatched As Integer
            For Each rc In task.redList.oldrclist
                Dim index = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If index < lastCells.Count Then
                    rc.color = lastColors.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X).ToVec3f
                Else
                    unmatched += 1
                End If
                If usedColors.Contains(rc.color) Then
                    unmatched += 1
                    rc.color = Palette_Basics.randomCellColor()
                End If
                usedColors.Add(rc.color)

                If task.redList.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) = 0 Then
                    rc.index = task.redList.oldrclist.Count
                    newCells.Add(rc)
                    task.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                End If
            Next

            task.redList.oldrclist = New List(Of oldrcData)(newCells)
            labels(3) = CStr(task.redList.oldrclist.Count) + " cells were identified."
            labels(2) = task.redList.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

            If task.redList.oldrclist.Count > 0 Then dst2 = PaletteFull(lastMap)
        End Sub
    End Class
End Namespace