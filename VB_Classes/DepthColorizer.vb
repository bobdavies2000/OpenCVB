Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class DepthColorizer_Basics : Inherits TaskParent
    Public buildCorrMap As New GridCell_CorrelationMap
    Public Sub New()
        desc = "Create a traditional depth color scheme."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.ColorizedDepth.Checked Then
            src = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters, cv.ThresholdTypes.Trunc)
            Dim depthNorm As cv.Mat = src * 255 / task.MaxZmeters
            depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
            cv.Cv2.ApplyColorMap(depthNorm, task.depthRGB, task.depthColorMap)
        Else
            buildCorrMap.Run(src)
            task.depthRGB = buildCorrMap.dst2
            If task.toggleOn Then task.depthRGB.SetTo(0, task.noDepthMask)
        End If
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