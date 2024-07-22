Imports cv = OpenCvSharp
Imports System.IO
Imports System.Numerics
Imports OpenCvSharp.ML
Imports OpenCvSharp
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles

Public Class Options_Annealing : Inherits VB_Parent
    Public cityCount As Integer = 25
    Public copyBestFlag As Boolean = False
    Public circularFlag As Boolean = True
    Public successCount As Integer = 8
    Public Sub New()
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
        Static travelCheck = FindCheckBox("Restart Traveling Salesman")
        Static circleCheck = FindCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static copyBestCheck = FindCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half")
        Static circularCheck = FindCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static citySlider = FindSlider("Anneal Number of Cities")
        Static successSlider = FindSlider("Success = top X threads agree on energy level.")

        copyBestFlag = copyBestCheck.checked
        circularFlag = circularCheck.checked
        cityCount = citySlider.Value
        successCount = successSlider.Value
        travelCheck.Checked = False
    End Sub
End Class






Public Class Options_CamShift : Inherits VB_Parent
    Public camMax As Integer = 255
    Public camSBins As cv.Scalar = New cv.Scalar(0, 40, 32)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("CamShift vMin", 0, 255, camSBins(2))
            sliders.setupTrackBar("CamShift vMax", 0, 255, camMax)
            sliders.setupTrackBar("CamShift Smin", 0, 255, camSBins(1))
        End If
    End Sub
    Public Sub RunVB()
        Static vMinSlider = FindSlider("CamShift vMin")
        Static vMaxSlider = FindSlider("CamShift vMax")
        Static sMinSlider = FindSlider("CamShift Smin")

        Dim vMin = vMinSlider.Value
        Dim vMax = vMaxSlider.Value
        Dim sMin = sMinSlider.Value

        Dim min = Math.Min(vMin, vMax)
        camMax = Math.Max(vMin, vMax)
        camSBins = New cv.Scalar(0, sMin, min)
    End Sub
End Class







Public Class Options_Contours2 : Inherits VB_Parent
    Public ApproximationMode = cv.ContourApproximationModes.ApproxTC89KCOS
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
        Static frm = FindFrm(traceName + " Radio Buttons")
        ApproximationMode = radioChoices(findRadioIndex(frm.check))
    End Sub
End Class





Public Class Options_Contours : Inherits VB_Parent
    Public retrievalMode As cv.RetrievalModes = cv.RetrievalModes.External
    Public ApproximationMode As cv.ContourApproximationModes = cv.ContourApproximationModes.ApproxTC89KCOS
    Public epsilon As Single = 3 / 100
    Public minPixels As Integer = 30
    Public cmPerTier As Integer = 50
    Public trueTextOffset As Integer = 80
    Dim maxContourCount As Integer = 50
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

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("cm's per tier", 10, 200, cmPerTier)
            ' sliders.setupTrackBar("Contour epsilon (arc length percent)", 0, 100, epsilon * 100)
            sliders.setupTrackBar("Min Pixels", 1, 2000, minPixels)
            sliders.setupTrackBar("Max contours", 1, 200, maxContourCount)
            sliders.setupTrackBar("TrueText offset", 1, dst2.Width / 3, trueTextOffset)
        End If
    End Sub
    Public Sub RunVB()
        options2.RunVB()
        Static cmSlider = FindSlider("cm's per tier")
        ' Static epsilonSlider = FindSlider("Contour epsilon (arc length percent)")
        Static minSlider = FindSlider("Min Pixels")
        Static countSlider = FindSlider("Max contours")
        Static offsetSlider = FindSlider("TrueText offset")
        maxContourCount = countSlider.value

        ' epsilon = epsilonSlider.Value / 100

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External,
                                       cv.RetrievalModes.FloodFill, cv.RetrievalModes.List,
                                       cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        ApproximationMode = options2.ApproximationMode
        minPixels = minSlider.value
        cmPerTier = cmSlider.value
        trueTextOffset = offsetSlider.value
    End Sub
End Class






Public Class Options_Draw : Inherits VB_Parent
    Public drawCount As Integer = 3
    Public drawFilled As Integer = 2
    Public drawRotated As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("DrawCount", 0, 20, drawCount)

        If check.Setup(traceName) Then
            check.addCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            check.addCheckBox("Draw filled (unchecked draw an outline)")
        End If
    End Sub
    Public Sub RunVB()
        Static countSlider = FindSlider("DrawCount")
        Static fillCheck = FindCheckBox("Draw filled (unchecked draw an outline)")
        Static rotateCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        drawCount = countSlider.Value
        drawFilled = If(fillCheck.checked, -1, 2)
        drawRotated = rotateCheck.checked
    End Sub
End Class




' https://answers.opencv.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Options_Encode : Inherits VB_Parent
    Public qualityLevel As Integer = 1
    Public scalingLevel As Integer = 85
    Public encodeOption = cv.ImwriteFlags.JpegProgressive
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Encode Quality Level", 1, 100, qualityLevel) ' make it low quality to highlight how different it can be.
            sliders.setupTrackBar("Encode Output Scaling", 1, 100, scalingLevel)
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
    Public Sub RunVB()
        Static qualitySlider = FindSlider("Encode Quality Level")
        Static scalingSlider = FindSlider("Encode Output Scaling")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next
        qualityLevel = qualitySlider.Value
        scalingLevel = scalingSlider.value
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
    End Sub
End Class







Public Class Options_Filter : Inherits VB_Parent
    Public kernelSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter kernel size", 1, 21, kernelSize)
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Filter kernel size")
        kernelSize = kernelSlider.value Or 1
    End Sub
End Class






Public Class Options_GeneticDrawing : Inherits VB_Parent
    Public stageTotal = 100
    Public brushPercent = 1.0
    Public strokeCount As Integer = 10
    Public snapCheck As Boolean = False
    Public generations As Integer = 20
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Snapshot Video input to initialize genetic drawing")
            check.addCheckBox("Restart the algorithm with the current settings")
            check.Box(1).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of Generations", 1, 200, generations)
            sliders.setupTrackBar("Number of Stages", 1, 2000, stageTotal)
            sliders.setupTrackBar("Brushstroke count per generation", 1, 20, strokeCount)
            sliders.setupTrackBar("Brush size Percentage", 5, 100, brushPercent * 100)
        End If
    End Sub
    Public Sub RunVB()
        Static genSlider = FindSlider("Number of Generations")
        Static stageSlider = FindSlider("Number of Stages")
        Static brushSlider = FindSlider("Brush size Percentage")
        Static snapCheckbox = FindCheckBox("Snapshot Video input to initialize genetic drawing")
        Static strokeSlider = FindSlider("Brushstroke count per generation")

        If snapCheckbox.checked Then snapCheckbox.checked = False
        snapCheck = snapCheckbox.checked
        stageTotal = stageSlider.value
        generations = genSlider.value
        brushPercent = brushSlider.value / 100
        strokeCount = strokeSlider.value
    End Sub
End Class







Public Class Options_MatchShapes : Inherits VB_Parent
    Public matchOption = cv.ShapeMatchModes.I1
    Public matchThreshold As Single = 0.4
    Public maxYdelta As Single = 0.05
    Public minSize As Single = dst2.Total / 100
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("I1 - Hu moments absolute sum of inverse differences")
            radio.addRadio("I2 - Hu moments absolute difference")
            radio.addRadio("I3 - Hu moments max absolute difference ratio")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Match Threshold %", 0, 100, matchThreshold * 100)
            sliders.setupTrackBar("Max Y Delta % (of height)", 0, 10, maxYdelta * 100)
            sliders.setupTrackBar("Min Size % of image size", 0, 20, dst2.Total / 100)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Match Threshold %")
        Static ySlider = FindSlider("Max Y Delta % (of height)")
        Static minSlider = FindSlider("Min Size % of image size")
        matchThreshold = thresholdSlider.Value / 100
        maxYdelta = ySlider.Value * dst2.Height / 100
        minSize = minSlider.value * dst2.Total / 100

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.ShapeMatchModes.I1, cv.ShapeMatchModes.I2, cv.ShapeMatchModes.I3)
                Exit For
            End If
        Next
    End Sub
End Class









Public Class Options_Plane : Inherits VB_Parent
    Public rmsThreshold As Single = 0.1
    Public useMaskPoints As Boolean
    Public useContourPoints As Boolean
    Public use3Points As Boolean
    Public reuseRawDepthData As Boolean
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use all points in the rc mask")
            radio.addRadio("Use only points in the contour of the rc mask")
            radio.addRadio("Use 3 points in the contour of the rc mask")
            radio.addRadio("Don't replace the depth data with computed plane data")
            radio.check(1).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("RMS error threshold for flat X100", 0, 100, rmsThreshold * 100)
        End If
    End Sub
    Public Sub RunVB()
        Static rmsSlider = FindSlider("RMS error threshold for flat X100")
        rmsThreshold = rmsSlider.Value / 100

        Static maskRadio = FindRadio("Use all points in the rc mask")
        Static contourRadio = FindRadio("Use only points in the contour of the rc mask")
        Static simpleRadio = FindRadio("Use 3 points in the contour of the rc mask")
        useMaskPoints = maskRadio.checked
        useContourPoints = contourRadio.checked
        use3Points = simpleRadio.checked

        Static depthRadio = FindRadio("Don't replace the depth data with computed plane data")
        reuseRawDepthData = depthRadio.checked
    End Sub

End Class







Public Class Options_Neighbors : Inherits VB_Parent
    Public threshold As Single = 0.005
    Public pixels As Integer = 6
    Public patchZ As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Difference from neighbor in mm's", 0, 20, threshold * 1000)
            sliders.setupTrackBar("Minimum offset to neighbor pixel", 1, 100, pixels)
            sliders.setupTrackBar("Patch z-values", 0, 1, 1)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Difference from neighbor in mm's")
        Static pixelSlider = FindSlider("Minimum offset to neighbor pixel")
        Static patchSlider = FindSlider("Patch z-values")
        threshold = thresholdSlider.value / 1000
        pixels = pixelSlider.value
        patchZ = patchSlider.value = 1
    End Sub
End Class






Public Class Options_Interpolate : Inherits VB_Parent
    Public resizePercent As Integer = 2
    Public interpolationThreshold As Integer = 4
    Public pixelCountThreshold As Integer = 0
    Public saveDefaultThreshold As Integer = resizePercent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Interpolation Resize %", 1, 100, resizePercent)
            sliders.setupTrackBar("Interpolation threshold", 1, 255, interpolationThreshold)
            sliders.setupTrackBar("Number of interplation pixels that changed", 0, 100, pixelCountThreshold)
        End If
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
    End Sub
    Public Sub RunVB()
        Static resizeSlider = FindSlider("Interpolation Resize %")
        Static interpolationSlider = FindSlider("Interpolation Resize %")
        Static pixelSlider = FindSlider("Number of interplation pixels that changed")
        resizePercent = resizeSlider.value
        interpolationThreshold = interpolationSlider.value
        pixelCountThreshold = pixelSlider.value
    End Sub
End Class







Public Class Options_Resize : Inherits VB_Parent
    Public warpFlag As cv.InterpolationFlags = cv.InterpolationFlags.Nearest
    Public radioIndex As Integer
    Public resizePercent As Single = 0.03
    Public topLeftOffset As Integer = 10
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Area")
            radio.addRadio("Cubic flag (best blended)")
            radio.addRadio("Lanczos4")
            radio.addRadio("Linear")
            radio.addRadio("Nearest (preserves pixel values best)")
            radio.addRadio("WarpFillOutliers")
            radio.addRadio("WarpInverseMap")
            radio.check(4).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resize Percentage (%)", 1, 100, resizePercent * 100)
            sliders.setupTrackBar("Offset from top left corner", 1, 50, topLeftOffset)
        End If
    End Sub
    Public Sub RunVB()
        Static percentSlider = FindSlider("Resize Percentage (%)")
        Static offsetSlider = FindSlider("Offset from top left corner")
        resizePercent = percentSlider.Value / 100
        topLeftOffset = offsetSlider.Value
        Static frm = FindFrm(traceName + " Radio Buttons")
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








Public Class Options_Smoothing : Inherits VB_Parent
    Public iterations As Integer = 8
    Public interiorTension As Single = 0.5
    Public stepSize As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Smoothing iterations", 1, 20, iterations)
            sliders.setupTrackBar("Smoothing tension X100 (Interior Only)", 1, 100, interiorTension * 100)
            sliders.setupTrackBar("Step size when adding points (1 is identity)", 1, 500, stepSize)
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = FindSlider("Smoothing iterations")
        Static tensionSlider = FindSlider("Smoothing tension X100 (Interior Only)")
        Static stepSlider = FindSlider("Step size when adding points (1 is identity)")
        iterations = iterSlider.Value
        interiorTension = tensionSlider.Value / 100
        stepSize = stepSlider.Value
    End Sub
End Class





Public Class Options_Structured : Inherits VB_Parent
    Public sliceSize As Integer = 1
    Public stepSize As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Structured Depth slice thickness in pixels", 1, 10, sliceSize)
            sliders.setupTrackBar("Slice step size in pixels (multi-slice option only)", 1, 100, stepSize)
        End If
    End Sub
    Public Sub RunVB()
        Static sliceSlider = FindSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = FindSlider("Slice step size in pixels (multi-slice option only)")
        sliceSize = sliceSlider.Value
        stepSize = stepSlider.Value
    End Sub
End Class










