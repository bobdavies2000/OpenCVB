Imports cvb = OpenCvSharp
Imports System.IO
Imports System.Drawing.Imaging
Public Class Palette_Basics : Inherits TaskParent
    Public whitebackground As Boolean
    Public Sub New()
        desc = "Apply the different color maps in OpenCV"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        labels(2) = "ColorMap = " + task.gOptions.Palettes.Text

        If src.Type = cvb.MatType.CV_32F Then
            src = Convert32f_To_8UC3(src)
            src.ConvertTo(src, cvb.MatType.CV_8U)
        End If

        Dim mapIndex = Choose(task.paletteIndex + 1, cvb.ColormapTypes.Autumn, cvb.ColormapTypes.Bone,
                              cvb.ColormapTypes.Cividis, cvb.ColormapTypes.Cool, cvb.ColormapTypes.Hot,
                              cvb.ColormapTypes.Hsv, cvb.ColormapTypes.Inferno, cvb.ColormapTypes.Jet,
                              cvb.ColormapTypes.Magma, cvb.ColormapTypes.Ocean, cvb.ColormapTypes.Parula,
                              cvb.ColormapTypes.Pink, cvb.ColormapTypes.Plasma, cvb.ColormapTypes.Rainbow,
                              cvb.ColormapTypes.Spring, cvb.ColormapTypes.Summer, cvb.ColormapTypes.Twilight,
                              cvb.ColormapTypes.TwilightShifted, cvb.ColormapTypes.Viridis, cvb.ColormapTypes.Winter)
        cvb.Cv2.ApplyColorMap(src, dst2, mapIndex)
    End Sub
End Class







Public Class Palette_Color : Inherits TaskParent
    Dim options As New Options_Colors
    Public Sub New()
        desc = "Define a color Using sliders."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dst2.SetTo(New cvb.Scalar(options.blueS, options.greenS, options.redS))
        dst3.SetTo(New cvb.Scalar(255 - options.blueS, 255 - options.greenS, 255 - options.redS))
        labels(2) = "Color (RGB) = " + CStr(options.blueS) + " " + CStr(options.greenS) + " " + CStr(options.redS)
        labels(3) = "Color (255 - RGB) = " + CStr(255 - options.blueS) + " " + CStr(255 - options.greenS) + " " +
                     CStr(255 - options.redS)
    End Sub
End Class







Public Class Palette_LinearPolar : Inherits TaskParent
    Public rotateOptions As New Options_Resize
    Dim pt = New cvb.Point2f(msRNG.Next(0, dst2.Cols - 1), msRNG.Next(0, dst2.Rows - 1))
    Public Sub New()
        desc = "Use LinearPolar To create gradient image"
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("LinearPolar radius", 0, dst2.Cols, dst2.Cols / 2)
        End If
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static radiusSlider = FindSlider("LinearPolar radius")
        Dim radius = radiusSlider.Value ' msRNG.next(0, dst2.Cols)

        dst2.SetTo(0)
        For i = 0 To dst2.Rows - 1
            Dim c = i * 255 / dst2.Rows
            dst2.Row(i).SetTo(New cvb.Scalar(c, c, c))
        Next

        rotateOptions.RunOpt()

        dst3.SetTo(0)
        If rotateOptions.warpFlag = cvb.InterpolationFlags.WarpInverseMap Then radiusSlider.Value = radiusSlider.Maximum
        cvb.Cv2.LinearPolar(dst2, dst2, pt, radius, rotateOptions.warpFlag)
        cvb.Cv2.LinearPolar(src, dst3, pt, radius, rotateOptions.warpFlag)
    End Sub
End Class







Public Class Palette_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'Reduction' to control results.")
        desc = "Map colors To different palette"
        labels(2) = "Reduced Colors"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst3 = reduction.dst2

        dst2 = ShowPalette(dst3 * 255 / reduction.classCount)
    End Sub
End Class




Public Class Palette_DrawTest : Inherits TaskParent
    Dim draw As New Draw_Shapes
    Public Sub New()
        desc = "Experiment With palette Using a drawn image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        draw.Run(src)
        dst2 = ShowPalette(draw.dst2)
    End Sub
End Class





Public Class Palette_Gradient : Inherits TaskParent
    Public color1 As cvb.Scalar
    Public color2 As cvb.Scalar
    Public Sub New()
        labels(3) = "From And To colors"
        desc = "Create gradient image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            If standaloneTest() Then
                ' every 30 frames try a different pair of random colors.
                color1 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                color2 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                dst3.SetTo(color1)
                dst3(New cvb.Rect(0, 0, dst3.Width, dst3.Height / 2)).SetTo(color2)
            End If

            dst1 = New cvb.Mat(255, 1, cvb.MatType.CV_8UC3)
            Dim f As Double = 1.0
            For i = 0 To dst1.Rows - 1
                dst1.Set(Of cvb.Vec3b)(i, 0, New cvb.Vec3b(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2)))
                f -= 1 / dst1.Rows
            Next
        End If
        If standaloneTest() Then dst2 = dst1.Resize(dst2.Size)
    End Sub
