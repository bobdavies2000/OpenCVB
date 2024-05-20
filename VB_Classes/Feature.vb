Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Feature_Basics : Inherits VB_Algorithm
    Dim matList As New List(Of cv.Mat)
    Dim ptList As New List(Of cv.Point2f)
    Dim knn As New KNN_Core
    Dim ptLost As New List(Of cv.Point2f)
    Dim gather As New Feature_Gather
    Public options As New Options_Features
    Public Sub New()
        task.features.Clear() ' in case it was previously in use...
        desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static featureMat As New List(Of cv.Mat)

        If task.optionsChanged Then task.features.Clear()

        matList.Clear()
        ptList.Clear()
        Dim correlationMat As New cv.Mat
        For i = 0 To Math.Min(featureMat.Count, task.features.Count) - 1
            Dim pt = task.features(i)
            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
            cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            If correlationMat.Get(Of Single)(0, 0) > options.correlationMin Then
                matList.Add(featureMat(i))
                ptList.Add(pt)
            Else
                Dim ptNew = New cv.Point2f(CInt(pt.X), CInt(pt.Y))
                If ptLost.Contains(ptNew) = False Then ptLost.Add(ptNew)
            End If
        Next

        featureMat = New List(Of cv.Mat)(matList)
        task.features = New List(Of cv.Point2f)(ptList)

        gather.Run(src)
        Dim nextFeatures = gather.features

        Dim extra = 1 + (1 - options.thresholdPercent)
        task.featureMotion = True

        If task.features.Count < nextFeatures.Count * options.thresholdPercent Or task.features.Count > extra * nextFeatures.Count Then
            featureMat.Clear()
            task.features.Clear()
            ptLost.Clear()
            For Each pt In nextFeatures
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            If ptLost.Count > 0 Then
                knn.queries = ptLost
                knn.trainInput = nextFeatures
                knn.Run(Nothing)

                For i = 0 To knn.queries.Count - 1
                    Dim pt = knn.queries(i)
                    Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                    featureMat.Add(src(rect))
                    task.features.Add(knn.trainInput(knn.result(i, 0)))
                Next
            Else
                task.featureMotion = False
            End If
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next
        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched using correlation coefficients and " +
                    CStr(ptLost.Count) + " features had to be relocated."
    End Sub
End Class







' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_BasicsNoFrills : Inherits VB_Algorithm
    Public options As New Options_Features
    Dim gather As New Feature_Gather
    Public Sub New()
        vbAddAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone

        gather.Run(src)

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In gather.features
            task.features.Add(pt)
            task.featurePoints.Add(New cv.Point(pt.X, pt.X))
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        labels(2) = gather.labels(2)
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Public featurePoints As New List(Of cv.Point2f)
    Public feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        knn.queries = New List(Of cv.Point2f)(task.features)
        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(empty)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = task.features(i)
            If pt.DistanceTo(qPt) > feat.options.minDistance Then knn.trainInput(trainIndex) = task.features(i)
        Next
        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class








Public Class Feature_Line : Inherits VB_Algorithm
    Dim lines As New Line_SubsetRect
    Dim lineDisp As New Line_DisplayInfo
    Dim options As New Options_Features
    Dim match As New Match_tCell
    Public tcells As List(Of tCell)
    Public Sub New()
        Dim tc As tCell
        tcells = New List(Of tCell)({tc, tc})
        labels = {"", "", "Longest line present.", ""}
        desc = "Find and track a line using the end points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
        Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

        Dim correlationMin = options.correlationMin
        Dim correlationTest = tcells(0).correlation <= correlationMin Or tcells(1).correlation <= correlationMin
        lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
        If task.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or lineDisp.distance < distanceThreshold Then
            Dim templatePad = options.templatePad
            lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
            lines.Run(src.Clone)

            If lines.mpList.Count = 0 Then
                setTrueText("No lines found.", 3)
                Exit Sub
            End If
            Dim lp = lines.sortByLen.ElementAt(0).Value

            tcells(0) = match.createCell(src, 0, lp.p1)
            tcells(1) = match.createCell(src, 0, lp.p2)
        End If

        dst2 = src.Clone
        For i = 0 To tcells.Count - 1
            match.tCells(0) = tcells(i)
            match.Run(src)
            tcells(i) = match.tCells(0)
            setTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y))
            setTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y), 3)
        Next

        lineDisp.tcells = New List(Of tCell)(tcells)
        lineDisp.Run(src)
        dst2 = lineDisp.dst2
        setTrueText(lineDisp.strOut, New cv.Point(10, 40), 3)
    End Sub
