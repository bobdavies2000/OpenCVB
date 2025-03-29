Imports VB_Classes.VBtask
Imports cv = OpenCvSharp
Public Class Visible_Lines : Inherits TaskParent
    Dim LRViews As New LeftRight_Lines
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
        desc = "Find lines that are visible in both the left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRViews.Run(src)
        dst2 = LRViews.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst1.SetTo(0)
        For i = LRViews.rightLines.Count - 1 To 0 Step -1
            Dim lp = LRViews.rightLines(i)
            dst1.Line(lp.p1, lp.p2, lp.index + 1, 2, cv.LineTypes.Link8) ' NOTE: added 1 to stay away from background 0
        Next

        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim leftCount As Integer, rightCount As Integer
        For Each lp In LRViews.leftLines
            If lp.depth = 0 Then Continue For
            Dim gc = task.gcList(lp.gcCenter)
            If lp.highlyVisible And gc.depth > 0 Then
                leftCount += 1
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)

                Dim pt As cv.Point2f = gc.center, index As Integer
                If task.rgbLeftAligned = False Then pt = translateColorToLeft(gc.center)
                pt.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / gc.depth
                index = dst1.Get(Of Integer)(pt.Y, pt.X)
                If index <> 0 Then
                    Dim lpR = LRViews.rightLines(index - 1) ' NOTE: subtract 1 - see above.
                    dst3.Line(lpR.p1, lpR.p2, task.highlight, task.lineWidth, task.lineType)
                    rightCount += 1
                End If
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(LRViews.leftLines.Count) + " lines present and " + CStr(leftCount) +
                        " were marked highly visible in the left and right images."
            labels(3) = CStr(LRViews.rightLines.Count) + " lines present and " + CStr(rightCount) +
                        " were highly visible in the right image."
        End If
    End Sub
End Class





Public Class Visible_LineCopy : Inherits TaskParent
    Dim LRViews As New LeftRight_Lines
    Public Sub New()
        desc = "Find lines that are highly visible in the left image and copy them to the right image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRViews.Run(src)
        dst2 = LRViews.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        'dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        'Dim leftCount As Integer, rightCount As Integer
        'For Each lp In LRViews.leftLines
        '    If lp.depth = 0 Then Continue For
        '    Dim gc = task.gcList(lp.gcCenter)
        '    If gc.correlation > threshold Then
        '        leftCount += 1
        '        dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)

        '        Dim pt As cv.Point2f = gc.center, index As Integer
        '        If task.rgbLeftAligned = False Then pt = translateColorToLeft(gc.center)
        '        pt.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / gc.depth
        '        index = dst1.Get(Of Integer)(pt.Y, pt.X)
        '        If index <> 0 Then
        '            Dim lpR = LRViews.rightLines(index - 1) ' NOTE: subtract 1 - see above.
        '            dst3.Line(lpR.p1, lpR.p2, task.highlight, task.lineWidth, task.lineType)
        '            rightCount += 1
        '        End If
        '    End If
        'Next
        'If task.heartBeat Then
        '    labels(2) = CStr(LRViews.leftLines.Count) + " lines present and " + CStr(leftCount) + " were highly visible in the left image."
        '    labels(3) = CStr(LRViews.rightLines.Count) + " lines present in the right image and " + CStr(rightCount) +
        '                " were highly visible in the right image."
        'End If
    End Sub
End Class
