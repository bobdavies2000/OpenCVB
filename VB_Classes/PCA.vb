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

Public Class PCA_Basics : Inherits TaskParent
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
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then dst2 = runRedC(src, labels(2))

        Dim rc = task.rc
        Dim inputPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
            If vec.Z > 0 Then inputPoints.Add(vec)
        Next

        If inputPoints.Count > 0 Then
            Dim inputMat = cv.Mat.FromPixelData(inputPoints.Count, 3, cv.MatType.CV_32F, inputPoints.ToArray)
            pca_analysis = New cv.PCA(inputMat, New cv.Mat, cv.PCA.Flags.DataAsRow)

            strOut = displayResults()
            SetTrueText(strOut, 3)
        Else
            SetTrueText("Select a cell to compute the eigenvector")
        End If
    End Sub
End Class









Public Class PCA_CellMask : Inherits TaskParent
    Dim pca As New PCA_Basics
    Dim pcaPrep As New PCA_Prep_CPP
    Public Sub New()
        pca.runRedCloud = True
        desc = "Find the Principal Component Analysis vector for all the 3D points in a RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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

        SetTrueText(strOut, 3)
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/pca.cpp
Public Class PCA_Reconstruct : Inherits TaskParent
    Dim images(7) As cv.Mat
    Dim images32f(images.Length) As cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Retained Variance", 1, 100, 95)
        desc = "Reconstruct a video stream as a composite of X images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static retainSlider = optiBase.FindSlider("Retained Variance")
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
            Dim pca = New cv.PCA(data, New cv.Mat, cv.PCA.Flags.DataAsRow, retainedVariance)  ' the pca inputarray cannot be Static so we reallocate each time.

            Dim point = pca.Project(data.Row(0))
            Dim reconstruction = pca.BackProject(point)
            reconstruction = reconstruction.Reshape(images(0).Channels(), images(0).Rows)
            reconstruction.ConvertTo(dst2, cv.MatType.CV_8UC1)
        End If
    End Sub
End Class



Public Class PCA_Depth : Inherits TaskParent
    Dim pca As New PCA_Reconstruct
    Public Sub New()
        desc = "Reconstruct a depth stream as a composite of X images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pca.Run(task.depthRGB)
        dst2 = pca.dst2
    End Sub
End Class




' https://docs.opencvb.org/3.1.0/d1/dee/tutorial_introduction_to_pca.html
Public Class PCA_DrawImage : Inherits TaskParent
    Dim pca As New PCA_Reconstruct
    Dim image As New cv.Mat
    Public Sub New()
        image = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/pca_test1.jpg")
        desc = "Use PCA to find the principal direction of an object."
        labels(2) = "Original image"
        labels(3) = "PCA Output"
    End Sub
    Sub drawAxis(img As cv.Mat, p As cv.Point, q As cv.Point, color As cv.Scalar, scale As Single)
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
    Public Overrides Sub RunAlg(src As cv.Mat)
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

            DrawCircle(dst3, cntr, task.DotSize + 1, cv.Scalar.BlueViolet)
            Dim factor As Single = 0.02 ' scaling factor for the lines depicting the principal components.
            Dim ept1 = New cv.Point(cntr.X + factor * eigen_vecs(0).X * eigen_val(0), cntr.Y + factor * eigen_vecs(0).Y * eigen_val(0))
            Dim ept2 = New cv.Point(cntr.X - factor * eigen_vecs(1).X * eigen_val(1), cntr.Y - factor * eigen_vecs(1).Y * eigen_val(1))

            drawAxis(dst3, cntr, ept1, cv.Scalar.Red, 1) ' primary principal component
            drawAxis(dst3, cntr, ept2, cv.Scalar.BlueViolet, 5) ' secondary principal component
        Next
    End Sub
End Class







Public Class PCA_Prep_CPP : Inherits TaskParent
    Public inputData As New cv.Mat
    Public Sub New()
        cPtr = PCA_Prep_Open()
        desc = "Take some pointcloud data and return the non-zero points in a point3f vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = PCA_Prep_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        Dim count = PCA_Prep_GetCount(cPtr)
        inputData = cv.Mat.FromPixelData(count, 3, cv.MatType.CV_32F, imagePtr).Clone
        SetTrueText("Data has been prepared and resides in inputData public")
    End Sub
    Public Sub Close()
        PCA_Prep_Close(cPtr)
    End Sub
End Class



