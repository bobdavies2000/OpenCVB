Imports cv = OpenCvSharp
Imports System.Numerics

Module Quaternion_module
    Public Function quaternion_exp(v As cv.Point3f) As Quaternion
        v *= 0.5
        Dim theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z
        Dim theta = Math.Sqrt(theta2)
        Dim c = Math.Cos(theta)
        Dim s = If(theta2 < Math.Sqrt(120 * Single.Epsilon), 1 - theta2 / 6, Math.Sin(theta) / theta2)
        Return New Quaternion(s * v.X, s * v.Y, s * v.Z, c)
    End Function
End Module

Public Class Quaterion_Basics
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 8)
            sliders.setupTrackBar(0, "quaternion A.x X100", -100, 100, -50)
            sliders.setupTrackBar(1, "quaternion A.y X100", -100, 100, 10)
            sliders.setupTrackBar(2, "quaternion A.z X100", -100, 100, 20)
            sliders.setupTrackBar(3, "quaternion Theta X100", -100, 100, 100)

            sliders.setupTrackBar(4, "quaternion B.x X100", -100, 100, -10)
            sliders.setupTrackBar(5, "quaternion B.y X100", -100, 100, -10)
            sliders.setupTrackBar(6, "quaternion B.z X100", -100, 100, -10)
            sliders.setupTrackBar(7, "quaternion Theta X100", -100, 100, 100)
        End If

        task.desc = "Use the quaternion values to multiply and compute conjugate"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim q1 = New Quaternion(CSng(sliders.trackbar(0).Value / 100), CSng(sliders.trackbar(1).Value / 100),
                                    CSng(sliders.trackbar(2).Value / 100), CSng(sliders.trackbar(3).Value / 100))
        Dim q2 = New Quaternion(CSng(sliders.trackbar(4).Value / 100), CSng(sliders.trackbar(5).Value / 100),
                                    CSng(sliders.trackbar(6).Value / 100), CSng(sliders.trackbar(7).Value / 100))

        Dim quatmul = Quaternion.Multiply(q1, q2)
        task.trueText("q1 = " + q1.ToString() + vbCrLf + "q2 = " + q2.ToString() + vbCrLf + "Multiply q1 * q2" + quatmul.ToString())
    End Sub
End Class




' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
Public Class Quaterion_IMUPrediction
    Inherits VBparent
    Dim host As IMU_HostFrameTimes
    Public Sub New()
        initParent()
        host = New IMU_HostFrameTimes()

        label1 = "Quaternion_IMUPrediction"
        label2 = ""
        task.desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
		' task.rank = 1
    End Sub

    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        host.Run()

        Dim dt = host.HostInterruptDelayEstimate

        Dim t = task.IMU_Translation
        Dim predictedTranslation = New cv.Point3f(dt * (dt / 2 * task.IMU_Acceleration.X + task.IMU_Velocity.X) + t.X,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Y + task.IMU_Velocity.Y) + t.Y,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Z + task.IMU_Velocity.Z) + t.Z)

        Dim predictedW = New cv.Point3f(dt * (dt / 2 * task.IMU_AngularAcceleration.X + task.IMU_AngularVelocity.X),
                                        dt * (dt / 2 * task.IMU_AngularAcceleration.Y + task.IMU_AngularVelocity.Y),
                                        dt * (dt / 2 * task.IMU_AngularAcceleration.Z + task.IMU_AngularVelocity.Z))

        Dim predictedRotation As New Quaternion
        predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), task.IMU_Rotation)

        Dim diffq = Quaternion.Subtract(task.IMU_Rotation, predictedRotation)

        task.trueText("IMU_Acceleration = " + vbTab +
                                 Format(task.IMU_Acceleration.X, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Acceleration.Y, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Acceleration.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "IMU_Velocity = " + vbTab + vbTab +
                                 Format(task.IMU_Velocity.X, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Velocity.Y, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Velocity.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "IMU_AngularAccel. = " + vbTab +
                                 Format(task.IMU_AngularAcceleration.X, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_AngularAcceleration.Y, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_AngularAcceleration.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "IMU_AngularVelocity = " + vbTab +
                                 Format(task.IMU_AngularVelocity.X, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_AngularVelocity.Y, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_AngularVelocity.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "dt = " + dt.ToString() + vbCrLf +
                                 "Pose quaternion = " + vbTab +
                                 Format(task.IMU_Rotation.X, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Rotation.Y, "0.0000") + ", " + vbTab +
                                 Format(task.IMU_Rotation.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "Prediction Rotation = " + vbTab +
                                 Format(predictedRotation.X, "0.0000") + ", " + vbTab +
                                 Format(predictedRotation.Y, "0.0000") + ", " + vbTab +
                                 Format(predictedRotation.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                 "difference = " + vbTab + vbTab +
                                 Format(diffq.X, "0.0000") + ", " + vbTab +
                                 Format(diffq.Y, "0.0000") + ", " + vbTab +
                                 Format(diffq.Z, "0.0000") + ", " + vbTab)
    End Sub
End Class



