'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim diff As Diff_Basics
    Dim contours As Contours_Basics
    Public intersect As Rectangle_Intersection
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public resetAll As Boolean
    Dim imu As IMU_IscameraStable
    Dim minSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        intersect = New Rectangle_Intersection
        contours = New Contours_Basics()
        imu = New IMU_IscameraStable
        diff = New Diff_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Single frame motion threshold", 1, 100000, If(task.color.Width = 1280, 20000, 1000)) ' used only externally...
            sliders.setupTrackBar(1, "Cumulative motion threshold", 1, src.Total, If(task.color.Width = 1280, 200000, 100000)) ' used only externally...
            sliders.setupTrackBar(2, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much camera motion is reasonable?
        End If

        minSlider = findSlider("Contour minimum area")
        minSlider.Value = 5

        label1 = "Enclosing rectangles are yellow in dst1 and dst2"
        task.desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        diff.src = src
        If diff.src.Channels = 3 Then diff.src = diff.src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else diff.src = diff.src.Clone

        imu.Run()

        Static cumulativeThreshold = findSlider("Cumulative motion threshold")
        Static pixelThreshold = findSlider("Single frame motion threshold")

        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()
        cumulativePixels += changedPixels

        resetAll = imu.cameraStable = False Or cumulativePixels > cumulativeThreshold.value Or changedPixels > pixelThreshold.value Or task.depthOptionsChanged
        If resetAll Then
            cumulativePixels = 0
            task.depthOptionsChanged = False
        End If

        contours.src = dst2
        contours.Run()

        dst1 = diff.src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If contours.contourlist.Count Then
            intersect.inputRects.Clear()
            For Each c In contours.contourlist
                Dim r = cv.Cv2.BoundingRect(c)
                If r.X < 0 Then r.X = 0
                If r.Y < 0 Then r.Y = 0
                If r.X + r.Width > dst2.Width Then r.Width = dst2.Width - r.X
                If r.Y + r.Height > dst2.Height Then r.Height = dst2.Height - r.Y
                intersect.inputRects.Add(r)
            Next
            intersect.Run()

            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In intersect.enclosingRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 2)
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            label2 = "Motion detected"
        Else
            label2 = "No motion detected with contours > " + CStr(minSlider.Value)
        End If
    End Sub
End Class









Public Class Motion_WithBlurDilate
    Inherits VBparent
    Dim blur As Blur_Basics
    Dim diff As Diff_Basics
    Dim dilate As DilateErode_Basics
    Dim contours As Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public Sub New()
        initParent()
        contours = New Contours_Basics()
        dilate = New DilateErode_Basics()
        diff = New Diff_Basics()
        blur = New Blur_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Frames to persist", 1, 100, 10)
        End If

        Dim iterSlider = findSlider("Dilate/Erode Kernel Size")
        iterSlider.Value = 2

        label2 = "Mask of pixel differences "
        task.desc = "Detect contours in the motion data using blur and dilate"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src.Clone
        blur.src = dst1
        blur.Run()
        dst1 = blur.dst1

        Static delayCounter = 0
        delayCounter += 1

        Static persistSlider = findSlider("Frames to persist")
        If delayCounter > persistSlider.value Then
            delayCounter = 0
            rectList.Clear()
        End If

        diff.src = dst1
        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()
        cumulativePixels += changedPixels

        dilate.src = dst2
        dilate.Run()

        contours.src = dilate.dst1
        contours.Run()

        For Each c In contours.contourlist
            Dim r = cv.Cv2.BoundingRect(c)
            If r.X >= 0 And r.Y >= 0 And r.X + r.Width < dst1.Width And r.Y + r.Height < dst1.Height Then
                Dim count = diff.dst2(r).CountNonZero()
                If count > 100 Then rectList.Add(r)
            End If
        Next

        dst1 = If(src.Channels = 1, src.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src.Clone)
        For i = 0 To rectList.Count - 1
            dst1.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class







Public Class Motion_MinMaxDepth
    Inherits VBparent
    Public motion As Motion_Basics
    Public externalReset As Boolean
    Public Sub New()
        initParent()
        motion = New Motion_Basics

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(0).Checked = True
        End If

        label1 = "32-bit format of the stable depth"
        label2 = "Motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f.Clone

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        dst2 = motion.dst2.Clone

        If motion.resetAll Or externalReset Then
            externalReset = False
            dst1 = input.Clone
        Else
            If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            input.CopyTo(dst1, dst2)

            Static useNone = findRadio("Use unchanged depth input")
            Static useMax = findRadio("Use farthest distance")
            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst1, dst1) Else cv.Cv2.Min(input, dst1, dst1)
            Else
                dst1 = input.Clone()
            End If
        End If
    End Sub
End Class








Public Class Motion_MinMaxPointCloud
    Inherits VBparent
    Public stable As Motion_MinMaxDepth
    Public splitPC() As cv.Mat
    Public Sub New()
        initParent()
        stable = New Motion_MinMaxDepth
        label1 = stable.label1
        label2 = stable.label2
        task.desc = "Use the stable depth values to create a stable point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud
        Dim split = input.Split()
        stable.src = split(2) * 1000
        stable.Run()

        dst1 = stable.dst1
        dst2 = stable.dst2
        label2 = "Cumulative Motion = " + Format(stable.motion.changedPixels / 1000, "#0.0") + "k pixels "
        If stable.motion.resetAll Or splitPC Is Nothing Or ocvb.frameCount < 30 Then
            splitPC = split
            dst2 = input
        Else
            splitPC(2) = (stable.dst1 * 0.001).ToMat
            split(0).CopyTo(splitPC(0), dst2)
            split(1).CopyTo(splitPC(1), dst2)
            cv.Cv2.Merge(splitPC, dst2)
        End If
    End Sub
