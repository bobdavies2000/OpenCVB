Imports cv = OpenCvSharp
Imports VBClasses
Public Class Brick_Basics : Inherits TaskParent
    Public instantUpdate As Boolean
    Public brickDepthCount As Integer
    Public brickList As New List(Of brickData)
    Public options As New Options_Features
    Public Sub New()
        labels(3) = "Right camera image.  Highlighted rectangle matches the dst2 (left) rectangle."
        desc = "Create the grid of bricks that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.optionsChanged Then brickList.Clear()

        Dim correlationMat As New cv.Mat
        brickList.Clear()
        Dim depthCount As Integer
        brickDepthCount = 0
        Dim colorstdev As cv.Scalar
        For i = 0 To task.gridRects.Count - 1
            Dim r As New brickData
            r.index = brickList.Count

            r.rect = task.gridRects(r.index)
            r.lRect = r.rect

            r.depth = task.pcSplit(2)(r.rect).Mean(task.depthmask(r.rect))
            r.mmDepth = GetMinMax(task.pcSplit(2)(r.rect), task.depthmask(r.rect))
            If r.depth > Single.MaxValue Or r.depth < Single.MinValue Then r.depth = 0

            cv.Cv2.MeanStdDev(src(r.rect), r.color, colorstdev)
            r.center = New cv.Point(r.rect.X + r.rect.Width / 2, r.rect.Y + r.rect.Height / 2)

            If r.depth > 0 Then
                r.mmDepth = GetMinMax(task.pcSplit(2)(r.rect), task.depthmask(r.rect))
                brickDepthCount += 1
                r.rRect = r.rect
                r.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / r.depth
                If r.rRect.X < 0 Or r.rRect.X + r.rRect.Width >= dst2.Width Then
                    r.rRect.Width = 0 ' off the image
                End If

                If r.lRect.Width = r.rRect.Width Then
                    cv.Cv2.MatchTemplate(task.leftView(r.lRect), task.rightView(r.rRect),
                                                         correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                    r.correlation = correlationMat.Get(Of Single)(0, 0)

                    Dim p0 = Cloud_Basics.worldCoordinates(r.rect.TopLeft, r.depth)
                    Dim p1 = Cloud_Basics.worldCoordinates(r.rect.BottomRight, r.depth)

                    ' clockwise around starting in upper left.
                    r.corners.Add(New cv.Point3f(p0.X, p0.Y, r.depth))
                    r.corners.Add(New cv.Point3f(p1.X, p0.Y, r.depth))
                    r.corners.Add(New cv.Point3f(p1.X, p1.Y, r.depth))
                    r.corners.Add(New cv.Point3f(p0.X, p1.Y, r.depth))
                End If
            End If

            If r.depth > 0 Then depthCount += 1
            brickList.Add(r)
        Next

        If standaloneTest() Then
            Static edgesLR As New Edge_LeftRight
            edgesLR.Run(emptyMat)
            dst2 = edgesLR.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = edgesLR.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            Dim index = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            Dim brick = brickList(index)
            dst2.Rectangle(brick.lRect, task.highlight, task.lineWidth + 1)
            task.color.Rectangle(brick.lRect, task.highlight, task.lineWidth + 1)
            dst3.Rectangle(brick.rRect, task.highlight, task.lineWidth + 1)
        End If

        If task.heartBeat Then
            labels(2) = CStr(brickList.Count) + " bricks and " +
                        CStr(brickDepthCount) + " had depth.  Left camera image is below."
        End If
    End Sub
End Class





Public Class NR_Brick_FullDepth : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        labels(2) = "Left image bricks - no overlap.  Click in any column to highlight that column."
        labels(3) = "Right image: corresponding bricks.  Overlap indicates uncertainty about depth."
        desc = "Display the bricks for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim col As Integer, bricksPerRow = task.bricksPerRow
        Static whiteCol As Integer = bricksPerRow / 2
        If task.mouseClickFlag Then
            whiteCol = Math.Round(bricksPerRow * (task.clickPoint.X - task.brickEdgeLen / 2) / dst2.Width)
        End If
        For Each brick In bricks.brickList
            If brick.depth > 0 Then
                Dim color = If(col = whiteCol, cv.Scalar.Black, task.scalarColors(255 * (col / bricksPerRow)))
                dst2.Rectangle(brick.rect, color, task.lineWidth)
                dst3.Rectangle(brick.rRect, color, task.lineWidth)
            End If
            col += 1
            If col >= bricksPerRow Then col = 0
        Next
    End Sub
End Class








Public Class NR_Brick_InstantUpdate : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        bricks.instantUpdate = True
        labels(3) = "Pointcloud image for cells with good visibility"
        desc = "Create the grid of bricks with good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        labels(2) = CStr(bricks.brickDepthCount) + " bricks have reasonable depth."

        dst2 = bricks.dst2
        dst3 = bricks.dst3

        labels(2) = bricks.labels(2)
    End Sub
End Class








Public Class NR_Brick_CorrelationInput : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim LRMeanSub As New MeanSubtraction_LeftRight
    Public Sub New()
        desc = "Given a left image cell, find it's match in the right image, and display their correlation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If task.optionsChanged Then Exit Sub ' settle down first...

        LRMeanSub.Run(src)
        dst2 = LRMeanSub.dst2
        dst3 = LRMeanSub.dst3

        Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If index < 0 Or index > bricks.brickList.Count Then Exit Sub
        task.brickD = bricks.brickList(index)

        Dim corr = task.brickD.correlation
        DrawCircle(dst2, task.brickD.lRect.TopLeft, task.DotSize, 255)
        Dim pt = New cv.Point(task.brickD.rect.X, task.brickD.rect.Y - 10)
        SetTrueText("Corr. " + Format(corr, fmt3) + vbCrLf, pt, 2)
        labels(3) = "Correlation of the left grid square to the right is " + Format(corr, fmt3)

        Dim grayScale As Integer = 128
        DrawRect(dst2, task.brickD.lRect, grayScale)
        DrawRect(dst3, task.brickD.rRect, grayScale)
        labels(2) = "The correlation coefficient at " + task.brickD.rect.TopLeft.ToString + " is " + Format(corr, fmt3)
    End Sub
End Class




Public Class NR_Brick_Info : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        task.clickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Display the info about the select grid square."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        labels(2) = bricks.labels(2)

        Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim r As brickData = bricks.brickList(index)
        dst2 = src

        strOut = labels(2) + vbCrLf + vbCrLf

        dst2.Rectangle(r.rect, task.highlight, task.lineWidth)

        strOut += CStr(index) + vbTab + "Grid ID" + vbCrLf
        strOut += CStr(r.age) + vbTab + "Age" + vbTab + vbCrLf
        strOut += Format(r.correlation, fmt3) + vbTab + "Correlation of the left image to right image" + vbCrLf
        strOut += Format(r.depth, fmt3) + vbTab + "Depth" + vbCrLf
        strOut += Format(r.mm.minVal, fmt3) + vbTab + "Depth mm.minval" + vbCrLf
        strOut += Format(r.mm.maxVal, fmt3) + vbTab + "Depth mm.maxval" + vbCrLf
        strOut += Format(r.mm.range, fmt3) + vbTab + "Depth mm.range" + vbCrLf

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class NR_Brick_LeftToColor : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        If task.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        desc = "Align grid square left rectangles in color with the left image.  StereoLabs and Orbbec already match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(0)
        Dim count As Integer
        For Each brick In bricks.brickList
            If brick.depth > 0 Then
                count += 1
                DrawCircle(task.color, brick.rect.TopLeft)
                DrawCircle(dst2, brick.lRect.TopLeft)
            End If
        Next
        labels(2) = CStr(count) + " bricks have depth and therefore an equivalent in the left and right views."
    End Sub
End Class






Public Class Brick_LeftRightMouse : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public means As New List(Of Single)
    Dim myBricks As New List(Of Integer)
    Public Sub New()
        labels(2) = "Move the mouse in the color image to see the matches in left and right images. Click to clear the rectangles."
        labels(3) = "Right view with the translated trace of bricks under the mouse."
        If task.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        If standalone Then task.gOptions.displayDst0.Checked = True
        desc = "Map the bricks from the color image into the left view and the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst0 = src
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone And task.testAllRunning Then
            Dim index As Integer = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            For i = index To index + 10
                If myBricks.Contains(i) = False Then myBricks.Add(i)
            Next
        Else
            Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            If myBricks.Contains(index) = False Then myBricks.Add(index)
        End If

        For Each index In myBricks
            Dim r = bricks.brickList(index)
            dst0.Rectangle(r.rect, task.highlight, task.lineWidth)
            dst2.Rectangle(r.lRect, task.highlight, task.lineWidth)
            dst3.Rectangle(r.rRect, task.highlight, task.lineWidth)
        Next
        If task.mouseClickFlag Then myBricks.Clear()
    End Sub
End Class






Public Class NR_Brick_RGBtoLeft : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        labels(3) = "Right camera image..."
        If task.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then
            task.gOptions.gravityPointCloud.Checked = False
        End If
        desc = "Translate the RGB to left view - only needed for the Intel RealSense cameras."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim r As brickData
        If index > 0 And index < bricks.brickList.Count Then
            r = bricks.brickList(index)
        Else
            r = bricks.brickList(bricks.brickList.Count / 2)
        End If

        Dim irPt As cv.Point = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        Dim rgbTop = r.rect.TopLeft
        ' stereolabs and orbbec already aligned the RGB and left images so depth in the left image
        ' can be found.  For Intel, the left image and RGB need to be aligned first.
        ' With depth the correlation between the left and right for that grid square will be accurate (if there is depth.)
        irPt = r.rect.TopLeft ' the above cameras are already have RGB aligned to the left image.
        labels(2) = "RGB point at " + rgbTop.ToString + " is at " + irPt.ToString + " in the left view "

        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst2.Rectangle(New cv.Rect(irPt.X, irPt.Y, r.rect.Width, r.rect.Height), task.highlight, task.lineWidth)
        dst3.Rectangle(r.rRect, task.highlight, task.lineWidth)
    End Sub
End Class






Public Class NR_Brick_CloudMaxVal : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim template As New Math_Intrinsics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Use RGB motion bricks to determine if depth has changed in any r."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If task.heartBeatLT Or task.frameCount < 3 Then task.pointCloud.CopyTo(dst2)

        Dim splitCount As Integer
        For Each brick In bricks.brickList
            If brick.depth > 0 Then
                If brick.age < 10 Then
                    Dim split() As cv.Mat = task.pointCloud(brick.rect).Split
                    split(2).SetTo(brick.mm.maxVal, task.depthmask(brick.rect))

                    cv.Cv2.Multiply(template.dst2(brick.rect), split(2), split(0))
                    split(0) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fx)

                    cv.Cv2.Multiply(template.dst3(brick.rect), split(2), split(1))
                    split(1) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fy)

                    cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(brick.rect))
                    splitCount += 1
                End If
            End If
        Next

        labels(2) = CStr(splitCount) + " bricks of " + CStr(task.gridRects.Count) + " were modified."
        If standaloneTest() Then dst3 = task.pointCloud
    End Sub
