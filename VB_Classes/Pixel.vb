Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Public Class Pixel_Viewer : Inherits VB_Parent
    Dim firstUpdate = True
    Public viewerForm As New PixelViewerForm
    Dim mouseLoc = New cv.Point(10, 10) ' assume 
    Enum displayTypes
        noType = -1
        type8uC3 = 0
        type8u = 1
        type32F = 2
        type32FC3 = 3
        type32SC1 = 4
        type32SC3 = 5
    End Enum
    Public Sub New()
        desc = "Display pixels under the cursor"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            task.dst0 = task.color.Clone
            task.dst1 = task.depthRGB.Clone
            task.dst2 = New cv.Mat(task.dst1.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
            task.dst3 = New cv.Mat(task.dst1.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        End If

        Dim dst = Choose(task.mousePicTag + 1, task.dst0, task.dst1, task.dst2, task.dst3)

        Dim displayType = displayTypes.noType
        If dst.Type = cv.MatType.CV_8UC3 Then displayType = displayTypes.type8uC3
        If dst.Type = cv.MatType.CV_8U Then displayType = displayTypes.type8u
        If dst.Type = cv.MatType.CV_32F Then displayType = displayTypes.type32F
        If dst.Type = cv.MatType.CV_32FC3 Then displayType = displayTypes.type32FC3
        If dst.Type = cv.MatType.CV_32SC1 Then displayType = displayTypes.type32SC1
        If dst.Type = cv.MatType.CV_32SC3 Then displayType = displayTypes.type32SC3
        If displayType < 0 Or dst.Channels() > 3 Then
            SetTrueText("The pixel Viewer does not support this cv.Mat!  Please add support.")
            Exit Sub
        End If

        Dim formatType = Choose(displayType + 1, "8UC3", "8UC1", "32FC1", "32FC3", "32SC1", "32SC3")
        viewerForm.Text = "Pixel Viewer for " + Choose(task.mousePicTag + 1, "Color", "RGB Depth", "dst2", "dst3") + " " + formatType

        ' yeah, kind of a mess but lots of factors...
        Dim drWidth As Integer = Choose(displayType + 1, 5, 17, 13, 3, 16, 5) * viewerForm.Width / 450 + 3
        Dim drHeight As Integer = CInt(viewerForm.Height / 16) + If(viewerForm.Height < 400, -3, If(viewerForm.Height < 800, -1, 1))
        If drHeight < 20 Then drHeight = 20

        If viewerForm.mousePoint <> New cv.Point Then
            task.mouseMovePoint += viewerForm.mousePoint
            task.mouseMovePointUpdated = True
            viewerForm.mousePoint = New cv.Point
        End If
        If task.mouseMovePoint.X Or task.mouseMovePoint.Y Then
            Dim x As Integer = If(task.mouseMovePoint.X >= drWidth, task.mouseMovePoint.X - drWidth - 1, 0)
            Dim y As Integer = If(task.mouseMovePoint.Y >= drHeight, task.mouseMovePoint.Y - drHeight - 1, 0)
            If task.mouseMovePoint.X >= drWidth Then x += 2
            If task.mouseMovePoint.Y >= drHeight Then y += 2
            mouseLoc = New cv.Point(x, y)
        End If

        task.pixelViewerRect = New cv.Rect(0, 0, -1, -1)
        task.pixelViewTag = task.mousePicTag
        Dim dw = New cv.Rect(mouseLoc.x, mouseLoc.y, drWidth, drHeight)
        dw = ValidateRect(dw)

        Dim img = dst(dw).Clone

        Dim mm As mmData
        Dim format32f = "0000.0"
        Dim format32S = "0000"
        If img.Type = cv.MatType.CV_32F Or img.Type = cv.MatType.CV_32FC3 Then
            If img.Channels() = 3 Then
                Dim tmp = img.Reshape(1)
                mm = GetMinMax(tmp)
            Else
                mm = GetMinMax(img)
            End If
            If mm.minVal >= 0 Then
                If mm.maxVal < 1000 Then format32f = "000.00"
                If mm.maxVal < 100 Then format32f = "00.000"
                If mm.maxVal < 10 Then format32f = "0.0000"
            Else
                mm.maxVal = Math.Max(-mm.minVal, mm.maxVal)
                format32f = " 0.000;-0.000"
                If mm.maxVal < 1000 Then format32f = " 000.0;-000.0"
                If mm.maxVal < 100 Then format32f = " 00.00;-00.00"
                If mm.maxVal < 10 Then format32f = " 0.000;-0.000"
            End If
        End If

        Dim imgText = ""
        Dim ClickPoint = New cv.Point(task.ClickPoint.X - dw.X, task.ClickPoint.Y - dw.Y)
        Select Case displayType

            Case displayTypes.type8uC3
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        Dim vec = img.Get(Of cv.Vec3b)(y, x)
                        imgText += Format(vec(0), "000") + " " + Format(vec(1), "000") + " " + Format(vec(2), "000") + "   "
                    Next
                    imgText += vbLf
                Next

            Case displayTypes.type8u
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        If (task.toggleOnOff And y = ClickPoint.Y) And (x = ClickPoint.X - 1 Or x = ClickPoint.X) Then
                            imgText += Format(img.Get(Of Byte)(y, x), "000") + If((dw.X + x) Mod 5 = 4, "***", "*")
                        Else
                            imgText += Format(img.Get(Of Byte)(y, x), "000") + If((dw.X + x) Mod 5 = 4, "   ", " ")
                        End If
                    Next
                    imgText += vbLf
                Next

            Case displayTypes.type32F
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        imgText += Format(img.Get(Of Single)(y, x), format32f) + If((dw.X + x) Mod 5 = 4, "   ", " ")
                    Next
                    imgText += vbLf
                Next

            Case displayTypes.type32FC3
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        Dim vec = img.Get(Of cv.Vec3f)(y, x)
                        imgText += Format(vec(0), format32f) + " " + Format(vec(1), format32f) + " " + Format(vec(2), format32f) + "   "
                    Next
                    imgText += vbLf
                Next

            Case displayTypes.type32SC1
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        imgText += Format(img.Get(Of Integer)(y, x), format32S) + "  "
                    Next
                    imgText += vbLf
                Next
            Case displayTypes.type32SC3
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth - 1)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        Dim vec = img.Get(Of cv.Vec3i)(y, x)
                        imgText += Format(vec(0), format32S) + " " + Format(vec(1), format32S) + " " + Format(vec(2), format32S) + "   "
                    Next
                    imgText += vbLf
                Next
        End Select
        task.pixelViewerRect = dw

        If viewerForm.rtb.Text <> imgText Then
            If firstUpdate Then viewerForm.rtb.Text = imgText Else viewerForm.saveText = imgText
            firstUpdate = False
        End If

        desc = "Display pixels under the cursor"
        SetTrueText("Move the mouse to location that you want to inspect." + vbCrLf +
                    "Click and hold the right-mouse button to move away from that location")
    End Sub
    Public Sub closeViewer()
        If viewerForm IsNot Nothing Then viewerForm.Close()
    End Sub
End Class








' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet : Inherits VB_Parent
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Time to copy using get/set,Generic Index, Marshal Copy"
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim rows = src.Height
        Dim cols = src.Width
        Dim output As String = ""
        Dim rgb = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)

        Dim watch = Stopwatch.StartNew()
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = rgb.Get(Of cv.Vec3b)(y, x)
                color(0).SwapWith(color(2))
                mats.mat(0).Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
        watch.Stop()
        output += "Upper left image is GetSet and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        mats.mat(1) = rgb.Clone()
        watch = Stopwatch.StartNew()
        Dim indexer = mats.mat(1).GetGenericIndexer(Of cv.Vec3b)
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = indexer(y, x)
                color(0).SwapWith(color(2))
                indexer(y, x) = color
            Next
        Next
        watch.Stop()
        output += "Upper right image is Generic Indexer and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        watch = Stopwatch.StartNew()
        Dim colorArray(cols * rows * rgb.ElemSize - 1) As Byte
        Marshal.Copy(rgb.Data, colorArray, 0, colorArray.Length)
        For i = 0 To colorArray.Length - 3 Step 3
            colorArray(i).SwapWith(colorArray(i + 2))
        Next
        mats.mat(2) = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, colorArray)
        watch.Stop()
        output += "Marshal Copy took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf

        SetTrueText(output, New cv.Point(src.Width / 2 + 10, src.Height / 2 + 20))

        mats.Run(Empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Pixel_Measure : Inherits VB_Parent
    Public Sub New()
        Dim maxZ = task.MaxZmeters * 1000
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Distance in mm", 50, If(maxZ < 1500, 1500, maxZ), maxZ)
        desc = "Compute how many pixels per meter at a requested distance"
    End Sub
    Public Function Compute(mmDist As Single) As Single
        Dim halfLineInMeters = Math.Tan(0.0174533 * task.hFov / 2) * mmDist
        Return halfLineInMeters * 2 / dst2.Width
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static distanceSlider = FindSlider("Distance in mm")
        Dim mmPP = Compute(distanceSlider.Value)
        SetTrueText("At a distance of " + CStr(distanceSlider.Value) + " mm's the camera's FOV is " +
                    Format(mmPP * src.Width / 1000, fmt2) + " meters wide" + vbCrLf +
                    "Pixels are " + Format(mmPP, fmt2) + " mm per pixel at " +
                    CStr(distanceSlider.Value) + " mm's in the image view")
    End Sub
End Class









Public Class Pixel_SampleColor : Inherits VB_Parent
    Public random As New Random_Basics
    Public maskColor As cv.Vec3b
    Public Sub New()
        desc = "Find the dominanant pixel color - not an average! This can provide consistent colorizing."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeat Then
                Dim w = 25, h = 25
                If task.drawRect <> New cv.Rect Then
                    random.range = task.drawRect
                Else
                    random.range = New cv.Rect(msRNG.Next(0, src.Width - w), msRNG.Next(0, src.Height - h), w, h)
                End If
            End If
        Else
            random.range = New cv.Rect(0, 0, src.Width, src.Height)
        End If

        Dim index As New List(Of cv.Point)
        Dim pixels As New List(Of cv.Vec3b)
        Dim counts As New List(Of Integer)
        Dim pixel0 = New cv.Vec3b
        random.Run(Empty)
        For Each pt In random.PointList
            Dim pixel = src.Get(Of cv.Vec3b)(pt.Y, pt.X)
            If pixel <> pixel0 Then
                If pixels.Contains(pixel) Then
                    counts(pixels.IndexOf(pixel)) += 1
                Else
                    pixels.Add(pixel)
                    counts.Add(1)
                End If
            End If
        Next

        If pixels.Count > 0 Then maskColor = pixels(counts.IndexOf(counts.Max))

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(random.range, cv.Scalar.White, 1)
            For Each pt In random.PointList
                DrawCircle(dst2,pt, task.DotSize, cv.Scalar.White)
            Next
            labels(2) = "Dominant color value = " + CStr(maskColor(0)) + ", " + CStr(maskColor(1)) + ", " + CStr(maskColor(2))
            SetTrueText("Draw in the image to select a region for testing.", New cv.Point(10, 200), 3)
        End If
    End Sub
End Class






Public Class Pixel_Unstable : Inherits VB_Parent
    Dim km As New KMeans_Basics
    Dim pixelCounts As New List(Of Integer)
    Dim k As Integer = -1
    Dim unstable As New List(Of cv.Mat)
    Dim lastImage As cv.Mat
    Public unstablePixels As New cv.Mat
    Dim kSlider = FindSlider("KMeans k")
    Public Sub New()
        task.gOptions.setPixelDifference(2)
        labels(2) = "KMeans_Basics output"
        desc = "Detect where pixels are unstable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        k = kSlider.Value

        km.Run(src)
        dst2 = km.dst2
        dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        If lastImage Is Nothing Then lastImage = dst2.Clone
        cv.Cv2.Subtract(dst2, lastImage, dst3)
        dst3 = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)

        unstable.Add(dst3)
        If unstable.Count > task.frameHistoryCount Then unstable.RemoveAt(0)

        unstablePixels = unstable(0)
        For i = 1 To unstable.Count - 1
            unstablePixels = unstablePixels Or unstable(i)
        Next
        dst3 = unstablePixels
        Dim unstableCount = dst3.CountNonZero

        pixelCounts.Add(unstableCount)
        If pixelCounts.Count > 10 Then pixelCounts.RemoveAt(0)

        ' compute stdev from the list
        Dim avg = pixelCounts.Average()
        Dim sum = pixelCounts.Sum(Function(d As Integer) Math.Pow(d - avg, 2))
        Dim stdev = Math.Sqrt(sum / pixelCounts.Count)
        labels(3) = "Unstable pixel count = " + Format(avg, "###,##0") + "    stdev = " + Format(stdev, "0.0")
        lastImage = dst2.Clone
    End Sub
