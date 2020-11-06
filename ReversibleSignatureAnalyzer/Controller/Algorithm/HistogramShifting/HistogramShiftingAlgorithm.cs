using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting
{
    class HistogramShiftingAlgorithm : IReversibleWatermarkingAlgorithm
    {
        public class RetrivalData
        {
            public int a { get; set; }
            public int b { get; set; }
            public (int, int)[] overhead { get; set; }

            public RetrivalData() { }
            public RetrivalData(int a, int b, (int, int)[] overhead)
            {
                this.a = a;
                this.b = b;
                this.overhead = overhead;
            }

        }

        private int[] GetHistogram(Bitmap inputImage)
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

        private (int, int) FindSignificantPoints(int[] histogram)
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

        private (int, int)[] RecodeMinPixels(Bitmap image, int b)
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

        private Bitmap ShiftImageAndEncode(Bitmap image, int a, int b, System.Collections.BitArray bitPayload)
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

        private string BitArrayToString(BitArray bitArray)
        {
            byte[] strArr = new byte[bitArray.Length / 8];
            ASCIIEncoding encoding = new ASCIIEncoding();
            for (int i = 0; i < bitArray.Length / 8; i++)
            {
                for (int index = i * 8, m = 1; index < i * 8 + 8; index++, m *= 2)
                {
                    strArr[i] += bitArray.Get(index) ? (byte)m : (byte)0;
                }
            }
            return encoding.GetString(strArr);
        }

        private string GatherPayload(Bitmap encodedImage, int a)
        {
            List<bool> payload = new List<bool>();
            for (int x = 0; x < encodedImage.Width; x++)
            {
                for (int y = 0; y < encodedImage.Height; y++)
                {
                    var currentPixel = encodedImage.GetPixel(x, y).R;
                    if (currentPixel == a)
                    {
                        payload.Add(false);
                    }
                    else if (currentPixel == a + 1)
                    {
                        payload.Add(true);
                    }
                }
            }
            BitArray bitArray = new BitArray(payload.ToArray());
            return BitArrayToString(bitArray);
        }

        private Bitmap ReshiftImage(Bitmap encodedImage, int a, int b, (int, int)[] overhead)
        {
            // Shift image back into place
            for (int x = 0; x < encodedImage.Width; x++)
            {
                for (int y = 0; y < encodedImage.Height; y++)
                {
                    var currentPixel = encodedImage.GetPixel(x, y);
                    if (currentPixel.R > a || currentPixel.R <= b)
                    {
                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R - 1, currentPixel.G, currentPixel.B));
                    }
                }
            }
            // Place overhead pixels back into place
            for (int i = 0; i < overhead.Length; i++)
            {
                int x, y;
                (x, y) = overhead[i];
                var currentPixel = encodedImage.GetPixel(x, y);
                encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, b, currentPixel.G, currentPixel.B));
            }
            return encodedImage;
        }

        public Bitmap Encode(Bitmap inputImage, string payload)
        {
            Bitmap newImage = new Bitmap(inputImage);
            BitArray bitPayload = new BitArray(Encoding.ASCII.GetBytes(payload));
            int[] histogram = GetHistogram(newImage);
            // Find Max 'a' and Min 'b'
            int a, b;
            (a, b) = FindSignificantPoints(histogram);
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
            (int, int)[] overhead = RecodeMinPixels(newImage, b);
            // Shift all pixels in range (a, b) by 1 and encode payload
            newImage = ShiftImageAndEncode(newImage, a, b, bitPayload);
            // Save overhead for decoding
            RetrivalData retrive = new RetrivalData(a, b, overhead);
            var json = JsonSerializer.Serialize(retrive);
            // HARDCODED HARDCODED HARDCODED, remember to change it
            System.IO.File.WriteAllText("RetrivalData.json", json);
            // HARDCODED HARDCODED HARDCODED, remember to change it
            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage)
        {
            Bitmap originalImage;
            // HARDCODED HARDCODED HARDCODED, remember to change it
            string json = System.IO.File.ReadAllText("RetrivalData.json");
            // HARDCODED HARDCODED HARDCODED, remember to change it
            RetrivalData retrival = JsonSerializer.Deserialize<RetrivalData>(json);
            // Get payload from the image
            string payload = GatherPayload(encodedImage, retrival.a).Replace("\0", string.Empty);
            // Return image to its original form
            originalImage = ReshiftImage(encodedImage, retrival.a, retrival.b, retrival.overhead);
            return new Tuple<Bitmap, string>(originalImage, payload);
        }
    }
}
