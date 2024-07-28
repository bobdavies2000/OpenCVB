//open System
//open System.Collections.Generic
//open System.Linq
//open VB_Classes
//open OpenCvSharp
//open System.Drawing
//open System.Runtime.InteropServices

namespace FS_Classes
module FS_Classes = 
    let greet = sprintf "Hello"
//type CS_Parent(task: VBtask) =
//    let mutable cPtr = IntPtr.Zero
//    let mutable standalone = false
//    let mutable desc = ""
//    let mutable dst0 = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//    let mutable dst1 = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//    let mutable dst2 = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//    let mutable dst3 = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//    let mutable empty = new Mat()
//    let mutable traceName = ""
//    let mutable labels = Array.create 4 ""
//    let mutable trueData = new List<trueText>()
//    let fmt0 = "0"
//    let fmt1 = "0.0"
//    let fmt2 = "0.00"
//    let fmt3 = "0.000"
//    let msRNG = new Random()
//    let mutable strOut = ""
//    let fmt4 = "0.0000"
//    let controls = new VB_Controls_CSharp()
//    let 0 = 0
//    let 1 = 1
//    let 2 = 2
//    let 3 = 3
//    let black = new Vec3b(0uy, 0uy, 0uy)
//    let mutable callStack = ""

//    do
//        traceName <- this.GetType().Name
//        labels <- [| ""; ""; traceName; "" |]
//        let stackTrace = Environment.StackTrace
//        let lines = stackTrace.Split([|Environment.NewLine|], StringSplitOptions.None)
        
//        for line in lines do
//            let trimmedLine = line.Trim()
//            let offset = trimmedLine.IndexOf("CS_Classes.")
//            if offset > 0 then
//                let partLine = trimmedLine.Substring(offset + 11)
//                if partLine.StartsWith("AlgorithmList.createCSAlgorithm") then
//                    ()
//                else
//                    let split = partLine.Split('\\')
//                    let partLine = partLine.Substring(0, partLine.IndexOf('.'))
//                    if not (partLine.StartsWith("CS_Parent") || partLine.StartsWith("VBtask")) then
//                        callStack <- partLine + "\\" + callStack
        
//        callStack <- callStack.Replace("CSAlgorithmList\\", "")
//        standalone <- controls.buildCallStack(traceName, callStack)

//    [<DllImport("gdi32.dll", EntryPoint = "BitBlt")>]
//    static member BitBlt(hdc: IntPtr, x: int, y: int, cx: int, cy: int, hdcSrc: IntPtr, x1: int, y1: int, rop: uint32) : bool = 
//        failwith "Not Implemented"

//    member this.GetWindowImage(WindowHandle: IntPtr, rect: Rect) =
//        let b = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb)
//        use img = Graphics.FromImage(b)
//        let ImageHDC = img.GetHdc()
//        use window = Graphics.FromHwnd(WindowHandle)
//        let WindowHDC = window.GetHdc()
//        CS_Parent.BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, uint32 CopyPixelOperation.SourceCopy) |> ignore
//        window.ReleaseHdc()
//        img.ReleaseHdc()
//        b

//    member this.vecToScalar(v: Vec3b) =
//        new Scalar(float v.Item0, float v.Item1, float v.Item2)

//    member this.DrawRotatedRect(rotatedRect: RotatedRect, dst: Mat, color: Scalar) =
//        let vertices2f = rotatedRect.Points()
//        let vertices = 
//            vertices2f 
//            |> Array.map (fun p -> new Point(int p.X, int p.Y))
//        Cv2.FillConvexPoly(dst, vertices, color, task.lineType)

//    member this.AddPlotScale(dst: Mat, minVal: float, maxVal: float, lineCount: int) =
//        let spacer = int (float dst.Height / float (lineCount + 1))
//        let mutable spaceVal = int ((maxVal - minVal) / float (lineCount + 1))
//        if lineCount > 1 && spaceVal < 1 then
//            spaceVal <- 1
//        if spaceVal > 10 then
//            spaceVal <- spaceVal + (spaceVal % 10)
        
