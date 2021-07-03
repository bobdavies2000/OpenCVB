Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer : Inherits VBparent
    Dim firstUpdate = True
    Public viewerForm As New PixelViewerForm
    Public Sub New()
        task.desc = "Display pixels under the cursor"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = Choose(task.mousePicTag + 1, task.color, task.RGBDepth, task.algorithmObject.dst2, task.algorithmObject.dst3)

        Dim displayType = -1 ' default is 8uc3
        If dst2.Type = cv.MatType.CV_8UC3 Then displayType = 0
        If dst2.Type = cv.MatType.CV_8U Then displayType = 1
        If dst2.Type = cv.MatType.CV_32F Then displayType = 2
        If dst2.Type = cv.MatType.CV_32FC3 Then displayType = 3
        If displayType < 0 Or dst2.Channels > 4 Then
            setTrueText("The pixel Viewer does not support this cv.Mat!  Please add support.")
            Exit Sub
        End If

        Dim formatType = Choose(displayType + 1, "8UC3", "8UC1", "32FC1", "32FC3")
        viewerForm.Text = "Pixel Viewer for " + Choose(task.mousePicTag + 1, "Color", "RGB Depth", "dst2", "dst3") + " " + formatType

        Dim drWidth = Choose(displayType + 1, 7, 22, 13, 4) * viewerForm.Width / 650 + 3
        Dim drHeight = CInt(viewerForm.Height / 16) + If(viewerForm.Height < 400, -3, If(viewerForm.Height < 800, -1, 1))
        If drHeight < 20 Then drHeight = 20

        If viewerForm.mousePoint <> New cv.Point Then
            task.mousePoint += viewerForm.mousePoint
            task.mousePointUpdated = True
            viewerForm.mousePoint = New cv.Point
        End If
        Static mouseLoc = New cv.Point(100, 100) ' assume 
        If task.mousePoint.X Or task.mousePoint.Y Then
            Dim x = If(task.mousePoint.X >= drWidth, CInt(task.mousePoint.X - drWidth), 0)
            Dim y = If(task.mousePoint.Y >= drHeight, task.mousePoint.Y - drHeight, 0)
            mouseLoc = New cv.Point(CInt(x), CInt(y))
        End If

        task.pixelViewerRect = New cv.Rect(0, 0, -1, -1)
        task.pixelViewTag = task.mousePicTag
        Dim dw = New cv.Rect(mouseLoc.x, mouseLoc.y, drWidth, drHeight)
        If dw.X < 0 Then dw.X = 0
        If dw.Y < 0 Then dw.Y = 0
        If dw.X + dw.Width > dst2.Width Then
            dw.X = dst2.Width - dw.Width
            dw.Width = dw.Width
        End If
        If dw.Y + dw.Height > dst2.Height Then
            dw.Y = dst2.Height - dw.Height
            dw.Height = dw.Height
        End If

        Dim testChange As cv.Mat = If(dst2.Channels = 1, dst2(dw).Clone, dst2(dw).CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Dim diff As New cv.Mat
        Static saveviewerForm As cv.Mat = testChange
        If saveviewerForm.Size <> testChange.Size Or saveviewerForm.Type <> testChange.Type Then
            saveviewerForm = testChange.Clone
        Else
            cv.Cv2.Absdiff(saveviewerForm, testChange, diff)
        End If

        Dim img = dst2(dw)
        Dim minVal As Single = 0, maxVal As Single = 255
        Dim format32f = "0000.0"
        If img.Type = cv.MatType.CV_32F Or img.Type = cv.MatType.CV_32FC3 Then
            img.MinMaxLoc(minVal, maxVal)
            If minVal >= 0 Then
                If maxVal < 1000 Then format32f = "000.00"
                If maxVal < 100 Then format32f = "00.000"
                If maxVal < 10 Then format32f = "0.0000"
            Else
                maxVal = Math.Max(-minVal, maxVal)
                format32f = " 0.000;-0.000"
                If maxVal < 1000 Then format32f = " 000.0;-000.0"
                If maxVal < 100 Then format32f = " 00.00;-00.00"
                If maxVal < 10 Then format32f = " 0.000;-0.000"
            End If
        End If

        saveviewerForm = testChange.Clone

        Dim imgText = ""
        Select Case displayType

            Case 0
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        Dim vec = img.Get(Of cv.Vec3b)(y, x)
                        imgText += Format(vec.Item0, "000") + " " + Format(vec.Item1, "000") + " " + Format(vec.Item2, "000") + "   "
                    Next
                    imgText += vbLf
                Next

            Case 1
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        imgText += Format(img.Get(Of Byte)(y, x), "000") + If((dw.X + x) Mod 5 = 4, "   ", " ")
                    Next
                    imgText += vbLf
                Next

            Case 2
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        imgText += Format(img.Get(Of Single)(y, x), format32f) + If((dw.X + x) Mod 5 = 4, "   ", " ")
                    Next
                    imgText += vbLf
                Next

            Case 3
                imgText += If(dw.X + drWidth > 1000, " col    ", " col    ") + CStr(dw.X) + " through " + CStr(CInt(dw.X + drWidth)) + vbLf
                For y = 0 To img.Height - 1
                    imgText += "r" + Format(dw.Y + y, "000") + "   "
                    For x = 0 To img.Width - 1
                        Dim vec = img.Get(Of cv.Vec3f)(y, x)
                        imgText += Format(vec.Item0, format32f) + " " + Format(vec.Item1, format32f) + " " + Format(vec.Item2, format32f) + "   "
                    Next
                    imgText += vbLf
                Next

        End Select
        task.pixelViewerRect = dw

        If viewerForm.rtb.Text <> imgText Then
            If viewerForm.UpdateFrequency.SelectedIndex = 0 Or firstUpdate Then viewerForm.rtb.Text = imgText Else viewerForm.saveText = imgText
            firstUpdate = False
        End If

        If task.desc = "Display pixels under the cursor" Then
            setTrueText("Move the mouse to location that you want to inspect." + vbCrLf +
                          "Click and hold the right-mouse button to move away from that location")
        End If
    End Sub
    Public Sub closeViewer()
        If viewerForm IsNot Nothing Then viewerForm.Close()
    End Sub
End Class








' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet : Inherits VBparent
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Time to copy using get/set,Generic Index, Marshal Copy"
        labels(3) = "Click any quadrant at left to view it below"
        task.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim rows = src.Height
        Dim cols = src.Width
        Dim output As String = ""
        Dim rgb = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)

        Dim watch = Stopwatch.StartNew()
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = rgb.Get(Of cv.Vec3b)(y, x)
                color.Item0.SwapWith(color.Item2)
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
                color.Item0.SwapWith(color.Item2)
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

        setTrueText(output, src.Width / 2 + 10, src.Height / 2 + 20)

        mats.RunClass(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Pixel_Measure : Inherits VBparent
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Distance in mm", 50, task.maxZ * 1000, 1500)
        End If

        task.desc = "Compute how many pixels per meter at a requested distance"
    End Sub
    Public Function Compute(mmDist As Single) As Single
        Dim halfLineInMeters = Math.Tan(0.0174533 * task.hFov / 2) * mmDist
        Return halfLineInMeters * 2 / dst2.Width
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static distanceSlider = findSlider("Distance in mm")
        Dim mmPP = Compute(distanceSlider.value)
        If standalone Then
            setTrueText("At a distance of " + CStr(distanceSlider.value) + " mm's the camera's FOV is " +
                           Format(mmPP * src.Width / 1000, "#0.00") + " meters wide" + vbCrLf +
                          "Pixels are " + Format(mmPP, "#0.00") + " mm per pixel at " +
                           CStr(distanceSlider.value) + " mm's in the image view", 10, 60)
        End If
    End Sub
End Class








Public Class Pixel_Sampler : Inherits VBparent
    Public random As New Random_Basics
    Public dominantGray As Byte
    Dim width = 100
    Dim height = 100
    Public Sub New()
        task.desc = "Find the dominanant pixel color - not an average! This can provide consistent colorizing."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        If standalone Then
            If task.frameCount Mod 30 = 0 Then
                If task.drawRect <> New cv.Rect Then
                    random.rangeRect = task.drawRect
                Else
                    random.rangeRect = New cv.Rect(msRNG.Next(0, src.Width - width), msRNG.Next(0, src.Height - height), width, height)
                End If
            End If
        Else
            random.rangeRect = New cv.Rect(0, 0, src.Width, src.Height)
        End If
        random.RunClass(Nothing)

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim index As New List(Of cv.Point)
        Dim pixels As New List(Of Byte)
        Dim counts(random.Points2f.Count - 1) As Integer
        For i = 0 To random.Points2f.Count - 1
            Dim pt = random.Points2f(i)
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
            cv.Cv2.MinMaxLoc(src, minval, maxval)
            dominantGray = maxVal
        End If

        If standalone Then
            dst2 = src
            dst2.Rectangle(random.rangeRect, cv.Scalar.White, 1)
            For i = 0 To random.Points2f.Count - 1
                dst2.Circle(random.Points2f(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next
            labels(2) = "Dominant gray value = " + CStr(dominantGray)
            setTrueText("Draw in the image to select a region for testing.", 10, 200, 3)
        End If
    End Sub
End Class






Public Class Pixel_Unstable : Inherits VBparent
    Dim km As New KMeans_BasicsFast
    Public unstablePixels As New cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "KMeans clustered difference threshold", 1, 50, 5)
        labels(2) = "KMeans_Basics output"
        task.desc = "Detect where pixels are unstable"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        Static diffSlider = findSlider("KMeans clustered difference threshold")
        Static retainSlider = findSlider("Retain x frames to measure unstable pixels")
        If task.frameCount = 0 Then retainSlider.enabled = True
        Static kSlider = findSlider("kMeans k")
        Static saveMaskIndex = -1
        Static pixelCounts As New List(Of Integer)
        Static k As Integer = -1
        Static unstable As New List(Of cv.Mat)
        If saveMaskIndex <> retainSlider.value Or k <> kSlider.value Then
            saveMaskIndex = retainSlider.value
            pixelCounts.Clear()
            unstable.Clear()
            k = kSlider.value
        End If

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        km.RunClass(src)
        If km.dst2.Channels <> 1 Then
            dst2 = km.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst2 = km.dst2
        End If
        dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        Static lastImage As cv.Mat = dst2
        cv.Cv2.Subtract(dst2, lastImage, dst3)
        dst3 = dst3.Threshold(diffSlider.value, 255, cv.ThresholdTypes.Binary)

        unstable.Add(dst3)
        If unstable.Count > retainSlider.value Then unstable.RemoveAt(0)

        unstablePixels = unstable(0)
        For i = 1 To unstable.Count - 1
            cv.Cv2.BitwiseOr(unstablePixels, unstable(i), unstablePixels)
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