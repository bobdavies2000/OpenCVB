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






<StructLayout(LayoutKind.Sequential)>
Public Class VB_to_ManagedCPP
    Public color As IntPtr
    Public leftview As IntPtr
    Public rightview As IntPtr
    Public depthRGB As IntPtr
    Public pointCloud As IntPtr
    Public rows As Integer
    Public cols As Integer
End Class





Public Class CPP_Managed
    Public color As cvb.Mat
    Public depthRGB As cvb.Mat
    Public leftView As cvb.Mat
    Public rightView As cvb.Mat
    Public pointCloud As cvb.Mat

    Public cols As Integer
    Public rows As Integer

    Public dst0 As cvb.Mat
    Public dst1 As cvb.Mat
    Public dst2 As cvb.Mat
    Public dst3 As cvb.Mat
    Public Sub New()
        ' This interface is called from the C++/CLR algorithms to build the task structure in C++/CLR.
        ' This is not an algorithm but an interface to the Managed C++ code.
    End Sub
    Public Function resumeTask() As VB_to_ManagedCPP
        Dim vbData As New VB_to_ManagedCPP
        vbData.color = task.color.Data
        vbData.leftview = task.leftView.Data
        vbData.rightview = task.rightView.Data
        vbData.depthRGB = task.depthRGB.Data
        vbData.pointCloud = task.pointCloud.Data
        vbData.rows = task.dst0.Rows
        vbData.cols = task.dst0.Cols
        Return vbData
    End Function
    Public Sub pauseTask(ptr0 As IntPtr, ptr1 As IntPtr, ptr2 As IntPtr, ptr3 As cvb.Mat)
        'task.dst0 = New cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, ptr0)
        'task.dst1 = New cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, ptr1)
        task.dst2 = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, ptr2)
        'task.dst3 = New cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, ptr3)
    End Sub
End Class


Module managedCPP_Interface
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ManagedCPP_Resume(rows As Integer, cols As Integer, colorPtr As IntPtr, leftPtr As IntPtr, rightPtr As IntPtr,
                                depthRGBPtr As IntPtr, cloud As IntPtr)
    End Sub

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ManagedCPP_Pause() As IntPtr
    End Function
End Module



Public Class CPP_ManagedTest : Inherits VB_Parent
    Dim hColor As GCHandle
    Dim hLeft As GCHandle
    Dim hRight As GCHandle
    Dim hDepthRGB As GCHandle
    Dim hCloud As GCHandle
    Public Sub New()
        desc = "Move data to the Managed C++/CLR code (CPP_Classes)"
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

        ManagedCPP_Resume(src.Rows, src.Cols, hColor.AddrOfPinnedObject(), hLeft.AddrOfPinnedObject(), hRight.AddrOfPinnedObject(),
                          hDepthRGB.AddrOfPinnedObject(), hCloud.AddrOfPinnedObject())

        hColor.Free()
        hLeft.Free()
        hRight.Free()
        hDepthRGB.Free()
        hCloud.Free()
    End Sub
    Public Sub Pause()
        task.dst2 = cvb.Mat.FromPixelData(task.dst2.Rows, task.dst2.Cols, cvb.MatType.CV_8UC3, ManagedCPP_Pause()).Clone
    End Sub
End Class
