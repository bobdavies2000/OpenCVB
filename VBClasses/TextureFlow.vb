Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class TextureFlow_Basics : Inherits TaskParent
        Dim options As New Options_Texture
        Public Sub New()
            desc = "Find and mark the texture flow in an image - see texture_flow.py"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src.Clone
            If src.Channels() <> 1 Then src = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
            Dim eigen = src.CornerEigenValsAndVecs(options.TFblockSize, options.TFksize)
            Dim split = eigen.Split()
            Dim d2 = options.TFdelta / 2
            For y = d2 To dst2.Height - 1 Step d2
                For x = d2 To dst2.Width - 1 Step d2
                    Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * options.TFdelta
                    Dim p1 = New cv.Point(CInt(x - delta.X), CInt(y - delta.Y))
                    Dim p2 = New cv.Point(CInt(x + delta.X), CInt(y + delta.Y))
                    vbc.DrawLine(dst2, p1, p2, white)
                Next
            Next
        End Sub
    End Class





    Public Class TextureFlow_Depth : Inherits TaskParent
        Dim flow As New TextureFlow_Basics
        Public Sub New()
            desc = "Display texture flow in the depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flow.Run(task.depthRGB)
            dst2 = flow.dst2
        End Sub
    End Class






    Public Class TextureFlow_Reduction : Inherits TaskParent
        Dim flow As New TextureFlow_Basics
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Display texture flow in the reduced color image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(src)
            dst2 = reduction.dst2

            flow.Run(reduction.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            dst3 = flow.dst2
        End Sub
    End Class







    Public Class TextureFlow_DepthSegments : Inherits TaskParent
        Dim segments As New Histogram_CloudSegments
        Dim diffx As New Edge_DiffX_CPP
        Dim flow As New TextureFlow_Basics
        Public Sub New()
            labels = {"", "", "TextureFlow output", "TextureFlow Input"}
            desc = "Find the texture flow for the depth segments output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            segments.Run(src)
            diffx.Run(segments.dst1)
            dst3 = segments.dst3
            flow.Run(dst3)
            dst2 = flow.dst2
        End Sub
    End Class






    Public Class TextureFlow_Bricks : Inherits TaskParent
        Dim bPoint As New BrickPoint_Best
        Dim flow As New TextureFlow_Basics
        Dim knn As New KNN_Basics
        Public Sub New()
            desc = "Use the grid points as input to the texture flow algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(task.grayStable)
            dst3 = bPoint.dst3

            knn.trainInput.Clear()
            For Each pt In bPoint.bestBricks
                knn.trainInput.Add(New cv.Point2f(pt.X, pt.Y))
            Next
            knn.queries = New List(Of cv.Point2f)(knn.trainInput)
            knn.Run(emptyMat)

            For i = 0 To knn.neighbors.Count - 1
                dst3.Line(knn.trainInput(i), knn.trainInput(knn.neighbors(i)(1)), 255, task.lineWidth, task.lineType)
                dst3.Line(knn.trainInput(i), knn.trainInput(knn.neighbors(i)(2)), 255, task.lineWidth, task.lineType)
            Next

            flow.Run(dst3)
            dst2 = flow.dst2
        End Sub
    End Class
End Namespace