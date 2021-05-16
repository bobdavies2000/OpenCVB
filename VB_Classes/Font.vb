Imports cv = OpenCvSharp
Public Class Font_OpenCV : Inherits VBparent
    Public Sub New()
        task.desc = "Display different font options available in OpenCV"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod 30 Then Exit Sub
        Dim hersheyFont = Choose(task.frameCount Mod 7 + 1, cv.HersheyFonts.HersheyComplex, cv.HersheyFonts.HersheyComplexSmall, cv.HersheyFonts.HersheyDuplex,
                                 cv.HersheyFonts.HersheyPlain, cv.HersheyFonts.HersheyScriptComplex, cv.HersheyFonts.HersheyScriptSimplex, cv.HersheyFonts.HersheySimplex,
                                 cv.HersheyFonts.HersheyTriplex, cv.HersheyFonts.Italic)
        Dim hersheyName = Choose(task.frameCount Mod 7 + 1, "HersheyComplex", "HersheyComplexSmall", "HersheyDuplex", "HersheyPlain", "HersheyScriptComplex",
                                 "HersheyScriptSimplex", "HersheySimplex", "HersheyTriplex", "Italic")
        label1 = hersheyName
        label2 = "Italicized " + hersheyName
        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 1 To 10
            Dim size = 1.5 - i * 0.1
            cv.Cv2.PutText(dst1, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFont, size, cv.Scalar.White, task.lineWidth, task.lineType)
            Dim hersheyFontItalics = hersheyFont + cv.HersheyFonts.Italic
            cv.Cv2.PutText(dst2, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFontItalics, size, cv.Scalar.White, task.lineWidth, task.lineType)
        Next
    End Sub
End Class




Public Class Font_TrueType : Inherits VBparent
    Public Sub New()
        task.desc = "Display different TrueType fonts"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gfontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        Dim fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        ' get the font on every iteration because it could have changed.  This should be done in any algorithm using OptionsFont.
        setTrueText("TrueType Font is currently set to " + fontName + " with size = " + CStr(gfontSize) + vbCrLf +
                      "Use the Settings button above to change the font name and size.")
    End Sub
End Class




Public Class Font_FlowText : Inherits VBparent
    Public msgs As New List(Of String)
    Public dst As Integer = RESULT1 ' set to result2 to appear in dst2
    Public maxLineCount = 22
    Public Sub New()
        If dst1.Height = 480 Then maxLineCount = 26
        task.desc = "Show TrueType text flowing through an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then
            msgs.Add("-------------------------------------------------------------------------------------------------------------------")
            msgs.Add("To get text to flow across an image in any algorithm, add 'flow = new Font_FlowText()' to the class constructor.")
            msgs.Add("Also optionally indicate if you want result1 or result2 for text (the default is result1.)")
            msgs.Add("Then in your Run method, add a line 'flow.msgs.add('your next line of text')' - for as many msgs as you need on each pass.")
            msgs.Add("Then at the end of your Run method, invoke flow.Run(Nothing)")
        End If
        Static lastCount As Integer

        Dim maxlines = 22
        If dst1.Height = 480 Then maxlines = 28
        Dim firstLine = If(msgs.Count - maxlines < 0, 0, msgs.Count - maxlines)
        Dim fullText As String = ""
        For i = firstLine To msgs.Count - 1
            fullText += msgs(i) + vbCrLf
        Next
        setTrueText(fullText, 10, 20, dst)

        If msgs.Count >= maxlines Then
            Try
                Dim index As Integer
                For i = 0 To lastCount - maxlines - 1
                    msgs.RemoveAt(index)
                    index += 1
                Next
            Catch ex As Exception
                msgs.Clear()
            End Try
        End If
        lastCount = msgs.Count
    End Sub
End Class


