Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Drawing.Imaging

Public Class Palette_Basics : Inherits VB_Algorithm
    Public whitebackground As Boolean
    Public Sub New()
        desc = "Apply the different color maps in OpenCV"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        labels(2) = "ColorMap = " + gOptions.Palettes.Text

        If src.Type = cv.MatType.CV_32F Then
            src = vbNormalize32f(src)
            src.ConvertTo(src, cv.MatType.CV_8U)
        End If

        Dim mapIndex = Choose(task.paletteIndex + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone,
                              cv.ColormapTypes.Cividis, cv.ColormapTypes.Cool, cv.ColormapTypes.Hot,
                              cv.ColormapTypes.Hsv, cv.ColormapTypes.Inferno, cv.ColormapTypes.Jet,
                              cv.ColormapTypes.Magma, cv.ColormapTypes.Ocean, cv.ColormapTypes.Parula,
                              cv.ColormapTypes.Pink, cv.ColormapTypes.Plasma, cv.ColormapTypes.Rainbow,
                              cv.ColormapTypes.Spring, cv.ColormapTypes.Summer, cv.ColormapTypes.Twilight,
                              cv.ColormapTypes.TwilightShifted, cv.ColormapTypes.Viridis, cv.ColormapTypes.Winter)
        cv.Cv2.ApplyColorMap(src, dst2, mapIndex)
    End Sub
End Class







Public Class Palette_LoadColorMap : Inherits VB_Algorithm
    Public whitebackground As Boolean
    Public gradientColorMap As New cv.Mat
    Dim cMapDir As New DirectoryInfo(task.homeDir + "opencv/modules/imgproc/doc/pics/colormaps")
    Public Sub New()
        buildColorMap()
        desc = "Apply the different color maps in OpenCV"
    End Sub
    Private Sub buildColorMap()
        Dim str = cMapDir.FullName + "/colorscale_" + gOptions.Palettes.Text + ".jpg"
        Dim mapFile As New FileInfo(str)
        gradientColorMap = cv.Cv2.ImRead(mapFile.FullName)
        gradientColorMap.Col(0).SetTo(If(whitebackground, cv.Scalar.White, cv.Scalar.Black))
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src Is Nothing Then Exit Sub
        labels(2) = "ColorMap = " + gOptions.Palettes.Text

        If task.optionsChanged Then buildColorMap()

        If src.Type = cv.MatType.CV_32F Then
            src = vbNormalize32f(src)
            src.ConvertTo(src, cv.MatType.CV_8U)
        End If
        Dim mapIndex = Choose(task.paletteIndex + 1, cv.ColormapTypes.Autumn, cv.ColormapTypes.Bone,
                              cv.ColormapTypes.Cividis, cv.ColormapTypes.Cool, cv.ColormapTypes.Hot,
                              cv.ColormapTypes.Hsv, cv.ColormapTypes.Inferno, cv.ColormapTypes.Jet,
                              cv.ColormapTypes.Magma, cv.ColormapTypes.Ocean, cv.ColormapTypes.Parula,
                              cv.ColormapTypes.Pink, cv.ColormapTypes.Plasma, cv.ColormapTypes.Rainbow,
                              cv.ColormapTypes.Spring, cv.ColormapTypes.Summer, cv.ColormapTypes.Twilight,
                              cv.ColormapTypes.TwilightShifted, cv.ColormapTypes.Viridis, cv.ColormapTypes.Winter)
        cv.Cv2.ApplyColorMap(src, dst2, mapIndex)
        dst3 = gradientColorMap.Resize(dst3.Size)
    End Sub
End Class






Public Class Palette_Color : Inherits VB_Algorithm
    Dim options As New Options_Colors
    Public Sub New()
        desc = "Define a color Using sliders."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2.SetTo(New cv.Scalar(options.blue, options.green, options.red))
        dst3.SetTo(New cv.Scalar(255 - options.blue, 255 - options.green, 255 - options.red))
        labels(2) = "Color (RGB) = " + CStr(options.blue) + " " + CStr(options.green) + " " + CStr(options.red)
        labels(3) = "Color (255 - RGB) = " + CStr(255 - options.blue) + " " + CStr(255 - options.green) + " " + CStr(255 - options.red)
    End Sub
End Class







