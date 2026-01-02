Imports OpenCvSharp
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Distance_Basics : Inherits TaskParent
        Dim distance As New Distance_Instant
        Public Sub New()
            desc = "Floodfill the distance_basics results"
        End Sub
        Public Shared Function distance3D(p1 As cv.Point3f, p2 As cv.Point3f) As Single
            Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
        End Function
        Public Shared Function distance3D(p1 As cv.Vec3b, p2 As cv.Vec3b) As Single
            Return Math.Sqrt((CInt(p1(0)) - CInt(p2(0))) * (CInt(p1(0)) - CInt(p2(0))) +
                             (CInt(p1(1)) - CInt(p2(1))) * (CInt(p1(1)) - CInt(p2(1))) +
                             (CInt(p1(2)) - CInt(p2(2))) * (CInt(p1(2)) - CInt(p2(2))))
        End Function
        Public Shared Function distance3D(p1 As cv.Point3i, p2 As cv.Point3i) As Single
            Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
        End Function
        Public Shared Function distance3D(p1 As cv.Scalar, p2 As cv.Scalar) As Single
            Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                             (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                             (p1(2) - p2(2)) * (p1(2) - p2(2)))
        End Function
        Public Shared Function GetMaxDist(ByRef md As maskData) As cv.Point
            Dim mask = md.mask.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += md.rect.X
            mm.maxLoc.Y += md.rect.Y

            Return mm.maxLoc
        End Function
        Public Shared Function GetMaxDist(ByRef maskInput As cv.Mat, rect As cv.Rect) As cv.Point
            Dim mask = maskInput.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += rect.X
            mm.maxLoc.Y += rect.Y

            Return mm.maxLoc
        End Function
        Public Shared Function GetMaxDistDepth(ByRef maskInput As cv.Mat, rect As cv.Rect) As cv.Point
            Dim depth As New cv.Mat
            task.depthmask(rect).CopyTo(depth, maskInput)
            depth.Rectangle(New cv.Rect(0, 0, depth.Width, depth.Height), 0, 1)
            Dim distance32f = depth.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += rect.X
            mm.maxLoc.Y += rect.Y

            Return mm.maxLoc
        End Function
        Public Shared Function GetMaxDist(ByRef rc As oldrcData) As cv.Point
            Dim mask = rc.mask.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += rc.rect.X
            mm.maxLoc.Y += rc.rect.Y

            Return mm.maxLoc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then src = task.depthmask.Clone
            If task.optionsChanged Then dst1 = src.Clone Else src.CopyTo(dst1, task.motionMask)
            distance.Run(dst1)
            dst2 = distance.dst2
            dst2.SetTo(0, task.noDepthMask)
        End Sub
    End Class








    Public Class Distance_Foreground : Inherits TaskParent
        Dim dist As New Distance_Basics
        Dim foreground As New Foreground_KMeans
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

            Dim DistanceType = cv.DistanceTypes.L2
            If cRadio.Checked Then DistanceType = cv.DistanceTypes.C
            If l1Radio.Checked Then DistanceType = cv.DistanceTypes.L1

            dst0 = dst3 And task.gray
            dst0 = dst0.DistanceTransform(DistanceType, cv.DistanceTransformMasks.Precise)
            Dim dist32f = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            dist32f.ConvertTo(dst1, cv.MatType.CV_8UC1)
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End Sub
    End Class







    Public Class Distance_Background : Inherits TaskParent
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
        Public inPoint1 As cv.Point3f
        Public inPoint2 As cv.Point3f
        Public distance As Single
        Public Sub New()
            desc = "Compute the distance in meters between 3D points in the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() And task.heartBeat Then
                inPoint1 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))
                inPoint2 = New cv.Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000))

                dst2.SetTo(0)
                Dim p1 = New cv.Point(inPoint1.X, inPoint1.Y)
                Dim p2 = New cv.Point(inPoint2.X, inPoint2.Y)
                vbc.DrawLine(dst2, p1, p2, task.highlight)

                Dim vec1 = task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X)
                Dim vec2 = task.pointCloud.Get(Of cv.Point3f)(p2.Y, p2.X)
            End If

            Dim x = inPoint1.X - inPoint2.X
            Dim y = inPoint1.Y - inPoint2.Y
            Dim z = inPoint1.Z - inPoint2.Z
            distance = Math.Sqrt(x * x + y * y + z * z)

            strOut = Format(inPoint1.X, fmt3) + ", " + Format(inPoint1.Y, fmt3) + ", " + Format(inPoint1.Z, fmt3) + vbCrLf
            strOut += Format(inPoint2.X, fmt3) + ", " + Format(inPoint2.Y, fmt3) + ", " + Format(inPoint2.Z, fmt3) + vbCrLf
            strOut += "Distance = " + Format(distance, fmt3)
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class Distance_Point4D : Inherits TaskParent
        Public inPoint1 As cv.Vec4f
        Public inPoint2 As cv.Vec4f
        Public distance As Single
        Public Sub New()
            desc = "Compute the distance between 4D points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                inPoint1 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
                inPoint2 = New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                    msRNG.Next(0, task.MaxZmeters), msRNG.Next(0, task.MaxZmeters))
            End If

            Dim x = inPoint1(0) - inPoint2(0)
            Dim y = inPoint1(1) - inPoint2(1)
            Dim z = inPoint1(2) - inPoint2(2)
            Dim d = inPoint1(3) - inPoint2(3)
            distance = Math.Sqrt(x * x + y * y + z * z + d * d)

            strOut = inPoint1.ToString + vbCrLf + inPoint2.ToString + vbCrLf + "Distance = " + Format(distance, fmt1)
            If standalone And task.heartBeat Then SetTrueText(strOut, New cv.Point(10, 10), 2)
        End Sub
    End Class






    Public Class Distance_BinaryImage : Inherits TaskParent
        Dim binary As New Binarize_Simple
        Dim distance As New Distance_Basics
        Public Sub New()
            If standalone Then task.gOptions.displaydst1.checked = True
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
            dst1 = dst3.Threshold(task.gOptions.DebugSlider.Value, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class






    Public Class Distance_PeakDepth : Inherits TaskParent
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            labels(2) = "The brightest grid rects are those farthest from zero depth"
            desc = "Find the grid rects which are furthest from the zero depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.noDepthMask.CountNonZero = task.noDepthMask.Total Then Exit Sub ' startup issue 
            Dim distance32f = task.depthMask.DistanceTransform(cv.DistanceTypes.L1, 0)

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
                    DrawCircle(dst3, pt, task.DotSize, task.highlight)
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






    Public Class Distance_PeakNoDepth : Inherits TaskParent
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            labels(2) = "The brightest grid rects are those farthest from any depth"
            desc = "Find the grid rects which are furthest from the zero depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.noDepthMask.CountNonZero = task.noDepthMask.Total Then Exit Sub ' startup issue 
            Dim distance32f = task.noDepthMask.DistanceTransform(cv.DistanceTypes.L1, 0)

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
                    DrawCircle(dst3, pt, task.DotSize, task.highlight)
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
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            cv.Cv2.DistanceTransformWithLabels(src, dst0, dst1, options.distanceType, cv.DistanceTransformMasks.Precise)
            dst2 = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            dst3 = PaletteFull(dst1)
            If standalone Then dst3.SetTo(0, task.depthMask)
        End Sub
    End Class







    Public Class Distance_LabelsNoDepth : Inherits TaskParent
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
            dst3.SetTo(0, task.depthMask)
        End Sub
    End Class






    Public Class Distance_LabelsDepth : Inherits TaskParent
        Dim labeller As New Distance_Labels
        Public Sub New()
            desc = "Distance algorithm for the regions with no depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeatLT = False Then Exit Sub
            labeller.Run(task.depthMask)
            dst2 = labeller.dst2
            dst3 = labeller.dst3
            labels = labeller.labels
            dst3.SetTo(0, task.noDepthMask)
        End Sub
    End Class






    Public Class Distance_Edges : Inherits TaskParent
        Dim distance As New Distance_Basics
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Combine the output of edge_Basics and distance_basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            distance.Run(task.depthMask)

            edges.Run(src)

            dst2 = ShowAddweighted(distance.dst2, edges.dst2, labels(2))
        End Sub
    End Class






    Public Class Distance_RedDistance : Inherits TaskParent
        Dim distance As New Distance_Basics
        Public Sub New()
            desc = "Combine the output of RedList_Basics and distance_basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            distance.Run(dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            dst2 = ShowAddweighted(distance.dst2, task.redList.dst2, labels(2))
        End Sub
    End Class






    Public Class Distance_RedColor : Inherits TaskParent
        Dim hColor As New Hist3Dcolor_Basics
        Public pixelVector As New List(Of List(Of Single))
        Dim distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
        Dim lastDistances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDoubleInverted)
        Dim lastrcList As New List(Of oldrcData)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            OptionParent.FindSlider("Histogram 3D Bins").Value = 5
            hColor.noMotionMask = True
            labels(1) = "3D Histogram distance for each of the cells at left"
            labels(2) = "RedColor output with the cell's distance from zero depth"
            desc = "Identify RedCloud cells using the cell's 3D histogram distance from zero"
        End Sub
        Private Function distanceFromZero(histlist As List(Of Single)) As Double
            Dim result As Double
            For Each d In histlist
                result += d * d
            Next
            Return Math.Sqrt(result)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = runRedList(src, labels(3))

            pixelVector.Clear()
            distances.Clear()
            For i = 1 To task.redList.oldrclist.Count - 1
                Dim rc = task.redList.oldrclist(i)
                hColor.inputMask = rc.mask
                hColor.Run(src(rc.rect))

                Dim nextD = distanceFromZero(hColor.histArray.ToList)
                distances.Add(nextD, i)
            Next

            If task.heartBeatLT Then
                strOut = "3D histogram distances from zero for each cell" + vbCrLf
                Dim index As Integer
                For Each el In distances
                    strOut += "(" + CStr(el.Value) + ") "
                    strOut += Format(el.Key, fmt1) + vbTab
                    If index Mod 6 = 5 Then strOut += vbCrLf
                    index += 1
                Next

                strOut += "----------------------" + vbCrLf
                index = 0
                For Each el In lastDistances
                    strOut += "(" + CStr(el.Value) + ") "
                    strOut += Format(el.Key, fmt1) + vbTab
                    If index Mod 6 = 5 Then strOut += vbCrLf
                    index += 1
                    Dim rc = lastrcList(el.Value)
                Next
            End If

            For Each el In lastDistances
                Dim rp = lastrcList(el.Value)
                SetTrueText(el.Value, New cv.Point(rp.maxDist.X, rp.maxDist.Y + 10))
            Next

            SetTrueText(strOut, 1)

            dst2.SetTo(0)
            For i = 0 To distances.Count - 1
                Dim rp = task.redList.oldrclist(distances.ElementAt(i).Value)
                task.color(rp.rect).CopyTo(dst2(rp.rect), rp.mask)
            Next
            labels(2) = task.redList.labels(3)

            lastDistances.Clear()
            For Each el In distances
                lastDistances.Add(el.Key, el.Value)
            Next

            lastrcList = New List(Of oldrcData)(task.redList.oldrclist)
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
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            dst0 = src.DistanceTransform(options.distanceType, 0)
            dst1 = Mat_Convert.Mat_32f_To_8UC3(dst0)
            dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
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

            If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
            Dim mm = GetMinMax(src)
            dst1 = src * 255 / mm.maxVal
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
            dst2 = dst1.DistanceTransform(options.distanceType, 0)
            dst3 = dst2.Threshold(task.gOptions.DebugSlider.Value, 255, cv.ThresholdTypes.Binary)
            mm = GetMinMax(dst2)
            labels(2) = "Distance results of 32F input data (usually Depth data).  Min = " + CStr(CInt(mm.minVal)) + " and max = " + CStr(CInt(mm.maxVal))
        End Sub
    End Class







    Public Class Distance_DepthPeaks : Inherits TaskParent
        Dim dist As New Distance_Depth
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, 0)
            desc = "Find the peaks in the depth data.  NOTE: use global option 'DebugSlider' to provide the threshold value."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dist.Run(src)
            dst2 = dist.dst2

            Dim threshold = Math.Abs(task.gOptions.DebugSlider.Value)
            Dim mask = dst2.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3.SetTo(0)
            dst2.CopyTo(dst3, mask)

            Dim mm = GetMinMax(dst2)
            labels(2) = "Distance results of 32F input data (usually Depth data).  Min = " + CStr(CInt(mm.minVal)) + " and max = " + CStr(CInt(mm.maxVal))
        End Sub
    End Class






    Public Class Distance_ClickPoint : Inherits TaskParent
        Dim options As New Options_Distance
        Public Sub New()
            labels(3) = "Inverse of dst2"
            desc = "Click anywhere to visualize the distance to the each pixel."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst1 = task.grayStable.InRange(0, 0)

            task.grayStable.SetTo(255, dst1)
            task.grayStable.Set(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X, 0)
            dst2 = task.grayStable.DistanceTransform(options.distanceType, 0)
            dst3 = 255 - dst2
        End Sub
    End Class






    Public Class Distance_DepthBricks : Inherits TaskParent
        Dim dist As New Distance_Depth
        Public Sub New()
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            task.gOptions.DebugSlider.Value = 20
            desc = "Threshold the maxDist in each brick to highlight centers for key objects.  Use the 'DebugSlider' to provide the value."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dist.Run(src)
            dst2 = dist.dst2
            dst3 = src.Clone

            Dim threshold = Math.Abs(task.gOptions.DebugSlider.Value)
            For Each brick In task.bricks.brickList
                Dim mm = GetMinMax(dst2(brick.rect))
                If mm.maxVal >= threshold Then
                    Dim pt = New cv.Point(mm.maxLoc.X + brick.rect.X, mm.maxLoc.Y + brick.rect.Y)
                    DrawCircle(dst3, pt)
                End If
            Next
        End Sub
    End Class







    Public Class Distance_Contour : Inherits TaskParent
        Dim options As New Options_Distance
        Public Sub New()
            If task.contours Is Nothing Then task.contours = New Contour_Basics_List
            If standalone Then task.gOptions.displayDst0.Checked = True
            If standalone Then task.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Compute the distance of each point from the top contour (or a selected contour.)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            task.contours.Run(src)

            dst2 = task.contours.dst2
            labels(2) = task.contours.labels(2)

            dst3 = src.Clone
            dst3(task.contourD.rect).SetTo(white, task.contourD.mask)

            dst1.SetTo(255)
            dst1(task.contourD.rect).SetTo(0, task.contourD.mask)

            dst0 = dst1.DistanceTransform(options.distanceType, 0)
        End Sub
    End Class
End Namespace