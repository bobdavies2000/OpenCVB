Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_Basics : Inherits VB_Algorithm
    Public vDemo As New CS_Classes.VoronoiDemo
    Public random As New Random_Basics
    Public Sub New()
        labels(2) = "Ordered list output for Voronoi algorithm"
        findSlider("Random Pixel Count").Maximum = 100
        desc = "Use the ordered list method to find the Voronoi segments"
    End Sub
    Public Sub vDisplay(ByRef dst As cv.Mat, points As List(Of cv.Point2f), color As cv.Scalar)
        dst = dst.Normalize(255).ConvertScaleAbs(255)
        dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For Each pt In points
            dst.Circle(pt, task.dotSize, color, -1, task.lineType)
        Next
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then random.Run(empty)
        vDemo.RunCS(dst2, random.PointList)
        vDisplay(dst2, random.PointList, cv.Scalar.Yellow)
    End Sub
End Class






'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_Compare : Inherits VB_Algorithm
    Dim basics As New Voronoi_Basics
    Public random As New Random_Basics
    Public Sub New()
        findSlider("Random Pixel Count").Maximum = 150
        findSlider("Random Pixel Count").Value = 150
        labels = {"", "", "Brute Force method - check log timings", "Ordered List method - check log for timing"}
        desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        random.Run(empty)
        basics.vDemo.RunCS(dst2, random.PointList, True)
        basics.vDisplay(dst2, random.PointList, cv.Scalar.Yellow)

        basics.vDemo.RunCS(dst3, random.PointList, False)
        basics.vDisplay(dst3, random.PointList, cv.Scalar.Yellow)
    End Sub
End Class








Module Voronoi
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Open(matlabFileName As String, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function VoronoiDemo_Run(pfPtr As IntPtr, Input As IntPtr, pointCount As Integer, width As Integer, height As Integer) As IntPtr
    End Function
End Module






'https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
Public Class Voronoi_CPP : Inherits VB_Algorithm
    Dim vDemo As New Voronoi_Basics
    Public Sub New()
        cPtr = VoronoiDemo_Open(task.homeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols)
        desc = "Use the C++ version of the Voronoi code"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = findSlider("Random Pixel Count")
        If heartBeat() Then vDemo.random.Run(empty)
        Dim ptList = vbFloat2Int(vDemo.random.pointList)
        Dim handleSrc = GCHandle.Alloc(ptList.ToArray, GCHandleType.Pinned)
        Dim imagePtr = VoronoiDemo_Run(cPtr, handleSrc.AddrOfPinnedObject(), countSlider.Value, dst2.Width, dst2.Height)
        handleSrc.Free()

        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_32F, imagePtr).Clone
        vDemo.vDisplay(dst2, vDemo.random.pointList, cv.Scalar.Yellow)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = VoronoiDemo_Close(cPtr)
    End Sub
End Class

