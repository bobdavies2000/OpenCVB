Imports cv = OpenCvSharp
Public Class Dilate_Basics : Inherits VB_Algorithm
    Public options As New Options_Dilate
    Public Sub New()
        desc = "Dilate the image provided."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        If options.noshape Or options.iterations = 0 Then dst2 = src Else dst2 = src.Dilate(options.element, Nothing, options.iterations)

        If standalone Then
            dst3 = task.depthRGB.Dilate(options.element, Nothing, options.iterations)
            labels(3) = "Dilated Depth " + CStr(options.iterations) + " times"
        End If
        labels(2) = "Dilated BGR " + CStr(options.iterations) + " times"
    End Sub
End Class








Public Class Dilate_OpenClose : Inherits VB_Algorithm
    Dim options As New Options_Dilate
    Public Sub New()
        desc = "Erode and dilate with MorphologyEx on the BGR and Depth image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        Dim openClose = If(options.iterations > 0, cv.MorphTypes.Open, cv.MorphTypes.Close)
        cv.Cv2.MorphologyEx(task.depthRGB, dst3, openClose, options.element)
        cv.Cv2.MorphologyEx(src, dst2, openClose, options.element)
    End Sub
End Class









Public Class Dilate_Erode : Inherits VB_Algorithm
    Dim options As New Options_Dilate
    Public Sub New()
        desc = "Erode and dilate with MorphologyEx on the input image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        cv.Cv2.MorphologyEx(src, dst2, cv.MorphTypes.Open, options.element)
        cv.Cv2.MorphologyEx(dst2, dst2, cv.MorphTypes.Close, options.element)
    End Sub
End Class