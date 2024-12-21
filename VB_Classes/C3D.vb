Imports System.Web.UI
Imports cvb = OpenCvSharp
Public Class C3D_Basics : Inherits TaskParent
    Dim gpoints As New Feature_GridPoints
    Public Sub New()
        task.gOptions.GridSlider.Minimum = 2 ' smaller will hang
        desc = "Find the correlation of image coordinates to pointcloud coordinates"
    End Sub
    Private Function getCorrelation(A As cvb.Mat, B As cvb.Mat) As Single
        Dim correlation As New cvb.Mat
        cvb.Cv2.MatchTemplate(A, B, correlation, cvb.TemplateMatchModes.CCoeffNormed)
        Return correlation.Get(Of Single)(0, 0)
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then task.redC.Run(src)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        gpoints.Run(src)

        Dim xList As New List(Of Single), yList As New List(Of Single), zList As New List(Of Single)
        For Each pt In task.rc.ptList
            Dim vec = task.pointCloud.Get(Of cvb.Point3f)(pt.Y, pt.X)
            xList.Add(vec.X)
            yList.Add(vec.Y)
            zList.Add(vec.Z)
        Next

        If xList.Count > 0 Then
            Dim xMat As cvb.Mat = cvb.Mat.FromPixelData(xList.Count, 1, cvb.MatType.CV_32F, xList.ToArray)
            Dim yMat As cvb.Mat = cvb.Mat.FromPixelData(xList.Count, 1, cvb.MatType.CV_32F, yList.ToArray)
            Dim zMat As cvb.Mat = cvb.Mat.FromPixelData(xList.Count, 1, cvb.MatType.CV_32F, zList.ToArray)

            Dim correlationXZ As Single = getCorrelation(xMat, zMat)
            Dim correlationYZ As Single = getCorrelation(yMat, zMat)

            strOut = "X to Z correlation = " + Format(correlationXZ, fmt1) + vbCrLf +
                     "Y to Z correlation = " + Format(correlationYZ, fmt1) + vbCrLf
        End If
        If task.heartBeat Then SetTrueText(strOut, 3)
        For Each pt In task.rc.ptList
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class
