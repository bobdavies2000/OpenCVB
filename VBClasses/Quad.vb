Imports cv = OpenCvSharp
Public Class Quad_RightView : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a grayscale low resolution quad view of the right image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim colorStdev As cv.Scalar, colorMean As cv.Scalar
        For Each rect In task.gridRects
            cv.Cv2.MeanStdDev(task.rightView(rect), colorMean, colorStdev)
            dst2(rect).SetTo(colorMean)
        Next
    End Sub
End Class








Public Class Quad_GridTiles : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        labels = {"", "RedColor cells", "", "Simplified depth map with RedColor cell colors"}
        desc = "Simplify the OpenGL quads without using OpenGL's point size"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        quadData.Clear()
        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim vec As cv.Scalar
        Dim shift As cv.Point3f
        If Not standalone Then
            Dim ptM = task.ogl.options4.moveAmount
            shift = New cv.Point3f(ptM(0), ptM(1), ptM(2))
        End If
        For Each roi In task.gridRects
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If standaloneTest() Then dst3(roi).SetTo(c)
            If c = black Then Continue For

            quadData.Add(New cv.Vec3f(c(0), c(1), c(2)))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y + roi.Height, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))
            If standaloneTest() Then dst1(roi).SetTo(v)
        Next
    End Sub
End Class








Public Class Quad_MinMax : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public depthList1 As New List(Of List(Of Single))
    Public depthList2 As New List(Of List(Of Single))
    Public colorList As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        desc = "Create a representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        oglOptions.Run()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        If task.optionsChanged Then
            depthList1 = New List(Of List(Of Single))
            depthList2 = New List(Of List(Of Single))
            For i = 0 To task.gridRects.Count
                depthList1.Add(New List(Of Single))
                depthList2.Add(New List(Of Single))
                colorList.Add(black)
            Next
        End If

        quadData.Clear()
        dst3.SetTo(0)

        Dim depth32f As cv.Mat = task.pcSplit(2) * 1000, depth32s As New cv.Mat
        depth32f.ConvertTo(depth32s, cv.MatType.CV_32S)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.redC.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Or index >= task.redC.rcList.Count Then
                depthList1(i).Clear()
                depthList2(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.redC.rcList(index)
            If rc.depth = 0 Then Continue For

            If colorList(i) <> rc.color Then
                depthList1(i).Clear()
                depthList2(i).Clear()
            End If

            Dim mm = GetMinMax(depth32s(roi), task.depthMask(roi))
            depthList1(i).Add(mm.minVal / 1000)
            depthList2(i).Add(mm.maxVal / 1000)
            colorList(i) = rc.color

            Dim d1 = depthList1(i).Average
            Dim d2 = depthList2(i).Average

            Dim depthCount = If(d1 = d2, 1, 2)
            For j = 0 To depthCount - 1
                Dim depth = Choose(j + 1, d1, d2)
                Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                Dim color = rc.color
                dst3(roi).SetTo(color)
                quadData.Add(New cv.Point3f(color(0), color(1), color(2)))
                quadData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
            Next
            SetTrueText(Format(d1, fmt1) + vbCrLf + Format(d2, fmt1), New cv.Point(roi.X, roi.Y), 3)

            If depthList1(i).Count >= depthListMaxCount Then depthList1(i).RemoveAt(0)
            If depthList2(i).Count >= depthListMaxCount Then depthList2(i).RemoveAt(0)
        Next
        labels(2) = traceName + " completed with " + Format(quadData.Count / 5, fmt0) +
                                " quad sets (with a 5th element for color)"
    End Sub
End Class








Public Class Quad_Hulls : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public depthList As New List(Of List(Of Single))
    Public colorList As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Dim hulls As New RedColor_Hulls
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        desc = "Create a triangle representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglOptions.Run()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        hulls.Run(src)
        dst2 = hulls.dst2

        If task.optionsChanged Then
            depthList = New List(Of List(Of Single))
            For i = 0 To task.gridRects.Count
                depthList.Add(New List(Of Single))
                colorList.Add(black)
            Next
        End If

        quadData.Clear()
        dst3.SetTo(0)

        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.redC.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then
                depthList(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.redC.rcList(index)
            If rc.depth = 0 Then Continue For

            If colorList(i) <> rc.color Then depthList(i).Clear()

            depthList(i).Add(rc.depth)
            colorList(i) = rc.color

            If depthList(i).Count > 0 Then
                dst3(roi).SetTo(colorList(i))

                Dim depth = depthList(i).Average
                Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                quadData.Add(New cv.Point3f(rc.color(0), rc.color(1), rc.color(2)))
                quadData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                quadData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))

                If depthList(i).Count >= depthListMaxCount Then depthList(i).RemoveAt(0)
            End If
        Next
        labels(2) = traceName + " completed with " + Format(quadData.Count / 5, fmt0) +
                                " quad sets (with a 5th element for color)"
    End Sub
End Class








