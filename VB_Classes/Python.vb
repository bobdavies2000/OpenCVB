Imports  System.IO
Imports System.Runtime.InteropServices
Imports  System.IO.MemoryMappedFiles
Imports  System.IO.Pipes
Imports cv = OpenCvSharp
Public Class Python_Basics : Inherits VB_Algorithm
    Public Function StartPython(arguments As String) As Boolean
        Dim pythonApp = New FileInfo(task.pythonTaskName)

        If pythonApp.Exists Then
            task.pythonProcess = New Process
            task.pythonProcess.StartInfo.FileName = "python"
            task.pythonProcess.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                task.pythonProcess.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                task.pythonProcess.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            Console.WriteLine("Starting Python with the following command:" + vbCrLf + task.pythonProcess.StartInfo.Arguments + vbCrLf)
            If task.showConsoleLog = False Then task.pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            Try
                task.pythonProcess.Start()
            Catch ex As Exception
                MsgBox("The python algorithm " + pythonApp.Name + " failed.  Is python in the path?")
            End Try
        Else
            If pythonApp.Name.EndsWith("Python_MemMap") Or pythonApp.Name.EndsWith("Python_Run") Then
                strOut = pythonApp.Name + " is a support algorithm for PyStream apps."
            Else
                strOut = pythonApp.FullName + " is missing."
            End If
            Return False
        End If
        Return True
    End Function
    Public Sub New()
        desc = "Access Python from OpenCVB - contains the startPython interface"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        setTrueText("There is no output from " + traceName + ".  It contains the interface to python.")
    End Sub
End Class






Public Class Python_Run : Inherits VB_Algorithm
    Dim python As New Python_Basics
    Public pyStream As Python_Stream
    Dim pythonApp As FileInfo
    Dim testPyStreamOakD As Boolean = False ' set this to true to test the PyStream problem with the OakD Python camera
    Public Sub OakDPipeIssue()
        setTrueText("Python Stream ('_PS.py') algorithms don't work reliably when using the Oak-D Python camera interface." + vbCrLf +
                    "They both use named pipes to communicate between OpenCVB and the external processes (a camera and a Python algorithm.)" + vbCrLf +
                    "To experiment with Python Stream algorithms, any of the other supported cameras work fine." + vbCrLf +
                    "To see the problem: comment out the camera test in RunVB below to test any '_PS.py' algorithm.  It may work but" + vbCrLf +
                    "if you move the algorithm window (separate from OpenCVB), the algorithm will hang.  More importantly," + vbCrLf +
                    "several of the algorithms just hang without moving the window.  Any suggestions would be gratefully received." + vbCrLf +
                    "Using another camera is the best option to observe all the Python Stream algorithms.")
    End Sub
    Public Sub New()
        If task.pythonTaskName = "" Then task.pythonTaskName = task.homeDir + "VB_Classes/PythonPackages.py"

        pythonApp = New FileInfo(task.pythonTaskName)
        If pythonApp.Name.EndsWith("_PS.py") Then
            If testPyStreamOakD Then
                pyStream = New Python_Stream()
            Else
                If task.cameraName <> "Oak-D camera" Then pyStream = New Python_Stream()
            End If
        Else
            python.StartPython("")
            If python.strOut <> "" Then setTrueText(python.strOut)
        End If
        desc = "Run Python app: " + pythonApp.Name
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.cameraName = "Oak-D camera" And pythonApp.Name.EndsWith("_PS.py") And testPyStreamOakD = False Then
            OakDPipeIssue()
        Else
            If pyStream IsNot Nothing Then
                pyStream.Run(src)
                dst2 = pyStream.dst2
                dst3 = pyStream.dst3
                labels(2) = "Output of Python Backend"
                labels(3) = "Second Output of Python Backend"
            Else
                If pythonApp.Name = "PyStream.py" Then
                    setTrueText("The PyStream.py algorithm is used by a wide variety of apps but has no output when run by itself.")
                End If
            End If
        End If
    End Sub
End Class