End Class







Public Class Pixel_Zoom : Inherits VB_Parent
    Public mousePoint = New cv.Point(msRNG.Next(0, dst1.Width / 2), msRNG.Next(0, dst1.Height))
    Public zoomSlider As TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Zoom Factor", 2, 16, 4)
        labels(2) = "To zoom move the mouse over the image"
        zoomSlider = FindSlider("Zoom Factor")
        desc = "Zoom into the pixels under the mouse in dst2"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim zoomArray() = {2, 2, 2, 2, 4, 4, 4, 4, 8, 8, 8, 8, 8, 8, 8, 8, 16}
        Dim zoomFactor = zoomArray(zoomSlider.Value)

        If task.mouseMovePoint <> New cv.Point Then mousePoint = task.mouseMovePoint
        Dim width As Double = src.Width / zoomFactor
        Dim height As Double = src.Height / zoomFactor
        Dim x = Math.Min(mousePoint.X, src.Width - width)
        Dim y = Math.Min(mousePoint.Y, src.Height - height)
        dst1 = src(New cv.Rect(CInt(x), CInt(y), width, height))
        dst2 = dst1.Resize(dst2.Size(), 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class Pixel_SubPixel : Inherits VB_Parent
    Public zoom As New Pixel_Zoom
    Public Sub New()
        desc = "Show how to use the GetRectSubPix OpenCV API"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim zoomArray() = {2, 2, 2, 2, 4, 4, 4, 4, 8, 8, 8, 8, 8, 8, 8, 8, 16}
        Dim zoomFactor = zoomArray(zoom.zoomSlider.Value)

        If task.mouseMovePoint <> New cv.Point Then zoom.mousePoint = task.mouseMovePoint
        Dim width As Double = src.Width / zoomFactor
        Dim height As Double = src.Height / zoomFactor
        Dim x = Math.Min(zoom.mousePoint.X, src.Width - width)
        Dim y = Math.Min(zoom.mousePoint.Y, src.Height - height)
        dst3 = src.GetRectSubPix(New cv.Size(width, height), New cv.Point2f(x, y)).Resize(dst2.Size)
        Dim r = New cv.Rect(x - width \ 2, y - height \ 2, width, height)
        r = ValidateRect(r)
        dst2 = src(r).Resize(dst2.Size)
        labels(2) = "Pixel_SubPixel: No SubPixel zoom with factor " + CStr(zoomFactor)
        labels(3) = "Pixel_SubPixel: SubPixel zoom with factor " + CStr(zoomFactor)
    End Sub
End Class





Public Class Pixel_NeighborsHorizontal : Inherits VB_Parent
    Public options As New Options_Neighbors
    Public pt1 As New List(Of cv.Point)
    Public pt2 As New List(Of cv.Point)
    Public Sub New()
        desc = "Manual step through depth data to find horizontal neighbors within x mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        pt1.Clear()
        pt2.Clear()
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - options.pixels - 1
                Dim x1 = src.Get(Of Single)(y, x)
                Dim x2 = src.Get(Of Single)(y, x + options.pixels)
                If x1 = 0 Or x2 = 0 Then Continue For
                If Math.Abs(x1 - x2) <= options.threshold Then
                    pt1.Add(New cv.Point(x, y))
                    pt2.Add(New cv.Point(x + options.pixels, y))
                    x += options.pixels
                End If
            Next
        Next

        dst2 = task.color.Clone
        For i = 0 To pt1.Count - 1
            DrawLine(dst2, pt1(i), pt2(i), cv.Scalar.Yellow)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + Format(options.threshold * 1000, fmt0) + " mm's with X pixel offset " + CStr(options.pixels)
    End Sub
End Class








Public Class Pixel_NeighborsVertical : Inherits VB_Parent
    Public options As New Options_Neighbors
    Public pt1 As New List(Of cv.Point)
    Public pt2 As New List(Of cv.Point)
    Public Sub New()
        desc = "Manual step through depth data to find vertical neighbors within x mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        pt1.Clear()
        pt2.Clear()
        For x = 0 To src.Width - 1
            For y = 0 To src.Height - options.pixels - 1
                Dim x1 = src.Get(Of Single)(y, x)
                Dim x2 = src.Get(Of Single)(y + options.pixels, x)
                If x1 = 0 Or x2 = 0 Then Continue For
                If Math.Abs(x1 - x2) <= options.threshold Then
                    pt1.Add(New cv.Point(x, y))
                    pt2.Add(New cv.Point(x, y + options.pixels))
                    y += options.pixels
                End If
            Next
        Next

        dst2 = task.color.Clone
        For i = 0 To pt1.Count - 1
            DrawLine(dst2, pt1(i), pt2(i), cv.Scalar.Yellow)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + Format(options.threshold * 1000, fmt0) + " mm's with Y pixel offset " + CStr(options.pixels)
    End Sub
End Class







Public Class Pixel_NeighborsMaskH : Inherits VB_Parent
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where horizontal neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim r1 = New cv.Rect(0, 0, dst2.Width, dst2.Height - options.pixels)
        Dim r2 = New cv.Rect(0, options.pixels, dst2.Width, dst2.Height - options.pixels)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(options.threshold, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(dst2.Width - options.pixels, 0, options.pixels, dst2.Height)).SetTo(0)
        labels(2) = "White: z is within " + Format(options.threshold, fmt0) + " mm's with X pixel offset " + CStr(options.pixels)
    End Sub
End Class







Public Class Pixel_NeighborsMaskV : Inherits VB_Parent
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim r1 = New cv.Rect(0, 0, dst2.Width, dst2.Height - options.pixels)
        Dim r2 = New cv.Rect(0, options.pixels, dst2.Width, dst2.Height - options.pixels)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(options.threshold, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(dst2.Width - options.pixels, 0, options.pixels, dst2.Height)).SetTo(0)
        labels(2) = "White: z is within " + Format(options.threshold, fmt0) + " mm's with X pixel offset " + CStr(options.pixels)
    End Sub
End Class









Public Class Pixel_NeighborsMask : Inherits VB_Parent
    Dim maskH As New Pixel_NeighborsMaskH
    Dim maskV As New Pixel_NeighborsMaskV
    Public Sub New()
        desc = "Show the mask for both the horizontal and vertical neighbors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        maskH.Run(src)
        dst2 = maskH.dst2

        maskV.Run(src)
        dst3 = maskV.dst2
    End Sub
End Class






Public Class Pixel_NeighborsPatchNeighbors : Inherits VB_Parent
    Public options As New Options_Neighbors
    Public Sub New()
        FindSlider("Minimum offset to neighbor pixel").Value = 1
        desc = "Update depth values for neighbors where they are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        dst2 = src
        If options.patchZ Then
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - options.pixels - 1 Step options.pixels
                    Dim x1 = src.Get(Of Single)(y, x)
                    Dim x2 = src.Get(Of Single)(y, x + options.pixels)
                    If x1 = 0 Then Continue For
                    If Math.Abs(x1 - x2) <= options.threshold Then
                        For i = x To x + options.pixels
                            dst2.Set(Of Single)(y, i, x1)
                        Next
                    End If
                Next
            Next

            For x = 0 To src.Width - 1
                For y = 0 To src.Height - options.pixels - 1 Step options.pixels
                    Dim y1 = src.Get(Of Single)(y, x)
                    Dim y2 = src.Get(Of Single)(y + options.pixels, x)
                    If y1 = 0 Then Continue For
                    If Math.Abs(y1 - y2) <= options.threshold Then
                        For i = y To y + options.pixels
                            dst2.Set(Of Single)(i, x, y1)
                        Next
                    End If
                Next
            Next
            labels(2) = "Updated z-values within " + Format(options.threshold * 1000, fmt0) + " mm's with X pixel offset " + CStr(options.pixels)
        Else
            labels(2) = "Z-values not updated "
        End If
        cv.Cv2.Merge(task.pcSplit, dst3)
    End Sub
End Class







Public Class Pixel_Vector3D : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Dim distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
    Public pixelVector As New List(Of List(Of Single))
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.redOptions.HistBinBar3D.Value = 3
        labels = {"", "RedCloud_Basics output", "3D Histogram counts for each of the cells at left", ""}
        desc = "Identify RedCloud cells and create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        Dim maxRegion = 20

        If task.heartBeat Then
            pixelVector.Clear()
            strOut = "3D histogram counts for each cell - " + CStr(maxRegion) + " largest only for readability..." + vbCrLf
            For Each cell In task.redCells
                hColor.inputMask = cell.mask
                hColor.Run(src(cell.rect))
                pixelVector.Add(hColor.histArray.ToList)
                strOut += "(" + CStr(cell.index) + ") "
                For Each count In hColor.histArray
                    strOut += CStr(count) + ","
                Next
                strOut += vbCrLf
                If cell.index >= maxRegion Then Exit For
            Next
        End If
        SetTrueText(strOut, 3)

        dst1.SetTo(0)
        dst2.SetTo(0)
        For Each cell In task.redCells
            task.color(cell.rect).CopyTo(dst2(cell.rect), cell.mask)
            dst1(cell.rect).SetTo(cell.color, cell.mask)
            If cell.index <= maxRegion Then SetTrueText(CStr(cell.index), cell.maxDist, 2)
        Next
        labels(2) = redC.labels(3)
    End Sub
End Class





Public Class Pixel_Unique_CPP_VB : Inherits VB_Parent
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        cPtr = Pixels_Vector_Open()
        desc = "Create the list of pixels in a RedCloud Cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.Resize(task.lowRes)
        If task.drawRect <> New cv.Rect Then src = src(task.drawRect)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim classCount = Pixels_Vector_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If classCount = 0 Then Exit Sub
        Dim pixelData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_8UC3, Pixels_Vector_Pixels(cPtr))
        SetTrueText(CStr(classCount) + " unique BGR pixels were found in the src after resizing to low resolution." + vbCrLf +
                    "Or " + Format(classCount / src.Total, "0%") + " of the input were unique pixels.")
    End Sub
    Public Sub Close()
        Pixels_Vector_Close(cPtr)
    End Sub
