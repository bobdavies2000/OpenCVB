﻿Imports cv = OpenCvSharp
Imports System.IO
Imports System.Numerics
Imports OpenCvSharp.ML
Imports System.Windows.Forms
Public Class Options_Quaternion : Inherits OptionParent
    Public q1 As Quaternion = New Quaternion
    Public q2 As Quaternion = New Quaternion
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
    Public Sub Run()
        Static axSlider = OptionParent.FindSlider("quaternion A.x X100")
        Static aySlider = OptionParent.FindSlider("quaternion A.y X100")
        Static azSlider = OptionParent.FindSlider("quaternion A.z X100")
        Static athetaSlider = OptionParent.FindSlider("quaternion A Theta X100")

        Static bxSlider = OptionParent.FindSlider("quaternion B.x X100")
        Static bySlider = OptionParent.FindSlider("quaternion B.y X100")
        Static bzSlider = OptionParent.FindSlider("quaternion B.z X100")
        Static bthetaSlider = OptionParent.FindSlider("quaternion B Theta X100")

        q1 = New Quaternion(CSng(axSlider.Value / 100), CSng(aySlider.Value / 100),
                                CSng(azSlider.Value / 100), CSng(athetaSlider.Value / 100))
        q2 = New Quaternion(CSng(bxSlider.Value / 100), CSng(bySlider.Value / 100),
                                    CSng(bzSlider.Value / 100), CSng(bthetaSlider.Value / 100))
    End Sub
End Class




Public Class Options_Annealing : Inherits OptionParent
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
    Public Sub Run()
        Static travelCheck = FindCheckBox("Restart Traveling Salesman")
        Static circleCheck = FindCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static copyBestCheck = FindCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half")
        Static circularCheck = FindCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        Static citySlider = OptionParent.FindSlider("Anneal Number of Cities")
        Static successSlider = OptionParent.FindSlider("Success = top X threads agree on energy level.")

        copyBestFlag = copyBestCheck.checked
        circularFlag = circularCheck.checked
        cityCount = citySlider.Value
        successCount = successSlider.Value
        travelCheck.Checked = False
    End Sub
End Class






Public Class Options_CamShift : Inherits OptionParent
    Public camMax As Integer = 255
    Public camSBins As cv.Scalar = New cv.Scalar(0, 40, 32)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("CamShift vMin", 0, 255, camSBins(2))
            sliders.setupTrackBar("CamShift vMax", 0, 255, camMax)
            sliders.setupTrackBar("CamShift Smin", 0, 255, camSBins(1))
        End If
    End Sub
    Public Sub Run()
        Static vMinSlider = OptionParent.FindSlider("CamShift vMin")
        Static vMaxSlider = OptionParent.FindSlider("CamShift vMax")
        Static sMinSlider = OptionParent.FindSlider("CamShift Smin")

        Dim vMin = vMinSlider.Value
        Dim vMax = vMaxSlider.Value
        Dim sMin = sMinSlider.Value

        Dim min = Math.Min(vMin, vMax)
        camMax = Math.Max(vMin, vMax)
        camSBins = New cv.Scalar(0, sMin, min)
    End Sub
End Class







Public Class Options_Contours2 : Inherits OptionParent
    Public ApproximationMode As cv.ContourApproximationModes = cv.ContourApproximationModes.ApproxTC89KCOS
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("ApproxNone")
            radio.addRadio("ApproxSimple")
            radio.addRadio("ApproxTC89KCOS")
            radio.addRadio("ApproxTC89L1")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static radioChoices() As cv.ContourApproximationModes = {cv.ContourApproximationModes.ApproxNone,
                                cv.ContourApproximationModes.ApproxSimple, cv.ContourApproximationModes.ApproxTC89KCOS,
                                cv.ContourApproximationModes.ApproxTC89L1}
        Static frm = FindFrm(traceName + " Radio Buttons")
        ApproximationMode = radioChoices(findRadioIndex(frm.check))
    End Sub
End Class





Public Class Options_Contours : Inherits OptionParent
    Public retrievalMode As cv.RetrievalModes = cv.RetrievalModes.External
    Public ApproximationMode As cv.ContourApproximationModes = cv.ContourApproximationModes.ApproxTC89KCOS
    Public epsilon As Double = 0.03
    Public options2 As New Options_Contours2
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("CComp")
            radio.addRadio("External")
            radio.addRadio("FloodFill")
            radio.addRadio("List")
            radio.addRadio("Tree")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub Run()
        options2.Run()
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
    End Sub
End Class




Public Class Options_DepthTiers : Inherits OptionParent
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
    Public Sub Run()
        Static cmSlider = OptionParent.FindSlider("cm's per tier")
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






' https://answers.opencv.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Options_Encode : Inherits OptionParent
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
    Public Sub Run()
        Static qualitySlider = OptionParent.FindSlider("Encode Quality Level")
        Static scalingSlider = OptionParent.FindSlider("Encode Output Scaling")
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







Public Class Options_Filter : Inherits OptionParent
    Public kernelSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Filter kernel size", 1, 21, kernelSize)
    End Sub
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Filter kernel size")
        kernelSize = kernelSlider.value Or 1
    End Sub
End Class






Public Class Options_GeneticDrawing : Inherits OptionParent
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
    Public Sub Run()
        Static genSlider = OptionParent.FindSlider("Number of Generations")
        Static stageSlider = OptionParent.FindSlider("Number of Stages")
        Static brushSlider = OptionParent.FindSlider("Brush size Percentage")
        Static snapCheckbox = FindCheckBox("Snapshot Video input to initialize genetic drawing")
        Static strokeSlider = OptionParent.FindSlider("Brushstroke count per generation")

        If snapCheckbox.checked Then snapCheckbox.checked = False
        snapCheck = snapCheckbox.checked
        stageTotal = stageSlider.value
        generations = genSlider.value
        brushPercent = brushSlider.value / 100
        strokeCount = strokeSlider.value
    End Sub
End Class







Public Class Options_MatchShapes : Inherits OptionParent
    Public matchOption As cv.ShapeMatchModes = cv.ShapeMatchModes.I1
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
            sliders.setupTrackBar("Min Size % of image size", 0, 20, task.cols * task.rows / 100)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Match Threshold %")
        Static ySlider = OptionParent.FindSlider("Max Y Delta % (of height)")
        Static minSlider = OptionParent.FindSlider("Min Size % of image size")
        matchThreshold = thresholdSlider.Value / 100
        maxYdelta = ySlider.Value * task.rows / 100
        minSize = minSlider.value * task.cols * task.rows / 100

        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.ShapeMatchModes.I1, cv.ShapeMatchModes.I2, cv.ShapeMatchModes.I3)
                Exit For
            End If
        Next
    End Sub
End Class









Public Class Options_Plane : Inherits OptionParent
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
    Public Sub Run()
        Static rmsSlider = OptionParent.FindSlider("RMS error threshold for flat X100")
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






Public Class Options_Interpolate : Inherits OptionParent
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
        findRadio("WarpFillOutliers").Enabled = False
        findRadio("WarpInverseMap").Enabled = False
    End Sub
    Public Sub Run()
        Static resizeSlider = OptionParent.FindSlider("Interpolation Resize %")
        Static interpolationSlider = OptionParent.FindSlider("Interpolation Resize %")
        Static pixelSlider = OptionParent.FindSlider("Number of interplation pixels that changed")
        resizePercent = resizeSlider.value
        interpolationThreshold = interpolationSlider.value
        pixelCountThreshold = pixelSlider.value
    End Sub
End Class







Public Class Options_Resize : Inherits OptionParent
    Public warpFlag As cv.InterpolationFlags = cv.InterpolationFlags.Nearest
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
    Public Sub Run()
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








Public Class Options_Smoothing : Inherits OptionParent
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
    Public Sub Run()
        Static iterSlider = OptionParent.FindSlider("Smoothing iterations")
        Static tensionSlider = OptionParent.FindSlider("Smoothing tension X100 (Interior Only)")
        Static stepSlider = OptionParent.FindSlider("Step size when adding points (1 is identity)")
        iterations = iterSlider.Value
        interiorTension = tensionSlider.Value / 100
        stepSize = stepSlider.Value
    End Sub
End Class










