
Public Enum tmode
    VBToCSharp = 1
    CSharpToVB = 2
    CSharpToCPP = 3
    'CPPToCSharp = 4
    'CPPToVB = 5
End Enum

Public Class Touchup
    Private Sub translate_Click(sender As Object, e As EventArgs) Handles translate.Click
        Dim inputCode = rtb.Text
        Dim inputLines = inputCode.Split(vbLf)

        Dim touchupMode = tmode.VBtoCSharp
        If CSharp_To_VB.Checked Then touchupMode = tmode.CSharpToVB
        If CSharp_To_CPP.Checked Then touchupMode = tmode.CSharpToCPP

        Dim className As String = ""
        Dim outputLines As String = ""
        For Each inline In inputLines
            Dim line = Trim(inline)
            If line.Length = 0 Then Continue For
            Select Case touchupMode
                Case tmode.CSharpToVB
                    If line.StartsWith("public class") Then
                        Dim split = line.Split(" ")
                        className = split(2)
                    End If
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
                    If line.StartsWith("Public Class CS_") Then
                        inline = inline.Replace("Public Class " + className + "_CS", "Public Class VB_")
                        inline = inline + " : Inherits VB_Parent"
                        Dim split = inline.Split(" ")
                        className = "VB_" + split(2)
                    End If
                    If inline.Contains("Inherits CS_Parent") Then Continue For
                    If inline.Contains("MyBase.New(task)") Then Continue For
                    inline = inline.Replace("CS_Parent", "VB_Parent")
                    inline = inline.Replace("New(task As VBtask)", "New()")
                    inline = inline.Replace(" RunCS(src As Mat)", " RunVB(src As Mat)")
                    inline = inline.Replace("Private ", "Dim ")
                    If inline.Contains(" Rect") Then
                        inline = inline.Replace(" Rect", " cv.Rect")
                    End If
                Case tmode.CSharpToCPP
                    inline = inline.Replace("_CS : public CS_Parent", "_CPP : public CPP_Parent")
                    inline = inline.Replace("UpdateAdvice(", "task->UpdateAdvice(")
                    inline = inline.Replace("VBtask task) : CS_Parent(task)", ")")
                    inline = inline.Replace("task.", "task->")
                    inline = inline.Replace(" options;", " *options;")
                    inline = inline.Replace("options.", "options->")
                    inline = inline.Replace("standaloneTest()", "standalone")
                    inline = inline.Replace("_CS()", "_CPP() : CPP_Parent()")
                    inline = inline.Replace("RunCS(", "RunCPP(")

            End Select

            outputLines += inline + vbCrLf
        Next

        rtb.Clear()
        rtb.Text = outputLines
    End Sub
    Private Sub VBtoCSharp_CheckedChanged(sender As Object, e As EventArgs) Handles VB_To_CSharp.CheckedChanged
        SaveSetting("OpenCVB", "tmode", "tmode", tmode.VBToCSharp)
    End Sub
    Private Sub CSharp_To_VB_CheckedChanged(sender As Object, e As EventArgs) Handles CSharp_To_VB.CheckedChanged
        SaveSetting("OpenCVB", "tmode", "tmode", tmode.CSharpToVB)
    End Sub
    Private Sub CSharp_To_CPP_CheckedChanged(sender As Object, e As EventArgs) Handles CSharp_To_CPP.CheckedChanged
        SaveSetting("OpenCVB", "tmode", "tmode", tmode.CSharpToCPP)
    End Sub
    Private Sub Touchup_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim touchup = GetSetting("OpenCVB", "tmode", "tmode", tmode.CSharpToCPP)
        Select Case touchup
            Case tmode.VBToCSharp
                VB_To_CSharp.Checked = True
            Case tmode.CSharpToVB
                CSharp_To_VB.Checked = True
            Case tmode.CSharpToCPP
                CSharp_To_CPP.Checked = True
        End Select
    End Sub
End Class