Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports cv = OpenCvSharp
'Public Class CSharp_Basics : Inherits VB_Parent
'    Public Sub New()
'        desc = "Invoke the selected C# algorithm"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)

'    End Sub
'End Class






'Public Class Blur_Gaussian : Inherits VB_Parent
'    Dim CS_BlurGaussian As New CS_BlurGaussian
'    Dim blur As New Blur_Basics
'    Public Sub New()
'        desc = "Smooth each pixel with a Gaussian kernel of different sizes."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static blurKernelSlider = FindSlider("Blur Kernel Size")
'        CS_BlurGaussian.RunCS(src, dst2, blurKernelSlider.Value Or 1)
'    End Sub
'End Class






'Public Class Blur_Median_CS : Inherits VB_Parent
'    Dim CS_BlurMedian As New CS_BlurMedian
'    Dim blur As New Blur_Basics
'    Public Sub New()
'        desc = "Replace each pixel with the median of neighborhood of varying sizes."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static blurKernelSlider = FindSlider("Blur Kernel Size")
'        CS_BlurMedian.RunCS(src, dst2, blurKernelSlider.Value Or 1)
'    End Sub
'End Class



'Public Class KAZE_KeypointsAKAZE_CS : Inherits VB_Parent
'    Dim CS_AKaze As New CS_Classes.AKaze_Basics
'    Public Sub New()
'        desc = "Find keypoints using AKAZE algorithm."
'        labels(2) = "AKAZE key points"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        CS_AKaze.GetKeypoints(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
'        src.CopyTo(dst2)
'        For i = 0 To CS_AKaze.akazeKeyPoints.Count - 1
'            drawCircle(dst2,CS_AKaze.akazeKeyPoints.ElementAt(i).Pt, task.dotSize, cv.Scalar.Red)
'        Next
'    End Sub
'    End ClassPublic Class KAZE_KeypointsKAZE_CS : Inherits VB_Parent
'    Dim CS_Kaze As New CS_Classes.Kaze_Basics
'    Public Sub New()
'        desc = "Find keypoints using KAZE algorithm."
'        labels(2) = "KAZE key points"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        CS_Kaze.GetKeypoints(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
'        src.CopyTo(dst2)
'        For i = 0 To CS_Kaze.kazeKeyPoints.Count - 1
'            drawCircle(dst2,CS_Kaze.kazeKeyPoints.ElementAt(i).Pt, task.dotSize, cv.Scalar.Red)
'        Next
'    End Sub
'End Class




'Public Class KAZE_Sample_CS : Inherits VB_Parent
'    Dim box As New cv.Mat
'    Dim box_in_scene As New cv.Mat
'    Dim CS_Kaze As New CS_Classes.Kaze_Sample
'    Public Sub New()
'        box = cv.Cv2.ImRead(task.homeDir + "opencv/Samples/Data/box.png", cv.ImreadModes.Color)
'        box_in_scene = cv.Cv2.ImRead(task.homeDir + "opencv/Samples/Data/box_in_scene.png", cv.ImreadModes.Color)
'        desc = "Match keypoints in 2 photos."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Dim result = CS_Kaze.RunCS(box, box_in_scene)
'        dst2 = result.Resize(src.Size())
'    End Sub
'End Class





'Public Class KAZE_LeftAligned_CS : Inherits VB_Parent
'    Dim CS_KazeLeft As New CS_Classes.Kaze_Basics
'    Dim CS_KazeRight As New CS_Classes.Kaze_Basics
'    Public Sub New()
'        If sliders.Setup(traceName) Then
'            sliders.setupTrackBar("Max number of points to match", 1, 300, 100)
'            sliders.setupTrackBar("When matching, max possible distance", 1, 200, 100)
'        End If

'        desc = "Match keypoints in the left and right images but display it as movement in the right image."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static maxSlider = FindSlider("Max number of points to match")
'        Static distSlider = FindSlider("When matching, max possible distance")

'        CS_KazeLeft.GetKeypoints(task.leftView)
'        CS_KazeRight.GetKeypoints(task.rightView)

'        dst3 = If(task.leftView.Channels = 1, task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.leftView)
'        dst2 = If(task.rightView.Channels = 1, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView)

