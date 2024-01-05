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
        Me.Height = 1000
        CPPrtb.Width = VBrtb.Width
        CPPrtb.Height = Me.Height - 190
        VBrtb.Height = CPPrtb.Height
        Me.Width = CPPrtb.Width + VBrtb.Width + 50
    End Sub
    Private Sub vbList_SelectedValueChanged(sender As Object, e As EventArgs) Handles vbList.SelectedValueChanged
        SaveSetting("OpenCVB1", "TranslateToCPP", "TranslateToCPP", vbList.Text)
        algorithmVBName = vbList.Text
        fIndexName = "f" + algorithmVBName
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

        VBrtb.Text = vbInput(vbIndex) + vbCrLf
        Dim tabCount = 1
        Dim tabs = ""
        vbLines.Add(vbInput(vbIndex))
        For vbIndex = vbIndex + 1 To vbInput.Count - 1
            Dim line = vbInput(vbIndex).Trim
            If line.Contains("End Class") Then tabCount = 0
            If line.Contains("End Sub") Then tabCount -= 1
            If line.Contains("End If") Then tabCount -= 1
            If line.StartsWith("Next") Then tabCount -= 1
            If tabCount < 0 Then tabCount = 0

            tabs = StrDup(tabCount, vbTab)
            VBrtb.Text += tabs + line + vbCrLf

            If line.EndsWith(" Then") Then tabCount += 1
            If line.Contains(" Sub ") Then tabCount += 1
            If line.Contains("For ") Then tabCount += 1
            If line.Contains(" Function ") Then tabCount += 1

            If line.Contains("'") Then line = line.Trim.Substring(0, InStr(line, "'") - 1)
            If line.Length = 0 Then Continue For
            If line.Contains("Options_") Then Continue For
            If line.Contains("options.Run") Then Continue For
            If line.Contains("cPtr") Then Continue For
            If line.Contains(".Free") Then Continue For
            If line.Contains("GCHandle.Alloc") Then Continue For
            If line.Contains("Marshal.") Then Continue For
            If line.Contains("AddrOfPinnedObject") Then Continue For
            vbLines.Add(line)

            If line.Contains("Public Sub Close()") Then
                line = line + vbCrLf + tabs +
                line = "End Class" ' Close subroutine is always at the end of the class
            End If

            If InStr(line, "End Class") Then Exit For
        Next

        Dim variables As New List(Of String)
        Dim stringConstants As New List(Of String)
        Dim numericConstants As New List(Of String)
        Dim splitstr() As Char = {"""", " ", "(", ")", ",", ":"}
        tabCount = 1
        For Each line In vbLines
            If line.Contains("""") Then
                Dim offset = InStr(line, """")
                Dim strLine = line.Substring(offset)
                line = line.Trim.Substring(0, offset - 1)
                While 1
                    offset = InStr(strLine, """")
                    If offset = 0 Then Exit While
                    stringConstants.Add(strLine.Substring(0, offset - 1))
                    strLine = strLine.Substring(offset)
                    If strLine.Contains("""") = False Then Exit While
                    strLine = strLine.Substring(InStr(strLine, """"))
                End While
            End If
            split = line.Trim.Split(splitstr)
            For i = 0 To split.Length - 1
                split(i) = split(i).Replace("cv.ColorConversionCodes.", "")
                If vbList.Items.Contains(split(i)) Then Continue For
                If split(i).Contains("RunVB") Then Continue For
                If split(i).Contains(".Run") Then Continue For
                If split(i).Contains(".") Then Continue For
                If IsNumeric(split(i)) Then
                    numericConstants.Add(split(i))
                Else
                    If variables.Contains(split(i)) = False Then
                        If vbKeywords.Contains(split(i)) = False And split(i) <> "" Then
                            variables.Add(split(i))
                        End If
                    End If
                End If
            Next
        Next

        ' output the C++ code.
        cppLines.Clear()

        cppLines.Add("class " + CPPName + " : public algorithmCPP")
        cppLines.Add("{")
        cppLines.Add("private:")
        cppLines.Add("public:")
        tabCount = 1

        vbLines.RemoveAt(0)
        Dim tabList As New List(Of String)
        Dim lastLine As String = ""
        For Each line In vbLines
            Dim semiColon = ""
            Dim nextLine = line
            If line.StartsWith("End ") Or line.Trim = "Next" Then tabCount -= 1
            If line.Contains("End Select") Then tabCount -= 1
            If line.StartsWith("Case") And lastLine.Contains("Select Case") = False Then tabCount -= 1
            If line.Contains(" Sub ") Or line.Contains(" Function ") Or line.EndsWith(" Then") Then
                nextLine += " {"
                semiColon = ""
            End If

            If line.Contains("End Class") Then
                tabList.Add("")
                cppLines.Add("};")
                Exit For
            End If

            If tabCount > 0 Then tabs = StrDup(tabCount, vbTab) Else tabs = ""

            Dim currTabCount = tabCount
            If line.StartsWith("Case") Then tabCount += 1
            If line.EndsWith(" Then") Then tabCount += 1
            If line.StartsWith("For ") Then tabCount += 1
            If line.Contains(" Sub ") Or line.Contains(" Function ") Then tabCount += 1
            If tabCount = currTabCount Then semiColon = ";"
            If line.StartsWith("End ") Or line.StartsWith("Next") Then
                If line.Contains("End Select") Then
                    nextLine = tabs + line
                    tabCount += 1
                Else
                    nextLine = tabs + "}"
                End If
            Else
                nextLine = tabs + nextLine + semiColon
            End If

            tabList.Add(tabs)
            cppLines.Add(nextLine)
            lastLine = line
        Next

        ' translate the VB.Net variable definitions to C++
        splitstr = {"""", " ", "(", ")", ",", ":", ";"}
        Dim initializeList As New List(Of String)
        Dim runList As New List(Of String)
        For i = 0 To cppLines.Count - 1
            Dim original = cppLines(i)
            split = cppLines(i).Trim.Split(splitstr)
            Dim algorithmName = If(split.Length > 2, split(split.Length - 2), "")
            If vbList.Items.Contains(algorithmName) Then
                cppLines(i) = countTabs(original) + "CPP_" + algorithmName + " *" + split(1) + ";"
                initializeList.Add(split(1) + " = new CPP_" + algorithmName + "(rows, cols);")
                runList.Add(split(1))
            Else
                cppLines(i) = Trim(cppLines(i))
            End If

            If original.Contains("If ") And original.Contains(" Then") Then
                original = original.Replace("If ", "if (")
                cppLines(i) = original.Replace(" Then", ")")
                Dim offset = InStr(original, " Then")
                If offset > 0 Then
                    Dim part1 = cppLines(i).Substring(0, offset + 1)
                    Dim part2 = cppLines(i).Substring(offset + 1)
                    part1 = part1.Replace(" = ", " == ")
                    cppLines(i) = part1 + part2
                End If
                If original.Contains(" Else") Then
                    cppLines(i) = cppLines(i).Replace(" Else", "; else")
                End If
            End If
            If original.Contains("Else") Then
                cppLines(i) = cppLines(i).Replace(vbTab + "Else;", "} else {")
            End If
        Next

        ' convert the constructor to C++
        For i = 0 To cppLines.Count - 1
            If cppLines(i).Contains("Public Sub New()") Then
                For j = initializeList.Count - 1 To 0 Step -1
                    cppLines.Insert(i + 1, tabList(i) + initializeList(j))
                Next
                Exit For
            End If
        Next

        ' Build the C++ constructor and Run
        For i = 0 To cppLines.Count - 1
            Dim line = cppLines(i)
            Dim offset = InStr(line, "Public Sub New()")
            tabs = countTabs(cppLines(i))
            If offset > 0 Then
                cppLines(i) = line.Substring(0, offset - 1) + CPPName + "(int rows, int cols) : algorithmCPP(rows, cols) {" +
                              vbCrLf + tabs + vbTab + "traceName = """ + CPPName + """;"
            End If

            offset = InStr(line, "Public Sub RunVB(src as cv.Mat)")
            If offset > 0 Then cppLines(i) = line.Substring(0, offset - 1) + "void Run(Mat src) {"

            For j = 0 To runList.Count - 1
                If line.Contains(runList(j) + ".") Then
                    cppLines(i) = cppLines(i).Replace(runList(j) + ".", runList(j) + "->")
                End If
            Next

            cppLines(i) = cppLines(i).Replace("cv.ColorConversionCodes.", "COLOR_")
            cppLines(i) = cppLines(i).Replace("task.", "task->")
        Next

        ' find the slider references
        Dim activeSliderVariable As New List(Of String)
        Dim activesliderValue As New List(Of String)
        Dim splitstrComma() As Char = {" ", ",", ")"}
        For i = 0 To cppLines.Count - 1
            If i < cppLines.Count Then
                Dim line = cppLines(i)
                If line.Contains("sliders.setupTrackBar(") Then
                    split = line.Split("""")
                    Dim sText = split(1)
                    split = split(2).Split(splitstrComma)
                    sliderText.Add(vbModule + " " + sText, split(split.Length - 2))
                    Continue For
                End If
                If line.Contains("= findSlider") Then
                    split = line.Split("""")
                    Dim val = findDefaultValue(split(1))
                    split = line.Trim.Split(" ")
                    activeSliderVariable.Add(split(1))
                    activesliderValue.Add(val)
                End If
            End If
        Next

        ' remove the slider references.
        For i = cppLines.Count - 1 To 0 Step -1
            If i < cppLines.Count Then
                Dim line = cppLines(i)
                If line.Contains("sliders.") Or line.Contains("= findSlider(") Then cppLines.RemoveAt(i)
                If line.Trim = "}" Then
                    If cppLines(i - 1).Contains("sliders.") Then cppLines.RemoveAt(i)
                End If
            End If
        Next

        ' set the slider variable to the default value
        For i = 0 To cppLines.Count - 1
            cppLines(i) = cppLines(i).Replace(".Channels", ".channels()")
            For j = 0 To activeSliderVariable.Count - 1
                If cppLines(i).Contains(activeSliderVariable(j) + ".Value") Then
                    cppLines(i) = cppLines(i).Replace(activeSliderVariable(j) + ".Value", activesliderValue(j))
                End If
            Next
        Next

        For i = 0 To cppLines.Count - 1
            If cppLines(i).Contains("Select Case ") Then i = buildSwitch(i)
            If cppLines(i).Contains("InRange(") Then
                cppLines(i) = prepInRange(cppLines(i))
                Continue For
            End If
            If cppLines(i).Contains(".Rectangle(") Then
                cppLines(i) = prepRectangle(cppLines(i))
                'Continue For
            End If
        Next

        ' a variety of cleanup
        Dim constructorLines As String = ""
        Dim pastConstructor As Boolean = False
        For i = 0 To cppLines.Count - 1
            Dim line = cppLines(i)
            Dim trimline = line.Trim()
            tabs = countTabs(line)

            If line.Contains(".CountNonZero") Then
                line = prepCountNonZero(line)
            Else
                line = line.Replace(".Count", ".size()")
            End If

            If line.Contains("New List(Of List(Of ") Then
                line = line.Replace("New List(Of List(Of ", "vector<vector<")
                line = line.Replace("))", ">>")
            End If
            line = line.Replace("basics->options_resizePercent", "resizepercent")
            line = line.Replace("options.warpFlag", "InterpolationFlags::INTER_CUBIC")
            line = line.Replace("dst2 = src.Resize(", "resize(src, dst2,")
            line = line.Replace("task->AddWeighted", "task->addWeightPercent / 100.0")
            line = line.Replace("vbNormalize32f", "task->normalize32f")
            line = line.Replace("dst2.Circle(", "circle(dst2, ")
            line = line.Replace("msRNG.Next(range.X, range.X + range.Width)", "range.x + float((rand() % range.width))")
            line = line.Replace("msRNG.Next(range.Y, range.Y + range.Height)", "range.y + float((rand() % range.height))")
            If line.Contains("New SortedList") Then line = findSortedList(line, tabs)
            If line.Contains(" Sub ") Then line = findSub(line, tabs)

            line = line.Replace("Dim ranges() = ", "Range ranges[] = ")
            line = line.Replace("New cv.Rangef", "Range")
            line = updateDeclaration(line)
            line = line.Replace("Single.IsNaN", "isnan")
            line = line.Replace("Double.IsNaN", "isnan")
            line = line.Replace("cv.MatType.", "")
            line = line.Replace("MatType.", "")
            line = line.Replace("setTrueText", "task->setText")
            If line.Contains("setText") And line.Contains(""");") Then
                line = line.Replace(""");", """, dst2);")
            End If

            line = line.Replace(" False", " false")
            line = line.Replace(" True", " true")

            line = line.Replace("Add(", "add(")
            line = line.Replace("cv.Cv2.", "")
            line = line.Replace("Cv2.", "")
            line = opencvAPIs(line)

            line = line.Replace(".Width", ".width")
            line = line.Replace(".Height", ".height")
            line = line.Replace(".Rows", ".rows")
            line = line.Replace(".Cols", ".cols")
            line = line.Replace("Static ", "static auto ")
            line = line.Replace(" = New cv.Size(", " = Size(")
            line = line.Replace(" = New cv.Rect(", " = Rect(")

            If line.Contains("traceName") Then pastConstructor = True
            If line.Contains(" = New Mat(") Then
                line = line.Replace(" = New Mat(", " = Mat(")
                If line.EndsWith(", 0);") Then
                    trimline = trimline.Replace("Public ", "")
                    split = trimline.Split(" ")
                    line = line.Replace(", 0);", ");")
                    Dim setToLine = tabs + split(0) + ".setTo(0);"
                    If pastConstructor Then line += setToLine Else constructorLines += vbCrLf + vbTab + setToLine
                End If
            End If

            line = line.Replace(" = New cv.Scalar", " = Scalar")
            line = line.Replace("cv.Scalar", "Scalar")

            line = validateDST(line)
            If line.Contains("For Each pt ") Then
                split = line.Split(" ")
                line = tabs + "for (Point2f " + split(2) + " : " + split(4) + ") {"
            End If
            If line.Contains("For Each rc ") Then
                split = line.Split(" ")
                line = tabs + "for (rcData " + split(2) + " : " + split(4) + ") {"
            End If
            line = markForLoop(line)

            line = line.Replace(".Clone", ".clone()")
            line = line.Replace(".Size", ".size()")
            line = line.Replace(".Total", ".total()")
            line = line.Replace(".ElemSize", ".elemSize()")
            line = line.Replace(".Type", ".type()")
            line = line.Replace("CInt(", "int(")
            line = line.Replace("CSng(", "float(")
            line = line.Replace("CDbl(", "double(")

            line = line.Replace("Line(", "line(")
            line = line.Replace("New cv.Point", "Point")

            line = line.Replace(vbTab + "Dim ", vbTab + "auto ")
            If line.Contains("Choose(") Then line = "// " + line + "// <<<<< build an array and index it."

            line = validateNorms(line)
            line = line.Replace("task->desc", "desc")

            line = line.Replace(".add(", ".push_back(")
            line = line.Replace(".Clear()", ".clear()")
            line = line.Replace(" And ", " && ")
            line = line.Replace(" Or ", " || ")

            line = line.Replace(" <> ", " != ")
            line = line.Replace(".SetTo(", ".setTo(")
            line = line.Replace("task->highlightColor", "YELLOW")
            line = line.Replace("(i)", "[i]")
            line = line.Replace("aaOptions.GridSize.Value", "task->gridSize")
            line = line.Replace("aaOptions.gravityPointCloud.Checked", "task->gravityPointCloud")

            line = line.Replace("options.", "options_")
            line = line.Replace(".ToMat", "")
            line = line.Replace("Math.Abs", "abs")
            line = line.Replace("Math.Cos", "cos")
            line = line.Replace("Math.Sin", "sin")
            line = line.Replace("Return ", "return ")
            line = line.Replace(" Is Nothing", " == NULL")
            If line.Contains("Public Sub Close() {") Then line = "};" ' special case when there is a close subroutine

            If line.Contains(".cvtColor") Then
                split = line.Split(".= ()".ToCharArray)
                If split(split.Length - 2).StartsWith("COLOR_") Then
                    Dim offset = InStr(line, "cvtColor")
                    Dim src = split(split.Length - 4)
                    Dim dst = split(split.Length - 7)
                    Dim part1 = line.Substring(0, offset - src.Length - dst.Length - 5)
                    line = part1 + "cvtColor(" + src + ", " + dst.Trim + ", " + split(split.Length - 2) + ");"
                End If
            End If
            If line.Contains(".convertScaleAbs") Then
                split = line.Trim.Split(". ()".ToCharArray)
                If split(split.Length - 3).StartsWith("convertScaleAbs") And split(split.Length - 5) = "=" Then
                    Dim src = split(split.Length - 4)
                    Dim dst = split(split.Length - 6)
                    Dim offset = InStr(line, dst + " = ")
                    Dim part1 = line.Substring(0, offset - 1)
                    line = part1 + "convertScaleAbs(" + src + ", " + dst + ");"
                End If
            End If

            If line.Contains("approxPolyDP(") Then
                line = line.Replace("approxPolyDP(", "approxPolyDP ")
                line = line.Replace(");", " ;")
                split = line.Trim.Split(".= ".ToCharArray)
                Dim bumpIndex As Integer
                If split(0) = "auto" Then bumpIndex = 1
                For j = 0 To split.Length - 1
                    If split(j).Contains("approxPolyDP") Then
                        line = tabs + "approxPolyDP(" + split(j + 1) + " " + split(bumpIndex) + ", " + split(j + 2) +
                               " " + split(j + 3) + ");"
                        If bumpIndex Then line = tabs + "Mat " + split(bumpIndex) + ";" + vbCrLf + line
                        Exit For
                    End If
                Next
            End If

            line = line.Replace("cv.ThresholdTypes.Binary", "THRESH_BINARY")
            line = line.Replace("cv.ThresholdTypes.BinaryInv", "THRESH_BINARY_INV")
            line = line.Replace("cv.ThresholdTypes.Trunc", "THRESH_TRUNC")
            line = line.Replace("cv.ThresholdTypes.Tozero", "THRESH_TOZERO")
            line = line.Replace("cv.ThresholdTypes.Triangle", "THRESH_TRIANGLE")
            line = line.Replace("cv.ThresholdTypes.Otsu", "THRESH_OTSU")

            line = line.Replace("ThresholdTypes.Binary", "THRESH_BINARY")
            line = line.Replace("ThresholdTypes.BinaryInv", "THRESH_BINARY_INV")
            line = line.Replace("ThresholdTypes.Trunc", "THRESH_TRUNC")
            line = line.Replace("ThresholdTypes.Tozero", "THRESH_TOZERO")
            line = line.Replace("ThresholdTypes.Triangle", "THRESH_TRIANGLE")
            line = line.Replace("ThresholdTypes.Otsu", "THRESH_OTSU")

            If line.Contains(".threshold(") Then
                split = line.Trim.Split(". ()".ToCharArray)
                If split(1) = "=" Then
                    Dim src = split(2)
                    Dim dst = split(0)
                    Dim offset1 = InStr(line, dst + " = ")
                    Dim offset2 = InStr(line, "threshold")
                    Dim part1 = line.Substring(0, offset1 - 1)
                    Dim part2 = line.Substring(offset2 + 9)
                    line = part1 + "threshold(" + src + ", " + dst + ", " + part2
                End If
            End If

            If line.Contains("labels(") Then
                line = line.Replace("labels(0)", "labels[0]")
                line = line.Replace("labels(1)", "labels[1]")
                line = line.Replace("labels(2)", "labels[2]")
                line = line.Replace("labels(3)", "labels[3]")
            End If
            line = line.Replace("labels = {", "labels = {")

            line = line.Replace("CStr", "to_string")

            line = line.Replace("Scalar.Red", "RED")
            line = line.Replace("Scalar.Blue", "BLUE")
            line = line.Replace("Scalar.Green", "GREEN")
            line = line.Replace("Scalar.Yellow", "YELLOW")
            line = line.Replace("Scalar.White", "WHITE")
            line = line.Replace("Scalar.Black", "BLACK")

            If line.Contains("calcHist(") Then
                line = tabs + "//int chan[] = { 0 };" + vbCrLf + tabs + "//int bins[] = { task->histogramBins };" + vbCrLf +
                              tabs + "//float hRange[] = { minRange, maxRange };" + vbCrLf + tabs + "//const float* range[] = { hRange };" + vbCrLf +
                              tabs + "//calcHist(&src, 1, chan, Mat(), hist, 1, bins, range, true, false);" + vbCrLf + line
            End If
            If line.Contains(".Get(Of ") Or line.Contains(".Set(Of ") Then line = dotGetSet(line)
            line = line.Replace("Single", "float")
            line = line.Replace(" Mod ", " % ")
            line = line.Replace("New Scalar", "Scalar")

            line = line.Replace("vbMinmax(", "task->getMinMax(")

            line = line.Replace("Public Sub ", "void ")
            line = line.Replace("Public Sub ", "void ")
            line = line.Replace("Exit Sub", "return")

            line = line.Replace(" New Mat", " Mat")
            line = line.Replace(" New Size2f", "Size2f")
            line = line.Replace(" New RotatedRect", "RotatedRect")
            line = line.Replace(" Math.Floor", "floor")
            line = line.Replace(" Math.Ceiling", "ceiling")
            line = line.Replace(" Math.Round", "round")

            line = line.Replace("(0)", "[0]")
            line = line.Replace("(1)", "[1]")
            line = line.Replace("(2)", "[2]")
            line = line.Replace("(3)", "[3]")

            line = line.Replace("New Rect", "Rect")

            line = line.Replace(".X ", ".x ")
            line = line.Replace(".Y ", ".y ")
            line = line.Replace(".X;", ".x;")
            line = line.Replace(".Y;", ".y;")
            line = line.Replace(".X)", ".x)")
            line = line.Replace(".Y)", ".y)")
            line = line.Replace(".X,", ".x,")
            line = line.Replace(".Y,", ".y,")

            line = line.Replace(" New Rangef", " Range")

            If line.Contains("calcBackProject") Then
                line = tabs + "// float bRange[] = { float(minRange.val[0]), float(maxRange.val[0])};" + vbCrLf +
                              tabs + "// const float* ranges[] = { bRange };" + vbCrLf +
                              tabs + "// calcBackProject(&input, 1, 0, hist->histogram, mask, ranges, 1, true);" + vbCrLf +
                              line
            End If
            line = line.Replace(" +;", " +")

            line = line.Replace("msRNG.Next(0,", "rand() % ")

            line = line.Replace("If(", "if(")

            line = line.Replace("RetrievalModes.List", "RetrievalModes::RETR_LIST")
            line = line.Replace("ContourApproximationModes.ApproxNone", "ContourApproximationModes::CHAIN_APPROX_NONE")
            line = line.Replace("LineTypes.Link4", "LineTypes::LINE_4")

            If line.Contains("Point[][] allContours") Then
                line = tabs + "vector<vector<Point>> allContours;"
            End If
            If line.Contains("findContours") Then
                line = tabs + "// findContours(dst1, allContours, RetrievalModes::RETR_LIST, ContourApproximationModes::CHAIN_APPROX_NONE);" + vbCrLf + line
                line = line.Replace("Nothing, ", "")
            End If
            line = line.Replace("aaOptions.PixelDiffThreshold.Value", "task->pixelDiffThreshold")
            line = line.Replace("ContourApproximationModes.ApproxSimple", "ContourApproximationModes::CHAIN_APPROX_SIMPLE")
            line = line.Replace("RetrievalModes.FloodFill", "RetrievalModes::RETR_FLOODFILL")
            line = line.Replace("vbDrawContour", "task->drawContour")
            line = line.Replace("->Run(Nothing)", "->Run(src)")
            line = line.Replace("vbNormalize32f", "task->normalize32fC1")
            line = line.Replace("tas->AddWeighted", "task->addWeightPercent")
            If line.Contains(" Function ") Then line = findFunction(line, tabs)

            If line.Trim.EndsWith("{") And line.Trim.Length > 1 Then line = line.Substring(0, line.Length - 1) + vbCrLf + tabs + "{"
            If line.Trim.Contains("{" + vbCrLf) Then line = line.Replace("{" + vbCrLf, vbCrLf + tabs + "{" + vbCrLf)
            line = line.Replace("aaOptions.", "task->")
            line = line.Replace(".Checked", "")
            line = line.Replace("(index)", "[index]")
            line = line.Replace("<Integer>", "<int>")
            line = line.Replace("vbCrLf", """/n""")
            line = line.Replace("FrameHistory.Value", "frameHistory")
            If line.Contains(".ElementAt(") Then
                line = line.Replace(".ElementAt(", "[")
                line = line.Replace(")", "]")
            End If
            line = line.Replace(".RemoveAt", ".erase")
            line = line.Replace("List(Of rcData)", "vector<rcData>")
            line = line.Replace("clone()()", "clone()")
            If line.Contains("rc = New rcData") Then line = ""
            If line.Contains("validateRect(") Then
                line = line.Replace("validateRect", "task->validateRect")
                line = line.Replace(");", ", dst2.cols, dst2.rows);")
            End If
            line = line.Replace("vbGetMaxDist", "task->getMaxDist")
            If line.Contains(".inRange") Then
                split = trimline.Split(" ")
                Dim offset = InStr(line, ".inRange")
                Dim nextLine = line.Substring(offset)
                line = tabs + line.Substring(0, offset - 1) + ";"
                Dim parms = nextLine.Substring(8)
                parms = parms.Replace(");", ", " + split(0) + ");")
                line += vbCrLf + tabs + "inRange(" + split(0) + ", " + parms
            End If
            line = line.Replace("List(Of Point2f)", "vector<Point2f>")
            line = line.Replace("lastCells(rc.indexLast)", "lastCells[rc.indexLast]")
            line = line.Replace("List(Of Point)", "vector<Point>")
            line = replaceContains(line, tabs)
            line = line.Replace("msRNG.Next(10, 240)", "rand() % 240")
            line = line.Replace("pcSplit(2)", "pcSplit[2]")
            line = line.Replace("redCells(selectedIndex)", "redCells[selectedIndex]")
            line = line.Replace("redF.", "redF->")
            line = line.Replace("rc.hull = convexHull(rc.contour.ToArray, true).ToList;", tabs +
                               "convexHull(rc.contour, rc.hull);")
            line = line.Replace("shapeCorrelation(", "task->shapeCorrelation(")
            line = scalarAllReplace(line, tabs)
            line = line.Replace("CPP_Basics", "//CPP_Basics")
            line = line.Replace("cpp.updateFunction(", "//cpp.updateFunction(")
            If line.Contains("While ") Then
                line = line.Replace("While ", "while (")
                line = line.Replace(";", ")" + vbCrLf + tabs + "{")
            End If
            If line.Contains("erase(") Then line = replaceErase(line, tabs)
            line = line.Replace("Byte", "unsigned char")




            cppLines(i) = line.Replace("New ", "")
        Next

        If Len(constructorLines) > 0 Then
            For i = 0 To cppLines.Count - 1
                Dim line = cppLines(i)
                If line.Contains("traceName = ") Then
                    cppLines(i) += constructorLines
                End If
            Next
        End If
        CPPrtb.Text = displayCPPLines()
    End Sub
End Class