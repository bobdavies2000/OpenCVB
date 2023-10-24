Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Flood_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Public floodCell As New FloodCell_Basics
    Public Sub New()
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "FloodFill the input and paint it"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        floodCell.Run(src)
        dst2 = floodCell.dst2
        dst3 = floodCell.dst3
        classCount = task.fCells.Count

        labels(2) = CStr(classCount) + " regions were identified"
    End Sub
End Class





Public Class Flood_Point : Inherits VB_Algorithm
    Public pixelCount As Integer
    Public rect As cv.Rect
    Dim edges As New Edge_BinarizedSobel
    Public centroid As cv.Point2f
    Public initialMask As New cv.Mat
    Public pt As cv.Point ' this is the floodfill point
    Dim options As New Options_Flood
    Public Sub New()
        labels(2) = "Input image to floodfill"
        labels(3) = If(standalone, "Flood_Point standalone just shows the edges", "Resulting mask from floodfill")
        desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone()
        If standalone Then
            pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
            edges.Run(src)
            dst2 = edges.mats.dst2
            dst3 = edges.mats.mat(task.quadrantIndex)
        Else
            Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
            Dim maskRect = New cv.Rect(1, 1, dst2.Width, dst2.Height)

            Dim zero = New cv.Scalar(0)
            pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, New cv.Point(CInt(pt.X), CInt(pt.Y)), cv.Scalar.White, rect, zero, zero, options.floodFlag Or (255 << 8))
            dst3 = maskPlus(maskRect).Clone
            pixelCount = pixelCount
            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
            centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            labels(3) = CStr(pixelCount) + " pixels at point pt(x=" + CStr(pt.X) + ",y=" + CStr(pt.Y)
        End If
    End Sub
End Class










Public Class Flood_Click : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim flood As New Flood_Point
    Public Sub New()
        flood.pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
        labels = {"", "", "Click anywhere TypeOf floodfill that area.", "Edges for 4 different light levels."}
        desc = "FloodFill where the mouse clicks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.mouseClickFlag Then
            flood.pt = task.clickPoint
            task.mouseClickFlag = False ' preempt any other uses
        End If

        edges.Run(src)
        dst2 = edges.dst3

        If flood.pt.X Or flood.pt.Y Then
            flood.Run(dst2.Clone)
            dst2.CopyTo(dst3)
            If flood.pixelCount > 0 Then dst3.SetTo(255, flood.dst3)
        End If
    End Sub
End Class









Public Class Flood_Palette : Inherits VB_Algorithm
    Public flood As New Flood_RedColor
    Dim options As New Options_Flood
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a floodfill image that is only 8-bit for use with a palette"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        flood.Run(src)
        dst2 = flood.redC.dst2
        labels(2) = "The color scheme below is controlled with the Global Option for Palette.  " +
                    Str(task.redCells.Count) + " regions > " + CStr(task.minPixels) + " pixels"
    End Sub
End Class








Public Class Flood_LastXImages : Inherits VB_Algorithm
    Dim flood As New Flood_RedColor
    Dim rgbX As New Color_Smoothing
    Public Sub New()
        labels(2) = "Results from running Flood_RedColor using the last X BGR images."
        desc = "Run Flood_RedColor with an image that is the average of the last X frames.  Not much different..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rgbX.Run(src)
        dst3 = rgbX.dst2
        labels(3) = rgbX.labels(2)

        flood.Run(rgbX.dst2)
        dst2 = flood.dst2
    End Sub
End Class








Public Class Flood_MotionRect : Inherits VB_Algorithm
    Dim motion As New Motion_Rect
    Dim flood As New Flood_RedColor
    Public Sub New()
        labels = {"", "", "Output of Flood_RedColor using the BGR constructed image", ""}
        desc = "Perform floodfill on the BGR image constructed from a heartbeat image and the motion rectangle."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst3 = motion.dst3

        flood.Run(motion.dst2)
        dst2 = flood.dst2
    End Sub
End Class








