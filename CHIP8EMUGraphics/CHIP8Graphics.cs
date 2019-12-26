using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CHIP8EMUGraphics
{
    class CHIP8Graphics
    {
        //private PixelFormat pf = PixelFormats.BlackWhite;
        private const int width = 64;  // just for simplicity's sake
        private const int height = 32;
        //private int rawStride;
        //private byte[] rawImage;
        //public BitmapSource bitmap;
        public WriteableBitmap Screen { get; }

        public CHIP8Graphics()
        {
            Screen = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);//new WriteableBitmap(width, height, 96, 96, PixelFormats.BlackWhite, null);  // a writeable bitmap
            //rawStride = (width * height);  // since we're only using 1 bit per pixel
            ////rawStride = (width * pf.BitsPerPixel + 7) / 8;  // stride is weird, hope this works!
            //rawImage = new byte[rawStride * height];
            //bitmap = BitmapSource.Create(width, height, 96, 96, pf, null, rawImage, rawStride);
        }

        public void ClearScreen()  // clear the screen, setting everything to black
        {

            byte[,] b = new byte[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    b[i, j] = 0;
                }
            }
            RenderPixelByteArray(Get4ByteRep(b));
        }

        public void UpdateGraphics(byte[,] graphicsArray)
        {
            byte[] graphics1D = new byte[height * width * PixelFormats.BlackWhite.BitsPerPixel];  // 1 byte per pixel
            int index = 0;
            for (int row = 0; row < height; row++)  // map everything down to a flat 1D array
            {
                for (int col = 0; col < width; col++)
                {
                    graphics1D[index++] = graphicsArray[row, col];
                }
            }
            Int32Rect rect = new Int32Rect((int)Screen.Width, (int)Screen.Height, (int)Screen.Width, (int)Screen.Height);
            Screen.WritePixels(rect, graphics1D, width, 0);  // oh good lord
        }

        public void GraphicsTestDriver()
        {
            byte[] b = GenerateRand1DArray();
            byte[] ee = Get4ByteRep(b);
            RenderPixelByteArray(ee);
        }


        public void RenderByteArray(byte[] arr)
        {
            byte[] ee = Get4ByteRep(arr);
            RenderPixelByteArray(ee);
        }

        public byte[,] GenerateRandomBWArray()
        {
            byte[,] b = new byte[width, height];
            Random rand = new Random();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if ((byte)rand.Next(0, 2) == 1)
                    {
                        b[i, j] = 255;
                    }
                    else
                    {
                        b[i, j] = 0;
                    }
                }
            }
            return b;
        }

        public byte[] GenerateRand1DArray()
        {
            Random ra = new Random();
            byte[] b = new byte[4096];
            for(int i = 0; i < b.Length; i++)
            {
                if ((byte)ra.Next(0, 2) == 1)
                {
                    b[i] = 255;
                }
                else
                {
                    b[i] = 0;
                }
            }
            return b;
        }

        public byte[] Get4ByteRep(byte[] arr1d)
        {
            int bytesPerPixel = (Screen.Format.BitsPerPixel + 7) / 8; // general formula
            //int stride = bytesPerPixel * width; // general formula valid for all PixelFormats
            int index = 0;
            byte[] pixelByteArrayOfColors = new byte[4*arr1d.GetLength(0)]; // General calculation of buffer size
            for (int i = 0; i < arr1d.GetLength(0); i++)
            {
               // Console.WriteLine(arr1d[i]);
                byte toRender;
                if(arr1d[i] == 1)
                {
                    toRender = 255;
                } else if(arr1d[i] == 0)
                {
                    toRender = 0;
                } else
                {
                    toRender = arr1d[i];
                }
                pixelByteArrayOfColors[index] = 0;        // blue nope
                pixelByteArrayOfColors[index + 1] = 0;  // green nope
                pixelByteArrayOfColors[index + 2] = 0;    // red nope
                pixelByteArrayOfColors[index + 3] = toRender;
                index += bytesPerPixel;
            }
            return pixelByteArrayOfColors;
        }

        public byte[] Get4ByteRep(byte[,] arr2d)
        {
            int bytesPerPixel = (Screen.Format.BitsPerPixel + 7) / 8; // general formula
            int stride = bytesPerPixel * width; // general formula valid for all PixelFormats
            int index = 0;
            byte[] pixelByteArrayOfColors = new byte[stride * height]; // calculate buffer size
            for (int i = 0; i < arr2d.GetLength(0); i++)
            {
                for (int j = 0; j < arr2d.GetLength(1); j++)
                {

                    pixelByteArrayOfColors[index] = 0;        // blue nope
                    pixelByteArrayOfColors[index + 1] = 0;  // green nope
                    pixelByteArrayOfColors[index + 2] = 0;    // red nope
                    pixelByteArrayOfColors[index + 3] = arr2d[i, j];   // alpha maybe
                    index += bytesPerPixel;
                }
            }
            return pixelByteArrayOfColors;
        }

        public void RenderPixelByteArray(byte[] pixelByteArrayOfColors)
        {
            //Console.WriteLine("CALL TO RENDERER");
            //Console.WriteLine(pixelByteArrayOfColors.Max());
            int bytesPerPixel = (Screen.Format.BitsPerPixel + 7) / 8; // general formula
            int stride = bytesPerPixel * width; // general formula valid for all PixelFormats
            Screen.WritePixels(new Int32Rect(0, 0, width, height), pixelByteArrayOfColors, stride, 0);
        }
    }
}
