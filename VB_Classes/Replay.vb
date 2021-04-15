Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Module recordPlaybackCommon
    Public bytesPerColor As Int64
    Public bytesPerdepth32f As Int64
    Public bytesPerRGBDepth As Int64
    Public bytesPerCloud As Int64
    Public Structure fileHeader
        Public pcBufferSize As integer ' indicates that a point cloud is in the data stream.

        Public colorWidth As integer
        Public colorHeight As integer
        Public colorElemsize As integer

        Public depthWidth As integer
        Public depthHeight As Integer
        Public depth32fElemsize As Integer

        Public RGBDepthWidth As integer
        Public RGBDepthHeight As integer
        Public RGBDepthElemsize As integer

        Public cloudWidth As integer
        Public cloudHeight As integer
        Public cloudElemsize As integer
    End Structure
    Public Sub writeHeader(binWrite As BinaryWriter)
        binWrite.Write(task.color.Width)
        binWrite.Write(task.color.Height)
        binWrite.Write(task.color.ElemSize)

        binWrite.Write(task.depth32f.Width)
        binWrite.Write(task.depth32f.Height)
        binWrite.Write(task.depth32f.ElemSize)

        binWrite.Write(task.RGBDepth.Width)
        binWrite.Write(task.RGBDepth.Height)
        binWrite.Write(task.RGBDepth.ElemSize)

        binWrite.Write(task.pointCloud.Width)
        binWrite.Write(task.pointCloud.Height)
        binWrite.Write(task.pointCloud.ElemSize)
    End Sub
    Public Sub readHeader(ByRef header As fileHeader, binRead As BinaryReader)
        header.colorWidth = binRead.ReadInt32()
        header.colorHeight = binRead.ReadInt32()
        header.colorElemsize = binRead.ReadInt32()

        header.depthWidth = binRead.ReadInt32()
        header.depthHeight = binRead.ReadInt32()
        header.depth32fElemsize = binRead.ReadInt32()

        header.RGBDepthWidth = binRead.ReadInt32()
        header.RGBDepthHeight = binRead.ReadInt32()
        header.RGBDepthElemsize = binRead.ReadInt32()

        header.cloudWidth = binRead.ReadInt32()
        header.cloudHeight = binRead.ReadInt32()
        header.cloudElemsize = binRead.ReadInt32()
    End Sub
End Module