Public Class Flood_TopX : Inherits VB_Algorithm
    Dim flood As New Flood_PointList
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the X largest regions in FloodFill", 1, 100, 50)
        desc = "Get the top X size regions in the floodfill output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static topXslider = findSlider("Show the X largest regions in FloodFill")
        Static desiredRegions As Integer
        If desiredRegions <> topXslider.Value Then
            flood.pointList.Clear()
            desiredRegions = topXslider.Value
        End If

        flood.Run(src)
        dst3 = flood.dst2

        Dim rc As New rcData
        If task.motionFlag Or task.optionsChanged Then dst2.SetTo(0)
        Dim rcCount = Math.Min(flood.redCells.Count, topXslider.Value)

        For i = 1 To rcCount - 1
            rc = flood.redCells(i)
            Dim c = src.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            c = New cv.Vec3b(If(c(0) < 50, 50, c(0)), If(c(1) < 50, 50, c(1)), If(c(2) < 50, 50, c(2)))
            dst2(rc.rect).SetTo(c, rc.mask)
        Next
        labels(2) = CStr(rcCount) + " regions were found.  Use the nearby slider to increase."
    End Sub
End Class







Public Class Flood_Objects : Inherits VB_Algorithm
    Public kFlood As New KMeans_FloodFill
    Public lutFlood As New LUT_FloodFill
    Public reductionFlood As New Reduction_Floodfill
    Public myFlood As Flood_RedColor
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use BGR KMeans")
            radio.addRadio("Use Depth KMeans")
            radio.addRadio("Use BGR LUT")
            radio.addRadio("Use Grayscale Reduction")
            radio.check(2).Checked = True
        End If

        labels(2) = "Click anywhere to get mask and depth value"
        desc = "Create a segmented image with KMeans, select a region, and isolate it for measurement."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static radioDepth = findRadio("Use Depth KMeans")
        Static radioLUT = findRadio("Use BGR LUT")
        Static radioReduction = findRadio("Use Grayscale Reduction")

        Dim fIndex = 0
        If radioLUT.checked Then fIndex = 2
        If radioReduction.checked Then fIndex = 2
        If radioDepth.checked Then
            src = task.pcSplit(2)
            fIndex = 1
        End If

        Select Case fIndex
            Case 0, 1
                If src.Type = cv.MatType.CV_32FC1 Then src = vbNormalize32f(src)
                kFlood.Run(src)
                myFlood = kFlood.flood
            Case 2
                lutFlood.Run(src)
                myFlood = lutFlood.flood
            Case 3
                'reductionFlood.Run(src)
                'myFlood = reductionFlood.flood
        End Select

        dst2 = vbPalette(myFlood.dst2)

        Dim meanDepth As cv.Scalar, stdevDepth As cv.Scalar
        labels(3) = "Selected area is an unstable region"

        If task.redCells.Count > 0 Then
            Dim rc = task.rcSelect
            Dim pt = task.clickPoint

            Dim input As cv.Mat = task.pcSplit(2)(rc.rect)
            cv.Cv2.MeanStdDev(input, meanDepth, stdevDepth, rc.mask)
            If meanDepth(0) < 0.2 Then
                labels(3) = "Region " + CStr(rc.index) + " has no depth"
            Else
                labels(3) = "Region " + CStr(rc.index) + " depth=" + Format(meanDepth(0) / 1000, fmt3) + "m (avg), stdev=" + Format(stdevDepth(0), "0.0")
            End If
            dst3.SetTo(0)
            dst2(rc.rect).CopyTo(dst3(rc.rect), rc.mask)
            Dim r = rc.rect
            If pt.X < r.X Or pt.X > r.X + r.Width Or pt.Y < r.Y Or pt.Y > r.Y + r.Height Then
                labels(3) = "The selected region does not have stable depth"
            End If
        End If
    End Sub
End Class






Public Class Flood_DepthAdd : Inherits VB_Algorithm
    Dim flood As New Flood_Objects
    Dim redCells As New List(Of rcData)
    Public Sub New()
        desc = "Use floodfill to enhance sparse depth data using pixels connected by color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2

        dst3 = New cv.Mat(task.pcSplit(2).Size, cv.MatType.CV_32F, 0)
        For Each rc In task.redCells
            Dim mean = task.pcSplit(2)(rc.rect).Mean(rc.mask)
            dst3(rc.rect).SetTo(mean.Val0, rc.mask)
            rc.depthMean.Z = mean(2)
            redCells.Add(rc)
        Next
    End Sub
End Class








Public Class Flood_Left : Inherits VB_Algorithm
    Dim options As New Options_LeftRight
    Public flood As New Flood_RedColor
    Public Sub New()
        labels = {"", "", "Left Image", "Floodfill of left image"}
        desc = "Floodfill the left image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If task.cameraName.Contains("StereoLabs") = False Then
            dst3 = (task.leftView * cv.Scalar.All(options.alpha) + options.beta).ToMat
            flood.Run(dst3)
        Else
            flood.Run(task.leftView)
        End If
        dst2 = flood.dst2
    End Sub