//        for i in 0 .. lineCount do
//            let p1 = new Point(0, spacer * i)
//            let p2 = new Point(dst.Width, spacer * i)
//            Cv2.Line(dst, p1, p2, Scalar.White, task.cvFontThickness)
//            let nextVal = maxVal - float (spaceVal * i)
//            let nextText = 
//                if maxVal > 1000.0 then 
//                    sprintf "%sk" ((nextVal / 1000.0).ToString(fmt2))
//                else 
//                    nextVal.ToString(fmt2)
//            Cv2.PutText(dst, nextText, p1, HersheyFonts.HersheyPlain, task.cvFontSize, Scalar.White, task.cvFontThickness, task.lineType)


//    let drawLine (dst: Mat) (p1: Point2f) (p2: Point2f) (color: Scalar) (lineWidth: int) =
//        let pt1 = Point(int p1.X, int p1.Y)
//        let pt2 = Point(int p2.X, int p2.Y)
//        dst.Line(pt1, pt2, color, lineWidth)

//    let getMaxDist (rc: rcData) =
//        use mask = rc.mask.Clone()
//        mask.Rectangle(Rect(0, 0, mask.Width, mask.Height), Scalar.Black, 1, LineTypes.Link8)
//        use distance32f = mask.DistanceTransform(DistanceTypes.L1, 0)
//        let mutable mm = Unchecked.defaultof<mmData>
//        distance32f.MinMaxLoc(&mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc)
//        mm.maxLoc.X <- mm.maxLoc.X + rc.rect.X
//        mm.maxLoc.Y <- mm.maxLoc.Y + rc.rect.Y
//        mm.maxLoc

//    let getMinMax (mat: Mat) (mask: Mat option) =
//        let mutable mm = Unchecked.defaultof<mmData>
//        match mask with
//        | None -> mat.MinMaxLoc(&mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc)
//        | Some m -> mat.MinMaxLoc(&mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc, m)
//        mm

//    let intersectTest (p1: Point2f) (p2: Point2f) (p3: Point2f) (p4: Point2f) (rect: Rect) =
//        let x = p3 - p1
//        let d1 = p2 - p1
//        let d2 = p4 - p3
//        let cross = d1.X * d2.Y - d1.Y * d2.X
//        if abs cross < 0.000001f then
//            Point2f()
//        else
//            let t1 = (x.X * d2.Y - x.Y * d2.X) / cross
//            p1 + d1 * t1

//    let prepareDepthInput (index: int) (task: Task) =
//        if task.useGravityPointcloud then
//            task.pcSplit.[index] // already oriented to gravity
//        else
//            // rebuild the pointcloud so it is oriented to gravity.
//            let pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat().Reshape(3, task.pointCloud.Rows)
//            let split = pc.Split()
//            split.[index]
//    let getNormalize32f (input: Mat) =
//        let outMat = input.Normalize(0.0, 255.0, NormTypes.MinMax)
//        if input.Channels() = 1 then
//            outMat.ConvertTo(outMat, MatType.CV_8U)
//            outMat.CvtColor(ColorConversionCodes.GRAY2BGR)
//        else
//            outMat.ConvertTo(outMat, MatType.CV_8UC3)
//            outMat

//    let distance3D (p1: Point3f) (p2: Point3f) =
//        Math.Sqrt((p1.X - p2.X) ** 2.0 + (p1.Y - p2.Y) ** 2.0 + (p1.Z - p2.Z) ** 2.0) |> float32

//    let distance3DVec3b (p1: Vec3b) (p2: Vec3b) =
//        Math.Sqrt(float ((int p1.Item0 - int p2.Item0) ** 2 + (int p1.Item1 - int p2.Item1) ** 2 + (int p1.Item2 - int p2.Item2) ** 2)) |> float32

