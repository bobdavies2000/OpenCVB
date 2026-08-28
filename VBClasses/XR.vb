Imports System.IO : Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    ' This class is a collection of algorithms that just don't justify having their own class.vb.
    Public Class XR_ImageOffset_Basics : Inherits TaskParent
        Public options As New Options_ImageOffset
        Dim options1 As New Options_Diff
        Public masks(2) As Mat
        Public dst(2) As Mat
        Public pcFiltered(2) As Mat
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst1 = New Mat(dst1.Size, MatType.CV_32FC1, New Scalar(0))
            dst2 = New Mat(dst2.Size, MatType.CV_32FC1, New Scalar(0))
            dst3 = New Mat(dst3.Size, MatType.CV_32FC1, New Scalar(0))
            desc = "Compute various differences between neighboring pixels"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            options1.Run()

            Dim r1 = New cv.Rect(1, 1, task.cols - 2, task.rows - 2)
            Dim r2 As cv.Rect
            Select Case options.offsetDirection
                Case "Upper Left"
                    r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
                Case "Above"
                    r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
                Case "Upper Right"
                    r2 = New cv.Rect(2, 0, r1.Width, r1.Height)
                Case "Left"
                    r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
                Case "Right"
                    r2 = New cv.Rect(2, 1, r1.Width, r1.Height)
                Case "Lower Left"
                    r2 = New cv.Rect(0, 2, r1.Width, r1.Height)
                Case "Below"
                    r2 = New cv.Rect(1, 2, r1.Width, r1.Height)
                Case "Below Right"
                    r2 = New cv.Rect(2, 2, r1.Width, r1.Height)
            End Select

            Dim r3 = New cv.Rect(1, 1, r1.Width, r1.Height)

            Absdiff(task.pcSplit(0)(r1), task.pcSplit(0)(r2), dst1(r3))
            Absdiff(task.pcSplit(1)(r1), task.pcSplit(1)(r2), dst2(r3))
            Absdiff(task.pcSplit(2)(r1), task.pcSplit(2)(r2), dst3(r3))

            dst = {dst1, dst2, dst3}
            For i = 0 To dst.Length - 1
                If masks(i) Is Nothing Then masks(i) = New Mat
                Threshold(dst(i), masks(i), options1.pixelDiffThreshold, 255, ThresholdTypes.BinaryInv)
                ConvertScaleAbs(masks(i), masks(i))
                pcFiltered(i) = New Mat(src.Size, MatType.CV_32FC1, New Scalar(0))
                task.pcSplit(i).CopyTo(pcFiltered(i), masks(i))
            Next
        End Sub
    End Class






    Public Class XR_ImageOffset_SliceH : Inherits TaskParent
        Dim iOff As New XR_ImageOffset_Basics
        Dim plot As New PlotOpenCV_Points
        Dim options As New Options_SLR
        Dim slr As New SLR
        Dim mats As New Mat_4to1
        Public Sub New()
            labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
            desc = "Visualize a slice through the ImageOffsets_Basics images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            iOff.Run(src)

            Dim pt = task.mouseMovePoint
            If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
                pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
            End If

            Dim slice As Mat
            For i = 0 To 2
                slice = iOff.pcFiltered(i).Row(pt.Y)
                Dim inputX As New List(Of Double)
                Dim inputY As New List(Of Double)
                For j = 0 To dst2.Width - 1
                    inputX.Add(j)
                    inputY.Add(slice.Get(Of Single)(0, j))
                Next

                Dim outputX As New List(Of Double)
                Dim outputY As New List(Of Double)
                slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                            outputX, outputY)

                plot.input.Clear()
                For j = 0 To outputX.Count - 1
                    plot.input.Add(New Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
                Next

                plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
                plot.maxY = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
                plot.Run(src)

                mats.mat(i) = plot.dst2.Clone
            Next

            mats.Run(emptyMat)
            dst2 = mats.dst2

            Dim p1 = New cv.Point(0, pt.Y), p2 = New cv.Point(dst2.Width, pt.Y)
            Line(task.color, p1, p2, task.highlight, task.lineWidth)
            Line(task.depthRGB, p1, p2, task.highlight, task.lineWidth)
        End Sub
    End Class







    Public Class XR_ImageOffset_SliceV : Inherits TaskParent
        Dim iOff As New XR_ImageOffset_Basics
        Dim plot As New PlotOpenCV_Points
        Dim options As New Options_SLR
        Dim slr As New SLR
        Dim mats As New Mat_4to1
        Public Sub New()
            labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
            desc = "Visualize a slice through the ImageOffsets_Basics images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            iOff.Run(src)

            Dim pt = task.mouseMovePoint
            If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
                pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
            End If

            Dim slice As Mat
            For i = 0 To 2
                slice = iOff.pcFiltered(i).Col(pt.X)
                Dim inputX As New List(Of Double)
                Dim inputY As New List(Of Double)
                For j = 0 To dst2.Height - 1
                    inputX.Add(CDbl(j))
                    inputY.Add(CDbl(slice.Get(Of Single)(j, 0)))
                Next

                Dim outputX As New List(Of Double)
                Dim outputY As New List(Of Double)
                slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                            outputX, outputY)

                plot.input.Clear()
                For j = 0 To outputX.Count - 1
                    plot.input.Add(New Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
                Next

                plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
                plot.maxY = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
                plot.Run(src)
                mats.mat(i) = plot.dst2.Clone
            Next

            mats.Run(emptyMat)
            dst2 = mats.dst2

            Dim p1 = New cv.Point(pt.X, 0), p2 = New cv.Point(pt.X, dst2.Height)
            Line(task.color, p1, p2, task.highlight, task.lineWidth)
            Line(task.depthRGB, p1, p2, task.highlight, task.lineWidth)
        End Sub
    End Class





    Public Class XR_ImageOffset_Cloud : Inherits TaskParent
        Public Sub New()
            desc = "Create a pointcloud with the results of the imageOffset slices"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
        End Sub
    End Class




    ' https://www.kaggle.com/datasets/balraj98/berkeley-segmentation-dataset-500-bsds500
    Public Class XR_Image_Basics : Inherits TaskParent
        Public inputFileName As String
        Public options As New Options_Images
        Public Sub New()
            desc = "Load an image into OpenCVB"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            src = options.fullsizeImage

            If src.Width <> dst2.Width Or src.Height <> dst2.Height Then
                Dim newSize = New Size(dst2.Height * src.Width / src.Height, dst2.Height)
                If newSize.Width > dst2.Width Then
                    newSize = New Size(dst2.Width, dst2.Width * src.Height / src.Width)
                End If
                dst2.SetTo(0)
                Resize(src, dst2(New cv.Rect(0, 0, newSize.Width, newSize.Height)), newSize)
            Else
                dst2 = src
            End If
        End Sub
    End Class










    Public Class XR_Image_Series : Inherits TaskParent
        Public images As New XR_Image_Basics
        Public Sub New()
            images.options.imageSeries = True
            desc = "Display a new image from the directory every heartbeat"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ' to work on a specific file, specify it here.
            ' options.fileInputName = new fileinfo(task.homeDir + "Images/train/103041.jpg")
            images.Run(images.options.fullsizeImage)
            dst2 = images.dst2
        End Sub
    End Class










    Public Class XR_Image_RedC : Inherits TaskParent
        Public images As New XR_Image_Series
        Dim redC As New RedC_Basics
        Dim reduction As New Reduction_Basics
        Public Sub New()
            task.fOptions.ReductionColor.Value = 50
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use RedCloud on a photo instead of the video stream."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            images.Run(src)
            dst0 = images.dst2.Clone
            CvtColor(images.dst2, dst1, ColorConversionCodes.BGR2GRAY)

            reduction.Run(dst1)

            redC.Run(reduction.dst2)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            InRange(dst1, 0, 0, dst0)
            dst2.SetTo(0, dst0)
        End Sub
    End Class








    Public Class XR_Image_MSER : Inherits TaskParent
        Public images As New XR_Image_Series
        Dim core As New MSER_Detect
        Dim options As New Options_Images
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            OptionParent.FindSlider("MSER Min Area").Value = 15
            OptionParent.FindSlider("MSER Max Area").Value = 200000
            desc = "Find the MSER (Maximally Stable Extermal Regions) in the still image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            images.Run(options.fullsizeImage)
            dst1 = images.dst2
            core.Run(dst1)
            dst2 = core.dst2
        End Sub
    End Class








    Public Class XR_Image_Icon : Inherits TaskParent
        Dim inputImage As Bitmap
        Public Sub New()
            Dim filePath As String = task.homeDir + "/MainUI/Data/Magnify.png"
            inputImage = New Bitmap(filePath)
            desc = "Create an icon from an image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If inputImage Is Nothing Then Exit Sub
            Dim iconHandle As IntPtr = inputImage.GetHicon()
            Dim icon As Icon = Icon.FromHandle(iconHandle)

            ' Save the icon to a file
            Using fs As New FileStream(task.homeDir + "/MainUI/Data/test.ico", FileMode.OpenOrCreate)
                icon.Save(fs)
            End Using
            inputImage = Nothing
        End Sub
    End Class




    Public Class XR_Fuzzy_Basics : Inherits TaskParent
        Implements IDisposable
        Dim reduction As New Reduction_Basics
        Dim options As New Options_Contours
        Public contours As cv.Point()()
        Public sortContours As New SortedList(Of Integer, Vec2i)(New compareAllowIdenticalIntegerInverted)
        Public Sub New()
            Dim floodRadio = OptionParent.findRadio("FloodFill")
            If floodRadio.Enabled Then floodRadio.Enabled = False ' too much special handling - cv_32SC1 image 
            If standalone Then task.gOptions.showMyDst1.Checked = True
            cPtr = Fuzzy_Open()
            OptionParent.findRadio("CComp").Checked = True
            labels = {"", "Solid regions", "8-Bit output of Fuzzy_Basics", "Fuzzy edges"}
            desc = "That which is not solid is fuzzy"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            reduction.Run(src)
            dst0 = reduction.dst2
            If dst0.Channels() <> 1 Then CvtColor(dst0, dst0, ColorConversionCodes.BGR2GRAY)

            Dim dataSrc(dst0.Total) As Byte
            dst0.GetArray(Of Byte)(dataSrc)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = Fuzzy_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst0.Rows, dst0.Cols)
            handleSrc.Free()

            dst2 = Mat.FromPixelData(dst0.Rows, dst0.Cols, MatType.CV_8UC1, imagePtr).Clone
            Threshold(dst2, dst3, 0, 255, ThresholdTypes.BinaryInv)

            Dim tmp As New Mat
            If options.retrievalMode = RetrievalModes.CComp Or options.retrievalMode = RetrievalModes.FloodFill Then
                dst3.ConvertTo(tmp, MatType.CV_32S)
            Else
                dst3.ConvertTo(tmp, MatType.CV_8U)
            End If
            contours = FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)

            sortContours.Clear()
            For i = 0 To contours.Length - 1
                ' get this region's ID
                Dim maskID As Integer = 0
                Dim pt = contours(i)(0)
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                            Dim val = dst2.Get(Of Byte)(y, x)
                            If val <> 0 Then
                                maskID = val
                                Exit For
                            End If
                        End If
                    Next
                    If maskID <> 0 Then Exit For
                Next
                sortContours.Add(contours(i).Length, New cv.Point(i, maskID))
            Next

            dst1 = Palettize(dst2 + 1, 0)
            dst1.SetTo(0, dst3)
            labels(1) = "There were " + CStr(sortContours.Count) + " contour > 100 points."
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = Fuzzy_Close(cPtr)
        End Sub
    End Class






    Public Class XR_Fuzzy_Filter : Inherits TaskParent
        Dim kernel As Mat
        Dim reduction As New Reduction_Basics
        Public contours As cv.Point()()
        Public sortContours As New SortedList(Of Integer, Vec2i)(New compareAllowIdenticalIntegerInverted)
        Dim options As New Options_Contours
        Public Sub New()
            Dim array() As Single = {1, 1, 1, 1, 1, 1, 1, 1, 1}
            kernel = Mat.FromPixelData(3, 3, MatType.CV_32F, array)
            kernel *= 1 / 9
            desc = "Use a 2D filter to find smooth areas"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels() <> 1 Then src = task.gray
            reduction.Run(src)

            Dim src32f As New Mat
            reduction.dst2.ConvertTo(src32f, MatType.CV_32F)
            Filter2D(src32f, dst2, -1, kernel)
            dst3 = dst2.Subtract(src32f)
            Threshold(dst3, dst3, 0, 255, ThresholdTypes.BinaryInv)
            dst3.ConvertTo(dst3, MatType.CV_8U)
            Threshold(dst3, dst3, 0, 255, ThresholdTypes.BinaryInv)

            If options.retrievalMode = RetrievalModes.FloodFill Then
                Dim tmp As New Mat
                dst3.ConvertTo(tmp, MatType.CV_32S)
                contours = FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)
            Else
                contours = FindContoursAsArray(dst3, options.retrievalMode, options.ApproximationMode)
            End If

            sortContours.Clear()
            For i = 0 To contours.Length - 1
                Dim maskID As Integer = 0
                Dim pt = contours(i)(0)
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                            Dim val = reduction.dst2.Get(Of Byte)(y, x)
                            If val <> 0 Then
                                maskID = val
                                Exit For
                            End If
                        End If
                    Next
                    If maskID <> 0 Then Exit For
                Next
                sortContours.Add(contours(i).Length, New cv.Point(i, maskID))
            Next

            dst2 = Palettize(reduction.dst2)
            dst2.SetTo(0, dst3)
        End Sub
    End Class








    Public Class XR_Fuzzy_ContoursDepth : Inherits TaskParent
        Public fuzzyD As New XR_Fuzzy_Basics
        Public Sub New()
            desc = "Use contours to outline solids in the depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fuzzyD.Run(task.depthRGB)
            dst2 = fuzzyD.dst1
        End Sub
    End Class








    Public Class XR_Fuzzy_NeighborProof : Inherits TaskParent
        Dim fuzzy As New XR_Fuzzy_Basics
        Dim proofFailed As Boolean = False
        Public Sub New()
            desc = "Prove that every contour cv.Point has at one and only one neighbor with the mask ID and that the rest are zero"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If proofFailed Then Exit Sub
            fuzzy.Run(src)
            dst2 = fuzzy.dst1
            For i = 0 To fuzzy.contours.Length - 1
                Dim len = fuzzy.contours(i).Length
                For j = 0 To len - 1
                    Dim pt = fuzzy.contours(i)(j)
                    Dim maskID As Integer = 0
                    For y = Math.Max(0, pt.Y - 1) To pt.Y + 1
                        For x = Math.Max(0, pt.X - 1) To pt.X + 1
                            If x < src.Width And y < src.Height Then
                                Dim val = dst2.Get(Of Byte)(y, x)
                                If val <> 0 Then maskID = val
                                If maskID <> 0 And val <> 0 And maskID <> val Then
                                    MessageBox.Show("Proof has failed!  There is more than one mask ID identified by this contour cv.Point.")
                                    proofFailed = True
                                    Exit Sub
                                End If
                            End If
                        Next
                    Next
                Next
            Next
            SetTrueText("Results are valid." + vbCrLf + "Mask ID's for all contour points in each region identified only one region.", New cv.Point(10, 50), 3)
        End Sub
    End Class








    Public Class XR_Fuzzy_TrackerDepthClick : Inherits TaskParent
        Public tracker As New XR_Fuzzy_TrackerDepth
        Public highlightPoint As cv.Point
        Public highlightRect As cv.Rect
        Public highlightRegion = -1
        Public Sub New()
            desc = "Create centroids and rect's for solid regions and track them - tracker"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tracker.Run(src)
            dst2 = tracker.dst2

            If highlightRegion < 0 Then SetTrueText("Click any color region to get more details and track it", New cv.Point(10, 50), 3)

            dst3 = tracker.fuzzy.dst1
            If task.mouseClickFlag Then
                highlightPoint = task.clickPoint
                highlightRegion = tracker.fuzzy.dst2.Get(Of Byte)(highlightPoint.Y, highlightPoint.X)
            End If
            If highlightRegion >= 0 Then
                Dim tmp As New Mat
                Threshold(tracker.fuzzy.dst2, tmp, 0, 255, cv.ThresholdTypes.Binary)
                'InRange(tracker.fuzzy.dst2, highlightRegion, highlightRegion + 1, dst1)
                'dst3.SetTo(Scalar.Yellow, dst1)
            End If
            labels(2) = CStr(tracker.fuzzy.sortContours.Count) + " regions were found in the image."
        End Sub
    End Class








    Public Class XR_Fuzzy_TrackerDepth : Inherits TaskParent
        Public fuzzy As New XR_Fuzzy_Basics
        Public centroids As New List(Of cv.Point)
        Public rects As New List(Of cv.Rect)
        Public layoutColor As New List(Of Integer)
        Public highlightPoint As cv.Point
        Public highlightRect As cv.Rect
        Public highlightRegion = -1
        Dim options As New Options_TrackerDepth
        Public Sub New()
            desc = "Create centroids and rect's for solid regions and track them - tracker"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            fuzzy.Run(task.depthRGB)
            dst2 = fuzzy.dst1

            centroids.Clear()
            rects.Clear()
            layoutColor.Clear()
            Dim minX As Double, maxX As Double
            Dim minY As Double, maxY As Double
            For Each vec In fuzzy.sortContours.Values
                Dim contours = fuzzy.contours(vec(0))
                Dim points = Mat.FromPixelData(contours.Length, 1, MatType.CV_32SC2, contours.ToArray)
                Dim center = Sum(points)
                points = Mat.FromPixelData(contours.Length, 2, MatType.CV_32S, contours.ToArray)
                MinMaxIdx(points.Col(0), minX, maxX)
                MinMaxIdx(points.Col(1), minY, maxY)

                Dim rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
                If rect.Width * rect.Height > options.minRectSize Then
                    Dim centroid = New Point2f(center(0) / contours.Length, center(1) / contours.Length)
                    centroids.Add(centroid)
                    rects.Add(rect)
                    layoutColor.Add(vec(1))
                    If options.displayRect Then
                        Circle(dst2, centroid, task.DotSize + 3, Scalar.Yellow, -1, task.lineType)
                        Circle(dst2, centroid, task.DotSize, Scalar.red, -1, task.lineType)
                        Rectangle(dst2, rect, Scalar.Yellow, 2)
                    End If
                End If
            Next

            labels(2) = CStr(fuzzy.sortContours.Count) + " regions were found in the image."
        End Sub
    End Class





    Public Class XR_Gravity_Basics_TAOld : Inherits TaskParent
        Public points As New List(Of Point2f)
        Public autoDisplay As Boolean
        Public Sub New()
            dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Find all the points where depth X-component transitions from positive to negative"
        End Sub
        Public Sub displayResults(p1 As cv.Point, p2 As cv.Point)
            If task.heartBeat Then
                If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
            End If

            dst2.SetTo(0)
            dst3.SetTo(0)
            For Each pt In points
                Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
            Next

            Line(dst2, task.lpGravity.p1, task.lpGravity.p2, white, task.lineWidth, task.lineType)
            Line(dst3, task.lpGravity.p1, task.lpGravity.p2, white, task.lineWidth, task.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

            Abs(dst0)
            Threshold(dst0, dst1, 0, 255, ThresholdTypes.Binary)
            ConvertScaleAbs(dst1, dst1)
            dst0.SetTo(task.MaxZmeters, Not dst1)

            points.Clear()
            For i = dst0.Height / 3 To dst0.Height * 2 / 3 - 1
                Dim mm1 = GetMinMax(dst0.Row(i))
                If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                    dst0.Row(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                    Dim mm2 = GetMinMax(dst0.Row(i))
                    If mm2.minVal > 0 And Math.Abs(mm1.minLoc.X - mm2.minLoc.X) <= 1 Then points.Add(New cv.Point(mm1.minLoc.X, i))
                End If
            Next

            labels(2) = CStr(points.Count) + " points found. "
            Dim p1 As Point2f
            Dim p2 As Point2f
            If points.Count >= 2 Then
                p1 = New Point2f(points(points.Count - 1).X, points(points.Count - 1).Y)
                p2 = New Point2f(points(0).X, points(0).Y)
            End If

            Dim distance = p1.DistanceTo(p2)
            If distance < 10 Then ' enough to get a line with some credibility
                strOut = "Gravity vector not found " + vbCrLf + "The distance of p1 to p2 is " +
                         CStr(CInt(distance)) + " pixels." + vbCrLf
                strOut += "Using the previous value for the gravity vector."
            Else
                Dim lp = New lpData(p1, p2)
                task.lpGravity = New lpData(lp.ptE1, lp.ptE2)
                If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
            End If

            task.lpHorizon = Line_Perpendicular.computePerp(task.lpGravity)
            SetTrueText(strOut, 3)
        End Sub
    End Class
End Namespace