End Class








Public Class Flood_Right : Inherits VB_Algorithm
    Dim Options As New Options_LeftRight
    Public flood As New Flood_RedColor
    Public Sub New()
        labels = {"", "", "Right Image", "Floodfill of right image"}
        desc = "Floodfill the right image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Options.RunVB()
        If task.cameraName.Contains("StereoLabs") = False Then
            dst3 = (task.rightView * cv.Scalar.All(Options.alpha) + Options.beta).ToMat
            flood.Run(dst3)
        Else
            flood.Run(task.rightView)
        End If
        dst2 = flood.dst2
    End Sub
End Class









'Public Class Flood_LeftRight : Inherits VB_Algorithm
'    Public left As New Flood_Left
'    Public right As New Flood_Right
'    Public leftCells As List(Of rcData)
'    Public rightCells As List(Of rcData)
'    Public Sub New()
'        If standalone Then gOptions.displayDst0.Checked = True
'        If standalone Then gOptions.displayDst1.Checked = True
'        labels = {"Right raw image", "Right Flood_RedColor output", "Left raw image", "Left Flood_RedColor output"}
'        desc = "Use floodfill on the left and right images to look for matchable shapes."
'    End Sub
'    Public Sub showLeftRight(rc As rcData, ByRef lrDst As cv.Mat, ByRef dst As cv.Mat)
'        vbDrawContour(lrDst(rc.rect), rc.contour, cv.Scalar.White, 3)
'        vbDrawContour(lrDst(rc.rect), rc.contour, cv.Scalar.Black, 1)
'        vbDrawContour(dst(rc.rect), rc.contour, cv.Scalar.White, -1)
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        left.Run(Nothing)
'        dst2 = task.leftview
'        dst3 = left.dst2
'        Dim rc = task.redCells(task.rcMatch.cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
'        showLeftRight(rc, dst2, dst3)
'        leftCells = New List(Of rcData)(task.redCells)

'        right.Run(Nothing)
'        dst0 = task.rightview
'        dst1 = right.dst2
'        rc = task.redCells(right.flood.colorC.rcMatch.cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
'        showLeftRight(rc, dst0, dst1)
'        rightCells = New List(Of rcData)(task.redCells)
'    End Sub
'End Class










'Public Class Flood_LeftRightKNN : Inherits VB_Algorithm
'    Dim lrFlood As New Flood_LeftRight
'    Dim knn As New KNN_Basics
'    Public Sub New()
'        If standalone Then gOptions.displayDst0.Checked = True
'        If standalone Then gOptions.displayDst1.Checked = True
'        labels = {"Left Flood_RedColor cells", "Right Flood_RedColor cells", "Cell centers - White is a LeftView cell, Red is a RightView cell - click to identify above", ""}
'        desc = "Use KNN to match cells in the left and right image output from Flood_RedColor"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        lrFlood.Run(src)
'        dst0 = lrFlood.dst1
'        dst1 = lrFlood.dst3

'        dst2.SetTo(0)
'        knn.queries.Clear()

'        For Each rc In lrFlood.leftCells
'            dst2.Circle(rc.maxDist, task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
'            knn.queries.Add(rc.maxDist)
'        Next

'        knn.trainInput.Clear()
'        For Each rc In lrFlood.rightCells
'            dst2.Circle(rc.maxDist, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
'            knn.trainInput.Add(rc.maxDist)
'        Next

'        knn.Run(Nothing)

'        For i = 0 To knn.neighbors.Count - 1
'            Dim index = knn.neighbors(i)(0)

'            Dim p1 = lrFlood.leftCells(i).maxDist
'            Dim p2 = lrFlood.rightCells(index).maxDist
'            dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
'        Next
'    End Sub
'End Class








Public Class Flood_FLessAndColor : Inherits VB_Algorithm
    Dim fLess As New FeatureLess_MotionAccum
    Public flood As New Flood_RedColor
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Output of Flood_RedColor", "Below are the cells that contain featureless areas"}
        desc = "Classify each cell as featureless or not."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        addw.src2 = src.Clone
        fLess.Run(src)

        flood.Run(src)
        dst2 = flood.dst2

        dst3.SetTo(0)
        For Each rc In task.redCells
            Dim tmp As New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
            fLess.dst2(rc.rect).CopyTo(tmp, rc.mask)
            If tmp.CountNonZero Then dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
        addw.Run(dst3)
        dst1 = addw.dst2
    End Sub
