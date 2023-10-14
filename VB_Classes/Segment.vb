Imports System.Runtime.InteropServices
Imports VB_Classes.VBtask
Imports cv = OpenCvSharp
Public Class Segment_Basics : Inherits VB_Algorithm
    Public tee As New RedCloud_BasicsOriginal
    Public Sub New()
        labels = {"", "", "Merged RedCloud output for depth and BGR", "RedCloud output for BGR"}
        desc = "Segment the whole image using RedCloud depth or BGR algorithms (toggle with 'Use Color...' global algorithm option)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        tee.Run(src)
        dst2 = tee.dst2
        dst3 = tee.dst2

        For Each rc In tee.redCells
            dst2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        If task.heartBeat Then labels(2) = "There were " + Format(tee.redCells.Count, "000") + " after merging RedCloud output for depth and color"
    End Sub
End Class








Public Class Segment_NoOverlap : Inherits VB_Algorithm
    Public fLess As New FeatureLess_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "RedCloud output for depth", "", "RedCloud output for featureless"}
        desc = "Segment the whole image using RedCloud depth and BGR algorithms."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        fLess.Run(src)
        dst3 = fLess.dst2.Clone

        If task.heartBeat Then labels(2) = "There were " + Format(redC.redCells.Count, "000") + " after merging RedCloud output for depth and color"
    End Sub
End Class







Public Class Segment_Flatten : Inherits VB_Algorithm
    Public segment As New Segment_Basics
    Dim templateX As cv.Mat
    Dim templateY As cv.Mat
    Public Sub New()
        templateX = New cv.Mat(dst2.Size, cv.MatType.CV_32F)
        templateY = New cv.Mat(dst2.Size, cv.MatType.CV_32F)
        For i = 0 To templateX.Width - 1
            templateX.Set(Of Single)(0, i, i)
        Next

        For i = 1 To templateX.Height - 1
            templateX.Row(0).CopyTo(templateX.Row(i))
            templateY.Set(Of Single)(i, 0, i)
        Next

        For i = 1 To templateY.Width - 1
            templateY.Col(0).CopyTo(templateY.Col(i))
        Next
        templateX -= task.parms.cameraInfo.ppx
        templateY -= task.parms.cameraInfo.ppy

        labels = {"", "", "Segmentation of the entire image", "PointCloud with flattened segments"}
        desc = "Use the segmented image to provide flat 3D segments for each cell - intended for use with OpenGL."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        segment.Run(src)
        dst2 = segment.dst2

        task.pcSplit(2).SetTo(0)
        For Each rc In segment.tee.redCells
            vbDrawContour(task.pcSplit(2)(rc.rect), rc.contour, rc.depthMean(2), -1)
        Next

        Dim worldX As New cv.Mat, worldY As New cv.Mat
        cv.Cv2.Multiply(templateX, task.pcSplit(2), worldX)
        worldX *= 1 / task.parms.cameraInfo.fx

        cv.Cv2.Multiply(templateY, task.pcSplit(2), worldY)
        worldY *= 1 / task.parms.cameraInfo.fy

        Dim pc As New cv.Mat
        cv.Cv2.Merge({worldX, worldY, task.pcSplit(2)}, dst3)



        'Dim samples(dst3.Total * 3 - 1) As Single
        'Marshal.Copy(dst3.Data, samples, 0, samples.Length)

        'Dim X(templateX.Total * 3 - 1) As Single
        'Marshal.Copy(templateX.Data, X, 0, X.Length)

        'Dim Y(templateY.Total * 3 - 1) As Single
        'Marshal.Copy(templateY.Data, Y, 0, Y.Length)






    End Sub
End Class

