using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting
{
    class HistogramShiftingAlgorithm : IReversibleWatermarkingAlgorithm
    {
        public int[] getHistogram(Bitmap inputImage)
        {
            int[] histogram = new int[256];
            for (int x = 0; x < inputImage.Width; x++)
            {
                for (int y = 0; y < inputImage.Height; y++)
                {
                    histogram[inputImage.GetPixel(x, y).R] += 1; // Temporary pixel channel reading
                }
            }
            return histogram;
        }

        public (int, int) findSignificantPoints(int [] histogram)
        {
            int a = Array.IndexOf(histogram, histogram.Max());
            int b = 0;
            int minVal = int.MaxValue;
            for (int x = 255; x > a; x--)
            {
                if (histogram[x] < minVal)
                {
                    minVal = histogram[x];
                    b = x;
                }
            }

            return (a, b);
        }

        public (int, int)[] recodeMinPixels(Bitmap image, int b)
        {
            // If b is 0 already ignore this step
            if (b == 0) return null;
            // else
            List<(int, int)> positions = new List<(int, int)>();
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image.GetPixel(x, y).R == b)
                    {
                        Color curPix = image.GetPixel(x, y);
                        positions.Add((x, y));
                        image.SetPixel(x, y, Color.FromArgb(curPix.A, 0, curPix.G, curPix.B)); // Temporary, set the value of pixel to 0
                    }
                }
            }
            return positions.ToArray();
        }

        public Bitmap shiftImageAndEncode(Bitmap image, int a, int b, System.Collections.BitArray bitPayload)
        {
            int payloadPos = 0;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color curPix = image.GetPixel(x, y);
                    if ((curPix.R > a) && (curPix.R < b))
                    {
                        image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R + 1, curPix.G, curPix.B)); // Temporary, shift value of pixel by 1
                    }
                    else if (curPix.R == a)
                    {
                        if (bitPayload.Length > payloadPos && bitPayload.Get(payloadPos))
                        {
                            image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R + 1, curPix.G, curPix.B)); // Temporary, shift value of pixel by 1
                        }
                        payloadPos++;
                    }
                }
            }
            return image;
        }

        public Bitmap Encode(Bitmap inputImage, string payload)
        {
            Bitmap newImage = new Bitmap(inputImage);
            BitArray bitPayload = new BitArray(Encoding.ASCII.GetBytes(payload));
            int[] histogram = getHistogram(newImage);
            // Find Max 'a' and Min 'b'
            int a = 0;
            int b = 0;
            (a, b) = findSignificantPoints(histogram);
            // If a < b the encoding is not possible
            if (b < a)
            {
                return inputImage; // Temporary
            }
            // If b - a > payload message, encoding is not possible
            if (histogram[a] - histogram[b] < bitPayload.Length)
            {
                return inputImage; // Temporary
            }
            // Gather overhead data, need to be stored for decoding
            (int, int)[] overhead = recodeMinPixels(newImage, b);
            // Shift all pixels in range (a, b) by 1 and encode payload
            newImage = shiftImageAndEncode(newImage, a, b, bitPayload);
            // REMEMBER, save overhead for decoding
            return newImage;
        }
	}
}
