Imports cv = OpenCvSharp
Imports System.IO
Imports System.Net
Imports System.Threading
Public Class Dlib_Sobel_CS : Inherits VB_Algorithm
    Dim d2Mat as New Mat_Dlib2Mat
    Dim sobel As New CS_Classes.Dlib_EdgesSobel
    Public Sub New()
        desc = "Testing the DLib interface with a simple Sobel example"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sobel.RunCS(input)

        d2Mat.dGray = sobel.edgeImage
        d2Mat.Run(src)
        dst2 = d2Mat.dst2
    End Sub
End Class







Public Class Dlib_GaussianBlur_CS : Inherits VB_Algorithm
    Dim blur As New CS_Classes.Dlib_GaussianBlur
    Dim d2Mat As New Mat_Dlib2Mat
    Public Sub New()
        labels(2) = "Gaussian Blur of grayscale image"
        labels(3) = "Gaussian Blur of BGR image"
        desc = "Use DlibDotNet to blur an image"
    End Sub
    Public Sub RunVB(src as cv.Mat)

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        blur.RunCS(input)

        d2Mat.dGray = blur.blurredGray
        d2Mat.Run(src)
        dst2 = d2Mat.dst2

        blur.RunCS(src) ' now blur the 8uc3 image
        d2Mat.dRGB = blur.blurredRGB
        dst3 = d2Mat.dst3
    End Sub
End Class







Public Class Dlib_FaceDetectHOG_CS : Inherits VB_Algorithm
    Dim faces As New CS_Classes.Dlib_FaceDetectHOG
    Dim d2Mat As New Mat_Dlib2Mat
    Public Sub New()
        faces.initialize()
        desc = "Use DlibDotNet to detect faces using the HOG detector"
    End Sub
    Public Sub RunVB(src as cv.Mat)

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        faces.RunCS(input)

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
Public Class Dlib_iBug300WDownload : Inherits VB_Algorithm
    Dim zippedBuffer As New MemoryStream()
    Dim downloadActive As Boolean
    Dim pythonActive As Boolean
    Public Sub New()
        If check.Setup(traceName) Then check.addCheckBox("Download the 1.7 Gb 300 Faces In-The-Wild database")
        desc = "Multi-threaded (responsive) download of the iBug 300W face database.  Not using iBug yet but planning to..."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static checkDownload = findCheckBox("Download the 1.7 Gb 300 Faces In-The-Wild database")
        Dim ibugDir = New DirectoryInfo(task.homeDir + "Data/ibug_300W_large_face_landmark_dataset")
        If ibugDir.Exists And downloadActive = False And pythonActive = False Then
            setTrueText("The iBug 300W face database was downloaded and is ready for use.", New cv.Point(40, 200))
            Exit Sub
        End If
        Dim fileToDecompress As New FileInfo(task.homeDir + "Data/ibug_300W_large_face_landmark_dataset.tar.gz")
        If downloadActive And pythonActive = False Then
            setTrueText("Downloading active (takes a while).  Current download size = " + Format(zippedBuffer.Length / 1000, "###,##0") + "k bytes" + vbCrLf +
                          "Download is " + Format(zippedBuffer.Length / 1797000000, "#0%") + " complete", New cv.Point(40, 200))
        Else
            If pythonActive Then
                setTrueText("iBug files are being unzipped to " + ibugDir.FullName, New cv.Point(40, 200))
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

                            Dim saveConsoleSetting = task.showConsoleLog
                            task.showConsoleLog = False
                            pythonActive = True
                            Dim pyScript = task.homeDir + "Data/extractiBug.py"
                            Dim fs = New StreamWriter(pyScript)
                            fs.WriteLine("import tarfile")
                            fs.WriteLine("import os")
                            fs.WriteLine("os.chdir(""" + task.homeDir + "Data/" + """)")
                            fs.WriteLine("tar = tarfile.open(""" + fileToDecompress.Name + """)")
                            fs.WriteLine("tar.extractall()")
                            fs.WriteLine("tar.close")
                            fs.Close()

                            task.pythonTaskName = pyScript
                            Dim p As New Process
                            p.StartInfo.FileName = "python"
                            p.StartInfo.WorkingDirectory = task.homeDir + "Data"
                            p.StartInfo.Arguments = """" + task.pythonTaskName + """"
                            If task.showConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                            p.Start()
                            p.WaitForExit()

                            task.showConsoleLog = saveConsoleSetting
                            My.Computer.FileSystem.DeleteFile(pyScript)
                            My.Computer.FileSystem.DeleteFile(fileToDecompress.FullName)
                            downloadActive = False
                            pythonActive = False
                        End Sub)
                    downloadthread.Start()
                Else
                    setTrueText("Check the box in the Options to download the iBug 300W face database", New cv.Point(40, 200))
                End If
            End If
        End If
    End Sub
End Class
