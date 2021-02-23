Imports cv = OpenCvSharp
Imports dl = DlibDotNet
Imports System.Runtime.InteropServices
Public Class Dlib_Sobel_CS
    Inherits VBparent
    Dim d2Mat As Mat_Dlib2Mat
    Dim sobel As New CS_Classes.Dlib_EdgesSobel
    Public Sub New()
        initParent()
        d2Mat = New Mat_Dlib2Mat
        task.desc = "Testing the DLib interface with a simple Sobel example"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sobel.Run(input)

        d2Mat.dGray = sobel.edgeImage
        d2Mat.Run()
        dst1 = d2Mat.dst1
    End Sub
End Class







Public Class Dlib_GaussianBlur_CS
    Inherits VBparent
    Dim blur As New CS_Classes.Dlib_GaussianBlur
    Dim d2Mat As Mat_Dlib2Mat
    Public Sub New()
        initParent()
        d2Mat = New Mat_Dlib2Mat
        label1 = "Gaussian Blur of grayscale image"
        label2 = "Gaussian Blur of BGR image"
        task.desc = "Use DlibDotNet to blur an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        blur.Run(input)

        d2Mat.dGray = blur.blurredGray
        d2Mat.Run()
        dst1 = d2Mat.dst1

        blur.Run(src) ' now blur the 8uc3 image
        d2Mat.dRGB = blur.blurredRGB
        dst2 = d2Mat.dst2
    End Sub
End Class







Public Class Dlib_FaceDetectHOG_CS
    Inherits VBparent
    Dim faces As New CS_Classes.Dlib_FaceDetectHOG
    Dim d2Mat As Mat_Dlib2Mat
    Public Sub New()
        initParent()
        faces.initialize()
        d2Mat = New Mat_Dlib2Mat
        task.desc = "Use DlibDotNet to detect faces using the HOG detector"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        faces.Run(input)

        dst1 = src
        For Each r In faces.rects
            Dim rect = New cv.Rect(r.Left, r.Top, r.Width, r.Height)
            dst1.Rectangle(rect, cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class