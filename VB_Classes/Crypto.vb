Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
' https://www.codeproject.com/Tips/5308853/Prefer-using-Stream-to-byte
Public Class Crypto_Hash : Inherits VB_Parent
    Dim flow As New Font_FlowText
    Dim images As New List(Of cv.Mat)
    Dim guids As New List(Of String)
    Public Sub New()
        desc = "Experiment with hashing algorithm and guid"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim iSize = src.Total * src.ElemSize
        Dim maxImages = 10
        images.Add(src)
        If images.Count >= maxImages Then
            Dim bytes(iSize * maxImages - 1) As Byte
            images.RemoveAt(0)

            Dim index As Integer = 0
            For Each mat In images
                Marshal.Copy(mat.Data, bytes, iSize * index, iSize)
            Next

            Dim algorithm = MD5.Create()
            bytes = algorithm.ComputeHash(bytes)

            flow.nextmsg = New Guid(bytes).ToString
            If guids.Count >= flow.flowText.Count Then guids.RemoveAt(0)
            guids.Add(flow.nextmsg)
            flow.Run(empty)
        End If
    End Sub
End Class