Public Class Options_SuperRes : Inherits VB_Parent
    Public method As String = "farneback"
    Public iterations As Integer = 10
    Public restartWithNewOptions As Boolean
    Dim radioChoices = {"farneback", "tvl1", "brox", "pyrlk"}
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("farneback")
            radio.addRadio("tvl1")
            radio.addRadio("brox")
            radio.addRadio("pyrlk")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("SuperRes Iterations", 10, 200, iterations)
    End Sub
    Public Sub RunVB()
        Static iterSlider = FindSlider("SuperRes Iterations")
        Static frm = FindFrm(traceName + " Radio Buttons")
        method = radioChoices(findRadioIndex(frm.check))
        Static lastMethod = method
        restartWithNewOptions = False
        If lastMethod <> method Or iterSlider.Value <> iterations Then restartWithNewOptions = True
        lastMethod = method
        iterations = iterSlider.Value
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM2 : Inherits VB_Parent
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
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                SVMType = Choose(i + 1, cv.ML.SVM.Types.CSvc, cv.ML.SVM.Types.EpsSvr, cv.ML.SVM.Types.NuSvc, cv.ML.SVM.Types.NuSvr, cv.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next
        If standaloneTest() Then SetTrueText(traceName + " has no output when run standaloneTest()." + vbCrLf + "It is used to setup more SVM options.")
    End Sub
End Class









' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM : Inherits VB_Parent
    Public kernelType = cv.ML.SVM.KernelTypes.Poly
    Public granularity As Integer = 5
    Public svmDegree As Single = 1
    Public gamma As Integer = 1
    Public svmCoef0 As Single = 1
    Public svmC As Single = 1
    Public svmNu As Single = 0.5
    Public svmP As Single = 0
    Public sampleCount As Integer = 500
    Dim options2 As New Options_SVM2
    Dim radioChoices = {cv.ML.SVM.KernelTypes.Linear, cv.ML.SVM.KernelTypes.Poly, cv.ML.SVM.KernelTypes.Rbf, cv.ML.SVM.KernelTypes.Sigmoid}
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Granularity", 1, 50, granularity)
            sliders.setupTrackBar("SVM Degree", 1, 100, svmDegree * 100)
            sliders.setupTrackBar("SVM Gamma", 1, 100, gamma * 100)
            sliders.setupTrackBar("SVM Coef0 X100", 1, 100, svmCoef0 * 100)
            sliders.setupTrackBar("SVM C X100", 0, 100, svmC * 100)
            sliders.setupTrackBar("SVM Nu X100", 1, 100, svmNu * 100)
            sliders.setupTrackBar("SVM P X100", 0, 100, 0)
            sliders.setupTrackBar("SVM Sample Count", 2, 1000, sampleCount)
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
        Static frm = FindFrm(traceName + " Radio Buttons")
        kernelType = radioChoices(findRadioIndex(frm.check))

        Static granSlider = FindSlider("Granularity")
        Static degreeSlider = FindSlider("SVM Degree")
        Static gammaSlider = FindSlider("SVM Gamma")
        Static coef0Slider = FindSlider("SVM Coef0 X100")
        Static svmCSlider = FindSlider("SVM C X100")
        Static svmNuSlider = FindSlider("SVM Nu X100")
        Static svmPSlider = FindSlider("SVM P X100")
        Static sampleSlider = FindSlider("SVM Sample Count")

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







Public Class Options_WarpModel : Inherits VB_Parent
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
        Static gradientCheck = FindCheckBox("Use Gradient in WarpInput")
        Static frm = FindFrm(traceName + " Radio Buttons")

        If task.optionsChanged Then
            options2.RunVB()
            warpMode = options2.warpMode
            useWarpAffine = options2.useWarpAffine
            useWarpHomography = options2.useWarpHomography

            useGradient = gradientCheck.checked

            For i = 0 To frm.check.Count - 1
                Dim nextRadio = frm.check(i)
                If nextRadio.Checked Then
                    Dim photo As New FileInfo(task.HomeDir + "Data\Prokudin\" + nextRadio.Text)
                    pkImage = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                    Exit For
                End If
            Next
        End If
    End Sub
End Class










Public Class Options_MinMaxNone : Inherits VB_Parent
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
        Static frm As OptionsRadioButtons = FindFrm(traceName + " Radio Buttons")
        useMax = frm.check(0).Checked
        useMin = frm.check(1).Checked
        useNone = frm.check(2).Checked
    End Sub
End Class







Public Class Options_OpenGL : Inherits VB_Parent
    Public FOV As Single = 75
    Public yaw As Single = -3
    Public pitch As Single = 3
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 20
    Public pointSize As Integer = 2
    Public zTrans As Single = 0.5
    Public eye As New cv.Vec3f(0, 0, -40)
    Public scaleXYZ As New cv.Vec3f(10, 10, 1)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL yaw (degrees)", -180, 180, yaw)
            sliders.setupTrackBar("OpenGL pitch (degrees)", -180, 180, pitch)
            sliders.setupTrackBar("OpenGL roll (degrees)", -180, 180, roll)

            sliders.setupTrackBar("OpenGL Eye X X100", -180, 180, eye(0))
            sliders.setupTrackBar("OpenGL Eye Y X100", -180, 180, eye(1))
            sliders.setupTrackBar("OpenGL Eye Z X100", -180, 180, eye(2))

            sliders.setupTrackBar("OpenGL Scale X X10", 1, 100, scaleXYZ(0))
            sliders.setupTrackBar("OpenGL Scale Y X10", 1, 100, scaleXYZ(1))
            sliders.setupTrackBar("OpenGL Scale Z X10", 1, 100, scaleXYZ(2))

            sliders.setupTrackBar("OpenGL zNear", 0, 100, zNear)
            sliders.setupTrackBar("OpenGL zFar", -50, 200, zFar)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, pointSize)
            sliders.setupTrackBar("zTrans (X100)", -1000, 1000, zTrans * 100)

            sliders.setupTrackBar("OpenGL FOV", 1, 180, FOV)
            If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then FindSlider("OpenGL yaw (degrees)").Value = 135
        End If
    End Sub
    Public Sub RunVB()
        Static yawSlider = FindSlider("OpenGL yaw (degrees)")
        Static pitchSlider = FindSlider("OpenGL pitch (degrees)")
        Static rollSlider = FindSlider("OpenGL roll (degrees)")
        Static eyeXSlider = FindSlider("OpenGL Eye X X100")
        Static eyeYSlider = FindSlider("OpenGL Eye Y X100")
        Static eyeZSlider = FindSlider("OpenGL Eye Z X100")
        Static scaleXSlider = FindSlider("OpenGL Scale X X10")
        Static scaleYSlider = FindSlider("OpenGL Scale Y X10")
        Static scaleZSlider = FindSlider("OpenGL Scale Z X10")
        Static zNearSlider = FindSlider("OpenGL zNear")
        Static zFarSlider = FindSlider("OpenGL zFar")
        Static zTransSlider = FindSlider("zTrans (X100)")
        Static fovSlider = FindSlider("OpenGL FOV")
        Static PointSizeSlider = FindSlider("OpenGL Point Size")

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









Public Class Options_OpenGLFunctions : Inherits VB_Parent
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
        PointSizeSlider = FindSlider("OpenGL Point Size")
    End Sub
    Public Sub RunVB()
        Static XmoveSlider = FindSlider("OpenGL shift left/right (X-axis) X100")
        Static YmoveSlider = FindSlider("OpenGL shift up/down (Y-axis) X100")
        Static ZmoveSlider = FindSlider("OpenGL shift fwd/back (Z-axis) X100")

        moveAmount = New cv.Point3f(XmoveSlider.Value / 100, YmoveSlider.Value / 100, ZmoveSlider.Value / 100)
    End Sub
End Class










Public Class Options_MinArea : Inherits VB_Parent
    Public srcPoints As New List(Of cv.Point2f)
    Public squareWidth As Integer = 100
    Public numPoints As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area Number of Points", 1, 30, numPoints)
            sliders.setupTrackBar("Area size", 10, 300, squareWidth * 2)
        End If
    End Sub
    Public Sub RunVB()
        Static numSlider = FindSlider("Area Number of Points")
        Static sizeSlider = FindSlider("Area size")
        Dim squareWidth = sizeSlider.Value / 2
        srcPoints.Clear()

        Dim pt As cv.Point2f
        numPoints = numSlider.Value
        For i = 0 To numPoints - 1
            pt.X = msRNG.Next(dst2.Width / 2 - squareWidth, dst2.Width / 2 + squareWidth)
            pt.Y = msRNG.Next(dst2.Height / 2 - squareWidth, dst2.Height / 2 + squareWidth)
            srcPoints.Add(pt)
        Next
    End Sub
End Class








Public Class Options_DCT : Inherits VB_Parent
    Public dctFlag As cv.DctFlags
    Public runLengthMin As Integer = 15
    Public removeFrequency As Integer = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Remove Frequencies < x", 0, 100, removeFrequency)
            sliders.setupTrackBar("Run Length Minimum", 1, 100, runLengthMin)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("DCT Flags None")
            radio.addRadio("DCT Flags Row")
            radio.addRadio("DCT Flags Inverse")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static removeSlider = FindSlider("Remove Frequencies < x")
        Static runLenSlider = FindSlider("Run Length Minimum")

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






Public Class Options_Eigen : Inherits VB_Parent
    Public highlight As Boolean
    Public recompute As Boolean
    Public randomCount As Integer = 100
    Public linePairCount As Integer = 20
    Public noiseOffset As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Random point count", 0, 500, randomCount)
            sliders.setupTrackBar("Line Point Count", 0, 500, linePairCount)
            sliders.setupTrackBar("Line Noise", 1, 100, noiseOffset)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Highlight Line Data")
            check.addCheckBox("Recompute with new random data")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static recomputeBox = FindCheckBox("Recompute with new random data")
        Static highlightBox = FindCheckBox("Highlight Line Data")
        Static randomSlider = FindSlider("Random point count")
        Static linePairSlider = FindSlider("Random point count")
        Static noiseSlider = FindSlider("Line Noise")
        randomCount = randomSlider.Value
        linePairCount = linePairSlider.Value
        highlight = highlightBox.checked
        recompute = recomputeBox.checked
        noiseOffset = noiseSlider.Value
    End Sub
End Class







Public Class Options_FitLine : Inherits VB_Parent
    Public radiusAccuracy As Integer = 10
    Public angleAccuracy As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Accuracy for the radius X100", 0, 100, radiusAccuracy)
            sliders.setupTrackBar("Accuracy for the angle X100", 0, 100, angleAccuracy)
        End If
    End Sub
    Public Sub RunVB()
        Static radiusSlider = FindSlider("Accuracy for the radius X100")
        Static angleSlider = FindSlider("Accuracy for the angle X100")
        radiusAccuracy = radiusSlider.Value
        angleAccuracy = angleSlider.Value
    End Sub
End Class







Public Class Options_Fractal : Inherits VB_Parent
    Public iterations As Integer = 34
    Public resetCheck As Windows.Forms.CheckBox
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Mandelbrot iterations", 1, 50, iterations)
        If check.Setup(traceName) Then check.addCheckBox("Reset to original Mandelbrot")
        resetCheck = FindCheckBox("Reset to original Mandelbrot")
    End Sub
    Public Sub RunVB()
        Static iterSlider = FindSlider("Mandelbrot iterations")
        iterations = iterSlider.Value
    End Sub
End Class







Public Class Options_ProCon : Inherits VB_Parent
    Public buffer(10 - 1) As Integer
    Public pduration As Integer = 1
    Public cduration As Integer = 1
    Public bufferSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Buffer Size", 1, 100, buffer.Length)
            sliders.setupTrackBar("Producer Workload Duration (ms)", 1, 1000, pduration)
            sliders.setupTrackBar("Consumer Workload Duration (ms)", 1, 1000, cduration)
        End If
        buffer = Enumerable.Repeat(-1, buffer.Length).ToArray
    End Sub
    Public Sub RunVB()
        Static sizeSlider = FindSlider("Buffer Size")
        Static proSlider = FindSlider("Producer Workload Duration (ms)")
        Static conSlider = FindSlider("Consumer Workload Duration (ms)")
        If task.optionsChanged Then
            bufferSize = sizeSlider.Value
            pduration = proSlider.Value
            cduration = conSlider.Value
        End If
    End Sub
End Class






Public Class Options_OilPaint : Inherits VB_Parent
    Public kernelSize As Integer = 4
    Public intensity As Integer = 20
    Public threshold As Integer = 25
    Public filterSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 2, 10, kernelSize)
            sliders.setupTrackBar("Intensity", 1, 250, intensity)
            sliders.setupTrackBar("Filter Size", 3, 15, filterSize)
            sliders.setupTrackBar("OilPaint Threshold", 0, 200, threshold)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Kernel Size")
        Static intensitySlider = FindSlider("Intensity")
        Static filterSlider = FindSlider("Filter Size")
        Static thresholdSlider = FindSlider("OilPaint Threshold")
        kernelSize = kernelSlider.Value Or 1

        intensity = intensitySlider.Value
        threshold = thresholdSlider.Value

        filterSize = filterSlider.Value
    End Sub
End Class










Public Class Options_Pointilism : Inherits VB_Parent
    Public smoothingRadius As Integer = 32 * 2 + 1
    Public strokeSize As Integer = 3
    Public useElliptical As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Stroke Scale", 1, 5, strokeSize)
            sliders.setupTrackBar("Smoothing Radius", 0, 100, smoothingRadius / 2 - 1)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("Use Elliptical stroke")
            radio.addRadio("Use Circular stroke")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static radiusSlider = FindSlider("Smoothing Radius")
        Static strokeSlider = FindSlider("Stroke Scale")
        Static ellipStroke = FindRadio("Use Elliptical stroke")
        smoothingRadius = radiusSlider.Value * 2 + 1
        strokeSize = strokeSlider.Value
        useElliptical = ellipStroke.checked
    End Sub
End Class







Public Class Options_MotionBlur : Inherits VB_Parent
    Public showDirection As Boolean = True
    Public redoCheckBox As Windows.Forms.CheckBox
    Public kernelSize As Integer = 51
    Public theta As Single
    Public restoreLen As Integer = 10
    Public SNR As Integer = 700
    Public gamma As Integer = 5
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Redo motion blurred image")
            check.Box(0).Checked = True
        End If
        redoCheckBox = FindCheckBox("Redo motion blurred image")

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Deblur Restore Vector", 1, 10, restoreLen)
            sliders.setupTrackBar("Deblur Angle of Restore Vector", -90, 90, theta)
            sliders.setupTrackBar("Motion Blur Angle", -90, 90, 0)
            sliders.setupTrackBar("Motion Blur Length", 1, 101, kernelSize)
            sliders.setupTrackBar("Deblur Signal to Noise Ratio", 1, 1000, SNR)
            sliders.setupTrackBar("Deblur Gamma", 1, 100, gamma)
        End If
    End Sub
    Public Sub RunVB()
        Static deblurSlider = FindSlider("Deblur Restore Vector")
        Static angleSlider = FindSlider("Deblur Angle of Restore Vector")
        Static blurSlider = FindSlider("Motion Blur Length")
        Static blurAngleSlider = FindSlider("Motion Blur Angle")
        Static SNRSlider = FindSlider("Deblur Signal to Noise Ratio")
        Static gammaSlider = FindSlider("Deblur Gamma")
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






Public Class Options_BinarizeNiBlack : Inherits VB_Parent
    Public kernelSize As Integer = 51
    Public niBlackK As Single = -200 / 1000
    Public nickK As Single = 100 / 1000
    Public sauvolaK As Single = 100 / 1000
    Public sauvolaR As Single = 64
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 3, 500, kernelSize)
            sliders.setupTrackBar("Niblack k", -1000, 1000, niBlackK * 1000)
            sliders.setupTrackBar("Nick k", -1000, 1000, nickK * 1000)
            sliders.setupTrackBar("Sauvola k", -1000, 1000, sauvolaK * 1000)
            sliders.setupTrackBar("Sauvola r", 1, 100, sauvolaR)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Kernel Size")
        Static NiBlackSlider = FindSlider("Niblack k")
        Static kSlider = FindSlider("Nick k")
        Static skSlider = FindSlider("Sauvola k")
        Static rSlider = FindSlider("Sauvola r")
        kernelSize = kernelSlider.Value Or 1
        niBlackK = NiBlackSlider.Value / 1000
        nickK = kSlider.Value / 1000
        sauvolaK = skSlider.Value / 1000
        sauvolaR = rSlider.Value
    End Sub
End Class






Public Class Options_Bernson : Inherits VB_Parent
    Public kernelSize As Integer = 51
    Public bgThreshold As Integer = 100
    Public contrastMin As Integer = 50
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 3, 500, kernelSize)
            sliders.setupTrackBar("Contrast min", 0, 255, contrastMin)
            sliders.setupTrackBar("bg Threshold", 0, 255, bgThreshold)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Kernel Size")
        Static contrastSlider = FindSlider("Contrast min")
        Static bgSlider = FindSlider("bg Threshold")
        kernelSize = kernelSlider.Value Or 1

        bgThreshold = bgSlider.Value
        contrastMin = contrastSlider.Value
    End Sub
End Class







Public Class Options_BlockMatching : Inherits VB_Parent
    Public numDisparity As Integer = 2 * 16
    Public blockSize As Integer = 15
    Public distance As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blockmatch max disparity", 2, 5, numDisparity / 16)
            sliders.setupTrackBar("Blockmatch block size", 5, 255, blockSize)
            sliders.setupTrackBar("Blockmatch distance in meters", 1, 100, distance)
        End If
    End Sub
    Public Sub RunVB()
        Static matchSlider = FindSlider("Blockmatch max disparity")
        Static sizeSlider = FindSlider("Blockmatch block size")
        Static distSlider = FindSlider("Blockmatch distance in meters")
        numDisparity = matchSlider.Value * 16 ' must be a multiple of 16
        blockSize = sizeSlider.Value Or 1
        distance = distSlider.Value
    End Sub
End Class






Public Class Options_Cartoonify : Inherits VB_Parent
    Public medianBlur As Integer = 7
    Public medianBlur2 As Integer = 3
    Public kernelSize As Integer = 5
    Public threshold As Integer = 80
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Cartoon Median Blur kernel", 1, 21, medianBlur)
            sliders.setupTrackBar("Cartoon Median Blur kernel 2", 1, 21, medianBlur2)
            sliders.setupTrackBar("Cartoon threshold", 1, 255, threshold)
            sliders.setupTrackBar("Cartoon Laplacian kernel", 1, 21, kernelSize)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Cartoon Median Blur kernel")
        Static kernel2Slider = FindSlider("Cartoon Median Blur kernel 2")
        Static thresholdSlider = FindSlider("Cartoon threshold")
        Static laplaceSlider = FindSlider("Cartoon Laplacian kernel")
        medianBlur = kernelSlider.Value Or 1
        medianBlur2 = kernel2Slider.Value Or 1
        kernelSize = laplaceSlider.Value Or 1
        threshold = thresholdSlider.Value
    End Sub
End Class






Public Class Options_Dither : Inherits VB_Parent
    Public radioIndex As Integer
    Public bppIndex As Integer = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Bits per color plane (Nbpp only)", 1, 5, bppIndex)
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
        Static bppSlider = FindSlider("Bits per color plane (Nbpp only)")
        bppIndex = bppSlider.Value

        Static frm = FindFrm(traceName + " Radio Buttons")
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







Public Class Options_SymmetricalShapes : Inherits VB_Parent
    Public rotateAngle As Single = 0
    Public fillColor As cv.Scalar = cv.Scalar.Red
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
        Static countSlider = FindSlider("Sample Size")
        Static r1Slider = FindSlider("Radius 1")
        Static r2Slider = FindSlider("Radius 2")
        Static nGenPerSlider = FindSlider("nGenPer")
        Static symCheck = FindCheckBox("Symmetric Ripple")
        Static fillCheck = FindCheckBox("Filled Shapes")
        Static regularCheck = FindCheckBox("Only Regular Shapes")
        Static reverseCheck = FindCheckBox("Reverse In/Out")
        Static demoCheck = FindCheckBox("Use demo mode")

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






Public Class Options_DrawArc : Inherits VB_Parent
    Public saveMargin As Integer = task.WorkingRes.Width / 16
    Public drawFull As Boolean
    Public drawFill As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Clearance from image edge (margin size)", 5, dst2.Width / 8, saveMargin * 16)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Draw Full Ellipse")
            radio.addRadio("Draw Filled Arc")
            radio.addRadio("Draw Arc")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static marginSlider = FindSlider("Clearance from image edge (margin size)")
        Static fillCheck = FindRadio("Draw Filled Arc")
        Static fullCheck = FindRadio("Draw Full Ellipse")
        saveMargin = marginSlider.Value / 16
        drawFull = fullCheck.checked
        drawFill = fillCheck.checked
    End Sub
End Class






