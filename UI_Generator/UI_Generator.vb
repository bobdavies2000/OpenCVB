Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Module UI_Generator
    Private ReadOnly MutexName As String = "SingleInstanceAppMutex"
    Sub Main(args As String())
#If DEBUG Then
        Console.WriteLine("Currently compiled with DEBUG (so it is slower.)")
#End If
        Using mutex As New Mutex(False, MutexName, createdNew:=False)
            mutex.WaitOne()
            mutex.ReleaseMutex()
        End Using


        Dim fullXRef As Boolean
        If args.Length > 0 Then
            If args(0) = "All" Then fullXRef = True
        Else
            Console.WriteLine("No arguments provided.")
        End If

        Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
        Dim exeDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
        Dim HomeDir = New DirectoryInfo(exeDir.FullName + "/../../../../")
        Directory.SetCurrentDirectory(HomeDir.FullName)
        HomeDir = New DirectoryInfo("./")

        Dim xRefFile = New FileInfo(HomeDir.FullName + "Data/XRef.txt")
        If xRefFile.Exists = False Then fullXRef = True

        Dim PythonProjFile As New FileInfo(HomeDir.FullName + "/Python/Python.pyproj")
        Dim pyFiles = File.ReadAllLines(PythonProjFile.FullName)
        Dim vbList As New SortedList(Of String, String)
        Dim pythonList As New SortedList(Of String, String)
        Dim cppList As New SortedList(Of String, String)
        Dim csList As New SortedList(Of String, String)
        Dim ccList As New SortedList(Of String, String)
        Dim cppManaged As New SortedList(Of String, String)
        Dim cppNative As New SortedList(Of String, String)
        Dim allButPython As New SortedList(Of String, String)
        Dim pyStream As New SortedList(Of String, String)
        Dim allList As New SortedList(Of String, String)
        Dim opengl As New SortedList(Of String, String)
        Dim CodeLineCount As Integer


        Dim srcList As New List(Of String)({HomeDir.FullName + "CPP_Managed/CPP_Managed.cpp",    ' all the managed C++ code
                                            HomeDir.FullName + "CS_Classes/CS_AI_Generated.cs",  ' all the C# code
                                            HomeDir.FullName + "CPP_Native/CPP_NativeClasses.h", ' all the native C++ code
                                            HomeDir.FullName + "CS_Classes/Non_AI.cs"})          ' all the old-style native code.
        Try
            Dim VBcodeDir As New DirectoryInfo(HomeDir.FullName + "VB_classes/") ' all the vb algorithms are here.

            Dim OptionsFile = New FileInfo(VBcodeDir.FullName + "Options.vb")
            Dim includeOptions = New FileInfo(HomeDir.FullName + "CPP_Native/Options.h")

            ' create the C++ native options.h with parameters set to default values.
            Dim result As Integer
            If includeOptions.Exists Then
                result = DateTime.Compare(OptionsFile.LastWriteTime, includeOptions.LastWriteTime)
            End If
            If result > 0 Or includeOptions.Exists = False Then
                includeOptions.Delete()
                ConvertOptionsToCPP(OptionsFile)
            End If

            Dim indexTestFile = New FileInfo(HomeDir.FullName + "/Data/AlgorithmGroupNames.txt")

            If fullXRef Then
                Console.WriteLine("Starting work to generate the user interface with updated XRef algorithms.")
            Else
                Console.WriteLine("Starting work to generate the user interface.")
            End If

            For Each line In pyFiles
                If line.Contains("<Compile Include=") = False Then Continue For
                Dim split = line.Split("""")
                pythonList.Add(split(1), split(1))
                srcList.Add(PythonProjFile.DirectoryName + "\" + split(1))

                If split(1).EndsWith("_PS.py") Then pyStream.Add(split(1), split(1))
                allList.Add(split(1), split(1))
            Next
            Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)
            For Each fn In fileEntries
                If fn.Contains(".Designer") Then Continue For
                If fn.Contains("AssemblyInfo") Then Continue For
                If fn.Contains(".resx") Then Continue For
                If fn.Contains(".vbproj") Then Continue For
                srcList.Add(fn)
            Next

            ' read all the code, count the lines, and get the algorithm list.
            For Each fn In srcList
                Dim srclines = File.ReadAllLines(fn)
                Dim classname As String = ""
                For Each line In srclines
                    line = Trim(line)
                    If line.Length = 0 Then Continue For
                    If line.StartsWith("//") Then Continue For
                    If line.StartsWith("'") Then Continue For
                    If line = "{" Or line = "}" Then Continue For

                    CodeLineCount += 1

                    If fn.EndsWith(".py") Then Continue For
                    If line.StartsWith("Public Class") Then ' VB algorithms
                        If line.EndsWith(" : Inherits VB_Parent") Then
                            Dim split As String() = Regex.Split(line, "\W+")
                            classname = split(2)
                            If classname.StartsWith("OpenGL_") Then opengl.Add(classname, line)
                            vbList.Add(classname, line)
                            allButPython.Add(classname, line)
                            allList.Add(classname, line)
                        End If
                    ElseIf line.StartsWith("public class ") Then ' C# algorithms
                        If line.EndsWith(" : VB_Parent") Then
                            Dim split As String() = Regex.Split(line, "\W+")
                            classname = split(2)
                            csList.Add(classname, line)
                            allButPython.Add(classname, line)
                            allList.Add(classname, line)
                        End If
                    ElseIf line.StartsWith("class") Then ' C++ Native algorithms
                        If line.Contains(" : public CPP_Parent") Then
                            Dim split = line.Split(" ")
                            classname = split(1)
                            cppList.Add(classname, line)
                            cppNative.Add(classname, line)
                            allButPython.Add(classname, line)
                            allList.Add(classname, line)
                            ccList.Add(classname, line)
                        End If
                    ElseIf line.StartsWith("public ref class ") Then ' Managed C++ algorithms.
                        If line.EndsWith(" : public VB_Parent") Then
                            Dim split = line.Split(" ")
                            classname = split(3)
                            cppList.Add(classname, line)
                            cppManaged.Add(classname, line)
                            allButPython.Add(classname, line)
                            allList.Add(classname, line)
                        End If
                    End If
                Next
            Next
        Catch ex As Exception
            Console.WriteLine("The UI_Generator failed collecting algorithm references.  Error is " + vbCrLf + ex.Message)
        End Try
        Console.WriteLine("Algorithm names collected.")




        Try
            Dim sw = New StreamWriter(HomeDir.FullName + "Data\AlgorithmCounts.txt")
            sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
            sw.WriteLine("AlgorithmCount = " + CStr(allList.Count))
            sw.Close()

            ' CPP_Enum.h
            sw = New StreamWriter(HomeDir.FullName + "CPP_Native/CPP_Enum.h")
            sw.WriteLine("#pragma once")
            sw.WriteLine("enum ccListFunctions")
            sw.WriteLine("{")
            For Each alg In ccList.Keys
                sw.WriteLine("_" + alg + ",")
            Next
            sw.WriteLine("};")
            sw.Close()



            ' C++ output
            Dim mainInfo As New FileInfo(HomeDir.FullName + "Main_UI\AlgorithmList.vb")
            sw = New StreamWriter(mainInfo.FullName)
            sw.WriteLine("' this file is automatically generated in a pre-build step.  Any manual modifications will be lost.")
            sw.WriteLine("Imports CS_Classes")
            sw.WriteLine("Imports VB_Classes")
            sw.WriteLine("Imports CPP_Managed")

            sw.WriteLine("Public Class algorithmList")
            sw.WriteLine("Public Enum ccFunctionNames")
            For Each alg In ccList.Keys
                sw.WriteLine("_" + alg)
            Next
            sw.WriteLine("End Enum")

            sw.WriteLine(vbTab + "Public Function createAlgorithm(algorithmName as string) as Object")
            sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
            For Each nextName In allList.Keys
                If nextName.StartsWith("CPP_Basics") Or nextName.StartsWith("cpp_Task") Then Continue For
                If nextName.EndsWith(".py") Then Continue For
                If nextName.EndsWith("_CC") Then
                    sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new CPP_Basics(ccFunctionNames._" + nextName + ")")
                Else
                    sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new " + nextName)
                End If
            Next
            sw.WriteLine(vbTab + vbTab + "Return Nothing")
            sw.WriteLine(vbTab + "End Function")
            sw.WriteLine("End Class")
            sw.Close()
        Catch ex As Exception
            Console.WriteLine("UI_Generator failed writing the C# and VB.Net algorithm lists.  Error is " + vbCrLf + ex.Message)
        End Try
        Console.WriteLine("AlgorithmList.vb prepared." + vbCrLf + "Now preparing the sorted algorithm cross reference.")






        Dim sortedXRefs As New List(Of String)
        Dim refCounts As New List(Of String)
        Try
            If fullXRef Then
                Dim tokens(allButPython.Count - 1) As String
                For i = 0 To allButPython.Keys.Count - 1
                    tokens(i) = allButPython.Keys(i)
                Next
                Dim references As New SortedList(Of String, String)
                For Each fn In srcList
                    If fn.EndsWith(".py") Then Continue For
                    Dim srclines = File.ReadAllLines(fn)
                    Dim classname As String = ""
                    For Each line In srclines
                        line = Trim(line)
                        If line.Length = 0 Then Continue For
                        If line.StartsWith("//") Then Continue For
                        If line.StartsWith("'") Then Continue For
                        If line = "{" Or line = "}" Then Continue For

                        If line.StartsWith("Public Class") Then ' VB algorithms
                            If line.EndsWith(" : Inherits VB_Parent") Then
                                Dim split As String() = Regex.Split(line, "\W+")
                                classname = split(2)
                            End If
                        ElseIf line.StartsWith("public class ") Then ' C# algorithms
                            If line.EndsWith(" : VB_Parent") Then
                                Dim split As String() = Regex.Split(line, "\W+")
                                classname = split(2)
                            End If
                        ElseIf line.StartsWith("class") Then ' C++ Native algorithms
                            If line.EndsWith("_CC") Or line.Contains(" : public CPP_Parent") Then
                                Dim split = line.Split(" ")
                                classname = split(1)
                            End If
                        ElseIf line.StartsWith("public ref class ") Then ' Managed C++ algorithms.
                            If line.EndsWith(" : public VB_Parent") Then
                                Dim split = line.Split(" ")
                                classname = split(3)
                            End If
                        End If
                        If classname <> "" Then
                            For Each alg In allButPython.Keys
                                If line.Contains(alg) Then
                                    Dim index = allButPython.IndexOfKey(alg)
                                    If tokens(index).Contains(classname) = False Then tokens(index) += "," + classname
                                End If
                            Next
                        End If
                    Next
                Next

                For i = 0 To tokens.Count - 1
                    ' sort the tokens before creating the final entry
                    Dim split As String() = Regex.Split(tokens(i), ",")
                    Dim tokenSort As New SortedList(Of String, String)
                    For j = 0 To split.Length - 1
                        tokenSort.Add(split(j), split(j))
                    Next
                    Dim finalEntry = allButPython.ElementAt(i).Key
                    For j = 0 To tokenSort.Keys.Count - 1
                        finalEntry += "," + tokenSort.ElementAt(j).Key
                    Next
                    sortedXRefs.Add(finalEntry)
                    refCounts.Add("(" + CStr(tokenSort.Keys.Count) + ") ")
                Next

                Dim xRefsw As New StreamWriter(xRefFile.FullName)
                For i = 0 To sortedXRefs.Count - 1
                    xRefsw.WriteLine(refCounts(i) + sortedXRefs(i))
                Next
                xRefsw.Close()

                Console.WriteLine("Algorithm references prepared.")
            Else
                Dim xreflines = File.ReadAllLines(xRefFile.FullName)
                For i = 0 To xreflines.Count - 1
                    sortedXRefs.Add(xreflines(i))
                Next
            End If
        Catch ex As Exception
            Console.WriteLine("UI_Generator failed creating the usage index.  Error is " + vbCrLf + ex.Message)
        End Try






        Try
            Dim sw = New StreamWriter(HomeDir.FullName + "Data/AlgorithmGroupNames.txt")
            sw.Write("(" + CStr(allList.Count) + ") < All >")
            For Each alg In allButPython.Keys
                If alg = "CPP_Basics" Or alg = "cpp_Task" Then Continue For
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(allButPython.Count) + ") < All but Python >")
            For Each alg In allButPython.Keys
                If alg = "CPP_Basics" Or alg = "cpp_Task" Then Continue For
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(ccList.Count) + ") < All C# >")
            For Each alg In ccList.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(cppList.Count) + ") < All C++ >")
            For Each alg In cppList.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(opengl.Count) + ") < All OpenGL >")
            For Each alg In opengl.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(pyStream.Count) + ") < All PyStream >")
            For Each alg In pyStream.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(pythonList.Count) + ") < All Python >")
            For Each alg In pythonList.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            sw.Write("(" + CStr(vbList.Count) + ") < All VB.Net >")
            For Each alg In vbList.Keys
                sw.Write("," + alg)
            Next
            sw.WriteLine()

            For i = 0 To sortedXRefs.Count - 1
                sw.WriteLine(sortedXRefs(i))
            Next
            sw.Close()
        Catch ex As Exception
            Console.WriteLine("UI_Generator failed writing the algorithm groups.  Error is " + vbCrLf + ex.Message)
        End Try
        Console.WriteLine("Algorithm Group Names prepared.")
    End Sub

    Private Function checkDates(dirInfo As DirectoryInfo, algorithmGroupNames As FileInfo) As Boolean
        For Each fileInfo As FileInfo In dirInfo.GetFiles()
            If fileInfo.Name = "VB_Common.vb" Then Continue For
            If fileInfo.Name = "VB_Parent.vb" Then Continue For
            If fileInfo.Name = "VB_Task.vb" Then Continue For
            If fileInfo.Name = "VB_Externs.vb" Then Continue For
            If fileInfo.Name.StartsWith("Options") Then Continue For
            Dim result As Integer = DateTime.Compare(fileInfo.LastWriteTime, algorithmGroupNames.LastWriteTime)
            If result > 0 Then Return True
        Next
        Return False
    End Function
    Private Sub ConvertOptionsToCPP(inputfile As FileInfo)
        Dim index As Integer = 0
        Dim output As New List(Of String)
        Dim phase1 As New List(Of String)
        Dim phase2 As New List(Of String)
        Dim phase3 As New List(Of String)
        Dim lines = File.ReadAllLines(inputfile.FullName)

        index = 0
        While index < lines.Count
            Dim line = Trim(lines(index))
            index += 1
            If line.StartsWith("Public Class Options_") Then
                For i = index To lines.Count - 1
                    line = line.Replace(" = True", " = true")
                    line = line.Replace(" = False", " = false")
                    line = line.Replace("(Of String)", "(Of string)")
                    line = line.Replace("Nothing", "null")

                    If line.Contains(" As New ") Then
                        If line.Contains(" As New List(Of ") = False Then line = ""
                    End If

                    phase1.Add(line)
                    line = Trim(lines(index))
                    index += 1
                    If line.Contains("Public Sub New") Then
                        phase1.Add(line)
                        Exit For
                    End If
                Next
            End If
        End While

        index = 0
        While index < phase1.Count
            Dim line = Trim(phase1(index))
            index += 1
            If line.Contains("{") Then
                Dim arrayLine = ""
                For i = index To phase1.Count - 1
                    If line.Contains("}") Then
                        arrayLine += line
                        phase2.Add(arrayLine)
                        Exit For
                    Else
                        arrayLine += line
                    End If
                    line = Trim(phase1(index))
                    index += 1
                Next
            Else
                phase2.Add(line)
            End If
        End While

        index = 0
        While index < phase2.Count
            Dim line = Trim(phase2(index))
            index += 1

            If line.Contains("TrackBar") Then Continue While
            If line.Contains("CheckBox") Then Continue While
            If line.Contains("RadioButton") Then Continue While
            If line.Contains("FileInfo") Then Continue While
            If line.Contains("fileName") Then Continue While
            If line.Contains("fileNames ") Then Continue While

            line = line.Replace("cvb.", "cv::")
            If line.StartsWith("Public Class Options_") Or line.Contains("Public Sub New") Then
                phase3.Add(line)
                Continue While
            End If

            If line.Contains(" As cv.Mat") Then Continue While

            phase3.Add(line)
        End While

        index = 0
        output.Add("// This file is automatically generated.  Don't waste your time altering it.")
        output.Add("// This file provides initialized variables for all the options to the C++ code.")
        output.Add("// It is statically defined because C++ options must be provided in C++ code")
        output.Add("// which is more work to be done when moving this code to another application.")
        output.Add("#include <string.h>")
        output.Add("using namespace cv;")
        output.Add("using namespace std;")
        While index < phase3.Count
            Dim line = Trim(phase3(index))
            index += 1
            If line.StartsWith("Public Class Options_") Then
                Dim splitLine = line.Split(" ")
                output.Add("class " + splitLine(2) + " {")
                output.Add("public:")
                For i = index To phase3.Count - 1
                    line = Trim(phase3(i))
                    index += 1
                    If line = "" Then Continue For
                    If line.StartsWith("Public Sub New") Then
                        output.Add(vbTab + "void RunOpt() {}")
                        output.Add(vbTab + splitLine(2) + "() {")
                        output.Add(vbTab + "}")
                        output.Add("};")
                        Exit For
                    Else

                        Dim split = line.Split(" ")
                        If split(3) = "Integer" Then split(3) = "int"
                        If split(3) = "Boolean" Then split(3) = "bool"
                        If split(3) = "Single" Then split(3) = "float"
                        If split(3) = "Double" Then split(3) = "double"
                        If split(3) = "String" Then split(3) = "string"
                        split(1) = split(1).Replace("()", "[]")

                        If line.Contains(" As New List(Of") Then
                            Dim listtype = split(5).Substring(0, Len(split(5)) - 1)
                            output.Add("vector<" + listtype + "> " + split(1) + ";")
                        Else
                            If line.Contains(" = ") Then
                                If line.Contains("reduceXYZ") Then
                                    output.Add(vbTab + "bool reduceXYZ[3] = {true, true, true};")
                                Else
                                    Dim splitEqual = line.Split("=")
                                    splitEqual(1) = splitEqual(1).Replace(" New ", " ")
                                    output.Add(vbTab + split(3) + " " + split(1) + " = " + splitEqual(1) + ";")
                                End If
                            Else
                                If line.Contains("buffer(9)") Then output.Add(vbTab + "int buffer[9];")
                                If line.Contains("inputPoints()") Then output.Add(vbTab + "Point2f inputPoints[];")
                                If line.Contains("radioChoices()") Then output.Add(vbTab + "Vec3i radioChoices[];")
                                If line.Contains("splits(4)") Then output.Add(vbTab + "int splits[5];")
                                If line.Contains("vals(4)") Then output.Add(vbTab + "int vals[5];")
                            End If
                        End If
                    End If
                Next
            End If
        End While

        ' special case language dependent changes.
        For i = 0 To output.Count - 1
            Dim nextLine = output(i)
            output(i) = ""
            nextLine = nextLine.Replace("ContourApproximationModes.", "ContourApproximationModes::")
            nextLine = nextLine.Replace("ApproxTC89KCOS", "CHAIN_APPROX_TC89_KCOS")
            nextLine = nextLine.Replace("ApproxNone", "CHAIN_APPROX_NONE")
            nextLine = nextLine.Replace("ApproxSimple", "CHAIN_APPROX_SIMPLE")
            nextLine = nextLine.Replace("ApproxTC89KCOS", "CHAIN_APPROX_TC89_KCOS")
            nextLine = nextLine.Replace("ApproxTC89L1", "CHAIN_APPROX_TC89_L1")
            nextLine = nextLine.Replace("RetrievalModes.", "RetrievalModes::")
            nextLine = nextLine.Replace("RetrievalModes::External", "cv::RetrievalModes::RETR_EXTERNAL")
            nextLine = nextLine.Replace("ImwriteFlags.JpegProgressive", "ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE")
            nextLine = nextLine.Replace("ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE ", "int ")

            nextLine = nextLine.Replace("ShapeMatchModes.I1", " ShapeMatchModes::CONTOURS_MATCH_I1")
            nextLine = nextLine.Replace("InterpolationFlags.Nearest", "InterpolationFlags::INTER_NEAREST")
            nextLine = nextLine.Replace("ML.SVM.Types.CSvc", "ml::SVM::C_SVC")
            nextLine = nextLine.Replace("ML.SVM.KernelTypes ", "int ")
            nextLine = nextLine.Replace("ML.SVM.KernelTypes.Poly", "ml::SVM::KernelTypes::POLY")
            nextLine = nextLine.Replace("DctFlags ", "int ")
            nextLine = nextLine.Replace("DctFlags;", "cv::DCT_INVERSE | cv::DCT_ROWS;")
            nextLine = nextLine.Replace("ColorConversionCodes.BGR2GRAY", "ColorConversionCodes::COLOR_BGR2GRAY")
            nextLine = nextLine.Replace("OpticalFlowFlags.FarnebackGaussian;", "cv::OPTFLOW_FARNEBACK_GAUSSIAN;")
            nextLine = nextLine.Replace("OpticalFlowFlags OpticalFlowFlag", "int OpticalFlowFlag")
            If nextLine.Contains("Quaternion") Then Continue For

            nextLine = nextLine.Replace("HomographyMethods.None ", "int ")
            nextLine = nextLine.Replace("HomographyMethods.None;", "LMEDS;")
            nextLine = nextLine.Replace("Math.PI", "CV_PI")
            nextLine = nextLine.Replace("SortFlags.EveryColumn", "cv::SortFlags::SORT_EVERY_COLUMN")
            nextLine = nextLine.Replace("SortFlags.Ascending", "cv::SortFlags::SORT_ASCENDING")
            nextLine = nextLine.Replace("SortFlags sortOption ", "int sortOption")
            nextLine = nextLine.Replace("DistanceTypes ", "int ")
            nextLine = nextLine.Replace("DistanceTypes.L1", "DistanceTypes::DIST_L1")
            nextLine = nextLine.Replace("HistCompMethods.Correl", "cv::HistCompMethods::HISTCMP_CORREL")
            nextLine = nextLine.Replace("EMTypes.CovMatDefault ", "int ")
            If nextLine.Contains("CovMatDefault") Then Continue For

            nextLine = nextLine.Replace("KMeansFlags ", "int ")
            nextLine = nextLine.Replace("KMeansFlags.RandomCenters", "KmeansFlags::KMEANS_RANDOM_CENTERS")
            nextLine = nextLine.Replace(" Or ", " | ")
            nextLine = nextLine.Replace("FloodFillFlags ", "int ")
            nextLine = nextLine.Replace("MorphShapes.Cross", "cv::MorphShapes::MORPH_CROSS")
            nextLine = nextLine.Replace("TemplateMatchModes.CCoeffNormed", "cv::TemplateMatchModes::TM_CCOEFF_NORMED")
            nextLine = nextLine.Replace("TemplateMatchModes ", "int ")
            nextLine = nextLine.Replace("ThresholdTypes.Binary", "cv::ThresholdTypes::THRESH_BINARY")
            nextLine = nextLine.Replace("AdaptiveThresholdTypes.GaussianC", "cv::AdaptiveThresholdTypes::ADAPTIVE_THRESH_GAUSSIAN_C")
            nextLine = nextLine.Replace("DftFlags ", "int ")
            nextLine = nextLine.Replace("DftFlags.ComplexOutput", "DftFlags::DFT_COMPLEX_OUTPUT")
            nextLine = nextLine.Replace("SeamlessCloneMethods ", "int ")
            nextLine = nextLine.Replace("SeamlessCloneMethods.MixedClone", "cv::MIXED_CLONE")
            nextLine = nextLine.Replace("DecompTypes ", "int ")
            nextLine = nextLine.Replace("DecompTypes.Cholesky", "DecompTypes::DECOMP_CHOLESKY")
            nextLine = nextLine.Replace("SimpleBlobDetector.Params ", "SimpleBlobDetector::Params ")


            nextLine = nextLine.Replace("FloodFillFlags.FixedRange", "FloodFillFlags::FLOODFILL_FIXED_RANGE")

            If nextLine.Contains("SimpleBlobDetector.Params") Then Continue For

            nextLine = nextLine.Replace("cv::int ", "int ")
            nextLine = nextLine.Replace("cv::cv::", "cv::")
            output(i) = nextLine
        Next

        Dim outFile = New FileInfo(inputfile.Directory.FullName + "/../CPP_Native/Options.h")
        File.WriteAllLines(outFile.FullName, output)
    End Sub
End Module
