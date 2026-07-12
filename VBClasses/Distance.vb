Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Distance_Basics : Inherits TaskParent
    Dim distance As New Distance_Instant
    Public Sub New()
        desc = "Floodfill the distance_basics results"
    End Sub
    Public Shared Function distance3D(p1 As Point3f, p2 As Point3f) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Shared Function distance3D(p1 As Vec3b, p2 As Vec3b) As Single
        Return Math.Sqrt((CInt(p1(0)) - CInt(p2(0))) * (CInt(p1(0)) - CInt(p2(0))) +
                                 (CInt(p1(1)) - CInt(p2(1))) * (CInt(p1(1)) - CInt(p2(1))) +
                                 (CInt(p1(2)) - CInt(p2(2))) * (CInt(p1(2)) - CInt(p2(2))))
    End Function
    Public Shared Function distance3D(p1 As Point3i, p2 As Point3i) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Shared Function distance3D(p1 As Scalar, p2 As Scalar) As Single
        Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                                 (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                                 (p1(2) - p2(2)) * (p1(2) - p2(2)))
    End Function
    Public Shared Function GetMaxDist(ByRef md As maskData) As cv.Point
        Dim mask = md.mask.Clone
        Rectangle(mask, New cv.Rect(0, 0, mask.Width, mask.Height), Scalar.All(0), 1)
        Dim distance32f As New Mat
        DistanceTransform(mask, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += md.rect.X
        mm.maxLoc.Y += md.rect.Y

        Return mm.maxLoc
    End Function
    Public Shared Function GetMaxDist(ByRef maskInput As Mat, rect As cv.Rect) As cv.Point
        Dim mask = maskInput.Clone
        Rectangle(mask, New cv.Rect(0, 0, mask.Width, mask.Height), Scalar.All(0), 1)
        Dim distance32f As New Mat
        DistanceTransform(mask, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rect.X
        mm.maxLoc.Y += rect.Y

        Return mm.maxLoc
    End Function
    Public Shared Function GetMaxDistDepth(ByRef maskInput As Mat, rect As cv.Rect) As cv.Point
        Dim depth As New Mat
        task.depthmask(rect).CopyTo(depth, maskInput)
        Rectangle(depth, New cv.Rect(0, 0, depth.Width, depth.Height), Scalar.All(0), 1)
        Dim distance32f As New Mat
        DistanceTransform(depth, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rect.X
        mm.maxLoc.Y += rect.Y

        Return mm.maxLoc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then src = task.depthmask.Clone
        If task.optionsChanged Then dst1 = src.Clone Else src.CopyTo(dst1, task.motion.motionMask)
        distance.Run(dst1)
        dst2 = distance.dst2
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Distance_Foreground : Inherits TaskParent
    Dim dist As New Distance_Basics
    Dim foreground As New XR_Foreground_KMeans
    Public useBackgroundAsInput As Boolean
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "Input mask to distance transform"
        desc = "Distance algorithm basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static cRadio = OptionParent.findRadio("C")
        Static l1Radio = OptionParent.findRadio("L1")

        foreground.Run(src)
        dst3 = If(useBackgroundAsInput, foreground.dst2, foreground.dst3)

        Dim DistanceType = DistanceTypes.L2
        If cRadio.Checked Then DistanceType = DistanceTypes.C
        If l1Radio.Checked Then DistanceType = DistanceTypes.L1

        dst0 = dst3 And task.gray
        DistanceTransform(dst0, dst0, DistanceType, DistanceTransformMasks.Precise, MatType.CV_32F)
        Dim dist32f As New Mat
        Normalize(dst0, dist32f, 0, 255, NormTypes.MinMax)
        dist32f.ConvertTo(dst1, MatType.CV_8UC1)
        CvtColor(dst1, dst2, ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class XR_Distance_Background : Inherits TaskParent
    Dim dist As New Distance_Foreground
    Public Sub New()
        dist.useBackgroundAsInput = True
        desc = "Use distance algorithm on the background"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dist.Run(src)
        dst2 = dist.dst2
        dst3 = dist.dst3
        labels(2) = dist.labels(2)
        labels(3) = dist.labels(3)
    End Sub
End Class






Public Class Distance_Point3D : Inherits TaskParent
    Public inPoint1 As Point3f
    Public inPoint2 As Point3f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance in meters between 3D points in the cv.Point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            inPoint1 = New Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))
            inPoint2 = New Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))

            dst2.SetTo(0)
            Dim p1 = New cv.Point(inPoint1.X, inPoint1.Y)
            Dim p2 = New cv.Point(inPoint2.X, inPoint2.Y)
            Line(dst2, p1, p2, task.highlight, task.lineWidth, task.lineType)

            Dim vec1 = task.pointCloud.Get(Of Point3f)(p1.Y, p1.X)
            Dim vec2 = task.pointCloud.Get(Of Point3f)(p2.Y, p2.X)
        End If

        Dim x = inPoint1.X - inPoint2.X
        Dim y = inPoint1.Y - inPoint2.Y
        Dim z = inPoint1.Z - inPoint2.Z
        distance = Math.Sqrt(x * x + y * y + z * z)

        strOut = inPoint1.X.ToString(fmt3) + ", " + inPoint1.Y.ToString(fmt3) + ", " + inPoint1.Z.ToString(fmt3) + vbCrLf
        strOut += inPoint2.X.ToString(fmt3) + ", " + inPoint2.Y.ToString(fmt3) + ", " + inPoint2.Z.ToString(fmt3) + vbCrLf
        strOut += "Distance = " + distance.ToString(fmt3)
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Distance_Point4D : Inherits TaskParent
    Public inPoint1 As Vec4f
    Public inPoint2 As Vec4f
    Public distance As Single
    Public Sub New()
        desc = "Compute the distance between 4D points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            inPoint1 = New Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                        msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
            inPoint2 = New Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                        msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
        End If

        Dim x = inPoint1(0) - inPoint2(0)
        Dim y = inPoint1(1) - inPoint2(1)
        Dim z = inPoint1(2) - inPoint2(2)
        Dim d = inPoint1(3) - inPoint2(3)
        distance = Math.Sqrt(x * x + y * y + z * z + d * d)

        strOut = inPoint1.ToString + vbCrLf + inPoint2.ToString + vbCrLf + "Distance = " + distance.ToString(fmt1)
        If standalone And task.heartBeat Then SetTrueText(strOut, New cv.Point(10, 10), 2)
    End Sub