End Class





Public Class Palette_DepthColorMap : Inherits TaskParent
    Public gradientColorMap As New cvb.Mat
    Dim gColor As New Gradient_ForDepth
    Public Sub New()
        UpdateAdvice(traceName + ": adjust color with 'Convert and Scale' slider")
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Convert And Scale", 0, 100, 45)
        labels(3) = "Palette used To color left image"
        desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static cvtScaleSlider = FindSlider("Convert And Scale")
        If task.optionsChanged Then
            gColor.color1 = cvb.Scalar.Yellow
            gColor.color2 = cvb.Scalar.Red
            Dim gradMat As New cvb.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(empty)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cvb.Scalar.Blue
            gColor.Run(empty)

            cvb.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cvb.Size(255, 1))

            If standaloneTest() Then
                If dst3.Width < 255 Then dst3 = New cvb.Mat(dst3.Height, 255, cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
                Dim r As New cvb.Rect(0, 0, 255, 1)
                For i = 0 To dst3.Height - 1
                    r.Y = i
                    dst3(r) = gradientColorMap
                Next
            End If
        End If

        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(cvtScaleSlider.Value)
        Dim ColorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, gradientColorMap.Data())
        cvb.Cv2.ApplyColorMap(depth8u, dst2, ColorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Palette_RGBDepth : Inherits TaskParent
    Dim gradientColorMap As New cvb.Mat
    Dim gColor As New Gradient_ForDepth
    Public Sub New()
        desc = "Build a colormap that best shows the depth.  NOTE: duplicate of Palette_DepthColorMap but no slider."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then
            gColor.color1 = cvb.Scalar.Yellow
            gColor.color2 = cvb.Scalar.Red
            Dim gradMat As New cvb.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(empty)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cvb.Scalar.Blue
            gColor.Run(empty)

            cvb.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cvb.Size(255, 1))
        End If

        Dim sliderVal = If(task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i", 50, 80)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(sliderVal)
        Dim ColorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, gradientColorMap.Data())
        cvb.Cv2.ApplyColorMap(depth8u, dst2, ColorMap)
    End Sub
End Class







Public Class Palette_Layout2D : Inherits TaskParent
    Public Sub New()
        desc = "Layout the available colors in a 2D grid"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim index As Integer
        For Each r In task.gridRects
            dst2(r).SetTo(task.scalarColors(index Mod 256))
            index += 1
        Next
        labels(2) = "Palette_Layout2D - " + CStr(task.gridRects.Count) + " regions"
    End Sub
End Class








Public Class Palette_LeftRightImages : Inherits TaskParent
    Public Sub New()
        desc = "Use a palette with the left and right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = ShowPalette(task.leftView.ConvertScaleAbs)
        dst3 = ShowPalette(task.rightView.ConvertScaleAbs)
    End Sub
End Class
Public Class Palette_TaskColors : Inherits TaskParent
    Dim direction = 1
    Public Sub New()
        labels = {"", "", "ScalarColors", "VecColors"}
        desc = "Display that task.scalarColors and task.vecColors"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.gridSize <= 10 Then direction *= -1
        If task.gridSize >= 100 Then direction *= -1

        task.gridSize -= direction * 1
        task.grid.Run(src)

        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            dst2(roi).SetTo(task.scalarColors(i Mod 256))
            dst3(roi).SetTo(task.vecColors(i Mod 256))
        Next
    End Sub
End Class