End Class






Public Class NR_Brick_CloudMean : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim template As New Math_Intrinsics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Use RGB motion bricks to determine if depth has changed in any r."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If task.heartBeatLT Or task.frameCount < 3 Then task.pointCloud.CopyTo(dst2)

        Dim splitCount As Integer
        For Each brick In bricks.brickList
            If brick.depth > 0 Then
                Dim split() As cv.Mat = task.pointCloud(brick.rect).Split
                split(2).SetTo(brick.depth, task.depthmask(brick.rect))

                cv.Cv2.Multiply(template.dst2(brick.rect), split(2), split(0))
                split(0) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fx)

                cv.Cv2.Multiply(template.dst3(brick.rect), split(2), split(1))
                split(1) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fy)

                cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(brick.rect))
                splitCount += 1
            End If
        Next

        labels(2) = CStr(splitCount) + " bricks of " + CStr(task.gridRects.Count) + " were modified."
        If standaloneTest() Then dst3 = task.pointCloud
    End Sub
End Class






Public Class NR_Brick_CloudRange : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim template As New Math_Intrinsics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Use RGB motion bricks to determine if depth has changed in any r."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If task.heartBeatLT Or task.frameCount < 3 Then task.pointCloud.CopyTo(dst2)

        Dim splitCount As Integer
        Dim newRange As Single = 0.1F
        For Each brick In bricks.brickList
            If brick.depth > 0 And brick.age = 1 Then
                If brick.mm.range > newRange Then ' if the range within a grid square is > 10 cm's, fit it within 10 cm's.
                    Dim split() As cv.Mat = task.pointCloud(brick.rect).Split
                    split(2) -= brick.mm.minVal
                    split(2) *= newRange / brick.mm.range
                    split(2) += brick.depth

                    cv.Cv2.Multiply(template.dst2(brick.rect), split(2), split(0))
                    split(0) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fx)

                    cv.Cv2.Multiply(template.dst3(brick.rect), split(2), split(1))
                    split(1) *= cv.Scalar.All(1 / task.calibData.leftIntrinsics.fy)

                    cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(brick.rect))
                    dst2(brick.rect).SetTo(0, task.noDepthMask(brick.rect))
                    splitCount += 1
                End If
            End If
        Next

        labels(2) = CStr(splitCount) + " bricks of " + CStr(task.gridRects.Count) + " were modified."
        If standaloneTest() Then dst3 = task.pointCloud
    End Sub
