Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/de/dd0/grabcut_8cpp-example.html
Public Class GrabCut_Basics : Inherits VBparent
    Dim fgnd As Depth_Foreground
    Public fgFineTune As cv.Mat
    Public bgFineTune As cv.Mat
    Public Sub New()
        fgnd = New Depth_Foreground

        label1 = "Foreground from depth data"
        label2 = "Foreground after GrabCut using mask in dst1"
        task.desc = "Use grabcut with just a foreground and background definition."
    End Sub
    Public Sub Run(src as cv.Mat)
        fgnd.Run(src)
        dst1 = fgnd.dst1

        Dim fg = dst1.Threshold(1, cv.GrabCutClasses.FGD, cv.ThresholdTypes.Binary)
        Dim bg = dst1.Threshold(1, cv.GrabCutClasses.BGD, cv.ThresholdTypes.BinaryInv)

        Dim mask As New cv.Mat
        cv.Cv2.BitwiseOr(bg, fg, mask)

        If fgFineTune IsNot Nothing Then mask.SetTo(cv.GrabCutClasses.FGD, fgFineTune)
        If bgFineTune IsNot Nothing Then mask.SetTo(cv.GrabCutClasses.BGD, bgFineTune)

        Static bgModel As New cv.Mat, fgModel As New cv.Mat
        Dim rect As New cv.Rect
        If fg.CountNonZero() > 100 And bg.CountNonZero() > 100 Then
            cv.Cv2.GrabCut(src, mask, rect, bgModel, fgModel, 1, cv.GrabCutModes.InitWithMask)
        End If
        dst2.SetTo(0)
        src.CopyTo(dst2, mask)
    End Sub
End Class









Public Class GrabCut_FineTune : Inherits VBparent
    Dim basics As GrabCut_Basics
    Dim mats as New Mat_4to1
    Public Sub New()
        basics = New GrabCut_Basics

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Selected rectangle is added to the foreground"
            radio.check(1).Text = "Selected rectangle is added to the background"
            radio.check(2).Text = "Clear all foreground and background fine tuning"
            radio.check(2).Checked = True
        End If

        label1 = "Foreground Mask, fg fine tuning, bg fine tuning, blank"
        label2 = "Grabcut results after adding fine tuning selections"
        task.desc = "There are probably mistakes in the initial Grabcut_Basics.  Use the checkbox to fine tune what is background and foreground"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static fgFineTuning = findRadio("Selected rectangle is added to the foreground")
        Static clearCheck = findRadio("Clear all foreground and background fine tuning")
        Static saveRadio = fgFineTuning.checked

        If clearCheck.checked Then
            basics.fgFineTune = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
            basics.bgFineTune = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        End If

        If saveRadio <> fgFineTuning.checked Then
            saveRadio = fgFineTuning.checked
            task.drawRectClear = True
            Exit Sub
        End If

        If task.drawRect.Width <> 0 Then
            If fgFineTuning.checked Then
                basics.fgFineTune(task.drawRect).SetTo(255)
            Else
                basics.bgFineTune(task.drawRect).SetTo(255)
            End If
        End If

        basics.Run(src)

        mats.mat(0) = basics.dst1
        mats.mat(1) = basics.fgFineTune
        mats.mat(2) = basics.bgFineTune
        mats.Run(Nothing)
        dst1 = mats.dst1

        dst2 = basics.dst2
    End Sub
End Class



