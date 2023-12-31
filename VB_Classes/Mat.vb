Imports System.Runtime.InteropServices
Imports DlibDotNet
Imports cv = OpenCvSharp
Public Class Mat_Repeat : Inherits VBparent
    Public Sub New()
        task.desc = "Use the repeat method to replicate data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim small = src.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst2 = small.Repeat(10, 10)
        small = task.RGBDepth.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst3 = small.Repeat(10, 10)
    End Sub
End Class








Public Class Mat_PointToMat : Inherits VBparent
    Dim random As New Random_Basics
    Public Sub New()
        random.plotPoints = True
        labels(2) = "Random_Basics points (original)"
        labels(3) = "Random_Basics points after format change"
        task.desc = "Convert pointf3 into a mat of points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        random.RunClass(Nothing)
        dst2 = random.dst2
        Dim rows = random.Points.Length
        Dim pMat = New cv.Mat(rows, 1, cv.MatType.CV_32SC2, random.Points)
        Dim indexer = pMat.GetGenericIndexer(Of cv.Vec2i)()
        dst3.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        For i = 0 To rows - 1
            dst3.Set(Of cv.Vec3b)(indexer(i).Item1, indexer(i).Item0, white)
        Next
    End Sub
End Class






Public Class Mat_MatToPoint : Inherits VBparent
    Public Sub New()
        task.desc = "Convert a mat into a vector of points."
        labels(2) = "Reconstructed RGB Image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim points(src.Total - 1) As cv.Vec3b
        Dim vec As New cv.Vec3b
        Dim index As integer = 0
        Dim m3b = src.Clone()
        Dim indexer = m3b.GetGenericIndexer(Of cv.Vec3b)()
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                vec = indexer(y, x)
                points(index) = New cv.Vec3b(vec.Item0, vec.Item1, vec.Item2)
                index += 1
            Next
        Next
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, points)
    End Sub
End Class







Public Class Mat_Transpose : Inherits VBparent
    Public Sub New()
        task.desc = "Transpose a Mat and show results."
        labels(2) = "Color Image Transposed"
        labels(3) = "Color Image Transposed back (artifacts)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim trColor = src.T()
        dst2 = trColor.ToMat.Resize(New cv.Size(src.Cols, src.Rows))
        Dim trBack = dst2.T()
        dst3 = trBack.ToMat.Resize(src.Size())
    End Sub
End Class






' https://csharp.hotexamples.com/examples/OpenCvSharp/Mat/-/php-mat-class-examples.html#0x95f170f4714e3258c220a78eacceeee99591440b9885a2997bbbc6b3aebdcf1c-19,,37,
Public Class Mat_Tricks : Inherits VBparent
    Public Sub New()
        labels(2) = "Image squeezed into square Mat"
        labels(3) = "Mat transposed around the diagonal"
        task.desc = "Show some Mat tricks."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim mat = src.Resize(New cv.Size(src.Height, src.Height))
        Dim roi = New cv.Rect(0, 0, mat.Width, mat.Height)
        dst2(roi) = mat
        dst3(roi) = mat.T
    End Sub
End Class








Public Class Mat_ImageXYZ_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public xyDepth As cv.Mat
    Public xyzPlanes() As cv.Mat
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 32
        findSlider("ThreadGrid Height").Value = 32

        xyDepth = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        Dim xyz As New cv.Point3f
        For xyz.Y = 0 To xyDepth.Height - 1
            For xyz.X = 0 To xyDepth.Width - 1
                xyDepth.Set(Of cv.Point3f)(xyz.Y, xyz.X, xyz)
            Next
        Next
        xyzPlanes = xyDepth.Split()

        task.desc = "Create a cv.Point3f vector with x, y, and z."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.RunClass(Nothing)
        Parallel.ForEach(grid.roiList,
          Sub(roi)
              xyzPlanes(2)(roi) = task.depth32f(roi)
          End Sub)

        cv.Cv2.Merge(xyzPlanes, xyDepth)
        If standalone Or task.intermediateActive Then setTrueText("Mat built with X, Y, and Z (Depth)", 10, 125)
    End Sub