Module PCA_NColor_CPP_Module
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PCA_NColor_Open() As IntPtr
    End Function
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub PCA_NColor_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PCA_NColor_RunCPP(cPtr As IntPtr, imagePtr As IntPtr, palettePtr As IntPtr, rows As Integer, cols As Integer, desiredNcolors As Integer) As IntPtr
    End Function
End Module







' https://www.codeproject.com/Tips/5384047/Implementing-Principal-Component-Analysis-Image-Se
Public Class PCA_Palettize : Inherits TaskParent
    Public palette As Byte()
    Public rgb(dst1.Total * dst1.ElemSize - 1) As Byte
    Public buff(rgb.Length - 1) As Byte
    Dim custom As New Palette_CustomColorMap
    Public paletteImage As Byte()
    Public nColor As New PCA_NColor
    Public options As New Options_PCA_NColor
    Public Sub New()
        optiBase.FindSlider("Desired number of colors").Value = 256
        desc = "Create a palette for the input image but don't use it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Marshal.Copy(src.Data, rgb, 0, rgb.Length)
        Marshal.Copy(src.Data, buff, 0, buff.Length)

        palette = nColor.MakePalette(rgb, dst2.Width, dst2.Height, options.desiredNcolors)

        If standaloneTest() Then
            paletteImage = nColor.RgbToIndex(rgb, dst1.Width, dst1.Height, palette, options.desiredNcolors)

            Dim img8u = New cv.Mat(dst2.Size, cv.MatType.CV_8U, cv.Scalar.All(0))
            Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length)

            custom.colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, palette)
            custom.Run(img8u)
            dst2 = custom.dst2
        End If

        labels(2) = "The palette found from the current image (repeated across the image) with " + CStr(options.desiredNcolors) + " entries"
    End Sub
End Class