//    let distance3DPoint3i (p1: Point3i) (p2: Point3i) =
//        Math.Sqrt(float ((p1.X - p2.X) ** 2 + (p1.Y - p2.Y) ** 2 + (p1.Z - p2.Z) ** 2)) |> float32

//    let distance3DScalar (p1: Scalar) (p2: Scalar) =
//        Math.Sqrt((p1.[0] - p2.[0]) ** 2.0 + (p1.[1] - p2.[1]) ** 2.0 + (p1.[2] - p2.[2]) ** 2.0) |> float32

//    let DrawFPoly (dst: Mat) (poly: Point2f list) (color: Scalar) =
//        let minMod = min poly.Length task.polyCount
//        for i in 0 .. minMod - 1 do
//            DrawLine(dst, poly.[i], poly.[(i + 1) % minMod], color, task.lineWidth)

//    let contourBuild (mask: Mat) (approxMode: ContourApproximationModes) =
//        let mutable allContours = [||]
//        let mutable test = [||]
//        Cv2.FindContours(mask, &allContours, &test, RetrievalModes.External, approxMode)

//        if allContours.Length > 0 then
//            allContours
//            |> Array.mapi (fun i contour -> i, contour.Length)
//            |> Array.maxBy snd
//            |> fun (maxIndex, _) -> allContours.[maxIndex] |> Array.toList
//        else
//            []

//    let setPointCloudGrid () =
//        task.gOptions.setGridSize(8)
//        match task.WorkingRes.Width with
//        | 640 -> task.gOptions.setGridSize(16)
//        | 1280 -> task.gOptions.setGridSize(32)
//        | _ -> ()

//    let gMatrixToStr (gMatrix: Mat) =
//        let outStr = "Gravity transform matrix\n"
//        [0 .. gMatrix.Rows - 1]
//        |> List.map (fun i ->
//            [0 .. gMatrix.Cols - 1]
//            |> List.map (fun j -> gMatrix.Get<float32>(j, i).ToString(fmt3))
//            |> String.concat "\t"
//        )
//        |> String.concat "\n"
//        |> fun s -> outStr + s + "\n"

//    let randomCellColor () =
//        Vec3b(byte (msRNG.Next(50, 240)), byte (msRNG.Next(50, 240)), byte (msRNG.Next(50, 240)))

//    let validContourPoint (rc: rcData) (pt: Point) (offset: int) =
//        if pt.X < rc.rect.Width && pt.Y < rc.rect.Height then
//            pt
//        else
//            rc.contour
//            |> List.skip (offset + 1)
//            |> List.append rc.contour
//            |> List.tryFind (fun p -> p.X < rc.rect.Width && p.Y < rc.rect.Height)
//            |> Option.defaultValue (Point())

//    let build3PointEquation (rc: rcData) =
//        if rc.contour.Length < 3 then
//            Vec4f()
//        else
//            let offset = rc.contour.Length / 3
//            let p1 = validContourPoint rc rc.contour.[offset * 0] (offset * 0)
//            let p2 = validContourPoint rc rc.contour.[offset * 1] (offset * 1)
//            let p3 = validContourPoint rc rc.contour.[offset * 2] (offset * 2)

//            let v1 = task.pointCloud.Get<Point3f>(rc.rect.Y + p1.Y, rc.rect.X + p1.X)
//            let v2 = task.pointCloud.Get<Point3f>(rc.rect.Y + p2.Y, rc.rect.X + p2.X)
//            let v3 = task.pointCloud.Get<Point3f>(rc.rect.Y + p3.Y, rc.rect.X + p3.X)