Public Class Options_SuperRes : Inherits OptionParent
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
    Public Sub Run()
        Static radioChoices = {"farneback", "tvl1", "brox", "pyrlk"}
        Static iterSlider = OptionParent.FindSlider("SuperRes Iterations")
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
Public Class Options_SVM2 : Inherits OptionParent
    Public SVMType As Integer = cv.ML.SVM.Types.CSvc
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("SVM Type = CSvc")
            radio.addRadio("SVM Type = EpsSvr")
            radio.addRadio("SVM Type = NuSvc")
            radio.addRadio("SVM Type = NuSvr")
            radio.addRadio("SVM Type = OneClass")

            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                SVMType = Choose(i + 1, cv.ML.SVM.Types.CSvc, cv.ML.SVM.Types.EpsSvr, cv.ML.SVM.Types.NuSvc, cv.ML.SVM.Types.NuSvr, cv.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next
    End Sub
End Class









' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class Options_SVM : Inherits OptionParent
    Public kernelType As cv.ML.SVM.KernelTypes = cv.ML.SVM.KernelTypes.Poly
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

    Public Sub Run()
        Static radioChoices = {cv.ML.SVM.KernelTypes.Linear, cv.ML.SVM.KernelTypes.Poly, cv.ML.SVM.KernelTypes.Rbf, cv.ML.SVM.KernelTypes.Sigmoid}
        options2.Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        kernelType = radioChoices(findRadioIndex(frm.check))

        Static granSlider = OptionParent.FindSlider("Granularity")
        Static degreeSlider = OptionParent.FindSlider("SVM Degree")
        Static gammaSlider = OptionParent.FindSlider("SVM Gamma")
        Static coef0Slider = OptionParent.FindSlider("SVM Coef0 X100")
        Static svmCSlider = OptionParent.FindSlider("SVM C X100")
        Static svmNuSlider = OptionParent.FindSlider("SVM Nu X100")
        Static svmPSlider = OptionParent.FindSlider("SVM P X100")
        Static sampleSlider = OptionParent.FindSlider("SVM Sample Count")

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







Public Class Options_WarpModel : Inherits OptionParent
    Public useGradient As Boolean = False
    Public pkImage As cv.Mat
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
    Public Sub Run()
        Static gradientCheck = FindCheckBox("Use Gradient in WarpInput")
        Static frm = FindFrm(traceName + " Radio Buttons")

        If task.optionsChanged Then
            options2.Run()
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










Public Class Options_MinMaxNone : Inherits OptionParent
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
    Public Sub Run()
        Static frm As OptionsRadioButtons = FindFrm(traceName + " Radio Buttons")
        useMax = frm.check(0).Checked
        useMin = frm.check(1).Checked
        useNone = frm.check(2).Checked
    End Sub
End Class







Public Class Options_DCT : Inherits OptionParent
    Public dctFlag As cv.DctFlags = New cv.DctFlags
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
    Public Sub Run()
        Static removeSlider = OptionParent.FindSlider("Remove Frequencies < x")
        Static runLenSlider = OptionParent.FindSlider("Run Length Minimum")

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






Public Class Options_Eigen : Inherits OptionParent
    Public noisyPointCount As Integer = 100
    Public noiseOffset As Integer = 25
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Line Point Count", 5, 200, noisyPointCount)
            sliders.setupTrackBar("Line Noise", 1, 500, noiseOffset)
        End If
    End Sub
    Public Sub Run()
        Static linePairSlider = OptionParent.FindSlider("Line Point Count")
        Static noiseSlider = OptionParent.FindSlider("Line Noise")
        noisyPointCount = linePairSlider.Value
        noiseOffset = noiseSlider.Value
    End Sub
End Class







Public Class Options_FitLine : Inherits OptionParent
    Public radiusAccuracy As Integer = 10
    Public angleAccuracy As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Accuracy for the radius X100", 0, 100, radiusAccuracy)
            sliders.setupTrackBar("Accuracy for the angle X100", 0, 100, angleAccuracy)
        End If
    End Sub
    Public Sub Run()
        Static radiusSlider = OptionParent.FindSlider("Accuracy for the radius X100")
        Static angleSlider = OptionParent.FindSlider("Accuracy for the angle X100")
        radiusAccuracy = radiusSlider.Value
        angleAccuracy = angleSlider.Value
    End Sub
End Class







Public Class Options_Fractal : Inherits OptionParent
    Public iterations As Integer = 34
    Public resetCheck As CheckBox
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Mandelbrot iterations", 1, 50, iterations)
        If check.Setup(traceName) Then check.addCheckBox("Reset to original Mandelbrot")
        resetCheck = FindCheckBox("Reset to original Mandelbrot")
    End Sub
    Public Sub Run()
        Static iterSlider = OptionParent.FindSlider("Mandelbrot iterations")
        iterations = iterSlider.Value
    End Sub
End Class







Public Class Options_ProCon : Inherits OptionParent
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
    Public Sub Run()
        Static sizeSlider = OptionParent.FindSlider("Buffer Size")
        Static proSlider = OptionParent.FindSlider("Producer Workload Duration (ms)")
        Static conSlider = OptionParent.FindSlider("Consumer Workload Duration (ms)")
        If task.optionsChanged Then
            bufferSize = sizeSlider.Value
            pduration = proSlider.Value
            cduration = conSlider.Value
        End If
    End Sub
End Class






Public Class Options_OilPaint : Inherits OptionParent
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
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Kernel Size")
        Static intensitySlider = OptionParent.FindSlider("Intensity")
        Static filterSlider = OptionParent.FindSlider("Filter Size")
        Static thresholdSlider = OptionParent.FindSlider("OilPaint Threshold")
        kernelSize = kernelSlider.Value Or 1

        intensity = intensitySlider.Value
        threshold = thresholdSlider.Value

        filterSize = filterSlider.Value
    End Sub
End Class










Public Class Options_Pointilism : Inherits OptionParent
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
    Public Sub Run()
        Static radiusSlider = OptionParent.FindSlider("Smoothing Radius")
        Static strokeSlider = OptionParent.FindSlider("Stroke Scale")
        Static ellipStroke = findRadio("Use Elliptical stroke")
        smoothingRadius = radiusSlider.Value * 2 + 1
        strokeSize = strokeSlider.Value
        useElliptical = ellipStroke.checked
    End Sub
End Class







Public Class Options_MotionBlur : Inherits OptionParent
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
    Public Sub Run()
        Static deblurSlider = OptionParent.FindSlider("Deblur Restore Vector")
        Static angleSlider = OptionParent.FindSlider("Deblur Angle of Restore Vector")
        Static blurSlider = OptionParent.FindSlider("Motion Blur Length")
        Static blurAngleSlider = OptionParent.FindSlider("Motion Blur Angle")
        Static SNRSlider = OptionParent.FindSlider("Deblur Signal to Noise Ratio")
        Static gammaSlider = OptionParent.FindSlider("Deblur Gamma")
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






Public Class Options_BinarizeNiBlack : Inherits OptionParent
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
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Kernel Size")
        Static NiBlackSlider = OptionParent.FindSlider("Niblack k")
        Static kSlider = OptionParent.FindSlider("Nick k")
        Static skSlider = OptionParent.FindSlider("Sauvola k")
        Static rSlider = OptionParent.FindSlider("Sauvola r")
        kernelSize = kernelSlider.Value Or 1
        niBlackK = NiBlackSlider.Value / 1000
        nickK = kSlider.Value / 1000
        sauvolaK = skSlider.Value / 1000
        sauvolaR = rSlider.Value
    End Sub
End Class






Public Class Options_Bernson : Inherits OptionParent
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
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Kernel Size")
        Static contrastSlider = OptionParent.FindSlider("Contrast min")
        Static bgSlider = OptionParent.FindSlider("bg Threshold")
        kernelSize = kernelSlider.Value Or 1

        bgThreshold = bgSlider.Value
        contrastMin = contrastSlider.Value
    End Sub
End Class







Public Class Options_BlockMatching : Inherits OptionParent
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
    Public Sub Run()
        Static matchSlider = OptionParent.FindSlider("Blockmatch max disparity")
        Static sizeSlider = OptionParent.FindSlider("Blockmatch block size")
        Static distSlider = OptionParent.FindSlider("Blockmatch distance in meters")
        numDisparity = matchSlider.Value * 16 ' must be a multiple of 16
        blockSize = sizeSlider.Value Or 1
        distance = distSlider.Value
    End Sub
End Class






Public Class Options_Cartoonify : Inherits OptionParent
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
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Cartoon Median Blur kernel")
        Static kernel2Slider = OptionParent.FindSlider("Cartoon Median Blur kernel 2")
        Static thresholdSlider = OptionParent.FindSlider("Cartoon threshold")
        Static laplaceSlider = OptionParent.FindSlider("Cartoon Laplacian kernel")
        medianBlur = kernelSlider.Value Or 1
        medianBlur2 = kernel2Slider.Value Or 1
        kernelSize = laplaceSlider.Value Or 1
        threshold = thresholdSlider.Value
    End Sub
End Class






Public Class Options_Dither : Inherits OptionParent
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
    Public Sub Run()
        Static bppSlider = OptionParent.FindSlider("Bits per color plane (Nbpp only)")
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







Public Class Options_SymmetricalShapes : Inherits OptionParent
    Public rotateAngle As Double = 0
    Public fillColor As cv.Scalar = New cv.Scalar(0, 0, 255)
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
            sliders.setupTrackBar("Radius 1", 1, task.rows / 2, task.rows / 4)
            sliders.setupTrackBar("Radius 2", 1, task.rows / 2, task.rows / 8)
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
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Sample Size")
        Static r1Slider = OptionParent.FindSlider("Radius 1")
        Static r2Slider = OptionParent.FindSlider("Radius 2")
        Static nGenPerSlider = OptionParent.FindSlider("nGenPer")
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






Public Class Options_DrawArc : Inherits OptionParent
    Public saveMargin As Integer = 32
    Public drawFull As Boolean = False
    Public drawFill As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Clearance from image edge (margin size)", 5, task.cols / 8, saveMargin * 16)
        End If

        If radio.Setup(traceName) Then
            radio.addRadio("Draw Full Ellipse")
            radio.addRadio("Draw Filled Arc")
            radio.addRadio("Draw Arc")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static marginSlider = OptionParent.FindSlider("Clearance from image edge (margin size)")
        Static fillCheck = findRadio("Draw Filled Arc")
        Static fullCheck = findRadio("Draw Full Ellipse")
        saveMargin = marginSlider.Value / 16
        drawFull = fullCheck.checked
        drawFill = fillCheck.checked
    End Sub
End Class






Public Class Options_FilterNorm : Inherits OptionParent
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
    Public Sub Run()
        Static alphaSlider = OptionParent.FindSlider("Normalize alpha X10")

        Dim normType = cv.NormTypes.L1
        kernel = cv.Mat.FromPixelData(1, 21, cv.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
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





Public Class Options_SepFilter2D : Inherits OptionParent
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
    Public Sub Run()
        Static xSlider = OptionParent.FindSlider("Kernel X size")
        Static ySlider = OptionParent.FindSlider("Kernel Y size")
        Static sigmaSlider = OptionParent.FindSlider("SepFilter2D Sigma X10")
        Static diffCheckBox = FindCheckBox("Show Difference SepFilter2D and Gaussian")
        xDim = xSlider.Value Or 1
        yDim = ySlider.Value Or 1
        sigma = sigmaSlider.Value / 10
        diffCheck = diffCheckBox.checked
    End Sub
End Class






Public Class Options_IMUFrameTime : Inherits OptionParent
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
    Public Sub Run()
        Static minSliderHost = OptionParent.FindSlider("Minimum Host interrupt delay (ms)")
        Static minSliderIMU = OptionParent.FindSlider("Minimum IMU to Capture time (ms)")
        Static plotSlider = OptionParent.FindSlider("Number of Plot Values")
        minDelayIMU = minSliderIMU.Value
        minDelayHost = minSliderHost.Value
        plotLastX = plotSlider.Value
    End Sub
End Class






Public Class Options_KLT : Inherits OptionParent
    Public ptInput() As cv.Point2f
    Public maxCorners As Integer = 100
    Public qualityLevel As Double = 0.01
    Public minDistance As Integer = 7
    Public blockSize As Integer = 7
    Public nightMode As Boolean = False
    Public subPixWinSize As cv.Size = New cv.Size(10, 10)
    Public winSize As cv.Size = New cv.Size(3, 3)
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
    Public Sub Run()
        Static maxSlider = OptionParent.FindSlider("KLT - MaxCorners")
        Static qualitySlider = OptionParent.FindSlider("KLT - qualityLevel")
        Static minSlider = OptionParent.FindSlider("KLT - minDistance")
        Static blockSlider = OptionParent.FindSlider("KLT - BlockSize")
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







Public Class Options_Laplacian : Inherits OptionParent
    Public kernel As cv.Size = New cv.Size(3, 3)
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
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Laplacian Kernel size")
        Static scaleSlider = OptionParent.FindSlider("Laplacian Scale")
        Static deltaSlider = OptionParent.FindSlider("Laplacian Delta")
        Static thresholdSlider = OptionParent.FindSlider("Laplacian Threshold")
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








Public Class Options_OpticalFlow : Inherits OptionParent
    Public pyrScale As Double = 0.35
    Public levels As Integer = 1
    Public winSize As Integer = 1
    Public iterations As Integer = 1
    Public polyN As Double = 0
    Public polySigma As Double = 0
    Public OpticalFlowFlags As cv.OpticalFlowFlags = cv.OpticalFlowFlags.FarnebackGaussian
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
    Public Sub Run()
        Static scaleSlider = OptionParent.FindSlider("Optical Flow pyrScale")
        Static levelSlider = OptionParent.FindSlider("Optical Flow Levels")
        Static sizeSlider = OptionParent.FindSlider("Optical Flow winSize")
        Static iterSlider = OptionParent.FindSlider("Optical Flow Iterations")
        Static polySlider = OptionParent.FindSlider("Optical Flow PolyN")
        Static flowSlider = OptionParent.FindSlider("Optical Flow Scaling Out")

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






Public Class Options_OpticalFlowSparse : Inherits OptionParent
    Public OpticalFlowFlag As cv.OpticalFlowFlags = cv.OpticalFlowFlags.FarnebackGaussian
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
    Public Sub Run()
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







Public Class Options_XPhoto : Inherits OptionParent
    Public colorCode As Integer = cv.ColorConversionCodes.BGR2GRAY
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
    Public Sub Run()
        Static ratioSlider = OptionParent.FindSlider("XPhoto Dynamic Ratio")
        Static sizeSlider = OptionParent.FindSlider("XPhoto Block Size")
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







Public Class Options_InPaint : Inherits OptionParent
    Public telea As Boolean = False
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("TELEA")
            radio.addRadio("Navier-Stokes")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static teleaRadio = findRadio("TELEA")
        telea = teleaRadio.checked
    End Sub
End Class










Public Class Options_RotatePoly : Inherits OptionParent
    Public changeCheck As CheckBox
    Public angleSlider As TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Amount to rotate triangle", -180, 180, 10)

        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Change center of rotation and triangle")
        End If

        angleSlider = OptionParent.FindSlider("Amount to rotate triangle")
        changeCheck = FindCheckBox("Change center of rotation and triangle")
    End Sub
    Public Sub Run()
    End Sub
End Class












Public Class Options_FPoly : Inherits OptionParent
    Public removeThreshold As Integer = 4
    Public autoResyncAfterX As Integer = 500
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Resync if feature moves > X pixels", 1, 20, removeThreshold)
            sliders.setupTrackBar("Points to use in Feature Poly", 2, 100, 10)
            sliders.setupTrackBar("Automatically resync after X frames", 10, 1000, autoResyncAfterX)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
        Static pointSlider = OptionParent.FindSlider("Points to use in Feature Poly")
        Static resyncSlider = OptionParent.FindSlider("Automatically resync after X frames")
        removeThreshold = thresholdSlider.Value
        task.polyCount = pointSlider.Value
        autoResyncAfterX = resyncSlider.Value
    End Sub
End Class













Public Class Options_Homography : Inherits OptionParent
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
    Public Sub Run()
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












Public Class Options_Random : Inherits OptionParent
    Public count As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Random Pixel Count", 1, task.cols * task.rows, 20)
        End If
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Random Pixel Count")
        count = countSlider.value
    End Sub
End Class








Public Class Options_Hough : Inherits OptionParent
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
    Public Sub Run()
        Static rhoslider = OptionParent.FindSlider("Hough rho")
        Static thetaSlider = OptionParent.FindSlider("Hough theta")
        Static thresholdSlider = OptionParent.FindSlider("Hough threshold")
        Static lineSlider = OptionParent.FindSlider("Lines to Plot")
        Static relativeSlider = OptionParent.FindSlider("Relative Intensity (Accord)")

        rho = rhoslider.Value
        theta = thetaSlider.Value / 1000
        threshold = thresholdSlider.Value
        lineCount = lineSlider.Value
        relativeIntensity = relativeSlider.Value / 100
    End Sub
End Class







Public Class Options_Canny : Inherits OptionParent
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
    Public Sub Run()
        Static t1Slider = OptionParent.FindSlider("Canny threshold1")
        Static t2Slider = OptionParent.FindSlider("Canny threshold2")
        Static apertureSlider = OptionParent.FindSlider("Canny Aperture")

        threshold1 = t1Slider.Value
        threshold2 = t2Slider.Value
        aperture = apertureSlider.Value Or 1
    End Sub
End Class







Public Class Options_ColorMatch : Inherits OptionParent
    Public maxDistanceCheck As Boolean = False
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show Max Distance point")
        End If
    End Sub
    Public Sub Run()
        Static maxCheck = FindCheckBox("Show Max Distance point")
        maxDistanceCheck = maxCheck.checked
    End Sub
End Class









Public Class Options_Sort : Inherits OptionParent
    Public sortOption As cv.SortFlags = cv.SortFlags.EveryColumn + cv.SortFlags.Ascending
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

        radio0 = findRadio("EveryColumn, Ascending")
        radio1 = findRadio("EveryColumn, Descending")
        radio2 = findRadio("EveryRow, Ascending")
        radio3 = findRadio("EveryRow, Descending")
        radio4 = findRadio("Sort all pixels ascending")
        radio5 = findRadio("Sort all pixels descending")
    End Sub
    Public Sub Run()
        Static sortSlider = OptionParent.FindSlider("Threshold for sort input")
        If radio1.Checked Then sortOption = cv.SortFlags.EveryColumn + cv.SortFlags.Descending
        If radio2.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Ascending
        If radio3.Checked Then sortOption = cv.SortFlags.EveryRow + cv.SortFlags.Descending
        sortThreshold = sortSlider.value
    End Sub
End Class






Public Class Options_Distance : Inherits OptionParent
    Public distanceType As cv.DistanceTypes = cv.DistanceTypes.L1
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("C")
            radio.addRadio("L1")
            radio.addRadio("L2")
            radio.check(1).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static cRadio = findRadio("C")
        Static l1Radio = findRadio("L1")
        Static l2Radio = findRadio("L2")
        If cRadio.Checked Then distanceType = cv.DistanceTypes.C
        If l1Radio.Checked Then distanceType = cv.DistanceTypes.L1
        If l2Radio.Checked Then distanceType = cv.DistanceTypes.L2
    End Sub
End Class









Public Class Options_Warp : Inherits OptionParent
    Public alpha As Double = 0
    Public beta As Double = 0
    Public gamma As Double = 0
    Public f As Double = 0
    Public distance As Double = 0
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
    Public Sub Run()
        Static alphaSlider = OptionParent.FindSlider("Alpha")
        Static betaSlider = OptionParent.FindSlider("Beta")
        Static gammaSlider = OptionParent.FindSlider("Gamma")
        Static fSlider = OptionParent.FindSlider("f")
        Static distanceSlider = OptionParent.FindSlider("distance")

        alpha = CDbl(alphaSlider.value - 90) * cv.Cv2.PI / 180
        beta = CDbl(betaSlider.value - 90) * cv.Cv2.PI / 180
        gamma = CDbl(gammaSlider.value - 90) * cv.Cv2.PI / 180
        f = fSlider.value
        distance = distanceSlider.value

        Dim a(,) As Double = {{1, 0, -task.cols / 2},
                            {0, 1, -task.rows / 2},
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

        Dim b(,) As Double = {{f, 0, task.cols / 2, 0},
                            {0, f, task.rows / 2, 0},
                            {0, 0, 1, 0}}

        Dim a1 = cv.Mat.FromPixelData(4, 3, cv.MatType.CV_64F, a)
        Dim rx = cv.Mat.FromPixelData(4, 4, cv.MatType.CV_64F, x)
        Dim ry = cv.Mat.FromPixelData(4, 4, cv.MatType.CV_64F, y)
        Dim rz = cv.Mat.FromPixelData(4, 4, cv.MatType.CV_64F, z)

        Dim tt = cv.Mat.FromPixelData(4, 4, cv.MatType.CV_64F, t)
        Dim a2 = cv.Mat.FromPixelData(3, 4, cv.MatType.CV_64F, b)

        Dim r = rx * ry * rz
        transformMatrix = a2 * (tt * (r * a1))
    End Sub
End Class








Public Class Options_HistCompare : Inherits OptionParent
    Public compareMethod As cv.HistCompMethods = cv.HistCompMethods.Correl
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
    Public Sub Run()
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












Public Class Options_MatchCell : Inherits OptionParent
    Public overlapPercent As Double = 0.5
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent overlap", 0, 100, overlapPercent * 100)
    End Sub
    Public Sub Run()
        Static overlapSlider = OptionParent.FindSlider("Percent overlap")
        overlapPercent = overlapSlider.value / 100
    End Sub
End Class








Public Class Options_Extrinsics : Inherits OptionParent
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
    Public Sub Run()
        Static leftSlider = OptionParent.FindSlider("Left image percent")
        Static rightSlider = OptionParent.FindSlider("Right image percent")
        Static heightSlider = OptionParent.FindSlider("Height percent")
        leftCorner = task.cols * leftSlider.value / 100
        rightCorner = task.cols * rightSlider.value / 100
        topCorner = task.rows * heightSlider.value / 100
    End Sub
End Class







Public Class Options_Translation : Inherits OptionParent
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
    Public Sub Run()
        Static leftSlider = OptionParent.FindSlider("Left translation percent")
        Static rightSlider = OptionParent.FindSlider("Right translation percent")
        leftTrans = task.cols * leftSlider.value / 100
        rightTrans = task.cols * rightSlider.value / 100
    End Sub
End Class












Public Class Options_OpenGL_Contours : Inherits OptionParent
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
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Filter threshold in meters X100")
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











Public Class Options_Motion : Inherits OptionParent
    Public motionThreshold As Integer = 0
    Public cumulativePercentThreshold As Double = 0.1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Single frame motion threshold", 1, task.cols * task.rows / 4, task.cols * task.rows / 16)
            sliders.setupTrackBar("Cumulative motion threshold percent of image", 1, 100, cumulativePercentThreshold * 100)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Single frame motion threshold")
        Static percentSlider = OptionParent.FindSlider("Cumulative motion threshold percent of image")
        motionThreshold = thresholdSlider.value
        cumulativePercentThreshold = percentSlider.value / 100
    End Sub
End Class








Public Class Options_Emax : Inherits OptionParent
    Public predictionStepSize As Integer = 5
    Public covarianceType = cv.EMTypes.CovMatDefault
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("EMax Prediction Step Size", 1, 20, predictionStepSize)

        If radio.Setup(traceName) Then
            radio.addRadio("EMax matrix type Spherical")
            radio.addRadio("EMax matrix type Diagonal")
            radio.addRadio("EMax matrix type Generic")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static stepSlider = OptionParent.FindSlider("EMax Prediction Step Size")
        predictionStepSize = stepSlider.value
        covarianceType = cv.EMTypes.CovMatDefault
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                covarianceType = Choose(i + 1, cv.EMTypes.CovMatSpherical, cv.EMTypes.CovMatDiagonal, cv.EMTypes.CovMatGeneric)
            End If
        Next
    End Sub
End Class









Public Class Options_Intercepts : Inherits OptionParent
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
    Public Sub showIntercepts(mousePoint As cv.Point, dst As cv.Mat)
    End Sub
    Public Sub Run()
        Static interceptSlider = OptionParent.FindSlider("Intercept width range in pixels")
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






Public Class Options_PlaneEstimation : Inherits OptionParent
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
    Public Sub Run()
        Static diagonalRadio = findRadio("Use diagonal lines")
        Static sidePointsRadio = findRadio("Use Contour_SidePoints to find the line pair")
        useDiagonalLines = diagonalRadio.checked
        useContour_SidePoints = sidePointsRadio.checked
    End Sub
End Class







Public Class Options_ForeGround : Inherits OptionParent
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
    Public Sub Run()
        Static depthSlider = OptionParent.FindSlider("Max foreground depth in mm's")
        Static minSizeSlider = OptionParent.FindSlider("Min length contour")
        Static regionSlider = OptionParent.FindSlider("Number of depth ranges")
        maxForegroundDepthInMeters = depthSlider.value / 1000
        minSizeContour = minSizeSlider.value
        numberOfRegions = regionSlider.value
        depthPerRegion = task.MaxZmeters / numberOfRegions
    End Sub
End Class






Public Class Options_Flood : Inherits OptionParent
    Public floodFlag As cv.FloodFillFlags = 4 Or cv.FloodFillFlags.FixedRange
    Public stepSize As Integer = 30
    Public minPixels As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Pixels", 1, 2000, 30)
            sliders.setupTrackBar("Step Size", 1, task.cols / 2, stepSize)
        End If
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use floating range")
            check.addCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        End If
    End Sub
    Public Sub Run()
        Static floatingCheck = FindCheckBox("Use floating range")
        Static connectCheck = FindCheckBox("Use connectivity 8 (unchecked in connectivity 4)")
        Static stepSlider = OptionParent.FindSlider("Step Size")
        Static minSlider = OptionParent.FindSlider("Min Pixels")

        stepSize = stepSlider.Value
        floodFlag = If(connectCheck.checked, 8, 4) Or If(floatingCheck.checked, cv.FloodFillFlags.FixedRange, 0)
        minPixels = minSlider.value
    End Sub
End Class






Public Class Options_ShapeDetect : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        fileName = findRadioText(frm.check)
    End Sub
End Class








Public Class Options_Blur : Inherits OptionParent
    Public kernelSize As Integer = 3
    Public sigmaX As Double = 1.5
    Public sigmaY As Double = 1.5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Blur Kernel Size", 0, 31, kernelSize)
            sliders.setupTrackBar("Blur SigmaX", 1, 10, sigmaX * 2)
            sliders.setupTrackBar("Blur SigmaY", 1, 10, sigmaY * 2)
        End If
    End Sub
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("Blur Kernel Size")
        Static sigmaXSlider = OptionParent.FindSlider("Blur SigmaX")
        Static sigmaYSlider = OptionParent.FindSlider("Blur SigmaY")
        kernelSize = kernelSlider.Value Or 1
        sigmaX = sigmaXSlider.value * 0.5
        sigmaY = sigmaYSlider.value * 0.5
    End Sub
End Class









Public Class Options_Wavelet : Inherits OptionParent
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
    Public Sub Run()
        Static iterSlider = OptionParent.FindSlider("Wavelet Iterations")
        Static haarRadio = findRadio("Haar")
        useHaar = haarRadio.checked
        iterations = iterSlider.value
    End Sub
End Class









Public Class Options_SOM : Inherits OptionParent
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
    Public Sub Run()
        Static iterSlider = OptionParent.FindSlider("Iterations (000's)")
        Static learnSlider = OptionParent.FindSlider("Initial Learning Rate %")
        Static radiusSlider = OptionParent.FindSlider("Radius in Pixels")
        iterations = iterSlider.value * 1000
        learningRate = learnSlider.value / 100
        radius = radiusSlider.value
    End Sub
End Class









Public Class Options_SURF : Inherits OptionParent
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
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Hessian threshold")
        Static BFRadio = findRadio("Use BF Matcher")
        Static rangeSlider = OptionParent.FindSlider("Surf Vertical Range to Search")
        Static countSlider = OptionParent.FindSlider("Points to Match")

        useBFMatcher = BFRadio.checked
        hessianThreshold = thresholdSlider.value
        verticalRange = rangeSlider.value
        pointCount = countSlider.value
    End Sub
End Class








Public Class Options_Sift : Inherits OptionParent
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
    Public Sub Run()
        Static bfRadio = findRadio("Use BF Matcher")
        Static countSlider = OptionParent.FindSlider("Points to Match")
        Static stepSlider = OptionParent.FindSlider("Sift StepSize")

        useBFMatcher = bfRadio.checked
        pointCount = countSlider.value
        stepSize = stepSlider.value
    End Sub
End Class







Public Class Options_Dilate : Inherits OptionParent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cv.MorphShapes = cv.MorphShapes.Cross
    Public element As cv.Mat
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
    Public Sub Run()
        Static ellipseRadio = findRadio("Dilate shape: Ellipse")
        Static rectRadio = findRadio("Dilate shape: Rect")
        Static iterSlider = OptionParent.FindSlider("Dilate Iterations")
        Static kernelSlider = OptionParent.FindSlider("Dilate Kernel Size")
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









Public Class Options_KMeans : Inherits OptionParent
    Public kMeansFlag As cv.KMeansFlags = cv.KMeansFlags.RandomCenters
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
        OptionParent.FindSlider("KMeans k").Value = k
    End Sub
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static kSlider = OptionParent.FindSlider("KMeans k")
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








Public Class Options_LUT : Inherits OptionParent
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
    End Sub
    Public Sub Run()
        Static lutSlider = OptionParent.FindSlider("Number of LUT Segments")
        Static zeroSlider = OptionParent.FindSlider("LUT zero through xxx")
        Static xSlider = OptionParent.FindSlider("LUT xxx through yyy")
        Static ySlider = OptionParent.FindSlider("LUT yyy through zzz")
        Static zSlider = OptionParent.FindSlider("LUT zzz through 255")

        splits = {zeroSlider.Value, xSlider.Value, ySlider.Value, zSlider.Value, 255}
        vals = {1, zeroSlider.Value, xSlider.Value, ySlider.Value, 255}
        lutSegments = lutSlider.value
    End Sub
End Class








Public Class Options_WarpModel2 : Inherits OptionParent
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
    End Sub
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then warpMode = i
        Next
        useWarpAffine = warpMode = 2
        useWarpHomography = warpMode = 3
    End Sub
End Class









' https://github.com/spmallick/learnopencv/tree/master/
Public Class Options_Photoshop : Inherits OptionParent
    Public switchColor As Integer = 3
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Second DuoTone Blue")
            radio.addRadio("Second DuoTone Green")
            radio.addRadio("Second DuoTone Red")
            radio.addRadio("Second DuoTone None")
            radio.check(switchColor).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For switchColor = 0 To frm.check.Count - 1
            If frm.check(switchColor).Checked Then Exit For
        Next
    End Sub
End Class





Public Class Options_Gif : Inherits OptionParent
    Public buildCheck As CheckBox
    Public restartRequest As Boolean
    Public dst0Radio As RadioButton
    Public dst1Radio As RadioButton
    Public dst2Radio As RadioButton
    Public dst3Radio As RadioButton
    Public Opencvwindow As RadioButton
    Public OpenGLwindow As RadioButton
    Public EntireScreen As RadioButton
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Step 1: Check this box when ready to capture the desired snapshot.")
            check.addCheckBox("Step 2: Build GIF file in <Opencv Home Directory>\Temp\myGIF.gif")
            check.addCheckBox("Optional: Restart - clear all previous images.")
        End If

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Capture dst0")
            radio.addRadio("Capture dst1")
            radio.addRadio("Capture dst2")
            radio.addRadio("Capture dst3")
            radio.addRadio("Capture entire OpenCVB app window")
            radio.addRadio("Capture OpenGL window")
            radio.addRadio("Capture entire screen")
            radio.check(4).Checked = True
        End If
        buildCheck = FindCheckBox("Step 2: Build GIF file in <Opencv Home Directory>\Temp\myGIF.gif")

        dst0Radio = findRadio("Capture dst0")
        dst1Radio = findRadio("Capture dst1")
        dst2Radio = findRadio("Capture dst2")
        dst3Radio = findRadio("Capture dst3")
        Opencvwindow = findRadio("Capture entire OpenCVB app window")
        OpenGLwindow = findRadio("Capture OpenGL window")
        EntireScreen = findRadio("Capture entire screen")
    End Sub
    Public Sub Run()
        Static frmCheck = FindFrm(traceName + " CheckBoxes")
        Static frmRadio = FindFrm(traceName + " Radio Buttons")
        If task.firstPass Or task.optionsChanged Then
            frmCheck.Left = task.gOptions.Width / 2
            frmCheck.top = task.gOptions.Height / 2
            frmRadio.left = task.gOptions.Width * 2 / 3
            frmRadio.top = task.gOptions.Height * 2 / 3
        End If

        If dst0Radio.Checked Then task.gifCaptureIndex = 0
        If dst1Radio.Checked Then task.gifCaptureIndex = 1
        If dst2Radio.Checked Then task.gifCaptureIndex = 2
        If dst3Radio.Checked Then task.gifCaptureIndex = 3
        If Opencvwindow.Checked Then task.gifCaptureIndex = 4
        If OpenGLwindow.Checked Then task.gifCaptureIndex = 5
        If EntireScreen.Checked Then task.gifCaptureIndex = 6

        Static restartCheck = FindCheckBox("Optional: Restart - clear all previous images.")
        restartRequest = restartCheck.checked
        restartCheck.checked = False

        task.optionsChanged = False
    End Sub
End Class






Public Class Options_IMU : Inherits OptionParent
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
    Public Sub Run()
        Static xRotateSlider = OptionParent.FindSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Z-axis (degrees)")
        Static alphaSlider = OptionParent.FindSlider("IMU_Basics: Alpha X100")
        Static stabilitySlider = OptionParent.FindSlider("IMU Stability Threshold (radians) X100")
        rotateX = xRotateSlider.value
        rotateY = yRotateSlider.value
        rotateZ = zRotateSlider.value
        task.IMU_AlphaFilter = alphaSlider.value / 100
        stableThreshold = stabilitySlider.value / 100
    End Sub
End Class





Public Class Options_FeatureMatch : Inherits OptionParent
    Public matchOption As cv.TemplateMatchModes = cv.TemplateMatchModes.CCoeffNormed
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
    Public Sub Run()
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





Public Class Options_HeatMap : Inherits OptionParent
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
    Public Sub Run()
        Static topCheck = FindCheckBox("Top View (Unchecked Side View)")
        Static redSlider = OptionParent.FindSlider("Threshold for Red channel")

        redThreshold = redSlider.value

        topView = topCheck.checked
        sideView = Not topView
        If sideView Then viewName = "horizontal"
    End Sub
End Class






Public Class Options_Boundary : Inherits OptionParent
    Public desiredBoundaries As Integer = 15
    Public peakDistance As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired boundary count", 2, 100, desiredBoundaries)
            sliders.setupTrackBar("Distance to next Peak (pixels)", 2, task.cols / 10, peakDistance)
        End If
    End Sub
    Public Sub Run()
        Static boundarySlider = OptionParent.FindSlider("Desired boundary count")
        Static distSlider = OptionParent.FindSlider("Distance to next Peak (pixels)")
        desiredBoundaries = boundarySlider.value
        peakDistance = distSlider.value
    End Sub
End Class









Public Class Options_Denoise : Inherits OptionParent
    Public removeSinglePixels As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Remove single pixels")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static singleCheck = FindCheckBox("Remove single pixels")
        removeSinglePixels = singleCheck.checked
    End Sub
End Class









'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class Options_MSER : Inherits OptionParent
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
        Select Case task.cols
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
    Public Sub Run()
        Static deltaSlider = OptionParent.FindSlider("MSER Delta")
        Static minAreaSlider = OptionParent.FindSlider("MSER Min Area")
        Static maxAreaSlider = OptionParent.FindSlider("MSER Max Area")
        Static variationSlider = OptionParent.FindSlider("MSER Max Variation")
        Static diversitySlider = OptionParent.FindSlider("MSER Diversity")
        Static evolutionSlider = OptionParent.FindSlider("MSER Max Evolution")
        Static thresholdSlider = OptionParent.FindSlider("MSER Area Threshold")
        Static marginSlider = OptionParent.FindSlider("MSER Min Margin")
        Static blurSlider = OptionParent.FindSlider("MSER Edge BlurSize")

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






Public Class Options_Spectrum : Inherits OptionParent
    Public gapDepth As Integer = 1
    Public gapGray As Integer = 1
    Public sampleThreshold As Integer = 10
    Public Sub New()
        task.redC = New RedColor_Basics
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Gap in depth spectrum (cm's)", 1, 50, gapDepth)
            sliders.setupTrackBar("Gap in gray spectrum", 1, 50, gapGray)
            sliders.setupTrackBar("Sample count threshold", 1, 50, sampleThreshold)
        End If
    End Sub
    Public Function buildDepthRanges(input As cv.Mat, typeSpec As String)
        Dim ranges As New List(Of rangeData)
        Dim sorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger) ' the spectrum of the values 
        Dim pixels As New List(Of Integer)
        Dim counts As New List(Of Integer)

        Dim rc = task.rcD
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
    Public Function buildColorRanges(input As cv.Mat, typespec As String) As List(Of rangeData)
        Dim ranges As New List(Of rangeData)
        Dim sorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger) ' the spectrum of the values 
        Dim pixels As New List(Of Integer)
        Dim counts As New List(Of Integer)

        Dim rc = task.rcD
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
    Public Sub Run()
        If task.firstPass Then task.redC.Run(task.color)
        Static frmSliders = FindFrm("Options_Spectrum Sliders")
        Static gapDSlider = OptionParent.FindSlider("Gap in depth spectrum (cm's)")
        Static gapGSlider = OptionParent.FindSlider("Gap in gray spectrum")
        Static countSlider = OptionParent.FindSlider("Sample count threshold")
        gapDepth = gapDSlider.value
        gapGray = gapGSlider.value
        sampleThreshold = countSlider.value

        If task.firstPass Then
            frmSliders.Left = task.gOptions.Width / 2
            frmSliders.top = task.gOptions.Height / 2
        End If
    End Sub
End Class






Public Class Options_HistXD : Inherits OptionParent
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
    Public Sub Run()
        Static topSlider = OptionParent.FindSlider("Min top bin samples")
        Static sideSlider = OptionParent.FindSlider("Min side bin samples")
        Static bothSlider = OptionParent.FindSlider("Min samples per bin")
        topThreshold = topSlider.value
        sideThreshold = sideSlider.value
        threshold3D = bothSlider.value
    End Sub
End Class






Public Class Options_Complexity : Inherits OptionParent
    Public filename As FileInfo
    Public filenames As List(Of String)
    Public plotColor As cv.Scalar = New cv.Scalar(255, 255, 0)
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
    Public Function setPlotColor() As cv.Scalar
        Static frm = FindFrm(traceName + " Radio Buttons")
        Dim index As Integer = 0
        For index = 0 To filenames.Count - 1
            If filename.FullName = filenames(index) Then Exit For
        Next
        plotColor = Choose(index Mod 4 + 1, cv.Scalar.White, cv.Scalar.Red, cv.Scalar.Green, cv.Scalar.Yellow)
        Return plotColor
    End Function
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.count - 1
            If frm.check(i).checked Then
                filename = New FileInfo(task.HomeDir + "Complexity/" + frm.check(i).text)
                plotColor = Choose((i + 1) Mod 4, cv.Scalar.White, cv.Scalar.Red, cv.Scalar.Green, cv.Scalar.Yellow)
                Exit For
            End If
        Next
        If task.firstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If
    End Sub
End Class






Public Class Options_BGSubtractSynthetic : Inherits OptionParent
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
    Public Sub Run()
        Static amplitudeSlider = OptionParent.FindSlider("Synthetic Amplitude x100")
        Static MagSlider = OptionParent.FindSlider("Synthetic Magnitude")
        Static speedSlider = OptionParent.FindSlider("Synthetic Wavespeed x100")
        Static objectSlider = OptionParent.FindSlider("Synthetic ObjectSpeed")

        amplitude = amplitudeSlider.Value
        magnitude = MagSlider.Value
        waveSpeed = speedSlider.Value
        objectSpeed = objectSlider.Value
    End Sub
End Class






Public Class Options_BGSubtract : Inherits OptionParent
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
    Public Sub Run()
        Static learnRateSlider = OptionParent.FindSlider("MOG Learn Rate X1000")
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







Public Class Options_Classifier : Inherits OptionParent
    Public methodIndex As Integer = 0
    Public methodName As String = "Normal Bayes (NBC)"
    Public sampleCount As Integer = 200
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Random Samples", 10, task.cols * task.rows, sampleCount)

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
    Public Sub Run()
        Static inputSlider = OptionParent.FindSlider("Random Samples")
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                methodIndex = i
                methodName = frm.check(i).Text
                Exit For
            End If
        Next
        If task.firstPass Then
            frm.Left = task.gOptions.Width / 2
            frm.top = task.gOptions.Height / 2
        End If

        sampleCount = inputSlider.value
    End Sub
End Class







Public Class Options_Threshold : Inherits OptionParent
    Public thresholdMethod As cv.ThresholdTypes = cv.ThresholdTypes.Binary
    Public thresholdName As String = ""
    Public threshold As Integer = 128
    Public gradient As New Gradient_Color
    Public inputGray As Boolean = False
    Public otsuOption As Boolean = False
    Public dst2 As cv.Mat
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
    Public Sub Run()
        If task.firstPass Then  ' special case!  Can't run it in constructor or measurements fail...
            gradient.Run(emptyMat)
            dst2 = gradient.dst2
        End If

        Static radioChoices = {cv.ThresholdTypes.Binary, cv.ThresholdTypes.BinaryInv,
                               cv.ThresholdTypes.Tozero, cv.ThresholdTypes.TozeroInv,
                               cv.ThresholdTypes.Triangle, cv.ThresholdTypes.Trunc}
        Static frm = FindFrm(traceName + " Radio Buttons")
        Dim index = findRadioIndex(frm.check)
        thresholdMethod = radioChoices(index)
        thresholdName = Choose(index + 1, "Binary", "BinaryInv", "Tozero", "TozeroInv", "Triangle", "Trunc")

        Static inputGrayCheck = FindCheckBox("GrayScale Input")
        Static otsuCheck = FindCheckBox("Add OTSU Option - a 50/50 split")
        Static threshSlider = OptionParent.FindSlider("Threshold value")

        inputGray = inputGrayCheck.checked
        otsuOption = otsuCheck.checked
        threshold = threshSlider.Value
    End Sub
End Class






Public Class Options_AdaptiveThreshold : Inherits OptionParent
    Public method As cv.AdaptiveThresholdTypes = cv.AdaptiveThresholdTypes.GaussianC
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

        findRadio("ToZero").Enabled = False
        findRadio("ToZero Inverse").Enabled = False
        findRadio("Trunc").Enabled = False
    End Sub
    Public Sub Run()
        Static gaussRadio = findRadio("GaussianC")
        Static constantSlider = OptionParent.FindSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = OptionParent.FindSlider("AdaptiveThreshold block size")

        method = If(gaussRadio.checked, cv.AdaptiveThresholdTypes.GaussianC, cv.AdaptiveThresholdTypes.MeanC)
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class







Public Class Options_Colors : Inherits OptionParent
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
    Public Sub Run()
        Static redSlider = OptionParent.FindSlider("Red")
        Static greenSlider = OptionParent.FindSlider("Green")
        Static blueSlider = OptionParent.FindSlider("Blue")
        redS = redSlider.Value
        greenS = greenSlider.Value
        blueS = blueSlider.Value
    End Sub
End Class





Public Class Options_Threshold_AdaptiveMin : Inherits OptionParent
    Public adaptiveMethod As cv.AdaptiveThresholdTypes = cv.AdaptiveThresholdTypes.GaussianC
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("GaussianC")
            radio.addRadio("MeanC")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static gaussRadio = findRadio("GaussianC")
        adaptiveMethod = If(gaussRadio.checked, cv.AdaptiveThresholdTypes.GaussianC, cv.AdaptiveThresholdTypes.MeanC)
    End Sub
End Class








Public Class Options_ThresholdAll : Inherits OptionParent
    Public thresholdMethod As cv.ThresholdTypes = cv.ThresholdTypes.Binary
    Public blockSize As Integer = 5
    Public constantVal As Integer = 0
    Public maxVal As Integer = 255
    Public threshold As Integer = 100
    Public gradient As New Gradient_Color
    Public inputGray As Boolean = False
    Public otsuOption As Boolean = False
    Public adaptiveMethod As cv.AdaptiveThresholdTypes = cv.AdaptiveThresholdTypes.GaussianC
    Dim options As New Options_Threshold_AdaptiveMin
    Public dst2 As cv.Mat
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
    Public Sub Run()
        If task.firstPass Then  ' special case!  Can't run it in constructor or measurements fail...
            gradient.Run(task.color.Clone)
            dst2 = gradient.dst2
        End If

        Dim radioChoices = {cv.ThresholdTypes.Binary, cv.ThresholdTypes.BinaryInv, cv.ThresholdTypes.Tozero,
                    cv.ThresholdTypes.TozeroInv, cv.ThresholdTypes.Triangle, cv.ThresholdTypes.Trunc}
        options.Run()
        adaptiveMethod = options.adaptiveMethod

        Static frm = FindFrm(traceName + " Radio Buttons")
        thresholdMethod = radioChoices(findRadioIndex(frm.check))

        Static inputGrayCheck = FindCheckBox("GrayScale Input")
        Static otsuCheck = FindCheckBox("Add OTSU Option - a 50/50 split")

        Static threshSlider = OptionParent.FindSlider("Threshold value")
        Static maxSlider = OptionParent.FindSlider("MaxVal setting")
        Static constantSlider = OptionParent.FindSlider("Constant subtracted from mean Or weighted mean")
        Static blockSlider = OptionParent.FindSlider("AdaptiveThreshold block size")

        inputGray = inputGrayCheck.checked
        otsuOption = otsuCheck.checked
        threshold = threshSlider.Value
        maxVal = maxSlider.Value
        blockSize = blockSlider.Value Or 1
        constantVal = constantSlider.value
    End Sub
End Class





Public Class Options_StdevGrid : Inherits OptionParent
    Public minThreshold As Integer = 30
    Public maxThreshold As Integer = 230
    Public diffThreshold As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min color threshold", 0, 50, minThreshold)
            sliders.setupTrackBar("Max color threshold", 0, 255, maxThreshold)
            sliders.setupTrackBar("Equal diff threshold", 0, 20, diffThreshold)
        End If
    End Sub
    Public Sub Run()
        Static minSlider = OptionParent.FindSlider("Min color threshold")
        Static maxSlider = OptionParent.FindSlider("Max color threshold")
        Static diffSlider = OptionParent.FindSlider("Equal diff threshold")
        minThreshold = minSlider.value
        maxThreshold = maxSlider.value
        diffThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_DFT : Inherits OptionParent
    Public radius As Integer = 120
    Public order As Integer = 2
    Public butterworthFilter(1) As cv.Mat
    Public dftFlag As cv.DftFlags = cv.DftFlags.ComplexOutput
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DFT B Filter - Radius", 1, task.rows, radius)
            sliders.setupTrackBar("DFT B Filter - Order", 1, task.rows, order)
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
    Public Sub Run()
        Static radiusSlider = OptionParent.FindSlider("DFT B Filter - Radius")
        Static orderSlider = OptionParent.FindSlider("DFT B Filter - Order")
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





Public Class Options_DFTShape : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm("Options_DFTShape Radio Buttons")
        dftShape = findRadioText(frm.check)
    End Sub
End Class






Public Class Options_FitEllipse : Inherits OptionParent
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
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("FitEllipse threshold")
        Static qRadio = findRadio("fitEllipseQ")
        Static amsRadio = findRadio("fitEllipseAMS")
        Static directRadio = findRadio("fitEllipseDirect")
        fitType = 0
        If amsRadio.checked Then fitType = 1
        If directRadio.checked Then fitType = 2

        threshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_TopX : Inherits OptionParent
    Public topX As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the top X cells", 1, 255, topX)
    End Sub
    Public Sub Run()
        Static topXSlider = OptionParent.FindSlider("Show the top X cells")
        topX = topXSlider.value
    End Sub
End Class






Public Class Options_EdgeOverlay : Inherits OptionParent
    Public xDisp As Integer = 7
    Public yDisp As Integer = 11
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Displacement in the X direction (in pixels)", 0, 100, xDisp)
            sliders.setupTrackBar("Displacement in the Y direction (in pixels)", 0, 100, yDisp)
        End If
    End Sub
    Public Sub Run()
        Static xSlider = OptionParent.FindSlider("Displacement in the X direction (in pixels)")
        Static ySlider = OptionParent.FindSlider("Displacement in the Y direction (in pixels)")
        xDisp = xSlider.Value
        yDisp = ySlider.Value
    End Sub
End Class







Public Class Options_AddWeighted : Inherits OptionParent
    Public addWeighted As Double = 0.5
    Public accumWeighted As Double = 0.1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Add Weighted %", 0, 100, addWeighted * 100)
            sliders.setupTrackBar("Accumulation weight of each image X100", 1, 100, accumWeighted * 100)
        End If
    End Sub
    Public Sub Run()
        Static weightSlider = OptionParent.FindSlider("Add Weighted %")
        Static accumSlider = OptionParent.FindSlider("Accumulation weight of each image X100")
        addWeighted = weightSlider.value / 100
        accumWeighted = accumSlider.value / 100
    End Sub
End Class







Public Class Options_ApproxPoly : Inherits OptionParent
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
    Public Sub Run()
        Static epsilonSlider = OptionParent.FindSlider("epsilon - max distance from original curve")
        Static closedPolyCheck = FindCheckBox("Closed polygon - connect first and last vertices.")
        epsilon = epsilonSlider.value
        closedPoly = closedPolyCheck.checked
    End Sub
End Class





Public Class Options_Bin3WayRedCloud : Inherits OptionParent
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
    Public Sub Run()
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






Public Class Options_Bin2WayRedCloud : Inherits OptionParent
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
    Public Sub Run()
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






Public Class Options_GuidedBPDepth : Inherits OptionParent
    Public bins As Integer = 1000
    Public maxClusters As Double = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram Bins for depth data", 3, 5000, bins)
            sliders.setupTrackBar("Maximum number of clusters", 1, 50, maxClusters)
        End If
    End Sub
    Public Sub Run()
        Static binSlider = OptionParent.FindSlider("Histogram Bins for depth data")
        Static clusterSlider = OptionParent.FindSlider("Maximum number of clusters")
        bins = binSlider.value
        maxClusters = clusterSlider.value
    End Sub
End Class







Public Class Options_OpenGL_Duster : Inherits OptionParent
    Public useClusterColors As Boolean = False
    Public useTaskPointCloud As Boolean = False
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Display cluster colors")
            check.addCheckBox("Use task.pointCloud")
        End If
    End Sub
    Public Sub Run()
        Static colorCheck = FindCheckBox("Display cluster colors")
        Static cloudCheck = FindCheckBox("Use task.pointCloud")
        useClusterColors = colorCheck.checked
        useTaskPointCloud = cloudCheck.checked
    End Sub
End Class







Public Class Options_AsciiArt : Inherits OptionParent
    Public hStep As Double = 31
    Public wStep As Double = 55
    Public size As cv.Size = New cv.Size(wStep, hStep)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Character height in pixels", 20, 100, hStep)
            sliders.setupTrackBar("Character width in pixels", 20, 200, wStep)
        End If
    End Sub
    Public Sub Run()
        Static hSlider = OptionParent.FindSlider("Character height in pixels")
        Static wSlider = OptionParent.FindSlider("Character width in pixels")

        hStep = CInt(task.rows / hSlider.value)
        wStep = CInt(task.cols / wSlider.value)
        size = New cv.Size(CInt(wSlider.value), CInt(hSlider.value))
    End Sub
End Class






Public Class Options_MotionDetect : Inherits OptionParent
    Public threadData As cv.Vec3i = New cv.Vec3i(0, 0, 0)
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
    Public Sub Run()
        Dim w = task.cols
        Dim h = task.rows
        Static radioChoices() As cv.Vec3i = {New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2),
                    New cv.Vec3i(8, w / 4, h / 2), New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4),
                    New cv.Vec3i(32, w / 8, h / 8), New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2),
                    New cv.Vec3i(8, w / 4, h / 2), New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4),
                    New cv.Vec3i(32, w / 8, h / 8)}

        Static correlationSlider = OptionParent.FindSlider("Correlation Threshold")
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static padSlider = OptionParent.FindSlider("Pad size in pixels for the search area")
        Static stdevSlider = OptionParent.FindSlider("Stdev threshold for using correlation")
        CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        threadData = radioChoices(findRadioIndex(frm.check))
        pad = padSlider.Value
        stdevThreshold = stdevSlider.Value
    End Sub
