'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics : Inherits VBparent
    Dim diff As New Diff_Basics
    Dim contours As New Contours_Basics
    Public intersect As New Rectangle_Intersection
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public resetAll As Boolean
    Dim minSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Single frame motion threshold", 1, 100000, If(task.color.Width = 1280, 20000, 1000)) ' used only externally...
            sliders.setupTrackBar(1, "Cumulative motion threshold", 1, dst2.Total, If(task.color.Width = 1280, 200000, 100000)) ' used only externally...
        End If

        minSlider = findSlider("Contour minimum area")
        minSlider.Value = 5

        labels(2) = "Enclosing rectangles are yellow in dst2 and dst3"
        task.desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static cumulativeThreshold = findSlider("Cumulative motion threshold")
        Static pixelThreshold = findSlider("Single frame motion threshold")

        src = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)

        diff.RunClass(src)
        dst3 = diff.dst3
        changedPixels = dst3.CountNonZero()
        cumulativePixels += changedPixels

        resetAll = task.cameraStable = False Or cumulativePixels > cumulativeThreshold.value Or changedPixels > pixelThreshold.value Or task.depthOptionsChanged
        If resetAll Then
            cumulativePixels = 0
            task.depthOptionsChanged = False
        End If

        contours.RunClass(dst3)

        dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If contours.contourlist.Count Then
            intersect.inputRects.Clear()
            For Each c In contours.contourlist
                Dim r = cv.Cv2.BoundingRect(c)
                If r.X < 0 Then r.X = 0
                If r.Y < 0 Then r.Y = 0
                If r.X + r.Width > dst3.Width Then r.Width = dst3.Width - r.X
                If r.Y + r.Height > dst3.Height Then r.Height = dst3.Height - r.Y
                intersect.inputRects.Add(r)
            Next
            intersect.RunClass(src)

            If dst3.Channels = 1 Then dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In intersect.enclosingRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
                dst3.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            labels(3) = "Motion detected"
        Else
            labels(3) = "No motion detected with contours > " + CStr(minSlider.Value)
        End If
    End Sub
End Class









Public Class Motion_WithBlurDilate : Inherits VBparent
    Dim blur As New Blur_Basics
    Dim diff As New Diff_Basics
    Dim dilate As New DilateErode_Basics
    Dim contours As New Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Frames to persist", 1, 100, 10)
        End If

        findSlider("Dilate/Erode Kernel Size").Value = 2

        labels(3) = "Mask of pixel differences "
        task.desc = "Detect contours in the motion data using blur and dilate"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static persistSlider = findSlider("Frames to persist")

        dst2 = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)
        blur.RunClass(dst2)
        dst2 = blur.dst2

        Static delayCounter = 0
        delayCounter += 1

        If delayCounter > persistSlider.value Then
            delayCounter = 0
            rectList.Clear()
        End If

        diff.RunClass(dst2)
        dst3 = diff.dst3
        changedPixels = dst3.CountNonZero()
        cumulativePixels += changedPixels

        dilate.RunClass(dst3)

        contours.RunClass(dilate.dst2)

        For Each c In contours.contourlist
            Dim r = cv.Cv2.BoundingRect(c)
            If r.X >= 0 And r.Y >= 0 And r.X + r.Width < dst2.Width And r.Y + r.Height < dst2.Height Then
                Dim count = diff.dst3(r).CountNonZero()
                If count > 100 Then rectList.Add(r)
            End If
        Next

        dst2 = If(src.Channels = 1, src.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src.Clone)
        For i = 0 To rectList.Count - 1
            dst2.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class







Public Class Motion_MinMaxDepth : Inherits VBparent
    Public motion As New Motion_Basics
    Public externalReset As Boolean
    Public Sub New()
        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(0).Checked = True
        End If

        labels(2) = "32-bit format of the stable depth"
        labels(3) = "Motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static useNone = findRadio("Use unchanged depth input")
        Static useMax = findRadio("Use farthest distance")
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f.Clone

        motion.RunClass(task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst3 = motion.dst3.Clone

        If motion.resetAll Or externalReset Then
            externalReset = False
            dst2 = input.Clone
        Else
            If dst3.Channels <> 1 Then dst3 = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            input.CopyTo(dst2, dst3)

            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst2, dst2) Else cv.Cv2.Min(input, dst2, dst2)
            Else
                dst2 = input.Clone()
            End If
        End If
    End Sub