//            let cross = crossProduct (v1 - v2) (v2 - v3)
//            let k = -(v1.X * cross.X + v1.Y * cross.Y + v1.Z * cross.Z)
//            Vec4f(cross.X, cross.Y, cross.Z, k)
//    let fitDepthPlane (fitDepth: Point3f list) =
//        let wDepth = Mat(fitDepth.Length, 1, MatType.CV_32FC3, fitDepth |> List.toArray)
//        let columnSum = wDepth.Sum()
//        let count = float fitDepth.Length
//        let mutable plane = Vec4f()
//        let mutable centroid = Scalar(0.0f)
    
//        if count > 0.0 then
//            centroid <- Scalar(float32 (columnSum.[0] / count), float32 (columnSum.[1] / count), float32 (columnSum.[2] / count))
//            wDepth.Subtract(centroid) |> ignore
        
//            let mutable xx, xy, xz, yy, yz, zz = 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
//            for i = 0 to wDepth.Rows - 1 do
//                let tmp = wDepth.Get<Point3f>(i, 0)
//                xx <- xx + float (tmp.X * tmp.X)
//                xy <- xy + float (tmp.X * tmp.Y)
//                xz <- xz + float (tmp.X * tmp.Z)
//                yy <- yy + float (tmp.Y * tmp.Y)
//                yz <- yz + float (tmp.Y * tmp.Z)
//                zz <- zz + float (tmp.Z * tmp.Z)
        
//            let det_x = yy * zz - yz * yz
//            let det_y = xx * zz - xz * xz
//            let det_z = xx * yy - xy * xy
        
//            let det_max = max (max det_x det_y) det_z
        
//            if det_max = det_x then
//                plane.[0] <- 1.0f
//                plane.[1] <- float32 ((xz * yz - xy * zz) / det_x)
//                plane.[2] <- float32 ((xy * yz - xz * yy) / det_x)
//            elif det_max = det_y then
//                plane.[0] <- float32 ((yz * xz - xy * zz) / det_y)
//                plane.[1] <- 1.0f
//                plane.[2] <- float32 ((xy * xz - yz * xx) / det_y)
//            else
//                plane.[0] <- float32 ((yz * xy - xz * yy) / det_z)
//                plane.[1] <- float32 ((xz * xy - yz * xx) / det_z)
//                plane.[2] <- 1.0f
    
//        let magnitude = sqrt (plane.[0] * plane.[0] + plane.[1] * plane.[1] + plane.[2] * plane.[2])
//        let normal = Scalar(float32 (plane.[0] / magnitude), float32 (plane.[1] / magnitude), float32 (plane.[2] / magnitude))
//        Vec4f(float32 normal.[0], float32 normal.[1], float32 normal.[2], 
//              float32 -(normal.[0] * centroid.[0] + normal.[1] * centroid.[1] + normal.[2] * centroid.[2]))

//    let crossProduct (v1: Point3f) (v2: Point3f) =
//        let product = Point3f()
//        product.X <- v1.Y * v2.Z - v1.Z * v2.Y
//        product.Y <- v1.Z * v2.X - v1.X * v2.Z
//        product.Z <- v1.X * v2.Y - v1.Y * v2.X
    
//        if Single.IsNaN(product.X) || Single.IsNaN(product.Y) || Single.IsNaN(product.Z) then
//            Point3f(0.0f, 0.0f, 0.0f)
//        else
//            let magnitude = sqrt (product.X * product.X + product.Y * product.Y + product.Z * product.Z)
//            if magnitude = 0.0 then
//                Point3f(0.0f, 0.0f, 0.0f)
//            else
//                Point3f(float32 (product.X / magnitude), float32 (product.Y / magnitude), float32 (product.Z / magnitude))

//    let dotProduct3D (v1: Point3f) (v2: Point3f) =
//        abs (v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z)

//    let getWorldCoordinates (p: Point3f) =
//        let x = (p.X - task.calibData.ppx) / task.calibData.fx
//        let y = (p.Y - task.calibData.ppy) / task.calibData.fy
//        Point3f(x * p.Z, y * p.Z, p.Z)

