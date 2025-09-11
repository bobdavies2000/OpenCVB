Imports SharpGL.SceneGraph.Lighting
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





Public Class GL_MainForm : Inherits TaskParent
    Public Sub New()
        desc = "Display the pointcloud in the main form - too much work..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.results.GLRequest = Comm.oCase.drawPointCloudRGB
        SetTrueText("Why not run all SharpGL algorithms here?" + vbCrLf +
                    "Because too much data has to move from task to main.")
    End Sub
End Class




Public Class GL_Line3DNoMotionInput : Inherits TaskParent
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

        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, dst0)
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

        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, dst0, dst1)
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class GL_RunSharp : Inherits TaskParent
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        desc = "Create a SharpGL view that uses the point cloud coordinates."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
        SetTrueText(strOut, 2)

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class






Public Class GL_RunSharpHist : Inherits TaskParent
    Dim plotHist As New GL_PlotHist
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
            SetTrueText(strOut, 2)
        End If

        plotHist.Run(task.sharpDepth.Resize(task.workRes, cv.MatType.CV_32F, cv.InterpolationFlags.Nearest))
        dst3 = plotHist.dst3
        labels(2) = plotHist.labels(2)

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class






Public Class GL_Line3DWhite : Inherits TaskParent
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
        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, dst3, dst1)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class GL_Line3DReconstructed : Inherits TaskParent
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
        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, dst3, dst1)
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

        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, line3D.pointcloud, dst1)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class GL_Line3DAll : Inherits TaskParent
    Public Sub New()
        desc = "Visualize all the reconstructed 3D lines found in the RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)
        For Each lp In task.lines.lpList
            DrawLine(dst2, lp, task.scalarColors(lp.index + 1))
        Next

        strOut = task.sharpGL.RunSharp(Comm.oCase.line3D, task.pointCloud, dst2)
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






Public Class GL_ReadPC : Inherits TaskParent
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
        SetTrueText(strOut, 2)

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class






Public Class GL_PlotHist : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        task.gOptions.MaxDepthBar.Value = 10
        task.gOptions.HistBinBar.Value = 10
        plotHist.minRange = 0.0
        plotHist.maxRange = 1.0
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True
        desc = "Read the pointcloud back from SharpGL and plot a histogram of the result."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
            SetTrueText(strOut, 2)
        End If

        Dim pcMask = task.sharpDepth.InRange(0.01F, 0.99F)
        task.sharpDepth.SetTo(0, Not pcMask)
        plotHist.Run(task.sharpDepth)
        dst3 = plotHist.dst2

        Dim histList = plotHist.histArray.ToList
        Dim maxBin = histList.IndexOf(histList.Max)
        SetTrueText("Max bin at " + CStr(maxBin) + " meters", New cv.Point(dst2.Width / 2, 10), 3)
        labels(3) = "Distances range from 0 to " + CStr(task.MaxZmeters) + " meters with 1m per bin (by default)"

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class




Public Class GL_ReadPCHist : Inherits TaskParent
    Dim glPlot As New GL_PlotHist
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        labels(3) = "The values returned by ReadPointCloud range from 0 to 1 and should have the same profile as Plot_Histogram."
        desc = "Read the point cloud from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
        labels(2) = strOut

        glPlot.Run(task.sharpDepth)
        dst3 = glPlot.dst3
        labels(2) += glPlot.labels(2)

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class





Public Class GL_DisplayPC : Inherits TaskParent
    Public Shared ppx = task.calibData.rgbIntrinsics.ppx
    Public Shared ppy = task.calibData.rgbIntrinsics.ppy
    Public Shared fx = task.calibData.rgbIntrinsics.fx
    Public Shared fy = task.calibData.rgbIntrinsics.fy
    Public Shared msg As String
    Shared mm As mmData
    Public Sub New()
        task.sharpDepth = New cv.Mat(task.workRes, cv.MatType.CV_32F, 0)
        desc = "Display the pointcloud read back from SharpGL and display it."
    End Sub
    Public Shared Function invertMat(glDepth As cv.Mat) As cv.Mat
        Dim dst As New cv.Mat(glDepth.Size, cv.MatType.CV_32F, 0)
        Dim count As Integer, count1 As Integer
        Dim depthvals As New List(Of Single)
        For y = 0 To glDepth.Height - 1
            For x = 0 To glDepth.Width - 1
                Dim d = glDepth.Get(Of Single)(y, x)
                If d = 0 Then Continue For
                count += 1

                Dim x_ndc As Single = (x / glDepth.Width) * 2.0F - 1.0F
                Dim y_ndc As Single = (y / glDepth.Height) * 2.0F - 1.0F
                Dim u = CInt((x_ndc * fx / d) + ppx)
                Dim v = CInt((y_ndc * fy / d) + ppy)

                depthvals.Add(d)

                If u >= 0 And u < dst.Width And v >= 0 And v < dst.Height Then
                    count1 += 1
                    dst.Set(Of Single)(v, u, d)
                End If
            Next
        Next
        msg = CStr(count) + " pixels had depth while " + CStr(count1) + " inverted pixels landed in the image."
        Return dst
    End Function
    Public Shared Function reProject(glCloud As cv.Mat) As cv.Mat
        mm = GetMinMax(task.pcSplit(2), task.depthMask)
        Dim pcMask = glCloud.InRange(0.01F, 0.99F)
        glCloud = glCloud * (mm.maxVal - mm.minVal) + mm.minVal
        glCloud.SetTo(0, Not pcMask)
        Return invertMat(glCloud)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            strOut = task.sharpGL.RunSharp(Comm.oCase.readPC)
            SetTrueText(strOut, 2)
        End If

        dst2 = reProject(task.sharpDepth)
        If standaloneTest() Then
            Dim pcMask = task.sharpDepth.InRange(0.01F, 0.99F)
            dst3 = task.sharpDepth * (mm.maxVal - mm.minVal) + mm.minVal
            dst3.SetTo(0, Not pcMask)
        End If
        If task.heartBeat Then labels(2) = msg
    End Sub
End Class





Public Class GL_ReadLines : Inherits TaskParent
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        desc = "Draw lines in SharpGL and read them back."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone

        dst3 = task.color.Clone
        For Each lp In task.lines.lpList
            'DrawLine(dst3, lp, task.scalarColors(lp.index))
            DrawLine(dst3, lp, white)
        Next

        strOut = task.sharpGL.RunSharp(Comm.oCase.readLines, task.pointCloud, dst3)
        SetTrueText(strOut, 3)

        labels(3) = task.lines.labels(2)

        displayPC.Run(src)
        dst2 = displayPC.dst2
        labels(2) = displayPC.labels(2)
    End Sub
End Class





Public Class GL_ReadQuads : Inherits TaskParent
    Dim displayPC As New GL_DisplayPC
    Public Sub New()
        desc = "Read the quads back from a rendered geometry"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = task.sharpGL.RunSharp(Comm.oCase.readQuads)
        SetTrueText(strOut, 2)

        displayPC.Run(emptyMat)
        dst2 = displayPC.dst2
    End Sub
End Class