Public Class Palette_Create : Inherits TaskParent
    Dim schemes() As FileInfo
    Dim schemeName As String
    Dim colorGrad As New cvb.Mat
    Dim activeSchemeName As String = ""
    Dim saveColorTransitionCount As Integer = -1
    Public Sub New()
        Dim dirInfo = New DirectoryInfo(task.HomeDir + "Data")
        schemes = dirInfo.GetFiles("scheme*.jpg")

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            For i = 0 To schemes.Count - 1
                radio.addRadio(Mid(schemes(i).Name, 1, Len(schemes(i).Name) - 4))
                If schemes(i).Name = "schemeRandom" Then radio.check(i).Checked = True
            Next
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Color Transitions", 0, 20, 10)

        desc = "Create a new palette"
    End Sub
    Private Function colorTransition(color1 As cvb.Scalar, color2 As cvb.Scalar, width As Integer) As cvb.Mat
        Dim f As Double = 1.0
        Dim gradientColors As New cvb.Mat(1, width, cvb.MatType.CV_64FC3)
        For i = 0 To width - 1
            gradientColors.Set(Of cvb.Scalar)(0, i, New cvb.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                             f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / width
        Next
        Dim result = New cvb.Mat(1, width, cvb.MatType.CV_8UC3)
        For i = 0 To width - 1
            result.Col(i).SetTo(gradientColors.Get(Of cvb.Scalar)(0, i))
        Next
        Return result
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        Static transitionSlider = FindSlider("Color Transitions")
        Dim colorTransitionCount = transitionSlider.Value

        Static frm = FindFrm(traceName + " Radio Buttons")
        schemeName = schemes(findRadioIndex(frm.check)).FullName

        If activeSchemeName <> schemeName Or colorTransitionCount <> saveColorTransitionCount Then
            activeSchemeName = schemeName
            saveColorTransitionCount = colorTransitionCount
            If activeSchemeName = "schemeRandom" Then
                Dim msRNG As New System.Random
                Dim color1 = New cvb.Scalar(0, 0, 0)
                Dim color2 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                Dim gradMat As New cvb.Mat
                For i = 0 To colorTransitionCount
                    gradMat = colorTransition(color1, color2, 255)
                    color1 = color2
                    color2 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                    If i = 0 Then colorGrad = gradMat Else cvb.Cv2.HConcat(colorGrad, gradMat, colorGrad)
                Next
                colorGrad = colorGrad.Resize(New cvb.Size(256, 1))
                cvb.Cv2.ImWrite(task.HomeDir + "data\nextScheme.jpg", colorGrad) ' use this to create new color schemes.
            Else
                colorGrad = cvb.Cv2.ImRead(schemeName).Row(0).Clone
            End If
        End If

        SetTrueText("Use the 'Color Transitions' slider and radio buttons to change the color ranges.", 3)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(colorTransitionCount)
        Dim colorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, colorGrad.Data())
        cvb.Cv2.ApplyColorMap(depth8u, dst2, colorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class





Public Class Palette_Random : Inherits TaskParent
    Public colorMap As cvb.Mat
    Public Sub New()
        UpdateAdvice(traceName + ": There are no options" + vbCrLf + "Just produces a colorMap filled with random vec3b's.")
        colorMap = New cvb.Mat(256, 1, cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        For i = 0 To 255
            Dim vec = randomCellColor()
            colorMap.Set(Of cvb.Vec3b)(i, 0, New cvb.Vec3b(vec(0), vec(1), vec(2)))
        Next

        desc = "Build a random colorGrad - no smooth transitions."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cvb.Cv2.ApplyColorMap(src, dst2, colorMap)
    End Sub
End Class





Public Class Palette_Variable : Inherits TaskParent
    Public colorGrad As cvb.Mat
    Public originalColorMap As cvb.Mat
    Public colors As New List(Of cvb.Vec3b)
    Public Sub New()
        colorGrad = New cvb.Mat(1, 256, cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        For i = 0 To 255
            Dim vec = randomCellColor()
            colorGrad.Set(Of cvb.Vec3b)(0, i, New cvb.Vec3b(vec(0), vec(1), vec(2)))
        Next
        originalColorMap = colorGrad.Clone
        desc = "Build a new palette for every frame."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        For i = 0 To colors.Count - 1
            colorGrad.Set(Of cvb.Vec3b)(0, i, colors(i))
        Next
        Dim colorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, colorGrad.Data())
        cvb.Cv2.ApplyColorMap(src, dst2, colorMap)
    End Sub
End Class





Public Class Palette_RandomColorMap : Inherits TaskParent
    Public gradientColorMap As New cvb.Mat
    Public transitionCount As Integer = -1
    Dim gColor As New Gradient_ForDepth
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Color transitions", 1, 255, 7)
        labels(3) = "Generated colormap"
        desc = "Build a random colormap that smoothly transitions colors"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static paletteSlider = FindSlider("Color transitions")
        If transitionCount <> paletteSlider.Value Then
            transitionCount = paletteSlider.Value

            gColor.color1 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            gColor.color2 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            For i = 0 To transitionCount - 1
                gColor.gradientWidth = dst2.Width
                gColor.Run(empty)
                gColor.color2 = gColor.color1
                gColor.color1 = New cvb.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                If i = 0 Then gradientColorMap = gColor.gradient Else cvb.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            Next
            gradientColorMap = gradientColorMap.Resize(New cvb.Size(256, 1))
            If standaloneTest() Then dst3 = gradientColorMap
            gradientColorMap.Set(Of cvb.Vec3b)(0, 0, New cvb.Vec3b) ' black is black!
        End If
        Dim ColorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, gradientColorMap.Data())
        cvb.Cv2.ApplyColorMap(src, dst2, ColorMap)
    End Sub
End Class









Public Class Palette_LoadColorMap : Inherits TaskParent
    Public whitebackground As Boolean
    Public colorMap As New cvb.Mat
    Dim cMapDir As New DirectoryInfo(task.HomeDir + "opencv/modules/imgproc/doc/pics/colormaps")
    Public Sub New()
        desc = "Apply the different color maps in OpenCV"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Or colorMap.Rows <> 256 Then
            labels(2) = "ColorMap = " + task.gOptions.Palettes.Text
            Dim str = cMapDir.FullName + "/colorscale_" + task.gOptions.Palettes.Text + ".jpg"
            Dim mapFile As New FileInfo(str)
            Dim tmp = cvb.Cv2.ImRead(mapFile.FullName)

            tmp.Col(0).SetTo(If(whitebackground, white, cvb.Scalar.Black))
            tmp = tmp.Row(0)
            colorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, tmp.Data).Clone
        End If

        If src.Type = cvb.MatType.CV_32F Then
            src = Convert32f_To_8UC3(src)
            src.ConvertTo(src, cvb.MatType.CV_8U)
        End If
        cvb.Cv2.ApplyColorMap(src, dst2, colorMap)
        If standalone Then dst3 = colorMap.Resize(dst3.Size)
    End Sub
