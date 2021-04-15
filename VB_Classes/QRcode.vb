Imports cv = OpenCvSharp
Imports System.IO
Public Class QRcode_Basics : Inherits VBparent
    Dim qrDecoder As New cv.QRCodeDetector
    Dim qrInput1 As New cv.Mat
    Dim qrInput2 As New cv.Mat
    Public Sub New()
        Dim fileInfo = New FileInfo(task.parms.homeDir + "data/QRcode1.png")
        If fileInfo.Exists Then qrInput1 = cv.Cv2.ImRead(fileInfo.FullName)
        fileInfo = New FileInfo(task.parms.homeDir + "Data/QRCode2.png")
        If fileInfo.Exists Then qrInput2 = cv.Cv2.ImRead(fileInfo.FullName)
        If dst1.Width < 480 Then ' for the smallest configurations the default size can be too big!
            qrInput1 = qrInput1.Resize(New cv.Size(120, 160))
            qrInput2 = qrInput2.Resize(New cv.Size(120, 160))
        End If
        task.desc = "Read a QR code"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim x = msRNG.Next(0, src.Width - Math.Max(qrInput1.Width, qrInput2.Width))
        Dim y = msRNG.Next(0, src.Height - Math.Max(qrInput1.Height, qrInput2.Height))
        If CInt(task.frameCount / 50) Mod 2 = 0 Then
            Dim roi = New cv.Rect(x, y, qrInput1.Width, qrInput1.Height)
            src(roi) = qrInput1
        Else
            Dim roi = New cv.Rect(x, y, qrInput2.Width, qrInput2.Height)
            src(roi) = qrInput2
        End If

        Dim box() As cv.Point2f = Nothing
        Dim rectifiedImage As New cv.Mat
        Dim refersTo = qrDecoder.DetectAndDecode(src, box, rectifiedImage)

        src.CopyTo(dst1)
        For i = 0 To box.Length - 1
            dst1.Line(box(i), box((i + 1) Mod 4), cv.Scalar.Red, 3, task.lineType)
        Next
        If refersTo <> "" Then label1 = refersTo
    End Sub
End Class



