Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Classifier_Basics : Inherits VB_Algorithm
    Dim options As New Options_Classifier
    Public Sub New()
        cPtr = OEX_Points_Classifier_Open()
        desc = "OpenCV Example Points_Classifier"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then gOptions.DebugCheckBox.Checked = True
        Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, options.sampleCount, options.methodIndex, dst2.Rows, dst2.Cols,
                                                    If(gOptions.DebugCheckBox.Checked, 1, 0))
        gOptions.DebugCheckBox.Checked = False
        dst1 = New cv.Mat(dst0.Rows, dst0.Cols, cv.MatType.CV_32S, imagePtr)

        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = vbPalette(dst0 * 255 / 2)
        imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.dotSize)
        dst3 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)

        setTrueText("Click the global DebugCheckBox to get another set of points.", 3)
    End Sub
    Public Sub Close()
        OEX_Points_Classifier_Close(cPtr)
    End Sub
End Class








Module OEX_Points_Classifier_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_Points_Classifier_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OEX_Points_Classifier_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_ShowPoints(cPtr As IntPtr, imgRows As Integer, imgCols As Integer, dotSize As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OEX_Points_Classifier_RunCPP(cPtr As IntPtr, count As Integer, methodIndex As Integer,
                                                 imgRows As Integer, imgCols As Integer, resetInput As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Classifier_Bayesian_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Classifier_Bayesian_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Classifier_Bayesian_RunCPP(cPtr As IntPtr, count As Integer, methodIndex As Integer,
                                                 imgRows As Integer, imgCols As Integer, resetInput As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Classifier_Bayesian_Train(cPtr As IntPtr, trainInput As IntPtr, response As IntPtr, count As Integer) As IntPtr
    End Function
End Module








Public Class Classifier_Bayesian : Inherits VB_Algorithm
    Public Sub New()
        cPtr = OEX_Points_Classifier_Open()
        desc = "Run the Bayesian classifier with the input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim sampleCount As Integer, methodIndex = 0
        If src.Type <> cv.MatType.CV_32FC2 Then
            Static options As New Options_Classifier
            options.RunVB()
            sampleCount = options.sampleCount
            methodIndex = options.methodIndex
        Else
            sampleCount = src.Rows
        End If
        If task.optionsChanged Then gOptions.DebugCheckBox.Checked = True
        Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, sampleCount, methodIndex, dst2.Rows, dst2.Cols,
                                                    If(gOptions.DebugCheckBox.Checked, 1, 0))
        gOptions.DebugCheckBox.Checked = False
        dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_32S, imagePtr)
        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = vbPalette(dst0 * 255 / 2)
        imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.dotSize)
    End Sub
End Class










Public Class Classifier_BayesianTest : Inherits VB_Algorithm
    Dim redC As New RedCloud_MasksBoth
    Dim bayes As New Classifier_Bayesian
    Public Sub New()
        desc = "Classify the neighbor cells to be similar to the selected cell or not."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst3
        labels = redC.labels


    End Sub
End Class
