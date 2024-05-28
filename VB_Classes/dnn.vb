Imports cv = OpenCvSharp
Imports OpenCvSharp.Dnn
Imports System.Net
Imports  System.IO
Imports OpenCvSharp.DnnSuperres

Public Class DNN_Test : Inherits VB_Algorithm
    Dim net As Net
    Dim classnames() As String
    Public Sub New()

        Dim modelFile As New FileInfo(task.homeDir + "Data/bvlc_googlenet.caffemodel")
        If File.Exists(modelFile.FullName) = False Then
            ' this site is apparently gone.  caffemodel is in the Data directory in OpenCVB_HomeDir
            Dim client = HttpWebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel")
            Dim response = client.GetResponse()
            Dim responseStream = response.GetResponseStream()
            Dim memory As New MemoryStream()
            responseStream.CopyTo(memory)
            File.WriteAllBytes(modelFile.FullName, memory.ToArray)
        End If
        Dim protoTxt = task.homeDir + "Data/bvlc_googlenet.prototxt"
        net = CvDnn.ReadNetFromCaffe(protoTxt, modelFile.FullName)
        Dim synsetWords = task.homeDir + "Data/synset_words.txt"
        classnames = File.ReadAllLines(synsetWords) ' .Select(line >= line.Split(' ').Last()).ToArray()
        For i = 0 To classNames.Count - 1
            classNames(i) = classNames(i).Split(" ").Last
        Next

        labels(3) = "Input Image"
        desc = "Download and use a Caffe database"
    End Sub
    Public Sub RunVB(src as cv.Mat)

        Dim image = cv.Cv2.ImRead(task.homeDir + "Data/space_shuttle.jpg")
        dst3 = image.Resize(dst3.Size())
        Dim inputBlob = CvDnn.BlobFromImage(image, 1, New cv.Size(224, 224), New cv.Scalar(104, 117, 123))
        net.SetInput(inputBlob, "data")
        Dim prob = net.Forward("prob")

        Dim mm as mmData = vbMinMax(prob.Reshape(1, 1))
        setTrueText("Best classification: index = " + CStr(mm.maxLoc.X) + " which is for '" + classnames(mm.maxLoc.X) + "' with Probability " +
                    Format(mm.maxVal, "#0.00%"), New cv.Point(40, 200))
    End Sub
End Class





Public Class DNN_Caffe_CS : Inherits VB_Algorithm
    Dim caffeCS As New CS_Classes.DNN
    Public Sub New()
        labels(3) = "Input Image"
        desc = "Download and use a Caffe database"

        Dim protoTxt = task.homeDir + "Data/bvlc_googlenet.prototxt"
        Dim modelFile = task.homeDir + "Data/bvlc_googlenet.caffemodel"
        Dim synsetWords = task.homeDir + "Data/synset_words.txt"
        caffeCS.initialize(protoTxt, modelFile, synsetWords)
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim image = cv.Cv2.ImRead(task.homeDir + "Data/space_shuttle.jpg")
        Dim str = caffeCS.RunCS(image)
        dst3 = image.Resize(dst3.Size())
        setTrueText(str)
    End Sub
End Class





