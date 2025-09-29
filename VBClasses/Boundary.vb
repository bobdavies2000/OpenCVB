Imports cv = OpenCvSharp
Public Class Boundary_Basics : Inherits TaskParent
    Public redCPP As New RedColor_CPP
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.gOptions.ColorSource.SelectedItem = "Bin4Way_Regions"
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask of the RedCloud cell boundaries"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = runRedOld(color8U.dst2, labels(2))

        redCPP.Run(dst1)

        dst3.SetTo(0)
        For i = 1 To task.redCold.rcList.Count - 1
            Dim rc = task.redCold.rcList(i)
            DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.redCold.rcList.Count} cells were found."
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
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        bounds.Run(src)

        dst2.SetTo(0)
        For Each rc In task.redCold.rcList
            dst2.Rectangle(rc.rect, task.highlight, task.lineWidth)
        Next
        labels(2) = $"{task.redCold.rcList.Count} rectangles before contain test"

        rects.Clear()
        For i = 0 To CInt(task.redCold.rcList.Count * options.percentRect) - 1
            rects.Add(task.redCold.rcList(i).rect)
        Next

        smallRects.Clear()
        smallContours.Clear()
        For i = task.redCold.rcList.Count - 1 To CInt(task.redCold.rcList.Count * options.percentRect) Step -1
            task.rcD = task.redCold.rcList(i)
            Dim r = task.rcD.rect
            Dim contained As Boolean = False
            For Each rc In task.redCold.rcList
                If r = rc.rect Then Continue For
                If rc.rect.Contains(r) Then
                    contained = True
                    Exit For
                End If
            Next

            If contained Then
                smallContours.Add(task.rcD.contour)
                smallRects.Add(r)
            Else
                rects.Add(r)
            End If
        Next

        dst3.SetTo(0)
        For Each r In rects
            dst3.Rectangle(r, task.highlight, task.lineWidth)
        Next
        labels(3) = $"{rects.Count} rectangles after contain test"
    End Sub
End Class









Public Class Boundary_RemovedRects : Inherits TaskParent
    Public bRects As New Boundary_Rectangles
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build the boundaries for rcList and remove interior rectangles"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bRects.Run(src)
        dst2 = bRects.bounds.dst2.Clone
        dst3 = bRects.dst2
        dst1 = bRects.dst3
        labels(3) = $"{task.redCold.rcList.Count} cells before contain test"

        For i = 0 To bRects.smallRects.Count - 1
            DrawTour(dst2(bRects.smallRects(i)), bRects.smallContours(i), cv.Scalar.Black, task.lineWidth)
        Next
        labels(1) = labels(2)
        labels(2) = $"{task.redCold.rcList.Count - bRects.smallRects.Count} cells after contain test"
    End Sub
End Class






Public Class Boundary_GuidedBP : Inherits TaskParent
    Dim guided As New GuidedBP_Depth
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask of the RedCloud cell boundaries using Guided Backprojection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        guided.Run(src)
        dst2 = runRedOld(guided.dst2, labels(2))

        dst3.SetTo(0)
        For i = 1 To task.redCold.rcList.Count - 1
            Dim rc = task.redCold.rcList(i)
            DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.redCold.rcList.Count} cells were found."
    End Sub
End Class






Public Class Boundary_RedColor : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Public Sub New()
        task.gOptions.MaxDepthBar.Value = 20
        task.gOptions.TrackingColor.Checked = True
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the RedCloud cell contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = runRedOld(prep.dst2, labels(2))

        dst3.SetTo(0)
        For i = 1 To task.redCold.rcList.Count - 1
            Dim rc = task.redCold.rcList(i)
            DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.redCold.rcList.Count} cells were found."
    End Sub
End Class





Public Class Boundary_RedCloud : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Public Sub New()
        task.gOptions.MaxDepthBar.Value = 20
        task.gOptions.TrackingColor.Checked = True
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the RedCloud cell contours"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = runRedC(prep.dst2, labels(2))

        dst3.SetTo(0)
        For i = 1 To task.redC.pcList.Count - 1
            Dim rc = task.redC.pcList(i)
            Dim contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawTour(dst3(rc.rect), contour, 255, task.lineWidth)
        Next

        labels(3) = $"{task.redC.pcList.Count} cells were found."
    End Sub
End Class