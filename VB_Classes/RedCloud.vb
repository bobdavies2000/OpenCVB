Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim prep As New RedCloud_Core
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
    Dim prep As New RedCloud_Core
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








Public Class RedCloud_Core : Inherits VB_Algorithm
    Public Sub New()
        redOptions.RedCloudOnly.Enabled = True
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
    Dim prep As New RedCloud_Core
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








Public Class RedCloud_InputColor : Inherits VB_Algorithm
    Dim fCell As New RedCell_Basics
    Dim km As New KMeans_Basics
    Dim reduction As New Reduction_Basics
    Dim fless As New FeatureLess_Basics
    Dim lut As New LUT_Basics
    Dim backP As New BackProject_Full
    Public Sub New()
        desc = "Floodfill the KMeans output so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Select Case redOptions.colorInput
            Case "BackProject_Full"
                backP.Run(src)
                dst1 = backP.dst2
            Case "KMeans_Basics"
                km.Run(src)
                dst1 = km.dst2
            Case "LUT_Basics"
                lut.Run(src)
                dst1 = lut.dst2
            Case "Reduction_Basics"
                reduction.Run(src)
                dst1 = reduction.dst2
            Case "FeatureLess_Basics"
                fless.Run(src)
                dst1 = fless.dst2
        End Select

        fCell.Run(dst1)

        dst2 = fCell.dst2
        dst3 = fCell.dst3
        labels(2) = fCell.labels(2)
    End Sub
End Class








Public Class RedCloud_InputCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Core
    Public guided As New GuidedBP_Depth
    Public Sub New()
        desc = "Build the reduced pointcloud or doctored back projection input to RedCloud/RedCell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Select Case redOptions.depthInput
            Case "GuidedBP_Depth"
                guided.Run(src)
                Dim maskOfDepth = guided.backProject.Threshold(0, 255, cv.ThresholdTypes.Binary)
                dst2 = guided.dst2
            Case "RedCloud_Core"
                redC.Run(src)
                dst2 = redC.dst2
        End Select
    End Sub
End Class








Public Class RedCloud_InputCombined : Inherits VB_Algorithm
    Dim color As New RedCloud_InputColor
    Dim cloud As New RedCloud_InputCloud
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        'Select Case redOptions.depthInput
        '    Case "GuidedBP_Depth"
        '        guided.Run(src)
        '        Dim maskOfDepth = guided.backProject.Threshold(0, 255, cv.ThresholdTypes.Binary)
        '        dst2 = guided.dst2
        '    Case "RedCloud_Core"
        '        redC.Run(src)
        '        dst2 = redC.dst2
        'End Select
    End Sub
End Class