Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Fuzzy_Basics : Inherits VBparent
    Dim Fuzzy As IntPtr
    Dim reduction As New Reduction_Basics
    Dim options As New Contours_Basics
    Public gray As cv.Mat
    Public contours As cv.Point()()
    Public sortContours As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        Dim floodRadio = findRadio("FloodFill")
        If floodRadio.Enabled Then floodRadio.Enabled = False ' too much special handling - cv_32SC1 image 

        Fuzzy = Fuzzy_Open()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Threshold for rectangle size", 50, 50000, 10000)
        End If
        If standalone Then sliders.Visible = False

        label1 = "Solid regions"
        label2 = "Fuzzy pixels - not solid"
        task.desc = "That which is not solid is fuzzy"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        options.setOptions()
        reduction.Run(src)
        dst1 = reduction.dst1
        If dst1.Channels <> 1 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim srcData(dst1.Total) As Byte
        Marshal.Copy(dst1.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Fuzzy_Run(Fuzzy, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(dst1.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            gray = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC1, dstData)
        End If

        dst2 = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        Dim tmp As New cv.Mat
        dst2.ConvertTo(tmp, cv.MatType.CV_32S)
        contours = cv.Cv2.FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)

        sortContours.Clear()
        Dim countContours As Integer
        For i = 0 To contours.Length - 1
            If contours(i).Length > 100 Then
                countContours += 1
                ' get this region's ID
                Dim maskID As Integer = 0
                Dim pt = contours(i)(0)
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                            Dim val = gray.Get(Of Byte)(y, x)
                            If val <> 0 Then
                                maskID = val
                                Exit For
                            End If
                        End If
                    Next
                    If maskID <> 0 Then Exit For
                Next
                sortContours.Add(contours(i).Length, New cv.Point(i, maskID))
            End If
        Next

        task.palette.Run(gray)
        dst1 = task.palette.dst1
        dst1.SetTo(0, dst2)
        label1 = "There were " + CStr(countContours) + " contour > 100 points."
    End Sub
    Public Sub Close()
        Fuzzy_Close(Fuzzy)
    End Sub
End Class






Public Class Fuzzy_Filter : Inherits VBparent
    Dim kernel As cv.Mat
    Dim reduction As New Reduction_Basics
    Public contours As cv.Point()()
    Public sortContours As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Dim options As New Contours_Basics
    Public Sub New()
        Dim array() As Single = {1, 1, 1, 1, 1, 1, 1, 1, 1}
        kernel = New cv.Mat(3, 3, cv.MatType.CV_32F, array)
        kernel *= 1 / 9
        task.desc = "Use a 2D filter to find smooth areas"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        options.setOptions()

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run(src)

        Dim src32f As New cv.Mat
        reduction.dst1.ConvertTo(src32f, cv.MatType.CV_32F)
        dst1 = src32f.Filter2D(-1, kernel)
        dst2 = dst1.Subtract(src32f)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)

        Dim tmp As New cv.Mat
        dst2.ConvertTo(tmp, cv.MatType.CV_32S)
        contours = cv.Cv2.FindContoursAsArray(tmp, options.retrievalMode, options.ApproximationMode)

        sortContours.Clear()
        For i = 0 To contours.Length - 1
            Dim maskID As Integer = 0
            Dim pt = contours(i)(0)
            For y = pt.Y - 1 To pt.Y + 1
                For x = pt.X - 1 To pt.X + 1
                    If x < src.Width And y < src.Height And x >= 0 And y >= 0 Then
                        Dim val = reduction.dst1.Get(Of Byte)(y, x)
                        If val <> 0 Then
                            maskID = val
                            Exit For
                        End If
                    End If
                Next
                If maskID <> 0 Then Exit For
            Next
            sortContours.Add(contours(i).Length, New cv.Point(i, maskID))
        Next

        task.palette.Run(reduction.dst1)
        dst1 = task.palette.dst1
        dst1.SetTo(0, dst2)
    End Sub
End Class








Module Fuzzy_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Fuzzy_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module








Public Class Fuzzy_ContoursDepth : Inherits VBparent
    Public fuzzyD as New Fuzzy_Basics
    Public Sub New()
        task.desc = "Use contours to outline solids in the depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fuzzyD.Run(task.RGBDepth)
        dst1 = fuzzyD.dst1
    End Sub
End Class








