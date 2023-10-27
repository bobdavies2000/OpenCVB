Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer : Inherits VB_Algorithm
    Dim firstUpdate = True
    Public viewerForm As New PixelViewerForm
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
        task.dst1 = dst2.Clone
        task.dst2 = dst2.Clone
        task.dst3 = dst2.Clone
        desc = "Display pixels under the cursor"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.dst0 Is Nothing Then task.dst0 = task.color
        Dim dst = Choose(task.mousePicTag + 1, task.dst0, task.dst1, task.dst2, task.dst3)

        Dim displayType = displayTypes.noType
        If dst.Type = cv.MatType.CV_8UC3 Then displayType = displayTypes.type8uC3
        If dst.Type = cv.MatType.CV_8U Then displayType = displayTypes.type8u
        If dst.Type = cv.MatType.CV_32F Then displayType = displayTypes.type32F
        If dst.Type = cv.MatType.CV_32FC3 Then displayType = displayTypes.type32FC3
        If dst.Type = cv.MatType.CV_32SC1 Then displayType = displayTypes.type32SC1
        If dst.Type = cv.MatType.CV_32SC3 Then displayType = displayTypes.type32SC3
        If displayType < 0 Or dst.Channels > 3 Then
            setTrueText("The pixel Viewer does not support this cv.Mat!  Please add support.")
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
        Static mouseLoc = New cv.Point(10, 10) ' assume 
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
        dw = validateRect(dw)

        Dim img As cv.Mat
        Try
            img = dst(dw).Clone
        Catch ex As Exception
            Exit Sub
        End Try

        Dim mm As mmData
        Dim format32f = "0000.0"
        Dim format32S = "0000"
        If img.Type = cv.MatType.CV_32F Or img.Type = cv.MatType.CV_32FC3 Then
            If img.Channels = 3 Then
                Dim tmp = img.Reshape(1)
                mm = vbMinMax(tmp)
            Else
                mm = vbMinMax(img)
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
        Dim clickPoint = New cv.Point(task.clickPoint.X - dw.X, task.clickPoint.Y - dw.Y)
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
                        If (task.toggleEverySecond And y = clickPoint.Y) And (x = clickPoint.X - 1 Or x = clickPoint.X) Then
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
        setTrueText("Move the mouse to location that you want to inspect." + vbCrLf +
                    "Click and hold the right-mouse button to move away from that location")
    End Sub
    Public Sub closeViewer()
        If viewerForm IsNot Nothing Then viewerForm.Close()
    End Sub
End Class








' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet : Inherits VB_Algorithm
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Time to copy using get/set,Generic Index, Marshal Copy"
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
        mats.mat(2) = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, colorArray)
        watch.Stop()
        output += "Marshal Copy took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf

        setTrueText(output, New cv.Point(src.Width / 2 + 10, src.Height / 2 + 20))

        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Pixel_Measure : Inherits VB_Algorithm
    Public Sub New()
        Dim maxZ = task.maxZmeters * 1000
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Distance in mm", 50, If(maxZ < 1500, 1500, maxZ), maxZ)
        desc = "Compute how many pixels per meter at a requested distance"
    End Sub
    Public Function Compute(mmDist As Single) As Single
        Dim halfLineInMeters = Math.Tan(0.0174533 * task.hFov / 2) * mmDist
        Return halfLineInMeters * 2 / dst2.Width
    End Function
    Public Sub RunVB(src as cv.Mat)
        Static distanceSlider = findSlider("Distance in mm")
        Dim mmPP = Compute(distanceSlider.Value)
        setTrueText("At a distance of " + CStr(distanceSlider.Value) + " mm's the camera's FOV is " +
                    Format(mmPP * src.Width / 1000, fmt2) + " meters wide" + vbCrLf +
                    "Pixels are " + Format(mmPP, fmt2) + " mm per pixel at " +
                    CStr(distanceSlider.Value) + " mm's in the image view")
    End Sub
End Class








Public Class Pixel_Sampler : Inherits VB_Algorithm
    Public random As New Random_Basics
    Public dominantGray As Byte
    Dim width = 25
    Dim height = 25
    Public Sub New()
        desc = "Find the dominanant pixel color - not an average! This can provide consistent colorizing."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone Then
            If heartBeat() Then
                If task.drawRect <> New cv.Rect Then
                    random.range = task.drawRect
                Else
                    random.range = New cv.Rect(msRNG.Next(0, src.Width - width), msRNG.Next(0, src.Height - height), width, height)
                End If
            End If
        Else
            random.range = New cv.Rect(0, 0, src.Width, src.Height)
        End If
        random.Run(Nothing)

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
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
            dominantGray = vbMinMax(src).maxVal
        End If

        If standalone Then
            dst2 = src
            dst2.Rectangle(random.range, cv.Scalar.White, 1)
            For Each pt In random.PointList
                dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next
            labels(2) = "Dominant gray value = " + CStr(dominantGray)
            setTrueText("Draw in the image to select a region for testing.", New cv.Point(10, 200), 3)
        End If
    End Sub
End Class








