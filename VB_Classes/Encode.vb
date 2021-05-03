Imports cv = OpenCvSharp
' https://www.programcreek.com/python/example/70396/cv2.imencode
Public Class Encode_Basics : Inherits VBparent
    Dim options As New Encode_Options
    Public Sub New()
        task.desc = "Error Level Analysis - to verify a jpg image has not been modified."
        label1 = "absDiff with original"
        label2 = "Original decompressed"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim encodeParams() As Integer = {options.getEncodeParameter(), options.qualityLevel}

        Dim buf() = src.ImEncode(".jpg", encodeParams)
        Dim image = New cv.Mat(buf.Count, 1, cv.MatType.CV_8U, buf)
        dst2 = cv.Cv2.ImDecode(image, cv.ImreadModes.AnyColor)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, output)

        Static scaleSlider = findSlider("Encode Output Scaling")
        If task.frameCount = 0 Then scaleSlider.value = 10

        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scaleSlider.Value)
        Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.0%") + ")"
    End Sub
End Class



' https://answers.opencv.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
Public Class Encode_Options : Inherits VBparent
    Public qualityLevel As Integer
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
            sliders.setupTrackBar(1, "Encode Output Scaling", 1, 100, 85)
        End If
        If radio.Setup(caller, 6) Then
            radio.check(0).Text = "JpegChromaQuality"
            radio.check(1).Text = "JpegLumaQuality"
            radio.check(2).Text = "JpegOptimize"
            radio.check(3).Text = "JpegProgressive"
            radio.check(4).Text = "JpegQuality"
            radio.check(5).Text = "WebPQuality"
            radio.check(1).Checked = True
        End If

        task.desc = "Encode options that affect quality."
        label1 = "absDiff with original image"
    End Sub
    Public Function getEncodeParameter() As Integer
        Static qualitySlider = findSlider("Encode Quality Level")
        Static frm = findfrm(caller + " Radio Options")
        Dim encodeOption As Integer
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next
        qualityLevel = qualitySlider.Value
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then qualityLevel = 1 ' just on or off
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then qualityLevel = 1 ' just on or off
        Return encodeOption
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim fileExtension = ".jpg"
        Dim encodeParams() As integer = {getEncodeParameter(), qualityLevel}

        Dim buf() = src.ImEncode(".jpg", encodeParams)
        Dim image = New cv.Mat(buf.Count, 1, cv.MatType.CV_8U, buf)
        dst2 = cv.Cv2.ImDecode(image, cv.ImreadModes.AnyColor)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, output)

        Dim scale = sliders.trackbar(1).Value
        output.ConvertTo(dst1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
        label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.0%") + ")"
    End Sub
End Class