'        Dim maxPoints = maxSlider.Value
'        Dim topDistance = distSlider.Value
'        Dim maxCount = Math.Min(maxPoints, Math.Min(CS_KazeRight.kazeKeyPoints.Count, CS_KazeLeft.kazeKeyPoints.Count))
'        For i = 0 To maxCount - 1
'            Dim pt1 = CS_KazeRight.kazeKeyPoints.ElementAt(i)
'            Dim minIndex As Integer
'            Dim minDistance As Single = Single.MaxValue
'            For j = 0 To CS_KazeLeft.kazeKeyPoints.Count - 1
'                Dim pt2 = CS_KazeLeft.kazeKeyPoints.ElementAt(j)
'                ' the right image point must be to the right of the left image point (pt1 X is < pt2 X) and at about the same Y
'                If Math.Abs(pt2.Pt.Y - pt1.Pt.Y) < 2 And pt1.Pt.X < pt2.Pt.X Then
'                    Dim distance = Math.Sqrt((pt1.Pt.X - pt2.Pt.X) * (pt1.Pt.X - pt2.Pt.X) + (pt1.Pt.Y - pt2.Pt.Y) * (pt1.Pt.Y - pt2.Pt.Y))
'                    ' it is not enough to just be at the same height.  Can't be too far away!
'                    If minDistance > distance And distance < topDistance Then
'                        minIndex = j
'                        minDistance = distance
'                    End If
'                End If
'            Next
'            If minDistance < Single.MaxValue Then
'                drawCircle(dst3,pt1.Pt, task.dotSize + 2, cv.Scalar.Blue)
'                drawCircle(dst2,pt1.Pt, task.dotSize + 2, cv.Scalar.Blue)
'                drawCircle(dst3,CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, task.dotSize + 2, cv.Scalar.Red)
'                drawLine(dst3, pt1.Pt, CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, cv.Scalar.Yellow)
'            End If
'        Next
'        labels(2) = "Right image has " + CStr(CS_KazeRight.kazeKeyPoints.Count) + " key points"
'        labels(3) = "Left image has " + CStr(CS_KazeLeft.kazeKeyPoints.Count) + " key points"
'    End Sub
'End Class






''https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
'Public Class Edge_Sobel : Inherits VB_Parent
'    Public addw As New AddWeighted_Basics
'    Public options As New Options_Sobel
'    Dim blur As New Blur_Gaussian
'    Public Sub New()
'        labels = {"", "", "Horizontal + Vertical derivative - use global 'Add Weighted' slider to see impact.", "Blur output"}
'        desc = "Show Sobel edge detection with varying kernel sizes."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        If options.useBlur Then
'            blur.Run(src)
'            dst3 = blur.dst2
'        Else
'            dst3 = src
'        End If

'        dst1 = If(dst3.Channels = 3, dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst3)
'        If options.horizontalDerivative Then dst2 = dst1.Sobel(cv.MatType.CV_32F, 1, 0, options.kernelSize)
'        If options.verticalDerivative Then dst0 = dst1.Sobel(cv.MatType.CV_32F, 0, 1, options.kernelSize)
'        dst2 = dst2.ConvertScaleAbs()
'        dst0 = dst0.ConvertScaleAbs()

'        addw.src2 = dst0
'        addw.Run(dst2)
'        dst2 = addw.dst2
'    End Sub
'End Class




'Public Class Blob_Basics : Inherits VB_Parent
'    Dim options As New Options_Blob
'    Dim input As New Blob_Input
'    Dim blobDetector As New CS_Classes.Blob_Basics
'    Public Sub New()
'        blobDetector = New CS_Classes.Blob_Basics
'        UpdateAdvice(traceName + ": click 'Show All' to see all the available options.")
'        desc = "Isolate and list blobs with specified options"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        If standaloneTest() Then
'            input.Run(src)
'            dst2 = input.dst2
'        Else
'            dst2 = src
'        End If
'        blobDetector.RunCS(dst2, dst3, options.blobParams)
'    End Sub
'End Class








'Public Class Options_Blob : Inherits VB_Parent
'    Dim blob As New Blob_Input
'    Dim blobDetector As New CS_Classes.Blob_Basics
'    Public blobParams = New cv.SimpleBlobDetector.Params
'    Public Sub New()
'        blobDetector = New CS_Classes.Blob_Basics
'        If standaloneTest() Then blob.updateFrequency = 30

'        If radio.Setup(traceName) Then
'            radio.addRadio("FilterByArea")
'            radio.addRadio("FilterByCircularity")
'            radio.addRadio("FilterByConvexity")
'            radio.addRadio("FilterByInertia")
'            radio.addRadio("FilterByColor")
'            radio.check(1).Checked = True
'        End If

