Imports cv = OpenCvSharp
Public Class Rectangle_Basics : Inherits VB_Parent
    Public rectangles As New List(Of cv.Rect)
    Public rotatedRectangles As New List(Of cv.RotatedRect)
    Dim options As New Options_Draw
    Public Sub New()
        desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If task.heartBeat Then
            dst2.SetTo(cv.Scalar.Black)
            rectangles.Clear()
            rotatedRectangles.Clear()
            For i = 0 To options.drawCount - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
                Dim width = msRNG.Next(0, src.Cols - nPoint.X - 1)
                Dim height = msRNG.Next(0, src.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)

                Dim nextColor = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
                Dim rr = New cv.RotatedRect(nPoint, eSize, angle)
                Dim r = New cv.Rect(nPoint.X, nPoint.Y, width, height)
                If options.drawRotated Then
                    DrawRotatedRectangle(rr, dst2, nextColor)
                Else
                    cv.Cv2.Rectangle(dst2, r, nextColor, options.drawFilled)
                End If
                rotatedRectangles.Add(rr)
                rectangles.Add(r)
            Next
        End If
    End Sub
End Class




Public Class Rectangle_Rotated : Inherits VB_Parent
    Public rectangle As New Rectangle_Basics
    Public Sub New()
        FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)").Checked = True
        desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rectangle.Run(src)
        dst2 = rectangle.dst2
    End Sub
End Class








Public Class Rectangle_Overlap : Inherits VB_Parent
    Public rect1 As cv.Rect
    Public rect2 As cv.Rect
    Public enclosingRect As cv.Rect
    Dim draw As New Rectangle_Basics
    Public Sub New()
        FindSlider("DrawCount").Value = 2
        desc = "Test if 2 rectangles overlap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static typeCheckBox = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
        If not task.heartBeat Then Exit Sub
        If standaloneTest() Then
            draw.Run(src)
            dst2 = draw.dst2
        End If

        dst3.SetTo(0)
        If typeCheckBox.Checked Then
            Dim r1 As cv.RotatedRect = draw.rotatedRectangles(0)
            Dim r2 As cv.RotatedRect = draw.rotatedRectangles(1)
            rect1 = r1.BoundingRect
            rect2 = r2.BoundingRect
            drawRotatedOutline(r1, dst3, cv.Scalar.Yellow)
            drawRotatedOutline(r2, dst3, cv.Scalar.Yellow)
        Else
            rect1 = draw.rectangles(0)
            rect2 = draw.rectangles(1)
        End If

        enclosingRect = New cv.Rect
        If rect1.IntersectsWith(rect2) Then
            enclosingRect = rect1.Union(rect2)
            dst3.Rectangle(enclosingRect, cv.Scalar.White, 4)
            labels(3) = "Rectangles intersect - red marks overlapping rectangle"
            dst3.Rectangle(rect1.Intersect(rect2), cv.Scalar.Red, -1)
        Else
            labels(3) = "Rectangles don't intersect"
        End If
        dst3.Rectangle(rect1, cv.Scalar.Yellow, 2)
        dst3.Rectangle(rect2, cv.Scalar.Yellow, 2)
    End Sub
End Class








Public Class Rectangle_Intersection : Inherits VB_Parent
    Public inputRects As New List(Of cv.Rect)
    Dim draw As New Rectangle_Basics
    Public enclosingRects As New List(Of cv.Rect)
    Dim otherRects As New List(Of cv.Rect)
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Merge rectangles within X pixels", 0, dst2.Width, If(dst2.Width = 1280, 500, 250))
        End If

        desc = "Test if any number of rectangles intersect."
    End Sub
    Private Function findEnclosingRect(rects As List(Of cv.Rect), proximity As Integer) As cv.Rect
        Dim enclosing = rects(0)
        Dim newOther As New List(Of cv.Rect)
        For i = 1 To rects.Count - 1
            Dim r1 = rects(i)
            If enclosing.IntersectsWith(r1) Or Math.Abs(r1.X - enclosing.X) < proximity Then
                enclosing = enclosing.Union(r1)
            Else
                newOther.Add(r1)
            End If
        Next
        otherRects = New List(Of cv.Rect)(newOther)
        Return enclosing
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static mergeSlider = FindSlider("Merge rectangles within X pixels")

        If standaloneTest() Then
            If task.heartBeat Then
                Static rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
                Static countSlider = FindSlider("DrawCount")

                rotatedCheck.Enabled = False
                countSlider.Value = msRNG.Next(2, 10)
                labels(2) = "Input rectangles = " + CStr(countSlider.Value)

                draw.Run(src)
                dst2 = draw.dst2
                inputRects = New List(Of cv.Rect)(draw.rectangles)
            End If
        Else
            dst2.SetTo(0)
            For Each r In inputRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 1)
            Next
        End If

        Dim sortedRect As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each r In inputRects
            sortedRect.Add(r.Width * r.Height, r)
        Next

        otherRects = New List(Of cv.Rect)(sortedRect.Values)

        Dim proximity = mergeSlider.Value
        enclosingRects.Clear()
        While otherRects.Count
            Dim enclosing = findEnclosingRect(otherRects, proximity)
            enclosingRects.Add(enclosing)
        End While
        labels(3) = CStr(enclosingRects.Count) + " enclosing rectangles were found"

        dst3.SetTo(0)
        For Each r In enclosingRects
            dst3.Rectangle(r, cv.Scalar.Yellow, 2)
        Next
        dst3 = dst2 * 0.5 Or dst3
    End Sub
