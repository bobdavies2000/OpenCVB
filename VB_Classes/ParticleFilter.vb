Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports  System.IO
Public Class ParticleFilter_Basics : Inherits VB_Algorithm
    Dim trace As New Feature_TraceKNN
    Dim plot2D As New Plot_Histogram2D
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Particle traffic", "Largest count in 2D Histogram"}
        desc = "Use the good features of an image to create a histogram of particle motion. Peak histogram is net movement of the camera."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static distanceSlider = findSlider("Distance threshold (pixels)")
        Static histogram As New cv.Mat
        Dim matSize = 2 * distanceSlider.value Or 1 ' must be odd
        Dim halfsize = distanceSlider.value
        If histogram.Rows <> matSize Then
            histogram = New cv.Mat(matSize, matSize, cv.MatType.CV_32F, 0)
            gOptions.HistBinSlider.Value = matSize
        End If

        trace.Run(src)
        dst2 = trace.dst2
        If firstPass Then Exit Sub ' all entries are identical on the first pass.

        histogram.SetTo(0)
        For Each mp In trace.mpList
            Dim x = mp.p1.X - mp.p2.X + halfsize
            Dim y = mp.p1.Y - mp.p2.Y + halfsize
            If x > matSize Or x < 0 Then Continue For
            If y > matSize Or y < 0 Then Continue For
            Dim val = histogram.Get(Of Single)(y, x)
            histogram.Set(Of Single)(y, x, val + 1)
        Next
        plot2D.Run(histogram)
        dst3 = plot2D.dst2

        Dim mm = vbMinMax(histogram)

        Dim w = CInt(dst2.Width / matSize)
        Dim h = CInt(dst2.Height / matSize)
        Dim maxLoc = New cv.Point2f(w * mm.maxLoc.X, h * mm.maxLoc.Y)
        dst1.SetTo(0)
        dst1.Rectangle(New cv.Rect(maxLoc.X, maxLoc.Y, w, h), cv.Scalar.White, task.lineWidth, task.lineType)

        Dim center = New cv.Point2f(mm.maxLoc.X - halfsize, mm.maxLoc.Y - halfsize)
        setTrueText("Histogram peak is at " + center.ToString, 1)
    End Sub
End Class







' https://github.com/masaddev/OpenCVParticleFilter/tree/master/OpenCVParticleFilter
Public Class ParticleFilter_Example : Inherits VB_Algorithm
    Public Sub New()
        cPtr = ParticleFilterTest_Open(task.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        desc = "Particle Filter example downloaded from github - hyperlink in the code shows URL."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static imageFrame = 12
        imageFrame += 1
        If imageFrame Mod 45 = 0 Then
            imageFrame = 13
            ParticleFilterTest_Close(cPtr)
            cPtr = ParticleFilterTest_Open(task.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        End If
        Dim nextFile As New FileInfo(task.homeDir + "Data/ballSequence/color_" + CStr(imageFrame) + ".png")
        dst3 = cv.Cv2.ImRead(nextFile.FullName).Resize(dst2.Size)
        Dim imagePtr = ParticleFilterTest_Run(cPtr)
        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = ParticleFilterTest_Close(cPtr)
    End Sub
End Class







Module ParticleFilter
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Run(pfPtr As IntPtr) As IntPtr
    End Function
End Module






Public Class ParticleFilter_Net : Inherits VB_Algorithm
    Dim trace As New Feature_TraceKNN
    Public Sub New()
        labels = {"", "", "Particle traffic", "Net movement of all the particles"}
        desc = "Use the good features of an image to create a particles that can estimate camera motion"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        trace.Run(src)
        dst2 = trace.dst2

        Dim net As cv.Point2f
        For Each mp In trace.mpList
            net.X += mp.p1.X - mp.p2.X
            net.Y += mp.p1.Y - mp.p2.Y
        Next
        net.X = dst2.Width / 2 + net.X / trace.mpList.Count
        net.Y = dst2.Height / 2 + net.Y / trace.mpList.Count
        dst3.SetTo(0)
        dst3.Line(New cv.Point2f(dst2.Width / 2, dst2.Height / 2), net, cv.Scalar.White, task.lineWidth, task.lineType)
        setTrueText(trace.strOut, 3)
    End Sub
End Class