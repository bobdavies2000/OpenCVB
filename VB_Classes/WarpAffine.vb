Imports cv = OpenCvSharp
' http://opencvexamples.blogspot.com/
Public Class WarpAffine_Basics : Inherits TaskParent
    Public options As New Options_Resize
    Public optionsWarp As New Options_WarpAffine
    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single ' in degrees
    Dim warpQT As New WarpAffine_BasicsQT
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Angle", 0, 360, 10)
        desc = "Use WarpAffine to transform input images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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
    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single ' in degrees
    Public Sub New()
        desc = "Use WarpAffine to transform input images with no options."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            SetTrueText("There is no output for the " + traceName + " algorithm.  Use WarpAffine_Basics to test.")
            Exit Sub
        End If
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1.0)
        cv.Cv2.WarpAffine(src, dst2, rotationMatrix, src.Size(), cv.InterpolationFlags.Nearest)

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
    Private Sub addNoise(image As cv.Mat)
        For n = 0 To 100
            Dim i = rng.Next(0, image.Cols - 1)
            Dim j = rng.Next(0, image.Rows - 1)
            Dim center = New cv.Point(i, j)
            Dim c = New cv.Scalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255))
            DrawCircle(image, center, rng.Next(1, 3), c)
        Next
    End Sub
    Private Sub addLines(ByRef image As cv.Mat)
        For i = 0 To captchaLength - 1
            Dim startX = rng.Next(0, image.Cols - 1)
            Dim endX = rng.Next(0, image.Cols - 1)
            Dim startY = rng.Next(0, image.Rows - 1)
            Dim endY = rng.Next(0, image.Rows - 1)

            Dim c = New cv.Scalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255))
            image.Line(New cv.Point(startX, startY), New cv.Point(endX, endY), c, rng.Next(1, 3), task.lineType)
        Next
    End Sub

    Private Sub scaleImg(input As cv.Mat, ByRef output As cv.Mat)
        Dim height = rng.Next(0, 19) * -1 + charHeight
        Dim width = rng.Next(0, 19) * -1 + charWidth
        Dim s = New cv.Size(width, height)
        output = input.Resize(s)
    End Sub
    Private Sub rotateImg(input As cv.Mat, ByRef output As cv.Mat)
        Dim sign = CInt(rng.NextDouble())
        If sign = 0 Then sign = -1
        Dim angle = rng.Next(0, 29) * sign ' between -30 and 30
        Dim center = New cv.Point2f(input.Cols / 2, input.Rows / 2)
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(center, angle, 1)
        cv.Cv2.WarpAffine(input, output, rotationMatrix, input.Size(), cv.InterpolationFlags.Linear, cv.BorderTypes.Constant, white)
    End Sub
    Private Sub transformPerspective(ByRef charImage As cv.Mat)
        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, charHeight), New cv.Point2f(charWidth, 0), New cv.Point2f(charWidth, charHeight)}

        Dim varWidth = charWidth / 2
        Dim varHeight = charHeight / 2.0
        Dim widthWarp = charHeight - varWidth + rng.NextDouble() * varWidth
        Dim heightWarp = charHeight - varHeight + rng.NextDouble() * varHeight

        Dim dstPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, charHeight), New cv.Point2f(charWidth, 0), New cv.Point2f(widthWarp, heightWarp)}

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(srcPt, dstPt)
        cv.Cv2.WarpPerspective(charImage, charImage, perpectiveTranx, New cv.Size(charImage.Cols, charImage.Rows), cv.InterpolationFlags.Cubic,
                               cv.BorderTypes.Constant, white)
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim characters() As String = {"a", "A", "b", "B", "c", "C", "D", "d", "e", "E", "f", "F", "g", "G", "h", "H", "j", "J", "k", "K", "m", "M", "n", "N", "q", "Q", "R", "t", "T", "w", "W", "x", "X", "y", "Y", "1", "2", "3", "4", "5", "6", "7", "8", "9"}
        Dim charactersSize = characters.Length / characters(0).Length

        Dim outImage As New cv.Mat(charHeight, charWidth * captchaLength, cv.MatType.CV_8UC3, white)

        For i = 0 To captchaLength - 1
            Dim charImage = New cv.Mat(charHeight, charWidth, cv.MatType.CV_8UC3, white)
            Dim c = characters(rng.Next(0, characters.Length - 1))
            cv.Cv2.PutText(charImage, c, New cv.Point(10, charHeight - 10), msRNG.Next(1, 6), msRNG.Next(3, 4),
                           task.vecColors(i), msRNG.Next(1, 5), cv.LineTypes.AntiAlias)
            transformPerspective(charImage)
            rotateImg(charImage, charImage)
            scaleImg(charImage, charImage)
            charImage.CopyTo(outImage(New cv.Rect(charWidth * i, 0, charImage.Cols, charImage.Rows)))
        Next

        addLines(outImage)
        addNoise(outImage)
        Dim roi As New cv.Rect(0, src.Height / 2 - charHeight / 2, dst2.Cols, charHeight)
        dst2(roi) = outImage.Resize(New cv.Size(dst2.Cols, charHeight))
    End Sub
