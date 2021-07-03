Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports DlibDotNet
Imports cv = OpenCvSharp

Module Python_Module
    Public Function StartPython(arguments As String) As Boolean
        Dim pythonApp = New FileInfo(task.pythonTaskName)

        If pythonApp.Name.StartsWith("LRS_") Then
            If task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Or task.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D455 Then
                task.trueText("The current OpenCVB camera is an Intel RealSense camera and it is also used by this Python script." + vbCrLf +
                              "The Python script will only run when using a non-RealSense camera in OpenCVB")
                Return False
            End If
        End If

        If pythonApp.Exists Then
            Dim p As New Process
            p.StartInfo.FileName = task.parms.PythonExe
            p.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                p.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                p.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            If task.parms.ShowConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If p.Start() = False Then MsgBox("The Python script " + pythonApp.Name + " failed to start")
        Else
            If pythonApp.Name.EndsWith("Python_MemMap") Or pythonApp.Name.EndsWith("Python_Run") Then
                task.trueText(pythonApp.Name + " is a support algorithm for PyStream apps.")
            Else
                task.trueText(pythonApp.FullName + " is missing.")
            End If
            Return False
        End If
        Return True
    End Function
End Module





Public Class Python_Run : Inherits VBparent
    Public Sub New()
        If task.pythonTaskName = "" Then task.pythonTaskName = task.parms.homeDir + "VB_Classes/PythonPackages.py"
        Dim pythonApp = New FileInfo(task.pythonTaskName)
        If pythonApp.Name.EndsWith("_PS.py") Then
            pyStream = New Python_Stream()
        Else
            StartPython("")
        End If
        task.desc = "Run Python app: " + pythonApp.Name
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If pyStream IsNot Nothing Then
            pyStream.RunClass(src)
            dst2 = pyStream.dst2
            dst3 = pyStream.dst3
            labels(2) = "Output of Python Backend"
            labels(3) = "Second Output of Python Backend"
        Else
            Dim pythonApp = New FileInfo(task.pythonTaskName)
            If pythonApp.Name.StartsWith("OakD") Then
                setTrueText("The " + pythonApp.Name + " python script is merely a placeholder while working on the OakD support " + vbCrLf +
                          "It can only be run outside of OpenCVB.")
                Exit Sub
            End If
            If pythonApp.Name = "PyStream.py" Then
                setTrueText("The PyStream.py algorithm is used by a wide variety of apps but has no output when run by itself.")
            End If
            Dim proc = Process.GetProcessesByName("python")
        End If
    End Sub
End Class





Public Class Python_MemMap : Inherits VBparent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(50 - 1) As Double ' more than we need - buffer for growth.  PyStream assumes 400 bytes length!  Do not change...
    Public memMapbufferSize As Integer
    Public Sub New()
        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If standalone Then
            If task.parms.externalPythonInvocation = False Then
                StartPython("--MemMapLength=" + CStr(memMapbufferSize))
            End If
            Dim pythonApp = New FileInfo(task.pythonTaskName)
            labels(2) = "No output for Python_MemMap - see Python console"
            task.desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then memMapValues(0) = task.frameCount
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)
    End Sub
End Class





'Public Class Python_SurfaceBlit : Inherits VBparent
'    Dim memMap As Python_MemMap
'    Dim pipeName As String
'    Dim pipe As NamedPipeServerStream
'    Dim rgbBuffer(1) As Byte
'    Dim PythonReady As Boolean
'    Public Sub New()
'        pipeName = "OpenCVBImages"
'        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut)

'        task.pythonTaskName = task.parms.homeDir + "VB_Classes/Python_SurfaceBlit.py"
'        memMap = New Python_MemMap()

'        If task.parms.externalPythonInvocation Then
'            PythonReady = True ' python was already running and invoked OpenCVB.
'        Else
'            PythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
'        End If
'        If PythonReady Then pipe.WaitForConnection()
'        task.desc = "Stream data to Python_SurfaceBlit Python script."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        If PythonReady Then
'            For i = 0 To memMap.memMapValues.Length - 1
'                memMap.memMapValues(i) = Choose(i + 1, task.frameCount, src.Total * src.ElemSize, 0, src.Rows, src.Cols)
'            Next
'            memMap.RunClass(src)

