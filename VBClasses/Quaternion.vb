Imports cv = OpenCvSharp
Imports System.Numerics
Namespace VBClasses
    Public Class Quaternion_Basics : Inherits TaskParent
        Dim options As New Options_Quaternion
        Public Sub New()
            desc = "Use the quaternion values to multiply and compute conjugate"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim quatmul = Quaternion.Multiply(options.q1, options.q2)
            SetTrueText("q1 = " + options.q1.ToString() + vbCrLf + "q2 = " + options.q2.ToString() + vbCrLf +
                        "Multiply q1 * q2" + quatmul.ToString())
        End Sub
    End Class







    ' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
    Public Class Quaternion_IMUPrediction : Inherits TaskParent
        Dim host As New IMU_PlotHostFrameTimes
        Public Sub New()
            labels(2) = "NR_Quaternion_IMUPrediction"
            labels(3) = ""
            desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
        End Sub
        Public Function quaternion_exp(v As cv.Point3f) As Quaternion
            v *= 0.5
            Dim theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z
            Dim theta = Math.Sqrt(theta2)
            Dim c = Math.Cos(theta)
            Dim s = If(theta2 < Math.Sqrt(120 * Single.Epsilon), 1 - theta2 / 6, Math.Sin(theta) / theta2)
            Return New Quaternion(s * v.X, s * v.Y, s * v.Z, c)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            host.Run(src)

            Dim dt = host.HostInterruptDelayEstimate

            Dim t = atask.IMU_Translation
            Dim predictedTranslation = New cv.Point3f(dt * (dt / 2 * atask.IMU_Acceleration.X + atask.IMU_AngularVelocity.X) + t.X,
                                                  dt * (dt / 2 * atask.IMU_Acceleration.Y + atask.IMU_AngularVelocity.Y) + t.Y,
                                                  dt * (dt / 2 * atask.IMU_Acceleration.Z + atask.IMU_AngularVelocity.Z) + t.Z)

            Dim predictedW = New cv.Point3f(dt * (dt / 2 * atask.IMU_AngularAcceleration.X + atask.IMU_AngularVelocity.X),
                                        dt * (dt / 2 * atask.IMU_AngularAcceleration.Y + atask.IMU_AngularVelocity.Y),
                                        dt * (dt / 2 * atask.IMU_AngularAcceleration.Z + atask.IMU_AngularVelocity.Z))

            Dim predictedRotation As New Quaternion
            predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), atask.IMU_Rotation)

            Dim diffq = Quaternion.Subtract(atask.IMU_Rotation, predictedRotation)

            SetTrueText("IMU_Acceleration = " + vbTab +
                                 Format(atask.IMU_Acceleration.X, fmt3) + vbTab +
                                 Format(atask.IMU_Acceleration.Y, fmt3) + vbTab +
                                 Format(atask.IMU_Acceleration.Z, fmt3) + vbTab + vbCrLf +
                    "IMU_AngularAccel. = " + vbTab +
                                 Format(atask.IMU_AngularAcceleration.X, fmt3) + vbTab +
                                 Format(atask.IMU_AngularAcceleration.Y, fmt3) + vbTab +
                                 Format(atask.IMU_AngularAcceleration.Z, fmt3) + vbTab + vbCrLf +
                    "IMU_AngularVelocity = " + vbTab +
                                 Format(atask.IMU_AngularVelocity.X, fmt3) + vbTab +
                                 Format(atask.IMU_AngularVelocity.Y, fmt3) + vbTab +
                                 Format(atask.IMU_AngularVelocity.Z, fmt3) + vbTab + vbCrLf + vbCrLf +
                    "dt = " + dt.ToString() + vbCrLf + vbCrLf +
                    "Pose quaternion = " + vbTab +
                                 Format(atask.IMU_Rotation.X, fmt3) + vbTab +
                                 Format(atask.IMU_Rotation.Y, fmt3) + vbTab +
                                 Format(atask.IMU_Rotation.Z, fmt3) + vbTab + vbCrLf +
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
End Namespace