Imports cv = OpenCvSharp
Public Class IdealD_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public diMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public diMeans As New List(Of Single)
    Public depth32f As cv.Mat
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Pointcloud image for cells with ideal visibility"
        desc = "Create the grid of cells with ideal visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim emptyRect As New cv.Rect
        options.RunOpt()
        grid.Run(src)
        If task.heartBeat Then labels(2) = CStr(task.diList.Count) + " grid cells have the ideal depth."

        If task.optionsChanged Then task.diList.Clear()

        depth32f = task.pcSplit(2).Clone
        Dim camInfo = task.calibData
        Dim diListNew As New List(Of depthIdeal)
        Dim maxPixels As Single = options.cellSize * options.cellSize
        For Each di In task.diList
            If task.motionMask(di.lRect).CountNonZero Then Continue For
            Dim pixels = task.depthMask(di.lRect).CountNonZero
            If pixels / maxPixels < options.percentThreshold Then Continue For
            If di.mm.maxVal - di.mm.minVal > options.rangeThresholdmm Then Continue For

            di.age += 1
            di.depth = depth32f(di.lRect).Mean(task.depthMask(di.lRect))
            If di.depth > 0 Then
                Dim r = di.lRect
                r.X -= camInfo.baseline * camInfo.fx / di.depth
                di.rRect = r
            End If
            diListNew.Add(di)
        Next

        For Each rect In grid.gridRectsAll
            If task.motionMask(rect).CountNonZero Then Continue For
            Dim pixels = task.depthMask(rect).CountNonZero
            If pixels / maxPixels < options.percentThreshold Then Continue For
            Dim mm = GetMinMax(depth32f(rect), task.depthMask(rect))
            If mm.maxVal - mm.minVal > options.rangeThresholdmm Then Continue For
            Dim di As New depthIdeal
            di.lRect = rect
            di.depth = depth32f(di.lRect).Mean(task.depthMask(di.lRect))
            di.rRect = di.lRect
            di.rRect.X -= camInfo.baseline * camInfo.fx / di.depth

            diListNew.Add(di)
        Next

        task.diList = New List(Of depthIdeal)(diListNew)

        dst2 = src.Clone
        dst3 = task.rightView.Clone
        For Each di In task.diList
            dst2.Rectangle(di.lRect, cv.Scalar.White, task.lineWidth)
            dst3.Rectangle(di.rRect, cv.Scalar.White, task.lineWidth)
        Next

        'diMeans.Clear()
        'diMap.SetTo(0)
        'For Each di In task.diListAll
        '    diMap(di.lRect).SetTo(255)
        '    diMeans.Add(di.depth)

        '    ' duplicate the top row of the roi in all the rows of the roi
        '    For y = 1 To di.lRect.Height - 1
        '        depth32f(di.lRect).Row(0).CopyTo(depth32f(di.lRect).Row(y))
        '    Next
        'Next

        'dst3.SetTo(0)
        'task.pointCloud.CopyTo(dst3, diMap)

        'dst2 = src.Clone
        'task.diList.Clear()
        'For Each di In task.diListAll
        '    If di.age > options.cellAge Then
        '        If di.pixels / cellPixels >= options.percentThreshold Then
        '            If di.mm.maxVal - di.mm.minVal <= options.rangeThresholdmm Then
        '                If di.matched Then
        '                    task.diList.Add(di)
        '                    dst2.Rectangle(di.lRect, cv.Scalar.White, task.lineWidth)
        '                    dst3.Rectangle(di.rRect, cv.Scalar.White, task.lineWidth)
        '                End If
        '            End If
        '        End If
        '    End If
        'Next
    End Sub
End Class







Public Class IdealD_BasicsSlow : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public diMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public diMeans As New List(Of Single)
    Public depth32f As cv.Mat
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Pointcloud image for cells with ideal visibility"
        desc = "Create the grid of cells with ideal visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim emptyRect As New cv.Rect
        options.RunOpt()
        grid.Run(src)
        If task.heartBeat Then labels(2) = CStr(task.diList.Count) + " grid cells have the ideal depth."

        If task.optionsChanged Then
            task.diListAll.Clear()
            For Each r In grid.gridRectsAll
                Dim di As New depthIdeal
                di.lRect = r
                task.diListAll.Add(di)
            Next
        End If

        depth32f = task.pcSplit(2).Clone
        Dim camInfo = task.calibData
        For i = 0 To task.diListAll.Count - 1
            Dim di = task.diListAll(i)
            di.age = If(task.motionMask(di.lRect).CountNonZero > 0, 1, di.age + 1)
            di.depth = depth32f(di.lRect).Mean(task.depthMask(di.lRect))
            If di.depth > 0 Then
                di.rRect = di.lRect
                di.rRect.X -= camInfo.baseline * camInfo.fx / di.depth
                di.mm = GetMinMax(depth32f(di.lRect), task.depthMask(di.lRect))
            End If
            task.diListAll(i) = di
        Next

        diMeans.Clear()
        diMap.SetTo(0)
        For Each di In task.diListAll
            diMap(di.lRect).SetTo(255)
            diMeans.Add(di.depth)

            ' duplicate the top row of the roi in all the rows of the roi
            For y = 1 To di.lRect.Height - 1
                depth32f(di.lRect).Row(0).CopyTo(depth32f(di.lRect).Row(y))
            Next
        Next

        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, diMap)

        dst2 = src.Clone
        task.diList.Clear()
        Dim cellPixels As Single = options.cellSize * options.cellSize
        For Each di In task.diListAll
            If di.age > options.cellAge Then
                di.depth = depth32f(di.lRect).Mean(task.depthMask(di.lRect))
                Dim pixels = task.depthMask(di.lRect).CountNonZero
                If pixels / cellPixels >= options.percentThreshold Then
                    If di.mm.maxVal - di.mm.minVal <= options.rangeThresholdmm Then
                        task.diList.Add(di)
                        dst2.Rectangle(di.lRect, cv.Scalar.White, task.lineWidth)
                        dst3.Rectangle(di.rRect, cv.Scalar.White, task.lineWidth)
                    End If
                End If
            End If
        Next
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
        For Each di In task.diList
            mats.mat(3).Rectangle(di.rRect, 255, task.lineWidth)
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
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Plot a histogram of an ideal depth cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim di As depthIdeal
        For Each di In task.diList
            dst2.Rectangle(di.lRect, 255, task.lineWidth)
        Next

        Dim index = task.idealD.grid.gridMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If task.diList.Count = 0 Or task.optionsChanged Then Exit Sub

        If index = 0 Or index >= task.diList.Count Then
            di = task.diList(task.diList.Count / 2)
            task.ClickPoint = New cv.Point(di.lRect.X + di.lRect.Width / 2, di.lRect.Y + di.lRect.Height / 2)
        Else
            di = task.diList(index)
        End If

        If task.heartBeat Then
            plot.Run(task.pcSplit(2)(di.lRect))
            dst3 = plot.dst2
            labels(3) = "X values vary from " + Format(plot.minRange, fmt3) +
                        " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class IdealD_FullDepth : Inherits TaskParent
    Public idList As New List(Of depthIdeal)
    Public Sub New()
        desc = "Create the disparity rectangles for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone

        For Each di In task.diList
            dst2.Rectangle(di.lRect, 255, task.lineWidth)
            dst3.Rectangle(di.rRect, 255, task.lineWidth)
        Next
    End Sub
End Class