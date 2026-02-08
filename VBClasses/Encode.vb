Imports cv = OpenCvSharp
' https://www.programcreek.com/python/example/70396/cv2.imencode
Namespace VBClasses
    Public Class NR_Encode_Basics : Inherits TaskParent
        Dim options As New Options_Encode
        Public Sub New()
            desc = "Error Level Analysis - to verify a jpg image has not been modified."
            labels(2) = "absDiff with original"
            labels(3) = "Original decompressed"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If tsk.firstPass Then OptionParent.FindSlider("Encode Output Scaling").Value = 10

            Dim encodeParams() As Integer = {options.encodeOption, options.qualityLevel}
            Dim buf() = src.ImEncode(".jpg", encodeParams)
            Dim image = cv.Mat.FromPixelData(buf.Count, 1, cv.MatType.CV_8U, buf)
            dst3 = cv.Cv2.ImDecode(image, cv.ImreadModes.AnyColor)

            Dim output As New cv.Mat
            cv.Cv2.Absdiff(src, dst3, output)

            output.ConvertTo(dst2, cv.MatType.CV_8UC3, options.scalingLevel)
            Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
            labels(3) = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.0%") + ")"
        End Sub
    End Class





    ' https://answers.opencvb.org/question/31519/encode-image-in-jpg-with-opencv-avoiding-the-artifacts-effect/
    Public Class NR_Encode_Scaling : Inherits TaskParent
        Dim options As New Options_Encode
        Public Sub New()
            desc = "JPEG Encoder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim encodeParams() As Integer = {options.encodeOption, options.qualityLevel}

            Dim buf() = src.ImEncode(".jpg", encodeParams)
            Dim image = cv.Mat.FromPixelData(buf.Count, 1, cv.MatType.CV_8U, buf)
            dst3 = cv.Cv2.ImDecode(image, cv.ImreadModes.AnyColor)

            Dim output As New cv.Mat
            cv.Cv2.Absdiff(src, dst3, output)

            output.ConvertTo(dst2, cv.MatType.CV_8UC3, options.scalingLevel)
            Dim compressionRatio = buf.Length / (src.Rows * src.Cols * src.ElemSize)
        End Sub
    End Class
End Namespace