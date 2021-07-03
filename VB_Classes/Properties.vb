Imports cv = OpenCvSharp
Public Class Properties_Basics : Inherits VBparent
    Dim cloud As New PointCloud_Basics
    Public Sub New()
        task.desc = "Build properties list from the given input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Then
            setTrueText("Properties analyzes the features of a selected region.  It does nothing when run standalone.")
            Exit Sub
        End If

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(dst1, src)

        cloud.RunClass(dst1)
        dst2 = cloud.dst2
        dst3 = cloud.dst3

        setTrueText("Number of objects = " + CStr(cloud.objectsFound), 10, 40, 3)
    End Sub
End Class







Public Class Properties_DepthRegion : Inherits VBparent
    Dim lut As New Depth_Objects
    Dim props As New Properties_Basics
    Dim mat As New Mat_4to1
    Public Sub New()
        usingdst0 = True
        labels(2) = "The masked region of the depth32f image."
        task.desc = "Find the properties of a depth region"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static radioLUT = findRadio("Use RGB LUT")
        radioLUT.checked = True

        lut.RunClass(src)
        dst0 = lut.dst1
        mat.mat(0) = dst0
        mat.mat(1) = lut.dst3

        props.RunClass(lut.dst3)
        mat.mat(2) = props.dst2
        mat.mat(3) = props.dst3

        mat.RunClass(Nothing)
        dst2 = mat.dst2

    End Sub
End Class