End Class








Public Class Motion_MinMaxDepthColorized
    Inherits VBparent
    Dim stable As Motion_MinMaxDepth
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        stable = New Motion_MinMaxDepth
        label1 = "32-bit format stable depth data"
        label2 = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Motion_MinMaxDepth at a minimum complexity)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static saveMin = task.minRangeSlider.Value
        Static saveMax = task.maxRangeSlider.Value
        If saveMin <> task.minRangeSlider.Value Or saveMax <> task.maxRangeSlider.Value Then stable.externalReset = True
        stable.Run()
        dst1 = stable.dst1

        colorize.src = dst1
        colorize.Run()
        dst2 = colorize.dst1
    End Sub
End Class








Public Class Motion_DepthShadow
    Inherits VBparent
    Dim motion As Motion_Basics
    Dim noiseRemover As Depth_NoiseRemovalMask
    Dim dMin As Depth_SmoothMin
    Public Sub New()
        initParent()
        noiseRemover = New Depth_NoiseRemovalMask
        motion = New Motion_Basics
        dMin = New Depth_SmoothMin

        Dim minSlider = findSlider("Contour minimum area")
        minSlider.Value = 100
        Dim cumSlider = findSlider("Cumulative motion threshold")
        cumSlider.Value = 2000

        label1 = "Motion of the depth shadow"
        task.desc = "Use the motion in the depth shadow to enhance Motion_Basics use of RGB"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        noiseRemover.src = src
        noiseRemover.Run()

        ' task.inrange.nodepthMask.convertto(dMin.src, cv.MatType.CV_32F)
        dMin.src = noiseRemover.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dMin.Run()
        dst2 = dMin.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        motion.src = dst2
        motion.Run()
        dst1 = motion.dst2

        label2 = "Shadow that is consistently present. " + Format(dst2.CountNonZero / 1000, "#,##0") + "k pixels"
    End Sub
End Class







Public Class Motion_ThruCorrelation
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        addw = New AddWeighted_Basics
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation coefficient threshold for motion X1000", 0, 1000, 950)
            sliders.setupTrackBar(1, "Stdev threshold for using correlation", 0, 100, 15)
            sliders.setupTrackBar(2, "Pad size in pixels for the search area", 0, 100, 20)
        End If

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.desc = "Detect motion through the correlation coefficient"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = input.Clone
        Static ccSlider = findSlider("Correlation coefficient threshold for motion X1000")
        Static padSlider = findSlider("Pad size in pixels for the search area")
        Static stdevSlider = findSlider("Stdev threshold for using correlation")
        Dim pad = padSlider.value
        Dim ccThreshold = ccSlider.value
        Dim stdevThreshold = stdevSlider.value

        dst2.SetTo(0)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim correlation As New cv.Mat
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(input(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cv.Cv2.MatchTemplate(lastFrame(roi), input(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                Dim minVal As Single, maxVal As Single
                correlation.MinMaxLoc(minVal, maxVal)
                If maxVal < ccThreshold / 1000 Then
                    If (i Mod grid.tilesPerRow) <> 0 Then dst2(grid.roiList(i - 1)).SetTo(255)
                    If (i Mod grid.tilesPerRow) < grid.tilesPerRow And i < grid.roiList.Count - 1 Then dst2(grid.roiList(i + 1)).SetTo(255)
                    If i > grid.tilesPerRow Then
                        dst2(grid.roiList(i - grid.tilesPerRow)).SetTo(255)
                        dst2(grid.roiList(i - grid.tilesPerRow + 1)).SetTo(255)
                    End If
                    If i < (grid.roiList.Count - grid.tilesPerRow - 1) Then
                        dst2(grid.roiList(i + grid.tilesPerRow)).SetTo(255)
                        dst2(grid.roiList(i + grid.tilesPerRow + 1)).SetTo(255)
                    End If
                    dst2(roi).SetTo(255)
                End If
            End If
        End Sub)

            lastFrame = input.Clone

        If standalone Then
            addw.src = input
            addw.src2 = dst2
            addw.Run()
            dst1 = addw.dst1
        Else
            dst1 = src
        End If
    End Sub
End Class







Public Class Motion_CCmerge
    Inherits VBparent
    Dim motionCC As Motion_ThruCorrelation
    Public Sub New()
        initParent()
        motionCC = New Motion_ThruCorrelation

        task.desc = "Use the correlation coefficient to maintain an up-to-date image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount < 10 Then dst1 = src.Clone

        motionCC.src = src
        motionCC.Run()

        Static lastFrame = src.Clone
        If motionCC.dst2.CountNonZero() > src.Total / 2 Then
            dst1 = src.Clone
            lastFrame = src.Clone
        End If

        src.CopyTo(dst1, motionCC.dst2)
        dst2 = motionCC.dst2
    End Sub
End Class