'            Dim rgb = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2RGB)
'            If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
'            Marshal.Copy(rgb.Data, rgbBuffer, 0, rgb.Total * rgb.ElemSize)

'            If pipe.IsConnected Then
'                On Error Resume Next
'                pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
'            End If
'            setTrueText("Blit works fine when here (Python_SurfaceBlit) but the same operation in Python_SurfaceBlit_PS.py fails." + vbCrLf +
'                          "The callback in the PyStream interface does not allow the SurfaceBlit API to work." + vbCrLf +
'                          "See 'Python_SurfaceBlit.py' to see how the surfaceBlit works then review Python_SurfaceBlit_PS.py failure.")
'        Else
'            setTrueText("Python is not available")
'        End If
'    End Sub
'End Class








Public Class Python_Stream : Inherits VBparent
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim dst1Buffer(1) As Byte
    Dim dst2Buffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public Sub New()
        task.pipeName = "PyStream2Way" + CStr(task.pipeIndex)
        Try
            task.pipeOut = New NamedPipeServerStream(task.pipeName, PipeDirection.Out)
        Catch ex As Exception
            task.pipeIndex += 1
            task.pipeName = "PyStream2Way" + CStr(task.pipeIndex) ' try another name 
            task.pipeOut = New NamedPipeServerStream(task.pipeName, PipeDirection.Out)
        End Try
        task.pipeIn = New NamedPipeServerStream(task.pipeName + "Results", PipeDirection.In)

        ' Was this class invoked standalone?  Then just run something that works with RGB and depth...
        If task.pythonTaskName.EndsWith("Python_Stream") Then
            task.pythonTaskName = task.parms.homeDir + "VB_Classes/Python_Stream_PS.py"
        End If

        memMap = New Python_MemMap()

        If task.parms.externalPythonInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            Dim testallStr = " --TestAllRunning=" + If(task.parms.testAllRunning, "1", "0")
            pythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + testallStr + " --pipeName=" + task.pipeName)
        End If
        If pythonReady Then
            task.pipeOut.WaitForConnection()
            task.pipeIn.WaitForConnection()
        End If
        labels(2) = "Output of Python Backend"
        task.desc = "General purpose class to pipe RGB and Depth to Python scripts."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, task.frameCount, src.Total * src.ElemSize,
                                                task.depth32f.Total * task.depth32f.ElemSize, src.Rows, src.Cols,
                                                task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height)
            Next
            memMap.RunClass(src)

            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
            If depthBuffer.Length <> task.depth32f.Total * task.depth32f.ElemSize Then ReDim depthBuffer(task.depth32f.Total * task.depth32f.ElemSize - 1)
            If dst1Buffer.Length <> dst2.Total * dst2.ElemSize Then ReDim dst1Buffer(dst2.Total * dst2.ElemSize - 1)
            If dst2Buffer.Length <> dst3.Total * dst3.ElemSize Then ReDim dst2Buffer(dst3.Total * dst3.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            Marshal.Copy(task.depth32f.Data, depthBuffer, 0, depthBuffer.Length)
            If task.pipeOut.IsConnected Then
                On Error Resume Next
                task.pipeOut.Write(rgbBuffer, 0, rgbBuffer.Length)
                task.pipeOut.Write(depthBuffer, 0, depthBuffer.Length)
                task.pipeIn.Read(dst1Buffer, 0, dst1Buffer.Length)
                task.pipeIn.Read(dst2Buffer, 0, dst2Buffer.Length)
            End If
            Marshal.Copy(dst1Buffer, 0, dst2.Data, dst1Buffer.Length)
            Marshal.Copy(dst2Buffer, 0, dst3.Data, dst2Buffer.Length)
        End If
    End Sub
End Class
