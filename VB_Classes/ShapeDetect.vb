'Imports cv = OpenCvSharp
'Imports System.Drawing.Imaging
'Imports System.Drawing
'' http://accord-framework.net/samples.html#
'Public Class ShapeDetect_Basics : Inherits VB_Algorithm
'    Public Sub New()
'        labels = {"", "", "Original image", "Labeled shapes"}
'        desc = "Accord Shape Detection"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If standaloneTest() Then
'            Dim bitmapTmp = New Bitmap(task.homeDir + "Data/demo1.jpg")
'            src = cv.Extensions.BitmapConverter.ToMat(bitmapTmp).Resize(src.Size)
'        End If

'        dst2 = src.Clone
'        Dim bitmap = cv.Extensions.BitmapConverter.ToBitmap(src)
'        Dim BitmapData = bitmap.LockBits(ImageLockMode.ReadWrite)

'        Dim blobCounter = New BlobCounter()
'        blobCounter.FilterBlobs = True
'        blobCounter.MinHeight = 5
'        blobCounter.MinWidth = 5
'        blobCounter.ProcessImage(BitmapData)

'        Dim blobs = blobCounter.GetObjectsInformation()
'        Dim shapeChecker = New SimpleShapeChecker()

'        bitmap.UnlockBits(BitmapData)
'        dst3 = src

'        For Each blob In blobs
'            Dim r = New cv.Rect(blob.Rectangle.Location.X, blob.Rectangle.Location.Y, blob.Rectangle.Width, blob.Rectangle.Height)
'            dst2.Rectangle(r, cv.Scalar.Yellow, task.lineWidth, task.lineType)

'            Dim edgePoints = blobCounter.GetBlobsEdgePoints(blob)
'            Dim pointlist As New List(Of cv.Point)
'            For Each pt In edgePoints
'                pointlist.Add(New cv.Point2f(pt.X, pt.Y))
'            Next
'            vbDrawContour(dst1, pointlist, cv.Scalar.Yellow, -1)

'            Dim center As Accord.Point
'            Dim radius As Single
'            If shapeChecker.IsCircle(edgePoints, center, radius) Then
'                dst3.Circle(New cv.Point(center.X, center.Y), radius, cv.Scalar.Red, task.lineWidth, task.lineType)
'            Else
'                Dim corners As New List(Of IntPoint)
'                shapeChecker.IsConvexPolygon(edgePoints, corners)

'                Dim subType = shapeChecker.CheckPolygonSubType(corners)
'                Dim cornerPoints As New List(Of cv.Point)
'                For Each pt In corners
'                    cornerPoints.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
'                Next
'                If subType = PolygonSubType.Unknown Then
'                    vbDrawContour(dst3, cornerPoints, cv.Scalar.White, -1)
'                Else
'                    vbDrawContour(dst3, cornerPoints, cv.Scalar.Green, -1)
'                End If
'            End If
'        Next
'    End Sub
'End Class







'' http://accord-framework.net/samples.html#
'Public Class ShapeDetect_Example : Inherits VB_Algorithm
'    Dim options As New Options_ShapeDetect
'    Public Sub New()
'        If standaloneTest() Then gOptions.displayDst0.Checked = True
'        If standaloneTest() Then gOptions.displayDst1.Checked = True

'        labels = {"", "Identified shapes", "Original image", "Labeled shapes"}
'        desc = "Accord Shape Detection example"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Options.RunVB()

'        Dim bitmap = New Bitmap(task.homeDir + "Data/" + options.fileName)
'        dst2 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(src.Size)

'        Dim colorfilter As New ColorFiltering
'        colorfilter.Red = New IntRange(0, 64)
'        colorfilter.Green = New IntRange(0, 64)
'        colorfilter.Blue = New IntRange(0, 64)
'        colorfilter.FillOutsideRange = False

'        Dim BitmapData = bitmap.LockBits(ImageLockMode.ReadWrite)
'        colorfilter.ApplyInPlace(BitmapData)

'        Dim blobCounter = New BlobCounter()
'        blobCounter.FilterBlobs = True
'        blobCounter.MinHeight = 5
'        blobCounter.MinWidth = 5
'        blobCounter.ProcessImage(BitmapData)

'        Dim blobs = blobCounter.GetObjectsInformation()
'        Dim shapeChecker = New SimpleShapeChecker()

'        bitmap.UnlockBits(BitmapData)
'        dst0 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(src.Size)
'        dst1 = dst0.Clone
'        dst3 = dst0.Clone

'        Dim fx As Single = bitmap.Width / src.Width
'        Dim fy As Single = bitmap.Height / src.Height
'        For Each blob In blobs
'            Dim r = New cv.Rect(blob.Rectangle.Location.X / fx, blob.Rectangle.Location.Y / fy, blob.Rectangle.Width / fx, blob.Rectangle.Height / fy)
'            dst0.Rectangle(r, cv.Scalar.Yellow, task.lineWidth, task.lineType)

'            Dim edgePoints = blobCounter.GetBlobsEdgePoints(blob)
'            Dim pointlist As New List(Of cv.Point)
'            For Each pt In edgePoints
'                pointlist.Add(New cv.Point2f(pt.X / fx, pt.Y / fy))
'            Next
'            vbDrawContour(dst1, pointlist, cv.Scalar.Yellow, -1)

'            Dim center As Accord.Point
'            Dim radius As Single
'            If shapeChecker.IsCircle(edgePoints, center, radius) Then
'                dst3.Circle(New cv.Point(center.X / fx, center.Y / fy), radius / fx, cv.Scalar.Red, task.lineWidth, task.lineType)
'            Else
'                Dim corners As New List(Of IntPoint)
'                shapeChecker.IsConvexPolygon(edgePoints, corners)

'                Dim subType = shapeChecker.CheckPolygonSubType(corners)
'                Dim cornerPoints As New List(Of cv.Point)
'                For Each pt In corners
'                    cornerPoints.Add(New cv.Point(CInt(pt.X / fx), CInt(pt.Y / fy)))
'                Next
'                If subType = PolygonSubType.Unknown Then
'                    vbDrawContour(dst3, cornerPoints, cv.Scalar.White, -1)
'                Else
'                    vbDrawContour(dst3, cornerPoints, cv.Scalar.Green, -1)
'                End If
'            End If
'        Next
'    End Sub
'End Class







'Public Class ShapeDetect_Example2 : Inherits VB_Algorithm
'    Dim options As New Options_ShapeDetect
'    Dim shape As New ShapeDetect_Basics
'    Public Sub New()
'        labels = {"", "Identified shapes", "Original image", "Labeled shapes"}
'        desc = "Accord Shape Detection example but use ShapeDetect_Basics"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Options.RunVB()

'        Dim bitmap = New Bitmap(task.homeDir + "Data/" + options.fileName)
'        Dim colorfilter As New ColorFiltering
'        colorfilter.Red = New IntRange(0, 64)
'        colorfilter.Green = New IntRange(0, 64)
'        colorfilter.Blue = New IntRange(0, 64)
'        colorfilter.FillOutsideRange = False

'        Dim BitmapData = bitmap.LockBits(ImageLockMode.ReadWrite)
'        colorfilter.ApplyInPlace(BitmapData)
'        bitmap.UnlockBits(BitmapData)

'        dst2 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(src.Size)

'        shape.Run(dst2.Clone)
'        dst3 = shape.dst3
'    End Sub
'End Class