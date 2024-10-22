Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports  System.IO
'https://docs.opencvb.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
Public Class Retina_Basics_CPP_VB : Inherits TaskParent
    Dim startInfo As New ProcessStartInfo
    Dim magnoData(0) As Byte
    Dim dataSrc(0) As Byte
    Dim samplingFactor As Single = -1 ' force open
    Dim options As New Options_Retina
    Dim saveUseLogSampling As Boolean
    Public Sub New()
        labels(2) = "Retina Parvo"
        labels(3) = "Retina Magno"
        desc = "Use the bio-inspired retina algorithm to adjust color and monitor motion."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If options.xmlCheck Then
            Dim fileinfo = New FileInfo(CurDir() + "/RetinaDefaultParameters.xml")
            If fileinfo.Exists Then
                FileCopy(CurDir() + "/RetinaDefaultParameters.xml", task.HomeDir + "data/RetinaDefaultParameters.xml")
                startInfo.FileName = "wordpad.exe"
                startInfo.Arguments = task.HomeDir + "Data/RetinaDefaultParameters.xml"
                Process.Start(startInfo)
            Else
                MsgBox("RetinaDefaultParameters.xml should have been created but was not found.  OpenCV error?")
            End If
        End If
        If saveUseLogSampling <> options.useLogSampling Or samplingFactor <> options.sampleFactor Then
            If cPtr <> 0 Then Retina_Basics_Close(cPtr)
            ReDim magnoData(src.Total - 1)
            ReDim dataSrc(src.Total * src.ElemSize - 1)
            saveUseLogSampling = options.useLogSampling
            samplingFactor = options.sampleFactor
            If task.testAllRunning = False Then cPtr = Retina_Basics_Open(src.Rows, src.Cols, options.useLogSampling, samplingFactor)
        End If
        Dim handleMagno = GCHandle.Alloc(magnoData, GCHandleType.Pinned)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr As IntPtr = 0
        If task.testAllRunning = False Then
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            imagePtr = Retina_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                         handleMagno.AddrOfPinnedObject(), options.useLogSampling)
        Else
            SetTrueText("Retina_Basics_CPP runs fine but during 'Test All' it is not run because it can oversubscribe OpenCL memory.")
            dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        End If
        handleSrc.Free()
        handleMagno.Free()

        If imagePtr <> 0 Then
            Dim nextFactor = samplingFactor
            If options.useLogSampling = False Then nextFactor = 1
            dst2 = cvb.Mat.FromPixelData(src.Rows / nextFactor, src.Cols / nextFactor, cvb.MatType.CV_8UC3, imagePtr).Resize(src.Size()).Clone
            dst3 = cvb.Mat.FromPixelData(src.Rows / nextFactor, src.Cols / nextFactor, cvb.MatType.CV_8U, magnoData).Resize(src.Size())
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Retina_Basics_Close(cPtr)
    End Sub
End Class






Public Class Retina_Depth : Inherits TaskParent
    Dim retina As New Retina_Basics_CPP_VB
    Dim lastMotion As New cvb.Mat
    Public Sub New()
        desc = "Use the bio-inspired retina algorithm with the depth data."
        labels(2) = "Last result || current result"
        labels(3) = "Current depth motion result"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        retina.Run(task.depthRGB)
        dst3 = retina.dst3
        If lastMotion.Width = 0 Then lastMotion = retina.dst3
        dst2 = lastMotion Or retina.dst3
        lastMotion = retina.dst3
    End Sub
End Class


