Imports cv = OpenCvSharp
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics : Inherits VBparent
    Dim siftCS As New CS_SiftBasics
    Dim lrView As LeftRightView_BrightnessContrast
    Public Sub New()
        lrView = New LeftRightView_BrightnessContrast

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Use BF Matcher"
            radio.check(1).Text = "Use Flann Matcher"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Points to Match", 1, 1000, 200)
        End If
        task.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(src as cv.Mat)

        lrView.Run(src)

        Dim doubleSize As New cv.Mat(lrView.dst1.Rows, lrView.dst1.Cols * 2, cv.MatType.CV_8UC3)

        siftCS.Run(lrView.dst1, lrView.dst2, doubleSize, radio.check(0).Checked, sliders.trackbar(0).Value)

        doubleSize(New cv.Rect(0, 0, dst1.Width, dst1.Height)).CopyTo(dst1)
        doubleSize(New cv.Rect(dst1.Width, 0, dst1.Width, dst1.Height)).CopyTo(dst2)

        label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class




Public Class Sift_Basics_MT : Inherits VBparent
    Dim grid As Thread_Grid
    Dim siftCS As New CS_SiftBasics
    Dim siftBasics As Sift_Basics
    Dim lrView As LeftRightView_BrightnessContrast
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        lrView = New LeftRightView_BrightnessContrast

        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Maximum = task.color.Cols * 2
        gridWidthSlider.Value = task.color.Cols * 2 ' we are just taking horizontal slices of the image.
        gridHeightSlider.Value = 10

        grid.Run(Nothing)

        siftBasics = New Sift_Basics
        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        task.desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
    End Sub
    Public Sub Run(src as cv.Mat)
        grid.Run(Nothing)

        lrView.Run(src)

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)
        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim left = lrView.dst1(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = lrView.dst2(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.Run(left, right, dstTmp, siftBasics.radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst1 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst2 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

        label1 = If(siftBasics.radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class
