Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Boundary_Basics : Inherits TaskParent
        Public redC As New RedColor_Basics
        Dim color8U As New Color8U_Basics
        Public Sub New()
            task.fOptions.Color8USource.SelectedItem = "Bin4Way_Regions"
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Create a mask of the RedCloud cell boundaries"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(src)
            redC.Run(color8U.dst2)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            For Each rc In redC.rcList
                DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
            Next

            labels(3) = $"{redC.rcList.Count} cells were found."
        End Sub
    End Class





    Public Class Boundary_Rectangles : Inherits TaskParent
        Public bounds As New Boundary_Basics
        Public rects As New List(Of cv.Rect)
        Public smallRects As New List(Of cv.Rect)
        Public smallContours As New List(Of List(Of cv.Point))
        Dim options As New Options_BoundaryRect
        Public Sub New()
            desc = "Build the boundaries for oldrclist and remove interior rectangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            bounds.Run(src)

            dst2.SetTo(0)
            For Each rc In bounds.redC.rcList
                dst2.Rectangle(rc.rect, task.highlight, task.lineWidth)
            Next
            labels(2) = $"{bounds.redC.rcList.Count} rectangles before contain test"

            rects.Clear()
            For i = 0 To CInt(bounds.redC.rcList.Count * options.percentRect) - 1
                rects.Add(bounds.redC.rcList(i).rect)
            Next

            smallRects.Clear()
            smallContours.Clear()
            For i = bounds.redC.rcList.Count - 1 To CInt(bounds.redC.rcList.Count * options.percentRect) Step -1
                task.rcD = bounds.redC.rcList(i)
                Dim r = task.rcD.rect
                Dim contained As Boolean = False
                For Each rc In bounds.redC.rcList
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









    Public Class NR_Boundary_RemovedRects : Inherits TaskParent
        Public bRects As New Boundary_Rectangles
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Build the boundaries for oldrclist and remove interior rectangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bRects.Run(src)
            dst2 = bRects.bounds.dst2.Clone
            dst3 = bRects.dst2
            dst1 = bRects.dst3
            labels(3) = $"{bRects.bounds.redC.rcList.Count} cells before contain test"

            For i = 0 To bRects.smallRects.Count - 1
                DrawTour(dst2(bRects.smallRects(i)), bRects.smallContours(i), cv.Scalar.Black, task.lineWidth)
            Next
            labels(1) = labels(2)
            labels(2) = $"{bRects.bounds.redC.rcList.Count - bRects.smallRects.Count} cells after contain test"
        End Sub
    End Class






    Public Class NR_Boundary_GuidedBP : Inherits TaskParent
        Dim guided As New GuidedBP_Depth
        Dim redC As New RedColor_Basics
        Public Sub New()
            task.gOptions.setHistogramBins(100)
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Create a mask of the RedCloud cell boundaries using Guided Backprojection"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            guided.Run(src)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            For Each rc In redC.rcList
                DrawTour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
            Next

            labels(3) = $"{redc.rclist.Count} cells were found."
        End Sub
    End Class






    Public Class Boundary_RedColor : Inherits TaskParent
        Dim prep As New RedPrep_Basics
        Public Sub New()
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find the RedCloud cell contours"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2
            labels(2) = prep.labels(2)
        End Sub
    End Class
End Namespace