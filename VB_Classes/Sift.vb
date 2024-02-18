Imports cv = OpenCvSharp
' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics : Inherits VB_Algorithm
    Dim siftCS As New CS_Classes.CS_SiftBasics
    Dim options As New Options_Sift
    Public Sub New()
        desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim doubleSize As New cv.Mat(dst2.Rows, dst2.Cols * 2, cv.MatType.CV_8UC3)
        siftCS.RunCS(task.leftView, task.rightView, doubleSize, options.useBFMatcher, options.pointCount)

        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

        labels(2) = If(options.useBFMatcher, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class









Public Class Sift_Basics_MT : Inherits VB_Algorithm
    Dim siftCS As New CS_Classes.CS_SiftBasics
    Dim siftBasics As New Sift_Basics
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Dim grid As New Grid_Rectangles
    Public Sub New()
        findSlider("Grid Cell Width").Maximum = dst2.Cols * 2
        findSlider("Grid Cell Width").Value = dst2.Cols * 2
        findSlider("Grid Cell Height").Value = 10

        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static bfRadio = findRadio("Use BF Matcher")
        grid.Run(src)

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim left = task.leftView(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = task.rightView(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.RunCS(left, right, dstTmp, bfRadio.Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst2 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst3 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

        labels(2) = If(bfRadio.Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class







'https://docs.opencv.org/4.x/da/df5/tutorial_py_sift_intro.html
Public Class Sift_Points : Inherits VB_Algorithm
    Dim sift As New CS_Classes.CS_SiftPoints
    Dim options As New Options_Sift
    Public stablePoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Keypoints found in SIFT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        sift.RunCS(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), options.pointCount)

        Dim newPoints As New List(Of cv.Point)
        For i = 0 To sift.keypoints.Count - 1
            Dim pt = sift.keypoints(i).Pt
            dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            newPoints.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
        Next

        dst3 = src.Clone
        Static history = New List(Of List(Of cv.Point))
        If task.optionsChanged Then history.clear()
        history.Add(newPoints)
        stablePoints.Clear()
        For Each pt In newPoints
            Dim missing = False
            For Each ptList In history
                If ptList.Contains(pt) = False Then
                    missing = True
                    Exit For
                End If
            Next
            If missing = False Then
                dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                stablePoints.Add(pt)
            End If
        Next
        If history.count >= task.frameHistoryCount Then history.removeat(0)
        labels(3) = "Sift keypoints that are present in the last " + CStr(task.frameHistoryCount) + "  frames."
    End Sub
End Class







' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Slices : Inherits VB_Algorithm
    Dim siftCS As New CS_Classes.CS_SiftBasics
    Dim options As New Options_Sift
    Public Sub New()
        findSlider("Points to Match").Value = 1
        desc = "Compare 2 images to get a homography but limit the search to a slice of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim doubleSize As New cv.Mat(dst2.Rows, dst2.Cols * 2, cv.MatType.CV_8UC3)
        Dim stepsize = options.stepSize
        For i = 0 To dst2.Height - 1 Step stepsize
            If i + stepsize >= dst2.Height Then stepsize = dst2.Height - i - 1
            Dim r1 = New cv.Rect(0, i, dst2.Width, stepsize)
            Dim r2 = New cv.Rect(0, i, dst2.Width * 2, stepsize)
            siftCS.RunCS(task.leftView(r1), task.rightView(r1), doubleSize(r2), options.useBFMatcher, options.pointCount)
        Next

        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

        labels(2) = If(options.useBFMatcher, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class