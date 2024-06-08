Imports cv = OpenCvSharp
Public Class Profile_Basics : Inherits VB_Parent
    Public ptLeft As cv.Point3f, ptRight As cv.Point3f, ptTop As cv.Point3f, ptBot As cv.Point3f, ptFront As cv.Point3f, ptBack As cv.Point3f
    Public cornerNames As New List(Of String)({"   First (white)", "   Left (light blue)", "   Right (red)", "   Top (green)",
                                               "   Bottom (white)", "   Front (yellow)", "   Back (blue)"})
    Public cornerColors As New List(Of cv.Scalar)({cv.Scalar.White, cv.Scalar.LightBlue, cv.Scalar.Red, cv.Scalar.Green,
                                                   cv.Scalar.White, cv.Scalar.Yellow, cv.Scalar.Blue})
    Public corners3D As New List(Of cv.Point3f)
    Public corners As New List(Of cv.Point)
    Public cornersRaw As New List(Of cv.Point)
    Public redC As New RedCloud_Basics
    Public Sub New()
        desc = "Find the left/right, top/bottom, and near/far sides of a cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        Dim rc = task.rc
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

        For i = 0 To 6 - 1
            Dim index = Choose(i + 1, 0, sortLeft.Count - 1, 0, sortTop.Count - 1, 0, sortFront.Count - 1)
            Dim ptList As SortedList(Of Integer, Integer) = Choose(i + 1, sortLeft, sortLeft, sortTop, sortTop, sortFront, sortFront)
            If ptList.Count > 0 Then
                Dim pt = rc.contour(ptList.ElementAt(index).Value)
                cornersRaw.Add(pt)
                corners.Add(New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
                corners3D.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
            End If
        Next

        For i = 0 To corners.Count - 1
            DrawCircle(dst3,corners(i), task.dotSize + 2, cornerColors(i))
        Next

        If task.heartBeat Then
            strOut = "X     " + vbTab + "Y     " + vbTab + "Z " + vbTab + "units=meters" + vbCrLf
            Dim w = task.gOptions.GridSize.Value
            For i = 0 To corners.Count - 1
                strOut += point3fToString(corners3D(i)) + vbTab + cornerNames(i) + vbCrLf
            Next
            strOut += vbCrLf + "The contour may show points further away but they don't have depth."
            If sortFront.Count = 0 Then strOut += vbCrLf + "None of the contour points had depth."
        End If
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class Profile_Rotation : Inherits VB_Parent
    Public gMat As New IMU_GMatrix
    Public strMsg As String = "Then use the 'Options_IMU' sliders to rotate the cell" + vbCrLf +
                              "It is a common mistake to the OpenGL sliders to try to move cell but they don't - use 'Options_IMU' sliders"
    Dim options As New Options_IMU
    Public Sub New()
        If standaloneTest() Then task.gOptions.gravityPointCloud.Checked = False
        labels(2) = "Top matrix is the current gMatrix while the bottom one includes the Y-axis rotation."
        desc = "Build the rotation matrix around the Y-axis"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            Static ySlider = FindSlider("Rotate pointcloud around Y-axis")
            ySlider.value += 1
            If ySlider.value = ySlider.maximum Then ySlider.value = ySlider.minimum
            setTrueText("When running standaloneTest(), the Y-axis slider is rotating from -90 to 90.", 3)
        End If

        gMat.Run(src)

        If standaloneTest() Then
            options.RunVB()
            strOut = "Gravity-oriented gMatrix" + vbCrLf
            strOut += task.gMat.strOut + vbCrLf
            strOut += vbCrLf + "New gMatrix from sliders" + vbCrLf
            strOut += gMatrixToStr(gMat.gMatrix) + vbCrLf + vbCrLf
            strOut += "Angle X = " + Format(options.rotateX, fmt1) + vbCrLf
            strOut += "Angle Y = " + Format(options.rotateY, fmt1) + vbCrLf
            strOut += "Angle Z = " + Format(options.rotateZ, fmt1) + vbCrLf
            setTrueText(strOut + vbCrLf + vbCrLf + strMsg)
        End If
    End Sub
End Class







Public Class Profile_Derivative : Inherits VB_Parent
    Public sides As New Profile_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Select a cell to analyze its contour", "Selected cell:  yellow = closer, blue = farther, white = no depth"}
        desc = "Visualize the derivative of X, Y, and Z in the contour of a RedCloud cell"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static saveTrueText As New List(Of trueText)
        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rc

        Dim offset As Integer = 30
        Dim rsizeX = (dst2.Width - offset * 2) / rc.rect.Width
        Dim rsizeY = (dst2.Height - offset * 2) / rc.rect.Height
        saveTrueText.Clear()
        trueData.Clear()
        dst3.SetTo(0)

        Dim color As cv.Scalar, near = cv.Scalar.Yellow, far = cv.Scalar.Blue
        If rc.index > 0 Then
            For i = 0 To rc.contour.Count - 1
                Dim pt = rc.contour(i)
                Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                pt = New cv.Point(pt.X * rsizeX + offset, pt.Y * rsizeY + offset)
                Dim t = If(rc.maxVec.Z = 0, 0, (vec.Z - rc.minVec.Z) / (rc.maxVec.Z - rc.minVec.Z))
                If vec.Z > 0 And t > 0 Then
                    Dim b = ((1 - t) * near(0) + t * far(0))
                    Dim g = ((1 - t) * near(1) + t * far(1))
                    Dim r = ((1 - t) * near(2) + t * far(2))
                    color = New cv.Scalar(b, g, r)
                Else
                    color = cv.Scalar.White
                End If
                DrawCircle(dst3,pt, task.dotSize, color)

                If sides.cornersRaw.Contains(rc.contour(i)) Then
                    Dim index = sides.cornersRaw.IndexOf(rc.contour(i))
                    DrawCircle(dst1,pt, task.dotSize + 5, cv.Scalar.White)
                    DrawCircle(dst1,pt, task.dotSize + 3, sides.cornerColors(index))
                    setTrueText(sides.cornerNames(index), pt, 3)
                End If
            Next
        End If

        strOut = "Points are presented clockwise starting at White dot (leftmost top point)" + vbCrLf +
                         "yellow = closer, blue = farther, " + vbCrLf + vbCrLf + sides.strOut

        dst1 = sides.dst3.Clone
        For i = 0 To sides.corners.Count - 1
            color = sides.cornerColors(i)
            setTrueText(sides.cornerNames(i), sides.corners(i), 1)
            DrawCircle(dst1,sides.corners(i), task.dotSize, color)
        Next
        setTrueText(strOut, 1)
        saveTrueText = New List(Of trueText)(trueData)
        If saveTrueText IsNot Nothing Then trueData = New List(Of trueText)(saveTrueText)
    End Sub
End Class








Public Class Profile_ConcentrationSide : Inherits VB_Parent
    Dim profile As New Profile_ConcentrationTop
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        labels = {"", "The outline of the selected RedCloud cell", traceName + " - click any RedCloud cell to visualize it's side view in the upper right image.", ""}
        desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        profile.Run(src)
        dst1 = profile.dst1
        dst2 = profile.dst2
        dst3 = profile.dst3

        labels(3) = profile.labels(3)
    End Sub
End Class







Public Class Profile_ConcentrationTop : Inherits VB_Parent
    Dim plot As New Plot_OverTimeSingle
    Dim rotate As New Profile_Rotation
    Public sides As New Profile_Basics
    Dim heat As New HeatMap_Basics
    Dim options As New Options_HeatMap
    Public Sub New()
        task.gOptions.gravityPointCloud.Checked = False
        task.gOptions.setDisplay1()
        desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()

        Static ySlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static maxAverage As Single
        Static peakRotation As Integer

        sides.Run(src)
        dst2 = sides.dst2
        Dim rc = task.rc
        If rc.contour3D.Count = 0 Then
            setTrueText("The selected cell has no 3D data.  The 3D data can only be computed from cells with depth data.", 1)
            Exit Sub
        End If
        Dim vecMat As New cv.Mat(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)

        ySlider.Value += 1
        rotate.Run(empty)
        Dim output = (vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix).ToMat  ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
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

        plot.plotData = New cv.Scalar(count, 0, 0)
        plot.Run(empty)
        dst3 = plot.dst2

        If ySlider.Value >= 45 Then
            maxAverage = 0
            peakRotation = -45
            ySlider.Value = -45
        End If

        labels(3) = "Peak cell concentration in the histogram = " + CStr(CInt(maxAverage)) + " at " + CStr(peakRotation) + " degrees"
    End Sub
End Class







Public Class Profile_OpenGL : Inherits VB_Parent
    Dim sides As New Profile_Basics
    Public rotate As New Profile_Rotation
    Dim heat As New HeatMap_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32FC3, 0)
        If standaloneTest() Then task.gOptions.gravityPointCloud.Checked = False
        task.ogl.options.PointSizeSlider.Value = 10
        task.ogl.oglFunction = oCase.pcPointsAlone
        desc = "Visualize just the RedCloud cell contour in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3
        Dim rc = task.rc

        If rc.contour3D.Count > 0 Then
            Dim vecMat As New cv.Mat(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)
            rotate.Run(empty)
            Dim output As cv.Mat = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix  ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
            task.ogl.dataInput = output.Reshape(3, vecMat.Rows)
            task.ogl.pointCloudInput = New cv.Mat

            task.ogl.Run(New cv.Mat)
            heat.Run(vecMat)
            dst1 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End If
        setTrueText("Select a RedCloud Cell to display the contour in OpenGL." + vbCrLf + rotate.strMsg, 3)
    End Sub
