Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class DepthColorizer_Basics : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            Dim gradientWidth = Math.Min(dst2.Width, 256)
            Dim f As Double = 1.0
            If saveVecColors.Count = 1 Then
                Dim initVal = 43
                Dim rand = New Random(initVal) ' This will make colors consistent across runs and they seem to look ok...
                Dim bgr(3) As Byte
                For i = 0 To tsk.vecColors.Length - 1
                    rand.NextBytes(bgr)
                    tsk.vecColors(i) = New cv.Vec3b(bgr(0), bgr(1), bgr(2))
                    tsk.scalarColors(i) = New cv.Scalar(tsk.vecColors(i)(0), tsk.vecColors(i)(1), tsk.vecColors(i)(2))
                Next

                Dim color1 = cv.Scalar.Blue, color2 = cv.Scalar.Yellow
                Dim colorList As New List(Of cv.Vec3b)
                For i = 0 To gradientWidth - 1
                    Dim v1 = f * color2(0) + (1 - f) * color1(0)
                    Dim v2 = f * color2(1) + (1 - f) * color1(1)
                    Dim v3 = f * color2(2) + (1 - f) * color1(2)
                    colorList.Add(New cv.Vec3b(v1, v2, v3))
                    f -= 1 / gradientWidth
                Next
                colorList(0) = New cv.Vec3b ' black for the first color...
                tsk.depthColorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, colorList.ToArray)

                saveVecColors = tsk.vecColors
                saveScalarColors = tsk.scalarColors
                saveDepthColorMap = tsk.depthColorMap
            Else
                ' why do this?  To preserve the same colors regardless of which algorithm is invoked.
                ' Colors will be different when OpenCVB is restarted.  
                tsk.vecColors = saveVecColors
                tsk.scalarColors = saveScalarColors
                tsk.depthColorMap = saveDepthColorMap
            End If

            tsk.colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, tsk.vecColors.ToArray)

            tsk.vecColors(0) = New cv.Vec3b ' first color is black...
            tsk.colorMapZeroIsBlack = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, tsk.vecColors.ToArray)

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
            tsk.correlationColorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, corrColors.ToArray)

            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Create a traditional depth color scheme."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.gOptions.displayDst1.Checked = False Or standaloneTest() Then
                If cPtr = 0 Then cPtr = Depth_Colorizer_Open()
                Dim depthData(tsk.pcSplit(2).Total * tsk.pcSplit(2).ElemSize - 1) As Byte
                Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
                Marshal.Copy(tsk.pcSplit(2).Data, depthData, 0, depthData.Length)
                Dim imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, tsk.MaxZmeters)
                handleSrc.Free()

                If imagePtr <> 0 Then tsk.depthRGB = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)

                Dim gridIndex = tsk.gridMap.Get(Of Integer)(tsk.mouseMovePoint.Y, tsk.mouseMovePoint.X)
                Dim depthGrid = tsk.pcSplit(2)(tsk.gridRects(gridIndex))
                Dim mask = tsk.depthmask(tsk.gridRects(gridIndex))
                Dim depth = depthGrid.Mean(mask)(0)
                Dim mm = GetMinMax(depthGrid, mask)
                tsk.depthAndDepthRange = "Depth = " + Format(depth, fmt1) + "m grid = " + CStr(gridIndex) +
                                          " " + vbCrLf + "Depth range = " +
                                          Format(mm.maxVal - mm.minVal, fmt3) + "m"
            Else
                tsk.depthAndDepthRange = ""
            End If
            If standaloneTest() Then dst2 = tsk.depthRGB
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = Depth_Colorizer_Close(cPtr)
        End Sub
    End Class





    Public Class DepthColorizer_CPP : Inherits TaskParent
        Implements IDisposable
        Public Sub New()
            cPtr = Depth_Colorizer_Open()
            desc = "Display depth data with InRange.  Higher contrast than others - yellow to blue always present."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = tsk.pcSplit(2)

            Dim depthData(src.Total * src.ElemSize - 1) As Byte
            Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
            Marshal.Copy(src.Data, depthData, 0, depthData.Length)
            Dim imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, tsk.MaxZmeters)
            handleSrc.Free()

            If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
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
            If src.Type <> cv.MatType.CV_32F Then src = tsk.pcSplit(2)
            avg.Run(src)

            dst3 = avg.dst2
            colorize.Run(dst3)
            dst2 = colorize.dst2
        End Sub
    End Class
End Namespace