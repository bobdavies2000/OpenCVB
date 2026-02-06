Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Magnify_Basics : Inherits TaskParent
        Public Sub New()
            atask.drawRect = New cv.Rect(10, 10, 50, 50)
            desc = "Magnify the drawn rectangle on dst2 and display it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            If atask.drawRect.Width > 0 And atask.drawRect.Height > 0 Then dst3 = src(atask.drawRect)
        End Sub
    End Class







    Public Class NR_Magnify_Example : Inherits TaskParent
        Dim prep As New Neighbor_Intersects
        Dim zoom As New Magnify_Basics
        Public Sub New()
            desc = "Magnify the output of the algorithm above."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2

            zoom.Run(dst2)
            dst3 = zoom.dst3
        End Sub
    End Class
End Namespace