End Class







Public Class Feature_VerticalVerify : Inherits VB_Algorithm
    Dim linesVH As New Feature_LinesVH
    Public verify As New IMU_VerticalVerify
    Public Sub New()
        desc = "Select a line or group of lines and track the result"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        linesVH.Run(src)

        verify.gCells = New List(Of gravityLine)(linesVH.gCells)
        verify.Run(src)
        dst2 = verify.dst2
    End Sub
End Class








Public Class Feature_LinesVH : Inherits VB_Algorithm
    Public gCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New Line_GCloud
    Dim options As New Options_Features
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Vertical lines")
            radio.addRadio("Horizontal lines")
            radio.check(0).Checked = True
        End If
        labels(3) = "More readable than dst1 - index, correlation, length (meters), and ArcY"
        desc = "Find and track all the horizontal or vertical lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim templatePad = options.templatePad
        ' gLines.lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
        gLines.Run(src)

        Static vertRadio = findRadio("Vertical lines")
        Dim sortedLines = If(vertRadio.checked, gLines.sortedVerticals, gLines.sortedHorizontals)
        If sortedLines.Count = 0 Then
            setTrueText("There were no vertical lines found.", 3)
            Exit Sub
        End If

        Dim gc As gravityLine
        gCells.Clear()
        match.tCells.Clear()
        For i = 0 To sortedLines.Count - 1
            gc = sortedLines.ElementAt(i).Value

            If i = 0 Then
                dst1.SetTo(0)
                gc.tc1.template.CopyTo(dst1(gc.tc1.rect))
                gc.tc2.template.CopyTo(dst1(gc.tc2.rect))
            End If

            match.tCells.Clear()
            match.tCells.Add(gc.tc1)
            match.tCells.Add(gc.tc2)

            match.Run(src)
            Dim correlationMin = options.correlationMin
            If match.tCells(0).correlation >= correlationMin And match.tCells(1).correlation >= correlationMin Then
                gc.tc1 = match.tCells(0)
                gc.tc2 = match.tCells(1)
                gc = gLines.updateGLine(src, gc, gc.tc1.center, gc.tc2.center)
                If gc.len3D > 0 Then gCells.Add(gc)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        For i = 0 To gCells.Count - 1
            Dim tc As tCell
            gc = gCells(i)
            Dim p1 As cv.Point2f, p2 As cv.Point2f
            For j = 0 To 2 - 1
                tc = Choose(j + 1, gc.tc1, gc.tc2)
                If j = 0 Then p1 = tc.center Else p2 = tc.center
            Next
            setTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 2)
            setTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(gc.arcY, fmt1), gc.tc1.center, 3)

            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class










Public Class Feature_Lines_Tutorial1 : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        labels(3) = "The highlighted lines are also lines in 3D."
        desc = "Find all the lines in the image and determine which are in the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of pointPair)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In lines.lpList
            If task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X) > 0 And task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p2.X) > 0 Then
                raw2D.Add(lp)
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        dst3 = src
        For i = 0 To raw2D.Count - 2 Step 2
            dst3.Line(raw2D(i).p1, raw2D(i).p2, task.highlightColor, task.lineWidth, task.lineType)
        Next
        If task.heartBeat Then labels(2) = "Starting with " + Format(lines.lpList.Count, "000") + " lines, there are " +
                                           Format(raw3D.Count / 2, "000") + " with depth data."
    End Sub
End Class







