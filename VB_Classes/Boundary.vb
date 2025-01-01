Imports cv = OpenCvSharp
Public Class Boundary_Basics : Inherits TaskParent
    Public redCPP As New RedCloud_CPP_VB
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public contours As New List(Of List(Of cv.Point))
    Public runRedCPP As Boolean = True
    Dim color8U As New Color8U_Basics
    Dim prep As New RedCloud_Reduce
    Dim guided As New GuidedBP_Depth
    Public Sub New()
        task.redOptions.setColorSource("Bin4Way_Regions")
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask of the RedCloud cell boundaries"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If src.Channels() <> 1 Then
            If task.redOptions.UseColorOnly.Checked Then
                color8U.Run(src)
                dst1 = color8U.dst2
            ElseIf task.redOptions.UseDepth.Checked Then
                prep.Run(src)
                dst1 = prep.dst2
            Else
                guided.Run(src)
                dst1 = guided.dst2
            End If
        End If

        If runRedCPP Then
            redCPP.Run(dst1)

            dst2.SetTo(0)
            rects.Clear()
            masks.Clear()
            contours.Clear()
            For i = 1 To redCPP.classCount - 1
                Dim rect = redCPP.rectList(i - 1)
                Dim mask = redCPP.dst2(rect).InRange(i, i)
                Dim contour = ContourBuild(mask, cv.ContourApproximationModes.ApproxNone)
                DrawContour(dst2(rect), contour, 255, task.lineWidth)
                rects.Add(rect)
                masks.Add(mask)
                contours.Add(contour)
            Next

            labels(2) = $"{redCPP.classCount} cells were found."
        End If
    End Sub
End Class






Public Class Boundary_Tiers : Inherits TaskParent
    Dim cells As New Boundary_Basics
    Dim contours As New Contour_DepthTiers
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Add the depth tiers to the cell boundaries"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        cells.Run(src)
        dst3 = cells.dst2

        contours.Run(src)
        dst2.SetTo(0)
        For Each tour In contours.contourlist
            DrawContour(dst2, tour.ToList, 255, 2)
        Next
        labels(2) = $"{contours.contourlist.Count} depth tiers were found."
        labels(3) = cells.labels(2)
    End Sub
End Class








Public Class Boundary_Rectangles : Inherits TaskParent
    Public bounds As New Boundary_Basics
    Public rects As New List(Of cv.Rect)
    Public smallRects As New List(Of cv.Rect)
    Public smallContours As New List(Of List(Of cv.Point))
    Dim options As New Options_BoundaryRect
    Public Sub New()
        desc = "Build the boundaries for redCells and remove interior rectangles"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        bounds.Run(src)

        dst2.SetTo(0)
        For Each r In bounds.rects
            dst2.Rectangle(r, task.HighlightColor, task.lineWidth)
        Next
        labels(2) = $"{bounds.rects.Count} rectangles before contain test"

        rects.Clear()
        smallRects.Clear()
        smallContours.Clear()
        For i = 0 To bounds.rects.Count * options.percentRect - 1
            rects.Add(bounds.rects(i))
        Next
        For i = bounds.rects.Count - 1 To CInt(bounds.rects.Count * options.percentRect) Step -1
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
            dst3.Rectangle(r, task.HighlightColor, task.lineWidth)
        Next
        labels(3) = $"{rects.Count} rectangles after contain test"
    End Sub
End Class









Public Class Boundary_RemovedRects : Inherits TaskParent
    Public bRects As New Boundary_Rectangles
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Build the boundaries for redCells and remove interior rectangles"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        bRects.Run(src)
        dst2 = bRects.bounds.dst2.Clone
        dst3 = bRects.dst2
        dst1 = bRects.dst3
        labels(3) = $"{bRects.bounds.rects.Count} cells before contain test"

        For i = 0 To bRects.smallRects.Count - 1
            DrawContour(dst2(bRects.smallRects(i)), bRects.smallContours(i), cv.Scalar.Black, task.lineWidth)
        Next
        labels(1) = labels(2)
        labels(2) = $"{bRects.bounds.rects.Count - bRects.smallRects.Count} cells after contain test"
    End Sub
End Class







Public Class Boundary_Overlap : Inherits TaskParent
    Dim bounds As New Boundary_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Determine if 2 contours overlap"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        bounds.Run(src)
        dst3 = bounds.dst2
        Dim overlapping As Boolean
        For i = 0 To bounds.contours.Count - 1
            Dim tour = bounds.contours(i)
            Dim rect = bounds.rects(i)
            For j = i + 1 To bounds.contours.Count - 1
                Dim r = bounds.rects(j)
                If r.IntersectsWith(rect) Then
                    dst2.SetTo(0)
                    Dim c1 = tour.Count
                    Dim c2 = bounds.contours(j).Count
                    DrawContour(dst2(rect), tour, 127, task.lineWidth)
                    DrawContour(dst2(r), bounds.contours(j), 255, task.lineWidth)
                    Dim count = dst2.CountNonZero
                    If count <> c1 + c2 Then
                        overlapping = True
                        Exit For
                    End If
                End If
            Next
            If overlapping Then Exit For
        Next
    End Sub
End Class
