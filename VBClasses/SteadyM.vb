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
            desc = "Use the M mat in SteadyCam_Basics to track points."
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
            desc = "Use M in SteadyCam_Basics to track points."
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






    Public Class XR_SteadyM_Longest1 : Inherits TaskParent
        Dim longest As New Line_Match2
        Dim lp As lpData
        Dim validLine As lpData
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of longest line."
            desc = "Use M in SteadyCam_Basics to track the longest line."
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




    Public Class XR_SteadyM_Longest : Inherits TaskParent
        Dim longest As lpData
        Dim retained As New List(Of Integer)
        Public Sub New()
            labels(3) = "SteadyCam map of longest line."
            desc = "Use M in SteadyCam_Basics to track the longest line."
        End Sub
        Public Shared Function checkLine(lp As lpData, map As cv.Mat) As Boolean
            If CountNonZero(task.motion.motionMask(lp.rect)) > 0 Then Return False ' motion near the line
            Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
            Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
            Dim index1 = map.Get(Of Byte)(p1.Y, p1.X)
            Dim index2 = map.Get(Of Byte)(p2.Y, p2.X)
            Return index1 <> 0 And index2 <> 0
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.color.Clone
            If task.lines.lpList.Count = 0 Then Exit Sub

            If longest IsNot Nothing AndAlso checkLine(longest, dst3) Then retained.Add(1) Else longest = Nothing

            If longest Is Nothing Then
                retained.Add(0)
                longest = task.lines.lpList(0)
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p2, task.steadyCam.M))

                Dim lpSteady = New lpData(p1, p2)
                dst3.SetTo(0)
                Line(dst3, lpSteady.ptE1, lpSteady.ptE2, cv.Scalar.All(128), task.lineWidth, cv.LineTypes.Link8)
            End If

            If longest IsNot Nothing Then
                Line(dst2, longest.p1, longest.p2, task.highlight, task.lineWidth + 1, cv.LineTypes.Link8)
            End If

            If retained.Count > 100 Then retained.RemoveAt(0)
            Dim avg = retained.Average
            labels(2) = "Longest line was found " + avg.ToString("#0%") + " of the time"
        End Sub
    End Class




    Public Class XR_SteadyM_LongestMatch : Inherits TaskParent
        Dim longest As lpData
        Dim template As cv.Mat
        Dim match As New Match_Basics
        Dim retained As New List(Of Integer)
        Public Sub New()
            labels(3) = "SteadyCam map of longest line."
            desc = "Cursor.ai: Keep the saved longest line when longest.rect still matches the stored template above MatchCorrSlider."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.color.Clone
            If src.Channels <> 1 Then src = task.gray
            If task.lines.lpList.Count = 0 Then Exit Sub

            Dim threshold = task.fOptions.MatchCorrSlider.Value / 100.0F
            If longest IsNot Nothing AndAlso template IsNot Nothing Then
                Dim r = ValidateRect(longest.rect)
                If r.Width >= template.Width And r.Height >= template.Height Then
                    match.template = template
                    match.Run(src(r))
                    If match.correlation >= threshold Then
                        retained.Add(1)
                        Line(dst2, longest.p1, longest.p2, task.highlight, task.lineWidth + 1, cv.LineTypes.Link8)
                        If retained.Count > 100 Then retained.RemoveAt(0)
                        labels(2) = "corr=" + match.correlation.ToString(fmt3) +
                                    "  keeping longest  found " + retained.Average.ToString("#0%") + " of the time"
                        Exit Sub
                    End If
                End If
            End If

            If longest IsNot Nothing AndAlso XR_SteadyM_Longest.checkLine(longest, dst3) Then
                retained.Add(1)
            Else
                longest = Nothing
            End If

            If longest Is Nothing Then
                retained.Add(0)
                longest = task.lines.lpList(0)
                template = src(ValidateRect(longest.rect)).Clone
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(longest.p2, task.steadyCam.M))

                Dim lpSteady = New lpData(p1, p2)
                dst3.SetTo(0)
                Line(dst3, lpSteady.ptE1, lpSteady.ptE2, cv.Scalar.All(128), task.lineWidth, cv.LineTypes.Link8)
            End If

            If longest IsNot Nothing Then
                Line(dst2, longest.p1, longest.p2, task.highlight, task.lineWidth + 1, cv.LineTypes.Link8)
            End If

            If retained.Count > 100 Then retained.RemoveAt(0)
            Dim avg = retained.Average
            labels(2) = "Longest line was found " + avg.ToString("#0%") + " of the time  corr threshold=" + threshold.ToString(fmt2)
        End Sub
    End Class





    Public Class SteadyM_Lines : Inherits TaskParent
        Dim lpList As New List(Of lpData)
        Dim validList As New List(Of lpData)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "SteadyCam map of lines.  It is updated when < X lines are found."
            desc = "Use M in SteadyCam_Basics to track lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If validList.Count < 5 Then
                lpList = New List(Of lpData)(task.lines.lpList)
                WarpAffine(task.lines.dst1, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            validList.Clear()
            For Each lp In task.lines.lpList
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                Dim index1 = dst0.Get(Of Byte)(p1.Y, p2.X)
                Dim index2 = dst0.Get(Of Byte)(p2.Y, p2.X)
                If index1 > 0 Or index2 > 0 Then validList.Add(lp)
            Next

            dst2 = task.color.Clone
            dst1.SetTo(0)
            For Each lp In validList
                Line(dst2, lp.p1, lp.p2, task.scalarColors(lp.index), task.lineWidth + 2, cv.LineTypes.Link8)
                Line(dst1, lp.p1, lp.p2, task.scalarColors(lp.index), task.lineWidth, task.lineType)
            Next

            dst3 = Palettize(dst0, 0)
            If task.heartBeat Then
                labels(2) = CStr(validList.Count) + " lines were tracked (see color) while " +
                            CStr(lpList.Count - validList.Count) + " were lost..."
                labels(1) = CStr(validList.Count) + " lines were found through the SteadyCam map."
            End If
        End Sub
    End Class





    Public Class SteadyM_DelaunayPoints : Inherits TaskParent
        Dim indexList As New List(Of Integer)
        Dim feat As New Feature_Basics
        Dim ptList As New List(Of cv.Point)
        Dim validList As New List(Of cv.Point)
        Dim delaunay As New Delaunay_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of features."
            desc = "Use M in SteadyCam_Basics with Delaunayto track points."
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
                Circle(dst2, validList(i), task.steadyLineWidth, task.scalarColors(indexList(i)), -1, task.lineType)
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





    Public Class SteadyM_DelaunayLines : Inherits TaskParent
        Dim validList As New List(Of lpData)
        Dim delaunay As New Delaunay_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "SteadyCam map of lines."
            desc = "Use M in SteadyCam_Basics with Delaunay to track points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If validList.Count < 3 Then
                dst1.SetTo(0)

                delaunay.ptList.Clear()
                For Each lp In task.lines.lpList
                    delaunay.ptList.Add(New cv.Point2f(lp.ptCenter.X, lp.ptCenter.Y))
                Next

                delaunay.Run(emptyMat)
                delaunay.dst3.ConvertTo(dst1, cv.MatType.CV_8U)

                WarpAffine(dst1, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            validList.Clear()
            For Each lp In task.lines.lpList
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(lp.ptCenter, task.steadyCam.M))
                Dim index = dst0.Get(Of Byte)(ptSteady.Y, ptSteady.X)
                If index <> 0 Then validList.Add(lp)
            Next

            dst2 = task.color.Clone
            dst0.SetTo(0)
            For Each lp In validList
                Line(dst2, lp.p1, lp.p2, task.scalarColors(lp.index), task.steadyLineWidth, task.lineType)
                Dim p1 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p1, task.steadyCam.M))
                Dim p2 = validatePoint(WarpAffine_Basics.WarpPoint(lp.p2, task.steadyCam.M))
                Line(dst0, lp.p1, lp.p2, cv.Scalar.All(lp.index), task.steadyLineWidth, cv.LineTypes.Link8)
            Next

            dst3 = Palettize(dst0, 0)
            If task.heartBeat Then
                labels(2) = CStr(validList.Count) + " lines were tracked while " +
                        CStr(task.lines.lpList.Count - validList.Count) + " were lost..."
            End If
        End Sub
    End Class





    Public Class XR_SteadyM_Cells : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            labels(3) = "SteadyCam version of redc.rcIndexMap."
            desc = "Use M in SteadyCam_Basics maintain the index for each redC cell."
        End Sub
        Public Shared Function setAge(rcList As List(Of rcDataOld), rcLastList As List(Of rcDataOld), map As cv.Mat) As List(Of rcDataOld)
            Dim usedList As New List(Of Integer)
            Dim rcListStable As New List(Of rcDataOld)
            For Each rc In rcList
                Dim previousIndex = map.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If previousIndex > 0 And usedList.Contains(previousIndex) = False Then
                    rc.index = previousIndex
                    If rc.index < rcLastList.Count Then
                        rc.age = rcLastList(rc.index).age + 1
                        If rc.age >= 1000 Then rc.age = 100
                        rcListStable.Add(rc)
                        usedList.Add(rc.index)
                    End If
                Else
                    rc.index = 0
                End If
            Next

            'Dim indexNew = 1
            'For Each rc In rcList
            '    If rc.index = 0 Then
            '        While usedList.Contains(indexNew)
            '            indexNew += 1
            '        End While
            '        rc.index = indexNew
            '        rc.age = 1
            '        usedList.Add(indexNew)
            '    End If
            'Next

            Return rcListStable
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcLastList = New List(Of rcDataOld)(redC.rcList)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.heartBeatLT Then
                WarpAffine(redC.rcIndexMap, dst0, task.steadyCam.M, dst0.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            End If

            setAge(redC.rcList, rcLastList, dst0)
            For Each rc In redC.rcList
                Dim ptSteady = validatePoint(WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.M))
                Dim index = dst0.Get(Of Byte)(ptSteady.Y, ptSteady.X)
                If index <> 0 Then rc.index = index
            Next

            dst1.SetTo(0)
            For Each rc In redC.rcList
                If rc.index > 0 Then dst1(rc.rect).SetTo(rc.index, rc.mask)
            Next

            dst3 = Palettize(dst1, 0)
        End Sub
    End Class





    Public Class SteadyM_RedCTest : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim rcIndexMap As cv.Mat
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Display the raw RedC rcIndexMap"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.heartBeatLT Then rcIndexMap = redC.rcIndexMap.Clone

            dst1.SetTo(0)
            For Each rc In redC.rcList
                Dim val1 = rcIndexMap.Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                Dim val2 = redC.rcIndexMap.Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                If val1 = val2 Then dst1(rc.rect).SetTo(rc.index, rc.mask)
            Next
            dst3 = Palettize(dst1, 0)

            If task.rcD IsNot Nothing Then SetTrueText(task.rcD.displayCell, 1)
        End Sub
    End Class

End Namespace