Imports cv = OpenCvSharp
Imports System.Threading
Public Class Texture_Basics
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim ellipse As Draw_Ellipses
    Public texture As New cv.Mat
    Public tRect As cv.Rect
    Dim texturePop As Integer
    Public tChange As Boolean ' if the texture hasn't changed this will be false.
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 64
        grid.Run()

        ellipse = New Draw_Ellipses()
        task.desc = "Use multi-threading to find the best sample 256x256 texture of a mask"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Or src.Channels <> 1 Then
            ellipse.Run()
            dst1 = ellipse.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst1 = dst1.ConvertScaleAbs(255)
            dst2 = ellipse.dst1.Clone
            dst2.SetTo(cv.Scalar.Yellow, grid.gridMask)
        Else
            dst1 = src
        End If

        tChange = True
        If texturePop > 0 Then
            Dim nextCount = dst1(tRect).CountNonZero()
            If nextCount >= texturePop * 0.95 Then tChange = False
        End If
        If tChange Then
            Dim sortcounts As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
            For Each roi In grid.roiList
                sortcounts.Add(dst1(roi).CountNonZero(), roi)
            Next
            If standalone or task.intermediateReview = caller Then dst2.Rectangle(sortcounts.ElementAt(0).Value, cv.Scalar.White, 2)
            tRect = sortcounts.ElementAt(0).Value
            texture = task.color(tRect)
            texturePop = dst1(tRect).CountNonZero()
        End If
        If standalone or task.intermediateReview = caller Then dst2.Rectangle(tRect, cv.Scalar.White, 2)
    End Sub
End Class






Public Class Texture_Flow
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Texture Flow Delta", 2, 100, 12)
            sliders.setupTrackBar(1, "Texture Eigen BlockSize", 1, 100, 20)
            sliders.setupTrackBar(2, "Texture Eigen Ksize", 1, 15, 1)
        End If

        task.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim TFdelta = sliders.trackbar(0).Value
        Dim TFblockSize = sliders.trackbar(1).Value * 2 + 1
        Dim TFksize = sliders.trackbar(2).Value * 2 + 1
        Dim gray = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone
        Dim eigen = gray.CornerEigenValsAndVecs(TFblockSize, TFksize)
        Dim split = eigen.Split()
        Dim d2 = TFdelta / 2
        For y = d2 To dst1.Height - 1 Step d2
            For x = d2 To dst1.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * TFdelta
                Dim p1 = New cv.Point(x - delta.X, y - delta.Y)
                Dim p2 = New cv.Point(x + delta.X, y + delta.Y)
                dst1.Line(p1, p2, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
End Class





Public Class Texture_Flow_Depth
    Inherits VBparent
    Dim texture As Texture_Flow
    Public Sub New()
        initParent()
        texture = New Texture_Flow()
        task.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        texture.src = task.RGBDepth
        texture.Run()
        dst1 = texture.dst1
    End Sub
End Class






Public Class Texture_Flow_Reduction
    Inherits VBparent
    Dim texture As Texture_Flow
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()
        texture = New Texture_Flow
        reduction = New Reduction_Basics
        task.desc = "Display texture flow in the reduced color image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.src = task.color
        reduction.Run()
        dst1 = reduction.dst1

        texture.src = reduction.dst1
        texture.Run()
        dst2 = texture.dst1
    End Sub
End Class







Public Class Texture_Shuffle
    Inherits VBparent
    Dim shuffle As Random_Shuffle
    Dim floor As OpenGL_FloorPlane
    Dim texture As Texture_Basics
    Public tRect As cv.Rect
    Public rgbaTexture As New cv.Mat
    Public Sub New()
        initParent()
        floor = New OpenGL_FloorPlane()
        texture = New Texture_Basics()
        shuffle = New Random_Shuffle()
        task.desc = "Use random shuffling to homogenize a texture sample of what the floor looks like."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            floor.plane.Run()
            dst2.SetTo(0)
            src.CopyTo(dst2, floor.plane.sliceMask)
            dst1 = floor.plane.dst1
            texture.src = floor.plane.sliceMask
        Else
            texture.src = src
        End If

        texture.Run()
        dst1 = texture.dst2
        dst2.Rectangle(texture.tRect, cv.Scalar.White, 2)
        shuffle.src = texture.texture
        shuffle.Run()
        tRect = New cv.Rect(0, 0, texture.tRect.Width * 4, texture.tRect.Height * 4)
        dst1(tRect) = shuffle.dst1.Repeat(4, 4)
        Dim split = dst1(tRect).Split()
        Dim alpha As New cv.Mat(split(0).Size, cv.MatType.CV_8U, 1)
        Dim merged() = {split(2), split(1), split(0), alpha}
        cv.Cv2.Merge(merged, rgbaTexture)
    End Sub
End Class
