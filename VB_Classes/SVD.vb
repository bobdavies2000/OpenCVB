Imports cvb = OpenCvSharp
' https://answers.opencvb.org/question/200080/parameters-of-cvsvdecomp/
Public Class SVD_Example : Inherits TaskParent
    Public Sub New()
        desc = "SVD example"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim inputData() As Single = {
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5
        }

        src = cvb.Mat.FromPixelData(5, 5, cvb.MatType.CV_32F, inputData)
        Dim W As New cvb.Mat, U As New cvb.Mat, VT As New cvb.Mat

        cvb.Cv2.SVDecomp(src, W, U, VT, cvb.SVD.Flags.FullUV)

        Dim WD As New cvb.Mat(5, 5, cvb.MatType.CV_32F, cvb.Scalar.All(0))
        W.CopyTo(WD.Diag)

        Dim rec As cvb.Mat = VT.T * WD * U.T
        strOut = ""
        For i = 0 To rec.Rows - 1
            For j = 0 To rec.Cols - 1
                strOut += Format(rec.Get(Of Single)(i, j), fmt3) + ", "
            Next
            strOut += vbCrLf
        Next

        SetTrueText(strOut)
    End Sub
End Class







' https://www.programcreek.com/python/example/89344/cv2.SVDecomp
' https://github.com/mzucker/page_dewarp/blob/master/page_dewarp.py
Public Class SVD_Example2 : Inherits TaskParent
    Public Sub New()
        desc = "Compute the mean and tangent of a RedCloud Cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        Dim rc = task.rc

        If task.heartBeat Then
            Dim m = cvb.Cv2.Moments(rc.mask, True)
            Dim center = New cvb.Point2f(m.M10 / rc.pixels, m.M01 / rc.pixels)
            DrawCircle(task.color(rc.rect), center, task.DotSize, task.HighlightColor)

            Dim mArea = cvb.Mat.FromPixelData(4, 1, cvb.MatType.CV_32F, {m.M20 / rc.pixels, m.Mu11 / rc.pixels, m.Mu11 / rc.pixels, m.Mu02 / rc.pixels})
            Dim U As New cvb.Mat
            cvb.Cv2.SVDecomp(mArea, New cvb.Mat, U, New cvb.Mat, cvb.SVD.Flags.FullUV)


            strOut = "The U Mat: " + vbCrLf
            For j = 0 To U.Rows - 1
                For i = 0 To U.Cols - 1
                    strOut += Format(U.Get(Of Single)(j, i), fmt3) + ", "
                Next
                strOut += vbCrLf
            Next
            strOut += vbCrLf

            strOut += "The tangent: " + vbCrLf
            For i = 0 To U.Cols - 1
                strOut += Format(U.Get(Of Single)(0, i), fmt3) + ", "
            Next
            strOut += vbCrLf

            Dim angle = Math.Atan2(U.Get(Of Single)(0, 1), U.Get(Of Single)(0, 0))
            strOut += "Angle = " + Format(angle, fmt3) + " radians" + vbCrLf

            strOut += "Center.X = " + Format(center.X, fmt2) + " Center.Y = " + Format(center.Y, fmt2) + vbCrLf

            strOut += "Rect is at (" + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ") with width/height = " + CStr(rc.rect.Width) + "/" + CStr(rc.rect.Height) + vbCrLf
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







' https://www.programcreek.com/python/example/89344/cv2.SVDecomp
Public Class SVD_Gaussian : Inherits TaskParent
    Dim covar As New Covariance_Images
    Public Sub New()
        desc = "Compute the SVD for the covariance of 2 images - only close to working..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        covar.Run(src)
        dst2 = src

        Dim U As New cvb.Mat, W As New cvb.Mat, VT As New cvb.Mat
        cvb.Cv2.SVDecomp(covar.covariance, W, U, VT, cvb.SVD.Flags.FullUV)

        strOut = "The Covariance Mat: " + vbCrLf
        For j = 0 To covar.covariance.Rows - 1
            For i = 0 To covar.covariance.Cols - 1
                strOut += Format(covar.covariance.Get(Of Double)(j, i), fmt3) + ", "
            Next
            strOut += vbCrLf
        Next
        strOut += vbCrLf

        strOut += "The W Mat: " + vbCrLf
        For j = 0 To W.Rows - 1
            For i = 0 To W.Cols - 1
                strOut += Format(W.Get(Of Double)(j, i), fmt3) + ", "
            Next
            strOut += vbCrLf
        Next
        strOut += vbCrLf

        strOut += "The U Mat: " + vbCrLf
        For j = 0 To U.Rows - 1
            For i = 0 To U.Cols - 1
                strOut += Format(U.Get(Of Double)(j, i), fmt3) + ", "
            Next
            strOut += vbCrLf
        Next
        strOut += vbCrLf

        Dim angle = -Math.Atan2(U.Get(Of Double)(0, 1), U.Get(Of Double)(0, 0)) * (180 / cvb.Cv2.PI)
        strOut += "Angle = " + Format(angle, fmt3) + " radians" + vbCrLf

        W = W.Sqrt() * 3
        Dim size = New cvb.Size2f(10, 100) ' New cvb.Size2f(W.Get(Of Double)(0, 0), W.Get(Of Double)(1, 0))
        Dim pt = New cvb.Point2f(covar.mean.Get(Of Double)(0, 0), covar.mean.Get(Of Double)(0, 1))
        Dim rrect = New cvb.RotatedRect(pt, size, angle)
        dst2.Ellipse(rrect, task.HighlightColor, task.lineWidth, task.lineType)

        SetTrueText(strOut, 3)
    End Sub
End Class