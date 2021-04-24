Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Basics : Inherits VBparent
    Public colorChangeValues As cv.Vec3f
    Public illuminationChangeValues As cv.Vec2f
    Public textureFlatteningValues As cv.Vec2f
    Public cloneSpec As Integer ' 0 is colorchange, 1 is illuminationchange, 2 is textureflattening
    Public Sub New()

        label1 = "Clone result - draw anywhere to clone a region"
        label2 = "Clone Region Mask"
        task.desc = "Clone a portion of one image into another.  Draw on any image to change selected area."
        task.drawRect = New cv.Rect(dst1.Width / 4, dst1.Height / 4, dst1.Width / 2, dst1.Height / 2)
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim mask As New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        If task.drawRect = New cv.Rect Then
            mask.SetTo(255)
        Else
            cv.Cv2.Rectangle(mask, task.drawRect, cv.Scalar.White, -1)
        End If
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone And task.frameCount Mod 10 = 0 Then cloneSpec += 1
        Select Case cloneSpec Mod 3
            Case 0
                cv.Cv2.ColorChange(src, mask, dst1, colorChangeValues(0), colorChangeValues(1), colorChangeValues(2))
            Case 1
                cv.Cv2.IlluminationChange(src, mask, dst1, illuminationChangeValues(0), illuminationChangeValues(1))
            Case 2
                cv.Cv2.TextureFlattening(src, mask, dst1, textureFlatteningValues(0), textureFlatteningValues(1))
        End Select
    End Sub
End Class




Public Class Clone_ColorChange : Inherits VBparent
    Dim clone As New Clone_Basics
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Color Change - Red", 5, 25, 15)
            sliders.setupTrackBar(1, "Color Change - Green", 5, 25, 5)
            sliders.setupTrackBar(2, "Color Change - Blue", 5, 25, 5)
        End If
        label1 = "Draw anywhere to select different clone region"
        label2 = "Mask used for clone"
        task.desc = "Clone a portion of one image into another controlling rgb.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(src As cv.Mat)
        clone.cloneSpec = 0
        clone.colorChangeValues = New cv.Point3f(sliders.trackbar(0).Value / 10, sliders.trackbar(1).Value / 10, sliders.trackbar(0).Value / 10)
        clone.Run(src)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class




Public Class Clone_IlluminationChange : Inherits VBparent
    Dim clone As New Clone_Basics
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Alpha", 0, 20, 2)
            sliders.setupTrackBar(1, "Beta", 0, 20, 2)
        End If
        label1 = "Draw anywhere to select different clone region"
        label2 = "Mask used for clone"
        task.desc = "Clone a portion of one image into another controlling illumination.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(src As cv.Mat)
        clone.cloneSpec = 1
        clone.illuminationChangeValues = New cv.Vec2f(sliders.trackbar(0).Value / 10, sliders.trackbar(1).Value / 10)
        clone.Run(src)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class





Public Class Clone_TextureFlattening : Inherits VBparent
    Dim clone As New Clone_Basics
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Low Threshold", 0, 100, 10)
            sliders.setupTrackBar(1, "High Threshold", 0, 100, 50)
        End If
        label1 = "Draw anywhere to select different clone region"
        label2 = "mask used for clone"
        task.desc = "Clone a portion of one image into another controlling texture.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(src as cv.Mat)
        clone.cloneSpec = 2
        clone.textureFlatteningValues = New cv.Vec2f(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        clone.Run(src)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_gui.cpp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
' https://www.learnopencv.com/seamless-cloning-using-opencv-python-cpp/
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Eagle : Inherits VBparent
    Dim sourceImage As cv.Mat
    Dim mask As cv.Mat
    Dim srcROI As cv.Rect
    Dim maskROI As cv.Rect
    Dim pt As cv.Point
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Seamless - Mixed Clone"
            radio.check(1).Text = "Seamless - MonochromeTransfer Clone"
            radio.check(2).Text = "Seamless - Normal Clone"
            radio.check(2).Checked = True
        End If
        sourceImage = cv.Cv2.ImRead(task.parms.homeDir + "Data/CloneSource.png")
        sourceImage = sourceImage.Resize(New cv.Size(sourceImage.Width * dst1.Width / 1280, sourceImage.Height * dst1.Height / 720))
        srcROI = New cv.Rect(0, 40, sourceImage.Width, sourceImage.Height)

        mask = cv.Cv2.ImRead(task.parms.homeDir + "Data/Clonemask.png")
        mask = mask.Resize(New cv.Size(mask.Width * dst1.Width / 1280, mask.Height * dst1.Height / 720))
        maskROI = New cv.Rect(srcROI.Width, 40, mask.Width, mask.Height)

        dst2.SetTo(0)
        dst2(srcROI) = sourceImage
        dst2(maskROI) = mask

        pt = New cv.Point(dst1.Width / 2, dst1.Height / 2)
        label1 = "Move Eagle by clicking in any location."
        label2 = "Source image and source mask."
        task.desc = "Clone an eagle into the video stream."
    End Sub
    Public Sub Run(src as cv.Mat)
        dst1 = src.Clone()
        If task.mouseClickFlag Then
            pt = task.mouseClickPoint  ' pt corresponds To the center Of the source image.  Roi can't be outside image boundary.
            If pt.X + srcROI.Width / 2 >= src.Width Then pt.X = src.Width - srcROI.Width / 2
            If pt.X - srcROI.Width / 2 < 0 Then pt.X = srcROI.Width / 2
            If pt.Y + srcROI.Height >= src.Height Then pt.Y = src.Height - srcROI.Height / 2
            If pt.Y - srcROI.Height < 0 Then pt.Y = srcROI.Height / 2
        End If

        Dim cloneFlag As New cv.SeamlessCloneMethods
        Static frm = findfrm("Clone_Eagle Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                cloneFlag = Choose(i + 1, cv.SeamlessCloneMethods.MixedClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.NormalClone)
                Exit For
            End If
        Next
        cv.Cv2.SeamlessClone(sourceImage, dst1, mask, pt, dst1, cloneFlag)
    End Sub
End Class




' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Cv2.SeamlessClone(OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.Point,%20OpenCvSharp.OutputArray,%20OpenCvSharp.SeamlessCloneMethods)/
Public Class Clone_Seamless : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Seamless Normal Clone"
            radio.check(1).Text = "Seamless Mono Clone"
            radio.check(2).Text = "Seamless Mixed Clone"
            radio.check(0).Checked = True
        End If

        label1 = "Results for SeamlessClone"
        label2 = "Mask for Clone"
        task.desc = "Use the seamlessclone API to merge color and depth..."
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim center As New cv.Point(src.Width / 2, src.Height / 2)
        Dim radius = 100
        If task.drawRect = New cv.Rect Then
            dst2.SetTo(0)
            dst2.Circle(center.X, center.Y, radius, cv.Scalar.White, -1, task.lineType)
        Else
            cv.Cv2.Rectangle(dst2, task.drawRect, cv.Scalar.White, -1)
        End If

        Dim style = cv.SeamlessCloneMethods.NormalClone
        Static frm = findfrm("Clone_Seamless Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                style = Choose(i + 1, cv.SeamlessCloneMethods.NormalClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.MixedClone)
                Exit For
            End If
        Next
        dst1 = src.Clone()
        cv.Cv2.SeamlessClone(task.RGBDepth, src, dst2, center, dst1, style)
        dst1.Circle(center, radius, cv.Scalar.White, 1, task.lineType)
    End Sub
End Class