End Class





Public Class Options_JpegQuality : Inherits OptionParent
    Public quality As Integer = 90
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("JPEG Quality", 1, 100, quality)
    End Sub
    Public Sub Run()
        Static qualitySlider = OptionParent.FindSlider("JPEG Quality")
        quality = qualitySlider.value
    End Sub
End Class




Public Class Options_PNGCompression : Inherits OptionParent
    Public compression As Integer = 90
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("PNG Compression", 1, 100, compression)
    End Sub
    Public Sub Run()
        Static compressionSlider = OptionParent.FindSlider("PNG Compression")
        compression = compressionSlider.value
    End Sub
End Class





Public Class Options_Binarize : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then binarizeLabel = radio.check(i).Text
        Next
    End Sub
End Class






Public Class Options_BlurTopo : Inherits OptionParent
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
    Public Sub Run()
        Static reductionSlider = OptionParent.FindSlider("Blur Color Reduction")
        Static frameSlider = OptionParent.FindSlider("Frame Count Cycle")
        Static percentSlider = OptionParent.FindSlider("Percent of Blurring")

        If task.optionsChanged Then
            savePercent = percentSlider.Value
            nextPercent = savePercent
        End If

        frameCycle = frameSlider.value
        reduction = reductionSlider.value / 100
        kernelSize = CInt(nextPercent / 100 * task.cols) Or 1
    End Sub
