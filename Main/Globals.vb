Imports cv = OpenCvSharp
Module GlobalVariables
    Public settings As MainForm.Json
    Public homeDirPath As String
    Public mainfrm As MainForm.MainForm

    Public emptyRect As New cv.Rect

    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"

    Public cameraNames As New List(Of String)({"Intel(R) RealSense(TM) Depth Camera 435i",
                                               "Intel(R) RealSense(TM) Depth Camera 455",
                                               "Oak-D camera",
                                               "Orbbec Gemini 335",
                                               "Orbbec Gemini 335L",
                                               "Orbbec Gemini 336L",
                                               "StereoLabs ZED 2/2i"
                                               })

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