End Class





Public Class Pixel_Vectors : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Dim hVector As New Hist3Dcolor_Vector
    Public pixelVector As New List(Of Single())
    Public redCells As New List(Of rcData)
    Dim distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", ""}
        desc = "Create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)

        pixelVector.Clear()
        For Each cell In task.redCells
            hVector.inputMask = cell.mask
            hVector.Run(src(cell.rect))
            pixelVector.Add(hVector.histArray)
        Next
        redCells = task.redCells

        SetTrueText("3D color histograms were created for " + CStr(pixelVector.Count) + " cells", 3)
    End Sub
End Class





Public Class Pixel_Mapper : Inherits VB_Parent
    Public colorMap As New cv.Mat(256, 1, cv.MatType.CV_8UC3, cv.Scalar.All(0))
    Public Sub New()
        desc = "Resize the input to a small image, convert to gray, and map gray to color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            Dim nSize = New cv.Size(src.Width / 8, src.Height / 8)
            dst1 = src.Resize(nSize)
            Dim samples(dst1.Total * dst1.ElemSize - 1) As Byte
            Marshal.Copy(dst1.Data, samples, 0, samples.Length)

            Dim sorted As New SortedList(Of Integer, cv.Vec3b)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To samples.Count - 1 Step 3
                Dim vecA = New cv.Vec3b(samples(i), samples(i + 1), samples(i + 2))
                Dim gPixel = CInt(vecA(2) * 0.299 + vecA(1) * 0.587 + vecA(0) * 0.114)
                If sorted.ContainsKey(gPixel) = False Then sorted.Add(gPixel, vecA)
            Next

            Dim averaged As New SortedList(Of Integer, cv.Vec3b)
            For i = 0 To sorted.Count - 1
                Dim ele = sorted.ElementAt(i)
                Dim index = ele.Key
                Dim vecA = ele.Value
                For j = i + 1 To sorted.Count - 1
                    If ele.Key <> sorted.ElementAt(j).Key Then Exit For
                    Dim vecB = sorted.ElementAt(j).Value
                    vecA = New cv.Vec3b(vecA(0) / 2 + vecB(0) / 2, vecA(1) / 2 + vecB(1) / 2, vecA(2) / 2 + vecB(2) / 2)
                    i = j
                Next
                averaged.Add(index, vecA)
            Next

            Dim vec = averaged.ElementAt(0).Value
            Dim iAvg = averaged.ElementAt(0).Key
            For i = 0 To 255
                If i < averaged.Count Then
                    If iAvg <> averaged.ElementAt(i).Key Then vec = averaged.ElementAt(i).Value
                End If
                colorMap.Set(Of cv.Vec3b)(i, vec)
            Next
        End If
        cv.Cv2.ApplyColorMap(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst2, colorMap)
    End Sub
