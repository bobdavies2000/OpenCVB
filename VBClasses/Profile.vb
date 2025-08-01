﻿Imports cv = OpenCvSharp
Public Class Profile_Basics : Inherits TaskParent
    Public ptLeft As cv.Point3f, ptRight As cv.Point3f, ptTop As cv.Point3f, ptBot As cv.Point3f, ptFront As cv.Point3f, ptBack As cv.Point3f
    Public cornerNames As New List(Of String)({"   First (white)", "   Left (light blue)", "   Right (red)", "   Top (green)",
                                               "   Bottom (white)", "   Front (yellow)", "   Back (blue)"})
    Public cornerColors As New List(Of cv.Scalar)({white, cv.Scalar.LightBlue, cv.Scalar.Red, cv.Scalar.Green,
                                                   white, cv.Scalar.Yellow, cv.Scalar.Blue})
    Public corners3D As New List(Of cv.Point3f)
    Public corners As New List(Of cv.Point)
    Public cornersRaw As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the left/right, top/bottom, and near/far sides of a cell"
    End Sub
    Private Function point3fToString(v As cv.Point3f) As String
        Return Format(v.X, fmt3) + vbTab + Format(v.Y, fmt3) + vbTab + Format(v.Z, fmt3)
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        Dim rc = task.rcD
        If rc.depthPixels = 0 Then
            strOut = "There is no depth data for that cell."
            Exit Sub
        End If
        If rc.contour.Count < 4 Then Exit Sub

        dst3.SetTo(0)
        DrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)

        Dim sortLeft As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sortTop As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sortFront As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sort2Dleft As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim sort2Dtop As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        rc.contour3D = New List(Of cv.Point3f)
        For i = 0 To rc.contour.Count - 1
            Dim pt = rc.contour(i)
            Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
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

        corners.Add(New cv.Point(rc.rect.X + rc.contour(0).X, rc.rect.Y + rc.contour(0).Y)) ' show the first contour point...
        cornersRaw.Add(rc.contour(0)) ' show the first contour point...
        corners3D.Add(task.pointCloud.Get(Of cv.Point3f)(rc.rect.Y + rc.contour(0).Y, rc.rect.X + rc.contour(0).X))

        For i As Integer = 0 To 6 - 1
            Dim index As Integer = Choose(i + 1, 0, sortLeft.Count - 1, 0, sortTop.Count - 1, 0, sortFront.Count - 1)
            Dim ptList As SortedList(Of Integer, Integer) = Choose(i + 1, sortLeft, sortLeft, sortTop, sortTop, sortFront, sortFront)
            If ptList.Count > 0 Then
                Dim pt = rc.contour(ptList.ElementAt(index).Value)
                cornersRaw.Add(pt)
                corners.Add(New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
                corners3D.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
            End If
        Next

        For i = 0 To corners.Count - 1
            DrawCircle(dst3, corners(i), task.DotSize + 2, cornerColors(i))
        Next

        If task.heartBeat Then
            strOut = "X     " + vbTab + "Y     " + vbTab + "Z " + vbTab + "units=meters" + vbCrLf
            Dim w = task.brickSize
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
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis")
            ySlider.value += 1
            If ySlider.value = ySlider.maximum Then ySlider.value = ySlider.minimum
            SetTrueText("When running standaloneTest(), the Y-axis slider is rotating from -90 to 90.", 3)
        End If

        If standaloneTest() Then
            options.Run()
            strOut = "Gravity-oriented gMatrix" + vbCrLf
            strOut += task.gmat.strOut + vbCrLf
            strOut += vbCrLf + "New gMatrix from sliders" + vbCrLf
            strOut += gMatrixToStr(task.gmat.gMatrix) + vbCrLf + vbCrLf
            strOut += "Angle X = " + Format(options.rotateX, fmt1) + vbCrLf
            strOut += "Angle Y = " + Format(options.rotateY, fmt1) + vbCrLf
            strOut += "Angle Z = " + Format(options.rotateZ, fmt1) + vbCrLf
            SetTrueText(strOut + vbCrLf + vbCrLf + strMsg)
        End If
    End Sub
End Class







Public Class Profile_Derivative : Inherits TaskParent
    Public sides As New Profile_Basics
    Dim saveTrueText As New List(Of TrueText)
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"", "", "Select a cell to analyze its contour", "Selected cell:  yellow = closer, blue = farther, white = no depth"}
        desc = "Visualize the derivative of X, Y, and Z in the contour of a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rcD

        Dim offset As Integer = 30
        Dim rsizeX = (dst2.Width - offset * 2) / rc.rect.Width
        Dim rsizeY = (dst2.Height - offset * 2) / rc.rect.Height
        saveTrueText.Clear()
        task.trueData.Clear()
        dst3.SetTo(0)

        Dim color As cv.Scalar, near = cv.Scalar.Yellow, far = cv.Scalar.Blue
        If rc.index > 0 Then
            For i = 0 To rc.contour.Count - 1
                Dim pt = rc.contour(i)
                Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                pt = New cv.Point(pt.X * rsizeX + offset, pt.Y * rsizeY + offset)
                Dim t = If(rc.mmZ.maxVal = 0, 0, (vec.Z - rc.mmZ.minVal) / (rc.mmZ.maxVal - rc.mmZ.minVal))
                If vec.Z > 0 And t > 0 Then
                    Dim b = ((1 - t) * near(0) + t * far(0))
                    Dim g = ((1 - t) * near(1) + t * far(1))
                    Dim r = ((1 - t) * near(2) + t * far(2))
                    color = New cv.Scalar(b, g, r)
                Else
                    color = white
                End If
                DrawCircle(dst3, pt, task.DotSize, color)

                If sides.cornersRaw.Contains(rc.contour(i)) Then
                    Dim index = sides.cornersRaw.IndexOf(rc.contour(i))
                    DrawCircle(dst1, pt, task.DotSize + 5, white)
                    DrawCircle(dst1, pt, task.DotSize + 3, sides.cornerColors(index))
                    SetTrueText(sides.cornerNames(index), pt, 3)
                End If
            Next
        End If

        strOut = "Points are presented clockwise starting at White dot (leftmost top point)" + vbCrLf +
                         "yellow = closer, blue = farther, " + vbCrLf + vbCrLf + sides.strOut

        dst1 = sides.dst3.Clone
        For i = 0 To sides.corners.Count - 1
            color = sides.cornerColors(i)
            SetTrueText(sides.cornerNames(i), sides.corners(i), 1)
            DrawCircle(dst1, sides.corners(i), task.DotSize, color)
        Next
        SetTrueText(strOut, 1)
        saveTrueText = New List(Of TrueText)(trueData)
        If saveTrueText IsNot Nothing Then trueData = New List(Of TrueText)(saveTrueText)
    End Sub
