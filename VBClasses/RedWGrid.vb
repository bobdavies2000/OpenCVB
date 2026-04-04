Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports VBClasses
    Public Class RedWGrid_Basics : Inherits TaskParent
        Public redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Consolidate duplicate world grid coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            labels(2) = redC.labels(2)

            Dim dups As New SortedList(Of String, Integer)(New compareAllowIdenticalString)
            For Each rc In redC.rcList
                dups.Add(Format(rc.wGrid.X, "000") + Format(rc.wGrid.Y, "000") + Format(rc.wGrid.Z, "000"),
                                rc.index - 1)
            Next

            Dim newList As New List(Of rcData)
            Dim rc1 As rcData = Nothing
            Dim rc2 As rcData = Nothing
            Dim r As cv.Rect
            dst1.SetTo(0)
            For i = 1 To dups.Count - 1
                If rc1 Is Nothing Then rc1 = redC.rcList(dups.Values(i - 1))
                rc2 = redC.rcList(dups.Values(i))

                If rc1.wGrid.X = rc2.wGrid.X And rc1.wGrid.Y = rc2.wGrid.Y And
                    Math.Abs(rc1.wcMean(2) - rc2.wcMean(2)) < 1.0 Then
                    r = rc1.rect.Union(rc2.rect)
                    dst1(r).SetTo(0)
                    dst1(rc1.rect).SetTo(255, rc1.mask)
                    dst1(rc2.rect).SetTo(255, rc2.mask)
                    rc1.rect = r
                    rc1.mask = dst1(r).Clone
                    ' take the values of depthdelta and wcmean from the larger of the 2 rcData's
                    If rc1.pixels < rc2.pixels Then
                        rc1.depthDelta = rc2.depthDelta
                        rc1.wcMean = rc2.wcMean
                    End If
                    rc1.multiMask = True
                    If rc1.wGrid.X = -2 And rc1.wGrid.Y = 0 Then Dim k = 0
                Else
                    If rc1.multiMask Then
                        rc1.contour = New List(Of cv.Point)
                        rc1.hull = New List(Of cv.Point)
                        rc1.pixels = rc1.mask.CountNonZero
                    End If
                    newList.Add(rc1)
                    rc1 = Nothing
                End If
            Next

            If rc1 IsNot Nothing Then
                rc1.contour = New List(Of cv.Point)
                rc1.hull = New List(Of cv.Point)
                rc1.pixels = rc1.mask.CountNonZero
                newList.Add(rc1)
            Else
                newList.Add(rc2)
            End If

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            Dim count As Integer
            For Each rc In newList
                If rc.multiMask Then count += 1
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
                dst2(rc.rect).SetTo(rc.color, rc.mask)

                If task.gOptions.DebugCheckBox.Checked And rc.multiMask Then
                    dst2(rc.rect).SetTo(task.highlight, rc.mask)
                End If
            Next

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)

            labels(2) = CStr(rcList.Count) + " cells remain after merging masks for " + CStr(count) + " wGrid points."
            labels(3) = CStr(count) + " multi-mask cells found"
        End Sub
    End Class





    Public Class RedWGrid_Basics2 : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Dim redC As New RedCloud_Basics
        Public Sub New()
            labels(3) = "Use debug slider to select region to display."
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Run RedCloud on the output of RedPrep_Core"
        End Sub
        Public Shared Function countClasses(input As cv.Mat, ByRef label As String) As cv.Mat
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({input}, {0}, task.depthmask, histogram, 1, {256}, {New cv.Rangef(0, 256)})
            Dim histArray(255) As Single
            histogram.GetArray(Of Single)(histArray)

            Dim sizeThreshold = input.Total * 0.001 ' ignore regions less than 0.1% - 1/10th of 1%
            Dim lutArray(255) As Byte
            Dim regionList As New List(Of Integer)
            For i = 1 To histArray.Count - 1
                If histArray(i) > sizeThreshold Then
                    regionList.Add(i)
                    lutArray(i) = regionList.Count
                End If
            Next

            Dim lut As New cv.Mat(1, 256, cv.MatType.CV_8U)
            lut.SetArray(Of Byte)(lutArray)

            label = CStr(regionList.Count) + " non-zero regions more than " + CStr(sizeThreshold) + " pixels"
            Return lut
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)

            dst3 = prepData.reduced32s.Normalize(255, 0, cv.NormTypes.MinMax)
            dst3.ConvertTo(dst3, cv.MatType.CV_8U)
            dst3.SetTo(0, task.noDepthMask)

            redC.Run(dst3)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End Sub
    End Class




    Public Class NR_RedWGrid_PrepData : Inherits TaskParent
        Dim prepData As New RedPrep_Core
        Public Sub New()
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)
            dst3 = prepData.reduced32s.Normalize(255, 0, cv.NormTypes.MinMax)
            dst3.SetTo(0, task.noDepthMask)
            dst2 = Palettize(dst3, 0)
            labels(2) = prepData.labels(2)

            Dim val = prepData.reduced32f.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
            SetTrueText("Depth = " + Format(val, fmt3), 3)
        End Sub
    End Class




    Public Class NR_RedWGrid_Basics1 : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public lut As New cv.Mat
        Public lutList As New List(Of Byte)
        Public Sub New()
            task.gOptions.DebugSlider.Value = 1
            labels(3) = "Use debug slider to select region to display."
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prepare the grid of point cloud data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(emptyMat)
            labels(2) = prepData.labels(2)

            lut = RedWGrid_Basics2.countClasses(prepData.dst2.Clone, labels(2))
            lutList.Clear()
            For i = 0 To lut.Cols - 1
                Dim val = lut.Get(Of Byte)(0, i)
                If val > 0 Then lutList.Add(val)
            Next
            dst1 = prepData.dst2.LUT(lut)
            dst2 = Palettize(dst1, 0)

            If standalone Then
                Dim index = Math.Abs(task.gOptions.DebugSlider.Value)
                If index < lutList.Count Then
                    dst3 = dst1.InRange(index, index)
                End If
            End If
        End Sub
    End Class





    Public Class RedWGrid_PrepXY : Inherits TaskParent
        Public RedWGrid As New RedWGrid_Basics2
        Public Sub New()
            OptionParent.findRadio("XY Reduction").Checked = True
            desc = "Prep the XY regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            RedWGrid.Run(src)
            dst2 = Palettize(RedWGrid.dst1, 0)
            labels(2) = RedWGrid.labels(2)
        End Sub
    End Class





    Public Class NR_RedWGrid_Validate : Inherits TaskParent
        Dim RedWGrid As New NR_RedWGrid_PrepY
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            task.gOptions.displayDst1.Checked = True
            desc = "Identify the different regions in the RedWGrid_PrepX/Y using the debugslider"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            RedWGrid.Run(src)
            dst2 = RedWGrid.dst2
            labels(2) = RedWGrid.labels(2)

            Dim mm = GetMinMax(RedWGrid.prepData.reduced32f)
            Dim ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            Dim histogram As New cv.Mat
            Dim histBins As Integer = 500
            cv.Cv2.CalcHist({RedWGrid.prepData.reduced32f}, {0}, task.depthmask, histogram, 1, {histBins}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)
            Dim incr = mm.range / histBins

            dst1.SetTo(0)
            For i = 0 To histArray.Count - 1
                Dim tmp = RedWGrid.prepData.reduced32f.InRange(mm.minVal + incr * i, mm.minVal + incr * (i + 1))
                dst1.SetTo(i + 1, tmp)
            Next
            dst1.SetTo(0, task.noDepthMask)

            dst3 = Palettize(dst1, 0)
        End Sub
    End Class




    Public Class NR_RedWGrid_CheckerBoardWall : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Use this algorithm to build a checkerboard when pointing at a wall."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' prepData.reductionName = "XY Reduction" ' default
            prepData.Run(src)

            Dim lut = RedWGrid_Basics2.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)

            edges.Run(dst2)
            dst3 = edges.dst2
        End Sub
    End Class





    Public Class NR_RedWGrid_CPP : Inherits TaskParent
        Implements IDisposable
        Dim prep As New RedPrep_Basics
        Public Sub New()
            cPtr = RedCart_CPP_Open()
            desc = "Hit the locations where floodfill slips up by placeing a dot in the intersection."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst1
            labels(2) = prep.labels(2)

            Dim cppData(dst2.Total - 1) As Byte
            dst2.GetArray(Of Byte)(cppData)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = RedCart_CPP_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols)
            handleSrc.Free()

            dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst3.SetTo(255, task.noDepthMask)
            dst2.SetTo(0, dst3)
        End Sub
        Protected Overrides Sub Finalize()
            RedCart_CPP_Close(cPtr)
        End Sub
    End Class




    Public Class NR_RedWGrid_PrepXOld : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public lut As cv.Mat
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedWGrid_Basics2.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
        End Sub
    End Class



    Public Class NR_RedWGrid_PrepY : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            OptionParent.findRadio("Y Reduction").Checked = True
            desc = "Prep the horizontal regions in the reduced depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedWGrid_Basics2.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
        End Sub
    End Class




    Public Class RedWGrid_PrepXYAlt : Inherits TaskParent
        Dim redX As New NR_RedWGrid_PrepX
        Dim redY As New NR_RedWGrid_PrepY
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)

            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Add the output of PrepX and PrepY.  Point camera at a wall for interesting results."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redX.Run(src)
            dst1 = redX.dst2
            dst1.SetTo(0, task.noDepthMask)
            labels(1) = CStr(redX.classCount) + " regions were found"

            redY.Run(src)
            dst3 = redY.dst2
            dst3.SetTo(0, task.noDepthMask)
            labels(3) = CStr(redY.classCount) + " regions were found"

            dst2 = dst1 Or dst3
            labels(2) = CStr(redX.classCount + redY.classCount) + " regions were found"
        End Sub
    End Class





    Public Class NR_RedWGrid_PrepX : Inherits TaskParent
        Public prepData As New RedPrep_Core
        Public classCount As Integer
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            desc = "Prep the vertical regions in the reduced depth data."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            prepData.Run(src)
            dst2 = prepData.dst3

            Dim lut = RedWGrid_Basics2.countClasses(prepData.dst2, labels(2))
            dst2 = prepData.dst2.LUT(lut)
        End Sub
    End Class





    Public Class RedWGrid_Debug : Inherits TaskParent
        Dim RedWGrid As New NR_RedWGrid_Basics1
        Public classCount As Integer
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify each region using the debug slider."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            RedWGrid.Run(emptyMat)
            dst3 = RedWGrid.dst2
            labels(3) = RedWGrid.labels(2)
            strOut = ""

            For i = 1 To RedWGrid.lutList.Count - 1
                dst2 = RedWGrid.dst1.InRange(i, i)
                Dim mean = RedWGrid.prepData.reduced32s.Mean(dst2)
                strOut += "Mean of selected region " + CStr(i) + " = " + Format(mean(0), fmt0) + "  "
                If i Mod 2 = 0 Then strOut += vbCrLf
            Next

            dst2 = (RedWGrid.prepData.reduced32s - -1000) * 255 / (2000)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2.SetTo(0, task.noDepthMask)


            labels(1) = CStr(RedWGrid.lutList.Count) + " non-zero regions."
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class NR_RedWGrid_Basics : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim currSet As New List(Of cv.Point3d)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify where RedCloud world coordinates are changing"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim lastSet As New List(Of cv.Point3d)(currSet)
            dst2.SetTo(0)
            Static count As Integer
            If task.heartBeatLT Or task.frameCount = 2 Then
                dst3.SetTo(0)
                count = 0
            End If
            currSet.Clear()
            For Each rc In redC.rcList
                currSet.Add(rc.wGrid)
                If lastSet.Contains(rc.wGrid) Then
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                Else
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    count += 1
                End If
            Next

            SetTrueText(redC.strOut, 1)

            labels(3) = CStr(count) + " unstable cells = not matched since the last heartbeatLT"
        End Sub
    End Class





    Public Class RedWGrid_Click : Inherits TaskParent
        Dim dups As New RedWGrid_Basics
        Dim options As New Options_WGrid
        Public Sub New()
            desc = "Click on any RedCloud cell to see similar cells connected by the wGrid point."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dups.Run(src)
            dst2 = dups.dst2
            labels(2) = dups.labels(2)

            If task.rcD Is Nothing Then
                SetTrueText("Click on any cell present in dst2", 3)
                Exit Sub
            End If

            SetTrueText(dups.strOut, 3)

            Select Case options.clickName
                Case "Identify Row"
                    Dim row = task.rcD.wGrid.Y
                    For Each rc In dups.redC.rcList
                        If rc.wGrid.Y = row Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                    labels(3) = "Row " + CStr(row) + " selected"
                Case "Identify Col"
                    Dim col = task.rcD.wGrid.X
                    For Each rc In dups.redC.rcList
                        If rc.wGrid.X = col Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                    labels(3) = "Col " + CStr(col) + " selected"
                Case "Identify Neighbors"
                    Dim row = task.rcD.wGrid.Y
                    Dim col = task.rcD.wGrid.X
                    For Each rc In dups.redC.rcList
                        If Math.Abs(task.rcD.wGrid.X - rc.wGrid.X) <= 1 And
                           Math.Abs(task.rcD.wGrid.Y - rc.wGrid.Y) <= 1 Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                Case "Identify Multi-Mask Cells"
                    For Each rc In dups.redC.rcList
                        If rc.multiMask Then dst2(rc.rect).SetTo(white, rc.mask)
                    Next
            End Select

        End Sub
    End Class




    Public Class RedWGrid_Pattern : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim points As New List(Of cv.Point)
        Dim colorIndex As Integer
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "World Grid X lines", "World Grid Y Lines"}
            desc = "Highlight the layout pattern of the World Grid."
        End Sub
        Private Sub nextLine(x As Integer, y As Integer, dst As cv.Mat)
            Dim pt = New cv.Point(x, y)
            Dim index = points.IndexOf(pt)
            If index >= 0 Then
                Dim rc = redC.rcList(index)
                dst(rc.rect).SetTo(task.scalarColors(colorIndex), rc.mask)
            End If

        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            If task.toggleOn Then
                dst1 = redC.dst2
            Else
                dst1.SetTo(0)
                SetTrueText(redC.strOut, 1)
            End If
            labels(1) = redC.labels(2)

            points.Clear()

            For Each rc In redC.rcList
                points.Add(New cv.Point(rc.wGrid.X, rc.wGrid.Y))
            Next

            colorIndex = 0
            dst2 = dst1.Clone
            For y = -10 To 10
                For x = -10 To 10
                    nextLine(x, y, dst2)
                Next
                colorIndex += 1
            Next

            dst3 = dst1.Clone
            colorIndex = 0
            For x = -10 To 10
                For y = -10 To 10
                    nextLine(x, y, dst3)
                Next
                colorIndex += 1
            Next
        End Sub
    End Class