End Class






Public Class Pixel_MapLeftRight : Inherits VB_Parent
    Dim mapper As New Pixel_Mapper
    Public Sub New()
        labels = {"", "", "Left view with averaged color", "Right view with averaged color"}
        desc = "Map the left and right grayscale images using the same colormap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mapper.Run(src)
        dst2 = mapper.dst2

        cv.Cv2.ApplyColorMap(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst3, mapper.colorMap)
    End Sub
End Class







Public Class Pixel_MapDistance : Inherits VB_Parent
    Dim mapper As New Pixel_Mapper
    Public Sub New()
        labels = {"", "", "Left view with averaged color after distance reduction", "Right view with averaged color after distance reduction"}
        desc = "Map the left and right grayscale images using the same colormap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mapper.Run(src)
        dst2 = mapper.dst2

        Static myColorMap As cv.Mat = mapper.colorMap.Clone
        If task.heartBeat Then
            Dim samples(mapper.colorMap.Total * mapper.colorMap.ElemSize - 1) As Byte
            Marshal.Copy(mapper.colorMap.Data, samples, 0, samples.Length)

            Dim vecs As New List(Of cv.Point3f)
            Dim vecs3b As New List(Of Byte)
            For i = 0 To samples.Count - 1 Step 3
                vecs.Add(New cv.Point3f(samples(i), samples(i + 1), samples(i + 2)))
                vecs3b.Add(samples(i))
                vecs3b.Add(samples(i + 1))
                vecs3b.Add(samples(i + 2))
            Next

            Dim distances As New List(Of Double)
            For i = 0 To vecs.Count - 2
                Dim vecA = vecs(i)
                Dim vecB = vecs(i + 1)
                distances.Add(distance3D(vecA, vecB))
            Next

            Dim avg = distances.Average
            Dim vec = New cv.Vec3b(vecs3b(0), samples(1), samples(2))
            For i = 0 To vecs.Count - 1
                If i < 255 Then If distances(i) > avg Then vec = New cv.Vec3b(vecs3b(i * 3), samples(i * 3 + 1), samples(i * 3 + 2))
                vecs3b(i * 3) = vec(0)
                vecs3b(i * 3 + 1) = vec(1)
                vecs3b(i * 3 + 2) = vec(2)
            Next

            Marshal.Copy(vecs3b.ToArray, 0, mapper.colorMap.Data, myColorMap.Total * myColorMap.ElemSize)
        End If
        cv.Cv2.ApplyColorMap(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst2, myColorMap)
        cv.Cv2.ApplyColorMap(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst3, myColorMap)
    End Sub
