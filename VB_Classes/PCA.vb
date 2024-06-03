Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

' You can find the main direction of a series of points using principal component analysis ‘(PCA).
' PCA is a statistical technique that can be used to find the directions of greatest variance in a dataset.
' The main direction of a series of points is the direction of greatest variance in the dataset.
' To find the main direction of a series of points using PCA, you can follow these steps:

'   1. Collect your data. The data should be a set of points in a multidimensional ‘space.
'   2. Compute the covariance matrix of the data. The covariance matrix is a
'      square matrix that measures the covariance between each pair of variables.
'   3. Find the eigenvectors of the covariance matrix. The eigenvectors are the directions of greatest variance in the dataset.
'   4. The eigenvector with the largest eigenvalue is the main direction of the dataset.

' For example, let's say you have a dataset of points that represent the locations of cities ‘in the United States.
' You can use PCA to find the main direction of this dataset. The ‘main direction will be the direction of greatest
' variance in the locations of the cities. ‘This direction could be used to represent the overall trend in the
' distribution of cities in ‘the United States.
' PCA is a powerful tool that can be used to find the main direction of a series of points.

Public Class PCA_Basics : Inherits VB_Parent
    Dim prep As New PCA_Prep_CPP
    Public pca_analysis As New cv.PCA
    Public runRedCloud As Boolean
    Public Sub New()
        desc = "Find the Principal Component Analysis vector for the 3D points in a RedCloud cell contour."
    End Sub
    Public Function displayResults() As String
        Dim pcaStr = "EigenVector 3X3 matrix from PCA_Analysis of cell point cloud data at contour points:" + vbCrLf
        For y = 0 To pca_analysis.Eigenvectors.Rows - 1
            For x = 0 To pca_analysis.Eigenvectors.Cols - 1
                Dim val = pca_analysis.Eigenvectors.Get(Of Single)(y, x)
                pcaStr += Format(val, fmt3) + vbTab
            Next
            pcaStr += vbCrLf
        Next

        Dim valList As New List(Of Single)
        pcaStr += "EigenValues (PCA)" + vbTab
        For i = 0 To pca_analysis.Eigenvalues.Rows - 1
            Dim val = pca_analysis.Eigenvalues.Get(Of Single)(i, 0)
            pcaStr += Format(val, fmt3) + vbTab
            valList.Add(val)
        Next

        If valList.Count = 0 Then Return pcaStr

        Dim best = valList.Min
        Dim index = valList.IndexOf(best)
        pcaStr += "Min EigenValue = " + Format(best, fmt3) + " at index = " + CStr(index) + vbCrLf
        pcaStr += "Principal Component Vector" + vbTab
        For j = 0 To pca_analysis.Eigenvectors.Cols - 1
            Dim val = pca_analysis.Eigenvectors.Get(Of Single)(index, j)
            pcaStr += Format(val, fmt3) + vbTab
        Next
        pcaStr += vbCrLf
        Return pcaStr
    End Function
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            Static redC As New RedCloud_Basics
            If firstPass Then redOptions.UseColorOnly.Checked = True
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rc
        Dim inputPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
            If vec.Z > 0 Then inputPoints.Add(vec)
        Next

        If inputPoints.Count > 0 Then
            Dim inputMat = New cv.Mat(inputPoints.Count, 3, cv.MatType.CV_32F, inputPoints.ToArray)
            pca_analysis = New cv.PCA(inputMat, New cv.Mat, cv.PCA.Flags.DataAsRow)

            strOut = displayResults()
            setTrueText(strOut, 3)
        Else
            setTrueText("Select a cell to compute the eigenvector")
        End If
    End Sub
End Class









Public Class PCA_CellMask : Inherits VB_Parent
    Dim pca As New PCA_Basics
    Dim pcaPrep As New PCA_Prep_CPP
    Public Sub New()
        pca.runRedCloud = True
        desc = "Find the Principal Component Analysis vector for all the 3D points in a RedCloud cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pca.Run(src)
        dst2 = pca.dst2
        labels(2) = pca.labels(2)

        Dim rc = task.rc
        If rc.maxVec.Z > 0 Then
            pcaPrep.Run(task.pointCloud(rc.rect).Clone)

            If pcaPrep.inputData.Rows > 0 Then
                pca.pca_analysis = New cv.PCA(pcaPrep.inputData, New cv.Mat, cv.PCA.Flags.DataAsRow)
                strOut = pca.displayResults()
            End If
        Else
            strOut = "Selected cell has no 3D data."
            pca.pca_analysis = Nothing
        End If

        setTrueText(strOut, 3)
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/pca.cpp
Public Class PCA_Reconstruct : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Retained Variance", 1, 100, 95)
        desc = "Reconstruct a video stream as a composite of X images."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static retainSlider = findSlider("Retained Variance")
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

            Dim retainedVariance As Single = retainSlider.Value / 100
            Dim pca = New cv.PCA(data, New cv.Mat, cv.PCA.Flags.DataAsRow, retainedVariance)  ' the pca inputarray cannot be static so we reallocate each time.

            Dim point = pca.Project(data.Row(0))
            Dim reconstruction = pca.BackProject(point)
            reconstruction = reconstruction.Reshape(images(0).Channels(), images(0).Rows)
            reconstruction.ConvertTo(dst2, cv.MatType.CV_8UC1)
        End If
    End Sub
