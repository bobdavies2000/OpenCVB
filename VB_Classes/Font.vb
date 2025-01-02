Imports cv = OpenCvSharp
Public Class Font_OpenCV : Inherits TaskParent
    Public Sub New()
        desc = "Display different font options available in OpenCV"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If not task.heartBeat Then Exit Sub
        Dim hersheyFont = Choose(task.frameCount Mod 7 + 1, cv.HersheyFonts.HersheyComplex, cv.HersheyFonts.HersheyComplexSmall, cv.HersheyFonts.HersheyDuplex,
                                 cv.HersheyFonts.HersheyPlain, cv.HersheyFonts.HersheyScriptComplex, cv.HersheyFonts.HersheyScriptSimplex, cv.HersheyFonts.HersheySimplex,
                                 cv.HersheyFonts.HersheyTriplex, cv.HersheyFonts.Italic)
        Dim hersheyName = Choose(task.frameCount Mod 7 + 1, "HersheyComplex", "HersheyComplexSmall", "HersheyDuplex", "HersheyPlain", "HersheyScriptComplex",
                                 "HersheyScriptSimplex", "HersheySimplex", "HersheyTriplex", "Italic")
        labels(2) = hersheyName
        labels(3) = "Italicized " + hersheyName
        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 1 To 10
            Dim size = 1.5 - i * 0.1
            cv.Cv2.PutText(dst2, hersheyName + " " + Format(size, fmt1), New cv.Point(10, 30 + i * 30), hersheyFont, size, white, task.lineWidth, task.lineType)
            Dim hersheyFontItalics = hersheyFont + cv.HersheyFonts.Italic
            cv.Cv2.PutText(dst3, hersheyName + " " + Format(size, fmt1), New cv.Point(10, 30 + i * 30), hersheyFontItalics, size, white, task.lineWidth, task.lineType)
        Next
    End Sub
End Class



Public Class Font_FlowTextOld : Inherits TaskParent
    Public msgs As New List(Of String)
    Public dst As Integer = 2
    Public Sub New()
        desc = "Show TrueType text flowing through an image."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            msgs.Add("-------------------------------------------------------------------------------------------------------------------")
            msgs.Add("To get text to flow across an image in any algorithm, add 'flow = new Font_FlowText()' to the class constructor.")
            msgs.Add("Also optionally indicate if you want result1 or result2 for text (the default is result1.)")
            msgs.Add("Then in your Run method, add a line 'flow.msgs.add('your next line of text')' - for as many msgs as you need on each pass.")
            msgs.Add("Then at the end of your Run method, invoke flow.Run(src)")
        End If

        Dim maxLines = 31
        If task.dst2.Height = 720 Or task.dst2.Height = 360 Or task.dst2.Height = 180 Then maxLines = 23
        Dim clearRequested As Boolean
        If msgs.Count > maxLines Then
            If msgs.Count < maxLines * 2 Then
                For i = 0 To msgs.Count - maxLines - 1
                    msgs.RemoveAt(0)
                Next
            Else
                clearRequested = True
            End If
        End If

        strOut = ""
        For i = 0 To Math.Min(maxLines, msgs.Count) - 1
            strOut += msgs(i) + vbCrLf
        Next
        SetTrueText(strOut, dst)
        If clearRequested Then msgs.Clear()
    End Sub
End Class









Public Class Font_FlowText : Inherits TaskParent
    Public flowText As New List(Of String)
    Public nextMsg As String
    Public maxLines As Integer = 23
    Public dst As Integer = 2
    Public textResult As New List(Of TrueText)
    Public parentData As Object
    Public Sub New()
        desc = "Show TrueType text flowing through an image."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            strOut = "-------------------------------------------------------------------------------------------------------------------" + vbCrLf
            strOut += "To get text to flow across an image in any algorithm, add Font_FlowText to your algorithm." + vbCrLf
            strOut += "Also optionally indicate if you want result1 or result2 for text (the default is result1.)" + vbCrLf
            strOut += "NOTE: add 'flow.parentData = me to your constructor for the algorithm." + vbCrLf
            strOut += "Then in your Run method, add a line 'flow.nextMsg = 'your next line of text'" + vbCrLf
            strOut += "Then at the end of your Run method, invoke flow.Run(src)" + vbCrLf
        Else
            flowText.Add(nextMsg)
            If flowText.Count > maxLines Then flowText.RemoveAt(0)

            strOut = ""
            For Each txt In flowText
                strOut += txt + vbCrLf
            Next
        End If
        SetTrueText(strOut, dst)
        If standalone = False Then parentData.trueData = trueData
    End Sub
End Class