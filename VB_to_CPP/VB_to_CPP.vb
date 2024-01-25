Imports System.IO
Module methods
    Public algorithmVBName As String
    Public fIndexName As String
    Public CPPName As String
    Public vbModule As String = ""
End Module
Public Class VB_to_CPP
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim input = New FileInfo("../../data/AlgorithmList.txt")
        Dim algList = File.ReadAllLines(input.FullName)
        Dim lastGroup As String = ""
        For i = 1 To algList.Count - 1
            Dim line = algList(i)
            If line.StartsWith("CPP_") Or line.EndsWith("_CPP") Then Continue For
            If line.EndsWith(".py") Then Continue For
            Dim tokens = line.Split("_")
            If line.StartsWith(lastGroup) = False Then vbList.Items.Add("")
            vbList.Items.Add(line)
            lastGroup = tokens(0)
        Next
        vbList.Text = GetSetting("OpenCVB1", "TranslateToCPP", "TranslateToCPP", "Addweighted_Basics")
    End Sub
    Private Sub vbList_SelectedValueChanged(sender As Object, e As EventArgs) Handles vbList.SelectedValueChanged
        SaveSetting("OpenCVB1", "TranslateToCPP", "TranslateToCPP", vbList.Text)
        algorithmVBName = vbList.Text
        fIndexName = "CPP_" + algorithmVBName + "_"
        CPPName = "CPP_" + algorithmVBName
        Dim split = algorithmVBName.Split("_")
        vbModule = split(0)
        Dim vbName = New FileInfo("../../VB_Classes/" + split(0) + ".vb")
        Dim vbInput = File.ReadAllLines(vbName.FullName)

        Dim vbLines As New List(Of String)
        Dim vbIndex As Integer
        For vbIndex = 0 To vbInput.Count - 1
            Dim line = vbInput(vbIndex)
            If line.Contains(algorithmVBName + " : Inherits VB_Algorithm") Then Exit For
        Next

        VBrtb.Text = "Translate this vb.Net code to C++" + vbCrLf
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
            If vbInput(i).Contains("End Class") Then Exit For
        Next

        UpdateInfrastructure.Text = "Step 5: Add " + CPPName + " to OpenCVB interface"
    End Sub
    Private Sub PrepareCPP_Click(sender As Object, e As EventArgs) Handles PrepareCPP.Click
        ' read all the existing CPP_ algorithms so names can be recognized when parsing the new algorithm.
        Dim Input = New FileInfo("../../CPP_Classes/CPP_Functions.h")
        Dim includeOnly = File.ReadAllLines(Input.FullName)
        Dim functions As New List(Of String)
        For Each line In includeOnly
            line = Trim(line)
            If line.StartsWith("_") Or line.Contains("MAX_FUNCTION") Then
                Dim triggerDone As Boolean
                If line.Contains("MAX_FUNCTION") Then
                    triggerDone = True
                    line = line.Substring(Len("MAX_FUNCTION = "))
                End If

                Dim nextLine = line.Substring(5)
                nextLine = nextLine.Substring(0, Len(nextLine) - 1)
                functions.Add(nextLine)
                If triggerDone Then Exit For
            End If
        Next

        Dim cppCode = CPPrtb.Text
        Dim split = cppCode.Split(vbLf)
        CPPrtb.Clear()
        Dim functionName As String = ""
        Dim constructorAdds As New List(Of String)
        Dim objectNames As New List(Of String)
        For i = 0 To split.Count - 1
            Dim line = split(i)
            If line.Contains("#include") Or line = "" Then Continue For
            line = line.Replace("Scalar minVal, maxVal;", "double minVal, maxVal;")
            If Trim(line).StartsWith("class") Then
                Dim tokens = line.Split(" ")
                functionName = tokens(1)
                line = line.Replace("VB_Algorithm", "algorithmCPP")
                line = line.Replace("class ", "class CPP_")
            End If

            If line.Contains("//") Then
                line = line.Substring(0, InStr(line, "//") - 1)
            End If

            If functionName <> "" Then
                If Trim(line).StartsWith(functionName) Then
                    line = line.Replace(functionName + "()", "CPP_" + functionName +
                                                "() : algorithmCPP() ") + vbCrLf
                    line += vbTab + "traceName = """ + "CPP_" + functionName + """;"
                    For Each con In constructorAdds
                        line += vbCrLf + con
                    Next
                End If
            End If

            If line.Contains("CPP_") = False And Trim(line).StartsWith("labels") = False Then
                For Each func In functions
                    If line.Contains(func) And functionName.Contains(func) = False Then
                        Dim nextLine = Trim(line)
                        Dim tokens = nextLine.Split(" ")
                        line = line.Replace(tokens(0), "CPP_" + tokens(0) + "*")
                        tokens(1) = tokens(1).Replace(";", "")
                        objectNames.Add(tokens(1))
                        constructorAdds.Add(vbTab + tokens(1) + " = new CPP_" + tokens(0) + "();")
                        Exit For
                    End If
                Next
            End If

            For Each obj In objectNames
                line = line.Replace(obj + ".", obj + "->")
            Next

            line = line.Replace("Mat dst", "dst = Mat")
            line = line.Replace("~" + functionName, "~CPP_" + functionName)
            line = line.Replace(" override", "")
            line = line.Replace(" || showIntermediate()", "")
            line = line.Replace("vbDrawContour", "task->drawContour")
            line = line.Replace("gOptions.FrameHistory.Value", "task->frameHistoryCount")
            line = line.Replace("gOptions.PixelDiffThreshold.Value", "task->pixelDiffThreshold")
            line = line.Replace("initRandomRect(", "task->initRandomRect(")
            line = line.Replace("validateRect(", "task->validateRect(")
            line = line.Replace("gOptions.UseKalman.Checked", "task->useKalman")
            line = line.Replace("gOptions.HistBinSlider.Value", "task->histogramBins")
            line = line.Replace("CStr(", "to_string(")
            line = line.Replace("task.", "task->")
            line = line.Replace("heartBeat()", "task->heartBeat")
            line = line.Replace("firstPass", "task->firstPass")
            line = line.Replace("setTrueText", "task->setTrueText")
            line = line.Replace("gOptions.GridSize.value", "task->gridSize")
            line = line.Replace("std::", "")
            line = line.Replace("const Mat& src", "Mat src")
            line = line.Replace("Mat& src", "Mat src")
            line = line.Replace("RunVB", "Run")
            line = line.Replace("CPP_CPP_", "CPP_")
            line = line.Replace("randomCellColor", "task->randomCellColor")
            If line.Contains(" options;") Then
                line = line.Replace("Options_", "CPP_Options_")
                Dim tokens = Trim(line).Split(" ")
                line = line.Replace(" options;", "*  options = new " + tokens(0) + ";")
            End If

            line = line.Replace("options.", "options->")
            ' updates for options
            If line.Contains("CPP_Options_") Then
                line = line.Replace(": public algorithmCPP", "")
            End If

            If line.Contains("CPP_Options_") Then
                If line.Contains("algorithmCPP()") Then
                    line = line.Replace("traceName", "//" + vbTab + "traceName")
                    line = line.Replace(": algorithmCPP()", "")
                End If
            End If

            If line.Contains("options->Run()") Then Continue For
            If line.Contains("vbAddAdvice(") Then Continue For
            line = line.Replace("cv::", "")
            line = line.Replace("std::", "")
            CPPrtb.Text += line + vbCrLf
        Next
    End Sub
    Private Sub UpdateInfrastructure_Click(sender As Object, e As EventArgs) Handles UpdateInfrastructure.Click
        Dim input = New FileInfo("../../CPP_Classes/CPP_Externs.h")
        Dim externs = File.ReadAllLines(input.FullName)
        Dim buttonText = UpdateInfrastructure.Text
        Dim split = buttonText.Split(" ")
        Dim functionName = Trim(split(3))
        For Each line In externs
            If line.Contains(functionName) Then
                MsgBox(functionName + " infrastructure is already present.")
                Exit Sub
            End If
        Next

        Dim sw = New StreamWriter(input.FullName)
        For Each line In externs
            sw.WriteLine(line)
            If line.Contains("new CPP_AddWeighted_Basics(); break; }") Then
                sw.WriteLine(vbTab + "case " + "_" + functionName + " :")
                sw.WriteLine(vbTab + "{task->alg = new " + functionName + "(); break; }")
            End If
        Next
        sw.Close()

        input = New FileInfo("../../CPP_Classes/CPP_Functions.h")
        Dim includeOnly = File.ReadAllLines(input.FullName)
        sw = New StreamWriter(input.FullName)
        For Each line In includeOnly
            sw.WriteLine(line)
            If line.Contains("_CPP_AddWeighted_Basics,") Then
                sw.WriteLine("_" + functionName + ",")
            End If
        Next
        sw.Close()
    End Sub
End Class