End Class






Public Class Options_BoundaryRect : Inherits OptionParent
    Public percentRect As Double = 25
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired percent of rectangles", 0, 100, percentRect)
    End Sub
    Public Sub Run()
        Static percentSlider = OptionParent.FindSlider("Desired percent of rectangles")
        percentRect = percentSlider.value / 100
    End Sub
End Class








Public Class Options_BrightnessContrast : Inherits OptionParent
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
#If AZURE_SUPPORT Then
        If task.cameraName = "Azure Kinect 4K" Then
            alphaDefault = 600
            betaDefault = 0
        End If
#End If
        If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then alphaDefault = 1500
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Alpha (contrast)", 0, 10000, alphaDefault)
            sliders.setupTrackBar("Beta (brightness)", -127, 127, betaDefault)
            sliders.setupTrackBar("HSV Brightness Value", 0, 150, hsvBrightness)
            sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, exponent * 100)
        End If
    End Sub
    Public Sub Run()
        Static betaSlider = OptionParent.FindSlider("Beta (brightness)")
        Static alphaSlider = OptionParent.FindSlider("Alpha (contrast)")
        brightness = alphaSlider.value / 500
        contrast = betaSlider.value
        Static brightnessSlider = OptionParent.FindSlider("HSV Brightness Value")
        hsvBrightness = brightnessSlider.Value / 100
        Static exponentSlider = OptionParent.FindSlider("Contrast exponent to use X100")
        exponent = exponentSlider.value / 100
    End Sub
End Class






Public Class Options_HistPointCloud : Inherits OptionParent
    Public threshold As Integer = 60
    Public xBins As Integer = 30
    Public yBins As Integer = 30
    Public zBins As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram threshold", 0, 1000, threshold)
            sliders.setupTrackBar("Histogram X bins", 1, task.cols, xBins)
            sliders.setupTrackBar("Histogram Y bins", 1, task.rows, yBins)
            sliders.setupTrackBar("Histogram Z bins", 1, 200, zBins)
        End If

        Select Case task.cols
            Case 640
                OptionParent.FindSlider("Histogram threshold").Value = 200
            Case 320
                OptionParent.FindSlider("Histogram threshold").Value = threshold
            Case 160
                OptionParent.FindSlider("Histogram threshold").Value = 25
        End Select
    End Sub
    Public Sub Run()
        Static xSlider = OptionParent.FindSlider("Histogram X bins")
        Static ySlider = OptionParent.FindSlider("Histogram Y bins")
        Static zSlider = OptionParent.FindSlider("Histogram Z bins")
        Static tSlider = OptionParent.FindSlider("Histogram threshold")
        xBins = xSlider.Value
        yBins = ySlider.Value
        zBins = zSlider.Value
        threshold = tSlider.value
    End Sub
End Class







Public Class Options_Harris : Inherits OptionParent
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
    Public Sub Run()
        Static thresholdslider = OptionParent.FindSlider("Harris Threshold")
        Static neighborSlider = OptionParent.FindSlider("Harris Neighborhood")
        Static apertureSlider = OptionParent.FindSlider("Harris aperture")
        Static parmSlider = OptionParent.FindSlider("Harris Parameter")

        threshold = thresholdslider.Value / 10000
        neighborhood = neighborSlider.Value Or 1
        aperture = apertureSlider.Value Or 1
        harrisParm = parmSlider.Value / 100
    End Sub
End Class






Public Class Options_HarrisCorners : Inherits OptionParent
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
    Public Sub Run()
        Static blockSlider = OptionParent.FindSlider("Corner block size")
        Static apertureSlider = OptionParent.FindSlider("Corner aperture size")
        Static qualitySlider = OptionParent.FindSlider("Corner quality level")
        quality = qualitySlider.Value
        aperture = apertureSlider.value Or 1
        blockSize = blockSlider.value Or 1
    End Sub
End Class






Public Class Options_Databases : Inherits OptionParent
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
    Public Sub Run()
        Static downloadAllCheck = findRadio("Download All")
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







Public Class Options_EdgeMatching : Inherits OptionParent '
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
    Public Sub Run()
        Static overlayCheck = FindCheckBox("Overlay thread grid")
        Static highlightCheck = FindCheckBox("Highlight all grid entries above threshold")
        Static clearCheck = FindCheckBox("Clear selected highlights (if Highlight all grid entries is unchecked)")
        Static thresholdSlider = OptionParent.FindSlider("Edge Correlation threshold X100")
        Static searchSlider = OptionParent.FindSlider("Search depth in pixels")
        threshold = thresholdSlider.Value / 100
        searchDepth = searchSlider.Value
        overlayChecked = overlayCheck.checked
        highlightChecked = highlightCheck.checked
        clearChecked = clearCheck.checked
    End Sub
End Class






Public Class Options_EmaxInputClusters : Inherits OptionParent
    Public samplesPerRegion As Integer = 10
    Public sigma As Integer = 10
    Public emaxCellSize As Integer = CInt(task.workRes.Width / 3)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("EMax Number of Samples per region", 1, 20, samplesPerRegion)
            sliders.setupTrackBar("EMax Sigma (spread)", 1, 100, sigma)
            sliders.setupTrackBar("EMax Cell Size", 1, task.workRes.Width, emaxCellSize)
        End If
    End Sub
    Public Sub Run()
        Static sampleSlider = OptionParent.FindSlider("EMax Number of Samples per region")
        Static sigmaSlider = OptionParent.FindSlider("EMax Sigma (spread)")
        Static sizeSlider = OptionParent.FindSlider("EMax Cell Size")
        samplesPerRegion = sampleSlider.value
        sigma = sigmaSlider.value
        emaxCellSize = sizeSlider.value
    End Sub
End Class





Public Class Options_CComp : Inherits OptionParent
    Public light As Integer = 127
    Public dark As Integer = 50
    Public threshold As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold for lighter input", 1, 255, light)
            sliders.setupTrackBar("Threshold for darker input", 1, 255, dark)
            sliders.setupTrackBar("CComp threshold", 0, 255, 50)
        End If
    End Sub
    Public Sub Run()
        Static lightSlider = OptionParent.FindSlider("Threshold for lighter input")
        Static darkSlider = OptionParent.FindSlider("Threshold for darker input")
        Static thresholdSlider = OptionParent.FindSlider("CComp threshold")
        threshold = thresholdSlider.value
        light = lightSlider.value
        dark = darkSlider.value
    End Sub
End Class






Public Class Options_CellAutomata : Inherits OptionParent
    Public currentRule As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Current Rule", 0, 255, currentRule)
    End Sub
    Public Sub Run()
        Static ruleSlider = OptionParent.FindSlider("Current Rule")
        currentRule = ruleSlider.value
    End Sub
End Class






Public Class Options_BackProject2D : Inherits OptionParent
    Public backProjectRow As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("BackProject Row")
            radio.addRadio("BackProject Col")
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static rowRadio = findRadio("BackProject Row")
        If task.mouseClickFlag Then rowRadio.checked = Not rowRadio.checked
        backProjectRow = rowRadio.checked
    End Sub
End Class





Public Class Options_Kaze : Inherits OptionParent
    Public pointsToMatch As Integer = 100
    Public maxDistance As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of points to match", 1, 300, pointsToMatch)
            sliders.setupTrackBar("When matching, max possible distance", 1, 200, maxDistance)
        End If
    End Sub
    Public Sub Run()
        Static maxSlider = OptionParent.FindSlider("Max number of points to match")
        Static distSlider = OptionParent.FindSlider("When matching, max possible distance")
        pointsToMatch = maxSlider.value
        maxDistance = distSlider.value
    End Sub
End Class





Public Class Options_Blob : Inherits OptionParent
    Dim blob As New Blob_Input
    Public blobParams As cv.SimpleBlobDetector.Params = New cv.SimpleBlobDetector.Params
    Public Sub New()
        blob.updateFrequency = 30

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
    Public Sub Run()
        Static minSlider = OptionParent.FindSlider("min Threshold")
        Static maxSlider = OptionParent.FindSlider("max Threshold")
        Static stepSlider = OptionParent.FindSlider("Threshold Step")
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






Public Class Options_SLR : Inherits OptionParent
    Public tolerance As Double = 0.3
    Public halfLength As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Approximate accuracy (tolerance) X100", 1, 1000, tolerance * 100)
            sliders.setupTrackBar("Simple moving average window size", 1, 100, halfLength)
        End If
    End Sub
    Public Sub Run()
        Static toleranceSlider = OptionParent.FindSlider("Approximate accuracy (tolerance) X100")
        Static movingAvgSlider = OptionParent.FindSlider("Simple moving average window size")
        tolerance = toleranceSlider.Value / 100
        halfLength = movingAvgSlider.Value
    End Sub
End Class




Public Class Options_KNN : Inherits OptionParent
    Public knnDimension As Integer = 2
    Public numPoints As Integer = 10
    Public multiplier As Integer = 10
    Public topXDistances As Integer = 20
    Public useOutSide As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("KNN Dimension", 2, 20, knnDimension)
            sliders.setupTrackBar("Random input points", 5, 100, numPoints)
            sliders.setupTrackBar("Average distance multiplier", 1, 20, multiplier)
            sliders.setupTrackBar("Top X distances", 1, 100, topXDistances)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Display queries")
            check.addCheckBox("Display training input and connecting line")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static dimSlider = OptionParent.FindSlider("KNN Dimension")
        Static randomSlider = OptionParent.FindSlider("Random input points")
        Static distSlider = OptionParent.FindSlider("Random input points")
        Static topXSlider = OptionParent.FindSlider("Top X distances")
        Static inOutCheck = FindCheckBox("Use 'Outside' feature points (unchecked use 'Inside'")
        knnDimension = dimSlider.Value
        numPoints = randomSlider.Value
        multiplier = distSlider.value
        topXDistances = topXSlider.value
    End Sub
End Class






Public Class Options_Clone : Inherits OptionParent
    Public alpha As Double = 0.2
    Public beta As Double = 0.2
    Public lowThreshold As Integer = 10
    Public highThreshold As Integer = 50
    Public blueChange As Double = 0.5
    Public greenChange As Double = 0.5
    Public redChange As Double = 1.5
    Public cloneFlag As cv.SeamlessCloneMethods = cv.SeamlessCloneMethods.MixedClone
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
    Public Sub Run()
        Static alphaSlider = OptionParent.FindSlider("Alpha")
        Static betaSlider = OptionParent.FindSlider("Beta")
        Static lowSlider = OptionParent.FindSlider("Low Threshold")
        Static highSlider = OptionParent.FindSlider("High Threshold")
        Static redSlider = OptionParent.FindSlider("Color Change - Red")
        Static greenSlider = OptionParent.FindSlider("Color Change - Green")
        Static blueSlider = OptionParent.FindSlider("Color Change - Blue")
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






Public Class Options_Coherence : Inherits OptionParent
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
    Public Sub Run()
        Static sigmaSlider = OptionParent.FindSlider("Coherence Sigma")
        Static blendSlider = OptionParent.FindSlider("Coherence Blend")
        Static strSlider = OptionParent.FindSlider("Coherence str_sigma")
        Static eigenSlider = OptionParent.FindSlider("Coherence eigen kernel")

        sigma = sigmaSlider.Value * 2 + 1
        blend = blendSlider.Value / 10
        str_sigma = strSlider.Value * 2 + 1
        eigenkernelsize = eigenSlider.Value * 2 + 1
    End Sub
End Class





