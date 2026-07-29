Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class StableDepth_Basics_TA : Inherits TaskParent
    Dim colorize As New DepthColorizer_CPP
    Public pointcloud As Mat
    Public pcSplit(2) As Mat
    Public Sub New()
        labels(2) = "Accumulated minimum values at each depth pixel.  Updated using RGB motion."
        labels(3) = "Pixels that were updated on the current frame."
        desc = "Stabilize X, Y, and Z of the cv.Point cloud using the minimum depth encountered."
    End Sub
    Public Shared Function updateXY(lastDepth As Mat, accumDepth As Mat) As Mat
        Dim diffDepth As New Mat
        Absdiff(lastDepth, accumDepth, diffDepth)
        Dim mask As New Mat
        Threshold(diffDepth, mask, 0, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(mask, mask)
        mask.SetTo(0, task.motion.motionMask)
        Return mask
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastDepth As Mat = task.pcSplit(2).Clone
        If task.heartBeat Then
            pointcloud = task.pointCloud.Clone
        Else
            task.pointCloud.CopyTo(pointcloud, task.motion.motionMask)
            task.pointCloud.CopyTo(pointcloud, task.noDepthMask)
        End If

        pcSplit = Split(pointcloud)
        Dim accumDepth As New Mat
        Min(pcSplit(2), lastDepth, accumDepth)

        If task.heartBeat = False Then
            dst3 = updateXY(lastDepth, accumDepth)
            task.pointCloud.CopyTo(pointcloud, dst3)
        End If

        colorize.Run(accumDepth)
        dst2 = colorize.dst2

        pcSplit = Split(pointcloud)
        lastDepth = pcSplit(2).Clone

        task.pointCloud = pointcloud.Clone
        task.pcSplit = pcSplit
        Threshold(pcSplit(2), task.depthmask, 0, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(task.depthmask, task.depthmask)
        task.noDepthMask = Not task.depthmask
    End Sub
End Class






Public Class StableDepth_Max : Inherits TaskParent
    Dim colorize As New DepthColorizer_CPP
    Public pointcloud As New Mat
    Public pcsplit() As Mat = Nothing
    Public Sub New()
        labels(2) = "Accumulated minimum values at each depth pixel.  Updated using RGB motion."
        labels(3) = "Pixels that were updated on the current frame."
        desc = "Stabilize X, Y, and Z of the cv.Point cloud using the maximum depth encountered."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Split(task.originalPointcloud, pcsplit)
        Static lastDepth As Mat = pcSplit(2).Clone

        Dim myHeartbeat = task.heartBeat Or task.optionsChanged
        If myHeartbeat Then
            pointcloud = task.pointCloud.Clone
        Else
            task.pointCloud.CopyTo(pointcloud, task.motion.motionMask)
            task.pointCloud.CopyTo(pointcloud, task.noDepthMask)
        End If

        Dim accumDepth As New Mat
        If task.heartBeat Then
            pcSplit(2).CopyTo(lastDepth)
            pcSplit(2).CopyTo(accumDepth)
        Else
            Max(pcSplit(2), lastDepth, accumDepth)
        End If

        If myHeartbeat = False Then
            dst3 = StableDepth_Basics_TA.updateXY(pcSplit(2), accumDepth)
        End If

        colorize.Run(accumDepth)
        dst2 = colorize.dst2
    End Sub
End Class