End Class





' https://csharp.hotexamples.com/examples/OpenCvSharp/MatExpr/-/php-matexpr-class-examples.html
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/MatOperations.cs
Public Class Mat_RowColRange : Inherits VBparent
    Public Sub New()
        labels(2) = "BitwiseNot of RowRange and ColRange"
        task.desc = "Perform operation on a range of cols and/or Rows."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim midX = src.Width / 2
        Dim midY = src.Height / 2
        dst2 = src
        cv.Cv2.BitwiseNot(dst2.RowRange(midY - 50, midY + 50), dst2.RowRange(midY - 50, midY + 50))
        cv.Cv2.BitwiseNot(dst2.ColRange(midX - 50, midX + 50), dst2.ColRange(midX - 50, midX + 50))
    End Sub
End Class





Public Class Mat_Managed : Inherits VBparent
    Public Sub New()
        labels(2) = "Color change is in the managed cv.vec3b array"
        task.desc = "There is a limited ability to use Mat data in Managed code directly."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static autoRand As New Random()
        Static img(src.Total) As cv.Vec3b
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, img)
        Static nextColor As cv.Vec3b
        If task.frameCount Mod 30 = 0 Then
            If nextColor = New cv.Vec3b(0, 0, 255) Then nextColor = New cv.Vec3b(0, 255, 0) Else nextColor = New cv.Vec3b(0, 0, 255)
        End If
        For i = 0 To img.Length - 1
            img(i) = nextColor
        Next
        Dim rect As New cv.Rect(autoRand.Next(0, src.Width - 50), autoRand.Next(0, src.Height - 50), 50, 50)
        dst2(rect).SetTo(0)
    End Sub
End Class






Public Class Mat_MultiplyReview : Inherits VBparent
    Dim flow As New Font_FlowText
    Public Sub New()
        task.desc = "Review matrix multiplication"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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

        flow.RunClass(Nothing)
    End Sub
End Class






' https://stackoverflow.com/questions/11015119/inverse-matrix-opencv-matrix-inv-not-working-properly
Public Class Mat_Inverse : Inherits VBparent
    Dim flow As New Font_FlowText
    Public matrix(,) As Single = {{1.1688, 0.23, 62.2}, {-0.013, 1.225, -6.29}, {0, 0, 1}}
    Public validateInverse As Boolean
    Public inverse As New cv.Mat
    Public Sub New()
        If radio.Setup(caller, 6) Then
            radio.check(0).Text = "Cholesky"
            radio.check(1).Text = "Eig (works but results are incorrect)"
            radio.check(2).Text = "LU"
            radio.check(3).Text = "Normal (not working)"
            radio.check(4).Text = "QR (not working)"
            radio.check(5).Text = "SVD (works but results are incorrect)"
            radio.check(0).Checked = True
            radio.check(3).Enabled = False ' not accepted!
            radio.check(4).Enabled = False ' not accepted!
        End If
        task.desc = "Given a 3x3 matrix, invert it and present results."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim nextline = ""

        Dim decompType = cv.DecompTypes.Cholesky
        Static frm = findfrm("Mat_Inverse Radio Options")
        For i = 0 To frm.check.length - 1
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

        flow.RunClass(Nothing)
    End Sub
End Class







' https://github.com/takuya-takeuchi/DlibDotNet/tree/master/examples/3rdparty/OpenCVSharp/MatToArray2D
Public Class Mat_2Dlib : Inherits VBparent
    Public dRGB As Array2D(Of BgrPixel)
    Public dGray As Array2D(Of Byte)
    Public Sub New()
        task.desc = "Convert a Mat to the expected Array2D for a DLib API"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim array(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, array, 0, array.Length)

        If src.Type = cv.MatType.CV_8U Then
            dGray = Dlib.LoadImageData(Of Byte)(array, src.Rows, src.Cols, src.Cols * src.ElemSize)
        Else
            dRGB = Dlib.LoadImageData(Of BgrPixel)(array, src.Rows, src.Cols, src.Cols * src.ElemSize)
        End If
        If standalone Then
            setTrueText("OpenCVB Mat converted to an Array2D for use with DlibDotNet")
        End If
    End Sub
