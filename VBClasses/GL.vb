Imports SharpGL.SceneGraph.Lighting
Imports cv = OpenCvSharp
Public Class GL_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Display the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText(task.sharpGLShow(oCase.drawPointCloudRGB), 2)
    End Sub
End Class






Public Class GL_Bricks : Inherits TaskParent
    Public Sub New()
        desc = "Display the bricks in SharpGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText(task.sharpGLShow(oCase.quadBasics), 2)
    End Sub
End Class








Public Class GL_ReadPointCloud : Inherits TaskParent
    Public Sub New()
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mm = GetMinMax(task.pcSplit(2))
        SetTrueText(task.sharpGLShow(oCase.readPointCloud), 2)
        Dim count As Integer
        'labels(2) = Format(mm.minVal, fmt1) + "m (min) to " + Format(mm.maxVal, fmt1) + "m (max)"

        dst2 = task.sharpDepth.Resize(task.workRes, cv.MatType.CV_32F, cv.InterpolationFlags.Nearest)
        dst2 *= task.gOptions.MaxDepthBar.Value

        dst3 = task.pcSplit(2)
        labels(3) = CStr(count)
    End Sub
End Class
