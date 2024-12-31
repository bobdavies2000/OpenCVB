Imports cvb = OpenCvSharp
' this module is somewhat redundant but it consolidates the algorithms that locate extrema in RedCloud cell contour.
Public Class Sides_Basics : Inherits TaskParent
    Public sides As New Profile_Basics
    Public corners As New Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedCloud output", "Selected Cell showing the various extrema."}
        desc = "Find the 6 extrema and the 4 farthest points in each quadrant for the selected RedCloud cell"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3

        Dim corners = sides.corners.ToList
        For i = 0 To corners.Count - 1
            Dim nextColor = sides.cornerColors(i)
            Dim nextLabel = sides.cornerNames(i)
            DrawLine(dst3, task.rc.maxDist, corners(i), white)
            SetTrueText(nextLabel, New cvb.Point(corners(i).X, corners(i).Y), 3)
        Next

        If corners.Count Then SetTrueText(sides.strOut, 3) Else SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Sides_Profile : Inherits TaskParent
    Dim sides As New Contour_SidePoints
    Public Sub New()
        labels = {"", "", "RedCloud_Basics Output", "Selected Cell"}
        desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText(sides.strOut, 3)
    End Sub
End Class








Public Class Sides_Corner : Inherits TaskParent
    Dim sides As New Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", ""}
        desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText("Center point is rcSelect.maxDist", 3)
    End Sub
End Class








Public Class Sides_ColorC : Inherits TaskParent
    Dim sides As New Sides_Basics
    Public Sub New()
        labels = {"", "", "RedColor Output", "Cell Extrema"}
        desc = "Find the extrema - top/bottom, left/right, near/far - points for a RedColor Cell"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        sides.Run(src)
        dst3 = sides.dst3
    End Sub
End Class
