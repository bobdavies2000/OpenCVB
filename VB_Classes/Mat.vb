Imports cv = OpenCvSharp
Public Class Mat_Repeat : Inherits VB_Algorithm
    Public Sub New()
        desc = "Use the repeat method to replicate data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim small = src.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst2 = small.Repeat(10, 10)
        small = task.depthRGB.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst3 = small.Repeat(10, 10)
    End Sub
End Class








Public Class Mat_PointToMat : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "Random_Basics points (original)"
        labels(3) = "Random_Basics points after format change with indexer"
        desc = "Convert point2f into a mat of points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        random.Run(Nothing)
        dst2 = random.dst2
        Dim rows = random.pointList.Count
        Dim pMat = New cv.Mat(rows, 1, cv.MatType.CV_32FC2, random.pointList.ToArray)
        Dim indexer = pMat.GetGenericIndexer(Of cv.Vec2f)()
        dst3.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        For i = 0 To rows - 1
            dst3.Set(Of cv.Vec3b)(indexer(i)(1), indexer(i)(0), white)
        Next
    End Sub
End Class






Public Class Mat_MatToPoint : Inherits VB_Algorithm
    Public Sub New()
        desc = "Convert a mat into a vector of points."
        labels(2) = "Reconstructed BGR Image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim points(src.Total - 1) As cv.Vec3b
        Dim vec As New cv.Vec3b
        Dim index As integer = 0
        Dim m3b = src.Clone()
        Dim indexer = m3b.GetGenericIndexer(Of cv.Vec3b)()
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                vec = indexer(y, x)
                points(index) = New cv.Vec3b(vec(0), vec(1), vec(2))
                index += 1
            Next
        Next
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, points)
    End Sub
End Class







Public Class Mat_Transpose : Inherits VB_Algorithm
    Public Sub New()
        desc = "Transpose a Mat and show results."
        labels(2) = "Color Image Transposed"
        labels(3) = "Color Image Transposed back (artifacts)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim trColor = src.T()
        dst2 = trColor.ToMat.Resize(New cv.Size(src.Cols, src.Rows))
        Dim trBack = dst2.T()
        dst3 = trBack.ToMat.Resize(src.Size())
    End Sub
End Class






' https://csharp.hotexamples.com/examples/OpenCvSharp/Mat/-/php-mat-class-examples.html#0x95f170f4714e3258c220a78eacceeee99591440b9885a2997bbbc6b3aebdcf1c-19,,37,
Public Class Mat_Tricks : Inherits VB_Algorithm
    Public Sub New()
        labels(2) = "Image squeezed into square Mat"
        labels(3) = "Mat transposed around the diagonal"
        desc = "Show some Mat tricks."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim mat = src.Resize(New cv.Size(src.Height, src.Height))
        Dim roi = New cv.Rect(0, 0, mat.Width, mat.Height)
        dst2(roi) = mat
        dst3(roi) = mat.T
    End Sub
End Class





' https://csharp.hotexamples.com/examples/OpenCvSharp/MatExpr/-/php-matexpr-class-examples.html
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/MatOperations.cs
Public Class Mat_RowColRange : Inherits VB_Algorithm
    Public Sub New()
        labels(2) = "BitwiseNot of RowRange and ColRange"
        desc = "Perform operation on a range of cols and/or Rows."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim midX = src.Width / 2
        Dim midY = src.Height / 2
        dst2 = src
        cv.Cv2.BitwiseNot(dst2.RowRange(midY - 25, midY + 25), dst2.RowRange(midY - 25, midY + 25))
        cv.Cv2.BitwiseNot(dst2.ColRange(midX - 25, midX + 25), dst2.ColRange(midX - 25, midX + 25))
    End Sub
End Class





Public Class Mat_Managed : Inherits VB_Algorithm
    Public Sub New()
        labels(2) = "Color change is in the managed cv.vec3b array"
        desc = "There is a limited ability to use Mat data in Managed code directly."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static autoRand As New Random()
        Static img(src.Total) As cv.Vec3b
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, img)
        Static nextColor As cv.Vec3b
        If heartBeat() Then
            If nextColor = New cv.Vec3b(0, 0, 255) Then nextColor = New cv.Vec3b(0, 255, 0) Else nextColor = New cv.Vec3b(0, 0, 255)
        End If
        For i = 0 To img.Length - 1
            img(i) = nextColor
        Next
        Dim rect As New cv.Rect(autoRand.Next(0, src.Width - 50), autoRand.Next(0, src.Height - 50), 50, 50)
        dst2(rect).SetTo(0)
    End Sub
End Class