' https://www.codeproject.com/Tips/5384047/Implementing-Principal-Component-Analysis-Image-Se
Public Class PCA_NColor : Inherits TaskParent
#Region "PCA_Specifics"

    <StructLayout(LayoutKind.Sequential)>
    Public Structure paletteEntry
        Public start As Integer
        Public nCount As Integer
        Public red As Byte
        Public green As Byte
        Public blue As Byte
        Public ErrorVal As Double
    End Structure
    Function CDiff(ByVal a As Byte(), start As Integer, ByVal b As Byte(), startPal As Integer) As Double
        Return (CInt(a(start + 0)) - CInt(b(startPal + 0))) * (CInt(a(start + 0)) - CInt(b(startPal + 0))) * 5 +
               (CInt(a(start + 1)) - CInt(b(startPal + 1))) * (CInt(a(start + 1)) - CInt(b(startPal + 1))) * 8 +
               (CInt(a(start + 2)) - CInt(b(startPal + 2))) * (CInt(a(start + 2)) - CInt(b(startPal + 2))) * 2
    End Function
    ' Convert an image to indexed form, using passed-in palette
    Function RgbToIndex(rgb As Byte(), width As Integer, height As Integer, pal As Byte(), nColor As Integer) As Byte()
        Dim answer(width * height - 1) As Byte

        For i = 0 To width * height - 1
            Dim best = CDiff(rgb, i * 3, pal, 0)
            Dim bestii = 0

            For ii = 1 To nColor - 1
                Dim nextError = CDiff(rgb, i * 3, pal, ii * 3)
                If nextError < best Then
                    best = nextError
                    bestii = ii
                End If
            Next

            answer(i) = CByte(bestii)
        Next

        Return answer
    End Function

    Public Function MakePalette(rgb As Byte(), width As Integer, height As Integer, nColors As Integer) As Byte()
        Dim buff(width * height * 3 - 1) As Byte
        Dim entry(nColors - 1) As paletteEntry
        Dim best As Double
        Dim bestii As Integer
        Dim i, ii As Integer
        Dim pal(256 * 3 - 1) As Byte

        Array.Copy(rgb, buff, width * height * 3)

        entry(0).start = 0
        entry(0).nCount = width * height
        CalcError(entry(0), buff)

        For i = 1 To nColors - 1
            best = entry(0).ErrorVal
            bestii = 0
            For ii = 0 To i - 1
                If entry(ii).ErrorVal > best Then
                    best = entry(ii).ErrorVal
                    bestii = ii
                End If
            Next
            SplitPCA(entry(bestii), entry(i), buff)
        Next

        For i = 0 To nColors - 1
            pal(i * 3) = entry(i).red
            pal(i * 3 + 1) = entry(i).green
            pal(i * 3 + 2) = entry(i).blue
        Next
        Return pal
    End Function

    Public Sub CalcError(ByRef entry As paletteEntry, ByRef buff() As Byte)
        entry.red = CByte(MeanColor(buff, entry.start * 3, entry.nCount, 0))
        entry.green = CByte(MeanColor(buff, entry.start * 3, entry.nCount, 1))
        entry.blue = CByte(MeanColor(buff, entry.start * 3, entry.nCount, 2))
        entry.ErrorVal = 0

        For i = 0 To entry.nCount - 1
            entry.ErrorVal += Math.Abs(CInt(buff((entry.start + i) * 3)) - entry.red)
            entry.ErrorVal += Math.Abs(CInt(buff((entry.start + i) * 3 + 1)) - entry.green)
            entry.ErrorVal += Math.Abs(CInt(buff((entry.start + i) * 3 + 2)) - entry.blue)
        Next
    End Sub

    Public Function MeanColor(rgb As Byte(), start As Integer, nnCount As Integer, index As Integer) As Double
        If nnCount = 0 Then Return 0
        Dim answer As Double = 0
        For i = 0 To nnCount - 1
            answer += rgb(start + i * 3 + index)
        Next
        Return answer / nnCount
    End Function

    ' Get principal components of variance
    ' Params: ret - return for components of major axis of variance
    '         pixels - the pixels
    '         nnCount - count of pixels
    Sub PCA(ByRef ret As Double(), pixels As Byte(), start As Integer, nnCount As Integer)
        Dim cov(2, 2) As Double
        Dim mu(2) As Double
        Dim i, j, k As Integer
        Dim var As Double
        Dim d(2) As Double
        Dim v(2, 2) As Double

        For i = 0 To 2
            mu(i) = MeanColor(pixels, start, nnCount, i)
        Next

        ' Calculate 3x3 channel covariance matrix
        For i = 0 To 2
            For j = 0 To i
                var = 0
                For k = 0 To nnCount - 1
                    var += (pixels(start + k * 3 + i) - mu(i)) * (pixels(start + k * 3 + j) - mu(j))
                Next
                cov(i, j) = var / nnCount
                cov(j, i) = var / nnCount
            Next
        Next

        EigenDecomposition(cov, v, d)
        ' Main component in col 3 of eigenvector matrix
        ret(0) = v(0, 2)
        ret(1) = v(1, 2)
        ret(2) = v(2, 2)
    End Sub
    Function Project(rgb As Byte(), start As Integer, comp As Double()) As Integer
        Return CInt(rgb(start) * comp(0) + rgb(start + 1) * comp(1) + rgb(start + 2) * comp(2))
    End Function
    ''' <summary>
    ''' Split an entry using PCA and Otsu thresholding.
    ''' We find the principal component of variance in RGB space.
    ''' Then we apply Otsu thresholding along that axis, and cut.
    ''' We partition using one pass of quick sort.
    ''' </summary>
    Public Sub SplitPCA(ByRef entry As paletteEntry, ByRef split As paletteEntry, ByRef buff As Byte())
        Dim low As Integer = 0
        Dim high As Integer = entry.nCount - 1
        Dim cut As Integer
        Dim comp(2) As Double
        Dim temp As Byte
        Dim i As Integer

        PCA(comp, buff, (entry.start * 3), entry.nCount)
        cut = GetOtsuThreshold2(buff, (entry.start * 3), entry.nCount, comp)

        While low < high
            While low < high AndAlso Project(buff, ((entry.start + low) * 3), comp) < cut
                low += 1
            End While
            While low < high AndAlso Project(buff, ((entry.start + high) * 3), comp) >= cut
                high -= 1
            End While
            If low < high Then
                For i = 0 To 2
                    temp = buff((entry.start + low) * 3 + i)
                    buff((entry.start + low) * 3 + i) = buff((entry.start + high) * 3 + i)
                    buff((entry.start + high) * 3 + i) = temp
                Next
            End If
            low += 1
            high -= 1
        End While

        split.start = entry.start + low
        split.nCount = entry.nCount - low
        entry.nCount = low

        CalcError(entry, buff)
        CalcError(split, buff)
    End Sub
    ''' <summary>
    ''' Get the Otsu threshold for image segmentation
    ''' </summary>
    ''' <param name="rgb">The RGB image data</param>
    ''' <param name="N">Total number of pixels</param>
    ''' <param name="remap">Remapping values for RGB channels</param>
    ''' <returns>Threshold at which to split pixels into foreground and background</returns>
    Public Function GetOtsuThreshold2(rgb As Byte(), start As Integer, N As Integer, remap As Double()) As Integer
        Dim hist(1023) As Integer
        Dim wB As Integer = 0
        Dim wF As Integer
        Dim mB, mF As Single
        Dim sum As Single = 0
        Dim sumB As Single = 0
        Dim varBetween As Single
        Dim varMax As Single = 0.0F
        Dim answer As Integer = 0

        For i As Integer = 0 To N - 1
            Dim nc As Integer = CInt(rgb(start + i * 3) * remap(0) + rgb(start + i * 3 + 1) * remap(1) + rgb(start + i * 3 + 2) * remap(2))
            hist(512 + nc) += 1
        Next

        ' Sum of all (for means)
        For k As Integer = 0 To 1023
            sum += k * hist(k)
        Next

        For k As Integer = 0 To 1023
            wB += hist(k)
            If wB = 0 Then
                Continue For
            End If

            wF = N - wB
            If wF = 0 Then
                Exit For
            End If

            sumB += CSng(k * hist(k))

            mB = sumB / wB            ' Mean Background
            mF = (sum - sumB) / wF    ' Mean Foreground

            ' Calculate Between Class Variance
            varBetween = CSng(wB) * CSng(wF) * (mB - mF) * (mB - mF)

            ' Check if new maximum found
            If varBetween > varMax Then
                varMax = varBetween
                answer = k
            End If
        Next

        Return answer - 512
    End Function
    Sub EigenDecomposition(A(,) As Double, ByRef V(,) As Double, ByRef d() As Double)
        Dim bufLen As Integer = A.GetLength(0)
        Dim e(bufLen - 1) As Double

        For i = 0 To bufLen - 1
            For j = 0 To bufLen - 1
                V(i, j) = A(i, j)
            Next
        Next

        Tred2(V, d, e)
        Tql2(V, d, e)
    End Sub
    Sub Tred2(ByRef V(,) As Double, ByRef d() As Double, ByRef e() As Double)
        Dim dLen As Integer = d.Length
        Dim i, j, k As Integer

        ' This is derived from the Algol procedures tred2 by
        ' Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
        ' Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
        ' Fortran subroutine in EISPACK.

        For j = 0 To dLen - 1
            d(j) = V(dLen - 1, j)
        Next

        ' Householder reduction to tridiagonal form.

        For i = dLen - 1 To 1 Step -1
            ' Scale to avoid under/overflow.

            Dim scale As Double = 0.0
            Dim h As Double = 0.0
            For k = 0 To i - 1
                scale += Math.Abs(d(k))
            Next

            If scale = 0.0 Then
                e(i) = d(i - 1)
                For j = 0 To i - 1
                    d(j) = V(i - 1, j)
                    V(i, j) = 0.0
                    V(j, i) = 0.0
                Next
            Else
                ' Generate Householder vector.
                Dim f, g As Double
                Dim hh As Double

                For k = 0 To i - 1
                    d(k) /= scale
                    h += d(k) * d(k)
                Next
                f = d(i - 1)
                g = Math.Sqrt(h)
                If f > 0 Then
                    g = -g
                End If
                e(i) = scale * g
                h = h - f * g
                d(i - 1) = f - g
                For j = 0 To i - 1
                    e(j) = 0.0
                Next

                ' Apply similarity transformation to remaining columns.

                For j = 0 To i - 1
                    f = d(j)
                    V(j, i) = f
                    g = e(j) + V(j, j) * f
                    For k = j + 1 To i - 1
                        g += V(k, j) * d(k)
                        e(k) += V(k, j) * f
                    Next
                    e(j) = g
                Next
                f = 0.0
                For j = 0 To i - 1
                    e(j) /= h
                    f += e(j) * d(j)
                Next
                hh = f / (h + h)
                For j = 0 To i - 1
                    e(j) -= hh * d(j)
                Next
                For j = 0 To i - 1
                    f = d(j)
                    g = e(j)
                    For k = j To i - 1
                        V(k, j) -= (f * e(k) + g * d(k))
                    Next
                    d(j) = V(i - 1, j)
                    V(i, j) = 0.0
                Next
            End If
            d(i) = h
        Next

        ' Accumulate transformations.

        For i = 0 To dLen - 2
            Dim h As Double
            V(dLen - 1, i) = V(i, i)
            V(i, i) = 1.0
            h = d(i + 1)
            If h <> 0.0 Then
                For k = 0 To i
                    d(k) = V(k, i + 1) / h
                Next
                For j = 0 To i
                    Dim g As Double = 0.0
                    For k = 0 To i
                        g += V(k, i + 1) * V(k, j)
                    Next
                    For k = 0 To i
                        V(k, j) -= g * d(k)
                    Next
                Next
            End If
            For k = 0 To i
                V(k, i + 1) = 0.0
            Next
        Next
        For j = 0 To dLen - 1
            d(j) = V(dLen - 1, j)
            V(dLen - 1, j) = 0.0
        Next
        V(dLen - 1, dLen - 1) = 1.0
        e(0) = 0.0
    End Sub

    ' Symmetric tridiagonal QL algorithm.
    Sub Tql2(ByRef V(,) As Double, ByRef d() As Double, ByRef e() As Double)
        ' This is derived from the Algol procedures tql2, by
        ' Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
        ' Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
        ' Fortran subroutine in EISPACK.

        Dim dLen = d.Length
        Dim i, j, k, l As Integer
        Dim f, tst1, eps As Double

        For i = 1 To dLen - 1
            e(i - 1) = e(i)
        Next
        e(dLen - 1) = 0.0

        f = 0.0
        tst1 = 0.0
        eps = Math.Pow(2.0, -52.0)
        For l = 0 To dLen - 1
            ' Find small subdiagonal element

            tst1 = Math.Max(tst1, Math.Abs(d(l)) + Math.Abs(e(l)))
            Dim m As Integer = l
            While m < dLen
                If Math.Abs(e(m)) <= eps * tst1 Then
                    Exit While
                End If
                m += 1
            End While

            ' If m == l, d(l) is an eigenvalue,
            ' otherwise, iterate.

            If m > l Then
                Dim iter As Integer = 0
                Do
                    Dim g, p, r As Double
                    Dim dl1 As Double
                    Dim h As Double
                    Dim c As Double
                    Dim c2 As Double
                    Dim c3 As Double
                    Dim el1 As Double
                    Dim s As Double
                    Dim s2 As Double

                    iter += 1  ' (Could check iteration count here.)

                    ' Compute implicit shift

                    g = d(l)
                    p = (d(l + 1) - g) / (2.0 * e(l))
                    r = Hypot(p, 1.0)
                    If p < 0 Then
                        r = -r
                    End If
                    d(l) = e(l) / (p + r)
                    d(l + 1) = e(l) * (p + r)
                    dl1 = d(l + 1)
                    h = g - d(l)
                    For i = l + 2 To dLen - 1
                        d(i) -= h
                    Next
                    f += h

                    ' Implicit QL transformation.

                    p = d(m)
                    c = 1.0
                    c2 = c
                    c3 = c
                    el1 = e(l + 1)
                    s = 0.0
                    s2 = 0.0
                    For i = m - 1 To l Step -1
                        c3 = c2
                        c2 = c
                        s2 = s
                        g = c * e(i)
                        h = c * p
                        r = Hypot(p, e(i))
                        e(i + 1) = s * r
                        s = e(i) / r
                        c = p / r
                        p = c * d(i) - s * g
                        d(i + 1) = h + s * (c * g + s * d(i))

                        ' Accumulate transformation.

                        For k = 0 To dLen - 1
                            h = V(k, i + 1)
                            V(k, i + 1) = s * V(k, i) + c * h
                            V(k, i) = c * V(k, i) - s * h
                        Next
                    Next
                    p = -s * s2 * c3 * el1 * e(l) / dl1
                    e(l) = s * p
                    d(l) = c * p

                    ' Check for convergence.

                Loop While Math.Abs(e(l)) > eps * tst1
            End If
            d(l) += f
            e(l) = 0.0
        Next

        ' Sort eigenvalues and corresponding vectors.

        For i = 0 To dLen - 2
            Dim k1 As Integer = i
            Dim p As Double = d(i)
            For j = i + 1 To dLen - 1
                If d(j) < p Then
                    k1 = j
                    p = d(j)
                End If
            Next
            If k1 <> i Then
                d(k1) = d(i)
                d(i) = p
                For j = 0 To dLen - 1
                    p = V(j, i)
                    V(j, i) = V(j, k1)
                    V(j, k1) = p
                Next
            End If
        Next
    End Sub

    Public Function Hypot(a As Double, b As Double) As Double
        Return Math.Sqrt(a * a + b * b)
    End Function
#End Region

    Dim custom As New Palette_CustomColorMap
    Public options As New Options_PCA_NColor
    Public palette(256 * 3) As Byte
    Public rgb(dst1.Total * dst1.ElemSize - 1) As Byte
    Public buff(rgb.Length - 1) As Byte
    Dim answer(dst1.Total - 1) As Byte
    Public Sub New()
        custom.colorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3)
        desc = "Use PCA to build a palettized CV_8U image from the input using a palette."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Marshal.Copy(src.Data, rgb, 0, rgb.Length)
        Marshal.Copy(src.Data, buff, 0, buff.Length)

        palette = MakePalette(rgb, dst2.Width, dst2.Height, options.desiredNcolors)
        Dim paletteImage = RgbToIndex(rgb, dst1.Width, dst1.Height, palette, options.desiredNcolors)

        Dim img8u = New cv.Mat(dst2.Size, cv.MatType.CV_8U, cv.Scalar.All(0))
        Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length)

        Marshal.Copy(palette, 0, custom.colorMap.Data, palette.Length)
        custom.Run(img8u)
        dst2 = custom.dst2

        Dim tmp = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, palette)
        Dim paletteCount = tmp.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()

        If standaloneTest() Then
            dst3 = ShowPalette(img8u * 256 / options.desiredNcolors)
            labels(3) = "dst2 is palettized using global palette option: " + task.gOptions.Palettes.Text
        End If

        labels(2) = "The image above is mapped to " + CStr(paletteCount) + " colors below.  "
    End Sub
End Class






' https://www.codeproject.com/Tips/5384047/Implementing-Principal-Component-Analysis-Image-Se
Public Class PCA_NColor_CPP : Inherits TaskParent
    Dim custom As New Palette_CustomColorMap
    Dim palettize As New PCA_Palettize
    Public rgb(dst1.Total * dst1.ElemSize - 1) As Byte
    Public classCount As Integer
    Public Sub New()
        cPtr = PCA_NColor_Open()
        optiBase.FindSlider("Desired number of colors").Value = 8
        UpdateAdvice(traceName + ": Adjust the 'Desired number of colors' between 1 and 256")
        labels = {"", "", "Palettized (CV_8U) version of color image.", ""}
        desc = "Create a faster version of the PCA_NColor algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then palettize.Run(src) ' get the palette in VB.Net
        Marshal.Copy(src.Data, rgb, 0, rgb.Length)
        classCount = palettize.options.desiredNcolors

        Dim handleSrc = GCHandle.Alloc(rgb, GCHandleType.Pinned)
        Dim handlePalette = GCHandle.Alloc(palettize.palette, GCHandleType.Pinned)
        Dim imagePtr = PCA_NColor_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), handlePalette.AddrOfPinnedObject(), src.Rows, src.Cols, classCount)
        handlePalette.Free()
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(dst2.Height, dst2.Width, cv.MatType.CV_8U, imagePtr)
        custom.colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, palettize.palette)

        custom.Run(dst2)
        If standaloneTest() Then dst3 = custom.dst2
        labels(2) = "The CV_8U image is below.  Values range from 0 to " + CStr(classCount)
        labels(3) = "The upper left image is mapped to " + CStr(classCount) + " colors below.  "
    End Sub
    Public Sub Close()
        PCA_NColor_Close(cPtr)
    End Sub
End Class






' https://www.codeproject.com/Tips/5384047/Implementing-Principal-Component-Analysis-Image-Se
Public Class PCA_NColorPalettize : Inherits TaskParent
    Dim custom As New Palette_CustomColorMap
    Dim palettize As New PCA_Palettize
    Dim answer(dst2.Width * dst2.Height - 1) As Byte
    Dim nColor As New PCA_NColor
    Dim rgb(dst1.Total * dst1.ElemSize - 1) As Byte
    Public Sub New()
        optiBase.FindSlider("Desired number of colors").Value = 8
        desc = "Create a faster version of the PCA_NColor algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then palettize.Run(src) ' get the palette in VB.Net which is very fast.

        Marshal.Copy(src.Data, rgb, 0, rgb.Length)
        Dim paletteImage = nColor.RgbToIndex(rgb, dst1.Width, dst1.Height, palettize.palette, palettize.options.desiredNcolors)

        Dim img8u = New cv.Mat(dst2.Size, cv.MatType.CV_8U, cv.Scalar.All(0))
        Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length)

        custom.colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, palettize.palette)

        custom.Run(img8u)
        dst2 = custom.dst2
    End Sub
End Class