End Class








Public Class Motion_MinMaxPointCloud : Inherits VBparent
    Public stable As New Motion_MinMaxDepth
    Public splitPC() As cv.Mat
    Public Sub New()
        labels(2) = stable.labels(2)
        labels(3) = stable.labels(3)
        task.desc = "Use the stable depth values to create a stable point cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud
        Dim split = input.Split()
        stable.RunClass(split(2) * 1000)

        dst2 = stable.dst2
        dst3 = stable.dst3
        labels(3) = "Cumulative Motion = " + Format(stable.motion.changedPixels / 1000, "#0.0") + "k pixels "
        If stable.motion.resetAll Or splitPC Is Nothing Or task.frameCount < 30 Then
            splitPC = split
            dst3 = input
        Else
            splitPC(2) = (stable.dst2 * 0.001).ToMat
            split(0).CopyTo(splitPC(0), dst3)
            split(1).CopyTo(splitPC(1), dst3)
            cv.Cv2.Merge(splitPC, dst3)
        End If
    End Sub
End Class








Public Class Motion_MinMaxDepthColorized : Inherits VBparent
    Dim stable As New Motion_MinMaxDepth
    Dim colorize As New Depth_ColorizerFastFade_CPP
    Public Sub New()
        labels(2) = "32-bit format stable depth data"
        labels(3) = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Motion_MinMaxDepth at a minimum complexity)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Static saveMin = task.minDepth
        Static saveMax = task.maxDepth
        If saveMin <> task.minDepth Or saveMax <> task.maxDepth Then stable.externalReset = True
        stable.RunClass(src)
        dst2 = stable.dst2

        colorize.RunClass(dst2)
        dst3 = colorize.dst2
    End Sub
End Class







Public Class Motion_ThruCorrelation : Inherits VBparent
    Dim grid As New Thread_Grid
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Correlation coefficient threshold for motion X1000", 0, 1000, 950)
            sliders.setupTrackBar(1, "Stdev threshold for using correlation", 0, 100, 15)
            sliders.setupTrackBar(2, "Pad size in pixels for the search area", 0, 100, 20)
        End If

        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.desc = "Detect motion through the correlation coefficient"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static ccSlider = findSlider("Correlation coefficient threshold for motion X1000")
        Static padSlider = findSlider("Pad size in pixels for the search area")
        Static stdevSlider = findSlider("Stdev threshold for using correlation")
        Dim pad = padSlider.value
        Dim ccThreshold = ccSlider.value
        Dim stdevThreshold = stdevSlider.value

        grid.RunClass(Nothing)

        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = input.Clone
        dst3.SetTo(0)
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
                    If (i Mod grid.tilesPerRow) <> 0 Then dst3(grid.roiList(i - 1)).SetTo(255)
                    If (i Mod grid.tilesPerRow) < grid.tilesPerRow And i < grid.roiList.Count - 1 Then dst3(grid.roiList(i + 1)).SetTo(255)
                    If i > grid.tilesPerRow Then
                        dst3(grid.roiList(i - grid.tilesPerRow)).SetTo(255)
                        dst3(grid.roiList(i - grid.tilesPerRow + 1)).SetTo(255)
                    End If
                    If i < (grid.roiList.Count - grid.tilesPerRow - 1) Then
                        dst3(grid.roiList(i + grid.tilesPerRow)).SetTo(255)
                        dst3(grid.roiList(i + grid.tilesPerRow + 1)).SetTo(255)
                    End If
                    dst3(roi).SetTo(255)
                End If
            End If
        End Sub)

        lastFrame = input.Clone

        dst2 = src
    End Sub
End Class







Public Class Motion_CCmerge : Inherits VBparent
    Dim motionCC As New Motion_ThruCorrelation
    Public Sub New()
        task.desc = "Use the correlation coefficient to maintain an up-to-date image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount < 10 Then dst2 = src.Clone

        motionCC.RunClass(src)

        Static lastFrame = src.Clone
        If motionCC.dst3.CountNonZero() > src.Total / 2 Then
            dst2 = src.Clone
            lastFrame = src.Clone
        End If

        src.CopyTo(dst2, motionCC.dst3)
        dst3 = motionCC.dst3
    End Sub
End Class
