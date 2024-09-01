Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text
Imports OpenCvSharp
Imports System.Windows.Forms.VisualStyles.VisualStyleElement

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

        cPtr = cppTask_Open(cppFunction, task.dst2.Rows, task.dst2.Cols,
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





Module managedCPP_Interface
    <DllImport(("CPP_Managed.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ManagedCPP_Resume(colorPtr As IntPtr, leftPtr As IntPtr, rightPtr As IntPtr,
                                depthRGBPtr As IntPtr, cloud As IntPtr) As Integer
    End Function

    <DllImport(("CPP_Managed.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ManagedCPP_Pause(ioIndex As Integer) As IntPtr
    End Function
End Module





Public Class CPP_ManagedTask : Inherits VB_Parent
    Dim hColor As GCHandle
    Dim hLeft As GCHandle
    Dim hRight As GCHandle
    Dim hDepthRGB As GCHandle
    Dim hCloud As GCHandle
    Dim ioIndex As Integer
    Public Sub New()
        desc = "Move data to the Managed C++/CLR code (CPP_Managed), run it, and retrieve the results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim colorData(task.color.Total * task.color.ElemSize - 1) As Byte
        Dim leftData(task.leftView.Total * task.leftView.ElemSize - 1) As Byte
        Dim rightData(task.rightView.Total * task.rightView.ElemSize - 1) As Byte
        Dim depthRGBData(task.depthRGB.Total * task.depthRGB.ElemSize - 1) As Byte
        Dim cloudData(task.pointCloud.Total * task.pointCloud.ElemSize - 1) As Byte

        Marshal.Copy(task.color.Data, colorData, 0, colorData.Length)
        Marshal.Copy(task.leftView.Data, leftData, 0, leftData.Length)
        Marshal.Copy(task.rightView.Data, rightData, 0, rightData.Length)
        Marshal.Copy(task.depthRGB.Data, depthRGBData, 0, depthRGBData.Length)
        Marshal.Copy(task.pointCloud.Data, cloudData, 0, cloudData.Length)

        hColor = GCHandle.Alloc(colorData, GCHandleType.Pinned)
        hLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)
        hRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)
        hDepthRGB = GCHandle.Alloc(depthRGBData, GCHandleType.Pinned)
        hCloud = GCHandle.Alloc(cloudData, GCHandleType.Pinned)

        ioIndex = ManagedCPP_Resume(hColor.AddrOfPinnedObject(), hLeft.AddrOfPinnedObject(), hRight.AddrOfPinnedObject(),
                                    hDepthRGB.AddrOfPinnedObject(), hCloud.AddrOfPinnedObject())

        hColor.Free()
        hLeft.Free()
        hRight.Free()
        hDepthRGB.Free()
        hCloud.Free()
    End Sub
    Public Sub Pause()
        Dim ptr As IntPtr = ManagedCPP_Pause(ioIndex)
        Dim pointers(3) As IntPtr
        Marshal.Copy(ptr, pointers, 0, 4)

        task.dst0 = cvb.Mat.FromPixelData(task.dst0.Rows, task.dst0.Cols, cvb.MatType.CV_8UC3, pointers(0)).Clone
        task.dst1 = cvb.Mat.FromPixelData(task.dst1.Rows, task.dst1.Cols, cvb.MatType.CV_8UC3, pointers(1)).Clone
        task.dst2 = cvb.Mat.FromPixelData(task.dst2.Rows, task.dst2.Cols, cvb.MatType.CV_8UC3, pointers(2)).Clone
        task.dst3 = cvb.Mat.FromPixelData(task.dst3.Rows, task.dst3.Cols, cvb.MatType.CV_8UC3, pointers(3)).Clone
    End Sub
End Class