End Class



Public Class PCA_Depth : Inherits VB_Parent
    Dim pca As New PCA_Reconstruct
    Public Sub New()
        desc = "Reconstruct a depth stream as a composite of X images."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pca.Run(task.depthRGB)
        dst2 = pca.dst2
    End Sub
End Class




' https://docs.opencv.org/3.1.0/d1/dee/tutorial_introduction_to_pca.html
Public Class PCA_DrawImage : Inherits VB_Parent
    Dim pca As New PCA_Reconstruct
    Dim image As New cv.Mat
    Public Sub New()
        image = cv.Cv2.ImRead(task.homeDir + "opencv/Samples/Data/pca_test1.jpg")
        desc = "Use PCA to find the principal direction of an object."
        labels(2) = "Original image"
        labels(3) = "PCA Output"
    End Sub
    Private Sub drawAxis(img As cv.Mat, p As cv.Point, q As cv.Point, color As cv.Scalar, scale As Single)
        Dim angle = Math.Atan2(p.Y - q.Y, p.X - q.X) ' angle in radians
        Dim hypotenuse = Math.Sqrt((p.Y - q.Y) * (p.Y - q.Y) + (p.X - q.X) * (p.X - q.X))
        q.X = p.X - scale * hypotenuse * Math.Cos(angle)
        q.Y = p.Y - scale * hypotenuse * Math.Sin(angle)
        img.Line(p, q, color, task.lineWidth, task.lineType)
        p.X = q.X + 9 * Math.Cos(angle + Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle + Math.PI / 4)
        img.Line(p, q, color, task.lineWidth, task.lineType)
        p.X = q.X + 9 * Math.Cos(angle - Math.PI / 4)
        p.Y = q.Y + 9 * Math.Sin(angle - Math.PI / 4)
        img.Line(p, q, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = image.Resize(dst2.Size())
        Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(50, 255, cv.ThresholdTypes.Binary Or cv.ThresholdTypes.Otsu)
        Dim hierarchy() As cv.HierarchyIndex
        Dim contours As cv.Point()()
        cv.Cv2.FindContours(gray, contours, hierarchy, cv.RetrievalModes.List, cv.ContourApproximationModes.ApproxNone)

        dst3.SetTo(0)
        For i = 0 To contours.Length - 1
            Dim area = cv.Cv2.ContourArea(contours(i))
            If area < 100 Or area > 100000 Then Continue For
            cv.Cv2.DrawContours(dst3, contours, i, cv.Scalar.Red, task.lineWidth, task.lineType)
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

            dst3.Circle(cntr, task.dotSize + 1, cv.Scalar.BlueViolet, -1, task.lineType)
            Dim factor As Single = 0.02 ' scaling factor for the lines depicting the principal components.
            Dim ept1 = New cv.Point(cntr.X + factor * eigen_vecs(0).X * eigen_val(0), cntr.Y + factor * eigen_vecs(0).Y * eigen_val(0))
            Dim ept2 = New cv.Point(cntr.X - factor * eigen_vecs(1).X * eigen_val(1), cntr.Y - factor * eigen_vecs(1).Y * eigen_val(1))

            drawAxis(dst3, cntr, ept1, cv.Scalar.Red, 1) ' primary principal component
            drawAxis(dst3, cntr, ept2, cv.Scalar.BlueViolet, 5) ' secondary principal component
        Next
    End Sub
End Class







Public Class PCA_Prep_CPP : Inherits VB_Parent
    Public inputData As New cv.Mat
    Public Sub New()
        cPtr = PCA_Prep_Open()
        desc = "Take some pointcloud data and return the non-zero points in a point3f vector"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = PCA_Prep_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        Dim count = pca_prep_getcount(cPtr)
        inputData = New cv.Mat(count, 3, cv.MatType.CV_32F, imagePtr).Clone
        setTrueText("Data has been prepared and resides in inputData public")
    End Sub
    Public Sub Close()
        PCA_Prep_Close(cPtr)
    End Sub
End Class