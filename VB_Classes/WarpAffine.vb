Imports cvb = OpenCvSharp
' http://opencvexamples.blogspot.com/
Public Class WarpAffine_Basics : Inherits TaskParent
    Public options As New Options_Resize
    Public optionsWarp As New Options_WarpAffine
    Public rotateCenter As cvb.Point2f
    Public rotateAngle As Single ' in degrees
    Dim warpQT As New WarpAffine_BasicsQT
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Angle", 0, 360, 10)
        desc = "Use WarpAffine to transform input images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optionsWarp.RunOpt()

        If standaloneTest() And task.heartBeat Then
            rotateAngle = optionsWarp.angle
            rotateCenter.X = msRNG.Next(0, dst2.Width)
            rotateCenter.Y = msRNG.Next(0, dst2.Height)
        End If

        warpQT.rotateCenter = rotateCenter
        warpQT.rotateAngle = rotateAngle
        warpQT.Run(src)
        labels = warpQT.labels
        dst2 = warpQT.dst2
    End Sub
End Class







' http://opencvexamples.blogspot.com/
Public Class WarpAffine_BasicsQT : Inherits TaskParent
    Public rotateCenter As cvb.Point2f
    Public rotateAngle As Single ' in degrees
    Public Sub New()
        desc = "Use WarpAffine to transform input images with no options."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() And task.heartBeat Then
            SetTrueText("There is no output for the " + traceName + " algorithm.  Use WarpAffine_Basics to test.")
            Exit Sub
        End If
        Dim rotationMatrix = cvb.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1.0)
        cvb.Cv2.WarpAffine(src, dst2, rotationMatrix, src.Size(), cvb.InterpolationFlags.Nearest)

        labels(2) = "Rotated around yellow point " + Format(rotateCenter.X, fmt0) + ", " + Format(rotateCenter.Y, fmt0) +
                    " with Warpaffine with angle: " + CStr(rotateAngle)
    End Sub
End Class







