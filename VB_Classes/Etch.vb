
Imports cv = OpenCvSharp
Public Class Etch_ASketch : Inherits VB_Parent
    Dim keys As Keyboard_Basics
    Dim slateColor = New cv.Scalar(122, 122, 122)
    Dim cursor As cv.Point
    Dim ms_rng As New System.Random
    Private Function randomCursor()
        Return New cv.Point(ms_rng.Next(0, dst2.Width), ms_rng.Next(0, dst2.Height))
    End Function
    Public Sub New()

        If check.Setup(traceName) Then
            check.addCheckBox("Etch_ASketch clean slate")
            check.addCheckBox("Demo mode")
            check.Box(1).Checked = True
            If task.testAllRunning Then check.Box(1).Checked = True
        End If

        keys = New Keyboard_Basics()

        cursor = randomCursor()
        dst2.SetTo(slateColor)
        desc = "Use OpenCV to simulate the Etch-a-Sketch Toy"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static cleanCheck = findCheckBox("Etch_ASketch clean slate")
        Static demoCheck = findCheckBox("Demo mode")
        keys.Run(src)
        Dim keyIn = New List(Of String)(keys.keyInput)

        If demoCheck.Checked Then
            keyIn.Clear() ' ignore any keyboard input when in Demo mode.
            Dim nextKey = Choose(ms_rng.Next(1, 5), "Down", "Up", "Left", "Right")
            labels(2) = "Etch_ASketch demo mode - moving randomly"
            For i = 0 To ms_rng.Next(10, 50)
                keyIn.Add(nextKey)
            Next
        Else
            labels(2) = "Use Up/Down/Left/Right keys to create image"
        End If
        If cleanCheck.Checked Then
            cleanCheck.Checked = False
            cursor = randomCursor()
            dst2.SetTo(slateColor)
        End If

        For i = 0 To keyIn.Count - 1
            Select Case keyIn(i)
                Case "Down"
                    cursor.Y += 1
                Case "Up"
                    cursor.Y -= 1
                Case "Left"
                    cursor.X -= 1
                Case "Right"
                    cursor.X += 1
            End Select
            If cursor.X < 0 Then cursor.X = 0
            If cursor.Y < 0 Then cursor.Y = 0
            If cursor.X >= src.Width Then cursor.X = src.Width - 1
            If cursor.Y >= src.Height Then cursor.Y = src.Height - 1
            dst2.Set(Of cv.Vec3b)(cursor.Y, cursor.X, black)
        Next
        If demoCheck.Checked Then
            Static lastCursor = cursor
            If lastCursor = cursor And task.frameCount <> 0 Then cursor = randomCursor()
            lastCursor = cursor
        End If
    End Sub
End Class


