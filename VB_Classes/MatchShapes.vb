Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/d3/dc0/group__imgproc__shape.html
' https://docs.opencv.org/3.4/d5/d45/tutorial_py_contours_more_functions.html
' https://stackoverflow.com/questions/55529371/opencv-shape-matching-between-two-similar-shapes
Public Class MatchShapes_Basics : Inherits VBparent
    Public contour1 As cv.Point()()
    Public contour2 As cv.Point()()
    Public matchOption As cv.ShapeMatchModes
    Dim options As New Contours_Options
    Public img1 As cv.Mat
    Public img2 As cv.Mat
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "I1 - Hu moments absolute sum of inverse differences"
            radio.check(1).Text = "I2 - Hu moments absolute difference"
            radio.check(2).Text = "I3 - Hu moments max absolute difference ratio"
            radio.check(0).Checked = True
        End If
        findRadio("CComp").Checked = True
        findRadio("FloodFill").Enabled = False
        findRadio("ApproxNone").Checked = True

        img1 = cv.Cv2.ImRead(task.parms.homeDir + "Data\star1.png", cv.ImreadModes.Color).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img2 = cv.Cv2.ImRead(task.parms.homeDir + "Data\star2.png", cv.ImreadModes.Color).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        task.desc = "MatchShapes compares single contour to single contour - pretty tricky"
    End Sub
    Public Function findBiggestContour(contour As cv.Point()(), maxLen As Integer, maxIndex As Integer, dst As cv.Mat) As Integer
        For i = 0 To contour.Length - 1
            If contour(i).Length > maxLen Then
                maxLen = contour(i).Length
                maxIndex = i
            End If
        Next

        For Each p In contour(maxIndex)
            dst.Circle(p, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
        Return maxIndex
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                matchOption = Choose(i + 1, cv.ShapeMatchModes.I1, cv.ShapeMatchModes.I2, cv.ShapeMatchModes.I3)
                Exit For
            End If
        Next

        options.Run(Nothing)

        If standalone Then
            dst2 = img1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = img2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        img1 = img1.Threshold(50, 255, cv.ThresholdTypes.Binary)
        contour1 = cv.Cv2.FindContoursAsArray(img1, options.retrievalMode, options.ApproximationMode)

        img2 = img2.Threshold(127, 255, cv.ThresholdTypes.Binary)
        contour2 = cv.Cv2.FindContoursAsArray(img2, options.retrievalMode, options.ApproximationMode)

        Dim maxLen1 As Integer, maxIndex1 As Integer, maxLen2 As Integer, maxIndex2 As Integer
        maxIndex1 = findBiggestContour(contour1, maxLen1, maxIndex1, dst2)
        maxIndex2 = findBiggestContour(contour2, maxLen2, maxIndex2, dst3)

        Dim match = cv.Cv2.MatchShapes(contour1(maxIndex1), contour2(maxIndex2), matchOption)
        labels(2) = "MatchShapes returned " + Format(match, "#0.00")
    End Sub
End Class