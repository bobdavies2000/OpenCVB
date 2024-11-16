Imports cvb = OpenCvSharp
Imports System.IO
Imports System.Numerics
Imports OpenCvSharp.ML
Imports System.Windows.Forms

Public Class Options_Annealing : Inherits TaskParent
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
    Public Sub RunOpt()
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






Public Class Options_CamShift : Inherits TaskParent
    Public camMax As Integer = 255
    Public camSBins As cvb.Scalar = New cvb.Scalar(0, 40, 32)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("CamShift vMin", 0, 255, camSBins(2))
            sliders.setupTrackBar("CamShift vMax", 0, 255, camMax)
            sliders.setupTrackBar("CamShift Smin", 0, 255, camSBins(1))
        End If
    End Sub
    Public Sub RunOpt()
        Static vMinSlider = FindSlider("CamShift vMin")
        Static vMaxSlider = FindSlider("CamShift vMax")
        Static sMinSlider = FindSlider("CamShift Smin")

        Dim vMin = vMinSlider.Value
        Dim vMax = vMaxSlider.Value
        Dim sMin = sMinSlider.Value

        Dim min = Math.Min(vMin, vMax)
        camMax = Math.Max(vMin, vMax)
        camSBins = New cvb.Scalar(0, sMin, min)
    End Sub
End Class







Public Class Options_Contours2 : Inherits TaskParent
    Public ApproximationMode As cvb.ContourApproximationModes = cvb.ContourApproximationModes.ApproxTC89KCOS
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("ApproxNone")
            radio.addRadio("ApproxSimple")
            radio.addRadio("ApproxTC89KCOS")
            radio.addRadio("ApproxTC89L1")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static radioChoices() As cvb.ContourApproximationModes = {cvb.ContourApproximationModes.ApproxNone,
                                cvb.ContourApproximationModes.ApproxSimple, cvb.ContourApproximationModes.ApproxTC89KCOS,
                                cvb.ContourApproximationModes.ApproxTC89L1}
        Static frm = FindFrm(traceName + " Radio Buttons")
        ApproximationMode = radioChoices(findRadioIndex(frm.check))
    End Sub
End Class





Public Class Options_Contours : Inherits TaskParent
    Public retrievalMode As cvb.RetrievalModes = cvb.RetrievalModes.External
    Public ApproximationMode As cvb.ContourApproximationModes = cvb.ContourApproximationModes.ApproxTC89KCOS
    Public epsilon As Double = 0.03
    Public minPixels As Integer = 30
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
            sliders.setupTrackBar("Min Pixels", 1, 2000, minPixels)
            sliders.setupTrackBar("Max contours", 1, 200, maxContourCount)
            sliders.setupTrackBar("TrueText offset", 1, dst2.Width / 3, trueTextOffset)
        End If
    End Sub
    Public Sub RunOpt()
        options2.RunOpt()
        Static minSlider = FindSlider("Min Pixels")
        Static countSlider = FindSlider("Max contours")
        Static offsetSlider = FindSlider("TrueText offset")
        maxContourCount = countSlider.value

        ' epsilon = epsilonSlider.Value / 100

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                retrievalMode = Choose(i + 1, cvb.RetrievalModes.CComp, cvb.RetrievalModes.External,
                                    cvb.RetrievalModes.FloodFill, cvb.RetrievalModes.List,
                                    cvb.RetrievalModes.Tree)
                Exit For
            End If
        Next
        ApproximationMode = options2.ApproximationMode
        minPixels = minSlider.value
        trueTextOffset = offsetSlider.value
    End Sub
End Class




Public Class Options_DepthTiers : Inherits TaskParent
    Public pcSplitIndex As Integer
    Public cmPerTier As Integer = 50
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("X-Range")
            radio.addRadio("Y-Range")
            radio.addRadio("Z-Range")
            radio.check(2).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("cm's per tier", 10, 200, cmPerTier)
    End Sub
    Public Sub RunOpt()
        Static cmSlider = FindSlider("cm's per tier")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                pcSplitIndex = i
                Exit For
            End If
        Next
        cmPerTier = cmSlider.value
    End Sub
End Class






' https://answers.opencvb.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Options_Encode : Inherits TaskParent
    Public qualityLevel As Integer = 1
    Public scalingLevel As Integer = 85
    Public encodeOption = cvb.ImwriteFlags.JpegProgressive
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
    Public Sub RunOpt()
        Static qualitySlider = FindSlider("Encode Quality Level")
        Static scalingSlider = FindSlider("Encode Output Scaling")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                encodeOption = Choose(i + 1, cvb.ImwriteFlags.JpegChromaQuality, cvb.ImwriteFlags.JpegLumaQuality, cvb.ImwriteFlags.JpegOptimize, cvb.ImwriteFlags.JpegProgressive,
                                            cvb.ImwriteFlags.JpegQuality, cvb.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next
        qualityLevel = qualitySlider.Value
        scalingLevel = scalingSlider.value
        If encodeOption = cvb.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cvb.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
    End Sub
End Class







Public Class Options_Filter : Inherits TaskParent
    Public kernelSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter kernel size", 1, 21, kernelSize)
    End Sub
    Public Sub RunOpt()
        Static kernelSlider = FindSlider("Filter kernel size")
        kernelSize = kernelSlider.value Or 1
    End Sub
End Class






Public Class Options_GeneticDrawing : Inherits TaskParent
    Public stageTotal As Integer = 100
    Public brushPercent As Double = 1.0
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
    Public Sub RunOpt()
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







Public Class Options_MatchShapes : Inherits TaskParent
    Public matchOption As cvb.ShapeMatchModes = cvb.ShapeMatchModes.I1
    Public matchThreshold As Double = 0.4
    Public maxYdelta As Double = 0.05
    Public minSize As Double = 120
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
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Match Threshold %")
        Static ySlider = FindSlider("Max Y Delta % (of height)")
        Static minSlider = FindSlider("Min Size % of image size")
        matchThreshold = thresholdSlider.Value / 100
        maxYdelta = ySlider.Value * dst2.Height / 100
        minSize = minSlider.value * dst2.Total / 100

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cvb.ShapeMatchModes.I1, cvb.ShapeMatchModes.I2, cvb.ShapeMatchModes.I3)
                Exit For
            End If
        Next
    End Sub
End Class









Public Class Options_Plane : Inherits TaskParent
    Public rmsThreshold As Double = 0.1
    Public useMaskPoints As Boolean = False
    Public useContourPoints As Boolean = False
    Public use3Points As Boolean = False
    Public reuseRawDepthData As Boolean = False
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
    Public Sub RunOpt()
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







Public Class Options_Neighbors : Inherits TaskParent
    Public threshold As Double = 0.005
    Public pixels As Integer = 6
    Public patchZ As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Difference from neighbor in mm's", 0, 20, threshold * 1000)
            sliders.setupTrackBar("Minimum offset to neighbor pixel", 1, 100, pixels)
            sliders.setupTrackBar("Patch z-values", 0, 1, 1)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Difference from neighbor in mm's")
        Static pixelSlider = FindSlider("Minimum offset to neighbor pixel")
        Static patchSlider = FindSlider("Patch z-values")
        threshold = thresholdSlider.value / 1000
        pixels = pixelSlider.value
        patchZ = patchSlider.value = 1
    End Sub
End Class






Public Class Options_Interpolate : Inherits TaskParent
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
    Public Sub RunOpt()
        Static resizeSlider = FindSlider("Interpolation Resize %")
        Static interpolationSlider = FindSlider("Interpolation Resize %")
        Static pixelSlider = FindSlider("Number of interplation pixels that changed")
        resizePercent = resizeSlider.value
        interpolationThreshold = interpolationSlider.value
        pixelCountThreshold = pixelSlider.value
    End Sub
End Class







Public Class Options_Resize : Inherits TaskParent
    Public warpFlag As cvb.InterpolationFlags = cvb.InterpolationFlags.Nearest
    Public radioIndex As Integer = 0
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
    End Sub
    Public Sub RunOpt()
        Static offsetSlider = FindSlider("Offset from top left corner")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                warpFlag = Choose(i + 1, cvb.InterpolationFlags.Area, cvb.InterpolationFlags.Cubic, cvb.InterpolationFlags.Lanczos4,
                                        cvb.InterpolationFlags.Linear, cvb.InterpolationFlags.Nearest,
                                        cvb.InterpolationFlags.WarpFillOutliers, cvb.InterpolationFlags.WarpInverseMap)
                radioIndex = i
                Exit For
            End If
        Next
    End Sub
End Class








Public Class Options_Smoothing : Inherits TaskParent
    Public iterations As Integer = 8
    Public interiorTension As Double = 0.5
    Public stepSize As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Smoothing iterations", 1, 20, iterations)
            sliders.setupTrackBar("Smoothing tension X100 (Interior Only)", 1, 100, interiorTension * 100)
            sliders.setupTrackBar("Step size when adding points (1 is identity)", 1, 500, stepSize)
        End If
    End Sub
    Public Sub RunOpt()
        Static iterSlider = FindSlider("Smoothing iterations")
        Static tensionSlider = FindSlider("Smoothing tension X100 (Interior Only)")
        Static stepSlider = FindSlider("Step size when adding points (1 is identity)")
        iterations = iterSlider.Value
        interiorTension = tensionSlider.Value / 100
        stepSize = stepSlider.Value
    End Sub
End Class










Public Class Options_SuperRes : Inherits TaskParent
    Public method As String = "farneback"
    Public iterations As Integer = 10
    Public restartWithNewOptions As Boolean = False
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
    Public Sub RunOpt()
        Static radioChoices = {"farneback", "tvl1", "brox", "pyrlk"}
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






