Imports System.IO
' this translator is hacked together from converting the 30 lines of an algorithm to C++
' There is no formal approach used here.  It is just a empirical - what do we need - approach.
' If you want a thorough VB.Net to C++ translator, consider this product:
'               https://www.tangiblesoftwaresolutions.com/product_details/vb_to_cplusplus_converter_details.html
Public Class VB_to_CPP
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim input = New FileInfo("../../data/VBKeywords.txt")
        Dim keys = File.ReadAllLines(input.FullName)
        For Each line In keys
            line = line.Trim
            If Len(line) = 0 Then Continue For
            vbKeywords.Add(line)
        Next
        vbKeywords.Add("New")
        vbKeywords.Add("New()")
        vbKeywords.Add("End")
        vbKeywords.Add("Class")
        vbKeywords.Add("Sub")
        vbKeywords.Add("if(")
        vbKeywords.Add("For")
        vbKeywords.Add("Structure")
        vbKeywords.Add("In")

        input = New FileInfo("../../data/OpenCVAPI.txt")
        Dim ocvLines = File.ReadLines(input.FullName)
        For Each line In ocvLines
            ocvKeywords.Add(line)
            line = UCase(line.Substring(0, 1)) + line.Substring(1)
            ocvbKeywords.Add(line)

        Next

        input = New FileInfo("../../data/AlgorithmList.txt")
        Dim algList = File.ReadAllLines(input.FullName)
        For i = 1 To algList.Count - 1
            Dim line = algList(i)
            If line.StartsWith("CPP_") Or line.EndsWith("_CPP") Then Continue For
            If line.EndsWith(".py") Then Continue For
            vbList.Items.Add(line)
        Next

        input = New FileInfo("../../VB_Classes/Options.vb")
        Dim optionsList = File.ReadAllLines(input.FullName)
        Dim splitstr1() As Char = {")", ","}
        Dim splitstr2() As Char = {" ", "_"}
        Dim split() As String
        For Each line In optionsList
            If line.Contains("Public Class") Then
                split = line.Split(splitstr2)
                vbModule = split(3)
            End If
            If line.Contains("sliders.setupTrackBar(") Then
                split = line.Split("""")
                Dim textVal = vbModule + " " + split(1)
                split = split(2).Trim.Split(splitstr1)
                sliderText.Add(textVal, split(2))
            End If
        Next
        vbList.Text = GetSetting("OpenCVB1", "TranslateToCPP", "TranslateToCPP", "Addweighted_Basics")
    End Sub
    Private Sub VB_to_CPP_Resize(sender As Object, e As EventArgs) Handles Me.Resize

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
    End Sub
End Class