Public Class Replay_Record : Inherits VBparent
    Dim binWrite As BinaryWriter
    Dim recordingActive As Boolean
    Dim colorBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim depth32fBytes() As Byte
    Dim cloudBytes() As Byte
    Dim maxBytes As Single = 20000000000
    Dim recordingFilename As FileInfo
    Dim fileNameForm As OptionsFileName
    Public Sub New()
        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.parms.homeDir + "Data/"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "ocvb (*.ocvb)|*.ocvb"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", task.parms.homeDir + "Recording.ocvb")
        fileNameForm.Text = "Select an OpenCVB bag file to create"
        fileNameForm.FileNameLabel.Text = "Select a file to record all the image data."
        fileNameForm.Setup(caller)
        fileNameForm.Show()

        task.desc = "Create a recording of camera data that contains color, depth, RGBDepth, pointCloud, and IMU data in an .bob file."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(fileNameForm.filename.Text)
        If task.parms.useRecordedData And recordingFilename.Exists = False Then
            task.trueText("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125)
            Exit Sub
        End If

        If fileNameForm.FileStarted Then
            If recordingActive = False Then
                bytesPerColor = task.color.Total * task.color.ElemSize
                bytesPerRGBDepth = task.RGBDepth.Total * task.RGBDepth.ElemSize
                bytesPerdepth32f = task.depth32f.Total * task.depth32f.ElemSize
                ' start recording...
                ReDim colorBytes(bytesPerColor - 1)
                ReDim depth32fBytes(bytesPerdepth32f - 1)
                ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
                Dim pcSize = task.pointCloud.Total * task.pointCloud.ElemSize
                ReDim cloudBytes(pcSize - 1)

                binWrite = New BinaryWriter(File.Open(recordingFilename.FullName, FileMode.Create))
                recordingActive = True
                writeHeader(binWrite)
            Else
                Marshal.Copy(task.color.Data, colorBytes, 0, colorBytes.Length)
                binWrite.Write(colorBytes)
                bytesTotal += colorBytes.Length

                Marshal.Copy(task.depth32f.Data, depth32fBytes, 0, depth32fBytes.Length)
                binWrite.Write(depth32fBytes)
                bytesTotal += depth32fBytes.Length

                Marshal.Copy(task.RGBDepth.Data, RGBDepthBytes, 0, RGBDepthBytes.Length)
                binWrite.Write(RGBDepthBytes)
                bytesTotal += RGBDepthBytes.Length

                Marshal.Copy(task.pointCloud.Data, cloudBytes, 0, cloudBytes.Length)
                binWrite.Write(cloudBytes)
                bytesTotal += cloudBytes.Length

                If bytesTotal >= maxBytes Then
                    recordingActive = False
                Else
                    fileNameForm.TrackBar1.Value = 10000 * bytesTotal / maxBytes
                End If
            End If
        Else
            If recordingActive Then
                ' stop recording
                binWrite.Close()
                recordingActive = False
            End If
        End If
    End Sub
    Public Sub Close()
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If recordingActive Then binWrite.Close()
    End Sub
End Class





Public Class Replay_Play : Inherits VBparent
    Dim binRead As BinaryReader
    Dim playbackActive As Boolean
    Dim colorBytes() As Byte
    Dim depth32fBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim cloudBytes() As Byte
    Dim fh As New fileHeader
    Dim fs As FileStream
    Dim recordingFilename As FileInfo
    Dim fileNameForm As OptionsFileName
    Public Sub New()

        fileNameForm = New OptionsFileName
        fileNameForm.OpenFileDialog1.InitialDirectory = task.parms.homeDir + "Data/"
        fileNameForm.OpenFileDialog1.FileName = "*.*"
        fileNameForm.OpenFileDialog1.CheckFileExists = False
        fileNameForm.OpenFileDialog1.Filter = "ocvb (*.ocvb)|*.ocvb"
        fileNameForm.OpenFileDialog1.FilterIndex = 1
        fileNameForm.filename.Text = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", task.parms.homeDir + "Recording.ocvb")
        fileNameForm.Text = "Select an OpenCVB bag file to create"
        fileNameForm.FileNameLabel.Text = "Select an OpenCVB bag file to read"
        fileNameForm.Setup(caller)
        fileNameForm.Show()

        task.desc = "Playback a file recorded by OpenCVB"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(fileNameForm.filename.Text)
        If recordingFilename.Exists = False Then task.trueText("File not found: " + recordingFilename.FullName, 10, 125)
        If fileNameForm.fileStarted And recordingFilename.Exists Then
            Dim maxBytes = recordingFilename.Length
            If playbackActive Then
                colorBytes = binRead.ReadBytes(bytesPerColor)
                Dim tmpMat = New cv.Mat(fh.colorHeight, fh.colorWidth, cv.MatType.CV_8UC3, colorBytes)
                task.color = tmpMat.Resize(task.color.Size())
                bytesTotal += colorBytes.Length

                depth32fBytes = binRead.ReadBytes(bytesPerdepth32f)
                tmpMat = New cv.Mat(fh.depthHeight, fh.depthWidth, cv.MatType.CV_16U, depth32fBytes)
                bytesTotal += depth32fBytes.Length

                RGBDepthBytes = binRead.ReadBytes(bytesPerRGBDepth)
                tmpMat = New cv.Mat(fh.RGBDepthHeight, fh.RGBDepthWidth, cv.MatType.CV_8UC3, RGBDepthBytes)
                task.RGBDepth = tmpMat.Resize(task.RGBDepth.Size())
                bytesTotal += RGBDepthBytes.Length

                cloudBytes = binRead.ReadBytes(bytesPerCloud)
                task.pointCloud = New cv.Mat(fh.cloudHeight, fh.cloudWidth, cv.MatType.CV_32FC3, cloudBytes)  ' we cannot resize the point cloud.
                bytesTotal += cloudBytes.Length

                ' restart the video at the beginning.
                If binRead.PeekChar < 0 Then
                    binRead.Close()
                    playbackActive = False
                    bytesTotal = 0
                End If
                fileNameForm.TrackBar1.Value = 10000 * bytesTotal / recordingFilename.Length
                dst1 = task.color.Clone()
                dst2 = task.RGBDepth.Clone()
            Else
                ' start playback...
                fs = New FileStream(recordingFilename.FullName, FileMode.Open, FileAccess.Read)
                binRead = New BinaryReader(fs)
                readHeader(fh, binRead)

                If fh.cloudWidth = src.Width Then ' the current width/height don't agree with the recorded data.  Often happens during "Test All"
                    bytesPerColor = fh.colorWidth * fh.colorHeight * fh.colorElemsize
                    bytesPerdepth32f = fh.cloudWidth * fh.cloudHeight * fh.depth32fElemsize
                    bytesPerRGBDepth = fh.colorWidth * fh.colorHeight * fh.RGBDepthElemsize
                    bytesPerCloud = fh.cloudWidth * fh.cloudHeight * fh.cloudElemsize

                    ReDim colorBytes(bytesPerColor - 1)
                    ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
                    ReDim cloudBytes(bytesPerCloud - 1)
                    playbackActive = True
                Else
                    task.trueText("Recorded data was saved at " + CStr(fh.cloudWidth) + "x" + CStr(fh.cloudHeight) + vbCrLf +
                                      "and the current format is " + CStr(src.Width) + "x" + CStr(src.Height))
                End If
            End If
        Else
            If playbackActive Then
                ' stop playback
                binRead.Close()
                playbackActive = False
            End If
        End If
    End Sub
    Public Sub Close()
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If playbackActive Then binRead.Close()
    End Sub
End Class





Public Class Replay_OpenGL : Inherits VBparent
    Dim ogl As OpenGL_Callbacks
    Dim replay As Replay_Play
    Public Sub New()
        ogl = New OpenGL_Callbacks()
        replay = New Replay_Play()
        task.desc = "Replay a recorded session with OpenGL"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        replay.Run(src)
        ogl.pointCloudInput = task.pointCloud
        ogl.Run(task.color)
    End Sub
End Class


