Imports cvb = OpenCvSharp
Imports System.Numerics
Public Class Quaterion_Basics : Inherits TaskParent
    Dim options As New Options_Quaternion
    Public Sub New()
        desc = "Use the quaternion values to multiply and compute conjugate"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        Dim quatmul = Quaternion.Multiply(options.q1, options.q2)
        SetTrueText("q1 = " + options.q1.ToString() + vbCrLf + "q2 = " + options.q2.ToString() + vbCrLf +
                    "Multiply q1 * q2" + quatmul.ToString())
    End Sub
End Class






Public Class Options_Quaternion : Inherits TaskParent
    Public q1 As Quaternion = New Quaternion
    Public q2 As Quaternion = New Quaternion
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("quaternion A.x X100", -100, 100, -50)
            sliders.setupTrackBar("quaternion A.y X100", -100, 100, 10)
            sliders.setupTrackBar("quaternion A.z X100", -100, 100, 20)
            sliders.setupTrackBar("quaternion A Theta X100", -100, 100, 100)

            sliders.setupTrackBar("quaternion B.x X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B.y X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B.z X100", -100, 100, -10)
            sliders.setupTrackBar("quaternion B Theta X100", -100, 100, 100)
        End If
    End Sub
    Public Sub RunOpt()
        Static axSlider = FindSlider("quaternion A.x X100")
        Static aySlider = FindSlider("quaternion A.y X100")
        Static azSlider = FindSlider("quaternion A.z X100")
        Static athetaSlider = FindSlider("quaternion A Theta X100")

        Static bxSlider = FindSlider("quaternion B.x X100")
        Static bySlider = FindSlider("quaternion B.y X100")
        Static bzSlider = FindSlider("quaternion B.z X100")
        Static bthetaSlider = FindSlider("quaternion B Theta X100")

        q1 = New Quaternion(CSng(axSlider.Value / 100), CSng(aySlider.Value / 100),
                                CSng(azSlider.Value / 100), CSng(athetaSlider.Value / 100))
        q2 = New Quaternion(CSng(bxSlider.Value / 100), CSng(bySlider.Value / 100),
                                    CSng(bzSlider.Value / 100), CSng(bthetaSlider.Value / 100))
    End Sub
End Class






' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
Public Class Quaterion_IMUPrediction : Inherits TaskParent
    Dim host As New IMU_PlotHostFrameTimes
    Public Sub New()
        labels(2) = "Quaternion_IMUPrediction"
        labels(3) = ""
        desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
    End Sub
    Public Function quaternion_exp(v As cvb.Point3f) As Quaternion
        v *= 0.5
        Dim theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z
        Dim theta = Math.Sqrt(theta2)
        Dim c = Math.Cos(theta)
        Dim s = If(theta2 < Math.Sqrt(120 * Single.Epsilon), 1 - theta2 / 6, Math.Sin(theta) / theta2)
        Return New Quaternion(s * v.X, s * v.Y, s * v.Z, c)
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        host.Run(src)

        Dim dt = host.HostInterruptDelayEstimate

        Dim t = task.IMU_Translation
        Dim predictedTranslation = New cvb.Point3f(dt * (dt / 2 * task.IMU_Acceleration.X + task.IMU_AngularVelocity.X) + t.X,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Y + task.IMU_AngularVelocity.Y) + t.Y,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Z + task.IMU_AngularVelocity.Z) + t.Z)

        Dim predictedW = New cvb.Point3f(dt * (dt / 2 * task.IMU_AngularAcceleration.X + task.IMU_AngularVelocity.X),
                                        dt * (dt / 2 * task.IMU_AngularAcceleration.Y + task.IMU_AngularVelocity.Y),
                                        dt * (dt / 2 * task.IMU_AngularAcceleration.Z + task.IMU_AngularVelocity.Z))

        Dim predictedRotation As New Quaternion
        predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), task.IMU_Rotation)

        Dim diffq = Quaternion.Subtract(task.IMU_Rotation, predictedRotation)

        SetTrueText("IMU_Acceleration = " + vbTab +
                                 Format(task.IMU_Acceleration.X, fmt3) + vbTab +
                                 Format(task.IMU_Acceleration.Y, fmt3) + vbTab +
                                 Format(task.IMU_Acceleration.Z, fmt3) + vbTab + vbCrLf +
                    "IMU_AngularAccel. = " + vbTab +
                                 Format(task.IMU_AngularAcceleration.X, fmt3) + vbTab +
                                 Format(task.IMU_AngularAcceleration.Y, fmt3) + vbTab +
                                 Format(task.IMU_AngularAcceleration.Z, fmt3) + vbTab + vbCrLf +
                    "IMU_AngularVelocity = " + vbTab +
                                 Format(task.IMU_AngularVelocity.X, fmt3) + vbTab +
                                 Format(task.IMU_AngularVelocity.Y, fmt3) + vbTab +
                                 Format(task.IMU_AngularVelocity.Z, fmt3) + vbTab + vbCrLf + vbCrLf +
                    "dt = " + dt.ToString() + vbCrLf + vbCrLf +
                    "Pose quaternion = " + vbTab +
                                 Format(task.IMU_Rotation.X, fmt3) + vbTab +
                                 Format(task.IMU_Rotation.Y, fmt3) + vbTab +
                                 Format(task.IMU_Rotation.Z, fmt3) + vbTab + vbCrLf +
                    "Prediction Rotation = " + vbTab +
                                 Format(predictedRotation.X, fmt3) + vbTab +
                                 Format(predictedRotation.Y, fmt3) + vbTab +
                                 Format(predictedRotation.Z, fmt3) + vbTab + vbCrLf +
                    "difference = " + vbTab + vbTab +
                                 Format(diffq.X, fmt3) + vbTab +
                                 Format(diffq.Y, fmt3) + vbTab +
                                 Format(diffq.Z, fmt3) + vbTab)
    End Sub
End Class



