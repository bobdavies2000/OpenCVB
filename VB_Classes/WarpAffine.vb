Imports cv = OpenCvSharp
' http://opencvexamples.blogspot.com/
Public Class WarpAffine_Captcha : Inherits VBparent
    Const charHeight = 100
    Const charWidth = 80
    Const captchaLength = 8
    Dim rng As New System.Random
    Public Sub New()
        task.desc = "Use OpenCV to build a captcha Turing test."
    End Sub
    Private Sub addNoise(image As cv.Mat)
        For n = 0 To 100
            Dim i = rng.Next(0, image.Cols - 1)
            Dim j = rng.Next(0, image.Rows - 1)
            Dim center = New cv.Point(i, j)
            Dim c = New cv.Scalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255))
            image.Circle(center, rng.Next(1, 3), c, -1, task.lineType)
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
        cv.Cv2.WarpAffine(input, output, rotationMatrix, input.Size(), cv.InterpolationFlags.Linear, cv.BorderTypes.Constant, cv.Scalar.White)
    End Sub
    Private Sub transformPerspective(ByRef charImage As cv.Mat)
        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, charHeight), New cv.Point2f(charWidth, 0), New cv.Point2f(charWidth, charHeight)}

        Dim varWidth = charWidth / 2
        Dim varHeight = charHeight / 2
        Dim widthWarp = charHeight - varWidth + rng.NextDouble() * varWidth
        Dim heightWarp = charHeight - varHeight + rng.NextDouble() * varHeight

        Dim dstPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, charHeight), New cv.Point2f(charWidth, 0), New cv.Point2f(widthWarp, heightWarp)}

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(srcPt, dstPt)
        cv.Cv2.WarpPerspective(charImage, charImage, perpectiveTranx, New cv.Size(charImage.Cols, charImage.Rows), cv.InterpolationFlags.Cubic,
                               cv.BorderTypes.Constant, cv.Scalar.White)
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim characters() As String = {"a", "A", "b", "B", "c", "C", "D", "d", "e", "E", "f", "F", "g", "G", "h", "H", "j", "J", "k", "K", "m", "M", "n", "N", "q", "Q", "R", "t", "T", "w", "W", "x", "X", "y", "Y", "1", "2", "3", "4", "5", "6", "7", "8", "9"}
        Dim charactersSize = characters.Length / characters(0).Length

        Dim outImage As New cv.Mat(charHeight, charWidth * captchaLength, cv.MatType.CV_8UC3, cv.Scalar.White)

        For i = 0 To captchaLength - 1
            Dim charImage = New cv.Mat(charHeight, charWidth, cv.MatType.CV_8UC3, cv.Scalar.White)
            Dim c = characters(rng.Next(0, characters.Length - 1))
            cv.Cv2.PutText(charImage, c, New cv.Point(10, charHeight - 10), msRNG.Next(1, 6), msRNG.Next(3, 4), task.vecColors(i), msRNG.Next(1, 5),
                           task.lineType)
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








' http://opencvexamples.blogspot.com/
Public Class WarpAffine_Basics : Inherits VBparent
    Public rotateOptions As New Resize_Options
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Angle", 0, 360, 10)
        End If

        task.desc = "Use WarpAffine to transform input images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frm = findfrm("WarpAffine_Basics Radio Options")

        rotateOptions.Run(src)

        Dim pt = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        Dim angle = sliders.trackbar(0).Value
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(pt, angle, 1.0)
        cv.Cv2.WarpAffine(src, dst2, rotationMatrix, src.Size(), rotateOptions.warpFlag)
        angle *= -1
        rotationMatrix = cv.Cv2.GetRotationMatrix2D(pt, angle, 1.0)
        cv.Cv2.WarpAffine(dst2, dst3, rotationMatrix, src.Size(), rotateOptions.warpFlag)
        label1 = "Rotated with Warpaffine with angle: " + CStr(angle)
        label2 = "Rotated back with inverse Warpaffine angle: " + CStr(-angle)
    End Sub
End Class








' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_imgproc/py_geometric_transformations/py_geometric_transformations.html
Public Class WarpAffine_3Points : Inherits VBparent
    Dim triangle As Area_MinTriangle_CPP
    Public Sub New()
        triangle = New Area_MinTriangle_CPP()
        triangle.sliders.trackbar(0).Value = 20
        triangle.sliders.trackbar(1).Value = 150

        task.desc = "Use 3 non-colinear points to build an affine transform and apply it to the color image."
        label1 = "Triangles define the affine transform"
        label2 = "Image with affine transform applied"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static M As New cv.Mat
        If task.frameCount Mod 60 = 0 Then
            Dim triangles(1) As cv.Mat
            triangle.Run(src)
            triangles(0) = triangle.triangle.Clone()
            Dim srcPoints1 = triangle.srcPoints.Clone()
            triangle.Run(src)
            triangles(1) = triangle.triangle.Clone()
            Dim srcPoints2 = triangle.srcPoints.Clone()

            Dim tOriginal = New cv.Mat(3, 1, cv.MatType.CV_32FC2, New Single() {0, 0, 0, src.Height, src.Width, src.Height})
            M = cv.Cv2.GetAffineTransform(tOriginal, triangles(1))

            Dim wideMat = New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3, 0)
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
                        wideMat.Line(p1, p3, cv.Scalar.White, task.lineWidth, task.lineType)
                    End If
                Next
            Next

            Dim corner = triangles(0).Get(Of cv.Point2f)(0)
            wideMat.Circle(corner, task.dotSize + 5, cv.Scalar.Yellow, -1, task.lineType)
            corner = New cv.Point2f(M.Get(Of Double)(0, 2) + src.Width, M.Get(Of Double)(1, 2))
            wideMat.Circle(corner, task.dotSize + 5, cv.Scalar.Yellow, -1, task.lineType)

            dst2 = wideMat(New cv.Rect(0, 0, src.Width, src.Height))
            dst3 = wideMat(New cv.Rect(src.Width, 0, src.Width, src.Height))

            Dim pt As cv.Point
            For i = 0 To srcPoints1.Length - 1
                pt = New cv.Point(CInt(srcPoints1(i).x), CInt(srcPoints1(i).y))
                dst2.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
                pt = New cv.Point(CInt(srcPoints2(i).x), CInt(srcPoints2(i).y))
                dst3.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            Next
        End If
        setTrueText("M defined as: " + vbCrLf +
                      Format(M.Get(Of Double)(0, 0), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(0, 1), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(0, 2), "#0.00") + vbCrLf +
                      Format(M.Get(Of Double)(1, 0), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(1, 1), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(1, 2), "#0.00"))
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_imgproc/py_geometric_transformations/py_geometric_transformations.html
Public Class WarpAffine_4Points : Inherits VBparent
    Dim rect As Area_MinRect
    Public Sub New()
        rect = New Area_MinRect()

        task.desc = "Use 4 non-colinear points to build a perspective transform and apply it to the color image."
        label1 = "Color image with perspective transform applied"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static M As New cv.Mat
        If task.frameCount Mod 60 = 0 Then

            Dim roi = New cv.Rect(50, src.Height / 2, src.Width / 6, src.Height / 6)
            Dim smallImage = src.Resize(New cv.Size(roi.Width, roi.Height))
            Dim rectangles(1) As cv.RotatedRect
            rect.Run(src)
            rectangles(1) = rect.minRect
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
                        dst2.Line(p1, p3, cv.Scalar.White, task.lineWidth, task.lineType)
                    End If
                    Dim color = Choose(i + 1, cv.Scalar.Red, cv.Scalar.White, cv.Scalar.Yellow, cv.Scalar.Green)
                    dst2.Line(p1, p2, color, task.lineWidth + 3, task.lineType)
                Next
            Next
        End If

        setTrueText("M defined as: " + vbCrLf +
                      Format(M.Get(Of Double)(0, 0), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(0, 1), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(0, 2), "#0.00") + vbCrLf +
                      Format(M.Get(Of Double)(1, 0), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(1, 1), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(1, 2), "#0.00") + vbCrLf +
                      Format(M.Get(Of Double)(2, 0), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(2, 1), "#0.00") + vbTab +
                      Format(M.Get(Of Double)(2, 2), "#0.00") + vbCrLf)
        Dim center As New cv.Point2f(M.Get(Of Double)(0, 2), M.Get(Of Double)(1, 2))
        dst2.Circle(center, task.dotSize + 5, cv.Scalar.Yellow, -1, task.lineType)
        center = New cv.Point2f(50, src.Height / 2)
        dst2.Circle(center, task.dotSize + 5, cv.Scalar.Yellow, -1, task.lineType)
    End Sub
End Class