Public Class Pixel_SampleColor : Inherits VB_Algorithm
    Public random As New Random_Basics
    Public maskColor As cv.Vec3b
    Public Sub New()
        desc = "Find the dominanant pixel color - not an average! This can provide consistent colorizing."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone Then
            If heartBeat() Then
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
        random.Run(Nothing)
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

        If standalone Then
            dst2 = src
            dst2.Rectangle(random.range, cv.Scalar.White, 1)
            For Each pt In random.PointList
                dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next
            labels(2) = "Dominant color value = " + CStr(maskColor(0)) + ", " + CStr(maskColor(1)) + ", " + CStr(maskColor(2))
            setTrueText("Draw in the image to select a region for testing.", New cv.Point(10, 200), 3)
        End If
    End Sub
End Class






Public Class Pixel_Unstable : Inherits VB_Algorithm
    Dim km As New KMeans_BasicsFast
    Public unstablePixels As New cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("KMeans clustered difference threshold", 1, 50, 5)
        labels(2) = "KMeans_Basics output"
        desc = "Detect where pixels are unstable"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static diffSlider = findSlider("KMeans clustered difference threshold")
        Static kSlider = findSlider("KMeans k")
        Static pixelCounts As New List(Of Integer)
        Static k As Integer = -1
        Static unstable As New List(Of cv.Mat)
        If task.optionsChanged Then
            pixelCounts.Clear()
            unstable.Clear()
            k = kSlider.Value
        End If

        km.Run(src)
        If km.dst2.Channels <> 1 Then
            dst2 = km.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst2 = km.dst2
        End If
        dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        Static lastImage As cv.Mat = dst2
        cv.Cv2.Subtract(dst2, lastImage, dst3)
        dst3 = dst3.Threshold(diffSlider.Value, 255, cv.ThresholdTypes.Binary)

        unstable.Add(dst3)
        If unstable.Count > task.historyCount Then unstable.RemoveAt(0)

        unstablePixels = unstable(0)
        For i = 1 To unstable.Count - 1
            unstablePixels = unstablePixels Or unstable(i)
        Next
        dst3 = unstablePixels
        Dim unstableCount = dst3.CountNonZero

        pixelCounts.Add(unstableCount)
        If pixelCounts.Count > 100 Then pixelCounts.RemoveAt(0)

        ' compute stdev from the list
        Dim avg = pixelCounts.Average()
        Dim sum = pixelCounts.Sum(Function(d As Integer) Math.Pow(d - avg, 2))
        Dim stdev = Math.Sqrt(sum / pixelCounts.Count)
        labels(3) = "Unstable pixel count = " + Format(avg, "###,##0") + "    stdev = " + Format(stdev, "0.0")
        lastImage = dst2.Clone
    End Sub
End Class







Public Class Pixel_Zoom : Inherits VB_Algorithm
    Public mousePoint = New cv.Point(msRNG.Next(0, dst1.Width / 2), msRNG.Next(0, dst1.Height))
    Public zoomSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Zoom Factor", 2, 16, 4)
        labels(2) = "To zoom move the mouse over the image"
        zoomSlider = findSlider("Zoom Factor")
        desc = "Zoom into the pixels under the mouse in dst2"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim zoomArray() = {2, 2, 2, 2, 4, 4, 4, 4, 8, 8, 8, 8, 8, 8, 8, 8, 16}
        Dim zoomFactor = zoomArray(zoomSlider.Value)

        If task.mouseMovePoint <> New cv.Point Then mousePoint = task.mouseMovePoint
        Dim width As Double = src.Width / zoomFactor
        Dim height As Double = src.Height / zoomFactor
        Dim x = Math.Min(mousePoint.X, src.Width - width)
        Dim y = Math.Min(mousePoint.Y, src.Height - height)
        dst1 = src(New cv.Rect(CInt(x), CInt(y), width, height))
        dst2 = dst1.Resize(dst2.Size, 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class Pixel_SubPixel : Inherits VB_Algorithm
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
        r = validateRect(r)
        dst2 = src(r).Resize(dst2.Size)
        labels(2) = "Pixel_SubPixel: No SubPixel zoom with factor " + CStr(zoomFactor)
        labels(3) = "Pixel_SubPixel: SubPixel zoom with factor " + CStr(zoomFactor)
    End Sub
End Class





Public Class Pixel_NeighborsHorizontal : Inherits VB_Algorithm
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
            dst2.Line(pt1(i), pt2(i), cv.Scalar.Yellow, task.lineWidth)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + Format(options.threshold * 1000, fmt0) + " mm's with X pixel offset " + CStr(options.pixels)
    End Sub
End Class








Public Class Pixel_NeighborsVertical : Inherits VB_Algorithm
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
            dst2.Line(pt1(i), pt2(i), cv.Scalar.Yellow, task.lineWidth)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + Format(options.threshold * 1000, fmt0) + " mm's with Y pixel offset " + CStr(options.pixels)
    End Sub
End Class







Public Class Pixel_NeighborsMaskH : Inherits VB_Algorithm
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where horizontal neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
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







Public Class Pixel_NeighborsMaskV : Inherits VB_Algorithm
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
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









Public Class Pixel_NeighborsMask : Inherits VB_Algorithm
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






Public Class Pixel_NeighborsPatchNeighbors : Inherits VB_Algorithm
    Public options As New Options_Neighbors
    Public Sub New()
        findSlider("Minimum offset to neighbor pixel").Value = 1
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