Imports  cv = OpenCvSharp
Imports ImGuiNET
Imports Veldrid
Imports Veldrid.StartupUtilities
Imports System.Runtime.InteropServices
Imports System.Threading
Public Class ImGui_Basics : Inherits VBparent
#If DEBUG Then
    Public active As Boolean = True
    Public window As Veldrid.Sdl2.Sdl2Window
    Dim gd As Veldrid.GraphicsDevice
    Dim cl As Veldrid.CommandList
    Public controller As Veldrid.ImGuiRenderer
    Public showWindow As Boolean = True
    Public callbackTarget As Action
    Public Sub New()
        Veldrid.StartupUtilities.VeldridStartup.CreateWindowAndGraphicsDevice(
            New Veldrid.StartupUtilities.WindowCreateInfo(task.parms.mainLocation.X + 20, task.parms.mainLocation.Y + 120, task.parms.mainSize.Width,
                                                          task.parms.mainSize.Height, Veldrid.WindowState.Normal, task.algName),
            New Veldrid.GraphicsDeviceOptions(True, Nothing, True), window, gd)
        cl = gd.ResourceFactory.CreateCommandList()
        controller = New Veldrid.ImGuiRenderer(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height)

        task.desc = "Infrastructure for the imgui interface in VB.Net"
    End Sub
    Public Sub doThis(callback As Action)
        If callback IsNot Nothing Then callback()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim snapshot As Veldrid.InputSnapshot = window.PumpEvents()
        If window.Exists = False Then Exit Sub
        controller.Update(1 / 60, snapshot)
        If standalone Then
            ImGui.Text("The ImGui_Basics algorithm is just the infrastructure for other imgui algorithms to use")
        Else
            callbackTarget()
        End If

        cl.Begin()
        cl.SetFramebuffer(gd.MainSwapchain.Framebuffer)
        cl.ClearColorTarget(0, New Veldrid.RgbaFloat(160 / 255, 160 / 255, 192 / 255, 255))
        controller.Render(gd, cl)
        cl.End()
        gd.SubmitCommands(cl)
        gd.SwapBuffers(gd.MainSwapchain)

        If task.optionschanged Then
            Dim hwnd = FindWindow(Nothing, task.algName)
            SetForegroundWindow(hwnd)
        End If
    End Sub
#Else
    Public Sub New()
        task.desc = "Infrastructure for the imgui interface in VB.Net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class







' https://github.com/00sk0/ImGui.Net_Example
Public Class ImGui_BasicsTest : Inherits VBparent
#If DEBUG Then
    Dim basics As New ImGui_Basics
    Public Sub New()
        basics.callbackTarget = AddressOf callback
        task.desc = "Testing the imgui_basics algorithm"
    End Sub
    Public Sub callback()
        ImGui.Begin("Test Window", basics.showWindow)
        ImGui.Text("hello world")
        ImGui.Checkbox("Show another window?", basics.showWindow)

        If basics.showWindow Then
            ImGui.SetNextWindowSize(New System.Numerics.Vector2(128, 128))
            ImGui.SetNextWindowPos(New System.Numerics.Vector2(128, 150))
            ImGui.Begin("another window", basics.showWindow)
            ImGui.Text("This is the second window")
            If ImGui.Button("I got it") Then basics.showWindow = False
            ImGui.End()
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        basics.Run(Nothing)
    End Sub
#Else
    Public Sub New()
        task.desc = "Testing the imgui_basics algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class






' https://github.com/00sk0/ImGui.Net_Example
Public Class ImGui_PlotHistogram : Inherits VBparent
#If DEBUG Then
    Dim basics As New ImGui_Basics
    Dim hist As New Histogram_Basics
    Public Sub New()
        basics.callbackTarget = AddressOf callback
        hist.noZeroEntry = True
        task.desc = "Testing the imgui interface in VB.Net"
    End Sub
    Public Sub callback()
        ImGui.Text("This histogram is not working but it does have the right number of columns.")
        ImGui.Text("Perhaps after using imgui more, some plothistogram examples may pop out.")
        dim mm = vbMinMax(hist.histogram)
        ' ImGui.PlotHistogram("Depth histogram", hist.histogram.Data, hist.histogram.Rows, 0, "", 0, maxVal, New Numerics.Vector2(500, 500), 1)
        ImGui.PlotHistogram("Depth histogram", hist.histogram.Data, hist.histogram.Rows)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist.Run(task.depth32f)

        For i = 0 To hist.histogram.Rows - 1
            hist.histogram.Set(Of Single)(i, 0, i)
        Next

        basics.Run(Nothing)
        setTrueText("Plothistogram API is really not working.  The number of bins is correct - adjust the bins to see it change." + vbCrLf +
                    "But the values are not correct and the sizing of the plot is strange.  See code for testing alternatives.")
    End Sub
