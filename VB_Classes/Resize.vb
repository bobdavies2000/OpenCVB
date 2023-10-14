Imports cv = OpenCvSharp
Public Class Resize_Basics : Inherits VB_Algorithm
    Public newSize As cv.Size
    Public options As New Options_Resize
    Public Sub New()
        If standalone Then task.drawRect = New cv.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2)
        desc = "Resize with different options and compare them"
        labels(2) = "Rectangle highlight above resized"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        If task.drawRect.Width <> 0 Then
            src = src(task.drawRect)
            newSize = task.drawRect.Size
        End If

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Resize_Smaller : Inherits VB_Algorithm
    Public options As New Options_Resize
    Public newSize As cv.Size
    Public Sub New()
        desc = "Resize by a percentage of the image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        newSize = New cv.Size(Math.Ceiling(src.Width * options.resizePercent), Math.Ceiling(src.Height * options.resizePercent))

        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
        labels(2) = "Image after resizing to: " + CStr(newSize.Width) + "X" + CStr(newSize.Height)
    End Sub
End Class







Public Class Resize_Preserve : Inherits VB_Algorithm
    Public options As New Options_Resize
    Public newSize As cv.Size
    Public Sub New()
        findSlider("Resize Percentage (%)").Maximum = 200
        findSlider("Resize Percentage (%)").Value = 120
        findSlider("Resize Percentage (%)").Minimum = 100
        desc = "Decrease the size but preserve the full image size."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        newSize = New cv.Size(Math.Ceiling(src.Width * options.resizePercent), Math.Ceiling(src.Height * options.resizePercent))

        dst0 = src.Resize(newSize).SetTo(0)

        Dim rect = New cv.Rect(options.topLeftOffset, options.topLeftOffset, dst2.Width, dst2.Height)
        src.CopyTo(dst0(rect))
        dst2 = dst0.Resize(dst2.Size, 0, 0, options.warpFlag)

        labels(2) = "Image after resizing to: " + CStr(newSize.Width) + "X" + CStr(newSize.Height)
    End Sub
End Class
