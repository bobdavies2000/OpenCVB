Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class Mat_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Use the repeat method to replicate data."
        End Sub
        Public Shared Function srcMustBe8U(src As cv.Mat) As cv.Mat
            If src.Type <> cv.MatType.CV_8U Then
                Static color8U As New Color8U_Basics
                color8U.Run(src)
                Return color8U.dst2
            End If
            Return src
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim small = src.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
            dst2 = small.Repeat(10, 10)
            small = taskA.depthRGB.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
            dst3 = small.Repeat(10, 10)
        End Sub
    End Class








    Public Class NR_Mat_PointToMat : Inherits TaskParent
        Dim random As New Random_Basics
        Public Sub New()
            labels(2) = "Random_Basics points (original)"
            labels(3) = "Random_Basics points after format change with indexer"
            desc = "Convert point2f into a mat of points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            random.Run(src)
            dst2.SetTo(0)
            For Each pt In random.PointList
                DrawCircle(dst2, pt, taskA.DotSize, cv.Scalar.Yellow)
            Next

            Dim rows = random.PointList.Count
            Dim pMat = cv.Mat.FromPixelData(rows, 1, cv.MatType.CV_32FC2, random.PointList.ToArray)
            Dim indexer = pMat.GetGenericIndexer(Of cv.Vec2f)()
            dst3.SetTo(0)
            Dim white = New cv.Vec3b(255, 255, 255)
            For i = 0 To rows - 1
                dst3.Set(Of cv.Vec3b)(indexer(i)(1), indexer(i)(0), white)
            Next
        End Sub
    End Class






    Public Class NR_Mat_MatToPoint : Inherits TaskParent
        Public Sub New()
            desc = "Convert a mat into a vector of points."
            labels(2) = "Reconstructed BGR Image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim points(src.Total - 1) As cv.Vec3b
            Dim vec As New cv.Vec3b
            Dim index As Integer = 0
            Dim m3b = src.Clone()
            Dim indexer = m3b.GetGenericIndexer(Of cv.Vec3b)()
            For y = 0 To src.Rows - 1
                For x = 0 To src.Cols - 1
                    vec = indexer(y, x)
                    points(index) = New cv.Vec3b(vec(0), vec(1), vec(2))
                    index += 1
                Next
            Next
            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, points)
        End Sub
    End Class







    Public Class NR_Mat_Transpose : Inherits TaskParent
        Public Sub New()
            desc = "Transpose a Mat and show taskA.results.."
            labels(2) = "Color Image Transposed"
            labels(3) = "Color Image Transposed back (artifacts)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim trColor = src.T()
            dst2 = trColor.ToMat.Resize(New cv.Size(src.Cols, src.Rows))
            Dim trBack = dst2.T()
            dst3 = trBack.ToMat.Resize(src.Size())
        End Sub
    End Class






    ' https://csharp.hotexamples.com/examples/OpenCvSharp/Mat/-/php-mat-class-examples.html#0x95f170f4714e3258c220a78eacceeee99591440b9885a2997bbbc6b3aebdcf1c-19,,37,
    Public Class NR_Mat_Tricks : Inherits TaskParent
        Public Sub New()
            labels(2) = "Image squeezed into square Mat"
            labels(3) = "Mat transposed around the diagonal"
            desc = "Show some Mat tricks."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim mat = src.Resize(New cv.Size(src.Height, src.Height))
            Dim roi = New cv.Rect(0, 0, mat.Width, mat.Height)
            dst2(roi) = mat
            dst3(roi) = mat.T
        End Sub
    End Class





    ' https://csharp.hotexamples.com/examples/OpenCvSharp/MatExpr/-/php-matexpr-class-examples.html
    ' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/MatOperations.cs
    Public Class NR_Mat_RowColRange : Inherits TaskParent
        Public Sub New()
            labels(2) = "BitwiseNot of RowRange and ColRange"
            desc = "Perform operation on a range of cols and/or Rows."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim midX = src.Width / 2
            Dim midY = src.Height / 2
            dst2 = src
            cv.Cv2.BitwiseNot(dst2.RowRange(midY - 25, midY + 25), dst2.RowRange(midY - 25, midY + 25))
            cv.Cv2.BitwiseNot(dst2.ColRange(midX - 25, midX + 25), dst2.ColRange(midX - 25, midX + 25))
        End Sub
    End Class





    Public Class NR_Mat_Managed : Inherits TaskParent
        Dim autoRand As New Random()
        Dim img(dst2.Total - 1) As cv.Vec3b
        Dim nextColor As cv.Vec3b
        Public Sub New()
            labels(2) = "Color change is in the managed cv.vec3b array"
            desc = "There is a limited ability to use Mat data in Managed code directly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, img)
            If taskA.heartBeat Then
                If nextColor = New cv.Vec3b(0, 0, 255) Then nextColor = New cv.Vec3b(0, 255, 0) Else nextColor = New cv.Vec3b(0, 0, 255)
            End If
            For i = 0 To img.Length - 1
                img(i) = nextColor
            Next
            Dim rect As New cv.Rect(autoRand.Next(0, src.Width - 50), autoRand.Next(0, src.Height - 50), 50, 50)
            dst2(rect).SetTo(0)
        End Sub
    End Class






    Public Class NR_Mat_MultiplyReview : Inherits TaskParent
        Public Sub New()
            desc = "Review matrix multiplication"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim a(,) = {{1, 4, 2}, {2, 5, 1}}
            Dim b(,) = {{3, 4, 2}, {3, 5, 7}, {1, 2, 1}}
            strOut = "Matrix a" + vbCrLf
            For i = 0 To a.GetLength(0) - 1
                For j = 0 To a.GetLength(1) - 1
                    strOut += CStr(a(i, j)) + vbTab
                Next
                strOut += vbCrLf
            Next

            strOut += "Matrix b" + vbCrLf
            For i = 0 To b.GetLength(0) - 1
                For j = 0 To b.GetLength(1) - 1
                    strOut += CStr(b(i, j)) + vbTab
                Next
                strOut += vbCrLf
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


            strOut += "Matrix c = a X b" + vbCrLf
            For i = 0 To a.GetLength(0) - 1
                For j = 0 To a.GetLength(1) - 1
                    strOut += CStr(c(i, j)) + " = " + input(i, j)
                Next
                strOut += vbCrLf
            Next

            SetTrueText(strOut, 2)
        End Sub
    End Class






    ' https://stackoverflow.com/questions/11015119/inverse-matrix-opencv-matrix-inv-not-working-properly
    Public Class NR_Mat_Inverse : Inherits TaskParent
        Public matrix(,) As Single = {{1.1688, 0.23, 62.2}, {-0.013, 1.225, -6.29}, {0, 0, 1}}
        Public validateInverse As Boolean
        Public inverse As New cv.Mat
        Dim options As New Options_Mat
        Public Sub New()
            desc = "Given a 3x3 matrix, invert it and present taskA.results.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standaloneTest() Or validateInverse Then
                strOut = "Matrix Input " + vbCrLf
                For i = 0 To matrix.GetLength(0) - 1
                    For j = 0 To matrix.GetLength(1) - 1
                        strOut += CStr(matrix(i, j)) + vbTab
                    Next
                    strOut += vbCrLf
                Next
                strOut += vbCrLf
            End If

            Dim input = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, matrix)
            cv.Cv2.Invert(input, inverse, options.decompType)

            If standaloneTest() Or validateInverse Then
                strOut += "Matrix Inverse " + vbCrLf
                For i = 0 To matrix.GetLength(0) - 1
                    For j = 0 To matrix.GetLength(1) - 1
                        strOut += CStr(inverse.Get(Of Single)(j, i)) + vbTab
                    Next
                    strOut += vbCrLf
                Next
                strOut += vbCrLf

                Dim identity = (input * inverse).ToMat

                strOut += "Verify Inverse is correct " + vbCrLf
                For i = 0 To matrix.GetLength(0) - 1
                    For j = 0 To matrix.GetLength(1) - 1
                        strOut += CStr(identity.Get(Of Single)(j, i)) + vbTab
                    Next
                    strOut += vbCrLf
                Next
                strOut += vbCrLf
            End If

            SetTrueText(strOut, 2)
        End Sub
    End Class






    Public Class NR_Mat_Inverse_4D : Inherits TaskParent
        Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
        Public input As cv.Mat
        Public Sub New()
            input = cv.Mat.FromPixelData(4, 4, cv.MatType.CV_64F, defaultInput)
            desc = "Use OpenCV to invert a matrix"
        End Sub
        Private Function printMatrixResults(src As cv.Mat, dst2 As cv.Mat) As String
            Dim outstr As String = "Original Matrix " + vbCrLf
            For y = 0 To src.Rows - 1
                For x = 0 To src.Cols - 1
                    outstr += Format(src.Get(Of Double)(y, x), fmt4) + vbTab
                Next
                outstr += vbCrLf
            Next
            outstr += vbCrLf + "Matrix Inverse" + vbCrLf
            For y = 0 To src.Rows - 1
                For x = 0 To src.Cols - 1
                    outstr += Format(dst2.Get(Of Double)(y, x), fmt4) + vbTab
                Next
                outstr += vbCrLf
            Next
            Return outstr
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If input.Width <> input.Height Then
                SetTrueText("The input matrix must be square!")
                Exit Sub
            End If

            Dim result As New cv.Mat
            cv.Cv2.Invert(input, result, cv.DecompTypes.LU)
            Dim outstr = printMatrixResults(input, result)
            SetTrueText(outstr)
        End Sub
    End Class







    Public Class Mat_2to1 : Inherits TaskParent
        Dim mat1 As cv.Mat
        Dim mat2 As cv.Mat
        Public mat() As cv.Mat = {mat1, mat2}
        Public lineSeparators = True ' if they want lines or not...
        Public Sub New()
            mat1 = New cv.Mat(New cv.Size(dst2.Rows, dst2.Cols), cv.MatType.CV_8UC3, cv.Scalar.All(0))
            mat2 = mat1.Clone()
            mat = {mat1, mat2}

            labels(2) = ""
            desc = "Fill a Mat with 2 images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim nSize = New cv.Size(taskA.workRes.Width, taskA.workRes.Height / 2)
            Dim roiTop = New cv.Rect(0, 0, nSize.Width, nSize.Height)
            Dim roibot = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
            If standaloneTest() Then
                mat1 = src
                mat2 = taskA.depthRGB
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
                    dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), white, taskA.lineWidth + 1)
                End If
            End If
        End Sub
    End Class





    Public Class Mat_4Click : Inherits TaskParent
        Public mats As New Mat_4to1
        Public mat() As cv.Mat
        Public quadrant As Integer = 3
        Public Sub New()
            mat = mats.mat
            labels(3) = "Click a quadrant in dst2 to view it in dst3"
            desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            mat = mats.mat
            mats.Run(emptyMat)
            dst2 = mats.dst2.Clone
            If standalone Then mats.defaultMats(emptyMat)
            If taskA.firstPass Then
                taskA.ClickPoint = New cv.Point(0, 0)
                taskA.mousePicTag = 2
            End If

            If taskA.mouseClickFlag And taskA.mousePicTag = 2 Then
                If taskA.ClickPoint.Y < dst2.Rows / 2 Then
                    quadrant = If(taskA.ClickPoint.X < taskA.workRes.Width / 2, 0, 1)
                Else
                    quadrant = If(taskA.ClickPoint.X < taskA.workRes.Width / 2, 2, 3)
                End If
            End If
            mats.Run(emptyMat)
            dst2 = mats.dst2.Clone
            dst3 = mats.mat(quadrant).Clone
        End Sub
    End Class







    Public Class Mat_4to1 : Inherits TaskParent
        Public mat(3) As cv.Mat
        Public lineSeparators = True ' if they want lines or not...
        Public quadrant As Integer = 0
        Public Sub New()
            For i = 0 To mat.Length - 1
                mat(i) = dst2.Clone
            Next
            labels(2) = "Combining 4 images into one"
            labels(3) = "Click any quadrant at left to view it below"
            desc = "Use one Mat for up to 4 images"
        End Sub
        Public Sub defaultMats(src As cv.Mat)
            Dim tmpLeft = If(taskA.leftView.Channels() = 1, taskA.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR),
                         taskA.leftView)
            Dim tmpRight = If(taskA.rightView.Channels() = 1, taskA.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR),
                          taskA.rightView)
            mat = {taskA.color.Clone, taskA.depthRGB.Clone, tmpLeft, tmpRight}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim nSize = New cv.Size(dst2.Width / 2, dst2.Height / 2)
            Dim roiTopLeft = New cv.Rect(0, 0, nSize.Width, nSize.Height)
            Dim roiTopRight = New cv.Rect(nSize.Width, 0, nSize.Width, nSize.Height)
            Dim roibotLeft = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
            Dim roibotRight = New cv.Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height)
            If standalone Then defaultMats(src)

            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC3)
            For i = 0 To 4 - 1
                Dim tmp = mat(i).Clone
                If tmp.Channels() = 1 Then tmp = mat(i).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                Dim roi = Choose(i + 1, roiTopLeft, roiTopRight, roibotLeft, roibotRight)
                dst2(roi) = tmp.Resize(nSize)
            Next
            If lineSeparators Then
                dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), white, taskA.lineWidth + 1)
                dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), white, taskA.lineWidth + 1)
            End If
        End Sub
    End Class







    Public Class NR_Mat_FindNearZero : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("FindNearZero threshold X1000", 0, 200, 10)
            desc = "Find samples near zero using FindNonZero"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static thresholdSlider = OptionParent.FindSlider("FindNearZero threshold X1000")
            Dim threshold = thresholdSlider.value / 1000

            dst3 = taskA.pcSplit(1).InRange(-threshold, threshold)
            dst3.SetTo(0, taskA.noDepthMask)
            dst3.ConvertTo(dst2, cv.MatType.CV_8U)

            dst1 = dst3.FindNonZero()
            If dst1.Rows > 0 Then
                Dim ptLeft = dst1.Get(Of cv.Point)(0, 0)
                Dim ptRight = dst1.Get(Of cv.Point)(dst1.Rows - 1, 0)
            End If
        End Sub
    End Class




    Public Class Mat_Convert : Inherits TaskParent
        Public Sub New()
            desc = "Convert the input into 8uC3."
        End Sub
        Public Shared Function Mat_32f_To_8UC3(Input As cv.Mat) As cv.Mat
            Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
            If Input.Channels() = 1 Then
                outMat.ConvertTo(outMat, cv.MatType.CV_8U)
                Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
            outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
            Return outMat
        End Function
        Public Shared Function Mat_Check8UC3(src As cv.Mat) As cv.Mat
            If src.Type = cv.MatType.CV_8UC3 Then Return src
            Dim dst As New cv.Mat
            If src.Type = cv.MatType.CV_32F Then
                dst = Mat_32f_To_8UC3(src)
            ElseIf src.Type = cv.MatType.CV_32SC1 Then
                src.ConvertTo(dst, cv.MatType.CV_32F)
                dst = Mat_32f_To_8UC3(dst)
            ElseIf src.Type = cv.MatType.CV_32SC3 Then
                src.ConvertTo(dst, cv.MatType.CV_32F)
                dst = dst.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                dst = Mat_32f_To_8UC3(dst)
            ElseIf src.Type = cv.MatType.CV_32FC3 Then
                dst = src.ConvertScaleAbs(255)
            Else
                dst = src.Clone
            End If
            If src.Channels() = 1 And src.Type = cv.MatType.CV_8UC1 Then dst = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then src = taskA.pointCloud
            dst2 = Mat_Check8UC3(src)
        End Sub
    End Class


End Namespace