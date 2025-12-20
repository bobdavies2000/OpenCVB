Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class RedList_CellStatsPlot : Inherits TaskParent
        Dim cells As New XO_RedCell_BasicsPlot
        Public Sub New()
            If standaloneTest() Then taskAlg.gOptions.displayDst1.Checked = True
            cells.runRedCflag = True
            desc = "Display the stats for the selected cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            cells.Run(src)
            dst1 = cells.dst3
            dst2 = cells.dst2
            labels(2) = cells.labels(2)

            SetTrueText(cells.strOut, 3)
        End Sub
    End Class









    Public Class RedList_FourColor : Inherits TaskParent
        Dim binar4 As New Bin4Way_Regions
        Public Sub New()
            labels(3) = "A 4-way split of the input grayscale image based on brightness"
            desc = "Use RedCloud on a 4-way split based on light to dark in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            binar4.Run(src)
            dst3 = PaletteFull(binar4.dst2)

            dst2 = runRedList(binar4.dst2, labels(2))
        End Sub
    End Class









    Public Class RedList_Hue : Inherits TaskParent
        Dim hue As New Color8U_Hue
        Public Sub New()
            labels(3) = "Mask of the areas with Hue"
            desc = "Run RedCloud on just the red hue regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hue.Run(src)
            dst3 = hue.dst2

            dst2 = runRedList(src, labels(2), Not dst3)
        End Sub
    End Class










    Public Class RedList_Consistent : Inherits TaskParent
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            OptionParent.FindSlider("Color Difference Threshold").Value = 1
            desc = "Remove RedColor results that are inconsistent with the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst3.SetTo(0)
            Dim count As Integer
            For Each rc In taskAlg.redList.oldrclist
                If rc.age > 1 Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " cells matched the previous generation."
        End Sub
    End Class







    Public Class RedList_Features : Inherits TaskParent
        Dim options As New Options_RedCloudFeatures
        Public Sub New()
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

            Dim rc = taskAlg.oldrcD

            dst0 = taskAlg.color
            Dim correlationMat As New cv.Mat, correlationXtoZ As Single, correlationYtoZ As Single
            dst3.SetTo(0)
            Select Case options.selection
                Case 0
                    Dim pt = rc.maxDist
                    dst2.Circle(pt, taskAlg.DotSize, taskAlg.highlight, -1, cv.LineTypes.AntiAlias)
                    labels(3) = "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
                Case 1
                    dst3(rc.rect).SetTo(vbNearFar((rc.depth) / taskAlg.MaxZmeters), rc.mask)
                    labels(3) = "rc.depth Is highlighted in dst2"
                    labels(3) = "Mean depth for the cell Is " + Format(rc.depth, fmt3)
                Case 2
                    cv.Cv2.MatchTemplate(taskAlg.pcSplit(0)(rc.rect), taskAlg.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                    correlationXtoZ = correlationMat.Get(Of Single)(0, 0)
                    labels(3) = "High correlation X to Z Is yellow, low correlation X to Z Is blue"
                Case 3
                    cv.Cv2.MatchTemplate(taskAlg.pcSplit(1)(rc.rect), taskAlg.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                    correlationYtoZ = correlationMat.Get(Of Single)(0, 0)
                    labels(3) = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue"
            End Select
            If options.selection = 2 Or options.selection = 3 Then
                dst3(rc.rect).SetTo(vbNearFar(If(options.selection = 2, correlationXtoZ, correlationYtoZ) + 1), rc.mask)
                SetTrueText("(" + Format(correlationXtoZ, fmt3) + ", " + Format(correlationYtoZ, fmt3) + ")", New cv.Point(rc.rect.X, rc.rect.Y), 3)
            End If
            DrawTour(dst0(rc.rect), rc.contour, cv.Scalar.Yellow)
            SetTrueText(labels(3), 3)
            labels(2) = "Highlighted feature = " + options.labelName
        End Sub
    End Class









    Public Class RedList_CPP : Inherits TaskParent
        Public classCount As Integer
        Public rectList As New List(Of cv.Rect)
        Public identifyCount As Integer = 255
        Public Sub New()
            cPtr = RedMask_Open()
            desc = "Run the C++ RedCloud Interface With Or without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = srcMustBe8U(src)

            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, 0)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
            dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

            classCount = Math.Min(RedMask_Count(cPtr), identifyCount * 2)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))

            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            rectList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
            Next

            If standaloneTest() Then dst3 = PaletteFull(dst2)

            If taskAlg.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
            If taskAlg.heartBeat Then labels(3) = "Palette version of the data In dst2 With " + CStr(classCount) + " regions."
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
        End Sub
    End Class










    Public Class RedList_Hulls : Inherits TaskParent
        Public oldrclist As New List(Of oldrcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            labels = {"", "Cells where convexity defects failed", "", "Improved contour results Using OpenCV's ConvexityDefects"}
            desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            Dim defectCount As Integer
            taskAlg.redList.rcMap.SetTo(0)
            oldrclist.Clear()
            For Each rc In taskAlg.redList.oldrclist
                If rc.contour.Count >= 5 Then
                    rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                    Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                    Try
                        Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                        rc.contour = Convex_RedColorDefects.betterContour(rc.contour, defects)
                    Catch ex As Exception
                        defectCount += 1
                    End Try
                    DrawTour(rcMap(rc.rect), rc.hull, rc.index, -1)
                    oldrclist.Add(rc)
                End If
            Next
            dst3 = PaletteFull(rcMap)
            labels(3) = CStr(oldrclist.Count) + " hulls identified below.  " + CStr(defectCount) +
                    " hulls failed to build the defect list."
        End Sub
    End Class






    Public Class RedList_CellDepthHistogram : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public Sub New()
            taskAlg.gOptions.setHistogramBins(100)
            plot.createHistogram = True
            desc = "Display the histogram of a selected RedColor cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            If taskAlg.heartBeat Then
                Dim depth As cv.Mat = taskAlg.pcSplit(2)(taskAlg.oldrcD.rect)
                depth.SetTo(0, taskAlg.noDepthMask(taskAlg.oldrcD.rect))
                plot.minRange = 0
                plot.maxRange = taskAlg.MaxZmeters
                plot.Run(depth)
                labels(3) = "0 meters to " + Format(taskAlg.MaxZmeters, fmt0) + " meters - vertical lines every meter"

                Dim incr = dst2.Width / taskAlg.MaxZmeters
                For i = 1 To CInt(taskAlg.MaxZmeters - 1)
                    Dim x = incr * i
                    vbc.DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
                Next
            End If
            dst3 = plot.dst2
        End Sub
    End Class






    Public Class RedList_EdgesZ : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Dim edgesZ As New RedPrep_EdgesZ
        Public Sub New()
            desc = "Add the depth edges in Z to the color image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(src)

            edgesZ.Run(reduction.dst3)

            dst2 = runRedList(edgesZ.dst2, labels(2))
        End Sub
    End Class
End Namespace