End Class








' https://github.com/takuya-takeuchi/DlibDotNet/tree/master/examples/3rdparty/OpenCVSharp/MatToArray2D
Public Class Mat_Dlib2Mat : Inherits VBparent
    Public dGray As Array2D(Of Byte)
    Public dRGB As Array2D(Of BgrPixel)
    Public d32f As Array2D(Of Single)
    Public Sub New()
        task.desc = "Convert a Dlib Array2D to an OpenCV Mat"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        If dGray IsNot Nothing Then
            dst2 = New cv.Mat(dGray.Rows, dGray.Columns, cv.MatType.CV_8U)
            Marshal.Copy(dGray.ToBytes, 0, dst2.Data, dst2.Total)
        End If

        If dRGB IsNot Nothing Then
            dst3 = New cv.Mat(dRGB.Rows, dRGB.Columns, cv.MatType.CV_8UC3)
            Marshal.Copy(dRGB.ToBytes, 0, dst3.Data, dst3.Total * dst3.ElemSize)
        End If
    End Sub
End Class








Public Class Mat_2to1 : Inherits VBparent
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Public mat() As cv.Mat = {mat1, mat2}
    Public lineSeparators = True ' if they want lines or not...
    Public Sub New()
        mat1 = New cv.Mat(New cv.Size(dst2.Rows, dst2.Cols), cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat = {mat1, mat2}

        labels(2) = ""
        task.desc = "Fill a Mat with 2 images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static nSize = New cv.Size(task.color.Width, task.color.Height / 2)
        Static roiTop = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roibot = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            mat1 = src
            mat2 = task.RGBDepth
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





Public Class Mat_4to1 : Inherits VBparent
    Dim model As New cv.Mat(task.color.Size, cv.MatType.CV_8UC3, 0)
    Public mat() As cv.Mat = {model.Clone, model.Clone, model.Clone, model.Clone}
    Public lineSeparators = True ' if they want lines or not...
    Public Sub New()
        labels(2) = "Combining 4 images into one"
        labels(3) = "Click any quadrant at left to view it below"
        task.desc = "Use one Mat for up to 4 images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static nSize = New cv.Size(dst2.Width / 2, dst2.Height / 2)
        Static roiTopLeft = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roiTopRight = New cv.Rect(nSize.Width, 0, nSize.Width, nSize.Height)
        Static roibotLeft = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        Static roibotRight = New cv.Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            mat = {task.color.Clone, task.RGBDepth.Clone, task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)}
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3)
        For i = 0 To 4 - 1
            Dim tmp = mat(i).Clone
            If tmp.Channels = 1 Then tmp = mat(i).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim roi = Choose(i + 1, roiTopLeft, roiTopRight, roibotLeft, roibotRight)
            dst2(roi) = tmp.Resize(nSize)
        Next
        If lineSeparators Then
            dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), cv.Scalar.White, task.lineWidth + 1)
            dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth + 1)
        End If
    End Sub
End Class









Public Class Mat_4Click : Inherits VBparent
    Public mats As New Mat_4to1
    Public mat() As cv.Mat
    Public Sub New()
        mat = mats.mat
        labels(3) = "Click a quadrant in dst2 to view it in dst3"
        task.desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        mat = mats.mat
        mats.RunClass(Nothing)
        dst2 = mats.dst2.Clone

        If task.mouseClickFlag And task.mousePicTag = RESULT_DST2 Then setMyActiveMat()
        dst3 = mats.mat(quadrantIndex).Clone
    End Sub
End Class