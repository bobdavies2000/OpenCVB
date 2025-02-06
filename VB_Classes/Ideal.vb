Imports cv = OpenCvSharp
Public Class Ideal_Basics : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public thresholdRangeZ As Single
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        task.iddMap = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        task.iddMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Mask of cells with useful depth values"
        desc = "Create the grid of depth cells that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static quadPoints() As cv.Point, rowStarts As New List(Of Integer), rowLength As Integer
        options.RunOpt()

        If task.optionsChanged Then
            task.iddSize = options.cellSize
            grid.Run(src)
            Dim val = task.iddSize
            quadPoints = {New cv.Point(0, 0), New cv.Point(0, val - 1),
                          New cv.Point(val - 1, val - 1), New cv.Point(0, val - 1)}

            rowStarts.Clear()
            task.iddListAll.Clear()
            For Each rect In grid.gridRectsAll
                Dim idd As New idealDepthData
                idd.lRect = rect
                idd.age = 0
                If rect.X = 0 Then rowStarts.Add(task.iddListAll.Count)
                task.iddListAll.Add(idd)
            Next
            rowLength = CInt(dst2.Width / task.iddSize)
        End If

        Dim colorStdev As cv.Scalar, colormean As cv.Scalar
        Dim camInfo = task.calibData
        For i = 0 To task.iddListAll.Count - 1
            Dim idd = task.iddListAll(i)
            Dim motion = task.motionMask(idd.lRect).CountNonZero
            If motion = 0 And idd.age > 0 Then
                idd.age += 1
            Else
                Dim pixelCount = task.depthMask(idd.lRect).CountNonZero
                If pixelCount / (idd.lRect.Width * idd.lRect.Height) < options.percentThreshold Then
                    idd.age = 0
                    idd.depth = 0
                Else
                    idd.age = 1
                    idd.depth = task.pcSplit(2)(idd.lRect).Mean(task.depthMask(idd.lRect))

                    idd.rRect = idd.lRect
                    idd.rRect.X -= camInfo.baseline * camInfo.fx / idd.depth

                    cv.Cv2.MeanStdDev(task.color(idd.lRect), colormean, colorStdev, task.depthMask(idd.lRect))
                    idd.color = New cv.Point3f(colormean(0), colormean(1), colormean(2))
                    idd.pcFrag = task.pointCloud(idd.lRect).Clone

                    Dim p0 = idd.pcFrag.Get(Of cv.Point3f)(0, 0)
                    Dim p1 = idd.pcFrag.Get(Of cv.Point3f)(idd.lRect.Height - 1, idd.lRect.Width - 1)

                    If p0.Z = 0 Then p0 = getWorldCoordinates(idd.lRect.TopLeft, idd.depth)
                    If p1.Z = 0 Then p1 = getWorldCoordinates(idd.lRect.BottomRight, idd.depth)

                    idd.quad.Add(New cv.Point3f(p0.X, p0.Y, idd.depth))
                    idd.quad.Add(New cv.Point3f(p1.X, p0.Y, idd.depth))
                    idd.quad.Add(New cv.Point3f(p1.X, p1.Y, idd.depth))
                    idd.quad.Add(New cv.Point3f(p0.X, p1.Y, idd.depth))
                End If
            End If
            task.iddListAll(i) = idd
        Next

        task.iddMap.SetTo(0)
        task.iddMask.SetTo(0)
        Dim count As Integer
        dst2.SetTo(0)
        For i = 0 To task.iddListAll.Count - 1
            Dim idd = task.iddListAll(i)
            If idd.depth > 0 Then
                task.iddMap(idd.lRect).SetTo(i)
                task.iddMask(idd.lRect).SetTo(255)
                dst2(idd.lRect).SetTo(idd.color)
                count += 1
            End If
        Next

        If task.heartBeat Then labels(2) = CStr(count) + " of " + CStr(task.iddListAll.Count) +
                                           " grid cells have the useful depth values."
    End Sub
End Class



