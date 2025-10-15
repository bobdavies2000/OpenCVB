Imports cv = OpenCvSharp
Public Class Resize_Basics : Inherits TaskParent
    Public newSize As cv.Size
    Public options As New Options_Resize
    Public Sub New()
        If standalone Then task.drawRect = New cv.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2)
        desc = "Resize with different options and compare them"
        labels(2) = "Rectangle highlight above resized"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        If task.drawRect.Width <> 0 Then
            src = src(task.drawRect)
            newSize = task.drawRect.Size
        End If

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Resize_Smaller : Inherits TaskParent
    Public options As New Options_Resize
    Public newSize As cv.Size
    Dim optGrid As New Options_GridFromResize
    Public Sub New()
        desc = "Resize by a percentage of the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        optGrid.Run()

        newSize = New cv.Size(Math.Ceiling(src.Width * optGrid.lowResPercent),
                               Math.Ceiling(src.Height * optGrid.lowResPercent))

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
        labels(2) = "Image after resizing to: " + CStr(newSize.Width) + "X" + CStr(newSize.Height)
    End Sub
End Class






Public Class Resize_Proportional : Inherits TaskParent
    Dim options As New Options_Spectrum
    Public Sub New()
        desc = "Resize the input but keep the results proportional to the original."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            options.Run()
            dst2 = runRedList(src, labels(2))
            src = src(task.oldrcD.rect)
        End If

        Dim newSize As cv.Size
        If dst0.Width / dst0.Height < src.Width / src.Height Then
            newSize = New cv.Size(dst2.Width, dst2.Height * dst0.Height / dst0.Width)
        Else
            newSize = New cv.Size(dst2.Width * dst0.Height / dst0.Width, dst2.Height)
        End If
        src = src.Resize(newSize, cv.InterpolationFlags.Nearest)
        Dim newRect = New cv.Rect(0, 0, newSize.Width, newSize.Height)
        dst3.SetTo(0)
        src.CopyTo(dst3(newRect))
    End Sub
End Class