End Class







Public Class Palette_CustomColorMap : Inherits TaskParent
    Public colorMap As cvb.Mat
    Public Sub New()
        labels(2) = "ColorMap = " + task.gOptions.Palettes.Text
        If standalone Then
            Dim cMapDir As New DirectoryInfo(task.HomeDir + "opencv/modules/imgproc/doc/pics/colormaps")
            Dim str = cMapDir.FullName + "/colorscale_" + task.gOptions.Palettes.Text + ".jpg"
            Dim mapFile As New FileInfo(str)
            Dim tmp = cvb.Cv2.ImRead(mapFile.FullName)

            colorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, tmp.Data).Clone
        End If
        desc = "Apply the provided color map to the input image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If colorMap Is Nothing Then
            SetTrueText("With " + traceName + " the colorMap must be provided.  Update the ColorMap Mat and then call Run(src)...")
            Exit Sub
        End If
        If src.Channels <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If src.Type = cvb.MatType.CV_32F Then
            src = Convert32f_To_8UC3(src)
            src.ConvertTo(src, cvb.MatType.CV_8U)
        End If
        cvb.Cv2.ApplyColorMap(src, dst2, colorMap)
        If standalone Then dst3 = colorMap.Resize(dst3.Size)
    End Sub
End Class







Public Class Palette_GrayToColor : Inherits TaskParent
    Public Sub New()
        desc = "Build a palette for the current image using samples from each gray level.  Everything turns out sepia-like."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim pixels As New List(Of Byte)
        Dim colors As New SortedList(Of Byte, cvb.Vec3b)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim val = dst2.Get(Of Byte)(y, x)
                Dim color = src.Get(Of cvb.Vec3b)(y, x)
                If pixels.Contains(val) = False Then
                    pixels.Add(val)
                    colors.Add(val, color)
                Else
                    Dim sum = CInt(color(0)) + CInt(color(1)) + CInt(color(2))
                    Dim index = colors.Keys.IndexOf(val)
                    Dim lastColor = colors.ElementAt(index).Value
                    Dim lastSum = CInt(lastColor(0)) + CInt(lastColor(1)) + CInt(lastColor(2))
                    If sum > lastSum Then
                        colors.RemoveAt(index)
                        colors.Add(val, color)
                    End If
                End If
            Next
        Next

        Dim ColorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, colors.Values.ToArray)
        cvb.Cv2.ApplyColorMap(src, dst2, ColorMap)
    End Sub
End Class







Public Class Palette_Bin4Way : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim tiers As New Depth_Tiers
    Public classCount As Integer
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        labels = {"", "", "CV_8U data is below", "Palettized version of dst2 at left"}
        desc = "Create a colorized representation of the 4-way bin split with and without depth tiers."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        binary.Run(src)

        dst2.SetTo(0)
        For i = 0 To binary.mats.mat.Count - 1
            dst2.SetTo(i, binary.mats.mat(i))
        Next

        tiers.Run(src)
        dst2 += tiers.dst2
        classCount = tiers.classCount + 4

        dst3 = ShowPalette(dst2 * 255 / classCount)
    End Sub
End Class
