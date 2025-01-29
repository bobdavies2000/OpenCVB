Imports System.Dynamic
Imports System.Windows.Controls
Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class IdealD_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public gridRects As New List(Of cv.Rect)
    Public options As New Options_IdealSize
    Public depth32f As cv.Mat
    Public gridMask As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public gridMeans As New List(Of Single)
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Pointcloud image for cells with ideal visibility"
        desc = "Create the grid of cells with ideal visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim emptyRect As New cv.Rect
        options.RunOpt()
        grid.Run(src)

        Dim newRects As New List(Of cv.Rect)
        Dim cellPixels = options.cellSize * options.cellSize
        For Each roi In gridRects
            If task.motionMask(roi).CountNonZero = 0 Then
                ' make sure the cell still has depth.
                If task.pcSplit(2)(roi).CountNonZero >= options.depthThreshold * cellPixels Then
                    newRects.Add(roi)
                End If
            End If
        Next

        depth32f = task.pcSplit(2).Clone
        For Each roi In grid.gridRectsAll
            If roi.X = 0 Then Continue For ' it is unlikely that a left-hugging rect could be matched.
            If task.motionMask(roi).CountNonZero > 0 Then
                If depth32f(roi).CountNonZero >= options.depthThreshold * cellPixels Then
                    Dim mm = GetMinMax(depth32f(roi))
                    If (mm.maxVal - mm.minVal) * 100 <= options.rangeThreshold Then newRects.Add(roi)
                End If
            End If
        Next

        gridRects = New List(Of cv.Rect)(newRects)
        gridMeans.Clear()
        gridMask.SetTo(0)
        For Each roi In gridRects
            gridMask(roi).SetTo(255)
            Dim depth = depth32f(roi).Mean(task.depthMask(roi))
            gridMeans.Add(depth)

            ' duplicate the top row of the roi in all the rows of the roi
            For y = 1 To roi.Height - 1
                depth32f(roi).Row(0).CopyTo(depth32f(roi).Row(y))
            Next
        Next

        depth32f.SetTo(0, Not gridMask)
        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, gridMask)

        dst2 = src.Clone
        For Each r In gridRects
            dst2.Rectangle(r, cv.Scalar.White, task.lineWidth)
        Next
        If task.heartBeat Then labels(2) = CStr(gridRects.Count) + " grid cells have the maximum depth pixels."
    End Sub
End Class







Public Class IdealD_RightView : Inherits TaskParent
    Public means As New List(Of Single)
    Public mats As New Mat_4to1
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"Draw below to see match in right image", "Right view image", "", ""}
        labels(2) = "Left view, right view, ideal depth (left), ideal depth (right)"
        labels(3) = "Right view with ideal depth cells marked."
        task.drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40)
        desc = "Map each ideal depth cell into the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView
        mats.mat(0) = task.leftView
        mats.mat(1) = task.rightView
        Dim depths As New List(Of Single)
        Dim camInfo = task.calibData
        mats.mat(2) = task.idealD.dst2
        mats.mat(3) = task.rightView.Clone
        For Each roi In task.idealD.gridRects
            Dim mean = task.pcSplit(2)(roi).Mean(task.depthMask(roi))
            roi.X -= camInfo.baseline * camInfo.fx / mean(0)
            mats.mat(3).Rectangle(roi, 255, task.lineWidth)
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






Public Class IdealD_CellPlot : Inherits TaskParent
    Dim toDisp As New IdealD_RightView
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Reconstruction the depth using the disparity cell overlap"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        toDisp.Run(src)
        dst2 = toDisp.mats.mat(3)

        Dim index = task.idealD.grid.gridMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        Dim roi As cv.Rect
        If task.idealD.gridRects.Count = 0 Or task.optionsChanged Then Exit Sub
        If index = 0 Or index >= task.idealD.gridRects.Count Then
            roi = task.idealD.gridRects(task.idealD.gridRects.Count / 2)
            task.ClickPoint = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
        Else
            roi = task.idealD.gridRects(index)
        End If

        If task.heartBeat Then
            plot.Run(task.pcSplit(2)(roi))
            dst3 = plot.dst2
            labels(3) = "X values vary from " + Format(plot.minRange, fmt3) +
                        " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class IdealD_FullDepth : Inherits TaskParent
    Public idList As New List(Of idealData)
    Dim depth32f As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public Sub New()
        desc = "Create the disparity rectangles for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone

        If task.optionsChanged Then Exit Sub

        Dim newRects As New List(Of idealData)
        depth32f = task.pcSplit(2).Clone
        For Each id In idList
            If task.motionMask(id.lRect).CountNonZero = 0 Then
                ' make sure the cell still has depth.
                If depth32f(id.lRect).CountNonZero > 0 Then
                    id.age += 1
                    newRects.Add(id)
                End If
            End If
        Next

        Dim threshold = task.idealD.options.rangeThreshold
        Dim camInfo = task.calibData
        For Each roi In task.idealD.grid.gridRectsAll
            If task.motionMask(roi).CountNonZero > 0 Then
                If depth32f(roi).CountNonZero > 0 Then
                    Dim mm = GetMinMax(depth32f(roi))
                    If (mm.maxVal - mm.minVal) * 100 <= threshold Then
                        Dim mean = depth32f(roi).Mean(task.depthMask(roi))
                        If mean(0) > 0 Then
                            Dim id As New idealData
                            id.lRect = roi
                            roi.X -= camInfo.baseline * camInfo.fx / mean(0)
                            id.rRect = roi
                            id.age = 1
                            id.depth = mean(0)
                            newRects.Add(id)
                        End If
                    End If
                End If
            End If
        Next

        idList = New List(Of idealData)(newRects)
        For Each id In idList
            dst2.Rectangle(id.lRect, 255, task.lineWidth)
            dst3.Rectangle(id.rRect, 255, task.lineWidth)
        Next
    End Sub
End Class
