Imports cv = OpenCvSharp
Imports System.Collections.Generic

Public Class KAZE_KeypointsKAZE_CS
    Inherits VBparent
    Dim CS_Kaze As New CS_Classes.Kaze_Basics
    Public Sub New()
        initParent()
        task.desc = "Find keypoints using KAZE algorithm."
		task.rank = 1
        label1 = "KAZE key points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        CS_Kaze.GetKeypoints(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        src.CopyTo(dst1)
        For i = 0 To CS_Kaze.kazeKeyPoints.Count - 1
            dst1.Circle(CS_Kaze.kazeKeyPoints.ElementAt(i).Pt, 3, cv.Scalar.Red, -1, task.lineType)
        Next
    End Sub
End Class




Public Class KAZE_KeypointsAKAZE_CS
    Inherits VBparent
    Dim CS_AKaze As New CS_Classes.AKaze_Basics
    Public Sub New()
        initParent()
        task.desc = "Find keypoints using AKAZE algorithm."
		task.rank = 1
        label1 = "AKAZE key points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        CS_AKaze.GetKeypoints(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        src.CopyTo(dst1)
        For i = 0 To CS_AKaze.akazeKeyPoints.Count - 1
            dst1.Circle(CS_AKaze.akazeKeyPoints.ElementAt(i).Pt, 3, cv.Scalar.Red, -1, task.lineType)
        Next
    End Sub
End Class



Public Class KAZE_Sample_CS
    Inherits VBparent
    Dim box As New cv.Mat
    Dim box_in_scene As New cv.Mat
    Dim CS_Kaze As New CS_Classes.Kaze_Sample
    Public Sub New()
        initParent()
        box = cv.Cv2.ImRead(task.parms.homeDir + "Data/box.png", cv.ImreadModes.Color)
        box_in_scene = cv.Cv2.ImRead(task.parms.homeDir + "Data/box_in_scene.png", cv.ImreadModes.Color)
        task.desc = "Match keypoints in 2 photos."
		task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim result = CS_Kaze.Run(box, box_in_scene)
        dst1 = result.Resize(src.Size())
    End Sub
End Class



Public Class KAZE_Match_CS
    Inherits VBparent
    Dim red As LeftRightView_Basics
    Dim CS_Kaze As New CS_Classes.Kaze_Sample
    Public Sub New()
        initParent()
        red = New LeftRightView_Basics()
        red.sliders.trackbar(0).Value = 45
        task.desc = "Match keypoints in the left and right images."
		task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        red.Run()
        dst1 = red.dst1
        dst2 = red.dst2
        Dim result = CS_Kaze.Run(dst1, dst2)
        result(New cv.Rect(0, 0, dst1.Width, dst1.Height)).CopyTo(dst1)
        result(New cv.Rect(dst1.Width, 0, dst1.Width, dst1.Height)).CopyTo(dst2)
    End Sub
End Class




Public Class KAZE_LeftAligned_CS
    Inherits VBparent
    Dim CS_KazeLeft As New CS_Classes.Kaze_Basics
    Dim CS_KazeRight As New CS_Classes.Kaze_Basics
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Max number of points to match", 1, 300, 100)
            sliders.setupTrackBar(1, "When matching, max possible distance", 1, 200, 100)
        End If

        task.desc = "Match keypoints in the left and right images but display it as movement in the right image."
		task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        CS_KazeLeft.GetKeypoints(task.leftView)
        CS_KazeRight.GetKeypoints(task.rightView)

        dst1 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim topDistance = sliders.trackbar(1).Value
        Dim maxPoints = sliders.trackbar(0).Value
        Dim maxCount = Math.Min(maxPoints, Math.Min(CS_KazeRight.kazeKeyPoints.Count, CS_KazeLeft.kazeKeyPoints.Count))
        For i = 0 To maxCount - 1
            Dim pt1 = CS_KazeRight.kazeKeyPoints.ElementAt(i)
            Dim minIndex As Integer
            Dim minDistance As Single = Single.MaxValue
            For j = 0 To CS_KazeLeft.kazeKeyPoints.Count - 1
                Dim pt2 = CS_KazeLeft.kazeKeyPoints.ElementAt(j)
                ' the right image point must be to the right of the left image point (pt1 X is < pt2 X) and at about the same Y
                If Math.Abs(pt2.Pt.Y - pt1.Pt.Y) < 2 And pt1.Pt.X < pt2.Pt.X Then
                    Dim distance = Math.Sqrt((pt1.Pt.X - pt2.Pt.X) * (pt1.Pt.X - pt2.Pt.X) + (pt1.Pt.Y - pt2.Pt.Y) * (pt1.Pt.Y - pt2.Pt.Y))
                    ' it is not enough to just be at the same height.  Can't be too far away!
                    If minDistance > distance And distance < topDistance Then
                        minIndex = j
                        minDistance = distance
                    End If
                End If
            Next
            If minDistance < Single.MaxValue Then
                dst2.Circle(pt1.Pt, 3, cv.Scalar.Blue, -1, task.lineType)
                dst1.Circle(pt1.Pt, 3, cv.Scalar.Blue, -1, task.lineType)
                dst2.Circle(CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, 3, cv.Scalar.Red, -1, task.lineType)
                dst2.Line(pt1.Pt, CS_KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, cv.Scalar.Yellow, 1, task.lineType)
            End If
        Next
        label1 = "Right image has " + CStr(CS_KazeRight.kazeKeyPoints.Count) + " key points"
        label2 = "Left image has " + CStr(CS_KazeLeft.kazeKeyPoints.Count) + " key points"
    End Sub
End Class




