Imports System.Dynamic
Imports VB_Classes.VBtask
Imports cv = OpenCvSharp
Public Class DepthCell_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_DepthCellSize
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean
    Public mouseD As New DepthCell_MouseDepth
    Public quad As New Quad_Basics
    Public merge As New Quad_CellConnect
    Dim iddCorr As New DepthCell_CorrelationMap
    Dim caminfo As cameraInfo
    Public Sub New()
        optiBase.FindSlider("Percent Depth Threshold").Value = 25
        desc = "Create the grid of depth cells that reduce depth volatility"
    End Sub
    Public Function translateColorToLeft(pt As cv.Point) As cv.Point
        Dim ir3D As cv.Point3f, irPt As cv.Point2f
        Dim pcTop = task.pointCloudRaw.Get(Of cv.Point3f)(pt.Y, pt.X)
        If pcTop.Z > 0 Then
            ir3D.X = caminfo.rotation(0) * pcTop.X +
                         caminfo.rotation(1) * pcTop.Y +
                         caminfo.rotation(2) * pcTop.Z + caminfo.translation(0)
            ir3D.Y = caminfo.rotation(3) * pcTop.X +
                         caminfo.rotation(4) * pcTop.Y +
                         caminfo.rotation(5) * pcTop.Z + caminfo.translation(1)
            ir3D.Z = caminfo.rotation(6) * pcTop.X +
                         caminfo.rotation(7) * pcTop.Y +
                         caminfo.rotation(8) * pcTop.Z + caminfo.translation(2)
            irPt.X = caminfo.leftIntrinsics.fx * ir3D.X / ir3D.Z + caminfo.leftIntrinsics.ppx
            irPt.Y = caminfo.leftIntrinsics.fy * ir3D.Y / ir3D.Z + caminfo.leftIntrinsics.ppy
        End If
        Return irPt
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        grid.Run(src)

        If task.optionsChanged Or instantUpdate Then
            task.iddList.Clear()
            For Each rect In grid.gridRectsAll
                Dim idd As New depthCell
                idd.cRect = ValidateRect(rect)
                idd.lRect = ValidateRect(rect) ' for some cameras the color image and the left image are the same.
                idd.center = New cv.Point(rect.TopLeft.X + task.dCellSize / 2, rect.TopLeft.Y + task.dCellSize / 2)
                idd.age = 0
                task.iddList.Add(idd)
            Next
        End If

        Dim colorStdev As cv.Scalar, colormean As cv.Scalar
        caminfo = task.calibData
        Dim correlationMat As New cv.Mat
        Dim rgbLeftAligned As Boolean
        If task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec") Then
            rgbLeftAligned = True
        End If
        Dim irPt As cv.Point2f
        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            Dim motion = task.motionMask(idd.cRect).CountNonZero
            If motion = 0 And idd.age > 0 Then
                idd.age += 1
            Else
                cv.Cv2.MeanStdDev(src(idd.cRect), colormean, colorStdev)
                idd.color = New cv.Point3f(colormean(0), colormean(1), colormean(2))

                idd.pixels = task.depthMaskRaw(idd.cRect).CountNonZero
                idd.correlation = 0
                If idd.pixels / (idd.cRect.Width * idd.cRect.Height) < options.percentThreshold Then
                    idd.age = 0
                    idd.depth = 0
                    idd.rRect = New cv.Rect
                Else
                    idd.age = 1
                    idd.depth = task.pcSplitRaw(2)(idd.cRect).Mean(task.depthMaskRaw(idd.cRect))(0)
                    If idd.depth > 0 Then
                        If rgbLeftAligned Then
                            idd.lRect = idd.cRect
                            idd.rRect = idd.lRect
                            idd.rRect.X -= caminfo.baseline * caminfo.rgbIntrinsics.fx / idd.depth
                            idd.rRect = ValidateRect(idd.rRect)
                            cv.Cv2.MatchTemplate(task.leftView(idd.lRect), task.rightView(idd.rRect), correlationMat,
                                                 cv.TemplateMatchModes.CCoeffNormed)

                            idd.correlation = correlationMat.Get(Of Single)(0, 0)
                        Else
                            irPt = translateColorToLeft(idd.cRect.TopLeft)
                            If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Then
                                idd.depth = 0 ' off the grid.
                                idd.lRect = New cv.Rect
                                idd.rRect = New cv.Rect
                            Else
                                idd.lRect = New cv.Rect(irPt.X, irPt.Y, idd.cRect.Width, idd.cRect.Height)
                                idd.lRect = ValidateRect(idd.lRect)

                                idd.rRect = idd.lRect
                                idd.rRect.X -= caminfo.baseline * caminfo.leftIntrinsics.fx / idd.depth
                                idd.rRect = ValidateRect(idd.rRect)
                                cv.Cv2.MatchTemplate(task.leftView(idd.lRect), task.rightView(idd.rRect), correlationMat,
                                                             cv.TemplateMatchModes.CCoeffNormed)

                                idd.correlation = correlationMat.Get(Of Single)(0, 0)
                            End If
                        End If
                    Else
                        idd.lRect = New cv.Rect
                        idd.rRect = New cv.Rect
                    End If
                End If
            End If
            task.iddList(i) = idd
        Next

        quad.Run(src)
        dst2 = quad.dst2

        merge.Run(src)
        dst3 = merge.dst2

        iddCorr.Run(src)

        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have the useful depth values."
    End Sub
