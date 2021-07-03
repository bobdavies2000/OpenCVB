Imports cv = OpenCvSharp
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics : Inherits VBparent
    Dim siftCS As New CS_SiftBasics
    Dim lrView As New LeftRight_Basics
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Use BF Matcher"
            radio.check(1).Text = "Use Flann Matcher"
            radio.check(0).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Points to Match", 1, 1000, 200)
        End If
        task.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        lrView.RunClass(src)

        Dim doubleSize As New cv.Mat(lrView.dst2.Rows, lrView.dst2.Cols * 2, cv.MatType.CV_8UC3)

        siftCS.Run(lrView.dst2, lrView.dst3, doubleSize, radio.check(0).Checked, sliders.trackbar(0).Value)

        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)

        labels(2) = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class




Public Class Sift_Basics_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim siftCS As New CS_SiftBasics
    Dim siftBasics As New Sift_Basics
    Dim lrView As New LeftRight_Basics
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Maximum = task.color.Cols * 2
        gridWidthSlider.Value = task.color.Cols * 2 ' we are just taking horizontal slices of the image.
        gridHeightSlider.Value = 10

        grid.RunClass(Nothing)

        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        task.desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.RunClass(Nothing)

        lrView.RunClass(src)

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim left = lrView.dst2(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = lrView.dst3(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.Run(left, right, dstTmp, siftBasics.radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst2 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst3 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

        labels(2) = If(siftBasics.radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class
