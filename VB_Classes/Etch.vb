Imports cvb = OpenCvSharp
Public Class Etch_ASketch : Inherits VB_Parent
    Dim keys As Keyboard_Basics
    Dim slateColor = New cvb.Scalar(122, 122, 122)
    Dim cursor As cvb.Point
    Dim ms_rng As New System.Random
    Dim options As New Options_Etch_ASketch
    Dim lastCursor As cvb.Point
    Private Function randomCursor()
        Dim nextCursor = New cvb.Point(ms_rng.Next(0, dst2.Width), ms_rng.Next(0, dst2.Height))
        lastCursor = nextCursor
        Return nextCursor
    End Function
    Public Sub New()
        keys = New Keyboard_Basics()

        cursor = randomCursor()
        dst2.SetTo(slateColor)
        desc = "Use OpenCV to simulate the Etch-a-Sketch Toy"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        keys.Run(src)
        Dim keyIn = New List(Of String)(keys.keyInput)

        If options.demoMode Then
            keyIn.Clear() ' ignore any keyboard input when in Demo mode.
            Dim nextKey = Choose(ms_rng.Next(1, 5), "Down", "Up", "Left", "Right")
            labels(2) = "Etch_ASketch demo mode - moving randomly"
            For i = 0 To ms_rng.Next(10, 50)
                keyIn.Add(nextKey)
            Next
        Else
            labels(2) = "Use Up/Down/Left/Right keys to create image"
        End If
        If options.cleanMode Then
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
            dst2.Set(Of cvb.Vec3b)(cursor.Y, cursor.X, black)
        Next
        If options.demoMode Then lastCursor = cursor
    End Sub
End Class