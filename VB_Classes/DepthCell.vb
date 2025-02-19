Imports System.Dynamic
Imports cv = OpenCvSharp
Public Class DepthCell_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_DepthCellSize
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean
    Public mouseD As New DepthCell_MouseDepth
    Public quad As New Quad_Basics
    Public merge As New Quad_CellConnect
    Dim alignLeft As New depthcell_LeftAlign
    Public Sub New()
        optiBase.FindSlider("Percent Depth Threshold").Value = 25
        desc = "Create the grid of depth cells that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        task.iddSize = options.cellSize
        grid.Run(src)

        If task.optionsChanged Or instantUpdate Then
            task.iddList.Clear()
            For Each rect In grid.gridRectsAll
                Dim idd As New depthCell
                idd.cRect = ValidateRect(rect)
                idd.lRect = ValidateRect(rect) ' for some cameras the color image and the left image are the same.
                Dim cellSize = task.dCell.options.cellSize
                idd.center = New cv.Point(rect.TopLeft.X + cellSize / 2, rect.TopLeft.Y + cellSize / 2)
                idd.age = 0
                task.iddList.Add(idd)
            Next
        End If

        Dim colorStdev As cv.Scalar, colormean As cv.Scalar
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        Dim updateRightRect As Boolean
        If task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec") Then
            updateRightRect = True
        End If
        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            Dim motion = task.motionMask(idd.cRect).CountNonZero
            If motion = 0 And idd.age > 0 Then
                idd.age += 1
            Else
                cv.Cv2.MeanStdDev(src(idd.cRect), colormean, colorStdev)
                idd.color = New cv.Point3f(colormean(0), colormean(1), colormean(2))

                idd.pixels = task.depthMask(idd.cRect).CountNonZero
                If idd.pixels / (idd.cRect.Width * idd.cRect.Height) < options.percentThreshold Then
                    idd.age = 0
                    idd.depth = 0
                    idd.correlation = 0
                    idd.rRect = New cv.Rect
                Else
                    idd.age = 1
                    idd.depth = task.pcSplit(2)(idd.cRect).Mean(task.depthMask(idd.cRect))(0)
                    If updateRightRect Then
                        idd.rRect = idd.cRect
                        If idd.depth > 0 Then
                            idd.rRect.X -= camInfo.baseline * camInfo.rgbIntrinsics.fx / idd.depth
                            idd.rRect = ValidateRect(idd.rRect)
                            cv.Cv2.MatchTemplate(task.leftView(idd.cRect), task.rightView(idd.rRect), correlationMat,
                                                 cv.TemplateMatchModes.CCoeffNormed)

                            idd.correlation = correlationMat.Get(Of Single)(0, 0)
                        Else
                            idd.rRect = New cv.Rect
                        End If
                    End If
                End If
            End If
            task.iddList(i) = idd
        Next

        quad.Run(src)
        dst2 = quad.dst2

        merge.Run(src)
        dst3 = merge.dst2

        alignLeft.Run(src) ' update the task.iddlist with the correct left and right rectangles in left/right views.

        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have the useful depth values."
    End Sub
End Class





Public Class DepthCell_MouseDepth : Inherits TaskParent
    Public pt As New cv.Point
    Public ptReal As New cv.Point
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
        pt = idd.cRect.TopLeft
        If pt.X > dst2.Width * 0.85 Or (pt.Y < dst2.Height * 0.15 And pt.X > dst2.Width * 0.15) Then
            pt.X -= dst2.Width * 0.15
        Else
            pt.Y -= idd.cRect.Height * 2
        End If
        strOut = Format(idd.depth, fmt3) + "m (" + Format(idd.pixels / (idd.cRect.Width * idd.cRect.Height), "0%") + ")"

        If standaloneTest() Then SetTrueText(strOut, pt, 2)
    End Sub
End Class









