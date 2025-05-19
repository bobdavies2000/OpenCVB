Imports System.Numerics
Imports cv = OpenCvSharp
' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_Mandelbrot : Inherits TaskParent
    Public startX As Single = -2
    Public endX As Single = 2
    Public startY As Single = -1.5
    Public endY As Single = 1.5
    Dim incrX As Single
    Dim incrY As Single
    Public options As New Options_Fractal
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "Use the iteration slider to see the impact of the number of iterations."
        desc = "Run the classic Mandalbrot algorithm"
    End Sub
    Public Sub reset()
        startX = -2
        endX = 2
        startY = -1.5
        endY = 1.5
        task.drawRectClear = True
    End Sub
    Public Sub mandelbrotLoop(y As Integer)
        incrX = (endX - startX) / dst2.Width
        incrY = (endY - startY) / dst2.Height
        For x = 0 To dst2.Width - 1
            Dim c = New Complex(startX + x * incrX, startY + y * incrY)
            Dim z = New Complex(0, 0)
            Dim iter = 0
            While Complex.Abs(z) < 2 And iter < options.iterations
                z = z * z + c
                iter += 1
            End While
            dst2.Set(Of Byte)(y, x, If(iter < options.iterations, 255 * iter / (options.iterations - 1), 0))
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()
        For y = 0 To src.Height - 1
            mandelbrotLoop(y)
        Next
    End Sub
End Class





' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_MandelbrotZoom : Inherits TaskParent
    Public mandel As New Fractal_Mandelbrot
    Dim saveDrawRect As New cv.Rect(1, 1, 1, 1)
    Public Sub New()
        desc = "Run the classic Mandalbrot algorithm and allow zooming in"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.drawRect.Width <> 0 Then
            Dim newStartX = mandel.startX + (mandel.endX - mandel.startX) * task.drawRect.X / src.Width
            mandel.endX = mandel.startX + (mandel.endX - mandel.startX) * (task.drawRect.X + task.drawRect.Width) / src.Width
            mandel.startX = newStartX

            Dim newStartY = mandel.startY + (mandel.endY - mandel.startY) * task.drawRect.Y / src.Height
            Dim Height = task.drawRect.Width * src.Height / src.Width ' maintain aspect ratio across zooms...
            mandel.endY = mandel.startY + (mandel.endY - mandel.startY) * (task.drawRect.Y + Height) / src.Height
            mandel.startY = newStartY

            task.drawRectClear = True
        End If
        If mandel.options.resetCheck.Checked Then mandel.reset()

        If task.optionsChanged Or saveDrawRect <> task.drawRect Then
            saveDrawRect = task.drawRect
            mandel.Run(src)
            mandel.options.resetCheck.Checked = False
        End If
        dst2 = mandel.dst2
        labels(2) = If(mandel.endX - mandel.startX >= 3.999, "Mandelbrot Zoom - draw anywhere", "Mandelbrot Zoom = ~" +
                                                          Format(4 / (mandel.endX - mandel.startX), "###,###.0") + "X zoom")
    End Sub
End Class






Public Class Fractal_MandelbrotZoomColor : Inherits TaskParent
    Public zoom As New Fractal_MandelbrotZoom
    Public Sub New()
        desc = "Classic Mandelbrot in color"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If zoom.mandel.options.resetCheck.Checked Then zoom.mandel.reset()
        zoom.Run(src)
        dst2 = ShowPalette(zoom.dst2)
        labels(2) = zoom.labels(2)
    End Sub
End Class








