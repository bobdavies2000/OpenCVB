Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_Basics : Inherits VBparent
    Public warpInput As New WarpModel_Input
    Dim cPtr As IntPtr
    Public warpMatrix() As Single
    Public src2 As New cv.Mat
    Public warpMode As Integer
    Public aligned As New cv.Mat
    Public outputRect As cv.Rect
    Public Sub New()
        cPtr = WarpModel_Open()

        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "Motion_Translation (fastest)"
            radio.check(1).Text = "Motion_Euclidean"
            radio.check(2).Text = "Motion_Affine (very slow - Be sure to configure CPP_Classes in Release Mode)"
            radio.check(3).Text = "Motion_Homography (even slower - Use CPP_Classes in Release Mode)"
            radio.check(0).Checked = True
        End If

        labels(2) = "Src image (align to this image)"
        labels(3) = "Src2 image aligned to src image"
        task.desc = "Use FindTransformECC to align 2 images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Then
            warpInput.RunClass(src)

            If warpInput.check.Box(0).Checked Then
                src = warpInput.gradient(0)
                src2 = warpInput.gradient(1)
            Else
                src = warpInput.rgb(0)
                src2 = warpInput.rgb(1)
            End If
        End If

        Static frm = findfrm("WarpModel_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then warpMode = i
        Next

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim src2Data(src2.Total * src2.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Marshal.Copy(src2.Data, src2Data, 0, src2Data.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim handleSrc2 = GCHandle.Alloc(src2Data, GCHandleType.Pinned)

        Dim matPtr = WarpModel_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleSrc2.AddrOfPinnedObject(), src.Rows, src.Cols, 1, warpMode)

        handleSrc.Free()
        handleSrc2.Free()

        If warpMode <> 3 Then
            ReDim warpMatrix(2 * 3 - 1)
        Else
            ReDim warpMatrix(3 * 3 - 1)
        End If
        Marshal.Copy(matPtr, warpMatrix, 0, warpMatrix.Length)

        If warpMode <> 3 Then
            Dim warpMat = New cv.Mat(2, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpAffine(src2, aligned, warpMat, src.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        Else
            Dim warpMat = New cv.Mat(3, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpPerspective(src2, aligned, warpMat, src.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        End If

        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)

        outputRect = New cv.Rect(0, 0, src.Width, src.Height)
        dst2(outputRect) = src
        dst3(outputRect) = src2

        Dim outStr = "The warp matrix is:" + vbCrLf
        For i = 0 To warpMatrix.Length - 1
            If i Mod 3 = 0 Then outStr += vbCrLf
            outStr += Format(warpMatrix(i), "#0.000") + vbTab
        Next

        If radio.check(2).Checked Or radio.check(3).Checked Then
            outStr += vbCrLf + "NOTE: Gradients may give better results."
        End If
        setTrueText(outStr, aligned.Width + 10, 220)
    End Sub
    Public Sub Close()
        WarpModel_Close(cPtr)
    End Sub
End Class






' https://github.com/ycui11/-Colorizing-Prokudin-Gorskii-images-of-the-Russian-Empire
' https://github.com/petraohlin/Colorizing-the-Prokudin-Gorskii-Collection
Public Class WarpModel_Input : Inherits VBparent
    Public rgb(3 - 1) As cv.Mat
    Public gradient(3 - 1) As cv.Mat
    Dim sobel As New Edges_Sobel
    Public Sub New()
        If radio.Setup(caller, 12) Then
            radio.check(0).Text = "building.jpg"
            radio.check(1).Text = "church.jpg"
            radio.check(2).Text = "emir.jpg"
            radio.check(3).Text = "Painting.jpg"
            radio.check(4).Text = "railroad.jpg"
            radio.check(5).Text = "river.jpg"
            radio.check(6).Text = "Cliff.jpg"
            radio.check(7).Text = "Column.jpg"
            radio.check(8).Text = "General.jpg"
            radio.check(9).Text = "Girls.jpg"
            radio.check(10).Text = "Tablet.jpg"
            radio.check(11).Text = "Valley.jpg"
            radio.check(9).Checked = True
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Use Gradient in WarpInput"
        End If

        task.desc = "Import the misaligned input."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static gradientCheck = findCheckBox("Use Gradient in WarpInput")
        Static frm = findfrm(caller + " Radio Options")
        Dim img As New cv.Mat
        For i = 0 To frm.check.length - 1
            Dim nextRadio = frm.check(i)
            If nextRadio.Checked Then
                Dim photo As New FileInfo(task.parms.homeDir + "Data\Prokudin\" + nextRadio.Text)
                img = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                labels(2) = photo.Name + " - red image"
                labels(3) = photo.Name + " - Naively aligned merge"
                Exit For
            End If
        Next
        Dim r() = {New cv.Rect(0, 0, img.Width, img.Height / 3), New cv.Rect(0, img.Height / 3, img.Width, img.Height / 3),
                   New cv.Rect(0, 2 * img.Height / 3, img.Width, img.Height / 3)}

        For i = 0 To r.Count - 1
            If gradientCheck.checked Then
                sobel.RunClass(img(r(i)))
                gradient(i) = sobel.dst2.Clone()
            End If
            rgb(i) = img(r(i))
        Next

        If src.Width < rgb(0).Width Or src.Height < rgb(0).Height Then
            For i = 0 To rgb.Count - 1
                Dim sz = New cv.Size(src.Width * rgb(i).Height / rgb(i).Width, src.Height)
                r(i) = New cv.Rect(0, 0, sz.Width, sz.Height)
                rgb(i) = rgb(i).Resize(sz)
            Next
        End If
        Dim merged As New cv.Mat
        cv.Cv2.Merge(rgb, merged)
        dst2.SetTo(0)
        dst3.SetTo(0)
        dst2(r(0)) = rgb(0).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3(r(0)) = merged
    End Sub
End Class




Module WarpModel_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub WarpModel_Close(WarpModelPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Run(WarpModelPtr As IntPtr, src1Ptr As IntPtr, src2Ptr As IntPtr, rows As Integer, cols As Integer, channels As Integer, warpMode As Integer) As IntPtr
    End Function
End Module







' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_AlignImages : Inherits VBparent
    Dim ecc As New WarpModel_Basics
    Public Sub New()
        labels(2) = "Aligned image"
        task.desc = "Align the RGB inputs raw images from the Prokudin examples."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static gradientCheck = findCheckBox("Use Gradient in WarpInput")
        If standalone Then ecc.warpInput.RunClass(src)
        Dim aligned() = {New cv.Mat, New cv.Mat}
        For i = 0 To 1
            If gradientCheck.Checked Then
                src = ecc.warpInput.gradient(0)
                ecc.src2 = Choose(i + 1, ecc.warpInput.gradient(1), ecc.warpInput.gradient(2))
            Else
                src = ecc.warpInput.rgb(0)
                ecc.src2 = Choose(i + 1, ecc.warpInput.rgb(1), ecc.warpInput.rgb(2))
            End If
            ecc.RunClass(src)
            aligned(i) = ecc.aligned.Clone()
        Next

        Dim mergeInput() = {src, aligned(0), aligned(1)}
        Dim merged As New cv.Mat
        cv.Cv2.Merge(mergeInput, merged)
        dst2.SetTo(0)
        dst2(New cv.Rect(0, 0, merged.Width, merged.Height)) = merged
        setTrueText("Note small displacement of" + vbCrLf + "the image when gradient is used." + vbCrLf +
                      "Other than that, images look the same." + vbCrLf +
                      "Displacement increases with Sobel" + vbCrLf + "kernel size", merged.Width + 10, 40)
    End Sub
End Class







'Public Class WarpModel_Image : Inherits VBparent
'    Public wbasics as New WarpModel_Basics
'    Dim sobel As New Edges_Sobel
'    Public lastFrame As cv.Mat
'    Dim match as New MatchTemplate_DrawRect
'    Public Sub New()
'        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
'        dst3 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)

'        If sliders.Setup(caller) Then
'            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 950)
'            sliders.setupTrackBar(1, "Stdev Threshold", 0, 100, 10)
'        End If

'        task.drawRect = New cv.Rect(100, 100, 100, 100)
'        labels(2) = "Previous ROI (align to this image)"
'        labels(3) = "Current ROI aligned to previous frame (dst2)"
'        task.desc = "Find the Translation and Euclidean warp matrix for the current grayscale image to the previous - needs more work"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static stdevSlider = findSlider("Stdev Threshold")
'        Dim stdevThreshold = CSng(stdevSlider.Value)
'        Static correlationSlider = findSlider("Correlation Threshold X1000")
'        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

'        sobel.src = src
'        sobel.RunClass()

'        dst2 = sobel.dst2
'        If lastFrame Is Nothing Then lastFrame = dst2.Clone

'        ' if the ROI has low stdev or low correlation between the 2 images, it may never converge.
'        ' Try to prevent failure by measuring stdev and correlation before findTransformECC
'        wbasics.src = dst2(task.drawRect)
'        wbasics.src2 = lastFrame(task.drawRect)


'        Dim mean As Single, stdev As Single
'        cv.Cv2.MeanStdDev(dst2(task.drawRect), mean, stdev)
'        Dim thresholdError As String = ""
'        If stdev > stdevThreshold Then
'            Dim correlation As New cv.Mat
'            cv.Cv2.MatchTemplate(dst2(task.drawRect), lastFrame(task.drawRect), correlation, cv.TemplateMatchModes.CCoeffNormed)
'            dst3 = lastFrame.Clone
'            Dim corr = correlation.Get(Of Single)(0, 0)


'            ' This line makes the algorithm work but also makes it a tautology (or just plain useless.)  This needs more study...
'            wbasics.src2 = dst2(task.drawRect)



'            If corr < CCthreshold Then
'                thresholdError = "The correlation is only " + Format(corr, "#0.00") + " which is below the threshold of " + Format(CCthreshold, "#0.00")
'            End If
'            wbasics.RunClass()
'            dst2(wbasics.outputRect) = wbasics.dst2(wbasics.outputRect)
'            dst3(wbasics.outputRect) = wbasics.dst3(wbasics.outputRect)
'        Else
'            thresholdError = "The stdev was only " + Format(stdev, "#0.00") + " which is below the threshold of " + Format(stdevThreshold, "#0.00")
'        End If

'        If thresholdError.Length > 0 Then setTrueText(thresholdError)
'        lastFrame = dst2.Clone
'    End Sub
'End Class







'Public Class WarpModel_Entropy : Inherits VBparent
'    Dim warp as New WarpModel_Image
'    Dim entropy as New Entropy_Highest
'    Public Sub New()
'        task.desc = "Find warp matrix for the whole image using just the segment with the highest entropy."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1

'        ' we only need to compute the max entropy every once in a while.  
'        If task.frameCount Mod 10 = 0 Then
'            entropy.src = src
'            entropy.RunClass()
'        End If

'        Dim r = entropy.eMaxRect
'        warp.src = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)
'        warp.src(r) = src(r)
'        warp.RunClass()
'        'dst2(warp.basics.outputRect) = warp.dst2(warp.basics.outputRect)
'        'dst3(warp.basics.outputRect) = warp.dst3(warp.basics.outputRect)
'        dst2 = warp.dst2
'        dst3 = warp.dst3
'    End Sub
'End Class
