Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_Basics
    Inherits VBparent
    Public vDemo As New CS_Classes.VoronoiDemo
    Public random As Random_Basics
    Public inputPoints As List(Of cv.Point)
    Public Sub New()
        random = New Random_Basics()
        label1 = "Ordered list output for Voronoi algorithm"
        task.desc = "Use the ordered list method to find the Voronoi segments"
		' task.rank = 1
    End Sub
    Public Sub vDisplay( ByRef dst As cv.Mat, points As List(Of cv.Point))
        dst = dst.Normalize(255).ConvertScaleAbs(255)
        dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For Each pt In points
            dst.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
    Public Sub Run(src as cv.Mat)

        If task.frameCount = 0 Then
            findSlider("Random Pixel Count").Maximum = 100
        End If
        random.Run(Nothing)
        inputPoints = New List(Of cv.Point)(random.Points)

        vDemo.Run(dst1, inputPoints)
        vDisplay(dst1, inputPoints)
    End Sub
End Class






'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_Compare
    Inherits VBparent
    Dim basics As Voronoi_Basics
    Public random As Random_Basics
    Public Sub New()
        basics = New Voronoi_Basics()
        random = New Random_Basics()

        label1 = "Brute Force method"
        label2 = "Ordered List method"
        task.desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)

        random.Run(Nothing)
        Dim points = New List(Of cv.Point)(random.Points)
        basics.vDemo.Run(dst1, points, True)
        basics.vDisplay(dst1, points)

        basics.vDemo.Run(dst2, points, False)
        basics.vDisplay(dst2, points)
    End Sub
End Class








Module Voronoi
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub VoronoiDemo_Close(pfPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Run(pfPtr As IntPtr, Input As IntPtr, pointCount As Integer, width As Integer, height As Integer) As IntPtr
    End Function
End Module






'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_CPP
    Inherits VBparent
    Dim vPtr As IntPtr
    Dim vDemo As Voronoi_Basics
    Public Sub New()
        vDemo = New Voronoi_Basics()
        vPtr = VoronoiDemo_Open(task.parms.homeDir + "/Data/ballSequence/", dst1.Rows, dst1.Cols)
        task.desc = "Use the C++ version of the Voronoi code"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)

        Static countSlider = findSlider("Random Pixel Count")
        vDemo.random.Run(Nothing)
        Dim handleSrc = GCHandle.Alloc(vDemo.random.Points, GCHandleType.Pinned)
        Dim imagePtr = VoronoiDemo_Run(vPtr, handleSrc.AddrOfPinnedObject(), countSlider.Value, dst1.Width, dst1.Height)
        handleSrc.Free()
        If imagePtr <> 0 Then
            Dim tmp As New cv.Mat(dst1.Size, cv.MatType.CV_32F)
            Dim dstData(tmp.Total * tmp.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_32F, dstData)

            Dim inputPoints = New List(Of cv.Point)(vDemo.random.Points)
            vDemo.vDisplay(dst1, inputPoints)
        End If
    End Sub
    Public Sub Close()
        VoronoiDemo_Close(vPtr)
    End Sub
End Class

