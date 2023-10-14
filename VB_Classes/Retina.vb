Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports  System.IO

Module Retina_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Open(rows As integer, cols As integer, useLogSampling As Boolean, samplingFactor As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Run(RetinaPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer, magno As IntPtr, useLogSampling As integer) As IntPtr
    End Function
End Module

'https://docs.opencv.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
Public Class Retina_Basics_CPP : Inherits VB_Algorithm
    Dim startInfo As New ProcessStartInfo
    Dim magnoData(0) As Byte
    Dim dataSrc(0) As Byte
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Retina Sample Factor", 1, 10, 2)
        If check.Setup(traceName) Then
            check.addCheckBox("Use log sampling")
            check.addCheckBox("Open resulting xml file")
        End If

        labels(2) = "Retina Parvo"
        labels(3) = "Retina Magno"
        desc = "Use the bio-inspired retina algorithm to adjust color and monitor motion."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static sampleSlider = findSlider("Retina Sample Factor")
        Static logCheck = findCheckBox("Use log sampling")
        Static xmlCheck = findCheckBox("Open resulting xml file")
        If xmlCheck.Checked Then
            xmlCheck.Checked = False
            Dim fileinfo = New FileInfo(CurDir() + "/RetinaDefaultParameters.xml")
            If fileinfo.Exists Then
                FileCopy(CurDir() + "/RetinaDefaultParameters.xml", task.homeDir + "data/RetinaDefaultParameters.xml")
                startInfo.FileName = "wordpad.exe"
                startInfo.Arguments = task.homeDir + "Data/RetinaDefaultParameters.xml"
                Process.Start(startInfo)
            Else
                MsgBox("RetinaDefaultParameters.xml should have been created but was not found.  OpenCV error?")
            End If
        End If
        Static useLogSampling As Integer = logCheck.Checked
        Static samplingFactor As Single = -1 ' force open
        If useLogSampling <> logCheck.Checked Or samplingFactor <> sampleSlider.Value Then
            If cPtr <> 0 Then Retina_Basics_Close(cPtr)
            ReDim magnoData(src.Total - 1)
            ReDim dataSrc(src.Total * src.ElemSize - 1)
            useLogSampling = logCheck.Checked
            samplingFactor = sampleSlider.Value
            If task.testAllRunning = False Then cPtr = Retina_Basics_Open(src.Rows, src.Cols, useLogSampling, samplingFactor)
        End If
        Dim handleMagno = GCHandle.Alloc(magnoData, GCHandleType.Pinned)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr As IntPtr = 0
        If task.testAllRunning = False Then
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            imagePtr = Retina_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, handleMagno.AddrOfPinnedObject(), useLogSampling)
        Else
            setTrueText("Retina_Basics_CPP runs fine but during 'Test All' it is not run because it can oversubscribe OpenCL memory.")
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1, 0)
        End If
        handleSrc.Free()
        handleMagno.Free()

        If imagePtr <> 0 Then
            Dim nextFactor = samplingFactor
            If useLogSampling = False Then nextFactor = 1
            dst2 = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8UC3, imagePtr).Resize(src.Size()).Clone
            dst3 = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8U, magnoData).Resize(src.Size())
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Retina_Basics_Close(cPtr)
    End Sub
End Class






Public Class Retina_Depth : Inherits VB_Algorithm
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        desc = "Use the bio-inspired retina algorithm with the depth data."
        labels(2) = "Last result || current result"
        labels(3) = "Current depth motion result"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        retina.Run(task.depthRGB)
        dst3 = retina.dst3
        Static lastMotion As New cv.Mat
        If lastMotion.Width = 0 Then lastMotion = retina.dst3
        dst2 = lastMotion Or retina.dst3
        lastMotion = retina.dst3
    End Sub
End Class


