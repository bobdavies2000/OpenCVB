Imports cv = OpenCvSharp

Public Class DilateErode_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Dilate/Erode Kernel Size", 1, 32, 3)
            sliders.setupTrackBar(1, "Erode (-) to Dilate (+)", -32, 32, 1)
        End If
        task.desc = "Dilate and Erode the RGB and Depth image."

        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "Dilate/Erode shape: Cross"
            radio.check(1).Text = "Dilate/Erode shape: Ellipse"
            radio.check(2).Text = "Dilate/Erode shape: Rect"
            radio.check(3).Text = "Dilate/Erode shape: None"
            radio.check(0).Checked = True
        End If
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static ellipseRadio = findRadio("Dilate/Erode shape: Ellipse")
        Static rectRadio = findRadio("Dilate/Erode shape: Rect")
        Static iterSlider = findSlider("Erode (-) to Dilate (+)")
        Static kernelSlider = findSlider("Dilate/Erode Kernel Size")
        Dim iterations = iterSlider.Value
        Dim kernelsize As Integer = kernelSlider.Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1

        Dim morphShape = cv.MorphShapes.Cross
        If ellipseRadio.Checked Then morphShape = cv.MorphShapes.Ellipse
        If rectRadio.Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        Static noShape = findRadio("Dilate/Erode shape: None")
        If noShape.Checked Then
            dst2 = src
        Else
            If iterations >= 0 Then
                src.Dilate(element, Nothing, iterations).CopyTo(dst2)
            Else
                src.Erode(element, Nothing, -iterations).CopyTo(dst2)
            End If
        End If


        If standalone or task.intermediateActive Then
            If iterations >= 0 Then
                dst3 = task.RGBDepth.Dilate(element, Nothing, iterations)
                labels(2) = "Dilate RGB " + CStr(iterations) + " times"
                labels(3) = "Dilate Depth " + CStr(iterations) + " times"
            Else
                dst3 = task.RGBDepth.Erode(element, Nothing, -iterations)
                labels(2) = "Erode RGB " + CStr(-iterations) + " times"
                labels(3) = "Erode Depth " + CStr(-iterations) + " times"
            End If
        End If
    End Sub
End Class





Public Class DilateErode_DepthSeed : Inherits VBparent
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "DepthSeed flat depth", 1, 200, 100)
            sliders.setupTrackBar(1, "DepthSeed max Depth", 1, 5000, 3000)
        End If
        task.desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim iterations = dilate.sliders.trackbar(1).Value
        Dim kernelsize = If(dilate.sliders.trackbar(0).Value Mod 2, dilate.sliders.trackbar(0).Value, dilate.sliders.trackbar(0).Value + 1)
        Dim morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If dilate.radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        Dim mat As New cv.Mat
        cv.Cv2.Erode(task.depth32f, mat, element)
        mat = task.depth32f - mat
        Dim seeds = mat.LessThan(sliders.trackbar(0).Value).ToMat
        dst3 = seeds

        Dim validImg = task.depth32f.GreaterThan(0).ToMat
        validImg.SetTo(0, task.depth32f.GreaterThan(sliders.trackbar(1).Value)) ' max distance
        cv.Cv2.BitwiseAnd(seeds, validImg, seeds)
        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst2, seeds)
    End Sub
End Class



Public Class DilateErode_OpenClose : Inherits VBparent
    Public Sub New()
        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "Open/Close shape: Cross"
            radio.check(1).Text = "Open/Close shape: Ellipse"
            radio.check(2).Text = "Open/Close shape: Rect"
            radio.check(2).Checked = True
        End If
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Dilate Open/Close Iterations", -10, 10, 10)
        End If
        task.desc = "Erode and dilate with MorphologyEx on the RGB and Depth image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim n = sliders.trackbar(0).Value
        Dim an As integer = If(n > 0, n, -n)
        Dim morphShape = cv.MorphShapes.Rect
        If radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect

        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(an * 2 + 1, an * 2 + 1), New cv.Point(an, an))
        If n < 0 Then
            cv.Cv2.MorphologyEx(task.RGBDepth, dst3, cv.MorphTypes.Open, element)
            cv.Cv2.MorphologyEx(src, dst2, cv.MorphTypes.Open, element)
        Else
            cv.Cv2.MorphologyEx(task.RGBDepth, dst3, cv.MorphTypes.Close, element)
            cv.Cv2.MorphologyEx(src, dst2, cv.MorphTypes.Close, element)
        End If
    End Sub
End Class




