Imports cvb = OpenCvSharp
Imports  System.IO
' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Basics : Inherits VB_Parent
    Public minBrushRange = New cvb.Rangef(0.1, 0.3)
    Public maxBrushRange = New cvb.Rangef(0.3, 0.7)
    Dim minSize As Single
    Dim maxSize As Single
    Public brushes(4 - 1) As cvb.Mat
    Dim DNAseq() As DNAentry
    Dim totalError As Single
    Dim stage As Integer
    Public generation As Integer
    Dim imgGeneration As cvb.Mat
    Dim imgStage As cvb.Mat
    Public mats As New Mat_4Click
    Dim options As Options_GeneticDrawing
    Public gradient As New Gradient_CartToPolar
    Public restartRequested As Boolean = True
    Public Sub New()
        options = New Options_GeneticDrawing()
        For i = 0 To brushes.Count - 1
            brushes(i) = cvb.Cv2.ImRead(task.HomeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Next

        labels(2) = "(clkwise) original, imgStage, imgGeneration, magnitude"
        labels(3) = "Current result"
        desc = "Create a painting from the current video input using a genetic algorithm. Draw anywhere to focus brushes"
    End Sub
    Private Function runDNAseq(dna() As DNAentry) As cvb.Mat
        Dim nextImage = imgGeneration.Clone()
        For i = 0 To dna.Count - 1
            Dim d = dna(i)
            Dim brushImg = brushes(d.brushNumber)

            Dim br = brushImg.Resize(New cvb.Size(CInt((brushImg.Width * d.size + 1) * options.brushPercent),
                                                 CInt((brushImg.Height * d.size + 1) * options.brushPercent)))
            Dim m = cvb.Cv2.GetRotationMatrix2D(New cvb.Point2f(br.Cols / 2, br.Rows / 2), d.rotation, 1)
            cvb.Cv2.WarpAffine(br, br, m, New cvb.Size(br.Cols, br.Rows))

            If d.rotation < 0 Then
                d.pt = New cvb.Point(d.pt.X - br.Width, d.pt.Y - br.Height)
                If d.pt.X < 0 Then d.pt.X = 0
                If d.pt.Y < 0 Then d.pt.Y = 0
            End If

            Dim background As New cvb.Mat, alpha As New cvb.Mat
            Dim rect = New cvb.Rect(d.pt.X, d.pt.Y, br.Width, br.Height)
            If d.pt.X + rect.Width > nextImage.Width Then rect.Width = nextImage.Width - d.pt.X
            If d.pt.Y + rect.Height > nextImage.Height Then rect.Height = nextImage.Height - d.pt.Y
            If br.Width <> rect.Width Or br.Height <> rect.Height Then br = br(New cvb.Rect(0, 0, rect.Width, rect.Height))
            nextImage(rect).ConvertTo(background, cvb.MatType.CV_32F)

            br.ConvertTo(alpha, cvb.MatType.CV_32F, 1 / 255)
            Dim foreground = New cvb.Mat(New cvb.Size(rect.Width, rect.Height), cvb.MatType.CV_32F, CSng(d.color))
            cvb.Cv2.Multiply(alpha, foreground, foreground)
            cvb.Cv2.Multiply((1.0 - alpha).ToMat, background, background)

            cvb.Cv2.Add(foreground, background, foreground)
            foreground.ConvertTo(nextImage(rect), cvb.MatType.CV_8U)
        Next
        Return nextImage
    End Function
    Private Function calcBrushSize(range As cvb.Rangef) As Single
        Dim t = stage / Math.Max(options.stageTotal - 1, 1)
        Return (range.End - range.Start) * (-t * t + 1) + range.Start
    End Function
    Private Function calculateError(ByRef img As cvb.Mat) As Single
        ' compute error for resulting image.
        Dim diff1 As New cvb.Mat, diff2 As New cvb.Mat
        cvb.Cv2.Subtract(mats.mat(0), img, diff1)
        cvb.Cv2.Subtract(img, mats.mat(0), diff2)
        cvb.Cv2.Add(diff1, diff2, diff1)
        Return diff1.Sum()
    End Function
    Private Sub startNewStage(r As cvb.Rect)
        ReDim DNAseq(options.strokeCount - 1)
        minSize = calcBrushSize(minBrushRange)
        maxSize = calcBrushSize(maxBrushRange)

        For i = 0 To options.strokeCount - 1
            Dim e = New DNAentry
            e.color = msRNG.Next(0, 255)
            e.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
            e.pt = New cvb.Point(r.X + msRNG.Next(r.Width), r.Y + msRNG.Next(r.Height))
            Dim localMagnitude = gradient.magnitude.Get(Of Single)(e.pt.Y, e.pt.X)
            Dim localAngle = gradient.angle.Get(Of Single)(e.pt.Y, e.pt.X) + 90
            e.rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
            e.brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
            DNAseq(i) = e
        Next

        imgGeneration = imgStage
        mats.mat(3) = runDNAseq(DNAseq)
        totalError = calculateError(mats.mat(3))
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.intermediateObject IsNot Nothing Then
            SetTrueText("There are too many operations inside GeneticDrawing_Basics to break down the intermediate results")
            Exit Sub
        End If

        Static r = New cvb.Rect(0, 0, src.Width, src.Height)
        If task.drawRect.Width > 0 Then r = task.drawRect
        If restartRequested Then
            restartRequested = False
            dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
            imgStage = dst3.Clone
            generation = 0
            stage = 0

            If standaloneTest() Then
                src = If(options.snapCheck, src.Clone, cvb.Cv2.ImRead(task.HomeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size()))
            End If

            src = If(src.Channels() = 3, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), src)
            mats.mat(0) = src
            gradient.Run(mats.mat(0))
            mats.mat(2) = gradient.magnitude.ConvertScaleAbs(255)

            startNewStage(r)
        End If
        If stage >= options.stageTotal Then Exit Sub ' request is complete...
        If DNAseq Is Nothing Then
            restartRequested = True
            Exit Sub
        End If
        ' evolve!
        Dim nextDNA(DNAseq.Count - 1) As DNAentry
        For i = 0 To DNAseq.Count - 1
            nextDNA(i) = DNAseq(i)
        Next
        Dim changes As Integer, childImg = imgGeneration.Clone, maxOption = 5, bestError As Single
        For i = 0 To nextDNA.Count - 1
            Dim changeCount = msRNG.Next(0, maxOption) + 1
            For j = 0 To changeCount - 1
                Select Case msRNG.Next(0, maxOption)
                    Case 0
                        nextDNA(i).color = CInt(msRNG.Next(0, 255))
                    Case 1, 2
                        nextDNA(i).pt = New cvb.Point(msRNG.Next(r.x, r.X + r.width), msRNG.Next(r.y, r.y + r.height))
                    Case 3
                        nextDNA(i).size = msRNG.NextDouble() * (maxSize - minSize) + minSize
                    Case 4
                        Dim localMagnitude = gradient.magnitude.Get(Of Single)(nextDNA(i).pt.Y, nextDNA(i).pt.X)
                        Dim localAngle = gradient.angle.Get(Of Single)(nextDNA(i).pt.Y, nextDNA(i).pt.X) + 90
                        nextDNA(i).rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
                    Case Else
                        nextDNA(i).brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
                End Select
            Next

            childImg = runDNAseq(nextDNA)
            Dim nextError = calculateError(childImg)
            If nextError < totalError Then
                bestError = nextError
                changes += 1
            Else
                nextDNA(i) = DNAseq(i)
            End If
        Next

        If changes Then
            totalError = bestError
            mats.mat(3) = runDNAseq(nextDNA)
            DNAseq = nextDNA
        End If

        generation += 1
        If generation = options.generations Then
            imgStage = mats.mat(3)
            mats.mat(1) = imgStage
            generation = 0
            stage += 1
            startNewStage(r)
        End If

        mats.Run(empty)
        dst2 = mats.dst2
        labels(3) = " stage " + CStr(stage) + "/" + CStr(options.stageTotal) + " Gen " + Format(generation, "00") + " chgs = " + CStr(changes) + " err/1000 = " + CStr(CInt(totalError / 1000))
        dst3 = mats.mat(mats.quadrant)
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Color : Inherits VB_Parent
    Dim gDraw(3 - 1) As GeneticDrawing_Basics
    Public Sub New()

        gDraw(0) = New GeneticDrawing_Basics()
        gDraw(1) = New GeneticDrawing_Basics()
        gDraw(2) = New GeneticDrawing_Basics()

        labels(2) = "Intermediate results - original+2 partial+Mag"
        desc = "Use the GeneticDrawing_Basics to create a color painting.  Draw anywhere to focus brushes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static restartCheck = FindCheckBox("Restart the algorithm with the current settings")
        Dim split() As cvb.Mat
        split = src.Split()

        Dim restartRequested = restartCheck.checked
        restartCheck.checked = False
        For i = 0 To split.Count - 1
            gDraw(i).restartRequested = restartRequested
            gDraw(i).Run(split(i))
            split(i) = gDraw(i).dst3
        Next

        cvb.Cv2.Merge(split, dst3)

        For i = 0 To split.Count - 1
            split(i) = If(gDraw(i).dst2.Channels() = 1, gDraw(i).dst2, gDraw(i).dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        Next
        cvb.Cv2.Merge(split, dst2)

        labels(3) = gDraw(2).labels(3)
    End Sub
End Class





Public Class GeneticDrawing_Photo : Inherits VB_Parent
    Dim gDraw As GeneticDrawing_Color
    Dim inputFileName As String
    Dim fileNameForm As OptionsFileName
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.HomeDir + "Data/"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "PhotoFileName", "PhotoFileName", task.HomeDir + "Data/GeneticDrawingExample.jpg")
        fileNameForm.Text = "Select an image file to create a paint version"
        fileNameForm.FileNameLabel.Text = "Select a file for use with the Sound_Basics algorithm."
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        desc = "Apply genetic drawing technique to any still photo.  Draw anywhere to focus brushes"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        Static fileInputName = New FileInfo(fileNameForm.filename.Text)
        If inputFileName <> fileInputName.FullName Or tInfo.optionsChanged Then
            inputFileName = fileInputName.FullName
            If fileInputName.Exists = False Then
                labels(2) = "No input file specified or file not found."
                Exit Sub
            End If

            Dim fullsizeImage = cvb.Cv2.ImRead(fileInputName.FullName)
            If fullsizeImage.Channels() <> 3 Then
                labels(2) = "Input file must be BGR 3-channel image!"
                Exit Sub
            End If
            ' If gDraw IsNot Nothing Then gDraw.Dispose()
            gDraw = New GeneticDrawing_Color()

            If fullsizeImage.Width <> dst2.Width Or fullsizeImage.Height <> dst2.Height Then
                Dim newSize = New cvb.Size(dst2.Height * fullsizeImage.Width / fullsizeImage.Height, dst2.Height)
                If newSize.Width > dst2.Width Then
                    newSize = New cvb.Size(dst2.Width, dst2.Width * fullsizeImage.Height / fullsizeImage.Width)
                End If
                src.SetTo(0)
                src(New cvb.Rect(0, 0, newSize.Width, newSize.Height)) = fullsizeImage.Resize(newSize)
            Else
                src = fullsizeImage
            End If
            SaveSetting("OpenCVB", "PhotoFileName", "PhotoFileName", fileInputName.FullName)
        End If

        gDraw.Run(src)

        dst2 = gDraw.dst2
        dst3 = gDraw.dst3
        labels(2) = gDraw.labels(2)
        labels(3) = gDraw.labels(3)
    End Sub
End Class