' https://github.com/twMr7/rscvdnn
Public Class DNN_Basics : Inherits VB_Algorithm
    Dim net As Net
    Dim dnnPrepared As Boolean
    Dim crop As cv.Rect
    Dim dnnWidth As Integer, dnnHeight As Integer
    Dim testImage As cv.Mat
    Dim kalman(10) As Kalman_Basics
    Public rect As cv.Rect
    Dim classNames() = {"background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                        "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"}
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DNN Scale Factor", 1, 10000, 78)
            sliders.setupTrackBar("DNN MeanVal", 1, 255, 127)
            sliders.setupTrackBar("DNN Confidence Threshold", 1, 100, 80)
        End If
        For i = 0 To kalman.Count - 1
            kalman(i) = New Kalman_Basics()
            ReDim kalman(i).kInput(4 - 1)
            ReDim kalman(i).kOutput(4 - 1)
        Next

        dnnWidth = dst2.Height ' height is always smaller than width...
        dnnHeight = dst2.Height
        crop = New cv.Rect(dst2.Width / 2 - dnnWidth / 2, dst2.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight)

        Dim infoText As New FileInfo(task.homeDir + "Data/MobileNetSSD_deploy.prototxt")
        If infoText.Exists Then
            Dim infoModel As New FileInfo(task.homeDir + "Data/MobileNetSSD_deploy.caffemodel")
            If infoModel.Exists Then
                net = CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName)
                dnnPrepared = True
            End If
        End If
        If dnnPrepared = False Then
            setTrueText("Caffe databases not found.  It should be in <OpenCVB_HomeDir>/Data.")
        End If
        desc = "Use OpenCV's dnn from Caffe file."
        labels(2) = "Cropped Input Image - must be square!"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static scaleSlider = findSlider("DNN Scale Factor")
        Static meanSlider = findSlider("DNN MeanVal")
        Static confidenceSlider = findSlider("DNN Confidence Threshold")
        If dnnPrepared Then
            Dim inScaleFactor As Single = scaleSlider.Value / scaleSlider.Maximum ' should be 0.0078 by default...
            Dim inputBlob = CvDnn.BlobFromImage(src(crop), inScaleFactor, New cv.Size(300, 300), CSng(meanSlider.Value), False)
            src.CopyTo(dst3)
            src(crop).CopyTo(dst2(crop))
            net.SetInput(inputBlob, "data")

            Dim detection = net.Forward("detection_out")
            Dim detectionMat = New cv.Mat(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

            Dim confidenceThreshold As Single = confidenceSlider.Value / 100
            Dim rows = src(crop).Rows
            Dim cols = src(crop).Cols
            labels(3) = ""

            Dim kPoints As New List(Of cv.Point)
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.Get(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                    If kalman(i).kInput(0) = 0 And kalman(i).kInput(1) = 0 Then
                        kPoints.Add(New cv.Point2f(vec(0) * cols + crop.Left, vec(1) * rows + crop.Top))
                    Else
                        kPoints.Add(New cv.Point2f(kalman(i).kInput(0), kalman(i).kInput(1)))
                    End If
                End If
            Next

            Static activeKalman As Integer
            If kPoints.Count > activeKalman Then activeKalman = kPoints.Count
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.Get(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim nextName = classNames(CInt(detectionMat.Get(Of Single)(i, 1)))
                    labels(3) += nextName + " "  ' display the name of what we found.
                    Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                    rect = New cv.Rect(vec(0) * cols + crop.Left, vec(1) * rows + crop.Top, (vec(2) - vec(0)) * cols, (vec(3) - vec(1)) * rows)
                    rect = New cv.Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height))

                    Dim pt = New cv.Point(rect.X, rect.Y)
                    Dim minIndex As Integer
                    Dim minDistance As Single = Single.MaxValue
                    For j = 0 To kPoints.Count - 1
                        Dim distance = Math.Sqrt((pt.X - kPoints(j).X) * (pt.X - kPoints(j).X) + (pt.Y - kPoints(j).Y) * (pt.Y - kPoints(j).Y))
                        If minDistance > distance Then
                            minIndex = j
                            minDistance = distance
                        End If
                    Next

                    If minIndex < kalman.Count Then
                        kalman(minIndex).kInput = {rect.X, rect.Y, rect.Width, rect.Height}
                        kalman(minIndex).Run(src)
                        rect = New cv.Rect(kalman(minIndex).kOutput(0), kalman(minIndex).kOutput(1), kalman(minIndex).kOutput(2), kalman(minIndex).kOutput(3))
                    End If
                    dst3.Rectangle(rect, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
                    rect.Width = src.Width / 12
                    rect.Height = src.Height / 16
                    dst3.Rectangle(rect, cv.Scalar.Black, -1)
                    setTrueText(nextName, New cv.Point(rect.X, rect.Y), 3)
                End If
            Next

            ' reinitialize any unused kalman filters.
            For i = kPoints.Count To activeKalman - 1
                If i < kalman.Count Then
                    kalman(i).kInput(0) = 0
                    kalman(i).kInput(1) = 0
                End If
            Next
        End If
    End Sub
End Class




' https://github.com/Saafke/EDSR_Tensorflow
' https://github.com/fannymonori/TF-ESPCN
' https://github.com/Saafke/FSRCNN_Tensorflow
' https://github.com/fannymonori/TF-LapSRN
' https//github.com/Saafke/FSRCNN_Tensorflow/tree/master/models
Public Class DNN_SuperRes : Inherits VB_Algorithm
    Public options As New Options_DNN
    Public dnn = New DnnSuperResImpl("fsrcnn", 4)
    Public Sub New()
        task.drawRect = New cv.Rect(10, 10, 20, 20)
        labels(2) = "Output of a resize using OpenCV"
        desc = "Get better super-resolution through a DNN"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()
        Static saveModelFile = ""
        Static multiplier As Integer
        If saveModelFile <> options.superResModelFileName Then
            saveModelFile = options.superResModelFileName
            multiplier = options.superResMultiplier
            dnn = New DnnSuperResImpl(options.shortModelName, multiplier)
            dnn.ReadModel(saveModelFile)
        End If
        Dim r = task.drawRect
        If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then Exit Sub
        Dim outRect = New cv.Rect(0, 0, r.Width * multiplier, r.Height * multiplier)
        If outRect.Width > dst3.Width Then
            r.Width = dst3.Width / multiplier
            outRect.Width = dst3.Width
        End If
        If outRect.Height > dst3.Height Then
            r.Height = dst3.Height / multiplier
            outRect.Height = dst3.Height
        End If
        dst2.SetTo(0)
        dst3.SetTo(0)
        dst2(outRect) = src(r).Resize(New cv.Size(r.Width * multiplier, r.Height * multiplier))
        dnn.Upsample(src(r), dst3(outRect))
        labels(3) = CStr(multiplier) + "X resize of selected area using DNN super resolution"
    End Sub
End Class







Public Class DNN_SuperResize : Inherits VB_Algorithm
    Dim super = New DNN_SuperRes
    Public Sub New()
        labels(2) = "Super Res resized back to original size"
        labels(3) = "dst3 = dst2 - src or no difference - honors original"
        desc = "Compare superRes reduced to original size"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        super.Run(src)
        Dim r = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        Dim tmp As New cv.Mat
        super.dnn.upsample(src, tmp)
        dst2 = tmp.Resize(dst2.Size)
        dst3 = dst2 - src
    End Sub
End Class