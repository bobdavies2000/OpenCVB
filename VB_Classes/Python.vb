Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports cv = OpenCvSharp

Module Python_Module
    Public Function checkPythonPackage( packageName As String) As Boolean
        If ocvb.parms.PythonExe = "" Then
            ocvb.trueText("Python is not present and needs to be installed." + vbCrLf +
                                                  "Get Python 3.7+ with Visual Studio's Install app.")
            Return False
        End If
        Dim pythonFileInfo = New FileInfo(ocvb.parms.pythonExe)
        Dim packageDir = New FileInfo(pythonFileInfo.DirectoryName + "\Lib\site-packages\")
        Dim packageFolder As New IO.DirectoryInfo(packageDir.DirectoryName + "\")
        Dim packageFiles = packageFolder.GetDirectories(packageName, IO.SearchOption.TopDirectoryOnly)

        If packageFiles.Count = 0 Then
            ocvb.trueText("Python is present but the packages needed by this Python script are not present." + vbCrLf +
                                                  "Use the PythonPackages.py script to show which imports are missing.'" + vbCrLf +
                                                  "Go to the Visual Studio menu 'Tools/Python/Python Environments'" + vbCrLf +
                                                  "Select 'Packages' in the combo box and search for packages required by this script.")
        End If
        Return True
    End Function

    Public Function StartPython( arguments As String) As Boolean
        If checkPythonPackage("cv2") = False Then Return False
        Dim pythonApp = New FileInfo(ocvb.pythonTaskName)

        ' when running the regression tests, some python processes are not completing before the next starts.  Then they build up.  What a mess.  This prevents it
        If ocvb.parms.testAllRunning Then
            For Each p In Process.GetProcesses
                If p.ProcessName.ToUpper.Contains("PYTHON") Then
                    Try
                        ' if it is not our process, we won't be able to kill it.
                        p.Kill()
                    Catch ex As Exception
                        Console.WriteLine("Out of sync 'Test All' tried to kill algorithm that was already terminated.")
                    End Try
                End If
            Next
        End If
        If pythonApp.Exists Then
            Dim p As New Process
            p.StartInfo.FileName = ocvb.parms.PythonExe
            p.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                p.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                p.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            If ocvb.parms.ShowConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If p.Start() = False Then MsgBox("The Python script " + pythonApp.Name + " failed to start")
        Else
            ocvb.trueText(pythonApp.FullName + " is missing.")
            Return False
        End If
        Return True
    End Function
End Module





Public Class Python_Run
    Inherits VBparent
    Dim tryCount As Integer
    Public Sub New()
        initParent()
        If ocvb.pythonTaskName = "" Then ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/PythonPackages.py"
        Dim pythonApp = New FileInfo(ocvb.pythonTaskName)

        If pythonApp.Name.EndsWith("_PS.py") Then
            pyStream = New Python_Stream()
        ElseIf pythonApp.Name.EndsWith("_PS1.py") Then
            pyStream = New Python_Stream1()
        Else
            StartPython("")
        End If
        task.desc = "Run Python app: " + pythonApp.Name
        label1 = ""
        label2 = ""
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If pyStream IsNot Nothing Then
            pyStream.src = src
            pyStream.Run()
            dst1 = pyStream.dst1
            dst2 = pyStream.dst2
            label1 = "Output of Python Backend"
            label2 = "Second Output of Python Backend"
        Else
            Dim proc = Process.GetProcessesByName("python")
            If proc.Count = 0 Then
                If tryCount < 3 Then StartPython("")
                tryCount += 1
            End If
        End If
    End Sub
End Class





Public Class Python_MemMap
    Inherits VBparent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(50 - 1) As Double ' more than we need - buffer for growth.  PyStream assumes 400 bytes length!  Do not change...
    Public memMapbufferSize As Integer
    Public Sub New()
        initParent()
        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If standalone Then
            If ocvb.parms.externalPythonInvocation = False Then
                StartPython("--MemMapLength=" + CStr(memMapbufferSize))
            End If
            Dim pythonApp = New FileInfo(ocvb.pythonTaskName)
            label1 = "No output for Python_MemMap - see Python console"
            task.desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then memMapValues(0) = ocvb.frameCount
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)
    End Sub
End Class





Public Class Python_SurfaceBlit
    Inherits VBparent
    Dim memMap As Python_MemMap
    Dim pipeName As String
    Dim pipe As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim PythonReady As Boolean
    Public Sub New()
        initParent()
        ' this Python script requires pygame to be present...
        If checkPythonPackage("pygame") = False Then
            PythonReady = False
            Exit Sub
        End If
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut)
        PipeTaskIndex += 1

        ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/Python_SurfaceBlit.py"
        memMap = New Python_MemMap()

        If ocvb.parms.externalPythonInvocation Then
            PythonReady = True ' python was already running and invoked OpenCVB.
        Else
            PythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If PythonReady Then pipe.WaitForConnection()
        task.desc = "Stream data to Python_SurfaceBlit Python script."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If PythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Total * src.ElemSize, 0, src.Rows, src.Cols)
            Next
            memMap.Run()

            Dim rgb = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2RGB)
            If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
            Marshal.Copy(rgb.Data, rgbBuffer, 0, rgb.Total * rgb.ElemSize)

            If pipe.IsConnected Then
                On Error Resume Next
                pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            End If
            ocvb.trueText("Blit works fine when here (Python_SurfaceBlit) but the same operation in Python_SurfaceBlit_PS.py fails." + vbCrLf +
                          "The callback in the PyStream interface does not allow the SurfaceBlit API to work." + vbCrLf +
                          "See 'Python_SurfaceBlit.py' to see how the surfaceBlit works then review Python_SurfaceBlit_PS.py failure.")
        Else
            ocvb.trueText("Python is not available")
        End If
    End Sub
