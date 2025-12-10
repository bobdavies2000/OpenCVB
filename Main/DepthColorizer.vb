Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
'Module Externs
'    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function Depth_Colorizer_Open() As IntPtr
'    End Function
'    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function Depth_Colorizer_Close(Depth_ColorizerPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, bgrPtr As IntPtr, rows As Integer, cols As Integer,
'                                        maxDepth As Single) As IntPtr
'    End Function
'End Module
'Public Class DepthColorizer_Basics : Inherits TaskParent
'    Public Sub New()
'        cPtr = Depth_Colorizer_Open()

'        dst1 = New cv.Mat(myTask.workRes, cv.MatType.CV_8U, 0)
'        desc = "Create a traditional depth color scheme."
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        Dim depthData(myTask.pcSplit(2).Total * myTask.pcSplit(2).ElemSize - 1) As Byte
'        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
'        Marshal.Copy(myTask.pcSplit(2).Data, depthData, 0, depthData.Length)
'        Dim imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, myTask.MaxZmeters)
'        handleSrc.Free()

'        myTask.depthRGB = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)

'        'Dim gridIndex = Task.gridMap.Get(Of Integer)(Task.mouseMovePoint.Y, Task.mouseMovePoint.X)
'        'Dim depthGrid = Task.pcSplit(2)(Task.gridRects(gridIndex))
'        'Dim mask = Task.depthMask(Task.gridRects(gridIndex))
'        'Dim depth = depthGrid.Mean(mask)(0)
'        'Dim mm = GetMinMax(depthGrid, mask)
'        'Task.depthAndDepthRange = "Depth = " + Format(depth, fmt1) + "m grid = " + CStr(gridIndex) + " " + vbCrLf +
'        '                                   "Depth range = " + Format(mm.minVal, fmt1) + "m to " + Format(mm.maxVal, fmt1) + "m"
'        If standalone Then dst2 = myTask.depthRGB
'    End Sub
'    Public Sub Close()
'        If cPtr <> 0 Then cPtr = Depth_Colorizer_Close(cPtr)
'    End Sub
'End Class