End Class






Public Class XR_Distance_BinaryImage : Inherits TaskParent
    Dim binary As New Binarize_Simple
    Dim distance As New Distance_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Measure the fragmentation of a binary image by using the distance transform"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        labels(2) = binary.labels(2) + " Draw a rectangle to measure specific area."

        If task.drawRect.Width > 0 Then
            distance.Run(dst2(task.drawRect))
        Else
            distance.Run(dst2)
        End If
        dst3 = distance.dst2
        Threshold(dst3, dst1, task.gOptions.DebugSlider.Value, 255, ThresholdTypes.Binary)
    End Sub
End Class






Public Class XR_Distance_PeakDepth : Inherits TaskParent
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        labels(2) = "The brightest grid rects are those farthest from zero depth"
        desc = "Find the grid rects which are furthest from the zero depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If CountNonZero(task.noDepthMask) = task.noDepthMask.Total Then Exit Sub ' startup issue 
        Dim distance32f As New Mat
        DistanceTransform(task.depthmask, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)

        Dim maxList As New List(Of Double)
        Dim ptList As New List(Of cv.Point)
        For Each rect In task.gridRects
            Dim mm = GetMinMax(distance32f(rect))
            maxList.Add(mm.maxVal)
            If mm.maxVal > 0 Then ptList.Add(New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y))
        Next


        If standalone Then
            dst3 = src.Clone
            For Each pt In ptList
                Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(3) = CStr(ptList.Count) + " points selected"
        End If

        Dim max = maxList.Max
        dst2.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            dst2(rect).SetTo(255 * maxList(i) / max)
        Next
    End Sub
End Class






Public Class XR_Distance_PeakNoDepth : Inherits TaskParent
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        labels(2) = "The brightest grid rects are those farthest from any depth"
        desc = "Find the grid rects which are furthest from the zero depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If CountNonZero(task.noDepthMask) = task.noDepthMask.Total Then Exit Sub ' startup issue 
        Dim distance32f As New Mat
        DistanceTransform(task.noDepthMask, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)

        Dim maxList As New List(Of Double)
        Dim ptList As New List(Of cv.Point)
        For Each rect In task.gridRects
            Dim mm = GetMinMax(distance32f(rect))
            maxList.Add(mm.maxVal)
            If mm.maxVal > 0 Then ptList.Add(New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y))
        Next

        If standalone Then
            dst3 = src.Clone
            For Each pt In ptList
                Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(3) = CStr(ptList.Count) + " points selected"
        End If

        Dim max = maxList.Max
        dst2.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            dst2(rect).SetTo(255 * maxList(i) / max)
        Next
    End Sub
End Class







Public Class Distance_Labels : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        labels(2) = "Distance results"
        labels(3) = "The labels for each vein of the distance transform output."
        desc = "Distance algorithm basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standalone Then src = task.noDepthMask
        If src.Channels() <> 1 Then src = task.gray

        DistanceTransformWithLabels(src, dst0, dst1, options.distanceType, DistanceTransformMasks.Precise)
        Normalize(dst0, dst2, 0, 255, NormTypes.MinMax)
        dst2.ConvertTo(dst2, MatType.CV_8UC1)
        CvtColor(dst2, dst2, ColorConversionCodes.GRAY2BGR)

        dst3 = Palettize(dst1)
        If standalone Then dst3.SetTo(0, task.depthmask)
    End Sub
End Class







Public Class XR_Distance_LabelsNoDepth : Inherits TaskParent
    Dim labeller As New Distance_Labels
    Public Sub New()
        desc = "Distance algorithm for the regions with no depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat = False Then Exit Sub
        labeller.Run(task.noDepthMask)
        dst2 = labeller.dst2
        dst3 = labeller.dst3
        labels = labeller.labels
        dst3.SetTo(0, task.depthmask)
    End Sub
