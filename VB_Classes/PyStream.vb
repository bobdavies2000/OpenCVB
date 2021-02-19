Imports System.Runtime.InteropServices
Imports System.IO.Pipes
Imports cv = OpenCvSharp
Public Class PyStream_Basics
    Inherits VBparent
    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public Sub New()
        initParent()
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        PipeTaskIndex += 1

        ' Was this class invoked standalone?  Then just run something that works with RGB and depth...
        If ocvb.pythonTaskName.EndsWith("PyStream_Basics") Then
            ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/AddWeighted_PS.py"
        End If

        memMap = New Python_MemMap()

        If ocvb.parms.externalPythonInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        task.desc = "General purpose class to pipe RGB and Depth to Python scripts."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Total * src.ElemSize,
                                                task.depth32f.Total * task.depth32f.ElemSize, src.Rows, src.Cols)
            Next
            memMap.Run()

            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
            If depthBuffer.Length <> task.depth32f.Total * task.depth32f.ElemSize Then ReDim depthBuffer(task.depth32f.Total * task.depth32f.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            Marshal.Copy(task.depth32f.Data, depthBuffer, 0, depthBuffer.Length)
            If pipeImages.IsConnected Then
                On Error Resume Next
                pipeImages.Write(rgbBuffer, 0, rgbBuffer.Length)
                If pipeImages.IsConnected Then pipeImages.Write(depthBuffer, 0, depthBuffer.Length)
            End If
        End If

        If ocvb.pythonTaskName.EndsWith("Python_SurfaceBlit_PS.py") Then
            ocvb.trueText("Blit works fine when using the Python_SurfaceBlit VB.Net algorithm but the same operation in Python_SurfaceBlit_PS.py fails." + vbCrLf +
                          "The callback in the PyStream interface does not allow the SurfaceBlit API to work." + vbCrLf +
                          "See 'Python_SurfaceBlit.py' to see how the surfaceBlit works then review Python_SurfaceBlit_PS.py failure.")
        End If
    End Sub
End Class









Public Class PyStream_2
    Inherits VBparent
    Dim pipeName As String
    Dim pipeIn As NamedPipeServerStream
    Dim pipeOut As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim pythonReady As Boolean
    Public memMap As Python_MemMap
    Public Sub New()
        initParent()
        pipeName = "PyStream2Way" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipeOut = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        pipeIn = New NamedPipeServerStream(pipeName + "Results", PipeDirection.In)

        ' Was this class invoked standalone?  Then just run something we know works
        If ocvb.pythonTaskName.EndsWith("PyStream_2") Then
            ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/PutText_PS2.py"
        End If

        memMap = New Python_MemMap()

        If ocvb.parms.externalPythonInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(" --MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then
            pipeOut.WaitForConnection()
            pipeIn.WaitForConnection()
        End If
        label1 = "Output of Python Backend"
        task.desc = "General purpose class to pipe RGB and Depth to Python scripts and get results back."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Total * src.ElemSize,
                                                task.depth32f.Total * task.depth32f.ElemSize, src.Rows, src.Cols,
                                                task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height)
            Next
            memMap.Run()

            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            If pipeOut.IsConnected Then
                On Error Resume Next
                pipeOut.Write(rgbBuffer, 0, rgbBuffer.Length)
                pipeIn.Read(rgbBuffer, 0, rgbBuffer.Length)
            End If
            Marshal.Copy(rgbBuffer, 0, dst1.Data, rgbBuffer.Length)
        End If
    End Sub
End Class
