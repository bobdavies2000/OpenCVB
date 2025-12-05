Imports cv = OpenCvSharp
Public Class Common
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

    Public Shared cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 455",
                                                      "StereoLabs ZED 2/2i",
                                                      "Orbbec Gemini 335L",
                                                      "Orbbec Gemini 336L",
                                                      "Oak-D camera",
                                                      "Intel(R) RealSense(TM) Depth Camera 435i",
                                                      "Orbbec Gemini 335"
                                                      })
End Class