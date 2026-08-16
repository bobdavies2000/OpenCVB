Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc
Public Class Thinning_Basics : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim options As New Options_Thinning
    Public Sub New()
        desc = "Thin each RedC cell mask and store the result on that cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim thin As New cv.Mat
        For Each rc In redC.rcList
            XImgProc.Thinning(rc.mask, thin, options.thinningType)
            If thin.Empty = False AndAlso thin.Size = rc.mask.Size Then
                dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), thin)
            End If
        Next
        labels(3) = CStr(redC.rcList.Count) + " cells thinned with " + options.thinningType.ToString()
    End Sub
End Class
