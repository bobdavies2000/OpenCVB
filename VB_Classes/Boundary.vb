Imports cv = OpenCvSharp
Public Class Boundary_Basics : Inherits VB_Algorithm
    Public colorC As New Color_Basics
    Dim redCPP As New RedCloud_MaskNone_CPP
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public contours As New List(Of List(Of cv.Point))
    Public Sub New()
        redOptions.ColorSource.SelectedItem() = "Binarize_Split4"
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask of the RedCloud cell boundaries"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst3 = colorC.dst3
        redCPP.Run(colorC.dst2)

        dst2.SetTo(0)
        rects.Clear()
        masks.Clear()
        contours.Clear()
        For i = 1 To redCPP.classCount - 1
            Dim rect = redCPP.rectData.Get(Of cv.Rect)(i - 1, 0)
            Dim mask = redCPP.dst2(rect).InRange(i, i)
            Dim contour = contourBuild(mask, cv.ContourApproximationModes.ApproxNone)
            vbDrawContour(dst2(rect), contour, 255, task.lineWidth)
            rects.Add(rect)
            masks.Add(mask)
            contours.Add(contour)
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








Public Class Boundary_Rectangles : Inherits VB_Algorithm
    Public bounds As New Boundary_Basics
    Public rects As New List(Of cv.Rect)
    Public smallRects As New List(Of cv.Rect)
    Public smallContours As New List(Of List(Of cv.Point))
    Public Sub New()
        labels = {"", "Rectangles before contain test", "", ""}
        desc = "Build the boundaries for redCells and remove interior rectangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bounds.Run(src)

        dst2.SetTo(0)
        For Each r In bounds.rects
            dst2.Rectangle(r, task.highlightColor, task.lineWidth)
        Next
        labels(2) = $"{bounds.rects.Count} rectangles before contain test"

        rects.Clear()
        smallRects.Clear()
        smallContours.Clear()
        For i = 0 To bounds.rects.Count / 4 - 1
            rects.Add(bounds.rects(i))
        Next
        For i = bounds.rects.Count - 1 To bounds.rects.Count / 4 Step -1
            Dim r = bounds.rects(i)
            Dim contained As Boolean = False
            For Each rect In bounds.rects
                If r = rect Then Continue For
                If rect.Contains(r) Then
                    contained = True
                    Exit For
                End If
            Next

            If contained Then
                smallContours.Add(bounds.contours(i))
                smallRects.Add(r)
            Else
                rects.Add(r)
            End If
        Next

        dst3.SetTo(0)
        For Each r In rects
            dst3.Rectangle(r, task.highlightColor, task.lineWidth)
        Next
        labels(3) = $"{rects.Count} rectangles after contain test"
    End Sub
End Class








Public Class Boundary_RemovedRects : Inherits VB_Algorithm
    Public bRects As New Boundary_Rectangles
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Rectangles before contain test", "", ""}
        desc = "Build the boundaries for redCells and remove interior rectangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bRects.Run(src)
        dst2 = bRects.bounds.dst2.Clone
        dst3 = bRects.dst2
        dst1 = bRects.dst3
        labels(3) = $"{bRects.bounds.rects.Count} cells before contain test"

        For i = 0 To bRects.smallRects.Count - 1
            vbDrawContour(dst2(bRects.smallRects(i)), bRects.smallContours(i), cv.Scalar.Black, task.lineWidth)
        Next
        labels(2) = $"{bRects.bounds.rects.Count - bRects.smallRects.Count} cells after contain test"
    End Sub
End Class