Public Class Options_FilterNorm : Inherits VB_Parent
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
        Static alphaSlider = FindSlider("Normalize alpha X10")

        Dim normType = cv.NormTypes.L1
        kernel = New cv.Mat(1, 21, cv.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                normType = Choose(i + 1, cv.NormTypes.INF, cv.NormTypes.L1, cv.NormTypes.L2, cv.NormTypes.MinMax)
                Exit For
            End If
        Next

        kernel = kernel.Normalize(alphaSlider.Value / 10, 0, normType)
    End Sub
End Class





Public Class Options_SepFilter2D : Inherits VB_Parent
    Public xDim As Integer = 5
    Public yDim As Integer = 11
    Public sigma As Single = 17
    Public diffCheck As Boolean
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Show Difference SepFilter2D and Gaussian")
            check.Box(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel X size", 1, 21, xDim)
            sliders.setupTrackBar("Kernel Y size", 1, 21, yDim)
            sliders.setupTrackBar("SepFilter2D Sigma X10", 0, 100, sigma)
        End If
    End Sub
    Public Sub RunVB()
        Static xSlider = FindSlider("Kernel X size")
        Static ySlider = FindSlider("Kernel Y size")
        Static sigmaSlider = FindSlider("SepFilter2D Sigma X10")
        Static diffCheckBox = FindCheckBox("Show Difference SepFilter2D and Gaussian")
        xDim = xSlider.Value Or 1
        yDim = ySlider.Value Or 1
        sigma = sigmaSlider.Value / 10
        diffCheck = diffCheckBox.checked
    End Sub
End Class






Public Class Options_IMUFrameTime : Inherits VB_Parent
    Public minDelayIMU As Integer = 4
    Public minDelayHost As Integer = 4
    Public plotLastX As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum Host interrupt delay (ms)", 1, 10, minDelayIMU)
            sliders.setupTrackBar("Minimum IMU to Capture time (ms)", 1, 10, minDelayHost)
            sliders.setupTrackBar("Number of Plot Values", 5, 30, plotLastX)
        End If
    End Sub
    Public Sub RunVB()
        Static minSliderHost = FindSlider("Minimum Host interrupt delay (ms)")
        Static minSliderIMU = FindSlider("Minimum IMU to Capture time (ms)")
        Static plotSlider = FindSlider("Number of Plot Values")
        minDelayIMU = minSliderIMU.Value
        minDelayHost = minSliderHost.Value
        plotLastX = plotSlider.Value
    End Sub
End Class






Public Class Options_KLT : Inherits VB_Parent
    Public inputPoints() As cv.Point2f
    Public maxCorners As Integer = 100
    Public qualityLevel As Single = 0.01
    Public minDistance As Integer = 7
    Public blockSize As Integer = 7
    Public nightMode As Boolean
    Public subPixWinSize As New cv.Size(10, 10)
    Public winSize As New cv.Size(3, 3)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KLT - MaxCorners", 1, 200, maxCorners)
            sliders.setupTrackBar("KLT - qualityLevel", 1, 100, qualityLevel * 100) ' low quality!  We want lots of points.
            sliders.setupTrackBar("KLT - minDistance", 1, 100, minDistance)
            sliders.setupTrackBar("KLT - BlockSize", 1, 100, blockSize)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("KLT - Night Mode")
            check.addCheckBox("KLT - delete all Points")
        End If
    End Sub
    Public Sub RunVB()
        Static maxSlider = FindSlider("KLT - MaxCorners")
        Static qualitySlider = FindSlider("KLT - qualityLevel")
        Static minSlider = FindSlider("KLT - minDistance")
        Static blockSlider = FindSlider("KLT - BlockSize")
        Static nightCheck = FindCheckBox("KLT - Night Mode")
        Static deleteCheck = FindCheckBox("KLT - delete all Points")

        If deleteCheck.Checked Or task.heartBeat Then
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







Public Class Options_Laplacian : Inherits VB_Parent
    Public kernel As New cv.Size(3, 3)
    Public scale As Single = 1
    Public delta As Single = 0
    Public gaussianBlur As Boolean
    Public boxFilterBlur As Boolean
    Public threshold As Integer = 15
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Laplacian Kernel size", 1, 21, kernel.Width)
            sliders.setupTrackBar("Laplacian Scale", 0, 100, scale * 100)
            sliders.setupTrackBar("Laplacian Delta", 0, 1000, delta * 100)
            sliders.setupTrackBar("Laplacian Threshold", 0, 100, threshold)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Add Gaussian Blur")
            radio.addRadio("Add boxfilter Blur")
            radio.addRadio("Add median Blur")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Laplacian Kernel size")
        Static scaleSlider = FindSlider("Laplacian Scale")
        Static deltaSlider = FindSlider("Laplacian Delta")
        Static thresholdSlider = FindSlider("Laplacian Threshold")
        Static blurCheck = FindRadio("Add Gaussian Blur")
        Static boxCheck = FindRadio("Add boxfilter Blur")
        Dim kernelSize As Integer = kernelSlider.Value Or 1
        scale = scaleSlider.Value / 100
        delta = deltaSlider.Value / 100
        kernel = New cv.Size(kernelSize, kernelSize)
        gaussianBlur = blurCheck.checked
        boxFilterBlur = boxCheck.checked
        threshold = thresholdSlider.value
    End Sub
End Class








Public Class Options_OpticalFlow : Inherits VB_Parent
    Public pyrScale As Single = 35 / 100
    Public levels As Integer = 1
    Public winSize As Integer = 1
    Public iterations As Integer = 1
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
            sliders.setupTrackBar("Optical Flow pyrScale", 1, 100, pyrScale * 100)
            sliders.setupTrackBar("Optical Flow Levels", 1, 10, levels)
            sliders.setupTrackBar("Optical Flow winSize", 1, 9, winSize)
            sliders.setupTrackBar("Optical Flow Iterations", 1, 10, iterations)
            sliders.setupTrackBar("Optical Flow PolyN", 1, 15, 5)
            sliders.setupTrackBar("Optical Flow Scaling Output", 1, 100, 50)
        End If
    End Sub
    Public Sub RunVB()
        Static scaleSlider = FindSlider("Optical Flow pyrScale")
        Static levelSlider = FindSlider("Optical Flow Levels")
        Static sizeSlider = FindSlider("Optical Flow winSize")
        Static iterSlider = FindSlider("Optical Flow Iterations")
        Static polySlider = FindSlider("Optical Flow PolyN")
        Static flowSlider = FindSlider("Optical Flow Scaling Out")

        pyrScale = scaleSlider.Value / scaleSlider.Maximum
        levels = levelSlider.Value
        winSize = sizeSlider.Value Or 1
        iterations = iterSlider.Value
        polyN = polySlider.Value Or 1
        polySigma = 1.5
        If polyN <= 5 Then polySigma = 1.1

        Static frm = FindFrm(traceName + " Radio Buttons")
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






Public Class Options_OpticalFlowSparse : Inherits VB_Parent
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
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                OpticalFlowFlag = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Quaternion : Inherits VB_Parent
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
        Static axSlider = FindSlider("quaternion A.x X100")
        Static aySlider = FindSlider("quaternion A.y X100")
        Static azSlider = FindSlider("quaternion A.z X100")
        Static athetaSlider = FindSlider("quaternion A Theta X100")

        Static bxSlider = FindSlider("quaternion B.x X100")
        Static bySlider = FindSlider("quaternion B.y X100")
        Static bzSlider = FindSlider("quaternion B.z X100")
        Static bthetaSlider = FindSlider("quaternion B Theta X100")

        q1 = New Quaternion(CSng(axSlider.Value / 100), CSng(aySlider.Value / 100),
                                CSng(azSlider.Value / 100), CSng(athetaSlider.Value / 100))
        q2 = New Quaternion(CSng(bxSlider.Value / 100), CSng(bySlider.Value / 100),
                                    CSng(bzSlider.Value / 100), CSng(bthetaSlider.Value / 100))
    End Sub
End Class






Public Class Options_XPhoto : Inherits VB_Parent
    Public colorCode As Integer = cv.ColorConversionCodes.BGR2GRAY
    Public dynamicRatio As Integer
    Public blockSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("XPhoto Dynamic Ratio", 1, 127, 7)
            sliders.setupTrackBar("XPhoto Block Size", 1, 100, 3)
        End If
        If radio.Setup(traceName) Then
            radio.addRadio("getGrayInput")
            radio.addRadio("BGR2HSV")
            radio.addRadio("BGR2YUV")
            radio.addRadio("BGR2XYZ")
            radio.addRadio("BGR2Lab")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static ratioSlider = FindSlider("XPhoto Dynamic Ratio")
        Static sizeSlider = FindSlider("XPhoto Block Size")
        dynamicRatio = ratioSlider.Value
        blockSize = sizeSlider.Value
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                colorCode = Choose(i + 1, cv.ColorConversionCodes.BGR2GRAY, cv.ColorConversionCodes.BGR2HSV, cv.ColorConversionCodes.BGR2YUV,
                                          cv.ColorConversionCodes.BGR2XYZ, cv.ColorConversionCodes.BGR2Lab)
                Exit For
            End If
        Next
    End Sub
End Class







Public Class Options_InPaint : Inherits VB_Parent
    Public telea As Boolean
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("TELEA")
            radio.addRadio("Navier-Stokes")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static teleaRadio = FindRadio("TELEA")
        telea = teleaRadio.checked
    End Sub
End Class










Public Class Options_RotatePoly : Inherits VB_Parent
    Public changeCheck As Windows.Forms.CheckBox
    Public angleSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Amount to rotate triangle", -180, 180, 10)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Change center of rotation and triangle")
        End If

        angleSlider = FindSlider("Amount to rotate triangle")
        changeCheck = FindCheckBox("Change center of rotation and triangle")
    End Sub
    Public Sub RunVB()
    End Sub
End Class












Public Class Options_FPoly : Inherits VB_Parent
    Public removeThreshold As Integer = 4
    Public autoResyncAfterX As Integer = 500
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resync if feature moves > X pixels", 1, 20, removeThreshold)
            sliders.setupTrackBar("Points to use in Feature Poly", 3, 20, 10)
            sliders.setupTrackBar("Automatically resync after X frames", 10, 1000, autoResyncAfterX)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Static pointSlider = FindSlider("Points to use in Feature Poly")
        Static resyncSlider = FindSlider("Automatically resync after X frames")
        removeThreshold = thresholdSlider.Value
        task.polyCount = pointSlider.Value
        autoResyncAfterX = resyncSlider.Value
    End Sub
End Class













Public Class Options_Homography : Inherits VB_Parent
    Public hMethod = cv.HomographyMethods.None
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
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
        Static frm = FindFrm(traceName + " Radio Buttons")
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












Public Class Options_Random : Inherits VB_Parent
    Public count As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Pixel Count", 1, dst2.Cols * dst2.Rows, 20)
    End Sub
    Public Sub RunVB()
        Static countSlider = FindSlider("Random Pixel Count")
        count = countSlider.value
    End Sub
End Class








Public Class Options_Hough : Inherits VB_Parent
    Public rho As Integer = 1
    Public theta As Single = 1000 * Math.PI / 180
    Public threshold As Integer = 3
    Public lineCount As Integer = 25
    Public relativeIntensity As Single = 90 / 1000
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Relative Intensity (Accord)", 1, 100, relativeIntensity * 1000)
            sliders.setupTrackBar("Hough rho", 1, 100, rho)
            sliders.setupTrackBar("Hough theta", 1, 1000, theta * 1000)
            sliders.setupTrackBar("Hough threshold", 1, 100, threshold)
            sliders.setupTrackBar("Lines to Plot", 1, 1000, lineCount)
            sliders.setupTrackBar("Minimum feature pixels", 0, 250, 25)
        End If
    End Sub
    Public Sub RunVB()
        Static rhoslider = FindSlider("Hough rho")
        Static thetaSlider = FindSlider("Hough theta")
        Static thresholdSlider = FindSlider("Hough threshold")
        Static lineSlider = FindSlider("Lines to Plot")
        Static relativeSlider = FindSlider("Relative Intensity (Accord)")

        rho = rhoslider.Value
        theta = thetaSlider.Value / 1000
        threshold = thresholdSlider.Value
        lineCount = lineSlider.Value
        relativeIntensity = relativeSlider.Value / 100
    End Sub
End Class







Public Class Options_Canny : Inherits VB_Parent
    Public threshold1 As Integer = 100
    Public threshold2 As Integer = 150
    Public aperture As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Canny threshold1", 1, 255, threshold1)
            sliders.setupTrackBar("Canny threshold2", 1, 255, threshold2)
            sliders.setupTrackBar("Canny Aperture", 3, 7, aperture)
        End If
    End Sub
    Public Sub RunVB()
        Static t1Slider = FindSlider("Canny threshold1")
        Static t2Slider = FindSlider("Canny threshold2")
        Static apertureSlider = FindSlider("Canny Aperture")

        threshold1 = t1Slider.Value
        threshold2 = t2Slider.Value
        aperture = apertureSlider.Value Or 1
    End Sub
End Class







Public Class Options_ColorMatch : Inherits VB_Parent
    Public maxDistanceCheck As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show Max Distance point")
        End If
    End Sub
    Public Sub RunVB()
        Static maxCheck = FindCheckBox("Show Max Distance point")
        maxDistanceCheck = maxCheck.checked
    End Sub
End Class









Public Class Options_Sort : Inherits VB_Parent
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

        radio0 = FindRadio("EveryColumn, Ascending")
        radio1 = FindRadio("EveryColumn, Descending")
        radio2 = FindRadio("EveryRow, Ascending")
        radio3 = FindRadio("EveryRow, Descending")
        radio4 = FindRadio("Sort all pixels ascending")
        radio5 = FindRadio("Sort all pixels descending")
    End Sub
    Public Sub RunVB()
        If radio1.Checked Then sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Descending
        If radio2.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Ascending
        If radio3.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Descending
    End Sub
End Class






Public Class Options_Distance : Inherits VB_Parent
    Public distanceType As cv.DistanceTypes
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("C")
            radio.addRadio("L1")
            radio.addRadio("L2")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static cRadio = FindRadio("C")
        Static l1Radio = FindRadio("L1")
        Static l2Radio = FindRadio("L2")
        If cRadio.Checked Then distanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then distanceType = cv.DistanceTypes.L1
        If l2Radio.Checked Then distanceType = cv.DistanceTypes.L2
    End Sub
End Class









Public Class Options_Warp : Inherits VB_Parent
    Public alpha As Double
    Public beta As Double
    Public gamma As Double
    Public f As Double
    Public distance As Double
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
        Static alphaSlider = FindSlider("Alpha")
        Static betaSlider = FindSlider("Beta")
        Static gammaSlider = FindSlider("Gamma")
        Static fSlider = FindSlider("f")
        Static distanceSlider = FindSlider("distance")

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








Public Class Options_HistCompare : Inherits VB_Parent
    Public compareMethod As cv.HistCompMethods = cv.HistCompMethods.Correl
    Public compareName As String
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
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
        Static correlationRadio = FindRadio("Compare using Correlation")
        Static chiRadio = FindRadio("Compare using Chi-squared")
        Static intersectionRadio = FindRadio("Compare using intersection")
        Static bhattRadio = FindRadio("Compare using Bhattacharyya")
        Static chiAltRadio = FindRadio("Compare using Chi-squared Alt")
        Static hellingerRadio = FindRadio("Compare using Hellinger")
        Static kldivRadio = FindRadio("Compare using KLDiv")

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












Public Class Options_MatchCell : Inherits VB_Parent
    Public overlapPercent As Single = 0.5
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent overlap", 0, 100, overlapPercent * 100)
    End Sub
    Public Sub RunVB()
        Static overlapSlider = FindSlider("Percent overlap")
        overlapPercent = overlapSlider.value / 100
    End Sub
End Class








Public Class Options_Extrinsics : Inherits VB_Parent
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
        Static leftSlider = FindSlider("Left image percent")
        Static rightSlider = FindSlider("Right image percent")
        Static heightSlider = FindSlider("Height percent")
        leftCorner = dst2.Width * leftSlider.value / 100
        rightCorner = dst2.Width * rightSlider.value / 100
        topCorner = dst2.Height * heightSlider.value / 100
    End Sub
End Class







Public Class Options_Translation : Inherits VB_Parent
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
        Static leftSlider = FindSlider("Left translation percent")
        Static rightSlider = FindSlider("Right translation percent")
        leftTrans = dst2.Width * leftSlider.value / 100
        rightTrans = dst2.Width * rightSlider.value / 100
    End Sub
End Class












Public Class Options_OpenGL_Contours : Inherits VB_Parent
    Public depthPointStyle As Integer
    Public filterThreshold As Single = 0.3
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Unfiltered depth points")
            radio.addRadio("Filtered depth points")
            radio.addRadio("Flatten depth points")
            radio.addRadio("Flatten and filter depth points")
            radio.check(3).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter threshold in meters X100", 0, 100, filterThreshold * 100)
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Filter threshold in meters X100")
        Static unFilteredRadio = FindRadio("Unfiltered depth points")
        Static filteredRadio = FindRadio("Filtered depth points")
        Static flattenRadio = FindRadio("Flatten depth points")
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











Public Class Options_Motion : Inherits VB_Parent
    Public motionThreshold As Integer
    Public cumulativePercentThreshold As Single = 0.1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Single frame motion threshold", 1, dst2.Total / 4, dst2.Total / 16)
            sliders.setupTrackBar("Cumulative motion threshold percent of image", 1, 100, cumulativePercentThreshold * 100)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Single frame motion threshold")
        Static percentSlider = FindSlider("Cumulative motion threshold percent of image")
        motionThreshold = thresholdSlider.value
        cumulativePercentThreshold = percentSlider.value / 100
    End Sub
End Class








Public Class Options_Emax : Inherits VB_Parent
    Public predictionStepSize As Integer = 5
    Public consistentcolors As Boolean
    Public covarianceType = cv.EMTypes.CovMatDefault
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("EMax Prediction Step Size", 1, 20, predictionStepSize)

        If radio.Setup(traceName) Then
            radio.addRadio("EMax matrix type Spherical")
            radio.addRadio("EMax matrix type Diagonal")
            radio.addRadio("EMax matrix type Generic")
            radio.check(0).Checked = True
        End If

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use palette to keep colors consistent")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static colorCheck = FindCheckBox("Use palette to keep colors consistent")
        Static stepSlider = FindSlider("EMax Prediction Step Size")
        predictionStepSize = stepSlider.value
        covarianceType = cv.EMTypes.CovMatDefault
        consistentcolors = colorCheck.checked
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                covarianceType = Choose(i + 1, cv.EMTypes.CovMatSpherical, cv.EMTypes.CovMatDiagonal, cv.EMTypes.CovMatGeneric)
            End If
        Next
    End Sub
End Class









Public Class Options_Intercepts : Inherits VB_Parent
    Public interceptRange As Integer = 10
    Public mouseMovePoint As Integer
    Public selectedIntercept As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Intercept width range in pixels", 1, 50, interceptRange)
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
        Static interceptSlider = FindSlider("Intercept width range in pixels")
        interceptRange = interceptSlider.Value

        Static topRadio = FindRadio("Show Top intercepts")
        Static botRadio = FindRadio("Show Bottom intercepts")
        Static leftRadio = FindRadio("Show Left intercepts")
        Static rightRadio = FindRadio("Show Right intercepts")

        For selectedIntercept = 0 To 3
            mouseMovePoint = Choose(selectedIntercept + 1, task.mouseMovePoint.X, task.mouseMovePoint.X, task.mouseMovePoint.Y, task.mouseMovePoint.Y)
            If Choose(selectedIntercept + 1, topRadio, botRadio, leftRadio, rightRadio).checked Then Exit For
        Next
    End Sub
End Class






Public Class Options_PlaneEstimation : Inherits VB_Parent
    Public useDiagonalLines As Boolean
    Public useContour_SidePoints As Boolean
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use diagonal lines")
            radio.addRadio("Use horizontal And vertical lines")
            radio.addRadio("Use Contour_SidePoints to find the line pair")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static diagonalRadio = FindRadio("Use diagonal lines")
        Static sidePointsRadio = FindRadio("Use Contour_SidePoints to find the line pair")
        useDiagonalLines = diagonalRadio.checked
        useContour_SidePoints = sidePointsRadio.checked
    End Sub
End Class







Public Class Options_ForeGround : Inherits VB_Parent
    Public maxForegroundDepthInMeters As Single = 1500 / 1000
    Public minSizeContour As Integer = 100
    Public depthPerRegion As Single
    Public numberOfRegions As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max foreground depth in mm's", 200, 2000, maxForegroundDepthInMeters * 1000)
            sliders.setupTrackBar("Min length contour", 10, 2000, minSizeContour)
            sliders.setupTrackBar("Number of depth ranges", 1, 20, numberOfRegions)
        End If
    End Sub
    Public Sub RunVB()
        Static depthSlider = FindSlider("Max foreground depth in mm's")
        Static minSizeSlider = FindSlider("Min length contour")
        Static regionSlider = FindSlider("Number of depth ranges")
        maxForegroundDepthInMeters = depthSlider.value / 1000
        minSizeContour = minSizeSlider.value
        numberOfRegions = regionSlider.value
        depthPerRegion = task.MaxZmeters / numberOfRegions
    End Sub
End Class






Public Class Options_Flood : Inherits VB_Parent
    Public floodFlag As cv.FloodFillFlags = 4 Or cv.FloodFillFlags.FixedRange
    Public stepSize As Integer = 30
    Public minPixels As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Pixels", 1, 2000, 30)
            sliders.setupTrackBar("Step Size", 1, dst2.Cols / 2, stepSize)
        End If
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use floating range")
            check.addCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        End If
    End Sub
    Public Sub RunVB()
        Static floatingCheck = FindCheckBox("Use floating range")
        Static connectCheck = FindCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        Static stepSlider = FindSlider("Step Size")
        Static minSlider = FindSlider("Min Pixels")

        stepSize = stepSlider.Value
        floodFlag = If(connectCheck.checked, 8, 4) Or If(floatingCheck.checked, cv.FloodFillFlags.FixedRange, 0)
        minPixels = minSlider.value
    End Sub
End Class






Public Class Options_ShapeDetect : Inherits VB_Parent
    Public fileName As String
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("coins.jpg")
            radio.addRadio("demo.jpg")
            radio.addRadio("demo1.jpg")
            radio.addRadio("demo2.jpg")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        fileName = findRadioText(frm.check)
    End Sub
End Class








Public Class Options_Blur : Inherits VB_Parent
    Public kernelSize As Integer = 3
    Public sigma As Single = 1.5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blur Kernel Size", 0, 32, kernelSize)
            sliders.setupTrackBar("Blur Sigma", 1, 10, sigma * 2)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("Blur Kernel Size")
        Static sigmaSlider = FindSlider("Blur Sigma")
        kernelSize = kernelSlider.Value Or 1
        sigma = sigmaSlider.value * 0.5
    End Sub
End Class









Public Class Options_Wavelet : Inherits VB_Parent
    Public useHaar As Boolean
    Public iterations As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Wavelet Iterations", 1, 5, iterations)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Haar")
            radio.addRadio("CDF")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = FindSlider("Wavelet Iterations")
        Static haarRadio = FindRadio("Haar")
        useHaar = haarRadio.checked
        iterations = iterSlider.value
    End Sub