Public Class Mat_MultiplyReview : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public Sub New()
        desc = "Review matrix multiplication"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim a(,) = {{1, 4, 2}, {2, 5, 1}}
        Dim b(,) = {{3, 4, 2}, {3, 5, 7}, {1, 2, 1}}
        Dim nextLine = ""
        flow.msgs.Add("Matrix a")
        For i = 0 To a.GetLength(0) - 1
            nextLine = ""
            For j = 0 To a.GetLength(1) - 1
                nextLine += CStr(a(i, j)) + vbTab
            Next
            flow.msgs.Add(nextLine)
        Next

        flow.msgs.Add("Matrix b")
        For i = 0 To b.GetLength(0) - 1
            nextLine = ""
            For j = 0 To b.GetLength(1) - 1
                nextLine += CStr(b(i, j)) + vbTab
            Next
            flow.msgs.Add(nextLine)
        Next

        Dim c(a.GetLength(0) - 1, a.GetLength(1) - 1) As Integer
        Dim input(a.GetLength(0) - 1, a.GetLength(1) - 1) As String
        For i = 0 To c.GetLength(0) - 1
            For j = 0 To c.GetLength(1) - 1
                input(i, j) = ""
                For k = 0 To c.GetLength(1) - 1
                    c(i, j) += a(i, k) * b(k, j)
                    input(i, j) += CStr(a(i, k)) + "*" + CStr(b(k, j)) + If(k < c.GetLength(1) - 1, " + ", vbTab)
                Next
            Next
        Next


        flow.msgs.Add("Matrix c = a X b")
        For i = 0 To a.GetLength(0) - 1
            nextLine = ""
            For j = 0 To a.GetLength(1) - 1
                nextLine += CStr(c(i, j)) + " = " + input(i, j)
            Next
            flow.msgs.Add(nextLine)
        Next

        flow.Run(Nothing)
    End Sub
End Class






