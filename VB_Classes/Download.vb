Imports cv = OpenCvSharp
Imports System.IO
Imports System.Net
Imports System.Threading
Public Class Download_Databases : Inherits VBparent
    Dim downloadActive As Boolean
    Dim pythonActive As Boolean
    Dim linkAddress As String = ""
    Dim zippedBuffer As New MemoryStream
    Public Sub New()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            radio.Setup(caller, 9)
            radio.check(0).Text = "Download the 1.7 Gb 300 Faces In-The-Wild database"
            radio.check(1).Text = "Download TensorFlow MobileNet-SSD v1"
            radio.check(2).Text = "Download TensorFlow MobileNet-SSD v1 PPN"
            radio.check(3).Text = "Download TensorFlow MobileNet-SSD v2"
            radio.check(4).Text = "Download TensorFlow Inception-SSD v2"
            radio.check(5).Text = "Download TensorFlow MobileNet-SSD v3"
            radio.check(6).Text = "Download TensorFlow Faster-RCNN Inception v2"
            radio.check(7).Text = "Download TensorFlow Faster-RCNN ResNet-50"
            radio.check(8).Text = "Download TensorFlow Mask-RCNN Inception v2"
            radio.check(6).Checked = True
        End If

        task.desc = "Multi-threaded (responsive) download of the iBug 300W face database.  Not using iBug yet but planning to..."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)

        Static checkiBug = findRadio("Download the 1.7 Gb 300 Faces In-The-Wild database")
        Static checkTensor1 = findRadio("Download TensorFlow MobileNet-SSD v1")
        Static checkTensor2 = findRadio("Download TensorFlow MobileNet-SSD v1 PPN")
        Static checkTensor3 = findRadio("Download TensorFlow MobileNet-SSD v2")
        Static checkTensor4 = findRadio("Download TensorFlow Inception-SSD v2")
        Static checkTensor5 = findRadio("Download TensorFlow MobileNet-SSD v3")
        Static checkTensor6 = findRadio("Download TensorFlow Faster-RCNN Inception v2")
        Static checkTensor7 = findRadio("Download TensorFlow Faster-RCNN ResNet-50")
        Static checkTensor8 = findRadio("Download TensorFlow Mask-RCNN Inception v2")

        If checkiBug.checked Then linkAddress = "http://dlib.net/files/data/ibug_300W_large_face_landmark_dataset.tar.gz"
        If checkTensor1.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2017_11_17.tar.gz"
        If checkTensor2.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_ppn_shared_box_predictor_300x300_coco14_sync_2018_07_03.tar.gz"
        If checkTensor3.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v2_coco_2018_03_29.tar.gz"
        If checkTensor4.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_inception_v2_coco_2017_11_17.tar.gz"
        If checkTensor5.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v3_large_coco_2020_01_14.tar.gz"
        If checkTensor6.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/faster_rcnn_inception_v2_coco_2018_01_28.tar.gz"
        If checkTensor7.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/faster_rcnn_resnet50_coco_2018_01_28.tar.gz"
        If checkTensor8.checked Then linkAddress = "http://download.tensorflow.org/models/object_detection/mask_rcnn_inception_v2_coco_2018_01_28.tar.gz"

        Dim filename = "ibug_300W_large_face_landmark_dataset.tar.gz"
        If checkTensor1.checked Then filename = "ssd_mobilenet_v1_coco_2017_11_17.tar.gz"
        If checkTensor2.checked Then filename = "ssd_mobilenet_v1_ppn_shared_box_predictor_300x300_coco14_sync_2018_07_03.tar.gz"
        If checkTensor3.checked Then filename = "ssd_mobilenet_v2_coco_2018_03_29.tar.gz"
        If checkTensor4.checked Then filename = "ssd_inception_v2_coco_2017_11_17.tar.gz"
        If checkTensor5.checked Then filename = "ssd_mobilenet_v3_large_coco_2020_01_14.tar.gz"
        If checkTensor6.checked Then filename = "faster_rcnn_inception_v2_coco_2018_01_28.tar.gz"
        If checkTensor7.checked Then filename = "faster_rcnn_resnet50_coco_2018_01_28.tar.gz"
        If checkTensor8.checked Then filename = "mask_rcnn_inception_v2_coco_2018_01_28.tar.gz"

        Dim fileToDecompress = New FileInfo(task.parms.homeDir + "Data/" + filename)
        Dim downloadDir = New DirectoryInfo(task.parms.homeDir + "Data/" + Mid(fileToDecompress.Name, 1, Len(fileToDecompress.Name) - Len(".tar.gz")))
        If downloadActive And pythonActive = False Then
            task.trueText("Downloading active (takes a while).  Current download size = " + Format(zippedBuffer.Length / 1000, "###,##0") + "k bytes" + vbCrLf +
                          "Download is " + Format(zippedBuffer.Length / 1797000000, "#0%") + " complete", 40, 200)
        Else
            If pythonActive Then
                task.trueText("Unzipping files to " + downloadDir.FullName, 40, 200)
            Else
                If linkAddress <> "" Then
                    If downloadDir.Exists Then
                        task.trueText("The database " + downloadDir.Name + " has been downloaded and is ready for use.", 40, 100)
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
                                task.parms.ShowConsoleLog = False
                                pythonActive = True
                                Dim pyScript = task.parms.homeDir + "Data/extractTarFiles.py"
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
                    task.trueText("Check the database to be downloaded in the Options nearby.", 40, 200)
                End If
            End If
        End If
    End Sub
End Class
