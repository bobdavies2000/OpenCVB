Imports System.IO
Module methods
    Public algorithmVBName As String
    Public CPPName As String
    Public vbModule As String = ""
End Module
Public Class VB_to_CPP
    Dim vbCode As New List(Of String)
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim input = New FileInfo("../../../data/AlgorithmList.txt")
        Dim algList = File.ReadAllLines(input.FullName)
        For i = 1 To algList.Count - 1
            Dim line = algList(i)
            If line.StartsWith("CPP_") Or line.EndsWith("_CPP") Then Continue For
            If line.EndsWith(".py") Then Continue For
            If line.StartsWith("Options_") Then vbList.Items.Add(line)
        Next
        vbList.Text = GetSetting("OpenCVB1", "OptionsToCPP", "OptionsToCPP", "Options_Annealing")
    End Sub
    Private Sub vbList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles vbList.SelectedIndexChanged
        vbCode.Clear()
        VBrtb.Clear()
        SaveSetting("OpenCVB1", "OptionsToCPP", "OptionsToCPP", vbList.Text)
        algorithmVBName = vbList.Text
        CPPName = "CPP_" + algorithmVBName
        Dim split = algorithmVBName.Split("_")
        vbModule = split(0)
        Dim vbName = New FileInfo("../../../VB_Classes/" + split(0) + ".vb")
        Dim vbInput = File.ReadAllLines(vbName.FullName)

        Dim vbIndex As Integer
        For vbIndex = 0 To vbInput.Count - 1
            Dim line = vbInput(vbIndex)
            If line.Contains(algorithmVBName + " : Inherits VB_Algorithm") Then Exit For
        Next

        Dim marshalCopyInput As New List(Of String)
        For i = vbIndex To vbInput.Count - 1
            If vbInput(i).Contains("_Open()") Then
                Dim tokens = Trim(vbInput(i)).Split(" ")
                Dim objName = tokens(2).Substring(0, InStr(tokens(2), "_") - 1)
                vbInput(i) = objName + "* cPtr" + vbCrLf + "task.cPtr = new " + objName
            End If

            If i < vbInput.Count - 1 Then
                If vbInput(i + 1).Contains("Marshal.Copy") Then
                    Dim tokens = Trim(vbInput(i)).Split(" "c, "."c, "("c)
                    marshalCopyInput.Add(tokens(2))
                    vbInput(i) = "'" + tokens(2)
                    vbInput(i + 1) = ""
                    vbInput(i + 2) = ""
                End If
            End If

            vbInput(i) = vbInput(i).Replace("vbDrawContour", "drawContours")
            If marshalCopyInput.Count > 0 Then
                vbInput(i) = vbInput(i).Replace("handleInput.AddrOfPinnedObject()", marshalCopyInput(0) + ".data()")
                vbInput(i) = vbInput(i).Replace("handleInput.AddrOfPinnedObject()", marshalCopyInput(0) + ".data()")
            End If
            If vbInput(i).Contains(".Free()") Then vbInput(i) = ""
            If vbInput(i) = "" Then Continue For
            VBrtb.Text += vbInput(i) + vbCrLf
            vbCode.Add(vbInput(i))
            If vbInput(i).Contains("End Class") Then Exit For
        Next
    End Sub
    Private Sub PrepareCPP_Click(sender As Object, e As EventArgs) Handles PrepareCPP.Click
        CPPrtb.Clear()
        Dim hitNew As Boolean
        Dim className As String = ""
        For Each line In vbCode
            If line.Contains("New()") Then hitNew = True
            If line.Contains("End Sub") Then
                CPPrtb.Text += vbTab + "}" + vbCrLf + vbTab + "void RunVB() {}" + vbCrLf + "};"
                Exit For
            End If
            If hitNew = False Then
                If line.Contains("Inherits VB_Algorithm") Then
                    Dim tokens = Trim(line).Split(" ")
                    className = "CPP_" + tokens(2)
                    line = "class " + className + " {" + vbCrLf + "public:"
                Else
                    If line.Contains(" As New") Then
                        line = "//" + line
                    Else
                        Dim saveline = Trim(line)
                        Dim tokens = saveline.Split(" ")
                        If tokens.Count >= 3 Then
                            line = tokens(3) + " " + tokens(1) + " "
                        End If
                        If saveline.Contains("=") Then
                            Dim split = Trim(saveline).Split("=")
                            line += " = " + split(1)
                        End If
                        line += ";"
                    End If
                End If
            Else
                If line.Contains("Public Sub New()") Then
                    line = vbTab + className + "() {"
                ElseIf line.Contains("}") = False Then
                    line = "//" + line
                End If
            End If


            line = line.Replace(" False", " false")
            line = line.Replace("dst2.Rows", "task->workingRes.width")
            line = line.Replace("dst2.Cols ", "task->workingRes.height")
            line = line.Replace("cv.", "cv::")
            line = line.Replace("New Scalar", "Scalar")
            line = line.Replace("task.", "task->")
            line = line.Replace(".Width", ".width")
            line = line.Replace("Double ", vbTab + "double ")
            line = line.Replace("Single ", vbTab + "float ")
            line = line.Replace("Integer ", vbTab + "int ")
            line = line.Replace("Boolean ", vbTab + "bool ")
            If line.StartsWith("//") = False Then CPPrtb.Text += line + vbCrLf
        Next
        vbCode.Clear()
    End Sub
End Class