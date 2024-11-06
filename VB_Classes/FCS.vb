Imports OpenCvSharp
Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class FCS_Basics : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcs As New FCS_Delaunay
    Dim fInfo As New FCS_Info
    Public featureInput As New List(Of cvb.Point2f)
    Dim clickPoint As New cvb.Point(dst2.Width / 2, dst2.Height / 2)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        FindSlider("Min Distance to next").Value = task.fPointMinDistance
        FindSlider("Feature Sample Size").Value = 250 ' keep within a byte boundary.
        labels(1) = "The index for each of the cells (if standalonetest)"
        desc = "Feature Coordinate System (FCS) - Create the fpList with rect, mask, index, and facets"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone

        If featureInput.Count = 0 Then
            feat.Run(src)
            fcs.featureInput = New List(Of cvb.Point2f)(task.features)
        Else
            fcs.featureInput = featureInput
        End If

        fcs.Run(src)

        dst2 = fcs.dst2
        task.fpMap = fcs.dst3.Clone

        If task.heartBeat Then labels(2) = CStr(featureInput.Count) + " feature grid cells."

        If task.mouseClickFlag Then clickPoint = task.ClickPoint
        task.fpSelected = task.fpList(task.fpMap.Get(Of Byte)(clickPoint.Y, clickPoint.X))
        fInfo.Run(empty)
        strOut = fInfo.strOut

        dst2.SetTo(0, task.fpOutline)
        Dim fp = task.fpSelected
        ' dst2(fp.rect).SetTo(cvb.Scalar.White, fp.mask)

        For i = 0 To fp.facets.Count - 1
            Dim p1 = fp.facets(i)
            Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
        Next

        dst2.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        dst0.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        dst1.SetTo(0)
        dst1.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        SetTrueText(strOut, 3)

        For Each fp In task.fpList
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst0, fp.pt, task.DotSize, task.HighlightColor)
            SetTrueText(CStr(fp.index), New cvb.Point(CInt(fp.pt.X), CInt(fp.pt.Y)), 1)
        Next
    End Sub
End Class





Public Class FCS_Info : Inherits TaskParent
    Public Sub New()
        desc = "Display the contents of the Feature Coordinate System (FCS) cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.fpList.Count = 0 Then Exit Sub

        Dim fp = task.fpSelected
        If task.ClickPoint.DistanceTo(fp.pt) < fp.rect.Width / 2 And
           task.ClickPoint.DistanceTo(fp.pt) < fp.rect.Height / 2 Then
            task.ClickPoint = task.fpSelected.pt
        End If

        strOut = "FCS cell selected: " + vbCrLf
        strOut += "Feature point: " + fp.pt.ToString + vbCrLf
        strOut += "Rect: x/y " + CStr(fp.rect.X) + "/" + CStr(fp.rect.Y) + " w/h "
        strOut += CStr(fp.rect.Width) + "/" + CStr(fp.rect.Height) + vbCrLf
        strOut += "index = " + CStr(fp.index) + vbCrLf
        strOut += "Facet count = " + CStr(fp.facet2f.Count) + " facets" + vbCrLf
        strOut += "ClickPoint = " + task.ClickPoint.ToString + vbCrLf + vbCrLf
        ' Dim vec = task.pointCloud.Get(Of cvb.Point3f)(fp.pt.Y, fp.pt.X)
        strOut += "Pointcloud entry: " + Format(fp.pt3D.X, fmt1) + "/" +
                                         Format(fp.pt3D.Y, fmt1) + "/" + Format(fp.pt3D.Z, fmt1) + vbCrLf
        strOut += "Pointcloud mean X/Y/Z: " + Format(fp.depthMean(0), fmt1) + "/" +
                              Format(fp.depthMean(1), fmt1) + "/" + Format(fp.depthMean(2), fmt1) + vbCrLf
        strOut += "Pointcloud stdev X/Y/Z: " + Format(fp.depthStdev(0), fmt1) + "/" +
                              Format(fp.depthStdev(1), fmt1) + "/" + Format(fp.depthStdev(2), fmt1) + vbCrLf
        strOut += "Facets: " + vbCrLf
        For i = 0 To fp.facets.Count - 1
            strOut += CStr(i) + ": " + CStr(fp.facets(i).X) + ", " + CStr(fp.facets(i).Y) + vbCrLf
        Next

        If standalone Then
            SetTrueText("Select a feature grid cell to get more information.", 2)
        End If
    End Sub
End Class





Public Class FCS_Lines : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Use lines as input to FCS."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src

        lines.Run(src)
        dst2 = lines.dst3

        fcs.featureInput.Clear()
        For Each lp In lines.lpList
            fcs.featureInput.Add(lp.center)
        Next

        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(white, lines.dst3)

        For i = 0 To lines.lpList.Count - 1
            Dim lp = lines.lpList(i)
            DrawCircle(dst2, lp.center, task.DotSize, red, -1)
            dst0.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            SetTrueText(CStr(i), lp.center, 1)
        Next

        SetTrueText(fcs.strOut, 3)
        If task.heartBeat Then labels(2) = CStr(fcs.featureInput.Count) + " lines were found."
    End Sub
