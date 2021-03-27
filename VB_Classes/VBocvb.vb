Imports cv = OpenCvSharp

Public Class VBocvb
    Public pixelsPerMeter As Single
    Public hFov As Single
    Public vFov As Single
    Public angleX As Single  ' rotation angle in radians around x-axis to align with gravity
    Public angleY As Single  ' this angle is only used manually - no IMU connection.
    Public angleZ As Single  ' rotation angle in radians around z-axis to align with gravity
    Public cz As Single
    Public sz As Single

    Public intermediateObject As VBparent

    Public label1 As String
    Public label2 As String

    Public pythonTaskName As String
    Public Sub New(_task As ActiveTask)

    End Sub
    Public Sub trueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        task.ttTextData.Add(str)
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        task.ttTextData.Add(str)
    End Sub
End Class