End Class








Public Class Pixel_Sampler : Inherits VB_Parent
    Public random As New Random_Basics
    Public dominantGray As Byte
    Dim width = 25
    Dim height = 25
    Public Sub New()
        desc = "Find the dominanant pixel color - not an average! This can provide consistent colorizing."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeat Then
                If task.drawRect <> New cv.Rect Then
                    random.range = task.drawRect
                Else
                    random.range = New cv.Rect(msRNG.Next(0, src.Width - width), msRNG.Next(0, src.Height - height), width, height)
                End If
            End If
        Else
            random.range = New cv.Rect(0, 0, src.Width, src.Height)
        End If
        random.Run(empty)

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim index As New List(Of cv.Point)
        Dim pixels As New List(Of Byte)
        Dim counts(random.PointList.Count - 1) As Integer
        For Each pt In random.PointList
            Dim pixel = src.Get(Of Byte)(pt.Y, pt.X)
            If pixel <> 0 Then
                If pixels.Contains(pixel) Then
                    counts(pixels.IndexOf(pixel)) += 1
                Else
                    pixels.Add(pixel)
                    counts(pixels.IndexOf(pixel)) = 1
                End If
            End If
        Next

        Dim maxValue = counts.Max
        If pixels.Count > 0 Then
            For i = 0 To counts.Count - 1
                If counts(i) = maxValue Then
                    dominantGray = pixels.ElementAt(i)
                    Exit For
                End If
            Next
        Else
            dominantGray = GetMinMax(src).maxVal
        End If

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(random.range, cv.Scalar.White, 1)
            For Each pt In random.PointList
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.White)
            Next
            labels(2) = "Dominant gray value = " + CStr(dominantGray)
            SetTrueText("Draw in the image to select a region for testing.", New cv.Point(10, 200), 3)
        End If
    End Sub
