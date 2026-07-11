Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class TextureFlow_Basics : Inherits TaskParent
    Dim options As New Options_Texture
    Public Sub New()
        desc = "Find and mark the texture flow in an image - see texture_flow.py"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        If src.Channels() <> 1 Then cv.Cv2.CvtColor(src, src, OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        Dim eigen As New cv.Mat
        cv.Cv2.CornerEigenValsAndVecs(src, eigen, options.TFblockSize, options.TFksize)
        Dim split = cv.Cv2.Split(eigen)
        Dim d2 = options.TFdelta / 2
        For y = d2 To dst2.Height - 1 Step d2
            For x = d2 To dst2.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * options.TFdelta
                Dim p1 = New cv.Point(CInt(x - delta.X), CInt(y - delta.Y))
                Dim p2 = New cv.Point(CInt(x + delta.X), CInt(y + delta.Y))
                cv.Cv2.Line(dst2, p1, p2, white, task.lineWidth, task.lineType)
            Next
        Next
    End Sub
End Class





Public Class XR_TextureFlow_Depth : Inherits TaskParent
    Dim flow As New TextureFlow_Basics
    Public Sub New()
        desc = "Display texture flow in the depth data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flow.Run(task.depthRGB)
        dst2 = flow.dst2
    End Sub
End Class






Public Class XR_TextureFlow_Reduction : Inherits TaskParent
    Dim flow As New TextureFlow_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Display texture flow in the reduced color image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2

        Dim _flow_cvt As New cv.Mat
        cv.Cv2.CvtColor(reduction.dst2, _flow_cvt, cv.ColorConversionCodes.GRAY2BGR)
        flow.Run(_flow_cvt)
        dst3 = flow.dst2
    End Sub
End Class







Public Class XR_TextureFlow_DepthSegments : Inherits TaskParent
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






Public Class XR_TextureFlow_Bricks : Inherits TaskParent
    Dim bPoint As New XR_BrickPoint_Best
    Dim flow As New TextureFlow_Basics
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Use the grid points as input to the texture flow algorithm"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(task.gray)
        dst3 = bPoint.dst3

        knn.trainInput.Clear()
        For Each pt In bPoint.bestBricks
            knn.trainInput.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        knn.queries = New List(Of cv.Point2f)(knn.trainInput)
        knn.Run(emptyMat)

        For i = 0 To knn.queries.Count - 1
            cv.Cv2.Line(dst3, knn.trainInput(i), knn.trainInput(knn.result(i, 1)), 255, task.lineWidth, task.lineType)
            cv.Cv2.Line(dst3, knn.trainInput(i), knn.trainInput(knn.result(i, 2)), 255, task.lineWidth, task.lineType)
        Next

        flow.Run(dst3)
        dst2 = flow.dst2
    End Sub
End Class