Public Class Palette_LinearPolar : Inherits VB_Algorithm
    Public rotateOptions As New Options_Resize
    Public Sub New()
        desc = "Use LinearPolar To create gradient image"
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("LinearPolar radius", 0, dst2.Cols, dst2.Cols / 2)
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static radiusSlider = findSlider("LinearPolar radius")
        Dim radius = radiusSlider.Value ' msRNG.next(0, dst2.Cols)

        dst2.SetTo(0)
        For i = 0 To dst2.Rows - 1
            Dim c = i * 255 / dst2.Rows
            dst2.Row(i).SetTo(New cv.Scalar(c, c, c))
        Next

        rotateOptions.RunVB()

        Static pt = New cv.Point2f(msRNG.Next(0, dst2.Cols - 1), msRNG.Next(0, dst2.Rows - 1))
        dst3.SetTo(0)
        If rotateOptions.warpFlag = cv.InterpolationFlags.WarpInverseMap Then radiusSlider.Value = radiusSlider.Maximum
        cv.Cv2.LinearPolar(dst2, dst2, pt, radius, rotateOptions.warpFlag)
        cv.Cv2.LinearPolar(src, dst3, pt, radius, rotateOptions.warpFlag)
    End Sub
End Class







Public Class Palette_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        advice = "Use 'Reduction' options (redOptions) to control results."
        desc = "Map colors To different palette"
        labels(2) = "Reduced Colors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst3 = reduction.dst2

        dst2 = vbPalette(dst3 * 255 / reduction.classCount)

        'Dim palette As New SortedList(Of Byte, Integer)
        'For y = 0 To dst2.Height - 1
        '    For x = 0 To dst2.Width - 1
        '        Dim nextVal = dst2.Get(Of Byte)(y, x)
        '        If nextVal <> cv.Scalar.Black Then
        '            If palette.ContainsKey(nextVal) Then
        '                palette(nextVal) = palette(nextVal) + 1
        '            Else
        '                palette.Add(nextVal, 1)
        '            End If
        '        End If
        '    Next
        'Next

        'labels(2) = "palette count = " + CStr(palette.Count)
        'Dim max As Integer
        'Dim maxIndex As Integer
        'For i = 0 To palette.Count - 1
        '    If palette.ElementAt(i).Value > max Then
        '        max = palette.ElementAt(i).Value
        '        maxIndex = i
        '    End If
        'Next

        'If palette.Count > 0 Then
        '    Dim nextVal = palette.ElementAt(maxIndex).Key
        '    Dim loValue = cv.Scalar.All(nextVal - 1)
        '    Dim hiValue = cv.Scalar.All(nextVal + 1)
        '    If loValue(0) < 0 Then loValue(0) = 0
        '    If hiValue(0) > 255 Then hiValue(0) = 255

        '    Dim mask As New cv.Mat
        '    cv.Cv2.InRange(dst2, loValue, hiValue, mask)

        '    Dim maxCount = cv.Cv2.CountNonZero(mask)

        '    dst3 = src.EmptyClone.SetTo(0)
        '    dst3.SetTo(cv.Scalar.All(255), mask)
        '    labels(3) = "Most Common Color +- " + CStr(1) + " count = " + CStr(maxCount)
        'End If
    End Sub
End Class




Public Class Palette_DrawTest : Inherits VB_Algorithm
    Dim draw As New Draw_Shapes
    Public Sub New()
        task.palette.whitebackground = True
        desc = "Experiment With palette Using a drawn image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        draw.Run(src)
        dst2 = vbPalette(draw.dst2)
    End Sub
End Class





Public Class Palette_Gradient : Inherits VB_Algorithm
    Public color1 As cv.Scalar
    Public color2 As cv.Scalar
    Public Sub New()
        labels(3) = "From And To colors"
        desc = "Create gradient image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then
            If standalone Then
                ' every 30 frames try a different pair of random colors.
                color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                dst3.SetTo(color1)
                dst3(New cv.Rect(0, 0, dst3.Width, dst3.Height / 2)).SetTo(color2)
            End If

            dst1 = New cv.Mat(255, 1, cv.MatType.CV_8UC3)
            Dim f As Double = 1.0
            For i = 0 To dst1.Rows - 1
                dst1.Set(Of cv.Vec3b)(i, 0, New cv.Vec3b(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2)))
                f -= 1 / dst1.Rows
            Next
        End If
        If standalone Then dst2 = dst1.Resize(dst2.Size)
    End Sub
End Class