End Class






Public Class FCS_LinesAndEdges : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim feat As New Feature_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Run Feature_Basics and Line_Basics for comparison."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst1 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        lines.Run(src)
        dst2 = lines.dst3

        feat.Run(src)
        dst3 = feat.dst2

        For Each pt In task.features
            DrawCircle(dst1, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class






Public Class FCS_BasicsOld : Inherits TaskParent
    Public fpList As New List(Of fPoint)
    Public fpMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
    Dim feat As New Feature_Basics
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        FindSlider("Feature Sample Size").Value = 255 ' keep within a byte boundary.
        desc = "Divide up the image based on the features found and track each cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        fpList.Clear()
        fpList.Add(New fPoint) ' index = 0
        Dim mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
        For i = 0 To facets.Length - 1
            Dim fp = New fPoint
            fp.facet2f = New List(Of Point2f)(facets(i))
            fp.facets = New List(Of cvb.Point)

            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cvb.Point(facets(i)(j).X, facets(i)(j).Y)
                If pt.X < 0 Then pt.X = 0
                If pt.Y < 0 Then pt.Y = 0
                If pt.X >= dst2.Width Then pt.X = dst2.Width - 1
                If pt.Y >= dst2.Height Then pt.Y = dst2.Height - 1
                xlist.Add(pt.X)
                ylist.Add(pt.Y)
                fp.facets.Add(pt)
            Next

            fp.rect = New cvb.Rect(xlist.Min, ylist.Min, xlist.Max - xlist.Min, ylist.Max - ylist.Min)

            mask32s(fp.rect).SetTo(0)
            mask32s.FillConvexPoly(fp.facets, 255, task.lineType)
            mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)
            fpList.Add(fp)
        Next

        dst3.SetTo(0)
        If task.heartBeat Then
            For i = 1 To fpList.Count - 1
                Dim fp = fpList(i)
                fp.index = i
                fpList(i) = fp
            Next
        Else
            Dim usedList(fpList.Count - 1) As Boolean
            For i = 1 To fpList.Count - 1
                Dim fp = fpList(i)
                If usedList(fp.index) Then
                    For j = 0 To usedList.Count - 1
                        If usedList(j) = False Then
                            fp.index = j
                            Exit For
                        End If
                    Next
                End If
                usedList(fp.index) = True
                fpList(i) = fp
            Next
        End If

        For i = 1 To fpList.Count - 1
            Dim fp = fpList(i)
            dst3(fp.rect).SetTo(fp.index, fp.mask)
        Next

        dst2 = ShowPalette(dst3)
        fpMap = dst3.Clone

        For i = 1 To fpList.Count - 1
            Dim fp = fpList(i)
            DrawCircle(dst3, fp.pt, task.DotSize, white)
        Next
        If task.heartBeat Then labels(3) = CStr(fpList.Count) + " feature grid entries."
    End Sub
End Class





Public Class FCS_NoTracking : Inherits TaskParent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_8U, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static feat As New Feature_Basics
            feat.Run(src)
            inputPoints = New List(Of cvb.Point2f)(task.features)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst1, ptList, 255, 1)
        Next

        facet32s.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        dst3.SetTo(0, dst1)
        dst2.SetTo(white, dst1)
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class







Public Class FCS_LeftRight : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcsL As New FCS_Basics
    Dim fcsR As New FCS_Basics
    Dim saveLeftMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
    Dim saveLeftList As New List(Of fPoint)
    Dim saveRightMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
    Dim saveRightList As New List(Of fPoint)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Build an FCS for both left and right views."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.fpMap = saveLeftMap.Clone
        task.fpList = saveLeftList

        feat.Run(task.leftView)
        fcsL.featureInput = New List(Of cvb.Point2f)(task.features)

        fcsL.Run(task.leftView)
        dst0 = fcsL.dst0.Clone
        dst2 = fcsL.dst2.Clone

        saveLeftMap = task.fpMap.Clone
        saveLeftList = New List(Of fPoint)(task.fpList)

        task.fpMap = saveRightMap.Clone
        task.fpList = saveRightList

        feat.Run(task.rightView)
        fcsR.featureInput = New List(Of cvb.Point2f)(task.features)

        fcsR.Run(task.rightView)
        dst1 = fcsR.dst0.Clone
        dst3 = fcsR.dst2.Clone

        saveRightMap = task.fpMap.Clone
        saveRightList = New List(Of fPoint)(task.fpList)
    End Sub
End Class







Public Class FCS_LeftView : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Build an FCS for both left and right views."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(task.leftView)
        fcs.featureInput = New List(Of cvb.Point2f)(task.features)

        fcs.Run(task.leftView)
        dst0 = fcs.dst0
        dst1 = fcs.dst1
        dst2 = fcs.dst2
        dst3 = fcs.dst3

    End Sub
