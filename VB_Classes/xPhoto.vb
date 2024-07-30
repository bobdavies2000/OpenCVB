Imports cv = OpenCvSharp
Imports OpenCvSharp.XPhoto
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class XPhoto_Bm3dDenoise : Inherits VB_Parent
    Public Sub New()
        desc = "Denoise image with block matching and filtering."
        labels(2) = "Bm3dDenoising"
        labels(3) = "Difference from Input"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        cv.Cv2.EqualizeHist(src, src)
        CvXPhoto.Bm3dDenoising(src, dst2)
        cv.Cv2.Subtract(dst2, src, dst3)
        Dim mm as mmData = GetMinMax(dst3)
        labels(3) = "Diff from input - max change=" + CStr(mm.maxVal)
        dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class





Public Class XPhoto_Bm3dDenoiseDepthImage : Inherits VB_Parent
    Public Sub New()
        desc = "Denoise the depth image with block matching and filtering."
        labels(3) = "Difference from Input"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim test = New cv.Mat(src.Size(), cv.MatType.CV_8U)
        Dim gray = task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        cv.Cv2.EqualizeHist(gray, gray)
        CvXPhoto.Bm3dDenoising(gray, dst2)
        cv.Cv2.Subtract(dst2, gray, dst3)
        Dim mm as mmData = GetMinMax(dst3)
        labels(3) = "Diff from input - max change=" + CStr(mm.maxVal)
        dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class







' https://github.com/opencv/opencv_contrib/blob/master/modules/xphoto/samples/oil.cpp
Public Class XPhoto_OilPaint_CPP : Inherits VB_Parent
    ReadOnly options As New Options_XPhoto
    Public Sub New()
        cPtr = xPhoto_OilPaint_Open()
        desc = "Use the xPhoto Oil Painting transform"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = xPhoto_OilPaint_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                           options.blockSize, options.dynamicRatio, options.colorCode)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = xPhoto_OilPaint_Close(cPtr)
    End Sub
End Class







Public Class XPhoto_Inpaint : Inherits VB_Parent
    Public basics As New InPaint_Basics
    Public options As New Options_XPhotoInpaint
    Public Sub New()
        labels(2) = "RGB input to xPhoto Inpaint"
        labels(3) = "Repaired result..."
        desc = "Use the xPhoto inpaint to fill in the depth holes"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src
        Dim mask = basics.drawRandomLine(dst2)
        'Dim iType = InpaintTypes.FSR_BEST
        'If radioFast.checked Then iType = InpaintTypes.FSR_FAST
        'If radioSMap.checked Then iType = InpaintTypes.SHIFTMAP
        'CvXPhoto.Inpaint(dst2, mask, dst3, InpaintTypes.FSR_BEST)
        SetTrueText("This VB interface for xPhoto Inpaint does not work...  Uncomment the lines above this msg to test.", 3)
    End Sub
End Class






Public Class XPhoto_Inpaint_CPP : Inherits VB_Parent
    ReadOnly inpVB As New XPhoto_Inpaint
    Public Sub New()
        cPtr = xPhoto_Inpaint_Open()
        labels = {"", "Mask for inpainted repair", "output with inpainted data repaired", "Input to the inpaint C++ algorithm - not working!!!"}
        desc = "Use the xPhoto Oil Painting transform"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        inpVB.options.RunVB()

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
        Dim imagePtr = xPhoto_Inpaint_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols, iType)
        handleSrc.Free()
        handleMask.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
        SetTrueText("The xPhoto Inpaint call hangs." + vbCrLf + "Uncomment the C++ line - see XPhoto.cpp - to test", 1)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = xPhoto_Inpaint_Close(cPtr)
    End Sub
End Class