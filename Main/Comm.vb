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

    '     "1344x752 - Full resolution", "672x376 - Quarter resolution", "336x188 - Small resolution  ",
    Public Shared resolutionList As New List(Of String)(
        {"1920x1080 - Full resolution", "960x540 - Quarter resolution", "480x270 - Small resolution",
         "1280x720 - Full resolution", "640x360 - Quarter resolution", "320x180 - Small resolution",
         "640x480 - Full resolution", "320x240 - Quarter resolution", "160x120 - Small resolution",
         "960x600 - Full resolution", "480x300 - Quarter resolution", "240x150 - Small resolution  ",
         "672x376 - Full resolution", "336x188 - Quarter resolution", "168x94 - Small resolution    "})
End Class