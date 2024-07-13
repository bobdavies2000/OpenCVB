

namespace FS_Classes
    
//type Class1() = 
//    member this.X = "F#"

//type Options_AddWeighted() =
//    member val AddWeighted = 0.0f with get, set
//    member this.RunVB() = ()

//type CS_AddWeighted_Basics(task: VBtask) =
//    inherit FS_Parent(task)

//    let mutable weight = 0.0f
//    let mutable src2 = Unchecked.defaultof<Mat>
//    let options = Options_AddWeighted()

//    do
//        base.UpdateAdvice(sprintf "%s: use the local option slider 'Add Weighted %%'" base.TraceName)
//        base.Desc <- "Add 2 images with specified weights."

//    member private this.GetNormalize32f(src: Mat) =
//        // Implement the GetNormalize32f function here
//        src

//    member private this.StandaloneTest() =
//        // Implement the standaloneTest function here
//        false

//    member this.RunCS(src: Mat) =
//        options.RunVB()
//        weight <- options.AddWeighted

//        let mutable srcPlus = src2
//        // algorithm user normally provides src2! 
//        if this.StandaloneTest() || isNull src2 then
//            srcPlus <- task.DepthRGB

//        if srcPlus.Type() <> src.Type() then
//            if src.Type() <> MatType.CV_8UC3 || srcPlus.Type() <> MatType.CV_8UC3 then
//                let mutable srcMut = src
//                let mutable srcPlusMut = srcPlus
//                if src.Type() = MatType.CV_32FC1 then
//                    srcMut <- this.GetNormalize32f(src)
//                if srcPlus.Type() = MatType.CV_32FC1 then
//                    srcPlusMut <- this.GetNormalize32f(srcPlus)
//                if src.Type() <> MatType.CV_8UC3 then
//                    srcMut <- srcMut.CvtColor(ColorConversionCodes.GRAY2BGR)
//                if srcPlus.Type() <> MatType.CV_8UC3 then
//                    srcPlusMut <- srcPlusMut.CvtColor(ColorConversionCodes.GRAY2BGR)
//                srcPlus <- srcPlusMut
//                src <- srcMut

//        Cv2.AddWeighted(src, float weight, srcPlus, 1.0 - float weight, 0.0, base.Dst2)
//        base.Labels.[2] <- sprintf "Depth %%: %.0f BGR %%: %d" (100.0 - float weight * 100.0) (int (weight * 100.0f))

        
