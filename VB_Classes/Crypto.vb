﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports  System.IO
' https://www.codeproject.com/Tips/5308853/Prefer-using-Stream-to-byte
Public Class Crypto_Hash : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public Sub New()
        desc = "Experiment with hashing algorithm and guid"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static images As New List(Of cv.Mat)
        Static guids As New List(Of String)
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

            guids.Add((New Guid(bytes)).ToString)
            flow.msgs.Clear()
            For i = 0 To guids.Count - 1
                flow.msgs.Add(guids(i))
            Next
            If guids.Count >= 25 Then guids.RemoveAt(0)
            flow.Run(Nothing)
        End If
    End Sub
End Class