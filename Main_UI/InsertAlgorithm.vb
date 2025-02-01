Imports  System.IO
Public Class InsertAlgorithm
    Dim VBoutputName As FileInfo
    Dim CPPoutputName As FileInfo
    Dim IncludeOnlyOutputName As FileInfo
    Dim OpenGLOutputName As FileInfo

    Dim sw As StreamWriter
    Dim vbSnippet() As String
    Dim cppSnippet() As String
    Public Enum algType
        addVB = 1
        addCPP = 2
        addCS = 3
        addPyStream = 4
        addOpenGL = 5
        addCPP_AI = 6
    End Enum
    Private Function nextAlgorithm(algorithmType As algType) As Boolean
        If InStr(AlgorithmName.Text, "_") = False Then
            MsgBox("The algorithm name must be of the form 'ModuleName_ClassName', i.e. AddWeighted_Basics")
            Return False
        End If

        Dim split = AlgorithmName.Text.Split("_")

        Dim ret As MsgBoxResult
        Select Case algorithmType
            Case algType.addVB
                VBoutputName = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes\" + split(0) + ".vb")

                ret = MsgBox("Would you like to add the algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + vbCrLf + vbCrLf +
                             " to: " + vbCrLf + vbCrLf + "VB File: " + VBoutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addCPP_AI
                ret = MsgBox("Would you like to add the C++ algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text +
                             vbCrLf + vbCrLf + " to: " + vbCrLf + vbCrLf + "File: CPP_NativeClasses.h?",
                             MsgBoxStyle.OkCancel)


            Case algType.addCPP
                VBoutputName = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes\" + split(0) + ".vb")

                CPPoutputName = New FileInfo(Main_UI.HomeDir.FullName + "CPP_Native\CPP_NativeClasses.h")

                ret = MsgBox("Would you like to add the C++ algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + "_VB" +
                             vbCrLf + vbCrLf + " to: " + vbCrLf + vbCrLf + "VB File: " + VBoutputName.Name +
                             vbCrLf + vbCrLf + " and to:" + vbCrLf + vbCrLf + CPPoutputName.Name, MsgBoxStyle.OkCancel)

            Case algType.addOpenGL
                VBoutputName = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes\OpenGL.vb")
                OpenGLOutputName = New FileInfo(Main_UI.HomeDir.FullName + "OpenGL\OpenGLFunction\OpenGLFunction.cpp")
                ret = MsgBox("Would you like to add the algorithm " + vbCrLf + vbCrLf + AlgorithmName.Text + "_VB" +
                             vbCrLf + vbCrLf + " to: " + vbCrLf + vbCrLf + "OpenGL C++ File: " +
                             OpenGLOutputName.Name + vbCrLf + vbCrLf + " and to:" + vbCrLf + vbCrLf +
                             VBoutputName.Name, MsgBoxStyle.OkCancel)

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
        If createVBfile Then sw.WriteLine("Imports cvb = OpenCvSharp") Else sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf)
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
        AlgorithmName.Text = Main_UI.AvailableAlgorithms.Text
        vbSnippet = File.ReadAllLines(Main_UI.HomeDir.FullName + "\OpenCVB.snippets\VB Class.snippet")
    End Sub
    Private Sub AddCPP_Click(sender As Object, e As EventArgs) Handles AddCPP.Click
        If AlgorithmName.Text.EndsWith("_cpp") Then AlgorithmName.Text = AlgorithmName.Text.Substring(1, Len(AlgorithmName.Text) - 4) + "_CPP"
        If AlgorithmName.Text.EndsWith("_CPP") = False Then AlgorithmName.Text = AlgorithmName.Text + "_CPP"
        If nextAlgorithm(algType.addCPP) = False Then Exit Sub

        Dim split = AlgorithmName.Text.Split("_")
        Dim nameNoCPP As String = split(0) + "_" + split(1)

        Dim createVBfile As Boolean
        If VBoutputName.Exists = False Then createVBfile = True

        Dim trigger As Boolean
        Dim cppCode As New List(Of String)
        sw = New StreamWriter(VBoutputName.FullName, True)
        If createVBfile Then
            sw.WriteLine("Imports cvb = OpenCvSharp")
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
        sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        For Each line In cppCode
            line = line.Replace("'//", "")
            sw.WriteLine(line)
        Next
        sw.Close()

        If createVBfile Then
            MsgBox("Be sure to add: " + VBoutputName.Name + " to the 'VB_Classes' project" + vbCrLf + vbCrLf +
                   "And edit the algorithm in:" + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        Else
            MsgBox("Edit the new algorithm in " + vbCrLf + vbCrLf + VBoutputName.Name + vbCrLf + vbCrLf + CPPoutputName.Name)
        End If
        Dim UIProcess As New Process
        UIProcess.StartInfo.FileName = Main_UI.HomeDir.FullName + "UI_Generator\bin\X64\Release\UI_Generator.exe"
        UIProcess.StartInfo.WorkingDirectory = Main_UI.HomeDir.FullName + "UI_Generator\bin\X64\Release\"
        UIProcess.StartInfo.Arguments = "All"
        UIProcess.Start()
        Me.Close()
    End Sub
    Private Sub AddOpenGL_Click(sender As Object, e As EventArgs) Handles AddOpenGL.Click
        If nextAlgorithm(algType.addOpenGL) = False Then Exit Sub

        Dim oglAll() As String
        oglAll = File.ReadAllLines(Main_UI.HomeDir.FullName + "OpenGL\OpenGL_Functions\OpenGL_Functions.cpp")

        Dim maxCase As Integer
        For i = 0 To oglAll.Count - 1
            If InStr(oglAll(i), "case ") Then
                Dim split = Trim(oglAll(i)).Split(" ")
                split(1) = split(1).Replace(":", "")
                maxCase = split(1)
            End If
        Next

        ' add the new case in OpenGL_Function.cpp
        sw = New StreamWriter(Main_UI.HomeDir.FullName + "OpenGL\OpenGL_Functions\OpenGL_Functions.cpp")
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
                sw.WriteLine(vbTab + vbTab + vbTab + "drawPointCloudRGB();")
                sw.WriteLine(vbTab + vbTab + vbTab + "glDisable(GL_TEXTURE_2D);")
                sw.WriteLine(vbTab + vbTab + vbTab + "// add code here...")
                sw.WriteLine(vbTab + vbTab + vbTab + "break;")
            End If
        Next
        sw.Close()

        VBoutputName = New FileInfo(Main_UI.HomeDir.FullName + "VB_Classes\OpenGL.vb")
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
    Private Sub InsertAlgorithm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub Add_AI_Generated_Click(sender As Object, e As EventArgs) Handles Add_AI_Generated.Click
        AlgorithmName.Text = AlgorithmName.Text.Replace("_CPP", "")
        AlgorithmName.Text = AlgorithmName.Text.Replace("_CS", "")
        AlgorithmName.Text = AlgorithmName.Text.Replace("_cpp", "")
        AlgorithmName.Text = AlgorithmName.Text.Replace("_cs", "")
        If AlgorithmName.Text.EndsWith("_CPP") = False Then AlgorithmName.Text = AlgorithmName.Text + "_CPP"
        If nextAlgorithm(algType.addCPP_AI) = False Then Exit Sub

        CPPoutputName = New FileInfo(Main_UI.HomeDir.FullName + "CPP_Native\CPP_NativeClasses.h")

        Dim trigger As Boolean
        sw = New StreamWriter(CPPoutputName.FullName, True)
        sw.WriteLine(vbCrLf + vbCrLf + vbCrLf + vbCrLf)
        For i = 0 To cppSnippet.Count - 1
            Dim line = cppSnippet(i)
            If line.Trim.StartsWith("class") Then trigger = True
            If trigger Then
                If InStr(line, "Anyname") Then line = line.Replace("Anyname", AlgorithmName.Text)
                sw.WriteLine(line)
                If line.Trim.EndsWith("};") Then Exit For
            End If
        Next
        sw.Close()

        'Main_UI.setupNewCPPalgorithm(AlgorithmName.Text)

        MsgBox("Edit the new algorithm in CPP_NativeClasses.h")
        Me.Close()
    End Sub
End Class