Imports OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices

Public Class Translator
#Region "NonVolatile"

    <DllImport("user32.dll")>
    Private Shared Function SetCursorPos(x As Integer, y As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Sub mouse_event(dwFlags As Integer, dx As Integer, dy As Integer, cButtons As Integer, dwExtraInfo As Integer)
    End Sub

    Private Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
    Private Const MOUSEEVENTF_LEFTUP As Integer = &H4
    Dim saveTask As Object
    Dim clipLines As String = ""
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        XYLoc.Text = "X = " + CStr(Cursor.Position.X) + ", Y = " + CStr(Cursor.Position.Y)
    End Sub
    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs) Handles WebView.CoreWebView2InitializationCompleted
        'subscribe to CoreWebView2 event(s) (add event handlers) 
        AddHandler WebView.CoreWebView2.HistoryChanged, AddressOf CoreWebView2_HistoryChanged
        For i = 0 To Main_UI.AvailableAlgorithms.Items.Count - 1
            Algorithms.Items.Add(Main_UI.AvailableAlgorithms.Items(i))
        Next
    End Sub
    Private Sub CoreWebView2_HistoryChanged(sender As Object, e As Object)
        Debug.WriteLine("CoreWebView2_HistoryChanged")
    End Sub
    Private Async Function InitializeAsync() As Task
        Await WebView.EnsureCoreWebView2Async()
        WebView.CoreWebView2.Navigate("https://www.codeconvert.ai/app")
    End Function
    Private Sub PerformMouseClick(ByVal clickType As String)
        Dim x As Integer = Cursor.Position.X
        Dim y As Integer = Cursor.Position.Y
        Select Case clickType
            Case "LeftClick"
                mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero)
                mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero)
            Case "RClick"
                ' Implement right click logic (similar to left click)
        End Select
    End Sub
    Private Sub setInputLanguage(langStr As String)
        SetCursorPos(385, 385)
        PerformMouseClick("LeftClick")

        SendKeys.Send(langStr)
        Application.DoEvents()
        SendKeys.Send(vbCrLf)
    End Sub
    Private Sub setOutputLanguage(langStr As String)
        SetCursorPos(815, 385)
        PerformMouseClick("LeftClick")

        SendKeys.Send(langStr)
        Application.DoEvents()
        SendKeys.Send(vbCrLf)
    End Sub
    Private Async Sub Translate_Click(sender As Object, e As EventArgs) Handles translate.Click
        Dim script = "document.getElementById('convert-btn').click();"
        Await WebView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub
    Private Sub VBtoCSharp_CheckedChanged(sender As Object, e As EventArgs) Handles VBtoCSharp.CheckedChanged
        'setInputLanguage("VB")
        'setOutputLanguage("Csharp")
        Main_UI.settings.translatorMode = "VB.Net to C#"
    End Sub
    Private Sub CsharpToCPP_CheckedChanged(sender As Object, e As EventArgs) Handles CsharpToCPP.CheckedChanged
        'setInputLanguage("VB")
        'setOutputLanguage("C++")
        Main_UI.settings.translatorMode = "C# to C++"
    End Sub
    Private Sub CsharpToVB_CheckedChanged(sender As Object, e As EventArgs) Handles CsharpToVB.CheckedChanged
        'setInputLanguage("Csharp")
        'setOutputLanguage("VB")
        Main_UI.settings.translatorMode = "C# to VB.Net (back)"
    End Sub
    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        Timer4.Enabled = False
        Select Case Main_UI.settings.translatorMode
            Case "VB.Net to C#"
                VBtoCSharp.Checked = True
                VBtoCSharp_CheckedChanged(sender, e)
            Case "C# to C++"
                CsharpToCPP.Checked = True
                CsharpToCPP_CheckedChanged(sender, e)
            Case "VB.Net to C++ (back)"
                CsharpToVB.Checked = True
                CsharpToVB_CheckedChanged(sender, e)
        End Select
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Algorithms.SelectedIndex = Main_UI.AvailableAlgorithms.SelectedIndex
    End Sub