Public Class Palette_DepthColorMap : Inherits VB_Algorithm
    Public gradientColorMap As New cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        advice = "Use the local 'Convert and Scale' slider to adjust color"
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Convert And Scale", 0, 100, 45)
        labels(3) = "Palette used To color left image"
        desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static cvtScaleSlider = findSlider("Convert And Scale")
        If task.optionsChanged Then
            gColor.color1 = cv.Scalar.Yellow
            gColor.color2 = cv.Scalar.Red
            Dim gradMat As New cv.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(empty)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cv.Scalar.Blue
            gColor.Run(empty)

            cv.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))

            If standalone Then
                If dst3.Width < 255 Then dst3 = New cv.Mat(dst3.Height, 255, cv.MatType.CV_8UC3, 0)
                Dim r As New cv.Rect(0, 0, 255, 1)
                For i = 0 To dst3.Height - 1
                    r.Y = i
                    dst3(r) = gradientColorMap
                Next
            End If
        End If

        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(cvtScaleSlider.Value)
        Dim ColorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, gradientColorMap.Data())
        cv.Cv2.ApplyColorMap(depth8u, dst2, ColorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Palette_RGBDepth : Inherits VB_Algorithm
    Dim gradientColorMap As New cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        desc = "Build a colormap that best shows the depth.  NOTE: duplicate of Palette_DepthColorMap but no slider."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then
            gColor.color1 = cv.Scalar.Yellow
            gColor.color2 = cv.Scalar.Red
            Dim gradMat As New cv.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(empty)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cv.Scalar.Blue
            gColor.Run(empty)

            cv.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))
        End If

        Dim sliderVal = If(task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i", 50, 80)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(sliderVal)
        Dim ColorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, gradientColorMap.Data())
        cv.Cv2.ApplyColorMap(depth8u, dst2, ColorMap)
    End Sub
End Class







Public Class Palette_Layout2D : Inherits VB_Algorithm
    Public Sub New()
        desc = "Layout the available colors in a 2D grid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim index As Integer
        For Each r In task.gridList
            dst2(r).SetTo(task.scalarColors(index Mod 256))
            index += 1
        Next
        labels(2) = "Palette_Layout2D - " + CStr(task.gridList.Count) + " regions"
    End Sub
End Class








Public Class Palette_LeftRightImages : Inherits VB_Algorithm
    Public Sub New()
        desc = "Use a palette with the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = vbPalette(task.leftView.ConvertScaleAbs)
        dst3 = vbPalette(task.rightView.ConvertScaleAbs)
    End Sub
End Class
Public Class Palette_TaskColors : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "ScalarColors", "VecColors"}
        desc = "Display that task.scalarColors and task.vecColors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static direction = 1

        If gOptions.GridSize.Value <= 10 Then direction *= -1
        If gOptions.GridSize.Value >= 100 Then direction *= -1

        gOptions.GridSize.Value -= direction * 1
        task.grid.Run(src)

        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            dst2(roi).SetTo(task.scalarColors(i Mod 256))
            dst3(roi).SetTo(task.vecColors(i Mod 256))
        Next
    End Sub
End Class






