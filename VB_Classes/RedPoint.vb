Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedPoint_CombineColor : Inherits VB_Algorithm
    Public guided As New GuidedBP_Depth
    Public redP As New RedCloudX_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Segment the image on based both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)
        Dim maskOfDepth = guided.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary).CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst1 = task.depthMask.Clone
        dst1.SetTo(0, maskOfDepth)

        reduction.Run(src)
        Dim combined As New cv.Mat
        reduction.dst2.ConvertTo(combined, cv.MatType.CV_32F)
        guided.dst0.CopyTo(combined, maskOfDepth)
        combined.ConvertTo(combined, cv.MatType.CV_8U)
        redP.Run(combined)

        'dst2 = redP.dst2
        'labels(2) = CStr(redP.prepCells.Count) + " cells identified with floodfill"
        'labels(3) = CStr(dst3.InRange(0, 0).CountNonZero) + " pixels unclassified."
    End Sub
End Class









Public Class RedPoint_MergeCells : Inherits VB_Algorithm
    Public inputCells As New List(Of rcPrep)
    Public prepCells As New List(Of rcPrep)
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired number of cells", 2, 254, 100)
        desc = "Merge cells below threshold with their neighbors with a similar color."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        'If standalone Then
        '    Static combine As New RedPoint_CombineColor
        '    combine.Run(src)
        '    inputCells = New List(Of rcPrep)(combine.redP.prepCells)
        'End If

        'prepCells.Clear()
        'For Each rp In inputCells
        '    If task.drawRect <> New cv.Rect Then
        '        If task.drawRect.Contains(rp.floodPoint) = False Then Continue For
        '    End If

        '    dst3(rp.rect).SetTo(prepCells.Count, rp.mask)
        '    prepCells.Add(rp)
        'Next

        'dst2 = vbPalette((dst3 * 255 / prepCells.Count).ToMat)
        'labels(2) = CStr(prepCells.Count) + " cells were found"
    End Sub
End Class







Public Class RedPoint_Color : Inherits VB_Algorithm
    Public merge As New RedPoint_MergeCells
    Public redCells As New List(Of rcData)
    Public rcSelect As New rcData
    Public rcMatch As New RedCloud_Basics
    Dim combine As New RedPoint_CombineColor
    Public Sub New()
        desc = "Segment the image based both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        'combine.Run(src)
        'merge.inputCells = New List(Of rcPrep)(combine.redP.prepCells)

        'merge.Run(src)

        'rcMatch.inputCells.Clear()
        'For Each rp In merge.prepCells
        '    Dim rc As New rcData
        '    rc.rect = rp.rect
        '    rc.mask = rp.mask
        '    rc.pixels = rp.pixels
        '    rc.floodPoint = rp.floodPoint
        '    rcMatch.inputCells.Add(rc)
        'Next

        'rcMatch.Run(src)
        'redCells = rcMatch.redCells
        'dst2 = rcMatch.dst2
        'dst3 = rcMatch.dst3

        'For Each rc In redCells
        '    setTrueText(CStr(rc.index), rc.maxDist)
        'Next
        'rcSelect = rcMatch.redSelect()
        'If heartBeat() Then labels = rcMatch.labels
    End Sub
End Class
