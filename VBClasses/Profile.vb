Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Profile_Basics : Inherits TaskParent
    Public ptLeft As Point3f, ptRight As Point3f, ptTop As Point3f, ptBot As Point3f, ptFront As Point3f, ptBack As Point3f
    Public cornerNames As New List(Of String)({"   First (white)", "   Left (light blue)", "   Right (red)", "   Top (green)",
                                                       "   Bottom (white)", "   Front (yellow)", "   Back (blue)"})
    Public cornerColors As New List(Of Scalar)({white, Scalar.LightBlue, Scalar.Red, Scalar.Green,
                                                           white, Scalar.Yellow, Scalar.Blue})
    Public corners3D As New List(Of Point3f)
    Public corners As New List(Of cv.Point)
    Public cornersRaw As New List(Of cv.Point)
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the left/right, top/bottom, and near/far sides of a cell"
    End Sub
    Private Function point3fToString(v As Point3f) As String
        Return v.X.ToString(fmt3) + vbTab + v.Y.ToString(fmt3) + vbTab + v.Z.ToString(fmt3)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)

        Dim rc = task.rcD
        Dim depthPixels = CountNonZero(task.depthmask(rc.rect))
        If depthPixels = 0 Then
            strOut = "There is no depth data for that cell."
            Exit Sub
        End If
        If rc.contour.Count < 4 Then Exit Sub

        dst3.SetTo(0)
        DrawTour(dst3(rc.rect), rc.contour, Scalar.Yellow)

        Dim sortLeft As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sortTop As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sortFront As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sort2Dleft As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sort2Dtop As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        rc.contour3D = New List(Of Point3f)
        For i = 0 To rc.contour.Count - 1
            Dim pt = rc.contour(i)
            Dim vec = task.pointCloud(rc.rect).Get(Of Point3f)(pt.Y, pt.X)
            If Single.IsNaN(vec.Z) Or Single.IsInfinity(vec.Z) Then Continue For
            If vec.Z Then
                sortLeft.Add(pt.X, i)
                sortTop.Add(pt.Y, i)
                sortFront.Add(CInt(vec.Z * 1000), i)
                rc.contour3D.Add(vec)
            Else
                sort2Dleft.Add(pt.X, i)
                sort2Dtop.Add(pt.Y, i)
            End If
        Next

        If sortLeft.Count = 0 Then
            sortLeft = sort2Dleft
            sortTop = sort2Dtop
        End If

        corners3D.Clear()
        corners.Clear()
        cornersRaw.Clear()

        corners.Add(New cv.Point(rc.rect.X + rc.contour(0).X, rc.rect.Y + rc.contour(0).Y)) ' show the first contour cv.Point...
        cornersRaw.Add(rc.contour(0)) ' show the first contour cv.Point...
        corners3D.Add(task.pointCloud.Get(Of Point3f)(rc.rect.Y + rc.contour(0).Y, rc.rect.X + rc.contour(0).X))

        For i As Integer = 0 To 6 - 1
            Dim index As Integer = Choose(i + 1, 0, sortLeft.Count - 1, 0, sortTop.Count - 1, 0, sortFront.Count - 1)
            Dim ptList As SortedList(Of Integer, Integer) = Choose(i + 1, sortLeft, sortLeft, sortTop, sortTop, sortFront, sortFront)
            If ptList.Count > 0 Then
                Dim pt = rc.contour(ptList.ElementAt(index).Value)
                cornersRaw.Add(pt)
                corners.Add(New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
                corners3D.Add(task.pointCloud(rc.rect).Get(Of Point3f)(pt.Y, pt.X))
            End If
        Next

        For i = 0 To corners.Count - 1
        Circle(dst3, corners(i), task.DotSize + 2, cornerColors(i), -1, task.lineType)
        Next

        If task.heartBeat Then
            strOut = "X     " + vbTab + "Y     " + vbTab + "Z " + vbTab + "units=meters" + vbCrLf
            Dim w = task.gridWH
            For i = 0 To corners.Count - 1
                strOut += point3fToString(corners3D(i)) + vbTab + cornerNames(i) + vbCrLf
            Next
            strOut += vbCrLf + "The contour may show points further away but they don't have depth."
            If sortFront.Count = 0 Then strOut += vbCrLf + "None of the contour points had depth."
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Profile_Rotation : Inherits TaskParent
    Public strMsg As String = "Then use the 'Options_IMU' sliders to rotate the cell" + vbCrLf +
                                  "It is a common mistake to the OpenGL sliders to try to move cell but they don't - use 'Options_IMU' sliders"
    Dim options As New Options_IMU
    Public Sub New()
        If standalone Then task.gOptions.setGravityUsage(False)
        labels(2) = "Top matrix is the current gMatrix while the bottom one includes the Y-axis rotation."
        desc = "Build the rotation matrix around the Y-axis"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis")
            ySlider.value += 1
            If ySlider.value = ySlider.maximum Then ySlider.value = ySlider.minimum
            SetTrueText("When running standaloneTest(), the Y-axis slider is rotating from -90 to 90.", 3)
        End If

        If standaloneTest() Then
            options.Run()
            strOut = "Gravity-oriented gMatrix" + vbCrLf
            strOut += task.gravityMatrix.strOut + vbCrLf
            strOut += vbCrLf + "New gMatrix from sliders" + vbCrLf
            strOut += IMU_GMatrix_TA.gMatrixToStr(task.gravityMatrix.gMatrix) + vbCrLf + vbCrLf
            strOut += "Angle X = " + options.rotateX.ToString(fmt1) + vbCrLf
            strOut += "Angle Y = " + options.rotateY.ToString(fmt1) + vbCrLf
            strOut += "Angle Z = " + options.rotateZ.ToString(fmt1) + vbCrLf
            SetTrueText(strOut + vbCrLf + vbCrLf + strMsg)
        End If
    End Sub
End Class







Public Class XR_Profile_Derivative : Inherits TaskParent
    Public sides As New Profile_Basics
    Dim saveTrueText As New List(Of TrueText)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Select a cell to analyze its contour", "Selected cell:  yellow = closer, blue = farther, white = no depth"}
        desc = "Visualize the derivative of X, Y, and Z in the contour of a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2

        If sides.redC.rcList.Count = 0 Then Exit Sub ' nothing to work on...
        Dim rc = task.rcD
        If rc Is Nothing Then rc = sides.redC.rcList(0)

        Dim offset As Integer = 30
        Dim rsizeX = (dst2.Width - offset * 2) / rc.rect.Width
        Dim rsizeY = (dst2.Height - offset * 2) / rc.rect.Height
        saveTrueText.Clear()
        task.trueData.Clear()
        dst3.SetTo(0)

        Dim color As Scalar, near = Scalar.Yellow, far = Scalar.Blue
        If rc.mapID > 0 Then
            For i = 0 To rc.contour.Count - 1
                Dim pt = rc.contour(i)
                Dim vec = task.pointCloud(rc.rect).Get(Of Point3f)(pt.Y, pt.X)
                pt = New cv.Point(pt.X * rsizeX + offset, pt.Y * rsizeY + offset)
                Dim mmZ = GetMinMax(task.pcSplit(2)(rc.rect), rc.mask)
                Dim t = If(mmZ.maxVal = 0, 0, (vec.Z - mmZ.minVal) / (mmZ.maxVal - mmZ.minVal))
                If vec.Z > 0 And t > 0 Then
                    Dim b = ((1 - t) * near(0) + t * far(0))
                    Dim g = ((1 - t) * near(1) + t * far(1))
                    Dim r = ((1 - t) * near(2) + t * far(2))
                    color = New Scalar(b, g, r)
                Else
                    color = white
                End If
                Circle(dst3, pt, task.DotSize, color, -1, task.lineType)

                If sides.cornersRaw.Contains(rc.contour(i)) Then
                    Dim index = sides.cornersRaw.IndexOf(rc.contour(i))
                    Circle(dst1, pt, task.DotSize + 5, white, -1, task.lineType)
                    Circle(dst1, pt, task.DotSize + 3, sides.cornerColors(index), -1, task.lineType)
                    SetTrueText(sides.cornerNames(index), pt, 3)
                End If
            Next
        End If

        strOut = "Points are presented clockwise starting at White dot (leftmost top cv.Point)" + vbCrLf +
                             "yellow = closer, blue = farther, " + vbCrLf + vbCrLf + sides.strOut

        dst1 = sides.dst3.Clone
        For i = 0 To sides.corners.Count - 1
            color = sides.cornerColors(i)
            SetTrueText(sides.cornerNames(i), sides.corners(i), 1)
            Circle(dst1, sides.corners(i), task.DotSize, color, -1, task.lineType)
        Next
        SetTrueText(strOut, 1)
        saveTrueText = New List(Of TrueText)(trueData)
        If saveTrueText IsNot Nothing Then trueData = New List(Of TrueText)(saveTrueText)
    End Sub
End Class








Public Class XR_Profile_ConcentrationSide : Inherits TaskParent
    Dim profile As New Profile_ConcentrationTop
    Public Sub New()
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        labels = {"", "The outline of the selected RedCloud cell", traceName + " - click any RedCloud cell to visualize it's side view in the upper right image.", ""}
        desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        profile.Run(src)
        dst1 = profile.dst1
        dst2 = profile.dst2
        dst3 = profile.dst3

        labels(3) = profile.labels(3)
    End Sub
End Class







Public Class Profile_ConcentrationTop : Inherits TaskParent
    Dim plot As New PlotTime_Single
    Dim rotate As New Profile_Rotation
    Public sides As New Profile_Basics
    Dim heat As New HeatMap_Basics
    Dim options As New Options_HeatMap
    Dim maxAverage As Single
    Dim peakRotation As Integer
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")

        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rcD
        If rc Is Nothing Then
            If sides.redC.rcList.Count = 0 Then Exit Sub
            rc = sides.redC.rcList(0)
        End If

        If rc.contour3D.Count = 0 Then
            SetTrueText("The selected cell has no 3D data.  The 3D data can only be computed from cells with depth data.", 1)
            Exit Sub
        End If
        Dim vecMat As Mat = Mat.FromPixelData(rc.contour3D.Count, 1, MatType.CV_32FC3, rc.contour3D.ToArray)

        ySlider.Value += 1
        rotate.Run(src)
        Dim output = (vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * task.gravityMatrix.gMatrix).ToMat  ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
        vecMat = output.Reshape(3, vecMat.Rows)

        heat.Run(vecMat)
        If options.topView Then
            Threshold(heat.dst0, dst1, 0, 255, ThresholdTypes.Binary)
        Else
            Threshold(heat.dst1, dst1, 0, 255, ThresholdTypes.Binary)
        End If

        Dim count = CountNonZero(dst1)
        If maxAverage < count Then
            maxAverage = count
            peakRotation = ySlider.Value
        End If

        plot.plotData = count
        plot.Run(src)
        dst3 = plot.dst2

        If ySlider.Value >= 45 Then
            maxAverage = 0
            peakRotation = -45
            ySlider.Value = -45
        End If

        labels(3) = "Peak cell concentration in the histogram = " + CStr(CInt(maxAverage)) + " at " + CStr(peakRotation) + " degrees"
    End Sub
End Class









Public Class XR_Profile_Kalman : Inherits TaskParent
    Dim sides As New Profile_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(12 - 1)
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Profile_Basics output without Kalman", "Profile_Basics output with Kalman"}
        desc = "Use Kalman to smooth the results of the contour key points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        If sides.redC.rcList.Count = 0 Then Exit Sub ' nothing to work on...
        dst1 = sides.dst2
        dst2 = sides.dst3
        Dim rc = task.rcD
        If rc Is Nothing Then rc = sides.redC.rcList(0)

        If kalman.kInput.Count <> sides.corners.Count * 2 Then ReDim kalman.kInput(sides.corners.Count * 2 - 1)
        For i = 0 To sides.corners.Count - 1
            kalman.kInput(i * 2) = sides.corners(i).X
            kalman.kInput(i * 2 + 1) = sides.corners(i).Y
        Next

        kalman.Run(emptyMat)

        If rc.mapID > 0 Then
            dst3.SetTo(0)
            DrawTour(dst3(rc.rect), rc.contour, Scalar.Yellow)
            For i = 0 To sides.corners.Count - 1
                Dim pt = New cv.Point(CInt(kalman.kOutput(i * 2)), CInt(kalman.kOutput(i * 2 + 1)))
                Circle(dst3, pt, task.DotSize + 2, sides.cornerColors(i), -1, task.lineType)
            Next
        End If
        SetTrueText(sides.strOut, 3)
        SetTrueText("Select a cell in the upper right image", 2)
    End Sub
End Class
