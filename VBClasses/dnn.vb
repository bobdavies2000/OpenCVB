Imports cv = OpenCvSharp
Imports OpenCvSharp.Dnn
Imports System.IO
Imports OpenCvSharp.DnnSuperres
' https://github.com/twMr7/rscvdnn
Namespace VBClasses
    Public Class NR_DNN_Basics : Inherits TaskParent
        Dim net As Net
        Dim dnnPrepared As Boolean
        Dim crop As cv.Rect
        Dim dnnWidth As Integer, dnnHeight As Integer
        Dim kalman(10) As Kalman_Basics
        Public rect As cv.Rect
        Dim options As New Options_DNN
        Dim classNames() = {"background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                            "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"}
        Dim activeKalman As Integer
        Public Sub New()
            For i = 0 To kalman.Count - 1
                kalman(i) = New Kalman_Basics()
                ReDim kalman(i).kInput(4 - 1)
                ReDim kalman(i).kOutput(4 - 1)
            Next

            dnnWidth = dst2.Height ' height is always smaller than width...
            dnnHeight = dst2.Height
            crop = New cv.Rect(dst2.Width / 2 - dnnWidth / 2, dst2.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight)

            Dim infoText As New FileInfo(atask.homeDir + "Data/MobileNetSSD_deploy.prototxt")
            If infoText.Exists Then
                Dim infoModel As New FileInfo(atask.homeDir + "Data/MobileNetSSD_deploy.caffemodel")
                If infoModel.Exists Then
                    net = CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName)
                    dnnPrepared = True
                End If
            End If
            If dnnPrepared = False Then
                SetTrueText("Caffe databases not found.  It should be in <OpenCVB_HomeDir>/Data.")
            End If
            desc = "Use OpenCV's dnn from Caffe file."
            labels(2) = "Cropped Input Image - must be square!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If dnnPrepared Then
                Dim inScaleFactor As Single = options.ScaleFactor / options.scaleMax ' should be 0.0078 by default...
                Dim inputBlob = CvDnn.BlobFromImage(src(crop), inScaleFactor, New cv.Size(300, 300), CSng(options.meanValue), False)
                src.CopyTo(dst3)
                src(crop).CopyTo(dst2(crop))
                net.SetInput(inputBlob, "data")

                Dim detection = net.Forward("detection_out")
                Dim detectionMat = cv.Mat.FromPixelData(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

                Dim rows = src(crop).Rows
                Dim cols = src(crop).Cols
                labels(3) = ""

                Dim kPoints As New List(Of cv.Point)
                For i = 0 To detectionMat.Rows - 1
                    Dim confidence = detectionMat.Get(Of Single)(i, 2)
                    If confidence > options.confidenceThreshold Then
                        Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                        If kalman(i).kInput(0) = 0 And kalman(i).kInput(1) = 0 Then
                            kPoints.Add(New cv.Point2f(vec(0) * cols + crop.Left, vec(1) * rows + crop.Top))
                        Else
                            kPoints.Add(New cv.Point2f(kalman(i).kInput(0), kalman(i).kInput(1)))
                        End If
                    End If
                Next

                If kPoints.Count > activeKalman Then activeKalman = kPoints.Count
                For i = 0 To detectionMat.Rows - 1
                    Dim confidence = detectionMat.Get(Of Single)(i, 2)
                    If confidence > options.confidenceThreshold Then
                        Dim nextName = classNames(CInt(detectionMat.Get(Of Single)(i, 1)))
                        labels(3) += nextName + " "  ' display the name of what we found.
                        Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                        rect = New cv.Rect(vec(0) * cols + crop.Left, vec(1) * rows + crop.Top, (vec(2) - vec(0)) * cols, (vec(3) - vec(1)) * rows)
                        rect = New cv.Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height))

                        Dim pt = rect.TopLeft
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
                        dst3.Rectangle(rect, cv.Scalar.Yellow, atask.lineWidth + 2, atask.lineType)
                        rect.Width = src.Width / 12
                        rect.Height = src.Height / 16
                        dst3.Rectangle(rect, cv.Scalar.Black, -1)
                        SetTrueText(nextName, New cv.Point(rect.X, rect.Y), 3)
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
    Public Class NR_DNN_SuperRes : Inherits TaskParent
        Public options As New Options_DNN
        Public dnn = New DnnSuperResImpl("fsrcnn", 4)
        Dim saveModelFile = ""
        Dim multiplier As Integer
        Public Sub New()
            atask.drawRect = New cv.Rect(10, 10, 20, 20)
            labels(2) = "Output of a resize using OpenCV"
            desc = "Get better super-resolution through a DNN"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If saveModelFile <> options.superResModelFileName Then
                saveModelFile = options.superResModelFileName
                multiplier = options.superResMultiplier
                dnn = New DnnSuperResImpl(options.shortModelName, multiplier)
                dnn.ReadModel(saveModelFile)
            End If
            Dim r = atask.drawRect
            If atask.drawRect.Width = 0 Or atask.drawRect.Height = 0 Then Exit Sub
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







    Public Class NR_DNN_SuperResize : Inherits TaskParent
        Dim super = New NR_DNN_SuperRes
        Public Sub New()
            labels(2) = "Super Res resized back to original size"
            labels(3) = "dst3 = dst2 - src or no difference - honors original"
            desc = "Compare superRes reduced to original size"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            super.Run(src)
            Dim r = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Dim tmp As New cv.Mat
            super.dnn.upsample(src, tmp)
            dst2 = tmp.Resize(dst2.Size)
            dst3 = dst2 - src
        End Sub
    End Class




    'Public Class DNN_Test : Inherits TaskParent
    '    Dim net As Net
    '    Dim classnames() As String
    '    Public Sub New()
    '        Dim modelFile As New FileInfo(atask.homeDir + "Data/bvlc_googlenet.caffemodel")
    '        If File.Exists(modelFile.FullName) = False Then
    '            ' this site is apparently gone.  caffemodel is in the Data directory in OpenCVB_HomeDir
    '            Dim client = HttpWebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel")
    '            Dim response = client.GetResponse()
    '            Dim responseStream = response.GetResponseStream()
    '            Dim memory As New MemoryStream()
    '            responseStream.CopyTo(memory)
    '            File.WriteAllBytes(modelFile.FullName, memory.ToArray)
    '        End If
    '        Dim protoTxt = atask.homeDir + "Data/bvlc_googlenet.prototxt"
    '        net = CvDnn.ReadNetFromCaffe(protoTxt, modelFile.FullName)
    '        Dim synsetWords = atask.homeDir + "Data/synset_words.txt"
    '        classnames = File.ReadAllLines(synsetWords) ' .Select(line >= line.Split(' ').Last()).ToArray()
    '        For i = 0 To classnames.Count - 1
    '            classnames(i) = classnames(i).Split(" ").Last
    '        Next

    '        labels(3) = "Input Image"
    '        desc = "Download and use a Caffe database"
    '    End Sub
    '    Public Overrides sub RunAlg(src As cv.Mat)

    '        Dim image = cv.Cv2.ImRead(atask.homeDir + "Data/space_shuttle.jpg")
    '        dst3 = image.Resize(dst3.Size())
    '        Dim inputBlob = CvDnn.BlobFromImage(image, 1, New cv.Size(224, 224), New cv.Scalar(104, 117, 123))
    '        net.SetInput(inputBlob, "data")
    '        Dim prob = net.Forward("prob")

    '        Dim mm As mmData = GetMinMax(prob.Reshape(1, 1))
    '        SetTrueText("Best classification: index = " + CStr(mm.maxLoc.X) + " which is for '" + classnames(mm.maxLoc.X) + "' with Probability " +
    '                    Format(mm.maxVal, "#0.00%"), New cv.Point(40, 200))
    '    End Sub
    'End Class
End Namespace