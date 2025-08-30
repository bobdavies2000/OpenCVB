Imports cv = OpenCvSharp
Imports  System.IO
Imports System.Runtime.InteropServices
' https://www.learnopencvb.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_Basics : Inherits TaskParent
    Dim ecc As New WarpModel_ECC
    Dim options As New Options_WarpModel
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"Original Blue plane", "Original Green plane", "Original Red plane", "ECC Aligned image"}
        desc = "Align the BGR inputs raw images from the Prokudin examples."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then ecc.warpInput.Run(src)
        dst0 = ecc.warpInput.rgb(0).Clone
        dst1 = ecc.warpInput.rgb(1).Clone
        dst2 = ecc.warpInput.rgb(2).Clone
        Dim aligned() = {New cv.Mat, New cv.Mat}
        For i = 0 To 1
            If options.useGradient Then
                src = ecc.warpInput.gradient(0)
                ecc.src2 = Choose(i + 1, ecc.warpInput.gradient(1), ecc.warpInput.gradient(2))
            Else
                src = ecc.warpInput.rgb(0)
                ecc.src2 = Choose(i + 1, ecc.warpInput.rgb(1), ecc.warpInput.rgb(2))
            End If
            ecc.Run(src)
            aligned(i) = ecc.aligned.Clone()
        Next

        Dim mergeInput() = {src, aligned(0), aligned(1)}
        Dim merged As New cv.Mat
        cv.Cv2.Merge(mergeInput, merged)
        dst3.SetTo(0)
        dst3(New cv.Rect(0, 0, merged.Width, merged.Height)) = merged
        SetTrueText("Note small displacement of" + vbCrLf + "the image when gradient is used." + vbCrLf +
                      "Other than that, images look the same." + vbCrLf +
                      "Displacement increases with Sobel" + vbCrLf + "kernel size", New cv.Point(merged.Width + 10, 40), 3)
    End Sub
End Class







' https://www.learnopencvb.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_ECC : Inherits TaskParent
    Public warpInput As New WarpModel_Input
    Public warpMatrix() As Single
    Public src2 As New cv.Mat
    Public aligned As New cv.Mat
    Public outputRect As cv.Rect
    Dim options As New Options_WarpModel
    Public Sub New()
        cPtr = WarpModel_Open()

        labels(2) = "Src image (align to this image)"
        labels(3) = "Src2 image aligned to src image"
        desc = "Use FindTransformECC to align 2 images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        If standaloneTest() Then
            warpInput.Run(src)
            If options.useGradient Then
                src = warpInput.gradient(0)
                src2 = warpInput.gradient(1)
            Else
                src = warpInput.rgb(0)
                src2 = warpInput.rgb(1)
            End If
        End If

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Dim src2Data(src2.Total * src2.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Marshal.Copy(src2.Data, src2Data, 0, src2Data.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim handleSrc2 = GCHandle.Alloc(src2Data, GCHandleType.Pinned)

        Dim imagePtr = WarpModel_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleSrc2.AddrOfPinnedObject(), src.Rows, src.Cols, 1, options.warpMode)

        handleSrc.Free()
        handleSrc2.Free()

        If options.warpMode <> 3 Then
            ReDim warpMatrix(2 * 3 - 1)
        Else
            ReDim warpMatrix(3 * 3 - 1)
        End If
        Marshal.Copy(imagePtr, warpMatrix, 0, warpMatrix.Length)

        If options.warpMode <> 3 Then
            Dim warpMat = cv.Mat.FromPixelData(2, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpAffine(src2, aligned, warpMat, src.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        Else
            Dim warpMat = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpPerspective(src2, aligned, warpMat, src.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        End If

        dst2 = New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))

        outputRect = New cv.Rect(0, 0, src.Width, src.Height)
        dst2(outputRect) = src
        dst3(outputRect) = src2

        Dim outStr = "The warp matrix is:" + vbCrLf
        For i = 0 To warpMatrix.Length - 1
            If i Mod 3 = 0 Then outStr += vbCrLf
            outStr += Format(warpMatrix(i), "#0.000") + vbTab
        Next

        If options.useWarpAffine Or options.useWarpHomography Then
            outStr += vbCrLf + "NOTE: Gradients may give better results."
        End If
        SetTrueText(outStr, New cv.Point(aligned.Width + 10, 220))
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = WarpModel_Close(cPtr)
    End Sub
End Class







' https://github.com/ycui11/-Colorizing-Prokudin-Gorskii-images-of-the-Russian-Empire
' https://github.com/petraohlin/Colorizing-the-Prokudin-Gorskii-Collection
Public Class WarpModel_Input : Inherits TaskParent
    Public rgb(3 - 1) As cv.Mat
    Public gradient(3 - 1) As cv.Mat
    Dim sobel As New Edge_Sobel
    Dim options As New Options_WarpModel
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"Original Blue plane", "Original Green plane", "Original Red plane", "Naively Aligned image"}
        desc = "Import the misaligned input."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        Dim r() = {New cv.Rect(0, 0, options.pkImage.Width, options.pkImage.Height / 3),
                   New cv.Rect(0, options.pkImage.Height / 3, options.pkImage.Width, options.pkImage.Height / 3),
                   New cv.Rect(0, 2 * options.pkImage.Height / 3, options.pkImage.Width, options.pkImage.Height / 3)}

        For i = 0 To r.Count - 1
            If options.useGradient Then
                sobel.Run(options.pkImage(r(i)))
                gradient(i) = sobel.dst2.Clone()
            End If
            rgb(i) = options.pkImage(r(i))
        Next

        If src.Width < rgb(0).Width Or src.Height < rgb(0).Height Then
            For i = 0 To rgb.Count - 1
                Dim sz = New cv.Size(src.Width * rgb(i).Height / rgb(i).Width, src.Height)
                r(i) = New cv.Rect(0, 0, sz.Width, sz.Height)
                rgb(i) = rgb(i).Resize(sz)
            Next
        End If

        dst0 = rgb(0)
        dst1 = rgb(1)
        dst2 = rgb(2)

        Dim merged As New cv.Mat
        cv.Cv2.Merge(rgb, merged)
        dst3.SetTo(0)
        dst3(r(0)) = merged
    End Sub
End Class