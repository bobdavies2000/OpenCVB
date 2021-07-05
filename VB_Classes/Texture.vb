Imports cv = OpenCvSharp
Imports System.Threading
Public Class Texture_Basics : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim ellipse As New Draw_Ellipses
    Public texture As New cv.Mat
    Public tRect As cv.Rect
    Dim texturePop As Integer
    Public tChange As Boolean ' if the texture hasn't changed this will be false.
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 64
        grid.RunClass(Nothing)
        task.desc = "Use multi-threading to find the best sample 256x256 texture of a mask"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        If standalone Or src.Channels <> 1 Then
            ellipse.RunClass(src)
            dst2 = ellipse.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst2 = dst2.ConvertScaleAbs(255)
            dst3 = ellipse.dst2.Clone
            dst3.SetTo(cv.Scalar.Yellow, grid.gridMask)
        Else
            dst2 = src
        End If

        tChange = True
        If texturePop > 0 Then
            Dim nextCount = dst2(tRect).CountNonZero()
            If nextCount >= texturePop * 0.95 Then tChange = False
        End If
        If tChange Then
            Dim sortcounts As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
            For Each roi In grid.roiList
                sortcounts.Add(dst2(roi).CountNonZero(), roi)
            Next
            If standalone or task.intermediateActive Then dst3.Rectangle(sortcounts.ElementAt(0).Value, cv.Scalar.White, 2)
            tRect = sortcounts.ElementAt(0).Value
            texture = task.color(tRect)
            texturePop = dst2(tRect).CountNonZero()
        End If
        If standalone or task.intermediateActive Then dst3.Rectangle(tRect, cv.Scalar.White, 2)
    End Sub
End Class






Public Class Texture_Flow : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Texture Flow Delta", 2, 100, 30)
            sliders.setupTrackBar(1, "Texture Eigen BlockSize", 1, 100, 50)
            sliders.setupTrackBar(2, "Texture Eigen Ksize", 1, 15, 1)
        End If

        task.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim TFdelta = sliders.trackbar(0).Value
        Dim TFblockSize = sliders.trackbar(1).Value * 2 + 1
        Dim TFksize = sliders.trackbar(2).Value * 2 + 1
        dst2 = src.Clone
        If src.Channels <> 1 Then src = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        Dim eigen = src.CornerEigenValsAndVecs(TFblockSize, TFksize)
        Dim split = eigen.Split()
        Dim d2 = TFdelta / 2
        For y = d2 To dst2.Height - 1 Step d2
            For x = d2 To dst2.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * TFdelta
                Dim p1 = New cv.Point(x - delta.X, y - delta.Y)
                Dim p2 = New cv.Point(x + delta.X, y + delta.Y)
                dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            Next
        Next
    End Sub
End Class





Public Class Texture_Flow_Depth : Inherits VBparent
    Dim texture As Texture_Flow
    Public Sub New()
        texture = New Texture_Flow()
        task.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        texture.RunClass(task.RGBDepth)
        dst2 = texture.dst2
    End Sub
End Class






Public Class Texture_Flow_Reduction : Inherits VBparent
    Dim texture As Texture_Flow
    Dim reduction As New Reduction_Basics
    Public Sub New()
        texture = New Texture_Flow
        task.desc = "Display texture flow in the reduced color image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(task.color)
        dst2 = reduction.dst2

        texture.RunClass(reduction.dst2)
        dst3 = texture.dst2
    End Sub
End Class







Public Class Texture_Shuffle : Inherits VBparent
    Dim shuffle As New Random_Shuffle
    Dim floor As New OpenGL_FloorPlane
    Dim texture As Texture_Basics
    Public tRect As cv.Rect
    Public rgbaTexture As New cv.Mat
    Public Sub New()
        texture = New Texture_Basics()
        task.desc = "Use random shuffling to homogenize a texture sample of what the floor looks like."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateActive Then
            floor.plane.RunClass(src)
            dst3.SetTo(0)
            src.CopyTo(dst3, floor.plane.sliceMask)
            dst2 = floor.plane.dst2
            src = floor.plane.sliceMask
        End If

        texture.RunClass(src)
        dst2 = texture.dst3
        dst3.Rectangle(texture.tRect, cv.Scalar.White, 2)
        shuffle.RunClass(texture.texture)
        tRect = New cv.Rect(0, 0, texture.tRect.Width * 4, texture.tRect.Height * 4)
        dst2(tRect) = shuffle.dst2.Repeat(4, 4)
        Dim split = dst2(tRect).Split()
        Dim alpha As New cv.Mat(split(0).Size, cv.MatType.CV_8U, 1)
        Dim merged() = {split(2), split(1), split(0), alpha}
        cv.Cv2.Merge(merged, rgbaTexture)
    End Sub
End Class