Public Class Options_Color : Inherits OptionParent
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
    Public Sub Run()
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colorFormat = radio.check(i).Text
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Grayscale8U : Inherits OptionParent
    Public useOpenCV As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use OpenCV to create grayscale image")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static grayCheck = FindCheckBox("Use OpenCV to create grayscale image")
        useOpenCV = grayCheck.checked
    End Sub
End Class





Public Class Options_Color8UTopX : Inherits OptionParent
    Public topXcount As Integer = 16
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Top X pixels", 2, 32, topXcount)
    End Sub
    Public Sub Run()
        Static topXSlider = OptionParent.FindSlider("Top X pixels")
        topXcount = topXSlider.value
    End Sub
End Class







Public Class Options_Morphology : Inherits OptionParent
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
    Public Sub Run()
        Static morphSlider = OptionParent.FindSlider("Morphology width/height")
        Static morphExSlider = OptionParent.FindSlider("MorphologyEx iterations")
        Static scaleSlider = OptionParent.FindSlider("MorphologyEx Scale factor X1000")
        widthHeight = morphSlider.value
        iterations = morphExSlider.value
        scaleFactor = 1 + scaleSlider.value / 1000
    End Sub
End Class





Public Class Options_Convex : Inherits OptionParent
    Public hullCount As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Hull random points", 4, 20, hullCount)
    End Sub
    Public Sub Run()
        Static hullSlider = OptionParent.FindSlider("Hull random points")
        hullCount = hullSlider.Value
    End Sub
End Class



Public Class Options_PreCorners : Inherits OptionParent
    Public kernelSize As Integer = 19
    Public subpixSize As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("kernel Size", 1, 20, kernelSize)
            sliders.setupTrackBar("SubPix kernel Size", 1, 20, subpixSize)
        End If
    End Sub
    Public Sub Run()
        Static kernelSlider = OptionParent.FindSlider("kernel Size")
        Static subpixSlider = OptionParent.FindSlider("SubPix kernel Size")
        kernelSize = kernelSlider.value Or 1
        subpixSize = subpixSlider.value Or 1
    End Sub
End Class





Public Class Options_ShiTomasi : Inherits OptionParent
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
    Public Sub Run()
        Static typeRadio = findRadio("Shi-Tomasi features")
        Static blockSlider = OptionParent.FindSlider("Corner block size")
        Static apertureSlider = OptionParent.FindSlider("Corner aperture size")
        Static thresholdSlider = OptionParent.FindSlider("Corner normalize threshold")
        threshold = thresholdSlider.Value
        useShiTomasi = typeRadio.checked
        aperture = apertureSlider.value Or 1
        blocksize = blockSlider.value Or 1
    End Sub
End Class




Public Class Options_FlatLand : Inherits OptionParent
    Public reductionFactor As Double = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Region Count", 1, 250, reductionFactor)
    End Sub
    Public Sub Run()
        Static regionSlider = OptionParent.FindSlider("Region Count")
        reductionFactor = regionSlider.Maximum - regionSlider.Value
    End Sub
End Class






Public Class Options_Depth : Inherits OptionParent
    Public millimeters As Integer = 8
    Public mmThreshold As Double = millimeters / 1000
    Public threshold As Double = 250
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold in millimeters", 0, 1000, mmThreshold * 1000)
            sliders.setupTrackBar("Threshold for punch", 0, 255, threshold)
        End If
    End Sub
    Public Sub Run()
        Static mmSlider = OptionParent.FindSlider("Threshold in millimeters")
        Static thresholdSlider = OptionParent.FindSlider("Threshold for punch")
        millimeters = mmSlider.value
        mmThreshold = mmSlider.Value / 1000.0
        threshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_DepthHoles : Inherits OptionParent
    Public borderDilation As Integer = 1
    Public holeDilation As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Amount of dilation of borderMask", 1, 10, borderDilation)
            sliders.setupTrackBar("Amount of dilation of holeMask", 0, 10, holeDilation)
        End If
    End Sub
    Public Sub Run()
        Static borderSlider = OptionParent.FindSlider("Amount of dilation of borderMask")
        Static holeSlider = OptionParent.FindSlider("Amount of dilation of holeMask")
        borderDilation = borderSlider.value
        holeDilation = holeSlider.value
    End Sub
End Class






Public Class Options_Uncertainty : Inherits OptionParent
    Public uncertaintyThreshold As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Uncertainty threshold", 1, 255, uncertaintyThreshold)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Uncertainty threshold")
        uncertaintyThreshold = thresholdSlider.value
    End Sub
End Class







Public Class Options_DepthColor : Inherits OptionParent
    Public alpha As Double = 0.05
    Public beta As Double = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Depth ColorMap Alpha X100", 1, 100, alpha * 100)
            sliders.setupTrackBar("Depth ColorMap Beta", 1, 100, beta)
        End If
    End Sub
    Public Sub Run()
        Static alphaSlider = OptionParent.FindSlider("Depth ColorMap Alpha X100")
        Static betaSlider = OptionParent.FindSlider("Depth ColorMap Beta")
        alpha = alphaSlider.value / 100
        beta = betaSlider.value
    End Sub
End Class






Public Class Options_DNN : Inherits OptionParent
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
    End Sub
    Public Sub Run()
        Static scaleSlider = OptionParent.FindSlider("DNN Scale Factor")
        Static meanSlider = OptionParent.FindSlider("DNN MeanVal")
        Static confidenceSlider = OptionParent.FindSlider("DNN Confidence Threshold")
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
                MessageBox.Show("The " + radio.check(index).Text + " super res model file is missing!")
                superResModelFileName = ""
            End If
        End If
    End Sub
End Class





Public Class Options_DrawNoise : Inherits OptionParent
    Public noiseCount As Integer = 100
    Public noiseWidth As Integer = 3
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Noise Count", 1, 1000, noiseCount)
            sliders.setupTrackBar("Noise Width", 1, 10, noiseWidth)
        End If
    End Sub
    Public Sub Run()
        Static widthSlider = OptionParent.FindSlider("Noise Width")
        Static CountSlider = OptionParent.FindSlider("Noise Count")
        noiseCount = CountSlider.Value
        noiseWidth = widthSlider.Value
    End Sub
End Class






Public Class Options_Edges : Inherits OptionParent
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
    Public Sub Run()
        Static sigmaSSlider = OptionParent.FindSlider("Edge-preserving Sigma_s")
        Static sigmaRSlider = OptionParent.FindSlider("Edge-preserving Sigma_r")
        Static recurseRadio = findRadio("Edge-preserving RecurseFilter")
        EP_Sigma_s = sigmaSSlider.Value
        EP_Sigma_r = sigmaRSlider.Value / sigmaRSlider.Maximum
        recurseCheck = recurseRadio.checked
    End Sub
End Class






Public Class Options_Edges2 : Inherits OptionParent
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
    Public Sub Run()
        Static freqSlider = OptionParent.FindSlider("Remove Frequencies < x")
        Static thresholdSlider = OptionParent.FindSlider("Threshold after Removal")
        Static rfSlider = OptionParent.FindSlider("Edges RF Threshold")

        edgeRFthreshold = rfSlider.value
        removeFrequencies = freqSlider.value
        dctThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_Edges3 : Inherits OptionParent
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
            sliders.setupTrackBar("Input pixel difference", 0, 50, If(task.cols = 640, gapdiff, 20))
        End If
    End Sub
    Public Sub Run()
        Static alphaSlider = OptionParent.FindSlider("Deriche Alpha X100")
        Static omegaSlider = OptionParent.FindSlider("Deriche Omega X1000")
        Static thresholdSlider = OptionParent.FindSlider("Output filter threshold")
        alpha = alphaSlider.value / 100
        omega = omegaSlider.value / 100
        threshold = thresholdSlider.value

        Static distanceSlider = OptionParent.FindSlider("Input pixel distance")
        Static diffSlider = OptionParent.FindSlider("Input pixel difference")
        gapDistance = distanceSlider.Value And 254
        gapdiff = diffSlider.Value
    End Sub
End Class







Public Class Options_Edges4 : Inherits OptionParent
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
    Public Sub Run()
        Static hCheck = FindCheckBox("Horizontal Edges")
        Static vCheck = FindCheckBox("Vertical Edges")
        Static vertSlider = OptionParent.FindSlider("Border Vertical in Pixels")
        Static horizSlider = OptionParent.FindSlider("Border Horizontal in Pixels")
        vertPixels = vertSlider.value
        horizPixels = horizSlider.value
        horizonCheck = hCheck.checked
        verticalCheck = vCheck.checked
    End Sub
End Class








Public Class Options_Erode : Inherits OptionParent
    Public kernelSize As Integer = 3
    Public iterations As Integer = 1
    Public morphShape As cv.MorphShapes = cv.MorphShapes.Cross
    Public element As cv.Mat
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
    Public Sub Run()
        Static ellipseRadio = findRadio("Erode shape: Ellipse")
        Static rectRadio = findRadio("Erode shape: Rect")
        Static iterSlider = OptionParent.FindSlider("Erode Iterations")
        Static kernelSlider = OptionParent.FindSlider("Erode Kernel Size")
        Static noShapeRadio = findRadio("Erode shape: None")
        Static depthSlider = OptionParent.FindSlider("DepthSeed flat depth X1000")
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





Public Class Options_Etch_ASketch : Inherits OptionParent
    Public demoMode As Boolean = False
    Public cleanMode As Boolean = False
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Etch_ASketch clean slate")
            check.addCheckBox("Demo mode")
            check.Box(1).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static cleanCheck = FindCheckBox("Etch_ASketch clean slate")
        Static demoCheck = FindCheckBox("Demo mode")
        demoMode = demoCheck.checked

        If cleanMode Then cleanCheck.checked = False ' only on for one frame.
        cleanMode = cleanCheck.checked
    End Sub
End Class





Public Class Options_LineFinder : Inherits OptionParent
    Public kernelSize As Integer = 5
    Public tolerance As Integer = 5
    Public kSize As Integer = kernelSize - 1
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area kernel size for depth", 1, 10, kernelSize)
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, tolerance)
        End If
    End Sub
    Public Sub Run()
        Static angleSlider = OptionParent.FindSlider("Angle tolerance in degrees")
        Static kernelSlider = OptionParent.FindSlider("Area kernel size for depth")
        kernelSize = kernelSlider.value * 2 - 1
        tolerance = angleSlider.value
        kSize = kernelSlider.Value - 1
    End Sub
End Class






Public Class Options_PCA_NColor : Inherits OptionParent
    Public desiredNcolors As Integer = 8
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired number of colors", 1, 256, desiredNcolors)
    End Sub
    Public Sub Run()
        Static nSlider = OptionParent.FindSlider("Desired number of colors")
        desiredNcolors = nSlider.value
    End Sub
End Class





Public Class Options_FPolyCore : Inherits OptionParent
    Public maxShift As Integer = 50
    Public resyncThreshold As Integer = 4
    Public anchorMovement As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Maximum shift to trigger resync", 1, 100, maxShift)
            sliders.setupTrackBar("Anchor point max movement", 1, 10, anchorMovement)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
        Static shiftSlider = OptionParent.FindSlider("Maximum shift to trigger resync")
        Static anchorSlider = OptionParent.FindSlider("Anchor point max movement")
        maxShift = shiftSlider.Value
        resyncThreshold = thresholdSlider.Value
        anchorMovement = anchorSlider.value
    End Sub
End Class





Public Class Options_FLANN : Inherits OptionParent
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
    Public Sub Run()
        Static reuseCheck = FindCheckBox("Reuse the same feature list (test different search parameters)")
        Static sortedCheck = FindCheckBox("Search params sorted")
        Static matchSlider = OptionParent.FindSlider("Match count")
        Static querySlider = OptionParent.FindSlider("Query count")
        Static searchSlider = OptionParent.FindSlider("Search check count")
        Static epsSlider = OptionParent.FindSlider("EPS X100")
        reuseData = reuseCheck.checked
        matchCount = matchSlider.value
        queryCount = querySlider.Value
        searchCheck = searchSlider.Value
        eps = epsSlider.Value / 100
        sorted = sortedCheck.checked
    End Sub
End Class








Public Class Options_TrackerDepth : Inherits OptionParent
    Public displayRect As Boolean = True
    Public minRectSize As Integer = 10000
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Display centroid and rectangle for each region")
            check.Box(0).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for rectangle size", 50, 50000, minRectSize)
    End Sub
    Public Sub Run()
        Static displayCheck = FindCheckBox("Display centroid and rectangle for each region")
        Static minRectSizeSlider = OptionParent.FindSlider("Threshold for rectangle size")

        displayRect = displayCheck.checked
        minRectSize = minRectSizeSlider.value
    End Sub
End Class




Public Class Options_Gabor : Inherits OptionParent
    Public gKernel As cv.Mat
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
    Public Sub Run()
        Static ksizeSlider = OptionParent.FindSlider("Gabor Kernel Size")
        Static sigmaSlider = OptionParent.FindSlider("Gabor Sigma")
        Static lambdaSlider = OptionParent.FindSlider("Gabor lambda")
        Static gammaSlider = OptionParent.FindSlider("Gabor gamma X10")
        Static phaseSlider = OptionParent.FindSlider("Gabor Phase offset X100")
        Static thetaSlider = OptionParent.FindSlider("Gabor Theta (degrees)")
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






Public Class Options_GrabCut : Inherits OptionParent
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
    Public Sub Run()
        Static fgFineTuning = findRadio("Selected rectangle is added to the foreground")
        Static clearCheck = findRadio("Clear all foreground and background fine tuning")
        Static saveRadio = fgFineTuning.checked
    End Sub
End Class






Public Class Options_Gradient : Inherits OptionParent
    Public exponent As Double = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, exponent)
        End If
    End Sub
    Public Sub Run()
        Static contrastSlider = OptionParent.FindSlider("Contrast exponent to use X100")
        exponent = contrastSlider.Value / 100
    End Sub
End Class







Public Class Options_Histogram : Inherits OptionParent
    Public minGray As Integer = 50
    Public maxGray As Integer = 200
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Gray", 0, 255, minGray)
            sliders.setupTrackBar("Max Gray", 0, 255, maxGray)
        End If
    End Sub
    Public Sub Run()
        Static minSlider = OptionParent.FindSlider("Min Gray")
        Static maxSlider = OptionParent.FindSlider("Max Gray")
        If minSlider.Value >= maxSlider.Value Then minSlider.Value = maxSlider.Value - Math.Min(10, maxSlider.Value - 1)
        If minSlider.Value = maxSlider.Value Then maxSlider.Value += 1
        minGray = minSlider.value
        maxGray = maxSlider.value
    End Sub
End Class






Public Class Options_Guess : Inherits OptionParent
    Public MaxDistance As Integer = 50
    Public Sub New()
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Max Distance from edge (pixels)", 0, 100, MaxDistance)
        End If
    End Sub
    Public Sub Run()
        Static distSlider = OptionParent.FindSlider("Max Distance from edge (pixels)")
        MaxDistance = distSlider.value
    End Sub
End Class





Public Class Options_HOG : Inherits OptionParent
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
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("HOG Threshold")
        Static strideSlider = OptionParent.FindSlider("HOG Stride")
        Static scaleSlider = OptionParent.FindSlider("HOG Scale X1000")

        thresholdHOG = thresholdSlider.Value
        strideHOG = CInt(strideSlider.Value)
        scaleHOG = scaleSlider.Value / 1000
    End Sub
End Class






Public Class Options_Images : Inherits OptionParent
    Public fileNameForm As New OptionsFileName
    Public fileIndex As Integer = 0
    Public fileNameList As New List(Of String)
    Public fileInputName As FileInfo
    Public dirName As String = ""
    Public imageSeries As Boolean = False
    Public fullsizeImage As cv.Mat
    Public Sub New()
        fileNameForm = New OptionsFileName
        dirName = task.HomeDir + "Images/train"
        fileNameForm.OpenFileDialog1.InitialDirectory = dirName
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("Opencv", "Image_Basics_Name", "Image_Basics_Name", task.HomeDir + "Images/train/2092.jpg")
        fileNameForm.Text = "Select an image file for use in Opencv"
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
    Public Sub Run()
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






Public Class Options_VerticalVerify : Inherits OptionParent
    Public angleThreshold As Integer = 80
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum Arc-Y threshold angle (degrees)", 70, 90, angleThreshold)
        End If
    End Sub
    Public Sub Run()
        Static arcYslider = OptionParent.FindSlider("Minimum Arc-Y threshold angle (degrees)")
        angleThreshold = arcYslider.Value
    End Sub
End Class







Public Class Options_IMUPlot : Inherits OptionParent
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
    Public Sub Run()
        Static blueCheck = FindCheckBox("Blue Variable")
        Static greenCheck = FindCheckBox("Green Variable")
        Static redCheck = FindCheckBox("Red Variable")
        setBlue = blueCheck.checked
        setGreen = greenCheck.checked
        setRed = redCheck.checked
    End Sub
End Class





Public Class Options_Kalman_VB : Inherits OptionParent
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
    Public Sub Run()
        Static matrix As New List(Of Double)
        Static inputSlider = OptionParent.FindSlider("Move this to see results")
        Static noisyInputSlider = OptionParent.FindSlider("Input with Noise")
        Static pointSlider = OptionParent.FindSlider("20 point average of output")
        Static avgSlider = OptionParent.FindSlider("20 Point average difference")
        Static noiseSlider = OptionParent.FindSlider("Simulated Noise")
        Static biasSlider = OptionParent.FindSlider("Simulated Bias")
        Static scaleSlider = OptionParent.FindSlider("Simulated Scale")
        Static outputSlider = OptionParent.FindSlider("Kalman Output")
        Static kDiffSlider = OptionParent.FindSlider("Kalman difference")

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
            Dim AverageOutput = (cv.Mat.FromPixelData(MAX_INPUT, 1, cv.MatType.CV_32F, matrix.ToArray)).Mean()(0)

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






Public Class Options_Kalman : Inherits OptionParent
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
    Public Sub Run()
        Static deltaSlider = OptionParent.FindSlider("Delta Time X100")
        Static covarSlider = OptionParent.FindSlider("Process Covariance X10000")
        Static pDotSlider = OptionParent.FindSlider("pDot entry X1000")
        Static avgSlider = OptionParent.FindSlider("Average input count")
        delta = deltaSlider.Value / 100
        pdotEntry = pDotSlider.Value / 1000
        processCovar = covarSlider.Value / 10000
        averageInputCount = avgSlider.value
    End Sub
End Class






Public Class Options_LaneFinder : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        inputName = findRadioText(frm.check)
    End Sub
End Class








