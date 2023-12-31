Imports cv = OpenCvSharp
Imports System.IO
Imports System.Net
Imports System.Threading
Public Class Dlib_Sobel_CS : Inherits VBparent
    Dim d2Mat as New Mat_Dlib2Mat
    Dim sobel As New CS_Classes.Dlib_EdgesSobel
    Public Sub New()
        task.desc = "Testing the DLib interface with a simple Sobel example"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sobel.Run(input)

        d2Mat.dGray = sobel.edgeImage
        d2Mat.RunClass(src)
        dst2 = d2Mat.dst2
    End Sub
End Class







Public Class Dlib_GaussianBlur_CS : Inherits VBparent
    Dim blur As New CS_Classes.Dlib_GaussianBlur
    Dim d2Mat as New Mat_Dlib2Mat
    Public Sub New()
        labels(2) = "Gaussian Blur of grayscale image"
        labels(3) = "Gaussian Blur of BGR image"
        task.desc = "Use DlibDotNet to blur an image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        blur.Run(input)

        d2Mat.dGray = blur.blurredGray
        d2Mat.RunClass(src)
        dst2 = d2Mat.dst2

        blur.Run(src) ' now blur the 8uc3 image
        d2Mat.dRGB = blur.blurredRGB
        dst3 = d2Mat.dst3
    End Sub
End Class







Public Class Dlib_FaceDetectHOG_CS : Inherits VBparent
    Dim faces As New CS_Classes.Dlib_FaceDetectHOG
    Dim d2Mat as New Mat_Dlib2Mat
    Public Sub New()
        faces.initialize()
        task.desc = "Use DlibDotNet to detect faces using the HOG detector"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        faces.Run(input)

        dst2 = src
        For Each r In faces.rects
            ' why divide by 2?  The algorithm did a pyramidUp to "allow the algorithm to detect more faces".
            Dim rect = New cv.Rect(r.Left / 2, r.Top / 2, r.Width / 2, r.Height / 2)
            dst2.Rectangle(rect, cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class








' https://ibug.doc.ic.ac.uk/resources/300-W/
' https://stackoverflow.com/questions/30887979/i-want-to-create-a-script-for-unzip-tar-gz-file-via-python
Public Class Dlib_iBug300WDownload : Inherits VBparent
    Dim zippedBuffer As New MemoryStream()
    Dim downloadActive As Boolean
    Dim pythonActive As Boolean
    Public Sub New()
        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Download the 1.7 Gb 300 Faces In-The-Wild database"
        End If
        task.desc = "Multi-threaded (responsive) download of the iBug 300W face database.  Not using iBug yet but planning to..."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static checkDownload = findCheckBox("Download the 1.7 Gb 300 Faces In-The-Wild database")
        Dim ibugDir = New DirectoryInfo(task.parms.homeDir + "Data/ibug_300W_large_face_landmark_dataset")
        If ibugDir.Exists And downloadActive = False And pythonActive = False Then
            setTrueText("The iBug 300W face database was downloaded and is ready for use.", 40, 200)
            Exit Sub
        End If
        Dim fileToDecompress As New FileInfo(task.parms.homeDir + "Data/ibug_300W_large_face_landmark_dataset.tar.gz")
        If downloadActive And pythonActive = False Then
            setTrueText("Downloading active (takes a while).  Current download size = " + Format(zippedBuffer.Length / 1000, "###,##0") + "k bytes" + vbCrLf +
                          "Download is " + Format(zippedBuffer.Length / 1797000000, "#0%") + " complete", 40, 200)
        Else
            If pythonActive Then
                setTrueText("iBug files are being unzipped to " + ibugDir.FullName, 40, 200)
            Else
                If checkDownload.checked Then
                    Static client = HttpWebRequest.CreateHttp("http://dlib.net/files/data/ibug_300W_large_face_landmark_dataset.tar.gz")
                    Static response = client.GetResponse()
                    Static responseStream = response.GetResponseStream()
                    downloadActive = True
                    Static downloadthread As New Thread(
                        Sub()
                            If fileToDecompress.Exists = False Then
                                responseStream.CopyTo(zippedBuffer)
                                File.WriteAllBytes(fileToDecompress.FullName, zippedBuffer.ToArray)
                            End If

                            Dim saveConsoleSetting = task.parms.ShowConsoleLog
                            task.parms.ShowConsoleLog = False
                            pythonActive = True
                            Dim pyScript = task.parms.homeDir + "Data/extractiBug.py"
                            Dim fs = New StreamWriter(pyScript)
                            fs.WriteLine("import tarfile")
                            fs.WriteLine("import os")
                            fs.WriteLine("os.chdir(""" + task.parms.homeDir + "Data/" + """)")
                            fs.WriteLine("tar = tarfile.open(""" + fileToDecompress.Name + """)")
                            fs.WriteLine("tar.extractall()")
                            fs.WriteLine("tar.close")
                            fs.Close()

                            task.pythonTaskName = pyScript
                            Dim p As New Process
                            p.StartInfo.FileName = task.parms.PythonExe
                            p.StartInfo.WorkingDirectory = task.parms.homeDir + "Data"
                            p.StartInfo.Arguments = """" + task.pythonTaskName + """"
                            If task.parms.ShowConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                            p.Start()
                            p.WaitForExit()

                            task.parms.ShowConsoleLog = saveConsoleSetting
                            My.Computer.FileSystem.DeleteFile(pyScript)
                            My.Computer.FileSystem.DeleteFile(fileToDecompress.FullName)
                            downloadActive = False
                            pythonActive = False
                        End Sub)
                    downloadthread.Start()
                Else
                    setTrueText("Check the box in the Options to download the iBug 300W face database", 40, 200)
                End If
            End If
        End If
    End Sub
End Class
