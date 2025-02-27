Imports cv = OpenCvSharp
Public Class Boundary_Basics : Inherits TaskParent
    Public redCPP As New RedColor_CPP
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.redOptions.ColorSource.SelectedItem = "Bin4Way_Regions"
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask of the RedCloud cell boundaries"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = runRedC(color8U.dst2, labels(2))

        redCPP.Run(dst1)

        dst3.SetTo(0)
        For i = 1 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
            DrawContour(dst3(rc.roi), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.rcList.Count} cells were found."
    End Sub
End Class






Public Class Boundary_Tiers : Inherits TaskParent
    Dim cells As New Boundary_Basics
    Dim contours As New Contour_DepthTiers
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Add the depth tiers to the cell boundaries"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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
        desc = "Build the boundaries for rcList and remove interior rectangles"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        bounds.Run(src)

        dst2.SetTo(0)
        For Each rc In task.rcList
            dst2.Rectangle(rc.roi, task.HighlightColor, task.lineWidth)
        Next
        labels(2) = $"{task.rcList.Count} rectangles before contain test"

        rects.Clear()
        For i = 0 To CInt(task.rcList.Count * options.percentRect) - 1
            rects.Add(task.rcList(i).roi)
        Next

        smallRects.Clear()
        smallContours.Clear()
        For i = task.rcList.Count - 1 To CInt(task.rcList.Count * options.percentRect) Step -1
            task.rc = task.rcList(i)
            Dim r = task.rc.roi
            Dim contained As Boolean = False
            For Each rc In task.rcList
                If r = rc.roi Then Continue For
                If rc.roi.Contains(r) Then
                    contained = True
                    Exit For
                End If
            Next

            If contained Then
                smallContours.Add(task.rc.contour)
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
        If standalone Then task.gOptions.displaydst1.checked = true
        desc = "Build the boundaries for rcList and remove interior rectangles"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        bRects.Run(src)
        dst2 = bRects.bounds.dst2.Clone
        dst3 = bRects.dst2
        dst1 = bRects.dst3
        labels(3) = $"{task.rcList.Count} cells before contain test"

        For i = 0 To bRects.smallRects.Count - 1
            DrawContour(dst2(bRects.smallRects(i)), bRects.smallContours(i), cv.Scalar.Black, task.lineWidth)
        Next
        labels(1) = labels(2)
        labels(2) = $"{task.rcList.Count - bRects.smallRects.Count} cells after contain test"
    End Sub
End Class







'Public Class Boundary_Overlap : Inherits TaskParent
'    Dim bounds As New Boundary_Basics
'    Public Sub New()
'        dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
'        desc = "Determine if 2 contours overlap"
'    End Sub
'    Public Overrides sub RunAlg(src As cv.Mat)
'        bounds.Run(src)
'        dst3 = bounds.dst2
'        Dim overlapping As Boolean
'        For i = 0 To bounds.contours.Count - 1
'            Dim tour = bounds.contours(i)
'            Dim rect = bounds.rects(i)
'            For j = i + 1 To bounds.contours.Count - 1
'                Dim r = bounds.rects(j)
'                If r.IntersectsWith(rect) Then
'                    dst2.SetTo(0)
'                    Dim c1 = tour.Count
'                    Dim c2 = bounds.contours(j).Count
'                    DrawContour(dst2(rect), tour, 127, task.lineWidth)
'                    DrawContour(dst2(r), bounds.contours(j), 255, task.lineWidth)
'                    Dim count = dst2.CountNonZero
'                    If count <> c1 + c2 Then
'                        overlapping = True
'                        Exit For
'                    End If
'                End If
'            Next
'            If overlapping Then Exit For
'        Next
'    End Sub
'End Class





Public Class Boundary_RedCloud : Inherits TaskParent
    Dim rCloud As New RedCloud_PrepData
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.gOptions.MaxDepthBar.Value = 20
        task.redOptions.TrackingColor.Checked = True
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the RedCloud cell contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rCloud.Run(src)
        dst2 = runRedC(rCloud.dst2, labels(2))

        dst3.SetTo(0)
        For i = 1 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
            DrawContour(dst3(rc.roi), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.rcList.Count} cells were found."
    End Sub
End Class





Public Class Boundary_GuidedBP : Inherits TaskParent
    Public redCPP As New RedColor_CPP
    Dim guided As New GuidedBP_Depth
    Public Sub New()
        task.redOptions.IdentifyCountBar.Value = 100
        task.gOptions.HistBinBar.Value = 100
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask of the RedCloud cell boundaries using Guided Backprojection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        guided.Run(src)
        dst2 = runRedC(guided.dst2, labels(2))

        dst3.SetTo(0)
        For i = 1 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
            DrawContour(dst3(rc.roi), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.rcList.Count} cells were found."
    End Sub
End Class