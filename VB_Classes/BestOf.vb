Imports cv = OpenCvSharp
Public Class BestOf_Binarize
    Inherits VBparent
    Dim binarize As Binarize_Basics
    Public Sub New()
        initParent()
        binarize = New Binarize_Basics
        task.desc = "Best way to binarize an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        binarize.src = src
        binarize.Run()
        dst1 = binarize.dst1
        label1 = binarize.label1
        label2 = binarize.label2
    End Sub
End Class







Public Class BestOf_Edges
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        task.desc = "Best way to get edges from an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst1
        dst2 = edges.dst2
        label1 = edges.label1
        label2 = edges.label2
    End Sub
End Class








Public Class BestOf_Contours
    Inherits VBparent
    Dim contours As Motion_Basics
    Public Sub New()
        initParent()
        contours = New Motion_Basics
        task.desc = "Best example of how to use contours"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        contours.src = src
        contours.Run()
        dst1 = contours.dst1
        dst2 = contours.dst2
        label1 = contours.label1
        label2 = contours.label2
    End Sub
End Class







Public Class BestOf_Blobs
    Inherits VBparent
    Dim blobs As Blob_DepthClusters
    Public Sub New()
        initParent()
        blobs = New Blob_DepthClusters
        task.desc = "Best example of using depth to identify blobs"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        blobs.src = src
        blobs.Run()
        dst1 = blobs.dst1
        dst2 = blobs.dst2
        label1 = blobs.label1
        label2 = blobs.label2
    End Sub
End Class







Public Class BestOf_CComp
    Inherits VBparent
    Dim ccomp As CComp_Binarized
    Public Sub New()
        initParent()
        ccomp = New CComp_Binarized
        task.desc = "Best example of using the connected components feature"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        ccomp.src = src
        ccomp.Run()
        dst1 = ccomp.dst1
        dst2 = ccomp.dst2
        label1 = ccomp.label1
        label2 = ccomp.label2
    End Sub
End Class








Public Class BestOf_FloodFill
    Inherits VBparent
    Dim flood As FloodFill_FullImage
    Public Sub New()
        initParent()
        flood = New FloodFill_FullImage
        task.desc = "Best example of using the FloodFill feature"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        flood.src = src
        flood.Run()
        dst1 = flood.dst1
        dst2 = flood.dst2
        label1 = flood.label1
        label2 = flood.label2
    End Sub
End Class








Public Class BestOf_KNN
    Inherits VBparent
    Dim myTopView As PointCloud_Kalman_TopView
    Public Sub New()
        initParent()
        myTopView = New PointCloud_Kalman_TopView
        task.desc = "Best example of using KNN to track objects"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        myTopView.src = task.pointCloud
        myTopView.Run()
        dst1 = myTopView.dst1

        myTopView.topView.cmat.src = dst1.Clone
        myTopView.topView.cmat.Run()
        dst2 = myTopView.topView.cmat.dst1
        label1 = myTopView.topView.label1
        label2 = myTopView.topView.label2
    End Sub
End Class







Public Class BestOf_Kalman
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics
        Dim r = initRandomRect(src.Width, src.Height, 50)
        kalman.kInput = New Single() {r.X, r.Y, r.Width, r.Height}
        task.desc = "A simple example to show how to use Kalman"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        kalman.Run()

        dst1 = src
        Dim rect = New cv.Rect(CInt(kalman.kOutput(0)), CInt(kalman.kOutput(1)), CInt(kalman.kOutput(2)), CInt(kalman.kOutput(3)))
        rect = validateRect(rect)
        Static lastRect = rect
        If rect = lastRect Then
            Dim r = initRandomRect(src.Width, src.Height, 50)
            kalman.kInput = New Single() {r.X, r.Y, r.Width, r.Height}
        End If
        lastRect = rect
        dst1.Rectangle(rect, cv.Scalar.White, 6)
        dst1.Rectangle(rect, cv.Scalar.Red, 1)
        label1 = kalman.label1
    End Sub
End Class







Public Class BestOf_MotionDetection
    Inherits VBparent
    Dim motion As Motion_Basics
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        task.desc = "Best example of detecting motion and isolating in the image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        motion.src = src
        motion.Run()
        dst1 = motion.dst1
        dst2 = motion.dst2
        label1 = motion.label1
        label2 = motion.label2
    End Sub
End Class









Public Class BestOf_MSER
    Inherits VBparent
    Dim mser As MSER_Basics
    Public Sub New()
        initParent()
        mser = New MSER_Basics
        task.desc = "Best example of how to detect the main objects of interest in a scene"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        mser.src = src
        mser.Run()
        dst1 = mser.dst1
        dst2 = mser.dst2
        label1 = mser.label1
        label2 = mser.label2
    End Sub
End Class







Public Class BestOf_Stabilizer
    Inherits VBparent
    Dim stable As Stabilizer_BasicsTest
    Public Sub New()
        initParent()
        stable = New Stabilizer_BasicsTest
        task.desc = "Best example of how to stabilize the rgb image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        stable.src = src
        stable.Run()
        dst1 = stable.dst1
        dst2 = stable.dst2
        label1 = stable.label1
        label2 = stable.label2
    End Sub
End Class
