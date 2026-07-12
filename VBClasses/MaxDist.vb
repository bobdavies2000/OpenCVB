Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class MaxDist_Basics : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Below left shows hullMask while below shows the contour mask."
        desc = "Find the point farthest from the edges of a mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim index As Integer = 1
        For Each rc In redC.rcList
            Dim rcTest = New rcDataOld(rc.mask, rc.rect, index)
            If rcTest.mapID >= 0 Then
                rcTest.color = rc.color
                dst3(rcTest.rect).SetTo(rcTest.color, rcTest.mask)
                Circle(dst3, rc.maxDist, task.DotSize, task.highlight, -1)
                index += 1
            End If
        Next
    End Sub
End Class





Public Class XR_MaxDist_NoRectangle : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Below left shows hullMask while below shows the contour mask."
        desc = "Does the mask need to have rectangle of zeros?  Answer: yes"
    End Sub
    Public Function setCloudData(_mask As cv.Mat, _rect As cv.Rect, _index As Integer,
                                                Optional zeroRectangle As Boolean = True) As rcDataOld
        Dim rc As New rcDataOld
                  InRange(_mask, _index, _index, rc.mask)
        rc.rect = _rect
        rc.mapID = _index
        Dim contour = ContourBuild(rc.mask)
        If contour.Count < 3 Then Return Nothing
        Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
        rc.mask = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
        DrawContours(rc.mask, listOfPoints, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)

        If zeroRectangle Then
            Dim tmp As cv.Mat = rc.mask.Clone
            ' see XR_MaxDist_NoRectangle below to confirm this is needed (it is.)
            Rectangle(tmp, New cv.Rect(0, 0, rc.mask.Width, rc.mask.Height), cv.Scalar.All(0), 1)
            Dim distance32f As New cv.Mat
            DistanceTransform(tmp, distance32f, cv.DistanceTypes.L1, cv.DistanceTransformMasks.Precise, cv.MatType.CV_32F)
            Dim mm As mmData = vbc.GetMinMax(distance32f)
            rc.maxDist.X = mm.maxLoc.X + rc.rect.X
            rc.maxDist.Y = mm.maxLoc.Y + rc.rect.Y
        End If

        rc.hull = ConvexHull(contour.ToArray, True).ToList

        rc.color = task.vecColors(rc.mapID)
        rc.pixels = CountNonZero(rc.mask)
        Return rc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rcList As New List(Of rcDataOld)
        dst3.SetTo(0)
        For Each rc In redC.rcList
            ' This rcList will NOT use the rectangle of zeros (definitely need the rectangle!)
            Dim rcTest = setCloudData(rc.mask, rc.rect, rcList.Count + 1, False)
            If rcTest Is Nothing Then Continue For
            If rcTest.mapID >= 0 Then
                rcTest.color = rc.color
                dst3(rcTest.rect).SetTo(rcTest.color, rcTest.mask)
                Circle(dst3, rc.maxDist, task.DotSize, task.highlight, -1)
                rcList.Add(rcTest)
            End If
        Next
    End Sub
End Class
