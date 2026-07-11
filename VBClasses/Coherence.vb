Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' http://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf
Public Class Coherence_Basics : Inherits TaskParent
    Dim options As New Options_Coherence
    Public Sub New()
        labels(2) = "Coherence - draw rectangle to apply"
        desc = "Find lines that are artistically coherent in the image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim side As Integer
        Select Case src.Height
            Case 120, 180
                side = 64
            Case 360, 480
                side = 256
            Case 720
                side = 512
            Case Else
                side = 50
        End Select
        If src.Channels <> 1 Then src = task.gray
        Dim xoffset = src.Width / 2 - side / 2
        Dim yoffset = src.Height / 2 - side / 2
        Dim srcRect = New cv.Rect(xoffset, yoffset, side, side)
        If task.drawRect.Width <> 0 Then srcRect = task.drawRect

        dst2 = src.Clone()
        src = src(srcRect)

        Dim eigen As New cv.Mat
        Dim split() As cv.Mat
        For i = 0 To 3
            cv.Cv2.CornerEigenValsAndVecs(src, eigen, options.str_sigma, options.eigenkernelsize)
            split = cv.Cv2.Split(eigen)
            Dim x = split(2), y = split(3)

            Dim gxx As New cv.Mat, gxy As New cv.Mat, gyy As New cv.Mat
            cv.Cv2.Sobel(src, gxx, cv.MatType.CV_32F, 2, 0, options.sigma)
            cv.Cv2.Sobel(src, gxy, cv.MatType.CV_32F, 1, 1, options.sigma)
            cv.Cv2.Sobel(src, gyy, cv.MatType.CV_32F, 0, 2, options.sigma)

            Dim tmpX As New cv.Mat, tmpXY As New cv.Mat, tmpY As New cv.Mat
            cv.Cv2.Multiply(x, x, tmpX)
            cv.Cv2.Multiply(tmpX, gxx, tmpX)
            cv.Cv2.Multiply(x, y, tmpXY)
            cv.Cv2.Multiply(tmpXY, gxy, tmpXY)
            tmpXY.Mul(tmpXY, 2)

            cv.Cv2.Multiply(y, y, tmpY)
            cv.Cv2.Multiply(tmpY, gyy, tmpY)

            Dim gvv As New cv.Mat
            gvv = tmpX + tmpXY + tmpY

            Dim mask As New cv.Mat
            cv.Cv2.Threshold(gvv, mask, 0, 255, cv.ThresholdTypes.BinaryInv)
            cv.Cv2.ConvertScaleAbs(mask, mask)

            Dim erode As New cv.Mat
            cv.Cv2.Erode(src, erode, New cv.Mat)
            Dim dilate As New cv.Mat
            cv.Cv2.Dilate(src, dilate, New cv.Mat)

            Dim imgl = erode
            dilate.CopyTo(imgl, mask)
            src = src * (1 - options.blend) + imgl * options.blend
        Next
        dst2(srcRect) = src
        cv.Cv2.Rectangle(dst2, srcRect, cv.Scalar.Yellow, 2)
        dst3.SetTo(0)
    End Sub
End Class







Public Class XR_Coherence_Depth : Inherits TaskParent
    Dim coherent As New Coherence_Basics
    Public Sub New()
        desc = "Find coherent lines in the depth image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        coherent.Run(task.depthRGB)
        dst2 = coherent.dst2
    End Sub
End Class
