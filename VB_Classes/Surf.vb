Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics
    Inherits VBparent
    Public CS_SurfBasics As New CS_SurfBasics
    Dim fisheye As FishEye_Rectified
    Public srcLeft As New cv.Mat
    Public srcRight As New cv.Mat
    Public Sub New()
        initParent()
        fisheye = New FishEye_Rectified()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Use BF Matcher"
            radio.check(1).Text = "Use Flann Matcher"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Hessian threshold", 1, 5000, 2000)
        End If

        task.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        srcLeft = task.leftView
        srcRight = task.rightView
        Dim doubleSize As New cv.Mat
        CS_SurfBasics.Run(srcLeft, srcRight, doubleSize, sliders.trackbar(0).Value, radio.check(0).Checked)

        doubleSize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst1)
        doubleSize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst2)
        label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
        If CS_SurfBasics.keypoints1 IsNot Nothing Then label1 += " " + CStr(CS_SurfBasics.keypoints1.Count)
    End Sub
End Class






' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_BasicsVB
    Inherits VBparent
    Dim surf As Surf_Basics
    Dim fisheye As FishEye_Rectified
    Public Sub New()
        initParent()
        fisheye = New FishEye_Rectified()
        surf = New Surf_Basics()
        task.desc = "Use left and right views to match points in horizontal slices."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        surf.src = src
        surf.Run()
        dst1 = surf.dst1
        dst2 = surf.dst2
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_DrawMatchManual_CS
    Inherits VBparent
    Dim surf As Surf_Basics
    Public Sub New()
        initParent()
        surf = New Surf_Basics()
        surf.CS_SurfBasics.drawPoints = False

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Surf Vertical Range to Search", 0, 50, 10)
        End If
        task.desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        surf.src = src
        surf.Run()
        dst1 = surf.srcLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = surf.srcRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2

        For i = 0 To keys1.Count - 1
            dst1.Circle(keys1(i).Pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next

        Dim matchCount As Integer
        For i = 0 To keys1.Count - 1
            Dim pt = keys1(i).Pt
            For j = 0 To keys2.Count - 1
                If Math.Abs(keys2(j).Pt.X - pt.X) < sliders.trackbar(0).Value And Math.Abs(keys2(j).Pt.Y - pt.Y) < sliders.trackbar(0).Value Then
                    dst2.Circle(keys2(j).Pt, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    keys2(j).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next
        ' mark those that were not
        For i = 0 To keys2.Count - 1
            Dim pt = keys2(i).Pt
            If pt.Y <> -1 Then dst2.Circle(keys2(i).Pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
        label2 = "Yellow matched left to right = " + CStr(matchCount) + ". Red is unmatched."
    End Sub
End Class

