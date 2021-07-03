Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics : Inherits VBparent
    Public CS_SurfBasics As New CS_SurfBasics
    Dim fisheye As New FishEye_Rectified
    Public srcLeft As New cv.Mat
    Public srcRight As New cv.Mat
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "Use BF Matcher"
            radio.check(1).Text = "Use Flann Matcher"
            radio.check(0).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Hessian threshold", 1, 5000, 2000)
        End If

        task.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        srcLeft = task.leftView
        srcRight = task.rightView
        Dim doubleSize As New cv.Mat
        CS_SurfBasics.Run(srcLeft, srcRight, doubleSize, sliders.trackbar(0).Value, radio.check(0).Checked)

        doubleSize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)
        labels(2) = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
        If CS_SurfBasics.keypoints1 IsNot Nothing Then labels(2) += " " + CStr(CS_SurfBasics.keypoints1.Count)
    End Sub
End Class






' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_BasicsVB : Inherits VBparent
    Dim surf As New Surf_Basics
    Dim fisheye As New FishEye_Rectified
    Public Sub New()
        task.desc = "Use left and right views to match points in horizontal slices."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        surf.RunClass(src)
        dst2 = surf.dst2
        dst3 = surf.dst3
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_DrawMatchManual_CS : Inherits VBparent
    Dim surf As New Surf_Basics
    Public Sub New()
        surf.CS_SurfBasics.drawPoints = False

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Surf Vertical Range to Search", 0, 50, 10)
        End If
        task.desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        surf.RunClass(src)
        dst2 = surf.srcLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = surf.srcRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2

        For i = 0 To keys1.Count - 1
            dst2.Circle(keys1(i).Pt, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim matchCount As Integer
        For i = 0 To keys1.Count - 1
            Dim pt = keys1(i).Pt
            For j = 0 To keys2.Count - 1
                If Math.Abs(keys2(j).Pt.X - pt.X) < sliders.trackbar(0).Value And Math.Abs(keys2(j).Pt.Y - pt.Y) < sliders.trackbar(0).Value Then
                    dst3.Circle(keys2(j).Pt, task.dotSize + 3, cv.Scalar.Yellow, -1, task.lineType)
                    keys2(j).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next
        ' mark those that were not
        For i = 0 To keys2.Count - 1
            Dim pt = keys2(i).Pt
            If pt.Y <> -1 Then dst3.Circle(keys2(i).Pt, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        Next
        labels(3) = "Yellow matched left to right = " + CStr(matchCount) + ". Red is unmatched."
    End Sub
End Class


