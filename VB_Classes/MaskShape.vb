Imports cv = OpenCvSharp
Public Class MaskShape_Basics : Inherits VBparent
    Public km As New KMeans_CCompMasks
    Dim mats As New Mat_4to1
    Dim tView As New TimeView_Basics
    Public Sub New()
        labels(2) = "Click the centroid to identify shape"
        labels(3) = "Object in RGB, object mask, side view, top view"
        task.desc = "Identify the shape of each object identified in RGB"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        km.Run(src)
        dst2 = km.dst2
        mats.mat(0).SetTo(0)
        task.color.CopyTo(mats.mat(0), km.dst3)
        mats.mat(1) = km.dst3

        Dim pc = New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, km.dst3)

        tView.Run(pc)
        mats.mat(2) = tView.dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        mats.mat(2).ConvertTo(mats.mat(2), cv.MatType.CV_8UC1)

        mats.mat(3) = tView.dst3.Normalize(0, 255, cv.NormTypes.MinMax)
        mats.mat(3).ConvertTo(mats.mat(3), cv.MatType.CV_8UC1)

        mats.Run(Nothing)
        dst3 = mats.dst2

        Static pixelCounts As New List(Of Integer)
        Static saveMaskIndex As Integer
        If saveMaskIndex <> km.km.maskIndex Then
            pixelCounts.Clear()
            saveMaskIndex = km.km.maskIndex
        End If
        pixelCounts.Add(km.km.masks(saveMaskIndex).CountNonZero)
        If pixelCounts.Count > 100 Then pixelCounts.RemoveAt(0)

        ' compute stdev from the list
        Dim avg = pixelCounts.Average()
        Dim sum = pixelCounts.Sum(Function(d As Integer) Math.Pow(d - avg, 2))
        Dim stdev = Math.Sqrt(sum / pixelCounts.Count)

        ' labels(2) = "Selected mask has stdev of " + Format(stdev, "#0") + " n=" + CStr(pixelCounts.Count) + " avg=" + Format(avg, "###,##0")
    End Sub
End Class






Public Class MaskShape_Depth : Inherits VBparent
    Dim proxy As New MaskShape_Basics
    Public Sub New()
        task.desc = "Get a mask from the Proximity_Basics using depth and use it to find its shape in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        Static kSlider = findSlider("kMeans k")
        If proxy.km.km.maskIndex = kSlider.Maximum Then
            setTrueText("The closest mask in depth matches the area with no depth so no data is displayed.", 10, 40, 3)
            dst3.SetTo(0)
        Else
            proxy.Run(task.depth32f)
            dst2 = proxy.dst2
            dst3 = proxy.dst3
        End If
        labels(2) = proxy.labels(2)
        labels(3) = proxy.labels(3)
    End Sub
End Class