Imports cvb = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Basics : Inherits TaskParent
    Public colorChangeValues As cvb.Vec3f
    Public illuminationChangeValues As cvb.Vec2f
    Public textureFlatteningValues As cvb.Vec2f
    Public cloneSpec As Integer ' 0 is colorchange, 1 is illuminationchange, 2 is textureflattening
    Public Sub New()

        labels(2) = "Clone result - draw anywhere to clone a region"
        labels(3) = "Clone Region Mask"
        desc = "Clone a portion of one image into another.  Draw on any image to change selected area."
        task.drawRect = New cvb.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2)
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim mask As New cvb.Mat(src.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        If task.drawRect = New cvb.Rect Then
            mask.SetTo(255)
        Else
            cvb.Cv2.Rectangle(mask, task.drawRect, cvb.Scalar.White, -1)
        End If
        dst3 = mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        If standaloneTest() And task.frameCount Mod 10 = 0 Then cloneSpec += 1
        Select Case cloneSpec Mod 3
            Case 0
                cvb.Cv2.ColorChange(src, mask, dst2, colorChangeValues(0), colorChangeValues(1), colorChangeValues(2))
            Case 1
                cvb.Cv2.IlluminationChange(src, mask, dst2, illuminationChangeValues(0), illuminationChangeValues(1))
            Case 2
                cvb.Cv2.TextureFlattening(src, mask, dst2, textureFlatteningValues(0), textureFlatteningValues(1))
        End Select
    End Sub
End Class




Public Class Clone_ColorChange : Inherits TaskParent
    Dim clone As New Clone_Basics
    Dim options As New Options_Clone
    Public Sub New()
        labels(2) = "Draw anywhere to select different clone region"
        labels(3) = "Mask used for clone"
        desc = "Clone a portion of one image into another controlling rgb.  Draw on any image to change selected area."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        clone.cloneSpec = 0
        clone.colorChangeValues = New cvb.Point3f(options.blueChange, options.greenChange, options.redChange)
        clone.Run(src)
        dst2 = clone.dst2
        dst3 = clone.dst3
    End Sub
End Class




Public Class Clone_IlluminationChange : Inherits TaskParent
    Dim clone As New Clone_Basics
    Dim options As New Options_Clone
    Public Sub New()
        labels(2) = "Draw anywhere to select different clone region"
        labels(3) = "Mask used for clone"
        desc = "Clone a portion of one image into another controlling illumination.  Draw on any image to change selected area."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        clone.cloneSpec = 1
        clone.illuminationChangeValues = New cvb.Vec2f(options.alpha, options.beta)
        clone.Run(src)
        dst2 = clone.dst2
        dst3 = clone.dst3
    End Sub
End Class





Public Class Clone_TextureFlattening : Inherits TaskParent
    Dim clone As New Clone_Basics
    Dim options As New Options_Clone
    Public Sub New()
        labels(2) = "Draw anywhere to select different clone region"
        labels(3) = "mask used for clone"
        desc = "Clone a portion of one image into another controlling texture.  Draw on any image to change selected area."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        clone.cloneSpec = 2
        clone.textureFlatteningValues = New cvb.Vec2f(options.lowThreshold, options.highThreshold)
        clone.Run(src)
        dst2 = clone.dst2
        dst3 = clone.dst3
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_gui.cpp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
' https://www.learnopencvb.com/seamless-cloning-using-opencv-python-cpp/
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Eagle : Inherits TaskParent
    Dim sourceImage As cvb.Mat
    Dim mask As cvb.Mat
    Dim srcROI As cvb.Rect
    Dim maskROI As cvb.Rect
    Dim pt As cvb.Point
    Dim options As New Options_Clone
    Public Sub New()
        sourceImage = cvb.Cv2.ImRead(task.HomeDir + "Data/CloneSource.png")
        sourceImage = sourceImage.Resize(New cvb.Size(sourceImage.Width * dst2.Width / 1280, sourceImage.Height * dst2.Height / 720))
        srcROI = New cvb.Rect(0, 40, sourceImage.Width, sourceImage.Height)

        mask = cvb.Cv2.ImRead(task.HomeDir + "Data/Clonemask.png")
        mask = mask.Resize(New cvb.Size(mask.Width * dst2.Width / 1280, mask.Height * dst2.Height / 720))
        maskROI = New cvb.Rect(srcROI.Width, 40, mask.Width, mask.Height)

        dst3.SetTo(0)
        dst3(srcROI) = sourceImage
        dst3(maskROI) = mask

        pt = New cvb.Point(dst2.Width / 2, dst2.Height / 2)
        labels(2) = "Move Eagle by clicking in any location."
        labels(3) = "Source image and source mask."
        desc = "Clone an eagle into the video stream."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone()
        If task.mouseClickFlag Then
            pt = task.ClickPoint  ' pt corresponds To the center Of the source image.  Roi can't be outside image boundary.
            If pt.X + srcROI.Width / 2 >= src.Width Then pt.X = src.Width - srcROI.Width / 2
            If pt.X - srcROI.Width / 2 < 0 Then pt.X = srcROI.Width / 2
            If pt.Y + srcROI.Height >= src.Height Then pt.Y = src.Height - srcROI.Height / 2
            If pt.Y - srcROI.Height < 0 Then pt.Y = srcROI.Height / 2
        End If

        cvb.Cv2.SeamlessClone(sourceImage, dst2, mask, pt, dst2, options.cloneFlag)
    End Sub
End Class




' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Cv2.SeamlessClone(OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.Point,%20OpenCvSharp.OutputArray,%20OpenCvSharp.SeamlessCloneMethods)/
Public Class Clone_Seamless : Inherits TaskParent
    Dim options As New Options_Clone
    Public Sub New()
        labels(2) = "Results for SeamlessClone"
        labels(3) = "Mask for Clone"
        desc = "Use the seamlessclone API to merge color and depth..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim center As New cvb.Point(src.Width / 2, src.Height / 2)
        Dim radius = 100
        If task.drawRect = New cvb.Rect Then
            dst3.SetTo(0)
            DrawCircle(dst3, center, radius, cvb.Scalar.White)
        Else
            cvb.Cv2.Rectangle(dst3, task.drawRect, cvb.Scalar.White, -1)
        End If

        dst2 = src.Clone()
        cvb.Cv2.SeamlessClone(task.depthRGB, src, dst3, center, dst2, options.cloneFlag)
        DrawCircle(dst2, center, radius, cvb.Scalar.White)
    End Sub
End Class


