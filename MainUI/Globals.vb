Imports System.Runtime.InteropServices

Module GlobalVariables
    Public settings As jsonShared.Settings

    Public cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 435i",
                                               "Intel(R) RealSense(TM) Depth Camera 455",
                                               "Oak-D camera",
                                               "Orbbec Gemini 335",
                                               "Orbbec Gemini 335L",
                                               "Orbbec Gemini 336L",
                                               "StereoLabs ZED 2/2i"
                                               })

    Public Class GdiMonitor
        <DllImport("user32.dll")>
        Private Shared Function GetGuiResources(hProcess As IntPtr, uiFlags As Integer) As Integer
        End Function

        Public Shared Function GetGdiCount() As Integer
            Return GetGuiResources(Process.GetCurrentProcess().Handle, 0)
        End Function

        Public Shared Function GetUserCount() As Integer
            Return GetGuiResources(Process.GetCurrentProcess().Handle, 1)
        End Function
    End Class

    Public Enum oCase
        drawPointCloudRGB
        drawLineAndCloud
        drawFloor
        drawPyramid
        drawCube
        quadBasics
        minMaxBlocks
        drawTiles
        drawCell
        drawCells
        floorStudy
        data3D
        sierpinski
        polygonCell
        Histogram3D
        pcPoints
        line3D
        pcPointsAlone
        drawLines
        drawAvgPointCloudRGB
        readPC
        readQuads
        draw3DLines
        draw3DLinesAndCloud
        readLines
        colorTriangles
        imageTriangles
    End Enum
End Module