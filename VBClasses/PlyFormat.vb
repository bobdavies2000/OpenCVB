Imports System.IO
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class PlyFormat_Basics : Inherits TaskParent
        Public options As New Options_PlyFormat
        Dim saveFileName As String
        Public Sub New()
            desc = "Create a .ply format file with the pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim fileInfo = New FileInfo(options.fileName)
            If saveFileName <> fileInfo.FullName Then
                Dim sw As New StreamWriter(fileInfo.FullName)
                saveFileName = fileInfo.FullName

                sw.WriteLine("ply")
                sw.WriteLine("format ascii 1.0")
                sw.WriteLine("element vertex " + CStr(taskAlg.pointCloud.Total))
                sw.WriteLine("property float x")
                sw.WriteLine("property float y")
                sw.WriteLine("property float z")
                sw.WriteLine("end_header")
                For y = 0 To taskAlg.pointCloud.Height - 1
                    For x = 0 To taskAlg.pointCloud.Width - 1
                        Dim vec = taskAlg.pointCloud.Get(Of cv.Vec3f)(y, x)
                        sw.WriteLine(Format(vec(0), fmt3) + " " + Format(vec(1), fmt3) + " " + Format(vec(2), fmt3))
                    Next
                Next
                sw.Close()
                taskAlg.Settings.plyFileName = saveFileName
            End If
            SetTrueText(".ply format file saved in " + options.fileName)
        End Sub
    End Class






    Public Class PlyFormat_PlusRGB : Inherits TaskParent
        Public options As New Options_PlyFormat
        Dim saveFileName As String
        Public Sub New()
            desc = "Save the pointcloud in .ply format and include the RGB data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim fileInfo = New FileInfo(options.fileName)
            If saveFileName <> fileInfo.FullName Then
                Try
                    Dim sw As New StreamWriter(fileInfo.FullName)
                    saveFileName = fileInfo.FullName

                    sw.WriteLine("ply")
                    sw.WriteLine("format ascii 1.0")
                    sw.WriteLine("element vertex " + CStr(taskAlg.pointCloud.Total))
                    sw.WriteLine("property float x")
                    sw.WriteLine("property float y")
                    sw.WriteLine("property float z")
                    sw.WriteLine("property uchar red")
                    sw.WriteLine("property uchar green")
                    sw.WriteLine("property uchar blue")

                    sw.WriteLine("end_header")
                    For y = 0 To taskAlg.pointCloud.Height - 1
                        For x = 0 To taskAlg.pointCloud.Width - 1
                            Dim vec = taskAlg.pointCloud.Get(Of cv.Vec3f)(y, x)
                            Dim bgr = src.Get(Of cv.Vec3b)(y, x)
                            sw.WriteLine(Format(vec(0), fmt3) + " " + Format(vec(1), fmt3) + " " + Format(vec(2), fmt3),
                                 CStr(bgr(2)) + " " + CStr(bgr(1)) + " " + CStr(bgr(0)))
                        Next
                    Next
                    sw.Close()
                    taskAlg.Settings.plyFileName = saveFileName
                Catch ex As Exception
                End Try
            End If
            SetTrueText(".ply format file saved in " + options.fileName)
        End Sub
    End Class

End Namespace