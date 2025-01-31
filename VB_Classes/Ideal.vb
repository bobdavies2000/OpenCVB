Imports cv = OpenCvSharp
Public Class Ideal_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Right View image cells with ideal visibility"
        desc = "Create the grid of cells with ideal visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim emptyRect As New cv.Rect
        options.RunOpt()
        grid.Run(src)

        If task.optionsChanged Then task.idList.Clear()

        Dim depth32f = task.pcSplit(2).Clone
        Dim camInfo = task.calibData
        Dim idListNew As New List(Of depthIdeal)
        Dim maxPixels As Single = options.cellSize * options.cellSize
        For Each id In task.idList
            If task.motionMask(id.lRect).CountNonZero Then Continue For
            id.motionFlag = False
            id.age += 1
            idListNew.Add(id)
        Next

        For Each rect In grid.gridRectsAll
            If task.motionMask(rect).CountNonZero = 0 Then Continue For
            Dim pixels = task.depthMask(rect).CountNonZero
            If pixels / maxPixels < options.percentThreshold Then Continue For
            Dim mm = GetMinMax(depth32f(rect), task.depthMask(rect))
            If mm.maxVal - mm.minVal > options.rangeThresholdmm Then Continue For

            Dim id As New depthIdeal
            id.lRect = rect
            id.depth = depth32f(id.lRect).Mean(task.depthMask(id.lRect))
            id.rRect = id.lRect
            id.rRect.X -= camInfo.baseline * camInfo.fx / id.depth
            id.motionFlag = True

            idListNew.Add(id)
        Next

        task.idList = New List(Of depthIdeal)(idListNew)

        dst2 = src.Clone
        dst3 = task.rightView.Clone
        For Each id In task.idList
            dst2.Rectangle(id.lRect, cv.Scalar.White, task.lineWidth)
            dst3.Rectangle(id.rRect, cv.Scalar.White, task.lineWidth)
        Next
        If task.heartBeat Then labels(2) = CStr(task.idList.Count) + " grid cells have the ideal depth."
    End Sub
End Class







Public Class Ideal_InstantUpdate : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public diMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
        If task.heartBeat Then labels(2) = CStr(task.idList.Count) + " grid cells have the ideal depth."

        If task.optionsChanged Then
            task.idListAll.Clear()
            For Each r In grid.gridRectsAll
                Dim id As New depthIdeal
                id.lRect = r
                task.idListAll.Add(id)
            Next
        End If

        depth32f = task.pcSplit(2).Clone
        Dim camInfo = task.calibData
        For i = 0 To task.idListAll.Count - 1
            Dim id = task.idListAll(i)
            id.age = If(task.motionMask(id.lRect).CountNonZero > 0, 1, id.age + 1)
            id.depth = depth32f(id.lRect).Mean(task.depthMask(id.lRect))
            If id.depth > 0 Then
                id.rRect = id.lRect
                id.rRect.X -= camInfo.baseline * camInfo.fx / id.depth
                id.mm = GetMinMax(depth32f(id.lRect), task.depthMask(id.lRect))
            End If
            task.idListAll(i) = id
        Next

        dst2 = src.Clone
        task.idList.Clear()
        Dim cellPixels As Single = options.cellSize * options.cellSize
        diMap.SetTo(0)
        For Each id In task.idListAll
            If id.age > options.cellAge Then
                id.depth = depth32f(id.lRect).Mean(task.depthMask(id.lRect))
                Dim pixels = task.depthMask(id.lRect).CountNonZero
                If pixels / cellPixels >= options.percentThreshold Then
                    If id.mm.maxVal - id.mm.minVal <= options.rangeThresholdmm Then
                        task.idList.Add(id)
                        dst2.Rectangle(id.lRect, cv.Scalar.White, task.lineWidth)
                        diMap(id.lRect).SetTo(255, task.depthMask(id.lRect))
                    End If
                End If
            End If
        Next

        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, diMap)
    End Sub
End Class







Public Class Ideal_RightView : Inherits TaskParent
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
        For Each id In task.idList
            mats.mat(3).Rectangle(id.rRect, 255, task.lineWidth)
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






Public Class Ideal_CellPlot : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Plot a histogram of an ideal depth cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim id As depthIdeal
        For Each id In task.idList
            dst2.Rectangle(id.lRect, 255, task.lineWidth)
        Next

        Dim index = task.idealD.grid.gridMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If task.idList.Count = 0 Or task.optionsChanged Then Exit Sub

        If index = 0 Or index >= task.idList.Count Then
            id = task.idList(task.idList.Count / 2)
            task.ClickPoint = New cv.Point(id.lRect.X + id.lRect.Width / 2, id.lRect.Y + id.lRect.Height / 2)
        Else
            id = task.idList(index)
        End If

        If task.heartBeat Then
            plot.Run(task.pcSplit(2)(id.lRect))
            dst3 = plot.dst2
            labels(3) = "X values vary from " + Format(plot.minRange, fmt3) +
                        " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class Ideal_FullDepth : Inherits TaskParent
    Public Sub New()
        desc = "Display the disparity rectangles for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone

        For Each id In task.idList
            dst2.Rectangle(id.lRect, 255, task.lineWidth)
            dst3.Rectangle(id.rRect, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class Ideal_ShapeTopRow : Inherits TaskParent
    Public idMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public idMask As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public idMeans As New List(Of Single)
    Public depth32f As cv.Mat
    Public shapeChoice As New Ideal_Shape
    Public Sub New()
        desc = "Shape the ideal depth cells using different techniques."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        idMeans.Clear()
        idMap.SetTo(0)
        For i = 0 To task.idList.Count - 1
            Dim id = task.idList(i)
            idMap(id.lRect).SetTo(i)
            idMask(id.lRect).SetTo(255)
            idMeans.Add(id.depth)
        Next

        shapeChoice.Run(task.pcSplit(2))
        depth32f = shapeChoice.dst2

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), depth32f}, dst3)
        dst3.SetTo(0, Not idMask)
    End Sub
End Class





Public Class Ideal_Shape : Inherits TaskParent
    Dim options As New Options_IdealShape
    Public Sub New()
        task.idOutline = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Modify the depth32f input with the selected options"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = src
        Select Case options.shapeChoice
            Case 0 ' Duplicate top row
                For Each id In task.idList
                    ' duplicate the top row of the roi in all the rows of the roi
                    For y = 1 To id.lRect.Height - 1
                        dst2(id.lRect).Row(0).CopyTo(dst2(id.lRect).Row(y))
                    Next
                Next
            Case 1 ' Cell outline
                If task.ogl.options.pcBufferCount <> 1 Then
                    optiBase.FindSlider("OpenCVB OpenGL buffer count").Value = 1
                End If
                ' create a mask that outlines the ideal cells
                task.idOutline.SetTo(0)
                For Each id In task.idList
                    Dim r = New cv.Rect(id.lRect.X, id.lRect.Y, id.lRect.Width + 1, id.lRect.Height + 1)
                    task.idOutline.Rectangle(r, 255, 1)
                Next
                dst2.SetTo(0, Not task.idOutline)
        End Select
    End Sub
End Class
