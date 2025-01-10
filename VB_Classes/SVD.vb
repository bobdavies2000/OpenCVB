Imports cv = OpenCvSharp
' https://answers.opencvb.org/question/200080/parameters-of-cvsvdecomp/
Public Class SVD_Example : Inherits TaskParent
    Public Sub New()
        desc = "SVD example"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Dim inputData() As Single = {
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5,
            1, 2, 3, 4, 5
        }

        src = cv.Mat.FromPixelData(5, 5, cv.MatType.CV_32F, inputData)
        Dim W As New cv.Mat, U As New cv.Mat, VT As New cv.Mat

        cv.Cv2.SVDecomp(src, W, U, VT, cv.SVD.Flags.FullUV)

        Dim WD As New cv.Mat(5, 5, cv.MatType.CV_32F, cv.Scalar.All(0))
        W.CopyTo(WD.Diag)

        Dim rec As cv.Mat = VT.T * WD * U.T
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
    Public Overrides sub runAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        Dim rc = task.rc

        If task.heartBeat Then
            Dim m = cv.Cv2.Moments(rc.mask, True)
            Dim center = New cv.Point2f(m.M10 / rc.pixels, m.M01 / rc.pixels)
            DrawCircle(task.color(rc.rect), center, task.DotSize, task.HighlightColor)

            Dim mArea = cv.Mat.FromPixelData(4, 1, cv.MatType.CV_32F, {m.M20 / rc.pixels, m.Mu11 / rc.pixels, m.Mu11 / rc.pixels, m.Mu02 / rc.pixels})
            Dim U As New cv.Mat
            cv.Cv2.SVDecomp(mArea, New cv.Mat, U, New cv.Mat, cv.SVD.Flags.FullUV)


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
    Public Overrides sub runAlg(src As cv.Mat)
        covar.Run(src)
        dst2 = src

        Dim U As New cv.Mat, W As New cv.Mat, VT As New cv.Mat
        cv.Cv2.SVDecomp(covar.covariance, W, U, VT, cv.SVD.Flags.FullUV)

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

        Dim angle = -Math.Atan2(U.Get(Of Double)(0, 1), U.Get(Of Double)(0, 0)) * (180 / cv.Cv2.PI)
        strOut += "Angle = " + Format(angle, fmt3) + " radians" + vbCrLf

        W = W.Sqrt() * 3
        Dim size = New cv.Size2f(10, 100) ' New cv.Size2f(W.Get(Of Double)(0, 0), W.Get(Of Double)(1, 0))
        Dim pt = New cv.Point2f(covar.mean.Get(Of Double)(0, 0), covar.mean.Get(Of Double)(0, 1))
        Dim rrect = New cv.RotatedRect(pt, size, angle)
        dst2.Ellipse(rrect, task.HighlightColor, task.lineWidth, task.lineType)

        SetTrueText(strOut, 3)
    End Sub
End Class