Imports cv = OpenCvSharp
Public Class FindNonZero_Basics : Inherits TaskParent
    Public ptMat As cv.Mat
    Public Sub New()
        labels(2) = "Coordinates of non-zero points"
        labels(3) = "Non-zero original points"
        desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            src = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            Dim srcPoints(100 - 1) As cv.Point ' doesn't really matter how many there are.
            For i = 0 To srcPoints.Length - 1
                srcPoints(i).X = msRNG.Next(0, src.Width)
                srcPoints(i).Y = msRNG.Next(0, src.Height)
                src.Set(Of Byte)(srcPoints(i).Y, srcPoints(i).X, 255)
            Next
        End If

        ptMat = src.FindNonZero()

        If standaloneTest() Then
            dst3 = New cv.Mat(src.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            ' mark the points so they are visible...
            For i = 0 To ptMat.Rows - 1
                DrawCircle(dst3, ptMat.Get(Of cv.Point)(0, i), task.DotSize, white)
            Next

            Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
            For i = 0 To ptMat.Rows - 1
                Dim pt = ptMat.Get(Of cv.Point)(0, i)
                outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
                If i > 100 Then Exit For ' for when there are way too many points found...
            Next
            SetTrueText(outstr)
        End If
    End Sub
End Class







Public Class FindNonZero_SoloPoints : Inherits TaskParent
    Dim hotTop As New BackProject_SoloTop
    Dim hotSide As New BackProject_SoloSide
    Dim nZero As New FindNonZero_Basics
    Public soloPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the solo points in the pointcloud histograms for top and side views."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hotTop.Run(src)
        dst2 = hotTop.dst3

        hotSide.Run(src)
        dst2 = dst2 Or hotSide.dst3

        nZero.Run(dst2)
        soloPoints.Clear()
        For i = 0 To nZero.ptMat.Rows - 1
            soloPoints.Add(nZero.ptMat.Get(Of cv.Point)(i, 0))
        Next

        If task.heartBeat Then labels(2) = $"There were {soloPoints.Count} points found"
    End Sub
End Class