#End Region
    Private Sub Translator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim saveTask = InitializeAsync()
        Me.Top = 0
        Me.Left = 0
        XYLoc.Left = 10
        XYLoc.Top = WebView.Top + WebView.Height + 3
        Me.WindowState = FormWindowState.Maximized
        Timer4.Enabled = True
    End Sub

    Private Sub ReadFileData()
        Dim algName = Algorithms.Text
        Dim split = algName.Split("_")
        Dim algType As Integer = 0

        Dim filename = New FileInfo(Main_UI.HomeDir.FullName + "CS_Classes/CS_AI_Generated.cs")
        If algName.Contains("_CS") = False And algName.Contains("_CPP") = False Then
            filename = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes/" + split(0) + ".vb")
        End If

        Dim lines = File.ReadAllLines(filename.FullName)
        clipLines = ""
        For i = 0 To lines.Count - 1
            Dim nextLine = lines(i).Trim()
            Select Case Main_UI.settings.translatorMode
                Case "VB.Net to C#"
                    If nextLine.StartsWith("Public Class " + algName + " ") Then
                        For j = i To lines.Count - 1
                            clipLines += lines(j) + vbCrLf
                            If lines(j).Trim = "End Class" Then Exit For
                        Next
                        Clipboard.SetText(clipLines)
                    End If
                Case "C# to VB.Net (back)"
                Case "C# to C++"
                    If nextLine.StartsWith("public class " + algName + " ") Then
                        clipLines += lines(i) + vbCrLf
                        For j = i + 1 To lines.Count - 1
                            nextLine = lines(j).Trim()
                            If nextLine.StartsWith("public class ") Then Exit For
                            clipLines += lines(j) + vbCrLf
                        Next
                        Clipboard.SetText(clipLines)
                    End If
            End Select
        Next
    End Sub
    Private Sub Algorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Algorithms.SelectedIndexChanged
        ReadFileData()
        Main_UI.AvailableAlgorithms.SelectedItem = Algorithms.Text
        Main_UI.jsonWrite()
        Timer2.Enabled = True
    End Sub
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Timer2.Enabled = False
        LoadData(sender, e)
    End Sub
    Private Sub LoadData(sender As Object, e As EventArgs)
        Dim testLines = My.Computer.Clipboard.GetText(System.Windows.Forms.TextDataFormat.Text)
        If testLines.Length = 0 Then Clipboard.SetText(clipLines)

        SetCursorPos(250, 900)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^v")
    End Sub
    Private Sub CopyResultsBack_Click(sender As Object, e As EventArgs) Handles CopyResultsBack.Click
        SetCursorPos(900, 900)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^c")

        Timer3.Enabled = True
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        Timer3.Enabled = False
        Dim inputLines = My.Computer.Clipboard.GetText(TextDataFormat.Text).Split(vbLf)
        Dim className As String = ""
        Dim outputLines As New List(Of String)
            For Each inline In inputLines
                Dim trimLine = Trim(inline)
                If trimLine.StartsWith("using ") Then Continue For
                If trimLine.Length = 0 Then Continue For
                If trimLine.StartsWith("public class") Then
                    Dim split = trimLine.Split(" ")
                    className = split(2)
                End If

                Select Case Main_UI.settings.translatorMode
                    Case "VB.Net to C#"
                    If inline.Contains("string desc;") Then Continue For
                    If inline.Contains("IntPtr cPtr;") Then Continue For
                    If inline.Contains(className) Then inline = inline.Replace(className, className + "_CS")
                    If inline.Contains("VB_Parent") Then inline = inline.Replace("VB_Parent", "CS_Parent")
                    If inline.Contains(".GetSubRect(") Then inline = inline.Replace(".GetSubRect(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' .Get(
                    If inline.Contains(".Get(") Then inline = inline.Replace(".Get(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' 
                    If inline.Contains("public " + className + "_CS") And inline.EndsWith("()") Then
                        inline = inline.Replace("()", "(VBtask task) : base(task)")
                    End If
                    inline = inline.Replace("private ", "")
                    inline = inline.Replace(" Run(Mat ", " RunAlg(Mat ")
                    inline = inline.Replace("RunAlgharp(Mat ", "RunAlg(Mat ")
                    inline = inline.Replace("Options_CS_", "Options_")
                    inline = inline.Replace("task.gOptions.FrameHistory.Value", "task.frameHistoryCount")
                    inline = inline.Replace("options;", "options")
                    inline = inline.Replace("Mat dst", "dst") ' Mat dst2 problem - should never need to be declared.
                    inline = inline.Replace("MCvScalar", "cvb.Scalar")
                    inline = inline.Replace("Rectangle r", "Rect r")
                    inline = inline.Replace("Rectangle(", "Rect(")
                    inline = inline.Replace("CvInvoke.", "cvb.")
                    inline = inline.Replace(" Point ", " cvb.Point ")
                    inline = inline.Replace(" Point(", " cvb.Point(")
                    inline = inline.Replace("<Point", "<cvb.Point")
                    inline = inline.Replace(" Size(", " cvb.Size(")
                    inline = inline.Replace(".Rect(", ".Rectangle(")
                    inline = inline.Replace("Cv2.Line(", "DrawLine(")
                    inline = inline.Replace("Cv2.Circle(", "DrawCircle(")
                    inline = inline.Replace("override ", "")
                    inline = Replace(inline, "bgr2gray", "BGR2GRAY", 1, -1, vbTextCompare)
                    inline = Replace(inline, "task.rightview", "task.rightView", 1, -1, vbTextCompare)
                    inline = Replace(inline, "task.leftview", "task.leftView", 1, -1, vbTextCompare)
                    inline = Replace(inline, "ColorConversion.BgrToGray", "cvb.ColorConversionCodes.BGR2GRAY", 1, -1, vbTextCompare)
                    inline = Replace(inline, "ColorConversion.GrayToBgr", "cvb.ColorConversionCodes.BGR2GRAY", 1, -1, vbTextCompare)
                    inline = Replace(inline, "ColorConversion.BgrToHsv", "cvb.ColorConversionCodes.BGR2HSV", 1, -1, vbTextCompare)
                    inline = Replace(inline, " Options.", " options.")

                    inline = Replace(inline, "task.pcSplit(2)", "task.pcSplit[2]")
                    inline = Replace(inline, "task.pcSplit(1)", "task.pcSplit[1]")
                    inline = Replace(inline, "task.pcSplit(i)", "task.pcSplit[i]")
                    inline = Replace(inline, ".Type", ".Type()")
                    inline = Replace(inline, ".Total", ".Total()")
                    inline = Replace(inline, "CountNonZero", "CountNonZero()")
                    inline = Replace(inline, ".Count", ".Count()")
                    inline = Replace(inline, "Count()NonZero", "CountNonZero")
                    inline = Replace(inline, ".Size", ".Size()")
                    inline = Replace(inline, ".Channels", ".Channels()")
                    inline = Replace(inline, ".ElemSize", ".ElemSize()")
                    inline = Replace(inline, "absdiff", "Absdiff")
                    inline = Replace(inline, "vbtab", """/t""")
                    inline = Replace(inline, "DepthType.", "MatType.")
                    inline = Replace(inline, "Cv8u", "CV_8U")
                    inline = Replace(inline, "Environment.NewLine", """\n""")
                    inline = Replace(inline, "CvPoint", "cvb.Point")
                    inline = inline.Replace(" Rect", " cvb.Rect")
                    inline = inline.Replace("<Rect", "<cvb.Rect")
                    inline = Replace(inline, "ColorConversion.BgraToBgr", "cvb.ColorConversionCodes.BGRA2BGR")
                    inline = Replace(inline, "ColorConversion.BgrToBgra", "cvb.ColorConversionCodes.BGR2BGRA")
                    inline = Replace(inline, "cPtr != 0", "cPtr != (IntPtr)0")

                    inline = Replace(inline, "()(", "(")
                    inline = Replace(inline, "()()", "()")
                    inline = Replace(inline, "cvb.Rectangle", "Rectangle")

                Case "C# to VB.Net (back)"
                    If trimLine.StartsWith("Public Class ") Then
                        className = className.Replace("_CS", "")
                        inline = "Public Class " + className + " : Inherits VB_Parent"
                    End If

                    If inline.Contains(" CS_Parent") Then Continue For
                    If inline.Contains("MyBase.New(task)") Then Continue For

                    inline = inline.Replace("task As VBtask", "")
                    inline = inline.Replace("Round(", "Math.Round(")
                    inline = inline.Replace("Math.Math.", "Math.")

                    inline = inline.Replace(" RunAlg(src As Mat)", " RunVB(src As Mat)")
                    inline = inline.Replace("Private ", "Dim ")
                    If inline.Contains(" Rect") Then
                        inline = inline.Replace(" Rect", " cvb.Rect")
                    End If
                    inline = inline.Replace(" Size(", " cvb.Size(")

                Case "C# to C++"
                    inline = inline.Replace("_CS : public CS_Parent", "_CPP : public CPP_Parent")
                    If inline.StartsWith("class") Or inline.StartsWith("public class") Then
                        If inline.StartsWith("class") Then inline = "public " + inline
                        Dim split = inline.Split(" ")
                        className = split(2)
                    End If
                    inline = inline.Replace("GetMinMax(", "task->vbMinMax(")
                    inline = inline.Replace("UpdateAdvice(", "task->UpdateAdvice(")
                    inline = inline.Replace("_CS(VBtask task) : CS_Parent(task)", "_CPP() : CPP_Parent()")
                    inline = inline.Replace("_CS(VBtask& task) : CS_Parent(task)", "_CPP() : CPP_Parent()")
                    inline = inline.Replace("task.", "task->")
                    inline = inline.Replace(" options;", " *options;")
                    inline = inline.Replace("options.", "options->")
                    inline = inline.Replace("standaloneTest()", "standalone")
                    inline = inline.Replace("_CS()", "_CPP() : CPP_Parent()")
                    inline = inline.Replace("RunAlg(", "Run(")

            End Select

            outputLines.Add(inline)
        Next

        If Main_UI.settings.translatorMode = "C# to C++" Then Main_UI.setupNewCPPalgorithm(className)
        TranslatorResults.rtb.Clear()
        For Each line In outputLines
            TranslatorResults.rtb.Text += line + vbCrLf
        Next
        TranslatorResults.Show()
    End Sub
End Class