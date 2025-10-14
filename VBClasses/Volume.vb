Imports cv = OpenCvSharp
Public Class Volume_Basics : Inherits TaskParent
    Public rc As New oldrcData
    Public volume As Single
    Public Sub New()
        desc = "Build a box containing all the 3D points of a RedCloud cell"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedList(src, labels(2))
            rc = task.rcD
        End If

        Dim xList As New List(Of Single)
        Dim yList As New List(Of Single)
        Dim zList As New List(Of Single)
        For Each pt In rc.contour
            Dim vec = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            If vec(0) <> 0 Then xList.Add(vec(0))
            If vec(1) <> 0 Then yList.Add(vec(1))
            If vec(2) <> 0 Then zList.Add(vec(2))
        Next

        Dim minX As Single, maxX As Single, minY As Single, maxY As Single, minZ As Single, maxZ As Single
        If xList.Count > 0 Then minX = xList.Min
        If yList.Count > 0 Then minY = yList.Min
        If zList.Count > 0 Then minZ = zList.Min

        If xList.Count > 0 Then maxX = xList.Max
        If yList.Count > 0 Then maxY = yList.Max
        If zList.Count > 0 Then maxZ = zList.Max

        Dim meterFactor As Integer = 100
        Dim mString = If(meterFactor = 100, "centimeters", If(meterFactor = 1, "meters", "decimeters"))
        volume = (maxX - minX) * (maxY - minY) * (maxZ - minZ) * meterFactor * meterFactor * meterFactor
        If task.heartBeat Then
            strOut = "Volume = " + Format(volume, fmt0) + " cubic " + mString + vbCrLf + vbCrLf
            strOut += "Min " + vbTab + "Max " + vbTab + "Range " + vbTab + " units=" + mString + vbCrLf
            strOut += Format(minX * meterFactor, fmt0) + vbTab + Format(maxX * meterFactor, fmt0) + vbTab + Format((maxX - minX) * meterFactor, fmt0) + vbTab + " X dimension" + vbCrLf
            strOut += Format(minY * meterFactor, fmt0) + vbTab + Format(maxY * meterFactor, fmt0) + vbTab + Format((maxY - minY) * meterFactor, fmt0) + vbTab + " Y dimension" + vbCrLf
            strOut += Format(minZ * meterFactor, fmt0) + vbTab + Format(maxZ * meterFactor, fmt0) + vbTab + Format((maxZ - minZ) * meterFactor, fmt0) + vbTab + " Z dimension" + vbCrLf
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class