' http://www.malinc.se/m/JuliaSets.php
' https://www.geeksforgeeks.org/julia-fractal-set-in-c-c-using-graphics/
Public Class Fractal_Julia : Inherits TaskParent
    Dim mandel As New Fractal_MandelbrotZoomColor
    Dim rt As Double = 0.282
    Dim mt As Double = -0.58
    Dim savedMouse = New cv.Point(-1, -1)
    Public Sub New()
        labels(3) = "Mouse selects different Julia Sets - zoom for detail"
        desc = "Build Julia set from any point in the Mandelbrot fractal"
    End Sub
    Private Function julia_point(x As Single, y As Single, r As Integer, depth As Integer, max As Integer, c As Complex, z As Complex)
        If Complex.Abs(z) > r Then
            Dim mt = (255 * Math.Pow(max - depth, 2) Mod (max * max)) Mod 256
            dst2.Set(Of Byte)(y, x, 255 - mt)
            depth = 0
        End If
        If Math.Sqrt(Math.Pow(x - dst2.Width / 2, 2) + Math.Pow(y - dst2.Height / 2, 2)) > dst2.Height / 2 Then dst2.Set(Of Byte)(y, x, 0)
        If depth < max / 4 Then Return 0
        Return julia_point(x, y, r, depth - 1, max, c, Complex.Pow(z, 2) + c)
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Static resetCheck = OptionParent.findCheckBox("Reset to original Mandelbrot")
        If savedMouse <> task.mouseMovePoint Or resetCheck.Checked Then
            savedMouse = task.mouseMovePoint
            mandel.Run(src)
            dst3 = mandel.dst2.Clone

            Dim detail = 1
            Dim depth = 100
            Dim r = 2
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            Dim m = mandel.zoom.mandel
            rt = m.startX + (m.endX - m.startX) * task.mouseMovePoint.X / src.Width
            mt = m.startY + (m.endY - m.startY) * task.mouseMovePoint.Y / src.Height
            Dim c = New Complex(rt, mt)
            Parallel.For(CInt(src.Width / 2 - src.Height / 2), CInt(src.Width / 2 + src.Height / 2),
                Sub(x)
                    For y = 0 To src.Height - 1 Step detail
                        Dim z = New Complex(2 * r * (x - src.Width / 2) / src.Height, 2 * r * (y - src.Height / 2) / src.Height)
                        julia_point(x, y, r, depth, depth, c, z)
                    Next
                End Sub)
            dst2 = ShowPalette(dst2)
        End If
    End Sub
End Class







' https://github.com/brian-xu/FractalDimension/blob/master/FractalDimension.py
Public Class Fractal_Dimension : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "RedColor_Basics output - select any region.", "The selected region (as a square)"}
        desc = "Compute the fractal dimension of the provided (square) image.  Algorithm is incomplete."
    End Sub
    Public Function dimension(Input As cv.Mat) As Double
        Dim tmp64f As New cv.Mat
        Input.ConvertTo(tmp64f, cv.MatType.CV_64F, 0, 0)
        Dim G = 256
        Dim d As Double

        For j = 2 To Input.Width / 2 - 1
            Dim h = Math.Max(1, Math.Floor(g / (Math.Floor(Input.Width / j))))
            Dim nr = 0
            Dim r = j / Input.Width
            For i = 0 To Input.Width Step j
                'Dim boxes() As 
            Next
        Next

        '    For L in range(2, (M // 2) + 1)
        '    h = max(1, g // (M // L))  # minimum box height Is 1
        '    N_r = 0
        '        r = L / M
        '        For i in range(0, M, L)
        '        boxes = [[]] * ((G + h - 1) // h)  # create enough boxes with height h to fill the fractal space
        '        For row in image[i:i +L]:   # boxes that exceed bounds are shrunk to fit
        '            For pixel in row[i:i +L]
        '                height = (pixel - G_min) // h  # lowest box Is at G_min And Each Is h gray levels tall
        '                boxes[height].append(pixel)  # assign the pixel intensity to the correct box
        '        stddev = np.sqrt(np.var(boxes, axis = 1))  # calculate the standard deviation Of Each box
        '        stddev = stddev[~np.isnan(stddev)]  # remove boxes With NaN standard deviations (src)
        '        nBox_r = 2 * (stddev // h) + 1
        '                    N_r += sum(nBox_r)
        '                    If N_r!= prev Then :
        '                          # check for plateauing
        '        r_Nr.append([r, N_r])
        '                        prev = N_r
        '                        x = np.array([np.log(1 / point[0]) For point In r_Nr])  # log(1/r)
        'y = np.array([np.log(point[1]) For point In r_Nr])  # log(Nr)
        'D = np.polyfit(x, y, 1)[0]  # D = lim r -> 0 log(Nr)/log(1/r)
        Return d
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dst3.SetTo(0)

        Static rect = New cv.Rect(0, 0, task.rcD.rect.Width, task.rcD.rect.Height)
        If task.optionsChanged Or task.mouseClickFlag Then
            rect = New cv.Rect(0, 0, task.rcD.rect.Width, task.rcD.rect.Height)
        End If

        If task.rcD.rect.Width = 0 Or task.rcD.rect.Height = 0 Then Exit Sub
        task.rcD.mask.CopyTo(dst3(New cv.Rect(0, 0, task.rcD.rect.Width, task.rcD.rect.Height)))
        If rect.Width < rect.Height Then rect.Width = rect.Height Else rect.Height = rect.Width
        dst3.Rectangle(rect, white, task.lineWidth, task.lineType)
    End Sub
End Class