Imports cv = OpenCvSharp
Imports OpenCvSharp.XPhoto
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class XPhoto_Bm3dDenoise : Inherits VBparent
    Public Sub New()
        task.desc = "Denoise image with block matching and filtering."
        label1 = "Bm3dDenoising"
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(src, src)
        CvXPhoto.Bm3dDenoising(src, dst1)
        cv.Cv2.Subtract(dst1, src, dst2)
        dst2.MinMaxLoc(minVal, maxVal)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class





Public Class XPhoto_Bm3dDenoiseDepthImage : Inherits VBparent
    Public Sub New()
        task.desc = "Denoise the depth image with block matching and filtering."
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(gray, gray)
        CvXPhoto.Bm3dDenoising(gray, dst1)
        cv.Cv2.Subtract(dst1, gray, dst2)
        dst2.MinMaxLoc(minVal, maxVal)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class




Module XPhoto_OilPaint_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub xPhoto_OilPaint_Close(xPhoto_OilPaint_Ptr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Run(xPhoto_OilPaint_Ptr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer,
                                       size As Integer, dynRatio As Integer, colorCode As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_Inpaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub xPhoto_Inpaint_Close(xPhoto_Inpaint_Ptr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_Inpaint_Run(xPhoto_Inpaint_Ptr As IntPtr, rgbPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer, iType As Integer) As IntPtr
    End Function
End Module







' https://github.com/opencv/opencv_contrib/blob/master/modules/xphoto/samples/oil.cpp
Public Class XPhoto_OilPaint_CPP : Inherits VBparent
    Dim xPhoto_OilPaint As IntPtr
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "XPhoto Dynamic Ratio", 1, 127, 7)
            sliders.setupTrackBar(1, "XPhoto Block Size", 1, 100, 3)
        End If
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 5)
            radio.check(0).Text = "BGR2GRAY"
            radio.check(1).Text = "BGR2HSV"
            radio.check(2).Text = "BGR2YUV  "
            radio.check(3).Text = "BGR2XYZ"
            radio.check(4).Text = "BGR2Lab"
            radio.check(0).Checked = True
        End If

        Application.DoEvents() ' because the rest of initialization takes so long, let the show() above take effect.
        xPhoto_OilPaint = xPhoto_OilPaint_Open()
        task.desc = "Use the xPhoto Oil Painting transform - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim colorCode As Integer = cv.ColorConversionCodes.BGR2GRAY
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                colorCode = Choose(i + 1, cv.ColorConversionCodes.BGR2GRAY, cv.ColorConversionCodes.BGR2HSV, cv.ColorConversionCodes.BGR2YUV,
                                   cv.ColorConversionCodes.BGR2XYZ, cv.ColorConversionCodes.BGR2Lab)
                Exit For
            End If
        Next

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = xPhoto_OilPaint_Run(xPhoto_OilPaint, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                           sliders.trackbar(1).Value, sliders.trackbar(0).Value, colorCode)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        xPhoto_OilPaint_Close(xPhoto_OilPaint)
    End Sub
End Class







Public Class XPhoto_Inpaint : Inherits VBparent
    Public basics As New InPaint_Basics
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "FSR_Best"
            radio.check(1).Text = "FSR_Fast"
            radio.check(2).Text = "ShiftMap"
            radio.check(0).Checked = True
        End If

        label1 = "RGB input to xPhoto Inpaint"
        label2 = "Repaired result..."
        task.desc = "Use the xPhoto inpaint to fill in the depth holes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static radioFast = findRadio("FSR_Fast")
        Static radioSMap = findRadio("ShiftMap")
        dst1 = src
        Dim mask = basics.drawRandomLine(dst1)
        Dim iType = InpaintTypes.FSR_BEST
        If radioFast.checked Then iType = InpaintTypes.FSR_FAST
        If radioSMap.checked Then iType = InpaintTypes.SHIFTMAP
        ' CvXPhoto.Inpaint(dst1, mask, dst2, InpaintTypes.FSR_BEST)
        task.trueText("This VB interface for xPhoto Inpaint does not work...  Uncomment the line above this msg to test.", 10, 200, 3)
    End Sub
End Class






Public Class XPhoto_Inpaint_CPP : Inherits VBparent
    Dim xPhoto_Inpaint As IntPtr
    Dim inpVB As New XPhoto_Inpaint
    Public Sub New()
        xPhoto_Inpaint = xPhoto_Inpaint_Open()
        task.desc = "Use the xPhoto Oil Painting transform - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static radioFast = findRadio("FSR_Fast")
        Static radioSMap = findRadio("ShiftMap")
        Dim iType = InpaintTypes.FSR_BEST
        If radioFast.checked Then iType = InpaintTypes.FSR_FAST
        If radioSMap.checked Then iType = InpaintTypes.SHIFTMAP

        Dim mask = inpVB.basics.drawRandomLine(src)
        dst1 = src

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim maskData(mask.Total * mask.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Marshal.Copy(mask.Data, maskData, 0, maskData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)
        Dim imagePtr = xPhoto_Inpaint_Run(xPhoto_Inpaint, handleSrc.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols, iType)
        handleSrc.Free()
        handleMask.Free()

        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)

        task.trueText("The infrastructure is all in place but the xPhoto Inpaint call hangs.  Uncomment the C++ line in Run to test", 10, 200, 3)
    End Sub
    Public Sub Close()
        xPhoto_Inpaint_Close(xPhoto_Inpaint)
    End Sub
End Class