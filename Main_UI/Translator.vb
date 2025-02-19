Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.Web.WebView2.Core

Public Class Translator
#Region "NonVolatile"
    Dim cursorInPoint As New cv.Point(250, 750)
    Dim cursorOutPoint As New cv.Point(650, 750)
    Dim algName As String = ""
    <DllImport("user32.dll")>
    Private Shared Function SetCursorPos(x As Integer, y As Integer) As Boolean
    End Function

    Private Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
    Private Const MOUSEEVENTF_LEFTUP As Integer = &H4
    Dim saveTask As Object
    Dim clipLines As String = ""
    <DllImport("user32.dll")>
    Private Shared Sub mouse_event(dwFlags As Integer, dx As Integer, dy As Integer, cButtons As Integer, dwExtraInfo As Integer)
    End Sub

    Private Sub LoadData(sender As Object, e As EventArgs)
        Dim testLines = My.Computer.Clipboard.GetText(System.Windows.Forms.TextDataFormat.Text)
        If testLines.Length = 0 Then Clipboard.SetText(clipLines)

        SetCursorPos(cursorInPoint.X, cursorInPoint.Y)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^v")
    End Sub
    Private Sub CopyResultsBack_Click(sender As Object, e As EventArgs) Handles CopyResultsBack.Click
        SetCursorPos(cursorOutPoint.X, cursorOutPoint.Y)
        PerformMouseClick("LeftClick")
        SendKeys.Send("^a")
        SendKeys.Send("^c")

        Timer3.Enabled = True
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
    Private Async Sub Translate_Click(sender As Object, e As EventArgs) Handles translate.Click
        Dim script = "document.getElementById('convert-btn').click();"
        Await WebView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub
    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        If algName = "" Then
            ReadFileData()
            Timer4.Enabled = False
        End If
    End Sub

    'Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
    '    Algorithms.SelectedIndex = Main_UI.AvailableAlgorithms.SelectedIndex
    'End Sub