'        If sliders.Setup(traceName) Then
'            sliders.setupTrackBar("min Threshold", 0, 255, 100)
'            sliders.setupTrackBar("max Threshold", 0, 255, 255)
'            sliders.setupTrackBar("Threshold Step", 1, 50, 5)
'        End If
'    End Sub
'    Public Sub RunVB()
'        Static minSlider = FindSlider("min Threshold")
'        Static maxSlider = FindSlider("max Threshold")
'        Static stepSlider = FindSlider("Threshold Step")
'        Static areaRadio = findRadio("FilterByArea")
'        Static circRadio = findRadio("FilterByCircularity")
'        Static convexRadio = findRadio("FilterByConvexity")
'        Static inertiaRadio = findRadio("FilterByInertia")
'        Static colorRadio = findRadio("FilterByColor")

'        blobParams = New cv.SimpleBlobDetector.Params
'        If areaRadio.Checked Then blobParams.FilterByArea = areaRadio.Checked
'        If circRadio.Checked Then blobParams.FilterByCircularity = circRadio.Checked
'        If convexRadio.Checked Then blobParams.FilterByConvexity = convexRadio.Checked
'        If inertiaRadio.Checked Then blobParams.FilterByInertia = inertiaRadio.Checked
'        If colorRadio.Checked Then blobParams.FilterByColor = colorRadio.Checked

'        blobParams.MaxArea = 100
'        blobParams.MinArea = 0.001

'        blobParams.MinThreshold = minSlider.Value
'        blobParams.MaxThreshold = maxSlider.Value
'        blobParams.ThresholdStep = stepSlider.Value

'        blobParams.MinDistBetweenBlobs = 10
'        blobParams.MinRepeatability = 1
'    End Sub
'End Class 
'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Sift_Basics : Inherits VB_Parent
'    Dim siftCS As New CS_Classes.CS_SiftBasics
'    Dim options As New Options_Sift
'    Public Sub New()
'        desc = "Compare 2 images to get a homography.  We will use left and right images."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        Dim doubleSize As New cv.Mat(dst2.Rows, dst2.Cols * 2, cv.MatType.CV_8UC3)
'        siftCS.RunCS(task.leftView, task.rightView, doubleSize, options.useBFMatcher, options.pointCount)

'        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
'        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

'        labels(2) = If(options.useBFMatcher, "BF Matcher output", "Flann Matcher output")
'    End Sub
'End Class









'Public Class Sift_Basics_MT : Inherits VB_Parent
'    Dim siftCS As New CS_Classes.CS_SiftBasics
'    Dim siftBasics As New Sift_Basics
'    Dim numPointSlider As System.Windows.Forms.TrackBar
'    Dim grid As New Grid_Rectangles
'    Public Sub New()
'        FindSlider("Grid Cell Width").Maximum = dst2.Cols * 2
'        FindSlider("Grid Cell Width").Value = dst2.Cols * 2
'        FindSlider("Grid Cell Height").Value = 10

'        numPointSlider = FindSlider("Points to Match")
'        numPointSlider.Value = 1

'        desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static bfRadio = findRadio("Use BF Matcher")
'        grid.Run(src)

'        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
'        Dim numFeatures = numPointSlider.Value
'        Parallel.ForEach(task.gridList,
'        Sub(roi)
'            Dim left = task.leftView(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
'            Dim right = task.rightView(roi).Clone()
'            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
'            Dim dstTmp = output(dstROI).Clone()
'            siftCS.RunCS(left, right, dstTmp, bfRadio.Checked, numFeatures)
'            dstTmp.CopyTo(output(dstROI))
'        End Sub)

'        dst2 = output(New cv.Rect(0, 0, src.Width, src.Height))
'        dst3 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

'        labels(2) = If(bfRadio.Checked, "BF Matcher output", "Flann Matcher output")
'    End Sub
'End Class







''https://docs.opencv.org/4.x/da/df5/tutorial_py_sift_intro.html
'Public Class Sift_Points : Inherits VB_Parent
'    Dim sift As New CS_Classes.CS_SiftPoints
'    Dim options As New Options_Sift
'    Public stablePoints As New List(Of cv.Point)
'    Public Sub New()
'        desc = "Keypoints found in SIFT"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()
'        dst2 = src.Clone
'        sift.RunCS(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), options.pointCount)

'        Dim newPoints As New List(Of cv.Point)
'        For i = 0 To sift.keypoints.Count - 1
'            Dim pt = sift.keypoints(i).Pt
'            drawCircle(dst2,pt, task.dotSize, cv.Scalar.Yellow)
'            newPoints.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
'        Next

