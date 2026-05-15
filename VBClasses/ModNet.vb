Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp.Dnn
Imports Microsoft.ML.OnnxRuntime
Imports Microsoft.ML.OnnxRuntime.Tensors

''' <summary>
''' Portrait matting via ONNX (MODNet-style): soft alpha matte and composite on light gray.
''' Uses ONNX Runtime (not OpenCV DNN) because MODNet graphs often hit 
''' "Inconsistent shape for ConcatLayer" in OpenCV.
''' Place modnet.onnx under ModNet/ (see candidate list in ResolveAndLoadModel).
''' </summary>
Public Class ModNet_Basics : Inherits TaskParent
    Dim ortSession As InferenceSession = Nothing
    Dim ortInputName As String = "input"
    Dim ortOutputName As String = ""
    Shared ReadOnly dnnSize As New cv.Size(512, 512)
    Public Sub New()
        desc = "Cursor.ai: Portrait matting (ONNX Runtime). Same MODNet ONNX as before; " +
               " avoids OpenCV DNN ConcatLayer errors. Model in ModNet/."
        labels = {"", "", "Subject with background removed", "Mask of isolated subject"}
    End Sub
    Private Sub ResolveAndLoadModel()
        Dim opts As New SessionOptions()
        opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        ortSession = New InferenceSession(task.homeDir + "ModNet/MODNet.onnx", opts)

        ortInputName = ortSession.InputMetadata.Keys.First()
        Dim outKeys = ortSession.OutputMetadata.Keys.ToList()
        If ortSession.OutputMetadata.ContainsKey("output") Then
            ortOutputName = "output"
        ElseIf ortSession.OutputMetadata.ContainsKey("pha") Then
            ortOutputName = "pha"
        ElseIf outKeys.Count > 0 Then
            ortOutputName = outKeys(0)
        End If
        If ortSession.InputMetadata.ContainsKey("input") Then ortInputName = "input"
    End Sub
    ''' <summary>NCHW float tensor [1,3,H,W] matching CvDnn.BlobFromImage 
    ''' (swapRB, mean 127.5, scale 1/127.5).</summary>
    Private Shared Function PreprocessToNCHW(resizedBgr As cv.Mat) As DenseTensor(Of Single)
        Dim count = 1 * 3 * dnnSize.Height * dnnSize.Width
        Dim data(count - 1) As Single
        Using blob = CvDnn.BlobFromImage(resizedBgr, 1.0 / 127.5, dnnSize,
                                         New cv.Scalar(127.5, 127.5, 127.5), True, False)
            Marshal.Copy(blob.Data, data, 0, count)
        End Using
        Return New DenseTensor(Of Single)(data, {1, 3, dnnSize.Height, dnnSize.Width})
    End Function
    Private Shared Function TensorToAlphaMat(t As Tensor(Of Single)) As cv.Mat
        Dim dims = t.Dimensions.ToArray()
        If dims.Length < 2 Then Return New cv.Mat()
        Dim h = dims(dims.Length - 2)
        Dim w = dims(dims.Length - 1)
        Dim dense = t.ToDenseTensor()
        Dim arr = dense.Buffer.ToArray()
        Dim need = h * w
        If arr.Length < need Then Return New cv.Mat()
        Return cv.Mat.FromPixelData(h, w, cv.MatType.CV_32F, arr).Clone()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If ortOutputName = "" Then ResolveAndLoadModel()

        Dim resized As New cv.Mat
        cv.Cv2.Resize(src, resized, dnnSize)
        Dim inputTensor = PreprocessToNCHW(resized)

        Dim alpha512 As cv.Mat = Nothing
        Try
            Dim inputs = New List(Of NamedOnnxValue) From {
                NamedOnnxValue.CreateFromTensor(ortInputName, inputTensor)
            }
            Using results = ortSession.Run(inputs)
                Dim picked As NamedOnnxValue = Nothing
                For Each v In results
                    If v.Name = ortOutputName Then
                        picked = v
                        Exit For
                    End If
                Next
                If picked Is Nothing Then picked = results.First()
                Dim tens = TryCast(picked.Value, Tensor(Of Single))
                If tens Is Nothing Then MsgBox("Unexpected ONNX output type; expected float tensor.")
                alpha512 = TensorToAlphaMat(tens)
            End Using
        Catch ex As Exception
            MsgBox("OnnxRuntime Run failed: " + ex.Message)
            Exit Sub
        End Try

        If alpha512 Is Nothing OrElse alpha512.Empty Then MsgBox("Empty alpha output from ONNX model.")

        Dim mm = GetMinMax(alpha512)
        If mm.maxVal > 1.05 Then alpha512.ConvertTo(alpha512, cv.MatType.CV_32F, 1.0 / 255.0)

        Dim alphaFull As New cv.Mat
        cv.Cv2.Resize(alpha512, alphaFull, src.Size(), 0, 0, cv.InterpolationFlags.Linear)

        Dim ones As New cv.Mat(alphaFull.Size, cv.MatType.CV_32F, New cv.Scalar(1))
        Dim zeros As New cv.Mat(alphaFull.Size, cv.MatType.CV_32F, New cv.Scalar(0))
        cv.Cv2.Min(alphaFull, ones, alphaFull)
        cv.Cv2.Max(alphaFull, zeros, alphaFull)

        Dim src32 As New cv.Mat
        src.ConvertTo(src32, cv.MatType.CV_32FC3)
        Dim ch = src32.Split()
        Dim inv As New cv.Mat(alphaFull.Size, cv.MatType.CV_32F)
        Dim onePlane As New cv.Mat(alphaFull.Size, cv.MatType.CV_32F, New cv.Scalar(1))
        cv.Cv2.Subtract(onePlane, alphaFull, inv)

        Dim bgPlane As New cv.Mat(alphaFull.Size, cv.MatType.CV_32F, New cv.Scalar(240.0F / 255.0F))
        For i = 0 To 2
            Dim fg As New cv.Mat
            Dim bgTerm As New cv.Mat
            cv.Cv2.Multiply(ch(i), alphaFull, fg)
            cv.Cv2.Multiply(inv, bgPlane, bgTerm)
            cv.Cv2.Add(fg, bgTerm, ch(i))
        Next

        cv.Cv2.Merge(ch, src32)
        src32.ConvertTo(dst2, cv.MatType.CV_8UC3)

        alphaFull.ConvertTo(dst3, cv.MatType.CV_8U, 255.0)
    End Sub
    Protected Overrides Sub Finalize()
        If ortSession IsNot Nothing Then ortSession.Dispose()
    End Sub
End Class
