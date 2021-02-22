using DlibDotNet;
using cv = OpenCvSharp;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CS_Classes
{
    public class Dlib_GaussianBlur
    {
        public Array2D<byte> blurredImg = new Array2D<byte>();
        public void New() { }
        public void Run(cv.Mat src)
        {
            var array = new byte[src.Width * src.Height * src.ElemSize()];
            Marshal.Copy(src.Data, array, 0, array.Length);
            using (var image = Dlib.LoadImageData<byte>(array, (uint)src.Height, (uint)src.Width, (uint)(src.Width * src.ElemSize())))
            {
                Dlib.GaussianBlur(image, blurredImg); // there appears to be no provision for a kernel size with the DlibDotNet interface...
            }
        }
    }

    public class Dlib_EdgesSobel
    {
        // public Array2D<byte> heatmap = new Array2D<byte>();
        public Array2D<byte> edgeImage = new Array2D<byte>();
        public void New() { }
        public void Run(cv.Mat src)
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
                var heatmap = Dlib.Heatmap(edgeImage);
                //using (var winHot = new ImageWindow(heatmap))
                //using (var jet = Dlib.Jet(edgeImage))
                //using (var winJet = new ImageWindow(jet))
                //{
                //    winHot.WaitUntilClosed();
                //    winJet.WaitUntilClosed();
                //}

            }
        }
    }
}
