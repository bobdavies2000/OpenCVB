Imports cv = OpenCvSharp
Public Class Reliable_Depth : Inherits VB_Parent
    Dim history8u As New History_Basics8U
    Dim diff As New Diff_Depth32f
    Public Sub New()
        task.reliableDepthMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Provide only depth that is reliable"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone And task.gOptions.UseReliableDepth.Checked Then
            dst2.SetTo(0)
            task.depthRGB.CopyTo(dst2, task.reliableDepthMask)
        Else
            diff.Run(task.pcSplit(2))
            diff.dst2.ConvertTo(dst3, cv.MatType.CV_8U)
            history8u.Run(dst3)
            dst2.SetTo(0)
            If task.FirstPass = False Then
                task.pointCloud.CopyTo(dst2, history8u.dst2)
                task.reliableDepthMask = history8u.dst2.Clone
            End If
        End If
    End Sub
End Class





Public Class Reliable_DepthNot : Inherits VB_Parent
    Dim history8u As New History_Basics8U
    Dim motionD As New Motion_Depth
    Public Sub New()
        desc = "Display an accumulation the noDepthMask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motionD.Run(src)

        history8u.Run(motionD.dst3)
        dst2 = history8u.dst2
        dst2.SetTo(255, task.noDepthMask)
    End Sub
End Class