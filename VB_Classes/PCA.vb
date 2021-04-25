Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/pca.cpp
Public Class PCA_Basics : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Retained Variance", 1, 100, 95)
        End If

        task.desc = "Reconstruct a video stream as a composite of X images."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static images(7) As cv.Mat
        Static images32f(images.Length) As cv.Mat
        Dim index = task.frameCount Mod images.Length
        images(index) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        images(index).ConvertTo(gray32f, cv.MatType.CV_32F)
        gray32f = gray32f.Normalize(0, 255, cv.NormTypes.MinMax)
        images32f(index) = gray32f.Reshape(1, 1)
        If task.frameCount >= images.Length Then
            Dim data = New cv.Mat(images.Length, src.Rows * src.Cols, cv.MatType.CV_32F)
            For i = 0 To images.Length - 1
                images32f(i).CopyTo(data.Row(i))
            Next

            Dim retainedVariance = sliders.trackbar(0).Value / 100
            Dim pca = New cv.PCA(data, New cv.Mat, cv.PCA.Flags.DataAsRow, retainedVariance)  ' the pca inputarray cannot be static so we reallocate each time.

            Dim point = pca.Project(data.Row(0))
            Dim reconstruction = pca.BackProject(point)
            reconstruction = reconstruction.Reshape(images(0).Channels(), images(0).Rows)
            reconstruction.ConvertTo(dst1, cv.MatType.CV_8UC1)
        End If
    End Sub
End Class



Public Class PCA_Depth : Inherits VBparent
    Dim pca As New PCA_Basics
    Public Sub New()
        task.desc = "Reconstruct a depth stream as a composite of X images."
    End Sub
    Public Sub Run(src as cv.Mat)
        pca.Run(task.RGBDepth)
        dst1 = pca.dst1
    End Sub
End Class




' https://docs.opencv.org/3.1.0/d1/dee/tutorial_introduction_to_pca.html
Public Class PCA_DrawImage : Inherits VBparent
    Dim pca As New PCA_Basics
    Dim image As New cv.Mat
    Public Sub New()
        image = cv.Cv2.ImRead(task.parms.homeDir + "Data/pca_test1.jpg")
        task.desc = "Use PCA to find the principle direction of an object."
        label1 = "Original image"
        label2 = "PCA Output"
    End Sub
    Private Sub drawAxis(img As cv.Mat, p As cv.Point, q As cv.Point, color As cv.Scalar, scale As Single)
        Dim angle = Math.Atan2(p.Y - q.Y, p.X - q.X) ' angle in radians
        Dim hypotenuse = Math.Sqrt((p.Y - q.Y) * (p.Y - q.Y) + (p.X - q.X) * (p.X - q.X))
        q.X = p.X - scale * hypotenuse * Math.Cos(angle)
        q.Y = p.Y - scale * hypotenuse * Math.Sin(angle)
        img.Line(p, q, color, 1, task.lineType)
        p.X = q.X + 9 * Math.Cos(angle + Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle + Math.PI / 4)
        img.Line(p, q, color, 1, task.lineType)
        p.X = q.X + 9 * Math.Cos(angle - Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle - Math.PI / 4)
        img.Line(p, q, color, 1, task.lineType)
    End Sub
    Public Sub Run(src as cv.Mat)
        dst1 = image.Resize(dst1.Size())
        Dim gray = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(50, 255, cv.ThresholdTypes.Binary Or cv.ThresholdTypes.Otsu)
        Dim hierarchy() As cv.HierarchyIndex = Nothing
        Dim contours As cv.Point()() = Nothing
        cv.Cv2.FindContours(gray, contours, hierarchy, cv.RetrievalModes.List, cv.ContourApproximationModes.ApproxNone)

        dst2.SetTo(0)
        For i = 0 To contours.Length - 1
            Dim area = cv.Cv2.ContourArea(contours(i))
            If area < 100 Or area > 100000 Then Continue For
            cv.Cv2.DrawContours(dst2, contours, i, cv.Scalar.Red, 1, task.lineType)
            Dim sz = contours(i).Length
            Dim data_pts = New cv.Mat(sz, 2, cv.MatType.CV_64FC1)
            For j = 0 To data_pts.Rows - 1
                data_pts.Set(Of Double)(j, 0, contours(i)(j).X)
                data_pts.Set(Of Double)(j, 1, contours(i)(j).Y)
            Next

            Dim pca_analysis = New cv.PCA(data_pts, New cv.Mat, cv.PCA.Flags.DataAsRow)
            Dim cntr = New cv.Point(CInt(pca_analysis.Mean.Get(Of Double)(0, 0)), CInt(pca_analysis.Mean.Get(Of Double)(0, 1)))
            Dim eigen_vecs(2) As cv.Point2d
            Dim eigen_val(2) As Double
            For j = 0 To 1
                eigen_vecs(j) = New cv.Point2d(pca_analysis.Eigenvectors.Get(Of Double)(j, 0), pca_analysis.Eigenvectors.Get(Of Double)(j, 1))
                eigen_val(j) = pca_analysis.Eigenvalues.Get(Of Double)(0, j)
            Next

            dst2.Circle(cntr, 3, cv.Scalar.BlueViolet, -1, task.lineType)
            Dim factor As Single = 0.02 ' scaling factor for the lines depicting the principle components.
            Dim ept1 = New cv.Point(cntr.X + factor * eigen_vecs(0).X * eigen_val(0), cntr.Y + factor * eigen_vecs(0).Y * eigen_val(0))
            Dim ept2 = New cv.Point(cntr.X - factor * eigen_vecs(1).X * eigen_val(1), cntr.Y - factor * eigen_vecs(1).Y * eigen_val(1))

            drawAxis(dst2, cntr, ept1, cv.Scalar.Red, 1) ' primary principle component
            drawAxis(dst2, cntr, ept2, cv.Scalar.BlueViolet, 5) ' secondary principle component
        Next
    End Sub
End Class