' https://docs.opencvb.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM2 : Inherits TaskParent
    Public SVMType As Integer = cvb.ML.SVM.Types.CSvc
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                SVMType = Choose(i + 1, cvb.ML.SVM.Types.CSvc, cvb.ML.SVM.Types.EpsSvr, cvb.ML.SVM.Types.NuSvc, cvb.ML.SVM.Types.NuSvr, cvb.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next
        If standaloneTest() Then SetTrueText(traceName + " has no output when run standaloneTest()." + vbCrLf + "It is used to setup more SVM options.")
    End Sub
End Class









' https://docs.opencvb.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM : Inherits TaskParent
    Public kernelType As cvb.ML.SVM.KernelTypes = cvb.ML.SVM.KernelTypes.Poly
    Public granularity As Integer = 5
    Public svmDegree As Double = 1
    Public gamma As Integer = 1
    Public svmCoef0 As Double = 1
    Public svmC As Double = 1
    Public svmNu As Double = 0.5
    Public svmP As Double = 0
    Public sampleCount As Integer = 500
    Dim options2 As New Options_SVM2
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
    Public Function createSVM() As cvb.ML.SVM
        Dim svm = cvb.ML.SVM.Create()
        svm.Type = options2.SVMType
        svm.KernelType = kernelType
        svm.TermCriteria = cvb.TermCriteria.Both(1000, 0.000001)
        svm.Degree = svmDegree
        svm.Gamma = gamma
        svm.Coef0 = svmCoef0
        svm.C = svmC
        svm.Nu = svmNu
        svm.P = svmP
        Return svm
    End Function

    Public Sub RunOpt()
        Static radioChoices = {cvb.ML.SVM.KernelTypes.Linear, cvb.ML.SVM.KernelTypes.Poly, cvb.ML.SVM.KernelTypes.Rbf, cvb.ML.SVM.KernelTypes.Sigmoid}
        options2.RunOpt()
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







Public Class Options_WarpModel : Inherits TaskParent
    Public useGradient As Boolean = False
    Public pkImage As cvb.Mat
    Public warpMode As Integer = 0
    Public useWarpAffine As Boolean = False
    Public useWarpHomography As Boolean = False
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
    Public Sub RunOpt()
        Static gradientCheck = FindCheckBox("Use Gradient in WarpInput")
        Static frm = FindFrm(traceName + " Radio Buttons")

        If task.optionsChanged Then
            options2.RunOpt()
            warpMode = options2.warpMode
            useWarpAffine = options2.useWarpAffine
            useWarpHomography = options2.useWarpHomography

            useGradient = gradientCheck.checked

            For i = 0 To frm.check.Count - 1
                Dim nextRadio = frm.check(i)
                If nextRadio.Checked Then
                    Dim photo As New FileInfo(task.HomeDir + "Data\Prokudin\" + nextRadio.Text)
                    pkImage = cvb.Cv2.ImRead(photo.FullName, cvb.ImreadModes.Grayscale)
                    Exit For
                End If
            Next
        End If
    End Sub
End Class










Public Class Options_MinMaxNone : Inherits TaskParent
    Public useMax As Boolean = False
    Public useMin As Boolean = False
    Public useNone As Boolean = False
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use farthest distance")
            radio.addRadio("Use closest distance")
            radio.addRadio("Use unchanged depth input")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm As OptionsRadioButtons = FindFrm(traceName + " Radio Buttons")
        useMax = frm.check(0).Checked
        useMin = frm.check(1).Checked
        useNone = frm.check(2).Checked
    End Sub
End Class






Public Class Options_MinArea : Inherits TaskParent
    Public srcPoints As New List(Of cvb.Point2f)
    Public squareWidth As Integer = 100
    Public numPoints As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area Number of Points", 1, 30, numPoints)
            sliders.setupTrackBar("Area size", 10, 300, squareWidth * 2)
        End If
    End Sub
    Public Sub RunOpt()
        Static numSlider = FindSlider("Area Number of Points")
        Static sizeSlider = FindSlider("Area size")
        Dim squareWidth = sizeSlider.Value / 2
        srcPoints.Clear()

        Dim pt As cvb.Point2f
        numPoints = numSlider.Value
        For i = 0 To numPoints - 1
            pt.X = msRNG.Next(dst2.Width / 2 - squareWidth, dst2.Width / 2 + squareWidth)
            pt.Y = msRNG.Next(dst2.Height / 2 - squareWidth, dst2.Height / 2 + squareWidth)
            srcPoints.Add(pt)
        Next
    End Sub
End Class








Public Class Options_DCT : Inherits TaskParent
    Public dctFlag As cvb.DctFlags = New cvb.DctFlags
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
    Public Sub RunOpt()
        Static removeSlider = FindSlider("Remove Frequencies < x")
        Static runLenSlider = FindSlider("Run Length Minimum")

        runLengthMin = runLenSlider.Value
        removeFrequency = removeSlider.Value
        For i = 0 To 2
            If radio.check(i).Checked Then
                dctFlag = Choose(i + 1, cvb.DctFlags.None, cvb.DctFlags.Rows, cvb.DctFlags.Inverse)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Eigen : Inherits TaskParent
    Public highlight As Boolean = False
    Public recompute As Boolean = False
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
    Public Sub RunOpt()
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







Public Class Options_FitLine : Inherits TaskParent
    Public radiusAccuracy As Integer = 10
    Public angleAccuracy As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Accuracy for the radius X100", 0, 100, radiusAccuracy)
            sliders.setupTrackBar("Accuracy for the angle X100", 0, 100, angleAccuracy)
        End If
    End Sub
    Public Sub RunOpt()
        Static radiusSlider = FindSlider("Accuracy for the radius X100")
        Static angleSlider = FindSlider("Accuracy for the angle X100")
        radiusAccuracy = radiusSlider.Value
        angleAccuracy = angleSlider.Value
    End Sub
End Class







Public Class Options_Fractal : Inherits TaskParent
    Public iterations As Integer = 34
    Public resetCheck As CheckBox
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Mandelbrot iterations", 1, 50, iterations)
        If check.Setup(traceName) Then check.addCheckBox("Reset to original Mandelbrot")
        resetCheck = FindCheckBox("Reset to original Mandelbrot")
    End Sub
    Public Sub RunOpt()
        Static iterSlider = FindSlider("Mandelbrot iterations")
        iterations = iterSlider.Value
    End Sub
End Class







Public Class Options_ProCon : Inherits TaskParent
    Public buffer(9) As Integer
    Public pduration As Integer = 1
    Public cduration As Integer = 1
    Public bufferSize As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Buffer Size", 1, 100, buffer.Length)
            sliders.setupTrackBar("Producer Workload Duration (ms)", 1, 1000, pduration)
            sliders.setupTrackBar("Consumer Workload Duration (ms)", 1, 1000, cduration)
        End If
        buffer = Enumerable.Repeat(-1, buffer.Length).ToArray
    End Sub
    Public Sub RunOpt()
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






Public Class Options_OilPaint : Inherits TaskParent
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
    Public Sub RunOpt()
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










Public Class Options_Pointilism : Inherits TaskParent
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
    Public Sub RunOpt()
        Static radiusSlider = FindSlider("Smoothing Radius")
        Static strokeSlider = FindSlider("Stroke Scale")
        Static ellipStroke = FindRadio("Use Elliptical stroke")
        smoothingRadius = radiusSlider.Value * 2 + 1
        strokeSize = strokeSlider.Value
        useElliptical = ellipStroke.checked
    End Sub
End Class







Public Class Options_MotionBlur : Inherits TaskParent
    Public showDirection As Boolean = True
    Public redoCheckBox As CheckBox
    Public kernelSize As Integer = 51
    Public theta As Double = 0
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
    Public Sub RunOpt()
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






Public Class Options_BinarizeNiBlack : Inherits TaskParent
    Public kernelSize As Integer = 51
    Public niBlackK As Double = -200 / 1000
    Public nickK As Double = 100 / 1000
    Public sauvolaK As Double = 100 / 1000
    Public sauvolaR As Double = 64
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Kernel Size", 3, 500, kernelSize)
            sliders.setupTrackBar("Niblack k", -1000, 1000, niBlackK * 1000)
            sliders.setupTrackBar("Nick k", -1000, 1000, nickK * 1000)
            sliders.setupTrackBar("Sauvola k", -1000, 1000, sauvolaK * 1000)
            sliders.setupTrackBar("Sauvola r", 1, 100, sauvolaR)
        End If
    End Sub
    Public Sub RunOpt()
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






Public Class Options_Bernson : Inherits TaskParent
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
    Public Sub RunOpt()
        Static kernelSlider = FindSlider("Kernel Size")
        Static contrastSlider = FindSlider("Contrast min")
        Static bgSlider = FindSlider("bg Threshold")
        kernelSize = kernelSlider.Value Or 1

        bgThreshold = bgSlider.Value
        contrastMin = contrastSlider.Value
    End Sub
End Class







Public Class Options_BlockMatching : Inherits TaskParent
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
    Public Sub RunOpt()
        Static matchSlider = FindSlider("Blockmatch max disparity")
        Static sizeSlider = FindSlider("Blockmatch block size")
        Static distSlider = FindSlider("Blockmatch distance in meters")
        numDisparity = matchSlider.Value * 16 ' must be a multiple of 16
        blockSize = sizeSlider.Value Or 1
        distance = distSlider.Value
    End Sub
End Class






Public Class Options_Cartoonify : Inherits TaskParent
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
    Public Sub RunOpt()
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






Public Class Options_Dither : Inherits TaskParent
    Public radioIndex As Integer = 0
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
    Public Sub RunOpt()
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







Public Class Options_SymmetricalShapes : Inherits TaskParent
    Public rotateAngle As Double = 0
    Public fillColor As cvb.Scalar = New cvb.Scalar(0, 0, 255)
    Public numPoints As Integer = 0
    Public nGenPer As Integer = 0
    Public radius1 As Integer = 0
    Public radius2 As Integer = 0
    Public dTheta As Double = 0
    Public symmetricRipple As Boolean = False
    Public reverseInOut As Boolean = False
    Public fillRequest As Boolean = False
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
    Public Sub RunOpt()
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
        dTheta = 2 * cvb.Cv2.PI / numPoints
        symmetricRipple = symCheck.Checked
        reverseInOut = reverseCheck.Checked
        fillRequest = fillCheck.checked
    End Sub
End Class






Public Class Options_DrawArc : Inherits TaskParent
    Public saveMargin As Integer = 32
    Public drawFull As Boolean = False
    Public drawFill As Boolean = False
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
    Public Sub RunOpt()
        Static marginSlider = FindSlider("Clearance from image edge (margin size)")
        Static fillCheck = FindRadio("Draw Filled Arc")
        Static fullCheck = FindRadio("Draw Full Ellipse")
        saveMargin = marginSlider.Value / 16
        drawFull = fullCheck.checked
        drawFill = fillCheck.checked
    End Sub
End Class






Public Class Options_FilterNorm : Inherits TaskParent
    Public kernel As cvb.Mat
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
    Public Sub RunOpt()
        Static alphaSlider = FindSlider("Normalize alpha X10")

        Dim normType = cvb.NormTypes.L1
        kernel = cvb.Mat.FromPixelData(1, 21, cvb.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                normType = Choose(i + 1, cvb.NormTypes.INF, cvb.NormTypes.L1, cvb.NormTypes.L2, cvb.NormTypes.MinMax)
                Exit For
            End If
        Next

        kernel = kernel.Normalize(alphaSlider.Value / 10, 0, normType)
    End Sub
End Class





Public Class Options_SepFilter2D : Inherits TaskParent
    Public xDim As Integer = 5
    Public yDim As Integer = 11
    Public sigma As Double = 17
    Public diffCheck As Boolean = False
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
    Public Sub RunOpt()
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






Public Class Options_IMUFrameTime : Inherits TaskParent
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
    Public Sub RunOpt()
        Static minSliderHost = FindSlider("Minimum Host interrupt delay (ms)")
        Static minSliderIMU = FindSlider("Minimum IMU to Capture time (ms)")
        Static plotSlider = FindSlider("Number of Plot Values")
        minDelayIMU = minSliderIMU.Value
        minDelayHost = minSliderHost.Value
        plotLastX = plotSlider.Value
    End Sub
End Class






Public Class Options_KLT : Inherits TaskParent
    Public ptInput() As cvb.Point2f
    Public maxCorners As Integer = 100
    Public qualityLevel As Double = 0.01
    Public minDistance As Integer = 7
    Public blockSize As Integer = 7
    Public nightMode As Boolean = False
    Public subPixWinSize As cvb.Size = New cvb.Size(10, 10)
    Public winSize As cvb.Size = New cvb.Size(3, 3)
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
    Public Sub RunOpt()
        Static maxSlider = FindSlider("KLT - MaxCorners")
        Static qualitySlider = FindSlider("KLT - qualityLevel")
        Static minSlider = FindSlider("KLT - minDistance")
        Static blockSlider = FindSlider("KLT - BlockSize")
        Static nightCheck = FindCheckBox("KLT - Night Mode")
        Static deleteCheck = FindCheckBox("KLT - delete all Points")

        If deleteCheck.Checked Or task.heartBeat Then
            ptInput = Nothing ' just delete all points and start again.
            deleteCheck.Checked = False
        End If

        maxCorners = maxSlider.Value
        qualityLevel = qualitySlider.Value / 100
        minDistance = minSlider.Value
        blockSize = blockSlider.Value
        nightMode = nightCheck.Checked
    End Sub
End Class







Public Class Options_Laplacian : Inherits TaskParent
    Public kernel As cvb.Size = New cvb.Size(3, 3)
    Public scale As Double = 1
    Public delta As Double = 0
    Public gaussianBlur As Boolean = False
    Public boxFilterBlur As Boolean = False
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
    Public Sub RunOpt()
        Static kernelSlider = FindSlider("Laplacian Kernel size")
        Static scaleSlider = FindSlider("Laplacian Scale")
        Static deltaSlider = FindSlider("Laplacian Delta")
        Static thresholdSlider = FindSlider("Laplacian Threshold")
        Static blurCheck = FindRadio("Add Gaussian Blur")
        Static boxCheck = FindRadio("Add boxfilter Blur")
        Dim kernelSize As Integer = kernelSlider.Value Or 1
        scale = scaleSlider.Value / 100
        delta = deltaSlider.Value / 100
        kernel = New cvb.Size(kernelSize, kernelSize)
        gaussianBlur = blurCheck.checked
        boxFilterBlur = boxCheck.checked
        threshold = thresholdSlider.value
    End Sub
End Class








Public Class Options_OpticalFlow : Inherits TaskParent
    Public pyrScale As Double = 0.35
    Public levels As Integer = 1
    Public winSize As Integer = 1
    Public iterations As Integer = 1
    Public polyN As Double = 0
    Public polySigma As Double = 0
    Public OpticalFlowFlags As cvb.OpticalFlowFlags = cvb.OpticalFlowFlags.FarnebackGaussian
    Public outputScaling As Integer = 0
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
    Public Sub RunOpt()
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
                OpticalFlowFlags = Choose(i + 1, cvb.OpticalFlowFlags.FarnebackGaussian, cvb.OpticalFlowFlags.LkGetMinEigenvals, cvb.OpticalFlowFlags.None,
                                                cvb.OpticalFlowFlags.PyrAReady, cvb.OpticalFlowFlags.PyrBReady, cvb.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
        outputScaling = flowSlider.Value
    End Sub
End Class






Public Class Options_OpticalFlowSparse : Inherits TaskParent
    Public OpticalFlowFlag As cvb.OpticalFlowFlags = cvb.OpticalFlowFlags.FarnebackGaussian
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                OpticalFlowFlag = Choose(i + 1, cvb.OpticalFlowFlags.FarnebackGaussian, cvb.OpticalFlowFlags.LkGetMinEigenvals, cvb.OpticalFlowFlags.None,
                                            cvb.OpticalFlowFlags.PyrAReady, cvb.OpticalFlowFlags.PyrBReady, cvb.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
    End Sub
End Class







Public Class Options_XPhoto : Inherits TaskParent
    Public colorCode As Integer = cvb.ColorConversionCodes.BGR2GRAY
    Public dynamicRatio As Integer = 0
    Public blockSize As Integer = 0
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
    Public Sub RunOpt()
        Static ratioSlider = FindSlider("XPhoto Dynamic Ratio")
        Static sizeSlider = FindSlider("XPhoto Block Size")
        dynamicRatio = ratioSlider.Value
        blockSize = sizeSlider.Value
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                colorCode = Choose(i + 1, cvb.ColorConversionCodes.BGR2GRAY, cvb.ColorConversionCodes.BGR2HSV, cvb.ColorConversionCodes.BGR2YUV,
                                        cvb.ColorConversionCodes.BGR2XYZ, cvb.ColorConversionCodes.BGR2Lab)
                Exit For
            End If
        Next
    End Sub
End Class







Public Class Options_InPaint : Inherits TaskParent
    Public telea As Boolean = False
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("TELEA")
            radio.addRadio("Navier-Stokes")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static teleaRadio = FindRadio("TELEA")
        telea = teleaRadio.checked
    End Sub
End Class










Public Class Options_RotatePoly : Inherits TaskParent
    Public changeCheck As CheckBox
    Public angleSlider As TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Amount to rotate triangle", -180, 180, 10)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Change center of rotation and triangle")
        End If

        angleSlider = FindSlider("Amount to rotate triangle")
        changeCheck = FindCheckBox("Change center of rotation and triangle")
    End Sub
    Public Sub RunOpt()
    End Sub
End Class












Public Class Options_FPoly : Inherits TaskParent
    Public removeThreshold As Integer = 4
    Public autoResyncAfterX As Integer = 500
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resync if feature moves > X pixels", 1, 20, removeThreshold)
            sliders.setupTrackBar("Points to use in Feature Poly", 2, 100, 10)
            sliders.setupTrackBar("Automatically resync after X frames", 10, 1000, autoResyncAfterX)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Static pointSlider = FindSlider("Points to use in Feature Poly")
        Static resyncSlider = FindSlider("Automatically resync after X frames")
        removeThreshold = thresholdSlider.Value
        task.polyCount = pointSlider.Value
        autoResyncAfterX = resyncSlider.Value
    End Sub
End Class













Public Class Options_Homography : Inherits TaskParent
    Public hMethod = cvb.HomographyMethods.None
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                hMethod = Choose(i + 1, cvb.HomographyMethods.None, cvb.HomographyMethods.LMedS, cvb.HomographyMethods.Ransac,
                                cvb.HomographyMethods.Rho, cvb.HomographyMethods.USAC_DEFAULT,
                                cvb.HomographyMethods.USAC_PARALLEL, cvb.HomographyMethods.USAC_FM_8PTS,
                                cvb.HomographyMethods.USAC_FAST, cvb.HomographyMethods.USAC_ACCURATE,
                                cvb.HomographyMethods.USAC_PROSAC, cvb.HomographyMethods.USAC_MAGSAC)
                Exit For
            End If
        Next
    End Sub
End Class












Public Class Options_Random : Inherits TaskParent
    Public count As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Pixel Count", 1, dst2.Cols * dst2.Rows, 20)
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Random Pixel Count")
        count = countSlider.value
    End Sub
End Class








Public Class Options_Hough : Inherits TaskParent
    Public rho As Integer = 1
    Public theta As Double = 1000 * Math.PI / 180
    Public threshold As Integer = 3
    Public lineCount As Integer = 25
    Public relativeIntensity As Double = 90 / 1000
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Relative Intensity (Accord)", 1, 100, relativeIntensity * 1000)
            sliders.setupTrackBar("Hough rho", 1, 100, rho)
            sliders.setupTrackBar("Hough theta", 1, 1000, theta * 1000)
            sliders.setupTrackBar("Hough threshold", 1, 500, threshold)
            sliders.setupTrackBar("Lines to Plot", 1, 1000, lineCount)
            sliders.setupTrackBar("Minimum feature pixels", 0, 250, 25)
        End If
    End Sub
    Public Sub RunOpt()
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







Public Class Options_Canny : Inherits TaskParent
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
    Public Sub RunOpt()
        Static t1Slider = FindSlider("Canny threshold1")
        Static t2Slider = FindSlider("Canny threshold2")
        Static apertureSlider = FindSlider("Canny Aperture")

        threshold1 = t1Slider.Value
        threshold2 = t2Slider.Value
        aperture = apertureSlider.Value Or 1
    End Sub
End Class







Public Class Options_ColorMatch : Inherits TaskParent
    Public maxDistanceCheck As Boolean = False
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show Max Distance point")
        End If
    End Sub
    Public Sub RunOpt()
        Static maxCheck = FindCheckBox("Show Max Distance point")
        maxDistanceCheck = maxCheck.checked
    End Sub
End Class









Public Class Options_Sort : Inherits TaskParent
    Public sortOption As cvb.SortFlags = cvb.SortFlags.EveryColumn + cvb.SortFlags.Ascending
    Public radio0 As RadioButton
    Public radio1 As RadioButton
    Public radio2 As RadioButton
    Public radio3 As RadioButton
    Public radio4 As RadioButton
    Public radio5 As RadioButton
    Public sortThreshold As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for sort input", 0, 255, 127)
        End If

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
    Public Sub RunOpt()
        Static sortSlider = FindSlider("Threshold for sort input")
        If radio1.Checked Then sortOption = cvb.SortFlags.EveryColumn + cvb.SortFlags.Descending
        If radio2.Checked Then sortOption = cvb.SortFlags.EveryRow + cvb.SortFlags.Ascending
        If radio3.Checked Then sortOption = cvb.SortFlags.EveryRow + cvb.SortFlags.Descending
        sortThreshold = sortSlider.value
    End Sub
End Class






Public Class Options_Distance : Inherits TaskParent
    Public distanceType As cvb.DistanceTypes = cvb.DistanceTypes.L1
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("C")
            radio.addRadio("L1")
            radio.addRadio("L2")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static cRadio = FindRadio("C")
        Static l1Radio = FindRadio("L1")
        Static l2Radio = FindRadio("L2")
        If cRadio.Checked Then distanceType = cvb.DistanceTypes.C
        If l1Radio.Checked Then distanceType = cvb.DistanceTypes.L1
        If l2Radio.Checked Then distanceType = cvb.DistanceTypes.L2
    End Sub
End Class









Public Class Options_Warp : Inherits TaskParent
    Public alpha As Double = 0
    Public beta As Double = 0
    Public gamma As Double = 0
    Public f As Double = 0
    Public distance As Double = 0
    Public transformMatrix As cvb.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Alpha", 0, 180, 90)
            sliders.setupTrackBar("Beta", 0, 180, 90)
            sliders.setupTrackBar("Gamma", 0, 180, 90)
            sliders.setupTrackBar("f", 0, 2000, 600)
            sliders.setupTrackBar("distance", 0, 2000, 400)
        End If
    End Sub
    Public Sub RunOpt()
        Static alphaSlider = FindSlider("Alpha")
        Static betaSlider = FindSlider("Beta")
        Static gammaSlider = FindSlider("Gamma")
        Static fSlider = FindSlider("f")
        Static distanceSlider = FindSlider("distance")

        alpha = CDbl(alphaSlider.value - 90) * cvb.Cv2.PI / 180
        beta = CDbl(betaSlider.value - 90) * cvb.Cv2.PI / 180
        gamma = CDbl(gammaSlider.value - 90) * cvb.Cv2.PI / 180
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

        Dim a1 = cvb.Mat.FromPixelData(4, 3, cvb.MatType.CV_64F, a)
        Dim rx = cvb.Mat.FromPixelData(4, 4, cvb.MatType.CV_64F, x)
        Dim ry = cvb.Mat.FromPixelData(4, 4, cvb.MatType.CV_64F, y)
        Dim rz = cvb.Mat.FromPixelData(4, 4, cvb.MatType.CV_64F, z)

        Dim tt = cvb.Mat.FromPixelData(4, 4, cvb.MatType.CV_64F, t)
        Dim a2 = cvb.Mat.FromPixelData(3, 4, cvb.MatType.CV_64F, b)

        Dim r = rx * ry * rz
        transformMatrix = a2 * (tt * (r * a1))
    End Sub
End Class








Public Class Options_HistCompare : Inherits TaskParent
    Public compareMethod As cvb.HistCompMethods = cvb.HistCompMethods.Correl
    Public compareName As String = "Chi Square Alt"
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
    Public Sub RunOpt()
        Static correlationRadio = FindRadio("Compare using Correlation")
        Static chiRadio = FindRadio("Compare using Chi-squared")
        Static intersectionRadio = FindRadio("Compare using intersection")
        Static bhattRadio = FindRadio("Compare using Bhattacharyya")
        Static chiAltRadio = FindRadio("Compare using Chi-squared Alt")
        Static hellingerRadio = FindRadio("Compare using Hellinger")
        Static kldivRadio = FindRadio("Compare using KLDiv")

        If correlationRadio.checked Then compareMethod = cvb.HistCompMethods.Correl
        If chiRadio.checked Then compareMethod = cvb.HistCompMethods.Chisqr
        If intersectionRadio.checked Then compareMethod = cvb.HistCompMethods.Intersect
        If bhattRadio.checked Then compareMethod = cvb.HistCompMethods.Bhattacharyya
        If chiAltRadio.checked Then compareMethod = cvb.HistCompMethods.ChisqrAlt
        If kldivRadio.checked Then compareMethod = cvb.HistCompMethods.KLDiv
        If hellingerRadio.checked Then compareMethod = cvb.HistCompMethods.Hellinger

        If correlationRadio.checked Then compareName = "Correlation"
        If chiRadio.checked Then compareName = "Chi Square"
        If intersectionRadio.checked Then compareName = "Intersection"
        If bhattRadio.checked Then compareName = "Bhattacharyya"
        If chiAltRadio.checked Then compareName = "Chi Square Alt"
        If kldivRadio.checked Then compareName = "KLDiv"
        If hellingerRadio.checked Then compareName = "Hellinger"
    End Sub
End Class












Public Class Options_MatchCell : Inherits TaskParent
    Public overlapPercent As Double = 0.5
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent overlap", 0, 100, overlapPercent * 100)
    End Sub
    Public Sub RunOpt()
        Static overlapSlider = FindSlider("Percent overlap")
        overlapPercent = overlapSlider.value / 100
    End Sub
End Class








Public Class Options_Extrinsics : Inherits TaskParent
    Public leftCorner As Integer = 0
    Public rightCorner As Integer = 0
    Public topCorner As Integer = 0
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
    Public Sub RunOpt()
        Static leftSlider = FindSlider("Left image percent")
        Static rightSlider = FindSlider("Right image percent")
        Static heightSlider = FindSlider("Height percent")
        leftCorner = dst2.Width * leftSlider.value / 100
        rightCorner = dst2.Width * rightSlider.value / 100
        topCorner = dst2.Height * heightSlider.value / 100
    End Sub
End Class







Public Class Options_Translation : Inherits TaskParent
    Public leftTrans As Integer = 0
    Public rightTrans As Integer = 0
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
    Public Sub RunOpt()
        Static leftSlider = FindSlider("Left translation percent")
        Static rightSlider = FindSlider("Right translation percent")
        leftTrans = dst2.Width * leftSlider.value / 100
        rightTrans = dst2.Width * rightSlider.value / 100
    End Sub
End Class












Public Class Options_OpenGL_Contours : Inherits TaskParent
    Public depthPointStyle As Integer = 0
    Public filterThreshold As Double = 0.3
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
    Public Sub RunOpt()
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











Public Class Options_Motion : Inherits TaskParent
    Public motionThreshold As Integer = 0
    Public cumulativePercentThreshold As Double = 0.1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Single frame motion threshold", 1, dst2.Total / 4, dst2.Total / 16)
            sliders.setupTrackBar("Cumulative motion threshold percent of image", 1, 100, cumulativePercentThreshold * 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Single frame motion threshold")
        Static percentSlider = FindSlider("Cumulative motion threshold percent of image")
        motionThreshold = thresholdSlider.value
        cumulativePercentThreshold = percentSlider.value / 100
    End Sub
End Class








Public Class Options_Emax : Inherits TaskParent
    Public predictionStepSize As Integer = 5
    Public consistentcolors As Boolean = False
    Public covarianceType = cvb.EMTypes.CovMatDefault
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
    Public Sub RunOpt()
        Static colorCheck = FindCheckBox("Use palette to keep colors consistent")
        Static stepSlider = FindSlider("EMax Prediction Step Size")
        predictionStepSize = stepSlider.value
        covarianceType = cvb.EMTypes.CovMatDefault
        consistentcolors = colorCheck.checked
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                covarianceType = Choose(i + 1, cvb.EMTypes.CovMatSpherical, cvb.EMTypes.CovMatDiagonal, cvb.EMTypes.CovMatGeneric)
            End If
        Next
    End Sub
End Class









Public Class Options_Intercepts : Inherits TaskParent
    Public interceptRange As Integer = 10
    Public mouseMovePoint As Integer = 0
    Public selectedIntercept As Integer = 0
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
    Public Sub showIntercepts(mousePoint As cvb.Point, dst As cvb.Mat)
    End Sub
    Public Sub RunOpt()
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






Public Class Options_PlaneEstimation : Inherits TaskParent
    Public useDiagonalLines As Boolean = False
    Public useContour_SidePoints As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use diagonal lines")
            radio.addRadio("Use horizontal And vertical lines")
            radio.addRadio("Use Contour_SidePoints to find the line pair")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static diagonalRadio = FindRadio("Use diagonal lines")
        Static sidePointsRadio = FindRadio("Use Contour_SidePoints to find the line pair")
        useDiagonalLines = diagonalRadio.checked
        useContour_SidePoints = sidePointsRadio.checked
    End Sub
End Class







Public Class Options_ForeGround : Inherits TaskParent
    Public maxForegroundDepthInMeters As Single = 1500 / 1000
    Public minSizeContour As Integer = 100
    Public depthPerRegion As Double = 0
    Public numberOfRegions As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max foreground depth in mm's", 200, 2000, maxForegroundDepthInMeters * 1000)
            sliders.setupTrackBar("Min length contour", 10, 2000, minSizeContour)
            sliders.setupTrackBar("Number of depth ranges", 1, 20, numberOfRegions)
        End If
    End Sub
    Public Sub RunOpt()
        Static depthSlider = FindSlider("Max foreground depth in mm's")
        Static minSizeSlider = FindSlider("Min length contour")
        Static regionSlider = FindSlider("Number of depth ranges")
        maxForegroundDepthInMeters = depthSlider.value / 1000
        minSizeContour = minSizeSlider.value
        numberOfRegions = regionSlider.value
        depthPerRegion = task.MaxZmeters / numberOfRegions
    End Sub
End Class






Public Class Options_Flood : Inherits TaskParent
    Public floodFlag As cvb.FloodFillFlags = 4 Or cvb.FloodFillFlags.FixedRange
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
    Public Sub RunOpt()
        Static floatingCheck = FindCheckBox("Use floating range")
        Static connectCheck = FindCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        Static stepSlider = FindSlider("Step Size")
        Static minSlider = FindSlider("Min Pixels")

        stepSize = stepSlider.Value
        floodFlag = If(connectCheck.checked, 8, 4) Or If(floatingCheck.checked, cvb.FloodFillFlags.FixedRange, 0)
        minPixels = minSlider.value
    End Sub
End Class






Public Class Options_ShapeDetect : Inherits TaskParent
    Public fileName As String = ""
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        fileName = findRadioText(frm.check)
    End Sub
End Class








Public Class Options_Blur : Inherits TaskParent
    Public kernelSize As Integer = 3
    Public sigma As Double = 1.5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blur Kernel Size", 0, 32, kernelSize)
            sliders.setupTrackBar("Blur Sigma", 1, 10, sigma * 2)
        End If
    End Sub
    Public Sub RunOpt()
        Static kernelSlider = FindSlider("Blur Kernel Size")
        Static sigmaSlider = FindSlider("Blur Sigma")
        kernelSize = kernelSlider.Value Or 1
        sigma = sigmaSlider.value * 0.5
    End Sub
End Class









Public Class Options_Wavelet : Inherits TaskParent
    Public useHaar As Boolean = True
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
    Public Sub RunOpt()
        Static iterSlider = FindSlider("Wavelet Iterations")
        Static haarRadio = FindRadio("Haar")
        useHaar = haarRadio.checked
        iterations = iterSlider.value
    End Sub
End Class









Public Class Options_SOM : Inherits TaskParent
    Public iterations As Integer = 3000
    Public learningRate As Double = 0.1
    Public radius As Integer = 15
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Iterations (000's)", 1, 10, iterations / 1000)
            sliders.setupTrackBar("Initial Learning Rate %", 1, 100, learningRate * 100)
            sliders.setupTrackBar("Radius in Pixels", 1, 100, radius)
        End If
    End Sub
    Public Sub RunOpt()
        Static iterSlider = FindSlider("Iterations (000's)")
        Static learnSlider = FindSlider("Initial Learning Rate %")
        Static radiusSlider = FindSlider("Radius in Pixels")
        iterations = iterSlider.value * 1000
        learningRate = learnSlider.value / 100
        radius = radiusSlider.value
    End Sub
End Class









Public Class Options_SURF : Inherits TaskParent
    Public hessianThreshold As Integer = 2000
    Public useBFMatcher As Boolean = True
    Public verticalRange As Integer = 1
    Public pointCount As Integer = 200
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use BF Matcher")
            radio.addRadio("Use Flann Matcher")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Hessian threshold", 1, 5000, hessianThreshold)
            sliders.setupTrackBar("Surf Vertical Range to Search", 0, 50, verticalRange)
            sliders.setupTrackBar("Points to Match", 1, 1000, pointCount)
        End If
    End Sub
    Public Sub RunOpt()
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








Public Class Options_Sift : Inherits TaskParent
    Public useBFMatcher As Boolean = False
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
    Public Sub RunOpt()
        Static bfRadio = FindRadio("Use BF Matcher")
        Static countSlider = FindSlider("Points to Match")
        Static stepSlider = FindSlider("Sift StepSize")

        useBFMatcher = bfRadio.checked
        pointCount = countSlider.value
        stepSize = stepSlider.value
    End Sub
End Class







Public Class Options_Dilate : Inherits TaskParent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cvb.MorphShapes = cvb.MorphShapes.Cross
    Public element As cvb.Mat
    Public noshape As Boolean = False
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
    Public Sub RunOpt()
        Static ellipseRadio = FindRadio("Dilate shape: Ellipse")
        Static rectRadio = FindRadio("Dilate shape: Rect")
        Static iterSlider = FindSlider("Dilate Iterations")
        Static kernelSlider = FindSlider("Dilate Kernel Size")
        Static noShapeRadio = FindRadio("Dilate shape: None")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cvb.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cvb.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cvb.MorphShapes.Rect
        element = cvb.Cv2.GetStructuringElement(morphShape, New cvb.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
    End Sub
End Class









Public Class Options_KMeans : Inherits TaskParent
    Public kMeansFlag As cvb.KMeansFlags = cvb.KMeansFlags.RandomCenters
    Public kMeansK As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KMeans k", 2, 32, kMeansK)
            sliders.setupTrackBar("Dimension", 1, 6, 1)
        End If

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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static kSlider = FindSlider("KMeans k")
        Select Case findRadioText(frm.check)
            Case "Use PpCenters"
                kMeansFlag = cvb.KMeansFlags.PpCenters
            Case "Use RandomCenters"
                kMeansFlag = cvb.KMeansFlags.RandomCenters
            Case "Use Initialized Labels"
                If task.optionsChanged Then kMeansFlag = cvb.KMeansFlags.PpCenters Else kMeansFlag = cvb.KMeansFlags.UseInitialLabels
        End Select
        kMeansK = kSlider.Value
    End Sub
End Class








Public Class Options_LUT : Inherits TaskParent
    Public lutSegments As Integer = 10
    Public splits(4) As Integer
    Public vals(4) As Integer
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
    Public Sub RunOpt()
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








Public Class Options_WarpModel2 : Inherits TaskParent
    Public warpMode As Integer = 0
    Public useWarpAffine As Boolean = False
    Public useWarpHomography As Boolean = False
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Motion_Translation (fastest)")
            radio.addRadio("Motion_Euclidean")
            radio.addRadio("Motion_Affine (very slow - Be sure to configure CPP_Native in Release Mode)")
            radio.addRadio("Motion_Homography (even slower - Use CPP_Native in Release Mode)")
            radio.check(0).Checked = True
        End If

        desc = "Additional WarpModel options - needed an additional radio button set."
    End Sub
    Public Sub RunOpt()
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
Public Class Options_Photoshop : Inherits TaskParent
    Public switchColor As Integer = 3
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Second DuoTone Blue")
            radio.addRadio("Second DuoTone Green")
            radio.addRadio("Second DuoTone Red")
            radio.addRadio("Second DuoTone None")
            radio.check(switchColor).Checked = True
        End If

        desc = "More options for the DuoTone image"
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For switchColor = 0 To frm.check.Count - 1
            If frm.check(switchColor).Checked Then Exit For
        Next
    End Sub
End Class





Public Class Options_Gif : Inherits TaskParent
    Public buildCheck As CheckBox
    Public restartCheck As CheckBox
    Public dst0Radio As RadioButton
    Public dst1Radio As RadioButton
    Public dst2Radio As RadioButton
    Public dst3Radio As RadioButton
    Public OpenCVBwindow As RadioButton
    Public OpenGLwindow As RadioButton
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
    Public Sub RunOpt()
        Static frmCheck = FindFrm(traceName + " CheckBoxes")
        Static frmRadio = FindFrm(traceName + " Radio Buttons")
        frmCheck.Left = task.gOptions.Width / 2
        frmCheck.top = task.gOptions.Height / 2
        frmRadio.left = task.gOptions.Width * 2 / 3
        frmRadio.top = task.gOptions.Height * 2 / 3

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







Public Class Options_IMU : Inherits TaskParent
    Public rotateX As Integer = 0
    Public rotateY As Integer = 0
    Public rotateZ As Integer = 0
    Public stableThreshold As Double = 0.02
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Rotate pointcloud around X-axis (degrees)", -90, 90, rotateX)
            sliders.setupTrackBar("Rotate pointcloud around Y-axis (degrees)", -90, 90, rotateY)
            sliders.setupTrackBar("Rotate pointcloud around Z-axis (degrees)", -90, 90, rotateZ)
            sliders.setupTrackBar("IMU_Basics: Alpha X100", 0, 100, task.IMU_AlphaFilter * 100)
            sliders.setupTrackBar("IMU Stability Threshold (radians) X100", 0, 100, stableThreshold * 100)
        End If
    End Sub
    Public Sub RunOpt()
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





Public Class Options_FeatureMatch : Inherits TaskParent
    Public matchOption As cvb.TemplateMatchModes = cvb.TemplateMatchModes.CCoeffNormed
    Public matchText As String = "CCoeffNormed"
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cvb.TemplateMatchModes.CCoeff, cvb.TemplateMatchModes.CCoeffNormed, cvb.TemplateMatchModes.CCorr,
                                        cvb.TemplateMatchModes.CCorrNormed, cvb.TemplateMatchModes.SqDiff, cvb.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_HeatMap : Inherits TaskParent
    Public redThreshold As Integer = 20
    Public viewName As String = "vertical"
    Public topView As Boolean = True
    Public sideView As Boolean = False
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
    Public Sub RunOpt()
        Static topCheck = FindCheckBox("Top View (Unchecked Side View)")
        Static redSlider = FindSlider("Threshold for Red channel")

        redThreshold = redSlider.value

        topView = topCheck.checked
        sideView = Not topView
        If sideView Then viewName = "horizontal"
    End Sub
End Class






Public Class Options_Boundary : Inherits TaskParent
    Public desiredBoundaries As Integer = 15
    Public peakDistance As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired boundary count", 2, 100, desiredBoundaries)
            sliders.setupTrackBar("Distance to next Peak (pixels)", 2, dst2.Width / 10, peakDistance)
        End If
    End Sub
    Public Sub RunOpt()
        Static boundarySlider = FindSlider("Desired boundary count")
        Static distSlider = FindSlider("Distance to next Peak (pixels)")
        desiredBoundaries = boundarySlider.value
        peakDistance = distSlider.value
    End Sub
End Class









Public Class Options_Denoise : Inherits TaskParent
    Public removeSinglePixels As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Remove single pixels")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static singleCheck = FindCheckBox("Remove single pixels")
        removeSinglePixels = singleCheck.checked
    End Sub
End Class









'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class Options_MSER : Inherits TaskParent
    Public delta As Integer = 9
    Public minArea As Integer = 0
    Public maxArea As Integer = 0
    Public maxVariation As Double = 0.25
    Public minDiversity As Double = 0.2
    Public maxEvolution As Integer = 200
    Public areaThreshold As Double = 1.01
    Public minMargin As Double = 0.003
    Public edgeBlurSize As Integer = 5
    Public pass2Setting As Integer = 0
    Public graySetting As Boolean = False
    Public Sub New()
        Select Case task.dst2.Width
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
    Public Sub RunOpt()
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






Public Class Options_Spectrum : Inherits TaskParent
    Public gapDepth As Integer = 1
    Public gapGray As Integer = 1
    Public sampleThreshold As Integer = 10
    Public redC As New RedCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gap in depth spectrum (cm's)", 1, 50, gapDepth)
            sliders.setupTrackBar("Gap in gray spectrum", 1, 50, gapGray)
            sliders.setupTrackBar("Sample count threshold", 1, 50, sampleThreshold)
        End If
    End Sub
    Public Function runRedCloud(ByRef label As String) As cvb.Mat
        label = redC.labels(2)
        Return redC.dst2
    End Function
    Public Function buildDepthRanges(input As cvb.Mat, typeSpec As String)
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

        Dim totalCount As Integer = 0
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
        Dim trimCount As Integer = 0
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
    Public Function buildColorRanges(input As cvb.Mat, typespec As String) As List(Of rangeData)
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

        Dim totalCount As Integer = 0
        For i = 0 To pixels.Count - 1
            sorted.Add(pixels(i), counts(i))
            totalCount += counts(i)
        Next

        strOut = "For the selected " + typespec + " cell:" + vbCrLf

        Dim rStart As Integer = sorted.ElementAt(0).Key
        Dim rEnd As Integer = rStart
        Dim count As Integer = sorted.ElementAt(0).Value
        Dim trimCount As Integer = 0
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
    Public Sub RunOpt()
        If task.FirstPass Then redC.Run(task.color) ' special case!  Can't run it in constructor or measurements fail...
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






Public Class Options_HistXD : Inherits TaskParent
    Public sideThreshold As Integer = 5
    Public topThreshold As Integer = 15
    Public threshold3D As Integer = 40
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min side bin samples", 0, 100, sideThreshold) ' for 2D histograms
            sliders.setupTrackBar("Min top bin samples", 0, 100, topThreshold) ' for 2D histograms
            sliders.setupTrackBar("Min samples per bin", 0, 500, threshold3D) ' for 3D histograms
        End If
    End Sub
    Public Sub RunOpt()
        Static topSlider = FindSlider("Min top bin samples")
        Static sideSlider = FindSlider("Min side bin samples")
        Static bothSlider = FindSlider("Min samples per bin")
        topThreshold = topSlider.value
        sideThreshold = sideSlider.value
        threshold3D = bothSlider.value
    End Sub
End Class






Public Class Options_Complexity : Inherits TaskParent
    Public filename As FileInfo
    Public filenames As List(Of String)
    Public plotColor As cvb.Scalar = New cvb.Scalar(255, 255, 0)
    Public Sub New()
        Dim fnames = Directory.GetFiles(task.HomeDir + "Complexity")
        filenames = fnames.ToList
        Dim latestFile = Directory.GetFiles(task.HomeDir + "Complexity").OrderByDescending(
                    Function(f) New FileInfo(f).LastWriteTime).First()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)

            Dim saveIndex As Integer = 0
            For i = 0 To filenames.Count - 1
                Dim filename = New FileInfo(filenames(i))
                If filename.FullName = latestFile Then saveIndex = i
                radio.addRadio(filename.Name)
            Next
            radio.check(saveIndex).Checked = True
        End If
    End Sub
    Public Function setPlotColor() As cvb.Scalar
        Static frm = FindFrm(traceName + " Radio Buttons")
        Dim index As Integer = 0
        For index = 0 To filenames.Count - 1
            If filename.FullName = filenames(index) Then Exit For
        Next
        plotColor = Choose(index Mod 4 + 1, cvb.Scalar.White, cvb.Scalar.Red, cvb.Scalar.Green, cvb.Scalar.Yellow)
        Return plotColor
    End Function
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.count - 1
            If frm.check(i).checked Then
                filename = New FileInfo(task.HomeDir + "Complexity/" + frm.check(i).text)
                plotColor = Choose((i + 1) Mod 4, cvb.Scalar.White, cvb.Scalar.Red, cvb.Scalar.Green, cvb.Scalar.Yellow)
                Exit For
            End If
        Next
        If task.FirstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If
    End Sub
End Class






Public Class Options_BGSubtractSynthetic : Inherits TaskParent
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
    Public Sub RunOpt()
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






Public Class Options_BGSubtract : Inherits TaskParent
    Public learnRate As Double = 0.01
    Public methodDesc As String = "MOG2"
    Public currMethod As Integer = 4
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("GMG")
            radio.addRadio("CNT - Counting")
            radio.addRadio("KNN")
            radio.addRadio("MOG")
            radio.addRadio("MOG2")
            radio.addRadio("GSOC")
            radio.addRadio("LSBP")
            radio.check(currMethod).Checked = True ' mog2 appears to be the best...
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("MOG Learn Rate X1000", 1, 1000, learnRate * 1000)
    End Sub
    Public Sub RunOpt()
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







Public Class Options_Classifier : Inherits TaskParent
    Public methodIndex As Integer = 0
    Public methodName As String = "Normal Bayes (NBC)"
    Public sampleCount As Integer = 200
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Samples", 10, dst2.Total, sampleCount)

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
    Public Sub RunOpt()
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








Public Class Options_Derivative : Inherits TaskParent
    Public channel As Integer = 0
    Dim options As New Options_Sobel
    Public kernelSize As Integer = 3
    Public derivativeRange As Double = 0.1
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("X Dimension")
            radio.addRadio("Y Dimension")
            radio.addRadio("Z Dimension")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        options.RunOpt()

        Static frm = FindFrm(traceName + " Radio Buttons")
        channel = 2
        If frm.check(2).checked = False Then
            If frm.check(0).checked Then channel = 0 Else channel = 1
        End If

        kernelSize = options.kernelSize
        derivativeRange = options.derivativeRange
    End Sub
End Class






Public Class Options_Threshold : Inherits TaskParent
    Public thresholdMethod As cvb.ThresholdTypes = cvb.ThresholdTypes.Binary
    Public thresholdName As String = ""
    Public threshold As Integer = 128
    Public gradient As New Gradient_Color
    Public inputGray As Boolean = False
    Public otsuOption As Boolean = False
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
    End Sub
    Public Sub RunOpt()
        If task.FirstPass Then  ' special case!  Can't run it in constructor or measurements fail...
            gradient.Run(empty)
            dst2 = gradient.dst2
        End If

        Static radioChoices = {cvb.ThresholdTypes.Binary, cvb.ThresholdTypes.BinaryInv, cvb.ThresholdTypes.Tozero,
                            cvb.ThresholdTypes.TozeroInv, cvb.ThresholdTypes.Triangle, cvb.ThresholdTypes.Trunc}
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






Public Class Options_AdaptiveThreshold : Inherits TaskParent
    Public method As cvb.AdaptiveThresholdTypes = cvb.AdaptiveThresholdTypes.GaussianC
    Public blockSize As Integer = 5
    Public constantVal As Integer = 0
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("AdaptiveThreshold block size", 3, 21, blockSize)
            sliders.setupTrackBar("Constant subtracted from mean Or weighted mean", -20, 20, constantVal)
        End If

        If standaloneTest() = False Then
            FindRadio("ToZero").Enabled = False
            FindRadio("ToZero Inverse").Enabled = False
            FindRadio("Trunc").Enabled = False
        End If
    End Sub
    Public Sub RunOpt()
        Static gaussRadio = FindRadio("GaussianC")
        Static constantSlider = FindSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = FindSlider("AdaptiveThreshold block size")

        method = If(gaussRadio.checked, cvb.AdaptiveThresholdTypes.GaussianC, cvb.AdaptiveThresholdTypes.MeanC)
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class







Public Class Options_Colors : Inherits TaskParent
    Public redS As Integer = 180
    Public greenS As Integer = 180
    Public blueS As Integer = 180
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.Setup(traceName)
            sliders.setupTrackBar("Red", 0, 255, redS)
            sliders.setupTrackBar("Green", 0, 255, greenS)
            sliders.setupTrackBar("Blue", 0, 255, blueS)
        End If
    End Sub
    Public Sub RunOpt()
        Static redSlider = FindSlider("Red")
        Static greenSlider = FindSlider("Green")
        Static blueSlider = FindSlider("Blue")
        redS = redSlider.Value
        greenS = greenSlider.Value
        blueS = blueSlider.Value
    End Sub
End Class





Public Class Options_Threshold_AdaptiveMin : Inherits TaskParent
    Public adaptiveMethod As cvb.AdaptiveThresholdTypes = cvb.AdaptiveThresholdTypes.GaussianC
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static gaussRadio = FindRadio("GaussianC")
        adaptiveMethod = If(gaussRadio.checked, cvb.AdaptiveThresholdTypes.GaussianC, cvb.AdaptiveThresholdTypes.MeanC)
    End Sub
End Class








Public Class Options_ThresholdAll : Inherits TaskParent
    Public thresholdMethod As cvb.ThresholdTypes = cvb.ThresholdTypes.Binary
    Public blockSize As Integer = 5
    Public constantVal As Integer = 0
    Public maxVal As Integer = 255
    Public threshold As Integer = 100
    Public gradient As New Gradient_Color
    Public inputGray As Boolean = False
    Public otsuOption As Boolean = False
    Public adaptiveMethod As cvb.AdaptiveThresholdTypes = cvb.AdaptiveThresholdTypes.GaussianC
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
    End Sub
    Public Sub RunOpt()
        If task.FirstPass Then  ' special case!  Can't run it in constructor or measurements fail...
            gradient.Run(empty)
            dst2 = gradient.dst2
        End If

        Dim radioChoices = {cvb.ThresholdTypes.Binary, cvb.ThresholdTypes.BinaryInv, cvb.ThresholdTypes.Tozero,
                    cvb.ThresholdTypes.TozeroInv, cvb.ThresholdTypes.Triangle, cvb.ThresholdTypes.Trunc}
        options.RunOpt()
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





Public Class Options_StdevGrid : Inherits TaskParent
    Public minThreshold As Integer = 30
    Public maxThreshold As Integer = 230
    Public diffThreshold As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min color threshold", 0, 50, minThreshold)
            sliders.setupTrackBar("Max color threshold", 0, 255, maxThreshold)
            sliders.setupTrackBar("Equal diff threshold", 0, 20, diffThreshold)
        End If

        desc = "Options for the StdevGrid algorithms."
    End Sub
    Public Sub RunOpt()
        Static minSlider = FindSlider("Min color threshold")
        Static maxSlider = FindSlider("Max color threshold")
        Static diffSlider = FindSlider("Equal diff threshold")
        minThreshold = minSlider.value
        maxThreshold = maxSlider.value
        diffThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_DFT : Inherits TaskParent
    Public radius As Integer = 120
    Public order As Integer = 2
    Public butterworthFilter(1) As cvb.Mat
    Public dftFlag As cvb.DftFlags = cvb.DftFlags.ComplexOutput
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
    Public Sub RunOpt()
        Static radiusSlider = FindSlider("DFT B Filter - Radius")
        Static orderSlider = FindSlider("DFT B Filter - Order")
        radius = radiusSlider.Value
        order = orderSlider.Value

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                dftFlag = Choose(i + 1, cvb.DftFlags.ComplexOutput, cvb.DftFlags.Inverse, cvb.DftFlags.None,
                                    cvb.DftFlags.RealOutput, cvb.DftFlags.Rows, cvb.DftFlags.Scale)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_DFTShape : Inherits TaskParent
    Public dftShape As String = "Draw Circle"
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
    Public Sub RunOpt()
        Static frm = FindFrm("Options_DFTShape Radio Buttons")
        dftShape = findRadioText(frm.check)
    End Sub
End Class






Public Class Options_FitEllipse : Inherits TaskParent
    Public fitType As Integer = 0
    Public threshold As Integer = 70
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("FitEllipse threshold", 0, 255, threshold)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("fitEllipseQ")
            radio.addRadio("fitEllipseAMS")
            radio.addRadio("fitEllipseDirect")
            radio.check(fitType).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
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







Public Class Options_TopX : Inherits TaskParent
    Public topX As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the top X cells", 1, 255, topX)
    End Sub
    Public Sub RunOpt()
        Static topXSlider = FindSlider("Show the top X cells")
        topX = topXSlider.value
    End Sub
End Class








Public Class Options_XNeighbors : Inherits TaskParent
    Public xNeighbors As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("X neighbors", 1, 255, xNeighbors)
    End Sub
    Public Sub RunOpt()
        Static topXSlider = FindSlider("X neighbors")
        xNeighbors = topXSlider.value
    End Sub
End Class








Public Class Options_Sobel : Inherits TaskParent
    Public kernelSize As Integer = 3
    Public threshold As Integer = 50
    Public distanceThreshold As Integer = 10
    Public derivativeRange As Double = 0.1
    Public horizontalDerivative As Boolean = True
    Public verticalDerivative As Boolean = True
    Public useBlur As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sobel kernel Size", 1, 31, kernelSize)
            sliders.setupTrackBar("Threshold to zero pixels below this value", 0, 255, threshold)
            sliders.setupTrackBar("Range around zero X100", 1, 500, derivativeRange * 100)
            sliders.setupTrackBar("Threshold distance", 0, 100, distanceThreshold)
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
    Public Sub RunOpt()
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





Public Class Options_EdgeOverlay : Inherits TaskParent
    Public xDisp As Integer = 7
    Public yDisp As Integer = 11
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Displacement in the X direction (in pixels)", 0, 100, xDisp)
            sliders.setupTrackBar("Displacement in the Y direction (in pixels)", 0, 100, yDisp)
        End If
    End Sub
    Public Sub RunOpt()
        Static xSlider = FindSlider("Displacement in the X direction (in pixels)")
        Static ySlider = FindSlider("Displacement in the Y direction (in pixels)")
        xDisp = xSlider.Value
        yDisp = ySlider.Value
    End Sub
End Class







Public Class Options_AddWeighted : Inherits TaskParent
    Public addWeighted As Double = 0.5
    Public accumWeighted As Double = 0.1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Add Weighted %", 0, 100, addWeighted * 100)
            sliders.setupTrackBar("Accumulation weight of each image X100", 1, 100, accumWeighted * 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static weightSlider = FindSlider("Add Weighted %")
        Static accumSlider = FindSlider("Accumulation weight of each image X100")
        addWeighted = weightSlider.value / 100
        accumWeighted = accumSlider.value / 100
    End Sub
End Class







Public Class Options_ApproxPoly : Inherits TaskParent
    Public epsilon As Double = 3
    Public closedPoly As Boolean = True
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("epsilon - max distance from original curve", 0, 100, 3)
        End If

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Closed polygon - connect first and last vertices.")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static epsilonSlider = FindSlider("epsilon - max distance from original curve")
        Static closedPolyCheck = FindCheckBox("Closed polygon - connect first and last vertices.")
        epsilon = epsilonSlider.value
        closedPoly = closedPolyCheck.checked
    End Sub
End Class





Public Class Options_Bin3WayRedCloud : Inherits TaskParent
    Public startRegion As Integer = 0
    Public endRegion As Integer = 0
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
    Public Sub RunOpt()
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






Public Class Options_Bin2WayRedCloud : Inherits TaskParent
    Public startRegion As Integer = 0
    Public endRegion As Integer = 0
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
    Public Sub RunOpt()
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






Public Class Options_GuidedBPDepth : Inherits TaskParent
    Public bins As Integer = 1000
    Public maxClusters As Double = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram Bins for depth data", 3, 5000, bins)
            sliders.setupTrackBar("Maximum number of clusters", 1, 50, maxClusters)
        End If
    End Sub
    Public Sub RunOpt()
        Static binSlider = FindSlider("Histogram Bins for depth data")
        Static clusterSlider = FindSlider("Maximum number of clusters")
        bins = binSlider.value
        maxClusters = clusterSlider.value
    End Sub
End Class







Public Class Options_OpenGL_Duster : Inherits TaskParent
    Public useClusterColors As Boolean = False
    Public useTaskPointCloud As Boolean = False
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Display cluster colors")
            check.addCheckBox("Use task.pointCloud")
        End If
    End Sub
    Public Sub RunOpt()
        Static colorCheck = FindCheckBox("Display cluster colors")
        Static cloudCheck = FindCheckBox("Use task.pointCloud")
        useClusterColors = colorCheck.checked
        useTaskPointCloud = cloudCheck.checked
    End Sub
End Class



Public Class Options_FeatureGather : Inherits TaskParent
    Public featureSource As Integer = 0
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
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                featureSource = Choose(i + 1, FeatureSrc.GoodFeaturesFull, FeatureSrc.GoodFeaturesGrid, FeatureSrc.Agast,
                                    FeatureSrc.BRISK, FeatureSrc.Harris, FeatureSrc.FAST)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_AsciiArt : Inherits TaskParent
    Public hStep As Double = 31
    Public wStep As Double = 55
    Public size As cvb.Size = New cvb.Size(wStep, hStep)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Character height in pixels", 20, 100, hStep)
            sliders.setupTrackBar("Character width in pixels", 20, 200, wStep)
        End If
    End Sub
    Public Sub RunOpt()
        Static hSlider = FindSlider("Character height in pixels")
        Static wSlider = FindSlider("Character width in pixels")

        hStep = CInt(task.dst2.Height / hSlider.value)
        wStep = CInt(task.dst2.Width / wSlider.value)
        size = New cvb.Size(CInt(wSlider.value), CInt(hSlider.value))
    End Sub
End Class






Public Class Options_MotionDetect : Inherits TaskParent
    Public threadData As cvb.Vec3i = New cvb.Vec3i(0, 0, 0)
    Public CCthreshold As Double = 0
    Public pad As Integer = 0
    Public stdevThreshold As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Correlation Threshold", 0, 1000, 980)
            sliders.setupTrackBar("Stdev threshold for using correlation", 0, 100, 15)
            sliders.setupTrackBar("Pad size in pixels for the search area", 0, 100, 20)
        End If
        If radio.Setup(traceName) Then
            For i = 0 To 7 - 1
                radio.addRadio(CStr(2 ^ i) + " threads")
            Next
            radio.check(5).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Dim w = dst2.Width
        Dim h = dst2.Height
        Static radioChoices() As cvb.Vec3i = {New cvb.Vec3i(1, w, h), New cvb.Vec3i(2, w / 2, h), New cvb.Vec3i(4, w / 2, h / 2),
                    New cvb.Vec3i(8, w / 4, h / 2), New cvb.Vec3i(16, w / 4, h / 4), New cvb.Vec3i(32, w / 8, h / 4),
                    New cvb.Vec3i(32, w / 8, h / 8), New cvb.Vec3i(1, w, h), New cvb.Vec3i(2, w / 2, h), New cvb.Vec3i(4, w / 2, h / 2),
                    New cvb.Vec3i(8, w / 4, h / 2), New cvb.Vec3i(16, w / 4, h / 4), New cvb.Vec3i(32, w / 8, h / 4),
                    New cvb.Vec3i(32, w / 8, h / 8)}

        Static correlationSlider = FindSlider("Correlation Threshold")
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static padSlider = FindSlider("Pad size in pixels for the search area")
        Static stdevSlider = FindSlider("Stdev threshold for using correlation")
        CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        threadData = radioChoices(findRadioIndex(frm.check))
        pad = padSlider.Value
        stdevThreshold = stdevSlider.Value
    End Sub
End Class





Public Class Options_JpegQuality : Inherits TaskParent
    Public quality As Integer = 90
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("JPEG Quality", 1, 100, quality)
    End Sub
    Public Sub RunOpt()
        Static qualitySlider = FindSlider("JPEG Quality")
        quality = qualitySlider.value
    End Sub
End Class




Public Class Options_PNGCompression : Inherits TaskParent
    Public compression As Integer = 90
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("PNG Compression", 1, 100, compression)
    End Sub
    Public Sub RunOpt()
        Static compressionSlider = FindSlider("PNG Compression")
        compression = compressionSlider.value
    End Sub
End Class





Public Class Options_Binarize : Inherits TaskParent
    Public binarizeLabel As String = "Binary"
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Binary")
            radio.addRadio("Binary + OTSU")
            radio.addRadio("OTSU")
            radio.addRadio("OTSU + Blur")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then binarizeLabel = radio.check(i).Text
        Next
    End Sub
End Class






Public Class Options_BlurTopo : Inherits TaskParent
    Public savePercent As Double = 0
    Public nextPercent As Double = 20
    Public reduction As Integer = 20
    Public frameCycle As Integer = 0.5
    Public kernelSize As Double = 101
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Percent of Blurring", 0, 100, nextPercent)
            sliders.setupTrackBar("Blur Color Reduction", 2, 64, reduction * 100)
            sliders.setupTrackBar("Frame Count Cycle", 1, 200, frameCycle)
        End If
    End Sub
    Public Sub RunOpt()
        Static reductionSlider = FindSlider("Blur Color Reduction")
        Static frameSlider = FindSlider("Frame Count Cycle")
        Static percentSlider = FindSlider("Percent of Blurring")

        If task.optionsChanged Then
            savePercent = percentSlider.Value
            nextPercent = savePercent
        End If

        frameCycle = frameSlider.value
        reduction = reductionSlider.value / 100
        kernelSize = CInt(nextPercent / 100 * dst2.Width) Or 1
    End Sub
End Class






Public Class Options_BoundaryRect : Inherits TaskParent
    Public percentRect As Double = 25
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired percent of rectangles", 0, 100, percentRect)
    End Sub
    Public Sub RunOpt()
        Static percentSlider = FindSlider("Desired percent of rectangles")
        percentRect = percentSlider.value / 100
    End Sub
End Class








Public Class Options_BrightnessContrast : Inherits TaskParent
    Public contrast As Integer = 500
    Public brightness As Double = -100
    Public hsvBrightness As Double = 100
    Public exponent As Double = 0.3
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
            sliders.setupTrackBar("HSV Brightness Value", 0, 150, hsvBrightness)
            sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, exponent * 100)
        End If
    End Sub
    Public Sub RunOpt()
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






Public Class Options_HistPointCloud : Inherits TaskParent
    Public threshold As Integer = 60
    Public xBins As Integer = 30
    Public yBins As Integer = 30
    Public zBins As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram threshold", 0, 1000, threshold)
            sliders.setupTrackBar("Histogram X bins", 1, dst2.Cols, xBins)
            sliders.setupTrackBar("Histogram Y bins", 1, dst2.Rows, yBins)
            sliders.setupTrackBar("Histogram Z bins", 1, 200, zBins)
        End If

        Select Case dst2.Width
            Case 640
                FindSlider("Histogram threshold").Value = 200
            Case 320
                FindSlider("Histogram threshold").Value = threshold
            Case 160
                FindSlider("Histogram threshold").Value = 25
        End Select
    End Sub
    Public Sub RunOpt()
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







Public Class Options_Harris : Inherits TaskParent
    Public threshold As Double = 0.00001
    Public neighborhood As Integer = 3
    Public aperture As Integer = 21
    Public harrisParm As Double = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Harris Threshold", 1, 100, threshold * 10000)
            sliders.setupTrackBar("Harris Neighborhood", 1, 41, neighborhood)
            sliders.setupTrackBar("Harris aperture", 1, 31, aperture)
            sliders.setupTrackBar("Harris Parameter", 1, 100, harrisParm)
        End If
    End Sub
    Public Sub RunOpt()
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






Public Class Options_HarrisCorners : Inherits TaskParent
    Public quality As Integer = 50
    Public qualityMax As Integer = 100
    Public blockSize As Integer = 3
    Public aperture As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, blockSize)
            sliders.setupTrackBar("Corner aperture size", 1, 21, aperture)
            sliders.setupTrackBar("Corner quality level", 1, qualityMax, quality)
        End If
    End Sub
    Public Sub RunOpt()
        Static blockSlider = FindSlider("Corner block size")
        Static apertureSlider = FindSlider("Corner aperture size")
        Static qualitySlider = FindSlider("Corner quality level")
        quality = qualitySlider.Value
        aperture = apertureSlider.value Or 1
        blockSize = blockSlider.value Or 1
    End Sub
End Class






Public Class Options_Databases : Inherits TaskParent
    Public linkAddress As String = ""
    Dim downloadActive As Boolean = False
    Dim downloadIndex As Integer = 0
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
    Public Sub RunOpt()
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







Public Class Options_EdgeMatching : Inherits TaskParent '
    Public searchDepth As Integer = 256
    Public threshold As Double = 80
    Public overlayChecked As Boolean = False
    Public highlightChecked As Boolean = True
    Public clearChecked As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Search depth in pixels", 1, 256, searchDepth)
            sliders.setupTrackBar("Edge Correlation threshold X100", 1, 100, threshold)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Overlay thread grid")
            check.addCheckBox("Highlight all grid entries above threshold")
            check.addCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
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






Public Class Options_EmaxInputClusters : Inherits TaskParent
    Public samplesPerRegion As Integer = 10
    Public sigma As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("EMax Number of Samples per region", 1, 20, samplesPerRegion)
            sliders.setupTrackBar("EMax Sigma (spread)", 1, 100, sigma)
        End If
    End Sub
    Public Sub RunOpt()
        Static sampleSlider = FindSlider("EMax Number of Samples per region")
        Static sigmaSlider = FindSlider("EMax Sigma (spread)")
        samplesPerRegion = sampleSlider.value
        sigma = sigmaSlider.value
    End Sub
End Class





Public Class Options_CComp : Inherits TaskParent
    Public light As Integer = 127
    Public dark As Integer = 50
    Public threshold As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for lighter input", 1, 255, light)
            sliders.setupTrackBar("Threshold for darker input", 1, 255, dark)
            sliders.setupTrackBar("CComp threshold", 0, 255, 50)
        End If

        desc = "Options for CComp_Both"
    End Sub
    Public Sub RunOpt()
        Static lightSlider = FindSlider("Threshold for lighter input")
        Static darkSlider = FindSlider("Threshold for darker input")
        Static thresholdSlider = FindSlider("CComp threshold")
        threshold = thresholdSlider.value
        light = lightSlider.value
        dark = darkSlider.value
    End Sub
End Class






Public Class Options_CellAutomata : Inherits TaskParent
    Public currentRule As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Current Rule", 0, 255, currentRule)
    End Sub
    Public Sub RunOpt()
        Static ruleSlider = FindSlider("Current Rule")
        currentRule = ruleSlider.value
    End Sub
End Class






Public Class Options_BackProject2D : Inherits TaskParent
    Public backProjectRow As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BackProject Row")
            radio.addRadio("BackProject Col")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static rowRadio = FindRadio("BackProject Row")
        If task.mouseClickFlag Then rowRadio.checked = Not rowRadio.checked
        backProjectRow = rowRadio.checked
    End Sub
End Class





Public Class Options_Kaze : Inherits TaskParent
    Public pointsToMatch As Integer = 100
    Public maxDistance As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of points to match", 1, 300, pointsToMatch)
            sliders.setupTrackBar("When matching, max possible distance", 1, 200, maxDistance)
        End If
    End Sub
    Public Sub RunOpt()
        Static maxSlider = FindSlider("Max number of points to match")
        Static distSlider = FindSlider("When matching, max possible distance")
        pointsToMatch = maxSlider.value
        maxDistance = distSlider.value
    End Sub
End Class





Public Class Options_Blob : Inherits TaskParent
    Dim blob As New Blob_Input
    Public blobParams As cvb.SimpleBlobDetector.Params = New cvb.SimpleBlobDetector.Params
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
    Public Sub RunOpt()
        Static minSlider = FindSlider("min Threshold")
        Static maxSlider = FindSlider("max Threshold")
        Static stepSlider = FindSlider("Threshold Step")
        Static areaRadio = FindRadio("FilterByArea")
        Static circRadio = FindRadio("FilterByCircularity")
        Static convexRadio = FindRadio("FilterByConvexity")
        Static inertiaRadio = FindRadio("FilterByInertia")
        Static colorRadio = FindRadio("FilterByColor")

        blobParams = New cvb.SimpleBlobDetector.Params
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






Public Class Options_SLR : Inherits TaskParent
    Public tolerance As Double = 0.3
    Public halfLength As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Approximate accuracy (tolerance) X100", 1, 1000, tolerance * 100)
            sliders.setupTrackBar("Simple moving average window size", 1, 100, halfLength)
        End If
    End Sub
    Public Sub RunOpt()
        Static toleranceSlider = FindSlider("Approximate accuracy (tolerance) X100")
        Static movingAvgSlider = FindSlider("Simple moving average window size")
        tolerance = toleranceSlider.Value / 100
        halfLength = movingAvgSlider.Value
    End Sub
End Class




Public Class Options_KNN : Inherits TaskParent
    Public knnDimension As Integer = 2
    Public numPoints As Integer = 10
    Public multiplier As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KNN Dimension", 2, 10, knnDimension)
            sliders.setupTrackBar("Random input points", 5, 100, numPoints)
            sliders.setupTrackBar("Average distance multiplier", 1, 20, multiplier)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Display queries")
            check.addCheckBox("Display training input and connecting line")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static dimSlider = FindSlider("KNN Dimension")
        Static randomSlider = FindSlider("Random input points")
        Static distSlider = FindSlider("Random input points")
        knnDimension = dimSlider.Value
        numPoints = randomSlider.Value
        multiplier = distSlider.value
    End Sub
End Class






Public Class Options_Clone : Inherits TaskParent
    Public alpha As Double = 0.2
    Public beta As Double = 0.2
    Public lowThreshold As Integer = 10
    Public highThreshold As Integer = 50
    Public blueChange As Double = 0.5
    Public greenChange As Double = 0.5
    Public redChange As Double = 1.5
    Public cloneFlag As cvb.SeamlessCloneMethods = cvb.SeamlessCloneMethods.MixedClone
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Alpha", 0, 20, alpha * 10)
            sliders.setupTrackBar("Beta", 0, 20, beta * 10)
            sliders.setupTrackBar("Low Threshold", 0, 100, lowThreshold)
            sliders.setupTrackBar("High Threshold", 0, 100, highThreshold)
            sliders.setupTrackBar("Color Change - Blue", 5, 25, blueChange * 10)
            sliders.setupTrackBar("Color Change - Green", 5, 25, greenChange * 10)
            sliders.setupTrackBar("Color Change - Red", 5, 25, redChange * 10)
        End If

        If (radio.Setup(traceName)) Then
            radio.addRadio("Seamless Normal Clone")
            radio.addRadio("Seamless Mono Clone")
            radio.addRadio("Seamless Mixed Clone")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
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
                cloneFlag = Choose(i + 1, cvb.SeamlessCloneMethods.MixedClone, cvb.SeamlessCloneMethods.MonochromeTransfer, cvb.SeamlessCloneMethods.NormalClone)
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Coherence : Inherits TaskParent
    Public sigma As Integer = 9
    Public blend As Double = 10
    Public str_sigma As Integer = 155
    Public eigenkernelsize As Integer = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Coherence Sigma", 1, 15, sigma)
            sliders.setupTrackBar("Coherence Blend", 1, 10, blend)
            sliders.setupTrackBar("Coherence str_sigma", 1, 15, str_sigma)
            sliders.setupTrackBar("Coherence eigen kernel", 1, 31, eigenkernelsize)
        End If
    End Sub
    Public Sub RunOpt()
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





Public Class Options_Color : Inherits TaskParent
    Public colorFormat As String = "BGR"
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
    Public Sub RunOpt()
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colorFormat = radio.check(i).Text
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Grayscale8U : Inherits TaskParent
    Public useOpenCV As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use OpenCV to create grayscale image")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static grayCheck = FindCheckBox("Use OpenCV to create grayscale image")
        useOpenCV = grayCheck.checked
    End Sub
End Class





Public Class Options_Color8UTopX : Inherits TaskParent
    Public topXcount As Integer = 16
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Top X pixels", 2, 32, topXcount)
    End Sub
    Public Sub RunOpt()
        Static topXSlider = FindSlider("Top X pixels")
        topXcount = topXSlider.value
    End Sub
End Class







Public Class Options_Morphology : Inherits TaskParent
    Public widthHeight As Integer = 20
    Public iterations As Integer = 1
    Public scaleFactor As Double = 70
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Morphology width/height", 1, 100, widthHeight)
            sliders.setupTrackBar("MorphologyEx iterations", 1, 5, iterations)
            sliders.setupTrackBar("MorphologyEx Scale factor X1000", 1, 500, scaleFactor)
        End If
    End Sub
    Public Sub RunOpt()
        Static morphSlider = FindSlider("Morphology width/height")
        Static morphExSlider = FindSlider("MorphologyEx iterations")
        Static scaleSlider = FindSlider("MorphologyEx Scale factor X1000")
        widthHeight = morphSlider.value
        iterations = morphExSlider.value
        scaleFactor = 1 + scaleSlider.value / 1000
    End Sub
End Class





Public Class Options_Convex : Inherits TaskParent
    Public hullCount As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Hull random points", 4, 20, hullCount)
    End Sub
    Public Sub RunOpt()
        Static hullSlider = FindSlider("Hull random points")
        hullCount = hullSlider.Value
    End Sub
End Class






Public Class Options_Corners : Inherits TaskParent
    Public useNonMax As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use Non-Max = True")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static nonMaxCheck = FindCheckBox("Use Non-Max = True")
        useNonMax = nonMaxCheck.checked
    End Sub
End Class


Public Class Options_PreCorners : Inherits TaskParent
    Public kernelSize As Integer = 19
    Public subpixSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("kernel Size", 1, 20, kernelSize)
            sliders.setupTrackBar("SubPix kernel Size", 1, 20, subpixSize)
        End If
    End Sub
    Public Sub RunOpt()
        Static kernelSlider = FindSlider("kernel Size")
        Static subpixSlider = FindSlider("SubPix kernel Size")
        kernelSize = kernelSlider.value Or 1
        subpixSize = subpixSlider.value Or 1
    End Sub
End Class





Public Class Options_ShiTomasi : Inherits TaskParent
    Public useShiTomasi As Boolean = True
    Public threshold As Integer = 0
    Public aperture As Integer = 3
    Public blocksize As Integer = 3

    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Harris features")
            radio.addRadio("Shi-Tomasi features")
            radio.check(1).Checked = True
        End If
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner normalize threshold", 0, 32, threshold)
            sliders.setupTrackBar("Corner aperture size", 1, 21, aperture)
            sliders.setupTrackBar("Corner block size", 1, 21, blocksize)
        End If
    End Sub
    Public Sub RunOpt()
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




Public Class Options_FlatLand : Inherits TaskParent
    Public reductionFactor As Double = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Region Count", 1, 250, reductionFactor)
    End Sub
    Public Sub RunOpt()
        Static regionSlider = FindSlider("Region Count")
        reductionFactor = regionSlider.Maximum - regionSlider.Value
    End Sub
End Class






Public Class Options_Depth : Inherits TaskParent
    Public millimeters As Integer = 8
    Public mmThreshold As Double = millimeters / 1000
    Public threshold As Double = 250
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold in millimeters", 0, 1000, mmThreshold * 1000)
            sliders.setupTrackBar("Threshold for punch", 0, 255, threshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static mmSlider = FindSlider("Threshold in millimeters")
        Static thresholdSlider = FindSlider("Threshold for punch")
        millimeters = mmSlider.value
        mmThreshold = mmSlider.Value / 1000.0
        threshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_DepthHoles : Inherits TaskParent
    Public borderDilation As Integer = 1
    Public holeDilation As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Amount of dilation of borderMask", 1, 10, borderDilation)
            sliders.setupTrackBar("Amount of dilation of holeMask", 0, 10, holeDilation)
        End If
    End Sub
    Public Sub RunOpt()
        Static borderSlider = FindSlider("Amount of dilation of borderMask")
        Static holeSlider = FindSlider("Amount of dilation of holeMask")
        borderDilation = borderSlider.value
        holeDilation = holeSlider.value
    End Sub
End Class






Public Class Options_Uncertainty : Inherits TaskParent
    Public uncertaintyThreshold As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Uncertainty threshold", 1, 255, uncertaintyThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Uncertainty threshold")
        uncertaintyThreshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_DepthColor : Inherits TaskParent
    Public alpha As Double = 0.05
    Public beta As Double = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Depth ColorMap Alpha X100", 1, 100, alpha * 100)
            sliders.setupTrackBar("Depth ColorMap Beta", 1, 100, beta)
        End If
    End Sub
    Public Sub RunOpt()
        Static alphaSlider = FindSlider("Depth ColorMap Alpha X100")
        Static betaSlider = FindSlider("Depth ColorMap Beta")
        alpha = alphaSlider.value / 100
        beta = betaSlider.value
    End Sub
End Class






Public Class Options_DNN : Inherits TaskParent
    Public superResModelFileName As String = ""
    Public shortModelName As String = ""
    Public superResMultiplier As Integer = 0
    Public ScaleFactor As Double = 78
    Public scaleMax As Double = 255
    Public meanValue As Double = 127
    Public confidenceThreshold As Double = 0.8
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DNN Scale Factor", 1, 10000, ScaleFactor)
            sliders.setupTrackBar("DNN MeanVal", 1, scaleMax, meanValue)
            sliders.setupTrackBar("DNN Confidence Threshold", 1, 100, confidenceThreshold * 100)
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
    Public Sub RunOpt()
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





Public Class Options_DrawNoise : Inherits TaskParent
    Public noiseCount As Integer = 100
    Public noiseWidth As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Noise Count", 1, 1000, noiseCount)
            sliders.setupTrackBar("Noise Width", 1, 10, noiseWidth)
        End If
    End Sub
    Public Sub RunOpt()
        Static widthSlider = FindSlider("Noise Width")
        Static CountSlider = FindSlider("Noise Count")
        noiseCount = CountSlider.Value
        noiseWidth = widthSlider.Value
    End Sub
End Class






Public Class Options_Edges : Inherits TaskParent
    Public scharrMultiplier As Double = 50
    Public EP_Sigma_s As Double = 10
    Public EP_Sigma_r As Double = 40
    Public recurseCheck As Boolean = True
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Edge-preserving RecurseFilter")
            radio.addRadio("Edge-preserving NormconvFilter")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Scharr multiplier X100", 1, 500, scharrMultiplier)
            sliders.setupTrackBar("Edge-preserving Sigma_s", 0, 200, EP_Sigma_s)
            sliders.setupTrackBar("Edge-preserving Sigma_r", 1, 100, EP_Sigma_r)
        End If
    End Sub
    Public Sub RunOpt()
        Static sigmaSSlider = FindSlider("Edge-preserving Sigma_s")
        Static sigmaRSlider = FindSlider("Edge-preserving Sigma_r")
        Static recurseRadio = FindRadio("Edge-preserving RecurseFilter")
        EP_Sigma_s = sigmaSSlider.Value
        EP_Sigma_r = sigmaRSlider.Value / sigmaRSlider.Maximum
        recurseCheck = recurseRadio.checked
    End Sub
End Class






Public Class Options_Edges2 : Inherits TaskParent
    Public removeFrequencies As Integer = 32
    Public dctThreshold As Integer = 20
    Public edgeRFthreshold As Integer = 35
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Remove Frequencies < x", 0, 100, removeFrequencies)
            sliders.setupTrackBar("Threshold after Removal", 1, 255, dctThreshold)
            sliders.setupTrackBar("Edges RF Threshold", 1, 255, edgeRFthreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static freqSlider = FindSlider("Remove Frequencies < x")
        Static thresholdSlider = FindSlider("Threshold after Removal")
        Static rfSlider = FindSlider("Edges RF Threshold")

        edgeRFthreshold = rfSlider.value
        removeFrequencies = freqSlider.value
        dctThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_Edges3 : Inherits TaskParent
    Public alpha As Double = 100
    Public omega As Double = 100
    Public gapDistance As Integer = 5
    Public threshold As Integer = 128
    Public gapdiff As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Deriche Alpha X100", 1, 400, alpha)
            sliders.setupTrackBar("Deriche Omega X1000", 1, 1000, omega)
            sliders.setupTrackBar("Output filter threshold", 0, 255, threshold)
            sliders.setupTrackBar("Input pixel distance", 0, 20, gapDistance)
            sliders.setupTrackBar("Input pixel difference", 0, 50, If(task.dst2.Width = 640, gapdiff, 20))
        End If
    End Sub
    Public Sub RunOpt()
        Static alphaSlider = FindSlider("Deriche Alpha X100")
        Static omegaSlider = FindSlider("Deriche Omega X1000")
        Static thresholdSlider = FindSlider("Output filter threshold")
        alpha = alphaSlider.value / 100
        omega = omegaSlider.value / 100
        threshold = thresholdSlider.value

        Static distanceSlider = FindSlider("Input pixel distance")
        Static diffSlider = FindSlider("Input pixel difference")
        gapDistance = distanceSlider.Value And 254
        gapdiff = diffSlider.Value
    End Sub
End Class







Public Class Options_DepthEdges : Inherits TaskParent
    Public depthDiff As Integer = 200
    Public depthOffset As Double = 0.001
    Public depthDist As Integer = 5
    Public mmDepthDiff As Integer = 1.0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for depth difference", 0, 255, 200)
            sliders.setupTrackBar("Rect offset X1000", 0, 20, depthOffset * 1000)
            sliders.setupTrackBar("Input depth distance", 0, 20, depthDist)
            sliders.setupTrackBar("Input depth difference in mm's", 0, 2000, mmDepthDiff * 1000)
        End If
    End Sub
    Public Sub RunOpt()
        Static diffSlider = FindSlider("Threshold for depth difference")
        Static rectSlider = FindSlider("Rect offset X1000")
        Static distanceSlider = FindSlider("Input depth distance")
        Static mmDiffSlider = FindSlider("Input depth difference in mm's")

        depthDiff = diffSlider.value
        depthOffset = rectSlider.value / 1000

        depthDist = distanceSlider.value And 254
        mmDepthDiff = mmDiffSlider.value / 1000
    End Sub
End Class






Public Class Options_Edges4 : Inherits TaskParent
    Public vertPixels As Integer = 5
    Public horizPixels As Integer = 5
    Public horizonCheck As Boolean = True
    Public verticalCheck As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Horizontal Edges")
            check.addCheckBox("Vertical Edges")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Border Vertical in Pixels", 1, 20, vertPixels)
            sliders.setupTrackBar("Border Horizontal in Pixels", 1, 20, horizPixels)
        End If
    End Sub
    Public Sub RunOpt()
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








Public Class Options_Erode : Inherits TaskParent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cvb.MorphShapes = cvb.MorphShapes.Cross
    Public element As cvb.Mat
    Public noshape As Boolean = False
    Public flatDepth As Double = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Erode Kernel Size", 1, 32, kernelSize)
            sliders.setupTrackBar("Erode Iterations", 0, 32, iterations)
            sliders.setupTrackBar("DepthSeed flat depth X1000", 1, 200, flatDepth)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Erode shape: Cross")
            radio.addRadio("Erode shape: Ellipse")
            radio.addRadio("Erode shape: Rect")
            radio.addRadio("Erode shape: None")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static ellipseRadio = FindRadio("Erode shape: Ellipse")
        Static rectRadio = FindRadio("Erode shape: Rect")
        Static iterSlider = FindSlider("Erode Iterations")
        Static kernelSlider = FindSlider("Erode Kernel Size")
        Static noShapeRadio = FindRadio("Erode shape: None")
        Static depthSlider = FindSlider("DepthSeed flat depth X1000")
        iterations = iterSlider.Value
        kernelSize = kernelSlider.Value Or 1

        morphShape = cvb.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cvb.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cvb.MorphShapes.Rect
        element = cvb.Cv2.GetStructuringElement(morphShape, New cvb.Size(kernelSize, kernelSize))
        noshape = noShapeRadio.checked
        flatDepth = depthSlider.value / 1000
    End Sub
End Class





Public Class Options_Etch_ASketch : Inherits TaskParent
    Public demoMode As Boolean = False
    Public cleanMode As Boolean = False
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Etch_ASketch clean slate")
            check.addCheckBox("Demo mode")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static cleanCheck = FindCheckBox("Etch_ASketch clean slate")
        Static demoCheck = FindCheckBox("Demo mode")
        demoMode = demoCheck.checked

        If cleanMode Then cleanCheck.checked = False ' only on for one frame.
        cleanMode = cleanCheck.checked
    End Sub
End Class






Public Class Options_Features : Inherits TaskParent
    Public quality As Double = 0.01
    Public minDistance As Double = 1
    Public matchOption As cvb.TemplateMatchModes = cvb.TemplateMatchModes.CCoeffNormed
    Public matchText As String = ""
    Public k As Double = 0.04
    Public blockSize As Integer = 3

    Public featurePoints As Integer = 400
    Public templatePad As Integer = 10
    Public templateSize As Integer = 0
    Public correlationMin As Double = 0.75
    Public resyncThreshold As Double = 0.95
    Public agastThreshold As Integer = 20
    Public useVertical As Boolean = False
    Public useBRISK As Boolean = False
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
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, 10)
            sliders.setupTrackBar("X angle tolerance in degrees", 0, 10, 2)
            sliders.setupTrackBar("Z angle tolerance in degrees", 0, 10, 7)
        End If
    End Sub
    Public Sub RunOpt()
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





Public Class Options_LineFinder : Inherits TaskParent
    Public kernelSize As Integer = 5
    Public tolerance As Integer = 5
    Public kSize As Integer = kernelSize - 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area kernel size for depth", 1, 10, kernelSize)
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, tolerance)
        End If
    End Sub
    Public Sub RunOpt()
        Static angleSlider = FindSlider("Angle tolerance in degrees")
        Static kernelSlider = FindSlider("Area kernel size for depth")
        kernelSize = kernelSlider.value * 2 - 1
        tolerance = angleSlider.value
        kSize = kernelSlider.Value - 1
    End Sub
End Class






Public Class Options_PCA_NColor : Inherits TaskParent
    Public desiredNcolors As Integer = 8
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired number of colors", 1, 256, desiredNcolors)
    End Sub
    Public Sub RunOpt()
        Static nSlider = FindSlider("Desired number of colors")
        desiredNcolors = nSlider.value
    End Sub
End Class





Public Class Options_FPolyCore : Inherits TaskParent
    Public maxShift As Integer = 50
    Public resyncThreshold As Integer = 4
    Public anchorMovement As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Maximum shift to trigger resync", 1, 100, maxShift)
            sliders.setupTrackBar("Anchor point max movement", 1, 10, anchorMovement)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Resync if feature moves > X pixels")
        Static shiftSlider = FindSlider("Maximum shift to trigger resync")
        Static anchorSlider = FindSlider("Anchor point max movement")
        maxShift = shiftSlider.Value
        resyncThreshold = thresholdSlider.Value
        anchorMovement = anchorSlider.value
    End Sub
End Class





Public Class Options_FLANN : Inherits TaskParent
    Public reuseData As Boolean = False
    Public matchCount As Integer = 0
    Public queryCount As Integer = 0
    Public searchCheck As Integer = 0
    Public eps As Double = 0
    Public sorted As Boolean = False
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
    Public Sub RunOpt()
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








Public Class Options_TrackerDepth : Inherits TaskParent
    Public displayRect As Boolean = True
    Public minRectSize As Integer = 10000
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Display centroid and rectangle for each region")
            check.Box(0).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for rectangle size", 50, 50000, minRectSize)
    End Sub
    Public Sub RunOpt()
        Static displayCheck = FindCheckBox("Display centroid and rectangle for each region")
        Static minRectSizeSlider = FindSlider("Threshold for rectangle size")

        displayRect = displayCheck.checked
        minRectSize = minRectSizeSlider.value
    End Sub
End Class




Public Class Options_Gabor : Inherits TaskParent
    Public gKernel As cvb.Mat
    Public ksize As Double = 15
    Public Sigma As Double = 4
    Public theta As Double = 90
    Public lambda As Double = 10
    Public gamma As Double = 5
    Public phaseOffset As Double = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gabor Kernel Size", 0, 50, ksize)
            sliders.setupTrackBar("Gabor Sigma", 0, 100, Sigma)
            sliders.setupTrackBar("Gabor Theta (degrees)", 0, 180, theta)
            sliders.setupTrackBar("Gabor lambda", 0, 100, lambda)
            sliders.setupTrackBar("Gabor gamma X10", 0, 10, gamma)
            sliders.setupTrackBar("Gabor Phase offset X100", 0, 100, phaseOffset)
        End If
    End Sub
    Public Sub RunOpt()
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
        gKernel = cvb.Cv2.GetGaborKernel(New cvb.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cvb.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier(0)
    End Sub
End Class






Public Class Options_GrabCut : Inherits TaskParent
    Public clearAll As Boolean = False
    Public fineTuning As Boolean = True
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Selected rectangle is added to the foreground")
            radio.addRadio("Selected rectangle is added to the background")
            radio.addRadio("Clear all foreground and background fine tuning")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static fgFineTuning = FindRadio("Selected rectangle is added to the foreground")
        Static clearCheck = FindRadio("Clear all foreground and background fine tuning")
        Static saveRadio = fgFineTuning.checked
    End Sub
End Class






Public Class Options_Gradient : Inherits TaskParent
    Public exponent As Double = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, exponent)
        End If
    End Sub
    Public Sub RunOpt()
        Static contrastSlider = FindSlider("Contrast exponent to use X100")
        exponent = contrastSlider.Value / 100
    End Sub
End Class






Public Class Options_Grid : Inherits TaskParent
    Public desiredFPS As Integer = 2
    Public width As Integer = 32
    Public height As Integer = 32
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired FPS rate", 1, 10, desiredFPS)
            sliders.setupTrackBar("Grid Cell Width", 1, dst2.Width, width)
            sliders.setupTrackBar("Grid Cell Height", 1, dst2.Height, height)
        End If
    End Sub
    Public Sub RunOpt()
        Static widthSlider = FindSlider("Grid Cell Width")
        Static heightSlider = FindSlider("Grid Cell Height")
        Static fpsSlider = FindSlider("Desired FPS rate")
        desiredFPS = fpsSlider.value
        width = widthSlider.value
        height = heightSlider.value
    End Sub
End Class






Public Class Options_Histogram : Inherits TaskParent
    Public minGray As Integer = 50
    Public maxGray As Integer = 200
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Gray", 0, 255, minGray)
            sliders.setupTrackBar("Max Gray", 0, 255, maxGray)
        End If
    End Sub
    Public Sub RunOpt()
        Static minSlider = FindSlider("Min Gray")
        Static maxSlider = FindSlider("Max Gray")
        If minSlider.Value >= maxSlider.Value Then minSlider.Value = maxSlider.Value - Math.Min(10, maxSlider.Value - 1)
        If minSlider.Value = maxSlider.Value Then maxSlider.Value += 1
        minGray = minSlider.value
        maxGray = maxSlider.value
    End Sub
End Class






Public Class Options_Guess : Inherits TaskParent
    Public MaxDistance As Integer = 50
    Public Sub New()
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Max Distance from edge (pixels)", 0, 100, MaxDistance)
        End If
    End Sub
    Public Sub RunOpt()
        Static distSlider = FindSlider("Max Distance from edge (pixels)")
        MaxDistance = distSlider.value
    End Sub
End Class





Public Class Options_Hist3D : Inherits TaskParent
    Public addCloud As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Add color and cloud 8UC1")
            radio.addRadio("Copy cloud into color 8UC1")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static addRadio = FindRadio("Add color and cloud 8UC1")
        addCloud = addRadio.checked
    End Sub
End Class





Public Class Options_HOG : Inherits TaskParent
    Public thresholdHOG As Integer = 0
    Public strideHOG As Integer = 1
    Public scaleHOG As Double = 0.3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("HOG Threshold", 0, 100, thresholdHOG)
            sliders.setupTrackBar("HOG Stride", 1, 100, strideHOG)
            sliders.setupTrackBar("HOG Scale X1000", 0, 2000, scaleHOG * 1000)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("HOG Threshold")
        Static strideSlider = FindSlider("HOG Stride")
        Static scaleSlider = FindSlider("HOG Scale X1000")

        thresholdHOG = thresholdSlider.Value
        strideHOG = CInt(strideSlider.Value)
        scaleHOG = scaleSlider.Value / 1000
    End Sub
End Class






Public Class Options_Images : Inherits TaskParent
    Public fileNameForm As New OptionsFileName
    Public fileIndex As Integer = 0
    Public fileNameList As New List(Of String)
    Public fileInputName As FileInfo
    Public dirName As String = ""
    Public imageSeries As Boolean = False
    Public fullsizeImage As cvb.Mat
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
    Public Sub RunOpt()
        Static nextCheck = FindCheckBox("Load the next image")
        If (task.heartBeat And imageSeries) Or fullsizeImage Is Nothing Then nextCheck.checked = True
        If nextCheck.checked = True Then
            If nextCheck.checked Then fileIndex += 1
            If fileIndex >= fileNameList.Count Then fileIndex = 0
            fileInputName = New FileInfo(fileNameList(fileIndex))
            fullsizeImage = cvb.Cv2.ImRead(fileInputName.FullName)
        End If
        nextCheck.checked = False
    End Sub
End Class






Public Class Options_VerticalVerify : Inherits TaskParent
    Public angleThreshold As Integer = 80
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum Arc-Y threshold angle (degrees)", 70, 90, angleThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static arcYslider = FindSlider("Minimum Arc-Y threshold angle (degrees)")
        angleThreshold = arcYslider.Value
    End Sub
End Class







Public Class Options_IMUPlot : Inherits TaskParent
    Public setBlue As Boolean = True
    Public setGreen As Boolean = True
    Public setRed As Boolean = True
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
    Public Sub RunOpt()
        Static blueCheck = FindCheckBox("Blue Variable")
        Static greenCheck = FindCheckBox("Green Variable")
        Static redCheck = FindCheckBox("Red Variable")
        setBlue = blueCheck.checked
        setGreen = greenCheck.checked
        setRed = redCheck.checked
    End Sub
End Class





Public Class Options_Kalman_VB : Inherits TaskParent
    Public kalmanInput As Double = 0
    Public noisyInput As Integer = 0
    Dim oRand As New System.Random
    Public angle As Double = 0
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
    Public Sub RunOpt()
        Static matrix As New List(Of Double)
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

        Dim scalefactor As Double = (scaleSlider.Value / 100) + 1 'This should be between 1 and 2
        Dim iRand = oRand.Next(0, noiseSlider.Value)
        noisyInput = CInt((kalmanInput * scalefactor) + biasSlider.Value + iRand - (noiseSlider.Value / 2))

        If noisyInput < 0 Then noisyInput = 0
        If noisyInput > noisyInputSlider.Maximum Then noisyInput = noisyInputSlider.Maximum
        noisyInputSlider.Value = noisyInput

        If matrix.Count > 0 Then
            Const MAX_INPUT = 20
            matrix(task.frameCount Mod MAX_INPUT) = kalmanInput
            Dim AverageOutput = (cvb.Mat.FromPixelData(MAX_INPUT, 1, cvb.MatType.CV_32F, matrix.ToArray)).Mean()(0)

            If AverageOutput < 0 Then AverageOutput = 0
            If AverageOutput > pointSlider.Maximum Then AverageOutput = pointSlider.Maximum
            pointSlider.Value = CInt(AverageOutput)

            Dim AverageDiff = CInt(Math.Abs(AverageOutput - kalmanInput) * 10)
            If AverageDiff > avgSlider.Maximum Then AverageDiff = avgSlider.Maximum
            avgSlider.Value = AverageDiff

            Dim KalmanOutput As Double = angle

            If KalmanOutput < 0 Then KalmanOutput = 0
            If KalmanOutput > outputSlider.Maximum Then KalmanOutput = outputSlider.Maximum
            outputSlider.Value = CInt(KalmanOutput)

            Dim KalmanDiff = CInt(Math.Abs(KalmanOutput - kalmanInput) * 10)
            If KalmanDiff > kDiffSlider.Maximum Then KalmanDiff = kDiffSlider.Maximum
            kDiffSlider.Value = KalmanDiff
        End If
    End Sub
End Class






Public Class Options_Kalman : Inherits TaskParent
    Public delta As Double = 0.05
    Public pdotEntry As Double = 0.3
    Public processCovar As Double = 0.0001
    Public averageInputCount As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Delta Time X100", 1, 30, delta * 100)
            sliders.setupTrackBar("pDot entry X1000", 0, 1000, pdotEntry * 1000)
            sliders.setupTrackBar("Process Covariance X10000", 0, 10000, processCovar * 10000)
            sliders.setupTrackBar("Average input count", 1, 500, averageInputCount)
        End If
    End Sub
    Public Sub RunOpt()
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






Public Class Options_LaneFinder : Inherits TaskParent
    Public inputName As String = "/Data/challenge.mp4"
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("challenge.mp4")
            radio.addRadio("solidWhiteRight.mp4")
            radio.addRadio("solidYellowLeft.mp4")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        inputName = findRadioText(frm.check)
    End Sub
End Class








Public Class Options_LaPlacianPyramid : Inherits TaskParent
    Public img As New cvb.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sharpest", 0, 10, 5)
            sliders.setupTrackBar("blurryMin", 0, 10, 1)
            sliders.setupTrackBar("blurryMed1", 0, 10, 1)
            sliders.setupTrackBar("blurryMed2", 0, 10, 1)
            sliders.setupTrackBar("blurryMax", 0, 10, 1)
            sliders.setupTrackBar("Saturate", 0, 10, 1)
        End If
    End Sub
    Public Sub RunOpt()
        Dim barCount = sliders.mytrackbars.Count

        ' this usage of sliders.mytrackbars(x) is OK as long as this algorithm is not reused in multiple places (which it isn't)
        Dim levelMat(barCount - 1) As cvb.Mat
        For i = 0 To barCount - 2
            Dim nextImg = img.PyrDown()
            levelMat(i) = (img - nextImg.PyrUp(img.Size)) * sliders.mytrackbars(i).Value
            img = nextImg
        Next
        levelMat(barCount - 1) = img * sliders.mytrackbars(barCount - 1).Value

        img = levelMat(barCount - 1)
        For i = barCount - 1 To 1 Step -1
            img = img.PyrUp(levelMat(i - 1).Size)
            img += levelMat(i - 1)
        Next
    End Sub
End Class






Public Class Options_LeftRight : Inherits TaskParent
    Public sliceY As Integer = 25
    Public sliceHeight As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Slice Starting Y", 0, 300, sliceY)
            sliders.setupTrackBar("Slice Height", 1, (dst2.Rows - 10) / 2, sliceHeight)
        End If
    End Sub
    Public Sub RunOpt()
        Static startYSlider = FindSlider("Slice Starting Y")
        Static hSlider = FindSlider("Slice Height")
        sliceY = startYSlider.Value
        sliceHeight = hSlider.Value
    End Sub
End Class






Public Class Options_LongLine : Inherits TaskParent
    Public maxCount As Integer = 25
    Public pad As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of lines to display", 0, 100, maxCount)
            sliders.setupTrackBar("Reduction for width/height in pixels", 1, 20, pad)
        End If
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Number of lines to display")
        Static searchSlider = FindSlider("Reduction for width/height in pixels")
        maxCount = countSlider.value
        pad = searchSlider.Value
    End Sub
End Class





Public Class Options_LUT_Create : Inherits TaskParent
    Public lutThreshold As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("LUT entry diff threshold", 1, 100, lutThreshold)
    End Sub
    Public Sub RunOpt()
        Static diffSlider = FindSlider("LUT entry diff threshold")
        lutThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_Mat : Inherits TaskParent
    Public decompType As cvb.DecompTypes = cvb.DecompTypes.Cholesky
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Cholesky")
            radio.addRadio("Eig (works but results are incorrect)")
            radio.addRadio("LU")
            radio.addRadio("Normal (not working)")
            radio.addRadio("QR (not working)")
            radio.addRadio("SVD (works but results are incorrect)")
            radio.check(0).Checked = True
            radio.check(3).Enabled = False ' not accepted!
            radio.check(4).Enabled = False ' not accepted!
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                decompType = Choose(i + 1, cvb.DecompTypes.Cholesky, cvb.DecompTypes.Eig, cvb.DecompTypes.LU, cvb.DecompTypes.Normal,
                                        cvb.DecompTypes.QR, cvb.DecompTypes.SVD)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_Match : Inherits TaskParent
    Public maxDistance As Integer = 5
    Public stdevThreshold As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, maxDistance)
            sliders.setupTrackBar("Stdev Threshold", 0, 100, stdevThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static distSlider = FindSlider("Maximum travel distance per frame")
        Static stdevSlider = FindSlider("Stdev Threshold")
        stdevThreshold = CSng(stdevSlider.Value)
        maxDistance = distSlider.Value
    End Sub
End Class







Public Class Options_Math : Inherits TaskParent
    Public showMean As Boolean = False
    Public showStdev As Boolean = False
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Show mean")
            check.addCheckBox("Show Stdev")
        End If
    End Sub
    Public Sub RunOpt()
        Static meanCheck = FindCheckBox("Show mean")
        Static stdevCheck = FindCheckBox("Show Stdev")
        showMean = meanCheck.checked
        showStdev = stdevCheck.checked
    End Sub
End Class






Public Class Options_MeanSubtraction : Inherits TaskParent
    Public scaleVal As Double = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Scaling Factor = mean/scaling factor X100", 1, 500, scaleVal * 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static scaleSlider = FindSlider("Scaling Factor = mean/scaling factor X100")
        scaleVal = scaleSlider.value / 100
    End Sub
End Class






Public Class Options_Mesh : Inherits TaskParent
    Public nabeCount As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of nearest neighbors", 1, 10, nabeCount)
    End Sub
    Public Sub RunOpt()
        Static nabeSlider = FindSlider("Number of nearest neighbors")
        nabeCount = nabeSlider.value
    End Sub
End Class






Public Class Options_OEX : Inherits TaskParent
    Public lows As cvb.Scalar = New cvb.Scalar(90, 50, 50)
    Public highs As cvb.Scalar = New cvb.Scalar(180, 150, 150)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Hue low", 0, 180, lows(0))
            sliders.setupTrackBar("Hue high", 0, 180, highs(0))
            sliders.setupTrackBar("Saturation low", 0, 255, lows(1))
            sliders.setupTrackBar("Saturation high", 0, 255, highs(1))
            sliders.setupTrackBar("Value low", 0, 255, lows(2))
            sliders.setupTrackBar("Value high", 0, 255, highs(2))
        End If
    End Sub
    Public Sub RunOpt()
        Static hueLowSlider = FindSlider("Hue low")
        Static hueHighSlider = FindSlider("Hue high")
        Static satLowSlider = FindSlider("Saturation low")
        Static satHighSlider = FindSlider("Saturation high")
        Static valLowSlider = FindSlider("Value low")
        Static valHighSlider = FindSlider("Value high")
        lows = New cvb.Scalar(hueLowSlider.value, satLowSlider.value, valLowSlider.value)
        highs = New cvb.Scalar(hueHighSlider.value, satHighSlider.value, valHighSlider.value)
    End Sub
End Class





Public Class Options_ORB : Inherits TaskParent
    Public desiredCount As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("ORB - desired point count", 10, 2000, desiredCount)
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("ORB - desired point count")
        desiredCount = countSlider.value
    End Sub
End Class






Public Class Options_Palette : Inherits TaskParent
    Public transitions As Integer = 7
    Public convertScale As Integer = 45
    Public schemeName As String = "schemeRandom"
    Public radius As Integer = 0
    Public schemes() As FileInfo
    Public Sub New()
        radius = dst2.Cols / 2
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Color transitions", 1, 255, transitions)
            sliders.setupTrackBar("Convert And Scale", 0, 100, convertScale)
            sliders.setupTrackBar("LinearPolar radius", 0, dst2.Cols, radius)
        End If
        Dim dirInfo = New DirectoryInfo(task.HomeDir + "Data")
        schemes = dirInfo.GetFiles("scheme*.jpg")
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            For i = 0 To schemes.Length - 1
                radio.addRadio(schemes(i).Name.Substring(0, schemes(i).Name.Length - 4))
                If schemes(i).Name = "schemeRandom" Then radio.check(i).Checked = True
            Next
        End If
    End Sub
    Public Sub RunOpt()
        Static paletteSlider = FindSlider("Color transitions")
        Static cvtScaleSlider = FindSlider("Convert And Scale")
        Static radiusSlider = FindSlider("LinearPolar radius")
        transitions = paletteSlider.value
        Static frm = FindFrm(traceName + " Radio Buttons")
        schemeName = schemes(findRadioIndex(frm.check)).FullName
        convertScale = cvtScaleSlider.value
        radius = radiusSlider.Value
    End Sub
End Class






Public Class Options_PCA : Inherits TaskParent
    Public retainedVariance As Double = 0.95
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Retained Variance X100", 1, 100, retainedVariance * 100)
    End Sub
    Public Sub RunOpt()
        Static retainSlider = FindSlider("Retained Variance X100")
        retainedVariance = retainSlider.value / 100
    End Sub
End Class





Public Class Options_Pendulum : Inherits TaskParent
    Public initialize As Boolean = False
    Public fps As Integer = 300
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Reset initial conditions")
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Pendulum FPS", 10, 1000, fps)
    End Sub
    Public Sub RunOpt()
        Static initCheck = FindCheckBox("Reset initial conditions")
        Static timeSlider = FindSlider("Pendulum FPS")
        If initCheck.checked Then initCheck.checked = False
        If task.FirstPass Then check.Box(0).Checked = True
        fps = timeSlider.value
    End Sub
End Class






Public Class Options_PhaseCorrelate : Inherits TaskParent
    Public shiftThreshold As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold shift to cause reset of lastFrame", 0, 100, shiftThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Threshold shift to cause reset of lastFrame")
        shiftThreshold = thresholdSlider.value
    End Sub
End Class





Public Class Options_PlaneFloor : Inherits TaskParent
    Public countThreshold As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Pixel Count threshold that indicates floor", 1, 100, countThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Pixel Count threshold that indicates floor")
        countThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_PlyFormat : Inherits TaskParent
    Public fileNameForm As New OptionsFileName
    Public fileName As String = ""
    Public playButton As String = ""
    Public allOptionsLeft As Integer = 0
    Public saveFileName As String = ""
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.HomeDir + "temp"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "ply (*.ply)|*.ply|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "plyFileName", "plyFileName", task.HomeDir + "temp\pointcloud.ply")

        fileNameForm.Text = "Select ply output file"
        fileNameForm.FileNameLabel.Text = "Select ply output file"
        fileNameForm.PlayButton.Text = "Save"
        fileNameForm.TrackBar1.Visible = False
        fileNameForm.Setup(traceName)
        fileNameForm.Show()
    End Sub
    Public Sub RunOpt()
        If task.FirstPass Then fileNameForm.Left = allOptions.Width / 3
        playButton = fileNameForm.PlayButton.Text
        fileName = fileNameForm.filename.Text

        Dim testDir = New FileInfo(fileNameForm.filename.Text)
        If testDir.Directory.Exists = False Then
            fileNameForm.filename.Text = task.HomeDir + "Temp\pointcloud.ply"
            If testDir.Directory.Name = "Temp" Then MkDir(testDir.Directory.FullName)
        End If

        If saveFileName <> fileName And fileName.Length > 0 Then
            SaveSetting("OpenCVB", "plyFileName", "plyFileName", fileName)
            saveFileName = fileName
        End If
    End Sub
End Class







Public Class Options_PointCloud : Inherits TaskParent
    Public deltaThreshold As Double = 5
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Delta Z threshold (cm)", 0, 100, deltaThreshold)
    End Sub
    Public Sub RunOpt()
        Static deltaSlider = FindSlider("Delta Z threshold (cm)")
        deltaThreshold = deltaSlider.value / 100
    End Sub
End Class




Public Class Options_PolyLines : Inherits TaskParent
    Public polyCount As Integer = 100
    Public polyClosed As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Polyline closed if checked")
            check.Box(0).Checked = polyClosed
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Polyline Count", 2, 500, polyCount)
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Polyline Count")
        Static closeCheck = FindCheckBox("Polyline closed if checked")
        polyCount = countSlider.value
        polyClosed = closeCheck.checked
    End Sub
End Class






Public Class Options_Projection : Inherits TaskParent
    Public topCheck As Boolean = True
    Public index As Integer = 0
    Public projectionThreshold As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Index of object", 0, 100, index) ' zero is the largest object present.
            sliders.setupTrackBar("Concentration threshold", 0, 100, projectionThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("Concentration threshold")
        Static topCheckBox = FindCheckBox("Top View (Unchecked Side View)")
        Static objSlider = FindSlider("Index of object")
        index = objSlider.value
        If topCheckBox IsNot Nothing Then topCheck = topCheckBox.checked
        projectionThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_Puzzle : Inherits TaskParent
    Public startPuzzle As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Start another puzzle")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static startBox = FindCheckBox("Start another puzzle")
        startPuzzle = startBox.checked
        startBox.checked = False
    End Sub
End Class






Public Class Options_Pyramid : Inherits TaskParent
    Public zoom As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Zoom in and out", -1, 1, zoom)
    End Sub
    Public Sub RunOpt()
        Static zoomSlider = FindSlider("Zoom in and out")
        zoom = zoomSlider.Value
    End Sub
End Class






Public Class Options_PyrFilter : Inherits TaskParent
    Public spatialRadius As Integer = 1
    Public colorRadius As Integer = 20
    Public maxPyramid As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("MeanShift Spatial Radius", 1, 100, spatialRadius)
            sliders.setupTrackBar("MeanShift color Radius", 1, 100, colorRadius)
            sliders.setupTrackBar("MeanShift Max Pyramid level", 1, 8, maxPyramid)
        End If
    End Sub
    Public Sub RunOpt()
        Static radiusSlider = FindSlider("MeanShift Spatial Radius")
        Static colorSlider = FindSlider("MeanShift color Radius")
        Static maxSlider = FindSlider("MeanShift Max Pyramid level")
        spatialRadius = radiusSlider.value
        colorRadius = colorSlider.value
        maxPyramid = maxSlider.value
    End Sub
End Class





Public Class Options_NormalDist : Inherits TaskParent
    Public blueVal As Integer = 125
    Public greenVal As Integer = 25
    Public redVal As Integer = 180
    Public stdev As Integer = 50
    Public grayChecked As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Random_NormalDist Blue Mean", 0, 255, blueVal)
            sliders.setupTrackBar("Random_NormalDist Green Mean", 0, 255, greenVal)
            sliders.setupTrackBar("Random_NormalDist Red Mean", 0, 255, redVal)
            sliders.setupTrackBar("Random_NormalDist Stdev", 0, 255, stdev)
        End If

        If check.Setup(traceName) Then check.addCheckBox("Use Grayscale image")
    End Sub
    Public Sub RunOpt()
        Static blueSlider = FindSlider("Random_NormalDist Blue Mean")
        Static greenSlider = FindSlider("Random_NormalDist Green Mean")
        Static redSlider = FindSlider("Random_NormalDist Red Mean")
        Static stdevSlider = FindSlider("Random_NormalDist Stdev")
        redVal = redSlider.value
        greenVal = greenSlider.value
        blueVal = blueSlider.value
        stdev = stdevSlider.value

        Static grayCheck = FindCheckBox("Use Grayscale image")
        grayChecked = grayCheck.checked
    End Sub
End Class






Public Class Options_MonteCarlo : Inherits TaskParent
    Public dimension As Integer = 91
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of bins", 1, 255, dimension)
    End Sub
    Public Sub RunOpt()
        Static binSlider = FindSlider("Number of bins")
        dimension = binSlider.value
    End Sub
End Class





Public Class Options_StaticTV : Inherits TaskParent
    Public rangeVal As Integer = 50
    Public threshPercent As Double = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Range of noise to apply (from 0 to this value)", 0, 255, rangeVal)
            sliders.setupTrackBar("Percentage of pixels to include noise", 0, 100, threshPercent)
        End If
    End Sub
    Public Sub RunOpt()
        Static valSlider = FindSlider("Range of noise to apply (from 0 to this value)")
        Static threshSlider = FindSlider("Percentage of pixels to include noise")
        rangeVal = valSlider.Value
        threshPercent = threshSlider.Value
    End Sub
End Class







Public Class Options_Clusters : Inherits TaskParent
    Public numClusters As Integer = 9
    Public numPoints As Integer = 20
    Public stdev As Double = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of Clusters", 1, 10, numClusters)
            sliders.setupTrackBar("Number of points per cluster", 1, 100, numPoints)
            sliders.setupTrackBar("Cluster stdev", 0, 100, stdev)
        End If
    End Sub
    Public Sub RunOpt()
        Static clustSlider = FindSlider("Number of Clusters")
        Static numSlider = FindSlider("Number of points per cluster")
        Static stdevSlider = FindSlider("Cluster stdev")
        numClusters = clustSlider.Value
        numPoints = numSlider.Value
        stdev = stdevSlider.Value
    End Sub
End Class






Public Class Options_Draw : Inherits TaskParent
    Public proximity As Integer = 250
    Public drawCount As Integer = 3
    Public drawFilled As Integer = 2
    Public drawRotated As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DrawCount", 0, 20, drawCount)
            sliders.setupTrackBar("Merge rectangles within X pixels", 0, dst2.Width, If(dst2.Width = 1280, proximity * 2, proximity))
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            check.addCheckBox("Draw filled (unchecked draw an outline)")
        End If
    End Sub
    Public Sub RunOpt()
        Static mergeSlider = FindSlider("Merge rectangles within X pixels")
        Static countSlider = FindSlider("DrawCount")
        Static fillCheck = FindCheckBox("Draw filled (unchecked draw an outline)")
        Static rotateCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        drawCount = countSlider.Value
        drawFilled = If(fillCheck.checked, -1, 2)
        drawRotated = rotateCheck.checked
        proximity = mergeSlider.Value
    End Sub
End Class






Public Class Options_RBF : Inherits TaskParent
    Public RBFCount As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RBF Recursion count", 1, 20, RBFCount)
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("RBF Recursion count")
        RBFCount = countSlider.value
    End Sub
End Class







Public Class Options_RedCloudOther : Inherits TaskParent
    Public range As Integer = 30
    Public reduceAmt As Integer = 250
    Public threshold As Double = 0.95
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Grayscale range around mean", 0, 100, range)
            sliders.setupTrackBar("RedCloud_Reduce Reduction", 1, 2500, reduceAmt)
            sliders.setupTrackBar("Percent featureLess threshold", 1, 100, threshold * 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static rangeSlider = FindSlider("Grayscale range around mean")
        Static reductionSlider = FindSlider("RedCloud_Reduce Reduction")
        Static thresholdSlider = FindSlider("Percent featureLess threshold")
        threshold = thresholdSlider.Value / 100.0
        reduceAmt = reductionSlider.value
        range = rangeSlider.value
    End Sub
End Class






Public Class Options_RedCloudFeatures : Inherits TaskParent
    Public selection As Integer = 3
    Public labelName As String = "Correlation Y to Z"
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("MaxDist Location")
            radio.addRadio("Depth mean")
            radio.addRadio("Correlation X to Z")
            radio.addRadio("Correlation Y to Z")
            radio.check(3).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        selection = 0
        labelName = ""
        For selection = 0 To frm.check.Count - 1
            If frm.check(selection).Checked Then
                labelName = frm.check(selection).text
                Exit For
            End If
        Next

    End Sub
End Class







Public Class Options_RedTrack : Inherits TaskParent
    Public maxDistance As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, maxDistance)
    End Sub
    Public Sub RunOpt()
        Static distSlider = FindSlider("Max feature travel distance")
        maxDistance = distSlider.Value
    End Sub
End Class







Public Class Options_Reduction : Inherits TaskParent
    Public reduceXYZ() As Boolean = {True, True, True}
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Reduce point cloud in X direction")
            check.addCheckBox("Reduce point cloud in Y direction")
            check.addCheckBox("Reduce point cloud in Z direction")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
            check.Box(2).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        For i = 0 To 2
            reduceXYZ(i) = check.Box(i).Checked
        Next
    End Sub
End Class





Public Class Options_Retina : Inherits TaskParent
    Public useLogSampling As Boolean = False
    Public sampleFactor As Integer = 2
    Public xmlCheck As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Retina Sample Factor", 1, 10, sampleFactor)
        If check.Setup(traceName) Then
            check.addCheckBox("Use log sampling")
            check.addCheckBox("Open resulting xml file")
        End If
    End Sub
    Public Sub RunOpt()
        Static sampleSlider = FindSlider("Retina Sample Factor")
        Static logCheckbox = FindCheckBox("Use log sampling")
        Static xmlCheckbox = FindCheckBox("Open resulting xml file")
        useLogSampling = logCheckbox.Checked
        If xmlCheckbox.checked Then xmlCheckbox.checked = False
        xmlCheck = xmlCheckbox.checked
        sampleFactor = sampleSlider.value
    End Sub
End Class






Public Class Options_ROI : Inherits TaskParent
    Public roiPercent As Double = 0.25
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max size area of interest %", 0, 100, roiPercent * 100)
    End Sub
    Public Sub RunOpt()
        Static roiSlider = FindSlider("Max size area of interest %")
        roiPercent = roiSlider.value / 100
    End Sub
End Class






Public Class Options_Rotate : Inherits TaskParent
    Public rotateAngle As Double = 24
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Rotation Angle in degrees", -180, 180, rotateAngle)
    End Sub
    Public Sub RunOpt()
        Static angleSlider = FindSlider("Rotation Angle in degrees")
        rotateAngle = angleSlider.Value
    End Sub
End Class





Public Class Options_Salience : Inherits TaskParent
    Public numScales As Integer = 6
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Salience numScales", 1, 6, numScales)
    End Sub
    Public Sub RunOpt()
        Static scaleSlider = FindSlider("Salience numScales")
        numScales = scaleSlider.Value
    End Sub
End Class







Public Class Options_SLRImages : Inherits TaskParent
    Public radioText As String = "Grayscale input"
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("pcSplit(2) input")
            radio.addRadio("Grayscale input")
            radio.addRadio("Blue input")
            radio.addRadio("Green input")
            radio.addRadio("Red input")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For Each rad In frm.check
            If rad.checked Then radioText = rad.text
        Next
    End Sub
End Class






Public Class Options_StabilizerOther : Inherits TaskParent
    Public fastThreshold As Integer = 0
    Public range As Integer = 8
    Public Sub New()
        fastThreshold = task.FASTthreshold
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("FAST Threshold", 0, 200, fastThreshold)
            sliders.setupTrackBar("Range of random motion introduced (absolute value in pixels)", 0, 30, fastThreshold)
        End If
    End Sub
    Public Sub RunOpt()
        Static thresholdSlider = FindSlider("FAST Threshold")
        Static rangeSlider = FindSlider("Range of random motion introduced (absolute value in pixels)")
        range = rangeSlider.Value
        fastThreshold = thresholdSlider.value
    End Sub
End Class





Public Class Options_Stabilizer : Inherits TaskParent
    Public lostMax As Double = 0.1
    Public width As Integer = 128
    Public height As Integer = 96
    Public minStdev As Double = 10
    Public corrThreshold As Double = 0.95
    Public pad As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max % of lost pixels before reseting image", 0, 100, lostMax * 100)
            sliders.setupTrackBar("Width of input to matchtemplate", 10, dst2.Width - pad, width)
            sliders.setupTrackBar("Height of input to matchtemplate", 10, dst2.Height - pad, height)
            sliders.setupTrackBar("Min stdev in correlation rect", 1, 50, minStdev)
            sliders.setupTrackBar("Stabilizer Correlation Threshold X1000", 0, 1000, corrThreshold * 1000)
        End If
    End Sub
    Public Sub RunOpt()
        Static widthSlider = FindSlider("Width of input to matchtemplate")
        Static heightSlider = FindSlider("Height of input to matchtemplate")
        Static netSlider = FindSlider("Max % of lost pixels before reseting image")
        Static stdevSlider = FindSlider("Min stdev in correlation rect")
        Static thresholdSlider = FindSlider("Stabilizer Correlation Threshold X1000")
        lostMax = netSlider.value / 100
        minStdev = stdevSlider.value
        corrThreshold = thresholdSlider.value / 1000
        width = widthSlider.value
        height = heightSlider.value
    End Sub
End Class







Public Class Options_Stitch : Inherits TaskParent
    Public imageCount As Integer = 10
    Public width As Integer = 0
    Public height As Integer = 0
    Public Sub New()
        width = task.dst2.Width / 2
        height = task.dst2.Height / 2
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of random images", 10, 50, 10)
            sliders.setupTrackBar("Rectangle width", task.dst2.Width / 4, task.dst2.Width - 1, width)
            sliders.setupTrackBar("Rectangle height", task.dst2.Height / 4, task.dst2.Height - 1, height)
        End If
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Number of random images")
        Static widthSlider = FindSlider("Rectangle width")
        Static heightSlider = FindSlider("Rectangle height")
        imageCount = countSlider.Value
        width = widthSlider.Value
        height = heightSlider.Value
    End Sub
End Class






Public Class Options_StructuredFloor : Inherits TaskParent
    Public xCheck As Boolean = False
    Public yCheck As Boolean = True
    Public zCheck As Boolean = False
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Smooth in X-direction")
            check.addCheckBox("Smooth in Y-direction")
            check.addCheckBox("Smooth in Z-direction")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static xCheckbox = FindCheckBox("Smooth in X-direction")
        Static yCheckbox = FindCheckBox("Smooth in Y-direction")
        Static zCheckbox = FindCheckBox("Smooth in Z-direction")
        xCheck = xCheckbox.checked
        yCheck = yCheckbox.checked
        zCheck = zCheckbox.checked
    End Sub
End Class







Public Class Options_StructuredCloud : Inherits TaskParent
    Public xLines As Integer = 50
    Public yLines As Integer = 50
    Public indexX As Integer = 50
    Public indexY As Integer = 50
    Public threshold As Integer = 10
    Public xConstraint As Boolean = True
    Public yConstraint As Boolean = True
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Lines in X-Direction", 0, 200, xLines)
            sliders.setupTrackBar("Lines in Y-Direction", 0, 200, yLines)
            sliders.setupTrackBar("Slice index X", 1, 200, indexX)
            sliders.setupTrackBar("Slice index Y", 1, 200, indexY)
            sliders.setupTrackBar("Continuity threshold in mm", 0, 100, threshold)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Impose constraints on X")
            check.addCheckBox("Impose constraints on Y")
            check.Box(0).Checked = xConstraint
            check.Box(1).Checked = yConstraint
        End If
    End Sub
    Public Sub RunOpt()
        Static xLineSlider = FindSlider("Lines in X-Direction")
        Static yLineSlider = FindSlider("Lines in Y-Direction")
        Static thresholdSlider = FindSlider("Continuity threshold in mm")
        Static xSlider = FindSlider("Slice index X")
        Static ySlider = FindSlider("Slice index Y")

        Static xCheck = FindCheckBox("Impose constraints on X")
        Static yCheck = FindCheckBox("Impose constraints on Y")

        xLines = xLineSlider.Value
        yLines = yLineSlider.Value
        threshold = thresholdSlider.Value

        xConstraint = xCheck.checked
        yConstraint = yCheck.checked

        indexX = xSlider.value
        indexY = ySlider.value
    End Sub
End Class






Public Class Options_StructuredMulti : Inherits TaskParent
    Public maxSides As Integer = 4
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of sides in the identified polygons", 3, 100, maxSides)
        End If
    End Sub
    Public Sub RunOpt()
        Static sidesSlider = FindSlider("Max number of sides in the identified polygons")
        maxSides = sidesSlider.Value
    End Sub
End Class





Public Class Options_Structured : Inherits TaskParent
    Public rebuilt As Boolean = True
    Public sliceSize As Integer = 1
    Public stepSize As Integer = 6
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Structured Depth slice thickness in pixels", 1, 10, sliceSize)
            sliders.setupTrackBar("Slice step size in pixels (multi-slice option only)", 1, 100, stepSize)
        End If
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Show original data")
            radio.addRadio("Show rebuilt data")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static sliceSlider = FindSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = FindSlider("Slice step size in pixels (multi-slice option only)")
        Static rebuiltRadio = FindRadio("Show rebuilt data")
        rebuilt = rebuiltRadio.checked
        sliceSize = sliceSlider.Value
        stepSize = stepSlider.Value
    End Sub
End Class






Public Class Options_SuperPixels : Inherits TaskParent
    Public numSuperPixels As Integer = 400
    Public numIterations As Integer = 4
    Public prior As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of SuperPixels", 1, 1000, numSuperPixels)
            sliders.setupTrackBar("SuperPixel Iterations", 0, 10, numIterations)
            sliders.setupTrackBar("Prior", 1, 10, prior)
        End If
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Number of SuperPixels")
        Static iterSlider = FindSlider("SuperPixel Iterations")
        Static priorSlider = FindSlider("Prior")
        numSuperPixels = countSlider.value
        numIterations = iterSlider.value
        prior = priorSlider.value
    End Sub
End Class





Public Class Options_Swarm : Inherits TaskParent
    Public ptCount As Integer = 2
    Public border As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Connect X KNN points", 1, 10, ptCount)
            sliders.setupTrackBar("Distance to image border", 1, 10, border)
        End If
    End Sub
    Public Sub RunOpt()
        Static ptSlider = FindSlider("Connect X KNN points")
        Static borderSlider = FindSlider("Distance to image border")
        ptCount = ptSlider.value
        border = borderSlider.value
    End Sub
End Class





Public Class Options_SwarmPercent : Inherits TaskParent
    Public percent As Double = 0.8
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Cells map X percent", 1, 100, percent * 100)
    End Sub
    Public Sub RunOpt()
        Static percentSlider = FindSlider("Cells map X percent")
        percent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_Texture : Inherits TaskParent
    Public TFdelta As Integer = 30
    Public TFblockSize As Integer = 50
    Public TFksize As Integer = 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Texture Flow Delta", 2, 100, TFdelta)
            sliders.setupTrackBar("Texture Eigen BlockSize", 1, 100, TFblockSize)
            sliders.setupTrackBar("Texture Eigen Ksize", 1, 15, TFksize)
        End If
    End Sub
    Public Sub RunOpt()
        Static deltaSlider = FindSlider("Texture Flow Delta")
        Static blockSlider = FindSlider("Texture Eigen BlockSize")
        Static ksizeSlider = FindSlider("Texture Eigen Ksize")

        TFdelta = deltaSlider.Value
        TFblockSize = blockSlider.Value * 2 + 1
        TFksize = ksizeSlider.Value * 2 + 1
    End Sub
End Class







Public Class Options_ThresholdDef : Inherits TaskParent
    Public threshold As Integer = 127
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Threshold", 0, 255, threshold)
    End Sub
    Public Sub RunOpt()
        Static truncateSlider = FindSlider("Threshold")
        threshold = truncateSlider.value
    End Sub
End Class





Public Class Options_Tracker : Inherits TaskParent
    Public trackType As Integer = 1
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Boosting")
            radio.addRadio("MIL")
            radio.addRadio("KCF - appears to not work...")
            radio.addRadio("TLD")
            radio.addRadio("MedianFlow")
            radio.addRadio("GoTurn - appears to not work...")
            radio.addRadio("Mosse")
            radio.addRadio("TrackerCSRT - Channel and Spatial Reliability Tracker")
            radio.check(trackType).Checked = True
            radio.check(2).Enabled = False
            radio.check(5).Enabled = False
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                labels(2) = "Method: " + radio.check(i).Text
                trackType = i
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Transform : Inherits TaskParent
    Public resizeFactor As Double = 0.5
    Public angle As Integer = 30
    Public scale As Double = 1
    Public firstCheck As Boolean = False
    Public secondCheck As Boolean = False
    Public centerX As Integer = 0
    Public centerY As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resize Percent", 50, 1000, resizeFactor * 100)
            sliders.setupTrackBar("Angle", -180, 180, angle)
            sliders.setupTrackBar("Scale Factor% (100% means no scaling)", 1, 100, scale * 100)
            sliders.setupTrackBar("Rotation center X", 1, dst2.Width, dst2.Width / 2)
            sliders.setupTrackBar("Rotation center Y", 1, dst2.Height, dst2.Height / 2)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Check to snap the first point cloud")
            check.addCheckBox("Check to snap the second point cloud")
        End If
    End Sub
    Public Sub RunOpt()
        Static angleSlider = FindSlider("Angle")
        Static scaleSlider = FindSlider("Scale Factor% (100% means no scaling)")
        Static centerXSlider = FindSlider("Rotation center X")
        Static centerYSlider = FindSlider("Rotation center Y")
        Static firstCheckBox = FindCheckBox("Check to snap the first point cloud")
        Static secondCheckBox = FindCheckBox("Check to snap the second point cloud")
        Static percentSlider = FindSlider("Resize Percent")
        resizeFactor = percentSlider.Value / 100
        firstCheck = firstCheckBox.checked
        secondCheck = secondCheckBox.checked

        angle = angleSlider.value
        scale = scaleSlider.value / 100
        centerX = centerXSlider.value
        centerY = centerYSlider.value
    End Sub
End Class






Public Class Options_TransformationMatrix : Inherits TaskParent
    Public mul As Integer = 500
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("TMatrix Top View multiplier", 1, 1000, mul)
    End Sub
    Public Sub RunOpt()
        Static multSlider = FindSlider("TMatrix Top View multiplier")
        mul = multSlider.value
    End Sub
End Class





Public Class Options_Vignetting : Inherits TaskParent
    Public radius As Double = 80
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Vignette radius X100", 1, 300, radius)
    End Sub
    Public Sub RunOpt()
        Static radiusSlider = FindSlider("Vignette radius X100")
        radius = radiusSlider.value / 100
    End Sub
End Class





Public Class Options_Video : Inherits TaskParent
    Public fileNameForm As New OptionsFileName
    Public fileInfo As FileInfo
    Public maxFrames As Integer = 1000
    Public currFrame As Integer = 0
    Public Sub New()
        fileInfo = New FileInfo(task.HomeDir + "Data\CarsDrivingUnderBridge.mp4")
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.HomeDir + "Data\"
        fileNameForm.OpenFileDialog1.FileName = "*.mp4"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "video files (*.mp4)|*.mp4|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "VideoFileName", "VideoFileName", fileInfo.FullName)
        fileNameForm.Text = "Select a video file for input"
        fileNameForm.FileNameLabel.Text = "Select a video file for input"
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        fileNameForm.filename.Text = fileInfo.FullName
    End Sub
    Public Sub RunOpt()
        If task.optionsChanged Then
            maxFrames = 1000
            currFrame = 0
            If fileNameForm.newFileName Then fileInfo = New FileInfo(fileNameForm.filename.Text)
            If fileInfo.Exists = False Then
                SetTrueText("File not found: " + fileInfo.FullName, New cvb.Point(10, 125))
                Exit Sub
            End If
        End If
        fileNameForm.TrackBar1.Maximum = maxFrames
        fileNameForm.TrackBar1.Value = currFrame
    End Sub
End Class






Public Class Options_WarpAffine : Inherits TaskParent
    Public angle As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Angle", 0, 360, angle)
    End Sub
    Public Sub RunOpt()
        Static angleSlider = FindSlider("Angle")
        angle = angleSlider.value
    End Sub
End Class






Public Class Options_WarpPerspective : Inherits TaskParent
    Public width As Integer = 0
    Public height As Integer = 0
    Public angle As Integer = 0
    Public Sub New()
        width = dst2.Cols - 50
        height = dst2.Rows - 50
        angle = 0
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Warped Width", 0, dst2.Cols, width)
            sliders.setupTrackBar("Warped Height", 0, dst2.Rows, height)
            sliders.setupTrackBar("Warped Angle", 0, 360, angle)
        End If
    End Sub
    Public Sub RunOpt()
        Static wSlider = FindSlider("Warped Width")
        Static hSlider = FindSlider("Warped Height")
        Static angleSlider = FindSlider("Warped Angle")
        width = wSlider.value
        height = hSlider.value
        angle = angleSlider.value
    End Sub
End Class






Public Class Options_XPhotoInpaint : Inherits TaskParent
    Public FSRFast As Boolean = False
    Public shiftMap As Boolean = False
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("FSR_Best")
            radio.addRadio("FSR_Fast")
            radio.addRadio("ShiftMap")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static radioFast = FindRadio("FSR_Fast")
        Static radioSMap = FindRadio("ShiftMap")
        FSRFast = radioFast.checked
        shiftMap = radioSMap.checked
    End Sub
End Class






Public Class Options_Density : Inherits TaskParent
    Public zCount As Integer = 3
    Public distance As Double = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Distance in meters X10000", 1, 2000, task.densityMetric)
            sliders.setupTrackBar("Neighboring Z count", 0, 8, zCount)
        End If
        distance = task.densityMetric
    End Sub
    Public Sub RunOpt()
        Static distSlider = FindSlider("Distance in meters X10000")
        Static neighborSlider = FindSlider("Neighboring Z count")
        zCount = neighborSlider.value
        distance = distSlider.value / 10000
    End Sub
End Class






Public Class Options_Edge_Basics : Inherits TaskParent
    Public edgeSelection As String = "Canny"
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Canny")
            radio.addRadio("Scharr")
            radio.addRadio("Sobel")
            radio.addRadio("Resize And Add")
            radio.addRadio("Binarized Reduction")
            radio.addRadio("Binarized Sobel")
            radio.addRadio("Color Gap")
            radio.addRadio("Deriche")
            radio.addRadio("Laplacian")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        edgeSelection = frm.check(findRadioIndex(frm.check)).text

        If task.frameCount < 100 Or task.optionsChanged Then frm.left = task.gOptions.Width / 2
    End Sub
End Class





Public Class Options_ColorMethod : Inherits TaskParent
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("BackProject_Full")
            check.addCheckBox("BackProject2D_Full")
            check.addCheckBox("Bin4Way_Regions")
            check.addCheckBox("Binarize_DepthTiers")
            check.addCheckBox("FeatureLess_Groups")
            check.addCheckBox("Hist3Dcolor_Basics")
            check.addCheckBox("KMeans_Basics")
            check.addCheckBox("LUT_Basics")
            check.addCheckBox("Reduction_Basics")
            check.addCheckBox("PCA_NColor_CPP_VB")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
    End Sub
End Class






Public Class Options_DiffDepth : Inherits TaskParent
    Public millimeters As Integer
    Public meters As Double
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Depth varies more than X mm's", 1, 2000, 1000)
    End Sub
    Public Sub RunOpt()
        Static mmSlider = FindSlider("Depth varies more than X mm's")
        millimeters = mmSlider.value
        meters = millimeters / 1000
    End Sub
End Class







Public Class Options_Outliers : Inherits TaskParent
    Public cutoffPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Cutoff bins with less than %", 0, 100, 1)
        End If
    End Sub
    Public Sub RunOpt()
        Static percentSlider = FindSlider("Cutoff bins with less than %")
        cutoffPercent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_BP_Regions : Inherits TaskParent
    Public cellCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of cells to identify", 1, 100, 50)
    End Sub
    Public Sub RunOpt()
        Static countSlider = FindSlider("Number of cells to identify")
        cellCount = countSlider.value
    End Sub
End Class





Public Class Options_ML : Inherits TaskParent
    Public ML_Name As String
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("ANN_MLP")
            radio.addRadio("Boost")
            radio.addRadio("DTrees")
            radio.addRadio("KNearest")
            radio.addRadio("LogisticRegression")
            radio.addRadio("NormalBayesClassifier")
            radio.addRadio("RTrees")
            radio.addRadio("SVM")
            radio.check(6).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static frm = FindFrm(traceName + " Radio Buttons")
        ML_Name = frm.check(findRadioIndex(frm.check)).Text
        If task.frameCount < 100 Or task.optionsChanged Then frm.left = task.gOptions.Width / 2 + 10
    End Sub
End Class






Public Class Options_GridFromResize : Inherits TaskParent
    Public lowResPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("LowRes %", 1, 100, 10)
    End Sub
    Public Sub RunOpt()
        Static percentSlider = FindSlider("LowRes %")
        lowResPercent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_LaplacianKernels : Inherits TaskParent
    Public gaussiankernelSize As Integer = 1
    Public LaplaciankernelSize As Integer = 3
    Public threshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gaussian Kernel", 1, 32, gaussiankernelSize)
            sliders.setupTrackBar("Laplacian Kernel", 1, 32, LaplaciankernelSize)
            sliders.setupTrackBar("Laplacian Threshold", 0, 255, 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static gaussSlider = FindSlider("Gaussian Kernel")
        Static LaplacianSlider = FindSlider("Laplacian Kernel")
        Static thresholdSlider = FindSlider("Laplacian Threshold")
        gaussiankernelSize = gaussSlider.Value Or 1
        LaplaciankernelSize = LaplacianSlider.Value Or 1
        threshold = thresholdSlider.Value
    End Sub
End Class






Public Class Options_LinearInput : Inherits TaskParent
    Public delta As Single
    Public dimension As Integer
    Public zy As Boolean ' true means use col while false means use row
    Public offsetDirection As String
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Delta (mm)", 1, 1000, 25)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("X Direction")
            radio.addRadio("Y Direction")
            radio.addRadio("Z in X-Direction")
            radio.addRadio("Z in Y-Direction")
            radio.addRadio("X and Y-Direction")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static deltaSlider = FindSlider("Delta (mm)")
        delta = deltaSlider.value / 1000

        Static frm = FindFrm(traceName + " Radio Buttons")
        offsetDirection = frm.check(findRadioIndex(frm.check)).Text

        Static xRadio = FindRadio("X Direction")
        Static yRadio = FindRadio("Y Direction")
        Static zxRadio = FindRadio("Z in X-Direction")
        Static zyRadio = FindRadio("Z in Y-Direction")

        dimension = 2
        If xRadio.checked Then dimension = 0
        If yRadio.checked Then dimension = 1

        If zyRadio.checked Then zy = True Else zy = False
    End Sub
End Class






Public Class Options_ImageOffset : Inherits TaskParent
    Public delta As Single
    Public offsetDirection As String
    Public horizontalSlice As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Delta (mm)", 1, 1000, 25)

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Upper Left")
            radio.addRadio("Above")
            radio.addRadio("Upper Right")
            radio.addRadio("Left")
            radio.addRadio("Right")
            radio.addRadio("Lower Left")
            radio.addRadio("Below")
            radio.addRadio("Below Right")
            radio.check(7).Checked = True
        End If


        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Slice Horizontally (off Vertically)")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub RunOpt()
        Static deltaSlider = FindSlider("Delta (mm)")
        delta = deltaSlider.value / 1000

        Static frm = FindFrm(traceName + " Radio Buttons")
        offsetDirection = frm.check(findRadioIndex(frm.check)).Text

        Static sliceDirection = FindCheckBox("Slice Horizontally (off Vertically)")
        horizontalSlice = sliceDirection.checked
    End Sub
End Class





Public Class Options_LowRes : Inherits TaskParent
    Public colorDifferenceThreshold As Integer
    Public Sub New()
        Dim thresholdVal = 3
        If task.cameraName.StartsWith("StereoLabs ZED 2") Then thresholdVal = 4

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Color difference threshold", 0, 100, thresholdVal)
        End If
    End Sub
    Public Sub RunOpt()
        Static diffSlider = FindSlider("Color difference threshold")
        colorDifferenceThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_Line : Inherits TaskParent
    Public minLength As Integer
    Public maxIntersection As Integer
    Public correlation As Single
    Public topX As Integer
    Public overlapPercent As Single
    Public minDistance As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Line Length", 1, 100, dst2.Height / 10)
            sliders.setupTrackBar("Intersection Maximum Pixel Count", 1, 100, 15)
            sliders.setupTrackBar("Min Correlation", 1, 100, 95)
            sliders.setupTrackBar("Top X count", 1, 254, 3)
            sliders.setupTrackBar("Same line overlap %", 1, 100, 50)
            sliders.setupTrackBar("Distance to next center", 1, 100, 30)
        End If
    End Sub
    Public Sub RunOpt()
        Static lenSlider = FindSlider("Min Line Length")
        Static interSlider = FindSlider("Intersection Maximum Pixel Count")
        Static correlSlider = FindSlider("Min Correlation")
        Static topXSlider = FindSlider("Top X count")
        Static overlapSlider = FindSlider("Same line overlap %")
        Static distanceSlider = FindSlider("Distance to next center")
        minLength = lenSlider.value
        maxIntersection = interSlider.value
        correlation = correlSlider.value / 100
        topX = topXSlider.value
        overlapPercent = overlapSlider.value / 100
        minDistance = distanceSlider.value
    End Sub
End Class










Public Class Options_OpenGLFunctions : Inherits TaskParent
    Public moveAmount As cvb.Scalar = New cvb.Scalar(0, 0, 0)
    Public FOV As Double = 75
    Public yaw As Double = -3
    Public pitch As Double = 3
    Public roll As Double = 0
    Public zNear As Double = 0
    Public zFar As Double = 20.0
    Public zTrans As Double = 0.5
    Public eye As cvb.Vec3f = New cvb.Vec3f(0, 0, -40)
    Public scaleXYZ As cvb.Vec3f = New cvb.Vec3f(10, 10, 1)
    Public PointSizeSlider As TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL shift left/right (X-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift up/down (Y-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift fwd/back (Z-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, 2)
        End If
        PointSizeSlider = FindSlider("OpenGL Point Size")
    End Sub
    Public Sub RunOpt()
        Static XmoveSlider = FindSlider("OpenGL shift left/right (X-axis) X100")
        Static YmoveSlider = FindSlider("OpenGL shift up/down (Y-axis) X100")
        Static ZmoveSlider = FindSlider("OpenGL shift fwd/back (Z-axis) X100")

        moveAmount = New cvb.Point3f(XmoveSlider.Value / 100, YmoveSlider.Value / 100, ZmoveSlider.Value / 100)
    End Sub
End Class






Public Class Options_OpenGL : Inherits TaskParent
    Public FOV As Double = 75
    Public yaw As Double = -3
    Public pitch As Double = 3
    Public roll As Double = 0
    Public zNear As Double = 0
    Public zFar As Double = 20
    Public pointSize As Integer = 2
    Public zTrans As Double = 0.5
    Public eye As cvb.Vec3f = New cvb.Vec3f(0, 0, -40)
    Public scaleXYZ As cvb.Vec3f = New cvb.Vec3f(10, 10, 1)
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
    Public Sub RunOpt()
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

        eye = New cvb.Vec3f(eyeXSlider.Value, eyeYSlider.Value, eyeZSlider.Value)
        scaleXYZ = New cvb.Vec3f(scaleXSlider.Value, scaleYSlider.Value, scaleZSlider.Value)
    End Sub
End Class