' http://opencvexamples.blogspot.com/
Public Class WarpAffine_Captcha : Inherits TaskParent
    Const charHeight = 40
    Const charWidth = 30
    Const captchaLength = 8
    Dim rng As New System.Random
    Public Sub New()
        desc = "Use OpenCV to build a captcha Turing test."
    End Sub
    Private Sub addNoise(image As cvb.Mat)
        For n = 0 To 100
            Dim i = rng.Next(0, image.Cols - 1)
            Dim j = rng.Next(0, image.Rows - 1)
            Dim center = New cvb.Point(i, j)
            Dim c = New cvb.Scalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255))
            DrawCircle(image, center, rng.Next(1, 3), c)
        Next
    End Sub
    Private Sub addLines(ByRef image As cvb.Mat)
        For i = 0 To captchaLength - 1
            Dim startX = rng.Next(0, image.Cols - 1)
            Dim endX = rng.Next(0, image.Cols - 1)
            Dim startY = rng.Next(0, image.Rows - 1)
            Dim endY = rng.Next(0, image.Rows - 1)

            Dim c = New cvb.Scalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255))
            image.Line(New cvb.Point(startX, startY), New cvb.Point(endX, endY), c, rng.Next(1, 3), task.lineType)
        Next
    End Sub

    Private Sub scaleImg(input As cvb.Mat, ByRef output As cvb.Mat)
        Dim height = rng.Next(0, 19) * -1 + charHeight
        Dim width = rng.Next(0, 19) * -1 + charWidth
        Dim s = New cvb.Size(width, height)
        output = input.Resize(s)
    End Sub
    Private Sub rotateImg(input As cvb.Mat, ByRef output As cvb.Mat)
        Dim sign = CInt(rng.NextDouble())
        If sign = 0 Then sign = -1
        Dim angle = rng.Next(0, 29) * sign ' between -30 and 30
        Dim center = New cvb.Point2f(input.Cols / 2, input.Rows / 2)
        Dim rotationMatrix = cvb.Cv2.GetRotationMatrix2D(center, angle, 1)
        cvb.Cv2.WarpAffine(input, output, rotationMatrix, input.Size(), cvb.InterpolationFlags.Linear, cvb.BorderTypes.Constant, cvb.Scalar.White)
    End Sub
    Private Sub transformPerspective(ByRef charImage As cvb.Mat)
        Dim srcPt() = {New cvb.Point2f(0, 0), New cvb.Point2f(0, charHeight), New cvb.Point2f(charWidth, 0), New cvb.Point2f(charWidth, charHeight)}

        Dim varWidth = charWidth / 2
        Dim varHeight = charHeight / 2.0
        Dim widthWarp = charHeight - varWidth + rng.NextDouble() * varWidth
        Dim heightWarp = charHeight - varHeight + rng.NextDouble() * varHeight

        Dim dstPt() = {New cvb.Point2f(0, 0), New cvb.Point2f(0, charHeight), New cvb.Point2f(charWidth, 0), New cvb.Point2f(widthWarp, heightWarp)}

        Dim perpectiveTranx = cvb.Cv2.GetPerspectiveTransform(srcPt, dstPt)
        cvb.Cv2.WarpPerspective(charImage, charImage, perpectiveTranx, New cvb.Size(charImage.Cols, charImage.Rows), cvb.InterpolationFlags.Cubic,
                               cvb.BorderTypes.Constant, cvb.Scalar.White)
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim characters() As String = {"a", "A", "b", "B", "c", "C", "D", "d", "e", "E", "f", "F", "g", "G", "h", "H", "j", "J", "k", "K", "m", "M", "n", "N", "q", "Q", "R", "t", "T", "w", "W", "x", "X", "y", "Y", "1", "2", "3", "4", "5", "6", "7", "8", "9"}
        Dim charactersSize = characters.Length / characters(0).Length

        Dim outImage As New cvb.Mat(charHeight, charWidth * captchaLength, cvb.MatType.CV_8UC3, cvb.Scalar.White)

        For i = 0 To captchaLength - 1
            Dim charImage = New cvb.Mat(charHeight, charWidth, cvb.MatType.CV_8UC3, cvb.Scalar.White)
            Dim c = characters(rng.Next(0, characters.Length - 1))
            cvb.Cv2.PutText(charImage, c, New cvb.Point(10, charHeight - 10), msRNG.Next(1, 6), msRNG.Next(3, 4),
                           task.vecColors(i), msRNG.Next(1, 5), cvb.LineTypes.AntiAlias)
            transformPerspective(charImage)
            rotateImg(charImage, charImage)
            scaleImg(charImage, charImage)
            charImage.CopyTo(outImage(New cvb.Rect(charWidth * i, 0, charImage.Cols, charImage.Rows)))
        Next

        addLines(outImage)
        addNoise(outImage)
        Dim roi As New cvb.Rect(0, src.Height / 2 - charHeight / 2, dst2.Cols, charHeight)
        dst2(roi) = outImage.Resize(New cvb.Size(dst2.Cols, charHeight))
    End Sub
End Class









