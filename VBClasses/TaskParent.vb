Imports cv = OpenCvSharp
Imports System.Drawing.Imaging
Public Class TrueText
    Declare Sub CopyClassToManagedCpp Lib "ManagedCppLibrary.dll" (dataPtr As IntPtr)
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
Public Class TaskParent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public standalone As Boolean
    Public dst0 As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat
    Public labels() As String = {"", "", "", ""}
    Public traceName As String
    Public desc As String
    Public cPtr As IntPtr
    Public trueData As New List(Of TrueText)
    Public strOut As String
    Public emptyRect As New cv.Rect
    Public Sub New()
        traceName = Me.GetType.Name

        If task.callTrace.Count = 0 Then task.callTrace.Add(task.algName + "\")
        labels = {"", "", traceName, ""}
        Dim stackTrace = Environment.StackTrace
        Dim lines() = stackTrace.Split(vbCrLf)
        Dim callStack As String = ""
        For i = 0 To lines.Count - 1
            If lines(i).Contains("System.Environment") Then Continue For
            If lines(i).Contains("TaskParent") Then Continue For
            lines(i) = Trim(lines(i))
            lines(i) = lines(i).Replace("at VBClasses.", "")
            lines(i) = lines(i).Replace(" at VBClasses.", "")
            lines(i) = lines(i).Substring(0, InStr(lines(i), ".") - 1)
            If lines(i).StartsWith("VBtask") Then Exit For
            If lines(i).StartsWith("at Microsoft") Then Continue For
            If lines(i).StartsWith("at System") Then Continue For
            If lines(i).StartsWith("at Main") Then Continue For
            callStack = lines(i) + "\" + callStack
        Next

        dst0 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
        dst3 = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)

        standalone = traceName = task.algName
        task.callTrace.Add(callStack)

        task.activeObjects.Add(Me)

        If standalone Then
            task.algorithm_ms.Clear()
            task.algorithmNames.Clear()
            task.algorithmNames.Add("waitingForInput")
            task.algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            task.algorithmNames.Add("inputBufferCopy")
            task.algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            task.algorithmNames.Add("ReturnCopyTime")
            task.algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            task.algorithmNames.Add(traceName)
            task.algorithmTimes.Add(Now)
            task.algorithm_ms.Add(0)

            task.algorithmStack = New Stack()
            task.algorithmStack.Push(0)
            task.algorithmStack.Push(1)
            task.algorithmStack.Push(2)
            task.algorithmStack.Push(3)
        End If
    End Sub
    Public Shared Function CaptureScreen() As Bitmap
        Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds
        Dim screenshot As New Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(screenshot)
            g.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy)
        End Using
        Return screenshot
    End Function
    Public Shared Sub SaveScreenshot(screenshot As Bitmap, filePath As String)
        ' Save the bitmap to a file (e.g., PNG)
        screenshot.Save(filePath, ImageFormat.Png)
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
    Public Sub DrawRotatedRect(rotatedRect As cv.RotatedRect, dst As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        dst.FillConvexPoly(vertices, color, task.lineType)
    End Sub
    Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        Dim spacer = CInt(dst.Height / (lineCount + 1))
        Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, white, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            Dim p3 = New cv.Point(0, p1.Y + 12)
            cv.Cv2.PutText(dst, nextText, p3, cv.HersheyFonts.HersheyPlain, task.cvFontSize,
                            white, task.cvFontThickness, task.lineType)
        Next
    End Sub
    Public Function Convert32f_To_8UC3(Input As cv.Mat) As cv.Mat
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
        Return Math.Sqrt((CInt(p1(0)) - CInt(p2(0))) * (CInt(p1(0)) - CInt(p2(0))) +
                         (CInt(p1(1)) - CInt(p2(1))) * (CInt(p1(1)) - CInt(p2(1))) +
                         (CInt(p1(2)) - CInt(p2(2))) * (CInt(p1(2)) - CInt(p2(2))))
    End Function
    Public Function distance3D(p1 As cv.Point3i, p2 As cv.Point3i) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cv.Scalar, p2 As cv.Scalar) As Single
        Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                         (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                         (p1(2) - p2(2)) * (p1(2) - p2(2)))
    End Function
    ' alternative optional parameter: ApproxTC89L1 or ApproxNone
    Public Shared Function ContourBuild(mask As cv.Mat, Optional approxMode As cv.ContourApproximationModes = cv.ContourApproximationModes.ApproxNone) As List(Of cv.Point)
        Dim allContours As cv.Point()() = Nothing
        cv.Cv2.FindContours(mask, allContours, Nothing, cv.RetrievalModes.External, approxMode)

        Dim tourCount As New List(Of Integer)
        For Each tour In allContours
            tourCount.Add(tour.Count)
        Next
        If tourCount.Count > 0 Then
            Return New List(Of cv.Point)(allContours(tourCount.IndexOf(tourCount.Max)).ToList)
        End If
        Return New List(Of cv.Point)
    End Function
    Public Sub setPointCloudGrid()
        task.gOptions.GridSlider.Value = 8
        If task.workRes.Width = 640 Then
            task.gOptions.GridSlider.Value = 16
        ElseIf task.workres.width = 1280 Then
            task.gOptions.GridSlider.Value = 32
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
    Public Function randomCellColor() As cv.Scalar
        Static msRNG As New System.Random
        ' trying to avoid extreme colors... 
        Return New cv.Scalar(msRNG.Next(50, 240), msRNG.Next(50, 240), msRNG.Next(50, 240))
    End Function
    Public Function validContourPoint(rc As oldrcData, pt As cv.Point, offset As Integer) As cv.Point
        If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Dim count = rc.contour.Count
        For i = offset + 1 To rc.contour.Count - 1
            pt = rc.contour(i Mod count)
            If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Next
        Return New cv.Point
    End Function
    Public Function build3PointEquation(rc As oldrcData) As cv.Vec4f
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
        Dim wDepth = cv.Mat.FromPixelData(fitDepth.Count, 1, cv.MatType.CV_32FC3, fitDepth.ToArray)
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
    Public Function worldCoordinatesD6(p As cv.Point3f) As cv.Vec6f
        Dim x = CSng((p.X - task.calibData.rgbIntrinsics.ppx) / task.calibData.rgbIntrinsics.fx)
        Dim y = CSng((p.Y - task.calibData.rgbIntrinsics.ppy) / task.calibData.rgbIntrinsics.fy)
        Return New cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
    End Function
    Public Shared Function ValidateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X + r.Width >= task.workRes.Width * ratio Then r.Width = task.workRes.Width * ratio - r.X - 1
        If r.Y + r.Height >= task.workRes.Height * ratio Then r.Height = task.workRes.Height * ratio - r.Y - 1
        If r.X >= task.workRes.Width * ratio Then r.X = task.workRes.Width - 1
        If r.Y >= task.workRes.Height * ratio Then r.Y = task.workRes.Height - 1
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        Return r
    End Function
    Public Function srcMustBe8U(src As cv.Mat) As cv.Mat
        If src.Type <> cv.MatType.CV_8U Then
            If task.featureOptions.ColorSource.SelectedItem() = "EdgeLine_Basics" Then
                If task.edgeLine Is Nothing Then task.edgeLine = New EdgeLine_Basics
                Return task.edgeLine.dst2 ' already been run with each frame.
            End If
            Static color8U As New Color8U_Basics
            color8U.Run(src)
            Return color8U.dst2
        End If
        Return src
    End Function
    Public Shared Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If

        If Double.IsInfinity(mm.maxVal) Then
            Console.WriteLine("IsInfinity encountered in getMinMax.")
            mm.maxVal = 0 ' skip ...
        End If
        mm.range = mm.maxVal - mm.minVal
        Return mm
    End Function
    Public Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
        Dim img As New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim mm As mmData = GetMinMax(hist)
        img.SetTo(0)
        If mm.maxVal > 0 Then
            For i = 0 To binCount - 2
                Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                If h = 0 Then h = 5 ' show the color range in the plot
                img.Rectangle(New cv.Rect(i * binWidth, img.Height - h, binWidth, h),
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
    Public Function GetMaxDist(ByRef md As maskData) As cv.Point
        Dim mask = md.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += md.rect.X
        mm.maxLoc.Y += md.rect.Y

        Return mm.maxLoc
    End Function
    Public Function GetMaxDist(ByRef maskInput As cv.Mat, rect As cv.Rect) As cv.Point
        Dim mask = maskInput.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rect.X
        mm.maxLoc.Y += rect.Y

        Return mm.maxLoc
    End Function
    Public Shared Function GetMaxDistDepth(ByRef maskInput As cv.Mat, rect As cv.Rect) As cv.Point
        Dim depth As New cv.Mat
        task.depthMask(rect).CopyTo(depth, maskInput)
        depth.Rectangle(New cv.Rect(0, 0, depth.Width, depth.Height), 0, 1)
        Dim distance32f = depth.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rect.X
        mm.maxLoc.Y += rect.Y

        Return mm.maxLoc
    End Function
    Public Function GetMaxDist(ByRef rc As oldrcData) As cv.Point
        Dim mask = rc.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = GetMinMax(distance32f)
        mm.maxLoc.X += rc.rect.X
        mm.maxLoc.Y += rc.rect.Y

        Return mm.maxLoc
    End Function
    Public Sub fpDisplayAge()
        For Each fp In task.fpList
            SetTrueText(CStr(fp.age), fp.pt, 2)
        Next
    End Sub
    Public Sub fpDSet()
        If task.fpList.Count = 0 Then Exit Sub
        Dim brickIndex = task.fpMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        Dim fpIndex = task.fpFromGridCell.IndexOf(brickIndex)
        If fpIndex >= 0 Then task.fpD = task.fpList(fpIndex)
    End Sub
    Public Sub fpDisplayMotion()
        dst1.SetTo(0)
        For Each fp In task.fpList
            For Each pt In fp.ptHistory
                DrawCircle(dst1, pt, task.DotSize, task.highlight)
            Next
        Next
    End Sub
    Public Sub fpCellContour(fp As fpData, dst As cv.Mat, Optional colorIndex As Integer = 0)
        Dim color = Choose(colorIndex + 1, cv.Scalar.White, cv.Scalar.Black)
        For i = 0 To fp.facets.Count - 1
            Dim p1 = fp.facets(i)
            Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
            dst.Line(p1, p2, color, task.lineWidth, task.lineType)
        Next
    End Sub
    Public Sub SetTrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        SetTrueTextBase(text, pt, picTag)
    End Sub
    Public Sub SetTrueText(text As String, Optional picTag As Integer = 2)
        SetTrueTextBase(text, New cv.Point(0, 0), picTag)
    End Sub
    Public Sub SetTrueTextBase(text As String, pt As cv.Point, picTag As Integer)
        If text Is Nothing Then Return
        Dim strnext As New TrueText(text, pt, picTag)
        trueData.Add(strnext)
    End Sub
    Public Function standaloneTest() As Boolean
        If standalone Or task.displayObjectName = traceName Then Return True
        Return False
    End Function
    Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect, color As cv.Scalar)
        dst.Rectangle(rect, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect)
        dst.Rectangle(rect, task.highlight, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar, lineWidth As Integer)
        dst.Line(p1, p2, color, lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
        dst.Line(lp.p1, lp.p2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cv.Mat, lp As lpData)
        dst.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f)
        dst.Line(p1, p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawFatLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
        dst.Line(lp.p1, lp.p2, task.highlight, task.lineWidth * 3, task.lineType)
    End Sub
    Public Sub DrawFatLine(p1 As cv.Point2f, p2 As cv.Point2f, dst As cv.Mat, color As cv.Scalar)
        dst.Line(p1, p2, task.highlight, task.lineWidth * 3, task.lineType)
    End Sub
    Public Shared Sub DrawLine(ByRef dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
        Dim pt1 = New cv.Point(p1.X, p1.Y)
        Dim pt2 = New cv.Point(p2.X, p2.Y)
        dst.Line(pt1, pt2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, radius As Integer, color As cv.Scalar,
                          Optional fillFlag As Integer = -1)
        dst.Circle(pt, radius, color, fillFlag, task.lineType)
    End Sub
    Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f)
        dst.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
    End Sub
    Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, color As cv.Scalar)
        dst.Circle(pt, task.DotSize, color, -1, task.lineType)
    End Sub
    Public Sub DrawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
        dst.Circle(pt, task.DotSize + 2, white, -1, task.lineType)
        DrawCircle(dst, pt, task.DotSize, cv.Scalar.Black)
    End Sub

    Public Sub DrawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
        Dim pts = rotatedRect.Points()
        Dim lastPt = pts(0)
        For i = 1 To pts.Length
            Dim index = i Mod pts.Length
            Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
            DrawLine(dst2, pt, lastPt, task.highlight)
            lastPt = pt
        Next
    End Sub
    Public Sub drawFeaturePoints(dst As cv.Mat, ptlist As List(Of cv.Point), color As cv.Scalar)
        DrawTour(dst, ptlist, color, 1)
    End Sub
    Public Function ShowPaletteDepth(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        cv.Cv2.ApplyColorMap(input, output, task.depthColorMap)
        output.SetTo(0, task.noDepthMask)
        Return output
    End Function
    Public Function ShowPaletteCorrelation(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        cv.Cv2.ApplyColorMap(input, output, task.correlationColorMap)
        Return output
    End Function
    Public Function ShowPaletteDepthOriginal(input As cv.Mat) As cv.Mat
        If task.palette Is Nothing Then task.palette = New Palette_LoadColorMap
        task.palette.Run(input)
        Return task.palette.dst2
    End Function
    Public Shared Function PaletteFull(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        If input.Type <> cv.MatType.CV_8U Then
            Dim input8u As New cv.Mat
            input.ConvertTo(input8u, cv.MatType.CV_8U)
            cv.Cv2.ApplyColorMap(input8u, output, task.colorMap)
        Else
            cv.Cv2.ApplyColorMap(input, output, task.colorMap)
        End If

        Return output
    End Function
    Public Shared Function PaletteBlackZero(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        If input.Type <> cv.MatType.CV_8U Then
            Dim input8u As New cv.Mat
            input.ConvertTo(input8u, cv.MatType.CV_8U)
            cv.Cv2.ApplyColorMap(input8u, output, task.colorMapZeroIsBlack)
        Else
            cv.Cv2.ApplyColorMap(input, output, task.colorMapZeroIsBlack)
        End If

        Return output
    End Function
    Public Shared Function ShowPaletteOriginal(input As cv.Mat) As cv.Mat
        If task.paletteRandom Is Nothing Then task.paletteRandom = New Palette_RandomColors
        If input.Type <> cv.MatType.CV_8U Then input.ConvertTo(input, cv.MatType.CV_8U)
        Return task.paletteRandom.useColorMapWithBlack(input).Clone
    End Function
    Public Function ShowPaletteFullColor(input As cv.Mat) As cv.Mat
        If task.paletteRandom Is Nothing Then task.paletteRandom = New Palette_RandomColors
        Return task.paletteRandom.useColorMapFull(input)
    End Function
    Public Function ShowAddweighted(src1 As cv.Mat, src2 As cv.Mat, ByRef label As String) As cv.Mat
        Static addw As New AddWeighted_Basics

        addw.src2 = src2
        addw.Run(src1)
        Dim wt = addw.options.addWeighted
        label = "AddWeighted: src1 = " + Format(wt, "0%") + " vs. src2 = " + Format(1 - wt, "0%")
        Return addw.dst2
    End Function
    Public Function runRedList(src As cv.Mat, ByRef label As String, removeMask As cv.Mat) As cv.Mat
        If task.redList Is Nothing Then task.redList = New RedList_Basics
        task.redList.inputRemoved = removeMask
        task.redList.Run(src)
        label = task.redList.labels(2)
        Return task.redList.dst2
    End Function
    Public Function runRedList(src As cv.Mat, ByRef label As String) As cv.Mat
        If task.redList Is Nothing Then task.redList = New RedList_Basics
        task.redList.Run(src)
        label = task.redList.labels(2)
        Return task.redList.dst2
    End Function
    Public Function runRedCloud(src As cv.Mat, ByRef label As String) As cv.Mat
        If task.redCloud Is Nothing Then task.redCloud = New RedCloud_Basics
        task.redCloud.Run(src)
        label = task.redCloud.labels(2)
        Return task.redCloud.dst2
    End Function
    Public Function runRedColor(src As cv.Mat, ByRef label As String) As cv.Mat
        If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
        task.redColor.Run(src)
        label = task.redColor.labels(2)
        Return task.redColor.dst2
    End Function
    Public Function InitRandomRect(margin As Integer) As cv.Rect
        Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
    End Function
    Public Function quickRandomPoints(howMany As Integer) As List(Of cv.Point2f)
        Dim srcPoints As New List(Of cv.Point2f)
        Dim w = task.workRes.Width
        Dim h = task.workRes.Height
        For i = 0 To howMany - 1
            Dim pt = New cv.Point2f(msRNG.Next(0, w), msRNG.Next(0, h))
            srcPoints.Add(pt)
        Next
        Return srcPoints
    End Function
    Public Sub measureStartRun(name As String)
        Dim nextTime = Now
        If task.algorithmNames.Contains(name) = False Then
            task.algorithmNames.Add(name)
            task.algorithm_ms.Add(0)
            task.algorithmTimes.Add(nextTime)
        End If

        If task.algorithmStack.Count > 0 Then
            Dim index = task.algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - task.algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

            index = task.algorithmNames.IndexOf(name)
            task.algorithmTimes(index) = nextTime
            task.algorithmStack.Push(index)
        End If
    End Sub
    Public Sub measureEndRun(name As String)
        Try
            Dim nextTime = Now
            Dim index = task.algorithmStack.Peek
            Dim elapsedTicks = nextTime.Ticks - task.algorithmTimes(index).Ticks
            Dim span = New TimeSpan(elapsedTicks)
            task.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
            task.algorithmStack.Pop()
            task.algorithmTimes(task.algorithmStack.Peek) = nextTime
        Catch ex As Exception
        End Try
    End Sub
    Public Shared Sub DrawTour(dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -1,
                        Optional lineType As cv.LineTypes = cv.LineTypes.Link8)
        If contour Is Nothing Then Exit Sub
        If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
        Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
        cv.Cv2.DrawContours(dst, listOfPoints, 0, color, lineWidth, lineType)
    End Sub
    Public Sub DrawPoly(result As cv.Mat, polyPoints As List(Of cv.Point), color As cv.Scalar)
        If polyPoints.Count < 3 Then Exit Sub
        Dim listOfPoints = New List(Of List(Of cv.Point))({polyPoints})
        cv.Cv2.DrawContours(result, listOfPoints, 0, color, 2)
    End Sub
    Public Sub DetectFace(ByRef src As cv.Mat, cascade As cv.CascadeClassifier)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim faces() = cascade.DetectMultiScale(gray, 1.08, 3, cv.HaarDetectionTypes.ScaleImage, New cv.Size(30, 30))
        For Each fface In faces
            DrawRect(src, fface, cv.Scalar.Red)
        Next
    End Sub
    Public Sub houghShowLines(dst As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As Integer)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cv.Point = New cv.Point(x + 1000 * -b, y + 1000 * a)
            Dim pt2 As cv.Point = New cv.Point(x - 1000 * -b, y - 1000 * a)
            dst.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 1, task.lineType, 0)
        Next
    End Sub
    Public Sub Run(src As cv.Mat)
        measureStartRun(traceName)

        trueData.Clear()
        RunAlg(src)

        measureEndRun(traceName)
    End Sub
    Public Overridable Sub RunAlg(src As cv.Mat)
        ' every algorithm overrides this Sub 
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class