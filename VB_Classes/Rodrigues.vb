'Imports cv = OpenCvSharp
'Imports System.Runtime.InteropServices
'Module Rodrigues_Basics_Exports
'    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function K4ARodrigues() As IntPtr
'    End Function
'End Module


'Public Class Rodrigues_ValidateKinect : Inherits VB_Algorithm
'    Public Sub New()
'        desc = "Validate the Rodrigues calibration for K4A camera (only)"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If task.cameraName <> "Azure Kinect 4K" Then
'            dst3.SetTo(0)
'            setTrueText("Only the K4A4Azure camera is currently supported for the Rodrigues calibration")
'            Exit Sub
'        End If

'        Dim out As IntPtr = K4ARodrigues()
'        Dim msg = Marshal.PtrToStringAnsi(out)
'        Dim split As String() = msg.Split(vbLf)

'        Dim output As String = ""
'        For i = 0 To split.Length - 1
'            output += split(i) + vbCrLf
'        Next
'        setTrueText(output)
'    End Sub
'End Class




'Public Class Rodrigues_ValidateVector : Inherits VB_Algorithm
'    Public Sub New()
'        desc = "Validate the Rodrigues calibration for StereoLabs ZED 2/2i (only)"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If task.cameraName <> "StereoLabs ZED 2/2i" Then
'            dst3.SetTo(0)
'            setTrueText("Only the StereoLabs Zed 2/2i and Intel T265 cameras are supported for this Rodrigues validation")
'            Exit Sub
'        End If

'        Dim rot = task.parms.RotationMatrix
'        Dim output = "IMU Rotation Matrix for Zed 2 camera" + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
'        Next

'        src = New cv.Mat(3, 3, cv.MatType.CV_32F, task.parms.RotationMatrix)
'        Dim dst2 As New cv.Mat(3, 1, src.Type)
'        cv.Cv2.Rodrigues(src, dst2)

'        output += vbCrLf + "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(dst2.Get(Of Single)(i), "#0.000000000") + vbTab
'        Next

'        output += vbCrLf + "Rotation Vector from IMU: " + vbCrLf
'        output += vbTab + Format(task.parms.RotationVector.X, "#0.000000000") + vbTab
'        output += vbTab + Format(task.parms.RotationVector.Y, "#0.000000000") + vbTab
'        output += vbTab + Format(task.parms.RotationVector.Z, "#0.000000000") + vbTab
'        setTrueText(output)
'    End Sub
'End Class





'Public Class Rodrigues_RotationMatrix : Inherits VB_Algorithm
'    Public Sub New()
'        desc = "Display the contents of the IMU Rotation Matrix"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Dim rot = task.parms.RotationMatrix
'        Dim output = "IMU Rotation Matrix (rotate the camera to see if it is working)" + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
'        Next

'        src = New cv.Mat(3, 3, cv.MatType.CV_32F, task.parms.RotationMatrix)
'        Dim dst2 As New cv.Mat(3, 1, src.Type, 3)
'        cv.Cv2.Rodrigues(src, dst2)

'        output += vbCrLf + "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(dst2.Get(Of Single)(i), "#0.000000000") + vbTab
'        Next
'        setTrueText(output)
'    End Sub
'End Class







'Public Class Rodrigues_Extrinsics : Inherits VB_Algorithm
'    Public Sub New()
'        desc = "Convert Camera extrinsics array to a Vector with Rodrigues"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If task.cameraName = "Oak-D camera" Then Exit Sub
'        Dim rot = task.parms.extrinsics.rotation
'        Dim output As String = "Extrinsics Rotation Matrix" + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(rot(i * 3), fmt2) + vbTab + Format(rot(i * 3 + 1), fmt2) + vbTab + Format(rot(i * 3 + 2), fmt2) + vbCrLf
'        Next

'        Dim src32f As New cv.Mat(3, 3, cv.MatType.CV_32F, task.parms.extrinsics.rotation)
'        src32f.ConvertTo(src, cv.MatType.CV_64F)
'        Dim Jacobian As New cv.Mat(9, 3, src.Type, 0)
'        Dim dst2 As New cv.Mat(3, 1, src.Type, 3)
'        cv.Cv2.Rodrigues(src, dst2)

'        output += "Extrinsic Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
'        For i = 0 To 2
'            output += vbTab + Format(dst2.Get(Of Double)(i), "#0.000000000") + vbTab
'        Next
'        setTrueText(output)
'    End Sub
'End Class



