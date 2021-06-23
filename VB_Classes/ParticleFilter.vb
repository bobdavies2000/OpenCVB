Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO




' https://github.com/masaddev/OpenCVParticleFilter/tree/master/OpenCVParticleFilter
Public Class ParticleFilter_Example : Inherits VBparent
    Dim pfPtr As IntPtr
    Public Sub New()
        pfPtr = ParticleFilterTest_Open(task.parms.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        task.desc = "Particle Filter example downloaded from github - hyperlink in the code shows URL."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static imageFrame = 12
        imageFrame += 1
        If imageFrame Mod 45 = 0 Then
            imageFrame = 13
            ParticleFilterTest_Close(pfPtr)
            pfPtr = ParticleFilterTest_Open(task.parms.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        End If
        Dim nextFile As New FileInfo(task.parms.homeDir + "Data/ballSequence/color_" + CStr(imageFrame) + ".png")
        dst3 = cv.Cv2.ImRead(nextFile.FullName).Resize(dst2.Size)
        Dim imagePtr = ParticleFilterTest_Run(pfPtr)
        If imagePtr <> 0 Then
            Dim dstData(dst2.Total * dst2.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, dstData)
        End If
    End Sub
    Public Sub Close()
        ParticleFilterTest_Close(pfPtr)
    End Sub
End Class







Module ParticleFilter
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ParticleFilterTest_Close(pfPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function ParticleFilterTest_Run(pfPtr As IntPtr) As IntPtr
    End Function
End Module