Public Class Feature_Lines_Tutorial2 : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim gMat As New IMU_GMatrix
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5)
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = findSlider("Area kernel size for depth")
        Dim k = kernelSlider.Value - 1
        Dim kernel = kernelSlider.Value * 2 - 1

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of pointPair)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In lines.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = validateRect(New cv.Rect(pt.x - k, pt.y - k, kernel, kernel))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next
            If pt1.Z > 0 And pt2.Z > 0 Then
                raw2D.Add(lp)
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        dst3 = src
        For i = 0 To raw2D.Count - 2 Step 2
            dst3.Line(raw2D(i).p1, raw2D(i).p2, task.highlightColor, task.lineWidth, task.lineType)
        Next
        If task.heartBeat Then labels(2) = "Starting with " + Format(lines.lpList.Count, "000") + " lines, there are " +
                                           Format(raw3D.Count, "000") + " with depth data."
        If raw3D.Count = 0 Then
            setTrueText("No vertical or horizontal lines were found")
        Else
            gMat.Run(empty)
            task.gMatrix = gMat.gMatrix
            Dim matLines3D As cv.Mat = (New cv.Mat(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix
        End If
    End Sub
End Class








Public Class Feature_LongestVerticalKNN : Inherits VB_Algorithm
    Dim gLines As New Line_GCloud
    Dim longest As New Feature_Longest
    Public Sub New()
        labels(3) = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines."
        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN."
    End Sub
    Private Function testLastPair(lastPair As pointPair, gc As gravityLine) As Boolean
        Dim distance1 = lastPair.p1.DistanceTo(lastPair.p2)
        Dim p1 = gc.tc1.center
        Dim p2 = gc.tc2.center
        If distance1 < 0.75 * p1.DistanceTo(p2) Then Return True ' it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
        Return False
    End Function
    Public Sub RunVB(src As cv.Mat)
        gLines.Run(src)
        If gLines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were present", 3)
            Exit Sub
        End If

        dst3 = src.Clone
        Dim index As Integer

        If testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value) Then longest.knn.lastPair = New pointPair
        For Each gc In gLines.sortedVerticals.Values
            If index >= 10 Then Exit For

            Dim p1 = gc.tc1.center
            Dim p2 = gc.tc2.center
            If longest.knn.lastPair.compare(New pointPair) Then longest.knn.lastPair = New pointPair(p1, p2)
            Dim pt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
            setTrueText(CStr(index) + vbCrLf + Format(gc.arcY, fmt1), pt, 3)
            index += 1

            dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            longest.knn.trainInput.Add(p1)
            longest.knn.trainInput.Add(p2)
        Next

        longest.Run(src)
        dst2 = longest.dst2
    End Sub
End Class






Public Class Feature_tCellTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Dim tracker As New Feature_Points
    Dim match As New Match_tCell
    Public tcells As New List(Of tCell)
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X regions with goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim correlationMin = options.correlationMin

        strOut = ""
        If tcells.Count < task.features.Count / 3 Or tcells.Count < 2 Or task.optionsChanged Then
            tracker.Run(src)
            tcells.Clear()
            For Each pt In task.features
                tcells.Add(match.createCell(src, 0, pt))
            Next
            strOut += "------------------" + vbCrLf + vbCrLf
        End If

        dst2 = src.Clone

        Dim newCells As New List(Of tCell)
        For Each tc In tcells
            match.tCells(0) = tc
            match.Run(src)
            If match.tCells(0).correlation >= correlationMin Then
                tc = match.tCells(0)
                setTrueText(Format(tc.correlation, fmt3), tc.center)
                If standaloneTest() Then strOut += Format(tc.correlation, fmt3) + ", "
                dst2.Circle(tc.center, task.dotSize, task.highlightColor, -1, task.lineType)
                dst2.Rectangle(tc.rect, task.highlightColor, task.lineWidth, task.lineType)
                newCells.Add(tc)
            End If
        Next

        If standaloneTest() Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        tcells = New List(Of tCell)(newCells)
        labels(2) = "Of the " + CStr(task.features.Count) + " input cells " + CStr(newCells.Count) + " cells were tracked with correlation above " +
                    Format(correlationMin, fmt1)
    End Sub
End Class







Public Class Feature_LongestV_Tutorial1 : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Public Sub New()
        desc = "Use Feature_Lines to find all the vertical lines and show the longest."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class Feature_LongestV_Tutorial2 : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Dim knn As New KNN_Core4D
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Public Sub New()
        desc = "Use Feature_Lines to find all the vertical lines.  Use KNN_Core4D to track each line."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst1 = lines.dst3

        If lines.sortedVerticals.Count = 0 Then
            setTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim match3D As New List(Of cv.Point3f)
        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim sIndex = lines.sortedVerticals.ElementAt(i).Value
            Dim x1 = lines.lines2D(sIndex)
            Dim x2 = lines.lines2D(sIndex + 1)
            Dim vec = If(x1.Y < x2.Y, New cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y), New cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y))
            If knn.queries.Count = 0 Then knn.queries.Add(vec)
            knn.trainInput.Add(vec)
            match3D.Add(lines.lines3D(sIndex))
            match3D.Add(lines.lines3D(sIndex + 1))
        Next

        Dim saveVec = knn.queries(0)
        knn.Run(empty)

        Dim index = knn.result(0, 0)
        Dim p1 = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
        Dim p2 = New cv.Point2f(knn.trainInput(index)(2), knn.trainInput(index)(3))
        pt1 = match3D(index * 2)
        pt2 = match3D(index * 2 + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

        Static lastLength = lines.sorted2DV.ElementAt(0).Key
        Dim bestLength = lines.sorted2DV.ElementAt(0).Key
        Static lengthReject As Integer
        knn.queries.Clear()
        If lastLength > 0.5 * bestLength Then
            knn.queries.Add(New cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y))
            lastLength = p1.DistanceTo(p2)
        Else
            lengthReject += 1
            lastLength = bestLength
        End If
        labels(3) = "Length rejects = " + Format(lengthReject / task.frameCount, "0%")
    End Sub
