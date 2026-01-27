Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class GL_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Display the pointcloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.drawPointCloudRGB)
            SetTrueText(strOut, 2)
        End Sub
    End Class





    Public Class NR_GL_MainForm : Inherits TaskParent
        Public Sub New()
            desc = "Display the pointcloud in the main form - too much work..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            task.GLRequest = Common.oCase.drawPointCloudRGB
            SetTrueText("Why not run all SharpGL algorithms here?" + vbCrLf +
                    "Because too much data has to move from task to main.")
        End Sub
    End Class




    Public Class NR_GL_Line3DNoMotionInput : Inherits TaskParent
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

            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, dst0)
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class NR_GL_Bricks : Inherits TaskParent
        Public Sub New()
            desc = "Display the bricks in SharpGL"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.quadBasics)
            SetTrueText(strOut, 2)
        End Sub
    End Class






    Public Class NR_GL_StructuredLines : Inherits TaskParent
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

            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, dst0, dst1)
            SetTrueText(strOut, 2)
        End Sub
    End Class






    Public Class NR_GL_RunSharp : Inherits TaskParent
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            desc = "Create a SharpGL view that uses the point cloud coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
            SetTrueText(strOut, 2)

            displayPC.Run(emptyMat)
            dst2 = displayPC.dst2
        End Sub
    End Class






    Public Class NR_GL_RunSharpHist : Inherits TaskParent
        Dim plotHist As New GL_PlotHist
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            desc = "Read the point cloud from a rendered geometry"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
                SetTrueText(strOut, 2)
            End If

            plotHist.Run(task.sharpDepth.Resize(task.workRes, cv.MatType.CV_32F, cv.InterpolationFlags.Nearest))
            dst3 = plotHist.dst3
            labels(2) = plotHist.labels(2)

            displayPC.Run(emptyMat)
            dst2 = displayPC.dst2
        End Sub
    End Class






    Public Class NR_GL_Line3DWhite : Inherits TaskParent
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
            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, dst3, dst1)
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class NR_GL_Line3DReconstructed : Inherits TaskParent
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
            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, dst3, dst1)
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class NR_GL_LinePoints3D : Inherits TaskParent
        Dim line3D As New Line3D_ReconstructLine
        Public Sub New()
            desc = "Visualize with OpenGL the reconstructed 3D line behind the RGB line selected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            line3D.Run(src)
            dst2 = line3D.dst2
            labels(2) = line3D.labels(2)

            dst1.SetTo(white)

            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, line3D.pointcloud, dst1)
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class NR_GL_LinePointsAll : Inherits TaskParent
        Public Sub New()
            desc = "Visualize all the reconstructed 3D lines found in the RGB image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2.SetTo(0)
            For Each lp In task.lines.lpList
                DrawLine(dst2, lp, lp.color)
            Next

            strOut = task.sharpGL.RunSharp(Common.oCase.line3D, task.pointCloud, dst2)
            SetTrueText(strOut, 2)
        End Sub
    End Class







    Public Class NR_GL_ReadPC : Inherits TaskParent
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            desc = "Read the point cloud from a rendered geometry"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
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
                strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
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




    Public Class NR_GL_ReadPCHist : Inherits TaskParent
        Dim glPlot As New GL_PlotHist
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            labels(3) = "The values returned by ReadPointCloud range from 0 to 1 and should have the same profile as Plot_Histogram."
            desc = "Read the point cloud from a rendered geometry"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
            labels(2) = strOut

            glPlot.Run(task.sharpDepth)
            dst3 = glPlot.dst3
            labels(2) += glPlot.labels(2)

            displayPC.Run(emptyMat)
            dst2 = displayPC.dst2
        End Sub
    End Class





    Public Class GL_DisplayPC : Inherits TaskParent
        Public Shared ppx = task.calibData.leftIntrinsics.ppx
        Public Shared ppy = task.calibData.leftIntrinsics.ppy
        Public Shared fx = task.calibData.leftIntrinsics.fx
        Public Shared fy = task.calibData.leftIntrinsics.fy
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
            mm = GetMinMax(task.pcSplit(2), task.depthmask)
            Dim pcMask = glCloud.InRange(0.01F, 0.99F)
            glCloud = glCloud * (mm.maxVal - mm.minVal) + mm.minVal
            glCloud.SetTo(0, Not pcMask)
            Return invertMat(glCloud)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                strOut = task.sharpGL.RunSharp(Common.oCase.readPC)
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





    Public Class NR_GL_ReadLines : Inherits TaskParent
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            desc = "Draw lines in SharpGL and read them back."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone

            dst3 = task.color.Clone
            For Each lp In task.lines.lpList
                'DrawLine(dst3, lp, lp.color)
                DrawLine(dst3, lp, white)
            Next

            strOut = task.sharpGL.RunSharp(Common.oCase.readLines, task.pointCloud, dst3)
            SetTrueText(strOut, 3)

            labels(3) = task.lines.labels(2)

            displayPC.Run(src)
            dst2 = displayPC.dst2
            labels(2) = displayPC.labels(2)
        End Sub
    End Class





    Public Class NR_GL_ReadQuads : Inherits TaskParent
        Dim displayPC As New GL_DisplayPC
        Public Sub New()
            desc = "Read the quads back from a rendered geometry"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunSharp(Common.oCase.readQuads)
            SetTrueText(strOut, 2)

            displayPC.Run(emptyMat)
            dst2 = displayPC.dst2
        End Sub
    End Class





    Public Class NR_GL_Line3D : Inherits TaskParent
        Dim line3d As New Line3D_DrawLines
        Public Sub New()
            desc = "Display the point cloud with the 3D lines drawn"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            line3d.Run(src)
            If task.toggleOn Then
                strOut = task.sharpGL.RunSharp(Common.oCase.drawPointCloudRGB, line3d.dst2, line3d.dst3)
            Else
                strOut = task.sharpGL.RunSharp(Common.oCase.drawPointCloudRGB, line3d.dst2, task.lines.dst2)
            End If
            SetTrueText(strOut, 2)
        End Sub
    End Class




    Public Class NR_GL_Line3D_Debug : Inherits TaskParent
        Dim line3d As New Line3D_DrawLines_Debug
        Public Sub New()
            If standalone Then task.gOptions.LineWidth.Value = 3
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Display the selected line in 3D with the pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            line3d.Run(src)
            dst1 = line3d.dst1
            dst2 = line3d.dst2
            labels(2) = line3d.labels(2)
            dst3 = line3d.dst3
            labels(3) = line3d.labels(3)

            strOut = task.sharpGL.RunSharp(Common.oCase.drawPointCloudRGB, line3d.dst2, dst3)
            SetTrueText(strOut, 2)
        End Sub
    End Class



    Public Class NR_GL_Line3D_DebugAlt : Inherits TaskParent
        Dim line3d As New Line3D_DrawLines_Debug
        Public Sub New()
            If standalone Then task.gOptions.LineWidth.Value = 3
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Display the selected line in 3D with the pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            line3d.Run(src)
            dst1 = line3d.dst1
            dst2 = line3d.dst2
            labels(2) = line3d.labels(2)
            dst3 = line3d.dst3
            labels(3) = line3d.labels(3)

            strOut = task.sharpGL.RunSharp(Common.oCase.drawPointCloudRGB, line3d.dst2, dst3)
            SetTrueText(strOut, 2)
        End Sub
    End Class





    Public Class NR_GL_Draw3DLines : Inherits TaskParent
        Public Sub New()
            desc = "Draw the RGB lines in SharpGL"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.lines.dst2
            strOut = task.sharpGL.RunSharp(Common.oCase.draw3DLines)
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class GL_LogicalLines : Inherits TaskParent
        Dim logLines As New Line3D_LogicalLines
        Public drawRequest As Integer = Common.oCase.draw3DLines
        Public Sub New()
            desc = "Draw the logical lines found in the point cloud with the RGB lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            logLines.Run(src)
            dst2 = logLines.dst2.Clone
            If task.toggleOn Then
                strOut = "Missing depth removed from lines in the image at left (dst2)"
                SetTrueText(strOut, 3)
                dst2.SetTo(0, task.noDepthMask)
            End If

            labels = logLines.labels
            task.sharpGL.RunLines(drawRequest, logLines.lpList)
        End Sub
    End Class




    Public Class NR_GL_LogicalCloud : Inherits TaskParent
        Dim glTest As New GL_LogicalLines
        Public Sub New()
            glTest.drawRequest = Common.oCase.draw3DLinesAndCloud
            desc = "Draw the logical lines found and include the entire pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            glTest.Run(src)
            SetTrueText(glTest.strOut, 3)
            dst2 = glTest.dst2
            labels = glTest.labels
        End Sub
    End Class






    Public Class NR_GL_ImageHullsColor : Inherits TaskParent
        Public Sub New()
            desc = "Prepare triangles from the RedCloud_HeartBeat output"
        End Sub
        Public Shared Function buildBuffer() As List(Of cv.Vec3f)
            Dim dataBuffer As New List(Of cv.Vec3f)
            Dim vec(2) As cv.Vec3f, pt As cv.Point
            For Each pc In task.redCloud.rcList
                Dim count As Single = pc.hull.Count
                For i = 0 To pc.hull.Count - 1
                    Dim goodDepth As Boolean = True
                    For j = 0 To vec.Length - 1
                        Select Case j
                            Case 0
                                pt = New cv.Point(CInt(pc.hull(i).X + pc.rect.X), CInt(pc.hull(i).Y + pc.rect.Y))
                            Case 1
                                pt = pc.maxDist
                            Case 2
                                pt = New cv.Point(CInt(pc.hull((i + 1) Mod count).X + pc.rect.X), CInt(pc.hull((i + 1) Mod count).Y + pc.rect.Y))
                        End Select

                        vec(j) = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
                        If vec(j)(0) = 0 Or vec(j)(1) = 0 Or vec(j)(2) = 0 Then goodDepth = False
                    Next

                    If goodDepth Then
                        dataBuffer.Add(New cv.Vec3f(pc.color(2), pc.color(1), pc.color(0)))
                        For j = 0 To vec.Length - 1
                            dataBuffer.Add(New cv.Vec3f(vec(j)(0), vec(j)(1), vec(j)(2)))
                        Next
                    End If
                Next
            Next
            Return dataBuffer
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))
            labels(3) = task.redCloud.labels(3)

            strOut = task.sharpGL.RunTriangles(Common.oCase.colorTriangles, buildBuffer())
        End Sub
    End Class






    Public Class NR_GL_ImageHulls : Inherits TaskParent
        Public Sub New()
            desc = "Prepare a texture map and project it onto the RedCloud_HeartBeat hulls"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            strOut = task.sharpGL.RunTriangles(Common.oCase.imageTriangles, Nothing)

            dst2 = task.sharpGL.hulls.dst2
            dst3 = task.sharpGL.hulls.dst3
            labels(2) = task.sharpGL.hulls.labels(2) + " " + Format(task.sharpGL.hulls.percentImage, "0.0%") +
                    " of depth data used."
            labels(3) = task.sharpGL.hulls.labels(3)
        End Sub
    End Class
End Namespace