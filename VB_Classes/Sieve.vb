Imports cv = OpenCvSharp
Imports System.Numerics
' https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
Public Class Sieve_BasicsVB : Inherits VB_Parent
    Public primes As New List(Of Integer)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Count of desired primes", 1, 10000, 400)
        desc = "Implement the Sieve of Eratothenes"
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
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = FindSlider("Count of desired primes")
        Dim count = countSlider.Value
        Dim nextEntry As Integer = 2
        Dim output = New List(Of Integer)
        While output.Count < count
            If output.All(Function(x)
                              If nextEntry Mod x <> 0 Then Return True
                              Return False
                          End Function) Then output.Add(nextEntry)
            nextEntry += 1
        End While
        If standaloneTest() Then
            If output.Count > 0 Then setTrueText(shareResults(output))
        Else
            primes = New List(Of Integer)(output)
        End If
        labels(2) = "The first " + CStr(output.Count) + " primes are below"
    End Sub
End Class







Public Class Sieve_Image : Inherits VB_Parent
    Dim zoom As New Pixel_Zoom
    Dim numArray() As Byte
    Dim referenceResults = New Dictionary(Of Integer, Integer) From
        {{10, 4}, {100, 25}, {1000, 168}, {10000, 1229}, {100000, 9592}, {1000000, 78498}, {10000000, 664579}, {100000000, 5761455}}
    Public Sub New()
        ReDim numArray(dst2.Total - 1)
        labels(2) = "NonZero pixels are primes"
        labels(3) = "Zoom output"
        desc = "Create an image marking primes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
        dst2 = Not dst2
        zoom.Run(dst2)
        dst3 = zoom.dst2
    End Sub
End Class