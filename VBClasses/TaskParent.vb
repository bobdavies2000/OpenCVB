Imports cv = OpenCvSharp
Imports System.Drawing.Imaging
Namespace VBClasses
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

            If algTask.callTrace.Count = 0 Then algTask.callTrace.Add(algTask.settings.algorithm + "\")
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
                If lines(i).StartsWith("AlgorithmTask") Then Exit For
                If lines(i).StartsWith("at Microsoft") Then Continue For
                If lines(i).StartsWith("at System") Then Continue For
                If lines(i).StartsWith("at Main") Then Continue For
                callStack = lines(i) + "\" + callStack
            Next

            dst0 = New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)
            dst1 = New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)
            dst2 = New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)
            dst3 = New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)

            standalone = traceName = algTask.settings.algorithm
            algTask.callTrace.Add(callStack)

            algTask.activeObjects.Add(Me)

            If standalone Then
                algTask.algorithm_ms.Clear()
                algTask.algorithmNames.Clear()
                algTask.algorithmNames.Add("waitingForInput")
                algTask.algorithmTimes.Add(Now)
                algTask.algorithm_ms.Add(0)

                algTask.algorithmNames.Add("inputBufferCopy")
                algTask.algorithmTimes.Add(Now)
                algTask.algorithm_ms.Add(0)

                algTask.algorithmNames.Add("ReturnCopyTime")
                algTask.algorithmTimes.Add(Now)
                algTask.algorithm_ms.Add(0)

                algTask.algorithmNames.Add(traceName)
                algTask.algorithmTimes.Add(Now)
                algTask.algorithm_ms.Add(0)

                algTask.algorithmStack = New Stack()
                algTask.algorithmStack.Push(0)
                algTask.algorithmStack.Push(1)
                algTask.algorithmStack.Push(2)
                algTask.algorithmStack.Push(3)
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
            dst.FillConvexPoly(vertices, color, algTask.lineType)
        End Sub
        Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
            Dim spacer = CInt(dst.Height / (lineCount + 1))
            Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
            If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
            If spaceVal > 10 Then spaceVal += spaceVal Mod 10
            For i = 0 To lineCount
                Dim p1 = New cv.Point(0, spacer * i)
                Dim p2 = New cv.Point(dst.Width, spacer * i)
                dst.Line(p1, p2, white, algTask.cvFontThickness)
                Dim nextVal = (maxVal - spaceVal * i)
                Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
                Dim p3 = New cv.Point(0, p1.Y + 12)
                cv.Cv2.PutText(dst, nextText, p3, cv.HersheyFonts.HersheyPlain, algTask.cvFontSize,
                            white, algTask.cvFontThickness, algTask.lineType)
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
        Public Sub setPointCloudGrid()
            algTask.gOptions.GridSlider.Value = 8
            If algTask.workRes.Width = 640 Then
                algTask.gOptions.GridSlider.Value = 16
            ElseIf algTask.workRes.Width = 1280 Then
                algTask.gOptions.GridSlider.Value = 32
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

            Dim v1 = algTask.pointCloud(rc.rect).Get(Of cv.Point3f)(p1.Y, p1.X)
            Dim v2 = algTask.pointCloud(rc.rect).Get(Of cv.Point3f)(p2.Y, p2.X)
            Dim v3 = algTask.pointCloud(rc.rect).Get(Of cv.Point3f)(p3.Y, p3.X)

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
            Dim x = CSng((p.X - algTask.calibData.rgbIntrinsics.ppx) / algTask.calibData.rgbIntrinsics.fx)
            Dim y = CSng((p.Y - algTask.calibData.rgbIntrinsics.ppy) / algTask.calibData.rgbIntrinsics.fy)
            Return New cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
        End Function
        Public Function srcMustBe8U(src As cv.Mat) As cv.Mat
            If src.Type <> cv.MatType.CV_8U Then
                Static color8U As New Color8U_Basics
                color8U.Run(src)
                Return color8U.dst2
            End If
            Return src
        End Function
        Public Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
            Dim img As New cv.Mat(New cv.Size(algTask.workRes.Width, algTask.workRes.Height), cv.MatType.CV_8UC3, cv.Scalar.All(0))
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
            algTask.depthmask(rect).CopyTo(depth, maskInput)
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
            For Each fp In algTask.fpList
                SetTrueText(CStr(fp.age), fp.pt, 2)
            Next
        End Sub
        Public Sub fpDSet()
            If algTask.fpList.Count = 0 Then Exit Sub
            Dim brickIndex = algTask.fpMap.Get(Of Single)(algTask.clickPoint.Y, algTask.clickPoint.X)
            Dim fpIndex = algTask.fpFromGridCell.IndexOf(brickIndex)
            If fpIndex >= 0 Then algTask.fpD = algTask.fpList(fpIndex)
        End Sub
        Public Sub fpDisplayMotion()
            dst1.SetTo(0)
            For Each fp In algTask.fpList
                For Each pt In fp.ptHistory
                    DrawCircle(dst1, pt, algTask.DotSize, algTask.highlight)
                Next
            Next
        End Sub
        Public Sub fpCellContour(fp As fpData, dst As cv.Mat, Optional colorIndex As Integer = 0)
            Dim color = Choose(colorIndex + 1, cv.Scalar.White, cv.Scalar.Black)
            For i = 0 To fp.facets.Count - 1
                Dim p1 = fp.facets(i)
                Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
                dst.Line(p1, p2, color, algTask.lineWidth, algTask.lineType)
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
            If standalone Or algTask.displayObjectName = traceName Then Return True
            Return False
        End Function
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect, color As cv.Scalar)
            dst.Rectangle(rect, color, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawRect(dst As cv.Mat, rect As cv.Rect)
            dst.Rectangle(rect, algTask.highlight, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawFatLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
            dst.Line(lp.p1, lp.p2, algTask.highlight, algTask.lineWidth * 3, algTask.lineType)
        End Sub
        Public Sub DrawFatLine(p1 As cv.Point2f, p2 As cv.Point2f, dst As cv.Mat, color As cv.Scalar)
            dst.Line(p1, p2, algTask.highlight, algTask.lineWidth * 3, algTask.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, radius As Integer, color As cv.Scalar,
                          Optional fillFlag As Integer = -1)
            dst.Circle(pt, radius, color, fillFlag, algTask.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f)
            dst.Circle(pt, algTask.DotSize, algTask.highlight, -1, algTask.lineType)
        End Sub
        Public Sub DrawCircle(dst As cv.Mat, pt As cv.Point2f, color As cv.Scalar)
            dst.Circle(pt, algTask.DotSize, color, -1, algTask.lineType)
        End Sub
        Public Sub DrawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
            dst.Circle(pt, algTask.DotSize + 2, white, -1, algTask.lineType)
            DrawCircle(dst, pt, algTask.DotSize, cv.Scalar.Black)
        End Sub

        Public Sub DrawRotatedOutline(rotatedRect As cv.RotatedRect, dst2 As cv.Mat, color As cv.Scalar)
            Dim pts = rotatedRect.Points()
            Dim lastPt = pts(0)
            For i = 1 To pts.Length
                Dim index = i Mod pts.Length
                Dim pt = New cv.Point(CInt(pts(index).X), CInt(pts(index).Y))
                vbc.DrawLine(dst2, pt, lastPt, algTask.highlight)
                lastPt = pt
            Next
        End Sub
        Public Sub drawFeaturePoints(dst As cv.Mat, ptlist As List(Of cv.Point), color As cv.Scalar)
            DrawTour(dst, ptlist, color, 1)
        End Sub
        Public Function ShowPaletteDepth(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            cv.Cv2.ApplyColorMap(input, output, algTask.depthColorMap)
            output.SetTo(0, algTask.noDepthMask)
            Return output
        End Function
        Public Function ShowPaletteCorrelation(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            cv.Cv2.ApplyColorMap(input, output, algTask.correlationColorMap)
            Return output
        End Function
        Public Function ShowPaletteDepthOriginal(input As cv.Mat) As cv.Mat
            If algTask.palette Is Nothing Then algTask.palette = New Palette_LoadColorMap
            algTask.palette.Run(input)
            Return algTask.palette.dst2
        End Function
        Public Shared Function PaletteFull(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, algTask.colorMap)
            Else
                cv.Cv2.ApplyColorMap(input, output, algTask.colorMap)
            End If

            Return output
        End Function
        Public Shared Function PaletteBlackZero(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            If input.Type <> cv.MatType.CV_8U Then
                Dim input8u As New cv.Mat
                input.ConvertTo(input8u, cv.MatType.CV_8U)
                cv.Cv2.ApplyColorMap(input8u, output, algTask.colorMapZeroIsBlack)
            Else
                cv.Cv2.ApplyColorMap(input, output, algTask.colorMapZeroIsBlack)
            End If

            Return output
        End Function
        Public Shared Function ShowPaletteOriginal(input As cv.Mat) As cv.Mat
            If algTask.paletteRandom Is Nothing Then algTask.paletteRandom = New Palette_RandomColors
            If input.Type <> cv.MatType.CV_8U Then input.ConvertTo(input, cv.MatType.CV_8U)
            Return algTask.paletteRandom.useColorMapWithBlack(input).Clone
        End Function
        Public Function ShowPaletteFullColor(input As cv.Mat) As cv.Mat
            If algTask.paletteRandom Is Nothing Then algTask.paletteRandom = New Palette_RandomColors
            Return algTask.paletteRandom.useColorMapFull(input)
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
            If algTask.redList Is Nothing Then algTask.redList = New RedList_Basics
            algTask.redList.inputRemoved = removeMask
            algTask.redList.Run(src)
            label = algTask.redList.labels(2)
            Return algTask.redList.dst2
        End Function
        Public Function runRedList(src As cv.Mat, ByRef label As String) As cv.Mat
            If algTask.redList Is Nothing Then algTask.redList = New RedList_Basics
            algTask.redList.Run(src)
            label = algTask.redList.labels(2)
            Return algTask.redList.dst2
        End Function
        Public Function runRedCloud(src As cv.Mat, ByRef label As String) As cv.Mat
            If algTask.redCloud Is Nothing Then algTask.redCloud = New RedCloud_Basics
            algTask.redCloud.Run(src)
            label = algTask.redCloud.labels(2)
            Return algTask.redCloud.dst2
        End Function
        Public Function runRedColor(src As cv.Mat, ByRef label As String) As cv.Mat
            If algTask.redColor Is Nothing Then algTask.redColor = New RedColor_Basics
            algTask.redColor.Run(src)
            label = algTask.redColor.labels(2)
            Return algTask.redColor.dst2
        End Function
        Public Function InitRandomRect(margin As Integer) As cv.Rect
            Return New cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin))
        End Function
        Public Function quickRandomPoints(howMany As Integer) As List(Of cv.Point2f)
            Dim srcPoints As New List(Of cv.Point2f)
            Dim w = algTask.workRes.Width
            Dim h = algTask.workRes.Height
            For i = 0 To howMany - 1
                Dim pt = New cv.Point2f(msRNG.Next(0, w), msRNG.Next(0, h))
                srcPoints.Add(pt)
            Next
            Return srcPoints
        End Function
        Public Sub measureStartRun(name As String)
            Dim nextTime = Now
            If algTask.algorithmNames.Contains(name) = False Then
                algTask.algorithmNames.Add(name)
                algTask.algorithm_ms.Add(0)
                algTask.algorithmTimes.Add(nextTime)
            End If

            If algTask.algorithmStack.Count > 0 Then
                Dim index = algTask.algorithmStack.Peek
                Dim elapsedTicks = nextTime.Ticks - algTask.algorithmTimes(index).Ticks
                Dim span = New TimeSpan(elapsedTicks)
                algTask.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond

                index = algTask.algorithmNames.IndexOf(name)
                algTask.algorithmTimes(index) = nextTime
                algTask.algorithmStack.Push(index)
            End If
        End Sub
        Public Sub measureEndRun(name As String)
            Try
                Dim nextTime = Now
                Dim index = algTask.algorithmStack.Peek
                Dim elapsedTicks = nextTime.Ticks - algTask.algorithmTimes(index).Ticks
                Dim span = New TimeSpan(elapsedTicks)
                algTask.algorithm_ms(index) += span.Ticks / TimeSpan.TicksPerMillisecond
                algTask.algorithmStack.Pop()
                algTask.algorithmTimes(algTask.algorithmStack.Peek) = nextTime
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
                dst.Line(pt1, pt2, cv.Scalar.Red, algTask.lineWidth + 1, algTask.lineType, 0)
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
End Namespace