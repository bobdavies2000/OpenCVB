Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO

Module Retina_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Open(rows As integer, cols As integer, useLogSampling As Boolean, samplingFactor As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Retina_Basics_Close(RetinaPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Run(RetinaPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer, magno As IntPtr, useLogSampling As integer) As IntPtr
    End Function
End Module

'https://docs.opencv.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
Public Class Retina_Basics_CPP : Inherits VBparent
    Dim Retina As IntPtr = 0
    Dim startInfo As New ProcessStartInfo
    Dim magnoData(0) As Byte
    Dim srcData(0) As Byte
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Retina Sample Factor", 1, 10, 2)
        End If

        If check.Setup(caller, 2) Then
            check.Box(0).Text = "Use log sampling"
            check.Box(1).Text = "Open resulting xml file"
        End If

        labels(2) = "Retina Parvo"
        labels(3) = "Retina Magno"
        task.desc = "Use the bio-inspired retina algorithm to adjust color and monitor motion."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If check.Box(1).Checked Then
            check.Box(1).Checked = False
            Dim fileinfo = New FileInfo(CurDir() + "/RetinaDefaultParameters.xml")
            If fileinfo.Exists Then
                FileCopy(CurDir() + "/RetinaDefaultParameters.xml", task.parms.homeDir + "data/RetinaDefaultParameters.xml")
                startInfo.FileName = "wordpad.exe"
                startInfo.Arguments = task.parms.homeDir + "Data/RetinaDefaultParameters.xml"
                Process.Start(startInfo)
            Else
                MsgBox("RetinaDefaultParameters.xml should have been created but was not found.  OpenCV error?")
            End If
        End If
        Static useLogSampling As Integer = check.Box(0).Checked
        Static samplingFactor As Single = -1 ' force open
        If useLogSampling <> check.Box(0).Checked Or samplingFactor <> sliders.trackbar(0).Value Then
            If Retina <> 0 Then Retina_Basics_Close(Retina)
            ReDim magnoData(src.Total - 1)
            ReDim srcData(src.Total * src.ElemSize - 1)
            useLogSampling = check.Box(0).Checked
            samplingFactor = sliders.trackbar(0).Value
            If task.parms.testAllRunning = False Then Retina = Retina_Basics_Open(src.Rows, src.Cols, useLogSampling, samplingFactor)
        End If
        Dim handleMagno = GCHandle.Alloc(magnoData, GCHandleType.Pinned)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim magnoPtr As IntPtr = 0
        If task.parms.testAllRunning = False Then
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            magnoPtr = Retina_Basics_Run(Retina, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, handleMagno.AddrOfPinnedObject(), useLogSampling)
        Else
            setTrueText("Retina_Basics_CPP runs fine but during 'Test All' it is not run because it can oversubscribe OpenCL memory.")
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1, 0)
        End If
        handleSrc.Free()
        handleMagno.Free()

        If magnoPtr <> 0 Then
            Dim nextFactor = samplingFactor
            If useLogSampling = False Then nextFactor = 1
            dst2 = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8UC3, magnoPtr).Resize(src.Size())
            dst3 = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8U, magnoData).Resize(src.Size())
        End If
    End Sub
    Public Sub Close()
        If Retina <> 0 Then Retina_Basics_Close(Retina)
    End Sub
End Class






Public Class Retina_Depth : Inherits VBparent
    Dim retina As New Retina_Basics_CPP
    Public Sub New()
        task.desc = "Use the bio-inspired retina algorithm with the depth data."
        labels(2) = "Last result || current result"
        labels(3) = "Current depth motion result"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        retina.RunClass(task.RGBDepth)
        dst3 = retina.dst3
        Static lastMotion As New cv.Mat
        If lastMotion.Width = 0 Then lastMotion = retina.dst3
        cv.Cv2.BitwiseOr(lastMotion, retina.dst3, dst2)
        lastMotion = retina.dst3
    End Sub
End Class


