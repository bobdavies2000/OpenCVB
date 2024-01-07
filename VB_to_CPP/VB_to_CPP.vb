Imports System.IO
' this translator is hacked together from converting the 30 lines of an algorithm to C++
' There is no formal approach used here.  It is just a empirical - what do we need - approach.
' If you want a thorough VB.Net to C++ translator, consider this product:
'               https://www.tangiblesoftwaresolutions.com/product_details/vb_to_cplusplus_converter_details.html
Public Class VB_to_CPP
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim input = New FileInfo("../../data/AlgorithmList.txt")
        Dim algList = File.ReadAllLines(input.FullName)
        For i = 1 To algList.Count - 1
            Dim line = algList(i)
            If line.StartsWith("CPP_") Or line.EndsWith("_CPP") Then Continue For
            If line.EndsWith(".py") Then Continue For
            vbList.Items.Add(line)
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
        For vbIndex = vbIndex To vbInput.Count - 1
            VBrtb.Text += vbInput(vbIndex) + vbCrLf
            If vbInput(vbIndex).Contains("End Class") Then Exit For
        Next

        UpdateInfrastructure.Text = "Step 5: Add " + CPPName + " to OpenCVB interface"
    End Sub
    Private Sub PrepareCPP_Click(sender As Object, e As EventArgs) Handles PrepareCPP.Click
        Dim cppCode = CPPrtb.Text
        Dim split = cppCode.Split(vbLf)
        CPPrtb.Clear()
        Dim functionName As String = ""
        For i = 0 To split.Count - 1
            If Trim(split(i)).StartsWith("class") Then
                Dim tokens = split(i).Split(" ")
                functionName = tokens(1)
                split(i) = split(i).Replace("{", ": public algorithmCPP" + " {")
                split(i) = split(i).Replace("class ", "class CPP_")
            End If

            If split(i).Contains("//") Then
                split(i) = split(i).Substring(0, InStr(split(i), "//") - 1)
            End If

            If Trim(split(i)).StartsWith(functionName) Then
                split(i) = split(i).Replace(functionName + "()", "CPP_" + functionName +
                                            "(int rows, int cols) : algorithmCPP(rows, cols) ") + vbCrLf
                split(i) += vbTab + "traceName = """ + "CPP_" + functionName + """;"
            End If

            split(i) = split(i).Replace("task.", "task->")
            split(i) = split(i).Replace("heartBeat()", "task->heartBeat")
            split(i) = split(i).Replace("firstPass", "task->firstPass")
            split(i) = split(i).Replace("setTrueText", "task->setTrueText")
            split(i) = split(i).Replace("gOptions.GridSize.value", "task->gridSize")
            split(i) = split(i).Replace("cv::", "")
            split(i) = split(i).Replace("std::", "")
            split(i) = split(i).Replace("const Mat& src", "Mat src")
            CPPrtb.Text += split(i) + vbCrLf
        Next
    End Sub
    Private Sub UpdateInfrastructure_Click(sender As Object, e As EventArgs) Handles UpdateInfrastructure.Click
        Dim input = New FileInfo("../../CPP_Classes/CPP_Names.h")
        Dim allNames = File.ReadAllLines(input.FullName)
        Dim buttonText = UpdateInfrastructure.Text
        Dim split = buttonText.Split(" ")
        Dim functionName = Trim(split(3))
        For Each line In allNames
            If line.Contains(functionName) Then
                MsgBox(functionName + " infrastructure is already present.")
                Exit Sub
            End If
        Next

        Dim sw = New StreamWriter(input.FullName)
        For Each line In allNames
            sw.WriteLine(line)
            If line.Contains("CPP_AddWeighted_Basics_") Then
                sw.WriteLine("""" + functionName + "_""" + ",")
            End If
        Next
        sw.Close()

        input = New FileInfo("../../CPP_Classes/CPP_Externs.h")
        Dim externs = File.ReadAllLines(input.FullName)
        sw = New StreamWriter(input.FullName)
        For Each line In externs
            sw.WriteLine(line)
            If line.Contains("new CPP_AddWeighted_Basics(rows, cols); break; }") Then
                sw.WriteLine(vbTab + "case " + functionName + "_" + " :")
                sw.WriteLine(vbTab + "{task->alg = new " + functionName + "(rows, cols); break; }")
            End If
        Next
        sw.Close()

        input = New FileInfo("../../CPP_Classes/CPP_IncludeOnly.h")
        Dim includeOnly = File.ReadAllLines(input.FullName)
        sw = New StreamWriter(input.FullName)
        For Each line In includeOnly
            sw.WriteLine(line)
            If line.Contains("CPP_AddWeighted_Basics_,") Then sw.WriteLine(functionName + "_,")
        Next
        sw.Close()
    End Sub
End Class