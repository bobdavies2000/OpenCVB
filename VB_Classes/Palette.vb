Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Public Class Palette_Basics : Inherits VB_Algorithm
    Public whitebackground As Boolean
    Public gradientColorMap As New cv.Mat
    Dim cMapDir As New DirectoryInfo(task.homeDir + "opencv/modules/imgproc/doc/pics/colormaps")
    Public Sub New()
        buildColorMap()
        desc = "Apply the different color maps in OpenCV - Painterly Effect"
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
        dst2 = Palette_Custom_Apply(src, gradientColorMap)
        dst3 = gradientColorMap.Resize(dst3.Size)
    End Sub
End Class








Public Class Palette_Color : Inherits VB_Algorithm
    Dim options As New Options_Colors
    Public Sub New()
        desc = "Define a color using sliders."
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
        desc = "Use LinearPolar to create gradient image"
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




Module Palette_Custom_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Palette_Custom(img As IntPtr, map As IntPtr, dst2 As IntPtr, rows As Integer, cols As Integer, channels As Integer)
    End Sub
    Public Function Palette_Custom_Apply(src As cv.Mat, customColorMap As cv.Mat) As cv.Mat
        ' the VB.Net interface to OpenCV doesn't support adding a random lookup table to ApplyColorMap API.  It is available in C++ though.
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

        Dim mapData(customColorMap.Total * customColorMap.ElemSize - 1) As Byte
        Dim handleMap = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Marshal.Copy(customColorMap.Data, mapData, 0, mapData.Length)

        Dim dstData(src.Total * 3 - 1) As Byte ' it always comes back in color...
        Dim handledst1 = GCHandle.Alloc(dstData, GCHandleType.Pinned)

        ' the custom colormap API is not implemented for custom color maps.  Only colormapTypes can be provided.
        Palette_Custom(handleSrc.AddrOfPinnedObject, handleMap.AddrOfPinnedObject, handledst1.AddrOfPinnedObject, src.Rows, src.Cols, src.Channels)

        Dim output = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        Marshal.Copy(dstData, 0, output.Data, dstData.Length)
        handleSrc.Free()
        handleMap.Free()
        handledst1.Free()
        Return output
    End Function
End Module







Public Class Palette_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        redOptions.SimpleReduction.Checked = True
        desc = "Map colors to different palette - Painterly Effect."
        labels(2) = "Reduced Colors"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If redOptions.SimpleReductionSlider.Value < 32 Then
            redOptions.SimpleReductionSlider.Value = 32
            Console.WriteLine("This algorithm gets very slow unless there is lots of reduction.  Resetting reduction slider value to 32")
        End If
        reduction.Run(src)
        dst2 = reduction.dst2

        Dim palette As New SortedList(Of Byte, Integer)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim nextVal = dst2.Get(Of Byte)(y, x)
                If nextVal <> cv.Scalar.Black Then
                    If palette.ContainsKey(nextVal) Then
                        palette(nextVal) = palette(nextVal) + 1
                    Else
                        palette.Add(nextVal, 1)
                    End If
                End If
            Next
        Next

        labels(2) = "palette count = " + CStr(palette.Count)
        Dim max As Integer
        Dim maxIndex As Integer
        For i = 0 To palette.Count - 1
            If palette.ElementAt(i).Value > max Then
                max = palette.ElementAt(i).Value
                maxIndex = i
            End If
        Next

        If palette.Count > 0 Then
            Dim nextVal = palette.ElementAt(maxIndex).Key
            Dim loValue = cv.Scalar.All(nextVal - 1)
            Dim hiValue = cv.Scalar.All(nextVal + 1)
            If loValue(0) < 0 Then loValue(0) = 0
            If hiValue(0) > 255 Then hiValue(0) = 255

            Dim mask As New cv.Mat
            cv.Cv2.InRange(dst2, loValue, hiValue, mask)

            Dim maxCount = cv.Cv2.CountNonZero(mask)

            dst3 = src.EmptyClone.SetTo(0)
            dst3.SetTo(cv.Scalar.All(255), mask)
            labels(3) = "Most Common Color +- " + CStr(1) + " count = " + CStr(maxCount)
        End If
    End Sub
End Class




Public Class Palette_DrawTest : Inherits VB_Algorithm
    Dim draw As New Draw_Shapes
    Public Sub New()
        task.palette.whitebackground = True
        desc = "Experiment with palette using a drawn image"
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
        labels(3) = "From and To colors"
        desc = "Create gradient image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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





