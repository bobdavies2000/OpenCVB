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
        Dim startComments As Boolean
        For i = 0 To split.Count - 1
            If split(i).Contains("#include") Or split(i) = "" Then Continue For
            split(i) = split(i).Replace("Scalar minVal, maxVal;", "double minVal, maxVal;")
            If Trim(split(i)).StartsWith("class") Then
                Dim tokens = split(i).Split(" ")
                functionName = tokens(1)
                split(i) = split(i).Replace("VB_Algorithm", "algorithmCPP")
                split(i) = split(i).Replace("class ", "class CPP_")
            End If

            If split(i).Contains("//") Then
                split(i) = split(i).Substring(0, InStr(split(i), "//") - 1)
            End If

            If functionName <> "" Then
                If Trim(split(i)).StartsWith(functionName) Then
                    split(i) = split(i).Replace(functionName + "()", "CPP_" + functionName +
                                                "() : algorithmCPP() ") + vbCrLf
                    split(i) += vbTab + "traceName = """ + "CPP_" + functionName + """;"
                    For Each con In constructorAdds
                        split(i) += vbCrLf + con
                    Next
                End If
            End If

            If split(i).Contains("CPP_") = False And Trim(split(i)).StartsWith("labels") = False Then
                For Each func In functions
                    If split(i).Contains(func) And functionName.Contains(func) = False Then
                        Dim nextLine = Trim(split(i))
                        Dim tokens = nextLine.Split(" ")
                        split(i) = split(i).Replace(tokens(0), "CPP_" + tokens(0) + "*")
                        tokens(1) = tokens(1).Replace(";", "")
                        objectNames.Add(tokens(1))
                        constructorAdds.Add(vbTab + tokens(1) + " = new CPP_" + tokens(0) + "();")
                        Exit For
                    End If
                Next
            End If

            For Each obj In objectNames
                split(i) = split(i).Replace(obj + ".", obj + "->")
            Next

            split(i) = split(i).Replace("Mat dst", "dst = Mat")
            split(i) = split(i).Replace("~" + functionName, "~CPP_" + functionName)
            split(i) = split(i).Replace(" override", "")
            split(i) = split(i).Replace(" || showIntermediate()", "")
            split(i) = split(i).Replace("vbDrawContour", "task->drawContour")
            split(i) = split(i).Replace("gOptions.FrameHistory.Value", "task->frameHistoryCount")
            split(i) = split(i).Replace("gOptions.PixelDiffThreshold.Value", "task->pixelDiffThreshold")
            split(i) = split(i).Replace("initRandomRect(", "task->initRandomRect(")
            split(i) = split(i).Replace("validateRect(", "task->validateRect(")
            split(i) = split(i).Replace("gOptions.UseKalman.Checked", "task->useKalman")
            split(i) = split(i).Replace("gOptions.HistBinSlider.Value", "task->histogramBins")
            split(i) = split(i).Replace("CStr(", "to_string(")
            split(i) = split(i).Replace("task.", "task->")
            split(i) = split(i).Replace("heartBeat()", "task->heartBeat")
            split(i) = split(i).Replace("firstPass", "task->firstPass")
            split(i) = split(i).Replace("setTrueText", "task->setTrueText")
            split(i) = split(i).Replace("gOptions.GridSize.value", "task->gridSize")
            split(i) = split(i).Replace("std::", "")
            split(i) = split(i).Replace("const Mat& src", "Mat src")
            split(i) = split(i).Replace("Mat& src", "Mat src")
            split(i) = split(i).Replace("RunVB", "Run")
            split(i) = split(i).Replace("CPP_CPP_", "CPP_")
            split(i) = split(i).Replace("randomCellColor", "task->randomCellColor")
            If split(i).Contains(" options;") Then
                split(i) = split(i).Replace("Options_", "CPP_Options_")
                Dim tokens = Trim(split(i)).Split(" ")
                split(i) = split(i).Replace(" options;", "*  options = new " + tokens(0) + ";")
            End If

            split(i) = split(i).Replace("options.", "options->")
            ' updates for options
            If split(i).Contains("CPP_Options_") Then
                split(i) = split(i).Replace(": public algorithmCPP", "")
            End If
            If startComments Then split(i) = vbTab + "//" + split(i)

            If split(i).Contains("CPP_Options_") Then
                If split(i).Contains("algorithmCPP()") Then
                    split(i) = split(i).Replace("traceName", "//" + vbTab + "traceName")
                    split(i) = split(i).Replace(": algorithmCPP()", "")
                    startComments = True
                End If
            End If
            If split(i).StartsWith(vbTab + "//    }") Then
                startComments = False
                split(i) = split(i).Replace("//", "")
            End If
            If split(i).Contains("void Run()") Then startComments = True

            CPPrtb.Text += split(i) + vbCrLf
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