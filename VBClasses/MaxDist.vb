Imports cv = OpenCvSharp
Namespace VBClasses
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
                Dim rcTest = New rcData(rc.mask, rc.rect, index)
                If rcTest.index >= 0 Then
                    rcTest.color = rc.color
                    dst3(rcTest.rect).SetTo(rcTest.color, rcTest.mask)
                    dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                    index += 1
                End If
            Next
        End Sub
    End Class





    Public Class NR_MaxDist_NoRectangle : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            labels(3) = "Below left shows hullMask while below shows the contour mask."
            desc = "Does the mask need to have rectangle of zeros?  Answer: yes"
        End Sub
        Public Function setCloudData(_mask As cv.Mat, _rect As cv.Rect, _index As Integer,
                                            Optional zeroRectangle As Boolean = True) As rcData
            Dim rc As New rcData
            rc.mask = _mask.InRange(_index, _index)
            rc.rect = _rect
            rc.index = _index
            Dim contour = ContourBuild(rc.mask)
            If contour.Count < 3 Then Return Nothing
            Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
            rc.mask = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
            cv.Cv2.DrawContours(rc.mask, listOfPoints, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)

            If zeroRectangle Then
                Dim tmp As cv.Mat = rc.mask.Clone
                ' see NR_MaxDist_NoRectangle below to confirm this is needed (it is.)
                tmp.Rectangle(New cv.Rect(0, 0, rc.mask.Width, rc.mask.Height), 0, 1)
                Dim distance32f = tmp.DistanceTransform(cv.DistanceTypes.L1, 0)
                Dim mm As mmData = vbc.GetMinMax(distance32f)
                rc.maxDist.X = mm.maxLoc.X + rc.rect.X
                rc.maxDist.Y = mm.maxLoc.Y + rc.rect.Y
            End If

            rc.hull = cv.Cv2.ConvexHull(contour.ToArray, True).ToList

            rc.color = task.vecColors(rc.index)
            rc.pixels = rc.mask.CountNonZero
            Return rc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim rcList As New List(Of rcData)
            dst3.SetTo(0)
            For Each rc In redC.rcList
                ' This rcList will NOT use the rectangle of zeros (definitely need the rectangle!)
                Dim rcTest = setCloudData(rc.mask, rc.rect, rcList.Count + 1, False)
                If rcTest Is Nothing Then Continue For
                If rcTest.index >= 0 Then
                    rcTest.color = rc.color
                    dst3(rcTest.rect).SetTo(rcTest.color, rcTest.mask)
                    dst3.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                    rcList.Add(rcTest)
                End If
            Next
        End Sub
    End Class
End Namespace