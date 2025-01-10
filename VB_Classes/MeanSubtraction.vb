Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics : Inherits TaskParent
    Dim options As New Options_MeanSubtraction
    Public Sub New()
        desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Dim mean = cv.Cv2.Mean(src)
        cv.Cv2.Subtract(mean, src, dst2)
        dst2 *= CSng(100 / options.scaleVal)
    End Sub
End Class





Public Class MeanSubtraction_LeftRight : Inherits TaskParent
    Dim meanSub As New MeanSubtraction_Basics
    Public Sub New()
        desc = "Apply mean subtraction to the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        meanSub.Run(task.leftView)
        dst2 = meanSub.dst2.Clone
        labels(2) = "LeftView image"

        meanSub.Run(task.rightView)
        dst3 = meanSub.dst2
        labels(3) = "RightView image"
    End Sub
End Class
