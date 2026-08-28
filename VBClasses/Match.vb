Imports System.Text.RegularExpressions
Imports System.Threading
Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Match_Basics : Inherits TaskParent
        Public template As New Mat ' caller provides this!
        Public correlation As Single
        Public newCenter As cv.Point
        Public newRect As New cv.Rect
        Public mm As mmData
        Public correlationMat As New cv.Mat
        Public Sub New()
            desc = "Find the requested template in an image.  Managing template is responsibility of caller " +
                   "(allows multiple targets per image.)"
        End Sub
        Public Shared Function showCorrelationMat(correlationMat As cv.Mat, minVal As Single, sz As cv.Size) As cv.Mat
            Dim dst As New cv.Mat(sz, cv.MatType.CV_8U, 0)
            Dim x = (dst.Width - correlationMat.Width) / 2
            Dim y = (dst.Height - correlationMat.Height) / 2
            Dim r = New cv.Rect(x, y, correlationMat.Width, correlationMat.Height)
            ConvertScaleAbs(correlationMat, dst(r), 255, -minVal)
            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText("Match is called often so no need for standalone test.")
                Exit Sub
            End If

            correlationMat = New cv.Mat
            MatchTemplate(template, src, correlationMat, TemplateMatchModes.CCoeffNormed)
            mm = GetMinMax(correlationMat)
            correlation = mm.maxVal

            dst3 = showCorrelationMat(correlationMat, mm.minVal, src.Size)
            Circle(dst3, newCenter, task.DotSize, black, -1, task.lineType)

            labels(2) = "Template (at right) has " + correlation.ToString(fmt3) + " Correlation to the src input"
            Dim w = template.Width, h = template.Height
            newCenter = New cv.Point(mm.maxLoc.X + w / 2, mm.maxLoc.Y + h / 2)
            newRect = New cv.Rect(mm.maxLoc.X, mm.maxLoc.Y, w, h)
            If standaloneTest() Then
                dst2.SetTo(0)
                Dim r = New cv.Rect(0, 0, src.Width, src.Height)
                dst2(r) = src
                Rectangle(dst2, newRect, white, task.lineWidth)
            End If
        End Sub
    End Class



    Public Class XR_Match_Basics1 : Inherits TaskParent
        Public template As New Mat ' caller provides this!
        Public correlation As Single
        Public newRect As New cv.Rect
        Public Sub New()
            desc = "Find the requested template in an image.  Managing template is responsibility of caller " +
                   "(allows multiple targets per image.)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText("Match is called often so no need for standalone test.")
                Exit Sub
            End If

            MatchTemplate(template, src, dst0, TemplateMatchModes.CCoeffNormed)
            Dim mm = GetMinMax(dst0)

            correlation = mm.maxVal
            labels(2) = "Template has " + correlation.ToString(fmt3) + " Correlation to the src input"
            newRect = New cv.Rect(mm.maxLoc.X, mm.maxLoc.Y, template.Width, template.Height)
            If standaloneTest() Then
                dst2 = task.gray.Clone
                Rectangle(dst2, newRect, white, task.lineWidth)
                Line(dst2, task.lines.lpList(0).p1, task.lines.lpList(0).p2, white, task.lineWidth, task.lineType)
            End If
        End Sub
    End Class







    Public Class XR_Match_BasicsTest : Inherits TaskParent
        Public match As New Match_Basics
        Dim matchRect As cv.Rect
        Public Sub New()
            labels = {"", "", "Draw a rectangle to be tracked", "Highest probability of a match at the brightest cv.Point below"}
            desc = "Test the Match_Basics algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If task.heartBeatLT Then
                    matchRect = ValidateRect(task.lines.lpList(0).rect)
                    match.template = src(matchRect)
                End If
            End If

            match.Run(src)
            matchRect = match.newRect

            If standaloneTest() Then
                dst2 = src
                Circle(dst2, match.newCenter, task.DotSize, white, -1, task.lineType)
                Rectangle(dst2, matchRect, task.highlight, task.lineWidth)
                Normalize(match.dst0, dst3, 0, 255, NormTypes.MinMax)
                SetTrueText(match.correlation.ToString(fmt3), match.newCenter)
            End If
        End Sub
    End Class








    Public Class XR_Match_RandomTest : Inherits TaskParent
        Dim flow As New Font_FlowText
        Public template As Mat
        Public correlationMat As New Mat
        Public correlation As Single
        Public mm As mmData
        Public minCorrelation As Single
        Public maxCorrelation As Single
        Public options As New Options_Features
        Public Sub New()
            flow.parentData = Me
            desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If standaloneTest() Then
                If task.optionsChanged Then
                    maxCorrelation = Single.MinValue
                    minCorrelation = Single.MaxValue
                End If
                template = New Mat(New Size(task.fOptions.FeatureSizeSlider.Value, 1), MatType.CV_32FC1)
                src = New Mat(New Size(task.fOptions.FeatureSizeSlider.Value, 1), MatType.CV_32FC1)
                Randn(template, 100, 25)
                Randn(src, 0, 25)
            End If

            MatchTemplate(template, src, correlationMat, options.matchOption)
            mm = GetMinMax(correlationMat)
            mm.maxLoc = New cv.Point(mm.maxLoc.X + template.Width / 2, mm.maxLoc.Y + template.Height / 2)
            correlation = mm.maxVal
            If correlation < minCorrelation Then minCorrelation = correlation
            If correlation > maxCorrelation Then maxCorrelation = correlation
            If standaloneTest() Then
                dst2.SetTo(0)
                If task.heartBeat Then
                    labels(2) = "For " + CStr(template.Cols) + " test samples correlation = " + correlation.ToString(fmt2)
                End If
                flow.nextMsg = "Correlation = " + correlation.ToString("#,##0.00")
                flow.Run(src)
                SetTrueText("The expectation is that the " + CStr(template.Cols) + " random test samples should produce" + vbCrLf +
                            " a correlation coefficient near zero" + vbCrLf +
                            "The larger the sample size, the closer to zero the correlation will be. " + vbCrLf +
                            "Adjust 'Feature Samples' in the Feature Options to test further." + vbCrLf +
                            "There should also be symmetry in the min and max around zero." + vbCrLf + vbCrLf +
                            "Min Correlation = " + minCorrelation.ToString(fmt3) + vbCrLf +
                            "Max Correlation = " + maxCorrelation.ToString(fmt3), 3)
            End If
        End Sub
    End Class







    Public Class XR_Match_Motion : Inherits TaskParent
        Dim options As New Options_Features
        Public mask As Mat
        Dim optionsMatch As New Options_Match
        Public Sub New()
            mask = New Mat(dst2.Size(), MatType.CV_8U)
            dst3 = mask.Clone
            desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            optionsMatch.Run()

            dst2 = src.Clone
            If dst2.Channels() = 3 Then CvtColor(dst2, dst2, ColorConversionCodes.BGR2GRAY)

            Static lastFrame As Mat = dst2.Clone()
            Dim saveFrame As Mat = dst2.Clone
            Dim updateCount As Integer
            mask.SetTo(0)

            For Each roi In task.gridRects
                Dim correlation As New Mat, mean As Single, stdev As Single
                MeanStdDev(dst2(roi), mean, stdev)
                If stdev > optionsMatch.stdevThreshold Then
                    MatchTemplate(dst2(roi), lastFrame(roi), correlation, options.matchOption)
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    If correlation.Get(Of Single)(0, 0) < task.fCorrThreshold Then
                        Interlocked.Increment(updateCount)
                    Else
                        mask(roi).SetTo(255)
                        dst2(roi).SetTo(0)
                    End If
                    SetTrueText(correlation.Get(Of Single)(0, 0).ToString(fmt2), pt, 2)
                Else
                    Interlocked.Increment(updateCount)
                End If
            Next

            dst2.SetTo(255, task.gridMask)
            dst3.SetTo(0)
            saveFrame.CopyTo(dst3, mask)
            lastFrame = saveFrame
            Dim corrPercent = task.fCorrThreshold.ToString("0.0%") + " correlation"
            labels(2) = "Correlation value for each cell is shown. " + CStr(updateCount) + " of " +
                         CStr(task.gridRects.Count) + " with < " + corrPercent + " or stdev < " +
                         optionsMatch.stdevThreshold.ToString(fmt0)
            labels(3) = CStr(task.gridRects.Count - updateCount) + " segments out of " + CStr(task.gridRects.Count) + " had > " + corrPercent
        End Sub
    End Class




    Public Class XR_Match_TraceRedC : Inherits TaskParent
        Dim frameList As New List(Of Mat)
        Dim redC As New RedColor_Basics
        Public Sub New()
            dst0 = New Mat(dst0.Size(), MatType.CV_32S, 0)
            dst1 = New Mat(dst1.Size(), MatType.CV_32S, 0)
            dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Track each RedCloud cell center to highlight zones of RedCloud cell instability.  Look for clusters of points in dst2."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst3 = redC.dst2
            labels(3) = redC.labels(2)

            If task.heartBeat Then dst2.SetTo(0)
            If task.optionsChanged Then frameList.Clear()

            dst0.SetTo(0)
            Dim points As New List(Of cv.Point)

            For Each rc In redC.rcList
                dst0.Set(Of Byte)(rc.maxDist.Y, rc.maxDist.X, 1)
            Next
            labels(2) = CStr(redC.rcList.Count) + " cells added"

            frameList.Add(dst0.Clone)
            If frameList.Count >= task.fOptions.FrameHistoryCount.Value Then
                dst1 = dst1.Subtract(frameList(0))
                frameList.RemoveAt(0)
            End If
            dst1 = dst1.Add(dst0)
            dst1.ConvertTo(dst2, MatType.CV_8U)
            Threshold(dst2, dst2, 0, 255, ThresholdTypes.Binary)
        End Sub
    End Class






    Public Class XR_Match_GoodFeatureKNN : Inherits TaskParent
        Public knn As New KNN_OneToOne
        Dim frameList As New List(Of Mat)
        Dim feat As New Feature_Basics
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, 5)
            dst0 = New Mat(dst2.Size(), MatType.CV_8UC1, 0)
            dst1 = New Mat(dst2.Size(), MatType.CV_8UC1, 0)
            labels(3) = "Shake camera to see tracking of the highlighted features"
            desc = "Track the GoodFeatures with KNN"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            Static distSlider = OptionParent.FindSlider("Maximum travel distance per frame")
            Dim maxDistance = distSlider.Value

            knn.queries.Clear()
            For Each pt In feat.features
                knn.queries.Add(pt)
            Next
            knn.Run(src)

            If task.optionsChanged Then
                frameList.Clear()
                dst1.SetTo(0)
            End If

            dst0.SetTo(0)
            For Each lp In knn.matches
                If lp.p1.DistanceTo(lp.p2) <= maxDistance Then Line(dst0, lp.p1, lp.p2, 255, task.lineWidth + 2, LineTypes.Link4)
            Next
            frameList.Add(dst0.Clone)
            If frameList.Count >= task.fOptions.FrameHistoryCount.Value Then
                dst1 = dst1.Subtract(frameList(0))
                frameList.RemoveAt(0)
            End If
            dst1 += dst0
            Threshold(dst1, dst2, 0, 255, ThresholdTypes.Binary)

            dst3 = src
            dst3.SetTo(task.highlight, dst2)
        End Sub
    End Class







    Public Class XR_Match_Point : Inherits TaskParent
        Public pt As Point2f
        Public target As Mat
        Public correlation As Single
        Public radius As Integer
        Public searchRect As cv.Rect
        Dim options As New Options_Features
        Public Sub New()
            labels(2) = "Rectangle shown is the search rectangle."
            desc = "Track the changes for the selected cv.Point"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                SetTrueText("Set the target mat and the pt then run to track an individual cv.Point." + vbCrLf +
                            "After running, the pt is updated with the new location and correlation with the updated correlation." + vbCrLf +
                            "There is no output when run standaloneTest()")
                Exit Sub
            End If

            Dim radius = task.gridWH / 2

            Dim rect = ValidateRect(New cv.Rect(pt.X - radius, pt.Y - radius, task.gridWH, task.gridWH))
            searchRect = ValidateRect(New cv.Rect(rect.X - task.gridWH, rect.Y - task.gridWH,
                                                  task.gridWH * 3, task.gridWH * 3))
            MatchTemplate(target(rect), src(searchRect), dst0, TemplateMatchModes.CCoeffNormed)
            Dim mmData = GetMinMax(dst0)
            correlation = mmData.maxVal
            pt = New Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
            Circle(src, pt, task.DotSize, white, -1, task.lineType)
            Rectangle(src, searchRect, Scalar.Yellow, 1)
        End Sub
    End Class









    Public Class Match_Brick : Inherits TaskParent
        Public match As New Match_Basics
        Public gridIndex As Integer ' provide this - it identifies the gRect 
        Public correlation As Single
        Public deltaX As Single, deltaY As Single
        Public Sub New()
            desc = "Match a gRect's movement from the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                gridIndex = task.gridMap.Get(Of Integer)(task.lines.lpList(0).p1.Y, task.lines.lpList(0).p1.X)
            End If
            Static lastImage As Mat = task.gray.Clone

            Dim rect = task.gridRects(gridIndex)
            match.template = task.gray(rect)
            Dim searchrect = task.gridNabeRects(gridIndex)

            match.Run(lastImage(searchrect))
            correlation = match.correlation

            Dim offsetX = rect.X - searchrect.X
            Dim offsetY = rect.Y - searchrect.Y

            deltaX = match.newRect.X - offsetX
            deltaY = match.newRect.Y - offsetY

            If standaloneTest() Then
                Dim newRect = rect
                newRect.X += deltaX
                newRect.Y += deltaY

                dst2 = task.gray.Clone
                Rectangle(dst2, newRect, white, task.lineWidth, task.lineType)
                Rectangle(dst2, task.gridNabeRects(gridIndex), white, task.lineWidth, task.lineType)

                dst3 = lastImage
                Rectangle(dst3, newRect, white, task.lineWidth, task.lineType)
            End If
            labels(2) = "Delta X/Y = " + deltaX.ToString(fmt2) + "/" + deltaY.ToString(fmt2) + ", corr: " +
                         correlation.ToString(fmt3)

            If correlation < task.fCorrThreshold Then lastImage = task.gray.Clone
        End Sub
    End Class








    Public Class XR_Match_LinePairTest : Inherits TaskParent
        Public ptx(2 - 1) As Point2f
        Public target(ptx.Count - 1) As Mat
        Public correlation(ptx.Count - 1)
        Public Sub New()
            desc = "Use MatchTemplate to find the new location of the template and update the cv.Point provided."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim radius = task.gridWH / 2

            Dim rect As cv.Rect

            If target(0) IsNot Nothing And correlation(0) < task.fCorrThreshold Then target(0) = Nothing
            If task.mouseClickFlag Then
                ptx(0) = task.clickPoint
                ptx(1) = New Point2f(task.msRNG.Next(task.gridWH, dst2.Width - 2 * task.gridWH),
                                        task.msRNG.Next(task.gridWH, dst2.Height - 2 * task.gridWH))

                rect = ValidateRect(New cv.Rect(ptx(0).X - radius, ptx(0).Y - radius, task.gridWH, task.gridWH))
                target(0) = src(rect)

                rect = ValidateRect(New cv.Rect(ptx(1).X - radius, ptx(1).Y - radius, task.gridWH, task.gridWH))
                target(1) = src(rect)
            End If

            If target(0) Is Nothing Or target(1) Is Nothing Then
                dst3 = src
                SetTrueText("Click anywhere in the image to start the algorithm.")
                Exit Sub
            End If

            dst3 = src.Clone
            dst2 = New Mat(dst2.Size(), MatType.CV_32FC1, 0)

            For i = 0 To ptx.Length - 1
                rect = ValidateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, task.gridWH, task.gridWH))
                Dim searchRect = ValidateRect(New cv.Rect(rect.X - task.gridWH, rect.Y - task.gridWH,
                                                          task.gridWH * 3, task.gridWH * 3))
                MatchTemplate(target(i), src(searchRect), dst0, TemplateMatchModes.CCoeffNormed)
                Dim mmData = GetMinMax(dst0)
                correlation(i) = mmData.maxVal
                If i = 0 Then
                    dst0.CopyTo(dst2(New cv.Rect(0, 0, dst0.Width, dst0.Height)))
                    Threshold(dst2, dst2, task.fCorrThreshold, 255, ThresholdTypes.Binary)
                End If
                ptx(i) = New Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
                Circle(dst3, ptx(i), task.DotSize, task.highlight, -1, task.lineType)
                Rectangle(dst3, searchRect, Scalar.Yellow, 1)
                rect = ValidateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, task.gridWH, task.gridWH))
                target(i) = task.color(rect)
            Next

            labels(3) = "p1 = " + CStr(ptx(0).X) + "," + CStr(ptx(0).Y) + " p2 = " + CStr(ptx(1).X) + "," + CStr(ptx(1).Y)
            labels(2) = "Correlation = " + correlation(0).ToString(fmt3) + " Search result is " + CStr(dst0.Width) + "X" + CStr(dst0.Height)
        End Sub
    End Class





    Public Class Match_LinesKNN : Inherits TaskParent
        Dim knn As New KNN_Minimal
        Public trainInput As New List(Of Vec4f)
        Public queries As New List(Of Vec4f)
        Public Sub New()
            labels(2) = "Match lines on the heartbeat using the line extended to the image edges."
            desc = "Use the 2 points from a line as input to a 4-dimension KNN"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = task.lines.lpList

            dst2 = src
            Static lastPt As New List(Of lpData)(lplist)

            queries.Clear()
            For Each lp In lplist
                queries.Add(New Vec4f(lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
            Next
            If task.optionsChanged Then trainInput = New List(Of Vec4f)(queries)

            Dim dimension = 4
            knn.queryMat = Mat.FromPixelData(queries.Count, dimension, MatType.CV_32F, queries.ToArray)
            knn.trainMat = Mat.FromPixelData(trainInput.Count, dimension, MatType.CV_32F, trainInput.ToArray)
            knn.Run(src)

            For Each i In knn.result
                If i >= lplist.Count Then Continue For
                Dim lp = lplist(i)

                Dim index = knn.result(i, 0)
                If index >= 0 And index < lastPt.Count Then
                    Dim lastMP = lastPt(index)
                    Line(dst2, lp.p1, lastMP.p2, Scalar.Red, task.lineWidth, task.lineType)
                End If
            Next

            trainInput = New List(Of Vec4f)(queries)
            lastPt = New List(Of lpData)(lplist)
        End Sub
    End Class





    Public Class Match_DrawRect : Inherits TaskParent
        Public correlation As Single
        Public newCenter As cv.Point
        Public newRect As cv.Rect
        Dim template As cv.Mat
        Public Sub New()
            labels = {"", "", "Best match (drawRect center + predicted location)", "Match probabilities"}
            desc = "Cursor.ai: task.drawRect template provided, search the full image, show probabilities in dst3 and best match in dst2."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            If task.optionsChanged Or task.mouseClickFlag Then
                If task.drawRect.Width < 2 Or task.drawRect.Height < 2 Then
                    task.drawRect = New cv.Rect(dst2.Width \ 2 - 20, dst2.Height \ 2 - 20, 40, 40)
                End If
                template = src(task.drawRect).Clone
            End If

            Dim corr As New cv.Mat
            MatchTemplate(src, template, corr, TemplateMatchModes.CCoeffNormed)
            Dim mm = GetMinMax(corr)
            correlation = mm.maxVal
            newRect = New cv.Rect(mm.maxLoc.X, mm.maxLoc.Y, template.Width, template.Height)
            newCenter = New cv.Point(CInt(mm.maxLoc.X + template.Width / 2), CInt(mm.maxLoc.Y + template.Height / 2))

            If standaloneTest() Then
                Dim prob As New cv.Mat
                Normalize(corr, prob, 0, 255, NormTypes.MinMax)
                prob.ConvertTo(prob, MatType.CV_8U)
                dst3 = New cv.Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
                ' Align probability peaks with the matched template center in image coordinates.
                Dim place = New cv.Rect(template.Width \ 2, template.Height \ 2, corr.Width, corr.Height)
                place = ValidateRect(place)
                Dim copyW = Math.Min(prob.Width, place.Width)
                Dim copyH = Math.Min(prob.Height, place.Height)
                If copyW > 0 AndAlso copyH > 0 Then
                    prob(New cv.Rect(0, 0, copyW, copyH)).CopyTo(dst3(New cv.Rect(place.X, place.Y, copyW, copyH)))
                End If

                dst2 = task.color.Clone()
                Dim drawCenter = New cv.Point(task.drawRect.X + task.drawRect.Width \ 2,
                                              task.drawRect.Y + task.drawRect.Height \ 2)
                Rectangle(dst2, task.drawRect, white, task.lineWidth, task.lineType)
                Circle(dst2, drawCenter, task.DotSize + 2, white, -1, task.lineType)
                Rectangle(dst2, newRect, task.highlight, task.lineWidth, task.lineType)
                Circle(dst2, newCenter, task.DotSize, task.highlight, -1, task.lineType)
                SetTrueText(correlation.ToString(fmt3), newCenter, 2)

                ' update the template for the next frame
                'template = src(newRect).Clone
            End If


            labels(2) = "Best match corr=" + correlation.ToString(fmt3) + " at (" +
                        CStr(newCenter.X) + "," + CStr(newCenter.Y) + ")"
            labels(3) = "MatchTemplate probabilities (brighter = higher)"
        End Sub
    End Class




    Public Class Match_CenterRect : Inherits TaskParent
        Public match As New Match_Basics
        Dim kalman As New Kalman_Basics
        Public shiftXY As cv.Point2f
        Public forceRecenter As Boolean
        Public centerRect As cv.Rect
        Public M As New cv.Mat(2, 3, cv.MatType.CV_64FC1)
        Public useKalman As Boolean = True
        Public Sub New()
            M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, 0)
            M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, 0)
            desc = "Cursor.ai: Match the image center using Match_Basics to find X/Y shift; dst3 is gray shifted to align (black edges where missing)."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayOriginal

            If task.heartBeatLT Or forceRecenter Or task.optionsChanged Then
                forceRecenter = False

                centerRect = Rectangle_Basics.centerRect(src.Size, 3)
                match.template = src(centerRect).Clone
                shiftXY = New cv.Point2f(-shiftXY.X, -shiftXY.Y) ' return to zero, zero
                If standaloneTest() Then dst3 = src.Clone
            Else
                match.Run(src)
                If standaloneTest() Then
                    dst2 = Match_Basics.showCorrelationMat(match.correlationMat, match.mm.minVal, src.Size)
                    Rectangle(dst2, centerRect, white, task.lineWidth)
                    Circle(dst2, match.newCenter, task.DotSize, black, -1, task.lineType)
                End If

                If centerRect.Contains(match.newCenter) = False Then forceRecenter = True

                shiftXY = New cv.Point2f(src.Width \ 2 - match.newCenter.X, src.Height \ 2 - match.newCenter.Y)

                If useKalman Then
                    kalman.kInput = {shiftXY.X, shiftXY.Y}
                    kalman.Run(emptyMat)
                    shiftXY = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
                End If
            End If

            M.Set(Of Double)(0, 0, 1) : M.Set(Of Double)(0, 1, 0) : M.Set(Of Double)(0, 2, shiftXY.X)
            M.Set(Of Double)(1, 0, 0) : M.Set(Of Double)(1, 1, 1) : M.Set(Of Double)(1, 2, shiftXY.Y)

            If standaloneTest() Then
                ' Shift gray so content stays locked to the template frame; 
                WarpAffine(src, dst3, M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

                labels(2) = "corr=" + match.correlation.ToString(fmt3) + "  shift=" + shiftXY.ToString
                labels(3) = "Aligned gray; missing data is black."
            End If
        End Sub
    End Class





    Public Class Match_Quadrants : Inherits TaskParent
        Dim matchCenter As New Match_CenterRect
        Dim quads(3) As cv.Rect
        Dim templates(quads.Length - 1) As cv.Mat
        Public Sub New()
            matchCenter.displayRequest = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            quads = Rectangle_Basics.buildQuads()
            desc = "Run Match_CenterRect on each of the 4 quadrants of the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayOriginal

            dst2.SetTo(0)
            Dim forceRecenter As Boolean
            Static saveRecenter As Boolean
            For i = 0 To quads.Length - 1
                If templates(i) IsNot Nothing Then matchCenter.match.template = templates(i).Clone
                matchCenter.forceRecenter = saveRecenter
                matchCenter.Run(src(quads(i)))
                templates(i) = matchCenter.match.template.Clone

                If matchCenter.forceRecenter Then forceRecenter = True
                If task.firstPass = False Then
                    dst2(quads(i)) = matchCenter.dst2.Clone
                    Rectangle(dst2(quads(i)), matchCenter.centerRect, white, task.lineWidth)
                    Circle(dst2(quads(i)), matchCenter.match.newCenter, task.DotSize, black, -1, task.lineType)

                    dst3(quads(i)) = matchCenter.dst3.Clone
                End If
            Next

            saveRecenter = forceRecenter
        End Sub
    End Class






    Public Class Match_Lines : Inherits TaskParent
        Dim accum As New AddWeighted_Accumulate
        Public Sub New()
            labels(3) = "The current set of lines after steadycam filter."
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Accumulate the lines in dst2 as a measure of how closely the lines will land."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            WarpAffine(task.lines.dst3, dst3, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            accum.Run(dst3)
            dst2 = accum.dst2
            labels(2) = "Accumulated lines with each frame given " + accum.options.accumWeighted.ToString("0%")
        End Sub
    End Class






    Public Class XR_Match_Point2 : Inherits TaskParent
        Public ptLast As New List(Of Point2f)
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Cursor.ai: Use Match_CenterRect.M to translate a point from the last image to the current image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static lastM As cv.Mat = Mat.Eye(2, 3, MatType.CV_64FC1)

            If task.heartBeatLT Then
                feat.Run(src)
                ptLast = New List(Of cv.Point2f)(feat.features)
                lastM = Mat.Eye(2, 3, MatType.CV_64FC1)
            End If

            If task.heartBeatLT = False Then
                Dim Minv As New Mat
                InvertAffineTransform(task.steadyCam.M, Minv)
                dst2 = task.steadyCam.dst3
                For Each pt In ptLast
                    Dim ptAligned = GravityRGB_Basics.WarpPoint(pt, lastM)
                    pt = GravityRGB_Basics.WarpPoint(ptAligned, Minv)
                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                Next
            End If

            lastM = task.steadyCam.M.Clone
        End Sub
    End Class





    Public Class Match_Features : Inherits TaskParent
        Dim feat As New Feature_Basics
        Dim knn As New KNN_Basics
        Public Sub New()
            desc = "Use SteadyCam.M to translate features from the current image to the steadyCam image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(src)

            If task.heartBeatLT Then dst3.SetTo(0)

            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
            knn.queries.Clear()
            For Each pt In feat.features
                knn.queries.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            CvtColor(task.steadyCam.dst3, dst2, cv.ColorConversionCodes.GRAY2BGR)
            For Each pt In knn.queries
                pt = GravityRGB_Basics.WarpPoint(pt, task.steadyCam.M)
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            knn.Run(emptyMat)
            If knn.result Is Nothing Then Exit Sub

            For i = 0 To knn.queries.Count - 1
                Dim pt = knn.queries(i)
                Dim ptAligned = knn.trainInput(knn.result(i, 0))
                If pt.DistanceTo(ptAligned) < 3 Then
                    Line(dst3, pt, ptAligned, task.highlight, task.lineWidth, task.lineType)
                End If
            Next
        End Sub
    End Class





    Public Class Match_RedC : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            desc = "Create a stable RedC output image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            WarpAffine(dst2, dst3, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
        End Sub
    End Class




    Public Class Match_ClickPoint : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim rcMap As New cv.Mat
        Dim mapID As Byte = 0
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use the clickpoint to confirm the "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.mouseClickFlag Then mapID = redC.rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)

            WarpAffine(dst2, dst3, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))
            WarpAffine(redC.rcMap, rcMap, task.steadyCam.M, src.Size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0))

            If task.rcD Is Nothing Then Exit Sub

            Dim pt = GravityRGB_Basics.WarpPoint(task.rcD.maxDist, task.steadyCam.M)
            Dim mapIDaligned = rcMap.Get(Of Integer)(pt.Y, pt.X)
            If mapIDaligned <> mapID Then
                SetTrueText("Tracking the selected cell was lost", 1)
                Exit Sub
            End If

        End Sub
    End Class

End Namespace