End Class






Public Class XR_Distance_LabelsDepth : Inherits TaskParent
    Dim labeller As New Distance_Labels
    Public Sub New()
        desc = "Distance algorithm for the regions with no depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT = False Then Exit Sub
        labeller.Run(task.depthmask)
        dst2 = labeller.dst2
        dst3 = labeller.dst3
        labels = labeller.labels
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class XR_Distance_Edges : Inherits TaskParent
    Dim distance As New Distance_Basics
    Public Sub New()
        desc = "Combine the output of Edge_Basics_TA and distance_basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        distance.Run(task.depthmask)
        dst2 = ShowAddweighted(distance.dst2, task.edges.dst2, labels(2))
    End Sub
End Class










Public Class Distance_Instant : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        labels = {"", "", "Distance transform - create a mask with threshold", ""}
        desc = "Distance algorithm basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then src = task.depthRGB
        If src.Channels() <> 1 Then src = task.gray

        DistanceTransform(src, dst0, options.distanceType, DistanceTransformMasks.Precise, MatType.CV_32F)
        dst1 = Mat_Convert.Mat_32f_To_8UC3(dst0)
        dst1.ConvertTo(dst2, MatType.CV_8UC1)
    End Sub
End Class





Public Class Distance_Depth : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        task.gOptions.DebugSlider.Value = 3
        desc = "Apply the distance transform to the depth data and clip values below specified threshold."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> MatType.CV_32F Then src = task.pcSplit(2)
        Dim mm = GetMinMax(src)
        dst1 = src * 255 / mm.maxVal
        dst1.ConvertTo(dst1, MatType.CV_8U)
        DistanceTransform(dst1, dst2, options.distanceType, DistanceTransformMasks.Precise, MatType.CV_32F)
        Threshold(dst2, dst3, task.gOptions.DebugSlider.Value, 255, ThresholdTypes.Binary)
        mm = GetMinMax(dst2)
        labels(2) = "Distance results of 32F input data (usually Depth data).  Min = " + CStr(CInt(mm.minVal)) + " and max = " + CStr(CInt(mm.maxVal))
    End Sub
End Class







Public Class XR_Distance_DepthPeaks : Inherits TaskParent
    Dim dist As New Distance_Depth
    Public Sub New()
        dst3 = New Mat(dst3.Size, MatType.CV_32F, 0)
        desc = "Find the peaks in the depth data.  NOTE: use global option 'DebugSlider' to provide the threshold value."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dist.Run(src)
        dst2 = dist.dst2

        Dim thresholdVal = Math.Abs(task.gOptions.DebugSlider.Value)
        Dim mask As New Mat
        Threshold(dst2, mask, thresholdVal, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(mask, mask)
        dst3.SetTo(0)
        dst2.CopyTo(dst3, mask)

        Dim mm = GetMinMax(dst2)
        labels(2) = "Distance results of 32F input data (usually Depth data).  Min = " + CStr(CInt(mm.minVal)) + " and max = " + CStr(CInt(mm.maxVal))
    End Sub
End Class






Public Class XR_Distance_ClickPoint : Inherits TaskParent
    Dim options As New Options_Distance
    Public Sub New()
        labels(3) = "Inverse of dst2"
        desc = "Click anywhere to visualize the distance to the each pixel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        InRange(task.gray, 0, 0, dst1)

        task.gray.SetTo(255, dst1)
        task.gray.Set(Of Byte)(task.clickPoint.Y, task.clickPoint.X, 0)
        DistanceTransform(task.gray, dst2, options.distanceType, DistanceTransformMasks.Precise, MatType.CV_32F)
        dst3 = 255 - dst2
    End Sub
End Class






Public Class XR_Distance_DepthBricks : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim dist As New Distance_Depth
    Public Sub New()
        task.gOptions.DebugSlider.Value = 20
        desc = "Threshold the maxDist in each grid square to highlight centers for key objects.  Use the 'DebugSlider' to provide the value."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dist.Run(src)
        dst2 = dist.dst2
        dst3 = src.Clone

        Dim threshold = Math.Abs(task.gOptions.DebugSlider.Value)
        For Each brick In bricks.brickList
            Dim mm = GetMinMax(dst2(brick.rect))
            If mm.maxVal >= threshold Then
                Dim pt = New cv.Point(mm.maxLoc.X + brick.rect.X, mm.maxLoc.Y + brick.rect.Y)
                Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
            End If
        Next
    End Sub
End Class







Public Class XR_Distance_Contour : Inherits TaskParent
    Dim options As New Options_Distance
    Dim contours As New Contour_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        desc = "Compute the distance of each cv.Point from the top contour (or a selected contour.)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        contours.Run(src)

        dst2 = contours.dst2
        labels(2) = contours.labels(2)

        dst3 = src.Clone
        dst3(task.contourD.rect).SetTo(white, task.contourD.mask)

        dst1.SetTo(255)
        dst1(task.contourD.rect).SetTo(0, task.contourD.mask)

        DistanceTransform(dst1, dst0, options.distanceType, DistanceTransformMasks.Precise, MatType.CV_32F)
    End Sub
End Class
