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
        Dim redC As New RedC_BasicsOld
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





    Public Class XR_RedColor_Basics : Inherits TaskParent
        Dim color8u As New Color8U_Basics
        Public rcList As New List(Of rcDataOld)
        Public rcIndexMap As New Mat
        Public runSelectCell As Boolean = True
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "FloodFill each color8U output and create an rclist"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then color8u.Run(task.gray) Else color8u.Run(src)
            src = color8u.dst2 + 1

            rcIndexMap = src.Clone
            rcList.Clear()
            Dim rect As cv.Rect
            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            For Each r In task.gridRects
                If mask(r).Get(Of Byte)(0, 0) = 0 Then
                    Dim mapID As Integer = rcIndexMap(r).Get(Of Byte)(0, 0)
                    Dim flags = FloodFillFlags.FixedRange Or FloodFillFlags.MaskOnly Or (255 << 8)
                    Dim count = FloodFill(rcIndexMap, mask, r.TopLeft, mapID, rect, 0, 0, flags)
                    If count > 0 Then rcList.Add(New rcDataOld(rcIndexMap(rect), rect, mapID))
                End If
            Next
            dst2 = Palettize(rcIndexMap)

            If task.rcDold IsNot Nothing And standaloneTest() Then Rectangle(dst2, task.rcDold.rect, task.highlight, task.lineWidth)

            Dim rcIndex As Integer
            For Each rc In rcList
                rc.index = rcIndex
                rcIndex += 1
            Next

            'strOut = Utility_Basics.selectCell(rcIndexMap, rcList)
            'SetTrueText(strOut, 3)

            labels(2) = CStr(rcIndex) + " cells were found."
        End Sub
    End Class




    Public Class XR_RedColor_CPP : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcDataOld)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            cPtr = RedCloudLined_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloudLined_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst0 = Mat.FromPixelData(dst1.Rows, dst1.Cols, MatType.CV_8U, imagePtr).Clone

            classCount = RedCloudLined_Count(cPtr)

            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = Mat.FromPixelData(classCount, 1, MatType.CV_32SC4, RedCloudLined_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rcListLast = New List(Of rcDataOld)(rcList)
            Dim rcMapLast As Mat = rcIndexMap.Clone

            Dim minPixels As Integer = dst2.Total * 0.001
            Dim index As Integer = 1
            Dim newList As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To rects.Length - 1
                Dim rc = New rcDataOld(dst0(rects(i)), rects(i), index)
                If rc.pixels < minPixels Then Continue For
                newList.Add(rc.pixels, rc)
                index += 1
            Next

            Dim r2 As cv.Rect
            Dim count As Integer
            rcList.Clear()
            Dim usedColor As New List(Of Integer)
            For Each rc In newList.Values
                Dim r1 = rc.rect
                r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
                Dim indexLast As Integer = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If indexLast > 0 And indexLast < rcListLast.Count Then
                    indexLast -= 1 ' index is 1 less than the rcIndexMap value
                    r2 = rcListLast(indexLast).rect
                Else
                    indexLast = -1
                End If
                If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                    rc.age = rcListLast(indexLast).age + 1
                    If rc.age >= 1000 Then rc.age = 100
                    count += 1
                End If

                If usedColor.Contains(rc.index) Then rc.age = 1
                usedColor.Add(rc.index)

                rc.mapID = rcList.Count + 1
                rcList.Add(rc)
                rcIndexMap(rc.rect).SetTo(rc.mapID, rc.mask)
                SetTrueText(CStr(rc.age), rc.maxDist)
            Next

            dst2.SetTo(0)
            For Each rc In rcList
                InRange(rcIndexMap(rc.rect), rc.mapID, rc.mapID, rc.mask)
                rc.buildMaxDist(rc.mask)
                dst2(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                Circle(dst2, rc.maxDist, task.DotSize, task.highlight, -1)
            Next

            labels(2) = CStr(classCount) + " cells. " + CStr(rcList.Count) + " cells >" +
                        " minpixels.  " + CStr(count) + " matched to previous generation"
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedCloudLined_Close(cPtr)
        End Sub
    End Class






    Public Class XR_RedColor_LeftRight : Inherits TaskParent
        Dim redLeft As New RedColor_BasicsOld
        Dim redRight As New RedColor_BasicsOld
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Display the RedColor_BasicsOld output for both the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(task.leftView)

            redLeft.Run(reduction.dst2)
            dst2 = Palettize(redLeft.dst2, 0)
            labels(2) = redLeft.labels(2) + " in the left image"

            reduction.Run(task.rightView)

            redRight.Run(reduction.dst2)
            dst3 = Palettize(redRight.dst2, 0)
            labels(3) = redRight.labels(2) + " in the right image"
        End Sub
    End Class





    Public Class XR_RedColor_NWay : Inherits TaskParent
        Dim binN As New BinNWay_Basics
        Dim redC As New RedColor_BasicsOld
        Public Sub New()
            desc = "Run RedColor on the output of the BinNWay_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            binN.Run(src)
            dst3 = binN.dst3
            labels(3) = binN.labels(3)

            redC.Run(binN.dst2)
            labels(2) = redC.labels(2)
            dst2 = redC.dst2
        End Sub
    End Class





    Public Class XR_RedColor_Bricks : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Dim color8u As New Color8U_Basics
        Public brickList As New List(Of brickData)
        Dim redC As New RedColor_BasicsOld
        Public Sub New()
            If standalone Then task.gOptions.showMyDst0.Checked = True
            desc = "Attach an color8u class to each r."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            dst0 = task.leftView
            color8u.Run(task.leftView)

            Dim _redC_cvt As New Mat
            CvtColor(color8u.dst3, _redC_cvt, ColorConversionCodes.BGR2GRAY)
            redC.Run(_redC_cvt)
            labels(2) = redC.labels(3)
            dst2 = redC.dst2

            Dim count As Integer
            dst1.SetTo(0)
            For Each brick As brickData In bricks.brickList
                If CountNonZero(redC.rcIndexMap(brick.lRect)) And brick.rRect.Width > 0 Then
                    dst2(brick.lRect).CopyTo(dst1(brick.rRect))
                    brick.colorClass = color8u.dst2.Get(Of Integer)
                    count += 1
                End If
            Next

            dst3 = ShowAddweighted(dst1, task.rightView, labels(3))
            labels(3) += " " + CStr(count) + " bricks mapped into the right image."
        End Sub
    End Class






    Public Class XR_RedColor_Hulls : Inherits TaskParent
        Public rclist As New List(Of rcDataOld)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Dim redC As New RedC_BasicsOld
        Public Sub New()
            labels = {"", "Cells where convexity defects failed", "", "Improved contour results Using OpenCV's ConvexityDefects"}
            desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim defectCount As Integer
            rcIndexMap.SetTo(0)
            rclist.Clear()
            For Each rc In redC.rcList
                If rc.contour IsNot Nothing AndAlso rc.contour.Count >= 3 Then
                    Dim defects As Vec4i() = Nothing
                    rc.contour = Convex_RedCDefects.checkDefects(rc.contour, defects)
                    If defects.Length > 0 Then
                        rc.contour = Convex_RedCDefects.betterContour(rc.contour, defects)
                    Else
                        defectCount += 1
                    End If
                    DrawTour(rcIndexMap(rc.rect), rc.hull, rc.mapID, -1)
                    rclist.Add(rc)
                End If
            Next
            dst3 = Palettize(rcIndexMap)
            labels(3) = CStr(rclist.Count) + " hulls identified below.  " + CStr(defectCount) +
                        " hulls failed building the defect list."
        End Sub
    End Class






    Public Class XR_RedColor_Lines : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            dst3 = New Mat(dst3.Size(), MatType.CV_8U, 0)
            desc = "Identify and track the lines in an image as RedCloud Cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeat Then dst3.SetTo(0)
            Dim index As Integer
            For Each lp In task.lines.lpList
                Line(dst3, lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                index += 1
                If index > 10 Then Exit For
            Next

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 3)
        End Sub
    End Class










    Public Class XR_RedColor_LineSingle : Inherits TaskParent
        Dim track As New XR_RedColor_Contour
        Dim leftMost As Integer, rightmost As Integer
        Dim leftCenter As cv.Point, rightCenter As cv.Point
        Public Sub New()
            desc = "Create a line between the rightmost and leftmost good feature to show camera motion"
        End Sub
        Private Function findNearest(pt As cv.Point) As Integer
            Dim bestDistance As Single = Single.MaxValue
            Dim bestIndex As Integer
            For Each rc In track.redC.rcList
                Dim d = pt.DistanceTo(rc.maxDist)
                If d < bestDistance Then
                    bestDistance = d
                    bestIndex = rc.mapID
                End If
            Next
            Return bestIndex
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            track.Run(src)
            dst2 = track.dst2
            If track.redC.rcList.Count = 0 Then
                SetTrueText("No lines found to track.", 3)
                Exit Sub
            End If
            Dim xList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each rc In track.redC.rcList
                If rc.mapID = 0 Then Continue For
                xList.Add(rc.rect.X, rc.mapID)
            Next

            Dim minLeft As Integer = xList.Count / 4
            Dim minRight As Integer = (xList.Count - minLeft)

            If leftMost = 0 Or rightmost = 0 Or leftMost = rightmost Then
                leftCenter = rightCenter ' force iteration...
                Dim iterations As Integer
                While leftCenter.DistanceTo(rightCenter) < dst2.Width / 4
                    leftMost = task.msRNG.Next(minLeft, minRight)
                    rightmost = task.msRNG.Next(minLeft, minRight)
                    leftCenter = track.redC.rcList(leftMost).maxDist
                    rightCenter = track.redC.rcList(rightmost).maxDist
                    iterations += 1
                    If iterations > 10 Then Exit Sub
                End While
            End If

            leftMost = findNearest(leftCenter) - 1
            rightmost = findNearest(rightCenter) - 1
            If leftMost >= 0 And leftMost < track.redC.rcList.Count And
                    rightmost >= 0 And rightmost < track.redC.rcList.Count Then
                leftCenter = track.redC.rcList(leftMost).maxDist
                rightCenter = track.redC.rcList(rightmost).maxDist

                Line(dst2, leftCenter, rightCenter, white, task.lineWidth, task.lineType)
            End If
            labels(2) = track.redC.labels(2)
        End Sub
    End Class







    Public Class XR_RedColor_FeaturesKNN : Inherits TaskParent
        Public knn As New KNN_Basics
        Dim feat As New Feature_Basics
        Public Sub New()
            labels = {"", "", "Output of Feature_Stable", "Grid of points to measure motion."}
            desc = "Use KNN with the good features in the image to create a grid of points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)
            dst2 = feat.dst2
            labels(2) = feat.labels(2)

            knn.ptListQuery = New List(Of cv.Point)(feat.features)
            knn.ptListTrain = New List(Of cv.Point)(feat.features)
            knn.Run(src)

            dst3 = src.Clone
            For i = 0 To knn.queries.Count - 1
                Dim p1 = knn.ptListQuery(i)
                Dim index = knn.result(i, knn.trainInput.Count - 1)
                If index >= 0 And index < knn.ptListTrain.Count Then
                    Dim p2 = knn.ptListTrain(index)
                    Circle(dst3, p1, task.DotSize, Scalar.Yellow, -1, task.lineType)
                    Circle(dst3, p2, task.DotSize, Scalar.Yellow, -1, task.lineType)
                    Line(dst3, p1, p2, white, task.lineWidth, task.lineType)
                End If
            Next
            knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)
        End Sub
    End Class







    Public Class XR_RedColor_GoodCellInput : Inherits TaskParent
        Public knn As New KNN_Basics
        Public featureList As New List(Of Point2f)
        Dim feat As New Feature_Basics
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
            desc = "Use KNN to find good features to track"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)
            dst2 = feat.dst2
            labels(2) = feat.labels(2)

            Static distSlider = OptionParent.FindSlider("Max feature travel distance")
            Dim maxDistance = distSlider.Value

            knn.ptListQuery = New List(Of cv.Point)(feat.features)
            knn.ptListTrain = New List(Of cv.Point)(feat.features)
            knn.Run(src)

            featureList.Clear()
            For i = 0 To knn.queries.Count - 1
                Dim p1 = knn.queries(i)
                Dim index = knn.result(i, 0) ' find nearest
                If index >= 0 And index < knn.trainInput.Count Then
                    Dim p2 = knn.trainInput(index)
                    If p1.DistanceTo(p2) < maxDistance Then featureList.Add(p1)
                End If
            Next
            knn.trainInput = New List(Of Point2f)(knn.queries)
        End Sub
    End Class







    Public Class XR_RedColor_Points : Inherits TaskParent
        Dim track As New XR_RedColor_Contour
        Public Sub New()
            dst3 = New Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0))
            labels = {"", "", "RedCloudX_Track output", "Input to RedCloudX_Track"}
            desc = "Identify and track the end points of lines in an image of RedCloud Cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3.SetTo(0)
            Dim index As Integer
            For Each lp In task.lines.lpList
                Circle(dst3, lp.p1, task.DotSize, 255, -1, task.lineType)
                Circle(dst3, lp.p2, task.DotSize, 255, -1, task.lineType)
                index += 1
                If index >= 10 Then Exit For
            Next

            track.Run(dst3)
            dst2 = track.dst2
        End Sub
    End Class






    Public Class XR_RedColor_DelaunayMap : Inherits TaskParent
        Public dMap As New Delaunay_Map
        Dim redC As New RedColor_BasicsOld
        Public Sub New()
            desc = "Run RedColor as usual but use the Delaunay map to select cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dMap.rcList = New List(Of rcDataOld)(redC.rcList)
            dMap.Run(task.gray)
            dst3 = dMap.dst3
            SetTrueText(redC.strOut, 3)
        End Sub
    End Class







    ''' <summary>
    ''' Isolate a main subject from the scene (similar intent to iPhone "Copy Subject"): run RedColor segmentation,
    ''' pick a salient cell at the image center that is not the dominant background, then composite that region onto a neutral backdrop.
    ''' Click a cell (task.rcDold) when available to override the auto-picked subject.
    ''' </summary>
    Public Class XR_RedColor_Isolate : Inherits TaskParent
        Dim redC As New RedColor_BasicsOld
        Public Sub New()
            desc = "Cursor.ai: Isolate subject via RedColor cells: auto-pick center cell (non-background size); use selected cell if set."
        End Sub
        Private Shared Function Clip(v As Integer, lo As Integer, hi As Integer) As Integer
            If v < lo Then Return lo
            If v > hi Then Return hi
            Return v
        End Function
        Private Shared Function CellMaskFull(rcIndexMap As Mat, rc As rcDataOld) As Mat
            Dim m As New Mat(rcIndexMap.Size, MatType.CV_8U, 0)
            Using roi = rcIndexMap(rc.rect)
                Dim part As New Mat
                InRange(roi, rc.mapID, rc.mapID, part)
                Using part
                    part.CopyTo(m(rc.rect))
                End Using
            End Using
            Return m
        End Function
        Private Shared Sub MorphClean(mask As Mat)
            Dim k = GetStructuringElement(MorphShapes.Rect, New Size(3, 3))
            MorphologyEx(mask, mask, MorphTypes.Open, k)
        End Sub
        Private Shared Function PickSubject(rcIndexMap As Mat, rcList As List(Of rcDataOld)) As rcDataOld
            Dim total = rcIndexMap.Rows * rcIndexMap.Cols
            Dim minPx = CInt(total * 0.003)
            Dim maxPx = CInt(total * 0.62)

            If task.rcDold IsNot Nothing AndAlso task.rcDold.pixels > 0 Then
                For Each rc In rcList
                    If rc.mapID = task.rcDold.mapID Then Return rc
                Next
            End If

            Dim cx = task.workRes.Width \ 2
            Dim cy = task.workRes.Height \ 2
            Dim idxCenter As Integer = rcIndexMap.Get(Of Single)(cy, cx) - 1
            If idxCenter >= 0 And idxCenter < rcList.Count Then
                Dim rc0 = rcList(idxCenter)
                If rc0.pixels >= minPx And rc0.pixels <= maxPx Then Return rc0
            End If

            Dim bestVotes As Integer = -1
            Dim bestRc As rcDataOld = Nothing
            Dim freq As New Dictionary(Of Integer, Integer)
            For dy = -4 To 4
                For dx = -4 To 4
                    Dim y = Clip(cy + dy, 0, rcIndexMap.Rows - 1)
                    Dim x = Clip(cx + dx, 0, rcIndexMap.Cols - 1)
                    Dim ix = rcIndexMap.Get(Of Single)(y, x)
                    If ix <= 0 OrElse ix > rcList.Count Then Continue For
                    Dim rc = rcList(ix - 1)
                    If rc.pixels < minPx OrElse rc.pixels > maxPx Then Continue For
                    If Not freq.ContainsKey(ix) Then freq(ix) = 0
                    freq(ix) += 1
                Next
            Next
            For Each kv In freq
                If kv.Value > bestVotes Then
                    bestVotes = kv.Value
                    bestRc = rcList(kv.Key - 1)
                End If
            Next
            If bestRc IsNot Nothing Then Return bestRc

            Dim bestScore As Integer = -1
            For Each rc In rcList
                If rc.pixels < minPx OrElse rc.pixels > maxPx Then Continue For
                If rc.rect.Contains(New cv.Point(cx, cy)) = False Then Continue For
                If rc.pixels > bestScore Then
                    bestScore = rc.pixels
                    bestRc = rc
                End If
            Next
            Return bestRc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim bgr = If(src IsNot Nothing And src.Channels() = 3, src, task.color)
            redC.Run(bgr)

            Dim subject = PickSubject(redC.rcIndexMap, redC.rcList)
            If subject Is Nothing Then
                dst2 = bgr.Clone
                labels(2) = "Could not auto-pick a subject; try clicking a RedColor cell or adjust scene."
                dst3.SetTo(0)
                Exit Sub
            End If

            Dim mask = CellMaskFull(redC.rcIndexMap, subject)
            MorphClean(mask)

            dst2 = New Mat(bgr.Size(), MatType.CV_8UC3, New Scalar(245, 245, 245))
            bgr.CopyTo(dst2, mask)

            dst3 = New Mat(mask.Size(), MatType.CV_8UC3, New Scalar(0, 0, 0))
            dst3.SetTo(New Scalar(255, 255, 255), mask)

            labels(2) = "Subject index=" + CStr(subject.mapID) + ", pixels=" + CStr(subject.pixels) +
                    " (RedColor cell cutout; not ML portrait matting)."
            labels(3) = "white = kept region. Select another cell with RedColor UI to retarget."
            strOut = "Uses RedCloud color flood cells (RedColor_BasicsOld). Auto-pick avoids cells covering most of the frame." + vbCrLf +
                 "For iPhone-like quality you would need a learned segmenter; this is a fast geometric proxy."
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class XR_RedColor_FeatureLess : Inherits TaskParent
        Public redC As New RedC_BasicsOld
        Dim fLess As New XR_FeatureLess_BasicsOld
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use the output of the FeatureLess_BasicsOld as input the RedColor_BasicsOld."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            dst3 = fLess.dst2
            labels(3) = fLess.labels(2)

            redC.Run(dst3)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
        End Sub
    End Class





    Public Class XR_RedColor_Contour : Inherits TaskParent
        Public redC As New RedColor_BasicsOld
        Public Sub New()
            If New Size(task.workRes.Width, task.workRes.Height) <> New Size(168, 94) Then task.fOptions.FrameHistoryCount.Value = 1
            desc = "Get stats on each RedColor cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst3 = redC.dst2
            labels(3) = redC.labels(2)

            dst2.SetTo(0)
            For Each rc In redC.rcList
                DrawTour(dst2(rc.rect), rc.contour, task.scalarColors(rc.index Mod 255), -1)
                If task.rcDold IsNot Nothing Then
                    If rc.mapID = task.rcDold.mapID Then DrawTour(dst2(rc.rect), rc.contour, white, -1)
                End If
            Next
        End Sub
    End Class




    Public Class XR_RedC_CellMerge1 : Inherits TaskParent
        Public rcList As New List(Of rcDataOld)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Dim flood As New Flood_CellMerge
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "rcIndexMap from Flood_CellMerge dst3 labels"
            desc = "Cursor.ai: Build rcList and rcIndexMap from the Flood_CellMerge dst3 output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst1 = flood.dst2
            dst2 = flood.dst3

            Dim labelMap = flood.dst1
            Dim mm = GetMinMax(labelMap)
            rcList.Clear()
            rcIndexMap.SetTo(0)
            For label = 1 To CInt(mm.maxVal)
                Dim cellMask As New Mat
                InRange(labelMap, label, label, cellMask)
                If CountNonZero(cellMask) = 0 Then Continue For
                Dim nz As New Mat
                FindNonZero(cellMask, nz)
                If nz.Rows = 0 Then Continue For
                Dim r = ValidateRect(BoundingRect(nz))
                Dim rc As New rcDataOld(cellMask(r), r, 255)
                If rc.pixels = 0 Then Continue For
                rc.mapID = label
                rc.index = rcList.Count + 1
                rcList.Add(rc)
                rcIndexMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            dst3 = Palettize(rcIndexMap, 0)
            SetTrueText(RedC_BasicsOld.displayCell(rcList, rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)
            If task.rcDold IsNot Nothing Then
                Circle(dst2, task.rcDold.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcDold.maxDStable, task.DotSize + 1, black, -1)
            End If
            labels(2) = flood.labels(3)
            labels(3) = CStr(rcList.Count) + " cells built from Flood_CellMerge"
        End Sub
    End Class






    Public Class XR_RedC_NeighborHist : Inherits TaskParent
        Public redC As New RedC_BasicsOld
        Dim lastCenter As cv.Point
        Public rcD As rcDataOld
        Public neighbors As New List(Of rcDataOld)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use a histogram to find the neighbors.  Not working..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcListLast = New List(Of rcDataOld)(redC.rcList)

            If task.heartBeatLT Then dst1.SetTo(0)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.mouseClickFlag Then lastCenter = task.clickPoint
            Dim index As Integer = redC.rcIndexMap.Get(Of Single)(lastCenter.Y, lastCenter.X)

            If index >= 0 Then
                rcD = RedC_BasicsOld.rcIndexFind(rcListLast, index)
            Else
                Dim rect As New cv.Rect(lastCenter.X, lastCenter.Y, task.gridWH, task.gridWH)
                Dim myMapID As Integer = redC.rcMapIDs.Get(Of Single)(lastCenter.Y, lastCenter.X)
                For Each rc In redC.rcList
                    If rc.mapID = myMapID And rc.rect.IntersectsWith(rect) Then
                        rcD = rc
                        Exit For
                    End If
                Next
            End If
            If rcD Is Nothing Then rcD = redC.rcList(0)
            SetTrueText(rcD.displayCell() + vbCrLf, 1)

            Dim histogram As New Mat, tmp As New cv.Mat
            Dim ranges() As Rangef = New Rangef() {New Rangef(0, redC.rcList.Count + 1)}
            Dim delta = task.gridWH / 2
            Dim r = New cv.Rect(rcD.rect.X - delta, rcD.rect.Y - delta, rcD.rect.Width + task.gridWH, rcD.rect.Height + task.gridWH)
            r = ValidateRect(r)
            CalcHist({redC.rcIndexMap(r)}, {0}, New Mat, histogram, 1, {redC.rcList.Count}, ranges)

            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            neighbors.Clear()

            For i = 1 To histArray.Length - 1
                If histArray(i) > 0 Then neighbors.Add(redC.rcList(i))
            Next

            dst3.SetTo(0)
            For i = 0 To neighbors.Count - 1
                dst3(neighbors(i).rect).SetTo(task.scalarColors(neighbors(i).mapID), neighbors(i).mask)
            Next

            dst3(rcD.rect).SetTo(task.highlight, rcD.mask)
            Rectangle(dst3, r, task.highlight, task.lineWidth)
            Rectangle(dst2, r, task.highlight, task.lineWidth)
            labels(3) = CStr(neighbors.Count) + " neighbors were present."

            lastCenter = rcD.maxDStable
            Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
        End Sub
    End Class







    Public Class XR_RedC_SteadyCam : Inherits TaskParent
        Dim steady As New SteadyCam_Basics_TA
        Dim redC As New RedC_BasicsOld
        Dim color8U As New Color8U_Basics
        Public Sub New()
            desc = "Build the RedC cells using the GravityRGB_SteadyXY output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            steady.Run(task.grayOriginal)
            dst3 = steady.dst3

            Threshold(dst3, dst1, 0, 255, cv.ThresholdTypes.BinaryInv)

            color8U.Run(dst3)
            color8U.dst2.SetTo(0, dst1)
            redC.Run(color8U.dst2)
            dst2 = redC.dst2
            dst2.SetTo(0, dst1)
            labels = redC.labels
        End Sub
    End Class




    Public Class XR_RedC_Smoothing : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Reduce the rc.contours points if the distance to the next is < X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2.Clone
            labels(2) = redC.labels(2)

            dst1.SetTo(0)
            For Each rc In redC.rcList
                If rc.pixels > 100 Then
                    Dim epsilon = 0.01 * Cv2.ArcLength(rc.contour, True)
                    Dim simplified() As Point = Cv2.ApproxPolyDP(rc.contour.ToArray, epsilon, True)
                    rc.contour = simplified.ToList
                End If
                DrawContours(dst2(rc.rect), {rc.contour.ToArray}, 0, task.highlight, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class





    Public Class XR_RedC_Reliable : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Public Sub New()
            desc = "Display only those cells that are consistently present since the last heartbeat."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            Dim count As Integer
            For Each rc In redC.rcList
                If rc.age > Math.Min(10, task.frameCount) Then
                    dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " were consistently present."
        End Sub
    End Class





    Public Class XR_RedC_Sizes : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Public Sub New()
            If standalone Then task.gOptions.DebugSlider.Value = 32
            desc = "Use the debug slider to display cells of X pixels or less."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.heartBeat Then dst3.SetTo(0)
            Dim count As Integer
            For Each rc In redC.rcList
                If rc.pixels <= task.gOptions.DebugSlider.Value Then
                    Dim vec = dst2.Get(Of Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    dst3(rc.rect).SetTo(vec, rc.mask)
                    count += 1
                End If
            Next

            labels(3) = CStr(count) + " cells smaller than " + CStr(task.gOptions.DebugSlider.Value) + " pixels."
        End Sub
    End Class







    Public Class XR_RedC_Hulls : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Public rcList As New List(Of rcDataOld)
        Public Sub New()
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Display the hull for each cell."
        End Sub
        Public Overrides Sub RunAlg(src As Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            For i = 0 To redC.rcList.Count - 1
                Dim rc = redC.rcList(i)
                If rc.hull IsNot Nothing Then FillPoly(dst1(rc.rect), {rc.hull}, rc.mapID)
            Next

            rcList = New List(Of rcDataOld)(redC.rcList)
            dst3 = Palettize(dst1)
            labels(3) = CStr(redC.rcList.Count) + " hulls with the smallest on top."
        End Sub
    End Class






    Public Class XR_RedC_TrackHull : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Dim lastCenter As cv.Point
        Dim lastMapID As Byte
        Dim lastRect As cv.Rect
        Public Sub New()
            task.gOptions.showMyDst1.Checked = True
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
            desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeatLT Then dst1.SetTo(0)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst0.SetTo(0)
            For i = redC.rcList.Count - 1 To 0 Step -1
                Dim rc = redC.rcList(i)
                If rc.hull IsNot Nothing Then FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
            Next

            SetTrueText(RedC_BasicsOld.displayCell(redC.rcList, redC.rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            dst3.SetTo(0)
            task.color(task.rcDold.rect).SetTo(white, task.rcDold.mask)
            FillPoly(dst3(task.rcDold.rect), {task.rcDold.hull}, task.scalarColors(task.rcDold.mapID + 1))
            dst3(task.rcDold.rect).SetTo(task.scalarColors(task.rcDold.mapID), task.rcDold.mask)
            Rectangle(dst2, task.rcDold.rect, task.highlight, task.lineWidth)
            Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
            SetTrueText(task.rcDold.displayCell() + vbCrLf, 1)

            lastCenter = Utility_Basics.ComputeHullCentroid(task.rcDold.hull.ToArray, task.rcDold)
            lastMapID = task.rcDold.mapID
            lastRect = task.rcDold.rect
        End Sub
    End Class





    Public Class XR_RedC_NeighborHulls : Inherits TaskParent
        Dim redC As New RedC_BasicsOld
        Dim clickPoint As cv.Point
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
            desc = "Find the neighbors for the selected cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst0.SetTo(0)
            For i = redC.rcList.Count - 1 To 0 Step -1
                Dim rc = redC.rcList(i)
                If rc.hull IsNot Nothing Then FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
            Next

            Dim index As Integer
            If task.mouseClickFlag Then clickPoint = task.clickPoint
            index = dst0.Get(Of Integer)(clickPoint.Y, clickPoint.X)

            Dim rcD = redC.rcList(index)
            SetTrueText(rcD.displayCell() + vbCrLf, 1)

            Dim neighbors As New List(Of Integer)
            For Each pt In rcD.contour
                pt.X += rcD.rect.X
                pt.Y += rcD.rect.Y
                Dim rect = ValidateRect(New cv.Rect(pt.X - task.gridWH / 2, pt.Y - task.gridWH / 2, task.gridWH, task.gridWH))
                Dim pixels(rect.Width * rect.Height - 1) As Integer
                Dim tmp = dst0(rect).Clone
                Marshal.Copy(tmp.Data, pixels, 0, pixels.Length)

                For i = 0 To pixels.Length - 1
                    If pixels(i) = 0 Then Continue For
                    If neighbors.Contains(pixels(i)) = False Then neighbors.Add(pixels(i))
                Next
            Next

            dst3.SetTo(0)
            dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
            For i = 0 To neighbors.Count - 1
                Dim rc = redC.rcList(neighbors(i))
                dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
            Next

            dst3(rcD.rect).SetTo(task.highlight, rcD.mask)
            Circle(dst3, clickPoint, task.DotSize + 2, white, -1)
            labels(3) = CStr(neighbors.Count) + " neighbors were present."
        End Sub
    End Class
End Namespace