End Class









Public Class Options_SOM : Inherits VB_Parent
    Public iterations As Integer = 3000
    Public learningRate As Single = 0.1
    Public radius As Integer = 15
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Iterations (000's)", 1, 10, iterations / 1000)
            sliders.setupTrackBar("Initial Learning Rate %", 1, 100, learningRate * 100)
            sliders.setupTrackBar("Radius in Pixels", 1, 100, radius)
        End If
    End Sub
    Public Sub RunVB()
        Static iterSlider = FindSlider("Iterations (000's)")
        Static learnSlider = FindSlider("Initial Learning Rate %")
        Static radiusSlider = FindSlider("Radius in Pixels")
        iterations = iterSlider.value * 1000
        learningRate = learnSlider.value / 100
        radius = radiusSlider.value
    End Sub
End Class









Public Class Options_SURF : Inherits VB_Parent
    Public hessianThreshold As Integer
    Public useBFMatcher As Boolean
    Public verticalRange As Integer
    Public pointCount As Integer = 200
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use BF Matcher")
            radio.addRadio("Use Flann Matcher")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Hessian threshold", 1, 5000, 2000)
            sliders.setupTrackBar("Surf Vertical Range to Search", 0, 50, 1)
            sliders.setupTrackBar("Points to Match", 1, 1000, pointCount)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Hessian threshold")
        Static BFRadio = FindRadio("Use BF Matcher")
        Static rangeSlider = FindSlider("Surf Vertical Range to Search")
        Static countSlider = FindSlider("Points to Match")

        useBFMatcher = BFRadio.checked
        hessianThreshold = thresholdSlider.value
        verticalRange = rangeSlider.value
        pointCount = countSlider.value
    End Sub
End Class








Public Class Options_Sift : Inherits VB_Parent
    Public useBFMatcher As Boolean
    Public pointCount As Integer = 200
    Public stepSize As Integer = 10
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use BF Matcher")
            radio.addRadio("Use Flann Matcher")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Points to Match", 1, 1000, pointCount)
            sliders.setupTrackBar("Sift StepSize", 1, 50, stepSize)
        End If
    End Sub
    Public Sub RunVB()
        Static bfRadio = FindRadio("Use BF Matcher")
        Static countSlider = FindSlider("Points to Match")
        Static stepSlider = FindSlider("Sift StepSize")

        useBFMatcher = bfRadio.checked
        pointCount = countSlider.value
        stepSize = stepSlider.value
    End Sub
End Class







Public Class Options_Dilate : Inherits VB_Parent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cv.MorphShapes
    Public element As cv.Mat
    Public noshape As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Dilate Kernel Size", 1, 32, kernelSize)
            sliders.setupTrackBar("Dilate Iterations", 0, 32, iterations)
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
        Static ellipseRadio = FindRadio("Dilate shape: Ellipse")
        Static rectRadio = FindRadio("Dilate shape: Rect")
        Static iterSlider = FindSlider("Dilate Iterations")
        Static kernelSlider = FindSlider("Dilate Kernel Size")
        Static noShapeRadio = FindRadio("Dilate shape: None")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cv.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cv.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cv.MorphShapes.Rect
        element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
    End Sub
End Class









Public Class Options_KMeans : Inherits VB_Parent
    Public kMeansFlag As cv.KMeansFlags
    Public kMeansK As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("KMeans k", 2, 32, kMeansK)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use PpCenters")
            radio.addRadio("Use RandomCenters")
            radio.addRadio("Use Initialized Labels")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub setK(k As Integer)
        FindSlider("KMeans k").Value = k
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static kSlider = FindSlider("KMeans k")
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








Public Class Options_LUT : Inherits VB_Parent
    Public lutSegments As Integer = 10
    Public splits() As Integer
    Public vals() As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of LUT Segments", 2, 255, lutSegments)
            sliders.setupTrackBar("LUT zero through xxx", 1, 255, 65)
            sliders.setupTrackBar("LUT xxx through yyy", 1, 255, 110)
            sliders.setupTrackBar("LUT yyy through zzz", 1, 255, 160)
            sliders.setupTrackBar("LUT zzz through 255", 1, 255, 210)
        End If

        desc = "Options for LUT algorithms"
    End Sub
    Public Sub RunVB()
        Static lutSlider = FindSlider("Number of LUT Segments")
        Static zeroSlider = FindSlider("LUT zero through xxx")
        Static xSlider = FindSlider("LUT xxx through yyy")
        Static ySlider = FindSlider("LUT yyy through zzz")
        Static zSlider = FindSlider("LUT zzz through 255")

        splits = {zeroSlider.Value, xSlider.Value, ySlider.Value, zSlider.Value, 255}
        vals = {1, zeroSlider.Value, xSlider.Value, ySlider.Value, 255}
        lutSegments = lutSlider.value
    End Sub
End Class








Public Class Options_WarpModel2 : Inherits VB_Parent
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
        If standaloneTest() Then
            SetTrueText("Options for the WarpModel algorithms.  No output when run standaloneTest().")
            Exit Sub
        End If

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then warpMode = i
        Next
        useWarpAffine = warpMode = 2
        useWarpHomography = warpMode = 3
    End Sub
End Class









' https://github.com/spmallick/learnopencv/tree/master/
Public Class Options_Photoshop : Inherits VB_Parent
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
        Static frm = FindFrm(traceName + " Radio Buttons")
        For switch = 0 To frm.check.Count - 1
            If frm.check(switch).Checked Then Exit For
        Next
    End Sub
End Class





Public Class Options_Gif : Inherits VB_Parent
    Public buildCheck As Windows.Forms.CheckBox
    Public restartCheck As Windows.Forms.CheckBox
    Public dst0Radio As Windows.Forms.RadioButton
    Public dst1Radio As Windows.Forms.RadioButton
    Public dst2Radio As Windows.Forms.RadioButton
    Public dst3Radio As Windows.Forms.RadioButton
    Public OpenCVBwindow As Windows.Forms.RadioButton
    Public OpenGLwindow As Windows.Forms.RadioButton
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Check this box when Gif_Basics dst2 contains the desired snapshot.")
            check.addCheckBox("Build GIF file in <OpenCVB Home Directory>\Temp\myGIF.gif")
            check.addCheckBox("Restart - clear all previous images.")
        End If

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Capture dst0")
            radio.addRadio("Capture dst1")
            radio.addRadio("Capture dst2")
            radio.addRadio("Capture dst3")
            radio.addRadio("Capture entire OpenCVB window")
            radio.addRadio("Capture OpenGL window")
            radio.check(4).Checked = True
        End If
        buildCheck = FindCheckBox("Build GIF file in <OpenCVB Home Directory>\Temp\myGIF.gif")
        restartCheck = FindCheckBox("Restart - clear all previous images.")

        dst0Radio = FindRadio("Capture dst0")
        dst1Radio = FindRadio("Capture dst1")
        dst2Radio = FindRadio("Capture dst2")
        dst3Radio = FindRadio("Capture dst3")
        OpenCVBwindow = FindRadio("Capture entire OpenCVB window")
        OpenGLwindow = FindRadio("Capture OpenGL window")
    End Sub
    Public Sub RunVB()
        Static frmCheck = FindFrm(traceName + " CheckBoxes")
        Static frmRadio = FindFrm(traceName + " Radio Buttons")
        If task.FirstPass Then
            Static myFrameCount As Integer = 0
            myFrameCount += 1
            frmCheck.Left = task.gOptions.Width / 2
            frmCheck.top = task.gOptions.Height / 2
            frmRadio.left = task.gOptions.Width * 2 / 3
            frmRadio.top = task.gOptions.Height * 2 / 3
        End If

        If dst0Radio.Checked Then task.gifCaptureIndex = 0
        If dst1Radio.Checked Then task.gifCaptureIndex = 1
        If dst2Radio.Checked Then task.gifCaptureIndex = 2
        If dst3Radio.Checked Then task.gifCaptureIndex = 3
        If OpenCVBwindow.Checked Then task.gifCaptureIndex = 4
        If OpenGLwindow.Checked Then task.gifCaptureIndex = 5

        task.gifBuild = buildCheck.Checked

        buildCheck.Checked = False
        restartCheck.Checked = False

        task.optionsChanged = False
    End Sub
End Class







Public Class Options_IMU : Inherits VB_Parent
    Public rotateX As Integer
    Public rotateY As Integer
    Public rotateZ As Integer
    Public stableThreshold As Single = 0.02
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Rotate pointcloud around X-axis (degrees)", -90, 90, rotateX)
            sliders.setupTrackBar("Rotate pointcloud around Y-axis (degrees)", -90, 90, rotateY)
            sliders.setupTrackBar("Rotate pointcloud around Z-axis (degrees)", -90, 90, rotateZ)
            sliders.setupTrackBar("IMU_Basics: Alpha X100", 0, 100, task.IMU_AlphaFilter * 100)
            sliders.setupTrackBar("IMU Stability Threshold (radians) X100", 0, 100, stableThreshold * 100)
        End If
    End Sub
    Public Sub RunVB()
        Static xRotateSlider = FindSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = FindSlider("Rotate pointcloud around Z-axis (degrees)")
        Static alphaSlider = FindSlider("IMU_Basics: Alpha X100")
        Static stabilitySlider = FindSlider("IMU Stability Threshold (radians) X100")
        rotateX = xRotateSlider.value
        rotateY = yRotateSlider.value
        rotateZ = zRotateSlider.value
        task.IMU_AlphaFilter = alphaSlider.value / 100
        stableThreshold = stabilitySlider.value / 100
    End Sub
End Class





Public Class Options_FeatureMatch : Inherits VB_Parent
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
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
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





Public Class Options_HeatMap : Inherits VB_Parent
    Public redThreshold As Integer = 20
    Public viewName As String = "vertical"
    Public showHistory As Boolean
    Public topView As Boolean = True
    Public sideView As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Top View (Unchecked Side View)")
            check.addCheckBox("Slice Vertically (Unchecked Slice Horizontally)")
            check.Box(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for Red channel", 0, 255, redThreshold)
        End If
    End Sub
    Public Sub RunVB()
        Static topCheck = FindCheckBox("Top View (Unchecked Side View)")
        Static redSlider = FindSlider("Threshold for Red channel")

        redThreshold = redSlider.value

        topView = topCheck.checked
        sideView = Not topView
        If sideView Then viewName = "horizontal"
    End Sub
End Class






Public Class Options_Boundary : Inherits VB_Parent
    Public desiredBoundaries As Integer = 15
    Public peakDistance As Integer = task.WorkingRes.Width / 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired boundary count", 2, 100, desiredBoundaries)
            sliders.setupTrackBar("Distance to next Peak (pixels)", 2, dst2.Width / 10, peakDistance)
        End If
    End Sub
    Public Sub RunVB()
        Static boundarySlider = FindSlider("Desired boundary count")
        Static distSlider = FindSlider("Distance to next Peak (pixels)")
        desiredBoundaries = boundarySlider.value
        peakDistance = distSlider.value
    End Sub
End Class









Public Class Options_Denoise : Inherits VB_Parent
    Public removeSinglePixels As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Remove single pixels")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static singleCheck = FindCheckBox("Remove single pixels")
        removeSinglePixels = singleCheck.checked
    End Sub
End Class









'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class Options_MSER : Inherits VB_Parent
    Public delta As Integer = 9
    Public minArea As Integer
    Public maxArea As Integer
    Public maxVariation As Single = 0.25
    Public minDiversity As Single = 0.2
    Public maxEvolution As Integer = 200
    Public areaThreshold As Single = 1.01
    Public minMargin As Single = 0.003
    Public edgeBlurSize As Integer = 5
    Public pass2Setting As Boolean
    Public graySetting As Boolean
    Public Sub New()
        Select Case task.WorkingRes.Width
            Case 1920
                maxArea = 350000
                minArea = 6000
            Case 960
                maxArea = 100000
                minArea = 3000
            Case 480
                maxArea = 25000
                minArea = 1500
            Case 1280
                maxArea = 150000
                minArea = 1500
            Case 640
                maxArea = 50000
                minArea = 600
            Case 320
                maxArea = 5000
                minArea = 300
            Case 672
                maxArea = 30000
                minArea = 300
            Case 336
                maxArea = 15000
                minArea = 200
            Case 168
                maxArea = 2000
                minArea = 10
        End Select
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("MSER Delta", 1, 100, delta)
            sliders.setupTrackBar("MSER Min Area", 1, 10000, minArea)
            sliders.setupTrackBar("MSER Max Area", 1000, 500000, maxArea)
            sliders.setupTrackBar("MSER Max Variation", 1, 100, maxVariation * 100)
            sliders.setupTrackBar("MSER Diversity", 0, 100, minDiversity)
            sliders.setupTrackBar("MSER Max Evolution", 1, 1000, maxEvolution)
            sliders.setupTrackBar("MSER Area Threshold", 1, 101, areaThreshold * 100)
            sliders.setupTrackBar("MSER Min Margin", 1, 100, minMargin * 1000)
            sliders.setupTrackBar("MSER Edge BlurSize", 1, 20, edgeBlurSize)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Pass2Only")
            check.addCheckBox("Use grayscale input")
        End If
    End Sub
    Public Sub RunVB()
        Static deltaSlider = FindSlider("MSER Delta")
        Static minAreaSlider = FindSlider("MSER Min Area")
        Static maxAreaSlider = FindSlider("MSER Max Area")
        Static variationSlider = FindSlider("MSER Max Variation")
        Static diversitySlider = FindSlider("MSER Diversity")
        Static evolutionSlider = FindSlider("MSER Max Evolution")
        Static thresholdSlider = FindSlider("MSER Area Threshold")
        Static marginSlider = FindSlider("MSER Min Margin")
        Static blurSlider = FindSlider("MSER Edge BlurSize")

        delta = deltaSlider.Value
        minArea = minAreaSlider.Value
        maxArea = maxAreaSlider.Value
        maxVariation = variationSlider.Value / 100
        minDiversity = diversitySlider.Value / 100
        maxEvolution = evolutionSlider.Value
        areaThreshold = thresholdSlider.Value / 100
        minMargin = marginSlider.Value / 1000
        edgeBlurSize = blurSlider.Value Or 1

        Static pass2Check = FindCheckBox("Pass2Only")
        Static grayCheck = FindCheckBox("Use grayscale input")
        pass2Setting = pass2Check.checked
        graySetting = grayCheck.checked
    End Sub
End Class






Public Class Options_Spectrum : Inherits VB_Parent
    Public gapDepth As Integer
    Public gapGray As Integer
    Public sampleThreshold As Integer
    Public redC As New RedCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gap in depth spectrum (cm's)", 1, 50, 1)
            sliders.setupTrackBar("Gap in gray spectrum", 1, 50, 1)
            sliders.setupTrackBar("Sample count threshold", 1, 50, 10)
        End If
    End Sub
    Public Function runRedCloud(ByRef label As String) As cv.Mat
        redC.Run(task.color)
        label = redC.labels(2)
        Return redC.dst2
    End Function
    Public Function buildDepthRanges(input As cv.Mat, typeSpec As String)
        Dim ranges As New List(Of rangeData)
        Dim sorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger) ' the spectrum of the values 
        Dim pixels As New List(Of Integer)
        Dim counts As New List(Of Integer)

        Dim rc = task.rc
        Dim mask = rc.mask.Clone
        mask.SetTo(0, task.noDepthMask(rc.rect))
        For y = 0 To input.Height - 1
            For x = 0 To input.Width - 1
                If mask.Get(Of Byte)(y, x) > 0 Then
                    Dim val = input.Get(Of Single)(y, x)
                    If val > 0 And val < 100 Then
                        Dim nextVal = CInt(val * 100) ' * 100 to convert to integer and cm's
                        Dim index = pixels.IndexOf(nextVal)
                        If index >= 0 Then
                            counts(index) += 1
                        Else
                            pixels.Add(nextVal)
                            counts.Add(1)
                        End If
                    End If
                End If
            Next
        Next

        Dim totalCount As Integer
        For i = 0 To pixels.Count - 1
            sorted.Add(pixels(i), counts(i))
            totalCount += counts(i)
        Next

        strOut = "For the selected " + typeSpec + " cell:" + vbCrLf

        If sorted.Count = 0 Then
            strOut = "There is no depth data for that cell."
            Return ranges
        End If

        Dim rStart As Integer = sorted.ElementAt(0).Key
        Dim rEnd As Integer = rStart
        Dim count As Integer = sorted.ElementAt(0).Value
        Dim trimCount As Integer
        For i = 0 To sorted.Count - 2
            If sorted.ElementAt(i + 1).Key - sorted.ElementAt(i).Key > gapDepth Then
                rEnd = sorted.ElementAt(i).Key
                If count > sampleThreshold Then
                    ranges.Add(New rangeData(rc.index, rStart, rEnd, count))
                    strOut += "From " + Format(rStart / 100, fmt2) + "m to " + Format(rEnd / 100, fmt2) + "m = " + CStr(count) + " samples" + vbCrLf
                Else
                    trimCount += count
                End If
                rStart = sorted.ElementAt(i + 1).Key
                count = sorted.ElementAt(i + 1).Value
            Else
                count += sorted.ElementAt(i + 1).Value
            End If
        Next

        If count > sampleThreshold Then
            If rEnd <> sorted.ElementAt(sorted.Count - 1).Key Then
                If count > sampleThreshold Then
                    rEnd = sorted.ElementAt(sorted.Count - 1).Key
                    ranges.Add(New rangeData(rc.index, rStart, rEnd, count))
                    strOut += "From " + Format(rStart / 100, fmt2) + "m to " + Format(rEnd / 100, fmt2) + "m = " + CStr(count) + " samples" + vbCrLf
                End If
            End If
        End If

        strOut += CStr(ranges.Count) + " " + typeSpec
        If ranges.Count > 1 Then strOut += " ranges were " Else strOut += " range was "
        strOut += " found while " + CStr(trimCount) + " pixels were tossed as they were in clusters with size < " + CStr(sampleThreshold) + vbCrLf
        Return ranges
    End Function
    Public Function buildColorRanges(input As cv.Mat, typespec As String)
        Dim ranges As New List(Of rangeData)
        Dim sorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger) ' the spectrum of the values 
        Dim pixels As New List(Of Integer)
        Dim counts As New List(Of Integer)

        Dim rc = task.rc
        For y = 0 To input.Height - 1
            For x = 0 To input.Width - 1
                If rc.mask.Get(Of Byte)(y, x) > 0 Then
                    Dim val = input.Get(Of Byte)(y, x)
                    If val <> 0 Then
                        Dim index = pixels.IndexOf(val)
                        If index >= 0 Then
                            counts(index) += 1
                        Else
                            pixels.Add(val)
                            counts.Add(1)
                        End If
                    End If
                End If
            Next
        Next

        If pixels.Count = 0 Then
            strOut = typespec + " data is missing."
            Return ranges
        End If

        Dim totalCount As Integer
        For i = 0 To pixels.Count - 1
            sorted.Add(pixels(i), counts(i))
            totalCount += counts(i)
        Next

        strOut = "For the selected " + typespec + " cell:" + vbCrLf

        Dim rStart As Integer = sorted.ElementAt(0).Key
        Dim rEnd As Integer = rStart
        Dim count As Integer = sorted.ElementAt(0).Value
        Dim trimCount As Integer
        For i = 0 To sorted.Count - 2
            If sorted.ElementAt(i + 1).Key - sorted.ElementAt(i).Key > gapGray Then
                rEnd = sorted.ElementAt(i).Key
                If count > sampleThreshold Then
                    ranges.Add(New rangeData(rc.index, rStart, rEnd, count))
                    strOut += "From " + CStr(rStart) + " to " + CStr(rEnd) + " = " + CStr(count) + " samples" + vbCrLf
                Else
                    trimCount += count
                End If
                rStart = sorted.ElementAt(i + 1).Key
                count = sorted.ElementAt(i + 1).Value
            Else
                count += sorted.ElementAt(i + 1).Value
            End If
        Next

        If count > sampleThreshold Then
            If rEnd <> sorted.ElementAt(sorted.Count - 1).Key Then
                If count > sampleThreshold Then
                    rEnd = sorted.ElementAt(sorted.Count - 1).Key
                    ranges.Add(New rangeData(rc.index, rStart, rEnd, count))
                    strOut += "From " + CStr(rStart) + " to " + CStr(rEnd) + " = " + CStr(count) + " samples" + vbCrLf
                End If
            End If
        End If

        strOut += CStr(ranges.Count) + typespec + " cells:" + vbCrLf
        If ranges.Count > 1 Then strOut += " ranges were " Else strOut += " range was "
        strOut += " found while " + CStr(trimCount) + " pixels were tossed as they were in clusters with size < " + CStr(sampleThreshold) + vbCrLf
        Return ranges
    End Function
    Public Sub RunVB()
        Static frmSliders = FindFrm("Options_Spectrum Sliders")
        Static gapDSlider = FindSlider("Gap in depth spectrum (cm's)")
        Static gapGSlider = FindSlider("Gap in gray spectrum")
        Static countSlider = FindSlider("Sample count threshold")
        gapDepth = gapDSlider.value
        gapGray = gapGSlider.value
        sampleThreshold = countSlider.value

        If task.FirstPass Then
            frmSliders.Left = task.gOptions.Width / 2
            frmSliders.top = task.gOptions.Height / 2
        End If
    End Sub
End Class






Public Class Options_HistXD : Inherits VB_Parent
    Public sideThreshold As Integer = 5
    Public topThreshold As Integer = 15
    Public threshold3D As Integer = 40
    Public selectedBin As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min side bin samples", 0, 100, sideThreshold) ' for 2D histograms
            sliders.setupTrackBar("Min top bin samples", 0, 100, topThreshold) ' for 2D histograms
            sliders.setupTrackBar("Min samples per bin", 0, 500, threshold3D) ' for 3D histograms
        End If
    End Sub
    Public Sub RunVB()
        Static topSlider = FindSlider("Min top bin samples")
        Static sideSlider = FindSlider("Min side bin samples")
        Static bothSlider = FindSlider("Min samples per bin")
        topThreshold = topSlider.value
        sideThreshold = sideSlider.value
        threshold3D = bothSlider.value
    End Sub
End Class






Public Class Options_Complexity : Inherits VB_Parent
    Public filename As FileInfo
    Public filenames As List(Of String)
    Public plotColor As cv.Scalar = cv.Scalar.Yellow
    Public Sub New()
        Dim fnames = Directory.GetFiles(task.HomeDir + "Complexity")
        filenames = fnames.ToList
        Dim latestFile = Directory.GetFiles(task.HomeDir + "Complexity").OrderByDescending(
                     Function(f) New FileInfo(f).LastWriteTime).First()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)

            Dim saveIndex As Integer
            For i = 0 To filenames.Count - 1
                Dim filename = New FileInfo(filenames(i))
                If filename.FullName = latestFile Then saveIndex = i
                radio.addRadio(filename.Name)
            Next
            radio.check(saveIndex).Checked = True
        End If
    End Sub
    Public Function setPlotColor() As cv.Scalar
        Static frm = FindFrm(traceName + " Radio Buttons")
        Dim index As Integer
        For index = 0 To filenames.Count - 1
            If filename.FullName = filenames(index) Then Exit For
        Next
        plotColor = Choose(index Mod 4 + 1, cv.Scalar.White, cv.Scalar.Red, cv.Scalar.Green, cv.Scalar.Yellow)
        Return plotColor
    End Function
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.count - 1
            If frm.check(i).checked Then
                filename = New FileInfo(task.HomeDir + "Complexity/" + frm.check(i).text)
                plotColor = Choose((i + 1) Mod 4, cv.Scalar.White, cv.Scalar.Red, cv.Scalar.Green, cv.Scalar.Yellow)
                Exit For
            End If
        Next
        If task.FirstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If
    End Sub