'Public Class Ideal_BasicsOld : Inherits TaskParent
'    Public grid As New Grid_Rectangles
'    Public options As New Options_IdealSize
'    Public cellSize As Integer
'    Public thresholdRangeZ As Single
'    Public cellPoints() As cv.Point
'    Public Sub New()
'        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
'        task.iddMap = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
'        task.iddMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        labels(3) = "Right View image cells with ideal visibility"
'        desc = "Create the grid of cells with ideal visibility"
'    End Sub
'    Public Function rebuildTriList(idd As idealDepthData) As idealDepthData
'        Dim triList = New List(Of cv.Point3f)
'        idd.triList.Clear()
'        If idd.pcFrag Is Nothing Then idd.pcFrag = task.pointCloud(idd.lRect).Clone
'        For i = 0 To 5
'            Dim pt = cellPoints(i)
'            Dim vec = idd.pcFrag.Get(Of cv.Point3f)(pt.Y, pt.X)
'            triList.Add(vec)
'            If i = 2 Or i = 5 Then
'                idd.triList.Add(triList)
'                triList = New List(Of cv.Point3f)
'            End If
'        Next
'        Return idd
'    End Function
'    Public Sub quadCorners()
'        For i = 0 To task.iddList.Count - 1
'            Dim idd = task.iddList(i)
'            Dim topLeft = getWorldCoordinates(New cv.Point3f(idd.lRect.X, idd.lRect.Y, idd.depth))
'            Dim botRight = getWorldCoordinates(New cv.Point3f(idd.lRect.X + idd.lRect.Width,
'                                                              idd.lRect.Y + idd.lRect.Height, idd.depth))

'            idd.quad.Add(New cv.Point3f(topLeft.X, topLeft.Y, idd.depth))
'            idd.quad.Add(New cv.Point3f(botRight.X, topLeft.Y, idd.depth))
'            idd.quad.Add(New cv.Point3f(botRight.X, botRight.Y, idd.depth))
'            idd.quad.Add(New cv.Point3f(topLeft.X, botRight.Y, idd.depth))

'            task.iddList(i) = task.idealD.rebuildTriList(idd)
'        Next
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        Dim emptyRect As New cv.Rect
'        options.RunOpt()
'        cellSize = options.cellSize
'        grid.Run(src)

'        If task.optionsChanged Then
'            cellPoints = {New cv.Point(0, 0), New cv.Point(0, cellSize - 1), New cv.Point(cellSize - 1, 0),
'                          New cv.Point(0, cellSize - 1), New cv.Point(cellSize - 1, cellSize - 1),
'                          New cv.Point(cellSize - 1, 0)}
'            task.iddList.Clear()
'        End If

'        Dim depth32f = task.pcSplit(2).Clone
'        Dim camInfo = task.calibData
'        Dim idListNew As New List(Of idealDepthData)
'        Dim maxPixels As Single = options.cellSize * options.cellSize
'        For Each idd In task.iddList
'            If task.motionMask(idd.lRect).CountNonZero Then Continue For
'            idd.age += 1
'            idd.index = idListNew.Count
'            idListNew.Add(idd)
'        Next

'        Dim colorStdev As cv.Scalar, colormean As cv.Scalar
'        Dim triList As New List(Of cv.Point3f)
'        For Each rect In grid.gridRectsAll
'            If task.motionMask(rect).CountNonZero = 0 Then Continue For
'            If rect.Height <> cellSize Or rect.Width <> cellSize Then Continue For ' oddball sizes on the edge.
'            Dim pixels = task.depthMask(rect).CountNonZero
'            If pixels / maxPixels < options.percentThreshold Then Continue For

'            Dim idd As New idealDepthData
'            idd.lRect = rect
'            idd.depth = depth32f(idd.lRect).Mean(task.depthMask(idd.lRect))
'            idd.rRect = idd.lRect
'            idd.rRect.X -= camInfo.baseline * camInfo.fx / idd.depth
'            cv.Cv2.MeanStdDev(task.color(idd.lRect), colormean, colorStdev, task.depthMask(idd.lRect))
'            idd.color = New cv.Point3f(colormean(0), colormean(1), colormean(2))
'            idd.index = idListNew.Count
'            idd.pcFrag = task.pointCloud(rect).Clone

