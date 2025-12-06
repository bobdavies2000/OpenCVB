Imports cv = OpenCvSharp
Public Class Common
    Public Shared optionsChanged As Boolean
    Public Shared allOptions As New OptionsContainer
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

    Public Shared cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 435i",
                                                      "Intel(R) RealSense(TM) Depth Camera 455",
                                                      "Oak-D camera",
                                                      "Orbbec Gemini 335",
                                                      "Orbbec Gemini 335L",
                                                      "Orbbec Gemini 336L",
                                                      "StereoLabs ZED 2/2i"
                                                      })
End Class