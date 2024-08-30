Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text
Imports OpenCvSharp

Public Class CPP_Basics : Inherits VB_Parent
    Dim cppFunction As Integer
    Public result As cvb.Mat
    Public neighbors As New List(Of cvb.Point2f)
    Public neighborIndexToTrain As List(Of Integer)
    Public Sub New(_cppFunction As Integer)
        updateFunction(_cppFunction)
    End Sub
    Public Sub updateFunction(_cppFunction As Integer)
        cppFunction = _cppFunction
        labels(2) = "Running CPP_Basics, Output from " + task.algName

        cPtr = cppTask_Open(cppFunction, task.WorkingRes.Height, task.WorkingRes.Width,
                            task.heartBeat, 0.5, task.lineWidth, task.lineType, task.DotSize,
                            task.gridSize, task.histogramBins,
                            task.useGravityPointcloud, task.gOptions.pixelDiffThreshold,
                            task.gOptions.UseKalman.Checked, task.gOptions.Palettes.SelectedIndex, task.optionsChanged,
                            task.frameHistoryCount, task.gOptions.displayDst0.Checked, task.gOptions.displayDst1.Checked)

        getOptions()
    End Sub
    Private Sub getOptions()
        Dim labelBuffer As StringBuilder = New StringBuilder(512)
        Dim descBuffer As StringBuilder = New StringBuilder(512)
        Dim adviceBuffer As StringBuilder = New StringBuilder(512)
        cppTask_OptionsCPPtoVB(cPtr, task.gridSize,
                               task.histogramBins,
                               task.gOptions.pixelDiffThreshold, task.gOptions.UseKalman.Checked,
                               task.frameHistoryCount, task.drawRect.X, task.drawRect.Y,
                               task.drawRect.Width, task.drawRect.Height, labelBuffer, descBuffer,
                               adviceBuffer)

        labels = labelBuffer.ToString.Split("|")
        UpdateAdvice(traceName + ": " + adviceBuffer.ToString)
        desc = descBuffer.ToString
    End Sub
    Public Sub New()
    End Sub

    Public Sub RunAlg(src As cvb.Mat)

        cppTask_OptionsVBtoCPP(cPtr, task.gridSize,
                               task.histogramBins,
                               task.gOptions.pixelDiffThreshold, task.gOptions.UseKalman.Checked,
                               task.frameHistoryCount,
                               task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height,
                               task.lineWidth, task.lineType, task.DotSize, task.lowRes.Width, task.lowRes.Height,
                               task.MaxZmeters, task.redOptions.PointCloudReduction, task.cvFontSize, task.cvFontThickness,
                               task.ClickPoint.X, task.ClickPoint.Y, task.mouseClickFlag,
                               task.mousePicTag, task.mouseMovePoint.X, task.mouseMovePoint.Y,
                               task.paletteIndex, 255, task.midHeartBeat,
                               task.quarterBeat, task.redOptions.colorInputIndex, task.redOptions.depthInputIndex,
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
                       task.gOptions.displayDst0.Checked, task.gOptions.displayDst1.Checked, task.gOptions.debugChecked)
        handleInput.Free()
        getOptions()

        Dim dstPtr As IntPtr, type As Integer
        dstPtr = cppTask_GetDst(cPtr, 0, type)
        dst0 = cvb.Mat.FromPixelData(src.Rows, src.Cols, type, dstPtr).Clone

        dstPtr = cppTask_GetDst(cPtr, 1, type)
        dst1 = cvb.Mat.FromPixelData(src.Rows, src.Cols, type, dstPtr).Clone

        dstPtr = cppTask_GetDst(cPtr, 2, type)
        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, type, dstPtr).Clone

        dstPtr = cppTask_GetDst(cPtr, 3, type)
        dst3 = cvb.Mat.FromPixelData(src.Rows, src.Cols, type, dstPtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = cppTask_Close(cPtr)
    End Sub
End Class






Public Class CPP_Managed
    Public workingRes As New cvb.Size
    Public color As cvb.Mat
    Public depthRGB As cvb.Mat
    Public pointCloud As cvb.Mat
    Public leftView As cvb.Mat
    Public rightView As cvb.Mat
    Public input As cvb.Mat
    Public cols As Integer
    Public rows As Integer
    Public srcType As Integer

    Public dst0 As cvb.Mat
    Public dst1 As cvb.Mat
    Public dst2 As cvb.Mat
    Public dst3 As cvb.Mat
    Public Sub New()
        ' This interface is called from the C++/CLR algorithms to build the task structure in C++/CLR."
    End Sub
    Public Sub resumeTask()
        cols = task.color.Width
        rows = task.color.Height
        srcType = task.color.Type
        color = task.color
        depthRGB = task.depthRGB
        pointCloud = task.pointCloud
        leftView = task.leftView
        rightView = task.rightView

        dst0 = task.dst0
        dst1 = task.dst1
        dst2 = task.dst2
        dst3 = task.dst3
    End Sub
    Public Sub pauseTask(dst0 As cvb.Mat, dst1 As cvb.Mat, dst2 As cvb.Mat, dst3 As cvb.Mat)
        task.dst0 = dst0
        task.dst1 = dst1
        task.dst2 = dst2
        task.dst3 = dst3
    End Sub
End Class