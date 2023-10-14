Imports  cv = OpenCvSharp
Public Class Voxels_Basics : Inherits VBparent
    Public voxels(1) As Single
    Public voxelMat As cv.Mat
    Public Sub New()
        If check.Setup(traceName, 1) Then
            check.Box(0).Text = "Display intermediate results"
            check.Box(0).Checked = True
        End If

        If standalone Then
            gOptions.GridWidthSlider.Value = 16
            gOptions.GridHeightSlider.Value = 16
        End If

        labels(3) = "Voxels labeled with their median distance"
        task.desc = "Use multi-threading to get median depth values as voxels."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        Dim split() = src.Split()

        Dim depthMask As New cv.Mat
        Dim input = (split(2) * 1000).ToMat
        cv.Cv2.InRange(input, task.minDepth, task.maxDepth, depthMask)

        If voxels.Length <> task.roiList.Count Then ReDim voxels(task.roiList.Count - 1)

        Parallel.For(0, task.roiList.Count,
        Sub(i)
            Dim roi = task.roiList(i)
            Dim count = depthMask(roi).CountNonZero
            If count > 0 Then
                voxels(i) = input(roi).Mean(depthMask(roi)).Item(0)
            Else
                voxels(i) = 0
            End If
        End Sub)
        voxelMat = New cv.Mat(voxels.Length, 1, cv.MatType.CV_32F)
        If check.Box(0).Checked Then
            dst2 = task.depthRGB.Clone()
            dst2.SetTo(task.highlightColor, task.gridMask)
            Dim img = New cv.Mat(split(2).Size, cv.MatType.CV_8UC3, 0)
            Parallel.For(0, task.roiList.Count,
                Sub(i)
                    Dim roi = task.roiList(i)
                    If voxels(i) >= task.minDepth And voxels(i) <= task.maxDepth Then
                        voxelMat.Set(Of Single)(i, 0, voxels(i))
                        Dim v = (voxels(i) - task.minDepth) / (task.maxDepth - task.minDepth)
                        Dim color = New cv.Scalar(((1 - v) * nearColor(0) + v * farColor(0)) >> 8,
                                                  ((1 - v) * nearColor(1) + v * farColor(1)) >> 8,
                                                  ((1 - v) * nearColor(2) + v * farColor(2)) >> 8)
                        img(roi).SetTo(color, depthMask(roi))
                    End If
                End Sub)
            dst3 = img.Resize(dst2.Size)
        End If
        voxelMat *= 255 / (task.maxDepth - task.minDepth) ' do the normalize manually to use the min and max Depth (more stable image)
    End Sub
End Class