End Class







Public Class NR_Brick_FeaturesAndEdges : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public feat As New NR_Brick_EdgeFlips
    Public boundaryCells As New List(Of List(Of Integer))
    Public Sub New()
        labels(2) = "Gray and black regions are featureless while white has features..."
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find the boundary cells between feature and featureless cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        feat.Run(src)
        dst1 = feat.featureMask.Clone

        boundaryCells.Clear()
        For Each nabeList In task.gridNabes
            Dim grA = task.gridRects(nabeList(0))
            Dim centerType = feat.featureMask.Get(Of Byte)(grA.Y, grA.X)
            If centerType <> 0 Then
                Dim boundList = New List(Of Integer)
                Dim addFirst As Boolean = True
                For i = 1 To nabeList.Count - 1
                    Dim grB = task.gridRects(nabeList(i))
                    Dim val = feat.featureMask.Get(Of Byte)(grB.Y, grB.X)
                    If centerType <> val Then
                        If addFirst Then boundList.Add(nabeList(0)) ' first element is the center point (has features)
                        addFirst = False
                        boundList.Add(nabeList(i))
                    End If
                Next
                If boundList.Count > 0 Then boundaryCells.Add(boundList)
            End If
        Next

        dst2.SetTo(0)
        For Each nlist In boundaryCells
            For Each n In nlist
                Dim mytoggle As Integer
                Dim r = task.gridRects(n)
                Dim val = feat.featureMask.Get(Of Byte)(r.Y, r.X)
                If val > 0 Then mytoggle = 255 Else mytoggle = 128
                dst2(task.gridRects(n)).SetTo(mytoggle)
            Next
        Next
    End Sub
