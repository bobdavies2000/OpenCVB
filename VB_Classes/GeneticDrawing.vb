Imports cv = OpenCvSharp
Imports System.IO



Public Class GeneticDrawing_Options
    Inherits VBparent
    Public stageTotal = 100
    Public Sub New()
        initParent()
        Windows.Forms.Application.DoEvents()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"
            check.Box(1).Text = "Restart the algorithm with the current settings"
            check.Box(1).Checked = True
        End If

        Windows.Forms.Application.DoEvents()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of Generations", 1, 200, 20)
            sliders.setupTrackBar(1, "Number of Stages", 1, 2000, stageTotal)
            sliders.setupTrackBar(2, "Brushstroke count per generation", 1, 20, 10)
            sliders.setupTrackBar(3, "Brush size Percentage", 5, 100, 100)
        End If
        task.desc = "Display all the options available to genetic drawing algorithms."
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        task.trueText("There is no output for this algorithm - just controls showing the genetic drawing options")
    End Sub
End Class




Public Structure DNAentry
    Dim color As Byte
    Dim pt As cv.Point
    Dim size As Single
    Dim rotation As Single
    Dim brushNumber As Integer
End Structure
' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Basics
    Inherits VBparent
    Public minBrushRange = New cv.Rangef(0.1, 0.3)
    Public maxBrushRange = New cv.Rangef(0.3, 0.7)
    Dim minSize As Single
    Dim maxSize As Single
    Public brushes(4 - 1) As cv.Mat
    Dim DNAseq() As DNAentry
    Dim totalError As Single
    Dim stage As Integer
    Public generation As Integer
    Dim imgGeneration As cv.Mat
    Dim imgStage As cv.Mat
    Public mats As Mat_4to1
    Dim brushPercent As Integer
    Dim options As GeneticDrawing_Options
    Dim stageTotal = 100
    Public gradient As Gradient_CartToPolar
    Public restartRequested As Boolean = True
    Public Sub New()
        initParent()

        options = New GeneticDrawing_Options()

        gradient = New Gradient_CartToPolar()
        For i = 0 To brushes.Count - 1
            brushes(i) = cv.Cv2.ImRead(task.parms.homeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Next

        mats = New Mat_4to1()

        label1 = "(clkwise) original, imgStage, imgGeneration, magnitude"
        label2 = "Current result"
        task.desc = "Create a painting from the current video input using a genetic algorithm. Draw anywhere to focus brushes. Painterly"
		' task.rank = 1
    End Sub
    Private Function runDNAseq(dna() As DNAentry) As cv.Mat
        Dim nextImage = imgGeneration.Clone()

        For i = 0 To dna.Count - 1
            Dim d = dna(i)
            Dim brushImg = brushes(d.brushNumber)

            Dim br = brushImg.Resize(New cv.Size((brushImg.Width * d.size + 1) * brushPercent / 100, (brushImg.Height * d.size + 1) * brushPercent / 100))
            Dim m = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(br.Cols / 2, br.Rows / 2), d.rotation, 1)
            cv.Cv2.WarpAffine(br, br, m, New cv.Size(br.Cols, br.Rows))

            If d.rotation < 0 Then
                d.pt = New cv.Point(d.pt.X - br.Width, d.pt.Y - br.Height)
                If d.pt.X < 0 Then d.pt.X = 0
                If d.pt.Y < 0 Then d.pt.Y = 0
            End If

            Dim background As New cv.Mat, alpha As New cv.Mat
            Dim rect = New cv.Rect(d.pt.X, d.pt.Y, br.Width, br.Height)
            If d.pt.X + rect.Width > nextImage.Width Then rect.Width = nextImage.Width - d.pt.X
            If d.pt.Y + rect.Height > nextImage.Height Then rect.Height = nextImage.Height - d.pt.Y
            If br.Width <> rect.Width Or br.Height <> rect.Height Then br = br(New cv.Rect(0, 0, rect.Width, rect.Height))
            nextImage(rect).ConvertTo(background, cv.MatType.CV_32F)

            br.ConvertTo(alpha, cv.MatType.CV_32F, 1 / 255)
            Dim foreground = New cv.Mat(New cv.Size(rect.Width, rect.Height), cv.MatType.CV_32F, CSng(d.color))
            cv.Cv2.Multiply(alpha, foreground, foreground)
            cv.Cv2.Multiply((1.0 - alpha).ToMat, background, background)

            cv.Cv2.Add(foreground, background, foreground)
            foreground.ConvertTo(nextImage(rect), cv.MatType.CV_8U)
        Next
        Return nextImage
    End Function
    Private Function calcBrushSize(range As cv.Rangef) As Single
        Dim t = stage / Math.Max(stageTotal - 1, 1)
        Return (range.End - range.Start) * (-t * t + 1) + range.Start
    End Function
    Private Function calculateError(ByRef img As cv.Mat) As Single
        ' compute error for resulting image.
        Dim diff1 As New cv.Mat, diff2 As New cv.Mat
        cv.Cv2.Subtract(mats.mat(0), img, diff1)
        cv.Cv2.Subtract(img, mats.mat(0), diff2)
        cv.Cv2.Add(diff1, diff2, diff1)
        Return diff1.Sum()
    End Function
    Private Sub startNewStage(r As cv.Rect)
        Static strokeSlider = findSlider("Brushstroke count per generation")
        Dim brushstrokeCount = strokeSlider.Value

        ReDim DNAseq(brushstrokeCount - 1)
        minSize = calcBrushSize(minBrushRange)
        maxSize = calcBrushSize(maxBrushRange)

        For i = 0 To brushstrokeCount - 1
            Dim e = New DNAentry
            e.color = msRNG.Next(0, 255)
            e.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
            e.pt = New cv.Point(r.X + msRNG.Next(r.Width), r.Y + msRNG.Next(r.Height))
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
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Static genSlider = findSlider("Number of Generations")
        Static stageSlider = findSlider("Number of Stages")
        Static brushSlider = findSlider("Brush size Percentage")
        Static gradientMagSlider = findSlider("Contrast exponent to use X100")
        Static sobelSlider = findSlider("Sobel kernel Size")
        Static snapCheck = findCheckBox("Snapshot Video input to initialize genetic drawing")

        brushPercent = brushSlider.Value
        stageTotal = stageSlider.Value
        Dim sobelKernel = sobelSlider.Value
        Static r = New cv.Rect(0, 0, src.Width, src.Height)
        If task.drawRect.Width > 0 Then r = task.drawRect
        If restartRequested Then
            restartRequested = False
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
            imgStage = dst2.Clone
            generation = 0
            stage = 0

            If standalone Or task.intermediateReview = caller Then
                src = If(snapCheck.Checked, src.Clone, cv.Cv2.ImRead(task.parms.homeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size()))
            End If
            snapCheck.Checked = False

            src = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)
            mats.mat(0) = src
            gradient.src = mats.mat(0)
            gradient.Run()
            mats.mat(2) = gradient.magnitude.ConvertScaleAbs(255)

            startNewStage(r)
        End If
        If stage >= stageTotal Then Exit Sub ' request is complete...

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
                        nextDNA(i).pt = New cv.Point(msRNG.Next(r.x, r.X + r.width), msRNG.Next(r.y, r.y + r.height))
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
        If generation = genSlider.Value Then
            imgStage = mats.mat(3)
            mats.mat(1) = imgStage
            generation = 0
            stage += 1
            startNewStage(r)
        End If

        mats.Run()
        dst1 = mats.dst1
        label2 = " stage " + CStr(stage) + "/" + CStr(stageTotal) + " Gen " + Format(generation, "00") + " chgs = " + CStr(changes) + " err/1000 = " + CStr(CInt(totalError / 1000))
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Color
    Inherits VBparent
    Dim gDraw(3 - 1) As GeneticDrawing_Basics
    Public Sub New()
        initParent()

        gDraw(0) = New GeneticDrawing_Basics()
        gDraw(1) = New GeneticDrawing_Basics()
        gDraw(2) = New GeneticDrawing_Basics()

        label1 = "Intermediate results - original+2 partial+Mag"
        task.desc = "Use the GeneticDrawing_Basics to create a color painting.  Draw anywhere to focus brushes. Painterly"
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim split() As cv.Mat
        split = src.Split()

        Static restartCheck = findCheckBox("Restart the algorithm with the current settings")
        Dim restartRequested = restartCheck.checked
        restartCheck.checked = False
        For i = 0 To split.Count - 1
            gDraw(i).restartRequested = restartRequested
            gDraw(i).src = split(i)
            gDraw(i).Run()
            split(i) = gDraw(i).dst2
        Next

        cv.Cv2.Merge(split, dst2)

        For i = 0 To split.Count - 1
            split(i) = gDraw(i).dst1
        Next
        cv.Cv2.Merge(split, dst1)

        label2 = gDraw(2).label2
    End Sub
