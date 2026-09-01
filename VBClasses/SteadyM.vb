Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class SteadyM_Basics : Inherits TaskParent
        Dim indexList As New List(Of Integer)
        Dim feat As New Feature_Basics
        Dim ptList As New List(Of cv.Point)
        Dim validList As New List(Of cv.Point)
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of features."
            desc = "Use the inverseM in SteadyCam_Basics to track points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.grayOriginal)

            If validList.Count < 3 Then
                dst1.SetTo(0)
                Dim index = 1
                ptList = New List(Of cv.Point)(feat.features)
                For Each pt In ptList
                    Circle(dst1, pt, task.DotSize * 5, cv.Scalar.All(index), -1, cv.LineTypes.Link8)
                    index += 1
                Next
                WarpAffine(dst1, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            validList.Clear()
            indexList.Clear()
            For Each pt In feat.features
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(pt, task.steadyCam.M))
                Dim index = dst0.Get(Of Byte)(ptSteady.Y, ptSteady.X)
                If index <> 0 Then
                    validList.Add(pt)
                    indexList.Add(index - 1)
                End If
            Next

            dst2 = task.color.Clone
            dst0.SetTo(0)
            For i = 0 To validList.Count - 1
                Circle(dst2, validList(i), task.DotSize * 5, task.scalarColors(indexList(i)), -1, task.lineType)
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(validList(i), task.steadyCam.M))
                Circle(dst0, ptSteady, task.DotSize * 5, cv.Scalar.All(indexList(i) + 1), -1, task.lineType)
            Next

            dst3 = Palettize(dst0, 0)
            If task.heartBeat Then
                labels(2) = CStr(validList.Count) + " features were tracked (see color) while " +
                        CStr(ptList.Count - validList.Count) + " were lost..."
            End If
        End Sub
    End Class





    Public Class XR_SteadyM_Delaunay : Inherits TaskParent
        Dim ptList As New List(Of cv.Point)
        Dim ptListLast As New List(Of cv.Point)
        Dim indexList As New List(Of Integer)
        Public delaunay As New Delaunay_Basics
        Public Sub New()
            delaunay.useFeatures = True
            desc = "Use the inverseM in SteadyCam_Basics to track points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            delaunay.Run(task.gray)
            delaunay.dst3.ConvertTo(dst1, cv.MatType.CV_8U)

            ptList.Clear()
            indexList.Clear()
            Dim newPoints As New List(Of cv.Point)
            For Each pt In ptListLast
                Dim ptAligned = WarpAffine_Basics.WarpPoint(pt, task.steadyCam.M)
                Dim index = dst1.Get(Of Byte)(ptAligned.Y, ptAligned.X)
                If indexList.Contains(index) Then
                    newPoints.Add(pt)
                Else
                    indexList.Add(index)
                    ptList.Add(WarpAffine_Basics.WarpPoint(ptAligned, task.steadyCam.inverseM))
                End If
            Next

            For Each pt In newPoints
                ptList.Add(pt)
                indexList.Add(indexList.Count)
            Next

            dst2 = task.color.Clone
            For i = 0 To ptList.Count - 1
                Circle(dst2, ptList(i), task.DotSize * 3, task.scalarColors(indexList(i)), -1, task.lineType)
            Next

            WarpAffine(dst1, dst3, task.steadyCam.M, dst3.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            ptListLast = New List(Of cv.Point)(delaunay.feat.features)
        End Sub
    End Class





    Public Class SteadyM_Delaunay : Inherits TaskParent
        Dim indexList As New List(Of Integer)
        Dim feat As New Feature_Basics
        Dim ptList As New List(Of cv.Point)
        Dim validList As New List(Of cv.Point)
        Public delaunay As New Delaunay_Basics
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of features."
            desc = "Use the inverseM in SteadyCam_Basics with Delaunayto track points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            If validList.Count < 3 Then
                dst1.SetTo(0)
                ptList = New List(Of cv.Point)(feat.features)

                delaunay.ptList.Clear()
                For Each pt In ptList
                    delaunay.ptList.Add(New cv.Point2f(pt.X, pt.Y))
                Next

                delaunay.Run(emptyMat)
                delaunay.dst3.ConvertTo(dst1, cv.MatType.CV_8U)

                WarpAffine(dst1, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            validList.Clear()
            indexList.Clear()
            For Each pt In feat.features
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(pt, task.steadyCam.M))
                Dim index = dst0.Get(Of Byte)(ptSteady.Y, ptSteady.X)
                If index <> 0 Then
                    validList.Add(pt)
                    indexList.Add(index - 1)
                End If
            Next

            dst2 = task.color.Clone
            dst0.SetTo(0)
            For i = 0 To validList.Count - 1
                Circle(dst2, validList(i), task.DotSize * 5, task.scalarColors(indexList(i)), -1, task.lineType)
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(validList(i), task.steadyCam.M))
                Circle(dst0, ptSteady, task.DotSize * 5, cv.Scalar.All(indexList(i) + 1), -1, task.lineType)
            Next

            dst3 = Palettize(dst0, 0)
            If task.heartBeat Then
                labels(2) = CStr(validList.Count) + " features were tracked (see color) while " +
                        CStr(ptList.Count - validList.Count) + " were lost..."
            End If
        End Sub
    End Class





    Public Class SteadyM_Lines : Inherits TaskParent
        Dim indexList As New List(Of Integer)
        Dim lpList As New List(Of lpData)
        Dim validList As New List(Of lpData)
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of lines."
            desc = "Use the inverseM in SteadyCam_Basics to track lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lineWidth = task.lineWidth * 5

            If validList.Count < 3 Then
                dst1.SetTo(0)
                Dim index = 1
                lpList = New List(Of lpData)(task.lines.lpList)
                For Each lp In lpList
                    Line(dst1, lp.p1, lp.p2, cv.Scalar.All(index), lineWidth, cv.LineTypes.Link8)
                    index += 1
                Next
                WarpAffine(dst1, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            validList.Clear()
            indexList.Clear()
            For Each lp In task.lines.lpList
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                Dim index1 = dst0.Get(Of Byte)(p1.Y, p2.X)
                Dim index2 = dst0.Get(Of Byte)(p2.Y, p2.X)
                If index1 <> 0 And index2 <> 0 Then
                    validList.Add(lp)
                    indexList.Add(index1 - 1)
                End If
            Next

            dst2 = task.color.Clone
            dst0.SetTo(0)
            For i = 0 To validList.Count - 1
                Dim lp = validList(i)
                Line(dst2, lp.p1, lp.p2, task.scalarColors(indexList(i)), task.lineWidth, cv.LineTypes.Link8)
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                Line(dst0, p1, p2, cv.Scalar.All(indexList(i) + 1), lineWidth, cv.LineTypes.Link8)
            Next

            dst3 = Palettize(dst0, 0)
            If task.heartBeat Then
                labels(2) = CStr(validList.Count) + " lines were tracked (see color) while " +
                        CStr(lpList.Count - validList.Count) + " were lost..."
            End If
        End Sub
    End Class




    Public Class XR_SteadyM_Longest : Inherits TaskParent
        Dim longest As New Line_Match2
        Dim lp As lpData
        Dim validLine As lpData
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of longest line."
            desc = "Use the inverseM in SteadyCam_Basics to track the longest line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lineWidth = task.lineWidth * 5
            dst2 = task.color.Clone
            longest.Run(task.gray)
            If longest.lp Is Nothing Then Exit Sub

            If validLine Is Nothing Then
                lp = longest.lp
                dst0.SetTo(0)
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                Dim lpSteady = New lpData(p1, p2)
                Line(dst0, lpSteady.ptE1, lpSteady.ptE2, cv.Scalar.All(1), lineWidth, cv.LineTypes.Link8)

                validLine = lp
                Line(dst2, validLine.p1, validLine.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
            Else
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(validLine.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(validLine.p2, task.steadyCam.M))
                Dim index1 = dst0.Get(Of Byte)(p1.Y, p2.X)
                Dim index2 = dst0.Get(Of Byte)(p2.Y, p2.X)
                If index1 <> 0 Or index2 <> 0 Then validLine = longest.lp Else validLine = Nothing

                If validLine IsNot Nothing Then
                    dst0.SetTo(0)
                    Line(dst2, validLine.p1, validLine.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)

                    Dim lpSteady = New lpData(p1, p2)
                    Line(dst0, lpSteady.ptE1, lpSteady.ptE2, cv.Scalar.All(1), lineWidth, cv.LineTypes.Link8)

                    dst3 = Palettize(dst0, 0)
                    labels(2) = "Longest line was found and tracked (see color)"
                Else
                    labels(2) = "Longest line was NOT found or could not be tracked."
                End If
            End If
        End Sub
    End Class




    Public Class SteadyM_Longest : Inherits TaskParent
        Dim longest As lpData
        Dim correlation As Double
        Dim template As cv.Mat
        Public Sub New()
            labels(3) = "SteadyCam map of longest line."
            desc = "Use the inverseM in SteadyCam_Basics to track the longest line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lineWidth = task.lineWidth * 5
            dst2 = task.color.Clone
            labels(2) = "Longest line was NOT found or could not be tracked."
            If task.lines.lpList.Count = 0 Then Exit Sub

            If longest Is Nothing Then
                longest = task.lines.lpList(0)
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p2, task.steadyCam.M))
                Dim lpSteady = New lpData(p1, p2)

                dst3.SetTo(0)
                Line(dst3, lpSteady.ptE1, lpSteady.ptE2, cv.Scalar.All(128), lineWidth, cv.LineTypes.Link8)

                Line(dst2, longest.p1, longest.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
                template = src(longest.rect).Clone
            Else
                Dim result As New Mat()
                Cv2.MatchTemplate(src(longest.rect), template, result, TemplateMatchModes.CCorrNormed)

                correlation = result.Get(Of Double)(0, 0)
                SetTrueText(correlation.ToString(fmt3), longest.rect.TopLeft)
                If correlation >= task.fOptions.MatchCorrSlider.Value / 100 Then
                    'Dim M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
                    'Dim pt = New cv.Point(longest.rect.X + match.newCenter.X, longest.rect.Y + match.newCenter.Y)
                    'shiftXY = validatePoint(New cv.Point2f(longest.ptCenter.X - pt.X, longest.ptCenter.Y - pt.Y))
                    ''kalman.kInput = {shiftXY.X, shiftXY.Y}
                    ''kalman.Run(emptyMat)
                    ''shiftXY = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))

                    'M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, shiftXY.X)
                    'M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, shiftXY.Y)
                    'Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p1, M))
                    'Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p2, M))

                    'longest = New lpData(p1, p2)
                    strOut = " confirms that line is still there"
                Else
                    longest = Nothing
                    For Each lp In task.lines.lpList
                        Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                        Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                        Dim index1 = dst3.Get(Of Byte)(p1.Y, p1.X)
                        Dim index2 = dst3.Get(Of Byte)(p2.Y, p2.X)
                        If index1 <> 0 And index2 <> 0 Then
                            longest = lp
                            Exit For
                        End If
                    Next
                    strOut = " - reseting longest line"
                End If
                labels(2) = "Correlation " + correlation.ToString(fmt3) + strOut
            End If

            If longest IsNot Nothing Then
                dst3.SetTo(0)
                Line(dst2, longest.p1, longest.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
                Line(dst3, longest.ptE1, longest.ptE2, cv.Scalar.All(128), lineWidth, cv.LineTypes.Link8)
            End If
        End Sub
    End Class
End Namespace