End Class






Public Class Pixel_Display : Inherits VB_Parent
    Public random As New Random_Basics
    Dim width = 25
    Dim height = 25
    Public Sub New()
        If task.drawRect.Width <> 0 Then
            random.range = task.drawRect
        Else
            random.range = New cv.Rect(msRNG.Next(0, dst2.Width - width), msRNG.Next(0, dst2.Height - height), width, height)
        End If
        random.Run(empty)
        task.drawRect = random.range

        labels(2) = "Draw a rectangle anywhere in the image to see the stats for that region."
        desc = "Find the pixels within the drawrect and display their stats."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src
        If task.heartBeat Then
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(src(task.drawRect), mean, stdev)
            Dim pt = New cv.Vec3i(mean(0), mean(1), mean(2))
            strOut = "Mean BGR " + pt.ToString() + vbCrLf + "Stdev BGR " + stdev.ToString
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class Pixel_ColorGuess : Inherits VB_Parent
    Dim mapper As New Pixel_Mapper
    Public Sub New()
        labels = {"", "", "Left view with averaged color after distance reduction", "Right view with averaged color after distance reduction"}
        desc = "Map the left and right grayscale images using the same colormap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mapper.Run(src)
        dst2 = mapper.dst2

        Static myColorMap As cv.Mat = mapper.colorMap.Clone
        If task.heartBeat Then
            Dim samples(mapper.colorMap.Total * mapper.colorMap.ElemSize - 1) As Byte
            Marshal.Copy(mapper.colorMap.Data, samples, 0, samples.Length)

            Dim vecs As New List(Of Integer)
            For i = 0 To samples.Count - 1 Step 3
                vecs.Add(samples(i))
                vecs.Add(samples(i + 1))
                vecs.Add(samples(i + 2))
            Next

            For i = 0 To samples.Count - 1 Step 3
                If Math.Abs(vecs(i + 1) - vecs(i + 2)) < 10 And vecs(i) < vecs(i + 1) Then
                    vecs(i) = 0
                    vecs(i + 1) = 255
                    vecs(i + 2) = 255
                    'ElseIf Math.Abs(vecs(i) - vecs(i + 1)) < 10 And vecs(i + 2) < vecs(i + 1) Then
                    '    vecs(i) = 0
                    '    vecs(i + 1) = 255
                    '    vecs(i + 2) = 255
                End If
            Next

            For i = 0 To myColorMap.Rows - 1
                Dim vec = New cv.Vec3b(vecs(i * 3), vecs(i * 3 + 1), vecs(i * 3 + 2))
                myColorMap.Set(Of cv.Vec3b)(i, 0, vec)
            Next
        End If
        cv.Cv2.ApplyColorMap(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst2, myColorMap)
        cv.Cv2.ApplyColorMap(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst3, myColorMap)
    End Sub
End Class