End Class








Public Class Options_Edges_All : Inherits VB_Parent
    Dim canny As New Edge_Canny
    Dim scharr As New Edge_Scharr
    Dim binRed As New Edge_BinarizedReduction
    Dim binSobel As New Bin4Way_Sobel
    Dim colorGap As New Edge_ColorGap_CPP
    Dim deriche As New Edge_Deriche_CPP
    Dim Laplacian As New Edge_Laplacian
    Dim resizeAdd As New Edge_ResizeAdd
    Dim regions As New Edge_Regions
    Public edgeSelection As String = ""
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Canny")
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
    Public Sub RunEdges(src As cv.Mat)
        Select Case edgeSelection
            Case "Canny"
                canny.Run(src)
                dst2 = canny.dst2
            Case "Scharr"
                scharr.Run(src)
                dst2 = scharr.dst3
            Case "Binarized Reduction"
                binRed.Run(src)
                dst2 = binRed.dst2
            Case "Binarized Sobel"
                binSobel.Run(src)
                dst2 = binSobel.dst2
            Case "Color Gap"
                colorGap.Run(src)
                dst2 = colorGap.dst2
            Case "Deriche"
                deriche.Run(src)
                dst2 = deriche.dst2
            Case "Laplacian"
                Laplacian.Run(src)
                dst2 = Laplacian.dst2
            Case "Resize And Add"
                resizeAdd.Run(src)
                dst2 = resizeAdd.dst2
            Case "Depth Region Boundaries"
                regions.Run(src)
                dst2 = regions.dst2
        End Select
    End Sub

    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        If task.FirstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If
        For i = 0 To frm.check.Count - 1
            If frm.check(i).checked Then
                edgeSelection = frm.check(i).text
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_BGSubtractSynthetic : Inherits VB_Parent
    Public amplitude As Double = 200
    Public magnitude As Double = 20
    Public waveSpeed As Double = 20
    Public objectSpeed As Double = 15
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Synthetic Amplitude x100", 1, 400, amplitude)
            sliders.setupTrackBar("Synthetic Magnitude", 1, 40, magnitude)
            sliders.setupTrackBar("Synthetic Wavespeed x100", 1, 400, waveSpeed)
            sliders.setupTrackBar("Synthetic ObjectSpeed", 1, 20, objectSpeed)
        End If
    End Sub
    Public Sub RunVB()
        Static amplitudeSlider = FindSlider("Synthetic Amplitude x100")
        Static MagSlider = FindSlider("Synthetic Magnitude")
        Static speedSlider = FindSlider("Synthetic Wavespeed x100")
        Static objectSlider = FindSlider("Synthetic ObjectSpeed")

        amplitude = amplitudeSlider.Value
        magnitude = MagSlider.Value
        waveSpeed = speedSlider.Value
        objectSpeed = objectSlider.Value
    End Sub
End Class






Public Class Options_BGSubtract : Inherits VB_Parent
    Public learnRate As Single = 100 / 1000
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
        If sliders.Setup(traceName) Then sliders.setupTrackBar("MOG Learn Rate X1000", 1, 1000, learnRate * 1000)
    End Sub
    Public Sub RunVB()
        Static learnRateSlider = FindSlider("MOG Learn Rate X1000")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    currMethod = i
                    methodDesc = "Method = " + frm.check(i).Text
                End If
            End If
        Next
        learnRate = learnRateSlider.Value / 1000
    End Sub
End Class







Public Class Options_Classifier : Inherits VB_Parent
    Public methodIndex As Integer
    Public methodName As String
    Public sampleCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Samples", 10, dst2.Total, 200)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Normal Bayes (NBC)")
            radio.addRadio("K Nearest Neighbor (KNN)")
            radio.addRadio("Support Vector Machine (SVM)")
            radio.addRadio("Decision Tree (DTree)")
            radio.addRadio("Boosted Tree (BTree)")
            radio.addRadio("Random Forest (RF)")
            radio.addRadio("Artificial Neural Net (ANN)")
            radio.addRadio("Expectation Maximization (EM)")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static inputSlider = FindSlider("Random Samples")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                methodIndex = i
                methodName = frm.check(i).Text
                Exit For
            End If
        Next
        If task.FirstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If

        sampleCount = inputSlider.value
    End Sub
End Class








Public Class Options_Derivative : Inherits VB_Parent
    Public channel As Integer = 0 ' assume X Dimension
    Public kernelSize As Integer
    Dim options As New Options_Sobel
    Public derivativeRange As Single = 0.1
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("X Dimension")
            radio.addRadio("Y Dimension")
            radio.addRadio("Z Dimension")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        options.RunVB()

        Static frm = FindFrm(traceName + " Radio Buttons")
        channel = 2
        If frm.check(2).checked = False Then
            If frm.check(0).checked Then channel = 0 Else channel = 1
        End If

        kernelSize = options.kernelSize
        derivativeRange = options.derivativeRange
    End Sub
End Class