Public Class Options_LaPlacianPyramid : Inherits OptionParent
    Public img As New cv.Mat
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
    Public Sub Run()
        Dim barCount = sliders.mytrackbars.Count

        ' this usage of sliders.mytrackbars(x) is OK as long as this algorithm is not reused in multiple places (which it isn't)
        Dim levelMat(barCount - 1) As cv.Mat
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






Public Class Options_LeftRight : Inherits OptionParent
    Public sliceY As Integer = 25
    Public sliceHeight As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Slice Starting Y", 0, 300, sliceY)
            sliders.setupTrackBar("Slice Height", 1, (task.rows - 10) / 2, sliceHeight)
        End If
    End Sub
    Public Sub Run()
        Static startYSlider = OptionParent.FindSlider("Slice Starting Y")
        Static hSlider = OptionParent.FindSlider("Slice Height")
        sliceY = startYSlider.Value
        sliceHeight = hSlider.Value
    End Sub
End Class






Public Class Options_LUT_Create : Inherits OptionParent
    Public lutThreshold As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("LUT entry diff threshold", 1, 100, lutThreshold)
    End Sub
    Public Sub Run()
        Static diffSlider = OptionParent.FindSlider("LUT entry diff threshold")
        lutThreshold = diffSlider.value
    End Sub
End Class






Public Class Options_Mat : Inherits OptionParent
    Public decompType As cv.DecompTypes = cv.DecompTypes.Cholesky
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                decompType = Choose(i + 1, cv.DecompTypes.Cholesky, cv.DecompTypes.Eig, cv.DecompTypes.LU, cv.DecompTypes.Normal,
                                        cv.DecompTypes.QR, cv.DecompTypes.SVD)
                Exit For
            End If
        Next
    End Sub
End Class





Public Class Options_Match : Inherits OptionParent
    Public maxDistance As Integer = 5
    Public stdevThreshold As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, maxDistance)
            sliders.setupTrackBar("Stdev Threshold", 0, 100, stdevThreshold)
        End If
    End Sub
    Public Sub Run()
        Static distSlider = OptionParent.FindSlider("Maximum travel distance per frame")
        Static stdevSlider = OptionParent.FindSlider("Stdev Threshold")
        stdevThreshold = CSng(stdevSlider.Value)
        maxDistance = distSlider.Value
    End Sub
End Class







Public Class Options_Math : Inherits OptionParent
    Public showMean As Boolean = False
    Public showStdev As Boolean = False
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Show mean")
            check.addCheckBox("Show Stdev")
        End If
    End Sub
    Public Sub Run()
        Static meanCheck = FindCheckBox("Show mean")
        Static stdevCheck = FindCheckBox("Show Stdev")
        showMean = meanCheck.checked
        showStdev = stdevCheck.checked
    End Sub
End Class






Public Class Options_MeanSubtraction : Inherits OptionParent
    Public scaleVal As Double = 16
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Scaling Factor X100", 1, 255, scaleVal)
    End Sub
    Public Sub Run()
        Static scaleSlider = OptionParent.FindSlider("Scaling Factor X100")
        scaleVal = scaleSlider.Value
    End Sub
End Class






Public Class Options_Mesh : Inherits OptionParent
    Public nabeCount As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of nearest neighbors", 1, 10, nabeCount)
    End Sub
    Public Sub Run()
        Static nabeSlider = OptionParent.FindSlider("Number of nearest neighbors")
        nabeCount = nabeSlider.value
    End Sub
End Class






Public Class Options_OEX : Inherits OptionParent
    Public lows As cv.Scalar = New cv.Scalar(90, 50, 50)
    Public highs As cv.Scalar = New cv.Scalar(180, 150, 150)
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
    Public Sub Run()
        Static hueLowSlider = OptionParent.FindSlider("Hue low")
        Static hueHighSlider = OptionParent.FindSlider("Hue high")
        Static satLowSlider = OptionParent.FindSlider("Saturation low")
        Static satHighSlider = OptionParent.FindSlider("Saturation high")
        Static valLowSlider = OptionParent.FindSlider("Value low")
        Static valHighSlider = OptionParent.FindSlider("Value high")
        lows = New cv.Scalar(hueLowSlider.value, satLowSlider.value, valLowSlider.value)
        highs = New cv.Scalar(hueHighSlider.value, satHighSlider.value, valHighSlider.value)
    End Sub
End Class





Public Class Options_ORB : Inherits OptionParent
    Public desiredCount As Integer = 100
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("ORB - desired point count", 10, 2000, desiredCount)
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("ORB - desired point count")
        desiredCount = countSlider.value
    End Sub
End Class






Public Class Options_Palette : Inherits OptionParent
    Public transitions As Integer = 7
    Public convertScale As Integer = 45
    Public schemeName As String = "schemeRandom"
    Public radius As Integer = 0
    Public schemes() As FileInfo
    Public Sub New()
        radius = task.cols / 2
        If (sliders.Setup(traceName)) Then
            sliders.setupTrackBar("Color transitions", 1, 255, transitions)
            sliders.setupTrackBar("Convert And Scale", 0, 100, convertScale)
            sliders.setupTrackBar("LinearPolar radius", 0, task.cols, radius)
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
    Public Sub Run()
        Static paletteSlider = OptionParent.FindSlider("Color transitions")
        Static cvtScaleSlider = OptionParent.FindSlider("Convert And Scale")
        Static radiusSlider = OptionParent.FindSlider("LinearPolar radius")
        transitions = paletteSlider.value
        Static frm = FindFrm(traceName + " Radio Buttons")
        schemeName = schemes(findRadioIndex(frm.check)).FullName
        convertScale = cvtScaleSlider.value
        radius = radiusSlider.Value
    End Sub
End Class






Public Class Options_PCA : Inherits OptionParent
    Public retainedVariance As Double = 0.95
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Retained Variance X100", 1, 100, retainedVariance * 100)
    End Sub
    Public Sub Run()
        Static retainSlider = OptionParent.FindSlider("Retained Variance X100")
        retainedVariance = retainSlider.value / 100
    End Sub
End Class





Public Class Options_Pendulum : Inherits OptionParent
    Public initialize As Boolean = False
    Public fps As Integer = 300
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Reset initial conditions")
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Pendulum FPS", 10, 1000, fps)
    End Sub
    Public Sub Run()
        Static initCheck = FindCheckBox("Reset initial conditions")
        Static timeSlider = OptionParent.FindSlider("Pendulum FPS")
        If initCheck.checked Then initCheck.checked = False
        If task.firstPass Then check.Box(0).Checked = True
        fps = timeSlider.value
    End Sub
End Class






Public Class Options_PhaseCorrelate : Inherits OptionParent
    Public shiftThreshold As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold shift to cause reset of lastFrame", 0, 100, shiftThreshold)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Threshold shift to cause reset of lastFrame")
        shiftThreshold = thresholdSlider.value
    End Sub
End Class





Public Class Options_PlaneFloor : Inherits OptionParent
    Public countThreshold As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Pixel Count threshold that indicates floor", 1, 100, countThreshold)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Pixel Count threshold that indicates floor")
        countThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_PlyFormat : Inherits OptionParent
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
        fileNameForm.filename.Text = GetSetting("Opencv", "plyFileName", "plyFileName", task.HomeDir + "temp\pointcloud.ply")

        fileNameForm.Text = "Select ply output file"
        fileNameForm.FileNameLabel.Text = "Select ply output file"
        fileNameForm.PlayButton.Text = "Save"
        fileNameForm.TrackBar1.Visible = False
        fileNameForm.Setup(traceName)
        fileNameForm.Show()
    End Sub
    Public Sub Run()
        If task.firstPass Then fileNameForm.Left = allOptions.Width / 3
        playButton = fileNameForm.PlayButton.Text
        fileName = fileNameForm.filename.Text

        Dim testDir = New FileInfo(fileNameForm.filename.Text)
        If testDir.Directory.Exists = False Then
            fileNameForm.filename.Text = task.HomeDir + "Temp\pointcloud.ply"
            If testDir.Directory.Name = "Temp" Then MkDir(testDir.Directory.FullName)
        End If

        If saveFileName <> fileName And fileName.Length > 0 Then
            SaveSetting("Opencv", "plyFileName", "plyFileName", fileName)
            saveFileName = fileName
        End If
    End Sub
End Class







Public Class Options_PointCloud : Inherits OptionParent
    Public deltaThreshold As Double = 5
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Delta Z threshold (cm)", 0, 100, deltaThreshold)
    End Sub
    Public Sub Run()
        Static deltaSlider = OptionParent.FindSlider("Delta Z threshold (cm)")
        deltaThreshold = deltaSlider.value / 100
    End Sub
End Class




Public Class Options_PolyLines : Inherits OptionParent
    Public polyCount As Integer = 100
    Public polyClosed As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Polyline closed if checked")
            check.Box(0).Checked = polyClosed
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Polyline Count", 2, 500, polyCount)
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Polyline Count")
        Static closeCheck = FindCheckBox("Polyline closed if checked")
        polyCount = countSlider.value
        polyClosed = closeCheck.checked
    End Sub
End Class






Public Class Options_Projection : Inherits OptionParent
    Public topCheck As Boolean = True
    Public index As Integer = 0
    Public projectionThreshold As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Index of object", 0, 100, index) ' zero is the largest object present.
            sliders.setupTrackBar("Concentration threshold", 0, 100, projectionThreshold)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Concentration threshold")
        Static topCheckBox = FindCheckBox("Top View (Unchecked Side View)")
        Static objSlider = OptionParent.FindSlider("Index of object")
        index = objSlider.value
        If topCheckBox IsNot Nothing Then topCheck = topCheckBox.checked
        projectionThreshold = thresholdSlider.value
    End Sub
End Class






Public Class Options_Puzzle : Inherits OptionParent
    Public startPuzzle As Boolean = True
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Start another puzzle")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static startBox = FindCheckBox("Start another puzzle")
        startPuzzle = startBox.checked
        startBox.checked = False
    End Sub
End Class






Public Class Options_Pyramid : Inherits OptionParent
    Public zoom As Integer = 0
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Zoom in and out", -1, 1, zoom)
    End Sub
    Public Sub Run()
        Static zoomSlider = OptionParent.FindSlider("Zoom in and out")
        zoom = zoomSlider.Value
    End Sub
End Class






Public Class Options_PyrFilter : Inherits OptionParent
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
    Public Sub Run()
        Static radiusSlider = OptionParent.FindSlider("MeanShift Spatial Radius")
        Static colorSlider = OptionParent.FindSlider("MeanShift color Radius")
        Static maxSlider = OptionParent.FindSlider("MeanShift Max Pyramid level")
        spatialRadius = radiusSlider.value
        colorRadius = colorSlider.value
        maxPyramid = maxSlider.value
    End Sub
End Class





Public Class Options_NormalDist : Inherits OptionParent
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
    Public Sub Run()
        Static blueSlider = OptionParent.FindSlider("Random_NormalDist Blue Mean")
        Static greenSlider = OptionParent.FindSlider("Random_NormalDist Green Mean")
        Static redSlider = OptionParent.FindSlider("Random_NormalDist Red Mean")
        Static stdevSlider = OptionParent.FindSlider("Random_NormalDist Stdev")
        redVal = redSlider.value
        greenVal = greenSlider.value
        blueVal = blueSlider.value
        stdev = stdevSlider.value

        Static grayCheck = FindCheckBox("Use Grayscale image")
        grayChecked = grayCheck.checked
    End Sub
End Class






Public Class Options_MonteCarlo : Inherits OptionParent
    Public dimension As Integer = 91
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of bins", 1, 255, dimension)
    End Sub
    Public Sub Run()
        Static binSlider = OptionParent.FindSlider("Number of bins")
        dimension = binSlider.value
    End Sub
End Class





Public Class Options_StaticTV : Inherits OptionParent
    Public rangeVal As Integer = 50
    Public threshPercent As Double = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Range of noise to apply (from 0 to this value)", 0, 255, rangeVal)
            sliders.setupTrackBar("Percentage of pixels to include noise", 0, 100, threshPercent)
        End If
    End Sub
    Public Sub Run()
        Static valSlider = OptionParent.FindSlider("Range of noise to apply (from 0 to this value)")
        Static threshSlider = OptionParent.FindSlider("Percentage of pixels to include noise")
        rangeVal = valSlider.Value
        threshPercent = threshSlider.Value
    End Sub
End Class







Public Class Options_Clusters : Inherits OptionParent
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
    Public Sub Run()
        Static clustSlider = OptionParent.FindSlider("Number of Clusters")
        Static numSlider = OptionParent.FindSlider("Number of points per cluster")
        Static stdevSlider = OptionParent.FindSlider("Cluster stdev")
        numClusters = clustSlider.Value
        numPoints = numSlider.Value
        stdev = stdevSlider.Value
    End Sub
End Class






Public Class Options_Draw : Inherits OptionParent
    Public proximity As Integer = 250
    Public drawCount As Integer = 3
    Public drawFilled As Integer = 2
    Public drawRotated As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("DrawCount", 0, 20, drawCount)
            sliders.setupTrackBar("Merge rectangles within X pixels", 0, task.cols, If(task.cols = 1280, proximity * 2, proximity))
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            check.addCheckBox("Draw filled (unchecked draw an outline)")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static mergeSlider = OptionParent.FindSlider("Merge rectangles within X pixels")
        Static countSlider = OptionParent.FindSlider("DrawCount")
        Static fillCheck = FindCheckBox("Draw filled (unchecked draw an outline)")
        Static rotateCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        drawCount = countSlider.Value
        drawFilled = If(fillCheck.checked, -1, 2)
        drawRotated = rotateCheck.checked
        proximity = mergeSlider.Value
    End Sub
End Class






Public Class Options_RBF : Inherits OptionParent
    Public RBFCount As Integer = 2
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RBF Recursion count", 1, 20, RBFCount)
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("RBF Recursion count")
        RBFCount = countSlider.value
    End Sub
End Class







Public Class Options_RedCloudFeatures : Inherits OptionParent
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
    Public Sub Run()
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







Public Class Options_RedTrack : Inherits OptionParent
    Public maxDistance As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, maxDistance)
    End Sub
    Public Sub Run()
        Static distSlider = OptionParent.FindSlider("Max feature travel distance")
        maxDistance = distSlider.Value
    End Sub
End Class






Public Class Options_Retina : Inherits OptionParent
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
    Public Sub Run()
        Static sampleSlider = OptionParent.FindSlider("Retina Sample Factor")
        Static logCheckbox = FindCheckBox("Use log sampling")
        Static xmlCheckbox = FindCheckBox("Open resulting xml file")
        useLogSampling = logCheckbox.Checked
        If xmlCheckbox.checked Then xmlCheckbox.checked = False
        xmlCheck = xmlCheckbox.checked
        sampleFactor = sampleSlider.value
    End Sub
End Class






Public Class Options_ROI : Inherits OptionParent
    Public roiPercent As Double = 0.25
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max size area of interest %", 0, 100, roiPercent * 100)
    End Sub
    Public Sub Run()
        Static roiSlider = OptionParent.FindSlider("Max size area of interest %")
        roiPercent = roiSlider.value / 100
    End Sub
End Class






Public Class Options_Rotate : Inherits OptionParent
    Public rotateAngle As Double = 24
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Rotation Angle in degrees X100", -18000, 18000, rotateAngle * 100)
    End Sub
    Public Sub Run()
        Static angleSlider = OptionParent.FindSlider("Rotation Angle in degrees X100")
        rotateAngle = angleSlider.Value / 100
    End Sub
End Class





Public Class Options_Salience : Inherits OptionParent
    Public numScales As Integer = 6
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Salience numScales", 1, 6, numScales)
    End Sub
    Public Sub Run()
        Static scaleSlider = OptionParent.FindSlider("Salience numScales")
        numScales = scaleSlider.Value
    End Sub
End Class







Public Class Options_SLRImages : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For Each rad In frm.check
            If rad.checked Then radioText = rad.text
        Next
    End Sub
End Class






Public Class Options_Stabilizer : Inherits OptionParent
    Public lostMax As Double = 0.1
    Public width As Integer = 128
    Public height As Integer = 96
    Public minStdev As Double = 10
    Public corrThreshold As Double = 0.95
    Public pad As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max % of lost pixels before reseting image", 0, 100, lostMax * 100)
            sliders.setupTrackBar("Width of input to matchtemplate", 10, task.cols - pad, width)
            sliders.setupTrackBar("Height of input to matchtemplate", 10, task.rows - pad, height)
            sliders.setupTrackBar("Min stdev in correlation rect", 1, 50, minStdev)
            sliders.setupTrackBar("Stabilizer Correlation Threshold X1000", 0, 1000, corrThreshold * 1000)
        End If
    End Sub
    Public Sub Run()
        Static widthSlider = OptionParent.FindSlider("Width of input to matchtemplate")
        Static heightSlider = OptionParent.FindSlider("Height of input to matchtemplate")
        Static netSlider = OptionParent.FindSlider("Max % of lost pixels before reseting image")
        Static stdevSlider = OptionParent.FindSlider("Min stdev in correlation rect")
        Static thresholdSlider = OptionParent.FindSlider("Stabilizer Correlation Threshold X1000")
        lostMax = netSlider.value / 100
        minStdev = stdevSlider.value
        corrThreshold = thresholdSlider.value / 1000
        width = widthSlider.value
        height = heightSlider.value
    End Sub
End Class







Public Class Options_Stitch : Inherits OptionParent
    Public imageCount As Integer = 10
    Public width As Integer = 0
    Public height As Integer = 0
    Public Sub New()
        width = task.cols / 2
        height = task.rows / 2
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of random images", 10, 50, 10)
            sliders.setupTrackBar("Rectangle width", task.cols / 4, task.cols - 1, width)
            sliders.setupTrackBar("Rectangle height", task.rows / 4, task.rows - 1, height)
        End If
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Number of random images")
        Static widthSlider = OptionParent.FindSlider("Rectangle width")
        Static heightSlider = OptionParent.FindSlider("Rectangle height")
        imageCount = countSlider.Value
        width = widthSlider.Value
        height = heightSlider.Value
    End Sub
End Class






Public Class Options_StructuredFloor : Inherits OptionParent
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
    Public Sub Run()
        Static xCheckbox = FindCheckBox("Smooth in X-direction")
        Static yCheckbox = FindCheckBox("Smooth in Y-direction")
        Static zCheckbox = FindCheckBox("Smooth in Z-direction")
        xCheck = xCheckbox.checked
        yCheck = yCheckbox.checked
        zCheck = zCheckbox.checked
    End Sub
End Class







