Imports cv = OpenCvSharp
' http://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf
Public Class Coherence_Basics : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Coherence Sigma", 1, 15, 9)
            sliders.setupTrackBar("Coherence Blend", 1, 10, 10)
            sliders.setupTrackBar("Coherence str_sigma", 1, 15, 15)
            sliders.setupTrackBar("Coherence eigen kernel", 1, 31, 1)
        End If
        labels(2) = "Coherence - draw rectangle to apply"
        desc = "Find lines that are artistically coherent in the image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static sigmaSlider = FindSlider("Coherence Sigma")
        Static blendSlider = FindSlider("Coherence Blend")
        Static strSlider = FindSlider("Coherence str_sigma")
        Static eigenSlider = FindSlider("Coherence eigen kernel")

        Dim sigma = sigmaSlider.Value * 2 + 1
        Dim blend As Single = blendSlider.Value / 10
        Dim str_sigma = strSlider.Value * 2 + 1
        Dim eigenKernelSize = eigenslider.Value * 2 + 1

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
        Dim xoffset = src.Width / 2 - side / 2
        Dim yoffset = src.Height / 2 - side / 2
        Dim srcRect = New cv.Rect(xoffset, yoffset, side, side)
        If task.drawRect.Width <> 0 Then srcRect = task.drawRect

        dst2 = src.Clone()
        src = src(srcRect)

        Dim gray As New cv.Mat
        Dim eigen As New cv.Mat
        Dim split() As cv.Mat
        For i = 0 To 3
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            eigen = gray.CornerEigenValsAndVecs(str_sigma, eigenKernelSize)
            split = eigen.Split()
            Dim x = split(2), y = split(3)

            Dim gxx = gray.Sobel(cv.MatType.CV_32F, 2, 0, sigma)
            Dim gxy = gray.Sobel(cv.MatType.CV_32F, 1, 1, sigma)
            Dim gyy = gray.Sobel(cv.MatType.CV_32F, 0, 2, sigma)

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

            Dim mask = gvv.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

            Dim erode = src.Erode(New cv.Mat)
            Dim dilate = src.Dilate(New cv.Mat)

            Dim imgl = erode
            dilate.CopyTo(imgl, mask)
            src = src * (1 - blend) + imgl * blend
        Next
        dst2(srcRect) = src
        dst2.Rectangle(srcRect, cv.Scalar.Yellow, 2)
        dst3.SetTo(0)
    End Sub
End Class







Public Class Coherence_Depth : Inherits VB_Parent
    Dim coherent As New Coherence_Basics
    Public Sub New()
        desc = "Find coherent lines in the depth image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        coherent.Run(task.depthRGB)
        dst2 = coherent.dst2
    End Sub
End Class