End Class





Public Class DepthCell_MouseDepth : Inherits TaskParent
    Public ptReal As New cv.Point
    Public ptDepthAndCorrelation As New cv.Point
    Public depthAndCorrelationText As String
    Public Sub New()
        desc = "Provide the mouse depth at the mouse movement location."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseMovePoint.X < 0 Or task.mouseMovePoint.X >= dst2.Width Then Exit Sub
        If task.mouseMovePoint.Y < 0 Or task.mouseMovePoint.Y >= dst2.Height Then Exit Sub
        Dim index = task.iddMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim idd = task.iddList(index)
        dst2 = task.dCell.dst2

        ptReal = idd.cRect.TopLeft
        Dim pt = idd.cRect.TopLeft
        If pt.X > dst2.Width * 0.85 Or (pt.Y < dst2.Height * 0.15 And pt.X > dst2.Width * 0.15) Then
            pt.X -= dst2.Width * 0.15
        Else
            pt.Y -= idd.cRect.Height * 2
        End If
        depthAndCorrelationText = Format(idd.depth, fmt3) +
                                  "m (" + Format(idd.pixels / (idd.cRect.Width * idd.cRect.Height), "0%") + ")" +
                                  vbCrLf + "correlation = " + Format(idd.correlation, fmt3)
        ptDepthAndCorrelation = pt
        If standaloneTest() Then SetTrueText(DepthAndCorrelationText, ptDepthAndCorrelation, 2)
    End Sub
End Class







Public Class DepthCell_Plot : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        plot.addLabels = False
        labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
        desc = "Select any cell To plot a histogram Of that cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.dCell.dst2
        dst2.SetTo(0, Not task.iddMask)

        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If task.iddList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim idd As depthCell
        If index < 0 Or index >= task.iddList.Count Then
            idd = task.iddList(task.iddList.Count / 2)
            task.mouseMovePoint = New cv.Point(idd.cRect.X + idd.cRect.Width / 2, idd.cRect.Y + idd.cRect.Height / 2)
        Else
            idd = task.iddList(index)
        End If

        Dim split() = task.pointCloud(idd.lRect).Split()
        Dim mm = GetMinMax(split(2))

        If Math.Abs(mm.maxVal - mm.minVal) > 0 Then
            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(split(2))
            dst3 = plot.dst2
            labels(3) = "Depth values vary from " + Format(plot.minRange, fmt3) +
                            " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class DepthCell_FullDepth : Inherits TaskParent
    Public Sub New()
        labels(2) = "Left image depth cells - no overlap.  Click in any column to highlight that column."
        labels(3) = "Right image: corresponding depth cells.  Overlap indicates uncertainty about depth."
        desc = "Display the depth cells for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim col As Integer, tilesPerRow = task.dCell.grid.tilesPerRow
        Static whiteCol As Integer = tilesPerRow / 2
        If task.mouseClickFlag Then whiteCol = Math.Round(tilesPerRow * (task.ClickPoint.X - task.dCellSize / 2) / dst2.Width)
        For Each idd In task.iddList
            If idd.depth > 0 Then
                Dim color = If(col = whiteCol, cv.Scalar.Black, task.scalarColors(255 * (col / tilesPerRow)))
                dst2.Rectangle(idd.cRect, color, task.lineWidth)
                dst3.Rectangle(idd.rRect, color, task.lineWidth)
            End If
            col += 1
            If col >= tilesPerRow Then col = 0
        Next
    End Sub
End Class








