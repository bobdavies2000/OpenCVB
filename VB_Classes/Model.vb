﻿Imports System.Security.Cryptography
Imports cvb = OpenCvSharp
Public Class Model_Basics : Inherits TaskParent
    Dim oglM As New OpenGL_BasicsMouse
    Public Sub New()
        labels = {"", "", "Captured OpenGL output", ""}
        desc = "Capture the output of the OpenGL window"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then oglM.Run(src)
        dst2 = oglM.dst2
        dst3 = oglM.dst3
    End Sub
End Class






Public Class Model_OpenGL_Sliders : Inherits TaskParent
    Dim model As New Model_Basics
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Basics"
        labels = {"", "", "Captured OpenGL output", ""}
        desc = "Capture the output of the OpenGL window"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        If standaloneTest() Then task.ogl.Run(src)
        model.Run(src)
        dst2 = model.dst2
    End Sub
End Class







Public Class Model_FlatSurfaces : Inherits TaskParent
    Public totalPixels As Integer
    Dim floorList As New List(Of Single)
    Dim ceilingList As New List(Of Single)
    Public Sub New()
        desc = "Minimalist approach to find a flat surface that is oriented to gravity (floor or ceiling)"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim ranges() = New cvb.Rangef() {New cvb.Rangef(-task.yRange, task.yRange), New cvb.Rangef(0, task.MaxZmeters)}
        cvb.Cv2.CalcHist({task.pointCloud}, {1, 2}, New cvb.Mat, dst0, 2,
                        {dst2.Height, dst2.Width}, ranges)

        Dim thicknessCMs = 0.1, rect As cvb.Rect, nextY As Single
        totalPixels = 0
        For y = dst0.Height - 2 To 0 Step -1
            rect = New cvb.Rect(0, y, dst0.Width - 1, 1)
            Dim count = dst0(rect).CountNonZero
            Dim pixelCount = dst0(rect).Sum()
            totalPixels += pixelCount.Val0
            If count > 10 Then
                nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs
                Exit For
            End If
        Next

        Dim floorY = rect.Y
        floorList.Add(nextY)
        task.pcFloor = floorList.Average()
        If floorList.Count > task.frameHistoryCount Then floorList.RemoveAt(0)
        labels(2) = "Y = " + Format(task.pcFloor, fmt3) + " separates the floor.  Total pixels below floor level = " + Format(totalPixels, fmt0)

        For y = 0 To dst2.Height - 1
            rect = New cvb.Rect(0, y, dst0.Width - 1, 1)
            Dim count = dst0(rect).CountNonZero
            Dim pixelCount = dst0(rect).Sum()
            totalPixels += pixelCount.Val0
            If count > 10 Then
                nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs
                Exit For
            End If
        Next

        Dim ceilingY = rect.Y
        ceilingList.Add(nextY)
        task.pcCeiling = ceilingList.Average()
        If ceilingList.Count > task.frameHistoryCount Then ceilingList.RemoveAt(0)
        labels(3) = "Y = " + Format(task.pcCeiling, fmt3) + " separates the ceiling.  Total pixels above ceiling level = " + Format(totalPixels, fmt0)

        If standaloneTest() Then
            dst2 = dst0.Threshold(0, 255, cvb.ThresholdTypes.Binary)
            dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
            dst2 = dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            dst2.Line(New cvb.Point(0, floorY), New cvb.Point(dst2.Width, floorY), cvb.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(New cvb.Point(0, ceilingY), New cvb.Point(dst2.Width, ceilingY), cvb.Scalar.Red, task.lineWidth + 2, task.lineType)
        End If
    End Sub
End Class







Public Class Model_RedCloud : Inherits TaskParent
    Public oglD As New OpenGL_DrawHulls
    Public Sub New()
        labels = {"", "", "OpenGL output", "RedCloud Output"}
        desc = "Capture the OpenGL output of the drawn cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        oglD.Run(src)
        dst2 = oglD.dst2
    End Sub
End Class








Public Class Model_CellZoom : Inherits TaskParent
    Dim oglData As New Model_RedCloud
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "RedCloud_Hull output", "Selected cell in 3D"}
        desc = "Zoom in on the selected RedCloud cell in the OpenGL output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        oglData.Run(src)
        dst2 = oglData.dst2
        dst3 = oglData.oglD.dst3

        Dim rcX = task.rc

        dst1.SetTo(0)
        Dim mask = dst3.InRange(cvb.Scalar.White, cvb.Scalar.White)

        dst3.CopyTo(dst1, mask)
        Dim points = mask.FindNonZero()
        If points.Rows > 0 Then
            Dim split = points.Split()
            Dim mmX = GetMinMax(split(0))
            Dim mmY = GetMinMax(split(1))

            Dim r = New cvb.Rect(mmX.minVal, mmY.minVal, mmX.maxVal - mmX.minVal, mmY.maxVal - mmY.minVal)
            dst1.Rectangle(r, cvb.Scalar.White, 1, task.lineType)
        End If
    End Sub
End Class