Public Class Quad_Bricks : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public depths As New List(Of Single)
    Public options As New Options_OpenGLFunctions
    Dim depthMinList As New List(Of List(Of Single))
    Dim depthMaxList As New List(Of List(Of Single))
    Dim myListMax = 10
    Public Sub New()
        desc = "Create triangles from each brick in point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        If task.optionsChanged Then
            depthMinList.Clear()
            depthMaxList.Clear()
            For i = 0 To task.gridRects.Count - 1
                depthMinList.Add(New List(Of Single))
                depthMaxList.Add(New List(Of Single))
            Next
        End If

        options.Run()
        Dim ptM = options.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        quadData.Clear()
        dst2 = runRedC(src, labels(2))

        Dim min(4 - 1) As cv.Point3f, max(4 - 1) As cv.Point3f
        depths.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim index = task.redC.rcMap.Get(Of Byte)(center.Y, center.X)
            Dim depthMin As Single = 0, depthMax As Single = 0, minLoc As cv.Point, maxLoc As cv.Point
            If index >= 0 And task.redC.rcList.Count > 0 Then
                task.pcSplit(2)(roi).MinMaxLoc(depthMin, depthMax, minLoc, maxLoc, task.depthMask(roi))
                Dim rc = task.redC.rcList(index)
                depthMin = If(depthMax > rc.depth, rc.depth, depthMin)

                If depthMin > 0 And depthMax > 0 And depthMax < task.MaxZmeters Then
                    depthMinList(i).Add(depthMin)
                    depthMaxList(i).Add(depthMax)

                    depthMin = depthMinList(i).Average
                    Dim avg = depthMaxList(i).Average - depthMin
                    depthMax = depthMin + If(avg < 0.2, avg, 0.2) ' trim the max depth - often unreliable 
                    Dim color = rc.color
                    quadData.Add(New cv.Point3f(color(2) / 255, color(1) / 255, color(0) / 255))
                    For j = 0 To 4 - 1
                        Dim x = Choose(j + 1, roi.X, roi.X + roi.Width, roi.X + roi.Width, roi.X)
                        Dim y = Choose(j + 1, roi.Y, roi.Y, roi.Y + roi.Height, roi.Y + roi.Height)
                        min(j) = getWorldCoordinates(New cv.Point3f(x, y, depthMin))
                        max(j) = getWorldCoordinates(New cv.Point3f(x, y, depthMax))
                        min(j) += shift
                        quadData.Add(min(j))
                    Next

                    For j = 0 To 4 - 1
                        max(j) += shift
                        quadData.Add(max(j))
                    Next

                    quadData.Add(max(0))
                    quadData.Add(min(0))
                    quadData.Add(min(1))
                    quadData.Add(max(1))

                    quadData.Add(max(0))
                    quadData.Add(min(0))
                    quadData.Add(min(3))
                    quadData.Add(max(3))

                    quadData.Add(max(1))
                    quadData.Add(min(1))
                    quadData.Add(min(2))
                    quadData.Add(max(2))

                    quadData.Add(max(2))
                    quadData.Add(min(2))
                    quadData.Add(min(3))
                    quadData.Add(max(3))

                    SetTrueText(Format(depthMin, fmt1) + vbCrLf + Format(depthMax, fmt1), New cv.Point(roi.X, roi.Y))
                    If depthMinList(i).Count >= myListMax Then depthMinList(i).RemoveAt(0)
                    If depthMaxList(i).Count >= myListMax Then depthMaxList(i).RemoveAt(0)
                End If
                depths.Add(depthMin)
                depths.Add(depthMax)
            End If
        Next
        labels(2) = traceName + " completed: " + Format(task.gridRects.Count, fmt0) + " ROI's produced " +
                                Format(quadData.Count / 25, fmt0) + " six sided bricks with color"
        SetTrueText("There should be no 0.0 values in the list of min and max depths in the dst2 image.", 3)
    End Sub
End Class





Public Class Quad_Boundaries : Inherits TaskParent
    Public Sub New()
        task.brickRunFlag = True
        labels(2) = "Depth differences large enough to label them boundaries"
        desc = "Find large differences in depth between cells that could provide boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.bricks.dst2.Clone
        Dim width = dst2.Width / task.brickSize
        Dim height = dst2.Height / task.brickSize
        For i = 0 To task.bricks.brickList.Count - width Step width
            For j = i + 1 To i + width - 1
                Dim d1 = task.bricks.brickList(j).depth
                Dim d2 = task.bricks.brickList(j - 1).depth
                If Math.Abs(d1 - d2) > task.depthDiffMeters Then
                    dst2.Rectangle(task.bricks.brickList(j).rect, task.highlight, -1)
                End If
            Next
        Next

        For i = 0 To width - 1
            For j = 1 To height - 1
                Dim d1 = task.bricks.brickList(j * width).depth
                Dim d2 = task.bricks.brickList((j - 1) * width).depth
                If Math.Abs(d1 - d2) > task.depthDiffMeters Then
                    dst2.Rectangle(task.bricks.brickList(j).rect, task.highlight, -1)
                End If
            Next
        Next
    End Sub
End Class