' https://docs.opencvb.org/3.0-beta/doc/py_tutorials/py_imgproc/py_geometric_transformations/py_geometric_transformations.html
Public Class WarpAffine_3Points : Inherits TaskParent
    Dim triangle As New Area_MinTriangle_CPP_VB
    Dim M As New cvb.Mat
    Public Sub New()
        desc = "Use 3 non-colinear points to build an affine transform and apply it to the color image."
        labels(2) = "Triangles define the affine transform"
        labels(3) = "Image with affine transform applied"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            Dim triangles(1) As cvb.Mat
            triangle.Run(src)
            triangles(0) = triangle.triangle.Clone()
            Dim srcPoints1 As New List(Of cvb.Point2f)(triangle.options.srcPoints)
            triangle.Run(src)
            triangles(1) = triangle.triangle.Clone()
            Dim srcPoints2 As New List(Of cvb.Point2f)(triangle.options.srcPoints)

            Dim tOriginal = cvb.Mat.FromPixelData(3, 1, cvb.MatType.CV_32FC2, New Single() {0, 0, 0, src.Height, src.Width, src.Height})
            M = cvb.Cv2.GetAffineTransform(tOriginal, triangles(1))

            Dim wideMat = New cvb.Mat(src.Rows, src.Cols * 2, cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
            ' uncomment this line to see original pose of the left triangle
            ' triangles(0) = tOriginal
            For j = 0 To 1
                For i = 0 To triangles(j).Rows - 1
                    Dim p1 = triangles(j).Get(Of cvb.Point2f)(i) + New cvb.Point2f(j * src.Width, 0)
                    Dim p2 = triangles(j).Get(Of cvb.Point2f)((i + 1) Mod 3) + New cvb.Point2f(j * src.Width, 0)
                    Dim color = Choose(i + 1, cvb.Scalar.Red, cvb.Scalar.White, cvb.Scalar.Yellow)
                    wideMat.Line(p1, p2, color, task.lineWidth + 3, task.lineType)
                    If j = 0 Then
                        Dim p3 = triangles(j + 1).Get(Of cvb.Point2f)(i) + New cvb.Point2f(src.Width, 0)
                        DrawLine(wideMat, p1, p3, cvb.Scalar.White)
                    End If
                Next
            Next

            Dim corner = triangles(0).Get(Of cvb.Point2f)(0)
            DrawCircle(wideMat, corner, task.DotSize + 5, cvb.Scalar.Yellow)
            corner = New cvb.Point2f(M.Get(Of Double)(0, 2) + src.Width, M.Get(Of Double)(1, 2))
            DrawCircle(wideMat, corner, task.DotSize + 5, cvb.Scalar.Yellow)

            dst2 = wideMat(New cvb.Rect(0, 0, src.Width, src.Height))
            dst3 = wideMat(New cvb.Rect(src.Width, 0, src.Width, src.Height))

            Dim pt As cvb.Point
            For i = 0 To srcPoints1.Count - 1
                pt = New cvb.Point(CInt(srcPoints1(i).X), CInt(srcPoints1(i).Y))
                DrawCircle(dst2, pt, task.DotSize + 2, cvb.Scalar.White)
                pt = New cvb.Point(CInt(srcPoints2(i).X), CInt(srcPoints2(i).Y))
                DrawCircle(dst3, pt, task.DotSize + 2, cvb.Scalar.White)
            Next
        End If
        SetTrueText("M defined as: " + vbCrLf +
                      Format(M.Get(Of Double)(0, 0), fmt2) + vbTab +
                      Format(M.Get(Of Double)(0, 1), fmt2) + vbTab +
                      Format(M.Get(Of Double)(0, 2), fmt2) + vbCrLf +
                      Format(M.Get(Of Double)(1, 0), fmt2) + vbTab +
                      Format(M.Get(Of Double)(1, 1), fmt2) + vbTab +
                      Format(M.Get(Of Double)(1, 2), fmt2))
    End Sub
End Class





' https://docs.opencvb.org/3.0-beta/doc/py_tutorials/py_imgproc/py_geometric_transformations/py_geometric_transformations.html
Public Class WarpAffine_4Points : Inherits TaskParent
    Dim mRect As New Area_MinRect
    Dim options As New Options_MinArea
    Dim M As New cvb.Mat
    Public Sub New()
        desc = "Use 4 non-colinear points to build a perspective transform and apply it to the color image."
        labels(2) = "Color image with perspective transform applied"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            options.RunOpt()
            mRect.inputPoints = options.srcPoints

            Dim roi = New cvb.Rect(50, src.Height / 2, src.Width / 6, src.Height / 6)
            Dim smallImage = src.Resize(New cvb.Size(roi.Width, roi.Height))
            Dim rectangles(1) As cvb.RotatedRect
            mRect.Run(src)
            rectangles(1) = mRect.minRect
            rectangles(1).Center.X = src.Width - rectangles(0).Center.X - roi.Width

            rectangles(0) = New cvb.RotatedRect(New cvb.Point2f(src.Width / 2, src.Height / 2), New cvb.Size2f(src.Width, src.Height), 0)
            M = cvb.Cv2.GetPerspectiveTransform(rectangles(0).Points.ToArray, rectangles(1).Points.ToArray)
            cvb.Cv2.WarpPerspective(src, dst2, M, src.Size())
            dst2(roi) = smallImage

            ' comment this line to see the real original dimensions and location.
            ' rectangles(0) = New cvb.RotatedRect(New cvb.Point2f(roi.X + roi.Width / 2, roi.Y + roi.Height / 2), New cvb.Size2f(roi.Width, roi.Height), 0)
            For j = 0 To 1
                For i = 0 To rectangles(j).Points.Length - 1
                    Dim p1 = rectangles(j).Points(i)
                    Dim p2 = rectangles(j).Points((i + 1) Mod rectangles(j).Points.Length)
                    If j = 0 Then
                        Dim p3 = rectangles(1).Points(i)
                        DrawLine(dst2, p1, p3, cvb.Scalar.White)
                    End If
                    Dim color = Choose(i + 1, cvb.Scalar.Red, cvb.Scalar.White, cvb.Scalar.Yellow, cvb.Scalar.Green)
                    dst2.Line(p1, p2, color, task.lineWidth + 3, task.lineType)
                Next
            Next
        End If

        SetTrueText("M defined as: " + vbCrLf +
                      Format(M.Get(Of Double)(0, 0), fmt2) + vbTab +
                      Format(M.Get(Of Double)(0, 1), fmt2) + vbTab +
                      Format(M.Get(Of Double)(0, 2), fmt2) + vbCrLf +
                      Format(M.Get(Of Double)(1, 0), fmt2) + vbTab +
                      Format(M.Get(Of Double)(1, 1), fmt2) + vbTab +
                      Format(M.Get(Of Double)(1, 2), fmt2) + vbCrLf +
                      Format(M.Get(Of Double)(2, 0), fmt2) + vbTab +
                      Format(M.Get(Of Double)(2, 1), fmt2) + vbTab +
                      Format(M.Get(Of Double)(2, 2), fmt2) + vbCrLf)
        Dim center As New cvb.Point2f(M.Get(Of Double)(0, 2), M.Get(Of Double)(1, 2))
        DrawCircle(dst2,center, task.DotSize + 5, cvb.Scalar.Yellow)
        center = New cvb.Point2f(50, src.Height / 2)
        DrawCircle(dst2,center, task.DotSize + 5, cvb.Scalar.Yellow)
    End Sub
End Class






' https://github.com/BhanuPrakashNani/Image_Processing/blob/master/Successive%20Rotations/rotation.py
Public Class WarpAffine_Repeated : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Rotated repeatedly 45 degrees - note the blur", "Rotated repeatedly 90 degrees"}
        desc = "Compare an image before and after repeated and equivalent in degrees rotations."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim rect = New cvb.Rect(0, 0, dst2.Height, dst2.Height)
        dst1 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst2 = dst1.Clone
        dst3 = dst1.Clone

        Dim center = New cvb.Point(rect.Width / 2, rect.Height / 2)
        Dim angle45 = 45, angle90 = 90, scale = 1.0, h = rect.Height, w = rect.Width

        Dim m1 = cvb.Cv2.GetRotationMatrix2D(center, angle45, scale)
        Dim m2 = cvb.Cv2.GetRotationMatrix2D(center, angle90, scale)

        Dim abs_cos = Math.Abs(m2.Get(Of Double)(0, 0))
        Dim abs_sin = Math.Abs(m2.Get(Of Double)(0, 1))

        Dim bound_w = CInt(h * abs_sin + w * abs_cos)
        Dim bound_h = CInt(h * abs_cos + w * abs_sin)

        Dim val = m1.Get(Of Double)(0, 2)
        m1.Set(Of Double)(0, 2, val + bound_w / 2 - center.X)
        val = m1.Get(Of Double)(1, 2)
        m1.Set(Of Double)(1, 2, val + bound_h / 2 - center.Y)

        val = m2.Get(Of Double)(0, 2)
        m2.Set(Of Double)(0, 2, val + bound_w / 2 - center.X)
        val = m2.Get(Of Double)(1, 2)
        m2.Set(Of Double)(1, 2, val + bound_h / 2 - center.Y)

        cvb.Cv2.WarpAffine(dst1(rect), dst2(rect), m1, New cvb.Size(bound_w, bound_h))

        For i = 0 To 6
            cvb.Cv2.WarpAffine(dst2(rect), dst2(rect), m1, New cvb.Size(bound_w, bound_h))
        Next

        cvb.Cv2.WarpAffine(dst1(rect), dst3(rect), m2, New cvb.Size(bound_w, bound_h))

        For i = 0 To 2
            cvb.Cv2.WarpAffine(dst3(rect), dst3(rect), m2, New cvb.Size(bound_w, bound_h))
        Next
        dst2.Rectangle(rect, cvb.Scalar.White, task.lineWidth, task.lineType)
        dst3.Rectangle(rect, cvb.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class







' https://github.com/BhanuPrakashNani/Image_Processing/blob/master/Successive%20Rotations/rotation.py
Public Class WarpAffine_RepeatedExample8 : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Rotated repeatedly 45 degrees", "Rotated repeatedly 90 degrees"}
        desc = "Compare an image before and after repeated rotations."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim input = cvb.Cv2.ImRead(task.HomeDir + "Data/8.jpg", cvb.ImreadModes.Color)

        Dim center = New cvb.Point(input.Width / 2, input.Height / 2)
        Dim angle45 = 45, angle90 = 90, scale = 1.0, h = input.Height, w = input.Width

        Dim m1 = cvb.Cv2.GetRotationMatrix2D(center, angle45, scale)
        Dim m2 = cvb.Cv2.GetRotationMatrix2D(center, angle90, scale)

        Dim abs_cos = Math.Abs(m2.Get(Of Double)(0, 0))
        Dim abs_sin = Math.Abs(m2.Get(Of Double)(0, 1))

        Dim bound_w = CInt(h * abs_sin + w * abs_cos)
        Dim bound_h = CInt(h * abs_cos + w * abs_sin)

        Dim val = m1.Get(Of Double)(0, 2)
        m1.Set(Of Double)(0, 2, val + bound_w / 2 - center.X)
        val = m1.Get(Of Double)(1, 2)
        m1.Set(Of Double)(1, 2, val + bound_h / 2 - center.Y)

        val = m2.Get(Of Double)(0, 2)
        m2.Set(Of Double)(0, 2, val + bound_w / 2 - center.X)
        val = m2.Get(Of Double)(1, 2)
        m2.Set(Of Double)(1, 2, val + bound_h / 2 - center.Y)

        cvb.Cv2.WarpAffine(input, dst2, m1, New cvb.Size(bound_w, bound_h))

        For i = 0 To 6
            cvb.Cv2.WarpAffine(dst2, dst2, m1, New cvb.Size(bound_w, bound_h))
        Next

        cvb.Cv2.WarpAffine(input, dst3, m2, New cvb.Size(bound_w, bound_h))

        For i = 0 To 2
            cvb.Cv2.WarpAffine(dst3, dst3, m2, New cvb.Size(bound_w, bound_h))
        Next
    End Sub
End Class