'        dst3 = src.Clone
'        Static history = New List(Of List(Of cv.Point))
'        If task.optionsChanged Then history.clear()
'        history.Add(newPoints)
'        stablePoints.Clear()
'        For Each pt In newPoints
'            Dim missing = False
'            For Each ptList In history
'                If ptList.Contains(pt) = False Then
'                    missing = True
'                    Exit For
'                End If
'            Next
'            If missing = False Then
'                drawCircle(dst3,pt, task.dotSize, cv.Scalar.Yellow)
'                stablePoints.Add(pt)
'            End If
'        Next
'        If history.count >= task.frameHistoryCount Then history.removeat(0)
'        labels(3) = "Sift keypoints that are present in the last " + CStr(task.frameHistoryCount) + "  frames."
'    End Sub
'End Class







'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Sift_Slices : Inherits VB_Parent
'    Dim siftCS As New CS_Classes.CS_SiftBasics
'    Dim options As New Options_Sift
'    Public Sub New()
'        FindSlider("Points to Match").Value = 1
'        desc = "Compare 2 images to get a homography but limit the search to a slice of the image."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        Dim doubleSize As New cv.Mat(dst2.Rows, dst2.Cols * 2, cv.MatType.CV_8UC3)
'        Dim stepsize = options.stepSize
'        For i = 0 To dst2.Height - 1 Step stepsize
'            If i + stepsize >= dst2.Height Then stepsize = dst2.Height - i - 1
'            Dim r1 = New cv.Rect(0, i, dst2.Width, stepsize)
'            Dim r2 = New cv.Rect(0, i, dst2.Width * 2, stepsize)
'            siftCS.RunCS(task.leftView(r1), task.rightView(r1), doubleSize(r2), options.useBFMatcher, options.pointCount)
'        Next

'        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
'        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

'        labels(2) = If(options.useBFMatcher, "BF Matcher output", "Flann Matcher output")
'    End Sub
'End Class




'' https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
'Public Class MatrixInverse_Basics_CS : Inherits VB_Parent
'    Public matrix As New MatrixInverse ' NOTE: C# class
'    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
'    Dim defaultBVector() As Double = {12, 7, 7, 13}
'    Dim input As cv.Mat
'    Public Sub New()
'        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
'        desc = "Manually invert a matrix"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If input.Width <> input.Height Then
'            setTrueText("The src matrix must be square!")
'            Exit Sub
'        End If

'        If standaloneTest() Then matrix.bVector = defaultBVector

'        Dim result = matrix.RunCS(input) ' C# class Run - see MatrixInverse.cs file...

'        Dim outstr = printMatrixResults(input, result)
'        setTrueText(outstr + vbCrLf + "Intermediate results are optionally available in the console log.")
'    End Sub
'End Class



'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Surf_Basics : Inherits VB_Parent
'    Public CS_SurfBasics As New CS_SurfBasics
'    Public options As New Options_SURF
'    Public Sub New()
'        desc = "Compare 2 images to get a homography.  We will use left and right images."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        Dim doubleSize As New cv.Mat
'        CS_SurfBasics.RunCS(task.leftView, task.rightView, doubleSize, options.surfThreshold, options.useBFMatch)

'        doubleSize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
'        doubleSize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)
'        labels(2) = If(options.useBFMatch, "BF Matcher output", "Flann Matcher output")
'        If CS_SurfBasics.keypoints1 IsNot Nothing Then labels(2) += " " + CStr(CS_SurfBasics.keypoints1.Count)
'    End Sub
'End Class





'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Surf_Draw : Inherits VB_Parent
'    Dim surf As New Surf_Basics
'    Public Sub New()
'        surf.CS_SurfBasics.drawPoints = False
'        desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        surf.Run(src)

'        dst2 = If(task.leftView.Channels = 1, task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.leftView)
'        dst3 = If(task.rightView.Channels = 1, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView)

'        Dim keys1 = surf.CS_SurfBasics.keypoints1
'        Dim keys2 = surf.CS_SurfBasics.keypoints2

'        For i = 0 To keys1.Count - 1
'            drawCircle(dst2,keys1(i).Pt, task.dotSize + 2, cv.Scalar.Red)
'        Next

'        Dim matchCount As Integer
'        Dim range = surf.options.verticalRange
'        For i = 0 To keys1.Count - 1
'            Dim pt = keys1(i).Pt

