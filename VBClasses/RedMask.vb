Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
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

            Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols,
                                       dst2.Total * 0.001)
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
                If md.rect.Width * md.rect.Height < dst2.Total * 0.001 Then Continue For
                md.mask = dst2(md.rect).InRange(i + 1, i + 1)
                md.contour = ContourBuild(md.mask)
                DrawTour(md.mask, md.contour, 255, -1)
                md.pixels = md.mask.CountNonZero
                md.maxDist = Distance_Basics.GetMaxDist(md)
                md.mm = vbc.GetMinMax(task.pcSplit(2)(md.rect), task.depthmask(md.rect))
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




    Public Class RedMask_Cells : Inherits TaskParent
        Public mdList As List(Of maskData)
        Public Sub New()
            desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If mdList Is Nothing Then
                SetTrueText("RedMask_Cells is run by numerous algorithms but generates no output when standalone. ", 2)
                Exit Sub
            End If
            If task.redList Is Nothing Then task.redList = New XO_RedList_Basics

            Dim initialList As New List(Of oldrcData)
            For i = 0 To mdList.Count - 1
                Dim rc As New oldrcData
                rc.rect = mdList(i).rect
                If rc.rect.Size = dst2.Size Then Continue For ' RedMask_List can find a cell this big.  
                rc.mask = mdList(i).mask
                rc.maxDist = mdList(i).maxDist
                rc.maxDStable = rc.maxDist
                rc.indexLast = task.redList.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.contour = mdList(i).contour
                DrawTour(rc.mask, rc.contour, 255, -1)
                rc.pixels = mdList(i).mask.CountNonZero
                If rc.indexLast >= task.redList.oldrclist.Count Then rc.indexLast = 0
                If rc.indexLast > 0 Then
                    Dim lrc = task.redList.oldrclist(rc.indexLast)
                    rc.age = lrc.age + 1
                    rc.depthPixels = lrc.depthPixels
                    rc.mmX = lrc.mmX
                    rc.mmY = lrc.mmY
                    rc.mmZ = lrc.mmZ
                    rc.maxDStable = lrc.maxDStable

                    If rc.pixels < dst2.Total * 0.001 Then
                        rc.color = yellow
                    Else
                        ' verify that the maxDStable is still good.
                        Dim v1 = task.redList.rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                        If v1 <> lrc.index Then
                            rc.maxDStable = rc.maxDist

                            rc.age = 1 ' a new cell was found that was probably part of another in the previous frame.
                        End If
                    End If
                Else
                    rc.age = 1
                End If

                Dim brickIndex = task.gridMap.Get(Of Integer)(rc.maxDStable.Y, rc.maxDStable.X)
                rc.color = task.scalarColors(brickIndex Mod 255)
                initialList.Add(rc)
            Next

            Dim sortedCells As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)

            Dim rcNewCount As Integer
            Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
            For Each rc In initialList
                rc.pixels = rc.mask.CountNonZero
                If rc.pixels = 0 Then Continue For

                Dim depthMask = rc.mask.Clone
                depthMask.SetTo(0, task.noDepthMask(rc.rect))
                Dim depthPixels = depthMask.CountNonZero

                If depthPixels / rc.pixels > 0.1 Then
                    rc.mmX = GetMinMax(task.pcSplit(0)(rc.rect), depthMask)
                    rc.mmY = GetMinMax(task.pcSplit(1)(rc.rect), depthMask)
                    rc.mmZ = GetMinMax(task.pcSplit(2)(rc.rect), depthMask)

                    cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, depthMask)
                    rc.depth = depthMean(2)
                    If Single.IsNaN(rc.depth) Or rc.depth < 0 Then rc.depth = 0
                End If

                If rc.age = 1 Then rcNewCount += 1
                sortedCells.Add(rc.pixels, rc)
            Next

            labels(2) = CStr(mdList.Count) + " total cells"
            dst2 = XO_RedList_MaxDist.RebuildRCMap(sortedCells.Values.ToList.ToList)
        End Sub
    End Class





    Public Class RedMask_List : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New RedMask_Cells
        Public redMask As New RedMask_Basics
        Public contours As New Contour_Basics
        Public Sub New()
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
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

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = redMask.dst3

            For Each md In cellGen.mdList
                dst2.Circle(md.maxDist, task.DotSize, task.highlight, -1)
            Next

            labels(2) = cellGen.labels(2)
            labels(3) = ""
            SetTrueText("", newPoint, 1)
        End Sub
    End Class






    Public Class RedMask_Color : Inherits TaskParent
        Dim cellGen As New RedMask_Cells
        Dim redMask As New RedMask_Basics
        Dim rclist As New List(Of rcData)
        Dim rcMap As cv.Mat ' redColor map 
        Dim contours As New Contour_Basics
        Public Sub New()
            rcMap = New cv.Mat(New cv.Size(dst2.Width, dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
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

            redMask.Run(dst1 + 1)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = cellGen.dst2

            rclist.Clear()
            For Each md In redMask.mdList
                Dim rc = New rcData(md.mask, md.rect, rclist.Count + 1)
                rc.buildMaxDist()
                rclist.Add(rc)
            Next

            labels(2) = cellGen.labels(2)
            labels(3) = ""
            SetTrueText("", newPoint, 1)
            strOut = RedUtil_Basics.selectCell(rcMap, rclist)
        End Sub
    End Class
End Namespace