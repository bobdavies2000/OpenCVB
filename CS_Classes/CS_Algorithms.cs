using OpenCvSharp;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using VB_Classes;

namespace CS_Classes
{ 
    //public class AddWeighted_Basics_CS : CS_Parent
    //{
    //    public Mat dst2 = null;
    //    public bool standalone = true;
    //    public Mat srcPlus;
    //    public Mat src2;
    //    public string desc;
    //    public VB_Classes.VBtask task;
    //    public AddWeighted_Basics_CS(VB_Classes.VBtask _task)
    //    {
    //        task = _task;
    //        var options = new VB_Classes.OptionsContainer();
    //        options.Show();
    //        this.src2 = null;
    //        desc = "Add 2 images with specified weights.";
    //    }
    //    //   public void TestMsg(VB_Classes.VBtask.algParms parms)
    //    //   {
    //    //       var options = new VB_Classes.OptionsContainer();
    //    //       var task = new VB_Classes.VBtask(parms);
    //    //       string messageBoxText = task.cameraName; // Your message
    //    //       string caption = "My Message Box"; // Title for the message box
    //    //       MessageBoxButtons buttons = MessageBoxButtons.OK; // You can choose other button options too

    //    //       // Show the message box
    //    //       MessageBox.Show(messageBoxText, caption, buttons);
    //    //       options.ShowDialog();
    //    //}

    //    public void RunCS()
    //    {
    //        Console.WriteLine(desc);
    //        Console.WriteLine(task.cameraName);
    //        OpenCvSharp.Cv2.ImShow("task.color", task.color);
    //        OpenCvSharp.Cv2.WaitKey(1);
    //        //srcPlus = this.src2;
    //        //// algorithm user normally provides src2! 
    //        //if (standalone || this.src2 == null)
    //        //{
    //        //    //srcPlus = task.depthRGB;
    //        //}
    //        //if (srcPlus.Type() != src.Type())
    //        //{
    //        //    //if (src.type() !== cv.CV_8UC3 || srcPlus.type() !== cv.CV_8UC3)
    //        //    //{
    //        //    //    if (src.type() === cv.CV_32FC1) src = this.vbNormalize32f(src);
    //        //    //    if (srcPlus.type() === cv.CV_32FC1) srcPlus = this.vbNormalize32f(srcPlus);
    //        //    //    if (src.type() !== cv.CV_8UC3) src = src.cvtColor(cv.COLOR_GRAY2BGR);
    //        //    //    if (srcPlus.type() !== cv.CV_8UC3) srcPlus = srcPlus.cvtColor(cv.COLOR_GRAY2BGR);
    //        //    //}
    //        //}
    //        //Cv2.AddWeighted(src, 0.5, src2, 0.5, 0, dst2);
    //    }
    //}




    public class AddWeighted_Basics_CS : CS_Parent
    {
        public Mat src2;
        public Options_AddWeighted options = new Options_AddWeighted();

        public AddWeighted_Basics_CS(VBtask task) : base(task) 
        {
            //AddAdvice(traceName + ": use the local option slider 'Add Weighted %'");
            desc = "Add 2 images with specified weights.";
        }

        public void RunVB(Mat src)
        {
            options.RunVB();

            //Mat srcPlus = src2;
            //// algorithm user normally provides src2! 
            //if (StandaloneTest() || src2 == null) srcPlus = task.depthRGB;
            //if (srcPlus.Type() != src.Type())
            //{
            //    if (src.Type() != MatType.CV_8UC3 || srcPlus.Type() != MatType.CV_8UC3)
            //    {
            //        //if (src.Type() == MatType.CV_32FC1) src = vbNormalize32f(src);
            //        //if (srcPlus.Type() == MatType.CV_32FC1) srcPlus = vbNormalize32f(srcPlus);
            //        if (src.Type() != MatType.CV_8UC3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
            //        if (srcPlus.Type() != MatType.CV_8UC3) srcPlus = srcPlus.CvtColor(ColorConversionCodes.GRAY2BGR);
            //    }
            //}
            //Cv2.AddWeighted(src, options.addWeighted, srcPlus, 1.0 - options.addWeighted, 0, dst2);
            //abels[2] = $"Depth %: {100 - options.addWeighted * 100} BGR %: {(int)(options.addWeighted * 100)}";
        }
    }
}



