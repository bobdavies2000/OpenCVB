Imports cvb = OpenCvSharp
Public Class GrayToColor_Palette : Inherits TaskParent
    Dim flood As New Flood_Basics
    Public Sub New()
        labels = {"", "Right View", "", "Grayscale left view after palette applied."}
        If standalone Then task.gOptions.setDisplay1()
        desc = "Identify the main colors in an image using RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)

        Dim indices(255) As Byte
        Dim colors(255) As cvb.Vec3b
        Dim sorted As New SortedList(Of Integer, cvb.Vec3b)(New compareAllowIdenticalInteger)
        For Each rc In task.redCells
            Dim index = rc.naturalGray
            If index = 0 Then Continue For
            colors(index) = rc.naturalColor
            indices(index) = index
            sorted.Add(index, rc.naturalColor)
        Next

        Dim firstIndex = sorted.ElementAt(0).Key
        Dim lastColor = colors(firstIndex)
        For i = 0 To colors.Count - 1
            If indices(i) = 0 Then colors(i) = lastColor Else lastColor = colors(i)
        Next

        dst1 = task.rightView
        Dim colorMap = cvb.Mat.FromPixelData(256, 1, cvb.MatType.CV_8UC3, colors.ToArray)
        cvb.Cv2.ApplyColorMap(task.leftView, dst3, colorMap)
    End Sub
End Class