Public Class Fuzzy_NeighborProof : Inherits VBparent
    Dim fuzzy as New Fuzzy_Basics
    Public Sub New()
        task.desc = "Prove that every contour point has at least one and only one neighbor with the mask ID and that the rest are zero"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static proofFailed As Boolean = False
        If proofFailed Then Exit Sub
        fuzzy.Run(src)
        dst1 = fuzzy.gray
        For i = 0 To fuzzy.contours.Length - 1
            Dim len = fuzzy.contours(i).Length
            For j = 0 To len - 1
                Dim pt = fuzzy.contours(i)(j)
                Dim maskID As Integer = 0
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height Then
                            Dim val = dst1.Get(Of Byte)(y, x)
                            If val <> 0 Then maskID = val
                            If maskID <> 0 And val <> 0 And maskID <> val Then
                                MsgBox("Proof has failed!  There is more than one mask ID identified by this contour point.")
                                proofFailed = True
                                Exit Sub
                            End If
                        End If
                    Next
                Next
            Next
        Next
        task.trueText("Mask ID's for all contour points in each region identified only one region.", 10, 50, 3)
    End Sub
End Class








Public Class Fuzzy_TrackerDepth : Inherits VBparent
    Public fuzzy as New Fuzzy_Basics
    Public centroids As New List(Of cv.Point)
    Public rects As New List(Of cv.Rect)
    Public layoutColor As New List(Of Integer)
    Public highlightPoint As cv.Point
    Public highlightRect As cv.Rect
    Public highlightRegion = -1
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Display centroid and rectangle for each region"
            check.Box(0).Checked = True
        End If
        task.desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static displayCheck = findCheckBox("Display centroid and rectangle for each region")
        Static minRectSizeSlider = findSlider("Threshold for rectangle size")

        fuzzy.Run(task.RGBDepth)
        dst1 = fuzzy.dst1

        centroids.Clear()
        rects.Clear()
        layoutColor.Clear()
        Dim minX As Double, maxX As Double
        Dim minY As Double, maxY As Double
        Dim minRectSize = minRectSizeSlider.value
        Dim displayRect = displayCheck.checked
        For Each c In fuzzy.sortContours
            Dim contours = fuzzy.contours(c.Value.Item0)
            Dim points = New cv.Mat(contours.Length, 1, cv.MatType.CV_32SC2, contours.ToArray)
            Dim center = points.Sum()
            points = New cv.Mat(contours.Length, 2, cv.MatType.CV_32S, contours.ToArray)
            points.Col(0).MinMaxIdx(minX, maxX)
            points.Col(1).MinMaxIdx(minY, maxY)

            Dim rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
            If rect.Width * rect.Height > minRectSize Then
                Dim centroid = New cv.Point2f(center.Item(0) / contours.Length, center.Item(1) / contours.Length)
                centroids.Add(centroid)
                rects.Add(rect)
                layoutColor.Add(c.Value.Item1)
                If displayRect Then
                    dst1.Circle(centroid, 6, cv.Scalar.Yellow, -1, task.lineType)
                    dst1.Circle(centroid, 3, cv.Scalar.Red, -1, task.lineType)
                    dst1.Rectangle(rect, cv.Scalar.Yellow, 2)
                End If
            End If
        Next

        label1 = CStr(fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class







Public Class Fuzzy_TrackerDepthClick : Inherits VBparent
    Public tracker as New Fuzzy_TrackerDepth
    Public highlightPoint As cv.Point
    Public highlightRect As cv.Rect
    Public highlightRegion = -1
    Public regionMask As cv.Mat
    Public Sub New()
        task.desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tracker.Run(src)
        dst1 = tracker.dst1

        If standalone And highlightRegion < 0 Then task.trueText("Click any color region to get more details and track it", 10, 50, 3)

        Dim gray = tracker.fuzzy.gray
        If task.mouseClickFlag Then
            highlightPoint = task.mouseClickPoint
            highlightRegion = gray.Get(Of Byte)(highlightPoint.Y, highlightPoint.X)
            task.mouseClickFlag = False
        End If
        If highlightRegion >= 0 Then
            dst2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            regionMask = gray.InRange(highlightRegion, highlightRegion + 1)
            dst2.SetTo(cv.Scalar.Yellow, regionMask)
        End If
        label1 = CStr(tracker.fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class








Public Class Fuzzy_PointTracker : Inherits VBparent
    Dim fuzzy as New Fuzzy_Basics
    Dim pTrack As New KNN_PointTracker
    Dim flood As New FloodFill_Palette
    Public Sub New()
        fuzzy.sliders.Visible = False
        task.desc = "FloodFill the regions defined as solid"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fuzzy.Run(src)
        dst2 = fuzzy.dst1

        flood.Run(fuzzy.dst1)

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryRects = flood.basics.rects
        pTrack.queryMasks = flood.basics.masks
        pTrack.Run(src)

        label2 = CStr(pTrack.drawRC.viewObjects.Count) + " regions were found"
        dst1 = pTrack.dst1
    End Sub
End Class


