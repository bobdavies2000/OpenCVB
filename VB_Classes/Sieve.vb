Imports cv = OpenCvSharp
Imports System.Numerics
Public Class Sieve_Basics : Inherits VBparent
    Dim printer As New Sieve_BasicsVB
    Dim sieve As New CS_Classes.Sieve
    Public Sub New()
        task.desc = "Implement the Sieve of Eratothenes in C#"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static countSlider = findSlider("Count of desired primes")
        setTrueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.value)))
    End Sub
End Class







' https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
Public Class Sieve_BasicsVB : Inherits VBparent
    Public primes As New List(Of Integer)
    Public Sub New()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "Count of desired primes", 1, 10000, 400)
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
        If standalone Then
            If output.Count > 0 Then setTrueText(shareResults(output))
        Else
            primes = New List(Of Integer)(output)
        End If
    End Sub
End Class







Public Class Sieve_Image : Inherits VBparent
    Dim zoom As New Pixel_Zoom
    Dim numArray(task.color.Total - 1) As Byte
    Dim referenceResults = New Dictionary(Of Integer, Integer) From
        {{10, 4}, {100, 25}, {1000, 168}, {10000, 1229}, {100000, 9592}, {1000000, 78498}, {10000000, 664579}, {100000000, 5761455}}
    Public Sub New()
        labels(2) = "NonZero pixels are primes"
        labels(3) = "Zoom output"
        task.desc = "Create an image marking primes"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim numCeiling = numArray.Length - 1
        ReDim numArray(numCeiling)
        numArray(0) = 255
        numArray(1) = 255
        For i = 2 To numCeiling \ 2 - 1
            For j = i + i To numCeiling - 1 Step i
                If numArray(j) <> 255 Then numArray(j) = 255
            Next
        Next

        Dim countPrimes As Integer
        For i = 2 To numCeiling - 1
            If numArray(i) = 0 Then countPrimes += 1
        Next

        If referenceResults.containskey(numCeiling) Then
            If referenceResults(numCeiling) <> countPrimes Then setTrueText("Invalid prime count - check this...")
        End If
        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, numArray.ToArray)
        cv.Cv2.BitwiseNot(dst2, dst2)
        zoom.RunClass(dst2)
        dst3 = zoom.dst2
    End Sub
End Class