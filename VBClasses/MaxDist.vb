Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Public Class MaxDist_Basics : Inherits TaskParent
    Public Sub New()
        labels(3) = "Below left shows hullMask while below shows the contour mask."
        desc = "Find the point farthest from the edges of a mask."
    End Sub
    Public Shared Function setCloudData(mask As cv.Mat, rect As cv.Rect, index As Integer,
                                        Optional zeroRectangle As Boolean = True) As rcData
        Dim pc As New rcData
        pc.mask = mask.InRange(index, index)
        pc.rect = rect
        pc.index = index
        pc.contour = ContourBuild(pc.mask, cv.ContourApproximationModes.ApproxNone) ' ApproxTC89L1 or ApproxNone
        If pc.contour.Count < 3 Then Return Nothing
        Dim listOfPoints = New List(Of List(Of cv.Point))({pc.contour})
        pc.contourMask = New cv.Mat(pc.mask.Size, cv.MatType.CV_8U, 0)
        cv.Cv2.DrawContours(pc.contourMask, listOfPoints, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)

        If zeroRectangle Then
            Dim tmp As cv.Mat = pc.contourMask.Clone
            ' see MaxDist_NoRectangle below to confirm this is needed (it is.)
            tmp.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = tmp.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            pc.maxDist.X = mm.maxLoc.X + pc.rect.X
            pc.maxDist.Y = mm.maxLoc.Y + pc.rect.Y
        End If

        pc.hull = cv.Cv2.ConvexHull(pc.contour.ToArray, True).ToList
        pc.hullMask = New cv.Mat(pc.mask.Size, cv.MatType.CV_8U, 0)
        listOfPoints = New List(Of List(Of cv.Point))({pc.hull})
        cv.Cv2.DrawContours(pc.hullMask, listOfPoints, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link8)

        pc.color = task.vecColors(pc.index)
        pc.pixels = pc.mask.CountNonZero
        Return pc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2))

        dst3.SetTo(0)
        Dim index As Integer = 1
        For Each pc In task.redCloud.rcList
            Dim pcTest = New rcData(pc.mask, pc.rect, index)
            If pcTest.index >= 0 Then
                pcTest.color = pc.color
                dst3(pcTest.rect).SetTo(pcTest.color, pcTest.mask)
                dst3.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
                index += 1
            End If
        Next
    End Sub
End Class





Public Class MaxDist_NoRectangle : Inherits TaskParent
    Public Sub New()
        labels(3) = "Below left shows hullMask while below shows the contour mask."
        desc = "Is it necessary to draw a rectangle of zeros at the edge of the mask?  Answer: no"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2)) ' task.redCloud.rcList uses the rectangle of zeros.

        Dim rcList As New List(Of rcData)
        dst3.SetTo(0)
        For Each pc In task.redCloud.rcList
            ' This rcList will NOT use the rectangle of zeros (definitely need the rectangle!)
            Dim pcTest = MaxDist_Basics.setCloudData(pc.mask, pc.rect, rcList.Count + 1, False)
            If pcTest.index >= 0 Then
                pcTest.color = pc.color
                dst3(pcTest.rect).SetTo(pcTest.color, pcTest.mask)
                dst3.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
                rcList.Add(pcTest)
            End If
        Next
    End Sub
End Class