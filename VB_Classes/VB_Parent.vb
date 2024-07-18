Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports OpenCvSharp

Public Class trueText
    Public text As String
    Public picTag = 2
    Public pt As cv.Point
    Private Sub setup(_text As String, _pt As cv.Point, camPicIndex As Integer)
        text = _text
        pt = _pt
        picTag = camPicIndex
    End Sub
    Public Sub New(_text As String, _pt As cv.Point, camPicIndex As Integer)
        setup(_text, _pt, camPicIndex)
    End Sub
    Public Sub New(_text As String, _pt As cv.Point)
        setup(_text, _pt, 2)
    End Sub
End Class
Public Class VB_Parent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public standalone As Boolean
    Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat, empty As cv.Mat
    Public labels(4 - 1) As String
    Public msRNG As New System.Random
    Public algorithm As Object
    Public traceName As String
    Public desc As String
    Public black As New cv.Vec3b, white As New cv.Vec3b(255, 255, 255), grayColor As New cv.Vec3b(127, 127, 127)
    Public yellow = New cv.Vec3b(0, 255, 255), purple = New cv.Vec3b(255, 0, 255), teal = New cv.Vec3b(255, 255, 0)
    Public red = New cv.Vec3b(0, 0, 255), green = New cv.Vec3b(0, 255, 0), blue = New cv.Vec3b(255, 0, 0)
    Public zero3f As New cv.Point3f(0, 0, 0)
    Public newVec4f As New cv.Vec4f
    Public cPtr As IntPtr
    Public trueData As New List(Of trueText)
    Public strOut As String
    Dim retryCount As Integer
    Public Const depthListMaxCount As Integer = 10

    Public pipeCount As Integer

    Public Const RESULT_DST0 = 0 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST1 = 1 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST2 = 2 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST3 = 3 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)

    Public pythonPipeIndex As Integer ' increment this for each algorithm to avoid any conflicts with other Python apps.
    Dim callStack = ""
    Public Sub New()
        algorithm = Me
        traceName = Me.GetType.Name
        labels = {"", "", traceName, ""}
        Dim stackTrace = Environment.StackTrace
        Dim lines() = stackTrace.Split(vbCrLf)
        For i = 0 To lines.Count - 1
            lines(i) = Trim(lines(i))
            Dim offset = InStr(lines(i), "VB_Classes.")
            If offset > 0 Then
                Dim partLine = Mid(lines(i), offset + 11)
                If partLine.StartsWith("AlgorithmList.createVBAlgorithm") Then Exit For
                Dim split() = partLine.Split("\")
                partLine = Mid(partLine, 1, InStr(partLine, ".") - 1)
                If Not (partLine.StartsWith("VB_Parent") Or partLine.StartsWith("VBtask")) Then
                    callStack = partLine + "\" + callStack
                End If
            End If
        Next

        If task.algName.StartsWith("CPP_") Then
            callTrace.Clear()
            algorithm_ms.Clear()
            algorithmNames.Clear()
            callTrace.Add("CPP_Basics\")
        End If

        If traceName <> "Controls_Basics" Then
            standalone = callTrace(0) = traceName + "\" ' only the first is standaloneTest() (the primary algorithm.)
            If traceName = "Python_Run" Then standalone = True
            If task.algName.StartsWith("CS_") Then callStack = callTrace(0) + callStack
            If standalone = False And callTrace.Contains(callStack) = False Then callTrace.Add(callStack)
        End If
        dst0 = New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        task.activeObjects.Add(Me)

        If task.recordTimings Then
            If standalone And task.testAllRunning = False Then
                algorithm_ms.Clear()
                algorithmNames.Clear()
                algorithmNames.Add("waitingForInput")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add("inputBufferCopy")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add("ReturnCopyTime")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add(traceName)
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmStack = New Stack()
                algorithmStack.Push(0)
                algorithmStack.Push(1)
                algorithmStack.Push(2)
                algorithmStack.Push(3)
            End If
        End If
    End Sub
    Public Function GetWindowImage(ByVal WindowHandle As IntPtr, ByVal rect As cv.Rect) As Bitmap
        Dim b As New Bitmap(rect.Width, rect.Height, Imaging.PixelFormat.Format24bppRgb)

        Using img As Graphics = Graphics.FromImage(b)
            Dim ImageHDC As IntPtr = img.GetHdc
            Try
                Using window As Graphics = Graphics.FromHwnd(WindowHandle)
                    Dim WindowHDC As IntPtr = window.GetHdc
                    BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, CopyPixelOperation.SourceCopy)
                    window.ReleaseHdc()
                End Using
                img.ReleaseHdc()
            Catch ex As Exception
                ' ignoring the error - they probably closed the OpenGL window.
            End Try
        End Using

        Return b
    End Function
    Public Function vecToScalar(v As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(v(0), v(1), v(2))
    End Function
    Public Sub DrawRotatedRectangle(rotatedRect As cv.RotatedRect, dst As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        dst.FillConvexPoly(vertices, color, task.lineType)
    End Sub
    Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        ' draw a scale along the side
        Dim spacer = CInt(dst.Height / (lineCount + 1))
        Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, cv.Scalar.White, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            cv.Cv2.PutText(dst, nextText, p1, cv.HersheyFonts.HersheyPlain, task.cvFontSize, cv.Scalar.White,
                           task.cvFontThickness, task.lineType)
        Next
    End Sub
    Public Sub DrawFatLine(p1 As cv.Point2f, p2 As cv.Point2f, dst As cv.Mat, fatColor As cv.Scalar)
        Dim pad = 2
        If task.WorkingRes.Width >= 640 Then pad = 6
        dst.Line(p1, p2, fatColor, task.lineWidth + pad, task.lineType)
        DrawLine(dst, p1, p2, cv.Scalar.Black)
    End Sub
    Public Function IntersectTest(p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f, rect As cv.Rect) As cv.Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        ' If pt.X >= rect.Width Or pt.Y >= rect.Height Then Return New cv.Point2f
        Return pt
    End Function
    Public Function PrepareDepthInput(index As Integer) As cv.Mat
        If task.useGravityPointcloud Then Return task.pcSplit(index) ' already oriented to gravity

        ' rebuild the pointcloud so it is oriented to gravity.
        Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
        Dim split = pc.Split()
        Return split(index)
    End Function
    Public Function GetNormalize32f(Input As cv.Mat) As cv.Mat
        Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
        If Input.Channels() = 1 Then
            outMat.ConvertTo(outMat, cv.MatType.CV_8U)
            Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function distance3D(p1 As cv.Point3f, p2 As cv.Point3f) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cv.Vec3b, p2 As cv.Vec3b) As Single
        Return Math.Sqrt((CInt(p1.Item0) - CInt(p2.Item0)) * (CInt(p1.Item0) - CInt(p2.Item0)) + (CInt(p1.Item1) - CInt(p2.Item1)) * (CInt(p1.Item1) - CInt(p2.Item1)) +
                         (CInt(p1.Item2) - CInt(p2.Item2)) * (CInt(p1.Item2) - CInt(p2.Item2)))
    End Function
    Public Function distance3D(p1 As cv.Point3i, p2 As cv.Point3i) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cv.Scalar, p2 As cv.Scalar) As Single
        Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                         (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                         (p1(2) - p2(2)) * (p1(2) - p2(2)))
    End Function
    Public Function contourBuild(mask As cv.Mat, approxMode As cv.ContourApproximationModes) As List(Of cv.Point)
        Dim allContours As cv.Point()()
        cv.Cv2.FindContours(mask, allContours, Nothing, cv.RetrievalModes.External, approxMode)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To allContours.Count - 1
            Dim len = CInt(allContours(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        If allContours.Count > 0 Then Return New List(Of cv.Point)(allContours(maxIndex).ToList)
        Return New List(Of cv.Point)
    End Function
    Public Sub setPointCloudGrid()
        task.gOptions.setGridSize(8)
        If task.WorkingRes.Width = 640 Then
            task.gOptions.setGridSize(16)
        ElseIf task.WorkingRes.Width = 1280 Then
            task.gOptions.setGridSize(32)
        End If
    End Sub
    Public Function gMatrixToStr(gMatrix As cv.Mat) As String
        Dim outStr = "Gravity transform matrix" + vbCrLf
        For i = 0 To gMatrix.Rows - 1
            For j = 0 To gMatrix.Cols - 1
                outStr += Format(gMatrix.Get(Of Single)(j, i), fmt3) + vbTab
            Next
            outStr += vbCrLf
        Next

        Return outStr
    End Function
    Public Function randomCellColor() As cv.Vec3b
        Static msRNG As New System.Random
        Return New cv.Vec3b(msRNG.Next(50, 240), msRNG.Next(50, 240), msRNG.Next(50, 240)) ' trying to avoid extreme colors... 
    End Function
    Public Function validContourPoint(rc As rcData, pt As cv.Point, offset As Integer) As cv.Point
        If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Dim count = rc.contour.Count
        For i = offset + 1 To rc.contour.Count - 1
            pt = rc.contour(i Mod count)
            If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Next
        Return New cv.Point
    End Function
    Public Function build3PointEquation(rc As rcData) As cv.Vec4f
        If rc.contour.Count < 3 Then Return New cv.Vec4f
        Dim offset = rc.contour.Count / 3
        Dim p1 = validContourPoint(rc, rc.contour(offset * 0), offset * 0)
        Dim p2 = validContourPoint(rc, rc.contour(offset * 1), offset * 1)
        Dim p3 = validContourPoint(rc, rc.contour(offset * 2), offset * 2)

        Dim v1 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p1.Y, p1.X)
        Dim v2 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p2.Y, p2.X)
        Dim v3 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p3.Y, p3.X)

        Dim cross = crossProduct(v1 - v2, v2 - v3)
        Dim k = -(v1.X * cross.X + v1.Y * cross.Y + v1.Z * cross.Z)
        Return New cv.Vec4f(cross.X, cross.Y, cross.Z, k)
    End Function
    Public Function fitDepthPlane(fitDepth As List(Of cv.Point3f)) As cv.Vec4f
        Dim wDepth = New cv.Mat(fitDepth.Count, 1, cv.MatType.CV_32FC3, fitDepth.ToArray)
        Dim columnSum = wDepth.Sum()
        Dim count = CDbl(fitDepth.Count)
        Dim plane As New cv.Vec4f
        Dim centroid = New cv.Point3f
        If count > 0 Then
            centroid = New cv.Point3f(columnSum(0) / count, columnSum(1) / count, columnSum(2) / count)
            wDepth = wDepth.Subtract(centroid)
            Dim xx As Double, xy As Double, xz As Double, yy As Double, yz As Double, zz As Double
            For i = 0 To wDepth.Rows - 1
                Dim tmp = wDepth.Get(Of cv.Point3f)(i, 0)
                xx += tmp.X * tmp.X
                xy += tmp.X * tmp.Y
                xz += tmp.X * tmp.Z
                yy += tmp.Y * tmp.Y
                yz += tmp.Y * tmp.Z
                zz += tmp.Z * tmp.Z
            Next

            Dim det_x = yy * zz - yz * yz
            Dim det_y = xx * zz - xz * xz
            Dim det_z = xx * yy - xy * xy

            Dim det_max = Math.Max(det_x, det_y)
            det_max = Math.Max(det_max, det_z)

            If det_max = det_x Then
                plane(0) = 1
                plane(1) = (xz * yz - xy * zz) / det_x
                plane(2) = (xy * yz - xz * yy) / det_x
            ElseIf det_max = det_y Then
                plane(0) = (yz * xz - xy * zz) / det_y
                plane(1) = 1
                plane(2) = (xy * xz - yz * xx) / det_y
            Else
                plane(0) = (yz * xy - xz * yy) / det_z
                plane(1) = (xz * xy - yz * xx) / det_z
                plane(2) = 1
            End If
        End If

        Dim magnitude = Math.Sqrt(plane(0) * plane(0) + plane(1) * plane(1) + plane(2) * plane(2))
        Dim normal = New cv.Point3f(plane(0) / magnitude, plane(1) / magnitude, plane(2) / magnitude)
        Return New cv.Vec4f(normal.X, normal.Y, normal.Z, -(normal.X * centroid.X + normal.Y * centroid.Y + normal.Z * centroid.Z))
    End Function
    ' http://james-ramsden.com/calculate-the-cross-product-c-code/
    Public Function crossProduct(v1 As cv.Point3f, v2 As cv.Point3f) As cv.Point3f
        Dim product As New cv.Point3f
        product.X = v1.Y * v2.Z - v1.Z * v2.Y
        product.Y = v1.Z * v2.X - v1.X * v2.Z
        product.Z = v1.X * v2.Y - v1.Y * v2.X

        If (Single.IsNaN(product.X) Or Single.IsNaN(product.Y) Or Single.IsNaN(product.Z)) Then Return New cv.Point3f(0, 0, 0)
        Dim magnitude = Math.Sqrt(product.X * product.X + product.Y * product.Y + product.Z * product.Z)
        If magnitude = 0 Then Return New cv.Point3f(0, 0, 0)
        Return New cv.Point3f(product.X / magnitude, product.Y / magnitude, product.Z / magnitude)
    End Function
    Public Function dotProduct3D(v1 As cv.Point3f, v2 As cv.Point3f) As Single
        Return Math.Abs(v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z)
    End Function
    Public Function getWorldCoordinates(p As cv.Point3f) As cv.Point3f
        Dim x = (p.X - task.calibData.ppx) / task.calibData.fx
        Dim y = (p.Y - task.calibData.ppy) / task.calibData.fy
        Return New cv.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Function getWorldCoordinatesD6(p As cv.Point3f) As cv.Vec6f
        Dim x = CSng((p.X - task.calibData.ppx) / task.calibData.fx)
        Dim y = CSng((p.Y - task.calibData.ppy) / task.calibData.fy)
        Return New cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
    End Function

    Public Function ValidateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > task.WorkingRes.Width * ratio Then r.X = task.WorkingRes.Width * ratio - 1
        If r.Y > task.WorkingRes.Height * ratio Then r.Y = task.WorkingRes.Height * ratio - 1
        If r.X + r.Width > task.WorkingRes.Width * ratio Then r.Width = task.WorkingRes.Width * ratio - r.X
        If r.Y + r.Height > task.WorkingRes.Height * ratio Then r.Height = task.WorkingRes.Height * ratio - r.Y
        If r.Width <= 0 Then r.Width = 1 ' check again (it might have changed.)
        If r.Height <= 0 Then r.Height = 1
        If r.X = task.WorkingRes.Width * ratio Then r.X = r.X - 1
        If r.Y = task.WorkingRes.Height * ratio Then r.Y = r.Y - 1
        Return r
    End Function
    Public Function FindSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.myTrackbars.Count - 1
                        If frm.myLabels(j).text.startswith(opt) Then Return frm.myTrackbars(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("FindSlider failed.  The application list of forms changed while iterating.  Not critical." + ex.Message)
        End Try
        Console.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
    Public Function FindCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBoxes") Then
                        For j = 0 To frm.Box.Count - 1
                            If frm.Box(j).text = opt Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("FindCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Private Function searchForms(opt As String, ByRef index As Integer)
        Dim retryCount As Integer
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Buttons") Then
                        For j = 0 To frm.check.count - 1
                            If frm.check(j).text = opt Then
                                index = j
                                Return frm.check
                            End If
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A Radio button was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function FindRadio(opt As String) As RadioButton
        Dim index As Integer
        Dim radio = searchForms(opt, index)
        If radio Is Nothing Then Return Nothing
        Return radio(index)
    End Function
    Public Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
        For Each rad In radioList
            If rad.Checked Then Return rad.Text
        Next
        Return radioList(0).Text
    End Function
    Public Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As String
        For i = 0 To radioList.Count - 1
            If radioList(i).Checked Then Return i
        Next
        Return 0
    End Function
    Public Function RebuildCells(sortedCells As SortedList(Of Integer, rcData)) As cv.Mat
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For Each rc In sortedCells.Values
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            If rc.index >= 255 Then Exit For
        Next

        Return DisplayCells()
    End Function
    Public Function DisplayCells() As cv.Mat
        Dim dst As New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        task.cellMap.SetTo(0)
        For Each rc In task.redCells
            dst(rc.rect).SetTo(If(task.redOptions.NaturalColor.Checked, rc.naturalColor, rc.color), rc.mask)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
        Next
        Return dst
    End Function
    Public Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
        Dim img As New cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, 0)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim mm As mmData = GetMinMax(hist)
        img.SetTo(0)
        If mm.maxVal > 0 Then
            For i = 0 To binCount - 2
                Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                If h = 0 Then h = 5 ' show the color range in the plot
                cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth, img.Height - h, binWidth, h),
                                 New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
            Next
        End If
        Return img
    End Function
    Public Function GetHist2Dminmax(input As cv.Mat, chan1 As Integer, chan2 As Integer) As cv.Rangef()
        If input.Type = cv.MatType.CV_8UC3 Then
            ' ranges are exclusive in OpenCV 
            Return {New cv.Rangef(-histDelta, 256),
                    New cv.Rangef(-histDelta, 256)}
        End If

        Dim xInput = input.ExtractChannel(chan1)
        Dim yInput = input.ExtractChannel(chan2)

        Dim mmX = GetMinMax(xInput)
        Dim mmY = GetMinMax(yInput)

        ' ranges are exclusive in OpenCV 
        Return {New cv.Rangef(mmX.minVal - histDelta, mmX.maxVal + histDelta),
                New cv.Rangef(mmY.minVal - histDelta, mmY.maxVal + histDelta)}
    End Function
    Public Function GetMaxDist(ByRef rc As rcData) As cv.Point
        Dim mask = rc.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rc.rect.X
        mm.maxLoc.Y += rc.rect.Y

        Return mm.maxLoc
    End Function
    Public Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
    Public Sub SetTrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub SetTrueText(text As String)
        Dim pt = New cv.Point(0, 0)
        Dim picTag = 2
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub SetTrueText(text As String, picTag As Integer)
        If text Is Nothing Then Return
        Dim pt = New cv.Point(0, 0)
        Dim str As New trueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Function standaloneTest() As Boolean
        If standalone Or showIntermediate() Then Return True
        Return False
    End Function
    Public Sub UpdateAdvice(advice As String)
        If task.advice.StartsWith("No advice for ") Then task.advice = ""
        Dim split = advice.Split(":")
        If task.advice.Contains(split(0) + ":") Then Return
        task.advice += advice + vbCrLf + vbCrLf
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar, lineWidth As Integer)
        dst.Line(p1, p2, color, lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
        dst.Line(p1, p2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawFPoly(ByRef dst As cv.Mat, poly As List(Of cv.Point2f), color As cv.Scalar)
        Dim minMod = Math.Min(poly.Count, task.polyCount)
        For i = 0 To minMod - 1
            DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
        Next
    End Sub
    Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, radius As Integer, color As cv.Scalar)
        dst.Circle(pt, radius, color, -1, task.lineType)
    End Sub
    Public Sub drawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
        dst.Circle(pt, task.DotSize + 2, cv.Scalar.White, -1, task.lineType)
        DrawCircle(dst, pt, task.DotSize, cv.Scalar.Black)
    End Sub

    Public Sub drawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
        Dim pts = rotatedRect.Points()
        Dim lastPt = pts(0)
        For i = 1 To pts.Length
            Dim index = i Mod pts.Length
            Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
            DrawLine(dst2, pt, lastPt, task.HighlightColor)
            lastPt = pt
        Next
    End Sub
    Public Function ShowPalette(input As cv.Mat) As cv.Mat
        If input.Type = cv.MatType.CV_32SC1 Then input.ConvertTo(input, cv.MatType.CV_8U)
        task.palette.Run(input)
        Return task.palette.dst2.Clone
    End Function
    Public Function showIntermediate() As Boolean
        If task.intermediateObject Is Nothing Then Return False
        If task.intermediateObject.traceName = traceName Then Return True
        Return False
    End Function
    Public Function InitRandomRect(margin As Integer) As cv.Rect
        Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
    End Function
    Public Function quickRandomPoints(howMany As Integer) As List(Of cv.Point2f)
        Dim srcPoints As New List(Of cv.Point2f)
        Dim w = task.WorkingRes.Width
        Dim h = task.WorkingRes.Height
        For i = 0 To howMany - 1
            Dim pt = New cv.Point2f(msRNG.Next(0, w), msRNG.Next(0, h))
            srcPoints.Add(pt)
        Next
        Return srcPoints
    End Function
    Public Sub Dispose() Implements IDisposable.Dispose
        If allOptions IsNot Nothing Then allOptions.Close()
        If task.pythonTaskName.EndsWith(".py") Then
            Dim proc = Process.GetProcesses()
            For i = 0 To proc.Count - 1
                If proc(i).ProcessName.ToLower.Contains("pythonw") Then Continue For
                If proc(i).ProcessName.ToLower.Contains("python") Then
                    If proc(i).HasExited = False Then proc(i).Kill()
                End If
            Next
        End If
        For Each algorithm In task.activeObjects
            Dim type As Type = algorithm.GetType()
            If type.GetMethod("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
        Next
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        combo.Dispose()
    End Sub
    Public Sub processFrame(src As cv.Mat)
        algorithm.Run(src)
        task.labels = labels

        task.dst0 = dst0
        task.dst1 = dst1
        task.dst2 = dst2
        task.dst3 = dst3
        task.trueData = New List(Of trueText)(trueData)
        trueData.Clear()
    End Sub
    Public Sub measureStartRun(name As String)
        If task.recordTimings = False Then Exit Sub
        Dim nextTime = Now
        If algorithmNames.Contains(name) = False Then
            algorithmNames.Add(name)
            algorithm_ms.Add(0)
            algorithmTimes.Add(nextTime)
        End If

        Dim index = algorithmStack.Peek
        Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
        Dim span = New TimeSpan(elapsedTicks)
        algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

        index = algorithmNames.IndexOf(name)
        algorithmTimes(index) = nextTime
        algorithmStack.Push(index)
    End Sub
    Public Sub measureEndRun(name As String)
        If task.recordTimings = False Then Exit Sub
        Dim nextTime = Now
        Dim index = algorithmStack.Peek
        Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
        Dim span = New TimeSpan(elapsedTicks)
        algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
        algorithmStack.Pop()
        algorithmTimes(algorithmStack.Peek) = nextTime
    End Sub
    Public Sub Run(src As cv.Mat)
        If task.testAllRunning = False Then measureStartRun(traceName)

        task.trueData.Clear()
        If task.paused = False Then algorithm.RunVB(src)
        If task.testAllRunning = False Then measureEndRun(traceName)
    End Sub
    Public Sub DrawContour(ByRef dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -10)
        If lineWidth = -10 Then lineWidth = task.lineWidth ' VB.Net only allows constants for optional parameter.
        If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
        Dim listOfPoints = New List(Of List(Of cv.Point))
        listOfPoints.Add(contour)
        cv.Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType)
    End Sub
    Public Sub drawPoly(result As cv.Mat, polyPoints As List(Of cv.Point), color As cv.Scalar)
        If polyPoints.Count < 3 Then Exit Sub
        Dim listOfPoints = New List(Of List(Of cv.Point))({polyPoints})
        cv.Cv2.DrawContours(result, listOfPoints, 0, color, 2)
    End Sub
    Public Sub detectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
        For Each fface In faces
            src.Rectangle(fface, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Class