Public Class Options_StructuredCloud : Inherits OptionParent
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
    Public Sub Run()
        Static xLineSlider = OptionParent.FindSlider("Lines in X-Direction")
        Static yLineSlider = OptionParent.FindSlider("Lines in Y-Direction")
        Static thresholdSlider = OptionParent.FindSlider("Continuity threshold in mm")
        Static xSlider = OptionParent.FindSlider("Slice index X")
        Static ySlider = OptionParent.FindSlider("Slice index Y")

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






Public Class Options_StructuredMulti : Inherits OptionParent
    Public maxSides As Integer = 4
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of sides in the identified polygons", 3, 100, maxSides)
        End If
    End Sub
    Public Sub Run()
        Static sidesSlider = OptionParent.FindSlider("Max number of sides in the identified polygons")
        maxSides = sidesSlider.Value
    End Sub
End Class





Public Class Options_Structured : Inherits OptionParent
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
    Public Sub Run()
        Static sliceSlider = OptionParent.FindSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = OptionParent.FindSlider("Slice step size in pixels (multi-slice option only)")
        Static rebuiltRadio = findRadio("Show rebuilt data")
        rebuilt = rebuiltRadio.checked
        sliceSize = sliceSlider.Value
        stepSize = stepSlider.Value
    End Sub
End Class






Public Class Options_SuperPixels : Inherits OptionParent
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
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Number of SuperPixels")
        Static iterSlider = OptionParent.FindSlider("SuperPixel Iterations")
        Static priorSlider = OptionParent.FindSlider("Prior")
        numSuperPixels = countSlider.value
        numIterations = iterSlider.value
        prior = priorSlider.value
    End Sub
End Class





Public Class Options_Swarm : Inherits OptionParent
    Public ptCount As Integer = 2
    Public border As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Connect X KNN points", 1, 10, ptCount)
            sliders.setupTrackBar("Distance to image border", 1, 10, border)
        End If
    End Sub
    Public Sub Run()
        Static ptSlider = OptionParent.FindSlider("Connect X KNN points")
        Static borderSlider = OptionParent.FindSlider("Distance to image border")
        ptCount = ptSlider.value
        border = borderSlider.value
    End Sub
End Class





Public Class Options_SwarmPercent : Inherits OptionParent
    Public percent As Double = 0.8
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Cells map X percent", 1, 100, percent * 100)
    End Sub
    Public Sub Run()
        Static percentSlider = OptionParent.FindSlider("Cells map X percent")
        percent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_Texture : Inherits OptionParent
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
    Public Sub Run()
        Static deltaSlider = OptionParent.FindSlider("Texture Flow Delta")
        Static blockSlider = OptionParent.FindSlider("Texture Eigen BlockSize")
        Static ksizeSlider = OptionParent.FindSlider("Texture Eigen Ksize")

        TFdelta = deltaSlider.Value
        TFblockSize = blockSlider.Value * 2 + 1
        TFksize = ksizeSlider.Value * 2 + 1
    End Sub
End Class







Public Class Options_ThresholdDef : Inherits OptionParent
    Public threshold As Integer = 127
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Defined Threshold", 0, 255, threshold)
    End Sub
    Public Sub Run()
        Static truncateSlider = OptionParent.FindSlider("Defined Threshold")
        threshold = truncateSlider.value
    End Sub
End Class





Public Class Options_Tracker : Inherits OptionParent
    Public trackType As Integer = 1
    Public label As String
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                label = "Method: " + radio.check(i).Text
                trackType = i
                Exit For
            End If
        Next
    End Sub
End Class






Public Class Options_Transform : Inherits OptionParent
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
            sliders.setupTrackBar("Rotation center X", 1, task.cols, task.cols / 2)
            sliders.setupTrackBar("Rotation center Y", 1, task.rows, task.rows / 2)
        End If
        If check.Setup(traceName) Then
            check.addCheckBox("Check to snap the first point cloud")
            check.addCheckBox("Check to snap the second point cloud")
        End If
    End Sub
    Public Sub Run()
        Static angleSlider = OptionParent.FindSlider("Angle")
        Static scaleSlider = OptionParent.FindSlider("Scale Factor% (100% means no scaling)")
        Static centerXSlider = OptionParent.FindSlider("Rotation center X")
        Static centerYSlider = OptionParent.FindSlider("Rotation center Y")
        Static firstCheckBox = FindCheckBox("Check to snap the first point cloud")
        Static secondCheckBox = FindCheckBox("Check to snap the second point cloud")
        Static percentSlider = OptionParent.FindSlider("Resize Percent")
        resizeFactor = percentSlider.Value / 100
        firstCheck = firstCheckBox.checked
        secondCheck = secondCheckBox.checked

        angle = angleSlider.value
        scale = scaleSlider.value / 100
        centerX = centerXSlider.value
        centerY = centerYSlider.value
    End Sub
End Class






Public Class Options_TransformationMatrix : Inherits OptionParent
    Public mul As Integer = 500
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("TMatrix Top View multiplier", 1, 1000, mul)
    End Sub
    Public Sub Run()
        Static multSlider = OptionParent.FindSlider("TMatrix Top View multiplier")
        mul = multSlider.value
    End Sub
End Class





Public Class Options_Vignetting : Inherits OptionParent
    Public radius As Double = 80
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Vignette radius X100", 1, 300, radius)
    End Sub
    Public Sub Run()
        Static radiusSlider = OptionParent.FindSlider("Vignette radius X100")
        radius = radiusSlider.value / 100
    End Sub
End Class





Public Class Options_Video : Inherits OptionParent
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
        fileNameForm.filename.Text = GetSetting("Opencv", "VideoFileName", "VideoFileName", fileInfo.FullName)
        fileNameForm.Text = "Select a video file for input"
        fileNameForm.FileNameLabel.Text = "Select a video file for input"
        fileNameForm.PlayButton.Hide()
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        fileNameForm.filename.Text = fileInfo.FullName
    End Sub
    Public Sub Run()
        If task.optionsChanged Then
            maxFrames = 1000
            currFrame = 0
            If fileNameForm.newFileName Then fileInfo = New FileInfo(fileNameForm.filename.Text)
            If fileInfo.Exists = False Then
                MessageBox.Show("File not found: " + fileInfo.FullName)
                Exit Sub
            End If
        End If
        fileNameForm.TrackBar1.Maximum = maxFrames
        fileNameForm.TrackBar1.Value = currFrame
    End Sub
End Class






Public Class Options_WarpAffine : Inherits OptionParent
    Public angle As Integer = 10
    Public Sub New()
        If (sliders.Setup(traceName)) Then sliders.setupTrackBar("Angle", 0, 360, angle)
    End Sub
    Public Sub Run()
        Static angleSlider = OptionParent.FindSlider("Angle")
        angle = angleSlider.value
    End Sub
End Class






Public Class Options_WarpPerspective : Inherits OptionParent
    Public width As Integer = 0
    Public height As Integer = 0
    Public angle As Integer = 0
    Public Sub New()
        width = task.cols - 50
        height = task.rows - 50
        angle = 0
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Warped Width", 0, task.cols, width)
            sliders.setupTrackBar("Warped Height", 0, task.rows, height)
            sliders.setupTrackBar("Warped Angle", 0, 360, angle)
        End If
    End Sub
    Public Sub Run()
        Static wSlider = OptionParent.FindSlider("Warped Width")
        Static hSlider = OptionParent.FindSlider("Warped Height")
        Static angleSlider = OptionParent.FindSlider("Warped Angle")
        width = wSlider.value
        height = hSlider.value
        angle = angleSlider.value
    End Sub
End Class






Public Class Options_XPhotoInpaint : Inherits OptionParent
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
    Public Sub Run()
        Static radioFast = findRadio("FSR_Fast")
        Static radioSMap = findRadio("ShiftMap")
        FSRFast = radioFast.checked
        shiftMap = radioSMap.checked
    End Sub
End Class






Public Class Options_Density : Inherits OptionParent
    Public zCount As Integer = 3
    Public distance As Double = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Distance in meters X10000", 1, 2000, task.densityMetric)
            sliders.setupTrackBar("Neighboring Z count", 0, 8, zCount)
        End If
        distance = task.densityMetric
    End Sub
    Public Sub Run()
        Static distSlider = OptionParent.FindSlider("Distance in meters X10000")
        Static neighborSlider = OptionParent.FindSlider("Neighboring Z count")
        zCount = neighborSlider.value
        distance = distSlider.value / 10000
    End Sub
End Class






Public Class Options_ColorMethod : Inherits OptionParent
    Public Sub New()
        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            For Each alg In task.gOptions.colorMethods
                check.addCheckBox(alg)
            Next
            check.Box(4).Checked = True
        End If
    End Sub
    Public Sub Run()
    End Sub
End Class






Public Class Options_DiffDepth : Inherits OptionParent
    Public millimeters As Integer
    Public meters As Double
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Depth varies more than X mm's", 1, 2000, 1000)
    End Sub
    Public Sub Run()
        Static mmSlider = OptionParent.FindSlider("Depth varies more than X mm's")
        millimeters = mmSlider.value
        meters = millimeters / 1000
    End Sub
End Class







Public Class Options_Outliers : Inherits OptionParent
    Public cutoffPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Cutoff bins with less than %", 0, 100, 1)
        End If
    End Sub
    Public Sub Run()
        Static percentSlider = OptionParent.FindSlider("Cutoff bins with less than %")
        cutoffPercent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_BP_Regions : Inherits OptionParent
    Public cellCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of cells to identify", 1, 100, 50)
    End Sub
    Public Sub Run()
        Static countSlider = OptionParent.FindSlider("Number of cells to identify")
        cellCount = countSlider.value
    End Sub
End Class





Public Class Options_ML : Inherits OptionParent
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        ML_Name = frm.check(findRadioIndex(frm.check)).Text
        If task.frameCount < 100 Or task.optionsChanged Then frm.left = task.gOptions.Width / 2 + 10
    End Sub
End Class






Public Class Options_GridFromResize : Inherits OptionParent
    Public lowResPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("LowRes %", 1, 100, 10)
    End Sub
    Public Sub Run()
        Static percentSlider = OptionParent.FindSlider("LowRes %")
        lowResPercent = percentSlider.value / 100
    End Sub
End Class






Public Class Options_LaplacianKernels : Inherits OptionParent
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
    Public Sub Run()
        Static gaussSlider = OptionParent.FindSlider("Gaussian Kernel")
        Static LaplacianSlider = OptionParent.FindSlider("Laplacian Kernel")
        Static thresholdSlider = OptionParent.FindSlider("Laplacian Threshold")
        gaussiankernelSize = gaussSlider.Value Or 1
        LaplaciankernelSize = LaplacianSlider.Value Or 1
        threshold = thresholdSlider.Value
    End Sub
End Class






Public Class Options_LinearInput : Inherits OptionParent
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
    Public Sub Run()
        Static deltaSlider = OptionParent.FindSlider("Delta (mm)")
        delta = deltaSlider.value / 1000

        Static frm = FindFrm(traceName + " Radio Buttons")
        offsetDirection = frm.check(findRadioIndex(frm.check)).Text

        Static xRadio = findRadio("X Direction")
        Static yRadio = findRadio("Y Direction")
        Static zxRadio = findRadio("Z in X-Direction")
        Static zyRadio = findRadio("Z in Y-Direction")

        dimension = 2
        If xRadio.checked Then dimension = 0
        If yRadio.checked Then dimension = 1

        If zyRadio.checked Then zy = True Else zy = False
    End Sub
End Class






Public Class Options_ImageOffset : Inherits OptionParent
    Public offsetDirection As String
    Public horizontalSlice As Boolean
    Public Sub New()
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
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        offsetDirection = frm.check(findRadioIndex(frm.check)).Text

        Static sliceDirection = FindCheckBox("Slice Horizontally (off Vertically)")
        horizontalSlice = sliceDirection.checked
    End Sub
End Class







Public Class Options_Line : Inherits OptionParent
    Public minLength As Integer = 1
    Public maxIntersection As Integer
    Public correlation As Single
    Public topX As Integer
    Public overlapPercent As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Intersection Maximum Pixel Count", 1, 100, 15)
            sliders.setupTrackBar("Min Correlation", 1, 100, 95)
            sliders.setupTrackBar("Top X count", 1, 254, 3)
            sliders.setupTrackBar("Same line overlap %", 1, 100, 50)
            sliders.setupTrackBar("Distance to next center", 1, 100, 30)
        End If
    End Sub
    Public Sub Run()
        Static lenSlider = OptionParent.FindSlider("Min Line Length")
        Static interSlider = OptionParent.FindSlider("Intersection Maximum Pixel Count")
        Static correlSlider = OptionParent.FindSlider("Min Correlation")
        Static topXSlider = OptionParent.FindSlider("Top X count")
        Static overlapSlider = OptionParent.FindSlider("Same line overlap %")
        Static distanceSlider = OptionParent.FindSlider("Distance to next center")
        If lenSlider Is Nothing Then Exit Sub ' diagnostic...
        minLength = lenSlider.value
        maxIntersection = interSlider.value
        correlation = correlSlider.value / 100
        topX = topXSlider.value
        overlapPercent = overlapSlider.value / 100
    End Sub
End Class










