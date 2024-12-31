Imports cvb = OpenCvSharp
Imports System.IO
'Public Class ParticleFilter_Basics : Inherits TaskParent
'    Dim trace As New Swarm_Basics
'    Dim plot1D As New Plot_Histogram2D
'    Dim histogram As New cvb.Mat
'    Public Sub New()
'        If standaloneTest() Then task.gOptions.setDisplay1()
'        labels = {"", "", "Particle traffic", "Largest count in 2D Histogram"}
'        desc = "Use the good features of an image to create a histogram of particle motion. Peak histogram is net movement of the camera."
'    End Sub
'    Public Overrides sub runAlg(src As cvb.Mat)
'        Static distanceSlider = FindSlider("Distance threshold (pixels)")
'        Dim matSize = 21 ' must be odd
'        Dim halfsize = 10
'        If histogram.Rows <> matSize Then
'            histogram = New cvb.Mat(matSize, matSize, cvb.MatType.CV_32F, cvb.Scalar.All(0))
'            task.gOptions.HistBinSlider.Value = matSize
'        End If

'        trace.Run(src)
'        dst2 = trace.dst2
'        If task.firstPass Then Exit Sub ' all entries are identical on the first pass.

'        histogram.SetTo(0)
'        For Each lp In trace.lpList
'            Dim x = lp.p1.X - lp.p2.X + halfsize
'            Dim y = lp.p1.Y - lp.p2.Y + halfsize
'            If x > matSize Or x < 0 Then Continue For
'            If y > matSize Or y < 0 Then Continue For
'            Dim val = histogram.Get(Of Single)(y, x)
'            histogram.Set(Of Single)(y, x, val + 1)
'        Next
'        plot1D.Run(histogram)
'        dst3 = plot1D.dst2

'        Dim mm as mmData = GetMinMax(histogram)

'        Dim w = CInt(dst2.Width / matSize)
'        Dim h = CInt(dst2.Height / matSize)
'        Dim maxLoc = New cvb.Point2f(w * mm.maxLoc.X, h * mm.maxLoc.Y)
'        dst1.SetTo(0)
'        dst1.Rectangle(New cvb.Rect(maxLoc.X, maxLoc.Y, w, h), white, task.lineWidth, task.lineType)

'        Dim center = New cvb.Point2f(mm.maxLoc.X - halfsize, mm.maxLoc.Y - halfsize)
'        SetTrueText("Histogram peak is at " + center.ToString, 1)
'    End Sub
'End Class







' https://github.com/masaddev/OpenCVParticleFilter/tree/master/OpenCVParticleFilter
Public Class ParticleFilter_Example : Inherits TaskParent
    Dim imageFrame = 12
    Public Sub New()
        cPtr = ParticleFilterTest_Open(task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        desc = "Particle Filter example downloaded from github - hyperlink in the code shows URL."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        imageFrame += 1
        If imageFrame Mod 45 = 0 Then
            imageFrame = 13
            ParticleFilterTest_Close(cPtr)
            cPtr = ParticleFilterTest_Open(task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        End If
        Dim nextFile As New FileInfo(task.HomeDir + "Data/ballSequence/color_" + CStr(imageFrame) + ".png")
        dst3 = cvb.Cv2.ImRead(nextFile.FullName).Resize(dst2.Size)
        Dim imagePtr = ParticleFilterTest_Run(cPtr)
        dst2 = cvb.Mat.FromPixelData(dst2.Rows, dst2.Cols, cvb.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = ParticleFilterTest_Close(cPtr)
    End Sub
End Class






'Public Class ParticleFilter_Net : Inherits TaskParent
'    Dim trace As New Swarm_Basics
'    Public Sub New()
'        labels = {"", "", "Particle traffic", "Net movement of all the particles"}
'        desc = "Use the good features of an image to create a set of particles that can estimate camera motion"
'    End Sub
'    Public Overrides sub runAlg(src As cvb.Mat)
'        trace.Run(src)
'        dst2 = trace.dst2

'        Dim net As cvb.Point2f
'        For Each mp In trace.lpList
'            net.X += mp.p1.X - mp.p2.X
'            net.Y += mp.p1.Y - mp.p2.Y
'        Next
'        net.X = dst2.Width / 2 + net.X / trace.lpList.Count
'        net.Y = dst2.Height / 2 + net.Y / trace.lpList.Count
'        dst3.SetTo(0)
'        DrawLine(dst3, New cvb.Point2f(dst2.Width / 2, dst2.Height / 2), net, white, task.lineWidth, task.lineType)
'        SetTrueText(trace.strOut, 3)
'    End Sub
'End Class