End Class








Public Class Python_Stream
    Inherits VBparent
    Dim pipeName As String
    Dim pipeIn As NamedPipeServerStream
    Dim pipeOut As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public Sub New()
        initParent()
        pipeName = "PyStream2Way" + CStr(PipeTaskIndex)
        pipeOut = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        pipeIn = New NamedPipeServerStream(pipeName + "Results", PipeDirection.In)
        PipeTaskIndex += 1

        ' Was this class invoked standalone?  Then just run something that works with RGB and depth...
        If ocvb.pythonTaskName.EndsWith("Python_Stream") Then
            ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/PutText_PS.py"
        End If

        memMap = New Python_MemMap()

        If ocvb.parms.externalPythonInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then
            pipeOut.WaitForConnection()
            pipeIn.WaitForConnection()
        End If
        label1 = "Output of Python Backend"
        task.desc = "General purpose class to pipe RGB and Depth to Python scripts."
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
            If depthBuffer.Length <> task.depth32f.Total * task.depth32f.ElemSize Then ReDim depthBuffer(task.depth32f.Total * task.depth32f.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            Marshal.Copy(task.depth32f.Data, depthBuffer, 0, depthBuffer.Length)
            If pipeOut.IsConnected Then
                On Error Resume Next
                pipeOut.Write(rgbBuffer, 0, rgbBuffer.Length)
                pipeOut.Write(depthBuffer, 0, depthBuffer.Length)
                pipeIn.Read(rgbBuffer, 0, rgbBuffer.Length)
            End If
            Marshal.Copy(rgbBuffer, 0, dst1.Data, rgbBuffer.Length)
        End If
    End Sub
End Class








Public Class Python_Stream1
    Inherits VBparent
    Dim pipeName As String
    Dim pipeIn As NamedPipeServerStream
    Dim dst1Buffer(1) As Byte
    Dim dst2Buffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public Sub New()
        initParent()
        pipeName = "PyStreamResults" + CStr(PipeTaskIndex)
        pipeIn = New NamedPipeServerStream(pipeName, PipeDirection.In)
        PipeTaskIndex += 1

        ' Was this class invoked standalone?  Then just run something that works with RGB and depth...
        If ocvb.pythonTaskName.EndsWith("Python_Stream") Then
            ocvb.pythonTaskName = ocvb.parms.homeDir + "VB_Classes/BG_Subtract_PS1.py"
        End If

        memMap = New Python_MemMap()

        If ocvb.parms.externalPythonInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeIn.WaitForConnection()
        task.desc = "General purpose class to invoke a Python script and get the outputs - dst1 and dst2 - back."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, dst1.Total * dst1.ElemSize,
                                                dst2.Total * dst2.ElemSize, dst1.Rows, dst1.Cols,
                                                task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height)
            Next
            memMap.Run()

            If dst1Buffer.Length <> dst1.Total * dst1.ElemSize Then ReDim dst1Buffer(dst1.Total * dst1.ElemSize - 1)
            If dst2Buffer.Length <> dst2.Total * dst2.ElemSize Then ReDim dst2Buffer(dst2.Total * dst2.ElemSize - 1)
            If pipeIn.IsConnected Then
                On Error Resume Next
                pipeIn.Read(dst1Buffer, 0, dst1Buffer.Length)
                pipeIn.Read(dst2Buffer, 0, dst2Buffer.Length)
            End If
            Marshal.Copy(dst1Buffer, 0, dst1.Data, dst1Buffer.Length)
            Marshal.Copy(dst2Buffer, 0, dst2.Data, dst2Buffer.Length)
            cv.Cv2.ImShow("dst2", dst2)
        End If
    End Sub
End Class