Public Class DepthCell_InstantUpdate : Inherits TaskParent
    Public Sub New()
        task.dCell.instantUpdate = True
        labels(3) = "Pointcloud image for cells with good visibility"
        desc = "Create the grid of depth cells with good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have reasonable depth."

        dst2 = task.dCell.dst2
        labels(2) = task.dCell.labels(2)
        dst2.SetTo(0, Not task.iddMask)
    End Sub
End Class





Public Class DepthCell_CorrelationMask : Inherits TaskParent
    Dim corrMap As New DepthCell_CorrelationMap
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Isolate only the depth values under the depth cell correlation mask (see DepthCell_CorrelatonMap"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corrMap.Run(src)
        dst3 = corrMap.dst2

        dst2.SetTo(0)
        labels = corrMap.labels
        task.pointCloud.CopyTo(dst2, corrMap.dst3)
    End Sub
End Class






Public Class DepthCell_RGBtoLeft : Inherits TaskParent
    Public Sub New()
        labels(3) = "Right camera image..."
        desc = "Translate the RGB to left view for all cameras except Stereolabs where left is RGB."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim idd As depthCell
        If index > 0 And index < task.iddList.Count Then
            idd = task.iddList(index)
        Else
            idd = task.iddList(task.iddList.Count / 2)
        End If

        Dim irPt As cv.Point = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        Dim rgbTop = idd.cRect.TopLeft, ir3D As cv.Point3f
        ' stereolabs and orbbec already aligned the RGB and left images so depth in the left image
        ' can be found.  For Intel and the Oak-D, the left image and RGB need to be aligned to get accurate depth.
        ' With depth the correlation between the left and right for that depth cell will be accurate (if there is depth.)
        ' NOTE: the Intel camera is accurate in X but way off in Y.  Probably my problem...
        If task.cameraName.StartsWith("Intel") Or task.cameraName.StartsWith("Oak-D") Then
            Dim pcTop = task.pointCloud.Get(Of cv.Point3f)(rgbTop.Y, rgbTop.X)
            If pcTop.Z > 0 Then
                ir3D.X = camInfo.rotation(0) * pcTop.X +
                         camInfo.rotation(1) * pcTop.Y +
                         camInfo.rotation(2) * pcTop.Z + camInfo.translation(0)
                ir3D.Y = camInfo.rotation(3) * pcTop.X +
                         camInfo.rotation(4) * pcTop.Y +
                         camInfo.rotation(5) * pcTop.Z + camInfo.translation(1)
                ir3D.Z = camInfo.rotation(6) * pcTop.X +
                         camInfo.rotation(7) * pcTop.Y +
                         camInfo.rotation(8) * pcTop.Z + camInfo.translation(2)
                irPt.X = camInfo.leftIntrinsics.fx * ir3D.X / ir3D.Z + camInfo.leftIntrinsics.ppx
                irPt.Y = camInfo.leftIntrinsics.fy * ir3D.Y / ir3D.Z + camInfo.leftIntrinsics.ppy
            End If
        Else
            irPt = idd.cRect.TopLeft ' the above cameras are already have RGB aligned to the left image.
        End If
        labels(2) = "RGB point at " + rgbTop.ToString + " is at " + irPt.ToString + " in the left view "

        dst2 = task.leftView
        dst3 = task.rightView
        Dim r = New cv.Rect(irPt.X, irPt.Y, idd.cRect.Width, idd.cRect.Height)
        dst2.Rectangle(r, 255, task.lineWidth)

        dst2.Circle(r.TopLeft, task.DotSize, 255, -1)
        ' SetTrueText("Correlation " + Format(idd.correlation, fmt3), task.dCell.mouseD.pt, 2)
    End Sub
End Class







Public Class DepthCell_LeftAlign : Inherits TaskParent
    Public Sub New()
        labels(3) = "Left view locations for the top left corner of each depth cell."
        desc = "Align depth cell left rectangles in color with the left image.  StereoLabs and Orbbec already match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3.SetTo(0)
        Dim count As Integer
        For Each idd In task.iddList
            If idd.depth > 0 Then
                count += 1
                task.color.Circle(idd.cRect.TopLeft, task.DotSize, task.HighlightColor, -1)
                dst2.Circle(idd.lRect.TopLeft, task.DotSize, 255, -1)
                dst3.Circle(idd.lRect.TopLeft, task.DotSize, cv.Scalar.White, -1)
            End If
        Next
        labels(2) = CStr(count) + " depth cells have depth and therefore an equivalent in the left view."
    End Sub
End Class









Public Class DepthCell_RightView : Inherits TaskParent
    Public means As New List(Of Single)
    Public Sub New()
        labels(2) = "Draw above in the color image to see the matches in left and right images"
        labels(3) = "Right view with the translated drawrect."
        task.drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40)
        desc = "Map Each depth cell into the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView

        Dim indexTop = task.iddMap.Get(Of Integer)(task.drawRect.Y, task.drawRect.X)
        If indexTop < 0 Or indexTop >= task.iddList.Count Then Exit Sub
        Dim indexBot = task.iddMap.Get(Of Integer)(task.drawRect.BottomRight.Y, task.drawRect.BottomRight.X)
        If indexBot < 0 Or indexBot >= task.iddList.Count Then Exit Sub

        Dim idd1 = task.iddList(indexTop)
        Dim idd2 = task.iddList(indexBot)

        Dim w = idd2.lRect.BottomRight.X - idd1.lRect.TopLeft.X
        Dim h = idd2.lRect.BottomRight.Y - idd1.lRect.TopLeft.Y
        Dim rectLeft = New cv.Rect(idd1.lRect.X, idd1.lRect.Y, w, h)

        w = idd2.rRect.BottomRight.X - idd1.rRect.TopLeft.X
        h = idd2.rRect.BottomRight.Y - idd1.rRect.TopLeft.Y
        Dim rectRight = New cv.Rect(idd1.rRect.X, idd1.rRect.Y, w, h)

        dst2.Rectangle(rectLeft, 0, task.lineWidth)
        dst3.Rectangle(rectRight, 0, task.lineWidth)
    End Sub
