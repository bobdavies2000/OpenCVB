Public Class Comm
    Public Enum oCase
        drawPointCloudRGB
        drawLineAndCloud
        drawFloor
        trianglesAndColor
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
        pcLines
        pcPointsAlone
        drawLines
        drawAvgPointCloudRGB
        readPointCloud
        draw3DLines
        draw3DLinesAndCloud
        test
    End Enum

    Public Shared cameraNames As New List(Of String)({"StereoLabs ZED 2/2i",
                                                      "Orbbec Gemini 335L",
                                                      "Orbbec Gemini 336L",
                                                      "Oak-D camera",
                                                      "Intel(R) RealSense(TM) Depth Camera 435i",
                                                      "Intel(R) RealSense(TM) Depth Camera 455",
                                                      "MYNT-EYE-D1000",
                                                      "Orbbec Gemini 335"
                                                      })
End Class