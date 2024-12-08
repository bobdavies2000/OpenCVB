Imports cvb = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Public Class TrueText
    Declare Sub CopyClassToManagedCpp Lib "ManagedCppLibrary.dll" (dataPtr As IntPtr)
    Public text As String
    Public picTag = 2
    Public pt As cvb.Point
    Private Sub setup(_text As String, _pt As cvb.Point, camPicIndex As Integer)
        text = _text
        pt = _pt
        picTag = camPicIndex
    End Sub
    Public Sub New(_text As String, _pt As cvb.Point, camPicIndex As Integer)
        setup(_text, _pt, camPicIndex)
    End Sub
    Public Sub New(_text As String, _pt As cvb.Point)
        setup(_text, _pt, 2)
    End Sub
End Class
Public Class TaskParent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public standalone As Boolean
    Public dst0 As cvb.Mat, dst1 As cvb.Mat, dst2 As cvb.Mat, dst3 As cvb.Mat, empty As cvb.Mat
    Public labels() As String = {"", "", "", ""}
    Public msRNG As New System.Random
    Public VB_Algorithm As Object
    Public traceName As String
    Public desc As String
    Public black As New cvb.Vec3b, white As New cvb.Vec3b(255, 255, 255), grayColor As New cvb.Vec3b(127, 127, 127)
    Public yellow As New cvb.Vec3b(0, 255, 255), purple As New cvb.Vec3b(255, 0, 255)
    Public teal As New cvb.Vec3b(255, 255, 0)
    Public red As New cvb.Vec3b(0, 0, 255), green As New cvb.Vec3b(0, 255, 0), blue As New cvb.Vec3b(255, 0, 0)
    Public zero3f As New cvb.Point3f(0, 0, 0)
    Public newVec4f As New cvb.Vec4f
    Public cPtr As IntPtr
    Public trueData As New List(Of TrueText)
    Public strOut As String
    Dim retryCount As Integer
    Public Const depthListMaxCount As Integer = 10
    Public primaryAlg As Boolean

    Public term As New cvb.TermCriteria(cvb.CriteriaTypes.Eps + cvb.CriteriaTypes.Count, 10, 1.0)

    Dim callStack = ""
    Public Enum FeatureSrc
        GoodFeaturesFull = 0
        GoodFeaturesGrid = 1
        Agast = 2
        BRISK = 3
        Harris = 4
        FAST = 5
    End Enum
    Public Sub New()
        VB_Algorithm = Me
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
                If Not (partLine.StartsWith("TaskParent") Or partLine.StartsWith("VBtask")) Then
                    callStack = partLine + "\" + callStack
                End If
            End If
        Next

        If task.algName.StartsWith("_CPP") Then
            callTrace.Clear()
            algorithm_ms.Clear()
            algorithmNames.Clear()
            callTrace.Add("CPP_Basics\")
        End If

        standalone = callTrace(0) = traceName + "\" ' only the first is standalone (the primary algorithm.)
        If traceName = "Python_Run" Then standalone = True
        If task.algName.EndsWith("_CS") Then callStack = callTrace(0) + callStack
        If standalone = False And callTrace.Contains(callStack) = False Then callTrace.Add(callStack)

        dst0 = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        dst1 = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        dst2 = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
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
    Public Function GetWindowImage(ByVal WindowHandle As IntPtr, ByVal rect As cvb.Rect) As Bitmap
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
    Public Sub DrawRotatedRect(rotatedRect As cvb.RotatedRect, dst As cvb.Mat, color As cvb.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cvb.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cvb.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        dst.FillConvexPoly(vertices, color, task.lineType)
    End Sub
    Public Sub AddPlotScale(dst As cvb.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        Dim spacer = CInt(dst.Height / (lineCount + 1))
        Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cvb.Point(0, spacer * i)
            Dim p2 = New cvb.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, white, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            Dim p3 = New cvb.Point(0, p1.Y + 12)
            cvb.Cv2.PutText(dst, nextText, p3, cvb.HersheyFonts.HersheyPlain, task.cvFontSize,
                            white, task.cvFontThickness, task.lineType)
        Next
    End Sub
    Public Sub DrawFatLine(p1 As cvb.Point2f, p2 As cvb.Point2f, dst As cvb.Mat, fatColor As cvb.Scalar)
        Dim pad = 2
        If task.dst2.Width >= 640 Then pad = 6
        dst.Line(p1, p2, fatColor, task.lineWidth + pad, task.lineType)
        DrawLine(dst, p1, p2, cvb.Scalar.Black)
    End Sub
    Public Function IntersectTest(p1 As cvb.Point2f, p2 As cvb.Point2f, p3 As cvb.Point2f, p4 As cvb.Point2f, rect As cvb.Rect) As cvb.Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cvb.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        ' If pt.X >= rect.Width Or pt.Y >= rect.Height Then Return New cvb.Point2f
        Return pt
    End Function
    Public Function PrepareDepthInput(index As Integer) As cvb.Mat
        If task.useGravityPointcloud Then Return task.pcSplit(index) ' already oriented to gravity

        ' rebuild the pointcloud so it is oriented to gravity.
        Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
        Dim split = pc.Split()
        Return split(index)
    End Function
    Public Function Convert32f_To_8UC3(Input As cvb.Mat) As cvb.Mat
        Dim outMat = Input.Normalize(0, 255, cvb.NormTypes.MinMax)
        If Input.Channels() = 1 Then
            outMat.ConvertTo(outMat, cvb.MatType.CV_8U)
            Return outMat.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cvb.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function distance3D(p1 As cvb.Point3f, p2 As cvb.Point3f) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cvb.Vec3b, p2 As cvb.Vec3b) As Single
        Return Math.Sqrt((CInt(p1(0)) - CInt(p2(0))) * (CInt(p1(0)) - CInt(p2(0))) +
                         (CInt(p1(1)) - CInt(p2(1))) * (CInt(p1(1)) - CInt(p2(1))) +
                         (CInt(p1(2)) - CInt(p2(2))) * (CInt(p1(2)) - CInt(p2(2))))
    End Function
    Public Function distance3D(p1 As cvb.Point3i, p2 As cvb.Point3i) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cvb.Scalar, p2 As cvb.Scalar) As Single
        Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                         (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                         (p1(2) - p2(2)) * (p1(2) - p2(2)))
    End Function
    Public Function ContourBuild(mask As cvb.Mat, approxMode As cvb.ContourApproximationModes) As List(Of cvb.Point)
        Dim allContours As cvb.Point()()
        cvb.Cv2.FindContours(mask, allContours, Nothing, cvb.RetrievalModes.External, approxMode)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To allContours.Count - 1
            Dim len = CInt(allContours(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        If allContours.Count > 0 Then Return New List(Of cvb.Point)(allContours(maxIndex).ToList)
        Return New List(Of cvb.Point)
    End Function
    Public Sub setPointCloudGrid()
        task.gOptions.setGridSize(8)
        If task.dst2.Width = 640 Then
            task.gOptions.setGridSize(16)
        ElseIf task.dst2.Width = 1280 Then
            task.gOptions.setGridSize(32)
        End If
    End Sub
    Public Function gMatrixToStr(gMatrix As cvb.Mat) As String
        Dim outStr = "Gravity transform matrix" + vbCrLf
        For i = 0 To gMatrix.Rows - 1
            For j = 0 To gMatrix.Cols - 1
                outStr += Format(gMatrix.Get(Of Single)(j, i), fmt3) + vbTab
            Next
            outStr += vbCrLf
        Next

        Return outStr
    End Function
    Public Function randomCellColor() As cvb.Scalar
        Static msRNG As New System.Random
        Return New cvb.Scalar(msRNG.Next(50, 240), msRNG.Next(50, 240), msRNG.Next(50, 240)) ' trying to avoid extreme colors... 
    End Function
    Public Function validContourPoint(rc As rcData, pt As cvb.Point, offset As Integer) As cvb.Point
        If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Dim count = rc.contour.Count
        For i = offset + 1 To rc.contour.Count - 1
            pt = rc.contour(i Mod count)
            If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Next
        Return New cvb.Point
    End Function
    Public Function build3PointEquation(rc As rcData) As cvb.Vec4f
        If rc.contour.Count < 3 Then Return New cvb.Vec4f
        Dim offset = rc.contour.Count / 3
        Dim p1 = validContourPoint(rc, rc.contour(offset * 0), offset * 0)
        Dim p2 = validContourPoint(rc, rc.contour(offset * 1), offset * 1)
        Dim p3 = validContourPoint(rc, rc.contour(offset * 2), offset * 2)

        Dim v1 = task.pointCloud(rc.rect).Get(Of cvb.Point3f)(p1.Y, p1.X)
        Dim v2 = task.pointCloud(rc.rect).Get(Of cvb.Point3f)(p2.Y, p2.X)
        Dim v3 = task.pointCloud(rc.rect).Get(Of cvb.Point3f)(p3.Y, p3.X)

        Dim cross = crossProduct(v1 - v2, v2 - v3)
        Dim k = -(v1.X * cross.X + v1.Y * cross.Y + v1.Z * cross.Z)
        Return New cvb.Vec4f(cross.X, cross.Y, cross.Z, k)
    End Function
    Public Function fitDepthPlane(fitDepth As List(Of cvb.Point3f)) As cvb.Vec4f
        Dim wDepth = cvb.Mat.FromPixelData(fitDepth.Count, 1, cvb.MatType.CV_32FC3, fitDepth.ToArray)
        Dim columnSum = wDepth.Sum()
        Dim count = CDbl(fitDepth.Count)
        Dim plane As New cvb.Vec4f
        Dim centroid = New cvb.Point3f
        If count > 0 Then
            centroid = New cvb.Point3f(columnSum(0) / count, columnSum(1) / count, columnSum(2) / count)
            wDepth = wDepth.Subtract(centroid)
            Dim xx As Double, xy As Double, xz As Double, yy As Double, yz As Double, zz As Double
            For i = 0 To wDepth.Rows - 1
                Dim tmp = wDepth.Get(Of cvb.Point3f)(i, 0)
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
        Dim normal = New cvb.Point3f(plane(0) / magnitude, plane(1) / magnitude, plane(2) / magnitude)
        Return New cvb.Vec4f(normal.X, normal.Y, normal.Z, -(normal.X * centroid.X + normal.Y * centroid.Y + normal.Z * centroid.Z))
    End Function
    ' http://james-ramsden.com/calculate-the-cross-product-c-code/
    Public Function crossProduct(v1 As cvb.Point3f, v2 As cvb.Point3f) As cvb.Point3f
        Dim product As New cvb.Point3f
        product.X = v1.Y * v2.Z - v1.Z * v2.Y
        product.Y = v1.Z * v2.X - v1.X * v2.Z
        product.Z = v1.X * v2.Y - v1.Y * v2.X

        If (Single.IsNaN(product.X) Or Single.IsNaN(product.Y) Or Single.IsNaN(product.Z)) Then Return New cvb.Point3f(0, 0, 0)
        Dim magnitude = Math.Sqrt(product.X * product.X + product.Y * product.Y + product.Z * product.Z)
        If magnitude = 0 Then Return New cvb.Point3f(0, 0, 0)
        Return New cvb.Point3f(product.X / magnitude, product.Y / magnitude, product.Z / magnitude)
    End Function
    Public Function dotProduct3D(v1 As cvb.Point3f, v2 As cvb.Point3f) As Single
        Return Math.Abs(v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z)
    End Function
    Public Function getWorldCoordinates(p As cvb.Point3f) As cvb.Point3f
        Dim x = (p.X - task.calibData.ppx) / task.calibData.fx
        Dim y = (p.Y - task.calibData.ppy) / task.calibData.fy
        Return New cvb.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Function getWorldCoordinatesD6(p As cvb.Point3f) As cvb.Vec6f
        Dim x = CSng((p.X - task.calibData.ppx) / task.calibData.fx)
        Dim y = CSng((p.Y - task.calibData.ppy) / task.calibData.fy)
        Return New cvb.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
    End Function

    Public Function ValidateRect(ByVal r As cvb.Rect, Optional ratio As Integer = 1) As cvb.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X + r.Width >= task.dst2.Width * ratio Then r.Width = task.dst2.Width * ratio - r.X
        If r.Y + r.Height >= task.dst2.Height * ratio Then r.Height = task.dst2.Height * ratio - r.Y
        If r.X >= task.dst2.Width * ratio Then r.X = dst2.Width - 1
        If r.Y >= task.dst2.Height * ratio Then r.Y = dst2.Height - 1
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
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
            Debug.WriteLine("FindSlider failed.  The application list of forms changed while iterating.  Not critical." + ex.Message)
        End Try
        Debug.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

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
                Debug.WriteLine("FindCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            retryCount += 1
            If retryCount >= 5 Then
                Debug.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
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
                Debug.WriteLine("findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            retryCount += 1
            If retryCount >= 5 Then
                Debug.WriteLine("A Radio button was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
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
    Public Function RebuildCells(sortedCells As SortedList(Of Integer, rcData)) As cvb.Mat
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For Each rc In sortedCells.Values
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            If rc.index >= 255 Then Exit For
        Next

        Return DisplayCells()
    End Function
    Public Function DisplayCells() As cvb.Mat
        Dim dst As New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        task.redMap.SetTo(0)
        For Each rc In task.redCells
            dst(rc.rect).SetTo(If(task.redOptions.NaturalColor.Checked, rc.naturalColor, rc.color), rc.mask)
            task.redMap(rc.rect).SetTo(rc.index, rc.mask)
        Next
        Return dst
    End Function
    Public Function Show_HSV_Hist(hist As cvb.Mat) As cvb.Mat
        Dim img As New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim mm As mmData = GetMinMax(hist)
        img.SetTo(0)
        If mm.maxVal > 0 Then
            For i = 0 To binCount - 2
                Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                If h = 0 Then h = 5 ' show the color range in the plot
                cvb.Cv2.Rectangle(img, New cvb.Rect(i * binWidth, img.Height - h, binWidth, h),
                                 New cvb.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
            Next
        End If
        Return img
    End Function
    Public Function GetHist2Dminmax(input As cvb.Mat, chan1 As Integer, chan2 As Integer) As cvb.Rangef()
        If input.Type = cvb.MatType.CV_8UC3 Then
            ' ranges are exclusive in OpenCV 
            Return {New cvb.Rangef(-histDelta, 256),
                    New cvb.Rangef(-histDelta, 256)}
        End If

        Dim xInput = input.ExtractChannel(chan1)
        Dim yInput = input.ExtractChannel(chan2)

        Dim mmX = GetMinMax(xInput)
        Dim mmY = GetMinMax(yInput)

        ' ranges are exclusive in OpenCV 
        Return {New cvb.Rangef(mmX.minVal - histDelta, mmX.maxVal + histDelta),
                New cvb.Rangef(mmY.minVal - histDelta, mmY.maxVal + histDelta)}
    End Function
    Public Function GetMaxDist(ByRef rc As rcData) As cvb.Point
        Dim mask = rc.mask.Clone
        mask.Rectangle(New cvb.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cvb.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rc.rect.X
        mm.maxLoc.Y += rc.rect.Y

        Return mm.maxLoc
    End Function
    Public Sub fpDisplayAge()
        dst3 = task.fpOutline
        For Each fp In task.fpList
            SetTrueText(CStr(fp.age), fp.ptCenter, 3)
        Next
    End Sub
    Public Sub fpDisplayCell()
        If task.ClickPoint.X = 0 And task.ClickPoint.Y = 0 Then
            task.ClickPoint = New cvb.Point2f(dst2.Width / 2, dst2.Height / 2)
        End If
        Dim index = task.fpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        task.fpSelected = task.fpList(index)
        SetTrueText(CStr(task.fpSelected.age), task.fpSelected.ptCenter, 0)
        fpCellContour(task.fpSelected, task.color)
    End Sub
    Public Sub fpDisplayMotion()
        dst1.SetTo(0)
        For Each fp In task.fpList
            For Each pt In fp.ptHistory
                DrawCircle(dst1, pt, task.DotSize, task.HighlightColor)
            Next
        Next
    End Sub
    Public Sub fpCellContour(fp As fpData, dst As cvb.Mat, Optional colorIndex As Integer = 0)
        Dim color = Choose(colorIndex + 1, cvb.Scalar.White, cvb.Scalar.Black)
        For i = 0 To fp.facets.Count - 1
            Dim p1 = fp.facets(i)
            Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
            dst.Line(p1, p2, color, task.lineWidth, task.lineType)
        Next
    End Sub
    Public Function buildRect(fp As fpData, mms() As Single) As fpData
        fp.rect = ValidateRect(New cvb.Rect(mms(0), mms(1), mms(2) - mms(0) + 1, mms(3) - mms(1) + 1))

        Static mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
        mask32s(fp.rect).SetTo(0)
        mask32s.FillConvexPoly(fp.facets, white, task.lineType)
        mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)
        'fp.mask.SetTo(0, task.noDepthMask(fp.rect))
        'If fp.mask.CountNonZero = 0 Then fp.mask.SetTo(255)

        Return fp
    End Function
    Public Function findRect(fp As fpData, mms() As Single) As fpData
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

        fp.mask = fp.mask(New cvb.Rect(minX, miny, maxX - minX + 1, maxY - miny + 1))
        fp.rect = New cvb.Rect(fp.rect.X + minX, fp.rect.Y + miny, maxX - minX + 1, maxY - miny + 1)
        Return fp
    End Function
    Public Function fpUpdate(fp As fpData, fpLast As fpData) As fpData
        While 1
            If task.fpIDlist.Contains(fp.ID) Then fp.ID += 0.1 Else Exit While
        End While
        fp.ID = fpLast.ID
        fp.indexLast = fpLast.index
        fp.colorTracking = fpLast.colorTracking
        fp.age = fpLast.age + 1
        fp.ptHistory = New List(Of cvb.Point)(fpLast.ptHistory) From {fp.pt}
        fp.travelDistance = fp.pt.DistanceTo(fp.ptHistory(0))
        If fp.ptHistory.Count > 20 Then fp.ptHistory.RemoveAt(0)
        Return fp
    End Function
    Public Function GetMaxDist(ByRef fp As fpData) As cvb.Point
        Dim mask = fp.mask.Clone
        mask.Rectangle(New cvb.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cvb.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += fp.rect.X
        mm.maxLoc.Y += fp.rect.Y

        Return mm.maxLoc
    End Function
    Public Function GetMinMax(mat As cvb.Mat, Optional mask As cvb.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If

        If Double.IsInfinity(mm.maxVal) Then
            Console.WriteLine("Infinity encountered in " + traceName + " algorithm")
            If mat.Type = cvb.MatType.CV_32F Then
                mm.maxVal = Single.MaxValue
            Else
                mm.maxVal = Double.MaxValue
            End If
        End If

        Return mm
    End Function
    Public Sub SetTrueText(text As String, pt As cvb.Point, Optional picTag As Integer = 2)
        If primaryAlg Then
            Dim str As New TrueText(text, pt, picTag)
            trueData.Add(str)
        End If
    End Sub
    Public Sub SetTrueTextRedC(text As String, Optional picTag As Integer = 1)
        Dim str As New TrueText(text, New cvb.Point(0, 0), picTag)
        trueData.Add(str)
    End Sub
    Public Sub SetTrueText(text As String)
        If primaryAlg Then
            Dim picTag = 2
            Dim str As New TrueText(text, New cvb.Point(0, 0), picTag)
            trueData.Add(str)
        End If
    End Sub
    Public Sub SetTrueText(text As String, picTag As Integer)
        If text Is Nothing Then Return
        If primaryAlg Then
            Dim pt = New cvb.Point(0, 0)
            Dim str As New TrueText(text, pt, picTag)
            trueData.Add(str)
        End If
    End Sub
    Public Function standaloneTest() As Boolean
        If standalone Or ShowIntermediate() Then Return True
        Return False
    End Function
    Public Sub UpdateAdvice(advice As String)
        If task.advice.StartsWith("No advice for ") Then task.advice = ""
        Dim split = advice.Split(":")
        If task.advice.Contains(split(0) + ":") Then Return
        task.advice += advice + vbCrLf + vbCrLf
    End Sub
    Public Sub DrawLine(dst As cvb.Mat, p1 As cvb.Point2f, p2 As cvb.Point2f, color As cvb.Scalar, lineWidth As Integer)
        dst.Line(p1, p2, color, lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cvb.Mat, p1 As cvb.Point2f, p2 As cvb.Point2f, color As cvb.Scalar)
        Dim pt1 = New cvb.Point(p1.X, p1.Y)
        Dim pt2 = New cvb.Point(p2.X, p2.Y)
        dst.Line(pt1, pt2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawFPoly(ByRef dst As cvb.Mat, poly As List(Of cvb.Point2f), color As cvb.Scalar)
        Dim minMod = Math.Min(poly.Count, task.polyCount)
        For i = 0 To minMod - 1
            DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
        Next
    End Sub
    Public Sub DrawCircle(dst As cvb.Mat, pt As cvb.Point2f, radius As Integer, color As cvb.Scalar, Optional fillFlag As Integer = -1)
        dst.Circle(pt, radius, color, fillFlag, task.lineType)
    End Sub
    Public Sub DrawPolkaDot(pt As cvb.Point2f, dst As cvb.Mat)
        dst.Circle(pt, task.DotSize + 2, white, -1, task.lineType)
        DrawCircle(dst, pt, task.DotSize, cvb.Scalar.Black)
    End Sub

    Public Sub DrawRotatedOutline(rotatedRect As cvb.RotatedRect, dst2 As cvb.Mat, color As cvb.Scalar)
        Dim pts = rotatedRect.Points()
        Dim lastPt = pts(0)
        For i = 1 To pts.Length
            Dim index = i Mod pts.Length
            Dim pt = New cvb.Point(CInt(pts(index).X), CInt(pts(index).Y))
            DrawLine(dst2, pt, lastPt, task.HighlightColor)
            lastPt = pt
        Next
    End Sub
    Public Sub drawFeaturePoints(dst As cvb.Mat, ptlist As List(Of cvb.Point), color As cvb.Scalar)
        DrawContour(dst, ptlist, color, 1)
    End Sub
    Public Function ShowPalette(input As cvb.Mat) As cvb.Mat
        If input.Type = cvb.MatType.CV_32S Then
            Dim mm = GetMinMax(input)
            Dim tmp = input.ConvertScaleAbs(255 / (mm.maxVal - mm.minVal), mm.minVal)
            input = tmp
        End If
        task.palette.Run(input)
        Return task.palette.dst2.Clone
    End Function
    Public Function ShowIntermediate() As Boolean
        If task.intermediateObject Is Nothing Then Return False
        If task.intermediateObject.traceName = traceName Then Return True
        Return False
    End Function
    Public Function InitRandomRect(margin As Integer) As cvb.Rect
        Return New cvb.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
    End Function
    Public Function quickRandomPoints(howMany As Integer) As List(Of cvb.Point2f)
        Dim srcPoints As New List(Of cvb.Point2f)
        Dim w = task.dst2.Width
        Dim h = task.dst2.Height
        For i = 0 To howMany - 1
            Dim pt = New cvb.Point2f(msRNG.Next(0, w), msRNG.Next(0, h))
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
    Public Sub processFrame(src As cvb.Mat)
        If dst2.Size <> src.Size And task.frameCount < 10 Then Exit Sub
        task.MainUI_Algorithm.Run(src)
        task.labels = labels

        ' C++/CLR apps have already put their results in task.dst...
        If task.algName.EndsWith("_CPP") = False Then
            task.dst0 = dst0
            task.dst1 = dst1
            task.dst2 = dst2
            task.dst3 = dst3
        End If
        task.trueData = New List(Of TrueText)(trueData)
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
        Try
            If task.recordTimings = False Then Exit Sub
            Dim nextTime = Now
            Dim index = algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
            algorithmStack.Pop()
            algorithmTimes(algorithmStack.Peek) = nextTime
        Catch ex As Exception
        End Try
    End Sub
    Public Sub DrawContour(dst As cvb.Mat, contour As List(Of cvb.Point), color As cvb.Scalar, Optional lineWidth As Integer = -10)
        If lineWidth = -10 Then lineWidth = task.lineWidth ' VB.Net only allows constants for optional parameter.
        If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
        Dim listOfPoints = New List(Of List(Of cvb.Point))
        listOfPoints.Add(contour)
        cvb.Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType)
    End Sub
    Public Sub DrawPoly(result As cvb.Mat, polyPoints As List(Of cvb.Point), color As cvb.Scalar)
        If polyPoints.Count < 3 Then Exit Sub
        Dim listOfPoints = New List(Of List(Of cvb.Point))({polyPoints})
        cvb.Cv2.DrawContours(result, listOfPoints, 0, color, 2)
    End Sub
    Public Sub DetectFace(ByRef src As cvb.Mat, cascade As cvb.CascadeClassifier)
        Dim gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cvb.HaarDetectionTypes.ScaleImage, New cvb.Size(30, 30))
        For Each fface In faces
            src.Rectangle(fface, cvb.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
    Public Sub houghShowLines(dst As cvb.Mat, segments() As cvb.LineSegmentPolar, desiredCount As Integer)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cvb.Point = New cvb.Point(x + 1000 * -b, y + 1000 * a)
            Dim pt2 As cvb.Point = New cvb.Point(x - 1000 * -b, y - 1000 * a)
            dst.Line(pt1, pt2, cvb.Scalar.Red, task.lineWidth + 1, task.lineType, 0)
        Next
    End Sub
    Public Sub Run(src As cvb.Mat)
        If task.testAllRunning = False Then measureStartRun(traceName)

        task.trueData.Clear()
        If task.paused = False Then
            trueData.Clear()
            If VB_Algorithm.traceName.EndsWith("_CPP") Then
                Static nativeTask As New CPP_ManagedTask()
                If nativeTask.ManagedObject Is Nothing Then nativeTask.ManagedObject = VB_Algorithm
                nativeTask.RunAlg(src)
                VB_Algorithm.RunAlg()
                nativeTask.Pause()
            Else
                VB_Algorithm.RunAlg(src)
            End If
        End If
        If task.testAllRunning = False Then measureEndRun(traceName)
    End Sub
End Class