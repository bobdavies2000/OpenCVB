Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Markup
Imports OpenCvSharp.Extensions

Public Class CPP_Basics : Inherits VB_Algorithm
    Public cppFunction As Integer
    Public result As cv.Mat
    Public neighbors As New List(Of cv.Point2f)
    Public neighborIndexToTrain As List(Of Integer)
    Public Sub New(_cppFunction As Integer)
        updateFunction(_cppFunction)
    End Sub
    Public Sub updateFunction(_cppFunction As Integer)
        cppFunction = _cppFunction
        labels(2) = "Running CPP_Basics, Output from " + task.algName

        cPtr = cppTask_Open(cppFunction, task.workingRes.Height, task.workingRes.Width,
                            task.heartBeat, 0.5, task.lineWidth, task.lineType, task.dotSize,
                            gOptions.GridSize.Value, task.histogramBins,
                            gOptions.gravityPointCloud.Checked, gOptions.PixelDiffThreshold.Value,
                            gOptions.UseKalman.Checked, gOptions.Palettes.SelectedIndex, task.optionsChanged,
                            task.frameHistoryCount, gOptions.displayDst0.Checked, gOptions.displayDst1.Checked)

        getOptions()
    End Sub
    Private Sub getOptions()
        Dim labelBuffer As StringBuilder = New StringBuilder(512)
        Dim descBuffer As StringBuilder = New StringBuilder(512)
        Dim adviceBuffer As StringBuilder = New StringBuilder(512)
        cppTask_OptionsCPPtoVB(cPtr, gOptions.GridSize.Value,
                               gOptions.HistBinSlider.Value,
                               gOptions.PixelDiffThreshold.Value, gOptions.UseKalman.Checked,
                               task.frameHistoryCount, task.drawRect.X, task.drawRect.Y,
                               task.drawRect.Width, task.drawRect.Height, labelBuffer, descBuffer,
                               adviceBuffer)

        labels = labelBuffer.ToString.Split("|")
        vbAddAdvice(traceName + ": " + adviceBuffer.ToString)
        desc = descBuffer.ToString
    End Sub
    Public Sub New()
    End Sub

    Public Sub RunVB(src As cv.Mat)

        cppTask_OptionsVBtoCPP(cPtr, gOptions.GridSize.Value,
                               gOptions.HistBinSlider.Value,
                               gOptions.PixelDiffThreshold.Value, gOptions.UseKalman.Checked,
                               task.frameHistoryCount,
                               task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height,
                               task.lineWidth, task.lineType, task.dotSize, task.lowRes.Width, task.lowRes.Height,
                               task.maxZmeters, redOptions.PCReduction, task.cvFontSize, task.cvFontThickness,
                               task.clickPoint.X, task.clickPoint.Y, task.mouseClickFlag,
                               task.mousePicTag, task.mouseMovePoint.X, task.mouseMovePoint.Y,
                               task.paletteIndex, 255, task.midHeartBeat,
                               task.quarterBeat, redOptions.colorInputIndex, redOptions.depthInputIndex,
                               task.xRangeDefault, task.yRangeDefault)

        Dim pointCloudData(task.pointCloud.Total * task.pointCloud.ElemSize - 1) As Byte
        Marshal.Copy(task.pointCloud.Data, pointCloudData, 0, pointCloudData.Length)
        Dim handlePointCloud = GCHandle.Alloc(pointCloudData, GCHandleType.Pinned)
        Dim pcPtr = cppTask_PointCloud(cPtr, handlePointCloud.AddrOfPinnedObject(),
                                       task.pointCloud.Rows, task.pointCloud.Cols)
        handlePointCloud.Free()

        Dim depthRGBData(task.depthRGB.Total * task.depthRGB.ElemSize - 1) As Byte
        Dim leftData(task.leftView.Total * task.leftView.ElemSize - 1) As Byte
        Dim rightData(task.rightView.Total * task.rightView.ElemSize - 1) As Byte

        Marshal.Copy(task.depthRGB.Data, depthRGBData, 0, depthRGBData.Length)
        Marshal.Copy(task.leftView.Data, leftData, 0, leftData.Length)
        Marshal.Copy(task.rightView.Data, rightData, 0, rightData.Length)

        Dim handleDepthRGB = GCHandle.Alloc(depthRGBData, GCHandleType.Pinned)
        Dim handleLeftView = GCHandle.Alloc(leftData, GCHandleType.Pinned)
        Dim handleRightView = GCHandle.Alloc(rightData, GCHandleType.Pinned)

        Dim depthRGBPtr = cppTask_DepthLeftRight(cPtr, handleDepthRGB.AddrOfPinnedObject(),
                                                 handleLeftView.AddrOfPinnedObject(),
                                                 handleRightView.AddrOfPinnedObject(),
                                                 task.depthRGB.Rows, task.depthRGB.Cols)

        handleDepthRGB.Free()
        handleLeftView.Free()
        handleRightView.Free()

        Dim inputImage(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputImage, 0, inputImage.Length)
        Dim handleInput = GCHandle.Alloc(inputImage, GCHandleType.Pinned)
        cppTask_RunCPP(cPtr, handleInput.AddrOfPinnedObject(), src.Channels, task.frameCount, dst2.Rows, dst2.Cols,
                       task.accRadians.X, task.accRadians.Y, task.accRadians.Z, task.optionsChanged, task.heartBeat,
                       gOptions.displayDst0.Checked, gOptions.displayDst1.Checked, gOptions.DebugCheckBox.Checked)
        handleInput.Free()
        getOptions()

        Dim channels As Integer, dstPtr As IntPtr
        dstPtr = cppTask_GetDst(cPtr, 0, channels)
        dst0 = New cv.Mat(src.Rows, src.Cols, If(channels = 1, cv.MatType.CV_8UC1, cv.MatType.CV_8UC3), dstPtr)

        dstPtr = cppTask_GetDst(cPtr, 1, channels)
        dst1 = New cv.Mat(src.Rows, src.Cols, If(channels = 1, cv.MatType.CV_8UC1, cv.MatType.CV_8UC3), dstPtr)

        dstPtr = cppTask_GetDst(cPtr, 2, channels)
        dst2 = New cv.Mat(src.Rows, src.Cols, If(channels = 1, cv.MatType.CV_8UC1, cv.MatType.CV_8UC3), dstPtr)

        dstPtr = cppTask_GetDst(cPtr, 3, channels)
        dst3 = New cv.Mat(src.Rows, src.Cols, If(channels = 1, cv.MatType.CV_8UC1, cv.MatType.CV_8UC3), dstPtr)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = cppTask_Close(cPtr)
    End Sub
End Class