End Class









Public Class Flood_PointList : Inherits VB_Algorithm
    Public pointList As New List(Of cv.Point)
    Public redCells As New List(Of rcData)
    Public cellMap As cv.Mat
    Public options As New Options_Flood
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "", "Grid Dots are the input points."}
        desc = "The bare minimum to use floodfill - supply points, get mask and rect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst3 = src.Clone

        reduction.Run(src)
        dst0 = reduction.dst2.Clone

        If task.optionsChanged Or standalone Then
            For y = options.stepSize To dst3.Height - 1 Step options.stepSize
                For x = options.stepSize To dst3.Width - options.stepSize - 1 Step options.stepSize
                    Dim p1 = New cv.Point(x, y)
                    pointList.Add(p1)
                    dst3.Circle(p1, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                Next
            Next

            If task.optionsChanged Then
                redCells.Clear()
                cellMap.SetTo(0)
                dst2.SetTo(0)
            End If
        End If

        Dim rect As New cv.Rect

        Dim totalPixels As Integer
        Dim SizeSorted As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim index = 1
        For Each pt In pointList
            Dim floodFlag = cv.FloodFillFlags.FixedRange Or (index << 8)
            Dim rc = New rcData
            rc.pixels = cv.Cv2.FloodFill(dst0, maskPlus, pt, 255, rc.rect, 0, 0, floodFlag)
            If rc.pixels >= task.minPixels And rc.rect.Width < dst2.Width And rc.rect.Height < dst2.Height And
               rc.rect.Width > 0 And rc.rect.Height > 0 Then
                rc.mask = maskPlus(rc.rect).InRange(index, index)

                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)
                rc.maxDist = vbGetMaxDist(rc)

                totalPixels += rc.pixels
                SizeSorted.Add(rc.pixels, rc)
            End If
        Next
        labels(2) = CStr(SizeSorted.Count) + " regions were found with " + Format(totalPixels / dst0.Total, "0%") + " pixels flooded"

        Dim lastcellMap = cellMap.Clone
        Dim lastCells = New List(Of rcData)(redCells)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData) ' stay away from 0...

        dst2.SetTo(0)
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For i = 0 To SizeSorted.Count - 1
            Dim rc = SizeSorted.ElementAt(i).Value
            rc.index = redCells.Count
            Dim lrc = If(lastCells.Count > 0, lastCells(lastcellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)), New rcData)
            rc.indexLast = lrc.index
            rc.color = lrc.color
            If usedColors.Contains(rc.color) Then
                rc.color = New cv.Vec3b(msRNG.Next(10, 240), msRNG.Next(10, 240), msRNG.Next(10, 240)) ' trying to avoid extreme colors... 
            End If

            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            vbDrawContour(cellMap(rc.rect), rc.contour, rc.index, -1)

            redCells.Add(rc)
            usedColors.Add(rc.color)
        Next
    End Sub
End Class





Public Class Flood_RedColor : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        redC.colorOnly = True
        desc = "Floodfill an image and track each cell from image to image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(src)
        redC.Run(fLess.dst2)
        dst2 = redC.dst2
        dst3 = redC.dst3

        labels = redC.labels
    End Sub
End Class







Public Class Flood_Featureless : Inherits VB_Algorithm
    Public classCount As Integer
    Dim floodCell As New FloodCell_Basics
    Public fCells As New List(Of fcData)
    Public Sub New()
        labels = {"", "", "", "Palette output of image at left"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "FloodFill the input and paint it with LUT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static fless As New FeatureLess_Basics
            fless.Run(src)
            src = fless.dst2
        End If

        floodCell.Run(src)
        classCount = task.fCells.Count

        Dim index As Integer = 1
        dst2.SetTo(0)
        fCells.Clear()
        For Each fc In task.fCells
            fc.hull = cv.Cv2.ConvexHull(fc.contour, True).ToList
            vbDrawContour(dst2(fc.rect), fc.hull, fc.index, -1)
            fCells.Add(fc)
        Next

        labels(2) = "Hulls were added for each of the " + CStr(fCells.Count) + " regions identified"
        dst3 = vbPalette(dst2 * 255 / fCells.Count)
    End Sub
End Class