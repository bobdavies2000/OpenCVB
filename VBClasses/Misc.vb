Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp : Imports gdip = OpenCvSharp.GdipExtensions
Imports System.Text.RegularExpressions
Namespace VBClasses
    ' https://github.com/ncosentino/DevLeader/tree/master/AsciiArtGenerator
    Public Class XR_AsciiArt_Basics : Inherits TaskParent
        ReadOnly asciiChars As String() = {"@", "%", "#", "*", "+", "=", "-", ":", ",", ".", " "}
        ReadOnly options As New Options_AsciiArt
        Public Sub New()
            labels = {"", "", "Ascii version", "Grayscale input to ascii art"}
            desc = "Build an ascii art representation of the input stream."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Resize(task.gray, dst3, options.size, 0, 0, InterpolationFlags.Nearest)
            For y = 0 To dst3.Height - 1
                For x = 0 To dst3.Width - 1
                    Dim grayValue = dst3.Get(Of Byte)(y, x)
                    Dim asciiChar = asciiChars(grayValue * (asciiChars.Length - 1) / 255)
                    SetTrueText(asciiChar, New cv.Point(x * options.wStep, y * options.hStep), 2)
                Next
            Next
            labels(2) = "Ascii version using " + (dst3.Height * dst3.Width).ToString(fmt0) + " characters"
        End Sub
    End Class







    Public Class XR_AsciiArt_Color : Inherits TaskParent
        Public Sub New()
            dst3 = New Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "A palette'd version of the ascii art data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim hStep = CInt(src.Height / 31) - 1
            Dim wStep = CInt(src.Width / 55) - 1
            Dim size = New Size(55, 31)
            Resize(task.gray, dst1, size, 0, 0, InterpolationFlags.Nearest)
            Dim grayRatio = 12 / 255
            For y = 0 To dst1.Height - 1
                For x = 0 To dst1.Width - 1
                    Dim r = New cv.Rect(x * wStep, y * hStep, wStep - 1, hStep - 1)
                    Dim asciiChar = CInt(dst1.Get(Of Byte)(y, x) * grayRatio)
                    dst3(r).SetTo(asciiChar)
                Next
            Next
            dst2 = Palettize(dst3, 0)
        End Sub
    End Class







    Public Class XR_AsciiArt_Diff : Inherits TaskParent
        ReadOnly grayAA As New XR_AsciiArt_Color
        ReadOnly diff As New Diff_Basics
        Public Sub New()
            desc = "Display the instability in image pixels."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            grayAA.Run(src)
            dst2 = grayAA.dst2

            Dim _diff_cvt As New Mat
            CvtColor(dst2, _diff_cvt, ColorConversionCodes.BGR2GRAY)
            diff.Run(_diff_cvt)
            dst3 = diff.dst2
        End Sub
    End Class




    ' Benford's Law is pretty cool but I don't think it is a phenomenon of nature.  It is produced from bringing real world measurements to a human scale.
    ' Reducing an image with compression works because human understanding maps the data within reach of the understanding embedded in our number system.
    ' (Further investigation: would a base other than 10 provide the same results?)
    ' If real world measurements do not conform to Benford's Law, it is likely because the measurement is not a good one or has been manipulated.
    ' Benford's law is a good indicator that the scale for the measurement is appropriate.
    ' Below are 2 types of examples - one just takes the grayscale image and applies Benford's analysis, the other uses jpeg/PNG before applying Benford.
    ' Only the JPEG/PNG examples match Benford while the grayscale image does not.
    ' Note that with the 10-99 Benford JPEG example, the results match Benford and then stop matching and abruptly fall off in the middle of the plot.
    ' This impact is likely the result of how JPEG compression truncates values as insignificant - a definite manipulation of the data.

    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_Basics : Inherits TaskParent
        Public expectedDistribution(10 - 1) As Single
        Public counts(expectedDistribution.Length - 1) As Single
        Dim plotHist As New PlotBar_Basics
        Dim use99 As Boolean
        Public Sub New()
            For i = 1 To expectedDistribution.Length - 1
                expectedDistribution(i) = Math.Log10(1 + 1 / i) ' get the expected values.
            Next

            labels(3) = "Actual distribution of input"
            desc = "Build the capability to perform a Benford analysis."
        End Sub
        Public Sub setup99()
            ReDim expectedDistribution(100 - 1)
            For i = 1 To expectedDistribution.Length - 1
                expectedDistribution(i) = Math.Log10(1 + 1 / i)
            Next
            ReDim counts(expectedDistribution.Length - 1)
            use99 = True
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New Mat
            If standalone Then
                task.gray.ConvertTo(gray32f, MatType.CV_32F)
            Else
                gray32f = src
            End If

            gray32f = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
            ReDim counts(expectedDistribution.Length - 1)
            If use99 = False Then
                For i = 0 To gray32f.Rows - 1
                    Dim val = gray32f.At(Of Single)(i, 0)
                    Dim valstr = val.ToString
                    If val <> 0 And Single.IsNaN(val) = False Then
                        Dim firstInt = Regex.Match(valstr, "[1-9]{1}")
                        If firstInt.Length > 0 Then counts(firstInt.Value) += 1
                    End If
                Next
            Else
                ' this is for the distribution 10-99
                For i = 0 To gray32f.Rows - 1
                    Dim val = gray32f.At(Of Single)(i, 0)
                    If val <> 0 And Single.IsNaN(val) = False Then
                        Dim valstr = val.ToString
                        Dim firstInt = Regex.Match(valstr, "[1-9]{1}").ToString
                        Dim index = valstr.IndexOf(firstInt)
                        If index < Len(valstr - 2) And index > 0 Then
                            Dim val99 = Mid(valstr, index + 1, 2)
                            If IsNumeric(val99) Then counts(val99) += 1
                        End If
                    End If
                Next
            End If

            Dim hist = Mat.FromPixelData(counts.Length, 1, MatType.CV_32F, counts)
            plotHist.backgroundColor = Scalar.Blue
            plotHist.Run(hist)
            dst3 = plotHist.dst2.Clone
            For i = 0 To counts.Length - 1
                counts(i) = gray32f.Cols * expectedDistribution(i)
            Next

            hist = Mat.FromPixelData(counts.Length, 1, MatType.CV_32F, counts)
            plotHist.backgroundColor = Scalar.Gray
            plotHist.Run(hist)

            dst2 = ShowAddweighted(Not plotHist.dst2, dst3, labels(2))
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_NormalizedImage : Inherits TaskParent
        Public benford As New XR_Benford_Basics
        Public Sub New()
            desc = "Perform a Benford analysis of an image normalized to between 0 and 1"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New Mat
            task.gray.ConvertTo(gray32f, MatType.CV_32F)

            benford.Run(gray32f)
            Normalize(benford.dst2, dst2, 1)
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_NormalizedImage99 : Inherits TaskParent
        Public benford As New XR_Benford_Basics
        Public Sub New()
            benford.setup99()

            desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gray32f As New Mat
            task.gray.ConvertTo(gray32f, MatType.CV_32F)

            benford.Run(gray32f)
            Normalize(benford.dst2, dst2, 1)
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_JPEG : Inherits TaskParent
        Public benford As New XR_Benford_Basics
        Dim options As New Options_JpegQuality
        Public Sub New()
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim param = New ImageEncodingParam(ImwriteFlags.JpegQuality, options.quality)
            Dim jpeg As Byte() = Nothing
            ImEncode(".jpg", src, jpeg, {param})
            Dim tmp = Mat.FromPixelData(jpeg.Length, 1, MatType.CV_8U, jpeg)
            dst3 = ImDecode(tmp, ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_JPEG99 : Inherits TaskParent
        Public benford As New XR_Benford_Basics
        Public options As New Options_JpegQuality
        Public Sub New()
            benford.setup99()
            desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim param = New ImageEncodingParam(ImwriteFlags.JpegQuality, options.quality)
            Dim jpeg As Byte() = Nothing
            ImEncode(".jpg", src, jpeg, {param})
            Dim tmp = Mat.FromPixelData(jpeg.Length, 1, MatType.CV_8U, jpeg)
            dst3 = ImDecode(tmp, ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class







    ' https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    Public Class XR_Benford_PNG : Inherits TaskParent
        Dim options As New Options_PNGCompression
        Public benford As New XR_Benford_Basics
        Public Sub New()
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim param = New ImageEncodingParam(ImwriteFlags.JpegQuality, 90)
            Dim png As Byte() = Nothing
            ImEncode(".jpg", src, png, {param})
            Dim tmp = Mat.FromPixelData(png.Length, 1, MatType.CV_8U, png)
            dst3 = ImDecode(tmp, ImreadModes.Color)
            benford.Run(tmp)
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
            labels(3) = "Input image"
        End Sub
    End Class






    Public Class XR_Benford_Depth : Inherits TaskParent
        Public benford As New XR_Benford_Basics
        Public Sub New()
            desc = "Apply Benford to the depth data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            benford.Run(task.pcSplit(2))
            dst2 = benford.dst2
            labels(2) = benford.labels(3)
        End Sub
    End Class









    Public Class XR_Benford_Primes : Inherits TaskParent
        Dim sieve As New Sieve_BasicsVB
        Dim benford As New XR_Benford_Basics
        Public Sub New()
            sieve.setMaxPrimes()
            labels = {"", "", "Actual Distribution of input", ""}
            desc = "Apply Benford to a list of primes"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then sieve.Run(src) ' only need to compute this once...
            SetTrueText($"Primes found: {sieve.primes.Count}", 3)

            Dim tmp = Mat.FromPixelData(sieve.primes.Count, 1, MatType.CV_32S, sieve.primes.ToArray())
            tmp.ConvertTo(tmp, MatType.CV_32F)
            benford.Run(tmp)
            dst2 = benford.dst2
        End Sub
    End Class





    Public Class XR_Bezier_Basics : Inherits TaskParent
        Public points() As cv.Point
        Public Sub New()
            points = {New cv.Point(100, 100),
                              New cv.Point(150, 50),
                              New cv.Point(250, 150),
                              New cv.Point(300, 100),
                              New cv.Point(350, 150),
                              New cv.Point(450, 50)}
            desc = "Use n points to draw a Bezier curve."
        End Sub
        Public Shared Function nextPoint(points() As cv.Point, i As Integer, t As Single) As cv.Point
            Dim x = Math.Pow(1 - t, 3) * points(i).X +
                            3 * t * Math.Pow(1 - t, 2) * points(i + 1).X +
                            3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).X +
                            Math.Pow(t, 3) * points(i + 3).X

            Dim y = Math.Pow(1 - t, 3) * points(i).Y +
                            3 * t * Math.Pow(1 - t, 2) * points(i + 1).Y +
                            3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).Y +
                            Math.Pow(t, 3) * points(i + 3).Y
            Return New cv.Point(x, y)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim p1 As cv.Point
            For i = 0 To points.Length - 4 Step 3
                For j = 0 To 100
                    Dim p2 = nextPoint(points, i, j / 100)
                    If j > 0 Then Line(dst2, p1, p2, task.highlight, task.lineWidth, task.lineWidth)
                    p1 = p2
                Next
            Next
            labels(2) = "Bezier output"
        End Sub
    End Class







    Public Class XR_Bezier_Example : Inherits TaskParent
        Public points() As cv.Point = {New cv.Point(task.DotSize, task.DotSize), New cv.Point(dst2.Width / 6, dst2.Width / 6),
                                           New cv.Point(dst2.Width * 3 / 4, dst2.Height / 2),
                                           New cv.Point(dst2.Width - task.DotSize * 2, dst2.Height - task.DotSize * 2)}
        Public Sub New()
            desc = "Draw a Bezier curve based with the 4 input points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2.SetTo(0)
            Dim p1 As cv.Point
            For i = 0 To 100 - 1
                Dim p2 = XR_Bezier_Basics.nextPoint(points, 0, i / 100)
                If i > 0 Then Line(dst2, p1, p2, task.highlight, task.lineWidth, task.lineWidth)
                p1 = p2
            Next

            For i = 0 To points.Length - 1
                Circle(dst2, points(i), task.DotSize + 2, white, -1, task.lineType)
            Next

            Line(dst2, points(0), points(1), white, task.lineWidth, task.lineWidth)
            Line(dst2, points(2), points(3), white, task.lineWidth, task.lineWidth)
        End Sub
    End Class





    ' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
    Public Class XR_Bitmap_ToMat : Inherits TaskParent
        Public Sub New()
            labels(2) = "Convert color bitmap to Mat"
            labels(3) = "Convert Mat to bitmap and then back to Mat"
            desc = "Convert a color and grayscale bitmap to a Mat"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim filePath As String = task.homeDir + "opencv/Samples/Data/lena.jpg"
            Dim bitmap = New System.Drawing.Bitmap(filePath)
            Resize(gdip.BitmapConverter.ToMat(bitmap), dst2, src.Size)

            bitmap = gdip.BitmapConverter.ToBitmap(src)
            dst3 = gdip.BitmapConverter.ToMat(bitmap)
        End Sub
    End Class





    Public Class XR_Blob_Basics : Inherits TaskParent
        Implements IDisposable
        Dim options As New Options_Blob
        Dim input As New XR_Blob_Input
        Dim simpleBlob As SimpleBlobDetector
        Public Sub New()
            desc = "Isolate and list blobs with specified options"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standaloneTest() Then
                input.Run(src)
                dst2 = input.dst2
            Else
                dst2 = src
            End If

            Dim binaryImage As New Mat
            CvtColor(dst2, binaryImage, ColorConversionCodes.BGR2GRAY)
            Threshold(binaryImage, binaryImage, thresh:=0, maxval:=255, type:=ThresholdTypes.Binary)

            If simpleBlob Is Nothing Then
                simpleBlob = SimpleBlobDetector.Create(CType(options.blobParams, SimpleBlobDetector.Params))
            End If
            Dim keypoint = simpleBlob.Detect(dst2)

            DrawKeypoints(image:=binaryImage,
                                 keypoints:=keypoint,
                                 outImage:=dst3,
                                 color:=Scalar.FromRgb(255, 0, 0),
                                 flags:=DrawMatchesFlags.DrawRichKeypoints)
        End Sub
        Protected Overrides Sub Finalize()
            If simpleBlob IsNot Nothing Then simpleBlob.Dispose()
        End Sub
    End Class





    ' https://stackoverflow.com/questions/14770756/opencv-simpleblobdetector-filterbyinertia-meaning
    Public Class XR_Blob_Input : Inherits TaskParent
        Dim rotatedRect As New Rectangle_Rotated
        Dim circles As New Draw_Circles
        Dim ellipses As New Draw_Ellipses
        Dim poly As New Draw_Polygon
        Public Mats As New Mat_4Click
        Public updateFrequency = 30
        Public Sub New()
            OptionParent.FindSlider("DrawCount").Value = 5
            OptionParent.FindCheckBox("Draw filled (unchecked draw an outline)").Checked = True

            Mats.mats.lineSeparators = False

            labels(2) = "Click any quadrant below to view it on the right"
            labels(3) = "Click any quadrant at left to view it below"
            desc = "Generate data to test Blob Detector."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rotatedRect.Run(src)
            Mats.mat(0) = rotatedRect.dst2

            circles.Run(src)
            Mats.mat(1) = circles.dst2

            ellipses.Run(src)
            Mats.mat(2) = ellipses.dst2

            poly.Run(src)
            Mats.mat(3) = poly.dst3

            Mats.Run(task.emptyMat)
            dst2 = Mats.dst2
            dst3 = Mats.dst3
        End Sub
    End Class




    Public Class XR_Blob_RenderBlobs : Inherits TaskParent
        Dim input As New XR_Blob_Input
        Public Sub New()
            labels(2) = "Input blobs"
            labels(3) = "Largest blob, centroid in yellow"
            desc = "Use connected components to find blobs."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeatLT Then
                input.Run(src)
                dst2 = input.dst2
                Dim binary As New Mat
                Threshold(task.gray, binary, 0, 255, ThresholdTypes.Otsu Or ThresholdTypes.Binary)
                Dim labelView = dst2.EmptyClone
                Dim stats As New Mat
                Dim centroids As New Mat
                Dim cc = ConnectedComponentsEx(binary)
                Dim labelCount = ConnectedComponentsWithStats(binary, labelView, stats, centroids)
                cc.RenderBlobs(labelView)

                'For Each b In cc.Blobs.Skip(1)
                '    dst2.Rectangle(b.Rect, Scalar.red, task.lineWidth + 1, task.lineType)
                'Next

                Dim maxBlob = cc.GetLargestBlob()
                dst3.SetTo(0)
                cc.FilterByBlob(dst2, dst3, maxBlob)

                Circle(dst3, New cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.DotSize + 3, Scalar.Blue, -1, task.lineType)
                Circle(dst3, New cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.DotSize, Scalar.Yellow, -1, task.lineType)
            End If
        End Sub
    End Class




    Public Class XR_MaxDist_Basics : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            labels(3) = "Below left shows hullMask while below shows the contour mask."
            desc = "Find the cv.Point farthest from the edges of a mask."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            Dim index As Integer = 1
            For Each rc In redC.rcList
                Dim rcTest = New rcData(rc.mask, rc.rect, index)
                If rcTest.mapID >= 0 Then
                    dst3(rcTest.rect).SetTo(task.scalarColors(rc.index Mod 255), rcTest.mask)
                    Circle(dst3, rc.maxDist, task.DotSize, task.highlight, -1)
                    index += 1
                End If
            Next
        End Sub
    End Class





    Public Class XR_MaxDist_NoRectangle : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            labels(3) = "Below left shows hullMask while below shows the contour mask."
            desc = "Does the mask need to have rectangle of zeros?  Answer: yes"
        End Sub
        Public Shared Function setCloudData(_mask As Mat, _rect As cv.Rect, _index As Integer,
                                                    Optional zeroRectangle As Boolean = True) As rcData
            Dim rc As New rcData
            InRange(_mask, _index, _index, rc.mask)
            rc.rect = _rect
            rc.mapID = _index
            Dim contour = ContourBuild(rc.mask)
            If contour.Count < 3 Then Return Nothing
            Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
            rc.mask = New Mat(rc.mask.Size, MatType.CV_8U, 0)
            DrawContours(rc.mask, listOfPoints, 0, Scalar.All(255), -1, LineTypes.Link4)

            If zeroRectangle Then
                Dim tmp As Mat = rc.mask.Clone
                ' see XR_MaxDist_NoRectangle below to confirm this is needed (it is.)
                Rectangle(tmp, New cv.Rect(0, 0, rc.mask.Width, rc.mask.Height), Scalar.All(0), 1)
                Dim distance32f As New Mat
                DistanceTransform(tmp, distance32f, DistanceTypes.L1, DistanceTransformMasks.Precise, MatType.CV_32F)
                Dim mm As mmData = vbc.GetMinMax(distance32f)
                rc.maxDist.X = mm.maxLoc.X + rc.rect.X
                rc.maxDist.Y = mm.maxLoc.Y + rc.rect.Y
            End If

            rc.hull = ConvexHull(contour.ToArray, True).ToList

            rc.pixels = CountNonZero(rc.mask)
            Return rc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim rcList As New List(Of rcData)
            dst3.SetTo(0)
            For Each rc In redC.rcList
                ' This rcList will NOT use the rectangle of zeros (definitely need the rectangle!)
                Dim rcTest = setCloudData(rc.mask, rc.rect, rcList.Count + 1, False)
                If rcTest Is Nothing Then Continue For
                If rcTest.mapID >= 0 Then
                    dst3(rcTest.rect).SetTo(task.scalarColors(rc.index Mod 255), rcTest.mask)
                    Circle(dst3, rc.maxDist, task.DotSize, task.highlight, -1)
                    rcList.Add(rcTest)
                End If
            Next
        End Sub
    End Class





    Public Class Keyboard_Basics : Inherits TaskParent
        Public keyInput As New List(Of String)
        Dim flow As New Font_FlowText
        Public checkKeys As New OptionsKeyboardInput
        Public Sub New()
            flow.parentData = Me
            checkKeys.Setup(traceName)
            labels(2) = "Use the Options form to send in keystrokes"
            desc = "Test the keyboard interface available to all algorithms"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() And checkKeys.inputText.Count > 0 Then
                For Each txt In checkKeys.inputText
                    flow.nextMsg += txt.ToString()
                Next
                flow.Run(src)
            End If
            checkKeys.inputText.Clear()
        End Sub
    End Class

End Namespace