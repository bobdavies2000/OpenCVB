Imports cv = OpenCvSharp
Public Class DepthCell_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_DepthCellSize
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean
    Public mouseD As New DepthCell_MouseDepth
    Public quad As New Quad_Basics
    Public merge As New Quad_CellConnect
    Public Sub New()
        optiBase.FindSlider("Percent Depth Threshold").Value = 25
        desc = "Create the grid of depth cells that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Or instantUpdate Then
            task.iddSize = options.cellSize
            grid.Run(src)
            task.iddList.Clear()
            For Each rect In grid.gridRectsAll
                If rect.Width <> task.iddSize Or rect.Height <> task.iddSize Then Continue For
                Dim idd As New depthCell
                idd.lRect = rect
                Dim cellSize = task.dCell.options.cellSize
                idd.center = New cv.Point(rect.TopLeft.X + cellSize / 2, rect.TopLeft.Y + cellSize / 2)
                idd.age = 0
                task.iddList.Add(idd)
            Next
        End If

        Dim colorStdev As cv.Scalar, colormean As cv.Scalar
        Dim camInfo = task.calibData
        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            Dim motion = task.motionMask(idd.lRect).CountNonZero
            If motion = 0 And idd.age > 0 Then
                idd.age += 1
            Else
                cv.Cv2.MeanStdDev(src(idd.lRect), colormean, colorStdev)
                idd.color = New cv.Point3f(colormean(0), colormean(1), colormean(2))

                Dim pixelCount = task.depthMask(idd.lRect).CountNonZero
                If pixelCount / (idd.lRect.Width * idd.lRect.Height) < options.percentThreshold Then
                    idd.age = 0
                    idd.depth = 0
                Else
                    idd.age = 1
                    idd.depth = task.pcSplit(2)(idd.lRect).Mean(task.depthMask(idd.lRect))(0)
                    If idd.depth > task.MaxZmeters Then idd.depth = task.MaxZmeters
                    idd.rRect = idd.lRect
                    idd.rRect.X -= camInfo.baseline * camInfo.fx / idd.depth
                End If
                idd.pcFrag = task.pointCloud(idd.lRect).Clone
            End If
            task.iddList(i) = idd
        Next

        quad.Run(src)
        dst2 = quad.dst2

        merge.Run(src)
        dst3 = merge.dst2

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
        ptReal = idd.center
        pt = idd.center
        If pt.X > dst2.Width * 0.85 Then pt.X -= dst2.Width * 0.15
        If pt.Y < dst2.Height * 0.1 Then pt.Y += dst2.Height * 0.03 Else pt.Y -= idd.lRect.Height * 2
        strOut = "Depth = " + Format(idd.depth, fmt3)
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
                        " Not typically aligned With the RGB camera In OpenCVB." + vbCrLf)
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
                Dim disp = 0.12 * camInfo.fx / depth(0)
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

        Dim index = task.dCell.grid.gridMap.Get(Of Byte)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If task.iddList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim idd As depthCell
        If index < 0 Or index >= task.iddList.Count Then
            idd = task.iddList(task.iddList.Count / 2)
            task.mouseMovePoint = New cv.Point(idd.lRect.X + idd.lRect.Width / 2, idd.lRect.Y + idd.lRect.Height / 2)
        Else
            idd = task.iddList(index)
        End If

        Dim split() = idd.pcFrag.Split()
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
                dst2.Rectangle(idd.lRect, 255, task.lineWidth)
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