'            idListNew.Add(rebuildTriList(idd))
'        Next

'        task.iddList = New List(Of idealDepthData)(idListNew)
'        quadCorners()

'        task.iddMap.SetTo(0)
'        task.iddMask.SetTo(0)
'        For i = 0 To task.iddList.Count - 1
'            Dim idd = task.iddList(i)
'            task.iddMap(idd.lRect).SetTo(i)
'            task.iddMask(idd.lRect).SetTo(255)
'        Next

'        dst2 = src.Clone
'        dst3 = task.rightView.Clone
'        For Each idd In task.iddList
'            dst2.Rectangle(idd.lRect, cv.Scalar.White, task.lineWidth)
'            dst3.Rectangle(idd.rRect, cv.Scalar.White, task.lineWidth)
'        Next
'        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have the ideal depth."
'    End Sub
'End Class







Public Class Ideal_InstantUpdate : Inherits TaskParent
    Public grid As New Grid_Rectangles
    Public options As New Options_IdealSize
    Public iddMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public depth32f As cv.Mat
    Public iddListAll As New List(Of idealDepthData)
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels(3) = "Pointcloud image for cells with ideal visibility"
        desc = "Create the grid of cells with ideal visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim emptyRect As New cv.Rect
        options.RunOpt()
        grid.Run(src)
        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have the ideal depth."

        If task.optionsChanged Then
            iddListAll.Clear()
            For Each r In grid.gridRectsAll
                Dim idd As New idealDepthData
                idd.lRect = r
                iddListAll.Add(idd)
            Next
        End If

        depth32f = task.pcSplit(2).Clone
        Dim camInfo = task.calibData
        For i = 0 To iddListAll.Count - 1
            Dim idd = iddListAll(i)
            idd.age = If(task.motionMask(idd.lRect).CountNonZero > 0, 1, idd.age + 1)
            idd.depth = depth32f(idd.lRect).Mean(task.depthMask(idd.lRect))
            If idd.depth > 0 Then
                idd.rRect = idd.lRect
                idd.rRect.X -= camInfo.baseline * camInfo.fx / idd.depth
                idd.mm = GetMinMax(depth32f(idd.lRect), task.depthMask(idd.lRect))
            End If
            iddListAll(i) = idd
        Next

        dst2 = src.Clone
        task.iddList.Clear()
        Dim cellPixels As Single = options.cellSize * options.cellSize
        iddMap.SetTo(0)
        For Each idd In iddListAll
            idd.depth = depth32f(idd.lRect).Mean(task.depthMask(idd.lRect))
            Dim pixels = task.depthMask(idd.lRect).CountNonZero
            If pixels / cellPixels >= options.percentThreshold Then
                task.iddList.Add(idd)
                dst2.Rectangle(idd.lRect, cv.Scalar.White, task.lineWidth)
                iddMap(idd.lRect).SetTo(255, task.depthMask(idd.lRect))
            End If
        Next

        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, iddMap)
    End Sub
End Class







Public Class Ideal_RightView : Inherits TaskParent
    Public means As New List(Of Single)
    Public mats As New Mat_4Click
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"Draw below to see match in right image", "Right view image", "", ""}
        labels(2) = "Left view, right view, ideal depth (left), ideal depth (right)"
        labels(3) = "Right view with ideal depth cells marked."
        task.drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40)
        desc = "Map each ideal depth cell into the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Or
            task.cameraName = "Oak-D camera" Then
            SetTrueText("The " + task.cameraName + " left and right cameras are" + vbCrLf +
                        " not typically aligned with the RGB camera in OpenCVB." + vbCrLf)
            Exit Sub
        End If
        dst1 = task.rightView
        mats.mat(0) = task.leftView
        mats.mat(1) = task.rightView
        Dim depths As New List(Of Single)
        Dim camInfo = task.calibData
        mats.mat(2) = task.idealD.dst2
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






