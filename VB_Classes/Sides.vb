Imports cv = OpenCvSharp
' this module is somewhat redundant but it consolidates the algorithms that locate extrema in RedCloud cell contour.
Public Class Sides_Basics : Inherits VB_Algorithm
    Public sides As New Profile_Basics
    Public corners As New Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedCloud output", "Selected Cell showing the various extrema."}
        desc = "Find the 6 extrema and the 4 farthest points in each quadrant for the selected RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3

        Dim corners = sides.corners.ToList
        For i = 0 To corners.Count - 1
            Dim nextColor = sides.cornerColors(i)
            Dim nextLabel = sides.cornerNames(i)
            dst3.Line(task.rcOld.maxDist, corners(i), cv.Scalar.White, task.lineWidth, task.lineType)
            setTrueText(nextLabel, New cv.Point(corners(i).X, corners(i).Y), 3)
        Next

        If corners.Count Then setTrueText(sides.strOut, 3) Else setTrueText(strOut, 3)
    End Sub
End Class







Public Class Sides_Profile : Inherits VB_Algorithm
    Dim sides As New Contour_SidePoints
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "RedCloud_Basics Output", "Selected Cell"}
        desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
        setTrueText(sides.strOut, 3)
    End Sub
End Class








Public Class Sides_Corner : Inherits VB_Algorithm
    Dim sides As New Contour_RedCloudCorners
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", ""}
        desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
        setTrueText("Center point is rcSelect.maxDist", 3)
    End Sub
End Class








Public Class Sides_ColorC : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim sides As New Sides_Basics
    Public Sub New()
        labels = {"", "", "RedColor Output", "Cell Extrema"}
        desc = "Find the extrema - top/bottom, left/right, near/far - points for a RedColor Cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
    End Sub
End Class
