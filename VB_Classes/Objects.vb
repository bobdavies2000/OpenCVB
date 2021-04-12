Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits VBparent
    Dim ccomp As CComp_ColorDepth
    Public Sub New()
        initParent()

        ccomp = New CComp_ColorDepth()

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        task.desc = "Identify objects in the foreground."
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            dst1 = task.depthmask
            dst2 = task.noDepthMask
        End If

        ccomp.src.SetTo(0)
        src.CopyTo(ccomp.src, task.depthmask)
        ccomp.Run()
        dst1 = ccomp.dst1
    End Sub
End Class


