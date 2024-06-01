Imports System.IO
Imports cv = OpenCvSharp
Public Class PlyFormat_Basics : Inherits VB_Algorithm
    Public fileNameForm As OptionsFileName
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = "c:\tmp"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "ply (*.ply)|*.ply|All files (*.*)|*.*"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "plyFileName", "plyFileName", "c:\tmp\pointcloud.ply")
        fileNameForm.Text = "Select ply output file"
        fileNameForm.FileNameLabel.Text = "Select ply output file"
        fileNameForm.PlayButton.Text = "Save"
        fileNameForm.TrackBar1.Visible = False
        fileNameForm.Setup(traceName)
        fileNameForm.Show()

        desc = "Create a .ply format file with the pointcloud."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If firstPass Then fileNameForm.Left = allOptions.Width / 3
        If fileNameForm.PlayButton.Text = "Save" Then Exit Sub
        fileNameForm.PlayButton.Text = "Save"

        Dim fileInfo = New FileInfo(fileNameForm.filename.Text)
        Dim sw As New StreamWriter(fileInfo.FullName)

        sw.WriteLine("ply")
        sw.WriteLine("format ascii 1.0")
        sw.WriteLine("element vertex " + CStr(task.pointCloud.Total))
        sw.WriteLine("property float x")
        sw.WriteLine("property float y")
        sw.WriteLine("property float z")
        sw.WriteLine("end_header")
        For y = 0 To task.pointCloud.Height - 1
            For x = 0 To task.pointCloud.Width - 1
                Dim vec = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                sw.WriteLine(Format(vec(0), fmt3) + " " + Format(vec(1), fmt3) + " " + Format(vec(2), fmt3))
            Next
        Next
        sw.Close()
    End Sub
End Class






Public Class PlyFormat_PlusRGB : Inherits VB_Algorithm
    Dim ply As New PlyFormat_Basics
    Public Sub New()
        desc = "Save the pointcloud in .ply format and include the RGB data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If firstPass Then ply.fileNameForm.Left = allOptions.Width / 3
        If ply.fileNameForm.PlayButton.Text = "Save" Then Exit Sub
        ply.fileNameForm.PlayButton.Text = "Save"

        Dim fileInfo = New FileInfo(ply.fileNameForm.filename.Text)
        Dim sw As New StreamWriter(fileInfo.FullName)

        sw.WriteLine("ply")
        sw.WriteLine("format ascii 1.0")
        sw.WriteLine("element vertex " + CStr(task.pointCloud.Total))
        sw.WriteLine("property float x")
        sw.WriteLine("property float y")
        sw.WriteLine("property float z")
        sw.WriteLine("property uchar red")
        sw.WriteLine("property uchar green")
        sw.WriteLine("property uchar blue")

        sw.WriteLine("end_header")
        For y = 0 To task.pointCloud.Height - 1
            For x = 0 To task.pointCloud.Width - 1
                Dim vec = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                Dim bgr = src.Get(Of cv.Vec3b)(y, x)
                sw.WriteLine(Format(vec(0), fmt3) + " " + Format(vec(1), fmt3) + " " + Format(vec(2), fmt3),
                             CStr(bgr(2)) + " " + CStr(bgr(1)) + " " + CStr(bgr(0)))
            Next
        Next
        sw.Close()
    End Sub
End Class
