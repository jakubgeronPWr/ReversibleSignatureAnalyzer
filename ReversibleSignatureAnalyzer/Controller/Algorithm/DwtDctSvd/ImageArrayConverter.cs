using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class ImageArrayConverter
    {
        public int[,] BitmapToArray2D(Bitmap image)
        {
            int[,] array2D = new int[image.Width, image.Height];

            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppRgb);

            unsafe
            {
                byte* address = (byte*)bitmapData.Scan0;

                int paddingOffset = bitmapData.Stride - (image.Width * 4);//4 bytes per pixel

                for (int i = 0; i < image.Width; i++)
                {
                    for (int j = 0; j < image.Height; j++)
                    {
                        byte[] temp = new byte[4];
                        temp[0] = address[0];
                        temp[1] = address[1];
                        temp[2] = address[2];
                        temp[3] = address[3];
                        array2D[i, j] = BitConverter.ToInt32(temp, 0);
                        ////array2D[i, j] = (int)address[0];

                        //4 bytes per pixel
                        address += 4;
                    }

                    address += paddingOffset;
                }
            }
            image.UnlockBits(bitmapData);

            return array2D;
        }

        public Bitmap Array2DToBitmap(int[,] integers)
        {
            int width = integers.GetLength(0);
            int height = integers.GetLength(1);

            int stride = width * 4;//int == 4-bytes

            Bitmap bitmap = null;

            unsafe
            {
                fixed (int* intPtr = &integers[0, 0])
                {
                    bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, new IntPtr(intPtr));
                    Debug.WriteLine($"image pixel format: ${bitmap.PixelFormat}");
                }
            }

            return bitmap;
        }

        private const int normOffset = 128;

        public Bitmap MatricesToBitmap(double[][,] matrices, bool offset = true)
        {
            double[,] first = matrices[2];
            int width = first.GetLength(0);
            int height = first.GetLength(1);

            Bitmap bitmap = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //double alpha = matrices[0][x, y];
                    double r = matrices[0][x, y];
                    double g = matrices[1][x, y];
                    double b = matrices[2][x, y];

                    byte R = (byte)(normOut(r, offset));
                    byte G = (byte)(normOut(g, offset));
                    byte B = (byte)(normOut(b, offset));
                    bitmap.SetPixel(x, y, Color.FromArgb(R, G, B));
                }
            }
            return bitmap;
        }

        private double normOut(double a, bool offset)
        {
            double o = offset ? normOffset : 0d;
            return Math.Min(Math.Max(a + o, 0), 255);
        }

        public double[][,] BitmapToMatrices(Bitmap b)
        {
            var width = b.Width;
            var height = b.Height;

            double[][,] matrices = new double[3][,];

            for (int i = 0; i < 3; i++)
            {
                matrices[i] = new double[width, height];
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //matrices[0][x, y] = b.GetPixel(x, y).A - normOffset;
                    matrices[0][x, y] = b.GetPixel(x, y).R - normOffset;
                    matrices[1][x, y] = b.GetPixel(x, y).G - normOffset;
                    matrices[2][x, y] = b.GetPixel(x, y).B - normOffset;
                }
            }
            return matrices;
        }

        public decimal[][,] BitmapToPrecisionMatrices(Bitmap b)
        {
            var width = b.Width;
            var height = b.Height;

            decimal[][,] matrices = new decimal[3][,];

            for (int i = 0; i < 3; i++)
            {
                matrices[i] = new decimal[width, height];
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //matrices[0][x, y] = b.GetPixel(x, y).A - normOffset;
                    matrices[0][x, y] = b.GetPixel(x, y).R - normOffset;
                    matrices[1][x, y] = b.GetPixel(x, y).G - normOffset;
                    matrices[2][x, y] = b.GetPixel(x, y).B - normOffset;
                }
            }
            return matrices;
        }

        public Bitmap MatricesPrecisionToBitmap(decimal[][,] matrices, bool offset = true)
        {
            decimal[,] first = matrices[2];
            int width = first.GetLength(0);
            int height = first.GetLength(1);

            Bitmap bitmap = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //double alpha = matrices[0][x, y];
                    double r = Convert.ToDouble(matrices[0][x, y]);
                    double g = Convert.ToDouble(matrices[1][x, y]);
                    double b = Convert.ToDouble(matrices[2][x, y]);

                    byte R = (byte)(normOut(r, offset));
                    byte G = (byte)(normOut(g, offset));
                    byte B = (byte)(normOut(b, offset));
                    bitmap.SetPixel(x, y, Color.FromArgb(R, G, B));
                }
            }
            return bitmap;
        }



        //__________________________________________________________________________________________
        //public double[,] BitmapToArray2D(Bitmap image)
        //{
        //    double[,] array2D = null;

        //    Debug.WriteLine($"image pixel format: ${image.PixelFormat}");

        //    if (image.PixelFormat == PixelFormat.Format8bppIndexed)
        //    {
        //        array2D = new double[image.Width, image.Height];
        //        BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
        //        int paddingOffset = bitmapData.Stride - (bitmapData.Width * 1); //1 == 1-byte per pixel

        //        unsafe
        //        {
        //            byte* memoryAddress = (byte*)bitmapData.Scan0;
        //            //ColorARGB* startingPosition = (ColorARGB*) bitmapData.Scan0;
        //            for (int i = 0; i < bitmapData.Width; i++)
        //            {
        //                for (int j = 0; j < bitmapData.Height; j++)
        //                {
        //                    byte tempByte = memoryAddress[0];
        //                    array2D[i, j] = (double)tempByte;
        //                    double tempArrayElement = array2D[i, j];
        //                    Debug.WriteLine($"tempArrayElement: ${tempArrayElement}");
        //                    //1-byte per pixel
        //                    memoryAddress += 1;
        //                }

        //                memoryAddress += paddingOffset;
        //            }
        //        }
        //        image.UnlockBits(bitmapData);
        //    }
        //    else
        //    {
        //        throw new Exception("8 bit/pixel indexed image required.");
        //    }

        //    return array2D;
        //}

        //public static Bitmap Array2DToBitmap(double[,] image)
        //{
        //    Bitmap output = new Bitmap(image.GetLength(0), image.GetLength(1));
        //    BitmapData bitmapData = output.LockBits(new Rectangle(0, 0, image.GetLength(0), image.GetLength(1)), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        //    int paddingOffset = bitmapData.Stride - (bitmapData.Width * 1); //1 == 1-byte per pixel
        //    unsafe
        //    {
        //        byte* memoryAddress = (byte*)bitmapData.Scan0;
        //        for (int i = 0; i < bitmapData.Height; i++)
        //        {
        //            for (int j = 0; j < bitmapData.Width; j++)
        //            {
        //                double tempInt = image[j, i];
        //                memoryAddress[0] = (byte)tempInt;
        //                byte tempByte = memoryAddress[0];
        //                //1-byte per pixel
        //                memoryAddress += 1;
        //            }

        //            memoryAddress += paddingOffset;
        //        }
        //    }

        //    output.UnlockBits(bitmapData);
        //    return output;
        //}



    }
}