'            For j = 0 To keys2.Count - 1
'                If Math.Abs(keys2(j).Pt.Y - pt.Y) < range Then
'                    drawCircle(dst3,keys2(j).Pt, task.dotSize + 2, task.highlightColor)
'                    keys2(j).Pt.Y = -1 ' so we don't match it again.
'                    matchCount += 1
'                End If
'            Next
'        Next

'        ' mark those that were not
'        For i = 0 To keys2.Count - 1
'            Dim pt = keys2(i).Pt
'            If pt.Y <> -1 Then drawCircle(dst3,keys2(i).Pt, task.dotSize + 2, cv.Scalar.Red)
'        Next
'        labels(3) = "Yellow matched left to right = " + CStr(matchCount) + ". Red is unmatched."
'    End Sub
'End Class







'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Surf_MatchBad : Inherits VB_Parent
'    Dim CS_SurfBasics As New CS_SurfBasics
'    Dim ptLeft As New SortedList(Of Integer, cv.Point)
'    Dim ptRight As New SortedList(Of Integer, cv.Point)
'    Dim options As New Options_SURF
'    Public Sub New()
'        CS_SurfBasics.drawPoints = False
'        desc = "Use left and right images as input to SURF and restrict matches to identical Y coordinate"
'    End Sub
'    Private Function prepPoints(input As cv.KeyPoint(), ByRef yList As List(Of Integer)) As SortedList(Of Integer, cv.Point)
'        Dim ptList As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
'        For i = 0 To input.Count - 1
'            Dim pt = input(i).Pt
'            Dim y = Math.Round(pt.Y)
'            ptList.Add(y, New cv.Point(pt.X, y))
'        Next

'        Dim removeList As New List(Of Integer)
'        For Each kpt In ptList
'            Dim y = kpt.Key
'            If yList.Contains(y) Then
'                Dim index = yList.IndexOf(y)
'                If removeList.Contains(index) = False Then
'                    removeList.Add(index)
'                    removeList.Add(index + 1)
'                End If
'            End If
'            yList.Add(y)
'        Next

'        'For i = removeList.Count - 1 To 0 Step -1
'        '    Dim index = removeList(i)
'        '    ptList.RemoveAt(index)
'        '    yList.RemoveAt(index)
'        'Next
'        Return ptList
'    End Function
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()

'        CS_SurfBasics.RunCS(task.leftView, task.rightView, New cv.Mat, options.surfThreshold, options.useBFMatch)
'        If CS_SurfBasics.keypoints1 Is Nothing Then Exit Sub

'        Dim doublesize As New cv.Mat
'        cv.Cv2.HConcat(task.leftView, task.rightView, doublesize)

'        Dim lefties As New List(Of Integer)
'        Dim righties As New List(Of Integer)
'        ptLeft = prepPoints(CS_SurfBasics.keypoints1, lefties)
'        ptRight = prepPoints(CS_SurfBasics.keypoints2, righties)

'        For Each kpt In ptLeft
'            Dim y = kpt.Key
'            Dim index = righties.IndexOf(y)
'            If index >= 0 Then
'                Dim key = ptRight.ElementAt(index).Key
'                Dim p2 = ptRight.ElementAt(index).Value
'                p2 = New cv.Point(p2.X + dst2.Width, p2.Y)
'                doublesize.Line(kpt.Value, p2, task.highlightColor, task.lineWidth)
'            End If
'        Next

'        doublesize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
'        doublesize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)
'        labels(2) = If(options.useBFMatch, "BF Matcher output", "Flann Matcher output")
'        labels(2) += " " + CStr(CS_SurfBasics.keypoints1.Count) + " key points found in the left view"
'        labels(3) = " " + CStr(CS_SurfBasics.keypoints2.Count) + " key points found in the right view"
'    End Sub
'End Class





'' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
'Public Class Surf_Match : Inherits VB_Parent
'    Dim surf As New Surf_Basics
'    Public Sub New()
'        surf.CS_SurfBasics.drawPoints = False
'        desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        surf.Run(src)

'        Dim doublesize As New cv.Mat
'        cv.Cv2.HConcat(task.leftView, task.rightView, doublesize)

'        Dim keys1 = surf.CS_SurfBasics.keypoints1
'        Dim keys2 = surf.CS_SurfBasics.keypoints2

'        For i = 0 To keys1.Count - 1
'            drawCircle(dst2,keys1(i).Pt, task.dotSize + 3, cv.Scalar.Red)
'        Next

