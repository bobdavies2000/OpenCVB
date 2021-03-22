Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Pixel_Viewer
    Inherits VBparent
    Public pixels As PixelViewerForm
    Public Sub New()
        initParent()

        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone (it is always present, not standalone)
        standalone = False
        task.desc = "Display pixels under the cursor"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If task.pixelViewerOn Then
            If pixels Is Nothing Then pixels = New PixelViewerForm
            If pixels.Visible = False Then pixels = New PixelViewerForm
            pixels.Show()

            dst1 = Choose(task.mousePicTag + 1, task.color, task.RGBDepth, task.algorithmObject.dst1, task.algorithmObject.dst2)
            If pixels.GrayScaleOnly.Checked And dst1.Channels <> 1 Then
                dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            End If
            Dim displayType = -1 ' default is 8uc3
            If dst1.Type = cv.MatType.CV_8UC3 Then displayType = 0
            If dst1.Type = cv.MatType.CV_8U Then displayType = 1
            If dst1.Type = cv.MatType.CV_32F Then displayType = 2
            If dst1.Type = cv.MatType.CV_32FC3 Then displayType = 3
            If displayType < 0 Or dst1.Channels > 4 Then
                ocvb.trueText("The pixel Viewer does not support this cv.Mat!")
                Exit Sub
            End If

            Dim formatType = Choose(displayType + 1, "8UC3", "8UC1", "32FC1", "32FC3")
            pixels.Text = "Pixel Viewer for " + Choose(task.mousePicTag + 1, "Color", "RGB Depth", "dst1", "dst2") + " " + formatType

            Dim drWidth = Choose(displayType + 1, 7, 22, 13, 4) * pixels.Width / 650 + 3
            Dim drHeight = CInt(pixels.Height / 16) + If(pixels.Height < 400, -3, If(pixels.Height < 800, -1, 1))
            If drHeight < 20 Then drHeight = 20

            If pixels.mousePoint <> New cv.Point Then
                task.mousePoint += pixels.mousePoint
                task.mousePointUpdated = True
                pixels.mousePoint = New cv.Point
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
            If dw.X + dw.Width > dst1.Width Then
                dw.X = dst1.Width - dw.Width
                dw.Width = dw.Width
            End If
            If dw.Y + dw.Height > dst1.Height Then
                dw.Y = dst1.Height - dw.Height
                dw.Height = dw.Height
            End If

            Dim testChange As cv.Mat = If(dst1.Channels = 1, dst1(dw).Clone, dst1(dw).CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            Dim diff As New cv.Mat
            Static savePixels As cv.Mat = testChange
            If savePixels.Size <> testChange.Size Or savePixels.Type <> testChange.Type Then
                savePixels = testChange.Clone
            Else
                cv.Cv2.Absdiff(savePixels, testChange, diff)
            End If

            Dim img = dst1(dw)
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

            savePixels = testChange.Clone

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

            If pixels.rtb.Text <> imgText Then
                pixels.rtb.Text = imgText
                pixels.Refresh()
            End If
        Else
            If pixels IsNot Nothing Then
                pixels.Close()
                pixels = Nothing
            End If
        End If
    End Sub
    Public Sub closeViewer()
        If pixels IsNot Nothing Then pixels.Close()
    End Sub
End Class








' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet
    Inherits VBparent
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()
        mats = New Mat_4to1()

        label1 = "Time to copy using get/set,Generic Index, Marshal Copy"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
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

        ocvb.trueText(output, src.Width / 2 + 10, src.Height / 2 + 20)

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class








Public Class Pixel_Measure
    Inherits VBparent
    Public distanceIn As Single
    Public pixelsPerMeter As Single
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Distance in mm", 50, ocvb.maxZ * 1000, 1500)
        End If

        task.desc = "Compute how many pixels per meter at a requested distance"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static distanceSlider = findSlider("Distance in mm")
        Dim xDist = distanceSlider.value
        If distanceIn <> 0 Then xDist = distanceIn ' alternate input from another algorithm.
        Dim pixeldistance = src.Height * (xDist / 1000) / ocvb.maxZ
        Dim FOV = ocvb.hFov / 2
        Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

        Dim xpt1 = New cv.Point2f(ocvb.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
        Dim xpt2 = New cv.Point2f(ocvb.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
        Dim lineWidth = xpt2.X - xpt1.X
        Dim blueLineMeters = (xDist * lineWidth / (1000 * pixeldistance))
        If standalone Then
            ocvb.trueText("At a distance of " + CStr(xDist) + " mm's the camera's FOV is " +
                           Format(blueLineMeters, "#0.00") + " meters wide" + vbCrLf +
                          "There are " + Format(1000 * blueLineMeters / dst1.Width, "#0.00") + " mm per pixel ", 10, 60)
        End If
    End Sub
End Class

