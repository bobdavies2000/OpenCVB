Imports cv = OpenCvSharp
Public Class Object_Basics : Inherits VBparent
    Dim ccomp As CComp_ColorDepth
    Public Sub New()

        ccomp = New CComp_ColorDepth()

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        task.desc = "Identify objects in the foreground."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If standalone or task.intermediateReview = caller Then
            dst1 = task.depthmask
            dst2 = task.noDepthMask
        End If

        src.SetTo(0)
        src.SetTo(0, task.noDepthMask)
        ccomp.Run(src)
        dst1 = ccomp.dst1
    End Sub
End Class


