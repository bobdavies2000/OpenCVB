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

Public Class Quaterion_Basics : Inherits VB_Parent
    Dim options As New Options_Quaternion
    Public Sub New()
        desc = "Use the quaternion values to multiply and compute conjugate"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        Dim quatmul = Quaternion.Multiply(options.q1, options.q2)
        SetTrueText("q1 = " + options.q1.ToString() + vbCrLf + "q2 = " + options.q2.ToString() + vbCrLf +
                    "Multiply q1 * q2" + quatmul.ToString())
    End Sub
End Class




' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
Public Class Quaterion_IMUPrediction : Inherits VB_Parent
    Dim host As New IMU_PlotHostFrameTimes
    Public Sub New()
        labels(2) = "Quaternion_IMUPrediction"
        labels(3) = ""
        desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        host.Run(src)

        Dim dt = host.HostInterruptDelayEstimate

        Dim t = task.IMU_Translation
        Dim predictedTranslation = New cv.Point3f(dt * (dt / 2 * task.IMU_Acceleration.X + task.IMU_AngularVelocity.X) + t.X,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Y + task.IMU_AngularVelocity.Y) + t.Y,
                                                  dt * (dt / 2 * task.IMU_Acceleration.Z + task.IMU_AngularVelocity.Z) + t.Z)

        Dim predictedW = New cv.Point3f(dt * (dt / 2 * task.IMU_AngularAcceleration.X + task.IMU_AngularVelocity.X),
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



