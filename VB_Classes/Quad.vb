Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Quad_Basics : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public Sub New()
        task.gOptions.GridSlider.Value = 4
        desc = "Create a quad representation of the redCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim shift As cv.Point3f
        If Not standalone Then
            Dim ptM = task.ogl.options.moveAmount
            shift = New cv.Point3f(ptM(0), ptM(1), ptM(2))
        End If

        dst2 = runRedC(src, labels(2))

        quadData.Clear()
        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Or index >= task.rcList.Count Then Continue For
            Dim rc = task.rcList(index)

            dst3(roi).SetTo(rc.color)
            SetTrueText(Format(rc.depthMean, fmt1), New cv.Point(roi.X, roi.Y))

            Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, rc.depthMean))
            Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, rc.depthMean))

            quadData.Add(New cv.Point3f(rc.color(0), rc.color(1), rc.color(2)))
            quadData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, rc.depthMean + shift.Z))
            quadData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, rc.depthMean + shift.Z))
            quadData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, rc.depthMean + shift.Z))
            quadData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, rc.depthMean + shift.Z))
        Next
    End Sub
End Class







Public Class Quad_GridTiles : Inherits TaskParent
    Public quadData As New List(Of cv.Point3f)
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC3, 0)
        labels = {"", "RedCloud cells", "", "Simplified depth map with RedCloud cell colors"}
        desc = "Simplify the OpenGL quads without using OpenGL's point size"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        quadData.Clear()
        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim vec As cv.Scalar
        Dim shift As cv.Point3f
        If Not standalone Then
            Dim ptM = task.ogl.options.moveAmount
            shift = New cv.Point3f(ptM(0), ptM(1), ptM(2))
        End If
        For Each roi In task.gridRects
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If standaloneTest() Then dst3(roi).SetTo(c)
            If c = black Then Continue For

            quadData.Add(New cv.Vec3f(c(0), c(1), c(2)))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y + roi.Height, v(2))) + shift
            quadData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))
            If standaloneTest() Then dst1(roi).SetTo(v)
        Next
    End Sub
End Class
