Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Imports System.IO
Public Class QRcode_Basics : Inherits TaskParent
    Dim qrDecoder As New QRCodeDetector
    Dim qrInput1 As New Mat
    Dim qrInput2 As New Mat
    Public Sub New()
        Dim fileInfo = New FileInfo(task.homeDir + "data/QRcode1.png")
        If fileInfo.Exists Then qrInput1 = ImRead(fileInfo.FullName)
        fileInfo = New FileInfo(task.homeDir + "Data/QRCode2.png")
        If fileInfo.Exists Then qrInput2 = ImRead(fileInfo.FullName)
        If dst2.Width < 480 Then ' for the smallest configurations the default size can be too big!
            Resize(qrInput1, qrInput1, New Size(120, 160))
            Resize(qrInput2, qrInput2, New Size(120, 160))
        End If
        desc = "Read a QR code"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Height < 240 Then
            SetTrueText("This QR Code test does not run at low resolutions")
            Exit Sub
        End If
        Dim x = msRNG.Next(0, src.Width - Math.Max(qrInput1.Width, qrInput2.Width))
        Dim y = msRNG.Next(0, src.Height - Math.Max(qrInput1.Height, qrInput2.Height))
        If task.frameCount \ 50 Mod 2 = 0 Then
            Dim roi = New cv.Rect(x, y, qrInput1.Width, qrInput1.Height)
            src(roi) = qrInput1
        Else
            Dim roi = New cv.Rect(x, y, qrInput2.Width, qrInput2.Height)
            src(roi) = qrInput2
        End If

        Dim box() As Point2f = Nothing
        Dim rectifiedImage As New Mat
        Dim refersTo = qrDecoder.DetectAndDecode(src, box, rectifiedImage)

        src.CopyTo(dst2)
        For i = 0 To box.Length - 1
            Line(dst2, box(i), box((i + 1) Mod 4), Scalar.Red, task.lineWidth + 2, task.lineType)
        Next
        If refersTo <> "" Then labels(2) = refersTo
    End Sub
End Class