Public Class DepthCell_RightView : Inherits TaskParent
    Public means As New List(Of Single)
    Public mats As New Mat_4Click
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"Draw below To see match In right image", "Right view image", "", ""}
        labels(2) = "Left view, Right view, Depth cell (left), depth cell (right)"
        labels(3) = "Right view With depth cells marked."
        task.drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40)
        optiBase.FindSlider("Depth Cell Size").Value = 8
        optiBase.FindSlider("Percent Depth Threshold").Value = 100
        desc = "Map Each depth cell into the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435I" Or
            task.cameraName = "Oak-D camera" Then
            SetTrueText("The " + task.cameraName + " left And right cameras are" + vbCrLf +
                        "Not typically aligned With the RGB camera In OpenCVB." + vbCrLf +
                        "Review the DepthCell_RGBAlignLeft to align RGB and left images.")
            Exit Sub
        End If
        dst1 = task.rightView
        mats.mat(0) = task.leftView
        mats.mat(1) = task.rightView
        Dim depths As New List(Of Single)
        Dim camInfo = task.calibData
        mats.mat(2) = task.dCell.dst2
        mats.mat(3) = task.rightView.Clone
        For Each idd In task.iddList
            mats.mat(3).Rectangle(idd.rRect, 255, task.lineWidth)
        Next

        If task.drawRect.Width > 0 Then
            mats.mat(0).Rectangle(task.drawRect, 255, task.lineWidth)
            mats.mat(1).Rectangle(task.drawRect, 255, task.lineWidth)
            Static dw As cv.Rect = task.drawRect
            Static disparities As New List(Of Single)
            If task.drawRect.X <> dw.X Or task.drawRect.Y <> dw.Y Then
                disparities.Clear()
                dw = task.drawRect
            End If
            Dim depth = task.pcSplit(2)(dw).Mean(task.depthMask(dw))
            If depth(0) > 0 Then
                Dim disp = 0.12 * camInfo.leftIntrinsics.fx / depth(0)
                disparities.Add(disp)
                Dim rect = dw
                rect.X -= disparities.Average
                mats.mat(1).Rectangle(rect, 255, task.lineWidth)
            End If
            If disparities.Count > 20 Then disparities.RemoveAt(0)
        End If

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.mat(3)
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
        optiBase.FindSlider("Depth Cell Size").Value = 12
        desc = "Display the disparity rectangles for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone

        For Each idd In task.iddList
            If idd.depth > 0 Then
                dst2.Rectangle(idd.cRect, 255, task.lineWidth)
                dst3.Rectangle(idd.rRect, 255, task.lineWidth)
            End If
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

        dst2 = ShowPalette(dst1)

        Dim index = task.dCell.grid.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If index > 0 And index < task.iddList.Count Then
            Dim idd = task.iddList(index)
            dst2.Circle(idd.cRect.TopLeft, task.DotSize, task.HighlightColor, -1)
            SetTrueText("Correlation " + Format(idd.correlation, fmt3), task.dCell.mouseD.pt, 2)
        End If

        labels(2) = task.dCell.labels(2)
        labels(3) = "There were " + CStr(count) + " cells (out of " + CStr(task.iddList.Count) +
                    ") with correlation coefficient > " + Format(minCorr, fmt1)
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
        Dim pt = task.dCell.mouseD.pt
        Dim corr = idd.correlation
        dst2.Circle(idd.cRect.TopLeft, task.DotSize, 255, -1)
        SetTrueText("Correlation " + Format(corr, fmt3), pt, 2)
        labels(3) = "Correlation of the left depth cell to the right is " + Format(corr, fmt3)

        dst2.Rectangle(idd.cRect, 255, task.lineWidth)
        dst3.Rectangle(idd.rRect, 255, task.lineWidth)
        labels(2) = "The correlation coefficient at " + pt.ToString + " is " + Format(corr, fmt3)
    End Sub
End Class






Public Class DepthCell_LeftAlign : Inherits TaskParent
    Public Sub New()
        desc = "Align depth cell left rectangles in color with the left image.  StereoLabs and Orbbec already match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = task.leftView.Clone
            If task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec") Then
                For Each idd In task.iddList
                    dst2.Circle(idd.cRect.TopLeft, task.DotSize, 255, -1)
                Next
                Exit Sub
            End If
        Else
            If task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec") Then Exit Sub
        End If

        Dim ir3D As cv.Point3f, irPt As cv.Point2f
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            Dim pcTop = task.pointCloud.Get(Of cv.Point3f)(idd.cRect.Y, idd.cRect.X)
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
                idd.lRect = New cv.Rect(irPt.X, irPt.Y, idd.cRect.Width, idd.cRect.Height)
                idd.rRect = idd.lRect
                idd.rRect.X -= camInfo.baseline * camInfo.leftIntrinsics.fx / pcTop.Z
                If standaloneTest() Then dst2.Circle(idd.rRect.TopLeft, task.DotSize, 255, -1)
                task.iddList(i) = idd
            End If
        Next
    End Sub
End Class