#End Region
    Private Sub Translator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim saveTask = InitializeAsync()
        Me.Top = 0
        Me.Left = 0
        For Each alg In Main_UI.AvailableAlgorithms.Items
            Algorithms.Items.Add(alg)
        Next
        Timer4.Enabled = True
    End Sub
    Dim result As String
    Private Async Sub translateSetting()
        Dim script As String = "document.documentElement.outerHTML;"
        result = Await WebView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub
    Private Sub ReadFileData()

        algName = Algorithms.Text
        If algName = "" Then Exit Sub
        If algName.EndsWith("_CPP") Or algName.EndsWith("_CPP_CS") Then
            MsgBox("The selected algorithm is a Native C++ algorithm" + vbCrLf + "Choose a VB.Net or C# algorithm.")
            Exit Sub
        End If

        Dim split = algName.Split("_")
        Dim filename = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes/" + split(0) + ".vb")

        Main_UI.settings.translatorMode = "VB.Net to C#"
        If algName.EndsWith("_CS") Then
            Main_UI.settings.translatorMode = "C# to C++"
            filename = New FileInfo(Main_UI.HomeDir.FullName + "CS_Classes/CS_AI_Generated.cs")
        End If

        translateSetting()

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
                Case "C# to C++"
                    If nextLine.StartsWith("public class") Then
                        If nextLine.StartsWith("public class " + algName + " ") Then
                            clipLines += lines(i) + vbCrLf
                            For j = i + 1 To lines.Count - 1
                                nextLine = lines(j).Trim()
                                If nextLine.StartsWith("public class ") Then Exit For
                                clipLines += lines(j) + vbCrLf
                            Next
                            Clipboard.SetText(clipLines)
                        End If
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


    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        Timer3.Enabled = False
        Dim inputLines = My.Computer.Clipboard.GetText(TextDataFormat.Text).Split(vbLf)
        Dim className As String = ""
        Dim originalName As String = ""
        Dim outputLines As New List(Of String)
        Dim lastLine As String = ""
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
                    If inline.Contains(".GetSubRect(") Then inline = inline.Replace(".GetSubRect(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' .Get(
                    If inline.Contains(".Get(") Then inline = inline.Replace(".Get(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' 
                    inline = inline.Replace(" Run(Mat ", " RunAlg(Mat ")
                    inline = inline.Replace("cv.", "cv.")
                    inline = inline.Replace("Options_CS_", "Options_")
                    inline = inline.Replace("task.", "vbc.task.")
                    inline = inline.Replace("options;", "options") ' make sure we see an error when this happens.
                    inline = inline.Replace("Mat dst", "dst") ' Mat dst2 problem - should never need to be declared.
                    inline = inline.Replace("MCvScalar", "cv.Scalar")
                    inline = inline.Replace("Rectangle r", "cv.Rect r")
                    inline = inline.Replace("Rectangle(", "cv.Rect(")
                    inline = inline.Replace(" Rect", " cv.Rect")
                    inline = inline.Replace("<Rect", "<cv.Rect")
                    inline = inline.Replace("CvInvoke.", "cv.")
                    inline = inline.Replace(" Point ", " cv.Point ")
                    inline = inline.Replace(" Point(", " cv.Point(")
                    inline = inline.Replace("<Point", "<cv.Point")
                    inline = inline.Replace(" Size(", " cv.Size(")
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
                    inline = Replace(inline, "ColorConversion.BgraToBgr", "cv.ColorConversionCodes.BGRA2BGR")
                    inline = Replace(inline, "ColorConversion.BgrToBgra", "cv.ColorConversionCodes.BGR2BGRA")
                    inline = Replace(inline, "cPtr != 0", "cPtr != (IntPtr)0")

                    inline = Replace(inline, "()(", "(")
                    inline = Replace(inline, "()()", "()")
                    inline = Replace(inline, "cv.Rectangle", "Rectangle")

                'Case "C# to VB.Net (back)"
                '    If trimLine.StartsWith("Public Class ") Then
                '        className = className.Replace("_CS", "")
                '        inline = "Public Class " + className + " : Inherits TaskParent"
                '    End If

                '    inline = inline.Replace("Round(", "Math.Round(")
                '    inline = inline.Replace("Math.Math.", "Math.")

                '    inline = inline.Replace("Private ", "Dim ")
                '    If inline.Contains(" Rect") Then
                '        inline = inline.Replace(" Rect", " cv.Rect")
                '    End If
                '    inline = inline.Replace(" Size(", " cv.Size(")

                Case "C# to C++"
                    If trimLine.StartsWith("#") Then Continue For
                    If trimLine.StartsWith("class") Then
                        Dim split = trimLine.Split(" ")
                        originalName = split(1)
                        className = originalName.Replace("_CS", "_CPP")
                        trimLine = "public ref class " + className + " : public TaskParent"
                    End If

                    If originalName <> "" Then trimLine = trimLine.Replace(originalName, className)
                    trimLine = trimLine.Replace("cv::", "")
                    trimLine = trimLine.Replace("RunAlg(cv::Mat& src)", "RunAlg()")
                    trimLine = trimLine.Replace("RunAlg(Mat src)", "RunAlg()")

                    If trimLine.StartsWith("public:") Then
                        outputLines.Add(trimLine)
                        outputLines.Add(vbTab + "size_t ioIndex;")
                        trimLine = vbTab + "unManagedIO* io;"
                    End If

                    If trimLine.StartsWith("desc = ") Then
                        outputLines.Add("unManagedIO* ioNew = new unManagedIO();")
                        outputLines.Add("ioIndex = ioList.size();")
                        outputLines.Add("ioList.push_back(ioNew);")
                        trimLine = "io = ioNew;"
                    End If

                    If trimLine = "{" And lastLine.Contains("RunAlg(") Then
                        outputLines.Add("{")
                        trimLine = "io = ioList[ioIndex];"
                    End If

                    inline = inline.Replace("(Mat& io->src)", "()")
                    If trimLine.StartsWith("src") Then trimLine = trimLine.Replace("src", "io->src")
                    trimLine = trimLine.Replace(".src", "io->src")
                    trimLine = trimLine.Replace(" src", " io->src")
                    trimLine = trimLine.Replace("(src", "(io->src")
                    trimLine = trimLine.Replace(".dst0.", ".io->dst0.")
                    trimLine = trimLine.Replace(".dst1.", ".io->dst1.")
                    trimLine = trimLine.Replace(".dst2.", ".io->dst2.")
                    trimLine = trimLine.Replace(".dst3.", ".io->dst3.")
                    trimLine = trimLine.Replace(" dst0", " io->dst0")
                    trimLine = trimLine.Replace(" dst1", " io->dst1")
                    trimLine = trimLine.Replace(" dst2", " io->dst2")
                    trimLine = trimLine.Replace(" dst3", " io->dst3")
                    trimLine = trimLine.Replace("(dst0.", "(io->dst0.")
                    trimLine = trimLine.Replace("(dst1.", "(io->dst1.")
                    trimLine = trimLine.Replace("(dst2.", "(io->dst2.")
                    trimLine = trimLine.Replace("(dst3.", "(io->dst3.")

                    trimLine = trimLine.Replace("vbc.task.", "task.")
                    If trimLine.EndsWith("options;") Then
                        Dim split = trimLine.Split(" ")
                        trimLine = split(0) + "^ options = gcnew " + split(0) + "();"
                    End If

                    trimLine = trimLine.Replace("GetMinMax(", "task->vbMinMax(")
                    trimLine = trimLine.Replace("UpdateAdvice(", "task->UpdateAdvice(")
                    trimLine = trimLine.Replace(" options;", " *options;")
                    trimLine = trimLine.Replace("options.", "options->")
                    trimLine = trimLine.Replace("standaloneTest()", "standalone")
                    trimLine = trimLine.Replace("RunAlg(Mat& io->src)", "RunAlg()")

                    inline = trimLine
            End Select

            outputLines.Add(inline)
            lastLine = inline
        Next

        'If Main_UI.settings.translatorMode = "C# to C++" Then Main_UI.setupNewCPPalgorithm(className)
        'TranslatorResults.rtb.Clear()
        'For Each line In outputLines
        '    TranslatorResults.rtb.Text += line + vbCrLf
        'Next
        'TranslatorResults.Show()
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        XYLoc.Text = "X = " + CStr(Cursor.Position.X) + ", Y = " + CStr(Cursor.Position.Y)
    End Sub
    Private Sub Timer5_Tick(sender As Object, e As EventArgs) Handles Timer5.Tick
        Me.Top = 0
        Me.Left = 0
        Me.Width = 880
        Me.Height = 880
    End Sub
End Class