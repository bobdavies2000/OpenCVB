Imports cv = OpenCvSharp
Public Class Disparity_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public rightView As cv.Mat
    Public rect As cv.Rect
    Public matchRect As cv.Rect
    Public Sub New()
        desc = "Given a brick, find the match in the right view image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone

        Dim index As Integer = task.bbo.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Static saveIndex As Integer = index
        Static saveCorrelations As New List(Of Single)
        Static bestRect As cv.Rect
        If saveIndex <> index Then
            saveCorrelations.Clear()
            saveIndex = index
        End If
        rect = task.gridRects(index)

        match.template = task.leftView(rect)
        Dim maxDisparity As Integer = 128
        match.searchRect = New cv.Rect(Math.Max(0, rect.X - maxDisparity), rect.Y,
                           rect.BottomRight.X - rect.X + maxDisparity, rect.Height)

        rightView = task.rightView

        dst2.Rectangle(rect, black, task.lineWidth)
        match.Run(rightView)
        dst3 = rightView
        matchRect = match.matchRect

        dst3.Rectangle(match.searchRect, black, task.lineWidth)
        dst3.Rectangle(match.matchRect, black, task.lineWidth)
        saveCorrelations.Add(match.correlation)

        Dim min = saveCorrelations.Min
        Dim max = saveCorrelations.Max

        If max = match.correlation And max > 0.8 Then bestRect = match.matchRect
        dst3.Rectangle(bestRect, black, task.lineWidth)

        If saveCorrelations.Count > 100 Then saveCorrelations.RemoveAt(0)

        labels(3) = "Correlation Min/Max = " + Format(min, fmt3) + "/" + Format(max, fmt3)
    End Sub
End Class







Public Class Disparity_Edges : Inherits TaskParent
    Dim edges As New EdgeLine_Raw
    Dim disparity As New Disparity_Basics
    Public Sub New()
        desc = "Use features in bricks to confirm depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.leftView)
        dst2 = task.edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2.Clone

        disparity.rightView = dst3
        disparity.Run(dst2)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels
    End Sub
End Class








Public Class Disparity_Validate : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "To validate Disparity_Basics, build the right view from the left view.  Should always match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim w = dst2.Width / 5
        Dim r1 = New cv.Rect(w, 0, dst2.Width - w, dst2.Height)
        Dim r2 = New cv.Rect(0, 0, r1.Width, dst2.Height)
        dst3.SetTo(0)
        task.leftView(r1).CopyTo(dst3(r2))
        disparity.rightView = dst3
        disparity.Run(task.leftView)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels
    End Sub
End Class







Public Class Disparity_RedMask : Inherits TaskParent
    Dim disparity As New Disparity_Basics
    Dim leftCells As New LeftRight_RedLeftGray
    Dim rightCells As New LeftRight_RedRightGray
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "To validate Disparity_Basics, just shift the left image right.  Should always match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView
        leftCells.Run(src)
        rightCells.Run(src)

        disparity.rightView = rightCells.dst2
        disparity.Run(leftCells.dst2)
        dst2 = disparity.dst2
        dst3 = disparity.dst3
        labels = disparity.labels

        task.color.Rectangle(disparity.rect, 255, task.lineWidth)
        dst1.Rectangle(disparity.matchRect, 255, task.lineWidth)
    End Sub
End Class





'Z = B * f / disparity  - we are using here: disparity = B * f / Z
'where:

' Z Is the depth in meters
' B Is the baseline in meters
' f Is the focal length in pixels
' disparity Is the disparity in pixels
' The baseline Is the distance between the two cameras in a stereo setup.
' The focal length Is the distance between the camera's lens and the sensor. The disparity is the difference in the x-coordinates of the same point in the left and right images.

' For example, if the baseline Is 0.5 meters, the focal length Is 1000 pixels, And the disparity Is 100 pixels, then the depth Is

' Z = 0.5 * 1000 / 100 = 5 meters
' The Function() relating depth To disparity Is only valid For a calibrated stereo setup.
Public Class Disparity_Inverse : Inherits TaskParent
    Public Sub New()
        task.drawRect = New cv.Rect(dst2.Width / 2 - 10, dst2.Height / 2 - 10, 20, 20)
        desc = "Use the depth to find the disparity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView
        ' assuming StereoLabs Zed 2i camera for now.
        ' disparity = B * f / depth
        Dim camInfo = task.calibData
        If task.drawRect.Width > 0 Then
            Dim white As New cv.Vec3b(255, 255, 255)
            For y = 0 To task.drawRect.Height - 1
                For x = 0 To task.drawRect.Width - 1
                    Dim depth = task.pcSplit(2)(task.drawRect).Get(Of Single)(y, x)
                    If depth > 0 Then
                        Dim disp = camInfo.baseline * camInfo.leftIntrinsics.fx / depth
                        dst3(task.drawRect).Set(Of cv.Vec3b)(y, x - disp, white)
                    End If
                Next
            Next
        End If
    End Sub
End Class








Public Class Disparity_Color8u : Inherits TaskParent
    Dim color8u As New Color8U_LeftRight
    Dim disparity As New Disparity_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Measure the impact of the color8u transforms on the bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.rightView.Clone
        color8u.Run(src)

        dst2 = src.Clone
        disparity.rightView = color8u.dst3
        disparity.Run(color8u.dst2)
        dst3 = disparity.dst3
        labels = disparity.labels

        task.color.Rectangle(disparity.rect, 255, task.lineWidth)
        dst1.Rectangle(disparity.matchRect, 255, task.lineWidth)

        Dim index As Integer = task.bbo.brickMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        Dim rect = task.gridRects(index)
        dst2.Rectangle(rect, 255, task.lineWidth)
    End Sub
End Class

