Imports cv = OpenCvSharp
Imports OpenCvSharp.XPhoto
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class xPhoto_Bm3dDenoise : Inherits VBparent
    Public Sub New()
        task.desc = "Denoise image with block matching and filtering."
		' task.rank = 1
        label1 = "Bm3dDenoising"
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(src as cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(src, src)
        CvXPhoto.Bm3dDenoising(src, dst1)
        cv.Cv2.Subtract(dst1, src, dst2)
        dst2.MinMaxLoc(minval, maxval)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class





Public Class xPhoto_Bm3dDenoiseDepthImage : Inherits VBparent
    Public Sub New()
        task.desc = "Denoise the depth image with block matching and filtering."
		' task.rank = 1
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(gray, gray)
        CvXPhoto.Bm3dDenoising(gray, dst1)
        cv.Cv2.Subtract(dst1, gray, dst2)
        dst2.MinMaxLoc(minval, maxval)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class




Module xPhoto_OilPaint_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub xPhoto_OilPaint_Close(xPhoto_OilPaint_Ptr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Run(xPhoto_OilPaint_Ptr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer,
                                       size As integer, dynRatio As integer, colorCode As integer) As IntPtr
    End Function
End Module



' https://github.com/opencv/opencv_contrib/blob/master/modules/xphoto/samples/oil.cpp
Public Class xPhoto_OilPaint_CPP : Inherits VBparent
    Dim xPhoto_OilPaint As IntPtr
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "xPhoto Dynamic Ratio", 1, 127, 7)
            sliders.setupTrackBar(1, "xPhoto Block Size", 1, 100, 3)
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
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim colorCode As integer = cv.ColorConversionCodes.BGR2GRAY
        Static frm = findfrm("xPhoto_OilPaint_CPP Radio Options")
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



