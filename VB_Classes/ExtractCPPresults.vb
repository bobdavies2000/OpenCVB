Imports cv = OpenCvSharp
Public Class ExtractCPPresults_FeatureLess : Inherits VB_Algorithm
    Dim cpp As New CPP_Basics
    Public Sub New()
        cpp.updateFunction(algorithmList.functionNames._CPP_RedColor_FeatureLess)
        desc = "This shows how to extract the output of a C++ algorithm back into VB.Net"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cpp.Run(src)
        dst2 = cpp.dst3
        labels(2) = cpp.labels(2)
    End Sub
End Class






Public Class ExtractCPPresults_EdgeDrawing : Inherits VB_Algorithm
    Dim cpp As New CPP_Basics
    Public Sub New()
        cpp.updateFunction(algorithmList.functionNames._CPP_EdgeDraw_Basics)
        desc = "Use EdgeDrawing to define featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cpp.Run(src)
        dst2 = cpp.dst2
        dst3 = cpp.dst3
    End Sub
End Class