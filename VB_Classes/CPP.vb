Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text
Public Class CPP_Basics : Inherits VB_Algorithm
    Public cppFunction As Integer
    Public result As cv.Mat
    Public neighbors As New List(Of cv.Point2f)
    Public neighborIndexToTrain As List(Of Integer)
    Dim taskList As New List(Of Integer)
    Public Sub New(_cppFunction As Integer)
        updateFunction(_cppFunction)
    End Sub
    Public Sub updateFunction(_cppFunction As Integer)
        cppFunction = _cppFunction
        labels(2) = "Running CPP_Basics, Output from " + task.algName

        cPtr = cppTask_Open(cppFunction, task.workingRes.Height, task.workingRes.Width,
                            heartBeat(), task.AddWeighted, task.lineWidth, task.lineType, task.dotSize,
                            gOptions.GridSize.Value, task.histogramBins,
                            gOptions.gravityPointCloud.Checked, gOptions.PixelDiffThreshold.Value,
                            gOptions.UseKalman.Checked, gOptions.Palettes.SelectedIndex, task.optionsChanged,
                            task.historyCount, task.clickPoint.X,
                            task.clickPoint.Y, task.mouseClickFlag,
                            task.mousePicTag, task.mouseMovePoint.X, task.mouseMovePoint.Y,
                            task.mouseMovePointUpdated,
                            task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height,
                            gOptions.displayDst0.Checked, gOptions.displayDst1.Checked)

        getOptions()
    End Sub
    Private Sub getOptions()
        Dim labelBuffer As StringBuilder = New StringBuilder(512)
        Dim buffer As StringBuilder = New StringBuilder(512)
        cppTask_OptionsCPPtoVB(cPtr, gOptions.GridSize.Value,
                               gOptions.HistBinSlider.Value,
                               gOptions.PixelDiffThreshold.Value, gOptions.UseKalman.Checked,
                               task.historyCount, task.drawRect.X, task.drawRect.Y,
                               task.drawRect.Width, task.drawRect.Height, labelBuffer, buffer)

        labels = labelBuffer.ToString.Split("|")
        desc = buffer.ToString
    End Sub
    Public Sub New()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim pointCloudData(task.pointCloud.Total * task.pointCloud.ElemSize - 1) As Byte
        Marshal.Copy(task.pointCloud.Data, pointCloudData, 0, pointCloudData.Length)
        Dim handlePointCloud = GCHandle.Alloc(pointCloudData, GCHandleType.Pinned)
        Dim pcPtr = cppTask_PointCloud(cPtr, handlePointCloud.AddrOfPinnedObject(),
                                       task.pointCloud.Rows, task.pointCloud.Cols)
        handlePointCloud.Free()
        If task.algName.StartsWith("CPP_") Then
            task.depthRGB = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, pcPtr)
        End If
        cppTask_OptionsVBtoCPP(cPtr, gOptions.GridSize.Value,
                               gOptions.HistBinSlider.Value,
                               gOptions.PixelDiffThreshold.Value, gOptions.UseKalman.Checked,
                               task.historyCount,
                               task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height,
                               task.lineWidth, task.lineType)


        Dim inputImage(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, inputImage, 0, inputImage.Length)
        Dim handleInput = GCHandle.Alloc(inputImage, GCHandleType.Pinned)
        Dim imagePtr = cppTask_RunCPP(cPtr, handleInput.AddrOfPinnedObject(), src.Channels, task.frameCount, dst2.Rows, dst2.Cols,
                                      task.accRadians.X, task.accRadians.Y, task.accRadians.Z,
                                      task.optionsChanged,
                                      heartBeat(), gOptions.displayDst0.Checked, gOptions.displayDst1.Checked,
                                      task.AddWeighted)
        handleInput.Free()
        getOptions()

        If imagePtr <> 0 Then
            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
            Dim dst3Ptr = cppTask_GetDst3(cPtr)
            dst3 = New cv.Mat(dst0.Rows, dst0.Cols, cv.MatType.CV_8UC3, dst3Ptr)
        Else
            setTrueText("The C++ algorithm interface appears to be failing!")
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = cppTask_Close(cPtr)
    End Sub
End Class







Module CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_Open(cppFunction As Integer, rows As Integer, cols As Integer,
                                 heartBeat As Boolean, addWeighted As Single, lineWidth As Integer,
                                 lineType As Integer, dotSize As Integer, gridSize As Integer,
                                 histogramBins As Integer, ocvheartBeat As Boolean, gravityPointCloud As Boolean,
                                 pixelDiffThreshold As Integer, useKalman As Boolean, paletteIndex As Integer,
                                 frameHistory As Integer, clickX As Integer,
                                 clickY As Integer, clickFlag As Boolean, picTag As Integer, moveX As Integer,
                                 moveY As Integer, moveUpdated As Boolean, rectX As Integer, rectY As Integer,
                                 rectWidth As Integer, rectHeight As Integer, displayDst0 As Boolean,
                                 displayDst1 As Boolean) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, channels As Integer, frameCount As Integer,
                                   rows As Integer, cols As Integer, x As Single, y As Single, z As Single,
                                   optionsChanged As Boolean, heartBeat As Boolean, displayDst0 As Boolean,
                                   displayDst1 As Boolean, addWeighted As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_PointCloud(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function cppTask_GetDst3(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub cppTask_OptionsCPPtoVB(cPtr As IntPtr, ByRef gridSize As Integer,
                                        ByRef histogramBins As Integer,
                                        ByRef pixelDiffThreshold As Integer,
                                        ByRef useKalman As Boolean, ByRef frameHistory As Integer,
                                        ByRef rectX As Integer, ByRef rectY As Integer, ByRef rectWidth As Integer,
                                        ByRef rectHeight As Integer,
                                        <MarshalAs(UnmanagedType.LPStr)> ByVal labels As StringBuilder,
                                        <MarshalAs(UnmanagedType.LPStr)> ByVal desc As StringBuilder)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub cppTask_OptionsVBtoCPP(cPtr As IntPtr, ByRef gridSize As Integer,
                                        ByRef histogramBins As Integer,
                                        ByRef pixelDiffThreshold As Integer,
                                        ByRef useKalman As Boolean, ByRef frameHistory As Integer,
                                        ByRef rectX As Integer, ByRef rectY As Integer, ByRef rectWidth As Integer,
                                        ByRef rectHeight As Integer, ByRef lineWidth As Integer, ByRef lineType As Integer)
    End Sub
End Module 