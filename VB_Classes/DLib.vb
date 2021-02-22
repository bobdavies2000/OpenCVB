Imports cv = OpenCvSharp
Imports dl = DlibDotNet
Imports System.Runtime.InteropServices
Public Class Dlib_Sobel
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







Public Class Dlib_GaussianBlur
    Inherits VBparent
    Dim blur As New CS_Classes.Dlib_GaussianBlur
    Dim d2Mat As Mat_Dlib2Mat
    Public Sub New()
        initParent()
        d2Mat = New Mat_Dlib2Mat
        task.desc = "Use DlibDotNet to blur an image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        blur.Run(input)

        d2Mat.dGray = blur.blurredImg
        d2Mat.Run()
        dst1 = d2Mat.dst1
    End Sub
End Class