End Class








Public Class Rectangle_Union : Inherits VB_Parent
    Dim draw As New Rectangle_Basics
    Public inputRects As New List(Of cv.Rect)
    Public allRect As cv.Rect ' a rectangle covering all the input
    Public Sub New()
        desc = "Create a rectangle that contains all the input rectangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            Static countSlider = FindSlider("DrawCount")
            Static rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            rotatedCheck.Enabled = False
            countSlider.Value = msRNG.Next(2, 10)
            labels(2) = "Input rectangles = " + CStr(draw.rectangles.Count)

            draw.Run(src)
            dst2 = draw.dst2
            inputRects = New List(Of cv.Rect)(draw.rectangles)
        Else
            dst2.SetTo(0)
            For Each r In inputRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 1)
            Next
            labels(2) = "Input rectangles = " + CStr(inputRects.Count)
        End If

        If inputRects.Count = 0 Then Exit Sub
        allRect = inputRects(0)
        For i = 1 To inputRects.Count - 1
            Dim r = inputRects(i)
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If allRect.Width > 0 And allRect.Height > 0 Then
                allRect = r.Union(allRect)
                If allRect.X + allRect.Width >= dst2.Width Then allRect.Width = dst2.Width - allRect.X
                If allRect.Height >= dst2.Height Then allRect.Height = dst2.Height - allRect.Y
            End If
        Next
        If allRect.X + allRect.Width >= dst2.Width Then allRect.Width = dst2.Width - allRect.X
        If allRect.Y + allRect.Height >= dst2.Height Then allRect.Height = dst2.Height - allRect.Y
        dst2.Rectangle(allRect, cv.Scalar.Red, 2)
    End Sub
End Class








Public Class Rectangle_MultiOverlap : Inherits VB_Parent
    Public inputRects As New List(Of cv.Rect)
    Public outputRects As New List(Of cv.Rect)
    Dim draw As New Rectangle_Basics
    Public Sub New()
        desc = "Given a group of rectangles, merge all the rectangles that overlap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            Static rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)")
            Static countSlider = FindSlider("DrawCount")
            rotatedCheck.Enabled = False
            countSlider.Value = msRNG.Next(2, 10)

            labels(2) = "Input rectangles = " + CStr(countSlider.Value)

            draw.Run(src)
            dst2 = draw.dst2
            inputRects = draw.rectangles
        End If

        Do
            Dim unionAdded = False
            For i = 0 To inputRects.Count - 1
                Dim r1 = inputRects(i)
                Dim rectCount = inputRects.Count
                For j = i + 1 To inputRects.Count - 1
                    Dim r2 = inputRects(j)
                    If r1.IntersectsWith(r2) Then
                        inputRects.RemoveAt(j)
                        inputRects.RemoveAt(i)
                        inputRects.Add(r1.Union(r2))
                        unionAdded = True
                        Exit For
                    End If
                Next
                If rectCount <> inputRects.Count Then Exit For
            Next
            If unionAdded = False Then Exit Do
        Loop
        outputRects = inputRects
        If standaloneTest() Then
            dst3.SetTo(0)
            For Each r In outputRects
                dst3.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            dst3 = dst2 * 0.5 Or dst3
            labels(3) = CStr(outputRects.Count) + " output rectangles"
        End If
    End Sub
End Class









Public Class Rectangle_EnclosingPoints : Inherits VB_Parent
    Public pointList As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Build an enclosing rectangle for the supplied pointlist"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            pointList = quickRandomPoints(20)
            dst2.SetTo(0)
            For Each pt In pointList
                DrawCircle(dst2,pt, task.DotSize, task.HighlightColor)
            Next
        End If

        Dim minRect = cv.Cv2.MinAreaRect(pointList.ToArray)
        drawRotatedOutline(minRect, dst2, cv.Scalar.Yellow)
    End Sub
End Class

