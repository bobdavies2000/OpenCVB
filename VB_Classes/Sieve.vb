Imports cv = OpenCvSharp
Imports System.Numerics
' https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
Public Class Sieve_BasicsVB : Inherits VBparent
    Public primes As New List(Of Integer)
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Count of desired primes", 1, 10000, 400)
        End If

        task.desc = "Implement the Sieve of Eratothenes"
    End Sub
    Public Function shareResults(sieveList As List(Of Integer)) As String
        Dim completeList As String = ""
        Dim nextList As String = "   "
        For Each n In sieveList
            nextList += n.ToString + ", "
            If nextList.Length >= 100 Then
                completeList += nextList + vbCrLf
                nextList = "   "
            End If
        Next
        Return completeList + Mid(nextList, 1, If(nextList.Length > 2, Len(nextList) - 2, ""))
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim count = sliders.trackbar(0).Value
        Dim nextEntry As Integer = 2
        Dim output = New List(Of Integer)
        While output.Count < sliders.trackbar(0).Value
            If output.All(Function(x)
                              If nextEntry Mod x <> 0 Then Return True
                              Return False
                          End Function) Then output.Add(nextEntry)
            nextEntry += 1
        End While
        If standalone Or task.intermediateReview = caller Then
            If output.Count > 0 Then task.trueText(shareResults(output))
        Else
            primes = New List(Of Integer)(output)
        End If
    End Sub
End Class






Public Class Sieve_Basics : Inherits VBparent
    Dim printer As New Sieve_BasicsVB
    Dim sieve As New CS_Classes.Sieve
    Public Sub New()
        task.desc = "Implement the Sieve of Eratothenes in C#"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static countSlider = findSlider("Count of desired primes")
        task.trueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.value)))
    End Sub
End Class