End Class





Public Class GeneticDrawing_Photo
    Inherits VBparent
    Dim gDraw As GeneticDrawing_Color
    Dim inputFileName As String
    Dim fileNameForm As OptionsFileName
    Public Sub New()
        initParent()

        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.parms.homeDir + "Data/"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "PhotoFileName", "PhotoFileName", task.parms.homeDir + "Data/GeneticDrawingExample.jpg")
        fileNameForm.Text = "Select an image file to create a paint version"
        fileNameForm.Label1.Text = "Select a file for use with the Sound_Basics algorithm."
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(caller)
        fileNameForm.Show()

        task.desc = "Apply genetic drawing technique to any still photo.  Draw anywhere to focus brushes. Painterly"
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Static fileInputName = New FileInfo(fileNameForm.filename.Text)
        If inputFileName <> fileInputName.FullName Or task.frameCount = 0 Then
            inputFileName = fileInputName.FullName
            If fileInputName.Exists = False Then
                label1 = "No input file specified or file not found."
                Exit Sub
            End If

            Dim fullsizeImage = cv.Cv2.ImRead(fileInputName.FullName)
            If fullsizeImage.Channels <> 3 Then
                label1 = "Input file must be RGB 3-channel image!"
                Exit Sub
            End If
            If gDraw IsNot Nothing Then gDraw.Dispose()
            gDraw = New GeneticDrawing_Color()

            If fullsizeImage.Width <> dst1.Width Or fullsizeImage.Height <> dst1.Height Then
                Dim newSize = New cv.Size(dst1.Height * fullsizeImage.Width / fullsizeImage.Height, dst1.Height)
                If newSize.Width > dst1.Width Then
                    newSize = New cv.Size(dst1.Width, dst1.Width * fullsizeImage.Height / fullsizeImage.Width)
                End If
                src.SetTo(0)
                src(New cv.Rect(0, 0, newSize.Width, newSize.Height)) = fullsizeImage.Resize(newSize)
            Else
                src = fullsizeImage
            End If
            SaveSetting("OpenCVB", "PhotoFileName", "PhotoFileName", fileInputName.FullName)
            gDraw.src = src
        End If

        gDraw.Run()

        dst1 = gDraw.dst1
        dst2 = gDraw.dst2
        label1 = gDraw.label1
        label2 = gDraw.label2
    End Sub
End Class

