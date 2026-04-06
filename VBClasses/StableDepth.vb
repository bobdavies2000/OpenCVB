Imports cv = OpenCvSharp
Public Class StableDepth_Basics : Inherits TaskParent
    Dim colorize As New DepthColorizer_CPP
    Public pointcloud As cv.Mat
    Public pcSplit(2) As cv.Mat
    Public TA_Active As Boolean = False
    Public Sub New()
        labels(2) = "Accumulated minimum values at each depth pixel.  Updated using RGB motion."
        labels(3) = "Pixels that were updated on the current frame."
        desc = "Stabilize X, Y, and Z of the point cloud using the minimum depth encountered."
    End Sub
    Public Shared Function updateXY(lastDepth As cv.Mat, accumDepth As cv.Mat) As cv.Mat
        Dim diffDepth As New cv.Mat
        cv.Cv2.Absdiff(lastDepth, accumDepth, diffDepth)
        Dim mask = diffDepth.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        mask.SetTo(0, task.motion.motionMask)
        Return mask
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastDepth As cv.Mat = task.pcSplit(2).Clone
        Dim myHeartbeat = task.heartBeat Or task.optionsChanged
        If myHeartbeat Then
            pointcloud = task.pointCloud.Clone
        Else
            task.pointCloud.CopyTo(pointcloud, task.motion.motionMask)
            task.pointCloud.CopyTo(pointcloud, task.noDepthMask)
        End If

        Dim pcSplit = pointcloud.Split()
        Dim accumDepth As New cv.Mat
        cv.Cv2.Min(pcSplit(2), lastDepth, accumDepth)

        If myHeartbeat = False Then
            dst3 = updateXY(lastDepth, accumDepth)
            task.pointCloud.CopyTo(pointcloud, dst3)
        End If

        colorize.Run(accumDepth)
        dst2 = colorize.dst2

        pcSplit = pointcloud.Split()
        lastDepth = pcSplit(2).Clone

        If TA_Active Then
            task.pointCloud = pointcloud.Clone
            task.pcSplit = pcSplit
            task.depthmask = pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            task.noDepthMask = Not task.depthmask
        End If
    End Sub
End Class






Public Class StableDepth_Max_TA : Inherits TaskParent
    Dim colorize As New DepthColorizer_CPP
    Public pointcloud As New cv.Mat
    Public pcsplit(2) As cv.Mat
    Public TA_Active As Boolean = False
    Public Sub New()
        labels(2) = "Accumulated minimum values at each depth pixel.  Updated using RGB motion."
        labels(3) = "Pixels that were updated on the current frame."
        desc = "Stabilize X, Y, and Z of the point cloud using the maximum depth encountered."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastDepth As cv.Mat = task.pcSplit(2).Clone
        Dim myHeartbeat = task.heartBeat Or task.optionsChanged
        If myHeartbeat Then
            pointcloud = task.pointCloud.Clone
        Else
            task.pointCloud.CopyTo(pointcloud, task.motion.motionMask)
            task.pointCloud.CopyTo(pointcloud, task.noDepthMask)
        End If

        Dim pcSplit = pointcloud.Split()

        Dim accumDepth As New cv.Mat
        cv.Cv2.Max(pcSplit(2), lastDepth, accumDepth)

        If myHeartbeat = False Then
            dst3 = StableDepth_Basics.updateXY(pcSplit(2), accumDepth)
            task.pointCloud.CopyTo(pointcloud, dst3)
        End If

        colorize.Run(accumDepth)
        dst2 = colorize.dst2

        pcSplit = pointcloud.Split()
        lastDepth = pcSplit(2).Clone

        If TA_Active Then
            task.pointCloud = pointcloud.Clone
            task.pcSplit = pcSplit
            task.depthmask = pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            task.noDepthMask = Not task.depthmask
        End If
    End Sub
End Class