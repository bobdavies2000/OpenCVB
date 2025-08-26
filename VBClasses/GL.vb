Imports System.Runtime.InteropServices
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
            SetTrueText(strOut, 3)
        End If
    End Sub
End Class





Public Class GL_Lines1 : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.FeatureSampleSize = 1000 ' want all the lines 
        desc = "Build a 3D model of the lines using the task.lines.lplist."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone

        dst2.SetTo(0)
        Dim lp = task.lineLongest
        If task.toggleOn Then
            Dim depthInit = task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X)
            Dim depthFinal = task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p1.X)
            Dim incr = depthInit - depthFinal

            Dim tmp As New cv.Mat
            dst2.Line(lp.p1, lp.p2, 128, task.lineWidth * 3)
            cv.Cv2.FindNonZero(dst2(lp.rect), tmp)

            Dim points(tmp.Rows * 2 - 1) As Integer
            Marshal.Copy(tmp.Data, points, 0, points.Length)
            For i = 0 To tmp.Rows - 1 Step 2
                Dim pt = New cv.Point(points(i + 1), points(i))
                Dim vec = getWorldCoordinates(pt, depthInit + incr * i)
                src.Set(Of cv.Vec3f)(pt.Y, pt.X, vec)
            Next
        End If
        For Each lp In task.lines.lpList
            If lp.p1 = task.lineLongest.p1 Then Continue For
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
        Next

        labels(2) = task.lines.labels(2)

        dst0 = src
        dst0.SetTo(0, Not dst2)

        If standalone Then
            dst1.SetTo(white)
            strOut = task.sharpGL.RunSharp(oCase.pcLines, dst0, dst1)
            SetTrueText(strOut, 3)
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

        strOut = task.sharpGL.RunSharp(oCase.pcLines, line3D.pointcloud, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class
