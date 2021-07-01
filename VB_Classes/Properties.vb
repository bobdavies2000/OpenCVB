Imports cv = OpenCvSharp
Public Class Properties_Basics : Inherits VBparent
    Public Sub New()
        task.desc = "Build properties list from the given input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Then
            setTrueText("Properties analyzes the features of a selected region.  It does nothing when run standalone.")
            Exit Sub
        End If


    End Sub
End Class