End Class









' https://docs.opencvb.org/3.0-beta/doc/py_tutorials/py_imgproc/py_geometric_transformations/py_geometric_transformations.html
Public Class WarpAffine_3Points : Inherits TaskParent
    Dim triangle As New Area_MinTriangle_CPP
    Dim M As New cv.Mat
    Public Sub New()
        desc = "Use 3 non-colinear points to build an affine transform and apply it to the color image."
        labels(2) = "Triangles define the affine transform"
        labels(3) = "Image with affine transform applied"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim triangles(1) As cv.Mat
            triangle.Run(src)
            triangles(0) = triangle.triangle.Clone()
            Dim srcPoints1 As New List(Of cv.Point2f)(triangle.options.srcPoints)
            triangle.Run(src)
            triangles(1) = triangle.triangle.Clone()
            Dim srcPoints2 As New List(Of cv.Point2f)(triangle.options.srcPoints)

            Dim tOriginal = cv.Mat.FromPixelData(3, 1, cv.MatType.CV_32FC2, New Single() {0, 0, 0, src.Height, src.Width, src.Height})
            M = cv.Cv2.GetAffineTransform(tOriginal, triangles(1))

            Dim wideMat = New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))
            ' uncomment this line to see original pose of the left triangle
            ' triangles(0) = tOriginal
            For j = 0 To 1
                For i = 0 To triangles(j).Rows - 1
                    Dim p1 = triangles(j).Get(Of cv.Point2f)(i) + New cv.Point2f(j * src.Width, 0)
                    Dim p2 = triangles(j).Get(Of cv.Point2f)((i + 1) Mod 3) + New cv.Point2f(j * src.Width, 0)
                    Dim color = Choose(i + 1, cv.Scalar.Red, cv.Scalar.White, cv.Scalar.Yellow)
                    wideMat.Line(p1, p2, color, task.lineWidth + 3, task.lineType)
                    If j = 0 Then
                        Dim p3 = triangles(j + 1).Get(Of cv.Point2f)(i) + New cv.Point2f(src.Width, 0)
                        DrawLine(wideMat, p1, p3, white)
                    End If
                Next
            Next

            Dim corner = triangles(0).Get(Of cv.Point2f)(0)
            DrawCircle(wideMat, corner, task.DotSize + 5, cv.Scalar.Yellow)
            corner = New cv.Point2f(M.Get(Of Double)(0, 2) + src.Width, M.Get(Of Double)(1, 2))
            DrawCircle(wideMat, corner, task.DotSize + 5, cv.Scalar.Yellow)

            dst2 = wideMat(New cv.Rect(0, 0, src.Width, src.Height))
            dst3 = wideMat(New cv.Rect(src.Width, 0, src.Width, src.Height))

            Dim pt As cv.Point
            For i = 0 To srcPoints1.Count - 1
                pt = New cv.Point(CInt(srcPoints1(i).X), CInt(srcPoints1(i).Y))
                DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.White)
                pt = New cv.Point(CInt(srcPoints2(i).X), CInt(srcPoints2(i).Y))
                DrawCircle(dst3, pt, task.DotSize + 2, cv.Scalar.White)
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
    Dim M As New cv.Mat
    Public Sub New()
        desc = "Use 4 non-colinear points to build a perspective transform and apply it to the color image."
        labels(2) = "Color image with perspective transform applied"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            options.RunOpt()
            mRect.inputPoints = options.srcPoints

            Dim roi = New cv.Rect(50, src.Height / 2, src.Width / 6, src.Height / 6)
            Dim smallImage = src.Resize(New cv.Size(roi.Width, roi.Height))
            Dim rectangles(1) As cv.RotatedRect
            mRect.Run(src)
            rectangles(1) = mRect.minRect
            rectangles(1).Center.X = src.Width - rectangles(0).Center.X - roi.Width

            rectangles(0) = New cv.RotatedRect(New cv.Point2f(src.Width / 2, src.Height / 2), New cv.Size2f(src.Width, src.Height), 0)
            M = cv.Cv2.GetPerspectiveTransform(rectangles(0).Points.ToArray, rectangles(1).Points.ToArray)
            cv.Cv2.WarpPerspective(src, dst2, M, src.Size())
            dst2(roi) = smallImage

            ' comment this line to see the real original dimensions and location.
            ' rectangles(0) = New cv.RotatedRect(New cv.Point2f(roi.X + roi.Width / 2, roi.Y + roi.Height / 2), New cv.Size2f(roi.Width, roi.Height), 0)
            For j = 0 To 1
                For i = 0 To rectangles(j).Points.Length - 1
                    Dim p1 = rectangles(j).Points(i)
                    Dim p2 = rectangles(j).Points((i + 1) Mod rectangles(j).Points.Length)
                    If j = 0 Then
                        Dim p3 = rectangles(1).Points(i)
                        DrawLine(dst2, p1, p3, white)
                    End If
                    Dim color = Choose(i + 1, cv.Scalar.Red, cv.Scalar.White, cv.Scalar.Yellow, cv.Scalar.Green)
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
        Dim center As New cv.Point2f(M.Get(Of Double)(0, 2), M.Get(Of Double)(1, 2))
        DrawCircle(dst2,center, task.DotSize + 5, cv.Scalar.Yellow)
        center = New cv.Point2f(50, src.Height / 2)
        DrawCircle(dst2,center, task.DotSize + 5, cv.Scalar.Yellow)
    End Sub
