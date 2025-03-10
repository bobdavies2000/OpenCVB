Imports cv = OpenCvSharp
Public Class RedConnect_Basics : Inherits TaskParent
    Dim connect As New Connected_Regions
    Public Sub New()
        desc = "Find the connected regions and run each through RedColor_Basics, one at a time."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        'dst0 = connect.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        'dst1 = Not dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        '' connect.dst2.CopyTo(dst3)
        'For Each md In connect.redM.mdList
        '    dst3.SetTo(0)
        '    src.CopyTo(dst3, dst1)
        '    src(md.rect).CopyTo(dst3(md.rect), md.mask)
        '    ' dst2 = runRedC(dst3, labels(3))

        '    Exit For
        'Next
    End Sub
End Class
