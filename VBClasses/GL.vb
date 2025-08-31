Imports cv = OpenCvSharp
Public Class GL_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Display the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.drawPointCloudRGB)
        SetTrueText(strOut, 2)
    End Sub
End Class





Public Class GL_BasicsMain : Inherits TaskParent
    Public Sub New()
        desc = "Display the pointcloud in the main form"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.results.GLRequest = Comm.oCase.drawPointCloudRGB
    End Sub
End Class




Public Class GL_LinesNoMotionInput : Inherits TaskParent
    Public Sub New()
        task.FeatureSampleSize = 1000 ' want all the lines 
        desc = "Build a 3D model of the lines found in the rgb data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone
        dst2 = task.lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = task.lines.labels(2)

        dst0 = src
        dst0.SetTo(0, Not dst2)

        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, dst0)
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class GL_Bricks : Inherits TaskParent
    Public Sub New()
        desc = "Display the bricks in SharpGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.quadBasics)
        SetTrueText(strOut, 2)
    End Sub
End Class








Public Class GL_ReadPointCloud : Inherits TaskParent
    Public Sub New()
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mm = GetMinMax(task.pcSplit(2))

        strOut = task.sharpGL.RunSharp(Comm.oCase.readPointCloud)
        SetTrueText(strOut, 2)

        Dim count As Integer
        'labels(2) = Format(mm.minVal, fmt1) + "m (min) to " + Format(mm.maxVal, fmt1) + "m (max)"

        dst2 = task.sharpDepth.Resize(task.workRes, cv.MatType.CV_32F, cv.InterpolationFlags.Nearest)
        dst2 *= task.gOptions.MaxDepthBar.Value

        dst3 = task.pcSplit(2)
        labels(3) = CStr(count)
    End Sub
End Class





Public Class GL_StructuredLines : Inherits TaskParent
    Dim sMask = New Structured_Mask
    Public Sub New()
        desc = "Build a 3D model of the lines found in the structured depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sMask.run(src)
        dst2 = sMask.dst2
        labels(2) = sMask.labels(2)

        dst0 = task.pointCloud.Clone
        dst0.SetTo(0, Not dst2)
        dst1.SetTo(white)

        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, dst0, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class GL_Lines : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        task.FeatureSampleSize = 1000 ' want all the lines 
        desc = "Build a 3D model of the lines using the task.lines.lplist."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pointcloud = src
        If pointcloud.Type <> cv.MatType.CV_32FC3 Then pointcloud = task.pointCloud.Clone

        Static count As Integer
        If task.heartBeatLT Then
            dst2.SetTo(0)
            dst3.SetTo(0)
            count = 0
        End If

        Dim mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For Each lp In task.lines.lpList
            If lp.age = 1 Or task.heartBeatLT Then
                mask(lp.rect).SetTo(0)
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
                pointcloud(lp.rect).CopyTo(dst3(lp.rect), dst2(lp.rect))
                count += dst2(lp.rect).CountNonZero
            End If
        Next

        labels(2) = task.lines.labels(2)
        labels(3) = CStr(count) + " pixels from the point cloud were moved to the GL input. "

        dst1.SetTo(white)
        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, dst3, dst1)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class GL_LinesReconstructed : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        task.FeatureSampleSize = 1000 ' want all the lines 
        desc = "Rework the point cloud data for lines to be linear in depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pointcloud = src
        If pointcloud.Type <> cv.MatType.CV_32FC3 Then pointcloud = task.pointCloud.Clone

        Static count As Integer
        If task.heartBeat Then
            dst2.SetTo(0)
            dst3.SetTo(0)
            count = 0
        End If

        Dim mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For Each lp In task.lines.lpList
            If lp.age = 1 Or task.heartBeat Then
                mask(lp.rect).SetTo(0)
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
                pointcloud(lp.rect).CopyTo(dst3(lp.rect), dst2(lp.rect))
                count += dst2(lp.rect).CountNonZero
            End If
        Next

        labels(2) = task.lines.labels(2)
        labels(3) = CStr(count) + " pixels from the point cloud were moved to the GL input. "

        dst1.SetTo(white)
        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, dst3, dst1)
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class GL_Line3D : Inherits TaskParent
    Dim line3D As New Line3D_ReconstructLine
    Public Sub New()
        desc = "Visualize with OpenGL the reconstructed 3D line behind the RGB line selected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        line3D.Run(src)
        dst2 = line3D.dst2
        labels(2) = line3D.labels(2)

        dst1.SetTo(white)

        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, line3D.pointcloud, dst1)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class GL_Line3Dall : Inherits TaskParent
    Dim line3D As New Line3D_ReconstructLines
    Public Sub New()
        desc = "Visualize all the reconstructed 3D lines found in the RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        line3D.Run(src)
        dst2 = line3D.dst2
        labels(2) = line3D.labels(2)

        dst1.SetTo(white)

        strOut = task.sharpGL.RunSharp(Comm.oCase.pcLines, line3D.pointcloud, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class




Public Class GL_Draw3DLines : Inherits TaskParent
    Public Sub New()
        task.featureOptions.FeatureSampleSize.Value = task.featureOptions.FeatureSampleSize.Maximum
        desc = "Draw the RGB lines in SharpGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        strOut = task.sharpGL.RunSharp(Comm.oCase.draw3DLines)
        SetTrueText(strOut, 3)
    End Sub
End Class



Public Class GL_Draw3DLinesAndCloud : Inherits TaskParent
    Dim line3D As New Line3D_ReconstructLines
    Public Sub New()
        task.featureOptions.FeatureSampleSize.Value = task.featureOptions.FeatureSampleSize.Maximum
        desc = "Draw the RGB lines in SharpGL and include the line points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone
        dst2 = task.lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = task.lines.labels(2)

        dst0 = src
        dst0.SetTo(0, Not dst2)

        strOut = task.sharpGL.RunSharp(Comm.oCase.draw3DLinesAndCloud, dst0)
        SetTrueText(strOut, 3)

        dst2 = task.lines.dst2
    End Sub
End Class
