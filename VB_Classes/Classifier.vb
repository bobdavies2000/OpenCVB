Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Classifier_Basics_CPP_VB : Inherits TaskParent
    Dim options As New Options_Classifier
    Public Sub New()
        cPtr = OEX_Points_Classifier_Open()
        desc = "OpenCV Example Points_Classifier"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Then task.gOptions.debugChecked = True
        Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, options.sampleCount, options.methodIndex, dst2.Rows, dst2.Cols,
If(task.gOptions.debugChecked, 1, 0))
        task.gOptions.debugChecked = False
        dst1 = cv.Mat.FromPixelData(dst0.Rows, dst0.Cols, cv.MatType.CV_32S, imagePtr)

        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst0 * 255 / 2)
        imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.DotSize)
        dst3 = cv.Mat.FromPixelData(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)

        SetTrueText("Click the global DebugCheckBox to get another set of points.", 3)
    End Sub
    Public Sub Close()
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








Public Class Classifier_Bayesian : Inherits TaskParent
    Dim options As New Options_Classifier
    Public Sub New()
        cPtr = OEX_Points_Classifier_Open()
        desc = "Run the Bayesian classifier with the input."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Dim sampleCount As Integer, methodIndex = 0
        If src.Type <> cv.MatType.CV_32FC2 Then
            options.RunOpt()
            sampleCount = options.sampleCount
            methodIndex = options.methodIndex
        Else
            sampleCount = src.Rows
        End If
        If task.optionsChanged Then task.gOptions.debugChecked = True
        Dim imagePtr = OEX_Points_Classifier_RunCPP(cPtr, sampleCount, methodIndex, dst2.Rows, dst2.Cols,
If(task.gOptions.debugChecked, 1, 0))
        task.gOptions.debugChecked = False
        dst1 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_32S, imagePtr)
        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst0 * 255 / 2)
        imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.DotSize)
    End Sub
End Class










Public Class Classifier_BayesianTest : Inherits TaskParent
    Dim nabs As New Neighbors_Precise
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "Mask of the neighbors to the selected cell", "RedCloud_Basics output", "Classifier_Bayesian output"}
        If standalone Then task.gOptions.setDisplay1()
        cPtr = Classifier_Bayesian_Open()
        desc = "Classify the neighbor cells to be similar to the selected cell or not."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        SetTrueText("Review the neighbors_Precise algorithm")
        'nabs.redCells = task.redCells
        'nabs.Run(task.redMap)

        'Dim trainList As New List(Of cv.Scalar)
        'Dim responseList As New List(Of Integer)
        'For Each rc In task.redCells
        '    trainList.Add(rc.depthMean)
        '    responseList.Add(0)
        'Next

        'dst1.SetTo(0)
        'For Each index In nabs.nabList(task.rc.index)
        '    Dim rc = task.redCells(index)
        '    dst1(rc.rect).SetTo(255, rc.mask)
        '    strOut += CStr(index) + ","
        '    responseList(index) = -1
        'Next

        'responseList(task.rc.index) = 1

        'Dim queryList As New List(Of cv.Scalar)
        'Dim maskList As New List(Of Integer)
        'For i = responseList.Count - 1 To 0 Step -1
        '    If responseList(i) = -1 Then
        '        responseList.RemoveAt(i)
        '        queryList.Add(trainList(i))
        '        trainList.RemoveAt(i)
        '        maskList.Add(i)
        '    End If
        'Next

        'Dim vecs = trainList.ToArray
        'Dim resp = responseList.ToArray
        'Dim handleTrainInput = GCHandle.Alloc(vecs, GCHandleType.Pinned)
        'Dim handleResponse = GCHandle.Alloc(resp, GCHandleType.Pinned)
        'Classifier_Bayesian_Train(cPtr, handleTrainInput.AddrOfPinnedObject(), handleResponse.AddrOfPinnedObject(), responseList.Count)
        'handleResponse.Free()
        'handleTrainInput.Free()

        'Dim results(queryList.Count - 1) As Integer
        'If queryList.Count > 0 Then
        '    Dim queries = queryList.ToArray
        '    Dim handleQueryInput = GCHandle.Alloc(queries, GCHandleType.Pinned)
        '    Dim resultsPtr = Classifier_Bayesian_RunCPP(cPtr, handleQueryInput.AddrOfPinnedObject(), queries.Count)
        '    handleQueryInput.Free()

        '    Marshal.Copy(resultsPtr, results, 0, results.Length)
        'End If
        'dst3.SetTo(0)
        'Dim zeroOutput As Boolean = True
        'For i = 0 To maskList.Count - 1
        '    If results(i) > 0 Then
        '        Dim rc = task.redCells(maskList(i))
        '        dst3(rc.rect).SetTo(rc.color, rc.mask)
        '        zeroOutput = False
        '    End If
        'Next
        'If zeroOutput Then SetTrueText("None of the neighbors were as similar to the selected cell.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then Classifier_Bayesian_Close(cPtr)
    End Sub
End Class