Public Class Options_LaplacianKernels : Inherits VB_Parent
    Public gaussiankernelSize As Integer
    Public LaplaciankernelSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gaussian Kernel", 1, 32, 1)
            sliders.setupTrackBar("Laplacian Kernel", 1, 32, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static gaussSlider = FindSlider("Gaussian Kernel")
        Static LaplacianSlider = FindSlider("Laplacian Kernel")
        gaussiankernelSize = gaussSlider.Value Or 1
        LaplaciankernelSize = LaplacianSlider.Value Or 1
    End Sub
End Class








Public Class Options_Threshold : Inherits VB_Parent
    Public thresholdMethod As cv.ThresholdTypes = cv.ThresholdTypes.Binary
    Public thresholdName As String
    Public threshold As Integer = 255
    Public gradient As New Gradient_Color
    Public inputGray As Boolean
    Public otsuOption As Boolean
    Dim radioChoices = {cv.ThresholdTypes.Binary, cv.ThresholdTypes.BinaryInv, cv.ThresholdTypes.Tozero,
                        cv.ThresholdTypes.TozeroInv, cv.ThresholdTypes.Triangle, cv.ThresholdTypes.Trunc}
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold value", 0, 255, threshold)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("GrayScale Input")
            check.addCheckBox("Add OTSU Option - a 50/50 split")
        End If

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Binary")
            radio.addRadio("Binary Inverse")
            radio.addRadio("ToZero")
            radio.addRadio("ToZero Inverse")
            radio.addRadio("Trunc")
            radio.check(0).Checked = True
        End If

        gradient.Run(empty)
        dst2 = gradient.dst2
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Dim index = findRadioIndex(frm.check)
        thresholdMethod = radioChoices(index)
        thresholdName = Choose(index + 1, "Binary", "BinaryInv", "Tozero", "TozeroInv", "Triangle", "Trunc")

        Static inputGrayCheck = FindCheckBox("GrayScale Input")
        Static otsuCheck = FindCheckBox("Add OTSU Option - a 50/50 split")
        Static threshSlider = FindSlider("Threshold value")
        Static maxSlider = FindSlider("MaxVal setting")

        inputGray = inputGrayCheck.checked
        otsuOption = otsuCheck.checked
        threshold = threshSlider.Value
    End Sub
End Class






Public Class Options_Threshold_Adaptive : Inherits VB_Parent
    Public method As cv.AdaptiveThresholdTypes
    Public blockSize As Integer = 5
    Public constantVal As Integer
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("AdaptiveThreshold block size", 3, 21, blockSize)
            sliders.setupTrackBar("Constant subtracted from mean Or weighted mean", -20, 20, 0)
        End If

        If standaloneTest() = False Then
            FindRadio("ToZero").Enabled = False
            FindRadio("ToZero Inverse").Enabled = False
            FindRadio("Trunc").Enabled = False
        End If
    End Sub
    Public Sub RunVB()
        Static gaussRadio = FindRadio("GaussianC")
        Static constantSlider = FindSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = FindSlider("AdaptiveThreshold block size")

        method = If(gaussRadio.checked, cv.AdaptiveThresholdTypes.GaussianC, cv.AdaptiveThresholdTypes.MeanC)
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class







Public Class Options_Colors : Inherits VB_Parent
    Public redS As Integer
    Public greenS As Integer
    Public blueS As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.Setup(traceName)
            sliders.setupTrackBar("Red", 0, 255, 180)
            sliders.setupTrackBar("Green", 0, 255, 180)
            sliders.setupTrackBar("Blue", 0, 255, 180)
        End If
    End Sub
    Public Sub RunVB()
        Static redSlider = FindSlider("Red")
        Static greenSlider = FindSlider("Green")
        Static blueSlider = FindSlider("Blue")
        redS = redSlider.Value
        greenS = greenSlider.Value
        blueS = blueSlider.Value
    End Sub
End Class





Public Class Options_Threshold_AdaptiveMin : Inherits VB_Parent
    Public adaptiveMethod As cv.AdaptiveThresholdTypes
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static gaussRadio = FindRadio("GaussianC")
        adaptiveMethod = If(gaussRadio.checked, cv.AdaptiveThresholdTypes.GaussianC, cv.AdaptiveThresholdTypes.MeanC)
    End Sub
End Class








Public Class Options_ThresholdAll : Inherits VB_Parent
    Public thresholdMethod As cv.ThresholdTypes = cv.ThresholdTypes.Binary
    Public blockSize As Integer = 5
    Public constantVal As Integer
    Public maxVal As Integer = 255
    Public threshold As Integer = 100
    Public gradient As New Gradient_Color
    Public inputGray As Boolean
    Public otsuOption As Boolean
    Public adaptiveMethod As cv.AdaptiveThresholdTypes
    Dim radioChoices = {cv.ThresholdTypes.Binary, cv.ThresholdTypes.BinaryInv, cv.ThresholdTypes.Tozero,
                        cv.ThresholdTypes.TozeroInv, cv.ThresholdTypes.Triangle, cv.ThresholdTypes.Trunc}
    Dim options As New Options_Threshold_AdaptiveMin
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold value", 0, 255, threshold)
            sliders.setupTrackBar("MaxVal setting", 0, 255, maxVal)
            sliders.setupTrackBar("AdaptiveThreshold block size", 3, 21, blockSize)
            sliders.setupTrackBar("Constant subtracted from mean Or weighted mean", -20, 20, 0)
        End If

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("GrayScale Input")
            check.addCheckBox("Add OTSU Option - a 50/50 split")
        End If

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Binary")
            radio.addRadio("Binary Inverse")
            radio.addRadio("ToZero")
            radio.addRadio("ToZero Inverse")
            radio.addRadio("Trunc")
            radio.check(4).Checked = True
        End If

        gradient.Run(empty)
        dst2 = gradient.dst2
    End Sub
    Public Sub RunVB()
        options.RunVB()
        adaptiveMethod = options.adaptiveMethod

        Static frm = FindFrm(traceName + " Radio Buttons")
        thresholdMethod = radioChoices(findRadioIndex(frm.check))

        Static inputGrayCheck = FindCheckBox("GrayScale Input")
        Static otsuCheck = FindCheckBox("Add OTSU Option - a 50/50 split")

        Static threshSlider = FindSlider("Threshold value")
        Static maxSlider = FindSlider("MaxVal setting")
        Static constantSlider = FindSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = FindSlider("AdaptiveThreshold block size")

        inputGray = inputGrayCheck.checked
        otsuOption = otsuCheck.checked
        threshold = threshSlider.Value
        maxVal = maxSlider.Value
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class





Public Class Options_StdevGrid : Inherits VB_Parent
    Public minThreshold As Integer
    Public maxThreshold As Integer
    Public diffThreshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min color threshold", 0, 50, 30)
            sliders.setupTrackBar("Max color threshold", 0, 255, 230)
            sliders.setupTrackBar("Equal diff threshold", 0, 20, 5)
        End If

        desc = "Options for the StdevGrid algorithms."
    End Sub
    Public Sub RunVB()
        Static minSlider = FindSlider("Min color threshold")
        Static maxSlider = FindSlider("Max color threshold")
        Static diffSlider = FindSlider("Equal diff threshold")
        minThreshold = minSlider.value
        maxThreshold = maxSlider.value
        diffThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_DFT : Inherits VB_Parent
    Public radius As Integer = dst2.Rows
    Public order As Integer = 2
    Public butterworthFilter(1) As cv.Mat
    Public dftFlag As cv.DctFlags
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DFT B Filter - Radius", 1, dst2.Rows, radius)
            sliders.setupTrackBar("DFT B Filter - Order", 1, dst2.Rows, order)
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
        Static radiusSlider = FindSlider("DFT B Filter - Radius")
        Static orderSlider = FindSlider("DFT B Filter - Order")
        radius = radiusSlider.Value
        order = orderSlider.Value

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                dftFlag = Choose(i + 1, cv.DftFlags.ComplexOutput, cv.DftFlags.Inverse, cv.DftFlags.None,
                                        cv.DftFlags.RealOutput, cv.DftFlags.Rows, cv.DftFlags.Scale)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_DFTShape : Inherits VB_Parent
    Public dftShape As String
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Draw Circle")
            radio.addRadio("Draw Ellipse")
            radio.addRadio("Draw Rectangle")
            radio.addRadio("Draw Polygon")
            radio.addRadio("Draw Line")
            radio.addRadio("Draw Symmetrical Shapes")
            radio.addRadio("Draw Point")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm("Options_DFTShape Radio Buttons")
        dftShape = findRadioText(frm.check)
    End Sub
End Class






Public Class Options_FitEllipse : Inherits VB_Parent
    Public fitType As Integer
    Public threshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("FitEllipse threshold", 0, 255, 70)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("fitEllipseQ")
            radio.addRadio("fitEllipseAMS")
            radio.addRadio("fitEllipseDirect")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("FitEllipse threshold")
        Static qRadio = FindRadio("fitEllipseQ")
        Static amsRadio = FindRadio("fitEllipseAMS")
        Static directRadio = FindRadio("fitEllipseDirect")
        fitType = 0
        If amsRadio.checked Then fitType = 1
        If directRadio.checked Then fitType = 2

        threshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_TopX : Inherits VB_Parent
    Public topX As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the top X cells", 1, 255, 10)
    End Sub
    Public Sub RunVB()
        Static topXSlider = FindSlider("Show the top X cells")
        topX = topXSlider.value
    End Sub
End Class








Public Class Options_XNeighbors : Inherits VB_Parent
    Public xNeighbors As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("X neighbors", 1, 255, 5)
    End Sub
    Public Sub RunVB()
        Static topXSlider = FindSlider("X neighbors")
        xNeighbors = topXSlider.value
    End Sub
End Class








Public Class Options_Sobel : Inherits VB_Parent
    Public kernelSize As Integer = 3
    Public threshold As Integer = 50
    Public distanceThreshold As Integer
    Public derivativeRange As Single = 0.1
    Public horizontalDerivative As Boolean
    Public verticalDerivative As Boolean
    Public useBlur As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sobel kernel Size", 1, 31, kernelSize)
            sliders.setupTrackBar("Threshold to zero pixels below this value", 0, 255, threshold)
            sliders.setupTrackBar("Range around zero X100", 1, 500, derivativeRange * 100)
            sliders.setupTrackBar("Threshold distance", 0, 100, 10)
        End If

        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Vertical Derivative")
            check.addCheckBox("Horizontal Derivative")
            check.addCheckBox("Blur input before Sobel")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub setKernelSize(size As Integer)
        FindSlider("Sobel kernel Size").Value = size
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Threshold to zero pixels below this value")
        Static ksizeSlider = FindSlider("Sobel kernel Size")
        Static rangeSlider = FindSlider("Range around zero X100")
        Static distanceSlider = FindSlider("Threshold distance")
        kernelSize = ksizeSlider.Value Or 1
        threshold = thresholdSlider.value
        Static vDeriv = FindCheckBox("Vertical Derivative")
        Static hDeriv = FindCheckBox("Horizontal Derivative")
        Static checkBlur = FindCheckBox("Blur input before Sobel")
        horizontalDerivative = hDeriv.checked
        verticalDerivative = vDeriv.checked
        useBlur = checkBlur.checked
        derivativeRange = rangeSlider.value / 100
        distanceThreshold = distanceSlider.value
    End Sub
End Class





Public Class Options_EdgeOverlay : Inherits VB_Parent
    Public xDisp As Integer = 7
    Public yDisp As Integer = 11
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Displacement in the X direction (in pixels)", 0, 100, xDisp)
            sliders.setupTrackBar("Displacement in the Y direction (in pixels)", 0, 100, yDisp)
        End If
    End Sub
    Public Sub RunVB()
        Static xSlider = FindSlider("Displacement in the X direction (in pixels)")
        Static ySlider = FindSlider("Displacement in the Y direction (in pixels)")
        xDisp = xSlider.Value
        yDisp = ySlider.Value
    End Sub
End Class





Public Class Options_Swarm : Inherits VB_Parent
    Public ptCount As Integer
    Public border As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Connect X KNN points", 1, 10, 2)
            sliders.setupTrackBar("Distance to image border", 1, 10, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static ptSlider = FindSlider("Connect X KNN points")
        Static borderSlider = FindSlider("Distance to image border")
        ptCount = ptSlider.value
        border = borderSlider.value
    End Sub
End Class






Public Class Options_AddWeightedAccum : Inherits VB_Parent
    Public addWeighted As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Accumulation weight of each image X100", 1, 100, 10)
    End Sub
    Public Sub RunVB()
        Static weightSlider = FindSlider("Accumulation weight of each image X100")
        addWeighted = weightSlider.value / 100
    End Sub
End Class






Public Class Options_AddWeighted : Inherits VB_Parent
    Public addWeighted As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Add Weighted %", 0, 100, 50)
    End Sub
    Public Sub RunVB()
        Static weightSlider = FindSlider("Add Weighted %")
        addWeighted = weightSlider.value / 100
    End Sub
End Class







Public Class Options_ApproxPoly : Inherits VB_Parent
    Public epsilon As Double
    Public closedPoly As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("epsilon - max distance from original curve", 0, 100, 3)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Closed polygon - connect first and last vertices.")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static epsilonSlider = FindSlider("epsilon - max distance from original curve")
        Static closedPolyCheck = FindCheckBox("Closed polygon - connect first and last vertices.")
        epsilon = epsilonSlider.value
        closedPoly = closedPolyCheck.checked
    End Sub
End Class





Public Class Options_Bin3WayRedCloud : Inherits VB_Parent
    Public startRegion As Integer
    Public endRegion As Integer
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Review All Regions")
            radio.addRadio("Review Darkest and Lightest")
            radio.addRadio("Review Darkest Regions")
            radio.addRadio("Review Lightest Regions")
            radio.addRadio("Review 'Other' Regions")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Select Case findRadioIndex(frm.check)
            Case 0
                startRegion = 0
                endRegion = 2
            Case 1
                startRegion = 0
                endRegion = 1
            Case 2
                startRegion = 0
                endRegion = 0
            Case 3
                startRegion = 1
                endRegion = 1
            Case 4
                startRegion = 2
                endRegion = 2
        End Select
    End Sub
End Class






Public Class Options_Bin2WayRedCloud : Inherits VB_Parent
    Public startRegion As Integer
    Public endRegion As Integer
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Review All Regions")
            radio.addRadio("Review Darkest Regions")
            radio.addRadio("Review Darker Regions")
            radio.addRadio("Review Lighter Regions")
            radio.addRadio("Review Lightest Regions")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Select Case findRadioIndex(frm.check)
            Case 0
                startRegion = 0
                endRegion = 3
            Case 1
                startRegion = 0
                endRegion = 0
            Case 2
                startRegion = 1
                endRegion = 1
            Case 3
                startRegion = 2
                endRegion = 2
            Case 4
                startRegion = 3
                endRegion = 3
        End Select
    End Sub
End Class






Public Class Options_GuidedBPDepth : Inherits VB_Parent
    Public bins As Integer
    Public maxClusters As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram Bins for depth data", 3, 5000, 1000)
            sliders.setupTrackBar("Maximum number of clusters", 1, 50, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static binSlider = FindSlider("Histogram Bins for depth data")
        Static clusterSlider = FindSlider("Maximum number of clusters")
        bins = binSlider.value
        maxClusters = clusterSlider.value
    End Sub
End Class







Public Class Options_OpenGL_Duster : Inherits VB_Parent
    Public useClusterColors As Boolean
    Public useTaskPointCloud As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Display cluster colors")
            check.addCheckBox("Use task.pointCloud")
        End If
    End Sub
    Public Sub RunVB()
        Static colorCheck = FindCheckBox("Display cluster colors")
        Static cloudCheck = FindCheckBox("Use task.pointCloud")
        useClusterColors = colorCheck.checked
        useTaskPointCloud = cloudCheck.checked
    End Sub
End Class




Public Enum FeatureSrc
    goodFeaturesFull = 0
    goodFeaturesGrid = 1
    Agast = 2
    BRISK = 3
    Harris = 4
    FAST = 5
End Enum


Public Class Options_FeatureGather : Inherits VB_Parent
    Public featureSource As FeatureSrc
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GoodFeatures (ShiTomasi) full image")
            radio.addRadio("GoodFeatures (ShiTomasi) grid")
            radio.addRadio("Agast Features")
            radio.addRadio("BRISK Features")
            radio.addRadio("Harris Features")
            radio.addRadio("FAST Features")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                featureSource = Choose(i + 1, FeatureSrc.goodFeaturesFull, FeatureSrc.goodFeaturesGrid, FeatureSrc.Agast,
                                       FeatureSrc.BRISK, FeatureSrc.Harris, FeatureSrc.FAST)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_AsciiArt : Inherits VB_Parent
    Public hStep As Single
    Public wStep As Single
    Public size As New cv.Size
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Character height in pixels", 20, 100, 31)
            sliders.setupTrackBar("Character width in pixels", 20, 200, 55)
        End If
    End Sub
    Public Sub RunVB()
        Static hSlider = FindSlider("Character height in pixels")
        Static wSlider = FindSlider("Character width in pixels")

        hStep = CInt(task.WorkingRes.Height / hSlider.value)
        wStep = CInt(task.WorkingRes.Width / wSlider.value)
        size = New cv.Size(CInt(wSlider.value), CInt(hSlider.value))
    End Sub
End Class






Public Class Options_MotionDetect : Inherits VB_Parent
    Public radioChoices As cv.Vec3i()
    Public threadData As cv.Vec3i
    Public CCthreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 0, 1000, 980)
        If radio.Setup(traceName) Then
            For i = 0 To 7 - 1
                radio.addRadio(CStr(2 ^ i) + " threads")
            Next
            radio.check(5).Checked = True
        End If
        Dim w = dst2.Width
        Dim h = dst2.Height
        radioChoices = {New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2),
                        New cv.Vec3i(8, w / 4, h / 2), New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4),
                        New cv.Vec3i(32, w / 8, h / 8), New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2),
                        New cv.Vec3i(8, w / 4, h / 2), New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4),
                        New cv.Vec3i(32, w / 8, h / 8)}
    End Sub
    Public Sub RunVB()
        Static correlationSlider = FindSlider("Correlation Threshold")
        Static frm = FindFrm(traceName + " Radio Buttons")
        CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        threadData = radioChoices(findRadioIndex(frm.check))

    End Sub
End Class





Public Class Options_JpegQuality : Inherits VB_Parent
    Public quality As Integer
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("JPEG Quality", 1, 100, 90)
    End Sub
    Public Sub RunVB()
        Static qualitySlider = FindSlider("JPEG Quality")
        quality = qualitySlider.value
    End Sub
End Class




Public Class Options_PNGCompression : Inherits VB_Parent
    Public compression As Integer
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("PNG Compression", 1, 100, 90)
    End Sub
    Public Sub RunVB()
        Static compressionSlider = FindSlider("PNG Compression")
        compression = compressionSlider.value
    End Sub
End Class





Public Class Options_Binarize : Inherits VB_Parent
    Public binarizeLabel As String
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Binary")
            radio.addRadio("Binary + OTSU")
            radio.addRadio("OTSU")
            radio.addRadio("OTSU + Blur")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then binarizeLabel = radio.check(i).Text
        Next
    End Sub
End Class






Public Class Options_BlurTopo : Inherits VB_Parent
    Public savePercent As Single
    Public nextPercent As Single
    Public reduction As Integer
    Public frameCycle As Integer
    Public kernelSize As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Percent of Blurring", 0, 100, 20)
            sliders.setupTrackBar("Blur Color Reduction", 2, 64, 20)
            sliders.setupTrackBar("Frame Count Cycle", 1, 200, 50)
        End If
    End Sub
    Public Sub RunVB()
        Static reductionSlider = FindSlider("Blur Color Reduction")
        Static frameSlider = FindSlider("Frame Count Cycle")
        Static percentSlider = FindSlider("Percent of Blurring")

        If savePercent <> percentSlider.Value Then
            savePercent = percentSlider.Value
            nextPercent = savePercent
        End If

        frameCycle = frameSlider.value
        reduction = reductionSlider.value / 100
        kernelSize = CInt(nextPercent / 100 * dst2.Width) Or 1
    End Sub
End Class






Public Class Options_BoundaryRect : Inherits VB_Parent
    Public percentRect As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired percent of rectangles", 0, 100, 25)
    End Sub
    Public Sub RunVB()
        Static percentSlider = FindSlider("Desired percent of rectangles")
        percentRect = percentSlider.value / 100
    End Sub
End Class








Public Class Options_BrightnessContrast : Inherits VB_Parent
    Public brightness As Single
    Public contrast As Integer
    Public hsvBrightness As Single
    Public exponent As Single
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
            sliders.setupTrackBar("Alpha (contrast)", 0, 10000, alphaDefault)
            sliders.setupTrackBar("Beta (brightness)", -127, 127, betaDefault)
            sliders.setupTrackBar("HSV Brightness Value", 0, 150, 100)
            sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, 30)
        End If
    End Sub
    Public Sub RunVB()
        Static betaSlider = FindSlider("Beta (brightness)")
        Static alphaSlider = FindSlider("Alpha (contrast)")
        brightness = alphaSlider.value / 500
        contrast = betaSlider.value
        Static brightnessSlider = FindSlider("HSV Brightness Value")
        hsvBrightness = brightnessSlider.Value / 100
        Static exponentSlider = FindSlider("Contrast exponent to use X100")
        exponent = exponentSlider.value / 100
    End Sub
End Class






Public Class Options_HistPointCloud : Inherits VB_Parent
    Public threshold As Integer
    Public xBins As Integer, yBins As Integer, zBins As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram X bins", 1, dst2.Cols, 30)
            sliders.setupTrackBar("Histogram Y bins", 1, dst2.Rows, 30)
            sliders.setupTrackBar("Histogram Z bins", 1, 200, 100)
            sliders.setupTrackBar("Histogram threshold", 0, 1000, 500)
        End If

        Select Case dst2.Width
            Case 640
                FindSlider("Histogram threshold").Value = 200
            Case 320
                FindSlider("Histogram threshold").Value = 60
            Case 160
                FindSlider("Histogram threshold").Value = 25
        End Select
    End Sub
    Public Sub RunVB()
        Static xSlider = FindSlider("Histogram X bins")
        Static ySlider = FindSlider("Histogram Y bins")
        Static zSlider = FindSlider("Histogram Z bins")
        Static tSlider = FindSlider("Histogram threshold")
        xBins = xSlider.Value
        yBins = ySlider.Value
        zBins = zSlider.Value
        threshold = tSlider.value
    End Sub
End Class







Public Class Options_Harris : Inherits VB_Parent
    Public threshold As Single = 1 / 10000
    Public neighborhood As Integer = 3
    Public aperture As Integer = 21
    Public harrisParm As Single = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Harris Threshold", 1, 100, threshold * 10000)
            sliders.setupTrackBar("Harris Neighborhood", 1, 41, neighborhood)
            sliders.setupTrackBar("Harris aperture", 1, 31, aperture)
            sliders.setupTrackBar("Harris Parameter", 1, 100, harrisParm)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdslider = FindSlider("Harris Threshold")
        Static neighborSlider = FindSlider("Harris Neighborhood")
        Static apertureSlider = FindSlider("Harris aperture")
        Static parmSlider = FindSlider("Harris Parameter")

        threshold = thresholdslider.Value / 10000
        neighborhood = neighborSlider.Value Or 1
        aperture = apertureSlider.Value Or 1
        harrisParm = parmSlider.Value / 100
    End Sub
End Class