'        Dim matchCount As Integer
'        Dim range = surf.options.verticalRange
'        For i = 0 To keys1.Count - 1
'            Dim p1 = keys1(i).Pt
'            For j = 0 To keys2.Count - 1
'                Dim p2 = keys2(j).Pt
'                p2 = New cv.Point2f(p2.X + dst2.Width, p2.Y)
'                If Math.Abs(keys2(j).Pt.Y - p1.Y) < range Then
'                    doublesize.Line(p1, p2, task.highlightColor, task.lineWidth)
'                    keys2(j).Pt.Y = -1 ' so we don't match it again.
'                    matchCount += 1
'                End If
'            Next
'        Next

'        doublesize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
'        doublesize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)

'        labels(2) = CStr(surf.CS_SurfBasics.keypoints1.Count) + " key points found in the left view - " +
'                    If(surf.options.useBFMatch, "BF Matcher output ", "Flann Matcher output ")
'        labels(3) = CStr(surf.CS_SurfBasics.keypoints2.Count) + " key points found in the right view " +
'                    CStr(matchCount) + " were matched."
'    End Sub
'End Class





'Public Class DNN_Caffe_CS : Inherits VB_Parent
'    Dim caffeCS As New CS_Classes.DNN
'    Public Sub New()
'        labels(3) = "Input Image"
'        desc = "Download and use a Caffe database"

'        Dim protoTxt = task.homeDir + "Data/bvlc_googlenet.prototxt"
'        Dim modelFile = task.homeDir + "Data/bvlc_googlenet.caffemodel"
'        Dim synsetWords = task.homeDir + "Data/synset_words.txt"
'        caffeCS.initialize(protoTxt, modelFile, synsetWords)
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Dim image = cv.Cv2.ImRead(task.homeDir + "Data/space_shuttle.jpg")
'        Dim str = caffeCS.RunCS(image)
'        dst3 = image.Resize(dst3.Size())
'        setTrueText(str)
'    End Sub
'End Class




'' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
'Public Class OilPaint_Manual : Inherits VB_Parent
'    Dim oilPaint As New CS_Classes.OilPaintManual
'    Public options As New Options_OilPaint
'    Public Sub New()
'        task.drawRect = New cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8)
'        labels(3) = "Selected area only"
'        desc = "Alter an image so it appears painted by a pointilist.  Select a region of interest to paint."
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Options.RunVB()
'        Dim roi = task.drawRect
'        src.CopyTo(dst2)
'        oilPaint.Start(src(roi), dst2(roi), options.kernelSize, options.intensity)
'        dst3 = src.EmptyClone.SetTo(0)
'        Dim factor As Integer = Math.Min(Math.Floor(dst3.Width / roi.Width), Math.Floor(dst3.Height / roi.Height))
'        Dim s = New cv.Size(roi.Width * factor, roi.Height * factor)
'        cv.Cv2.Resize(dst2(roi), dst3(New cv.Rect(0, 0, s.Width, s.Height)), s)
'    End Sub
'End Class






'' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
'Public Class OilPaint_Cartoon : Inherits VB_Parent
'    Dim oil As New OilPaint_Manual
'    Dim Laplacian As New Edge_Laplacian
'    Public Sub New()
'        task.drawRect = New cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8)
'        labels(2) = "OilPaint_Cartoon"
'        labels(3) = "Laplacian Edges"
'        desc = "Alter an image so it appears more like a cartoon"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Dim roi = task.drawRect
'        Laplacian.Run(src)
'        dst3 = Laplacian.dst2

'        oil.Run(src)
'        dst2 = oil.dst2

'        Dim vec000 = New cv.Vec3b(0, 0, 0)
'        For y = 0 To roi.Height - 1
'            For x = 0 To roi.Width - 1
'                If dst3(roi).Get(Of Byte)(y, x) >= oil.options.threshold Then
'                    dst2(roi).Set(Of cv.Vec3b)(y, x, vec000)
'                End If
'            Next
'        Next
'    End Sub
'End Class
'
'
'
'
'
'Public Class Sieve_Basics : Inherits VB_Parent
'Dim printer As New Sieve_BasicsVB
'Dim sieve As New CS_Classes.Sieve
'Public Sub New()
'    desc = "Implement the Sieve of Eratothenes in C#"
'End Sub
'Public Sub RunVB(src As cv.Mat)
'    Static countSlider = FindSlider("Count of desired primes")
'    setTrueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.Value)))
'End Sub
'End Class




'' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
'Public Class SLR_Basics : Inherits VB_Parent
'    Public input As New SLR_Data
'    Dim slr As New CS_Classes.SLR
'    Dim plot As New Plot_Basics_CPP
'    Public Sub New()
'        If standaloneTest() Then
'            input.Run(dst2)
'            labels(2) = "Sample data input"
'        End If

