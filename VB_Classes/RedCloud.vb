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
                Dim maskOfDepth = guided.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
                dst2 = guided.dst2
            Case "RedCloud_Core"
                redC.Run(src)
                dst2 = redC.dst2
            Case "N"
                redC.Run(src)
                dst2 = redC.dst2
        End Select
    End Sub
End Class








Public Class RedCloud_InputCombined : Inherits VB_Algorithm
    Dim color As New Color_Basics
    Dim cloud As New RedCloud_InputCloud
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If redOptions.colorInput = "No Color Input" Then
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Else
            color.Run(src)
            dst2 = color.dst2
        End If

        If redOptions.depthInput <> "No Pointcloud Data" Then
            cloud.Run(src)
            cloud.dst2.CopyTo(dst2, task.depthMask)
        End If
    End Sub
End Class








Public Class RedBP_CombineColor : Inherits VB_Algorithm
    Public guided As New GuidedBP_Depth
    Public redP As New RedBP_Flood_CPP
    Public prepCells As New List(Of rcPrep)
    Dim color As New Color_Basics
    Public colorOnly As Boolean = False
    Public depthOnly As Boolean = False
    Public Sub New()
        desc = "Segment the image on based both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim combined As New cv.Mat
        If colorOnly Then
            color.Run(src)
            combined = color.dst2
        Else
            guided.Run(src)
            Dim maskOfDepth = guided.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

            If depthOnly = False Then color.Run(src)

            guided.dst2.CopyTo(color.dst2, maskOfDepth)
        End If

        redP.Run(color.dst2)
        dst2 = redP.dst2

        prepCells.Clear()
        For Each key In redP.prepCells
            Dim rp = key.Value
            If task.drawRect <> New cv.Rect Then
                If task.drawRect.Contains(rp.floodPoint) = False Then Continue For
            End If

            prepCells.Add(rp)
        Next
    End Sub
End Class