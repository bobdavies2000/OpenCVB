Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class XR_Classifier_Basics_CPP : Inherits TaskParent
        Implements IDisposable
        Dim options As New Options_Classifier
        Public Sub New()
            cPtr = OEX_Points_Classifier_Open()
            desc = "OpenCV Example Points_Classifier"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.optionsChanged Then task.gOptions.DebugCheckBox.Checked = True
            Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, options.sampleCount, options.methodIndex,
                                                            dst2.Rows, dst2.Cols,
                                                            If(task.gOptions.DebugCheckBox.Checked, 1, 0))
            task.gOptions.DebugCheckBox.Checked = False
            dst1 = Mat.FromPixelData(dst0.Rows, dst0.Cols, MatType.CV_32S, imagePtr)

            dst1.ConvertTo(dst0, MatType.CV_8U)
            dst2 = Palettize(dst0)
            imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.DotSize)
            dst3 = Mat.FromPixelData(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr)

            SetTrueText("Click the global DebugCheckBox to get another set of points.", 3)
        End Sub
        Protected Overrides Sub Finalize()
            OEX_Points_Classifier_Close(cPtr)
        End Sub
    End Class








    Module OEX_Points_Classifier_CPP_Module
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Function OEX_Points_Classifier_Open() As IntPtr
        End Function
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Sub OEX_Points_Classifier_Close(cPtr As IntPtr)
        End Sub
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Function OEX_ShowPoints(cPtr As IntPtr, imgRows As Integer, imgCols As Integer, DotSize As Integer) As IntPtr
        End Function
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Function OEX_Points_Classifier_RunCPP(cPtr As IntPtr, count As Integer, methodIndex As Integer,
                                                     imgRows As Integer, imgCols As Integer, resetInput As Integer) As IntPtr
        End Function




        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Function Classifier_Bayesian_Open() As IntPtr
        End Function
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Sub Classifier_Bayesian_Close(cPtr As IntPtr)
        End Sub
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Sub Classifier_Bayesian_Train(cPtr As IntPtr, trainInput As IntPtr, response As IntPtr, count As Integer)
        End Sub
        <DllImport(("CPP_Native.dll"), CallingConvention:=CallingConvention.Cdecl)>
        Public Function Classifier_Bayesian_RunCPP(cPtr As IntPtr, trainInput As IntPtr, count As Integer) As IntPtr
        End Function
    End Module








    Public Class XR_Classifier_Bayesian : Inherits TaskParent
        Implements IDisposable
        Dim options As New Options_Classifier
        Public Sub New()
            cPtr = OEX_Points_Classifier_Open()
            desc = "Run the Bayesian classifier with the input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim sampleCount As Integer, methodIndex = 0
            If src.Type <> MatType.CV_32FC2 Then
                options.Run()
                sampleCount = options.sampleCount
                methodIndex = options.methodIndex
            Else
                sampleCount = src.Rows
            End If
            If task.optionsChanged Then task.gOptions.DebugCheckBox.Checked = True
            Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, sampleCount, methodIndex, dst2.Rows, dst2.Cols,
            If(task.gOptions.DebugCheckBox.Checked, 1, 0))
            task.gOptions.DebugCheckBox.Checked = False
            dst1 = Mat.FromPixelData(dst1.Rows, dst1.Cols, MatType.CV_32S, imagePtr)
            dst1.ConvertTo(dst0, MatType.CV_8U)
            dst2 = Palettize(dst0)
        End Sub
        Protected Overrides Sub Finalize()
            OEX_Points_Classifier_Close(cPtr)
        End Sub
    End Class
End Namespace