#Else
    Public Sub New()
        task.desc = "Testing the imgui interface in VB.Net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class








Public Class ImGui_Example_CS : Inherits VBparent
#If DEBUG Then
    Dim imguiCS As New CS_Classes.imgui_example
    Public Sub New()
        imguiCS.initialize()
        task.desc = "Using imgui interface in some C# code"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        imguiCS.RunCS()
    End Sub
    Public Sub Close()
        imguiCS.cleanup()
    End Sub
#Else
    Public Sub New()
        task.desc = "Using imgui interface in some C# code"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class









Module imgui_Example_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function imgui_Example_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub imgui_Example_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function imgui_Example_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module






Public Class ImGui_Example_CPP : Inherits VBparent
#If DEBUG Then
    Dim cPtr As IntPtr
    Dim context As IntPtr
    Dim io As ImGuiIOPtr
    Public Sub New()
        context = ImGui.CreateContext()
        io = ImGui.GetIO()
        cPtr = imgui_Example_Open()
        task.desc = "Trying to use the C++ interface to imgui but this is not working..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        'Dim input = src
        'If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        'Dim dataSrc(input.Total * input.ElemSize - 1) As Byte
        'Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
        'Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        'Dim imagePtr = imgui_Example_Run(cPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, input.Channels)
        'handleSrc.Free()

        'If imagePtr <> 0 Then
        '    Dim dstData(input.Total * input.ElemSize - 1) As Byte
        '    Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
        '    'dst2 = New cv.Mat(input.Rows, input.Cols, If(input.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), dstData)
        'End If
        setTrueText("This example is not working.  Need help figuring out how to get access to the C++ imgui interface." + vbCrLf +
                    "See the C++ code referenced above to see the problem.")
    End Sub
    Public Sub Close()
        imgui_Example_Close(cPtr)
    End Sub
#Else
    Public Sub New()
        task.desc = "Trying to use the C++ interface to imgui but this is not working..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class








' https://github.com/mellinoe/ImGui.NET/tree/master/src/ImGui.NET.SampleProgram
Public Class ImGui_MemoryEditor : Inherits VBparent
#If DEBUG Then
    Dim memEdit As New CS_Classes.imgui_MemoryEditor
    Public Sub New()
        memEdit.initialize()
        task.desc = "Incorporate the C# memory editor sample program from imgui.net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        memEdit.RunCS()
        If task.optionschanged Then
            Dim hwnd = FindWindow(Nothing, "ImGui.NET Sample Program")
            SetForegroundWindow(hwnd)
        End If
    End Sub
    Public Sub Close()
        memEdit.cleanup()
    End Sub
#Else
    Public Sub New()
        task.desc = "Incorporate the C# memory editor sample program from imgui.net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("imgui algorithms are only available in DEBUG mode - not in the RELEASE mode - due to rules about unsafe code for VB.Net")
    End Sub
#End If
End Class








' https://github.com/mellinoe/ImGui.NET/tree/master/src
' Cutting losses here.  The code works but the algorithm cannot be restarted.  It fails reallocating the graphics device.
' Leaving this in would mean failure on the regression testing. Switching back from other algorithms also fails.
'Public Class ImGui_XNA_CS : Inherits VBparent
'    Dim XNAgame As New CS_Classes.imgui_XNA
'    Dim XNAHandle As Thread = Nothing
'    Public Sub New()
'        task.desc = "Porting the XNA sample program from imgui.net"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        If XNAHandle Is Nothing Then
'            XNAHandle = New Thread(AddressOf XNAgame.Run)
'            XNAHandle.Name = "XNA Game Task"
'            XNAHandle.Start()
'        End If
'    End Sub
'    Public Sub Close()
'        XNAgame.Close()
'        XNAHandle = Nothing
'    End Sub
'End Class
