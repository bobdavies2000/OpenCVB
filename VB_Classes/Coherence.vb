Imports cvb = OpenCvSharp
' http://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf
Public Class Coherence_Basics : Inherits VB_Parent
    Dim options As New Options_Coherence
    Public Sub New()
        labels(2) = "Coherence - draw rectangle to apply"
        desc = "Find lines that are artistically coherent in the image"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

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
        Dim srcRect = New cvb.Rect(xoffset, yoffset, side, side)
        If task.drawRect.Width <> 0 Then srcRect = task.drawRect

        dst2 = src.Clone()
        src = src(srcRect)

        Dim gray As New cvb.Mat
        Dim eigen As New cvb.Mat
        Dim split() As cvb.Mat
        For i = 0 To 3
            gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            eigen = gray.CornerEigenValsAndVecs(options.str_sigma, options.eigenkernelsize)
            split = eigen.Split()
            Dim x = split(2), y = split(3)

            Dim gxx = gray.Sobel(cvb.MatType.CV_32F, 2, 0, options.sigma)
            Dim gxy = gray.Sobel(cvb.MatType.CV_32F, 1, 1, options.sigma)
            Dim gyy = gray.Sobel(cvb.MatType.CV_32F, 0, 2, options.sigma)

            Dim tmpX As New cvb.Mat, tmpXY As New cvb.Mat, tmpY As New cvb.Mat
            cvb.Cv2.Multiply(x, x, tmpX)
            cvb.Cv2.Multiply(tmpX, gxx, tmpX)
            cvb.Cv2.Multiply(x, y, tmpXY)
            cvb.Cv2.Multiply(tmpXY, gxy, tmpXY)
            tmpXY.Mul(tmpXY, 2)

            cvb.Cv2.Multiply(y, y, tmpY)
            cvb.Cv2.Multiply(tmpY, gyy, tmpY)

            Dim gvv As New cvb.Mat
            gvv = tmpX + tmpXY + tmpY

            Dim mask = gvv.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv).ConvertScaleAbs()

            Dim erode = src.Erode(New cvb.Mat)
            Dim dilate = src.Dilate(New cvb.Mat)

            Dim imgl = erode
            dilate.CopyTo(imgl, mask)
            src = src * (1 - options.blend) + imgl * options.blend
        Next
        dst2(srcRect) = src
        dst2.Rectangle(srcRect, cvb.Scalar.Yellow, 2)
        dst3.SetTo(0)
    End Sub
End Class







Public Class Coherence_Depth : Inherits VB_Parent
    Dim coherent As New Coherence_Basics
    Public Sub New()
        desc = "Find coherent lines in the depth image"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        coherent.Run(task.depthRGB)
        dst2 = coherent.dst2
    End Sub
End Class