Public Class Palette_RandomColorMap : Inherits VB_Algorithm
    Public gradientColorMap As New cv.Mat
    Public transitionCount As Integer = -1
    Dim gColor As New Gradient_Color
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of color transitions (Used only with Random)", 1, 255, 180)
        End If

        labels(3) = "Generated colormap"
        desc = "Build a random colormap that smoothly transitions colors - Painterly Effect"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static paletteSlider = findSlider("Number of color transitions (Used only with Random)")
        If transitionCount <> paletteSlider.Value Then
            transitionCount = paletteSlider.Value

            gColor.color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            gColor.color2 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            For i = 0 To transitionCount - 1
                gColor.gradientWidth = dst2.Width
                gColor.Run(Nothing)
                gColor.color2 = gColor.color1
                gColor.color1 = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                If i = 0 Then gradientColorMap = gColor.gradient Else cv.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            Next
            gradientColorMap = gradientColorMap.Resize(New cv.Size(256, 1))
            If standalone Then dst3 = gradientColorMap
            gradientColorMap.Set(Of cv.Vec3b)(0, 0, New cv.Vec3b) ' black is black!
        End If
        dst2 = Palette_Custom_Apply(src, gradientColorMap)
    End Sub
End Class





Public Class Palette_DepthColorMap : Inherits VB_Algorithm
    Public gradientColorMap As New cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Convert and Scale value", 0, 100, 45)
        labels(3) = "Palette used to color left image"
        desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static cvtScaleSlider = findSlider("Convert and Scale value")
        If task.optionsChanged Then
            gColor.color1 = cv.Scalar.Yellow
            gColor.color2 = cv.Scalar.Red
            Dim gradMat As New cv.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(Nothing)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cv.Scalar.Blue
            gColor.Run(Nothing)

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
        dst2 = Palette_Custom_Apply(depth8u, gradientColorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Palette_RGBDepth : Inherits VB_Algorithm
    Dim gradientColorMap As New cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        desc = "Build a colormap that best shows the depth.  NOTE: duplicate of Palette_DepthColorMap but no slider."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.optionsChanged Then
            gColor.color1 = cv.Scalar.Yellow
            gColor.color2 = cv.Scalar.Red
            Dim gradMat As New cv.Mat

            gColor.gradientWidth = dst1.Width
            gColor.Run(Nothing)
            gradientColorMap = gColor.gradient

            gColor.color2 = gColor.color1
            gColor.color1 = cv.Scalar.Blue
            gColor.Run(Nothing)

            cv.Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap)
            gradientColorMap = gradientColorMap.Resize(New cv.Size(255, 1))
        End If

        Dim sliderVal = If(task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i", 50, 80)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(sliderVal)
        dst2 = Palette_Custom_Apply(depth8u, gradientColorMap)
    End Sub
End Class







Public Class Palette_Layout2D : Inherits VB_Algorithm
    Public Sub New()
        desc = "Layout the available colors in a 2D grid"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
        dst2 = vbPalette(task.leftview.ConvertScaleAbs)
        dst3 = vbPalette(task.rightview.ConvertScaleAbs)
    End Sub
End Class







Public Class Palette_TaskColors : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "ScalarColors", "VecColors"}
        desc = "Display that task.scalarColors and task.vecColors"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
    Dim colorMap As New cv.Mat
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
                    If i = 0 Then colorMap = gradMat Else cv.Cv2.HConcat(colorMap, gradMat, colorMap)
                Next
                colorMap = colorMap.Resize(New cv.Size(256, 1))
                cv.Cv2.ImWrite(task.homeDir + "data\nextScheme.jpg", colorMap) ' use this to create new color schemes.
            Else
                colorMap = cv.Cv2.ImRead(schemeName).Row(0).Clone
            End If
        End If

        setTrueText("Use the 'Color Transitions' slider and radio buttons to change the color ranges.", 3)
        Dim depth8u = task.pcSplit(2).ConvertScaleAbs(colorTransitionCount)
        dst2 = Palette_Custom_Apply(depth8u, colorMap)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class





Public Class Palette_Random : Inherits VB_Algorithm
    Public colorMap As cv.Mat
    Public Sub New()
        colorMap = New cv.Mat(1, 256, cv.MatType.CV_8UC3, 0)
        For i = 0 To 255
            colorMap.Set(Of cv.Vec3b)(0, i, randomCellColor())
        Next
        colorMap.Set(Of cv.Vec3b)(0, 0, New cv.Vec3b(0, 0, 0)) ' set 0th entry to black...

        desc = "Build a random colormap - no smooth transitions."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = Palette_Custom_Apply(src, colorMap)
    End Sub
End Class





Public Class Palette_Variable : Inherits VB_Algorithm
    Public colorMap As cv.Mat
    Public originalColorMap As cv.Mat
    Public colors As New List(Of cv.Vec3b)
    Public Sub New()
        colorMap = New cv.Mat(1, 256, cv.MatType.CV_8UC3, 0)
        For i = 0 To 255
            colorMap.Set(Of cv.Vec3b)(0, i, randomCellColor())
        Next
        originalColorMap = colorMap.Clone
        desc = "Build a new palette for every frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For i = 0 To colors.Count - 1
            colorMap.Set(Of cv.Vec3b)(0, i, colors(i))
        Next
        dst2 = Palette_Custom_Apply(src, colorMap)
    End Sub
End Class
