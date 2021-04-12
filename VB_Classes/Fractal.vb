Imports System.Numerics
Imports cv = OpenCvSharp
' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_Mandelbrot
    Inherits VBparent
    Public startX As Single = -2
    Public endX As Single = 2
    Public startY As Single = -1.5
    Public endY As Single = 1.5
    Public saveIterations As Integer
    Public incrX As Single
    Public incrY As Single
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mandelbrot iterations", 1, 50, 34)
        End If
        task.desc = "Run the classic Mandalbrot algorithm"
		' task.rank = 1
        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        saveIterations = 0
    End Sub
    Public Sub mandelbrotLoop(y As Integer, iterations As Integer)
        incrX = (endX - startX) / src.Width
        incrY = (endY - startY) / src.Height
        For x = 0 To src.Width - 1
            Dim c = New Complex(startX + x * incrX, startY + y * incrY)
            Dim z = New Complex(0, 0)
            Dim iter = 0
            While Complex.Abs(z) < 2 And iter < iterations
                z = z * z + c
                iter += 1
            End While
            dst1.Set(Of Byte)(y, x, If(iter < iterations, 255 * iter / (iterations - 1), 0))
        Next
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim iterations = sliders.trackbar(0).Value
        If saveIterations <> iterations Then
            saveIterations = iterations
            For y = 0 To src.Height - 1
                mandelbrotLoop(y, iterations)
            Next
        End If
    End Sub
End Class





' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_Mandelbrot_MT
    Inherits VBparent
    Dim mandel As Fractal_Mandelbrot
    Public Sub New()
        initParent()
        mandel = New Fractal_Mandelbrot()
        task.desc = "Run a multi-threaded version of the Mandalbrot algorithm"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim iterations = mandel.sliders.trackbar(0).Value
        Parallel.For(0, src.Height,
        Sub(y)
            mandel.mandelbrotLoop(y, iterations)
        End Sub)
        dst1 = mandel.dst1
    End Sub
End Class






' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_MandelbrotZoom
    Inherits VBparent
    Public mandel As Fractal_Mandelbrot
    Public Sub New()
        initParent()
        mandel = New Fractal_Mandelbrot()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Reset to original Mandelbrot"
        End If
        task.desc = "Run the classic Mandalbrot algorithm and allow zooming in"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim iterations = mandel.sliders.trackbar(0).Value

        If check.Box(0).Checked Then
            mandel.Dispose()
            mandel = New Fractal_Mandelbrot()
        End If
        If task.drawRect.Width <> 0 Then
            Dim newStartX = mandel.startX + (mandel.endX - mandel.startX) * task.drawRect.X / src.Width
            mandel.endX = mandel.startX + (mandel.endX - mandel.startX) * (task.drawRect.X + task.drawRect.Width) / src.Width
            mandel.startX = newStartX

            Dim newStartY = mandel.startY + (mandel.endY - mandel.startY) * task.drawRect.Y / src.Height
            Dim Height = task.drawRect.Width * src.Height / src.Width ' maintain aspect ratio across zooms...
            mandel.endY = mandel.startY + (mandel.endY - mandel.startY) * (task.drawRect.Y + Height) / src.Height
            mandel.startY = newStartY

            mandel.saveIterations = 0
            task.drawRectClear = True
        End If
        If mandel.saveIterations <> iterations Then
            mandel.saveIterations = iterations
            Parallel.For(0, src.Height,
            Sub(y)
                mandel.mandelbrotLoop(y, iterations)
            End Sub)
            check.Box(0).Checked = False
        End If
        dst1 = mandel.dst1
        label1 = If(mandel.endX - mandel.startX >= 3.999, "Mandelbrot Zoom - draw anywhere", "Mandelbrot Zoom = ~" +
                                                          Format(4 / (mandel.endX - mandel.startX), "###,###.0") + "X zoom")
    End Sub
End Class






Public Class Fractal_MandelbrotZoomColor
    Inherits VBparent
    Public mandel As Fractal_MandelbrotZoom
    Public palette As Palette_Basics
    Public Sub New()
        initParent()
        mandel = New Fractal_MandelbrotZoom()
        palette = New Palette_Basics()
        Dim hsvRadio = findRadio("Hsv")
        hsvRadio.Checked = True
        task.desc = "Classic Mandelbrot in color"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        mandel.Run()
        palette.src = mandel.dst1
        palette.Run()
        dst1 = palette.dst1
        label1 = mandel.label1
    End Sub
End Class








' http://www.malinc.se/m/JuliaSets.php
' https://www.geeksforgeeks.org/julia-fractal-set-in-c-c-using-graphics/
Public Class Fractal_Julia
    Inherits VBparent
    Dim mandel As Fractal_MandelbrotZoomColor
    Dim rt As Double = 0.282
    Dim mt As Double = -0.58
    Public Sub New()
        initParent()
        mandel = New Fractal_MandelbrotZoomColor()
        label2 = "Mouse selects different Julia Sets - zoom for detail"
        task.desc = "Build Julia set from any point in the Mandelbrot fractal"
		' task.rank = 1
    End Sub
    Private Function julia_point(x As Single, y As Single, r As Integer, depth As Integer, max As Integer, c As Complex, z As Complex)
        If Complex.Abs(z) > r Then
            Dim mt = (255 * Math.Pow(max - depth, 2) Mod (max * max)) Mod 255
            dst1.Set(Of Byte)(y, x, 255 - mt)
            depth = 0
        End If
        If Math.Sqrt(Math.Pow(x - src.Width / 2, 2) + Math.Pow(y - src.Height / 2, 2)) > src.Height / 2 Then dst1.Set(Of Byte)(y, x, 0)
        If depth < max / 4 Then Return 0
        Return julia_point(x, y, r, depth - 1, max, c, Complex.Pow(z, 2) + c)
    End Function
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Static savedMouse = New cv.Point(-1, -1)
        If savedMouse <> task.mousePoint Or mandel.mandel.check.Box(0).Checked Then
            savedMouse = task.mousePoint
            mandel.Run()
            dst2 = mandel.dst1.Clone

            Dim detail = 1
            Dim depth = 100
            Dim r = 2
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            Dim m = mandel.mandel.mandel
            rt = m.startX + (m.endX - m.startX) * task.mousePoint.X / src.Width
            mt = m.startY + (m.endY - m.startY) * task.mousePoint.Y / src.Height
            Dim c = New Complex(rt, mt)
            Parallel.For(CInt(src.Width / 2 - src.Height / 2), CInt(src.Width / 2 + src.Height / 2),
                Sub(x)
                    For y = 0 To src.Height - 1 Step detail
                        Dim z = New Complex(2 * r * (x - src.Width / 2) / src.Height, 2 * r * (y - src.Height / 2) / src.Height)
                        julia_point(x, y, r, depth, depth, c, z)
                    Next
                End Sub)
            mandel.palette.src = dst1
            mandel.palette.Run()
            dst1 = mandel.palette.dst1
        End If
    End Sub
End Class

