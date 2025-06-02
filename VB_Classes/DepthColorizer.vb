Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class DepthColorizer_Basics : Inherits TaskParent
    Public Sub New()
        Dim gradientWidth = Math.Min(dst2.Width, 256)
        Dim f As Double = 1.0
        If saveVecColors.Count = 1 Then
            Dim initVal = 43
            Dim rand = New Random(initVal) ' This will make colors consistent across runs and they seem to look ok...
            Dim bgr(3) As Byte
            For i = 0 To task.vecColors.Length - 1
                rand.NextBytes(bgr)
                task.vecColors(i) = New cv.Vec3b(bgr(0), bgr(1), bgr(2))
                task.scalarColors(i) = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
            Next

            Dim color1 = cv.Scalar.Blue, color2 = cv.Scalar.Yellow
            task.depthColorList = New List(Of cv.Vec3b)
            For i = 0 To gradientWidth - 1
                Dim v1 = f * color2(0) + (1 - f) * color1(0)
                Dim v2 = f * color2(1) + (1 - f) * color1(1)
                Dim v3 = f * color2(2) + (1 - f) * color1(2)
                task.depthColorList.Add(New cv.Vec3b(v1, v2, v3))
                f -= 1 / gradientWidth
            Next
            task.depthColorList(0) = New cv.Vec3b ' black for the first color...
            task.depthColorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, task.depthColorList.ToArray)

            saveVecColors = task.vecColors
            saveScalarColors = task.scalarColors
            saveDepthColorMap = task.depthColorMap
            saveDepthColorList = New List(Of cv.Vec3b)(task.depthColorList)
        Else
            ' why do this?  To preserve the same colors regardless of which algorithm is invoked.
            ' Colors will be different when OpenCVB is restarted.  
            task.vecColors = saveVecColors
            task.scalarColors = saveScalarColors
            task.depthColorMap = saveDepthColorMap
            task.depthColorList = saveDepthColorList
        End If

        Dim color3 = cv.Scalar.Black, color4 = cv.Scalar.Red
        Dim corrColors = New List(Of cv.Vec3b)
        f = 1.0
        For i = 0 To gradientWidth - 1
            Dim v1 = f * color3(0) + (1 - f) * color4(0)
            Dim v2 = f * color3(1) + (1 - f) * color4(1)
            Dim v3 = f * color3(2) + (1 - f) * color4(2)
            corrColors.Add(New cv.Vec3b(v1, v2, v3))
            f -= 1 / gradientWidth
        Next
        task.correlationColorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, corrColors.ToArray)

        desc = "Create a traditional depth color scheme."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' a direct call from another algorithm is unnecessary - already been run...
        If task.gOptions.GridDepth.Checked Then
            task.depthRGB = task.brickBasics.dst2
        ElseIf task.gOptions.ColorizedDepth.Checked Then
            Dim depthNorm As cv.Mat = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters, cv.ThresholdTypes.Trunc)
            depthNorm = depthNorm * 255 / task.MaxZmeters
            depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
            task.depthRGB = ShowPaletteCorrelation(depthNorm)
        Else
            task.depthRGB = task.buildCorr.dst2.Clone
        End If
        If standaloneTest() Then dst2 = task.depthRGB.Clone
    End Sub
End Class






Public Class DepthColorizer_CPP : Inherits TaskParent
    Public Sub New()
        cPtr = Depth_Colorizer_Open()
        desc = "Display depth data with InRange.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.MaxZmeters)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Depth_Colorizer_Close(cPtr)
    End Sub
End Class







Public Class DepthColorizer_Mean : Inherits TaskParent
    Public avg As New Math_ImageAverage
    Public colorize As New DepthColorizer_CPP
    Public Sub New()
        labels(3) = "32-bit format depth data"
        desc = "Take the average depth at each pixel but eliminate any pixels that had zero depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        avg.Run(src)

        dst3 = avg.dst2
        colorize.Run(dst3)
        dst2 = colorize.dst2
    End Sub
End Class