End Class






Public Class Feature_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public lines2D As New List(Of cv.Point2f)
    Public lines3D As New List(Of cv.Point3f)
    Public sorted2DV As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedVerticals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5)
            sliders.setupTrackBar("Angle tolerance in degrees", 0, 20, 5)
        End If
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static angleSlider = findSlider("Angle tolerance in degrees")
        Static kernelSlider = findSlider("Area kernel size for depth")
        Dim tolerance = angleSlider.Value
        Dim k = kernelSlider.Value - 1
        Dim kernel = kernelSlider.Value * 2 - 1
        dst3 = src.Clone

        lines2D.Clear()
        lines3D.Clear()
        sorted2DV.Clear()
        sortedVerticals.Clear()
        sortedHorizontals.Clear()

        lines.Run(src)
        dst2 = lines.dst2

        Dim raw2D As New List(Of pointPair)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In lines.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = validateRect(New cv.Rect(pt.x - k, pt.y - k, kernel, kernel))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next

            If pt1.Z > 0 And pt2.Z > 0 And pt1.Z < 4 And pt2.Z < 4 Then ' points more than X meters away are not accurate...
                raw2D.Add(lp)
                raw3D.Add(pt1)
                raw3D.Add(pt2)
            End If
        Next

        If raw3D.Count = 0 Then
            setTrueText("No vertical or horizontal lines were found")
        Else
            Dim matLines3D As cv.Mat = (New cv.Mat(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix

            For i = 0 To raw2D.Count - 2 Step 2
                Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                Dim len3D = distance3D(pt1, pt2)
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                If Math.Abs(arcY - 90) < tolerance Then
                    dst3.Line(raw2D(i).p1, raw2D(i).p2, cv.Scalar.Blue, task.lineWidth, task.lineType)
                    sortedVerticals.Add(len3D, lines3D.Count)
                    sorted2DV.Add(raw2D(i).p1.DistanceTo(raw2D(i).p2), lines2D.Count)
                    If pt1.Y > pt2.Y Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
                If Math.Abs(arcY) < tolerance Then
                    dst3.Line(raw2D(i).p1, raw2D(i).p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    sortedHorizontals.Add(len3D, lines3D.Count)
                    If pt1.X < pt2.X Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
            Next
        End If
        labels(2) = "Starting with " + Format(lines.lpList.Count, "000") + " lines, there are " +
                                       Format(lines3D.Count / 2, "000") + " with depth data."
        labels(3) = "There were " + CStr(sortedVerticals.Count) + " vertical lines (blue) and " + CStr(sortedHorizontals.Count) + " horizontal lines (yellow)"
    End Sub
End Class







Public Class Feature_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Good features", "History of good features"}
        desc = "Get the features in a reduction grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = src

        feat.Run(reduction.dst2)
        If task.heartBeat Then dst3.SetTo(0)
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class







Public Class Feature_MultiPass : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public featurePoints As New List(Of cv.Point2f)
    Dim sharpen As New PhotoShop_SharpenDetail
    Public Sub New()
        gOptions.RGBFilterActive.Checked = True
        gOptions.RGBFilterList.SelectedIndex = gOptions.RGBFilterList.Items.IndexOf("Filter_Laplacian")
        desc = "Run Feature_Basics twice and compare results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(task.color)
        dst2 = src.Clone
        featurePoints = New List(Of cv.Point2f)(task.features)
        Dim passCounts As String = CStr(featurePoints.Count) + "/"

        feat.Run(src)
        For Each pt In task.features
            featurePoints.Add(pt)
        Next
        passCounts += CStr(task.features.Count) + "/"

        sharpen.Run(task.color)
        feat.Run(sharpen.dst2)
        For Each pt In task.features
            featurePoints.Add(pt)
        Next
        passCounts += CStr(task.features.Count)

        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        If task.heartBeat Then
            labels(2) = "Total features = " + CStr(featurePoints.Count) + ", pass counts = " + passCounts
        End If
    End Sub
End Class








Public Class Feature_PointTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public feat As New Feature_Basics
    Dim mPoints As New Match_Points
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim correlationMin = options.correlationMin
        Dim templatePad = options.templatePad
        Dim templateSize = options.templateSize

        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            mPoints.ptx.Clear()
            feat.Run(src)
            For Each pt In task.features
                mPoints.ptx.Add(pt)
                Dim rect = validateRect(New cv.Rect(pt.X - templatePad, pt.Y - templatePad, templateSize, templateSize))
            Next
            strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
        End If
        mPoints.Run(src)

        dst2 = src.Clone
        For i = mPoints.ptx.Count - 1 To 0 Step -1
            If mPoints.correlation(i) > correlationMin Then
                dst2.Circle(mPoints.ptx(i), task.dotSize, task.highlightColor, -1, task.lineType)
                strOut += Format(mPoints.correlation(i), fmt3) + ", "
            Else
                mPoints.ptx.RemoveAt(i)
            End If
        Next
        If standaloneTest() Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        labels(2) = "Of the " + CStr(task.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(correlationMin, fmt2)
    End Sub
End Class






Public Class Feature_LongestKNN : Inherits VB_Algorithm
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match As New Match_Basics
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src

        Static p1 As cv.Point, p2 As cv.Point
        knn.Run(src.Clone)
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2
        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect)
        match.Run(src)
        If match.correlation >= options.correlationMin Then
            dst3 = match.dst0.Resize(dst3.Size)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
            rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            task.highlightColor = If(task.highlightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New pointPair(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class






Public Class Feature_Longest : Inherits VB_Algorithm
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match1 As New Match_Basics
    Public match2 As New Match_Basics
    Public Sub New()
        labels(2) = "Longest line end points are highlighted "
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        Dim correlationMin = match1.options.correlationMin
        Dim templatePad = match1.options.templatePad
        Dim templateSize = match1.options.templateSize

        Static p1 As cv.Point, p2 As cv.Point
        If task.heartBeat Or match1.correlation < correlationMin And match2.correlation < correlationMin Then
            knn.Run(src.Clone)

            p1 = knn.lastPair.p1
            Dim r1 = validateRect(New cv.Rect(p1.X - templatePad, p1.Y - templatePad, templateSize, templateSize))
            match1.template = src(r1).Clone

            p2 = knn.lastPair.p2
            Dim r2 = validateRect(New cv.Rect(p2.X - templatePad, p2.Y - templatePad, templateSize, templateSize))
            match2.template = src(r2).Clone
        End If

        match1.Run(src)
        p1 = match1.matchCenter

        match2.Run(src)
        p2 = match2.matchCenter

        gline = glines.updateGLine(src, gline, p1, p2)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
        dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
        setTrueText(Format(match1.correlation, fmt3), p1)
        setTrueText(Format(match2.correlation, fmt3), p2)
    End Sub
End Class







Public Class Feature_Delaunay : Inherits VB_Algorithm
    Dim facet As New Delaunay_Contours
    Dim feat As New Feature_Basics
    Public Sub New()
        findSlider("Min Distance to next").Value = 10
        desc = "Divide the image into contours with Delaunay using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        facet.inputPoints.Clear()
        For Each pt In task.features
            facet.inputPoints.Add(pt)
        Next

        facet.Run(src)
        dst3 = facet.dst2
        For Each pt In task.features
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
        labels(3) = "There were " + CStr(task.features.Count) + " Delaunay contours"
    End Sub
End Class







Public Class Feature_LRMatched : Inherits VB_Algorithm
    Dim match As New FeatureLeftRight_Basics
    Public Sub New()
        desc = "Placeholder to make it easier to find FeatureLeftRight_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        match.Run(src)
        dst2 = match.dst2
        dst3 = match.dst3
        labels = match.labels
    End Sub
End Class






Public Class Feature_LucasKanade : Inherits VB_Algorithm
    Dim pyr As New FeatureFlow_LucasKanade
    Public ptList As New List(Of cv.Point)
    Public ptLast As New List(Of cv.Point)
    Public Sub New()
        desc = "Provide a trace of the tracked features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static ptHist As New List(Of List(Of cv.Point))
        pyr.Run(src)
        dst2 = src
        labels(2) = pyr.labels(2)

        If task.heartBeat Then dst3.SetTo(0)

        ptList.Clear()
        Dim stationary As Integer, motion As Integer
        For i = 0 To pyr.features.Count - 1
            Dim pt = New cv.Point(pyr.features(i).X, pyr.features(i).Y)
            ptList.Add(pt)
            If ptLast.Contains(pt) Then
                dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                stationary += 1
            Else
                dst3.Line(pyr.lastFeatures(i), pyr.features(i), cv.Scalar.White, task.lineWidth, task.lineType)
                motion += 1
            End If
        Next

        If task.heartBeat Then labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
        ptLast = New List(Of cv.Point)(ptList)
    End Sub
End Class







' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_GridSimple : Inherits VB_Algorithm
    Public options As New Options_Features
    Public Sub New()
        findSlider("Feature Sample Size").Value = 1
        desc = "Find good features to track in each roi of the task.gridList"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        options.RunVB()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        task.features.Clear()
        For Each roi In task.gridList
            Dim features = cv.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, Nothing,
                                                      options.blockSize, True, options.k)
            For Each pt In features
                task.features.Add(New cv.Point2f(roi.X + pt.X, roi.Y + pt.Y))
            Next
        Next

        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        labels(2) = "Found " + CStr(task.features.Count) + " points with quality = " + CStr(options.quality) +
                    " and minimum distance = " + CStr(options.minDistance) + " and blocksize " + CStr(options.blockSize)
    End Sub
End Class





Public Class Feature_Grid : Inherits VB_Algorithm
    Dim options As New Options_Features
    Dim matList As New List(Of cv.Mat)
    Dim ptList As New List(Of cv.Point2f)
    Dim knn As New KNN_Core
    Dim ptLost As New List(Of cv.Point2f)
    Dim gather As New Feature_Gather
    Public Sub New()
        findRadio("GoodFeatures (ShiTomasi) grid").Checked = True
        findSlider("Feature Sample Size").Value = 4
        desc = "Find good features to track in each roi of the task.gridList"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static featureMat As New List(Of cv.Mat)
        If task.optionsChanged Then task.features.Clear()

        matList.Clear()
        ptList.Clear()
        ptLost.Clear()
        Dim correlationMat As New cv.Mat
        For i = 0 To task.features.Count - 1
            Dim pt = task.features(i)
            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
            cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            If correlationMat.Get(Of Single)(0, 0) > options.correlationMin Then
                matList.Add(featureMat(i))
                ptList.Add(pt)
            Else
                ptLost.Add(pt)
            End If
        Next

        featureMat = New List(Of cv.Mat)(matList)
        task.features = New List(Of cv.Point2f)(ptList)

        gather.Run(src)
        Dim nextFeatures = gather.features

        If task.features.Count < nextFeatures.Count * options.thresholdPercent Then
            featureMat.Clear()
            task.features.Clear()
            For Each pt In nextFeatures
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            knn.queries = ptLost
            knn.trainInput = nextFeatures
            knn.Run(Nothing)

            For i = 0 To knn.queries.Count - 1
                Dim pt = knn.queries(i)
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(knn.trainInput(knn.result(i, 0)))
            Next
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next
        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched using correlation coefficients and " +
                    CStr(ptLost.Count) + " features had to be relocated."
    End Sub
End Class







Public Class Feature_LineAngle : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Public Sub New()
        desc = "Use Feature_Lines data to identify the longest lines and show its angle."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                setTrueText("No vertical lines were found", 3)
                Exit Sub
            End If
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst3.SetTo(0)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        Dim pt1 = lines.lines3D(index)
        Dim pt2 = lines.lines3D(index + 1)
        Dim len3D = distance3D(pt1, pt2)
        Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
        setTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
        setTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m distant", p1, 3)
    End Sub
End Class






Public Class Feature_LineAngleAll : Inherits VB_Algorithm
    Dim lines As New Feature_Lines
    Dim flow As New Font_FlowText
    Dim arcList As New List(Of Single)
    Dim arcLongAverage As New List(Of Single)
    Public Sub New()
        flow.dst = 3
        desc = "Use Feature_Lines data to collect vertical lines and measure accuracy of each."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                setTrueText("No vertical lines were found", 3)
                Exit Sub
            End If

            dst3.SetTo(0)
            arcList.Clear()
            flow.msgs.Clear()
            flow.msgs.Add("ID" + vbTab + "length" + vbTab + "distance")
            For i = 0 To Math.Min(10, lines.sortedVerticals.Count) - 1
                Dim index = lines.sortedVerticals.ElementAt(i).Value
                Dim p1 = lines.lines2D(index)
                Dim p2 = lines.lines2D(index + 1)
                dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
                setTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
                dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

                Dim pt1 = lines.lines3D(index)
                Dim pt2 = lines.lines3D(index + 1)
                Dim len3D = distance3D(pt1, pt2)
                If len3D > 0 Then
                    Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                    arcList.Add(arcY)
                    flow.msgs.Add(Format(arcY, fmt3) + vbTab + Format(len3D, fmt3) + "m " + vbTab + Format(pt1.Z, fmt1) + "m")
                End If
            Next
        End If
        flow.Run(Nothing)

        Static firstAverage As New List(Of Single)
        Static firstBest As Integer
        Dim mostAccurate = arcList(0)
        firstAverage.Add(mostAccurate)
        For Each arc In arcList
            If arc > mostAccurate Then
                mostAccurate = arc
                Exit For
            End If
        Next
        If mostAccurate = arcList(0) Then firstBest += 1

        Dim avg = arcList.Average()
        arcLongAverage.Add(avg)
        labels(3) = "arcY avg = " + Format(avg, fmt1) + ", long term average = " + Format(arcLongAverage.Average, fmt1) +
                    ", first was best " + Format(firstBest / task.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
        If arcLongAverage.Count > 1000 Then
            arcLongAverage.RemoveAt(0)
            firstAverage.RemoveAt(0)
        End If
    End Sub
End Class







Public Class Feature_NearestCell : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim feat As New FeatureLeftRight_Basics
    Dim knn As New KNN_Core
    Public Sub New()
        desc = "Find the nearest feature to every cell in task.redCells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst2.Clone
        labels(2) = redC.labels(2)

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDStable)
        Next

        knn.trainInput.Clear()
        For Each mp In feat.mpList
            knn.trainInput.Add(New cv.Point2f(mp.p1.X, mp.p1.Y))
        Next

        knn.Run(Nothing)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nearestFeature = knn.trainInput(knn.result(i, 0))
            dst3.Line(rc.nearestFeature, rc.maxDStable, task.highlightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class Feature_Points : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        If task.heartBeat Then dst3.SetTo(0)

        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(task.features.Count) + " targets were present with " + CStr(feat.options.featurePoints) + " requested."
    End Sub
End Class






Public Class Feature_Trace : Inherits VB_Algorithm
    Dim track As New RedTrack_Features
    Public Sub New()
        desc = "Placeholder to help find RedTrack_Features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        labels = track.labels
    End Sub
End Class





Public Class Feature_TraceDelaunay : Inherits VB_Algorithm
    Dim features As New Feature_Delaunay
    Public goodList As New List(Of List(Of cv.Point2f)) ' stable points only
    Public Sub New()
        labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
        desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        features.Run(src)
        dst3 = features.dst2

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(task.features)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                task.color.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                Dim c = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
                dst2.Circle(pt, task.dotSize + 1, c, -1, task.lineType)
            Next
        Next
        labels(2) = CStr(task.features.Count) + " features were identified in the image."
    End Sub
End Class






Public Class Feature_ShiTomasi : Inherits VB_Algorithm
    Dim harris As New Corners_HarrisDetector
    Dim shiTomasi As New Corners_ShiTomasi_CPP
    Dim options As New Options_ShiTomasi
    Public Sub New()
        findSlider("Corner normalize threshold").Value = 15
        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If options.useShiTomasi Then
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(task.highlightColor, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Else
            harris.Run(task.leftView)
            dst2 = harris.dst2.Clone
            harris.Run(task.rightView)
            dst3 = harris.dst2
        End If
    End Sub
End Class






Public Class Feature_Gather : Inherits VB_Algorithm
    Dim harris As New Corners_HarrisDetector
    Dim FAST As New Corners_Basics
    Dim myOptions As New Options_FeatureGather
    Public features As New List(Of cv.Point2f)
    Dim brisk As New BRISK_Basics
    Public options As New Options_Features
    Public Sub New()
        cPtr = Agast_Open()
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        myOptions.RunVB()
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Select Case myOptions.featureSource
            Case FeatureSrc.goodFeaturesFull
                Static sampleSlider = findSlider("Feature Sample Size")
                sampleSlider.value = 400
                features = cv.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality, options.minDistance, New cv.Mat,
                                                      options.blockSize, True, options.k).ToList
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.goodFeaturesGrid
                options.featurePoints = 4
                features.Clear()
                For i = 0 To task.gridList.Count - 1
                    Dim roi = task.gridList(i)
                    Dim tmpFeatures = cv.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, New cv.Mat,
                                                                 options.blockSize, True, options.k).ToList
                    For j = 0 To tmpFeatures.Count - 1
                        features.Add(New cv.Point2f(tmpFeatures(j).X + roi.X, tmpFeatures(j).Y + roi.Y))
                    Next
                Next
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Agast
                src = task.color.Clone
                Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

                Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                handleSrc.Free()

                Dim ptMat = New cv.Mat(Agast_Count(cPtr), 1, cv.MatType.CV_32FC2, imagePtr).Clone
                features.Clear()
                If standaloneTest() Then dst2 = src

                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                    features.Add(pt)
                    If standaloneTest() Then dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
                Next

                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.BRISK
                brisk.Run(src)
                features = brisk.features
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Harris
                harris.Run(src)
                features = harris.features
                labels(2) = "Harris Detector produced " + CStr(features.Count) + " features"
            Case FeatureSrc.FAST
                FAST.Run(src)
                features = FAST.features
                labels(2) = "FAST produced " + CStr(features.Count) + " features"
        End Select

        If standaloneTest() Then
            dst2 = task.color.Clone
            For Each pt In features
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class





Public Class Feature_Generations : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim features As New List(Of cv.Point)
    Dim gens As New List(Of Integer)
    Public Sub New()
        vbAddAdvice(traceName + ": Local options will determine how many features are present.")
        desc = "Find feature age maximum and average."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        Dim newfeatures As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For Each pt In task.featurePoints
            Dim index = features.IndexOf(pt)
            If index >= 0 Then newfeatures.Add(gens(index) + 1, pt) Else newfeatures.Add(1, pt)
        Next

        If task.heartBeat Then
            features.Clear()
            gens.Clear()
        End If

        features = New List(Of cv.Point)(newfeatures.Values)
        gens = New List(Of Integer)(newfeatures.Keys)

        dst2 = src
        For i = 0 To features.Count - 1
            If gens(i) = 1 Then Exit For
            Dim pt = features(i)
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + Format(gens.Average, fmt0) + " generations"
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits VB_Algorithm
    Public features As New List(Of cv.Point)
    Public feat As New Feature_Basics
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static featureHistory As New List(Of List(Of cv.Point))
        Static gens As New List(Of Integer)
        Dim histCount = gOptions.FrameHistory.Value

        feat.Run(src)
        dst2 = src.Clone

        featureHistory.Add(New List(Of cv.Point)(task.featurePoints))

        Dim newFeatures As New List(Of cv.Point)
        gens.Clear()
        For Each cList In featureHistory
            For Each pt In cList
                Dim index = newFeatures.IndexOf(pt)
                If index >= 0 Then
                    gens(index) += 1
                Else
                    newFeatures.Add(pt)
                    gens.Add(1)
                End If
            Next
        Next

        Dim threshold = If(histCount = 1, 0, 1)
        features.Clear()
        Dim whiteCount As Integer
        For i = 0 To newFeatures.Count - 1
            If gens(i) > threshold Then
                Dim pt = newFeatures(i)
                features.Add(pt)
                If gens(i) < histCount Then
                    dst2.Circle(pt, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
                Else
                    whiteCount += 1
                    dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                End If
            End If
        Next

        If featureHistory.Count > histCount Then featureHistory.RemoveAt(0)
        If task.heartBeat Then
            labels(2) = CStr(features.Count) + "/" + CStr(whiteCount) + " present/present on every frame" +
                        " Red is a recent addition, yellow is present on previous " + CStr(histCount) + " frames"
        End If
    End Sub
End Class






Public Class Feature_GridPopulation : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        dst3.SetTo(0)
        For Each pt In task.featurePoints
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        For Each roi In task.gridList
            Dim test = dst3(roi).FindNonZero()
            setTrueText(CStr(test.Rows), roi.TopLeft, 3)
        Next
    End Sub
End Class










Public Class Feature_Compare : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim noFrill As New Feature_BasicsNoFrills
    Public Sub New()
        desc = "Prepare features for the left and right views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveLFeatures As New List(Of cv.Point2f)
        Static saveRFeatures As New List(Of cv.Point2f)

        task.features = New List(Of cv.Point2f)(saveLFeatures)
        feat.Run(src.Clone)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)
        saveLFeatures = New List(Of cv.Point2f)(task.features)

        task.features = New List(Of cv.Point2f)(saveRFeatures)
        noFrill.Run(src.Clone)
        dst3 = noFrill.dst2
        labels(3) = "With no correlation coefficients " + noFrill.labels(2)
        saveRFeatures = New List(Of cv.Point2f)(task.features)
    End Sub
End Class