End Class







Public Class FCS_Delaunay : Inherits TaskParent
    Public featureInput As New List(Of cvb.Point2f)
    Dim facetList As New List(Of List(Of cvb.Point))
    Dim subdiv As New cvb.Subdiv2D
    Dim mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Build a Feature Coordinate System by subdividing an image based on the points provided."
    End Sub
    Private Function buildRect(fp As fPoint, mms() As Single) As fPoint
        fp.rect = ValidateRect(New cvb.Rect(mms(0), mms(1), mms(2) - mms(0), mms(3) - mms(1)))

        mask32s(fp.rect).SetTo(0)
        mask32s.FillConvexPoly(fp.facets, white, task.lineType)
        mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)

        Return fp
    End Function
    Private Function findRect(fp As fPoint, mms() As Single) As fPoint
        Dim pts As cvb.Mat = fp.mask.FindNonZero()

        Dim points(pts.Total * 2 - 1) As Integer
        Marshal.Copy(pts.Data, points, 0, points.Length)

        Dim minX As Integer = Integer.MaxValue, miny As Integer = Integer.MaxValue
        Dim maxX As Integer, maxY As Integer
        For i = 0 To points.Length - 1 Step 2
            Dim x = points(i)
            Dim y = points(i + 1)
            If x < minX Then minX = x
            If y < miny Then miny = y
            If x > maxX Then maxX = x
            If y > maxY Then maxY = y
        Next

        fp.mask = fp.mask(New cvb.Rect(minX, miny, maxX - minX, maxY - miny))
        fp.rect = New cvb.Rect(fp.rect.X + minX, fp.rect.Y + miny, maxX - minX, maxY - miny)
        Return fp
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static feat As New Feature_Basics
            feat.Run(src)
            featureInput = task.features
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(featureInput)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        task.fpList.Clear()
        For i = 0 To facets.Length - 1
            Dim fp = New fPoint
            fp.facet2f = New List(Of Point2f)(facets(i))
            fp.facets = New List(Of cvb.Point)
            fp.index = i

            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cvb.Point(facets(i)(j).X, facets(i)(j).Y)
                xlist.Add(pt.X)
                ylist.Add(pt.Y)
                fp.facets.Add(pt)
            Next

            Dim minX = xlist.Min, minY = ylist.Min, maxX = xlist.Max, maxY = ylist.Max
            Dim mms() As Single = {minX, minY, maxX, maxY}
            fp = buildRect(fp, mms)

            If minX < 0 Or minY < 0 Or maxX >= dst2.Width Or maxY >= dst2.Height Then
                fp = findRect(fp, mms)
            End If

            fp.pt = task.features(i)
            fp.pt3D = task.pointCloud.Get(Of cvb.Point3f)(fp.pt.Y, fp.pt.X)
            cvb.Cv2.MeanStdDev(task.pointCloud(fp.rect), fp.depthMean, fp.depthStdev, fp.mask)
            task.fpList.Add(fp)
        Next

        dst3.SetTo(0)
        For Each fp In task.fpList
            dst3(fp.rect).SetTo(fp.index, fp.mask)
        Next

        task.fpOutline = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(task.fpOutline, ptList, white, 1)
        Next

        dst2 = ShowPalette(dst3)
        labels(2) = traceName + ": " + Format(featureInput.Count, "000") + " cells were present."
    End Sub
End Class







Public Class FCS_DepthCells : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim clickPoint As New cvb.Point(dst2.Width / 2, dst2.Height / 2)
    Dim fInfo As New FCS_Info
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        dst1 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        desc = "Assign the depth of the feature point to the whole cell and display."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone
        fcs.Run(src)

        If task.mouseClickFlag Then clickPoint = task.ClickPoint
        task.fpSelected = task.fpList(task.fpMap.Get(Of Byte)(clickPoint.Y, clickPoint.X))
        fInfo.Run(empty)
        SetTrueText(fInfo.strOut, 3)

        dst1.SetTo(0)
        For Each fp In task.fpList
            Dim mask = fp.mask And task.depthMask(fp.rect)
            dst1(fp.rect).SetTo(255 * fp.depthMean(2) / task.MaxZmeters, mask)
        Next

        dst2 = ShowPalette(dst1)

        For Each fp In task.fpList
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst0, fp.pt, task.DotSize, task.HighlightColor)
            SetTrueText(CStr(fp.index), New cvb.Point(CInt(fp.pt.X), CInt(fp.pt.Y)), 1)
        Next

        For i = 0 To task.fpSelected.facets.Count - 1
            Dim p1 = task.fpSelected.facets(i)
            Dim p2 = task.fpSelected.facets((i + 1) Mod task.fpSelected.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth + 1, task.lineType)
            dst0.Line(p1, p2, cvb.Scalar.White, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class