End Class






Public Class NR_Brick_MLColor : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim ml As New ML_Basics
    Dim bounds As New NR_Brick_FeaturesAndEdges
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        ml.buildEveryPass = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using only color from boundary neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        bounds.Run(src)
        Dim edgeMask = bounds.feat.edges.dst2

        Dim rgb32f As New cv.Mat, tmp As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

        dst1 = bounds.feat.fLessMask
        Dim trainRGB As cv.Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first r is the center one and the only r with edges.  The rest are featureless.
            Dim r = task.gridRects(nList(0))
            Dim edgePixels = edgeMask(r).FindNonZero()

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New cv.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                               cv.MatType.CV_32F, New cv.Scalar(2))
            trainRGB = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32FC3)

            For j = 1 To nList.Count - 1
                Dim grA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(grA.X * task.bricksPerRow / task.cols)
                Dim y As Integer = Math.Floor(grA.Y * task.bricksPerCol / task.rows)
                Dim val = task.lowResColor.Get(Of cv.Vec3f)(y, x)
                trainRGB.Set(Of cv.Vec3f)(j - 1, 0, val)
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                Dim val = rgb32f.Get(Of cv.Vec3f)(r.Y + pt.Y, r.X + pt.X)
                trainRGB.Set(Of cv.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
            Next

            ml.trainMats = {trainRGB}

            Dim grB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(grB)}
            ml.Run(src)

            dst1(grB) = ml.predictions.Threshold(1.5, 255, cv.ThresholdTypes.BinaryInv).
                                            ConvertScaleAbs.Reshape(1, grB.Height)
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst1)

        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                      " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class






