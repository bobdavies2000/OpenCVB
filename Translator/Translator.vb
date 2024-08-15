Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.IO
Imports System.Text.RegularExpressions
Imports Microsoft.Web.WebView2.Core
Imports System.Reflection
Imports System.Windows.Forms.TextDataFormat

Public Class TranslatorForm
#Region "NonVolatile"

    <DllImport("user32.dll")>
    Private Shared Function SetCursorPos(x As Integer, y As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Sub mouse_event(dwFlags As Integer, dx As Integer, dy As Integer, cButtons As Integer, dwExtraInfo As Integer)
    End Sub
    Public Enum tmode
        VBToCSharp = 1
        CSharpToVB = 2
        CSharpToCPP = 3
        'CPPToCSharp = 4
        'CPPToVB = 5
    End Enum

    Private Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
    Private Const MOUSEEVENTF_LEFTUP As Integer = &H4
    Dim homeDir As DirectoryInfo
    Dim saveTask As Object
    Dim clipLines As String = ""
    Private Sub addNextAlgorithm(nextName As String, ByRef lastNameSplit As String)
        Dim nameSplit = nextName.Split("_")
        If nameSplit(0) <> lastNameSplit And lastNameSplit <> "" Then Algorithms.Items.Add("")
        lastNameSplit = nameSplit(0)
        Algorithms.Items.Add(nextName)
    End Sub
    Private Sub loadAlgorithms()
        Dim AlgorithmList = New FileInfo(homeDir.FullName + "Data/AlgorithmList.txt")
        Dim sr = New StreamReader(AlgorithmList.FullName)

        Dim infoLine = sr.ReadLine
        SortAlgorithms.Items.Clear()
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            infoLine = UCase(Mid(infoLine, 1, 1)) + Mid(infoLine, 2)
            If infoLine.StartsWith("Options_") = False Then SortAlgorithms.Items.Add(infoLine)
        End While
        sr.Close()

        Dim lastNameSplit As String = ""
        For Each aName In SortAlgorithms.Items
            addNextAlgorithm(aName, lastNameSplit)
        Next

        Dim jsonFileName = New FileInfo(homeDir.FullName + "settings.json")
        If jsonFileName.Exists = False Then
            Algorithms.SelectedIndex = 0
        Else
            sr = New StreamReader(jsonFileName.FullName)
            Dim line = sr.ReadLine
            line = sr.ReadLine
            line = sr.ReadLine
            Dim split = line.Split("""")
            Algorithms.SelectedItem = split(3)
            sr.Close()
        End If
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Label1.Text = "X = " + CStr(Cursor.Position.X) + ", Y = " + CStr(Cursor.Position.Y)
    End Sub
    Private Sub WebView_CoreWebView2InitializationCompleted(sender As Object, e As Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs) Handles WebView.CoreWebView2InitializationCompleted
        LogMsg("WebView_CoreWebView2InitializationCompleted")
        LogMsg("UserDataFolder: " & WebView.CoreWebView2.Environment.UserDataFolder.ToString())
        'subscribe to CoreWebView2 event(s) (add event handlers) 
        AddHandler WebView.CoreWebView2.HistoryChanged, AddressOf CoreWebView2_HistoryChanged
        collectIDs()
        loadAlgorithms()
    End Sub
    Private Sub CoreWebView2_HistoryChanged(sender As Object, e As Object)
        LogMsg("CoreWebView2_HistoryChanged")
    End Sub
    Private Sub LogMsg(msg As String, Optional addTimestamp As Boolean = True)
        If addTimestamp Then
            msg = String.Format("{0} - {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), msg)
        End If

        Debug.WriteLine(msg)
    End Sub
    Private Async Function InitializeAsync() As Task
        Await WebView.EnsureCoreWebView2Async()
        WebView.CoreWebView2.Navigate("https://www.codeconvert.ai/app")
    End Function
    Private Async Sub collectIDs()
        Dim script As String = "
                        var ids = [];
                        var elements = document.querySelectorAll('[id]');
                        elements.forEach(function(element) {
                            ids.push(element.id);
                        });
                        ids.join(',');
                    "
        Dim result = Await WebView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub
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
        SetCursorPos(540, 540)
        PerformMouseClick("LeftClick")

        SendKeys.Send(langStr)
        Application.DoEvents()
        SendKeys.Send(vbCrLf)
    End Sub
    Private Sub setOutputLanguage(langStr As String)
        SetCursorPos(1180, 540)
        PerformMouseClick("LeftClick")

        SendKeys.Send(langStr)
        Application.DoEvents()
        SendKeys.Send(vbCrLf)
    End Sub
    Private Async Sub convert()
        Dim script = "document.getElementById('convert-btn').click();"
        Await WebView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub
    Private Sub Translate_Click(sender As Object, e As EventArgs) Handles Translate.Click
        convert()
    End Sub
    Private Sub Algorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Algorithms.SelectedIndexChanged
        ReadFileData()
        Timer2.Enabled = True
    End Sub
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Timer2.Enabled = False
        LoadData_Click(sender, e)
    End Sub
#End Region




    Private Sub TranslatorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LogMsg("++++++++++++++++++++++++++++++++++++++")
        saveTask = InitializeAsync()

        homeDir = New DirectoryInfo(CurDir() + "../../../")
        Me.Top = 0
        Me.Left = 0
        Label1.Left = 10
        Label1.Top = WebView.Top + WebView.Height + 3
        LogMsg("load complete")
    End Sub



    Private Sub ReadFileData()
        Dim filename As FileInfo
        Dim algName = Algorithms.Text
        Dim split = algName.Split("_")
        Dim algType As Integer = 0
        If algName.Contains("_CS") Then
            filename = New FileInfo(homeDir.FullName + "CS_Classes/CS_AI_Generated.cs")
            algType = 1
        ElseIf algName.Contains("_CPP") Then
            filename = New FileInfo(homeDir.FullName + "CPP_Classes/CPP_AI_Generated.cpp")
            algType = 2
        Else
            filename = New FileInfo(homeDir.FullName + "VB_Classes/" + split(0) + ".vb")
            algType = 0
        End If

        Dim lines = File.ReadAllLines(filename.FullName)
        clipLines = ""
        For i = 0 To lines.Count - 1
            Dim nextLine = lines(i)
            Select Case algType
                Case 0
                    If nextLine.StartsWith("Public Class " + algName) Then
                        For j = i To lines.Count - 1
                            clipLines += lines(j) + vbCrLf
                            If lines(j).Trim = "End Class" Then Exit For
                        Next
                        Clipboard.SetText(clipLines)
                        'setInputLanguage("VB")
                        'setOutputLanguage("Csharp")
                    End If
                Case 1
                Case 2
            End Select
        Next
    End Sub
    Private Sub LoadData_Click(sender As Object, e As EventArgs) Handles LoadData.Click
        Dim testLines = My.Computer.Clipboard.GetText(System.Windows.Forms.TextDataFormat.Text)
        If testLines.Length = 0 Then Clipboard.SetText(clipLines)

        rtb.Visible = False
        LogMsg("setInputText Starting...")

        SetCursorPos(250, 900)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^v")

        LogMsg("setInputText attempted...")
    End Sub
    Private Sub CopyResultsBack_Click(sender As Object, e As EventArgs) Handles CopyResultsBack.Click
        SetCursorPos(900, 900)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^c")

        Timer3.Enabled = True
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        Dim inputLines = My.Computer.Clipboard.GetText(TextDataFormat.Text).Split(vbLf)
        If rtb.Text <> clipLines Then
            Timer3.Enabled = False

            Dim touchupMode = tmode.VBToCSharp
            'If CSharp_To_VB.Checked Then touchupMode = tmode.CSharpToVB
            'If CSharp_To_CPP.Checked Then touchupMode = tmode.CSharpToCPP

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

                Select Case touchupMode
                    Case tmode.VBToCSharp
                        If inline.Contains("string desc;") Then Continue For
                        If inline.Contains("IntPtr cPtr;") Then Continue For
                        If inline.Contains(className) Then inline = inline.Replace(className, className + "_CS")
                        If inline.Contains("VB_Parent") Then inline = inline.Replace("VB_Parent", "CS_Parent")
                        If inline.Contains("RunVB(Mat") Then inline = inline.Replace("RunVB(Mat", "RunCS(Mat")
                        If inline.Contains("RunVB(cv.Mat") Then inline = inline.Replace("RunVB(cv.Mat", "RunCS(Mat")
                        If inline.Contains(".GetSubRect(") Then inline = inline.Replace(".GetSubRect(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' .Get(
                        If inline.Contains(".Get(") Then inline = inline.Replace(".Get(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' 
                        If inline.Contains("public " + className + "_CS") And inline.EndsWith("()") Then
                            inline = inline.Replace("()", "(VBtask task) : base(task)")
                        End If
                        inline = inline.Replace("private ", "")
                        inline = inline.Replace(" Run(Mat ", " RunCS(Mat ")
                        inline = inline.Replace("RunCSharp(Mat ", "RunCS(Mat ")
                        inline = inline.Replace("Options_CS_", "Options_")
                        inline = inline.Replace("task.gOptions.FrameHistory.Value", "task.frameHistoryCount")
                        inline = inline.Replace("options.RunCSharp", "options.RunVB")
                        inline = inline.Replace("options.Run(", "options.RunVB(")
                        inline = inline.Replace("options;", "options")
                        inline = inline.Replace("Mat dst", "dst") ' Mat dst2 problem - should never need to be declared.
                        inline = inline.Replace("MCvScalar", "cv.Scalar")
                        inline = inline.Replace("Rectangle r", "Rect r")
                        inline = inline.Replace("Rectangle(", "Rect(")
                        inline = inline.Replace("CvInvoke.", "cv.")
                        inline = inline.Replace(" Point ", " cv.Point ")
                        inline = inline.Replace(" Point(", " cv.Point(")
                        inline = inline.Replace("<Point", "<cv.Point")
                        inline = inline.Replace(" Size(", " cv.Size(")
                        inline = inline.Replace(".Rect(", ".Rectangle(")
                        inline = inline.Replace("Cv2.Line(", "DrawLine(")
                        inline = inline.Replace("Cv2.Circle(", "DrawCircle(")
                        inline = inline.Replace("override ", "")
                        inline = Replace(inline, "bgr2gray", "BGR2GRAY", 1, -1, vbTextCompare)
                        inline = Replace(inline, "task.rightview", "task.rightView", 1, -1, vbTextCompare)
                        inline = Replace(inline, "task.leftview", "task.leftView", 1, -1, vbTextCompare)
                        inline = Replace(inline, "ColorConversion.BgrToGray", "cv.ColorConversionCodes.BGR2GRAY", 1, -1, vbTextCompare)
                        inline = Replace(inline, "ColorConversion.GrayToBgr", "cv.ColorConversionCodes.BGR2GRAY", 1, -1, vbTextCompare)
                        inline = Replace(inline, "ColorConversion.BgrToHsv", "cv.ColorConversionCodes.BGR2HSV", 1, -1, vbTextCompare)
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
                        inline = Replace(inline, "CvPoint", "cv.Point")
                        inline = inline.Replace(" Rect", " cv.Rect")
                        inline = inline.Replace("<Rect", "<cv.Rect")
                        inline = Replace(inline, "ColorConversion.BgraToBgr", "cv.ColorConversionCodes.BGRA2BGR")
                        inline = Replace(inline, "ColorConversion.BgrToBgra", "cv.ColorConversionCodes.BGR2BGRA")
                        inline = Replace(inline, "cPtr != 0", "cPtr != (IntPtr)0")

                        inline = Replace(inline, "()(", "(")
                        inline = Replace(inline, "()()", "()")
                        inline = Replace(inline, "cv.Rectangle", "Rectangle")

                    Case tmode.CSharpToVB
                        If trimLine.StartsWith("Public Class ") Then
                            className = className.Replace("_CS", "")
                            inline = "Public Class " + className + " : Inherits VB_Parent"
                        End If

                        If inline.Contains(" CS_Parent") Then Continue For
                        If inline.Contains("MyBase.New(task)") Then Continue For

                        inline = inline.Replace("task As VBtask", "")
                        inline = inline.Replace("Round(", "Math.Round(")
                        inline = inline.Replace("Math.Math.", "Math.")

                        inline = inline.Replace(" RunCS(src As Mat)", " RunVB(src As Mat)")
                        inline = inline.Replace("Private ", "Dim ")
                        If inline.Contains(" Rect") Then
                            inline = inline.Replace(" Rect", " cv.Rect")
                        End If
                        inline = inline.Replace(" Size(", " cv.Size(")
                    Case tmode.CSharpToCPP
                        inline = inline.Replace("_CS : public CS_Parent", "_CPP : public CPP_Parent")
                        inline = inline.Replace("UpdateAdvice(", "task->UpdateAdvice(")
                        inline = inline.Replace("_CS(VBtask task) : CS_Parent(task)", "_CPP() : CPP_Parent()")
                        inline = inline.Replace("_CS(VBtask& task) : CS_Parent(task)", "_CPP() : CPP_Parent()")
                        inline = inline.Replace("task.", "task->")
                        inline = inline.Replace(" options;", " *options;")
                        inline = inline.Replace("options.", "options->")
                        inline = inline.Replace("standaloneTest()", "standalone")
                        inline = inline.Replace("_CS()", "_CPP() : CPP_Parent()")
                        inline = inline.Replace("RunCS(", "Run(")

                End Select

                outputLines.Add(inline)
            Next

            rtb.Visible = True
            rtb.Left = WebView.Left + WebView.Width / 2
            rtb.Top = WebView.Top
            rtb.Width = WebView.Width / 2
            rtb.Height = WebView.Height
            rtb.Clear()
            For Each line In outputLines
                rtb.Text += line + vbCrLf
            Next
        End If
    End Sub
    Private Sub TranslatorForm_MouseClick(sender As Object, e As MouseEventArgs) Handles Me.MouseClick
        rtb.Visible = False
    End Sub
End Class