Public Class Ideal_CellPlot : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Select any cell to plot a histogram of that ideal cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.idealD.dst2

        Dim index = task.idealD.grid.gridMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If task.iddList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim idd As idealDepthData
        If index = 0 Or index >= task.iddList.Count Then
            idd = task.iddList(task.iddList.Count / 2)
            task.ClickPoint = New cv.Point(idd.lRect.X + idd.lRect.Width / 2, idd.lRect.Y + idd.lRect.Height / 2)
        Else
            idd = task.iddList(index)
        End If

        If task.heartBeat Then
            plot.minRange = idd.mm.minVal
            plot.maxRange = idd.mm.maxVal
            plot.Run(idd.pcFrag)
            dst3 = plot.dst2
            labels(3) = "X values vary from " + Format(plot.minRange, fmt3) +
                        " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class Ideal_FullDepth : Inherits TaskParent
    Public Sub New()
        optiBase.FindSlider("Percent Depth Threshold").Value = 25
        desc = "Display the disparity rectangles for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone

        For Each idd In task.iddList
            dst2.Rectangle(idd.lRect, 255, task.lineWidth)
            dst3.Rectangle(idd.rRect, 255, task.lineWidth)
        Next
    End Sub
End Class




Public Class Ideal_Shape : Inherits TaskParent
    Dim options As New Options_IdealShape
    Dim cellPoints() As cv.Point
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Shape the ideal depth cells using different techniques."
    End Sub
    Public Function rebuildTriList(idd As idealDepthData) As idealDepthData
        Dim triList = New List(Of cv.Point3f)
        idd.triList.Clear()
        ' If idd.pcFrag Is Nothing Then idd.pcFrag = task.pointCloud(idd.lRect).Clone
        For i = 0 To 5
            Dim pt = cellPoints(i)
            Dim vec = idd.pcFrag.Get(Of cv.Point3f)(pt.Y, pt.X)
            triList.Add(vec)
            If i = 2 Or i = 5 Then
                idd.triList.Add(triList)
                triList = New List(Of cv.Point3f)
            End If
        Next
        Return idd
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        Dim cellsize = task.iddSize

        cellPoints = {New cv.Point(0, 0), New cv.Point(0, cellsize - 1), New cv.Point(cellsize - 1, 0),
                      New cv.Point(0, cellsize - 1), New cv.Point(cellsize - 1, cellsize - 1),
                      New cv.Point(cellsize - 1, 0)}

        dst2.SetTo(0)
        Select Case options.shapeChoice
            Case 0
                ' do nothing to the depth data.
                task.pointCloud.CopyTo(dst2, task.iddMask)
            Case 1 ' Duplicate top row
                For i = 0 To task.iddList.Count - 1
                    Dim idd = task.iddList(i)
                    Dim split = idd.pcFrag.Split()
                    For y = 1 To idd.lRect.Height - 1
                        split(2).Row(0).CopyTo(split(2).Row(y))
                    Next
                    cv.Cv2.Merge(split, idd.pcFrag)
                    idd.pcFrag.CopyTo(dst2(idd.lRect))
                    task.iddList(i) = rebuildTriList(idd)
                Next
            Case 2 ' Duplicate left col
                For i = 0 To task.iddList.Count - 1
                    Dim idd = task.iddList(i)
                    Dim split = idd.pcFrag.Split()
                    For y = 1 To idd.lRect.Height - 1
                        split(2).Col(0).CopyTo(split(2).Col(y))
                    Next
                    cv.Cv2.Merge(split, idd.pcFrag)
                    idd.pcFrag.CopyTo(dst2(idd.lRect))
                    task.iddList(i) = rebuildTriList(idd)
                Next
            Case 3 ' Set cell to mean depth
                For i = 0 To task.iddList.Count - 1
                    Dim idd = task.iddList(i)
                    Dim split = idd.pcFrag.Split()
                    split(2).SetTo(idd.depth)
                    cv.Cv2.Merge(split, idd.pcFrag)
                    idd.pcFrag.CopyTo(dst2(idd.lRect))
                    task.iddList(i) = rebuildTriList(idd)
                Next
            Case 4 ' Corners at mean depth
                ' corners are now built in Ideal_Basics so nothing needs to be done here.
        End Select

        labels(2) = CStr(task.iddList.Count) + " ideal depth cells found using '" + options.shapeLabel + "'"
    End Sub
End Class