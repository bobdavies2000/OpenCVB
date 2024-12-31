Imports System.IO
Imports cvb = OpenCvSharp
' https://docs.opencvb.org/3.3.1/de/dd0/grabcut_8cpp-example.html
Public Class GrabCut_Basics : Inherits TaskParent
    Public fgFineTune As cvb.Mat
    Public bgFineTune As cvb.Mat
    Public fore As New Foreground_Basics
    Dim bgModel As cvb.Mat = New cvb.Mat(1, 65, cvb.MatType.CV_64F, cvb.Scalar.All(0))
    Dim fgModel As cvb.Mat = New cvb.Mat(1, 65, cvb.MatType.CV_64F, cvb.Scalar.All(0))
    Public Sub New()
        desc = "Use Foreground_Basics to define the foreground for use in GrabCut."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        fore.Run(src)
        dst2 = fore.dst2
        dst3 = fore.dst3

        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_8U, cvb.GrabCutClasses.PR_BGD)
        dst0.SetTo(cvb.GrabCutClasses.FGD, fore.fg)
        dst0.SetTo(cvb.GrabCutClasses.BGD, fore.bg)

        ' cvb.Cv2.GrabCut(src, dst0, New cvb.Rect, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithMask)

        fore.bg = Not fore.fg

        If fore.fg.CountNonZero Then
            If fgFineTune IsNot Nothing Then dst0.SetTo(cvb.GrabCutClasses.FGD, fgFineTune)
            If bgFineTune IsNot Nothing Then dst0.SetTo(cvb.GrabCutClasses.BGD, bgFineTune)

            cvb.Cv2.GrabCut(src, dst0, New cvb.Rect, bgModel, fgModel, 1, cvb.GrabCutModes.Eval)
        End If
        dst3.SetTo(0)
        src.CopyTo(dst3, dst0)
        labels(2) = "KMeans output defining the " + CStr(fore.classCount) + " classes."
    End Sub
End Class









Public Class GrabCut_FineTune : Inherits TaskParent
    Dim basics As New GrabCut_Basics
    Dim mats As New Mat_4to1
    Dim options As New Options_GrabCut
    Dim saveRadio As Boolean = True
    Public Sub New()
        labels(2) = "Foreground Mask, fg fine tuning, bg fine tuning, blank"
        labels(3) = "Grabcut results after adding fine tuning selections"
        desc = "There are probably mistakes in the initial Grabcut_Basics.  Use the checkbox to fine tune what is background and foreground"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        If options.clearAll Or basics.fgFineTune Is Nothing Then
            basics.fgFineTune = New cvb.Mat(src.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
            basics.bgFineTune = New cvb.Mat(src.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        End If

        If saveRadio <> options.fineTuning Then
            saveRadio = options.fineTuning
            task.drawRectClear = True
            Exit Sub
        End If

        If task.drawRect.Width <> 0 Then
            If options.fineTuning Then
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








Public Class GrabCut_ImageRect : Inherits TaskParent
    Dim image As cvb.Mat
    Dim bgModel As New cvb.Mat, fgModel As New cvb.Mat
    Dim bgRect1 = New cvb.Rect(482, 0, 128, 640)
    Dim bgRect2 = New cvb.Rect(0, 0, 162, 320)
    Dim fgRect1 = New cvb.Rect(196, 134, 212, 344)
    Dim fgRect2 = New cvb.Rect(133, 420, 284, 60)
    Public Sub New()
        Dim fileInputName = New FileInfo(task.HomeDir + "data/cat.jpg")
        image = cvb.Cv2.ImRead(fileInputName.FullName)
        desc = "Grabcut example using a single image.  Fix this."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        dst2 = image

        dst0 = New cvb.Mat(image.Size(), cvb.MatType.CV_8U, cvb.GrabCutClasses.PR_BGD)
        dst0(bgRect1).SetTo(cvb.GrabCutClasses.BGD)
        dst0(bgRect2).SetTo(cvb.GrabCutClasses.BGD)
        dst0(fgRect1).SetTo(cvb.GrabCutClasses.FGD)
        dst0(fgRect2).SetTo(cvb.GrabCutClasses.FGD)

        If task.firstPass Then
            cvb.Cv2.GrabCut(dst2, dst0, bgRect1, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithRect)
            cvb.Cv2.GrabCut(dst2, dst0, bgRect2, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithRect)
            cvb.Cv2.GrabCut(dst2, dst0, fgRect1, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithRect)
            cvb.Cv2.GrabCut(dst2, dst0, fgRect2, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithRect)
        End If

        Dim rect As New cvb.Rect
        cvb.Cv2.GrabCut(dst2, dst0, rect, bgModel, fgModel, 1, cvb.GrabCutModes.Eval)

        dst3.SetTo(0)
        dst2.CopyTo(dst3, dst0 + 1)
    End Sub
End Class







Public Class GrabCut_ImageMask : Inherits TaskParent
    Dim image As cvb.Mat
    Public Sub New()
        Dim fileInputName = New FileInfo(task.HomeDir + "data/cat.jpg")
        image = cvb.Cv2.ImRead(fileInputName.FullName)
        desc = "Grabcut example using a single image. "
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Static bgModel As New cvb.Mat, fgModel As New cvb.Mat

        If task.heartBeat Then
            dst2 = image
            dst0 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray).Threshold(50, 255, cvb.ThresholdTypes.Binary)
            dst1 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.GrabCutClasses.PR_BGD)
            dst1.SetTo(cvb.GrabCutClasses.FGD, dst0)

            cvb.Cv2.GrabCut(dst2, dst1, New cvb.Rect, bgModel, fgModel, 1, cvb.GrabCutModes.InitWithMask)
        Else
            cvb.Cv2.GrabCut(dst2, dst1, New cvb.Rect, bgModel, fgModel, 5, cvb.GrabCutModes.Eval)
        End If

        dst3.SetTo(0)
        dst2.CopyTo(dst3, dst1 + 1)
    End Sub
End Class