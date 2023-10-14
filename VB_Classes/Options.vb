Imports cv = OpenCvSharp
Imports System.IO
Imports System.Numerics
Imports System.Windows.Forms
Imports System.Windows.Controls
Imports System.Drawing

Public Class Options_Annealing : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public anneal As Annealing_Basics_CPP
    Public cityCount As Integer
    Public copyBestFlag As Boolean
    Public circularFlag As Boolean
    Public successCount As Integer
    Public Sub New()
        random.Run(Nothing) ' get the city positions (may or may not be used below.)

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Anneal Number of Cities", 5, 500, 25)
            sliders.setupTrackBar("Success = top X threads agree on energy level.", 2, Environment.ProcessorCount, 4)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Restart Traveling Salesman")
            check.addCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half")
            check.addCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
            check.Box(0).Checked = True
            check.Box(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static travelCheck = findCheckBox("Restart Traveling Salesman")
        Static circleCheck = findCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static copyBestCheck = findCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half")
        Static circularCheck = findCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static citySlider = findSlider("Anneal Number of Cities")
        Static successSlider = findSlider("Success = top X threads agree on energy level.")

        copyBestFlag = copyBestCheck.checked
        circularFlag = circularCheck.checked
        cityCount = citySlider.Value
        successCount = successSlider.Value
        travelCheck.Checked = False
    End Sub
End Class






Public Class Options_BGSubtract : Inherits VB_Algorithm
    Public MOGlearnRate As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("MOG Learn Rate X1000", 1, 1000, 1)
    End Sub
    Public Sub RunVB()
        Static learnRateSlider = findSlider("MOG Learn Rate X1000")
        MOGlearnRate = learnRateSlider.Value / 1000
    End Sub
End Class








Public Class Options_Blob : Inherits VB_Algorithm
    Dim blob As New Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public blobParams = New cv.SimpleBlobDetector.Params
    Public Sub New()
        blobDetector = New CS_Classes.Blob_Basics
        If standalone Then blob.updateFrequency = 30

        If radio.Setup(traceName) Then
            radio.addRadio("FilterByArea")
            radio.addRadio("FilterByCircularity")
            radio.addRadio("FilterByConvexity")
            radio.addRadio("FilterByInertia")
            radio.addRadio("FilterByColor")
            radio.check(1).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("min Threshold", 0, 255, 100)
            sliders.setupTrackBar("max Threshold", 0, 255, 255)
            sliders.setupTrackBar("Threshold Step", 1, 50, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static minSlider = findSlider("min Threshold")
        Static maxSlider = findSlider("max Threshold")
        Static stepSlider = findSlider("Threshold Step")
        Static areaRadio = findRadio("FilterByArea")
        Static circRadio = findRadio("FilterByCircularity")
        Static convexRadio = findRadio("FilterByConvexity")
        Static inertiaRadio = findRadio("FilterByInertia")
        Static colorRadio = findRadio("FilterByColor")

        blobParams = New cv.SimpleBlobDetector.Params
        If areaRadio.Checked Then blobParams.FilterByArea = areaRadio.Checked
        If circRadio.Checked Then blobParams.FilterByCircularity = circRadio.Checked
        If convexRadio.Checked Then blobParams.FilterByConvexity = convexRadio.Checked
        If inertiaRadio.Checked Then blobParams.FilterByInertia = inertiaRadio.Checked
        If colorRadio.Checked Then blobParams.FilterByColor = colorRadio.Checked

        blobParams.MaxArea = 100
        blobParams.MinArea = 0.001

        blobParams.MinThreshold = minSlider.Value
        blobParams.MaxThreshold = maxSlider.Value
        blobParams.ThresholdStep = stepSlider.Value

        blobParams.MinDistBetweenBlobs = 10
        blobParams.MinRepeatability = 1
    End Sub
End Class






Public Class Options_CamShift : Inherits VB_Algorithm
    Public camMax As Integer
    Public camSBins As cv.Scalar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("CamShift vMin", 0, 255, 32)
            sliders.setupTrackBar("CamShift vMax", 0, 255, 255)
            sliders.setupTrackBar("CamShift Smin", 0, 255, 40)
        End If
    End Sub
    Public Sub RunVB()
        Static vMinSlider = findSlider("CamShift vMin")
        Static vMaxSlider = findSlider("CamShift vMax")
        Static sMinSlider = findSlider("CamShift Smin")

        Dim vMin = vMinSlider.Value
        Dim vMax = vMaxSlider.Value
        Dim sMin = sMinSlider.Value

        Dim min = Math.Min(vMin, vMax)
        camMax = Math.Max(vMin, vMax)
        camSBins = New cv.Scalar(0, sMin, min)
    End Sub
End Class







Public Class Options_Contours2 : Inherits VB_Algorithm
    Public ApproximationMode As cv.ContourApproximationModes
    Dim radioChoices = {cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                        cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1}
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("ApproxNone")
            radio.addRadio("ApproxSimple")
            radio.addRadio("ApproxTC89KCOS")
            radio.addRadio("ApproxTC89L1")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        ApproximationMode = radioChoices(findRadioIndex(frm.check))
    End Sub
End Class





Public Class Options_Contours : Inherits VB_Algorithm
    Public retrievalMode As cv.RetrievalModes
    Public ApproximationMode As cv.ContourApproximationModes
    Public epsilon As Single
    Public minLength = 4 ' use any contour with enough points to make a contour!
    Dim options2 As New Options_Contours2
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("CComp")
            radio.addRadio("External")
            radio.addRadio("FloodFill")
            radio.addRadio("List")
            radio.addRadio("Tree")
            radio.check(1).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Contour epsilon (arc length percent)", 0, 100, 3)
    End Sub
    Public Sub RunVB()
        options2.RunVB()
        Static epsilonSlider = findSlider("Contour epsilon (arc length percent)")

        epsilon = epsilonSlider.Value

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        ApproximationMode = options2.ApproximationMode
    End Sub
End Class






Public Class Options_Draw : Inherits VB_Algorithm
    Public drawCount As Integer
    Public updateFrequency As Integer
    Public drawFilled As Integer
    Public drawRotated As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("DrawCount", 0, 20, 3)

        If check.Setup(traceName) Then
            check.addCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            check.addCheckBox("Draw filled (unchecked draw an outline)")
        End If
    End Sub
    Public Sub RunVB()
        Static countSlider = findSlider("DrawCount")
        Static fillCheck = findCheckBox("Draw filled (unchecked draw an outline)")
        Static rotateCheck = findCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        drawCount = countSlider.Value
        drawFilled = If(fillCheck.checked, -1, 2)
        drawRotated = rotateCheck.checked
    End Sub
End Class



' https://answers.opencv.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Options_Encode : Inherits VB_Algorithm
    Public qualityLevel As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
            sliders.setupTrackBar("Encode Output Scaling", 1, 100, 85)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("JpegChromaQuality")
            radio.addRadio("JpegLumaQuality")
            radio.addRadio("JpegOptimize")
            radio.addRadio("JpegProgressive")
            radio.addRadio("JpegQuality")
            radio.addRadio("WebPQuality")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Function getEncodeParameter() As Integer
        Static qualitySlider = findSlider("Encode Quality Level")
        Static frm = findfrm(traceName + " Radio Buttons")
        Dim encodeOption As Integer
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next
        qualityLevel = qualitySlider.Value
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
        Return encodeOption
    End Function
    Public Sub RunVB()
        Dim src = task.color
        Static scaleSlider = findSlider("Encode Output Scaling")

        Dim fileExtension = ".jpg"
        Dim encodeParams() As Integer = {getEncodeParameter(), qualityLevel}

        Dim buf() = src.ImEncode(".jpg", encodeParams)
        Dim image = New cv.Mat(buf.Count, 1, cv.MatType.CV_8U, buf)
        dst3 = cv.Cv2.ImDecode(image, cv.ImreadModes.AnyColor)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(src, dst3, output)

        Dim scale = scaleSlider.Value
        output.ConvertTo(dst2, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
    End Sub
End Class










Public Class Options_Filter : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter kernel size", 1, 21, 3)
    End Sub
    Public Sub RunVB()
    End Sub
End Class






Public Class Options_GeneticDrawing : Inherits VB_Algorithm
    Public stageTotal = 100
    Public Sub New()
        Windows.Forms.Application.DoEvents()

        If check.Setup(traceName) Then
            check.addCheckBox("Snapshot Video input to initialize genetic drawing")
            check.addCheckBox("Restart the algorithm with the current settings")
            check.Box(1).Checked = True
        End If

        Windows.Forms.Application.DoEvents()

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of Generations", 1, 200, 20)
            sliders.setupTrackBar("Number of Stages", 1, 2000, stageTotal)
            sliders.setupTrackBar("Brushstroke count per generation", 1, 20, 10)
            sliders.setupTrackBar("Brush size Percentage", 5, 100, 100)
        End If
    End Sub
    Public Sub RunVB()
    End Sub
End Class









Public Class Options_Line : Inherits VB_Algorithm
    Public lineLengthThreshold As Integer
    Public searchRange As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Line length threshold in pixels", 1, 400, 20)
    End Sub
    Public Sub RunVB()
        Static lenSlider = findSlider("Line length threshold in pixels")
        lineLengthThreshold = lenSlider.Value
    End Sub
End Class










Public Class Options_MatchShapes : Inherits VB_Algorithm
    Public matchOption As cv.ShapeMatchModes
    Public matchThreshold As Single
    Public maxYdelta As Single
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("I1 - Hu moments absolute sum of inverse differences")
            radio.addRadio("I2 - Hu moments absolute difference")
            radio.addRadio("I3 - Hu moments max absolute difference ratio")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Match Threshold %", 0, 100, 80)
            sliders.setupTrackBar("Max Y Delta % (of height)", 0, 10, 5)
            sliders.setupTrackBar("Min Size % of image size", 0, 20, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Match Threshold %")
        Static ySlider = findSlider("Max Y Delta % (of height)")
        matchThreshold = thresholdSlider.Value / 100
        maxYdelta = ySlider.Value * dst2.Height / 100

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.ShapeMatchModes.I1, cv.ShapeMatchModes.I2, cv.ShapeMatchModes.I3)
                Exit For
            End If
        Next
    End Sub
End Class









Public Class Options_Plane : Inherits VB_Algorithm
    Public rmsThreshold As Single
    Public useMaskPoints As Boolean
    Public useContourPoints As Boolean
    Public use3Points As Boolean
    Public reuseRawDepthData As Boolean
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use all points in the rc mask")
            radio.addRadio("Use only points in the contour of the rc mask")
            radio.addRadio("Use 3 points in the contour of the rc mask")
            radio.addRadio("Don't replace the depth data with computed plane data")
            radio.check(1).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("RMS error threshold for flat X100", 0, 100, 10)
    End Sub
    Public Sub RunVB()
        Static rmsSlider = findSlider("RMS error threshold for flat X100")
        rmsThreshold = rmsSlider.Value / 100

        Static maskRadio = findRadio("Use all points in the rc mask")
        Static contourRadio = findRadio("Use only points in the contour of the rc mask")
        Static simpleRadio = findRadio("Use 3 points in the contour of the rc mask")
        useMaskPoints = maskRadio.checked
        useContourPoints = contourRadio.checked
        use3Points = simpleRadio.checked

        Static depthRadio = findRadio("Don't replace the depth data with computed plane data")
        reuseRawDepthData = depthRadio.checked
    End Sub

End Class







Public Class Options_Neighbors : Inherits VB_Algorithm
    Public threshold As Single
    Public pixels As Integer
    Public patchZ As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Difference from neighbor in mm's", 0, 20, 5)
            sliders.setupTrackBar("Minimum offset to neighbor pixel", 1, 100, 6)
            sliders.setupTrackBar("Patch z-values", 0, 1, 1)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Difference from neighbor in mm's")
        Static pixelSlider = findSlider("Minimum offset to neighbor pixel")
        Static patchSlider = findSlider("Patch z-values")
        threshold = thresholdSlider.value / 1000
        pixels = pixelSlider.value
        patchZ = patchSlider.value = 1
    End Sub
End Class







Public Class Options_Resize : Inherits VB_Algorithm
    Public warpFlag As cv.InterpolationFlags
    Public radioIndex As Integer
    Public resizePercent As Single
    Public topLeftOffset As Integer
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Area")
            radio.addRadio("Cubic flag (best blended)")
            radio.addRadio("Lanczos4")
            radio.addRadio("Linear")
            radio.addRadio("Nearest (preserves pixel values best)")
            radio.addRadio("WarpFillOutliers")
            radio.addRadio("WarpInverseMap")
            radio.check(3).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resize Percentage (%)", 1, 100, 3)
            sliders.setupTrackBar("Offset from top left corner", 1, 50, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static percentSlider = findSlider("Resize Percentage (%)")
        Static offsetSlider = findSlider("Offset from top left corner")
        resizePercent = percentSlider.Value / 100
        topLeftOffset = offsetSlider.Value
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                warpFlag = Choose(i + 1, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic, cv.InterpolationFlags.Lanczos4,
                                         cv.InterpolationFlags.Linear, cv.InterpolationFlags.Nearest,
                                         cv.InterpolationFlags.WarpFillOutliers, cv.InterpolationFlags.WarpInverseMap)
                radioIndex = i
                Exit For
            End If
        Next
    End Sub
End Class








Public Class Options_Smoothing : Inherits VB_Algorithm
    Public iterations As Integer
    Public interiorTension As Single
    Public stepSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Smoothing iterations", 1, 20, 8)
            sliders.setupTrackBar("Smoothing tension X100 (Interior Only)", 1, 100, 50)
            sliders.setupTrackBar("Step size when adding points (1 is identity)", 1, 500, 30)
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = findSlider("Smoothing iterations")
        Static tensionSlider = findSlider("Smoothing tension X100 (Interior Only)")
        Static stepSlider = findSlider("Step size when adding points (1 is identity)")
        iterations = iterSlider.Value
        interiorTension = tensionSlider.Value / 100
        stepSize = stepSlider.Value
    End Sub
End Class





Public Class Options_Structured : Inherits VB_Algorithm
    Public cushion As Integer
    Public stepSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Structured Depth slice thickness in pixels", 1, 10, 1)
            sliders.setupTrackBar("Slice step size in pixels (multi-slice option only)", 1, 100, 20)
        End If
    End Sub
    Public Sub RunVB()
        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        cushion = cushionSlider.Value
        stepSize = stepSlider.Value
    End Sub
End Class










Public Class Options_SuperRes : Inherits VB_Algorithm
    Public method As String = "farneback"
    Public iterations As Integer = 10
    Public restartWithNewOptions As Boolean
    Dim radioChoices = {"farneback", "tvl1", "brox", "pyrlk"}
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("farneback")
            radio.addRadio("tvl1")
            radio.addRadio("brox")
            radio.addRadio("pyrlk")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("SuperRes Iterations", 10, 200, 10)
    End Sub
    Public Sub RunVB()
        Static iterSlider = findSlider("SuperRes Iterations")
        Static frm = findfrm(traceName + " Radio Buttons")
        method = radioChoices(findRadioIndex(frm.check))
        Static lastMethod = method
        restartWithNewOptions = False
        If lastMethod <> method Or iterSlider.Value <> iterations Then restartWithNewOptions = True
        lastMethod = method
        iterations = iterSlider.Value
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM2 : Inherits VB_Algorithm
    Public SVMType = cv.ML.SVM.Types.CSvc
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("SVM Type = CSvc")
            radio.addRadio("SVM Type = EpsSvr")
            radio.addRadio("SVM Type = NuSvc")
            radio.addRadio("SVM Type = NuSvr")
            radio.addRadio("SVM Type = OneClass")

            radio.check(0).Checked = True
        End If
        labels(2) = "Options_SVM2 - only options, no output"
        desc = "SVM has many options - enough for 2 options classes."
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                SVMType = Choose(i + 1, cv.ML.SVM.Types.CSvc, cv.ML.SVM.Types.EpsSvr, cv.ML.SVM.Types.NuSvc, cv.ML.SVM.Types.NuSvr, cv.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next
        If standalone Then setTrueText(traceName + " has no output when run standalone." + vbCrLf + "It is used to setup more SVM options.")
    End Sub
End Class









' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM : Inherits VB_Algorithm
    Public kernelType = cv.ML.SVM.KernelTypes.Poly
    Public granularity As Integer
    Public svmDegree As Integer
    Public gamma As Integer
    Public svmCoef0 As Single
    Public svmC As Single
    Public svmNu As Single
    Public svmP As Single
    Public sampleCount As Integer
    Dim options2 As New Options_SVM2
    Dim radioChoices = {cv.ML.SVM.KernelTypes.Linear, cv.ML.SVM.KernelTypes.Poly, cv.ML.SVM.KernelTypes.Rbf, cv.ML.SVM.KernelTypes.Sigmoid}
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Granularity", 1, 50, 5)
            sliders.setupTrackBar("SVM Degree", 1, 100, 100)
            sliders.setupTrackBar("SVM Gamma", 1, 100, 100)
            sliders.setupTrackBar("SVM Coef0 X100", 1, 100, 100)
            sliders.setupTrackBar("SVM C X100", 0, 100, 100)
            sliders.setupTrackBar("SVM Nu X100", 1, 100, 50)
            sliders.setupTrackBar("SVM P X100", 0, 100, 0)
            sliders.setupTrackBar("SVM Sample Count", 2, 1000, 500)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("kernel Type = Linear")
            radio.addRadio("kernel Type = Poly (not working)")
            radio.addRadio("kernel Type = RBF")
            radio.addRadio("kernel Type = Sigmoid (not working)")

            radio.check(1).Enabled = False
            radio.check(3).Enabled = False
            radio.check(2).Checked = True
        End If
    End Sub
    Public Function createSVM() As cv.ML.SVM
        Dim svm = cv.ML.SVM.Create()
        svm.Type = options2.SVMType
        svm.KernelType = kernelType
        svm.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
        svm.Degree = svmDegree
        svm.Gamma = gamma
        svm.Coef0 = svmCoef0
        svm.C = svmC
        svm.Nu = svmNu
        svm.P = svmP
        Return svm
    End Function

    Public Sub RunVB()
        options2.RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        kernelType = radioChoices(findRadioIndex(frm.check))

        Static granSlider = findSlider("Granularity")
        Static degreeSlider = findSlider("SVM Degree")
        Static gammaSlider = findSlider("SVM Gamma")
        Static coef0Slider = findSlider("SVM Coef0 X100")
        Static svmCSlider = findSlider("SVM C X100")
        Static svmNuSlider = findSlider("SVM Nu X100")
        Static svmPSlider = findSlider("SVM P X100")
        Static sampleSlider = findSlider("SVM Sample Count")

        granularity = granSlider.Value
        svmDegree = degreeSlider.Value / 100
        gamma = gammaSlider.Value / 100
        svmCoef0 = coef0Slider.Value / 100
        svmC = svmCSlider.Value / 100
        svmNu = svmNuSlider.Value / 100
        svmP = svmPSlider.Value / 100
        sampleCount = sampleSlider.Value
    End Sub
End Class








Public Class Options_Threshold : Inherits VB_Algorithm
    Public thresholdOption As cv.ThresholdTypes
    Public maxVal As Integer
    Public threshold As Integer
    Public gradient As New Gradient_Color
    Public inputGray As Boolean
    Public otsuOption As Boolean
    Dim radioChoices = {cv.ThresholdTypes.Binary, cv.ThresholdTypes.BinaryInv, cv.ThresholdTypes.Tozero,
                        cv.ThresholdTypes.TozeroInv, cv.ThresholdTypes.Triangle, cv.ThresholdTypes.Trunc}
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold value", 0, 255, 100)
            sliders.setupTrackBar("MaxVal setting", 0, 255, 255)
        End If

        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("GrayScale Input")
            check.addCheckBox("Add OTSU Option - a 50/50 split")
        End If

        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Binary")
            radio.addRadio("Binary Inverse")
            radio.addRadio("ToZero")
            radio.addRadio("ToZero Inverse")
            radio.addRadio("Triangle - use maxVal slider to see impact")
            radio.addRadio("Trunc")
            radio.check(0).Checked = True
        End If

        gradient.Run(Nothing)
        dst2 = gradient.dst2
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        thresholdOption = radioChoices(findRadioIndex(frm.check))

        Static inputGrayCheck = findCheckBox("GrayScale Input")
        Static otsuCheck = findCheckBox("Add OTSU Option - a 50/50 split")
        Static threshSlider = findSlider("Threshold value")
        Static maxSlider = findSlider("MaxVal setting")

        inputGray = inputGrayCheck.checked
        otsuOption = otsuCheck.checked
        threshold = threshSlider.Value
        maxVal = maxSlider.Value
    End Sub
End Class






Public Class Options_WarpModel : Inherits VB_Algorithm
    Public useGradient As Boolean
    Public pkImage As cv.Mat
    Public warpMode As Integer
    Public useWarpAffine As Boolean
    Public useWarpHomography As Boolean
    Public options2 As New Options_WarpModel2
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("church.jpg")
            radio.addRadio("emir.jpg")
            radio.addRadio("Painting.jpg")
            radio.addRadio("railroad.jpg")
            radio.addRadio("river.jpg")
            radio.addRadio("Cliff.jpg")
            radio.addRadio("Column.jpg")
            radio.addRadio("General.jpg")
            radio.addRadio("Girls.jpg")
            radio.addRadio("Tablet.jpg")
            radio.addRadio("Valley.jpg")
            radio.check(8).Checked = True
        End If

        If check.Setup(traceName) Then check.addCheckBox("Use Gradient in WarpInput")
    End Sub
    Public Sub RunVB()
        Static gradientCheck = findCheckBox("Use Gradient in WarpInput")
        Static frm = findfrm(traceName + " Radio Buttons")

        If task.optionsChanged Then
            options2.RunVB()
            warpMode = options2.warpMode
            useWarpAffine = options2.useWarpAffine
            useWarpHomography = options2.useWarpHomography

            useGradient = gradientCheck.checked

            For i = 0 To frm.check.Count - 1
                Dim nextRadio = frm.check(i)
                If nextRadio.Checked Then
                    Dim photo As New FileInfo(task.homeDir + "Data\Prokudin\" + nextRadio.Text)
                    pkImage = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                    Exit For
                End If
            Next
        End If
    End Sub
End Class










Public Class Options_MinMaxNone : Inherits VB_Algorithm
    Public useMax As Boolean
    Public useMin As Boolean
    Public useNone As Boolean
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use farthest distance")
            radio.addRadio("Use closest distance")
            radio.addRadio("Use unchanged depth input")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm As OptionsRadioButtons = findfrm(traceName + " Radio Buttons")
        useMax = frm.check(0).Checked
        useMin = frm.check(1).Checked
        useNone = frm.check(2).Checked
    End Sub
End Class







Public Class Options_OpenGL : Inherits VB_Algorithm
    Public FOV As Single = 75
    Public yaw As Single = -3
    Public pitch As Single = 3
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 20.0
    Public pointSize As Integer = 2
    Public zTrans As Single = 0.5
    Public eye As New cv.Vec3f(0, 0, -40)
    Public scaleXYZ As New cv.Vec3f(10, 10, 1)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL yaw (degrees)", -180, 180, -3)
            sliders.setupTrackBar("OpenGL pitch (degrees)", -180, 180, 3)
            sliders.setupTrackBar("OpenGL roll (degrees)", -180, 180, 0)

            sliders.setupTrackBar("OpenGL Eye X X100", -180, 180, 0)
            sliders.setupTrackBar("OpenGL Eye Y X100", -180, 180, 0)
            sliders.setupTrackBar("OpenGL Eye Z X100", -180, 180, -40)

            sliders.setupTrackBar("OpenGL Scale X X10", 1, 100, 10)
            sliders.setupTrackBar("OpenGL Scale Y X10", 1, 100, 10)
            sliders.setupTrackBar("OpenGL Scale Z X10", 1, 100, 1)

            sliders.setupTrackBar("OpenGL zNear", 0, 100, 0)
            sliders.setupTrackBar("OpenGL zFar", -50, 200, 20)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, 2)
            sliders.setupTrackBar("zTrans (X100)", -1000, 1000, 50)

            sliders.setupTrackBar("OpenGL FOV", 1, 180, 75)
            If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then findSlider("OpenGL yaw (degrees)").Value = 135
        End If
    End Sub
    Public Sub RunVB()
        Static yawSlider = findSlider("OpenGL yaw (degrees)")
        Static pitchSlider = findSlider("OpenGL pitch (degrees)")
        Static rollSlider = findSlider("OpenGL roll (degrees)")
        Static eyeXSlider = findSlider("OpenGL Eye X X100")
        Static eyeYSlider = findSlider("OpenGL Eye Y X100")
        Static eyeZSlider = findSlider("OpenGL Eye Z X100")
        Static scaleXSlider = findSlider("OpenGL Scale X X10")
        Static scaleYSlider = findSlider("OpenGL Scale Y X10")
        Static scaleZSlider = findSlider("OpenGL Scale Z X10")
        Static zNearSlider = findSlider("OpenGL zNear")
        Static zFarSlider = findSlider("OpenGL zFar")
        Static zTransSlider = findSlider("zTrans (X100)")
        Static fovSlider = findSlider("OpenGL FOV")
        Static PointSizeSlider = findSlider("OpenGL Point Size")

        FOV = fovSlider.Value
        yaw = yawSlider.Value
        pitch = pitchSlider.Value
        roll = rollSlider.Value

        zNear = zNearSlider.Value
        zFar = zFarSlider.Value
        pointSize = PointSizeSlider.Value
        zTrans = zTransSlider.Value / 100

        eye = New cv.Vec3f(eyeXSlider.Value, eyeYSlider.Value, eyeZSlider.Value)
        scaleXYZ = New cv.Vec3f(scaleXSlider.Value, scaleYSlider.Value, scaleZSlider.Value)
    End Sub
End Class









Public Class Options_OpenGLFunctions : Inherits VB_Algorithm
    Public moveAmount As cv.Point3f
    Public FOV As Single = 75
    Public yaw As Single = -3
    Public pitch As Single = 3
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 20.0
    Public zTrans As Single = 0.5
    Public eye As New cv.Vec3f(0, 0, -40)
    Public scaleXYZ As New cv.Vec3f(10, 10, 1)
    Public PointSizeSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL shift left/right (X-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift up/down (Y-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift fwd/back (Z-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, 2)
        End If
        PointSizeSlider = findSlider("OpenGL Point Size")
    End Sub
    Public Sub RunVB()
        Static XmoveSlider = findSlider("OpenGL shift left/right (X-axis) X100")
        Static YmoveSlider = findSlider("OpenGL shift up/down (Y-axis) X100")
        Static ZmoveSlider = findSlider("OpenGL shift fwd/back (Z-axis) X100")

        moveAmount = New cv.Point3f(XmoveSlider.Value / 100, YmoveSlider.Value / 100, ZmoveSlider.Value / 100)
    End Sub
End Class










Public Class Options_MinArea : Inherits VB_Algorithm
    Public srcPoints As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area Number of Points", 1, 30, 5)
            sliders.setupTrackBar("Area size", 10, 300, 200)
        End If
    End Sub
    Public Sub RunVB()
        Static numSlider = findSlider("Area Number of Points")
        Static sizeSlider = findSlider("Area size")
        Dim squareWidth = sizeSlider.Value / 2
        srcPoints.Clear()

        Dim pt As cv.Point2f
        Dim num = numSlider.Value
        For i = 0 To num - 1
            pt.X = msRNG.Next(dst2.Width / 2 - squareWidth, dst2.Width / 2 + squareWidth)
            pt.Y = msRNG.Next(dst2.Height / 2 - squareWidth, dst2.Height / 2 + squareWidth)
            srcPoints.Add(pt)
        Next
    End Sub
End Class








Public Class Options_DCT : Inherits VB_Algorithm
    Public dctFlag As cv.DctFlags
    Public runLengthMin As Integer
    Public removeFrequency As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Remove Frequencies < x", 0, 100, 1)
            sliders.setupTrackBar("Run Length Minimum", 1, 100, 15)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("DCT Flags None")
            radio.addRadio("DCT Flags Row")
            radio.addRadio("DCT Flags Inverse")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static removeSlider = findSlider("Remove Frequencies < x")
        Static runLenSlider = findSlider("Run Length Minimum")

        runLengthMin = runLenSlider.Value
        removeFrequency = removeSlider.Value
        For i = 0 To 2
            If radio.check(i).Checked Then
                dctFlag = Choose(i + 1, cv.DctFlags.None, cv.DctFlags.Rows, cv.DctFlags.Inverse)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Eigen : Inherits VB_Algorithm
    Public highlight As Boolean
    Public recompute As Boolean
    Public randomCount As Integer
    Public linePointCount As Integer
    Public noiseOffset As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Random point count", 0, 500, 100)
            sliders.setupTrackBar("Line Point Count", 0, 500, 20)
            sliders.setupTrackBar("Line Noise", 1, 100, 10)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Highlight Line Data")
            check.addCheckBox("Recompute with new random data")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static recomputeBox = findCheckBox("Recompute with new random data")
        Static highlightBox = findCheckBox("Highlight Line Data")
        Static randomSlider = findSlider("Random point count")
        Static linePointSlider = findSlider("Random point count")
        Static noiseSlider = findSlider("Line Noise")
        randomCount = randomSlider.Value
        linePointCount = linePointSlider.Value
        highlight = highlightBox.checked
        recompute = recomputeBox.checked
        noiseOffset = noiseSlider.Value
    End Sub
End Class







Public Class Options_FitLine : Inherits VB_Algorithm
    Public radiusAccuracy As Integer
    Public angleAccuracy As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Accuracy for the radius X100", 0, 100, 10)
            sliders.setupTrackBar("Accuracy for the angle X100", 0, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static radiusSlider = findSlider("Accuracy for the radius X100")
        Static angleSlider = findSlider("Accuracy for the angle X100")
        radiusAccuracy = radiusSlider.Value
        angleAccuracy = angleSlider.Value
    End Sub
End Class







Public Class Options_Fractal : Inherits VB_Algorithm
    Public iterations As Integer
    Public resetCheck As Windows.Forms.CheckBox
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Mandelbrot iterations", 1, 50, 34)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Reset to original Mandelbrot")
        End If
        resetCheck = findCheckBox("Reset to original Mandelbrot")
    End Sub
    Public Sub RunVB()
        Static iterSlider = findSlider("Mandelbrot iterations")
        iterations = iterSlider.Value
    End Sub
End Class







Public Class Options_ProCon : Inherits VB_Algorithm
    Public buffer(10 - 1) As Integer
    Public pduration As Integer
    Public cduration As Integer
    Public bufferSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Buffer Size", 1, 100, buffer.Length)
            sliders.setupTrackBar("Producer Workload Duration (ms)", 1, 1000, 1)
            sliders.setupTrackBar("Consumer Workload Duration (ms)", 1, 1000, 1)
        End If
        buffer = Enumerable.Repeat(-1, buffer.Length).ToArray
    End Sub
    Public Sub RunVB()
        Static sizeSlider = findSlider("Buffer Size")
        Static proSlider = findSlider("Producer Workload Duration (ms)")
        Static conSlider = findSlider("Consumer Workload Duration (ms)")
        If task.optionsChanged Then
            bufferSize = sizeSlider.Value
            pduration = proSlider.Value
            cduration = conSlider.Value
        End If
    End Sub
End Class






Public Class Options_OilPaint : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public intensity As Integer
    Public threshold As Integer
    Public filterSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 2, 10, 4)
            sliders.setupTrackBar("Intensity", 1, 250, 20)
            sliders.setupTrackBar("Filter Size", 3, 15, 3)
            sliders.setupTrackBar("Threshold", 0, 200, 25)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Kernel Size")
        Static intensitySlider = findSlider("Intensity")
        Static thresholdSlider = findSlider("Threshold")
        Static filterSlider = findSlider("Filter Size")
        kernelSize = kernelSlider.Value Or 1

        intensity = intensitySlider.Value
        threshold = thresholdSlider.Value

        filterSize = filterSlider.Value
    End Sub
End Class










Public Class Options_Pointilism : Inherits VB_Algorithm
    Public smoothingRadius As Integer
    Public strokeSize As Integer
    Public useElliptical As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Stroke Scale", 1, 5, 3)
            sliders.setupTrackBar("Smoothing Radius", 0, 100, 32)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("Use Elliptical stroke")
            radio.addRadio("Use Circular stroke")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static radiusSlider = findSlider("Smoothing Radius")
        Static strokeSlider = findSlider("Stroke Scale")
        Static ellipStroke = findRadio("Use Elliptical stroke")
        smoothingRadius = radiusSlider.Value * 2 + 1
        strokeSize = strokeSlider.Value
        useElliptical = ellipStroke.checked
    End Sub
End Class







Public Class Options_MotionBlur : Inherits VB_Algorithm
    Public showDirection As Boolean = True
    Public redoCheckBox As Windows.Forms.CheckBox
    Public kernelSize As Integer
    Public theta As Single
    Public restoreLen As Integer
    Public SNR As Integer
    Public gamma As Integer
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Redo motion blurred image")
            check.Box(0).Checked = True
        End If
        redoCheckBox = findCheckBox("Redo motion blurred image")

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Deblur Restore Vector", 1, 10, 10)
            sliders.setupTrackBar("Deblur Angle of Restore Vector", -90, 90, 0)
            sliders.setupTrackBar("Motion Blur Angle", -90, 90, 0)
            sliders.setupTrackBar("Motion Blur Length", 1, 101, 51)
            sliders.setupTrackBar("Deblur Signal to Noise Ratio", 1, 1000, 700)
            sliders.setupTrackBar("Deblur Gamma", 1, 100, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static deblurSlider = findSlider("Deblur Restore Vector")
        Static angleSlider = findSlider("Deblur Angle of Restore Vector")
        Static blurSlider = findSlider("Motion Blur Length")
        Static blurAngleSlider = findSlider("Motion Blur Angle")
        Static SNRSlider = findSlider("Deblur Signal to Noise Ratio")
        Static gammaSlider = findSlider("Deblur Gamma")
        If redoCheckBox.Checked Then
            deblurSlider.Value = msRNG.Next(deblurSlider.Minimum, deblurSlider.Maximum)
            angleSlider.Value = msRNG.Next(angleSlider.Minimum, angleSlider.Maximum)
        End If
        kernelSize = blurSlider.Value
        theta = angleSlider.Value / (180 / Math.PI)
        restoreLen = deblurSlider.Value

        SNR = CDbl(SNRSlider.Value)
        gamma = CDbl(gammaSlider.Value)

    End Sub
End Class







Public Class Options_BGSubtractSynthetic : Inherits VB_Algorithm
    Public amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Synthetic Amplitude x100", 1, 400, 200)
            sliders.setupTrackBar("Synthetic Magnitude", 1, 40, 20)
            sliders.setupTrackBar("Synthetic Wavespeed x100", 1, 400, 20)
            sliders.setupTrackBar("Synthetic ObjectSpeed", 1, 20, 15)
        End If
    End Sub
    Public Sub RunVB()
        Static amplitudeSlider = findSlider("Synthetic Amplitude x100")
        Static MagSlider = findSlider("Synthetic Magnitude")
        Static speedSlider = findSlider("Synthetic Wavespeed x100")
        Static objectSlider = findSlider("Synthetic ObjectSpeed")

        amplitude = amplitudeSlider.Value
        magnitude = MagSlider.Value
        waveSpeed = speedSlider.Value
        objectSpeed = objectSlider.Value
    End Sub
End Class





Public Class Options_BinarizeNiBlack : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public niBlackK As Single
    Public nickK As Single
    Public sauvolaK As Single
    Public sauvolaR As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 3, 500, 51)
            sliders.setupTrackBar("Niblack k", -1000, 1000, -200)
            sliders.setupTrackBar("Nick k", -1000, 1000, 100)
            sliders.setupTrackBar("Sauvola k", -1000, 1000, 100)
            sliders.setupTrackBar("Sauvola r", 1, 100, 64)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Kernel Size")
        Static NiBlackSlider = findSlider("Niblack k")
        Static kSlider = findSlider("Nick k")
        Static skSlider = findSlider("Sauvola k")
        Static rSlider = findSlider("Sauvola r")
        kernelSize = kernelSlider.Value Or 1
        niBlackK = NiBlackSlider.Value / 1000
        nickK = kSlider.Value / 1000
        sauvolaK = skSlider.Value / 1000
        sauvolaR = rSlider.Value
    End Sub
End Class






Public Class Options_Bernson : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public bgThreshold As Integer
    Public contrastMin As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 3, 500, 51)
            sliders.setupTrackBar("Contrast min", 0, 255, 50)
            sliders.setupTrackBar("bg Threshold", 0, 255, 100)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Kernel Size")
        Static contrastSlider = findSlider("Contrast min")
        Static bgSlider = findSlider("bg Threshold")
        kernelSize = kernelSlider.Value Or 1

        bgThreshold = bgSlider.Value
        contrastMin = contrastSlider.Value
    End Sub
End Class







Public Class Options_BlockMatching : Inherits VB_Algorithm
    Public numDisparity As Integer
    Public blockSize As Integer
    Public distance As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blockmatch max disparity", 2, 5, 2)
            sliders.setupTrackBar("Blockmatch block size", 5, 255, 15)
            sliders.setupTrackBar("Blockmatch distance in meters", 1, 100, 20)
        End If
    End Sub
    Public Sub RunVB()
        Static matchSlider = findSlider("Blockmatch max disparity")
        Static sizeSlider = findSlider("Blockmatch block size")
        Static distSlider = findSlider("Blockmatch distance in meters")
        numDisparity = matchSlider.Value * 16 ' must be a multiple of 16
        blockSize = sizeSlider.Value Or 1
        distance = distSlider.Value
    End Sub
End Class






Public Class Options_Cartoonify : Inherits VB_Algorithm
    Public medianBlur As Integer
    Public medianBlur2 As Integer
    Public kernelSize As Integer
    Public threshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Cartoon Median Blur kernel", 1, 21, 7)
            sliders.setupTrackBar("Cartoon Median Blur kernel 2", 1, 21, 3)
            sliders.setupTrackBar("Cartoon threshold", 1, 255, 80)
            sliders.setupTrackBar("Cartoon Laplacian kernel", 1, 21, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Cartoon Median Blur kernel")
        Static kernel2Slider = findSlider("Cartoon Median Blur kernel 2")
        Static thresholdSlider = findSlider("Cartoon threshold")
        Static laplaceSlider = findSlider("Cartoon Laplacian kernel")
        medianBlur = kernelSlider.Value Or 1
        medianBlur2 = kernel2Slider.Value Or 1
        kernelSize = laplaceSlider.Value Or 1
        threshold = thresholdSlider.Value
    End Sub
End Class





Public Class Options_DFT : Inherits VB_Algorithm
    Public radius As Integer
    Public order As Integer
    Public butterworthFilter(1) As cv.Mat
    Public dftFlag As cv.DctFlags
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DFT B Filter - Radius", 1, dst2.Rows, dst2.Rows)
            sliders.setupTrackBar("DFT B Filter - Order", 1, dst2.Rows, 2)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("DFT Flags ComplexOutput")
            radio.addRadio("DFT Flags Inverse")
            radio.addRadio("DFT Flags None")
            radio.addRadio("DFT Flags RealOutput")
            radio.addRadio("DFT Flags Rows")
            radio.addRadio("DFT Flags Scale")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static radiusSlider = findSlider("DFT B Filter - Radius")
        Static orderSlider = findSlider("DFT B Filter - Order")
        radius = radiusSlider.Value
        order = orderSlider.Value

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                dftFlag = Choose(i + 1, cv.DftFlags.ComplexOutput, cv.DftFlags.Inverse, cv.DftFlags.None,
                                        cv.DftFlags.RealOutput, cv.DftFlags.Rows, cv.DftFlags.Scale)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_Dither : Inherits VB_Algorithm
    Public radioIndex As Integer
    Public bppIndex As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Bits per color plane (Nbpp only)", 1, 5, 1)
        End If
        If radio.Setup(traceName) Then
            Dim radioChoices = {"Bayer16", "Bayer8", "Bayer4", "Bayer3", "Bayer2", "BayerRgbNbpp", "BayerRgb3bpp", "BayerRgb6bpp",
                                           "BayerRgb9bpp", "BayerRgb12bpp", "BayerRgb15bpp", "BayerRgb18bpp", "FSRgbNbpp", "Floyd-Steinberg",
                                           "FSRgb3bpp", "FSRgb6bpp", "FSRgb9bpp", "FSRgb12bpp", "FSRgb15bpp", "FSRgb18bpp",
                                           "SierraLiteRgbNbpp", "SierraLite", "SierraRgbNbpp", "Sierra"}
            For i = 0 To radioChoices.Count - 1
                radio.addRadio(radioChoices(i))
            Next
            radio.check(4).Checked = True ' this one was interesting...
        End If
    End Sub
    Public Sub RunVB()
        Static bppSlider = findSlider("Bits per color plane (Nbpp only)")
        bppIndex = bppSlider.Value

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                radioIndex = i
                Exit For
            End If
        Next
        Select Case radioIndex
            Case 5, 12, 20, 22
                bppSlider.Enabled = True
            Case Else
                bppSlider.Enabled = False
        End Select
    End Sub
End Class







Public Class Options_SymmetricalShapes : Inherits VB_Algorithm
    Public rotateAngle As Single = 0
    Public fillColor = cv.Scalar.Red
    Public numPoints As Integer
    Public nGenPer As Integer
    Public radius1 As Integer
    Public radius2 As Integer
    Public dTheta As Single
    Public symmetricRipple As Boolean
    Public reverseInOut As Boolean
    Public fillRequest As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sample Size", 200, 1000, 500)
            sliders.setupTrackBar("Radius 1", 1, dst2.Rows / 2, dst2.Rows / 4)
            sliders.setupTrackBar("Radius 2", 1, dst2.Rows / 2, dst2.Rows / 8)
            sliders.setupTrackBar("nGenPer", 1, 500, 100)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Symmetric Ripple")
            check.addCheckBox("Only Regular Shapes")
            check.addCheckBox("Filled Shapes")
            check.addCheckBox("Reverse In/Out")
            check.addCheckBox("Use demo mode")
            check.Box(4).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static countSlider = findSlider("Sample Size")
        Static r1Slider = findSlider("Radius 1")
        Static r2Slider = findSlider("Radius 2")
        Static nGenPerSlider = findSlider("nGenPer")
        Static symCheck = findCheckBox("Symmetric Ripple")
        Static fillCheck = findCheckBox("Filled Shapes")
        Static regularCheck = findCheckBox("Only Regular Shapes")
        Static reverseCheck = findCheckBox("Reverse In/Out")
        Static demoCheck = findCheckBox("Use demo mode")

        If demoCheck.Checked Then
            If task.frameCount Mod 30 = 0 Then
                If countSlider.Value < countSlider.Maximum - 17 Then countSlider.Value += 17 Else countSlider.Value = countSlider.Minimum
                If r1Slider.Value < r1Slider.Maximum - 10 Then r1Slider.Value += 10 Else r1Slider.Value = 1
                If r2Slider.Value > 13 Then r2Slider.Value -= 13 Else r2Slider.Value = r2Slider.Maximum
                If nGenPerSlider.Value > 27 Then nGenPerSlider.Value -= 27 Else nGenPerSlider.Value = nGenPerSlider.Maximum
                fillColor = task.scalarColors(task.frameCount Mod 256)
            End If
            If task.frameCount Mod 37 = 0 Then symCheck.Checked = Not symCheck.Checked
            If task.frameCount Mod 222 = 0 Then fillCheck.Checked = Not fillCheck.Checked
            If task.frameCount Mod 77 = 0 Then regularCheck.Checked = Not regularCheck.Checked
            If task.frameCount Mod 100 = 0 Then reverseCheck.Checked = Not reverseCheck.Checked
            rotateAngle += 1
        End If

        numPoints = countSlider.Value
        nGenPer = nGenPerSlider.Value
        If regularCheck.Checked Then numPoints = CInt(numPoints / nGenPer) * nGenPer ' harmonize
        radius1 = r1Slider.Value
        radius2 = r2Slider.Value
        dTheta = 2 * cv.Cv2.PI / numPoints
        symmetricRipple = symCheck.Checked
        reverseInOut = reverseCheck.Checked
        fillRequest = fillCheck.checked
    End Sub
End Class






Public Class Options_DrawArc : Inherits VB_Algorithm
    Public saveMargin As Integer
    Public drawFull As Boolean
    Public drawFill As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Clearance from image edge (margin size)", 5, dst2.Width / 8, dst2.Width / 16)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Draw Full Ellipse")
            radio.addRadio("Draw Filled Arc")
            radio.addRadio("Draw Arc")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static marginSlider = findSlider("Clearance from image edge (margin size)")
        Static fillCheck = findRadio("Draw Filled Arc")
        Static fullCheck = findRadio("Draw Full Ellipse")
        saveMargin = marginSlider.Value
        drawFull = fullCheck.checked
        drawFill = fillCheck.checked
    End Sub
End Class






Public Class Options_FilterNorm : Inherits VB_Algorithm
    Public kernel As cv.Mat
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("INF")
            radio.addRadio("L1")
            radio.check(1).Checked = True
            radio.addRadio("L2")
            radio.addRadio("MinMax")
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Normalize alpha X10", 1, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static alphaSlider = findSlider("Normalize alpha X10")

        Dim normType = cv.NormTypes.L1
        kernel = New cv.Mat(1, 21, cv.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                normType = Choose(i + 1, cv.NormTypes.INF, cv.NormTypes.L1, cv.NormTypes.L2, cv.NormTypes.MinMax)
                Exit For
            End If
        Next

        kernel = kernel.Normalize(alphaSlider.Value / 10, 0, normType)
    End Sub
End Class





Public Class Options_SepFilter2D : Inherits VB_Algorithm
    Public xDim As Integer
    Public yDim As Integer
    Public sigma As Single
    Public diffCheck As Boolean
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Show Difference SepFilter2D and Gaussian")
            check.Box(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel X size", 1, 21, 5)
            sliders.setupTrackBar("Kernel Y size", 1, 21, 11)
            sliders.setupTrackBar("SepFilter2D Sigma X10", 0, 100, 17)
        End If
    End Sub
    Public Sub RunVB()
        Static xSlider = findSlider("Kernel X size")
        Static ySlider = findSlider("Kernel Y size")
        Static sigmaSlider = findSlider("SepFilter2D Sigma X10")
        Static diffCheckBox = findCheckBox("Show Difference SepFilter2D and Gaussian")
        xDim = xSlider.Value Or 1
        yDim = ySlider.Value Or 1
        sigma = sigmaSlider.Value / 10
        diffCheck = diffCheckBox.checked
    End Sub
End Class






Public Class Options_IMUFrameTime : Inherits VB_Algorithm
    Public minDelayIMU As Integer
    Public minDelayHost As Integer
    Public plotLastX As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum Host interrupt delay (ms)", 1, 10, 4)
            sliders.setupTrackBar("Minimum IMU to Capture time (ms)", 1, 10, 4)
            sliders.setupTrackBar("Number of Plot Values", 5, 30, 20)
        End If
    End Sub
    Public Sub RunVB()
        Static minSliderHost = findSlider("Minimum Host interrupt delay (ms)")
        Static minSliderIMU = findSlider("Minimum IMU to Capture time (ms)")
        Static plotSlider = findSlider("Number of Plot Values")
        minDelayIMU = minSliderIMU.Value
        minDelayHost = minSliderHost.Value
        plotLastX = plotSlider.Value
    End Sub
End Class





Public Class Options_Kalman_VB : Inherits VB_Algorithm
    Public kalmanInput As Single
    Public matrix As New List(Of Single)
    Public noisyInput As Integer
    Dim oRand As System.Random
    Public angle As Single
    Public Sub New()
        oRand = New System.Random(DateTime.Now.Millisecond)
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Move this to see results", 0, 1000, 500)
            sliders.setupTrackBar("Input with Noise", 0, 1000, 500)
            sliders.setupTrackBar("20 point average of output", 0, 1000, 500)
            sliders.setupTrackBar("Kalman Output", 0, 1000, 500)
            sliders.setupTrackBar("20 Point average difference", 0, 1000, 500)
            sliders.setupTrackBar("Kalman difference", 0, 1000, 500)
            sliders.setupTrackBar("Simulated Noise", 0, 100, 25)
            sliders.setupTrackBar("Simulated Bias", -100, 100, 0)
            sliders.setupTrackBar("Simulated Scale", 0, 100, 0)
        End If
    End Sub
    Public Sub RunVB()
        Static inputSlider = findSlider("Move this to see results")
        Static noisyInputSlider = findSlider("Input with Noise")
        Static pointSlider = findSlider("20 point average of output")
        Static avgSlider = findSlider("20 Point average difference")
        Static noiseSlider = findSlider("Simulated Noise")
        Static biasSlider = findSlider("Simulated Bias")
        Static scaleSlider = findSlider("Simulated Scale")
        Static outputSlider = findSlider("Kalman Output")
        Static kDiffSlider = findSlider("Kalman difference")

        kalmanInput = inputSlider.Value

        Dim scalefactor As Single = (scaleSlider.Value / 100) + 1 'This should be between 1 and 2
        Dim iRand = oRand.Next(0, noiseSlider.Value)
        noisyInput = CInt((kalmanInput * scalefactor) + biasSlider.Value + iRand - (noiseSlider.Value / 2))

        If noisyInput < 0 Then noisyInput = 0
        If noisyInput > noisyInputSlider.Maximum Then noisyInput = noisyInputSlider.Maximum
        noisyInputSlider.Value = noisyInput

        If matrix.Count > 0 Then
            Const MAX_INPUT = 20
            matrix(task.frameCount Mod MAX_INPUT) = kalmanInput
            Dim AverageOutput = (New cv.Mat(MAX_INPUT, 1, cv.MatType.CV_32F, matrix.ToArray)).Mean()(0)

            If AverageOutput < 0 Then AverageOutput = 0
            If AverageOutput > pointSlider.Maximum Then AverageOutput = pointSlider.Maximum
            pointSlider.Value = CInt(AverageOutput)

            Dim AverageDiff = CInt(Math.Abs(AverageOutput - kalmanInput) * 10)
            If AverageDiff > avgSlider.Maximum Then AverageDiff = avgSlider.Maximum
            avgSlider.Value = AverageDiff

            Dim KalmanOutput As Single = angle

            If KalmanOutput < 0 Then KalmanOutput = 0
            If KalmanOutput > outputSlider.Maximum Then KalmanOutput = outputSlider.Maximum
            outputSlider.Value = CInt(KalmanOutput)

            Dim KalmanDiff = CInt(Math.Abs(KalmanOutput - kalmanInput) * 10)
            If KalmanDiff > kDiffSlider.Maximum Then KalmanDiff = kDiffSlider.Maximum
            kDiffSlider.Value = KalmanDiff
        End If
    End Sub
End Class






Public Class Options_KLT : Inherits VB_Algorithm
    Public inputPoints() As cv.Point2f
    Public maxCorners As Integer
    Public qualityLevel As Single
    Public minDistance As Integer
    Public blockSize As Integer
    Public nightMode As Boolean
    Public subPixWinSize As New cv.Size(10, 10)
    Public winSize As New cv.Size(3, 3)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KLT - MaxCorners", 1, 200, 100)
            sliders.setupTrackBar("KLT - qualityLevel", 1, 100, 1) ' low quality!  We want lots of points.
            sliders.setupTrackBar("KLT - minDistance", 1, 100, 7)
            sliders.setupTrackBar("KLT - BlockSize", 1, 100, 7)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("KLT - Night Mode")
            check.addCheckBox("KLT - delete all Points")
        End If
    End Sub
    Public Sub RunVB()
        Static maxSlider = findSlider("KLT - MaxCorners")
        Static qualitySlider = findSlider("KLT - qualityLevel")
        Static minSlider = findSlider("KLT - minDistance")
        Static blockSlider = findSlider("KLT - BlockSize")
        Static nightCheck = findCheckBox("KLT - Night Mode")
        Static deleteCheck = findCheckBox("KLT - delete all Points")

        If deleteCheck.Checked Or heartBeat() Then
            inputPoints = Nothing ' just delete all points and start again.
            deleteCheck.Checked = False
        End If

        maxCorners = maxSlider.Value
        qualityLevel = qualitySlider.Value / 100
        minDistance = minSlider.Value
        blockSize = blockSlider.Value
        nightMode = nightCheck.Checked
    End Sub
End Class







Public Class Options_Laplacian : Inherits VB_Algorithm
    Public kernel As cv.Size
    Public scale As Single
    Public delta As Single
    Public gaussianBlur As Boolean
    Public boxFilterBlur As Boolean
    Public threshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Laplacian Kernel size", 1, 21, 3)
            sliders.setupTrackBar("Laplacian Scale", 0, 100, 100)
            sliders.setupTrackBar("Laplacian Delta", 0, 1000, 0)
            sliders.setupTrackBar("LaPlacian Threshold", 0, 100, 15)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Add Gaussian Blur")
            radio.addRadio("Add boxfilter Blur")
            radio.addRadio("Add median Blur")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Laplacian Kernel size")
        Static scaleSlider = findSlider("Laplacian Scale")
        Static deltaSlider = findSlider("Laplacian Delta")
        Static thresholdSlider = findSlider("LaPlacian Threshold")
        Static blurCheck = findRadio("Add Gaussian Blur")
        Static boxCheck = findRadio("Add boxfilter Blur")
        Dim kernelSize As Integer = kernelSlider.Value Or 1
        scale = scaleSlider.Value / 100
        delta = deltaSlider.Value / 100
        kernel = New cv.Size(kernelSize, kernelSize)
        gaussianBlur = blurCheck.checked
        boxFilterBlur = boxCheck.checked
        threshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_Colors : Inherits VB_Algorithm
    Public red As Integer
    Public green As Integer
    Public blue As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.Setup(traceName)
            sliders.setupTrackBar("Red", 0, 255, CInt(msRNG.Next(0, 255)))
            sliders.setupTrackBar("Green", 0, 255, CInt(msRNG.Next(0, 255)))
            sliders.setupTrackBar("Blue", 0, 255, CInt(msRNG.Next(0, 255)))
        End If
    End Sub
    Public Sub RunVB()
        Static redSlider = findSlider("Red")
        Static greenSlider = findSlider("Green")
        Static blueSlider = findSlider("Blue")
        red = redSlider.Value
        green = greenSlider.Value
        blue = blueSlider.Value
    End Sub
End Class







Public Class Options_OpticalFlow : Inherits VB_Algorithm
    Public pyrScale As Single
    Public levels As Integer
    Public winSize As Integer
    Public iterations As Integer
    Public polyN As Single
    Public polySigma As Single
    Public OpticalFlowFlags As cv.OpticalFlowFlags
    Public outputScaling As Integer
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("FarnebackGaussian")
            radio.addRadio("LkGetMinEigenvals")
            radio.addRadio("None")
            radio.addRadio("PyrAReady")
            radio.addRadio("PyrBReady")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Optical Flow pyrScale", 1, 100, 35)
            sliders.setupTrackBar("Optical Flow Levels", 1, 10, 1)
            sliders.setupTrackBar("Optical Flow winSize", 1, 9, 1)
            sliders.setupTrackBar("Optical Flow Iterations", 1, 10, 1)
            sliders.setupTrackBar("Optical Flow PolyN", 1, 15, 5)
            sliders.setupTrackBar("Optical Flow Scaling Output", 1, 100, 50)
        End If
    End Sub
    Public Sub RunVB()
        Static scaleSlider = findSlider("Optical Flow pyrScale")
        Static levelSlider = findSlider("Optical Flow Levels")
        Static sizeSlider = findSlider("Optical Flow winSize")
        Static iterSlider = findSlider("Optical Flow Iterations")
        Static polySlider = findSlider("Optical Flow PolyN")
        Static flowSlider = findSlider("Optical Flow Scaling Out")

        pyrScale = scaleSlider.Value / scaleSlider.Maximum
        levels = levelSlider.Value
        winSize = sizeSlider.Value Or 1
        iterations = iterSlider.Value
        polyN = polySlider.Value Or 1
        polySigma = 1.5
        If polyN <= 5 Then polySigma = 1.1

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                OpticalFlowFlags = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                 cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
        outputScaling = flowSlider.Value
    End Sub
End Class






Public Class Options_OpticalFlowSparse : Inherits VB_Algorithm
    Public OpticalFlowFlag As cv.OpticalFlowFlags
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("FarnebackGaussian")
            radio.addRadio("LkGetMinEigenvals")
            radio.addRadio("None")
            radio.addRadio("PyrAReady")
            radio.addRadio("PyrBReady")
            radio.addRadio("UseInitialFlow")
            radio.check(5).Enabled = False
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                OpticalFlowFlag = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Quaternion : Inherits VB_Algorithm
    Public q1 As Quaternion
    Public q2 As Quaternion
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("quaternion A.x X100", -100, 100, -50)
            sliders.setupTrackBar("quaternion A.y X100", -100, 100, 10)
            sliders.setupTrackBar("quaternion A.z X100", -100, 100, 20)
            sliders.setupTrackBar("quaternion A Theta X100", -100, 100, 100)

            sliders.setupTrackBar("quaternion B.x X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B.y X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B.z X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B Theta X100", -100, 100, 100)
        End If
    End Sub
    Public Sub RunVB()
        Static axSlider = findSlider("quaternion A.x X100")
        Static aySlider = findSlider("quaternion A.y X100")
        Static azSlider = findSlider("quaternion A.z X100")
        Static athetaSlider = findSlider("quaternion A Theta X100")

        Static bxSlider = findSlider("quaternion B.x X100")
        Static bySlider = findSlider("quaternion B.y X100")
        Static bzSlider = findSlider("quaternion B.z X100")
        Static bthetaSlider = findSlider("quaternion B Theta X100")

        q1 = New Quaternion(CSng(axSlider.Value / 100), CSng(aySlider.Value / 100),
                                CSng(azSlider.Value / 100), CSng(athetaSlider.Value / 100))
        q2 = New Quaternion(CSng(bxSlider.Value / 100), CSng(bySlider.Value / 100),
                                    CSng(bzSlider.Value / 100), CSng(bthetaSlider.Value / 100))
    End Sub
End Class






Public Class Options_XPhoto : Inherits VB_Algorithm
    Public colorCode As Integer = cv.ColorConversionCodes.BGR2GRAY
    Public dynamicRatio As Integer
    Public blockSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("XPhoto Dynamic Ratio", 1, 127, 7)
            sliders.setupTrackBar("XPhoto Block Size", 1, 100, 3)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("BGR2GRAY")
            radio.addRadio("BGR2HSV")
            radio.addRadio("BGR2YUV")
            radio.addRadio("BGR2XYZ")
            radio.addRadio("BGR2Lab")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static ratioSlider = findSlider("XPhoto Dynamic Ratio")
        Static sizeSlider = findSlider("XPhoto Block Size")
        dynamicRatio = ratioSlider.Value
        blockSize = sizeSlider.Value
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                colorCode = Choose(i + 1, cv.ColorConversionCodes.BGR2GRAY, cv.ColorConversionCodes.BGR2HSV, cv.ColorConversionCodes.BGR2YUV,
                                          cv.ColorConversionCodes.BGR2XYZ, cv.ColorConversionCodes.BGR2Lab)
                Exit For
            End If
        Next
    End Sub
End Class







Public Class Options_InPaint : Inherits VB_Algorithm
    Public telea As Boolean
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("TELEA")
            radio.addRadio("Navier-Stokes")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static teleaRadio = findRadio("TELEA")
        telea = teleaRadio.checked
    End Sub
End Class










Public Class Options_RotatePoly : Inherits VB_Algorithm
    Public changeCheck As Windows.Forms.CheckBox
    Public angleSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Amount to rotate triangle", -180, 180, 10)

        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Change center of rotation and triangle")
        End If

        angleSlider = findSlider("Amount to rotate triangle")
        changeCheck = findCheckBox("Change center of rotation and triangle")
    End Sub
    Public Sub RunVB()
    End Sub
End Class












Public Class Options_FPoly : Inherits VB_Algorithm
    Public removeThreshold As Integer
    Public autoResyncAfterX As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resync if feature moves > X pixels", 1, 20, 4)
            sliders.setupTrackBar("Points to use in Feature Poly", 3, 20, 10)
            sliders.setupTrackBar("Automatically resync after X frames", 10, 1000, 500)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Resync if feature moves > X pixels")
        Static pointSlider = findSlider("Points to use in Feature Poly")
        Static resyncSlider = findSlider("Automatically resync after X frames")
        removeThreshold = thresholdSlider.Value
        task.polyCount = pointSlider.Value
        autoResyncAfterX = resyncSlider.Value
    End Sub
End Class













Public Class Options_Homography : Inherits VB_Algorithm
    Public hMethod = cv.HomographyMethods.None
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("None")
            radio.addRadio("LMedS")
            radio.addRadio("Ransac")
            radio.addRadio("Rho")
            radio.addRadio("USAC_DEFAULT")
            radio.addRadio("USAC_PARALLEL")
            radio.addRadio("USAC_FM_8PTS")
            radio.addRadio("USAC_FAST")
            radio.addRadio("USAC_ACCURATE")
            radio.addRadio("USAC_PROSAC")
            radio.addRadio("USAC_MAGSAC")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                hMethod = Choose(i + 1, cv.HomographyMethods.None, cv.HomographyMethods.LMedS, cv.HomographyMethods.Ransac,
                                 cv.HomographyMethods.Rho, cv.HomographyMethods.USAC_DEFAULT,
                                 cv.HomographyMethods.USAC_PARALLEL, cv.HomographyMethods.USAC_FM_8PTS,
                                 cv.HomographyMethods.USAC_FAST, cv.HomographyMethods.USAC_ACCURATE,
                                 cv.HomographyMethods.USAC_PROSAC, cv.HomographyMethods.USAC_MAGSAC)
                Exit For
            End If
        Next
    End Sub
End Class












Public Class Options_Random : Inherits VB_Algorithm
    Public countSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Pixel Count", 1, dst2.Cols * dst2.Rows, 20)
        countSlider = findSlider("Random Pixel Count")
    End Sub
    Public Sub RunVB()
    End Sub
End Class








Public Class Options_Hough : Inherits VB_Algorithm
    Public rho As Integer
    Public theta As Single
    Public threshold As Integer
    Public lineCount As Integer
    Public relativeIntensity As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Relative Intensity (Accord)", 1, 100, 90)
            sliders.setupTrackBar("Hough rho", 1, 100, 1)
            sliders.setupTrackBar("Hough theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar("Hough threshold", 1, 100, 3)
            sliders.setupTrackBar("Lines to Plot", 1, 1000, 25)
            sliders.setupTrackBar("Minimum feature pixels", 0, 250, 25)
        End If
    End Sub
    Public Sub RunVB()
        Static rhoslider = findSlider("Hough rho")
        Static thetaSlider = findSlider("Hough theta")
        Static thresholdSlider = findSlider("Hough threshold")
        Static lineSlider = findSlider("Lines to Plot")
        Static relativeSlider = findSlider("Relative Intensity (Accord)")

        rho = rhoslider.Value
        theta = thetaSlider.Value / 1000
        threshold = thresholdSlider.Value
        lineCount = lineSlider.Value
        relativeIntensity = relativeSlider.Value / 100
    End Sub
End Class







Public Class Options_Canny : Inherits VB_Algorithm
    Public threshold1 As Integer
    Public threshold2 As Integer
    Public aperture As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Canny threshold1", 1, 255, 100)
            sliders.setupTrackBar("Canny threshold2", 1, 255, 150)
            sliders.setupTrackBar("Canny Aperture", 3, 7, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static t1Slider = findSlider("Canny threshold1")
        Static t2Slider = findSlider("Canny threshold2")
        Static apertureSlider = findSlider("Canny Aperture")

        threshold1 = t1Slider.Value
        threshold2 = t2Slider.Value
        aperture = apertureSlider.Value Or 1
    End Sub
End Class







Public Class Options_ColorMatch : Inherits VB_Algorithm
    Public minSizeRegion As Integer
    Public maxDistanceCheck As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Remove contours < X points", 1, 100, 10)

        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show Max Distance point")
            check.Box(0).Checked = False
        End If
    End Sub
    Public Sub RunVB()
        Static maxCheck = findCheckBox("Show Max Distance point")
        maxDistanceCheck = maxCheck.checked
    End Sub
End Class









Public Class Options_Sort : Inherits VB_Algorithm
    Public sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Ascending
    Public radio0 As Windows.Forms.RadioButton
    Public radio1 As Windows.Forms.RadioButton
    Public radio2 As Windows.Forms.RadioButton
    Public radio3 As Windows.Forms.RadioButton
    Public radio4 As Windows.Forms.RadioButton
    Public radio5 As Windows.Forms.RadioButton
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("EveryColumn, Ascending")
            radio.addRadio("EveryColumn, Descending")
            radio.addRadio("EveryRow, Ascending")
            radio.addRadio("EveryRow, Descending")
            radio.addRadio("Sort all pixels ascending")
            radio.addRadio("Sort all pixels descending")
            radio.check(0).Checked = True
        End If

        radio0 = findRadio("EveryColumn, Ascending")
        radio1 = findRadio("EveryColumn, Descending")
        radio2 = findRadio("EveryRow, Ascending")
        radio3 = findRadio("EveryRow, Descending")
        radio4 = findRadio("Sort all pixels ascending")
        radio5 = findRadio("Sort all pixels descending")
    End Sub
    Public Sub RunVB()
        If radio1.Checked Then sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Descending
        If radio2.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Ascending
        If radio3.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Descending
    End Sub
End Class






Public Class Options_Distance : Inherits VB_Algorithm
    Public distanceType As cv.DistanceTypes
    Public kernelSize = 0
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("C")
            radio.addRadio("L1")
            radio.addRadio("L2")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static cRadio = findRadio("C")
        Static l1Radio = findRadio("L1")
        Static l2Radio = findRadio("L2")
        If cRadio.Checked Then distanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then distanceType = cv.DistanceTypes.L1
        If l2Radio.Checked Then distanceType = cv.DistanceTypes.L2
    End Sub
End Class









Public Class Options_Warp : Inherits VB_Algorithm
    Public alpha As Double, beta As Double, gamma As Double, f As Double, distance As Double
    Public transformMatrix As cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Alpha", 0, 180, 90)
            sliders.setupTrackBar("Beta", 0, 180, 90)
            sliders.setupTrackBar("Gamma", 0, 180, 90)
            sliders.setupTrackBar("f", 0, 2000, 600)
            sliders.setupTrackBar("distance", 0, 2000, 400)
        End If
    End Sub
    Public Sub RunVB()
        Static alphaSlider = findSlider("Alpha")
        Static betaSlider = findSlider("Beta")
        Static gammaSlider = findSlider("Gamma")
        Static fSlider = findSlider("f")
        Static distanceSlider = findSlider("distance")

        alpha = CDbl(alphaSlider.value - 90) * cv.Cv2.PI / 180
        beta = CDbl(betaSlider.value - 90) * cv.Cv2.PI / 180
        gamma = CDbl(gammaSlider.value - 90) * cv.Cv2.PI / 180
        f = fSlider.value
        distance = distanceSlider.value

        Dim a(,) As Double = {{1, 0, -dst2.Width / 2},
                              {0, 1, -dst2.Height / 2},
                              {0, 0, 0},
                              {0, 0, 1}}

        Dim x(,) As Double = {{1, 0, 0, 0},
                              {0, Math.Cos(alpha), -Math.Sin(alpha), 0},
                              {0, Math.Sin(alpha), Math.Cos(alpha), 0},
                              {0, 0, 0, 1}}

        Dim y(,) As Double = {{Math.Cos(beta), 0, -Math.Sin(beta), 0},
                              {0, 1, 0, 0},
                              {Math.Sin(beta), 0, Math.Cos(beta), 0},
                              {0, 0, 0, 1}}

        Dim z(,) As Double = {{Math.Cos(gamma), -Math.Sin(gamma), 0, 0},
                              {Math.Sin(gamma), Math.Cos(gamma), 0, 0},
                              {0, 0, 1, 0},
                              {0, 0, 0, 1}}

        Dim t(,) As Double = {{1, 0, 0, 0},
                              {0, 1, 0, 0},
                              {0, 0, 1, distance},
                              {0, 0, 0, 1}}

        Dim b(,) As Double = {{f, 0, dst2.Width / 2, 0},
                              {0, f, dst2.Height / 2, 0},
                              {0, 0, 1, 0}}

        Dim a1 = New cv.Mat(4, 3, cv.MatType.CV_64F, a)
        Dim rx = New cv.Mat(4, 4, cv.MatType.CV_64F, x)
        Dim ry = New cv.Mat(4, 4, cv.MatType.CV_64F, y)
        Dim rz = New cv.Mat(4, 4, cv.MatType.CV_64F, z)

        Dim tt = New cv.Mat(4, 4, cv.MatType.CV_64F, t)
        Dim a2 = New cv.Mat(3, 4, cv.MatType.CV_64F, b)

        Dim r = rx * ry * rz
        transformMatrix = a2 * (tt * (r * a1))
    End Sub
End Class








Public Class Options_HistCompare : Inherits VB_Algorithm
    Public compareMethod As cv.HistCompMethods = cv.HistCompMethods.Correl
    Public compareName As String
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Compare using Correlation")
            radio.addRadio("Compare using Chi-squared")
            radio.addRadio("Compare using Chi-squared Alt")
            radio.addRadio("Compare using intersection")
            radio.addRadio("Compare using Bhattacharyya")
            radio.addRadio("Compare using KLDiv")
            radio.addRadio("Compare using Hellinger")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static correlationRadio = findRadio("Compare using Correlation")
        Static chiRadio = findRadio("Compare using Chi-squared")
        Static intersectionRadio = findRadio("Compare using intersection")
        Static bhattRadio = findRadio("Compare using Bhattacharyya")
        Static chiAltRadio = findRadio("Compare using Chi-squared Alt")
        Static hellingerRadio = findRadio("Compare using Hellinger")
        Static kldivRadio = findRadio("Compare using KLDiv")

        If correlationRadio.checked Then compareMethod = cv.HistCompMethods.Correl
        If chiRadio.checked Then compareMethod = cv.HistCompMethods.Chisqr
        If intersectionRadio.checked Then compareMethod = cv.HistCompMethods.Intersect
        If bhattRadio.checked Then compareMethod = cv.HistCompMethods.Bhattacharyya
        If chiAltRadio.checked Then compareMethod = cv.HistCompMethods.ChisqrAlt
        If kldivRadio.checked Then compareMethod = cv.HistCompMethods.KLDiv
        If hellingerRadio.checked Then compareMethod = cv.HistCompMethods.Hellinger

        If correlationRadio.checked Then compareName = "Correlation"
        If chiRadio.checked Then compareName = "Chi Square"
        If intersectionRadio.checked Then compareName = "Intersection"
        If bhattRadio.checked Then compareName = "Bhattacharyya"
        If chiAltRadio.checked Then compareName = "Chi Square Alt"
        If kldivRadio.checked Then compareName = "KLDiv"
        If hellingerRadio.checked Then compareName = "Hellinger"
    End Sub
End Class








Public Class Options_LeftRight : Inherits VB_Algorithm
    Public alpha As Integer
    Public beta As Integer
    Public Sub New()
        Dim alphaDefault = 2000
        Dim betaDefault = -100
        If task.cameraName = "Oak-D camera" Then
            alphaDefault = 500
            betaDefault = 0
        End If
        If task.cameraName = "Azure Kinect 4K" Then
            alphaDefault = 600
            betaDefault = 0
        End If
        If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then alphaDefault = 1500
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Brightness Alpha (contrast)", 0, 10000, alphaDefault)
            sliders.setupTrackBar("Brightness Beta (brightness)", -255, 255, betaDefault)
        End If
    End Sub
    Public Sub RunVB()
        Static betaSlider = findSlider("Brightness Beta (brightness)")
        Static alphaSlider = findSlider("Brightness Alpha (contrast)")
        alpha = alphaSlider.value / 500
        beta = betaSlider.value
    End Sub
End Class











Public Class Options_MatchCell : Inherits VB_Algorithm
    Public overlapPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent overlap", 0, 100, 50)
    End Sub
    Public Sub RunVB()
        Static overlapSlider = findSlider("Percent overlap")
        overlapPercent = overlapSlider.value / 100
    End Sub
End Class








Public Class Options_Extrinsics : Inherits VB_Algorithm
    Public leftCorner As Integer
    Public rightCorner As Integer
    Public topCorner As Integer
    Public Sub New()
        Dim leftVal As Integer = 15
        Dim rightVal As Integer = 15
        Dim topBotVal As Integer = 15
        Select Case task.cameraName
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                leftVal = 14
                rightVal = 13
                topBotVal = 14
            Case "Oak-D camera"
                leftVal = 10
                rightVal = 10
                topBotVal = 10
        End Select
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Left image percent", 0, 50, leftVal)
            sliders.setupTrackBar("Right image percent", 0, 50, rightVal)
            sliders.setupTrackBar("Height percent", 0, 50, topBotVal)
        End If
    End Sub
    Public Sub RunVB()
        Static leftSlider = findSlider("Left image percent")
        Static rightSlider = findSlider("Right image percent")
        Static heightSlider = findSlider("Height percent")
        leftCorner = dst2.Width * leftSlider.value / 100
        rightCorner = dst2.Width * rightSlider.value / 100
        topCorner = dst2.Height * heightSlider.value / 100
    End Sub
End Class







Public Class Options_Translation : Inherits VB_Algorithm
    Public leftTrans As Integer
    Public rightTrans As Integer
    Public Sub New()
        Dim rightShift As Integer = 4
        Dim leftShift As Integer = 5
        If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then
            leftShift = 1
            rightShift = 2
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Left translation percent", 0, 50, leftShift)
            sliders.setupTrackBar("Right translation percent", 0, 50, rightShift)
        End If
    End Sub
    Public Sub RunVB()
        Static leftSlider = findSlider("Left translation percent")
        Static rightSlider = findSlider("Right translation percent")
        leftTrans = dst2.Width * leftSlider.value / 100
        rightTrans = dst2.Width * rightSlider.value / 100
    End Sub
End Class












Public Class Options_OpenGL_Contours : Inherits VB_Algorithm
    Public depthPointStyle As Integer
    Public filterThreshold As Single
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Unfiltered depth points")
            radio.addRadio("Filtered depth points")
            radio.addRadio("Flatten depth points")
            radio.addRadio("Flatten and filter depth points")
            radio.check(3).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter threshold in meters X100", 0, 100, 30)
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Filter threshold in meters X100")
        Static unFilteredRadio = findRadio("Unfiltered depth points")
        Static filteredRadio = findRadio("Filtered depth points")
        Static flattenRadio = findRadio("Flatten depth points")
        filterThreshold = thresholdSlider.value / 100

        If unFilteredRadio.checked Then
            depthPointStyle = pointStyle.unFiltered
        ElseIf filteredRadio.checked Then
            depthPointStyle = pointStyle.filtered
        ElseIf flattenRadio.checked Then
            depthPointStyle = pointStyle.flattened
        Else
            depthPointStyle = pointStyle.flattenedAndFiltered
        End If
    End Sub
End Class











Public Class Options_Motion : Inherits VB_Algorithm
    Public motionThreshold As Integer
    Public cumulativePercentThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Single frame motion threshold", 1, dst2.Total / 4, dst2.Total / 16)
            sliders.setupTrackBar("Cumulative motion threshold percent of image", 1, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Single frame motion threshold")
        Static percentSlider = findSlider("Cumulative motion threshold percent of image")
        motionThreshold = thresholdSlider.value
        cumulativePercentThreshold = percentSlider.value / 100
    End Sub
End Class







Public Class Options_BGSubtract_CPP : Inherits VB_Algorithm
    Public methodDesc As String
    Public currMethod As Integer
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("GMG")
            radio.addRadio("CNT - Counting")
            radio.addRadio("KNN")
            radio.addRadio("MOG")
            radio.addRadio("MOG2")
            radio.addRadio("GSOC")
            radio.addRadio("LSBP")
            radio.check(4).Checked = True ' mog2 appears to be the best...
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    If firstPass = False Then BGSubtract_BGFG_Close(cPtr)
                    currMethod = i
                    methodDesc = "Method = " + frm.check(i).Text
                End If
            End If
        Next
    End Sub
End Class












Public Class Options_Emax : Inherits VB_Algorithm
    Public predictionStepSize As Integer
    Public consistentcolors As Integer
    Public covarianceType = cv.EMTypes.CovMatDefault
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("EMax Prediction Step Size", 1, 20, 5)

        If radio.Setup(traceName) Then
            radio.addRadio("EMax matrix type Spherical")
            radio.addRadio("EMax matrix type Diagonal")
            radio.addRadio("EMax matrix type Generic")
            radio.check(0).Checked = True
        End If

        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use palette to keep colors consistent")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static colorCheck = findCheckBox("Use palette to keep colors consistent")
        Static stepSlider = findSlider("EMax Prediction Step Size")
        predictionStepSize = stepSlider.value
        covarianceType = cv.EMTypes.CovMatDefault
        consistentcolors = colorCheck.checked
        Static emaxFrm = findfrm(traceName + " Radio Buttons")
        For i = 0 To emaxFrm.check.Count - 1
            If emaxFrm.check(i).Checked = True Then
                covarianceType = Choose(i + 1, cv.EMTypes.CovMatSpherical, cv.EMTypes.CovMatDiagonal, cv.EMTypes.CovMatGeneric)
            End If
        Next
    End Sub
End Class













Public Class Options_Threshold_Adaptive : Inherits VB_Algorithm
    Public method As cv.AdaptiveThresholdTypes
    Public blockSize As Integer
    Public constantVal As Integer
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("AdaptiveThreshold block size", 3, 21, 5)
            sliders.setupTrackBar("Constant subtracted from mean Or weighted mean", -20, 20, 0)
        End If

        If standalone = False Then
            findRadio("ToZero").Enabled = False
            findRadio("ToZero Inverse").Enabled = False
            findRadio("Triangle - use maxVal slider to see impact").Enabled = False
            findRadio("Trunc").Enabled = False
        End If
    End Sub
    Public Sub RunVB()
        Static gaussRadio = findRadio("GaussianC")
        Static constantSlider = findSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = findSlider("AdaptiveThreshold block size")

        method = If(gaussRadio.checked, cv.AdaptiveThresholdTypes.GaussianC, cv.AdaptiveThresholdTypes.MeanC)
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class












Public Class Options_Edges_All : Inherits VB_Algorithm
    Public edges As Object
    Public saveSelection As String
    Dim canny As New Edge_Canny
    Dim scharr As New Edge_Scharr
    Dim binRed As New Edge_BinarizedReduction
    Dim sobel = New Edge_Sobel
    Dim binSobel As New Edge_BinarizedSobel
    Dim colorGap As New Edge_ColorGap_CPP
    Dim deriche As New Edge_Deriche_CPP
    Dim laplacian As New Edge_Laplacian
    Dim resizeAdd As New Edge_ResizeAdd
    Dim regions As New Edge_DepthRegions
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Canny")
            radio.addRadio("Sobel")
            radio.addRadio("Scharr")
            radio.addRadio("Binarized Reduction")
            radio.addRadio("Binarized Sobel")
            radio.addRadio("Color Gap")
            radio.addRadio("Deriche")
            radio.addRadio("Laplacian")
            radio.addRadio("Resize And Add")
            radio.addRadio("Depth Region Boundaries")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        Dim eSelection As String = ""
        For i = 0 To frm.check.Count - 1
            If frm.check(i).checked Then
                eSelection = frm.check(i).text
                Exit For
            End If
        Next

        If saveSelection <> eSelection Then
            Select Case eSelection
                Case "Canny"
                    edges = canny
                Case "Sobel"
                    edges = sobel
                Case "Scharr"
                    edges = scharr
                Case "Binarized Reduction"
                    edges = binRed
                Case "Binarized Sobel"
                    edges = binSobel
                Case "Color Gap"
                    edges = colorGap
                Case "Deriche"
                    edges = deriche
                Case "Laplacian"
                    edges = laplacian
                Case "Resize And Add"
                    edges = resizeAdd
                Case "Depth Region Boundaries"
                    edges = regions
            End Select
            saveSelection = eSelection
        End If
    End Sub
End Class












Public Class Options_Intercepts : Inherits VB_Algorithm
    Public interceptRange As Integer
    Public mouseMovePoint As Integer
    Public selectedIntercept As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Intercept width range in pixels", 1, 50, 10)
        If radio.Setup(traceName) Then
            radio.addRadio("Show Top intercepts")
            radio.addRadio("Show Bottom intercepts")
            radio.addRadio("Show Left intercepts")
            radio.addRadio("Show Right intercepts")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub showIntercepts(mousePoint As cv.Point, dst As cv.Mat)
    End Sub
    Public Sub RunVB()
        Static interceptSlider = findSlider("Intercept width range in pixels")
        interceptRange = interceptSlider.Value

        Static topRadio = findRadio("Show Top intercepts")
        Static botRadio = findRadio("Show Bottom intercepts")
        Static leftRadio = findRadio("Show Left intercepts")
        Static rightRadio = findRadio("Show Right intercepts")

        For selectedIntercept = 0 To 3
            mouseMovePoint = Choose(selectedIntercept + 1, task.mouseMovePoint.X, task.mouseMovePoint.X, task.mouseMovePoint.Y, task.mouseMovePoint.Y)
            If Choose(selectedIntercept + 1, topRadio, botRadio, leftRadio, rightRadio).checked Then Exit For
        Next
    End Sub
End Class






Public Class Options_PlaneEstimation : Inherits VB_Algorithm
    Public useDiagonalLines As Boolean
    Public useContour_SidePoints As Boolean
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use diagonal lines")
            radio.addRadio("Use horizontal And vertical lines")
            radio.addRadio("Use Contour_SidePoints to find the line pair")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static diagonalRadio = findRadio("Use diagonal lines")
        Static sidePointsRadio = findRadio("Use Contour_SidePoints to find the line pair")
        useDiagonalLines = diagonalRadio.checked
        useContour_SidePoints = sidePointsRadio.checked
    End Sub
End Class







Public Class Options_ForeGround : Inherits VB_Algorithm
    Public maxForegroundDepthInMeters As Single
    Public minSizeContour As Integer
    Public depthPerRegion As Single
    Public numberOfRegions As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max foreground depth in mm's", 200, 2000, 1500)
            sliders.setupTrackBar("Min length contour", 10, 2000, 100)
            sliders.setupTrackBar("Number of depth ranges", 1, 20, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static depthSlider = findSlider("Max foreground depth in mm's")
        Static minSizeSlider = findSlider("Min length contour")
        Static regionSlider = findSlider("Number of depth ranges")
        maxForegroundDepthInMeters = depthSlider.value / 1000
        minSizeContour = minSizeSlider.value
        numberOfRegions = regionSlider.value
        depthPerRegion = task.maxZmeters / numberOfRegions
    End Sub
End Class







Public Class Options_Sobel : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public threshold As Integer
    Public horizontalDerivative As Boolean
    Public verticalDerivative As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sobel kernel Size", 1, 31, 3)
            sliders.setupTrackBar("Threshold to zero pixels below this value", 0, 255, 50)
        End If

        If findfrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Vertical Derivative")
            check.addCheckBox("Horizontal Derivative")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
        Static ksizeSlider = findSlider("Sobel kernel Size")
        kernelSize = ksizeSlider.Value Or 1
        threshold = thresholdSlider.value
        Static vDeriv = findCheckBox("Vertical Derivative")
        Static hDeriv = findCheckBox("Horizontal Derivative")
        horizontalDerivative = hDeriv.checked
        verticalDerivative = vDeriv.checked
    End Sub
End Class






Public Class Options_RedCloud : Inherits VB_Algorithm
    Public reduction As Integer
    Public prepDataCase As Integer
    Public planeCoefficientThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("PointCloud Reduction", 1, 2500, 250)
            sliders.setupTrackBar("Plane Coefficient threshold", 1, 100, 95)
        End If

        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Reduce point cloud in X")
            radio.addRadio("Reduce point cloud in Y")
            radio.addRadio("Reduce point cloud in Z")
            radio.addRadio("Reduce point cloud in XY")
            radio.addRadio("Reduce point cloud in XZ")
            radio.addRadio("Reduce point cloud in YZ")
            radio.addRadio("Reduce point cloud in XYZ")
            radio.check(3).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For prepDataCase = 0 To frm.check.count - 1
            If frm.check(prepDataCase).Checked Then Exit For
        Next

        Static reduceSlider = findSlider("PointCloud Reduction")
        Static planeSlider = findSlider("Plane Coefficient threshold")

        reduction = reduceSlider.Value
        planeCoefficientThreshold = planeSlider.Value / 100
    End Sub
End Class








Public Class Options_Flood : Inherits VB_Algorithm
    Public floodFlag As cv.FloodFillFlags = 4 Or cv.FloodFillFlags.FixedRange
    Public stepSize As Integer
    Public gridHeight As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Step Size", 1, dst2.Cols / 2, 30)
        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use floating range")
            check.addCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        End If
    End Sub
    Public Sub RunVB()
        Static floatingCheck = findCheckBox("Use floating range")
        Static connectCheck = findCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        Static stepSlider = findSlider("Step Size")

        stepSize = stepSlider.Value
        floodFlag = If(connectCheck.checked, 8, 4) Or If(floatingCheck.checked, cv.FloodFillFlags.FixedRange, 0)
    End Sub
End Class






Public Class Options_ShapeDetect : Inherits VB_Algorithm
    Public fileName As String
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("coins.jpg")
            radio.addRadio("demo.jpg")
            radio.addRadio("demo1.jpg")
            radio.addRadio("demo2.jpg")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        fileName = findRadioText(frm.check)
    End Sub
End Class











Public Class Options_Classifier : Inherits VB_Algorithm
    Public classifierName As String
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Normal Bayes (NBC)")
            radio.addRadio("K Nearest Neighbor (KNN)")
            radio.addRadio("Support Vector Machine (SVM)")
            radio.addRadio("Decision Tree (DTree)")
            radio.addRadio("Boosted Tree (BTree)")
            radio.addRadio("Random Forest (RF)")
            radio.addRadio("Artificial Neural Net (ANN)")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        classifierName = findRadioText(frm.check)
    End Sub
End Class












Public Class Options_Blur : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public sigma As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blur Kernel Size", 0, 32, 3)
            sliders.setupTrackBar("Blur Sigma", 1, 10, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = findSlider("Blur Kernel Size")
        Static sigmaSlider = findSlider("Blur Sigma")
        kernelSize = kernelSlider.Value Or 1
        sigma = sigmaSlider.value * 0.5
    End Sub
End Class












Public Class Options_Harris : Inherits VB_Algorithm
    Public threshold As Single
    Public neighborhood As Integer
    Public aperture As Integer
    Public harrisParm As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Harris Threshold", 1, 100, 1)
            sliders.setupTrackBar("Harris Neighborhood", 1, 41, 21)
            sliders.setupTrackBar("Harris aperture", 1, 31, 21)
            sliders.setupTrackBar("Harris Parameter", 1, 100, 1)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdslider = findSlider("Harris Threshold")
        Static neighborSlider = findSlider("Harris Neighborhood")
        Static apertureSlider = findSlider("Harris aperture")
        Static parmSlider = findSlider("Harris Parameter")

        threshold = thresholdslider.Value / 10000
        neighborhood = neighborSlider.Value Or 1
        aperture = apertureSlider.Value Or 1
        harrisParm = parmSlider.Value / 100
    End Sub
End Class












Public Class Options_AccordSuite : Inherits VB_Algorithm
    Public selection As String
    Public selectedIndex As Integer
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("0: Grayscale")
            radio.addRadio("1: Sepia")
            radio.addRadio("2: Invert")
            radio.addRadio("3: Rotate Channel")
            radio.addRadio("4: Color Filtering")
            radio.addRadio("5: Levels linear correction")
            radio.addRadio("6: Hue Modifier")
            radio.addRadio("7: Saturation Adjusting")
            radio.addRadio("8: Brightness Adjusting")
            radio.addRadio("9: Contrast Adjusting")
            radio.addRadio("10: HSL Filtering")
            radio.addRadio("11: YCbCr linear correction")
            radio.addRadio("12: YCbCr filtering")
            radio.addRadio("13: Threshold binarization")
            radio.addRadio("14: Floyd-Steinberg dithering")
            radio.addRadio("15: Ordered dithering")
            radio.addRadio("16: Convolution")
            radio.addRadio("17: Sharpen")
            radio.addRadio("18: Gaussian Blur")
            radio.addRadio("19: Difference Edge Detector")
            radio.addRadio("20: Homography Edge Detector")
            radio.addRadio("21: Sobel Edge Detector")
            radio.addRadio("22: Jitter")
            radio.addRadio("23: Oil Painting")
            radio.addRadio("24: Texture")
            radio.addRadio("25: FastGuidedFilter")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        If standalone Then Exit Sub
        Static frm = findfrm(traceName + " Radio Buttons")
        selection = frm.check(findRadioIndex(frm.check)).text
        selectedIndex = findRadioIndex(frm.check)
    End Sub
End Class












Public Class Options_Wavelet : Inherits VB_Algorithm
    Public useHaar As Boolean
    Public iterations As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Wavelet Iterations", 1, 5, 3)

        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Haar")
            radio.addRadio("CDF")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = findSlider("Wavelet Iterations")
        Static haarRadio = findRadio("Haar")
        useHaar = haarRadio.checked
        iterations = iterSlider.value
    End Sub
End Class









Public Class Options_SOM : Inherits VB_Algorithm
    Public iterations As Integer
    Public learningRate As Single
    Public radius As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Iterations (000's)", 1, 10, 3)
            sliders.setupTrackBar("Initial Learning Rate %", 1, 100, 10)
            sliders.setupTrackBar("Radius in Pixels", 1, 100, 15)
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = findSlider("Iterations (000's)")
        Static learnSlider = findSlider("Initial Learning Rate %")
        Static radiusSlider = findSlider("Radius in Pixels")
        iterations = iterSlider.value * 1000
        learningRate = learnSlider.value / 100
        radius = radiusSlider.value
    End Sub
End Class












Public Class Options_Sift : Inherits VB_Algorithm
    Public useBFMatcher As Boolean
    Public pointCount As Integer
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use BF Matcher")
            radio.addRadio("Use Flann Matcher")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Points to Match", 1, 1000, 200)
    End Sub
    Public Sub RunVB()
        Static bfRadio = findRadio("Use BF Matcher")
        Static countSlider = findSlider("Points to Match")

        useBFMatcher = bfRadio.checked
        pointCount = countSlider.value
    End Sub
End Class








Public Class Options_Erode : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public iterations As Integer
    Public morphShape As cv.MorphShapes
    Public element As cv.Mat
    Public noshape As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Erode Kernel Size", 1, 32, 3)
            sliders.setupTrackBar("Erode Iterations", 0, 32, 1)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Erode shape: Cross")
            radio.addRadio("Erode shape: Ellipse")
            radio.addRadio("Erode shape: Rect")
            radio.addRadio("Erode shape: None")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static ellipseRadio = findRadio("Erode shape: Ellipse")
        Static rectRadio = findRadio("Erode shape: Rect")
        Static iterSlider = findSlider("Erode Iterations")
        Static kernelSlider = findSlider("Erode Kernel Size")
        Static noShapeRadio = findRadio("Erode shape: None")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cv.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cv.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cv.MorphShapes.Rect
        element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
    End Sub
End Class







Public Class Options_Dilate : Inherits VB_Algorithm
    Public kernelSize As Integer
    Public iterations As Integer
    Public morphShape As cv.MorphShapes
    Public element As cv.Mat
    Public noshape As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Dilate Kernel Size", 1, 32, 3)
            sliders.setupTrackBar("Dilate Iterations", 0, 32, 1)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Dilate shape: Cross")
            radio.addRadio("Dilate shape: Ellipse")
            radio.addRadio("Dilate shape: Rect")
            radio.addRadio("Dilate shape: None")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static ellipseRadio = findRadio("Dilate shape: Ellipse")
        Static rectRadio = findRadio("Dilate shape: Rect")
        Static iterSlider = findSlider("Dilate Iterations")
        Static kernelSlider = findSlider("Dilate Kernel Size")
        Static noShapeRadio = findRadio("Dilate shape: None")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cv.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cv.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cv.MorphShapes.Rect
        element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
    End Sub
End Class












Public Class Options_Histogram2D : Inherits VB_Algorithm
    Public channels() As Integer = {0, 1}
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Channels 0 and 1")
            radio.addRadio("Channels 0 and 2")
            radio.addRadio("Channels 1 and 2")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static channel01Radio = findRadio("Channels 0 and 1")
        Static channel02Radio = findRadio("Channels 0 and 2")
        Static channel12Radio = findRadio("Channels 1 and 2")

        If channel01Radio.checked Then channels = {0, 1}
        If channel02Radio.checked Then channels = {0, 2}
        If channel12Radio.checked Then channels = {1, 2}
    End Sub
End Class













Public Class Options_KMeans : Inherits VB_Algorithm
    Public kMeansFlag As cv.KMeansFlags
    Public kMeansK As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KMeans k", 2, 32, 5)
            sliders.setupTrackBar("Resize Factor", 1, 8, 2)
        End If

        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use PpCenters")
            radio.addRadio("Use RandomCenters")
            radio.addRadio("Use Initialized Labels")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        Static kSlider = findSlider("KMeans k")
        Select Case findRadioText(frm.check)
            Case "Use PpCenters"
                kMeansFlag = cv.KMeansFlags.PpCenters
            Case "Use RandomCenters"
                kMeansFlag = cv.KMeansFlags.RandomCenters
            Case "Use Initialized Labels"
                If task.optionsChanged Then kMeansFlag = cv.KMeansFlags.PpCenters Else kMeansFlag = cv.KMeansFlags.UseInitialLabels
        End Select
        kMeansK = kSlider.Value
    End Sub
End Class






Public Class Options_Reduction : Inherits VB_Algorithm
    Public bitwiseChecked As Boolean
    Public simpleChecked As Boolean
    Public reductionVal As Integer
    Public bitsliderVal As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Bits to remove in bitwise reduction", 0, 7, 5)
            sliders.setupTrackBar("Color Reduction", 1, 255, task.colorReductionDefault)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("Use simple reduction")
            radio.addRadio("Use bitwise reduction")
            radio.addRadio("No reduction")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static bitwiseCheck = findRadio("Use bitwise reduction")
        Static simpleCheck = findRadio("Use simple reduction")
        Static reductionSlider = findSlider("Color Reduction")
        Static bitSlider = findSlider("Bits to remove in bitwise reduction")

        simpleChecked = simpleCheck.checked
        bitwiseChecked = bitwiseCheck.checked
        reductionVal = CInt(reductionSlider.Value)
        bitsliderVal = bitSlider.value
    End Sub
End Class







Public Class Options_LUT : Inherits VB_Algorithm
    Public lutSegments As Integer
    Public splits() As Integer
    Public vals() As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of LUT Segments", 2, 255, 10)
            sliders.setupTrackBar("LUT zero through xxx", 1, 255, 65)
            sliders.setupTrackBar("LUT xxx through yyy", 1, 255, 110)
            sliders.setupTrackBar("LUT yyy through zzz", 1, 255, 160)
            sliders.setupTrackBar("LUT zzz through 255", 1, 255, 210)
        End If

        desc = "Options for LUT algorithms"
    End Sub
    Public Sub RunVB()
        Static lutSlider = findSlider("Number of LUT Segments")
        Static zeroSlider = findSlider("LUT zero through xxx")
        Static xSlider = findSlider("LUT xxx through yyy")
        Static ySlider = findSlider("LUT yyy through zzz")
        Static zSlider = findSlider("LUT zzz through 255")

        splits = {zeroSlider.Value, xSlider.Value, ySlider.Value, zSlider.Value, 255}
        vals = {1, zeroSlider.Value, xSlider.Value, ySlider.Value, 255}
        lutSegments = lutSlider.value
    End Sub
End Class








Public Class Options_ColorFormat : Inherits VB_Algorithm
    Public colorFormat As String
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BGR")
            radio.addRadio("LAB")
            radio.addRadio("HSV")
            radio.addRadio("XYZ")
            radio.addRadio("HLS")
            radio.addRadio("YUV")
            radio.addRadio("YCrCb")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Dim src = task.color
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then colorFormat = radio.check(i).Text
        Next

        If colorFormat Is Nothing Then colorFormat = "BGR" ' multiple invocations cause this to be necessary but how to fix?
        Select Case colorFormat
            Case "BGR"
                dst2 = src
            Case "LAB"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2Lab)
            Case "HSV"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
            Case "XYZ"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2XYZ)
            Case "HLS"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HLS)
            Case "YUV"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YUV)
            Case "YCrCb"
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YCrCb)
        End Select
    End Sub
End Class





Public Class Options_CComp : Inherits VB_Algorithm
    Public light As Integer
    Public dark As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for lighter input", 1, 255, 127)
            sliders.setupTrackBar("Threshold for darker input", 1, 255, 50)
        End If

        desc = "Options for CComp_Both"
    End Sub
    Public Sub RunVB()
        Static lightSlider = findSlider("Threshold for lighter input")
        Static darkSlider = findSlider("Threshold for darker input")
        light = lightSlider.value
        dark = darkSlider.value
    End Sub
End Class








Public Class Options_WarpModel2 : Inherits VB_Algorithm
    Public warpMode As Integer
    Public useWarpAffine As Boolean
    Public useWarpHomography As Boolean
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Motion_Translation (fastest)")
            radio.addRadio("Motion_Euclidean")
            radio.addRadio("Motion_Affine (very slow - Be sure to configure CPP_Classes in Release Mode)")
            radio.addRadio("Motion_Homography (even slower - Use CPP_Classes in Release Mode)")
            radio.check(0).Checked = True
        End If

        desc = "Additional WarpModel options - needed an additional radio button set."
    End Sub
    Public Sub RunVB()
        If standalone Then
            setTrueText("Options for the WarpModel algorithms.  No output when run standalone.")
            Exit Sub
        End If

        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then warpMode = i
        Next
        useWarpAffine = warpMode = 2
        useWarpHomography = warpMode = 3
    End Sub
End Class







Public Class Options_DNN : Inherits VB_Algorithm
    Public superResModelFileName As String
    Public shortModelName As String
    Public superResMultiplier As Integer
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("EDSR_x2.pb")
            radio.addRadio("EDSR_x3.pb")
            radio.addRadio("EDSR_x4.pb")
            radio.addRadio("ESPCN_x2.pb")
            radio.addRadio("ESPCN_x3.pb")
            radio.addRadio("ESPCN_x4.pb")
            radio.addRadio("FSRCNN_X2.pb")
            radio.addRadio("FSRCNN_X3.pb")
            radio.addRadio("FSRCNN_X4.pb")
            radio.addRadio("LapSRN_x2.pb")
            radio.addRadio("LapSRN_x4.pb")
            radio.addRadio("LapSRN_x8.pb")
            radio.check(8).Checked = True
        End If
        desc = "Options for the different SuperRes models and multipliers."
    End Sub
    Public Sub RunVB()
        superResModelFileName = task.homeDir + "Data/DNN_SuperResModels/"
        Static frm = findfrm(traceName + " Radio Buttons")
        Dim index = findRadioIndex(frm.check)
        If radio.check(index).Checked Then
            superResModelFileName += radio.check(index).Text
            Dim split = radio.check(index).Text.Split("_")
            shortModelName = LCase(split(0))
            superResMultiplier = CInt(split(1).Substring(1, 1))
            Dim testFile As New FileInfo(superResModelFileName)
            If testFile.Exists = False Then
                MsgBox("The " + radio.check(index).Text + " super res model file is missing!")
                superResModelFileName = ""
            End If
        End If
        setTrueText("Current Options: " + shortModelName + " at resolution " + CStr(superResMultiplier) + vbCrLf +
                    superResModelFileName + " is present and will be used.")
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/
Public Class Options_Photoshop : Inherits VB_Algorithm
    Public switch As Integer
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Second DuoTone Blue")
            radio.addRadio("Second DuoTone Green")
            radio.addRadio("Second DuoTone Red")
            radio.addRadio("Second DuoTone None")
            radio.check(3).Checked = True
        End If

        desc = "More options for the DuoTone image"
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For switch = 0 To frm.check.Count - 1
            If frm.check(switch).Checked Then Exit For
        Next
    End Sub
End Class





Public Class Options_Gif : Inherits VB_Algorithm
    Public buildCheck As Windows.Forms.CheckBox
    Public restartCheck As Windows.Forms.CheckBox
    Public dst0Radio As Windows.Forms.RadioButton
    Public dst1Radio As Windows.Forms.RadioButton
    Public dst2Radio As Windows.Forms.RadioButton
    Public dst3Radio As Windows.Forms.RadioButton
    Public OpenCVBwindow As Windows.Forms.RadioButton
    Public OpenGLwindow As Windows.Forms.RadioButton
    Public Sub New()
        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Check this box when Gif_Basics dst2 contains the desired snapshot.")
            check.addCheckBox("Build GIF file in <OpenCVB Home Directory>\Temp\myGIF.gif")
            check.addCheckBox("Restart - clear all previous images.")
        End If

        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Capture dst0")
            radio.addRadio("Capture dst1")
            radio.addRadio("Capture dst2")
            radio.addRadio("Capture dst3")
            radio.addRadio("Capture entire OpenCVB window")
            radio.addRadio("Capture OpenGL window")
            radio.check(2).Checked = True
        End If
        buildCheck = findCheckBox("Build GIF file in <OpenCVB Home Directory>\Temp\myGIF.gif")
        restartCheck = findCheckBox("Restart - clear all previous images.")

        dst0Radio = findRadio("Capture dst0")
        dst1Radio = findRadio("Capture dst1")
        dst2Radio = findRadio("Capture dst2")
        dst3Radio = findRadio("Capture dst3")
        OpenCVBwindow = findRadio("Capture entire OpenCVB window")
        OpenGLwindow = findRadio("Capture OpenGL window")
    End Sub
    Public Sub RunVB()
        Static frmCheck = findfrm("Options_Gif CheckBoxes")
        Static frmRadio = findfrm("Options_Gif Radio Buttons")
        If firstPass Then
            frmCheck.Left = gOptions.Width / 2
            frmCheck.top = gOptions.Height / 2
            frmRadio.left = gOptions.Width * 2 / 3
            frmRadio.top = gOptions.Height * 2 / 3
        End If

        If dst2Radio.Checked Then task.gifCaptureIndex = 0
        If dst3Radio.Checked Then task.gifCaptureIndex = 1
        If OpenCVBwindow.Checked Then task.gifCaptureIndex = 2
        If OpenGLwindow.Checked Then task.gifCaptureIndex = 3
        If dst0Radio.Checked Then task.gifCaptureIndex = 4
        If dst1Radio.Checked Then task.gifCaptureIndex = 5

        task.gifBuild = buildCheck.Checked

        buildCheck.Checked = False
        restartCheck.Checked = False

        task.optionsChanged = False
    End Sub
End Class







Public Class Options_IMU : Inherits VB_Algorithm
    Public rotateX As Integer
    Public rotateY As Integer
    Public rotateZ As Integer
    Public alpha As Single
    Public stableThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Rotate pointcloud around X-axis (degrees)", -90, 90, 0)
            sliders.setupTrackBar("Rotate pointcloud around Y-axis (degrees)", -90, 90, 0)
            sliders.setupTrackBar("Rotate pointcloud around Z-axis (degrees)", -90, 90, 0)
            sliders.setupTrackBar("IMU_Basics: Alpha X100", 0, 100, 98)
            sliders.setupTrackBar("IMU Stability Threshold (radians) X100", 0, 100, 2)
        End If
    End Sub
    Public Sub RunVB()
        Static xRotateSlider = findSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
        Static alphaSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
        Static stabilitySlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
        rotateX = xRotateSlider.value
        rotateY = yRotateSlider.value
        rotateZ = zRotateSlider.value
        alpha = alphaSlider.value / 100
        stableThreshold = stabilitySlider.value / 100
    End Sub
End Class





Public Class Options_FeatureMatch : Inherits VB_Algorithm
    Public matchOption As cv.TemplateMatchModes = cv.TemplateMatchModes.CCoeffNormed
    Public matchText As String = ""
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("CCoeff")
            radio.addRadio("CCoeffNormed")
            radio.addRadio("CCorr")
            radio.addRadio("CCorrNormed")
            radio.addRadio("SqDiff")
            radio.addRadio("SqDiffNormed")
            radio.check(1).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sample Size", 1, 1000, 16)
            sliders.setupTrackBar("Feature Correlation Threshold", 1, 100, 90)
            sliders.setupTrackBar("MatchTemplate Cell Size", 2, 60, If(task.workingRes.Height >= 480, 20, 10))
        End If
    End Sub
    Public Sub RunVB()
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
    End Sub
End Class









Public Class Options_Features : Inherits VB_Algorithm
    Public useBRISK As Boolean
    Public sampleSize As Integer
    Public quality As Double
    Public minDistance As Double
    Public rSize As Integer
    Public roi As cv.Rect
    Public correlationThreshold As Single
    Public distanceThreshold As Integer
    Public matchOption As cv.TemplateMatchModes = cv.TemplateMatchModes.CCoeffNormed
    Public matchText As String = ""
    Dim fOptions As New Options_FeatureMatch
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Distance threshold (pixels)", 1, 30, 16)
            sliders.setupTrackBar("Min Distance to next", 1, 100, 15)
            sliders.setupTrackBar("Quality Level", 1, 100, 1)
            sliders.setupTrackBar("Minimum Arc-Y threshold angle (degrees)", 70, 90, 80)
        End If

        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use GoodFeatures")
            radio.addRadio("Use BRISK")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static distanceSlider = findSlider("Distance threshold (pixels)")
        Static numSlider = findSlider("Sample Size")
        Static qualitySlider = findSlider("Quality Level")
        Static distSlider = findSlider("Min Distance to next")
        Static cellSlider = findSlider("MatchTemplate Cell Size")
        Static thresholdSlider = findSlider("Feature Correlation Threshold")
        Static briskRadio = findRadio("Use BRISK")
        useBRISK = briskRadio.checked

        fOptions.RunVB()
        matchOption = fOptions.matchOption
        matchText = fOptions.matchText

        If task.optionsChanged Then
            correlationThreshold = thresholdSlider.Value / 100
            distanceThreshold = distanceSlider.value
            sampleSize = numSlider.Value
            quality = qualitySlider.Value / 100
            minDistance = distSlider.Value
            rSize = cellSlider.Value / 2
            roi = New cv.Rect(0, 0, rSize * 2, rSize * 2)
        End If
    End Sub
End Class





Public Class Options_HeatMap : Inherits VB_Algorithm
    Public showFrustrum As Boolean
    Public redThreshold As Integer
    Public viewName As String = "vertical"
    Public showHistory As Boolean
    Public topView As Boolean = True
    Public sideView As Boolean
    Public Sub New()
        If findfrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Top View (Unchecked Side View)")
            check.addCheckBox("Slice Vertically (Unchecked Slice Horizontally)")
            check.addCheckBox("Show Frustrum")
            check.addCheckBox("Turn off history")
            check.Box(0).Checked = True
            check.Box(2).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for Red channel", 0, 255, 20)
            sliders.setupTrackBar("Show top concentration %", 0, 100, 25)
        End If
    End Sub
    Public Sub RunVB()
        Static topCheck = findCheckBox("Top View (Unchecked Side View)")
        Static frustrumCheck = findCheckBox("Show Frustrum")
        Static noHistoryCheck = findCheckBox("Turn off history")
        Static redSlider = findSlider("Threshold for Red channel")

        redThreshold = redSlider.value

        topView = topCheck.checked
        sideView = Not topView
        If sideView Then viewName = "horizontal"

        showFrustrum = frustrumCheck.checked
        showHistory = Not noHistoryCheck.checked
    End Sub
End Class






Public Class Options_XYRanges : Inherits VB_Algorithm
    Public Sub New()
        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
        Select Case task.cameraName
            Case "Azure Kinect 4K"
                task.xRange = 4.4
                task.yRange = 1.5
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                If dst2.Height = 480 Or dst2.Height = 240 Or dst2.Height = 120 Then
                    task.xRange = 1.38
                    task.yRange = 1.0
                Else
                    task.xRange = 2.5
                    task.yRange = 0.8
                End If
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                If dst2.Height = 480 Or dst2.Height = 240 Or dst2.Height = 120 Then
                    task.xRange = 2.04
                    task.yRange = 2.14
                Else
                    task.xRange = 3.22
                    task.yRange = 1.39
                End If
            Case "Oak-D camera"
                task.xRange = 4.07
                task.yRange = 1.32
            Case "StereoLabs ZED 2/2i"
                task.xRange = 4
                task.yRange = 1.5
            Case "MYNT-EYE-D1000"
                task.xRange = 3.5
                task.yRange = 1.5
        End Select


        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("X-Range X100", 1, 1000, task.xRange * 100)
            sliders.setupTrackBar("Y-Range X100", 1, 1000, task.yRange * 100)
            sliders.setupTrackBar("Side View Red Threshold", 0, 100, 10)
            sliders.setupTrackBar("Top View Red Threshold", 0, 100, 10)
        End If

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(dst2.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        task.bins2D = {dst2.Height, dst2.Width}

        desc = "Control the pointcloud histogram output for X and Y and camera location."
    End Sub
    Public Sub RunVB()
        Static xSlider = findSlider("X-Range X100")
        Static ySlider = findSlider("Y-Range X100")
        Static sideSlider = findSlider("Side View Red Threshold")
        Static topSlider = findSlider("Top View Red Threshold")

        task.xRange = xSlider.value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.
        task.yRange = ySlider.value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.maxZmeters += 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.sideCameraPoint = New cv.Point(0, CInt(dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(dst2.Width / 2), 0)

        task.rangesTop = New cv.Rangef() {New cv.Rangef(0.1, task.maxZmeters), New cv.Rangef(-task.xRange, task.xRange)}
        task.rangesSide = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0.1, task.maxZmeters)}

        task.redThresholdSide = sideSlider.value
        task.redThresholdTop = topSlider.value
    End Sub
End Class






Public Class Options_Boundary : Inherits VB_Algorithm
    Public desiredBoundaries As Integer
    Public peakDistance As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired boundary count", 2, 100, 15)
            sliders.setupTrackBar("Distance to next Peak (pixels)", 2, dst2.Width / 10, dst2.Width / 20)
        End If
    End Sub
    Public Sub RunVB()
        Static boundarySlider = findSlider("Desired boundary count")
        Static distSlider = findSlider("Distance to next Peak (pixels)")
        desiredBoundaries = boundarySlider.value
        peakDistance = distSlider.value
    End Sub
End Class







Public Class Options_Color : Inherits VB_Algorithm
    Public radioText As String
    Public Sub New()
        If findfrm(traceName + " Radio Options") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BackProject_Full")
            radio.addRadio("KMeans_Basics")
            radio.addRadio("LUT_Basics")
            radio.addRadio("Reduction_Basics")
            radio.check(3).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frmRadio = findfrm("Options_Color Radio Buttons")
        For i = 0 To frmRadio.check.count - 1
            If frmRadio.check(i).checked Then radioText = frmRadio.check(i).text
        Next
        If task.optionsChanged Then
            frmRadio.Left = gOptions.Width / 2
            frmRadio.Top = gOptions.Height / 2
        End If
    End Sub
End Class








Public Class Options_Denoise : Inherits VB_Algorithm
    Public removeSinglePixels As Boolean
    Public Sub New()
        If findfrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Remove single pixels")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static singleCheck = findCheckBox("Remove single pixels")
        removeSinglePixels = singleCheck.checked
    End Sub
End Class









'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class Options_MSER : Inherits VB_Algorithm
    Public delta As Integer
    Public minArea As Integer
    Public maxArea As Integer
    Public maxVariation As Single
    Public minDiversity As Single
    Public maxEvolution As Integer
    Public areaThreshold As Single
    Public minMargin As Single
    Public edgeBlurSize As Integer
    Public pass2Setting As Boolean
    Public graySetting As Boolean
    Public Sub New()
        Dim maxDefault = 10000, minDefault = 1000
        Select Case task.workingRes.Width
            Case 1920
                maxDefault = 350000
                minDefault = 6000
            Case 960
                maxDefault = 100000
                minDefault = 3000
            Case 480
                maxDefault = 25000
                minDefault = 1500
            Case 1280
                maxDefault = 150000
                minDefault = 1500
            Case 640
                maxDefault = 50000
                minDefault = 600
            Case 320
                maxDefault = 5000
                minDefault = 300
            Case 672
                maxDefault = 30000
                minDefault = 300
            Case 336
                maxDefault = 15000
                minDefault = 200
            Case 168
                maxDefault = 2000
                minDefault = 1
        End Select
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("MSER Delta", 1, 100, 9)
            sliders.setupTrackBar("MSER Min Area", 1, 10000, minDefault)
            sliders.setupTrackBar("MSER Max Area", 1000, 500000, maxDefault)
            sliders.setupTrackBar("MSER Max Variation", 1, 100, 25)
            sliders.setupTrackBar("MSER Diversity", 0, 100, 20)
            sliders.setupTrackBar("MSER Max Evolution", 1, 1000, 200)
            sliders.setupTrackBar("MSER Area Threshold", 1, 101, 101)
            sliders.setupTrackBar("MSER Min Margin", 1, 100, 3)
            sliders.setupTrackBar("MSER Edge BlurSize", 1, 20, 5)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Pass2Only")
            check.addCheckBox("Use grayscale input")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static deltaSlider = findSlider("MSER Delta")
        Static minAreaSlider = findSlider("MSER Min Area")
        Static maxAreaSlider = findSlider("MSER Max Area")
        Static variationSlider = findSlider("MSER Max Variation")
        Static diversitySlider = findSlider("MSER Diversity")
        Static evolutionSlider = findSlider("MSER Max Evolution")
        Static thresholdSlider = findSlider("MSER Area Threshold")
        Static marginSlider = findSlider("MSER Min Margin")
        Static blurSlider = findSlider("MSER Edge BlurSize")

        delta = deltaSlider.Value
        minArea = minAreaSlider.Value
        maxArea = maxAreaSlider.Value
        maxVariation = variationSlider.Value / 100
        minDiversity = diversitySlider.Value / 100
        maxEvolution = evolutionSlider.Value
        areaThreshold = thresholdSlider.Value / 100
        minMargin = marginSlider.Value / 1000
        edgeBlurSize = blurSlider.Value Or 1

        Static pass2Check = findCheckBox("Pass2Only")
        Static grayCheck = findCheckBox("Use grayscale input")
        pass2Setting = pass2Check.checked
        graySetting = grayCheck.checked
    End Sub
End Class