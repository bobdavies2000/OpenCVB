Imports cv = OpenCvSharp
Public Class MaskShape_Basics : Inherits VBparent
    Dim tView As New TimeView_Basics
    Dim mats As New Mat_4Click
    Dim proxy As New Proximity_BasicsRGB
    Public Sub New()
        task.desc = "Get a mask from the Proximity_Basics (default RGB mode) and use it to find its shape in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static maskSlider = findSlider("Select Mask - light to dark or farthest to closest")
        Static saveMaskIndex = -1
        Static pixelCounts As New List(Of Integer)
        If saveMaskIndex <> maskSlider.value Then
            saveMaskIndex = maskSlider.value
            pixelCounts.Clear()
        End If

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

        pixelCounts.Add(mats.mat(1).CountNonZero)
        If pixelCounts.Count > 100 Then pixelCounts.RemoveAt(0)

        ' compute stdev from the list
        Dim avg = pixelCounts.Average()
        Dim sum = pixelCounts.Sum(Function(d As Integer) Math.Pow(d - avg, 2))
        Dim stdev = Math.Sqrt(sum / pixelCounts.Count)

        label1 = "KMeans, selected mask, mask sideview, mask topview"
        label2 = "Selected mask has stdev of " + Format(stdev, "#0.00") + " n=" + CStr(pixelCounts.Count) + " avg=" + Format(avg, "#0")
    End Sub
End Class







Public Class MaskShape_Depth : Inherits VBparent
    Dim proxy As New MaskShape_Basics
    Public Sub New()
        task.desc = "Get a mask from the Proximity_Basics using depth and use it to find its shape in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static maskSlider = findSlider("Select Mask - light to dark or farthest to closest")
        If maskSlider.maximum = maskSlider.value Then
            setTrueText("The closest mask in depth matches the area with no depth so no data is displayed.", 10, 40, 3)
            dst2.SetTo(0)
        Else
            proxy.Run(task.depth32f)
            dst1 = proxy.dst1
            dst2 = proxy.dst2
        End If
    End Sub
End Class