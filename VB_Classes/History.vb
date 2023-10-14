Imports cv = OpenCvSharp
Public Class History_Sum8u : Inherits VB_Algorithm
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history and sum the last X frames - not that saturation is permitted."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.historyCount = 1 Then
            dst2 = src
            Exit Sub
        End If

        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If firstPass Then
            If dst2.Type <> input.Type Or dst2.Channels <> input.Channels Then dst2 = New cv.Mat(input.Size, input.Type, 0)
        End If

        If task.optionsChanged Then
            saveFrames.Clear()
            dst2.SetTo(0)
        End If

        If saveFrames.Count >= task.historyCount Then
            dst2 = dst2.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(input)
        dst2 = input + dst2
    End Sub
End Class





Public Class History_Sum8uNoSaturation : Inherits VB_Algorithm
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history and sum the last X frames (without saturation!)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If input.Type <> cv.MatType.CV_32F Then input.ConvertTo(input, cv.MatType.CV_32F)
        If dst3.Type <> input.Type Or dst3.Channels <> input.Channels Then dst3 = New cv.Mat(input.Size, input.Type, 0)
        input.ConvertTo(input, cv.MatType.CV_32F)
        input /= 255 ' input is all zeros or ones.

        If task.optionsChanged Then
            saveFrames.Clear()
            dst3.SetTo(0)
        End If

        If saveFrames.Count >= task.historyCount Then
            dst3 = dst3.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(input)
        dst3 += input
        dst1 = 255 * dst3 / saveFrames.Count
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class








Public Class History_Sum32f : Inherits VB_Algorithm
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history to sum the last X frames"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If input.Type <> cv.MatType.CV_32F Then input.ConvertTo(input, cv.MatType.CV_32F)

        If dst2.Type <> input.Type Or dst2.Channels <> input.Channels Then dst2 = New cv.Mat(input.Size, input.Type, 0)

        If task.optionsChanged Then
            saveFrames.Clear()
            dst2.SetTo(0)
        End If

        If saveFrames.Count >= task.historyCount Then
            dst2 = dst2.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(input)
        dst2 = input + dst2
    End Sub
End Class







Public Class History_Average : Inherits VB_Algorithm
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history and average the last X frames"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_32F Then input.ConvertTo(input, cv.MatType.CV_32F)
        If input.Channels <> dst1.Channels Then dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)

        If task.optionsChanged Then
            saveFrames.Clear()
            dst1 = input
        Else
            dst1 += input
        End If

        saveFrames.Add(input)
        If saveFrames.Count > task.historyCount Then
            dst1 -= saveFrames(0)
            saveFrames.RemoveAt(0)
        End If

        dst2 = dst1 / saveFrames.Count
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC3)
        labels(2) = "The image below is composed of " + CStr(saveFrames.Count) + " BGR frames"
    End Sub
End Class








Public Class History_MaskCopy : Inherits VB_Algorithm
    Public Sub New()
        desc = "Create a frame history that creates a mask for the current frame and copies merges it with the last X frames"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src.Clone
        Static countFrames As Integer
        If task.optionsChanged Or countFrames >= task.historyCount Then
            dst2 = input
            countFrames = 1
        Else
            Dim mask = input.ConvertScaleAbs()
            input.CopyTo(dst2, mask)
            countFrames += 1
        End If
        If heartBeat() Then labels(2) = "The image below is composed from the last " + CStr(countFrames) + " frames"
        setTrueText("The image below is composed from the last " + CStr(countFrames) + " frames", 3)
    End Sub
End Class








Public Class History_MaskCopy8U : Inherits VB_Algorithm
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a frame history that creates a mask for the current frame and copies merges it with the last X frames"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src.Clone
        Static countFrames As Integer
        If task.optionsChanged Or task.frameCount Mod task.historyCount = 0 Then
            dst2 = input
            countFrames = 1
        Else
            input.CopyTo(dst2, input)
            countFrames += 1
            If heartBeat() Then labels(2) = "The image below is composed from the last " + CStr(countFrames) + " frames"
        End If
        setTrueText("The image below is composed from the last " + CStr(countFrames) + " frames", 3)
    End Sub
End Class







Public Class History_Cloud : Inherits VB_Algorithm
    Public sum8u As New History_Sum8uNoSaturation
    Public Sub New()
        desc = "Create a frame history and sum the last X task.pointcloud's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveFrames As New List(Of cv.Mat)

        If src.Type <> cv.MatType.CV_32FC3 Or src.Channels <> 3 Then src = task.pointCloud

        If task.optionsChanged Or dst3.Type <> cv.MatType.CV_32FC3 Then
            saveFrames.Clear()
            dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        End If

        If saveFrames.Count >= task.historyCount Then
            dst3 = dst3.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(src)
        dst3 = src + dst3
        dst2 = dst3 / saveFrames.Count

        sum8u.Run(task.depthMask)
        dst2.SetTo(0, Not sum8u.dst2)
    End Sub
End Class