Imports SharpGL.SceneGraph.Lighting
Imports cv = OpenCvSharp
Public Class GL_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Display the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(oCase.drawPointCloudRGB)
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class GL_Bricks : Inherits TaskParent
    Public Sub New()
        desc = "Display the bricks in SharpGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(oCase.quadBasics)
        SetTrueText(strOut, 2)
    End Sub
End Class








Public Class GL_ReadPointCloud : Inherits TaskParent
    Public Sub New()
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mm = GetMinMax(task.pcSplit(2))

        strOut = task.sharpGL.RunSharp(oCase.readPointCloud)
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

        strOut = task.sharpGL.RunSharp(oCase.pcLines, dst0, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class GL_Lines : Inherits TaskParent
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

        If standalone Then
            dst1.SetTo(white)
            strOut = task.sharpGL.RunSharp(oCase.pcLines, dst0, dst1)
            SetTrueText(strOut, 2)
        End If
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

        strOut = task.sharpGL.RunSharp(oCase.pcLines, line3D.pointcloud, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class




Public Class GL_Lines3D : Inherits TaskParent
    Dim line3D As New Line3D_ReconstructLines
    Public Sub New()
        desc = "Visualize all the reconstructed 3D lines found in the RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        line3D.Run(src)
        dst2 = line3D.dst2
        labels(2) = line3D.labels(2)

        dst1.SetTo(white)

        strOut = task.sharpGL.RunSharp(oCase.pcLines, line3D.pointcloud, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class