Public Class NR_Brick_CorrelationMap : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        labels(3) = "The map to identify each r's depth."
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display a heatmap of the correlation of the left and right images for each r."
    End Sub
    Public Function ShowPaletteCorrelation(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        cv.Cv2.ApplyColorMap(input, output, task.colorMapBricks)
        Return output
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst1.SetTo(0)
        task.depthAndDepthRange = ""
        For Each brick In bricks.brickList
            dst1(brick.rect).SetTo((brick.correlation + 1) * 255 / 2)
        Next

        dst2 = ShowPaletteCorrelation(dst1)
        dst2.SetTo(0, task.noDepthMask)

        Dim pt = task.mouseMovePoint, w = task.workRes.Width, h = task.workRes.Height
        If pt.X >= 0 And pt.X < w And pt.Y >= 0 And pt.Y < h Then
            Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            task.brickD = bricks.brickList(index)
            task.depthAndDepthRange = "depth = " + Format(task.brickD.depth, fmt3) + "m ID=" +
                                          CStr(task.brickD.index) + vbCrLf + " range " +
                                          Format(task.brickD.mm.minVal, fmt1) + "-" +
                                          Format(task.brickD.mm.maxVal, fmt1) + "m, age = " +
                                          CStr(task.brickD.age) + vbCrLf +
                                          " correlation = " + Format(task.brickD.correlation, fmt3)

            Dim ptTextLoc = task.brickD.rect.TopLeft
            If ptTextLoc.X > w * 0.85 Or (ptTextLoc.Y < h * 0.15 And ptTextLoc.X > w * 0.15) Then
                ptTextLoc.X -= w * 0.15
            Else
                ptTextLoc.Y -= task.brickD.rect.Height * 3
            End If

            SetTrueText(task.depthAndDepthRange, ptTextLoc, 2)
            SetTrueText(task.depthAndDepthRange, 3)
        End If
        labels(2) = bricks.labels(2)
    End Sub
End Class






Public Class Brick_LeftRight : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public means As New List(Of Single)
    Public Sub New()
        labels(2) = "Only every other colum is shown to make it clear which bricks are being translated (can get crowded otherwise.)"
        labels(3) = "Right view with the translated bricks shown at left."
        desc = "Map the column of bricks in the color image into the left view and then to the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim colorIndex As Integer
        For i = 0 To task.bricksPerRow - 1 Step 2
            colorIndex = 0
            For j = i To task.gridRects.Count - task.bricksPerRow - 1 Step task.bricksPerRow
                Dim brick = bricks.brickList(j)
                Dim color = task.scalarColors(colorIndex)
                If brick.depth > 0 Then
                    dst2.Rectangle(brick.lRect, color, task.lineWidth)
                    dst3.Rectangle(brick.rRect, color, task.lineWidth)
                End If
                colorIndex += 1
            Next
        Next
    End Sub
End Class







Public Class NR_Brick_EdgeFlips : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public edges As New Edge_Basics
    Public featureRects As New List(Of cv.Rect)
    Public featureMask As New cv.Mat
    Public fLessMask As New cv.Mat
    Public fLessRects As New List(Of cv.Rect)
    Public Sub New()
        featureMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        fLessMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        task.fOptions.EdgeMethods.SelectedItem() = "Laplacian"
        desc = "Add edges to features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        Static stateList As New List(Of Single)
        Static lastDepth As cv.Mat = task.lowResDepth.Clone

        edges.Run(src)

        featureRects.Clear()
        fLessRects.Clear()
        featureMask.SetTo(0)
        fLessMask.SetTo(0)
        Dim flist As New List(Of Single)
        For Each r In task.gridRects
            flist.Add(If(edges.dst2(r).CountNonZero <= 1, 1, 2))
        Next

        If task.optionsChanged Or stateList.Count = 0 Then
            stateList.Clear()
            For Each n In flist
                stateList.Add(n)
            Next
        End If

        Dim flipRects As New List(Of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            stateList(i) = (stateList(i) + flist(i)) / 2
            Dim r = task.gridRects(i)
            If stateList(i) >= 1.95 Then
                featureRects.Add(r)
                featureMask(r).SetTo(255)
            ElseIf stateList(i) <= 1.05 Then
                fLessRects.Add(r)
                fLessMask(r).SetTo(255)
            Else
                flipRects.Add(r)
            End If
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        src.CopyTo(dst2, featureMask)
        src.CopyTo(dst3, featureMask)

        For Each r In flipRects
            dst2.Rectangle(r, task.highlight, task.lineWidth)
        Next

        For Each r In fLessRects
            Dim x = CInt(r.X / task.brickEdgeLen)
            Dim y = CInt(r.Y / task.brickEdgeLen)
            task.lowResDepth.Set(Of Single)(y, x, lastDepth.Get(Of Single)(y, x))
        Next
        lastDepth = task.lowResDepth.Clone
        If task.heartBeat Then
            labels(2) = CStr(fLessRects.Count) + " cells without features were found.  " +
                            "Cells that are flipping (with and without edges) are highlighted"
        End If
    End Sub
End Class







Public Class NR_Brick_Edges : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim options As New Options_LeftRightCorrelation
    Public edges As New Edge_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Add edges to features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()

        edges.Run(task.leftView)
        dst2 = edges.dst2

        Dim count As Integer
        dst3.SetTo(0)
        For Each brick As brickData In bricks.brickList
            If dst2(brick.lRect).CountNonZero And brick.rRect.Width > 0 And brick.correlation > options.correlation Then
                task.rightView(brick.rRect).CopyTo(dst3(brick.rRect))
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " of " + CStr(bricks.brickList.Count) + " rects were identified in dst3"
    End Sub
End Class




Public Class NR_Brick_Lines : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim lines As New Line_Basics_TA
    Dim options As New Options_LeftRightCorrelation
    Dim motionLeft As New Motion_Basics_TA
    Public Sub New()
        labels(2) = "The lines are for the left image."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find all the bricks that contain lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()

        motionLeft.Run(task.leftView)

        lines.motionMask = motionLeft.dst3
        lines.Run(task.leftView)
        dst2 = lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim count As Integer
        dst3.SetTo(0)
        For Each brick As brickData In bricks.brickList
            If dst2(brick.lRect).CountNonZero And brick.rRect.Width > 0 And
                        brick.correlation > options.correlation Then

                task.leftView(brick.lRect).CopyTo(dst3(brick.lRect))
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " of " + CStr(bricks.brickList.Count) +
                            " rects had an edge and a range for depth > X cm's"
    End Sub
End Class




Public Class NR_Brick_HighRange : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim lines As New Line_Basics_TA
    Dim options As New Options_LeftRightCorrelation
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Left view (stable)"
        desc = "Find all the bricks that have a high range (> X mm's)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()
        dst2 = task.leftView

        Dim count As Integer
        dst3.SetTo(0)
        For Each brick As brickData In bricks.brickList
            If brick.mm.maxVal - brick.mm.minVal > options.mmRange Then
                dst2(brick.lRect).CopyTo(dst3(brick.lRect))
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " of " + CStr(bricks.brickList.Count) +
                            " rects had a range for depth > " + CStr(options.mmRange) + " mm's"
    End Sub
End Class





Public Class NR_Brick_NoDepthLines : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim lines As New Line_Basics_TA
    Dim options As New Options_LeftRightCorrelation
    Dim motionLeft As New Motion_Basics_TA
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        labels(2) = "The lines are for the left image."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find bricks that contain lines and depth zeros."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()
        dst0 = task.leftView

        motionLeft.Run(task.leftView)

        lines.motionMask = motionLeft.dst3
        lines.Run(task.leftView)
        dst2 = lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim count As Integer
        dst3.SetTo(0)
        For Each brick In bricks.brickList
            If dst2(brick.lRect).CountNonZero And task.noDepthMask(brick.rect).CountNonZero Then
                If brick.mm.maxVal - brick.mm.minVal > options.mmRange Then
                    task.leftView(brick.lRect).CopyTo(dst3(brick.lRect))
                    count += 1
                End If
            End If
        Next
        labels(3) = CStr(count) + " of " + CStr(bricks.brickList.Count) +
                            " rects had an edge and pixels with zero depth"
    End Sub
End Class





Public Class Brick_Variability : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim depthList As New List(Of Single)
    Dim options As New Options_DiffDepth
    Public depthJumpers As New List(Of Integer)
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        OptionParent.FindSlider("Depth varies more than X mm's").Value = 30
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Visualize where bricks have highly variable depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()

        fLess.Run(task.leftView)
        dst2 = src.Clone
        dst2.SetTo(0, fLess.dst2)
        labels(2) = fLess.labels(2)

        depthList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim depth = task.pcSplit(2)(r).Mean(task.depthmask(r))(0)
            depthList.Add(depth)
        Next

        If standaloneTest() Then
            Static edges As New Edge_Canny
            edges.Run(src)
            dst3 = edges.dst2
        Else
            dst3.SetTo(0)
        End If

        If task.heartBeat Then
            Static lastDepthList = New List(Of Single)(depthList)
            depthJumpers.Clear()
            For i = 0 To depthList.Count - 1
                Dim r = task.gridRects(i)
                Dim val = fLess.dst2.Get(Of Byte)(r.Y, r.X)
                If val = 0 And depthList(i) <> 0 And lastDepthList(i) <> 0 Then
                    Dim diff = Math.Abs(depthList(i) - lastDepthList(i))
                    If diff > options.meters Then
                        dst3(task.gridRects(i)).SetTo(255)
                        depthJumpers.Add(i)
                    End If
                End If
            Next

            lastDepthList = New List(Of Single)(depthList)
            labels(3) = CStr(depthJumpers.Count) + " grid squares had depth variability > " + Format(options.meters, fmt3) + " meters"
        End If
    End Sub
End Class




Public Class Brick_Ranges : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim options As New Options_DiffDepth
    Public rangeJumpers As New List(Of Integer)
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        OptionParent.FindSlider("Depth varies more than X mm's").Value = 300
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Visualize where bricks have highly variable depth range."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayOriginal)

        bricks.Run(src)
        options.Run()

        dst2 = src.Clone
        dst2.SetTo(0, fLess.dst2)
        labels(2) = fLess.labels(2)

        If standaloneTest() Then
            Static edges As New Edge_Canny
            edges.Run(src)
            dst3 = edges.dst2
        Else
            dst3.SetTo(0)
        End If

        rangeJumpers.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim mmDepth = GetMinMax(task.pcSplit(2)(r), task.depthmask(r))
            If mmDepth.range >= options.meters Then
                rangeJumpers.Add(i)
                dst3(task.gridRects(i)).SetTo(255)
            End If
        Next

        labels(3) = CStr(rangeJumpers.Count) + " grid squares had depth range > " +
                            Format(options.meters, fmt3) + " meters"
    End Sub
End Class




Public Class Brick_Features : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        desc = "Use FeatureLess_Basics to identify bricks with good contrast."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.grayOriginal
        fLess.Run(src)

        bricks.Run(src)

        dst2 = src.Clone
        dst2.SetTo(0, fLess.dst2)
        labels(2) = fLess.labels(2)

        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim index = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim brick = bricks.brickList(index)
        dst2.Rectangle(brick.lRect, task.highlight, task.lineWidth)
        task.color.Rectangle(brick.lRect, task.highlight, task.lineWidth)
        dst3.Rectangle(brick.rRect, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class NR_Brick_Plot : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim plotHist As New PlotBar_Basics
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.addLabels = False
        plotHist.removeZeroEntry = False
        labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
        desc = "Select any cell To plot a histogram Of that cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2 = task.leftView

        Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If bricks.brickList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim brick As brickData
        If index < 0 Or index >= bricks.brickList.Count Then
            brick = bricks.brickList(bricks.brickList.Count / 2)
            task.mouseMovePoint = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
        Else
            brick = bricks.brickList(index)
        End If

        Dim split() = task.pointCloud(brick.rect).Split()
        Dim mmDepth = GetMinMax(split(2))
        If Single.IsInfinity(mmDepth.maxVal) Then Exit Sub

        Static lastMouse As cv.Point = task.mouseMovePoint
        If task.heartBeat Or lastMouse <> task.mouseMovePoint Then
            lastMouse = task.mouseMovePoint
            If Math.Abs(mmDepth.maxVal - mmDepth.minVal) > 0 Then
                plotHist.minRange = mmDepth.minVal
                plotHist.maxRange = mmDepth.maxVal
                plotHist.Run(split(2))
                dst3 = plotHist.dst2
                labels(3) = "Depth values vary from " + Format(plotHist.minRange, fmt3) +
                                    " to " + Format(plotHist.maxRange, fmt3)
            End If
        End If
    End Sub
End Class





Public Class Brick_Plot : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim plotHist As New PlotBar_Basics
    Public Sub New()
        labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
        desc = "Select any cell To plot a histogram Of that cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2 = task.leftView

        Dim index As Integer = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If bricks.brickList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim brick As brickData
        If index < 0 Or index >= bricks.brickList.Count Then
            brick = bricks.brickList(bricks.brickList.Count / 2)
            task.mouseMovePoint = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
        Else
            brick = bricks.brickList(index)
        End If

        Dim split() = task.pointCloud(brick.rect).Split()
        Dim mmDepth = GetMinMax(split(2))
        If Single.IsInfinity(mmDepth.maxVal) Then Exit Sub

        Static lastMouse As New cv.Point(0, 0)
        If lastMouse <> task.mouseMovePoint Then
            lastMouse = task.mouseMovePoint
            If Math.Abs(mmDepth.maxVal - mmDepth.minVal) > 0 Then
                plotHist.minRange = mmDepth.minVal
                plotHist.maxRange = mmDepth.maxVal
                plotHist.Run(split(2))

                Dim brickIndex = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
                task.drawRect = task.gridRects(brickIndex)

                dst3 = plotHist.dst2
                labels(3) = "Depth values vary from " + Format(plotHist.minRange, fmt3) +
                                    " to " + Format(plotHist.maxRange, fmt3)
            End If
        End If
    End Sub
End Class





Public Class Brick_Search : Inherits TaskParent
    Dim lpList As New List(Of lpData)
    Public Sub New()
        task.fOptions.MatchCorrSlider.Value = 0.8
        desc = "Search the previous image for the endpoints of the top 10 lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastImage As cv.Mat = task.gray.Clone
        Dim searchArea As cv.Mat
        Dim mm1 As mmData
        Dim mm2 As mmData
        lpList.Clear()
        dst2 = src.Clone
        Dim corrThreshold = task.fOptions.MatchCorrSlider.Value
        For i = 0 To Math.Min(10, task.lines.lpList.Count) - 1
            Dim lp = task.lines.lpList(i)
            Dim rect1 = task.gridNabeRects(lp.p1GridIndex)
            searchArea = lastImage(rect1)
            cv.Cv2.MatchTemplate(searchArea, task.gray(task.gridRects(lp.p1GridIndex)), dst1,
                                 cv.TemplateMatchModes.CCoeffNormed)
            mm1 = GetMinMax(dst1)

            If mm1.maxVal > corrThreshold Then
                Dim rect2 = task.gridNabeRects(lp.p2GridIndex)
                searchArea = lastImage(rect2)
                cv.Cv2.MatchTemplate(searchArea, task.gray(task.gridRects(lp.p2GridIndex)), dst1,
                                     cv.TemplateMatchModes.CCoeffNormed)
                mm2 = GetMinMax(dst1)

                If mm2.maxVal > corrThreshold Then
                    lp = New lpData(mm1.maxLoc, mm2.maxLoc)
                    lpList.Add(lp)
                    dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 2)
                End If
            End If
        Next

        lastImage = task.gray.Clone
    End Sub
End Class
