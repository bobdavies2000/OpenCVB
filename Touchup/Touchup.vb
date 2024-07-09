Imports System.Reflection.Emit

Public Class Touchup
    Private Sub translate_Click(sender As Object, e As EventArgs) Handles translate.Click
        Dim inputCode = rtb.Text
        Dim inputLines = inputCode.Split(vbLf)

        Dim className As String = ""
        Dim outputLines As String = ""
        For Each inline In inputLines
            Dim line = Trim(inline)
            If line.Length = 0 Then Continue For
            If line.StartsWith("public class") Then
                Dim split = line.Split(" ")
                className = split(2)
            End If
            If inline.Contains(className) Then inline = inline.Replace(className, "CS_" + className)
            If inline.Contains("VB_Parent") Then inline = inline.Replace("VB_Parent", "CS_Parent")
            If inline.Contains("RunVB(Mat") Then inline = inline.Replace("RunVB(Mat", "RunCS(Mat")
            If inline.Contains("RunVB(cv.Mat") Then inline = inline.Replace("RunVB(cv.Mat", "RunCS(Mat")
            If inline.Contains(".GetSubRect(") Then inline = inline.Replace(".GetSubRect(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' .Get(
            If inline.Contains(".Get(") Then inline = inline.Replace(".Get(", "[") ' force a compile error to indicate you have to manually put the corresponding close bracket ']' 
            If inline.Contains("public CS_") And inline.EndsWith("()") Then inline = inline.Replace("()", "(VBtask task) : base(task)")
            inline = inline.Replace("private ", "")
            inline = inline.Replace("BGR2Gray ", "BGR2GRAY")
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
            inline = inline.Replace(" Size(", " cv.Size(")
            outputLines += inline + vbCrLf
        Next

        rtb.Clear()
        rtb.Text = outputLines
    End Sub
End Class