' https://stackoverflow.com/questions/11015119/inverse-matrix-opencv-matrix-inv-not-working-properly
Public Class Mat_Inverse : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public matrix(,) As Single = {{1.1688, 0.23, 62.2}, {-0.013, 1.225, -6.29}, {0, 0, 1}}
    Public validateInverse As Boolean
    Public inverse As New cv.Mat
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Cholesky")
            radio.addRadio("Eig (works but results are incorrect)")
            radio.addRadio("LU")
            radio.addRadio("Normal (not working)")
            radio.addRadio("QR (not working)")
            radio.addRadio("SVD (works but results are incorrect)")
            radio.check(0).Checked = True
            radio.check(3).Enabled = False ' not accepted!
            radio.check(4).Enabled = False ' not accepted!
        End If
        desc = "Given a 3x3 matrix, invert it and present results."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim nextline = ""

        Dim decompType = cv.DecompTypes.Cholesky
        Static frm = findfrm("Mat_Inverse Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                decompType = Choose(i + 1, cv.DecompTypes.Cholesky, cv.DecompTypes.Eig, cv.DecompTypes.LU, cv.DecompTypes.Normal,
                                         cv.DecompTypes.QR, cv.DecompTypes.SVD)
            End If
        Next

        If standalone Or validateInverse Then
            flow.msgs.Add("Matrix Input")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(matrix(i, j)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next
        End If

        Dim input = New cv.Mat(3, 3, cv.MatType.CV_32F, matrix)
        cv.Cv2.Invert(input, inverse, decompType)

        If standalone Or validateInverse Then
            flow.msgs.Add("Matrix Inverse")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(inverse.Get(Of Single)(j, i)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next

            Dim identity = (input * inverse).ToMat

            flow.msgs.Add("Verify Inverse is correct")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(identity.Get(Of Single)(j, i)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next
        End If

        flow.Run(Nothing)
    End Sub
End Class







'' https://github.com/takuya-takeuchi/DlibDotNet/tree/master/examples/3rdparty/OpenCVSharp/MatToArray2D
'Public Class Mat_2Dlib : Inherits VB_Algorithm
'    Public dRGB As Array2D(Of BgrPixel)
'    Public dGray As Array2D(Of Byte)
'    Public Sub New()
'        desc = "Convert a Mat to the expected Array2D for a DLib API"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Dim array(src.Total * src.ElemSize - 1) As Byte
'        Marshal.Copy(src.Data, array, 0, array.Length)

'        If src.Type = cv.MatType.CV_8U Then
'            dGray = Dlib.LoadImageData(Of Byte)(array, src.Rows, src.Cols, src.Cols * src.ElemSize)
'        Else
'            dRGB = Dlib.LoadImageData(Of BgrPixel)(array, src.Rows, src.Cols, src.Cols * src.ElemSize)
'        End If
'        setTrueText("OpenCVB Mat converted to an Array2D for use with DlibDotNet")
'    End Sub
'End Class








'' https://github.com/takuya-takeuchi/DlibDotNet/tree/master/examples/3rdparty/OpenCVSharp/MatToArray2D
'Public Class Mat_Dlib2Mat : Inherits VB_Algorithm
'    Public dGray As Array2D(Of Byte)
'    Public dRGB As Array2D(Of BgrPixel)
'    Public d32f As Array2D(Of Single)
'    Public Sub New()
'        desc = "Convert a Dlib Array2D to an OpenCV Mat"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)

'        If dGray IsNot Nothing Then
'            dst2 = New cv.Mat(dGray.Rows, dGray.Columns, cv.MatType.CV_8U, 0)
'            Marshal.Copy(dGray.ToBytes, 0, dst2.Data, dst2.Total)
'        End If

'        If dRGB IsNot Nothing Then
'            dst3 = New cv.Mat(dRGB.Rows, dRGB.Columns, cv.MatType.CV_8UC3)
'            Marshal.Copy(dRGB.ToBytes, 0, dst3.Data, dst3.Total * dst3.ElemSize)
'        End If
'    End Sub
'End Class








Public Class Mat_2to1 : Inherits VB_Algorithm
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Public mat() As cv.Mat = {mat1, mat2}
    Public lineSeparators = True ' if they want lines or not...
    Public Sub New()
        mat1 = New cv.Mat(New cv.Size(dst2.Rows, dst2.Cols), cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat = {mat1, mat2}

        labels(2) = ""
        desc = "Fill a Mat with 2 images"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim nSize = New cv.Size(task.workingRes.Width, task.workingRes.Height / 2)
        Dim roiTop = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Dim roibot = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            mat1 = src
            mat2 = task.depthRGB
            mat = {mat1, mat2}
        End If
        dst2.SetTo(0)
        If mat(0) IsNot Nothing Then
            If dst2.Type <> mat(0).Type Then dst2 = New cv.Mat(dst2.Size(), mat(0).Type)
            For i = 0 To 1
                Dim roi = Choose(i + 1, roiTop, roibot)
                If mat(i).Empty = False Then dst2(roi) = mat(i).Resize(nSize)
            Next
            If lineSeparators Then
                dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), cv.Scalar.White, task.lineWidth + 1)
            End If
        End If
    End Sub
End Class










Public Class Mat_4Click : Inherits VB_Algorithm
    Public mats As New Mat_4to1
    Public mat() As cv.Mat
    Public Sub New()
        mat = mats.mat
        labels(3) = "Click a quadrant in dst2 to view it in dst3"
        desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        mat = mats.mat
        mats.Run(Nothing)
        dst2 = mats.dst2.Clone
        If standalone Then
            mat(0) = task.color.Clone
            mat(1) = task.depthRGB.Clone
            mat(2) = If(task.leftview.Channels = 1, task.leftview.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.leftview)
            mat(3) = If(task.rightview.Channels = 1, task.rightview.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightview)
        End If

        If (task.mouseClickFlag And task.mousePicTag) = RESULT_DST2 Or firstPass Then
            If firstPass Then
                task.mouseClickFlag = True
                task.clickPoint = New cv.Point(1, 1)
                task.mousePicTag = RESULT_DST2
            End If
            setMyActiveMat()
        End If
        dst3 = mats.mat(task.quadrantIndex).Clone
    End Sub
End Class







Public Class Mat_4to1 : Inherits VB_Algorithm
    Public mat(3) As cv.Mat
    Public lineSeparators = True ' if they want lines or not...
    Public Sub New()
        For i = 0 To mat.Length - 1
            mat(i) = dst2.Clone
        Next
        labels(2) = "Combining 4 images into one"
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Use one Mat for up to 4 images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim nSize = New cv.Size(dst2.Width / 2, dst2.Height / 2)
        Dim roiTopLeft = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Dim roiTopRight = New cv.Rect(nSize.Width, 0, nSize.Width, nSize.Height)
        Dim roibotLeft = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        Dim roibotRight = New cv.Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            Dim tmpLeft = If(task.leftview.Channels = 1, task.leftview.CvtColor(cv.ColorConversionCodes.GRAY2BGR),
                             task.leftview)
            Dim tmpRight = If(task.rightview.Channels = 1, task.rightview.CvtColor(cv.ColorConversionCodes.GRAY2BGR),
                              task.rightview)
            mat = {task.color.Clone, task.depthRGB.Clone, tmpLeft, tmpRight}
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3)
        For i = 0 To 4 - 1
            Dim tmp = mat(i).Clone
            If tmp.Channels = 1 Then tmp = mat(i).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim roi = Choose(i + 1, roiTopLeft, roiTopRight, roibotLeft, roibotRight)
            dst2(roi) = tmp.Resize(nSize)
        Next
        If lineSeparators Then
            dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2),
                      cv.Scalar.White, task.lineWidth + 1)
            dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height),
                      cv.Scalar.White, task.lineWidth + 1)
        End If
    End Sub
End Class





Public Class Mat_ToList : Inherits VB_Algorithm
    Dim autoX As New OpAuto_XRange
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        desc = "Convert a Mat to List of points in 2 ways to measure which is better"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        autoX.Run(hist2d.histogram)
        dst2 = hist2d.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        Dim ptList As New List(Of cv.Point)
        If gOptions.DebugCheckBox.Checked Then
            For y = 0 To dst2.Height - 1
                For x = 0 To dst2.Width - 1
                    If dst2.Get(Of Byte)(y, x) <> 0 Then ptList.Add(New cv.Point(x, y))
                Next
            Next
        Else
            Dim points = dst2.FindNonZero()
            For i = 0 To points.Rows - 1
                ptList.Add(points.Get(Of cv.Point)(i, 0))
            Next
        End If

        labels(2) = "There were " + CStr(ptList.Count) + " points identified"
    End Sub
End Class