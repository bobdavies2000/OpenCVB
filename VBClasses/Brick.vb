Imports System.Windows.Forms.Design.AxImporter
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Brick_Basics : Inherits TaskParent
        Public instantUpdate As Boolean
        Public brickDepthCount As Integer
        Public brickList As New List(Of brickData)
        Public options As New Options_Features
        Public Sub New()
            atask.bricks = Me
            labels(3) = "Right camera image.  Highlighted rectangle matches the dst2 (left) rectangle."
            desc = "Create the grid of bricks that reduce depth volatility"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If atask.bricks.brickList.Count <> atask.gridRects.Count Then atask.bricks.brickList.Clear()

            Dim correlationMat As New cv.Mat
            Dim maxPixels = atask.brickSize * atask.brickSize
            atask.bricks.brickList.Clear()
            Dim depthCount As Integer
            brickDepthCount = 0
            Dim colorstdev As cv.Scalar
            For i = 0 To atask.gridRects.Count - 1
                Dim gr As New brickData
                gr.index = atask.bricks.brickList.Count

                gr.rect = atask.gridRects(gr.index)
                gr.lRect = gr.rect

                gr.depth = atask.pcSplit(2)(gr.rect).Mean(atask.depthmask(gr.rect))
                gr.mmDepth = GetMinMax(atask.pcSplit(2)(gr.rect), atask.depthmask(gr.rect))
                If gr.depth > Single.MaxValue Or gr.depth < Single.MinValue Then gr.depth = 0

                cv.Cv2.MeanStdDev(src(gr.rect), gr.color, colorstdev)
                gr.center = New cv.Point(gr.rect.X + gr.rect.Width / 2, gr.rect.Y + gr.rect.Height / 2)

                If gr.depth > 0 Then
                    gr.mm = GetMinMax(atask.pcSplit(2)(gr.rect), atask.depthmask(gr.rect))
                    brickDepthCount += 1
                    gr.rRect = gr.rect
                    gr.rRect.X -= atask.calibData.baseline * atask.calibData.leftIntrinsics.fx / gr.depth
                    If gr.rRect.X < 0 Or gr.rRect.X + gr.rRect.Width >= dst2.Width Then
                        gr.rRect.Width = 0 ' off the image
                    End If

                    If gr.lRect.Width = gr.rRect.Width Then
                        cv.Cv2.MatchTemplate(atask.leftView(gr.lRect), atask.rightView(gr.rRect),
                                                     correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                        gr.correlation = correlationMat.Get(Of Single)(0, 0)

                        Dim p0 = Cloud_Basics.worldCoordinates(gr.rect.TopLeft, gr.depth)
                        Dim p1 = Cloud_Basics.worldCoordinates(gr.rect.BottomRight, gr.depth)

                        ' clockwise around starting in upper left.
                        gr.corners.Add(New cv.Point3f(p0.X, p0.Y, gr.depth))
                        gr.corners.Add(New cv.Point3f(p1.X, p0.Y, gr.depth))
                        gr.corners.Add(New cv.Point3f(p1.X, p1.Y, gr.depth))
                        gr.corners.Add(New cv.Point3f(p0.X, p1.Y, gr.depth))
                    End If
                End If

                If gr.depth > 0 Then depthCount += 1
                atask.bricks.brickList.Add(gr)
            Next

            If standaloneTest() Then
                Static edgesLR As New Edge_LeftRight
                edgesLR.Run(emptyMat)
                dst2 = edgesLR.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                dst3 = edgesLR.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

                Dim index = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
                Dim br = atask.bricks.brickList(index)
                dst2.Rectangle(br.lRect, atask.highlight, atask.lineWidth + 1)
                atask.color.Rectangle(br.lRect, atask.highlight, atask.lineWidth + 1)
                dst3.Rectangle(br.rRect, atask.highlight, atask.lineWidth + 1)
            End If

            If atask.heartBeat Then
                labels(2) = CStr(atask.bricks.brickList.Count) + " bricks and " +
                    CStr(brickDepthCount) + " had depth.  Left camera image is below."
            End If
        End Sub
    End Class





    Public Class NR_Brick_FullDepth : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(2) = "Left image bricks - no overlap.  Click in any column to highlight that column."
            labels(3) = "Right image: corresponding bricks.  Overlap indicates uncertainty about depth."
            desc = "Display the bricks for all cells with depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = atask.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            Dim col As Integer, bricksPerRow = atask.bricksPerRow
            Static whiteCol As Integer = bricksPerRow / 2
            If atask.mouseClickFlag Then
                whiteCol = Math.Round(bricksPerRow * (atask.clickPoint.X - atask.brickSize / 2) / dst2.Width)
            End If
            For Each gr In atask.bricks.brickList
                If gr.depth > 0 Then
                    Dim color = If(col = whiteCol, cv.Scalar.Black, atask.scalarColors(255 * (col / bricksPerRow)))
                    dst2.Rectangle(gr.rect, color, atask.lineWidth)
                    dst3.Rectangle(gr.rRect, color, atask.lineWidth)
                End If
                col += 1
                If col >= bricksPerRow Then col = 0
            Next
        End Sub
    End Class








    Public Class NR_Brick_InstantUpdate : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            atask.bricks = New Brick_Basics
            atask.bricks.instantUpdate = True
            labels(3) = "Pointcloud image for cells with good visibility"
            desc = "Create the grid of bricks with good visibility"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.heartBeat Then labels(2) = CStr(atask.bricks.brickDepthCount) + " bricks have reasonable depth."

            dst2 = atask.bricks.dst2
            dst3 = atask.bricks.dst3

            labels(2) = atask.bricks.labels(2)
        End Sub
    End Class






    Public Class NR_Brick_MLColorDepth : Inherits TaskParent
        Dim ml As New ML_Basics
        Dim bounds As New Brick_FeaturesAndEdges
        Public Sub New()
            If standalone Then atask.gOptions.displayDst1.Checked = True
            ml.buildEveryPass = True
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
            desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bounds.Run(src)
            Dim edgeMask = bounds.feat.edges.dst2

            Dim rgb32f As New cv.Mat, tmp As New cv.Mat
            src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

            dst1 = bounds.feat.fLessMask
            Dim trainRGB As cv.Mat, trainDepth As cv.Mat
            For i = 0 To bounds.boundaryCells.Count - 1
                Dim nList = bounds.boundaryCells(i)

                ' the first gr is the center one and the only gr with edges.  The rest are featureless.
                Dim gr = atask.gridRects(nList(0))
                Dim edgePixels = edgeMask(gr).FindNonZero()

                ' mark the edge pixels as class 2 - others will be updated next
                ml.trainResponse = New cv.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cv.MatType.CV_32F, New cv.Scalar(2))
                trainRGB = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32FC3)
                trainDepth = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32F)

                For j = 1 To nList.Count - 1
                    Dim grA = atask.gridRects(nList(j))
                    Dim x As Integer = Math.Floor(grA.X * atask.bricksPerRow / atask.cols)
                    Dim y As Integer = Math.Floor(grA.Y * atask.bricksPerCol / atask.rows)
                    Dim val = atask.lowResColor.Get(Of cv.Vec3f)(y, x)
                    trainRGB.Set(Of cv.Vec3f)(j - 1, 0, val)
                    trainDepth.Set(Of Single)(j - 1, 0, atask.lowResDepth.Get(Of Single)(y, x))
                    ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
                Next

                ' next, add the edge pixels in the target cell - they are the feature identifiers.
                Dim index = nList.Count - 1
                For j = 0 To edgePixels.Rows - 1
                    Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                    Dim val = rgb32f(gr).Get(Of cv.Vec3f)(pt.Y, pt.X)
                    trainRGB.Set(Of cv.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
                    Dim depth = atask.pcSplit(2)(gr).Get(Of Single)(pt.Y, pt.X)
                    trainDepth.Set(Of Single)(index + j, 0, depth)
                Next

                ml.trainMats = {trainRGB, trainDepth}

                Dim grB = atask.gridRects(nList(0))
                ml.testMats = {rgb32f(grB), atask.pcSplit(2)(grB)}
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







    Public Class NR_Brick_EdgeDraw : Inherits TaskParent
        Dim regions As New Region_Contours
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            desc = "Lines can mean cells are connected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            regions.Run(src)
            dst2 = regions.dst3
            labels(2) = regions.labels(2)

            edgeline.Run(atask.grayStable)
            dst2.SetTo(cv.Scalar.White, edgeline.dst2)
        End Sub
    End Class











    Public Class Brick_CorrelationInput : Inherits TaskParent
        Dim LRMeanSub As New MeanSubtraction_LeftRight
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            desc = "Given a left image cell, find it's match in the right image, and display their correlation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.optionsChanged Then Exit Sub ' settle down first...

            LRMeanSub.Run(src)
            dst2 = LRMeanSub.dst2
            dst3 = LRMeanSub.dst3

            Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
            If index < 0 Or index > atask.bricks.brickList.Count Then Exit Sub
            atask.brickD = atask.bricks.brickList(index)

            Dim corr = atask.brickD.correlation
            DrawCircle(dst2, atask.brickD.lRect.TopLeft, atask.DotSize, 255)
            Dim pt = New cv.Point(atask.brickD.rect.TopLeft.X, atask.brickD.rect.TopLeft.Y - 10)
            SetTrueText("Corr. " + Format(corr, fmt3) + vbCrLf, pt, 2)
            labels(3) = "Correlation of the left gr to the right is " + Format(corr, fmt3)

            Dim grayScale As Integer = 128
            DrawRect(dst2, atask.brickD.lRect, grayScale)
            DrawRect(dst3, atask.brickD.rRect, grayScale)
            labels(2) = "The correlation coefficient at " + atask.brickD.rect.TopLeft.ToString + " is " + Format(corr, fmt3)
        End Sub
    End Class




    Public Class Brick_Info : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            atask.clickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
            desc = "Display the info about the select gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            labels(2) = atask.bricks.labels(2)

            Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)

            Dim gr As brickData = atask.bricks.brickList(index)
            dst2 = src

            strOut = labels(2) + vbCrLf + vbCrLf

            dst2.Rectangle(gr.rect, atask.highlight, atask.lineWidth)

            strOut += CStr(index) + vbTab + "Grid ID" + vbCrLf
            strOut += CStr(gr.age) + vbTab + "Age" + vbTab + vbCrLf
            strOut += Format(gr.correlation, fmt3) + vbTab + "Correlation of the left image to right image" + vbCrLf
            strOut += Format(gr.depth, fmt3) + vbTab + "Depth" + vbCrLf
            strOut += Format(gr.mm.minVal, fmt3) + vbTab + "Depth mm.minval" + vbCrLf
            strOut += Format(gr.mm.maxVal, fmt3) + vbTab + "Depth mm.maxval" + vbCrLf
            strOut += Format(gr.mm.range, fmt3) + vbTab + "Depth mm.range" + vbCrLf

            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class NR_Brick_LeftToColor : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            If atask.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then atask.gOptions.gravityPointCloud.Checked = False
            desc = "Align gr left rectangles in color with the left image.  StereoLabs and Orbbec already match."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3.SetTo(0)
            Dim count As Integer
            For Each gr In atask.bricks.brickList
                If gr.depth > 0 Then
                    count += 1
                    DrawCircle(atask.color, gr.rect.TopLeft)
                    DrawCircle(dst2, gr.lRect.TopLeft)
                End If
            Next
            labels(2) = CStr(count) + " bricks have depth and therefore an equivalent in the left and right views."
        End Sub
    End Class






    Public Class Brick_LeftRightMouse : Inherits TaskParent
        Public means As New List(Of Single)
        Dim myBricks As New List(Of Integer)
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(2) = "Move the mouse in the color image to see the matches in left and right images. Click to clear the rectangles."
            labels(3) = "Right view with the translated trace of bricks under the mouse."
            If atask.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then atask.gOptions.gravityPointCloud.Checked = False
            If standalone Then atask.gOptions.displayDst0.Checked = True
            desc = "Map the bricks from the color image into the left view and the right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst0 = src
            dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = atask.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            If standalone And atask.testAllRunning Then
                Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.clickPoint.Y, atask.clickPoint.X)
                For i = index To index + 10
                    If myBricks.Contains(i) = False Then myBricks.Add(i)
                Next
            Else
                Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
                If myBricks.Contains(index) = False Then myBricks.Add(index)
            End If

            For Each index In myBricks
                Dim gr = atask.bricks.brickList(index)
                dst0.Rectangle(gr.rect, atask.highlight, atask.lineWidth)
                dst2.Rectangle(gr.lRect, atask.highlight, atask.lineWidth)
                dst3.Rectangle(gr.rRect, atask.highlight, atask.lineWidth)
            Next
            If atask.mouseClickFlag Then myBricks.Clear()
        End Sub
    End Class






    Public Class NR_Brick_RGBtoLeft : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(3) = "Right camera image..."
            If atask.Settings.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then
                atask.gOptions.gravityPointCloud.Checked = False
            End If
            desc = "Translate the RGB to left view - only needed for the Intel RealSense cameras."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim camInfo = atask.calibData, correlationMat As New cv.Mat
            Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
            Dim gr As brickData
            If index > 0 And index < atask.bricks.brickList.Count Then
                gr = atask.bricks.brickList(index)
            Else
                gr = atask.bricks.brickList(atask.bricks.brickList.Count / 2)
            End If

            Dim irPt As cv.Point = New cv.Point(dst2.Width / 2, dst2.Height / 2)
            Dim rgbTop = gr.rect.TopLeft
            ' stereolabs and orbbec already aligned the RGB and left images so depth in the left image
            ' can be found.  For Intel, the left image and RGB need to be aligned first.
            ' With depth the correlation between the left and right for that gr will be accurate (if there is depth.)
            irPt = gr.rect.TopLeft ' the above cameras are already have RGB aligned to the left image.
            labels(2) = "RGB point at " + rgbTop.ToString + " is at " + irPt.ToString + " in the left view "

            dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = atask.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            dst2.Rectangle(New cv.Rect(irPt.X, irPt.Y, gr.rect.Width, gr.rect.Height), atask.highlight, atask.lineWidth)
            dst3.Rectangle(gr.rRect, atask.highlight, atask.lineWidth)
        End Sub
    End Class







    Public Class NR_Brick_RegionLines : Inherits TaskParent
        Dim regions As New Region_Contours
        Public Sub New()
            desc = "Lines can mean cells are connected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            regions.Run(src)
            dst2 = regions.dst2
            dst3 = regions.dst3
            labels = regions.labels

            For Each lp In atask.lines.lpList
                Dim c1 = dst2.Get(Of cv.Vec3b)(lp.p1.Y, lp.p1.X)
                Dim c2 = dst2.Get(Of cv.Vec3b)(lp.p2.Y, lp.p2.X)
                If c1 <> c2 Then
                    dst3.Line(lp.p1, lp.p2, white, atask.lineWidth, atask.lineWidth)
                Else
                    dst2.Line(lp.p1, lp.p2, white, atask.lineWidth, atask.lineWidth)
                End If
            Next
        End Sub
    End Class






    Public Class NR_Brick_CloudMaxVal : Inherits TaskParent
        Dim template As New Math_Intrinsics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
            desc = "Use RGB motion bricks to determine if depth has changed in any gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.heartBeatLT Or atask.frameCount < 3 Then atask.pointCloud.CopyTo(dst2)

            Dim splitCount As Integer
            For Each gr In atask.bricks.brickList
                If gr.depth > 0 Then
                    If gr.age < 10 Then
                        Dim split() As cv.Mat = atask.pointCloud(gr.rect).Split
                        split(2).SetTo(gr.mm.maxVal, atask.depthmask(gr.rect))

                        cv.Cv2.Multiply(template.dst2(gr.rect), split(2), split(0))
                        split(0) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fx)

                        cv.Cv2.Multiply(template.dst3(gr.rect), split(2), split(1))
                        split(1) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fy)

                        cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(gr.rect))
                        splitCount += 1
                    End If
                End If
            Next

            labels(2) = CStr(splitCount) + " bricks of " + CStr(atask.gridRects.Count) + " were modified."
            If standaloneTest() Then dst3 = atask.pointCloud
        End Sub
    End Class






    Public Class NR_Brick_CloudMean : Inherits TaskParent
        Dim template As New Math_Intrinsics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
            desc = "Use RGB motion bricks to determine if depth has changed in any gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.heartBeatLT Or atask.frameCount < 3 Then atask.pointCloud.CopyTo(dst2)

            Dim splitCount As Integer
            For Each gr In atask.bricks.brickList
                If gr.depth > 0 Then
                    Dim split() As cv.Mat = atask.pointCloud(gr.rect).Split
                    split(2).SetTo(gr.depth, atask.depthmask(gr.rect))

                    cv.Cv2.Multiply(template.dst2(gr.rect), split(2), split(0))
                    split(0) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fx)

                    cv.Cv2.Multiply(template.dst3(gr.rect), split(2), split(1))
                    split(1) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fy)

                    cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(gr.rect))
                    splitCount += 1
                End If
            Next

            labels(2) = CStr(splitCount) + " bricks of " + CStr(atask.gridRects.Count) + " were modified."
            If standaloneTest() Then dst3 = atask.pointCloud
        End Sub
    End Class






    Public Class NR_Brick_CloudRange : Inherits TaskParent
        Dim template As New Math_Intrinsics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
            desc = "Use RGB motion bricks to determine if depth has changed in any gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.heartBeatLT Or atask.frameCount < 3 Then atask.pointCloud.CopyTo(dst2)

            Dim splitCount As Integer
            Dim newRange As Single = 0.1F
            For Each gr In atask.bricks.brickList
                If gr.depth > 0 And gr.age = 1 Then
                    If gr.mm.range > newRange Then ' if the range within a gr is > 10 cm's, fit it within 10 cm's.
                        Dim split() As cv.Mat = atask.pointCloud(gr.rect).Split
                        split(2) -= gr.mm.minVal
                        split(2) *= newRange / gr.mm.range
                        split(2) += gr.depth

                        cv.Cv2.Multiply(template.dst2(gr.rect), split(2), split(0))
                        split(0) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fx)

                        cv.Cv2.Multiply(template.dst3(gr.rect), split(2), split(1))
                        split(1) *= cv.Scalar.All(1 / atask.calibData.leftIntrinsics.fy)

                        cv.Cv2.Merge({split(0), split(1), split(2)}, dst2(gr.rect))
                        dst2(gr.rect).SetTo(0, atask.noDepthMask(gr.rect))
                        splitCount += 1
                    End If
                End If
            Next

            labels(2) = CStr(splitCount) + " bricks of " + CStr(atask.gridRects.Count) + " were modified."
            If standaloneTest() Then dst3 = atask.pointCloud
        End Sub
    End Class






    Public Class Brick_Features : Inherits TaskParent
        Public featureBricks As New List(Of cv.Rect)
        Public Sub New()
            atask.gOptions.LineWidth.Value = 3
            If atask.feat Is Nothing Then atask.feat = New Feature_Basics
            labels(3) = "Featureless areas"
            desc = "Identify the cells with features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = atask.feat.dst2

            featureBricks.Clear()
            Dim featList As New List(Of cv.Point)(atask.feat.features)
            For Each pt In featList
                Dim index As Integer = atask.gridMap.Get(Of Integer)(pt.Y, pt.X)
                featureBricks.Add(atask.gridRects(index))
            Next

            If atask.gOptions.DebugCheckBox.Checked Then
                For Each pt In featList
                    DrawCircle(dst2, pt, atask.DotSize, cv.Scalar.Black)
                Next
            End If

            If standaloneTest() Then
                dst3.SetTo(0)
                For Each r In featureBricks
                    dst3.Rectangle(r, white, -1)
                Next
                dst3 = Not dst3
            End If

            If atask.heartBeat Then
                Dim flessCount = atask.gridRects.Count - featureBricks.Count
                labels(2) = CStr(featureBricks.Count) + " cells had features while " + CStr(flessCount) + " had none"
            End If
        End Sub
    End Class








    Public Class Brick_FeaturesAndEdges : Inherits TaskParent
        Public feat As New Brick_EdgeFlips
        Public boundaryCells As New List(Of List(Of Integer))
        Public Sub New()
            labels(2) = "Gray and black regions are featureless while white has features..."
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
            desc = "Find the boundary cells between feature and featureless cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(src)
            dst1 = feat.featureMask.Clone

            boundaryCells.Clear()
            For Each nabeList In atask.grid.gridNeighbors
                Dim grA = atask.gridRects(nabeList(0))
                Dim centerType = feat.featureMask.Get(Of Byte)(grA.Y, grA.X)
                If centerType <> 0 Then
                    Dim boundList = New List(Of Integer)
                    Dim addFirst As Boolean = True
                    For i = 1 To nabeList.Count - 1
                        Dim grB = atask.gridRects(nabeList(i))
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
                    Dim gr = atask.gridRects(n)
                    Dim val = feat.featureMask.Get(Of Byte)(gr.Y, gr.X)
                    If val > 0 Then mytoggle = 255 Else mytoggle = 128
                    dst2(atask.gridRects(n)).SetTo(mytoggle)
                Next
            Next
        End Sub
    End Class






    Public Class NR_Brick_MLColor : Inherits TaskParent
        Dim ml As New ML_Basics
        Dim bounds As New Brick_FeaturesAndEdges
        Public Sub New()
            If standalone Then atask.gOptions.displayDst1.Checked = True
            ml.buildEveryPass = True
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
            desc = "Train an ML tree to predict each pixel of the boundary cells using only color from boundary neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bounds.Run(src)
            Dim edgeMask = bounds.feat.edges.dst2

            Dim rgb32f As New cv.Mat, tmp As New cv.Mat
            src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

            dst1 = bounds.feat.fLessMask
            Dim trainRGB As cv.Mat
            For i = 0 To bounds.boundaryCells.Count - 1
                Dim nList = bounds.boundaryCells(i)

                ' the first gr is the center one and the only gr with edges.  The rest are featureless.
                Dim gr = atask.gridRects(nList(0))
                Dim edgePixels = edgeMask(gr).FindNonZero()

                ' mark the edge pixels as class 2 - others will be updated next
                ml.trainResponse = New cv.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cv.MatType.CV_32F, New cv.Scalar(2))
                trainRGB = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32FC3)

                For j = 1 To nList.Count - 1
                    Dim grA = atask.gridRects(nList(j))
                    Dim x As Integer = Math.Floor(grA.X * atask.bricksPerRow / atask.cols)
                    Dim y As Integer = Math.Floor(grA.Y * atask.bricksPerCol / atask.rows)
                    Dim val = atask.lowResColor.Get(Of cv.Vec3f)(y, x)
                    trainRGB.Set(Of cv.Vec3f)(j - 1, 0, val)
                    ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
                Next

                ' next, add the edge pixels in the target cell - they are the feature identifiers.
                Dim index = nList.Count - 1
                For j = 0 To edgePixels.Rows - 1
                    Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                    Dim val = rgb32f.Get(Of cv.Vec3f)(gr.Y + pt.Y, gr.X + pt.X)
                    trainRGB.Set(Of cv.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
                Next

                ml.trainMats = {trainRGB}

                Dim grB = atask.gridRects(nList(0))
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
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(3) = "The map to identify each gr's depth."
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Display a heatmap of the correlation of the left and right images for each gr."
        End Sub
        Public Function ShowPaletteCorrelation(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            cv.Cv2.ApplyColorMap(input, output, atask.correlationColorMap)
            Return output
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1.SetTo(0)
            atask.depthAndDepthRange = ""
            For Each gr In atask.bricks.brickList
                dst1(gr.rect).SetTo((gr.correlation + 1) * 255 / 2)
            Next

            dst2 = ShowPaletteCorrelation(dst1)
            dst2.SetTo(0, atask.noDepthMask)

            Dim pt = atask.mouseMovePoint, w = atask.workRes.Width, h = atask.workRes.Height
            If pt.X >= 0 And pt.X < w And pt.Y >= 0 And pt.Y < h Then
                Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
                atask.brickD = atask.bricks.brickList(index)
                atask.depthAndDepthRange = "depth = " + Format(atask.brickD.depth, fmt3) + "m ID=" +
                                      CStr(atask.brickD.index) + vbCrLf + " range " +
                                      Format(atask.brickD.mm.minVal, fmt1) + "-" +
                                      Format(atask.brickD.mm.maxVal, fmt1) + "m, age = " +
                                      CStr(atask.brickD.age) + vbCrLf +
                                      " correlation = " + Format(atask.brickD.correlation, fmt3)

                Dim ptTextLoc = atask.brickD.rect.TopLeft
                If ptTextLoc.X > w * 0.85 Or (ptTextLoc.Y < h * 0.15 And ptTextLoc.X > w * 0.15) Then
                    ptTextLoc.X -= w * 0.15
                Else
                    ptTextLoc.Y -= atask.brickD.rect.Height * 3
                End If

                SetTrueText(atask.depthAndDepthRange, ptTextLoc, 2)
                SetTrueText(atask.depthAndDepthRange, 3)
            End If
            labels(2) = atask.bricks.labels(2)
        End Sub
    End Class






    Public Class Brick_LeftRight : Inherits TaskParent
        Public means As New List(Of Single)
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(2) = "Only every other colum is shown to make it clear which bricks are being translated (can get crowded otherwise.)"
            labels(3) = "Right view with the translated bricks shown at left."
            desc = "Map the column of bricks in the color image into the left view and then to the right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = atask.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = atask.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            Dim colorIndex As Integer
            For i = 0 To atask.bricksPerRow - 1 Step 2
                colorIndex = 0
                For j = i To atask.gridRects.Count - atask.bricksPerRow - 1 Step atask.bricksPerRow
                    Dim gr = atask.bricks.brickList(j)
                    Dim color = atask.scalarColors(colorIndex)
                    If gr.depth > 0 Then
                        dst2.Rectangle(gr.lRect, color, atask.lineWidth)
                        dst3.Rectangle(gr.rRect, color, atask.lineWidth)
                    End If
                    colorIndex += 1
                Next
            Next
        End Sub
    End Class







    Public Class Brick_EdgeFlips : Inherits TaskParent
        Public edges As New Edge_Basics
        Public featureRects As New List(Of cv.Rect)
        Public featureMask As New cv.Mat
        Public fLessMask As New cv.Mat
        Public fLessRects As New List(Of cv.Rect)
        Public Sub New()
            featureMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
            fLessMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
            atask.featureOptions.EdgeMethods.SelectedItem() = "Laplacian"
            desc = "Add edges to features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static stateList As New List(Of Single)
            Static lastDepth As cv.Mat = atask.lowResDepth.Clone

            edges.Run(src)

            featureRects.Clear()
            fLessRects.Clear()
            featureMask.SetTo(0)
            fLessMask.SetTo(0)
            Dim flist As New List(Of Single)
            For Each r In atask.gridRects
                flist.Add(If(edges.dst2(r).CountNonZero <= 1, 1, 2))
            Next

            If atask.optionsChanged Or stateList.Count = 0 Then
                stateList.Clear()
                For Each n In flist
                    stateList.Add(n)
                Next
            End If

            Dim flipRects As New List(Of cv.Rect)
            For i = 0 To atask.gridRects.Count - 1
                stateList(i) = (stateList(i) + flist(i)) / 2
                Dim r = atask.gridRects(i)
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
                dst2.Rectangle(r, atask.highlight, atask.lineWidth)
            Next

            For Each r In fLessRects
                Dim x = CInt(r.X / atask.brickSize)
                Dim y = CInt(r.Y / atask.brickSize)
                atask.lowResDepth.Set(Of Single)(y, x, lastDepth.Get(Of Single)(y, x))
            Next
            lastDepth = atask.lowResDepth.Clone
            If atask.heartBeat Then
                labels(2) = CStr(fLessRects.Count) + " cells without features were found.  " +
                        "Cells that are flipping (with and without edges) are highlighted"
            End If
        End Sub
    End Class







    Public Class Brick_Edges : Inherits TaskParent
        Dim options As New Options_LeftRightCorrelation
        Public edges As New Edge_Basics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Add edges to features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            edges.Run(atask.leftView)
            dst2 = edges.dst2

            Dim count As Integer
            dst3.SetTo(0)
            For Each gr As brickData In atask.bricks.brickList
                If dst2(gr.lRect).CountNonZero And gr.rRect.Width > 0 And gr.correlation > options.correlation Then
                    atask.rightView(gr.rRect).CopyTo(dst3(gr.rRect))
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " of " + CStr(atask.bricks.brickList.Count) + " rects were identified in dst3"
        End Sub
    End Class




    Public Class Brick_Lines : Inherits TaskParent
        Dim lines As New Line_Basics
        Dim options As New Options_LeftRightCorrelation
        Dim motionLeft As New Motion_Basics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            labels(2) = "The lines are for the left image."
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find all the bricks that contain lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            motionLeft.Run(atask.leftView)

            lines.motionMask = motionLeft.dst3
            lines.Run(atask.leftView)
            dst2 = lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim count As Integer
            dst3.SetTo(0)
            For Each gr As brickData In atask.bricks.brickList
                If dst2(gr.lRect).CountNonZero And gr.rRect.Width > 0 And
                    gr.correlation > options.correlation Then

                    atask.leftView(gr.lRect).CopyTo(dst3(gr.lRect))
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " of " + CStr(atask.bricks.brickList.Count) +
                        " rects had an edge and a range for depth > X cm's"
        End Sub
    End Class




    Public Class Brick_HighRange : Inherits TaskParent
        Dim lines As New Line_Basics
        Dim options As New Options_LeftRightCorrelation
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            labels(2) = "Left view (stable)"
            desc = "Find all the bricks that have a high range (> X mm's)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst2 = atask.leftView

            Dim count As Integer
            dst3.SetTo(0)
            For Each gr As brickData In atask.bricks.brickList
                If gr.mm.maxVal - gr.mm.minVal > options.mmRange Then
                    dst2(gr.lRect).CopyTo(dst3(gr.lRect))
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " of " + CStr(atask.bricks.brickList.Count) +
                        " rects had a range for depth > " + CStr(options.mmRange) + " mm's"
        End Sub
    End Class





    Public Class Brick_Plot : Inherits TaskParent
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            plotHist.createHistogram = True
            plotHist.addLabels = False
            plotHist.removeZeroEntry = False
            labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
            desc = "Select any cell To plot a histogram Of that cell's depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = atask.leftView

            Dim index As Integer = atask.gridMap.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
            If atask.bricks.brickList.Count = 0 Or atask.optionsChanged Then Exit Sub

            Dim gr As brickData
            If index < 0 Or index >= atask.bricks.brickList.Count Then
                gr = atask.bricks.brickList(atask.bricks.brickList.Count / 2)
                atask.mouseMovePoint = New cv.Point(gr.rect.X + gr.rect.Width / 2, gr.rect.Y + gr.rect.Height / 2)
            Else
                gr = atask.bricks.brickList(index)
            End If

            Dim split() = atask.pointCloud(gr.rect).Split()
            Dim mm = GetMinMax(split(2))
            If Single.IsInfinity(mm.maxVal) Then Exit Sub

            Static lastMouse As cv.Point = atask.mouseMovePoint
            If atask.heartBeat Or lastMouse <> atask.mouseMovePoint Then
                lastMouse = atask.mouseMovePoint
                If Math.Abs(mm.maxVal - mm.minVal) > 0 Then
                    plotHist.minRange = mm.minVal
                    plotHist.maxRange = mm.maxVal
                    plotHist.Run(split(2))
                    dst3 = plotHist.dst2
                    labels(3) = "Depth values vary from " + Format(plotHist.minRange, fmt3) +
                                " to " + Format(plotHist.maxRange, fmt3)
                End If
            End If
        End Sub
    End Class




    Public Class Brick_NoDepthLines : Inherits TaskParent
        Dim lines As New Line_Basics
        Dim options As New Options_LeftRightCorrelation
        Dim motionLeft As New Motion_Basics
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            If standalone Then atask.gOptions.displayDst0.Checked = True
            labels(2) = "The lines are for the left image."
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find bricks that contain lines and depth zeros."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst0 = atask.leftView

            motionLeft.Run(atask.leftView)

            lines.motionMask = motionLeft.dst3
            lines.Run(atask.leftView)
            dst2 = lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim count As Integer
            dst3.SetTo(0)
            For Each gr In atask.bricks.brickList
                If dst2(gr.lRect).CountNonZero And atask.noDepthMask(gr.rect).CountNonZero Then
                    If gr.mm.maxVal - gr.mm.minVal > options.mmRange Then
                        atask.leftView(gr.lRect).CopyTo(dst3(gr.lRect))
                        count += 1
                    End If
                End If
            Next
            labels(3) = CStr(count) + " of " + CStr(atask.bricks.brickList.Count) +
                        " rects had an edge and pixels with zero depth"
        End Sub
    End Class
End Namespace