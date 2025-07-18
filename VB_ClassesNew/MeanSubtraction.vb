Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics : Inherits TaskParent
    Public scaleValue As Single = 16
    Public Sub New()
        desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mean = cv.Cv2.Mean(src)
        cv.Cv2.Subtract(mean, src, dst2)
        dst2 *= 100 / scaleValue
    End Sub
End Class





Public Class MeanSubtraction_LeftRight : Inherits TaskParent
    Dim LRMeanSub As New MeanSubtraction_Gray
    Public Sub New()
        desc = "Apply mean subtraction to the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRMeanSub.Run(task.leftView)
        dst2 = LRMeanSub.dst2.Clone
        labels(2) = "LeftView image"

        LRMeanSub.Run(task.rightView)
        dst3 = LRMeanSub.dst2
        labels(3) = "RightView image"
    End Sub
End Class





Public Class MeanSubtraction_Gray : Inherits TaskParent
    Public MeanSub As New MeanSubtraction_Basics
    Public classCount As Integer
    Public Sub New()
        labels(3) = "Image below is 255 - dst2"
        desc = "Apply mean subtraction of the grayscale image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MeanSub.Run(src)
        dst2 = MeanSub.dst2
        labels(2) = "MeanSubtraction gray image "

        dst3 = 255 - dst2
    End Sub
End Class





Public Class MeanSubtraction_Left : Inherits TaskParent
    Public scaleValue As Single = 16
    Public Sub New()
        desc = "Apply mean subtraction to the left image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mean = cv.Cv2.Mean(task.leftView)
        cv.Cv2.Subtract(mean, task.leftView, dst2)
        dst2 *= 100 / scaleValue
    End Sub
End Class