Public Class Palette_Create : Inherits VB_Algorithm
    Dim schemes() As FileInfo
    Dim schemeName As String
    Dim colorGrad As New cv.Mat
    Public Sub New()
        Dim dirInfo = New DirectoryInfo(task.homeDir + "Data")
        schemes = dirInfo.GetFiles("scheme*.jpg")

        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            For i = 0 To schemes.Count - 1
                radio.addRadio(Mid(schemes(i).Name, 1, Len(schemes(i).Name) - 4))
                If schemes(i).Name = "schemeRandom" Then radio.check(i).Checked = True
            Next
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Color Transitions", 0, 20, 10)

        desc = "Create a new palette"
    End Sub
    Private Function colorTransition(color1 As cv.Scalar, color2 As cv.Scalar, width As Integer) As cv.Mat
        Dim f As Double = 1.0
        Dim gradientColors As New cv.Mat(1, width, cv.MatType.CV_64FC3)
        For i = 0 To width - 1
            gradientColors.Set(Of cv.Scalar)(0, i, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                             f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / width
        Next
        Dim result = New cv.Mat(1, width, cv.MatType.CV_8UC3)
        For i = 0 To width - 1
            result.Col(i).SetTo(gradientColors.Get(Of cv.Scalar)(0, i))
        Next
        Return result
    End Function
    Public Sub RunVB(src as cv.Mat)
        Static transitionSlider = findSlider("Color Transitions")
        Dim colorTransitionCount = transitionSlider.Value

        Static frm = findfrm(traceName + " Radio Buttons")
        schemeName = schemes(findRadioIndex(frm.check)).FullName

        Static activeSchemeName As String = ""
        Static saveColorTransitionCount As Integer = -1
        If activeSchemeName <> schemeName Or colorTransitionCount <> saveColorTransitionCount Then
            activeSchemeName = schemeName
            saveColorTransitionCount = colorTransitionCount
            If activeSchemeName = "schemeRandom" Then
                Dim msRNG As New System.Random
                Dim color1 = New cv.Scalar(0, 0, 0)
                Dim color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                Dim gradMat As New cv.Mat
                For i = 0 To colorTransitionCount
                    gradMat = colorTransition(color1, color2, 255)
                    color1 = color2
                    color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                    If i = 0 Then colorGrad = gradMat Else cv.Cv2.HConcat(colorGrad, gradMat, colorGrad)
                Next
                colorGrad = colorGrad.Resize(New cv.Size(256, 1))
                cv.Cv2.ImWrite(task.homeDir + "data\nextScheme.jpg", colorGrad) ' use this to create new color schemes.
            Else
                colorGrad = cv.Cv2.ImRead(schemeName).Row(0).Clone
            End If
        End If

        setTrueText("Use the 'Color Transitions' slider and radio buttons to change the color ranges.", 3)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(colorTransitionCount)
        Dim colorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, colorGrad.Data())
        cv.Cv2.ApplyColorMap(depth8u, dst2, colorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class





Public Class Palette_Random : Inherits VB_Algorithm
    Public colorMap As cv.Mat
    Public Sub New()
        advice = "There are no options - just produces a colorMap filled with random vec3b's."
        colorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, 0)
        For i = 0 To 255
            colorMap.Set(Of cv.Vec3b)(i, 0, randomCellColor())
        Next
        colorMap.Set(Of cv.Vec3b)(0, 0, New cv.Vec3b(0, 0, 0)) ' set 0th entry to black...

        desc = "Build a random colorGrad - no smooth transitions."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        cv.Cv2.ApplyColorMap(src, dst2, colorMap)
    End Sub
End Class





Public Class Palette_Variable : Inherits VB_Algorithm
    Public colorGrad As cv.Mat
    Public originalColorMap As cv.Mat
    Public colors As New List(Of cv.Vec3b)
    Public Sub New()
        colorGrad = New cv.Mat(1, 256, cv.MatType.CV_8UC3, 0)
        For i = 0 To 255
            colorGrad.Set(Of cv.Vec3b)(0, i, randomCellColor())
        Next
        originalColorMap = colorGrad.Clone
        desc = "Build a new palette for every frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For i = 0 To colors.Count - 1
            colorGrad.Set(Of cv.Vec3b)(0, i, colors(i))
        Next
        Dim colorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, colorGrad.Data())
        cv.Cv2.ApplyColorMap(src, dst2, colorMap)
    End Sub
End Class





Public Class Palette_RandomColorMap : Inherits VB_Algorithm
    Public gradientColorMap As New cv.Mat
    Public transitionCount As Integer = -1
    Dim gColor As New Gradient_Color
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Color transitions", 1, 255, 7)
        labels(3) = "Generated colormap"
        desc = "Build a random colormap that smoothly transitions colors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static paletteSlider = findSlider("Color transitions")
        If transitionCount <> paletteSlider.Value Then
            transitionCount = paletteSlider.Value

            gColor.color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            gColor.color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            For i = 0 To transitionCount - 1
                gColor.gradientWidth = dst2.Width
                gColor.Run(empty)
                gColor.color2 = gColor.color1
                gColor.color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                If i = 0 Then gradientColorMap = gColor.gradient Else cv.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            Next
            gradientColorMap = gradientColorMap.Resize(New cv.Size(256, 1))
            If standalone Then dst3 = gradientColorMap
            gradientColorMap.Set(Of cv.Vec3b)(0, 0, New cv.Vec3b) ' black is black!
        End If
        Dim ColorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, gradientColorMap.Data())
        cv.Cv2.ApplyColorMap(src, dst2, ColorMap)
    End Sub
End Class