'        If sliders.Setup(traceName) Then
'            sliders.setupTrackBar("Approximate accuracy (tolerance) X100", 1, 1000, 30)
'            sliders.setupTrackBar("Simple moving average window size", 1, 100, 10)
'        End If
'        desc = "Segmented Linear Regression example"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static toleranceSlider = FindSlider("Approximate accuracy (tolerance) X100")
'        Static movingAvgSlider = FindSlider("Simple moving average window size")
'        Dim tolerance = toleranceSlider.Value / 100
'        Dim halfLength = movingAvgSlider.Value

'        Dim resultX As New List(Of Double)
'        Dim resultY As New List(Of Double)

'        slr.SegmentedRegressionFast(input.dataX, input.dataY, tolerance, halfLength, resultX, resultY)

'        labels(2) = "Tolerance = " + CStr(tolerance) + " and moving average window = " + CStr(halfLength)
'        If resultX.Count > 0 Then
'            plot.srcX = input.dataX
'            plot.srcY = input.dataY
'            plot.Run(src)
'            dst2 = plot.dst2.Clone

'            plot.srcX = resultX
'            plot.srcY = resultY
'            plot.Run(src)
'            dst3 = plot.dst2
'        Else
'            dst2.SetTo(0)
'            dst3.SetTo(0)
'            setTrueText(labels(2) + " yielded no results...")
'        End If
'        If standaloneTest() = False Then
'            input.dataX.Clear()
'            input.dataY.Clear()
'        End If
'    End Sub
'End Class







'Public Class SLR_Image : Inherits VB_Parent
'    Public slr As New SLR_Basics
'    Public hist As New Hist_Basics
'    Public Sub New()
'        labels(2) = "Original data"
'        desc = "Run Segmented Linear Regression on grayscale image data - just an experiment"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        hist.Run(src)
'        dst2 = hist.dst2
'        For i = 0 To hist.histogram.Rows - 1
'            slr.input.dataX.Add(i)
'            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
'        Next
'        slr.Run(src)
'        dst3 = slr.dst3
'    End Sub
'End Class









'Public Class SLR_TrendCompare : Inherits VB_Parent
'    Public slr As Object = New SLR_Image
'    Dim valList As New List(Of Single)
'    Dim barMidPoint As Integer
'    Dim lastPoint As cv.Point2f
'    Public resultingPoints As New List(Of cv.Point2f)
'    Public Sub New()
'        desc = "Find trends by filling in short histogram gaps in the given image's histogram."
'    End Sub
'    Private Sub connectLine(i As Integer)
'        Dim p1 = New cv.Point2f(barMidPoint + dst2.Width * i / valList.Count, dst2.Height - dst2.Height * valList(i) / slr.hist.plot.maxValue)
'        resultingPoints.Add(p1)
'        drawLine(dst2,lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
'        lastPoint = p1
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        labels(2) = "Histogram with Yellow line showing the trends"
'        slr.hist.plot.backcolor = cv.Scalar.Red
'        slr.Run(src)
'        dst2 = slr.dst2
'        dst3 = slr.dst3

'        Dim indexer = slr.hist.histogram.GetGenericIndexer(Of Single)()
'        valList = New List(Of Single)
'        For i = 0 To slr.hist.histogram.Rows - 1
'            valList.Add(indexer(i))
'        Next
'        barMidPoint = dst2.Width / valList.Count / 2

'        If valList.Count < 2 Then Exit Sub
'        slr.hist.plot.maxValue = valList.Max
'        lastPoint = New cv.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / slr.hist.plot.maxValue)
'        resultingPoints.Clear()
'        resultingPoints.Add(lastPoint)
'        For i = 1 To valList.Count - 2
'            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then valList(i) = (valList(i - 1) + valList(i + 1)) / 2
'            connectLine(i)
'        Next
'        connectLine(valList.Count - 1)
'    End Sub
'End Class







''https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
'Public Class Voronoi_Basics : Inherits VB_Parent
'    Public vDemo As New CS_Classes.VoronoiDemo
'    Public random As New Random_Basics
'    Public Sub New()
'        labels(2) = "Ordered list output for Voronoi algorithm"
'        FindSlider("Random Pixel Count").Maximum = 100
'        desc = "Use the ordered list method to find the Voronoi segments"
'    End Sub
'    Public Sub vDisplay(ByRef dst As cv.Mat, points As List(Of cv.Point2f), color As cv.Scalar)
'        dst = dst.Normalize(255).ConvertScaleAbs(255)
'        dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

