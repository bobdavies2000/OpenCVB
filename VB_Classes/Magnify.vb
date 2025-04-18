﻿Imports cv = OpenCvSharp
Public Class Magnify_Basics : Inherits TaskParent
    Public Sub New()
        task.drawRect = New cv.Rect(10, 10, 50, 50)
        desc = "Magnify the drawn rectangle on dst2 and display it."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = src
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            task.drawRect = ValidateRect(task.drawRect)
            dst3 = dst2(task.drawRect).Resize(dst3.Size(), 0, 0, cv.InterpolationFlags.Nearest)
        End If
    End Sub
End Class







Public Class Magnify_Example : Inherits TaskParent
    Dim prep As New Neighbor_Intersects
    Dim zoom As New Magnify_Basics
    Public Sub New()
        desc = "Magnify the output of the algorithm above."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2

        zoom.Run(dst2)
        dst3 = zoom.dst3
    End Sub
End Class
