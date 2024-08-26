Imports cvb = OpenCvSharp
Imports  System.IO
Imports System.Runtime.InteropServices
Public Class Vignetting_Basics : Inherits VB_Parent
    Public removeVig As Boolean
    Dim center As New cvb.Point(dst2.Width / 2, dst2.Height / 2)
    Dim options As New Options_Vignetting
    Public Sub New()
        cPtr = Vignetting_Open()
        desc = "C++ version of vignetting for comparison with the VB version."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.ClickPoint <> New cvb.Point Then center = task.ClickPoint

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Vignetting_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.radius, center.X, center.Y, removeVig)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Vignetting_Close(cPtr)
    End Sub
End Class






Public Class Vignetting_VB : Inherits VB_Parent
    Public removeVig As Boolean
    Dim center As New cvb.Point(dst2.Width / 2, dst2.Height / 2)
    Dim options As New Options_Vignetting
    Public Sub New()
        labels = {"", "", "Resulting vignetting.  Click where the center should be located for vignetting", ""}
        desc = "Create a stream of images that have been vignetted."
    End Sub
    Public Function fastCos(x As Double) As Double
        x += cvb.Cv2.PI / 2
        If x > cvb.Cv2.PI Then x -= 2 * cvb.Cv2.PI
        If x < 0 Then Return 1.27323954 * x + 0.405284735 * x * x
        Return 1.27323954 * x - 0.405284735 * x * x
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.ClickPoint <> New cvb.Point Then center = task.ClickPoint
        Dim maxDist = New cvb.Point(0, 0).DistanceTo(center) * options.radius
        Dim tmp As Double
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim pt = New cvb.Point(x, y)
                Dim cos = fastCos(CDbl(pt.DistanceTo(center) / maxDist))
                cos *= cos
                Dim val = src.Get(Of cvb.Vec3b)(y, x)
                For i = 0 To 2
                    If (removeVig) Then tmp = Math.Floor(CDbl(val(i) / cos)) Else tmp = Math.Floor(CDbl(val(i) * cos))
                    val(i) = If(tmp > 255, 255, tmp)
                Next
                dst2.Set(Of cvb.Vec3b)(y, x, val)
            Next
        Next
    End Sub
End Class





' https://github.com/dajuric/dot-devignetting
Public Class Vignetting_Removal : Inherits VB_Parent
    Dim basics As New Vignetting_Basics
    Dim defaultImage As cvb.Mat
    Public Sub New()
        basics.removeVig = True
        labels = {"", "", "Vignetted input - click anywhere to adjust the center of the vignetting.", "The devignetted output - brighter, more vivid colors."}
        desc = "Demonstrate devignetting"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() And defaultImage Is Nothing Then
            Dim fileInfo = New FileInfo(task.HomeDir + "data/nature.jpg")
            If fileInfo.Exists Then defaultImage = cvb.Cv2.ImRead(fileInfo.FullName)
            defaultImage = defaultImage.Resize(dst3.Size)
            dst2 = defaultImage.Clone
        End If
        If standaloneTest() Then basics.Run(defaultImage) Else basics.Run(src)
        dst3 = basics.dst2
    End Sub
End Class






' https://github.com/dajuric/dot-devignetting
' https://stackoverflow.com/questions/22654770/creating-vignette-filter-in-opencv
Public Class Vignetting_Devignetting : Inherits VB_Parent
    Dim devignet As New Vignetting_Removal
    Dim basics As New Vignetting_Basics
    Public Sub New()
        labels = {"", "", "Vignetted image", "Devignetted image"}
        desc = "Inject vignetting into the image and then remove it to test devignetting.  Click to relocate the center"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        basics.Run(src)
        dst2 = basics.dst2

        devignet.Run(dst2)
        dst3 = devignet.dst3
    End Sub
End Class