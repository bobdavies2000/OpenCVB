Imports cv = OpenCvSharp
Public Class RedConnect_Basics : Inherits TaskParent
    Dim connect As New Connected_Regions
    Public Sub New()
        desc = "Update the depthGroup property of each redCell and color it appropriately."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        For Each rc In task.rcList
            Dim color = connect.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            dst2(rc.rect).SetTo(color, rc.mask)
        Next
        'dst0 = connect.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        'dst1 = Not dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        '' connect.dst2.CopyTo(dst3)
        'For Each md In connect.redM.mdList
        '    dst3.SetTo(0)
        '    src.CopyTo(dst3, dst1)
        '    src(md.rect).CopyTo(dst3(md.rect), md.mask)

        '    Exit For
        'Next
    End Sub
End Class






Public Class RedConnect_Simple : Inherits TaskParent
    Dim connect As New Connected_Contours
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use the contours as input to RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst3.SetTo(0)
        For Each md In connect.redM.mdList
            md.contour = ContourBuild(md.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(dst3(md.rect), md.contour, 255, task.lineWidth)
        Next
        dst2 = runRedC(dst3, labels(2))
    End Sub
End Class

