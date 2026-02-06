Imports cv = OpenCvSharp
Imports OpenCvSharp.XPhoto
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class NR_ExPhoto_Bm3dDenoise : Inherits TaskParent
        Public Sub New()
            desc = "Denoise image with block matching and filtering."
            labels(2) = "Bm3dDenoising"
            labels(3) = "Difference from Input"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.EqualizeHist(src, src)
            CvXPhoto.Bm3dDenoising(src, dst2)
            cv.Cv2.Subtract(dst2, src, dst3)
            Dim mm As mmData = GetMinMax(dst3)
            labels(3) = "Diff from input - max change=" + CStr(mm.maxVal)
            dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax)
        End Sub
    End Class





    Public Class NR_ExPhoto_Bm3dDenoiseDepthImage : Inherits TaskParent
        Public Sub New()
            desc = "Denoise the depth image with block matching and filtering."
            labels(3) = "Difference from Input"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim test = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            Dim gray As New cv.Mat
            cv.Cv2.EqualizeHist(atask.gray, gray)
            CvXPhoto.Bm3dDenoising(gray, dst2)
            cv.Cv2.Subtract(dst2, gray, dst3)
            Dim mm As mmData = GetMinMax(dst3)
            labels(3) = "Diff from input - max change=" + CStr(mm.maxVal)
            dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax)
        End Sub
    End Class







    ' https://github.com/opencv/opencv_contrib/blob/master/modules/xphoto/samples/oil.cpp
    Public Class NR_ExPhoto_OilPaint_CPP : Inherits TaskParent
        Implements IDisposable
        Dim options As New Options_XPhoto
        Public Sub New()
            cPtr = ExPhoto_OilPaint_Open()
            desc = "Use the xPhoto Oil Painting transform"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = ExPhoto_OilPaint_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                           options.blockSize, options.dynamicRatio, options.colorCode)
            handleSrc.Free()

            If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = ExPhoto_OilPaint_Close(cPtr)
        End Sub
    End Class







    Public Class ExPhoto_Inpaint : Inherits TaskParent
        Public basics As New InPaint_Basics
        Public options As New Options_XPhotoInpaint
        Public Sub New()
            labels(2) = "RGB input to xPhoto Inpaint"
            labels(3) = "Repaired result..."
            desc = "Use the xPhoto inpaint to fill in the depth holes"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src
            Dim mask = basics.drawRandomLine(dst2)
            'Dim iType = InpaintTypes.FSR_BEST
            'If radioFast.checked Then iType = InpaintTypes.FSR_FAST
            'If radioSMap.checked Then iType = InpaintTypes.SHIFTMAP
            'CvXPhoto.Inpaint(dst2, mask, dst3, InpaintTypes.FSR_BEST)
            SetTrueText("This VB interface for xPhoto Inpaint does not work...  Uncomment the lines above this msg to test.", 3)
        End Sub
    End Class






    Public Class NR_ExPhoto_Inpaint_CPP : Inherits TaskParent
        Implements IDisposable
        Dim inpVB As New ExPhoto_Inpaint
        Public Sub New()
            cPtr = ExPhoto_Inpaint_Open()
            labels = {"", "Mask for inpainted repair", "output with inpainted data repaired", "Input to the inpaint C++ algorithm - not working!!!"}
            desc = "Use the xPhoto Oil Painting transform"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            inpVB.options.Run()

            Dim iType = InpaintTypes.FSR_BEST
            If inpVB.options.FSRFast Then iType = InpaintTypes.FSR_FAST
            If inpVB.options.shiftMap Then iType = InpaintTypes.SHIFTMAP

            dst1 = inpVB.basics.drawRandomLine(src)
            dst3 = src.Clone
            dst3 = src.SetTo(0, dst1)

            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Dim maskData(dst1.Total * dst1.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Marshal.Copy(dst1.Data, maskData, 0, maskData.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)
            Dim imagePtr = ExPhoto_Inpaint_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols, iType)
            handleSrc.Free()
            handleMask.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
            SetTrueText("The xPhoto Inpaint call hangs." + vbCrLf + "Uncomment the C++ line - see XPhoto.cpp - to test", 1)
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = ExPhoto_Inpaint_Close(cPtr)
        End Sub
    End Class
End Namespace