Public Class Options_OpenGLFunctions : Inherits OptionParent
    Public moveAmount As cv.Scalar = New cv.Scalar(0, 0, 0)
    Public FOV As Double = 75
    Public yaw As Double = -3
    Public pitch As Double = 3
    Public roll As Double = 0
    Public zNear As Double = 0
    Public zFar As Double = 20.0
    Public zTrans As Double = 0.5
    Public eye As cv.Vec3f = New cv.Vec3f(4, 20, -2)
    Public scaleXYZ As cv.Vec3f = New cv.Vec3f(15, 30, 1)
    Public pointSize As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL shift left/right (X-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift up/down (Y-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift fwd/back (Z-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, 2)
        End If
    End Sub
    Public Sub Run()
        Static XmoveSlider = OptionParent.FindSlider("OpenGL shift left/right (X-axis) X100")
        Static YmoveSlider = OptionParent.FindSlider("OpenGL shift up/down (Y-axis) X100")
        Static ZmoveSlider = OptionParent.FindSlider("OpenGL shift fwd/back (Z-axis) X100")
        Static PointSizeSlider = OptionParent.FindSlider("OpenGL Point Size")

        pointSize = PointSizeSlider.value
        moveAmount = New cv.Point3f(XmoveSlider.Value / 100, YmoveSlider.Value / 100, ZmoveSlider.Value / 100)
    End Sub
End Class





Public Class Options_Derivative : Inherits OptionParent
    Public channel As Integer = 2
    Dim options As New Options_Sobel
    Public kernelSize As Integer = 3
    Public derivativeRange As Double = 0.1
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("X Dimension")
            radio.addRadio("Y Dimension")
            radio.addRadio("Z Dimension")
            radio.check(channel).Checked = True
        End If
    End Sub
    Public Sub Run()
        options.Run()

        Static frm = FindFrm(traceName + " Radio Buttons")
        If frm.check(0).checked Then channel = 0
        If frm.check(1).checked Then channel = 1
        If frm.check(2).checked Then channel = 2

        kernelSize = options.kernelSize
        derivativeRange = options.derivativeRange
    End Sub
End Class




Public Class Options_DerivativeBasics : Inherits OptionParent
    Public mmThreshold As Single = 100
    Public histBars As Integer = 1
    Public rect1 As cv.Rect, rect2 As cv.Rect
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Plus/Minus bars around center", 1, 100, histBars)
            sliders.setupTrackBar("mm Threshold", 0, 1000, mmThreshold)
        End If

        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Vertical Derivative")
            radio.addRadio("Horizontal Derivative")
            radio.addRadio("Both Derivatives")
            radio.check(2).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("mm Threshold")
        Static barSlider = OptionParent.FindSlider("Plus/Minus bars around center")
        mmThreshold = thresholdSlider.value / 1000
        histBars = barSlider.value

        Static vertRadio = findRadio("Vertical Derivative")
        Static horizRadio = findRadio("Horizontal Derivative")
        Static bothRadio = findRadio("Both Derivatives")

        Dim horizontalDerivative As Boolean
        Dim verticalDerivative As Boolean
        If bothRadio.checked Then
            horizontalDerivative = True
            verticalDerivative = True
        Else
            If vertRadio.checked Then verticalDerivative = True Else horizontalDerivative = True
        End If

        Dim offsetX As Integer = If(horizontalDerivative, 1, 0)
        Dim offsetY As Integer = If(verticalDerivative, 1, 0)
        rect1 = New cv.Rect(0, 0, task.cols - offsetX, task.rows - offsetY)
        rect2 = New cv.Rect(offsetX, offsetY, task.cols - offsetX, task.rows - offsetY)
    End Sub
End Class







Public Class Options_Grid : Inherits OptionParent
    Public width As Integer = 32
    Public height As Integer = 8
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("brick Width", 1, task.cols, width)
            sliders.setupTrackBar("brick Height", 1, task.rows, height)
        End If
    End Sub
    Public Sub Run()
        Static widthSlider = OptionParent.FindSlider("brick Width")
        Static heightSlider = OptionParent.FindSlider("brick Height")
        width = widthSlider.value
        height = heightSlider.value
    End Sub
End Class






Public Class Options_Regions : Inherits OptionParent
    Public displayIndex As Integer
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Raw Pointcloud")
            radio.addRadio("Flat bricks")
            radio.addRadio("Connected bricks")
            radio.addRadio("Vertical Cells")
            radio.addRadio("Horizontal Cells")
            radio.check(3).Checked = True
        End If
    End Sub
    Public Sub Run()
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then displayIndex = i
        Next
    End Sub
End Class






Public Class Options_RGBAlign : Inherits OptionParent
    Public xDisp As Integer
    Public yDisp As Integer
    Public xShift As Integer
    Public yShift As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("X Displacement", 0, 100, 46)
            sliders.setupTrackBar("Y Displacement", 0, 100, 26)
            sliders.setupTrackBar("X Shift", -50, 50, -3)
            sliders.setupTrackBar("Y Shift", -50, 50, 3)
        End If
    End Sub
    Public Sub Run()
        Static XSlider = OptionParent.FindSlider("X Displacement")
        Static YSlider = OptionParent.FindSlider("Y Displacement")
        Static xShiftSlider = OptionParent.FindSlider("X Shift")
        Static yShiftSlider = OptionParent.FindSlider("Y Shift")
        xDisp = XSlider.value
        yDisp = YSlider.value
        xShift = xShiftSlider.value
        yShift = yShiftSlider.value
    End Sub
End Class








Public Class Options_OpenGL1 : Inherits OptionParent
    Public pcBufferCount As Integer = 10
    Public yaw As Double = -3
    Public pitch As Double = 3
    Public roll As Double = 0
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenCVB OpenGL buffer count", 1, 100, pcBufferCount)
            sliders.setupTrackBar("OpenGL yaw (degrees)", -180, 180, yaw)
            sliders.setupTrackBar("OpenGL pitch (degrees)", -180, 180, pitch)
            sliders.setupTrackBar("OpenGL roll (degrees)", -180, 180, roll)
            If task.cameraName = "Intel(R) RealSense(TM) Depth Camera 435i" Then
                OptionParent.FindSlider("OpenGL yaw (degrees)").Value = 135
            End If
        End If
    End Sub
    Public Sub Run()
        Static pcBufferSlider = OptionParent.FindSlider("OpenCVB OpenGL buffer count")
        Static yawSlider = OptionParent.FindSlider("OpenGL yaw (degrees)")
        Static pitchSlider = OptionParent.FindSlider("OpenGL pitch (degrees)")
        Static rollSlider = OptionParent.FindSlider("OpenGL roll (degrees)")

        yaw = yawSlider.Value
        pitch = pitchSlider.Value
        roll = rollSlider.Value
        pcBufferCount = pcBufferSlider.value
    End Sub
End Class





Public Class Options_OpenGL2 : Inherits OptionParent
    Public moveAmount As cv.Scalar = New cv.Scalar(0, 0, 0)
    Public FOV As Double = 80
    Public zNear As Double = 0
    Public zFar As Double = 20
    Public eye As cv.Vec3f = New cv.Vec3f(4, 20, -2)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL Eye X X100", -180, 180, eye(1))
            sliders.setupTrackBar("OpenGL Eye Y X100", -180, 180, eye(0))
            sliders.setupTrackBar("OpenGL Eye Z X100", -180, 180, eye(2))
            sliders.setupTrackBar("OpenGL FOV", 1, 180, FOV)
        End If
    End Sub
    Public Sub Run()
        Static eyeXSlider = OptionParent.FindSlider("OpenGL Eye X X100")
        Static eyeYSlider = OptionParent.FindSlider("OpenGL Eye Y X100")
        Static eyeZSlider = OptionParent.FindSlider("OpenGL Eye Z X100")
        Static fovSlider = OptionParent.FindSlider("OpenGL FOV")

        FOV = fovSlider.Value
        eye = New cv.Vec3f(eyeXSlider.Value, eyeYSlider.Value, eyeZSlider.Value)
    End Sub
End Class





Public Class Options_OpenGL3 : Inherits OptionParent
    Public zNear As Double = 0
    Public zFar As Double = 20
    Public zTrans As Double = 0.5
    Public pointSize As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL zNear", 0, 100, zNear)
            sliders.setupTrackBar("OpenGL zFar", -50, 200, zFar)
            sliders.setupTrackBar("OpenGL Point Size", 1, 20, pointSize)
            sliders.setupTrackBar("zTrans (X100)", -1000, 1000, zTrans * 100)
        End If
    End Sub
    Public Sub Run()
        Static zNearSlider = OptionParent.FindSlider("OpenGL zNear")
        Static zFarSlider = OptionParent.FindSlider("OpenGL zFar")
        Static zTransSlider = OptionParent.FindSlider("zTrans (X100)")
        Static PointSizeSlider = OptionParent.FindSlider("OpenGL Point Size")

        zNear = zNearSlider.Value
        zFar = zFarSlider.Value
        pointSize = PointSizeSlider.Value
        zTrans = zTransSlider.Value / 100
    End Sub
End Class





Public Class Options_OpenGL4 : Inherits OptionParent
    Public moveAmount As cv.Scalar = New cv.Scalar(0, 0, 0)
    Public scaleXYZ As cv.Vec3f = New cv.Vec3f(15, 30, 1)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("OpenGL shift left/right (X-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift up/down (Y-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL shift fwd/back (Z-axis) X100", -300, 300, 0)
            sliders.setupTrackBar("OpenGL Scale X X10", 1, 100, scaleXYZ(0))
            sliders.setupTrackBar("OpenGL Scale Y X10", 1, 100, scaleXYZ(1))
            sliders.setupTrackBar("OpenGL Scale Z X10", 1, 100, scaleXYZ(2))
        End If
    End Sub
    Public Sub Run()
        Static XmoveSlider = OptionParent.FindSlider("OpenGL shift left/right (X-axis) X100")
        Static YmoveSlider = OptionParent.FindSlider("OpenGL shift up/down (Y-axis) X100")
        Static ZmoveSlider = OptionParent.FindSlider("OpenGL shift fwd/back (Z-axis) X100")
        Static scaleXSlider = OptionParent.FindSlider("OpenGL Scale X X10")
        Static scaleYSlider = OptionParent.FindSlider("OpenGL Scale Y X10")
        Static scaleZSlider = OptionParent.FindSlider("OpenGL Scale Z X10")

        moveAmount = New cv.Point3f(XmoveSlider.Value / 100, YmoveSlider.Value / 100, ZmoveSlider.Value / 100)
        scaleXYZ = New cv.Vec3f(scaleXSlider.Value, scaleYSlider.Value, scaleZSlider.Value)
    End Sub
End Class




Public Class Options_Stdev : Inherits OptionParent
    Public stdevThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Stdev Threshold ", 0, 20, 12)
        End If
    End Sub
    Public Sub Run()
        Static stdevSlider = OptionParent.FindSlider("Stdev Threshold")
        stdevThreshold = stdevSlider.value
    End Sub
End Class






Public Class Options_GridStdev : Inherits OptionParent
    Public depthThreshold As Single
    Public colorThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Stdev Depth Threshold X100", 0, 100, 50)
            sliders.setupTrackBar("Stdev Color Threshold", 0, 100, 20)
        End If
    End Sub
    Public Sub Run()
        Static depthSlider = OptionParent.FindSlider("Stdev Depth Threshold X100")
        Static colorSlider = OptionParent.FindSlider("Stdev Color Threshold")
        depthThreshold = depthSlider.value / 100
        colorThreshold = colorSlider.value
    End Sub
End Class





Public Class Options_LineRect : Inherits OptionParent
    Public depthThreshold As Single
    Public colorThreshold As Single
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("LineRect Depth Threshold X100", 0, 100, 50)
            sliders.setupTrackBar("LineRect Color Threshold", 0, 100, 20)
        End If
    End Sub
    Public Sub Run()
        Static depthSlider = OptionParent.FindSlider("LineRect Depth Threshold X100")
        Static colorSlider = OptionParent.FindSlider("LineRect Color Threshold")
        depthThreshold = depthSlider.value / 100
        colorThreshold = colorSlider.value
    End Sub
End Class






Public Class Options_Agast : Inherits OptionParent
    Public useNonMaxSuppression As Boolean
    Public agastThreshold As Integer = 30
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Agast Threshold", 0, 100, agastThreshold)

        If FindFrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Use non-max suppression in Agast")
            check.Box(0).Checked = True
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Agast Threshold")
        Static nonMaxCheck = FindCheckBox("Use non-max suppression in Agast")
        useNonMaxSuppression = nonMaxCheck.checked
        agastThreshold = thresholdSlider.value
    End Sub
End Class





Public Class Options_FCSLine : Inherits OptionParent
    Public proximity As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Proximity in pixels", 0, 100, 10)
        End If
    End Sub
    Public Sub Run()
        Static proximitySlider = OptionParent.FindSlider("Proximity in pixels")
        proximity = proximitySlider.value
    End Sub
End Class








Public Class Options_Neighbors : Inherits OptionParent
    Public threshold As Double = 0.005
    Public pixels As Integer = 6
    Public patchZ As Boolean = False
    Public neighbors As Integer = 10
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Difference from neighbor in mm's", 0, 20, threshold * 1000)
            sliders.setupTrackBar("Minimum offset to neighbor pixel", 1, 100, pixels)
            sliders.setupTrackBar("Patch z-values", 0, 1, 1)
            sliders.setupTrackBar("X neighbors", 1, 255, neighbors)
        End If
    End Sub
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Difference from neighbor in mm's")
        Static pixelSlider = OptionParent.FindSlider("Minimum offset to neighbor pixel")
        Static patchSlider = OptionParent.FindSlider("Patch z-values")
        Static topXSlider = OptionParent.FindSlider("X neighbors")
        neighbors = topXSlider.value
        threshold = thresholdSlider.value / 1000
        pixels = pixelSlider.value
        patchZ = patchSlider.value = 1
    End Sub
End Class




Public Class Options_GridCells : Inherits OptionParent
    Public disparityThreshold As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Disparity Threshold", 0, 20, 3)
        End If
    End Sub
    Public Sub Run()
        Static shiftSlider = OptionParent.FindSlider("Disparity Threshold")
        disparityThreshold = shiftSlider.value
    End Sub
End Class







Public Class Options_MinArea : Inherits OptionParent
    Public srcPoints As New List(Of cv.Point2f)
    Public minSize As Integer = 10
    Public numPoints As Integer = 5
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area Number of Points", 1, 30, numPoints)
            sliders.setupTrackBar("Minimum width and height", 10, 300, minSize)
        End If
    End Sub
    Public Sub Run()
        Static numSlider = OptionParent.FindSlider("Area Number of Points")
        Static sizeSlider = OptionParent.FindSlider("Area size")
        srcPoints.Clear()

        Dim pt As cv.Point2f
        numPoints = numSlider.Value
        For i = 0 To numPoints - 1
            pt.X = msRNG.Next(task.cols / 2 - minSize, task.cols / 2 + minSize)
            pt.Y = msRNG.Next(task.rows / 2 - minSize, task.rows / 2 + minSize)
            srcPoints.Add(pt)
        Next
    End Sub
End Class






Public Class Options_FAST : Inherits OptionParent
    Public useNonMax As Boolean = True
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use Non-Max = True")
            check.Box(0).Checked = True
        End If
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Fast Threshold", 0, 100, 50)
    End Sub
    Public Sub Run()
        Static nonMaxCheck = FindCheckBox("Use Non-Max = True")
        useNonMax = nonMaxCheck.checked

        Static thresholdSlider = OptionParent.FindSlider("Fast Threshold")
        task.FASTthreshold = thresholdSlider.Value
    End Sub
End Class






Public Class Options_Sobel : Inherits OptionParent
    Public kernelSize As Integer = 3
    Public sobelThreshold As Integer = 150
    Public distanceThreshold As Integer = 10
    Public derivativeRange As Double = 0.1
    Public horizontalDerivative As Boolean = True
    Public verticalDerivative As Boolean = True
    Public useBlur As Boolean = False
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sobel kernel Size", 1, 31, kernelSize)
            sliders.setupTrackBar("Sobel Intensity Threshold", 0, 255, sobelThreshold)
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
    Public Sub Run()
        Static thresholdSlider = OptionParent.FindSlider("Sobel Intensity Threshold")
        Static ksizeSlider = OptionParent.FindSlider("Sobel kernel Size")
        Static rangeSlider = OptionParent.FindSlider("Range around zero X100")
        Static distanceSlider = OptionParent.FindSlider("Threshold distance")
        kernelSize = ksizeSlider.Value Or 1
        sobelThreshold = thresholdSlider.value
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





Public Class Options_Hist3D : Inherits OptionParent
    Public histogram3DBins As Integer = 4
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Histogram 3D Bins", 2, 16, histogram3DBins)
    End Sub
    Public Sub Run()
        Static binSlider = FindSlider("Histogram 3D Bins")
        histogram3DBins = binSlider.value
    End Sub
End Class






Public Class Options_RedCloud : Inherits OptionParent
    Public reductionTarget As Integer ' how many classes will there be in the resulting reduced cloud data.
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Reduction Target", 1, 255, 200)
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("X Reduction")
            radio.addRadio("Y Reduction")
            radio.addRadio("Z Reduction")
            radio.addRadio("XY Reduction")
            radio.addRadio("XZ Reduction")
            radio.addRadio("YZ Reduction")
            radio.addRadio("XYZ Reduction")
            radio.check(3).Checked = True
        End If
        setupCalcHist()
    End Sub
    Public Shared Sub setupCalcHist()
        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
        Select Case task.cameraName
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                If task.dst2.Height = 480 Or task.dst2.Height = 240 Or task.dst2.Height = 120 Then
                    task.xRange = 1.38
                    task.yRange = 1.0
                Else
                    task.xRange = 2.5
                    task.yRange = 0.8
                End If
            Case "Intel(R) RealSense(TM) Depth Camera 455", ""
                If task.dst2.Height = 480 Or task.dst2.Height = 240 Or task.dst2.Height = 120 Then
                    task.xRange = 2.04
                    task.yRange = 2.14
                Else
                    task.xRange = 3.22
                    task.yRange = 1.39
                End If
#If AZURE_SUPPORT Then
            Case "Azure Kinect 4K"
                task.xRange = 4
                task.yRange = 1.5
#End If
            Case "StereoLabs ZED 2/2i"
                task.xRange = 4
                task.yRange = 1.5
            Case "Oak-D camera"
                task.xRange = 4.07
                task.yRange = 1.32
            Case "MYNT-EYE-D1000"
                task.xRange = 3.5
                task.yRange = 1.5
            Case "Orbbec Gemini 335L"
                task.xRange = 3.5
                task.yRange = 1.5
            Case "Orbbec Gemini 335"
                task.xRange = 3.5
                task.yRange = 1.5
        End Select

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(task.dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.dst2.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        task.rangesTop = New cv.Rangef() {New cv.Rangef(0.1, task.MaxZmeters + 0.1),
                                          New cv.Rangef(-task.xRange, task.xRange)}
        task.rangesSide = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange),
                                           New cv.Rangef(0.1, task.MaxZmeters + 0.1)}

        task.sideCameraPoint = New cv.Point(0, CInt(task.dst2.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.dst2.Width / 2), 0)

        task.projectionThreshold = 3 ' ProjectionThresholdBar.Value
        task.channelCount = 1
        task.channelIndex = 0

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.MaxZmeters)
        task.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1),
                                            New cv.Rangef(rz.Item0, rz.Item1)}

        Select Case task.reductionName
            Case "X Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1)}
                task.channels = {0}
                task.histBinList = {task.histogramBins}
            Case "Y Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(ry.Item0, ry.Item1)}
                task.channels = {1}
                task.histBinList = {task.histogramBins}
            Case "Z Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(rz.Item0, rz.Item1)}
                task.channels = {2}
                task.histBinList = {task.histogramBins}
            Case "XY Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1)}
                task.channelCount = 2
                task.channels = {0, 1}
                task.histBinList = {task.histogramBins, task.histogramBins}
            Case "XZ Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                task.channelCount = 2
                task.channels = {0, 2}
                task.channelIndex = 1
                task.histBinList = {task.histogramBins, task.histogramBins}
            Case "YZ Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(ry.Item0, ry.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                task.channelCount = 2
                task.channels = {1, 2}
                task.channelIndex = 1
                task.histBinList = {task.histogramBins, task.histogramBins}
            Case "XYZ Reduction"
                task.ranges = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1), New cv.Rangef(rz.Item0, rz.Item1)}
                task.channelCount = 3
                task.channels = {0, 1, 2}
                task.channelIndex = 2
                task.histBinList = {task.histogramBins, task.histogramBins, task.histogramBins}
        End Select
    End Sub
    Public Sub Run()
        Static frm = FindFrm(traceName + " Radio Buttons")
        Static redSlider = FindSlider("Reduction Target")
        reductionTarget = redSlider.value
        task.reductionName = frm.check(findRadioIndex(frm.check)).Text
    End Sub
End Class





Public Class Options_ReductionXYZ : Inherits OptionParent
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
    Public Sub Run()
        For i = 0 To 2
            reduceXYZ(i) = check.Box(i).Checked
        Next
    End Sub
End Class





Public Class Options_Reduction : Inherits OptionParent
    Public simpleReduction As Boolean
    Public bitwiseReduction As Boolean
    Public noReduction As Boolean
    Public simpleReductionValue As Integer
    Public bitwiseValue As Integer
    Public Sub New()
        If FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use Simple Reduction")
            radio.addRadio("Use Bitwise Reduction")
            radio.addRadio("No Reduction")
            radio.check(0).Checked = True
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Simple Reduction", 1, 255, 60)
            sliders.setupTrackBar("Reduct X Bits", 0, 8, 5)
        End If

    End Sub
    Public Sub Run()
        Static simpleSlider = FindSlider("Simple Reduction")
        Static bitwiseSlider = FindSlider("Reduct X Bits")
        Static simpleRadio = findRadio("Use Simple Reduction")
        Static bitwiseRadio = findRadio("Use Bitwise Reduction")
        bitwiseReduction = bitwiseRadio.checked
        simpleReduction = simpleRadio.checked
        noReduction = simpleReduction = False And bitwiseReduction = False
        simpleReductionValue = simpleSlider.value
        bitwiseValue = bitwiseSlider.value
    End Sub
End Class




Public Class Options_Features : Inherits OptionParent
    Public quality As Double = 0.01
    Public matchOption As cv.TemplateMatchModes = cv.TemplateMatchModes.CCoeffNormed
    Public matchText As String = ""
    Public k As Double = 0.04
    Public blockSize As Integer = 3

    Dim options As New Options_FeaturesEx

    Public resyncThreshold As Double = 0.95
    Public agastThreshold As Integer = 20
    Public pixelThreshold As Integer = 8
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Quality Level", 1, 100, quality * 100)
            sliders.setupTrackBar("k X1000", 1, 1000, k * 1000)
            sliders.setupTrackBar("Threshold for EndPoint comparisons", 0, 20, pixelThreshold)
        End If
    End Sub
    Public Sub Run()
        options.Run()

        resyncThreshold = options.resyncThreshold
        agastThreshold = options.agastThreshold

        Static qualitySlider = OptionParent.FindSlider("Quality Level")
        Static kSlider = OptionParent.FindSlider("k X1000")
        Static vertRadio = findRadio("Vertical lines")
        k = kSlider.value / 1000

        quality = qualitySlider.Value / 100

        Static thresholdSlider = FindSlider("Threshold for EndPoint comparisons")
        pixelThreshold = thresholdSlider.value
    End Sub
End Class








Public Class Options_FeaturesEx : Inherits OptionParent
    Public resyncThreshold As Double = 0.95
    Public agastThreshold As Integer = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold Percent for Resync", 1, 99, resyncThreshold * 100)
            sliders.setupTrackBar("Agast Threshold", 1, 100, agastThreshold)
        End If
    End Sub
    Public Sub Run()
        Static resyncSlider = OptionParent.FindSlider("Threshold Percent for Resync")
        Static agastslider = OptionParent.FindSlider("Agast Threshold")

        resyncThreshold = resyncSlider.value / 100
        agastThreshold = agastslider.value
    End Sub
End Class

