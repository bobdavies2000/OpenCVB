Imports cv = OpenCvSharp
Public Class MaskShape_Basics : Inherits VBparent
    Dim tView As New TimeView_Basics
    Dim mats As New Mat_4Click
    Dim proxy As New Proximity_BasicsRGB
    Public Sub New()
        task.desc = "Get a mask from the Proximity_Basics (default RGB mode) and use it to find its shape in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        proxy.Run(src)
        mats.mat(0) = proxy.dst1
        mats.mat(1) = proxy.dst2

        Dim pc = New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, proxy.dst2)

        tView.Run(pc)

        mats.mat(2) = tView.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        mats.mat(2).ConvertTo(mats.mat(2), cv.MatType.CV_8UC1)

        mats.mat(3) = tView.dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        mats.mat(3).ConvertTo(mats.mat(3), cv.MatType.CV_8UC1)

        mats.Run(Nothing)

        dst1 = mats.dst1
        dst2 = mats.dst2
    End Sub
End Class







Public Class MaskShape_Depth : Inherits VBparent
    Dim proxy As New MaskShape_Basics
    Public Sub New()
        task.desc = "Get a mask from the Proximity_Basics using depth and use it to find its shape in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static maskSlider = findSlider("Mask - light to dark or farthest to closest")
        If maskSlider.maximum = maskSlider.value Then setTrueText("The closest mask in depth matches the area with no depth so no data is displayed.", 10, 40, 3)
        proxy.Run(task.depth32f)
        dst1 = proxy.dst1
        dst2 = proxy.dst2
    End Sub
End Class