End Class






Public Class DepthCell_LeftRightSize : Inherits TaskParent
    Public Sub New()
        desc = "Build the left and right images so they are the same size as the color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim minX As Integer = Integer.MaxValue, minY As Integer = Integer.MaxValue
        Dim maxX As Integer = Integer.MinValue, maxY As Integer = Integer.MinValue
        For Each idd In task.iddList
            If idd.depth > 0 Then
                If idd.lRect.X < minX Then minX = idd.lRect.X
                If idd.lRect.Y < minY Then minY = idd.lRect.Y
                If idd.lRect.BottomRight.X > maxX Then maxX = idd.lRect.BottomRight.X
                If idd.lRect.BottomRight.Y > maxY Then maxY = idd.lRect.BottomRight.Y
            End If
        Next

        Dim rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
        dst2 = task.leftView(rect).Resize(task.color.Size)
    End Sub
End Class






Public Class DepthCell_CorrelationMap : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display a heatmap of the correlation of the left and right images for each depth cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        dst3.SetTo(0)

        Dim minCorr = task.dCell.options.correlationThreshold
        Dim count As Integer
        For Each idd In task.iddList
            If idd.depth > 0 Then
                Dim val = (idd.correlation + 1) * 255 / 2
                dst1(idd.cRect).SetTo(val)
                If idd.correlation > minCorr Then
                    dst3(idd.cRect).SetTo(255)
                    count += 1
                End If
            End If
        Next

        task.iddCorr = ShowPalette(dst1)

        labels(2) = task.dCell.labels(2)
        labels(3) = "There were " + CStr(count) + " cells (out of " + CStr(task.iddList.Count) +
                    ") with correlation coefficient > " + Format(minCorr, fmt1)
    End Sub
End Class








Public Class DepthCell_Correlation : Inherits TaskParent
    Public Sub New()
        desc = "Given a left image cell, find it's match in the right image, and display their correlation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then Exit Sub ' settle down first...

        dst2 = task.leftView
        dst3 = task.rightView
        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If index < 0 Or index > task.iddList.Count Then Exit Sub

        Dim idd = task.iddList(index)
        Dim pt = task.dCell.mouseD.ptDepthAndCorrelation
        Dim corr = idd.correlation
        dst2.Circle(idd.lRect.TopLeft, task.DotSize, 255, -1)
        SetTrueText("Correlation " + Format(corr, fmt3), pt, 2)
        labels(3) = "Correlation of the left depth cell to the right is " + Format(corr, fmt3)

        dst2.Rectangle(idd.lRect, 255, task.lineWidth)
        dst3.Rectangle(idd.rRect, 255, task.lineWidth)
        labels(2) = "The correlation coefficient at " + pt.ToString + " is " + Format(corr, fmt3)
    End Sub
End Class