Public Class Options_HarrisCorners : Inherits VB_Parent
    Public quality As Integer
    Public qualityMax As Integer = 100
    Public blockSize As Integer
    Public aperture As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, 3)
            sliders.setupTrackBar("Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar("Corner quality level", 1, 100, 50)
        End If
    End Sub
    Public Sub RunVB()
        Static blockSlider = FindSlider("Corner block size")
        Static apertureSlider = FindSlider("Corner aperture size")
        Static qualitySlider = FindSlider("Corner quality level")
        quality = qualitySlider.Value
        aperture = apertureSlider.value Or 1
        blockSize = blockSlider.value Or 1
    End Sub
End Class






Public Class Options_Databases : Inherits VB_Parent
    Public linkAddress As String
    Dim downloadActive As Boolean
    Dim downloadIndex As Integer
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
    End Sub
    Public Sub RunVB()
        Static downloadAllCheck = FindRadio("Download All")
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
    End Sub
End Class







Public Class Options_EdgeMatching : Inherits VB_Parent '
    Public threshold As Single
    Public searchDepth As Integer
    Public overlayChecked As Boolean
    Public highlightChecked As Boolean
    Public clearChecked As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Search depth in pixels", 1, 256, 256)
            sliders.setupTrackBar("Edge Correlation threshold X100", 1, 100, 80)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Overlay thread grid")
            check.addCheckBox("Highlight all grid entries above threshold")
            check.addCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static overlayCheck = FindCheckBox("Overlay thread grid")
        Static highlightCheck = FindCheckBox("Highlight all grid entries above threshold")
        Static clearCheck = FindCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
        Static thresholdSlider = FindSlider("Edge Correlation threshold X100")
        Static searchSlider = FindSlider("Search depth in pixels")
        threshold = thresholdSlider.Value / 100
        searchDepth = searchSlider.Value
        overlayChecked = overlayCheck.checked
        highlightChecked = highlightCheck.checked
        clearChecked = clearCheck.checked
    End Sub
End Class






Public Class Options_EmaxInputClusters : Inherits VB_Parent
    Public samplesPerRegion As Integer
    Public sigma As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("EMax Number of Samples per region", 1, 20, 10)
            sliders.setupTrackBar("EMax Sigma (spread)", 1, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static sampleSlider = FindSlider("EMax Number of Samples per region")
        Static sigmaSlider = FindSlider("EMax Sigma (spread)")
        samplesPerRegion = sampleSlider.value
        sigma = sigmaSlider.value
    End Sub
End Class





Public Class Options_CComp : Inherits VB_Parent
    Public light As Integer = 127
    Public dark As Integer = 50
    Public threshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for lighter input", 1, 255, light)
            sliders.setupTrackBar("Threshold for darker input", 1, 255, dark)
            sliders.setupTrackBar("CComp threshold", 0, 255, 50)
        End If

        desc = "Options for CComp_Both"
    End Sub
    Public Sub RunVB()
        Static lightSlider = FindSlider("Threshold for lighter input")
        Static darkSlider = FindSlider("Threshold for darker input")
        Static thresholdSlider = FindSlider("CComp threshold")
        threshold = thresholdSlider.value
        light = lightSlider.value
        dark = darkSlider.value
    End Sub
End Class






Public Class Options_CellAutomata : Inherits VB_Parent
    Public currentRule As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Current Rule", 0, 255, 0)
    End Sub
    Public Sub RunVB()
        Static ruleSlider = FindSlider("Current Rule")
        currentRule = ruleSlider.value
    End Sub
End Class






Public Class Options_BackProject2D : Inherits VB_Parent
    Public backProjectRow As Boolean
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BackProject Row")
            radio.addRadio("BackProject Col")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static rowRadio = FindRadio("BackProject Row")
        If task.mouseClickFlag Then rowRadio.checked = Not rowRadio.checked
        backProjectRow = rowRadio.checked
    End Sub
End Class





Public Class Options_Kaze : Inherits VB_Parent
    Public pointsToMatch As Integer
    Public maxDistance As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of points to match", 1, 300, 100)
            sliders.setupTrackBar("When matching, max possible distance", 1, 200, 100)
        End If
    End Sub
    Public Sub RunVB()
        Static maxSlider = FindSlider("Max number of points to match")
        Static distSlider = FindSlider("When matching, max possible distance")
        pointsToMatch = maxSlider.value
        maxDistance = distSlider.value
    End Sub
End Class





Public Class Options_Blob : Inherits VB_Parent
    Dim blob As New Blob_Input
    Public blobParams = New cv.SimpleBlobDetector.Params
    Public Sub New()
        If standaloneTest() Then blob.updateFrequency = 30

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
        Static minSlider = FindSlider("min Threshold")
        Static maxSlider = FindSlider("max Threshold")
        Static stepSlider = FindSlider("Threshold Step")
        Static areaRadio = FindRadio("FilterByArea")
        Static circRadio = FindRadio("FilterByCircularity")
        Static convexRadio = FindRadio("FilterByConvexity")
        Static inertiaRadio = FindRadio("FilterByInertia")
        Static colorRadio = FindRadio("FilterByColor")

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






Public Class Options_SLR : Inherits VB_Parent
    Public tolerance As Single
    Public halfLength As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Approximate accuracy (tolerance) X100", 1, 1000, 30)
            sliders.setupTrackBar("Simple moving average window size", 1, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static toleranceSlider = FindSlider("Approximate accuracy (tolerance) X100")
        Static movingAvgSlider = FindSlider("Simple moving average window size")
        tolerance = toleranceSlider.Value / 100
        halfLength = movingAvgSlider.Value
    End Sub
End Class




Public Class Options_KNN : Inherits VB_Parent
    Public knnDimension As Integer
    Public numPoints As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KNN Dimension", 2, 10, 2)
            sliders.setupTrackBar("Random input points", 5, 100, 10)
        End If
    End Sub
    Public Sub RunVB()
        Static dimSlider = FindSlider("KNN Dimension")
        Static randomSlider = FindSlider("Random input points")
        knnDimension = dimSlider.Value
        numPoints = randomSlider.Value
    End Sub
End Class






Public Class Options_Clone : Inherits VB_Parent
    Public alpha As Single
    Public beta As Single
    Public lowThreshold As Integer
    Public highThreshold As Integer
    Public blueChange As Single
    Public greenChange As Single
    Public redChange As Single
    Public cloneFlag As New cv.SeamlessCloneMethods
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Alpha", 0, 20, 2)
            sliders.setupTrackBar("Beta", 0, 20, 2)
            sliders.setupTrackBar("Low Threshold", 0, 100, 10)
            sliders.setupTrackBar("High Threshold", 0, 100, 50)
            sliders.setupTrackBar("Color Change - Red", 5, 25, 15)
            sliders.setupTrackBar("Color Change - Green", 5, 25, 5)
            sliders.setupTrackBar("Color Change - Blue", 5, 25, 5)
        End If

        If (radio.Setup(traceName)) Then
            radio.addRadio("Seamless Normal Clone")
            radio.addRadio("Seamless Mono Clone")
            radio.addRadio("Seamless Mixed Clone")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static alphaSlider = FindSlider("Alpha")
        Static betaSlider = FindSlider("Beta")
        Static lowSlider = FindSlider("Low Threshold")
        Static highSlider = FindSlider("High Threshold")
        Static redSlider = FindSlider("Color Change - Red")
        Static greenSlider = FindSlider("Color Change - Green")
        Static blueSlider = FindSlider("Color Change - Blue")
        alpha = alphaSlider.value / 10
        beta = betaSlider.value / 10
        lowThreshold = lowSlider.value
        highThreshold = highSlider.value
        blueChange = blueSlider.value / 10
        greenChange = greenSlider.value / 10
        redChange = redSlider.value / 10

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                cloneFlag = Choose(i + 1, cv.SeamlessCloneMethods.MixedClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.NormalClone)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Coherence : Inherits VB_Parent
    Public sigma As Integer
    Public blend As Single
    Public str_sigma As Integer
    Public eigenkernelsize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Coherence Sigma", 1, 15, 9)
            sliders.setupTrackBar("Coherence Blend", 1, 10, 10)
            sliders.setupTrackBar("Coherence str_sigma", 1, 15, 15)
            sliders.setupTrackBar("Coherence eigen kernel", 1, 31, 1)
        End If
    End Sub
    Public Sub RunVB()
        Static sigmaSlider = FindSlider("Coherence Sigma")
        Static blendSlider = FindSlider("Coherence Blend")
        Static strSlider = FindSlider("Coherence str_sigma")
        Static eigenSlider = FindSlider("Coherence eigen kernel")

        sigma = sigmaSlider.Value * 2 + 1
        blend = blendSlider.Value / 10
        str_sigma = strSlider.Value * 2 + 1
        eigenkernelsize = eigenSlider.Value * 2 + 1
    End Sub
End Class





Public Class Options_Color : Inherits VB_Parent
    Public colorFormat As String
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
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
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colorFormat = radio.check(i).Text
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Grayscale8U : Inherits VB_Parent
    Public useOpenCV As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use OpenCV to create grayscale image")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static grayCheck = FindCheckBox("Use OpenCV to create grayscale image")
        useOpenCV = grayCheck.checked
    End Sub
End Class





Public Class Options_Color8UTopX : Inherits VB_Parent
    Public topXcount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Top X pixels", 2, 32, 16)
    End Sub
    Public Sub RunVB()
        Static topXSlider = FindSlider("Top X pixels")
        topXcount = topXSlider.value
    End Sub
End Class







Public Class Options_Morphology : Inherits VB_Parent
    Public widthHeight As Integer
    Public iterations As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Morphology width/height", 1, 100, 20)
            sliders.setupTrackBar("MorphologyEx iterations", 1, 5, 1)
        End If
    End Sub
    Public Sub RunVB()
        Static morphSlider = FindSlider("Morphology width/height")
        Static morphExSlider = FindSlider("MorphologyEx iterations")
        widthHeight = morphSlider.value
        iterations = morphExSlider.value
    End Sub
End Class





Public Class Options_Convex : Inherits VB_Parent
    Public hullCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Hull random points", 4, 20, 10)
    End Sub
    Public Sub RunVB()
        Static hullSlider = FindSlider("Hull random points")
        hullCount = hullSlider.Value
    End Sub
End Class






Public Class Options_Corners : Inherits VB_Parent
    Public useNonMax As Boolean
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use Non-Max = True")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static nonMaxCheck = FindCheckBox("Use Non-Max = True")
        useNonMax = nonMaxCheck.checked
    End Sub
End Class


Public Class Options_PreCorners : Inherits VB_Parent
    Public kernelSize As Integer
    Public subpixSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("kernel Size", 1, 20, 19)
            sliders.setupTrackBar("SubPix kernel Size", 1, 20, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static kernelSlider = FindSlider("kernel Size")
        Static subpixSlider = FindSlider("SubPix kernel Size")
        kernelSize = kernelSlider.value Or 1
        subpixSize = subpixSlider.value Or 1
    End Sub
End Class





Public Class Options_ShiTomasi : Inherits VB_Parent
    Public useShiTomasi As Boolean = True
    Public threshold As Integer
    Public aperture As Integer
    Public blocksize As Integer

    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Harris features")
            radio.addRadio("Shi-Tomasi features")
            radio.check(1).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, 3)
            sliders.setupTrackBar("Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar("Corner normalize threshold", 0, 32, 0)
        End If
    End Sub
    Public Sub RunVB()
        Static typeRadio = FindRadio("Shi-Tomasi features")
        Static blockSlider = FindSlider("Corner block size")
        Static apertureSlider = FindSlider("Corner aperture size")
        Static thresholdSlider = FindSlider("Corner normalize threshold")
        threshold = thresholdSlider.Value
        useShiTomasi = typeRadio.checked
        aperture = apertureSlider.value Or 1
        blocksize = blockSlider.value Or 1
    End Sub
End Class




Public Class Options_FlatLand : Inherits VB_Parent
    Public reductionFactor As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Region Count", 1, 250, 10)
    End Sub
    Public Sub RunVB()
        Static regionSlider = FindSlider("Region Count")
        reductionFactor = regionSlider.Maximum - regionSlider.Value
    End Sub
End Class






Public Class Options_Depth : Inherits VB_Parent
    Public mmThreshold As Double
    Public millimeters As Integer
    Public threshold As Double
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold in millimeters", 0, 1000, 8)
            sliders.setupTrackBar("Threshold for punch", 0, 255, 250)
        End If
    End Sub
    Public Sub RunVB()
        Static mmSlider = FindSlider("Threshold in millimeters")
        Static thresholdSlider = FindSlider("Threshold for punch")
        millimeters = mmSlider.value
        mmThreshold = mmSlider.Value / 1000.0
        threshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_DepthHoles : Inherits VB_Parent
    Public borderDilation As Integer
    Public holeDilation As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Amount of dilation of borderMask", 1, 10, 1)
            sliders.setupTrackBar("Amount of dilation of holeMask", 0, 10, 0)
        End If
    End Sub
    Public Sub RunVB()
        Static borderSlider = FindSlider("Amount of dilation of borderMask")
        Static holeSlider = FindSlider("Amount of dilation of holeMask")
        borderDilation = borderSlider.value
        holeDilation = holeSlider.value
    End Sub
End Class






Public Class Options_Uncertainty : Inherits VB_Parent
    Public uncertaintyThreshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Uncertainty threshold", 1, 255, 100)
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Uncertainty threshold")
        uncertaintyThreshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_DepthColor : Inherits VB_Parent
    Public alpha As Single
    Public beta As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Depth ColorMap Alpha X100", 1, 100, 5)
            sliders.setupTrackBar("Depth ColorMap Beta", 1, 100, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static alphaSlider = FindSlider("Depth ColorMap Alpha X100")
        Static betaSlider = FindSlider("Depth ColorMap Beta")
        alpha = alphaSlider.value / 100
        beta = betaSlider.value
    End Sub
End Class






Public Class Options_DNN : Inherits VB_Parent
    Public superResModelFileName As String
    Public shortModelName As String
    Public superResMultiplier As Integer
    Public ScaleFactor As Single
    Public scaleMax As Single
    Public meanValue As Single
    Public confidenceThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DNN Scale Factor", 1, 10000, 78)
            sliders.setupTrackBar("DNN MeanVal", 1, 255, 127)
            sliders.setupTrackBar("DNN Confidence Threshold", 1, 100, 80)
        End If
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
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
        Static scaleSlider = FindSlider("DNN Scale Factor")
        Static meanSlider = FindSlider("DNN MeanVal")
        Static confidenceSlider = FindSlider("DNN Confidence Threshold")
        confidenceThreshold = confidenceSlider.Value / 100
        ScaleFactor = scaleSlider.value
        scaleMax = scaleSlider.maximum
        meanValue = meanSlider.value

        superResModelFileName = task.HomeDir + "Data/DNN_SuperResModels/"
        Static frm = FindFrm(traceName + " Radio Buttons")
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
        SetTrueText("Current Options: " + shortModelName + " at resolution " + CStr(superResMultiplier) + vbCrLf +
                    superResModelFileName + " is present and will be used.")
    End Sub
End Class





Public Class Options_DrawNoise : Inherits VB_Parent
    Public noiseCount As Integer
    Public noiseWidth As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Noise Count", 1, 1000, 100)
            sliders.setupTrackBar("Noise Width", 1, 10, 3)
        End If
    End Sub
    Public Sub RunVB()
        Static widthSlider = FindSlider("Noise Width")
        Static CountSlider = FindSlider("Noise Count")
        noiseCount = CountSlider.Value
        noiseWidth = widthSlider.Value
    End Sub
End Class






Public Class Options_Edges : Inherits VB_Parent
    Public scharrMultiplier As Single
    Public recurseCheck As Boolean
    Public EP_Sigma_s As Single
    Public EP_Sigma_r As Single
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Edge-preserving RecurseFilter")
            radio.addRadio("Edge-preserving NormconvFilter")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Scharr multiplier X100", 1, 500, 50)
            sliders.setupTrackBar("Edge-preserving Sigma_s", 0, 200, 10)
            sliders.setupTrackBar("Edge-preserving Sigma_r", 1, 100, 40)
        End If
    End Sub
    Public Sub RunVB()
        Static sigmaSSlider = FindSlider("Edge-preserving Sigma_s")
        Static sigmaRSlider = FindSlider("Edge-preserving Sigma_r")
        Static recurseRadio = FindRadio("Edge-preserving RecurseFilter")
        EP_Sigma_s = sigmaSSlider.Value
        EP_Sigma_r = sigmaRSlider.Value / sigmaRSlider.Maximum
        recurseCheck = recurseRadio.checked
    End Sub
End Class






Public Class Options_Edges2 : Inherits VB_Parent
    Public edgeRFthreshold As Integer
    Public removeFrequencies As Integer
    Public dctThreshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Remove Frequencies < x", 0, 100, 32)
            sliders.setupTrackBar("Threshold after Removal", 1, 255, 20)
            sliders.setupTrackBar("Edges RF Threshold", 1, 255, 35)
        End If
    End Sub
    Public Sub RunVB()
        Static freqSlider = FindSlider("Remove Frequencies < x")
        Static thresholdSlider = FindSlider("Threshold after Removal")

        Static rfSlider = FindSlider("Edges RF Threshold")
        edgeRFthreshold = rfSlider.value
        removeFrequencies = freqSlider.value
        dctThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_Edges3 : Inherits VB_Parent
    Public alpha As Single
    Public omega As Single
    Public gapDistance As Integer
    Public gapdiff As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Deriche Alpha X100", 1, 400, 100)
            sliders.setupTrackBar("Deriche Omega X1000", 1, 1000, 100)
            sliders.setupTrackBar("Input pixel distance", 0, 20, 5)
            sliders.setupTrackBar("Input pixel difference", 0, 50, If(task.WorkingRes.Width = 640, 10, 20))
        End If
    End Sub
    Public Sub RunVB()
        Static alphaSlider = FindSlider("Deriche Alpha X100")
        Static omegaSlider = FindSlider("Deriche Omega X1000")
        alpha = alphaSlider.value / 100
        omega = omegaSlider.value / 100

        Static distanceSlider = FindSlider("Input pixel distance")
        Static diffSlider = FindSlider("Input pixel difference")
        gapDistance = distanceSlider.Value And 254
        gapdiff = diffSlider.Value
    End Sub
End Class







Public Class Options_DepthEdges : Inherits VB_Parent
    Public depthDiff As Integer
    Public depthOffset As Single
    Public depthDist As Integer
    Public mmDepthDiff As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for depth difference", 0, 255, 200)
            sliders.setupTrackBar("cv.rect offset X1000", 0, 20, 1)
            sliders.setupTrackBar("Input depth distance", 0, 20, 5)
            sliders.setupTrackBar("Input depth difference in mm's", 0, 2000, 1000)
        End If
    End Sub
    Public Sub RunVB()
        Static diffSlider = FindSlider("Threshold for depth difference")
        Static rectSlider = FindSlider("cv.rect offset X1000")
        Static distanceSlider = FindSlider("Input depth distance")
        Static mmDiffSlider = FindSlider("Input depth difference in mm's")

        depthDiff = diffSlider.value
        depthOffset = rectSlider.value / 1000

        depthDist = distanceSlider.value And 254
        mmDepthDiff = mmDiffSlider.value / 1000
    End Sub
End Class






Public Class Options_Edges4 : Inherits VB_Parent
    Public vertPixels As Integer
    Public horizPixels As Integer
    Public horizonCheck As Boolean
    Public verticalCheck As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Horizontal Edges")
            check.addCheckBox("Vertical Edges")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Border Vertical in Pixels", 1, 20, 5)
            sliders.setupTrackBar("Border Horizontal in Pixels", 1, 20, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static hCheck = FindCheckBox("Horizontal Edges")
        Static vCheck = FindCheckBox("Vertical Edges")
        Static vertSlider = FindSlider("Border Vertical in Pixels")
        Static horizSlider = FindSlider("Border Horizontal in Pixels")
        vertPixels = vertSlider.value
        horizPixels = horizSlider.value
        horizonCheck = hCheck.checked
        verticalCheck = vCheck.checked
    End Sub
End Class








Public Class Options_Erode : Inherits VB_Parent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cv.MorphShapes
    Public element As cv.Mat
    Public noshape As Boolean
    Public flatDepth As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Erode Kernel Size", 1, 32, kernelSize)
            sliders.setupTrackBar("Erode Iterations", 0, 32, iterations)
            sliders.setupTrackBar("DepthSeed flat depth X1000", 1, 200, 100)
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
        Static ellipseRadio = FindRadio("Erode shape: Ellipse")
        Static rectRadio = FindRadio("Erode shape: Rect")
        Static iterSlider = FindSlider("Erode Iterations")
        Static kernelSlider = FindSlider("Erode Kernel Size")
        Static noShapeRadio = FindRadio("Erode shape: None")
        Static depthSlider = FindSlider("DepthSeed flat depth X1000")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cv.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cv.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cv.MorphShapes.Rect
        element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
        flatDepth = depthSlider.value / 1000
    End Sub
End Class





Public Class Options_Etch_ASketch : Inherits VB_Parent
    Public demoMode As Boolean
    Public cleanMode As Boolean
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Etch_ASketch clean slate")
            check.addCheckBox("Demo mode")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static cleanCheck = FindCheckBox("Etch_ASketch clean slate")
        Static demoCheck = FindCheckBox("Demo mode")
        demoMode = demoCheck.checked

        If cleanMode Then cleanCheck.checked = False ' only on for one frame.
        cleanMode = cleanCheck.checked
    End Sub
End Class









Public Class Options_Features : Inherits VB_Parent
    Public quality As Double = 0.01
    Public minDistance As Double = 1
    Public matchOption As cv.TemplateMatchModes = cv.TemplateMatchModes.CCoeffNormed
    Public matchText As String = ""
    Public k As Double = 0.04
    Public blockSize As Integer = 3

    Public featurePoints As Integer = 400
    Public templatePad As Integer = 10
    Public templateSize As Integer
    Public correlationMin As Single = 0.75
    Public resyncThreshold As Single = 0.95
    Public agastThreshold As Integer = 20
    Public useVertical As Boolean
    Public Sub New()
        correlationMin = If(dst2.Width > 336, 0.8, 0.9)
        templatePad = If(dst2.Width > 336, 20, 10)
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Vertical lines")
            radio.addRadio("Horizontal lines")
            radio.check(0).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Distance to next", 1, 100, minDistance)

            sliders.setupTrackBar("Feature Sample Size", 1, 1000, featurePoints)
            sliders.setupTrackBar("Feature Correlation Threshold", 1, 100, correlationMin * 100)
            sliders.setupTrackBar("MatchTemplate Cell Size", 2, 100, templatePad)
            sliders.setupTrackBar("Threshold Percent for Resync", 1, 99, resyncThreshold * 100)

            sliders.setupTrackBar("Quality Level", 1, 100, quality * 100)
            sliders.setupTrackBar("k X1000", 1, 1000, k * 1000)
            sliders.setupTrackBar("Blocksize", 1, 21, blockSize)
            sliders.setupTrackBar("Agast Threshold", 1, 100, agastThreshold)
            sliders.setupTrackBar("FAST Threshold", 0, 200, task.FASTthreshold)
        End If
    End Sub
    Public Sub RunVB()
        Static qualitySlider = FindSlider("Quality Level")
        Static distSlider = FindSlider("Min Distance to next")
        Static kSlider = FindSlider("k X1000")
        Static blocksizeSlider = FindSlider("Blocksize")
        Static featureSlider = FindSlider("Feature Sample Size")
        Static corrSlider = FindSlider("Feature Correlation Threshold")
        Static cellSlider = FindSlider("MatchTemplate Cell Size")
        Static resyncSlider = FindSlider("Threshold Percent for Resync")
        Static agastslider = FindSlider("Agast Threshold")
        Static FASTslider = FindSlider("FAST Threshold")
        Static vertRadio = FindRadio("Vertical lines")
        useVertical = vertRadio.checked
        task.FASTthreshold = FASTslider.value

        blockSize = blocksizeSlider.value Or 1
        k = kSlider.value / 1000

        featurePoints = featureSlider.value
        correlationMin = corrSlider.value / 100
        templatePad = CInt(cellSlider.value / 2)
        templateSize = cellSlider.value Or 1
        resyncThreshold = resyncSlider.value / 100
        agastThreshold = agastslider.value

        If task.optionsChanged Then
            quality = qualitySlider.Value / 100
            minDistance = distSlider.Value
        End If
    End Sub
End Class





Public Class Options_LineFinder : Inherits VB_Parent
    Public tolerance As Integer
    Public kernelSize As Integer
    Public kSize As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5)
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static angleSlider = FindSlider("Angle tolerance in degrees")
        Static kernelSlider = FindSlider("Area kernel size for depth")
        kernelSize = kernelSlider.value * 2 - 1
        tolerance = angleSlider.value
        kSize = kernelSlider.Value - 1
    End Sub
End Class






Public Class Options_PCA_NColor : Inherits VB_Parent
    Public desiredNcolors As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired number of colors", 1, 256, 8)
    End Sub
    Public Sub RunVB()
        Static nSlider = FindSlider("Desired number of colors")
        desiredNcolors = nSlider.value
    End Sub
End Class





Public Class Options_FPolyCore : Inherits VB_Parent
    Public maxShift As Integer
    Public resyncThreshold As Integer
    Public anchorMovement As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Maximum shift to trigger resync", 1, 100, 50)
            sliders.setupTrackBar("Anchor point max movement", 1, 10, 5)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Static shiftSlider = FindSlider("Maximum shift to trigger resync")
        Static anchorSlider = FindSlider("Anchor point max movement")
        maxShift = shiftSlider.Value
        resyncThreshold = thresholdSlider.Value
        anchorMovement = anchorSlider.value
    End Sub
End Class





Public Class Options_FLANN : Inherits VB_Parent
    Public reuseData As Boolean
    Public matchCount As Integer
    Public queryCount As Integer
    Public searchCheck As Integer
    Public eps As Single
    Public sorted As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Query count", 1, 100, 1)
            sliders.setupTrackBar("Match count", 1, 100, 1)
            sliders.setupTrackBar("Search check count", 1, 1000, 5)
            sliders.setupTrackBar("EPS X100", 0, 100, 0)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Search params sorted")
            check.addCheckBox("Reuse the same feature list (test different search parameters)")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static reuseCheck = FindCheckBox("Reuse the same feature list (test different search parameters)")
        Static sortedCheck = FindCheckBox("Search params sorted")
        Static matchSlider = FindSlider("Match count")
        Static querySlider = FindSlider("Query count")
        Static searchSlider = FindSlider("Search check count")
        Static epsSlider = FindSlider("EPS X100")
        reuseData = reuseCheck.checked
        matchCount = matchSlider.value
        queryCount = querySlider.Value
        searchCheck = searchSlider.Value
        eps = epsSlider.Value / 100
        sorted = sortedCheck.checked
    End Sub
End Class








Public Class Options_TrackerDepth : Inherits VB_Parent
    Public displayRect As Boolean
    Public minRectSize As Integer
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Display centroid and rectangle for each region")
            check.Box(0).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for rectangle size", 50, 50000, 10000)
    End Sub
    Public Sub RunVB()
        Static displayCheck = FindCheckBox("Display centroid and rectangle for each region")
        Static minRectSizeSlider = FindSlider("Threshold for rectangle size")

        displayRect = displayCheck.checked
        minRectSize = minRectSizeSlider.value
    End Sub
End Class




Public Class Options_Gabor : Inherits VB_Parent
    Public gKernel As cv.Mat
    Public ksize As Double
    Public Sigma As Double
    Public theta As Double
    Public lambda As Double
    Public gamma As Double
    Public phaseOffset As Double
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gabor Kernel Size", 0, 50, 15)
            sliders.setupTrackBar("Gabor Sigma", 0, 100, 4)
            sliders.setupTrackBar("Gabor Theta (degrees)", 0, 180, 90)
            sliders.setupTrackBar("Gabor lambda", 0, 100, 10)
            sliders.setupTrackBar("Gabor gamma X10", 0, 10, 5)
            sliders.setupTrackBar("Gabor Phase offset X100", 0, 100, 0)
        End If
    End Sub
    Public Sub RunVB()
        Static ksizeSlider = FindSlider("Gabor Kernel Size")
        Static sigmaSlider = FindSlider("Gabor Sigma")
        Static lambdaSlider = FindSlider("Gabor lambda")
        Static gammaSlider = FindSlider("Gabor gamma X10")
        Static phaseSlider = FindSlider("Gabor Phase offset X100")
        Static thetaSlider = FindSlider("Gabor Theta (degrees)")
        ksize = ksizeSlider.Value * 2 + 1
        Sigma = sigmaSlider.Value
        lambda = lambdaSlider.Value
        gamma = gammaSlider.Value / 10
        phaseOffset = phaseSlider.Value / 1000
        theta = Math.PI * thetaSlider.Value / 180
        gKernel = cv.Cv2.GetGaborKernel(New cv.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cv.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier(0)
    End Sub
End Class






Public Class Options_GrabCut : Inherits VB_Parent
    Public clearAll As Boolean
    Public fineTuning As Boolean

    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Selected rectangle is added to the foreground")
            radio.addRadio("Selected rectangle is added to the background")
            radio.addRadio("Clear all foreground and background fine tuning")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static fgFineTuning = FindRadio("Selected rectangle is added to the foreground")
        Static clearCheck = FindRadio("Clear all foreground and background fine tuning")
        Static saveRadio = fgFineTuning.checked
    End Sub
End Class






Public Class Options_Gradient : Inherits VB_Parent
    Public exponent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, 30)
    End Sub
    Public Sub RunVB()
        Static contrastSlider = FindSlider("Contrast exponent to use X100")
        exponent = contrastSlider.Value / 100
    End Sub
End Class






Public Class Options_Grid : Inherits VB_Parent
    Public desiredFPS As Integer
    Public width As Integer
    Public height As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Grid Cell Width", 1, dst2.Width, 32)
            sliders.setupTrackBar("Grid Cell Height", 1, dst2.Height, 32)
            sliders.setupTrackBar("Desired FPS rate", 1, 10, 2)
        End If
    End Sub
    Public Sub RunVB()
        Static widthSlider = FindSlider("Grid Cell Width")
        Static heightSlider = FindSlider("Grid Cell Height")
        Static fpsSlider = FindSlider("Desired FPS rate")
        desiredFPS = fpsSlider.value
        width = widthSlider.value
        height = heightSlider.value
    End Sub
End Class






Public Class Options_Histogram : Inherits VB_Parent
    Public minGray As Integer
    Public maxGray As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Gray", 0, 255, 50)
            sliders.setupTrackBar("Max Gray", 0, 255, 200)
        End If
    End Sub
    Public Sub RunVB()
        Static minSlider = FindSlider("Min Gray")
        Static maxSlider = FindSlider("Max Gray")
        If minSlider.Value >= maxSlider.Value Then minSlider.Value = maxSlider.Value - Math.Min(10, maxSlider.Value - 1)
        If minSlider.Value = maxSlider.Value Then maxSlider.Value += 1
        minGray = minSlider.value
        maxGray = maxSlider.value
    End Sub
End Class






Public Class Options_Guess : Inherits VB_Parent
    Public MaxDistance As Integer
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Max Distance from edge (pixels)", 0, 100, 50)
    End Sub
    Public Sub RunVB()
        Static distSlider = FindSlider("Max Distance from edge (pixels)")
        MaxDistance = distSlider.value
    End Sub
End Class





Public Class Options_Hist3D : Inherits VB_Parent
    Public addCloud As Boolean
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Add color and cloud 8UC1")
            radio.addRadio("Copy cloud into color 8UC1")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static addRadio = FindRadio("Add color and cloud 8UC1")
        addCloud = addRadio.checked
    End Sub
End Class





Public Class Options_HOG : Inherits VB_Parent
    Public thresholdHOG As Integer
    Public strideHOG As Integer
    Public scaleHOG As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("HOG Threshold", 0, 100, 0)
            sliders.setupTrackBar("HOG Stride", 1, 100, 1)
            sliders.setupTrackBar("HOG Scale", 0, 2000, 300)
        End If
    End Sub
    Public Sub RunVB()
        Static thresholdSlider = FindSlider("HOG Threshold")
        Static strideSlider = FindSlider("HOG Stride")
        Static scaleSlider = FindSlider("HOG Scale")

        thresholdHOG = thresholdSlider.Value
        strideHOG = CInt(strideSlider.Value)
        scaleHOG = scaleSlider.Value / 1000
    End Sub
End Class






Public Class Options_Images : Inherits VB_Parent
    Public fileNameForm As OptionsFileName
    Public fileIndex As Integer
    Public fileNameList As New List(Of String)
    Public fileInputName As FileInfo
    Public dirName As String
    Public imageSeries As Boolean
    Public fullsizeImage As cv.Mat
    Public Sub New()
        fileNameForm = New OptionsFileName
        dirName = task.HomeDir + "Images/train"
        fileNameForm.OpenFileDialog1.InitialDirectory = dirName
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "Image_Basics_Name", "Image_Basics_Name", task.HomeDir + "Images/train/2092.jpg")
        fileNameForm.Text = "Select an image file for use in OpenCVB"
        fileNameForm.FileNameLabel.Text = "Select a file."
        fileNameForm.PlayButton.Hide()
        fileNameForm.TrackBar1.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        Dim inputDir = New DirectoryInfo(dirName)
        Dim fileList As IO.FileInfo() = inputDir.GetFiles("*.jpg")
        For Each file In fileList
            fileNameList.Add(file.FullName)
        Next

        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Load the next image")
        End If
    End Sub
    Public Sub RunVB()
        Static nextCheck = FindCheckBox("Load the next image")
        If (task.heartBeat And imageSeries) Or fullsizeImage Is Nothing Then nextCheck.checked = True
        If nextCheck.checked = True Then
            If nextCheck.checked Then fileIndex += 1
            If fileIndex >= fileNameList.Count Then fileIndex = 0
            fileInputName = New FileInfo(fileNameList(fileIndex))
            fullsizeImage = cv.Cv2.ImRead(fileInputName.FullName)
        End If
        nextCheck.checked = False
    End Sub
End Class






Public Class Options_VerticalVerify : Inherits VB_Parent
    Public angleThreshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum Arc-Y threshold angle (degrees)", 70, 90, 80)
        End If
    End Sub
    Public Sub RunVB()
        Static arcYslider = FindSlider("Minimum Arc-Y threshold angle (degrees)")
        angleThreshold = arcYslider.Value
    End Sub
End Class







Public Class Options_IMUPlot : Inherits VB_Parent
    Public setBlue As Boolean
    Public setGreen As Boolean
    Public setRed As Boolean
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Blue Variable")
            check.addCheckBox("Green Variable")
            check.addCheckBox("Red Variable")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
            check.Box(2).Checked = True
        End If
    End Sub
    Public Sub RunVB()
        Static blueCheck = FindCheckBox("Blue Variable")
        Static greenCheck = FindCheckBox("Green Variable")
        Static redCheck = FindCheckBox("Red Variable")
        setBlue = blueCheck.checked
        setGreen = greenCheck.checked
        setRed = redCheck.checked
    End Sub
End Class





Public Class Options_Kalman_VB : Inherits VB_Parent
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
        Static inputSlider = FindSlider("Move this to see results")
        Static noisyInputSlider = FindSlider("Input with Noise")
        Static pointSlider = FindSlider("20 point average of output")
        Static avgSlider = FindSlider("20 Point average difference")
        Static noiseSlider = FindSlider("Simulated Noise")
        Static biasSlider = FindSlider("Simulated Bias")
        Static scaleSlider = FindSlider("Simulated Scale")
        Static outputSlider = FindSlider("Kalman Output")
        Static kDiffSlider = FindSlider("Kalman difference")

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






Public Class Options_Kalman : Inherits VB_Parent
    Public delta As Single
    Public pdotEntry As Single
    Public processCovar As Single
    Public averageInputCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Average input count", 1, 500, 20)
            sliders.setupTrackBar("Delta Time X100", 1, 30, 5)
            sliders.setupTrackBar("Process Covariance X10000", 0, 10000, 10)
            sliders.setupTrackBar("pDot entry X1000", 0, 1000, 300)
        End If
    End Sub
    Public Sub RunVB()
        Static deltaSlider = FindSlider("Delta Time X100")
        Static covarSlider = FindSlider("Process Covariance X10000")
        Static pDotSlider = FindSlider("pDot entry X1000")
        Static avgSlider = FindSlider("Average input count")
        delta = deltaSlider.Value / 100
        pdotEntry = pDotSlider.Value / 1000
        processCovar = covarSlider.Value / 10000
        averageInputCount = avgSlider.value
    End Sub
End Class
