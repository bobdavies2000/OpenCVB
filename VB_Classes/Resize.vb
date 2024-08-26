Imports cvb = OpenCvSharp
Public Class Resize_Basics : Inherits VB_Parent
    Public newSize As cvb.Size
    Public options As New Options_Resize
    Public Sub New()
        If standaloneTest() Then task.drawRect = New cvb.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2)
        desc = "Resize with different options and compare them"
        labels(2) = "Rectangle highlight above resized"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        If task.drawRect.Width <> 0 Then
            src = src(task.drawRect)
            newSize = task.drawRect.Size
        End If

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Resize_Smaller : Inherits VB_Parent
    Public options As New Options_Resize
    Public newSize As cvb.Size
    Public Sub New()
        desc = "Resize by a percentage of the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        newSize = New cvb.Size(Math.Ceiling(src.Width * options.resizePercent), Math.Ceiling(src.Height * options.resizePercent))

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
        labels(2) = "Image after resizing to: " + CStr(newSize.Width) + "X" + CStr(newSize.Height)
    End Sub
End Class







Public Class Resize_Preserve : Inherits VB_Parent
    Public options As New Options_Resize
    Public newSize As cvb.Size
    Public Sub New()
        FindSlider("Resize Percentage (%)").Maximum = 200
        FindSlider("Resize Percentage (%)").Value = 120
        FindSlider("Resize Percentage (%)").Minimum = 100
        desc = "Decrease the size but preserve the full image size."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        newSize = New cvb.Size(Math.Ceiling(src.Width * options.resizePercent), Math.Ceiling(src.Height * options.resizePercent))

        dst0 = src.Resize(newSize, cvb.InterpolationFlags.Nearest).SetTo(0)

        Dim rect = New cvb.Rect(options.topLeftOffset, options.topLeftOffset, dst2.Width, dst2.Height)
        src.CopyTo(dst0(rect))
        dst2 = dst0.Resize(dst2.Size(), 0, 0, options.warpFlag)

        labels(2) = "Image after resizing to: " + CStr(newSize.Width) + "X" + CStr(newSize.Height)
    End Sub
End Class









Public Class Resize_Proportional : Inherits VB_Parent
    Dim options As New Options_Spectrum
    Public Sub New()
        desc = "Resize the input but keep the results proportional to the original."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            options.RunOpt()
            dst2 = options.runRedCloud(labels(2))
            src = src(task.rc.rect)
        End If

        Dim newSize As cvb.Size
        If dst0.Width / dst0.Height < src.Width / src.Height Then
            newSize = New cvb.Size(dst2.Width, dst2.Height * dst0.Height / dst0.Width)
        Else
            newSize = New cvb.Size(dst2.Width * dst0.Height / dst0.Width, dst2.Height)
        End If
        src = src.Resize(newSize, cvb.InterpolationFlags.Nearest)
        Dim newRect = New cvb.Rect(0, 0, newSize.Width, newSize.Height)
        dst3.SetTo(0)
        src.CopyTo(dst3(newRect))
    End Sub
End Class
