Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Hull_Basics : Inherits TaskParent
        Dim redColor As New RedColor_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Compare hulls and contours for the redC cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redColor.Run(src)
            dst2 = redColor.dst2
            labels(2) = redColor.labels(2)

            Dim rc = task.rcD
            If rc Is Nothing Then rc = redColor.rcList(1)
            If rc.contour Is Nothing Then Exit Sub

            dst1.SetTo(0)
            dst3.SetTo(0)

            DrawTour(dst3(rc.rect), rc.contour, task.highlight, task.lineWidth)
            Dim hull = Cv2.ConvexHull(rc.contour.ToArray)
            DrawTour(dst1(rc.rect), hull.ToList, task.highlight)

            labels(2) = "Contour points count reduced from " +
                         CStr(rc.contour.Count) + " to " + CStr(hull.Length)
        End Sub
    End Class





    Public Class Hull_Edges : Inherits TaskParent
        Dim redC As New RedColor_FLessBasics
        Public Sub New()
            desc = "Match hull edges that are also real edges."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            CvtColor(task.edges.dst2, dst3, cv.ColorConversionCodes.GRAY2BGR)
            For Each rc In redC.rcList
                DrawTour(dst3(rc.rect), rc.contour, task.highlight, task.lineWidth)
            Next
        End Sub
    End Class






    Public Class XR_Hull_Basics : Inherits TaskParent
        Dim random As New Random_Basics
        Public inputPoints As New List(Of Point2f)
        Public hull As New List(Of cv.Point)
        Public useRandomPoints As Boolean
        Public Sub New()
            labels = {"", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", ""}
            If standalone Then random.range = New cv.Rect(100, 100, 50, 50)
            desc = "Given a list of points, create a hull that encloses them."
        End Sub
        Private Shared Function vbFloat2Int(ptList2f As List(Of Point2f)) As List(Of cv.Point)
            Dim ptList As New List(Of cv.Point)
            For Each pt In ptList2f
                ptList.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
            Next
            Return ptList
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If (standaloneTest() And heartbeatX()) Or (useRandomPoints And heartbeatX()) Then
                random.Run(src)
                dst2.SetTo(0)
                For Each pt In random.PointList
                    Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
                Next
                inputPoints = New List(Of Point2f)(random.PointList)
            End If
            Dim hull2f = ConvexHull(inputPoints, True)
            hull = vbFloat2Int(hull2f.ToList)
            DrawTour(dst2, hull, Scalar.Yellow)
        End Sub
    End Class




    Public Class Hull_Defect : Inherits TaskParent
        Public hull As New List(Of cv.Point)
        Public contour As cv.Point()
        Public Sub New()
            desc = "Find defects in the hull provided."
        End Sub
        Public Shared Function indicesMayFail(contour As List(Of cv.Point)) As Boolean
            For i = 0 To contour.Count - 1
                Dim p1 = contour(i)
                For j = i + 1 To contour.Count - 1
                    Dim p2 = contour(j)
                    If p1 = p2 Then Return True
                Next
            Next
            Return False
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static redC As New RedC_Basics
                redC.Run(task.gray)
                If redC.rcList.Count <= 1 Then Exit Sub
                dst2 = redC.dst2
                labels(2) = redC.labels(2)
                hull = redC.rcList(0).contour

                contour = redC.rcList(0).contour.ToArray
            End If

            Dim contourList = contour.ToList()
            Dim defects As Vec4i() = Nothing
            contourList = Convex_RedCDefects.checkDefects(contourList, defects)
            contour = contourList.ToArray()

            If contour.Length > 0 Then
                Dim lastV As Integer = -1
                Dim newC As New List(Of cv.Point)
                For Each v In defects
                    If v(0) <> lastV And lastV >= 0 Then
                        For i = lastV To v(0) - 1
                            newC.Add(contour(i))
                        Next
                    End If
                    newC.Add(contour(v(0)))
                    newC.Add(contour(v(2)))
                    newC.Add(contour(v(1)))
                    lastV = v(1)
                Next
                dst3.SetTo(0)
                DrawTour(dst3, newC, white)
            End If
        End Sub
    End Class




    Public Class Hull_Contour : Inherits TaskParent
        Dim redC As New RedColor_FLessBasics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(1) = "Contour is in white and hull is highlighted."
            desc = "Compare the hull to the contour of a cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If redC.rcList.Count <= 1 Then Exit Sub

            Dim rc = task.rcD
            If rc Is Nothing Then rc = redC.rcList(1)

            Dim newSize = New cv.Size(rc.rect.Width, rc.rect.Width * dst2.Height / dst2.Width)
            If rc.rect.Width * dst2.Height / dst2.Width Then
                newSize = New cv.Size(rc.rect.Height * dst2.Width / dst2.Height, rc.rect.Height)
            End If
            Dim tmp As New cv.Mat(newSize, cv.MatType.CV_8UC3, 0)
            DrawTour(tmp, rc.contour, white, task.lineWidth)
            Dim hull() As Point = Cv2.ConvexHull(rc.contour)
            DrawTour(tmp, hull.ToList, task.highlight, task.lineWidth)
            Resize(tmp, dst1, dst1.Size, 0, 0, cv.InterpolationFlags.Nearest)

            Dim defects As Vec4i() = Nothing
            rc.contour = Convex_RedCDefects.checkDefects(rc.contour, defects)
            If defects.Length = 0 Then Exit Sub
            If rc.contour.Count = 0 Then Exit Sub

            Dim lastV As Integer = -1
            Dim tour As New List(Of cv.Point)
            For Each v In defects
                If v(0) <> lastV And lastV >= 0 Then
                    For i = lastV To v(0) - 1
                        tour.Add(rc.contour(i))
                    Next
                End If
                tour.Add(rc.contour(v(0)))
                tour.Add(rc.contour(v(2)))
                tour.Add(rc.contour(v(1)))
                lastV = v(1)
            Next

            dst3.SetTo(0)
            DrawTour(dst3, rc.contour, white, task.lineWidth)
            For Each d In defects
                Dim startPt = rc.contour(d.Item0)
                Dim endPt = rc.contour(d.Item1)
                Dim farPt = rc.contour(d.Item2)

                ' Start and end points
                Cv2.Circle(dst3, startPt, task.DotSize, task.highlight, -1)
                Cv2.Circle(dst3, endPt, task.DotSize, task.highlight, -1)

                ' Far point (deepest defect)
                Cv2.Circle(dst3, farPt, task.DotSize + 1, Scalar.Red, -1)

                ' Optional: draw line from start → far → end
                Cv2.Line(dst3, startPt, farPt, task.highlight, task.lineWidth)
                Cv2.Line(dst3, farPt, endPt, task.highlight, task.lineWidth)
            Next
        End Sub
    End Class






    Public Class Hull_RedC : Inherits TaskParent
        Dim redC As New RedColor_FLessBasics
        Public hulls As New List(Of List(Of cv.Point))
        Public rclist As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            labels = {"", "Cells where convexity defects failed", "", "Defect regions filled for each RedC hull"}
            desc = "Cursor.ai: Fill convexity defect regions for each hull in RedC.rcList"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim defectCount As Integer
            Dim filledDefects As Integer
            rcMap.SetTo(0)
            dst3.SetTo(0)
            rclist.Clear()
            For Each rc In redC.rcList
                If rc.contour.Count >= 3 Then
                    Dim hull = ConvexHull(rc.contour.ToArray, True).ToList
                    hulls.Add(hull.ToList)
                    Dim color = task.scalarColors(rc.index)
                    dst3(rc.rect).SetTo(color, rc.mask)
                    DrawTour(rcMap(rc.rect), hull.ToList, rc.index, -1)

                    Dim defects As Vec4i() = Nothing
                    rc.contour = Convex_RedCDefects.checkDefects(rc.contour, defects)
                    If defects.Length = 0 Then
                        defectCount += 1
                    Else
                        ' Fill each defect triangle (start / far / end) into mask, map, and display.
                        For Each d In defects
                            Dim tri = New cv.Point() {rc.contour(d.Item0), rc.contour(d.Item2), rc.contour(d.Item1)}
                            FillConvexPoly(dst3(rc.rect), tri, Scalar.All(255))
                            filledDefects += 1
                        Next
                        rc.contour = Convex_RedCDefects.betterContour(rc.contour, defects)
                        rc.pixels = CountNonZero(rc.mask)
                    End If
                End If

                rclist.Add(rc)
                If rc.index >= 10 Then Exit For
            Next

            labels(3) = CStr(rclist.Count) + " hulls, " + CStr(filledDefects) +
                        " defect regions filled.  " + CStr(defectCount) +
                        " hulls failed building the defect list."
        End Sub
    End Class
End Namespace
