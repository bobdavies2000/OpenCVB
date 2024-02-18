Imports cv = OpenCvSharp
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics : Inherits VB_Algorithm
    Public CS_SurfBasics As New CS_SurfBasics
    Public options As New Options_SURF
    Public Sub New()
        desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim doubleSize As New cv.Mat
        CS_SurfBasics.RunCS(task.leftView, task.rightView, doubleSize, options.surfThreshold, options.useBFMatch)

        doubleSize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)
        labels(2) = If(options.useBFMatch, "BF Matcher output", "Flann Matcher output")
        If CS_SurfBasics.keypoints1 IsNot Nothing Then labels(2) += " " + CStr(CS_SurfBasics.keypoints1.Count)
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Draw : Inherits VB_Algorithm
    Dim surf As New Surf_Basics
    Public Sub New()
        surf.CS_SurfBasics.drawPoints = False
        desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        surf.Run(src)

        dst2 = If(task.leftView.Channels = 1, task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.leftView)
        dst3 = If(task.rightView.Channels = 1, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView)

        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2

        For i = 0 To keys1.Count - 1
            dst2.Circle(keys1(i).Pt, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim matchCount As Integer
        Dim range = surf.options.verticalRange
        For i = 0 To keys1.Count - 1
            Dim pt = keys1(i).Pt

            For j = 0 To keys2.Count - 1
                If Math.Abs(keys2(j).Pt.Y - pt.Y) < range Then
                    dst3.Circle(keys2(j).Pt, task.dotSize + 2, task.highlightColor, -1, task.lineType)
                    keys2(j).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next

        ' mark those that were not
        For i = 0 To keys2.Count - 1
            Dim pt = keys2(i).Pt
            If pt.Y <> -1 Then dst3.Circle(keys2(i).Pt, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        Next
        labels(3) = "Yellow matched left to right = " + CStr(matchCount) + ". Red is unmatched."
    End Sub
End Class







' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_MatchBad : Inherits VB_Algorithm
    Dim CS_SurfBasics As New CS_SurfBasics
    Dim ptLeft As New SortedList(Of Integer, cv.Point)
    Dim ptRight As New SortedList(Of Integer, cv.Point)
    Dim options As New Options_SURF
    Public Sub New()
        CS_SurfBasics.drawPoints = False
        desc = "Use left and right images as input to SURF and restrict matches to identical Y coordinate"
    End Sub
    Private Function prepPoints(input As cv.KeyPoint(), ByRef yList As List(Of Integer)) As SortedList(Of Integer, cv.Point)
        Dim ptList As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
        For i = 0 To input.Count - 1
            Dim pt = input(i).Pt
            Dim y = Math.Round(pt.Y)
            ptList.Add(y, New cv.Point(pt.X, y))
        Next

        Dim removeList As New List(Of Integer)
        For Each kpt In ptList
            Dim y = kpt.Key
            If yList.Contains(y) Then
                Dim index = yList.IndexOf(y)
                If removeList.Contains(index) = False Then
                    removeList.Add(index)
                    removeList.Add(index + 1)
                End If
            End If
            yList.Add(y)
        Next

        'For i = removeList.Count - 1 To 0 Step -1
        '    Dim index = removeList(i)
        '    ptList.RemoveAt(index)
        '    yList.RemoveAt(index)
        'Next
        Return ptList
    End Function
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        CS_SurfBasics.RunCS(task.leftView, task.rightView, New cv.Mat, options.surfThreshold, options.useBFMatch)
        If CS_SurfBasics.keypoints1 Is Nothing Then Exit Sub

        Dim doublesize As New cv.Mat
        cv.Cv2.HConcat(task.leftView, task.rightView, doublesize)

        Dim lefties As New List(Of Integer)
        Dim righties As New List(Of Integer)
        ptLeft = prepPoints(CS_SurfBasics.keypoints1, lefties)
        ptRight = prepPoints(CS_SurfBasics.keypoints2, righties)

        For Each kpt In ptLeft
            Dim y = kpt.Key
            Dim index = righties.IndexOf(y)
            If index >= 0 Then
                Dim key = ptRight.ElementAt(index).Key
                Dim p2 = ptRight.ElementAt(index).Value
                p2 = New cv.Point(p2.X + dst2.Width, p2.Y)
                doublesize.Line(kpt.Value, p2, task.highlightColor, task.lineWidth)
            End If
        Next

        doublesize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
        doublesize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)
        labels(2) = If(options.useBFMatch, "BF Matcher output", "Flann Matcher output")
        labels(2) += " " + CStr(CS_SurfBasics.keypoints1.Count) + " key points found in the left view"
        labels(3) = " " + CStr(CS_SurfBasics.keypoints2.Count) + " key points found in the right view"
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Match : Inherits VB_Algorithm
    Dim surf As New Surf_Basics
    Public Sub New()
        surf.CS_SurfBasics.drawPoints = False
        desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        surf.Run(src)

        Dim doublesize As New cv.Mat
        cv.Cv2.HConcat(task.leftView, task.rightView, doublesize)

        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2

        For i = 0 To keys1.Count - 1
            dst2.Circle(keys1(i).Pt, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim matchCount As Integer
        Dim range = surf.options.verticalRange
        For i = 0 To keys1.Count - 1
            Dim p1 = keys1(i).Pt
            For j = 0 To keys2.Count - 1
                Dim p2 = keys2(j).Pt
                p2 = New cv.Point2f(p2.X + dst2.Width, p2.Y)
                If Math.Abs(keys2(j).Pt.Y - p1.Y) < range Then
                    doublesize.Line(p1, p2, task.highlightColor, task.lineWidth)
                    keys2(j).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next

        doublesize(New cv.Rect(0, 0, src.Width, src.Height)).CopyTo(dst2)
        doublesize(New cv.Rect(src.Width, 0, src.Width, src.Height)).CopyTo(dst3)

        labels(2) = CStr(surf.CS_SurfBasics.keypoints1.Count) + " key points found in the left view - " +
                    If(surf.options.useBFMatch, "BF Matcher output ", "Flann Matcher output ")
        labels(3) = CStr(surf.CS_SurfBasics.keypoints2.Count) + " key points found in the right view " +
                    CStr(matchCount) + " were matched."
    End Sub
End Class