End Class






' https://github.com/BhanuPrakashNani/Image_Processing/blob/master/Successive%20Rotations/rotation.py
Public Class WarpAffine_Repeated : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Rotated repeatedly 45 degrees - note the blur", "Rotated repeatedly 90 degrees"}
        desc = "Compare an image before and after repeated and equivalent in degrees rotations."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim rect = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst1.Clone
        dst3 = dst1.Clone

        Dim center = New cv.Point(rect.Width / 2, rect.Height / 2)
        Dim angle45 = 45, angle90 = 90, scale = 1.0, h = rect.Height, w = rect.Width

        Dim m1 = cv.Cv2.GetRotationMatrix2D(center, angle45, scale)
        Dim m2 = cv.Cv2.GetRotationMatrix2D(center, angle90, scale)

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

        cv.Cv2.WarpAffine(dst1(rect), dst2(rect), m1, New cv.Size(bound_w, bound_h))

        For i = 0 To 6
            cv.Cv2.WarpAffine(dst2(rect), dst2(rect), m1, New cv.Size(bound_w, bound_h))
        Next

        cv.Cv2.WarpAffine(dst1(rect), dst3(rect), m2, New cv.Size(bound_w, bound_h))

        For i = 0 To 2
            cv.Cv2.WarpAffine(dst3(rect), dst3(rect), m2, New cv.Size(bound_w, bound_h))
        Next
        dst2.Rectangle(rect, white, task.lineWidth, task.lineType)
        dst3.Rectangle(rect, white, task.lineWidth, task.lineType)
    End Sub
End Class







' https://github.com/BhanuPrakashNani/Image_Processing/blob/master/Successive%20Rotations/rotation.py
Public Class WarpAffine_RepeatedExample8 : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Rotated repeatedly 45 degrees", "Rotated repeatedly 90 degrees"}
        desc = "Compare an image before and after repeated rotations."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim input = cv.Cv2.ImRead(task.HomeDir + "Data/8.jpg", cv.ImreadModes.Color)

        Dim center = New cv.Point(input.Width / 2, input.Height / 2)
        Dim angle45 = 45, angle90 = 90, scale = 1.0, h = input.Height, w = input.Width

        Dim m1 = cv.Cv2.GetRotationMatrix2D(center, angle45, scale)
        Dim m2 = cv.Cv2.GetRotationMatrix2D(center, angle90, scale)

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

        cv.Cv2.WarpAffine(input, dst2, m1, New cv.Size(bound_w, bound_h))

        For i = 0 To 6
            cv.Cv2.WarpAffine(dst2, dst2, m1, New cv.Size(bound_w, bound_h))
        Next

        cv.Cv2.WarpAffine(input, dst3, m2, New cv.Size(bound_w, bound_h))

        For i = 0 To 2
            cv.Cv2.WarpAffine(dst3, dst3, m2, New cv.Size(bound_w, bound_h))
        Next
    End Sub
End Class