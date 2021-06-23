Imports cv = OpenCvSharp
Public Class IntrinsicsLeft_Basics : Inherits VBparent
    Public Sub New()
        task.desc = "Show the depth camera intrinsicsLeft."
        label2 = "ppx/ppy location"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.parms.intrinsicsLeft.coeffs Is Nothing Then
            setTrueText("This camera is missing the intrinsics.")
            Exit Sub
        End If
        Dim ttStart = 40
        Dim ttStr As String = "Width = " + CStr(src.Width) + vbTab + " height = " + CStr(src.Height) + vbCrLf
        ttStr += "fx = " + CStr(task.parms.intrinsicsLeft.fx) + vbTab + " fy = " + CStr(task.parms.intrinsicsLeft.fy) + vbCrLf
        ttStr += "ppx = " + CStr(task.parms.intrinsicsLeft.ppx) + vbTab + " ppy = " + CStr(task.parms.intrinsicsLeft.ppy) + vbCrLf + vbCrLf
        For i = 0 To task.parms.intrinsicsLeft.coeffs.Length - 1
            ttStr += "coeffs(" + CStr(i) + ") = " + Format(task.parms.intrinsicsLeft.coeffs(i), "#0.000") + "   " + vbCrLf
        Next
        If task.parms.intrinsicsLeft.FOV IsNot Nothing Then
            If task.parms.intrinsicsLeft.FOV(0) = 0 Then
                ttStr += "Approximate FOV in x = " + CStr(task.hFov) + vbCrLf +
                         "Approximate FOV in y = " + CStr(task.vFov) + vbCrLf
            Else
                ttStr += "FOV in x = " + CStr(task.parms.intrinsicsLeft.FOV(0)) + vbCrLf + "FOV in y = " + CStr(task.parms.intrinsicsLeft.FOV(1)) + vbCrLf
            End If
        Else
            ttStr += "Approximate FOV in x = " + CStr(task.hFov) + vbCrLf +
                     "Approximate FOV in y = " + CStr(task.vFov) + vbCrLf
        End If
        setTrueText(ttStr)

        dst3.SetTo(0)
        dst3.Line(New cv.Point(src.Width / 2, 0), New cv.Point(src.Width / 2, src.Height), cv.Scalar.White, task.lineWidth)
        dst3.Line(New cv.Point(0, src.Height / 2), New cv.Point(src.Width, src.Height / 2), cv.Scalar.White, task.lineWidth)

        Dim nextline = "(" + CStr(task.parms.intrinsicsLeft.ppx) + ", " + CStr(task.parms.intrinsicsLeft.ppy) + ")"
        Dim ttLocation = New cv.Point(CInt(src.Width / 2) + 20, CInt(src.Height / 2) + 40)
        cv.Cv2.PutText(dst3, nextline, ttLocation, task.font, task.fontSize, cv.Scalar.White, task.lineWidth, task.lineType)

        Dim ptLoc = New cv.Point(src.Width / 2 + 4, src.Height / 2 + 4)
        dst3.Line(ptLoc, ttLocation, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
    End Sub
End Class



