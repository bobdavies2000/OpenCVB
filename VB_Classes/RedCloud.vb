Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim prep As New RedCloud_PointCloud
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False ' no artifacts.
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "Floodfill the prep'd pointcloud output so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src.Clone)
        dst3 = reduction.dst2

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(dst3, task.depthMask)

        fCell.Run(dst3)

        dst2 = fCell.dst2
        labels(2) = fCell.labels(2)
    End Sub
End Class









Public Class RedCloud_CloudOnly : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim prep As New RedCloud_PointCloud
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False ' no artifacts.
        desc = "Run RedCell_Basics only on the prep'd data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        prep.Run(Nothing)

        fCell.Run(prep.dst2)

        dst2 = fCell.dst2
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = fCell.labels(2)
    End Sub
End Class








Public Class RedCloud_PointCloud : Inherits VB_Algorithm
    Public Sub New()
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim reduceAmt = redOptions.PCreductionSlider.Value
        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()

        Select Case redOptions.PCReduction
            Case OptionsRedCloud.reduceX
                dst0 = split(0) * reduceAmt
            Case OptionsRedCloud.reduceY
                dst0 = split(1) * reduceAmt
            Case OptionsRedCloud.reduceZ
                dst0 = split(2) * reduceAmt
            Case OptionsRedCloud.reduceXY
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt
            Case OptionsRedCloud.reduceXZ
                dst0 = split(0) * reduceAmt + split(2) * reduceAmt
            Case OptionsRedCloud.reduceYZ
                dst0 = split(1) * reduceAmt + split(2) * reduceAmt
            Case OptionsRedCloud.reduceXYZ
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt
        End Select

        Dim mm = vbMinMax(dst0)
        dst2 = (dst0 - mm.minVal)

        dst2.SetTo(mm.maxVal - mm.minVal, task.maxDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        mm = vbMinMax(dst2)
        dst2 *= 254 / mm.maxVal
        dst2 += 1
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        labels(2) = "Reduced Pointcloud - reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_Test : Inherits VB_Algorithm
    Dim prep As New RedCloud_PointCloud
    Public fCell As New RedCell_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(reduction.dst2, task.depthMask)

        fCell.Run(reduction.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions identified"
    End Sub
End Class