//    let getWorldCoordinatesD6 (p: Point3f) =
//        let x = (p.X - task.calibData.ppx) / task.calibData.fx
//        let y = (p.Y - task.calibData.ppy) / task.calibData.fy
//        Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0.0f)
//    let contourBuild (mask: Mat) (approxMode: ContourApproximationModes) =
//    let mutable allContours = [||]
//    let mutable _ = Hierarchy()
//    Cv2.FindContours(mask, &allContours, &_, RetrievalModes.External, approxMode)

//    let maxContour =
//        allContours
//        |> Array.mapi (fun i contour -> i, contour.Length)
//        |> Array.maxBy snd
//        |> fst

//    if allContours.Length > 0 then
//        allContours.[maxContour] |> Array.toList
//    else
//        []

//    let showHsvHist (hist: Mat) =
//        let img = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//        let binCount = hist.Height
//        let binWidth = img.Width / hist.Height
//        let mm = getMinMax hist
//        img.SetTo(Scalar.All(0))

//        if mm.maxVal > 0.0 then
//            for i in 0 .. binCount - 2 do
//                let h = float img.Height * float32 (hist.At<float32>(i, 0)) / mm.maxVal
//                let h = if h = 0.0 then 5.0 else h // show the color range in the plot
//                Cv2.Rectangle(img, 
//                              Rect(i * binWidth, img.Height - int h, binWidth, int h),
//                              Scalar(180.0 * float i / float binCount, 255.0, 255.0), 
//                              -1)
//        img

//    let validateRect (r: Rect) (ratio: int) =
//        let mutable rect = r
//        rect.Width <- max 1 rect.Width
//        rect.Height <- max 1 rect.Height
//        rect.X <- max 0 rect.X
//        rect.Y <- max 0 rect.Y
//        rect.X <- min (task.WorkingRes.Width * ratio - 1) rect.X
//        rect.Y <- min (task.WorkingRes.Height * ratio - 1) rect.Y
//        rect.Width <- min (task.WorkingRes.Width * ratio - rect.X) rect.Width
//        rect.Height <- min (task.WorkingRes.Height * ratio - rect.Y) rect.Height
//        rect.Width <- max 1 rect.Width
//        rect.Height <- max 1 rect.Height
//        rect.X <- if rect.X = task.WorkingRes.Width * ratio then rect.X - 1 else rect.X
//        rect.Y <- if rect.Y = task.WorkingRes.Height * ratio then rect.Y - 1 else rect.Y
//        rect

//    let rebuildCells (sortedCells: SortedList<int, rcData>) =
//        task.redCells.Clear()
//        task.redCells.Add(new rcData())
//        for rc in sortedCells.Values do
//            rc.index <- task.redCells.Count
//            task.redCells.Add(rc)
//            if rc.index >= 255 then
//                break
//        displayCells()

//    let displayCells() =
//        let dst = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0))
//        task.cellMap.SetTo(Scalar.All(0))
//        for rc in task.redCells do
//            let natural = task.redOptions.useNaturalColor
//            dst.[rc.rect].SetTo(if natural then rc.naturalColor else rc.color, rc.mask)
//            task.cellMap.[rc.rect].SetTo(Scalar(float rc.index), rc.mask)
//        dst

//    let standaloneTest() =
//        standalone || ShowIntermediate()

//    let ShowIntermediate() =
//        false

//    let initRandomRect (margin: int) =
//        Rect(
//            msRNG.Next(margin, dst2.Width - 2 * margin),
//            msRNG.Next(margin, dst2.Height - 2 * margin),
//            msRNG.Next(margin, dst2.Width - 2 * margin),
//            msRNG.Next(margin, dst2.Height - 2 * margin)
//        )

//    let updateAdvice (advice: string) =
//        if task.advice.StartsWith("No advice for ") then
//            task.advice <- String.Empty