End Class









Public Class Profile_Kalman : Inherits VB_Parent
    Dim sides As New Profile_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(12 - 1)
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Profile_Basics output without Kalman", "Profile_Basics output with Kalman"}
        desc = "Use Kalman to smooth the results of the contour key points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sides.Run(src)
        dst0 = sides.redC.dst0
        dst1 = sides.dst2
        dst2 = sides.dst3
        Dim rc = task.rc

        If kalman.kInput.Count <> sides.corners.Count * 2 Then ReDim kalman.kInput(sides.corners.Count * 2 - 1)
        For i = 0 To sides.corners.Count - 1
            kalman.kInput(i * 2) = sides.corners(i).X
            kalman.kInput(i * 2 + 1) = sides.corners(i).Y
        Next

        kalman.Run(empty)

        If rc.index > 0 Then
            dst3.SetTo(0)
            DrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
            For i = 0 To sides.corners.Count - 1
                Dim pt = New cv.Point(CInt(kalman.kOutput(i * 2)), CInt(kalman.kOutput(i * 2 + 1)))
                DrawCircle(dst3,pt, task.dotSize + 2, sides.cornerColors(i))
            Next
        End If
        setTrueText(sides.strOut, 3)
        setTrueText("Select a cell in the upper right image", 2)
    End Sub
End Class
