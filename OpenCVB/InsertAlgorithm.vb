﻿Imports  System.IO
Public Class InsertAlgorithm
    Dim VBoutputName As FileInfo
    Dim CPPoutputName As FileInfo
    Dim CSharpOutputName As FileInfo
    Dim PyStreamOutputName As FileInfo
    Dim IncludeOnlyOutputName As FileInfo
    Dim OpenGLOutputName As FileInfo

    Dim sw As StreamWriter
    Dim vbSnippet() As String
    Dim cppSnippet() As String
    Dim cSharpSnippet() As String
    Dim pyStream() As String
    Public Enum algType
        addVB = 1
        addCPP = 2
        addCS = 3
        addPyStream = 4
        addIncludeOnly = 5
        addOpenGL = 6
    End Enum
    Private Function nextAlgorithm(algorithmType As algType) As Boolean
        If InStr(AlgorithmName.Text, "_") = False Then
            MsgBox("The algorithm name must be of the form 'ModuleName_ClassName', i.e. TEE_Basics")
            Return False
        End If
        Dim split = AlgorithmName.Text.Split("_")

        Dim ret As MsgBoxResult
        Select Case algorithmType
            Case algType.addVB
                VBoutputName = New FileInfo("..\..\VB_Classes\" + split(0) + ".vb")
                ret = MsgBox("Would you like to add the algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "VB File: " + VBoutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addCPP
                If split.Count <> 3 Then
                    MsgBox("A new C++ algorithm should have the form like: RedCloud_Test_CPP" + vbCrLf + "Try again...")
                    Return False
                End If
                VBoutputName = New FileInfo("..\..\VB_Classes\" + split(0) + ".vb")
                CPPoutputName = New FileInfo("..\..\CPP_Classes\" + split(0) + ".cpp")

                ret = MsgBox("Would you like to add the C++ algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "VB File: " + VBoutputName.Name + vbCrLf + vbCrLf +
                             " and to:" + vbCrLf + vbCrLf + CPPoutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addCS
                VBoutputName = New FileInfo("..\..\VB_Classes\" + split(0) + ".vb")
                CSharpOutputName = New FileInfo("..\..\CS_Classes\" + split(0) + ".cs")

                ret = MsgBox("Would you like to add the CSharp algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "VB File: " + VBoutputName.Name + vbCrLf + vbCrLf +
                             " and to:" + vbCrLf + vbCrLf + CSharpOutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addPyStream
                PyStreamOutputName = New FileInfo("..\..\VB_Classes\" + AlgorithmName.Text + "_PS.py")

                ret = MsgBox("Would you like to add the PyStream algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "Python File: " + PyStreamOutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addIncludeOnly
                If split.Length <> 3 Then
                    MsgBox("The algorithm name must be of the form 'CPP_ModuleName_ClassName', i.e. CPP_TEE_Basics")
                    Return False
                End If

                IncludeOnlyOutputName = New FileInfo("..\..\CPP_Classes\CPP_IncludeOnly.h")
                ret = MsgBox("Would you like to add " + AlgorithmName.Text + " to: " + vbCrLf + vbCrLf +
                             IncludeOnlyOutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addOpenGL
                VBoutputName = New FileInfo("..\..\VB_Classes\OpenGL.vb")
                OpenGLOutputName = New FileInfo("..\..\OpenGL\OpenGLFunction\OpenGLFunction.cpp")

                ret = MsgBox("Would you like to add the algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "OpenGL C++ File: " + OpenGLOutputName.Name + vbCrLf + vbCrLf +
                             " and to:" + vbCrLf + vbCrLf + VBoutputName.Name, MsgBoxStyle.OkCancel)
        End Select

        If ret = MsgBoxResult.Cancel Then Return False

        Return True
    End Function
    Private Sub AddVB_Click(sender As Object, e As EventArgs) Handles AddVB.Click
        If nextAlgorithm(algType.addVB) = False Then Exit Sub

        Dim createVBfile As Boolean
        If VBoutputName.Exists = False Then
            Dim ret = MsgBox("The file " + VBoutputName.FullName + " will be created." + vbCrLf + vbCrLf +
                             "When complete, add " + VBoutputName.Name + " to the VB_Classes project." + vbCrLf + vbCrLf +
                             "Is this OK?", MsgBoxStyle.OkCancel)
            If ret = MsgBoxResult.Cancel Then Exit Sub
            createVBfile = True
        End If
        sw = New StreamWriter(VBoutputName.FullName, True)
        If createVBfile Then sw.WriteLine("Imports cv = OpenCvSharp") Else sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        Dim trigger As Boolean
        For i = 0 To vbSnippet.Count - 1
            Dim line = vbSnippet(i)
            If InStr(line, "Public") Then trigger = True
            If InStr(line, "newClass_Basics") Then line = line.Replace("newClass_Basics", AlgorithmName.Text)
            If InStr(line, "End Class") Then
                sw.Write(line)
                Exit For
            End If
            If trigger Then sw.WriteLine(line)
        Next
        sw.Close()

        If createVBfile Then
            MsgBox("Be sure to add: " + VBoutputName.Name + vbCrLf + vbCrLf + "to the 'VB_Classes' project" + vbCrLf + vbCrLf +
                   "and Edit " + AlgorithmName.Text + vbCrLf + vbCrLf + VBoutputName.Name + " in 'VB_Classes' project")
        Else
            MsgBox("Edit " + AlgorithmName.Text + vbCrLf + vbCrLf + VBoutputName.Name + " in 'VB_Classes' project")
        End If
        Me.Close()
    End Sub
    Private Sub AddAlgorithm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AlgorithmName.Text = OpenCVB.AvailableAlgorithms.Text
        vbSnippet = File.ReadAllLines("..\..\OpenCVB.snippets\VB_Class - new Class.snippet")
        cppSnippet = File.ReadAllLines("..\..\OpenCVB.snippets\CPP Class - new C++.snippet")
        cSharpSnippet = File.ReadAllLines("..\..\OpenCVB.snippets\CSharp_Class - new Class.snippet")
        pyStream = File.ReadAllLines("..\..\VB_Classes\z_AddWeighted_PS.py")
    End Sub
    Private Sub AddCPP_Click(sender As Object, e As EventArgs) Handles AddCPP.Click
        If AlgorithmName.Text.EndsWith("_cpp") Then AlgorithmName.Text = AlgorithmName.Text.Substring(1, Len(AlgorithmName.Text) - 4) + "_CPP"
        If AlgorithmName.Text.EndsWith("_CPP") = False Then AlgorithmName.Text = AlgorithmName.Text + "_CPP"
        If nextAlgorithm(algType.addCPP) = False Then Exit Sub

        Dim split = AlgorithmName.Text.Split("_")
        Dim nameNoCPP As String = split(0) + "_" + split(1)

        Dim createCPPfile As Boolean, createVBfile As Boolean
        If CPPoutputName.Exists = False Then createCPPfile = True
        If VBoutputName.Exists = False Then createVBfile = True

        Dim trigger As Boolean
        Dim cppCode As New List(Of String)
        sw = New StreamWriter(VBoutputName.FullName, True)
        If createVBfile Then
            sw.WriteLine("Imports cv = OpenCvSharp")
            sw.WriteLine("Imports System.Runtime.InteropServices")
        Else
            sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        End If

        For i = 0 To cppSnippet.Count - 1
            Dim line = cppSnippet(i)
            If InStr(line, "Public") Then trigger = True
            If InStr(line, "Anyname") Then line = line.Replace("Anyname", nameNoCPP)
            If line.StartsWith("'//") Then
                If line.Contains("#include") = False And line.Contains("namespace") = False Then cppCode.Add(line)
            End If
            If InStr(line, "End Module") Then
                sw.Write(line)
                Exit For
            End If
            If trigger Then sw.WriteLine(line)
        Next
        sw.Close()

        sw = New StreamWriter(CPPoutputName.FullName, True)
        If createCPPfile Then
            sw.WriteLine("#include <cstdlib>")
            sw.WriteLine("#include <cstdio>")
            sw.WriteLine("#include <iostream>")
            sw.WriteLine("#include <algorithm>")
            sw.WriteLine("#include <opencv2/core.hpp>")
            sw.WriteLine("#include <opencv2/ximgproc.hpp>")
            sw.WriteLine("#include <opencv2/highgui.hpp>")
            sw.WriteLine("#include <opencv2/core/utility.hpp>")
            sw.WriteLine("using namespace std;")
            sw.WriteLine("using namespace  cv;")
        Else
            sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        End If
        For Each line In cppCode
            line = line.Replace("'//", "")
            sw.WriteLine(line)
        Next
        sw.Close()

        If createCPPfile And createVBfile Then
            MsgBox("Be sure to add: " + CPPoutputName.Name + vbCrLf + vbCrLf + "to the 'CPP_Classes' project" + vbCrLf + vbCrLf +
                   "And add " + VBoutputName.Name + " to the 'VB_Classes' project" + vbCrLf + vbCrLf +
                   "And edit the algorithm in:" + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        ElseIf createCPPfile Then
            MsgBox("Be sure to add: " + CPPoutputName.Name + vbCrLf + vbCrLf + "to the 'CPP_Classes' project" + vbCrLf + vbCrLf +
                   "And edit the algorithm in:" + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        ElseIf createVBfile Then
            MsgBox("Be sure to add: " + VBoutputName.Name + " to the 'VB_Classes' project" + vbCrLf + vbCrLf +
                   "And edit the algorithm in:" + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        Else
            MsgBox("Edit the new algorithm in " + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        End If
        Me.Close()
    End Sub
    Private Sub AddOpenGL_Click(sender As Object, e As EventArgs) Handles AddOpenGL.Click
        If nextAlgorithm(algType.addOpenGL) = False Then Exit Sub

        Dim oglAll() As String
        oglAll = File.ReadAllLines("..\..\OpenGL\OpenGL_Functions\OpenGL_Functions.cpp")

        Dim maxCase As Integer
        For i = 0 To oglAll.Count - 1
            If InStr(oglAll(i), "case ") Then
                Dim split = Trim(oglAll(i)).Split(" ")
                split(1) = split(1).Replace(":", "")
                maxCase = split(1)
            End If
        Next

        ' add the new case in OpenGL_Function.cpp
        sw = New StreamWriter("..\..\OpenGL\OpenGL_Functions\OpenGL_Functions.cpp")
        For i = 0 To oglAll.Count - 1
            Dim line = Trim(oglAll(i))
            sw.WriteLine(line)
            If InStr(line, "case " + CStr(maxCase)) Then
                While InStr(line, "break;") = False
                    i += 1
                    line = Trim(oglAll(i))
                    sw.WriteLine(line)
                End While
                sw.WriteLine(vbTab + vbTab + "}")
                sw.WriteLine(vbTab + "case " + CStr(maxCase + 1) + ":" + " // oglFunction = oCase.function" + CStr(maxCase + 1))
                sw.WriteLine(vbTab + vbTab + "{")
                sw.WriteLine(vbTab + vbTab + vbTab + "drawPointCloud();")
                sw.WriteLine(vbTab + vbTab + vbTab + "glDisable(GL_TEXTURE_2D);")
                sw.WriteLine(vbTab + vbTab + vbTab + "// add code here...")
                sw.WriteLine(vbTab + vbTab + vbTab + "break;")
            End If
        Next
        sw.Close()

        VBoutputName = New FileInfo("..\..\VB_Classes\OpenGL.vb")
        Dim sr = New System.IO.StreamReader(VBoutputName.FullName)
        Dim vbCode As New List(Of String)
        While sr.EndOfStream = False
            Dim line = sr.ReadLine
            vbCode.Add(line)
            If line.Contains("Public Enum oCase") Then
                While 1
                    line = sr.ReadLine
                    If line.Contains("End Enum") Then
                        vbCode.Add("function" + CStr(maxCase + 1) + " = " + CStr(maxCase + 1))
                        vbCode.Add(line)
                        Exit While
                    End If
                    vbCode.Add(line)
                End While
            End If
        End While
        sr.Close()

        vbCode.Add(vbCrLf + vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        Dim trigger As Boolean
        For i = 0 To vbSnippet.Count - 1
            Dim line = vbSnippet(i)
            If InStr(line, "Public") Then trigger = True
            If InStr(line, "newClass_Basics") Then line = line.Replace("newClass_Basics", AlgorithmName.Text)
            If trigger Then vbCode.Add(line)
            If InStr(line, "RunVB") Then
                vbCode.Add(vbTab + vbTab + "task.ogl.pointCloudInput = task.pointCloud")
                vbCode.Add(vbTab + vbTab + "task.ogl.Run(src)")
            End If
            If InStr(line, "End Class") Then Exit For
            If InStr(line, "New()") Then
                vbCode.Add(vbTab + vbTab + "task.ogl.oglFunction = oCase.function" + CStr(maxCase + 1))
                vbCode.Add(vbTab + vbTab + "task.OpenGLTitle = ""OpenGL_Functions""")
            End If
            ' If line.StartsWith("Public Class ") Then vbCode.Add("Dim ogl as New OpenGL_Basics")
        Next

        sw = New StreamWriter(VBoutputName.FullName, False)
        For Each line In vbCode
            sw.WriteLine(line)
        Next
        sw.Close()
        MsgBox("Edit the new algorithm " + AlgorithmName.Text + " in: " + vbCrLf + vbCrLf + "OpenGL.vb (project VB_Classes)" + vbCrLf + vbCrLf + "and in: " + vbCrLf + vbCrLf +
               "OpenGL_Functions.cpp (project OpenGL/OpenGL_Functions) ")
    End Sub
    Private Sub AddCSharp_Click(sender As Object, e As EventArgs) Handles AddCSharp.Click
        MsgBox("It is easier to just use OpenCVB's C# snippet." + vbCrLf + "Add a C# module with the new algorithm and add a new" +
               vbCrLf + "VB algorithm that interfaces to the new C# code.")
    End Sub
    Private Sub AddPyStream_Click(sender As Object, e As EventArgs) Handles AddPyStream.Click
        If nextAlgorithm(algType.addPyStream) = False Then Exit Sub

        Dim pyFile = New FileInfo("..\..\VB_Classes\" + AlgorithmName.Text + "_PS.py")
        Dim alreadyPresent As Boolean
        If pyFile.Exists Then
            Dim ret = MsgBox(pyFile.FullName + " exists." + vbCrLf + "Do you want to overwrite it?", MsgBoxStyle.OkCancel, "Add PyStream Algorithm")
            If ret = MsgBoxResult.Cancel Then Exit Sub
            alreadyPresent = True
        End If
        sw = New StreamWriter(pyFile.FullName)
        For i = 0 To pyStream.Count - 1
            Dim line = pyStream(i)
            If InStr(line, "AddWeighted_PS.py") Then line = line.Replace("AddWeighted_PS.py", pyFile.Name)
            sw.WriteLine(line)
        Next
        sw.Close()
        If alreadyPresent = False Then
            MsgBox(pyFile.Name + " has been prepared." + vbCrLf + vbCrLf + "Add '" + pyFile.Name + "' to the VB_Classes project." + vbCrLf +
               "Once added, restart OpenCVB and it will appear.")
        Else
            MsgBox(pyFile.Name + " is ready to run.")
        End If
        Me.Close()
    End Sub
    Private Sub InsertAlgorithm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub AddIncludeOnly_Click(sender As Object, e As EventArgs) Handles AddIncludeOnly.Click
        If AlgorithmName.Text.StartsWith("cpp_") Then AlgorithmName.Text = AlgorithmName.Text.Substring(4) + "CPP_"
        If AlgorithmName.Text.StartsWith("CPP_") = False Then AlgorithmName.Text = "CPP_" + AlgorithmName.Text
        If nextAlgorithm(algType.addIncludeOnly) = False Then Exit Sub

        Dim split = AlgorithmName.Text.Split("_")
        Dim nameNoCPP As String = split(1) + "_" + split(2)

        VBoutputName = New FileInfo("..\..\VB_Classes\CPP.vb") ' Touch a VB.Net to force recompile of VB_Classes.
        Dim cppVBcode = File.ReadAllLines(VBoutputName.FullName)
        sw = New StreamWriter(VBoutputName.FullName, True)
        sw.Write(" ") ' force a recompile of the VB_Classes to trigger the UIGenerator 
        sw.Close()

        Dim incOnlyName = New FileInfo("..\..\CPP_Classes\CPP_IncludeOnly.h")
        Dim incOnlycode = File.ReadAllLines(incOnlyName.FullName)
        sw = New StreamWriter(incOnlyName.FullName)
        Dim enumTrigger As Boolean
        For Each line In incOnlycode
            If InStr(line, "enum functions") Then enumTrigger = True
            If enumTrigger Then
                If InStr(line, "fStable_BasicsCount,") Then
                    sw.WriteLine("f" + nameNoCPP + ",")
                    enumTrigger = False
                End If
            End If
            sw.WriteLine(line)
        Next

        sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf + "class CPP_" + nameNoCPP + " : public algorithmX")
        sw.WriteLine("{")
        sw.WriteLine("private: ")
        sw.WriteLine("public: ")
        sw.WriteLine(vbTab + "CPP_" + nameNoCPP + "(int rows, int cols) : algorithmX(rows, cols) " + vbCrLf + "{" +
                     vbCrLf + vbTab + "traceName = """ + "CPP_" + nameNoCPP + """;" + vbCrLf + vbTab + "}")
        sw.WriteLine(vbTab + "void Run(Mat src)")
        sw.WriteLine(vbTab + "{")
        sw.WriteLine(vbTab + vbTab + "dst2 = src.clone();")
        sw.WriteLine(vbTab + "}")
        sw.WriteLine("};")

        sw.Close()

        Dim externName = New FileInfo("..\..\CPP_Classes\CPP_Externs.h")
        Dim externCode = File.ReadAllLines(externName.FullName)
        sw = New StreamWriter(externName.FullName)
        For Each line In externCode
            If line.Contains("end of switch") Then
                sw.WriteLine(vbTab + "case f" + nameNoCPP + " :")
                sw.WriteLine(vbTab + "{task->alg = new CPP_" + nameNoCPP + "(rows, cols);break;}")
            End If
            sw.WriteLine(line)
        Next

        sw.Close()

        MsgBox("Restart OpenCVB to use: " + AlgorithmName.Text + vbCrLf + vbCrLf + "Edit " + AlgorithmName.Text + " in: " + vbCrLf + vbCrLf +
               "CPP_Classes\CPP_IncludeOnly.h")
        Me.Close()
    End Sub
End Class