End Class








Public Class Profile_ConcentrationSide : Inherits TaskParent
    Dim profile As New Profile_ConcentrationTop
    Public Sub New()
        OptionParent.findCheckBox("Top View (Unchecked Side View)").Checked = False
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
    Dim plot As New Plot_OverTimeSingle
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
        If rc.contour3D.Count = 0 Then
            SetTrueText("The selected cell has no 3D data.  The 3D data can only be computed from cells with depth data.", 1)
            Exit Sub
        End If
        Dim vecMat As cv.Mat = cv.Mat.FromPixelData(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)

        ySlider.Value += 1
        rotate.Run(src)
        Dim output = (vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * task.gmat.gMatrix).ToMat  ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
        vecMat = output.Reshape(3, vecMat.Rows)

        heat.Run(vecMat)
        If options.topView Then
            dst1 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Else
            dst1 = heat.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End If

        Dim count = dst1.CountNonZero
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









Public Class Profile_Kalman : Inherits TaskParent
    Dim sides As New Profile_Basics
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(12 - 1)
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Profile_Basics output without Kalman", "Profile_Basics output with Kalman"}
        desc = "Use Kalman to smooth the results of the contour key points"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst1 = sides.dst2
        dst2 = sides.dst3
        Dim rc = task.rcD

        If task.kalman.kInput.Count <> sides.corners.Count * 2 Then ReDim task.kalman.kInput(sides.corners.Count * 2 - 1)
        For i = 0 To sides.corners.Count - 1
            task.kalman.kInput(i * 2) = sides.corners(i).X
            task.kalman.kInput(i * 2 + 1) = sides.corners(i).Y
        Next

        task.kalman.Run(emptyMat)

        If rc.index > 0 Then
            dst3.SetTo(0)
            DrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
            For i = 0 To sides.corners.Count - 1
                Dim pt = New cv.Point(CInt(task.kalman.kOutput(i * 2)), CInt(task.kalman.kOutput(i * 2 + 1)))
                DrawCircle(dst3,pt, task.DotSize + 2, sides.cornerColors(i))
            Next
        End If
        SetTrueText(sides.strOut, 3)
        SetTrueText("Select a cell in the upper right image", 2)
    End Sub
End Class