//        let split = advice.Split(':')
//        if not (task.advice.Contains(split.[0] + ":")) then
//            task.advice <- task.advice + advice + Environment.NewLine + Environment.NewLine

//    let drawContour (dst: Mat) (contour: Point list) (color: Scalar) (lineWidth: int) =
//        let lineWidth = if lineWidth = -10 then task.lineWidth else lineWidth
//        if contour.Length >= 3 then
//            Cv2.DrawContours(dst, [contour], -1, color, lineWidth, task.lineType)

//    let drawCircle (dst: Mat) (p1: Point2f) (radius: int) (color: Scalar) (lineWidth: int) =
//        let pt = Point(int p1.X, int p1.Y)
//        dst.Circle(pt, radius, color, lineWidth, task.lineType)

//    let drawRotatedOutline (rotatedRect: RotatedRect) (dst2: Mat) (color: Scalar) =
//        let pts = rotatedRect.Points()
//        let mutable lastPt = Point(int pts.[0].X, int pts.[0].Y)
//        for i in 1 .. pts.Length do
//            let index = i % pts.Length
//            let pt = Point(int pts.[index].X, int pts.[index].Y)
//            Cv2.Line(dst2, pt, lastPt, color)
//            lastPt <- pt

//    type TrueText = { Text: string; Point: Point; PicTag: int }

//    let quickRandomPoints (howMany: int) (task: 'a) =
//        let w = task.WorkingRes.Width
//        let h = task.WorkingRes.Height
//        let rng = Random()
//        [ for _ in 1 .. howMany -> Point2f(float32 (rng.Next(0, w)), float32 (rng.Next(0, h))) ]

//    let setTrueText (trueData: ResizeArray<TrueText>) =
//        function
//        | (text: string, pt: Point, picTag: int) ->
//            trueData.Add({ Text = text; Point = pt; PicTag = picTag })
//        | (text: string, pt: Point2f, picTag: int) ->
//            trueData.Add({ Text = text; Point = Point(int pt.X, int pt.Y); PicTag = picTag })
//        | (text: string) ->
//            trueData.Add({ Text = text; Point = Point(0, 0); PicTag = 2 })
//        | (text: string, picTag: int) ->
//            trueData.Add({ Text = text; Point = Point(0, 0); PicTag = picTag })

//    let setSlider (controls: 'a) (opt: string) (val: int) =
//        controls.CS_SetSlider(opt, val)

//    let findSlider (controls: 'a) (opt: string) : TrackBar =
//        controls.CS_GetSlider(opt)

//    let findCheckBox (controls: 'a) (opt: string) : CheckBox =
//        controls.CS_FindCheckBox(opt)

//    let findRadio (controls: 'a) (opt: string) : RadioButton =
//        controls.CS_FindRadio(opt)

//    let runAndMeasure (controls: 'a) (src: Mat) (csCode: obj) =
//        controls.RunFromVB(src, csCode)

//    let showPalette (task: 'a) (input: Mat) =
//        let mutable inputMat = input
//        if input.Type() = MatType.CV_32SC1 then
//            input.ConvertTo(inputMat, MatType.CV_8U)
//        task.palette.RunVB(inputMat)
//        task.palette.dst2.Clone()

//    let drawContour (dst: Mat) (contour: Point list) (color: Scalar) (lineWidth: int option) (task: 'a) =
//        if contour.Length >= 3 then
//            let width = defaultArg lineWidth task.lineWidth
//            Cv2.DrawContours(dst, [contour], -1, color, width, task.lineType)

//    let detectFace (src: Mat) (cascade: CascadeClassifier) (task: 'a) =
//        use gray = src.CvtColor(ColorConversionCodes.BGR2GRAY)
//        let faces = cascade.DetectMultiScale(gray, 1.08, 3, HaarDetectionTypes.ScaleImage, Size(30, 30))
//        for face in faces do
//            Cv2.Rectangle(src, face, Scalar.Red, task.lineWidth, task.lineType)