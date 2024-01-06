Imports System.IO
Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/de/dd0/grabcut_8cpp-example.html
Public Class GrabCut_Basics : Inherits VB_Algorithm
    Public fgFineTune As cv.Mat
    Public bgFineTune As cv.Mat
    Public fore As New Foreground_Basics
    Public Sub New()
        desc = "Use Foreground_Basics to define the foreground for use in GrabCut."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static bgModel As New cv.Mat(1, 65, cv.MatType.CV_64F, 0), fgModel As New cv.Mat(1, 65, cv.MatType.CV_64F, 0)
        Static fgFineTune As cv.Mat, bgFineTune As cv.Mat
        fore.Run(src)
        dst2 = fore.dst2
        dst3 = fore.dst3

        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, cv.GrabCutClasses.PR_BGD)
        dst0.SetTo(cv.GrabCutClasses.FGD, fore.fg)
        dst0.SetTo(cv.GrabCutClasses.BGD, fore.bg)

        ' cv.Cv2.GrabCut(src, dst0, New cv.Rect, bgModel, fgModel, 1, cv.GrabCutModes.InitWithMask)

        fore.bg = Not fore.fg

        If fore.fg.CountNonZero Then
            If fgFineTune IsNot Nothing Then dst0.SetTo(cv.GrabCutClasses.FGD, fgFineTune)
            If bgFineTune IsNot Nothing Then dst0.SetTo(cv.GrabCutClasses.BGD, bgFineTune)

            cv.Cv2.GrabCut(src, dst0, New cv.Rect, bgModel, fgModel, 1, cv.GrabCutModes.Eval)
        End If
        dst3.SetTo(0)
        src.CopyTo(dst3, dst0)
        labels(2) = "KMeans output defining the " + CStr(fore.classCount) + " classes."
    End Sub
End Class









Public Class GrabCut_FineTune : Inherits VB_Algorithm
    Dim basics As New GrabCut_Basics
    Dim mats As New Mat_4to1
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Selected rectangle is added to the foreground")
            radio.addRadio("Selected rectangle is added to the background")
            radio.addRadio("Clear all foreground and background fine tuning")
            radio.check(0).Checked = True
        End If

        labels(2) = "Foreground Mask, fg fine tuning, bg fine tuning, blank"
        labels(3) = "Grabcut results after adding fine tuning selections"
        desc = "There are probably mistakes in the initial Grabcut_Basics.  Use the checkbox to fine tune what is background and foreground"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static fgFineTuning = findRadio("Selected rectangle is added to the foreground")
        Static clearCheck = findRadio("Clear all foreground and background fine tuning")
        Static saveRadio = fgFineTuning.checked

        If clearCheck.checked Or basics.fgFineTune Is Nothing Then
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

        mats.mat(0) = basics.dst2
        mats.mat(1) = basics.fgFineTune
        mats.mat(2) = basics.bgFineTune
        mats.Run(empty)
        dst2 = mats.dst2

        dst3 = basics.dst3
    End Sub
End Class








Public Class GrabCut_ImageRect : Inherits VB_Algorithm
    Dim image As cv.Mat
    Public Sub New()
        Dim fileInputName = New FileInfo(task.homeDir + "data/cat.jpg")
        image = cv.Cv2.ImRead(fileInputName.FullName)
        desc = "Grabcut example using a single image.  Fix this."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static bgModel As New cv.Mat, fgModel As New cv.Mat
        Static bgRect1 = New cv.Rect(482, 0, 128, 640)
        Static bgRect2 = New cv.Rect(0, 0, 162, 320)
        Static fgRect1 = New cv.Rect(196, 134, 212, 344)
        Static fgRect2 = New cv.Rect(133, 420, 284, 60)

        dst2 = image

        dst0 = New cv.Mat(image.Size, cv.MatType.CV_8U, cv.GrabCutClasses.PR_BGD)
        dst0(bgRect1).SetTo(cv.GrabCutClasses.BGD)
        dst0(bgRect2).SetTo(cv.GrabCutClasses.BGD)
        dst0(fgRect1).SetTo(cv.GrabCutClasses.FGD)
        dst0(fgRect2).SetTo(cv.GrabCutClasses.FGD)

        If firstPass Then
            cv.Cv2.GrabCut(dst2, dst0, bgRect1, bgModel, fgModel, 1, cv.GrabCutModes.InitWithRect)
            cv.Cv2.GrabCut(dst2, dst0, bgRect2, bgModel, fgModel, 1, cv.GrabCutModes.InitWithRect)
            cv.Cv2.GrabCut(dst2, dst0, fgRect1, bgModel, fgModel, 1, cv.GrabCutModes.InitWithRect)
            cv.Cv2.GrabCut(dst2, dst0, fgRect2, bgModel, fgModel, 1, cv.GrabCutModes.InitWithRect)
        End If

        Dim rect As New cv.Rect
        cv.Cv2.GrabCut(dst2, dst0, rect, bgModel, fgModel, 1, cv.GrabCutModes.Eval)

        dst3.SetTo(0)
        dst2.CopyTo(dst3, dst0 + 1)
    End Sub
End Class







Public Class GrabCut_ImageMask : Inherits VB_Algorithm
    Dim image As cv.Mat
    Public Sub New()
        Dim fileInputName = New FileInfo(task.homeDir + "data/cat.jpg")
        image = cv.Cv2.ImRead(fileInputName.FullName)
        desc = "Grabcut example using a single image. "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static bgModel As New cv.Mat, fgModel As New cv.Mat

        If heartBeat() Then
            dst2 = image
            dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(50, 255, cv.ThresholdTypes.Binary)
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, cv.GrabCutClasses.PR_BGD)
            dst1.SetTo(cv.GrabCutClasses.FGD, dst0)

            cv.Cv2.GrabCut(dst2, dst1, New cv.Rect, bgModel, fgModel, 1, cv.GrabCutModes.InitWithMask)
        Else
            cv.Cv2.GrabCut(dst2, dst1, New cv.Rect, bgModel, fgModel, 5, cv.GrabCutModes.Eval)
        End If

        dst3.SetTo(0)
        dst2.CopyTo(dst3, dst1 + 1)
    End Sub
End Class