Public Class Python_MemMap : Inherits VB_Algorithm
    Dim python As New Python_Basics
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(50 - 1) As Double ' more than we need - buffer for growth.  PyStream assumes 400 bytes length!  Do not change without changing everywhere.
    Public memMapbufferSize As Integer
    Public Sub New()
        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length)

        If standaloneTest() Then
            If task.externalPythonInvocation = False Then
                python.StartPython("--MemMapLength=" + CStr(memMapbufferSize))
                If python.strOut <> "" Then setTrueText(python.strOut)
            End If
            Dim pythonApp = New FileInfo(task.pythonTaskName)
            setTrueText("No output for Python_MemMap - see Python console log (see Options/'Show Console Log for external processes' in the main form)")
            desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            setTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        memMapValues(0) = task.frameCount
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length)
    End Sub
End Class









Public Class Python_Stream : Inherits VB_Algorithm
    Dim python As New Python_Basics
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim dst1Buffer(1) As Byte
    Dim dst2Buffer(1) As Byte
    Dim memMap As Python_MemMap
    Public Sub New()
        task.pipeName = "PyStream2Way" + CStr(pythonPipeIndex)
        pythonPipeIndex += 1
        Try
            task.pythonPipeOut = New NamedPipeServerStream(task.pipeName, PipeDirection.Out)
        Catch ex As Exception
            setTrueText("Python_Stream: pipeOut NamedPipeServerStream failed to open.")
            Exit Sub
        End Try
        task.pythonPipeIn = New NamedPipeServerStream(task.pipeName + "Results", PipeDirection.In)

        ' Was this class invoked standaloneTest()?  Then just run something that works with BGR and depth...
        If task.pythonTaskName.EndsWith("Python_Stream") Then
            task.pythonTaskName = task.homeDir + "VB_Classes/Python_Stream_PS.py"
        End If

        memMap = New Python_MemMap()

        If task.externalPythonInvocation Then
            task.pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            task.pythonReady = python.StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + task.pipeName)
            If python.strOut <> "" Then setTrueText(python.strOut)
        End If
        If task.pythonReady Then
            task.pythonPipeOut.WaitForConnection()
            task.pythonPipeIn.WaitForConnection()
        End If
        labels(2) = "Output of Python Backend"
        desc = "General purpose class to pipe BGR and Depth to Python scripts."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            setTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        If task.pythonReady And task.pcSplit(2).Width > 0 Then
            Dim depth32f As cv.Mat = task.pcSplit(2) * 1000
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, task.frameCount, src.Total * src.ElemSize,
                                                depth32f.Total * depth32f.ElemSize, src.Rows, src.Cols,
                                                task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height)
            Next
            memMap.Run(src)

            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
            If depthBuffer.Length <> depth32f.Total * depth32f.ElemSize Then ReDim depthBuffer(depth32f.Total * depth32f.ElemSize - 1)
            If dst1Buffer.Length <> dst2.Total * dst2.ElemSize Then ReDim dst1Buffer(dst2.Total * dst2.ElemSize - 1)
            If dst2Buffer.Length <> dst3.Total * dst3.ElemSize Then ReDim dst2Buffer(dst3.Total * dst3.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            Marshal.Copy(depth32f.Data, depthBuffer, 0, depthBuffer.Length)
            If task.pythonPipeOut.IsConnected Then
                On Error Resume Next
                task.pythonPipeOut.Write(rgbBuffer, 0, rgbBuffer.Length)
                task.pythonPipeOut.Write(depthBuffer, 0, depthBuffer.Length)
                task.pythonPipeIn.Read(dst1Buffer, 0, dst1Buffer.Length)
                task.pythonPipeIn.Read(dst2Buffer, 0, dst2Buffer.Length)
                Marshal.Copy(dst1Buffer, 0, dst2.Data, dst1Buffer.Length)
                Marshal.Copy(dst2Buffer, 0, dst3.Data, dst2Buffer.Length)
            End If
        End If
    End Sub
    Public Sub Close()
        If task.pythonPipeOut IsNot Nothing Then task.pythonPipeOut.Close()
        If task.pythonPipeIn IsNot Nothing Then task.pythonPipeIn.Close()
    End Sub
End Class