'        For Each pt In points
'            drawCircle(dst, pt, task.dotSize, color)
'        Next
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        If task.heartBeat Then random.Run(empty)
'        vDemo.RunCS(dst2, random.pointList)
'        vDisplay(dst2, random.pointList, cv.Scalar.Yellow)
'    End Sub
'End Class






''https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
'Public Class Voronoi_Compare : Inherits VB_Parent
'    Dim basics As New Voronoi_Basics
'    Public random As New Random_Basics
'    Public Sub New()
'        FindSlider("Random Pixel Count").Maximum = 150
'        FindSlider("Random Pixel Count").Value = 150
'        labels = {"", "", "Brute Force method - check log timings", "Ordered List method - check log for timing"}
'        desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        random.Run(empty)
'        basics.vDemo.RunCS(dst2, random.pointList, True)
'        basics.vDisplay(dst2, random.pointList, cv.Scalar.Yellow)

'        basics.vDemo.RunCS(dst3, random.pointList, False)
'        basics.vDisplay(dst3, random.pointList, cv.Scalar.Yellow)
'    End Sub
'End Class





''https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
'Public Class Voronoi_CPP : Inherits VB_Parent
'    Dim vDemo As New Voronoi_Basics
'    Public Sub New()
'        cPtr = VoronoiDemo_Open(task.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
'        desc = "Use the C++ version of the Voronoi code"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static countSlider = FindSlider("Random Pixel Count")
'        If task.heartBeat Then vDemo.random.Run(empty)
'        Dim ptList = vbFloat2Int(vDemo.random.pointList)
'        Dim handleSrc = GCHandle.Alloc(ptList.ToArray, GCHandleType.Pinned)
'        Dim imagePtr = VoronoiDemo_Run(cPtr, handleSrc.AddrOfPinnedObject(), countSlider.Value, dst2.Width, dst2.Height)
'        handleSrc.Free()

'        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_32F, imagePtr).Clone
'        vDemo.vDisplay(dst2, vDemo.random.pointList, cv.Scalar.Yellow)
'    End Sub
'    Public Sub Close()
'        If cPtr <> 0 Then cPtr = VoronoiDemo_Close(cPtr)
'    End Sub
'End Class











'Public Class Edge_Motion : Inherits VB_Parent
'    Dim diff As New Diff_Basics
'    Dim edges As New Edge_Sobel
'    Public Sub New()
'        desc = "Measure camera motion using Sobel and Diff from last frame."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        edges.Run(src)
'        diff.Run(edges.dst2)

'        dst2 = diff.dst2
'        dst3 = dst2 And edges.dst2
'        If task.quarterBeat Then labels(3) = $"{dst3.CountNonZero} pixels overlap between Sobel edges and diff with last Sobel edges."
'    End Sub
'End Class








'Public Class Edge_NoDepth : Inherits VB_Parent
'    Dim edges As New Edge_Sobel
'    Dim blur As New Blur_Basics
'    Public Sub New()
'        desc = "Find the edges in regions without depth."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        src = ShowPalette(src)
'        edges.Run(src)

'        dst2.SetTo(0)
'        blur.Run(task.noDepthMask)
'        edges.dst2.CopyTo(dst2, blur.dst2)
'        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
'    End Sub
'End Class







'Public Class OEX_Sobel_Demo : Inherits VB_Parent
'    Dim sobel As New Edge_Sobel
'    Public Sub New()
'        desc = "OpenCV Example Sobel_Demo became Edge_Sobel algorithm."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        sobel.Run(src)
'        dst2 = sobel.dst2
'        dst3 = sobel.dst3
'        labels = sobel.labels
'    End Sub
'End Class










'Public Class Hist_SLR : Inherits VB_Parent
'    Public slr As New SLR_Basics
'    Public hist As New Hist_Basics
'    Public Sub New()
'        labels = {"", "", "Original data", "Segmented Linear Regression (SLR) version of the same data.  Red line is zero."}
'        desc = "Run Segmented Linear Regression on depth data"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        hist.Run(src)
'        hist.histogram.Set(Of Single)(0, 0, 0)
'        dst2 = hist.dst2
'        For i = 0 To hist.histogram.Rows - 1
'            slr.input.dataX.Add(i)
'            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
'        Next
'        slr.Run(src)
'        dst3 = slr.dst3
'    End Sub
'End Class