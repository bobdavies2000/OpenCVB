Imports cv = OpenCvSharp
Imports  System.IO
Imports System.Net
Imports System.Threading
Public Class Download_Databases : Inherits VB_Algorithm
    Dim downloadActive As Boolean
    Dim pythonActive As Boolean
    Dim linkAddress As String = ""
    Dim zippedBuffer As New MemoryStream
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Download the 1.7 Gb 300 Faces In-The-Wild database")
            radio.addRadio("Download TensorFlow MobileNet-SSD v1")
            radio.addRadio("Download TensorFlow MobileNet-SSD v1 PPN")
            radio.addRadio("Download TensorFlow MobileNet-SSD v2")
            radio.addRadio("Download TensorFlow Inception-SSD v2")
            radio.addRadio("Download TensorFlow MobileNet-SSD v3")
            radio.addRadio("Download TensorFlow Faster-RCNN Inception v2")
            radio.addRadio("Download TensorFlow Faster-RCNN ResNet-50")
            radio.addRadio("Download TensorFlow Mask-RCNN Inception v2")
            radio.addRadio("Download All")
            radio.check(6).Checked = True
        End If
        desc = "Multi-threaded (responsive) download of the iBug 300W face database.  Not using iBug yet but planning to..."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static downloadAllCheck = findRadio("Download All")
        Static downloadIndex = -1
        If downloadAllCheck.checked Then
            If downloadActive = False Then
                downloadIndex += 1
                If downloadIndex >= radio.check.Count - 1 Then
                    downloadIndex = 0
                    radio.check(6).Checked = True
                End If
            End If
        End If

        If radio.check(0).Checked Then linkAddress = "http://dlib.net/files/data/ibug_300W_large_face_landmark_dataset.tar.gz"
        If radio.check(1).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2017_11_17.tar.gz"
        If radio.check(2).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_ppn_shared_box_predictor_300x300_coco14_sync_2018_07_03.tar.gz"
        If radio.check(3).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v2_coco_2018_03_29.tar.gz"
        If radio.check(4).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_inception_v2_coco_2017_11_17.tar.gz"
        If radio.check(5).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v3_large_coco_2020_01_14.tar.gz"
        If radio.check(6).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/faster_rcnn_inception_v2_coco_2018_01_28.tar.gz"
        If radio.check(7).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/faster_rcnn_resnet50_coco_2018_01_28.tar.gz"
        If radio.check(8).Checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/mask_rcnn_inception_v2_coco_2018_01_28.tar.gz"

        Dim filename As String = ""
        If radio.check(0).Checked Or downloadIndex = 0 Then filename = "ibug_300W_large_face_landmark_dataset.tar.gz"
        If radio.check(1).Checked Or downloadIndex = 1 Then filename = "ssd_mobilenet_v1_coco_2017_11_17.tar.gz"
        If radio.check(2).Checked Or downloadIndex = 2 Then filename = "ssd_mobilenet_v1_ppn_shared_box_predictor_300x300_coco14_sync_2018_07_03.tar.gz"
        If radio.check(3).Checked Or downloadIndex = 3 Then filename = "ssd_mobilenet_v2_coco_2018_03_29.tar.gz"
        If radio.check(4).Checked Or downloadIndex = 4 Then filename = "ssd_inception_v2_coco_2017_11_17.tar.gz"
        If radio.check(5).Checked Or downloadIndex = 5 Then filename = "ssd_mobilenet_v3_large_coco_2020_01_14.tar.gz"
        If radio.check(6).Checked Or downloadIndex = 6 Then filename = "faster_rcnn_inception_v2_coco_2018_01_28.tar.gz"
        If radio.check(7).Checked Or downloadIndex = 7 Then filename = "faster_rcnn_resnet50_coco_2018_01_28.tar.gz"
        If radio.check(8).Checked Or downloadIndex = 8 Then filename = "mask_rcnn_inception_v2_coco_2018_01_28.tar.gz"

        Dim fileToDecompress = New FileInfo(task.homeDir + "Data/" + filename)
        Dim downloadDir = New DirectoryInfo(task.homeDir + "Data/" + Mid(fileToDecompress.Name, 1, Len(fileToDecompress.Name) - Len(".tar.gz")))
        If downloadActive And pythonActive = False Then
            setTrueText("Downloading active (takes a while).  Current download size = " + Format(zippedBuffer.Length / 1000, "###,##0") + "k bytes" + vbCrLf +
                          "Download is " + Format(zippedBuffer.Length / 1797000000, "#0%") + " complete", New cv.Point(40, 200))
        Else
            If pythonActive Then
                setTrueText("Unzipping files to " + downloadDir.FullName, New cv.Point(40, 200))
            Else
                If linkAddress <> "" Then
                    If downloadDir.Exists Then
                        setTrueText("The database " + downloadDir.Name + " has been downloaded and is ready for use.", New cv.Point(40, 100))
                        Exit Sub
                    End If
                    downloadActive = True
                    Dim downloadThread = New Thread(
                        Sub()
                            Dim client = HttpWebRequest.CreateHttp(linkAddress)
                            Dim response = client.GetResponse()
                            Dim responseStream = response.GetResponseStream()
                            zippedBuffer = New MemoryStream
                            If fileToDecompress.Exists = False Then
                                responseStream.CopyTo(zippedBuffer)
                                File.WriteAllBytes(fileToDecompress.FullName, zippedBuffer.ToArray)
                            End If

                            If fileToDecompress.Name.EndsWith(".tar.gz") Then
                                task.showConsoleLog = False
                                pythonActive = True
                                Dim pyScript = task.homeDir + "Data/extractTarFiles.py"
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
                                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                                p.Start()
                                p.WaitForExit()

                                My.Computer.FileSystem.DeleteFile(pyScript)
                            End If
                            My.Computer.FileSystem.DeleteFile(fileToDecompress.FullName)
                            downloadActive = False
                            pythonActive = False
                        End Sub)
                    downloadThread.Start()
                Else
                    setTrueText("Check the database to be downloaded in the Options nearby.", New cv.Point(40, 200))
                End If
            End If
        End If
    End Sub
End Class
