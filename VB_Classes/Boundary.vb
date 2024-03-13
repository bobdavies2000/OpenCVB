Imports cv = OpenCvSharp
Public Class Boundary_Basics : Inherits VB_Algorithm
    Dim binar4 As New Binarize_Split4
    Dim flood As New Flood_Basics
    Dim redCPP As New RedCloud_MaskNone
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask of the RedCloud cell boundaries"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binar4.Run(src)
        dst3 = binar4.dst3
        redCPP.Run(binar4.dst2)

        dst2.SetTo(0)
        For i = 1 To redCPP.classCount - 1
            Dim rect = redCPP.rectData.Get(Of cv.Rect)(i - 1, 0)
            Dim mask = redCPP.dst2(rect).InRange(i, i)
            Dim contour = contourBuild(mask, cv.ContourApproximationModes.ApproxNone)
            vbDrawContour(dst2(rect), contour, 255, task.lineWidth)
        Next

        Dim maxDepthContour = contourBuild(task.maxDepthMask, cv.ContourApproximationModes.ApproxNone)
        vbDrawContour(task.maxDepthMask, maxDepthContour, 255, -1)
        dst2.SetTo(0, task.maxDepthMask)
        vbDrawContour(dst2, maxDepthContour, 255, task.lineWidth)

        labels(2) = $"{redCPP.classCount} cells were found."
    End Sub
End Class






Public Class Boundary_Tiers : Inherits VB_Algorithm
    Dim cells As New Boundary_Basics
    Dim contours As New Contour_DepthTiers
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Add the depth tiers to the cell boundaries"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cells.Run(src)
        dst3 = cells.dst2

        contours.Run(src)
        dst2.SetTo(0)
        For Each tour In contours.contourlist
            vbDrawContour(dst2, tour.ToList, 255, 2)
        Next
        labels(2) = $"{contours.contourlist.Count} depth tiers were found."
        labels(3) = cells.labels(2)
    End Sub
End Class
