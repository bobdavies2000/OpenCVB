using DlibDotNet;
using cv = OpenCvSharp;
using System.Runtime.InteropServices;
using System.Drawing;

namespace CS_Classes
{
    public class Dlib_GaussianBlur
    {
        public Array2D<byte> blurredGray = new Array2D<byte>();
        public Array2D<BgrPixel> blurredRGB = new Array2D<BgrPixel>();
        public void RunCS(cv.Mat src)
        {
            var array = new byte[src.Width * src.Height * src.ElemSize()];
            Marshal.Copy(src.Data, array, 0, array.Length);
            if (src.Channels() == 1)
            {
                using (var image = Dlib.LoadImageData<byte>(array, (uint)src.Height, (uint)src.Width, (uint)(src.Width * src.ElemSize())))
                {
                    Dlib.GaussianBlur(image, blurredGray);
                }
            }
            else
            {
                using (var image = Dlib.LoadImageData<BgrPixel>(array, (uint)src.Height, (uint)src.Width, (uint)(src.Width * src.ElemSize())))
                {
                    Dlib.GaussianBlur(image, blurredRGB);
                }
            }
        }
    }

    // http://dlib.net/image_ex.cpp.html
    public class Dlib_EdgesSobel
    {
        // public Array2D<byte> heatmap = new Array2D<byte>();
        public Array2D<byte> edgeImage = new Array2D<byte>();
        public void RunCS(cv.Mat src)
        {
            var array = new byte[src.Width * src.Height * src.ElemSize()];
            Marshal.Copy(src.Data, array, 0, array.Length);
            using (var image = Dlib.LoadImageData<byte>(array, (uint)src.Height, (uint)src.Width, (uint)(src.Width * src.ElemSize())))
            using (var blurredImg = new Array2D<byte>())
            using (var horzGradient = new Array2D<short>())
            using (var vertGradient = new Array2D<short>())
            {
                Dlib.GaussianBlur(image, blurredImg); // there appears to be no provision for a kernel size with the DlibDotNet interface...
                // Now find the horizontal and vertical gradient images.
                Dlib.SobelEdgeDetector(blurredImg, horzGradient, vertGradient);

                // now we do the non-maximum edge suppression step so that our edges are nice and thin
                Dlib.SuppressNonMaximumEdges(horzGradient, vertGradient, edgeImage);
            }
        }
    }

    // https://github.com/KingCobrass/face.image.extractor/blob/master/face.image.extractor/face.image.extractor.consoleapp/FaceExtractor.cs
    public class Dlib_FaceDetectHOG
    {
        FrontalFaceDetector detector;
        public DlibDotNet.Rectangle[] rects;

        // the C# spec says that one class cannot call another's constructor.  Thank goodness this is not the case in VB.Net.
        // https://stackoverflow.com/questions/19162656/why-is-this-c-sharp-constructor-not-working-as-expected/19162779
        public void initialize() 
        {
            detector = Dlib.GetFrontalFaceDetector();
        }
        public void RunCS(cv.Mat src)
        {
            var array = new byte[src.Width * src.Height * src.ElemSize()];
            Marshal.Copy(src.Data, array, 0, array.Length);
            using (var image = Dlib.LoadImageData<byte>(array, (uint)src.Height, (uint)src.Width, (uint)(src.Width * src.ElemSize())))
            {
                Dlib.PyramidUp(image);
                rects = detector.Operator(image);
            }
        }
    }
}
