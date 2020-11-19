using ReversibleSignatureAnalyzer.Controller.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting
{
    class HistogramShiftingAlgorithm : IReversibleWatermarkingAlgorithm
    {
        [Serializable]
        public class RetrivalData
        {
            public int originalImageHeigth { get; set; }
            public int[][] significantPoints { get; set; }
            public List<(int, int, EmbeddingChanel)> overhead { get; set; }

            public HashSet<EmbeddingChanel> embeddingChannels { get; set; }

            public RetrivalData() { }
            public RetrivalData(int[][] significantPoints, List<(int, int, EmbeddingChanel)> overhead, int originalImageHeigth, HashSet<EmbeddingChanel> embeddingChannels)
            {
                this.significantPoints = significantPoints;
                this.overhead = overhead;
                this.originalImageHeigth = originalImageHeigth;
                this.embeddingChannels = embeddingChannels;
            }

        }

        private int[][] GetHistogram(Bitmap inputImage)
        {
            int[][] histogram = new int[3][];
            for (int x = 0; x < histogram.Length; x++)
                histogram[x] = new int[256];
            for (int x = 0; x < inputImage.Width; x++)
            {
                for (int y = 0; y < inputImage.Height; y++)
                {
                    var curPix = inputImage.GetPixel(x, y);
                    histogram[0][curPix.R] += 1;
                    histogram[1][curPix.G] += 1;
                    histogram[2][curPix.B] += 1;
                }
            }
            return histogram;
        }

        private int[][] FindSignificantPoints(int[][] histogram)
        {
            int[][] significantPoints = new int[histogram.Length][];
            for (int i = 0; i < histogram.Length; i++)
            {
                significantPoints[i] = new int[2];
                int a = Array.IndexOf(histogram[i], histogram[i].Max());
                int b = 0;
                int minVal = int.MaxValue;
                for (int x = 255; x > a; x--)
                {
                    if (histogram[i][x] < minVal)
                    {
                        minVal = histogram[i][x];
                        b = x;
                    }
                }
                significantPoints[i][0] = a;
                significantPoints[i][1] = b;
            }
            return significantPoints;
        }

        private (int, int, EmbeddingChanel)[] RecodeMinPixels(Bitmap image, int[][] significantPoints, EmbeddingChanel channel)
        {
            int b = significantPoints[(int)channel][1];
            // If b is 0 already ignore this step
            if (b == 0) return null;
            // else
            List<(int, int, EmbeddingChanel)> positions = new List<(int, int, EmbeddingChanel)>();
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (channel == EmbeddingChanel.R && image.GetPixel(x, y).R == b)
                    {
                        Color curPix = image.GetPixel(x, y);
                        positions.Add((x, y, channel));
                        image.SetPixel(x, y, Color.FromArgb(curPix.A, 0, curPix.G, curPix.B));
                    }
                    else if (channel == EmbeddingChanel.G && image.GetPixel(x, y).G == b)
                    {
                        Color curPix = image.GetPixel(x, y);
                        positions.Add((x, y, channel));
                        image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, 0, curPix.B));
                    }
                    else if (channel == EmbeddingChanel.B && image.GetPixel(x, y).B == b)
                    {
                        Color curPix = image.GetPixel(x, y);
                        positions.Add((x, y, channel));
                        image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, curPix.G, 0));
                    }
                }
            }
            return positions.ToArray();
        }

        private Bitmap ShiftImageAndEncode(Bitmap image, int[][] significantPoints, EmbeddingChanel channel, BitArray bitPayload)
        {
            int payloadPos = 0;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color curPix = image.GetPixel(x, y);
                    if (channel == EmbeddingChanel.R)
                    {
                        if ((curPix.R > significantPoints[(int)channel][0]) && (curPix.R < significantPoints[(int)channel][1]))
                        {
                            image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R + 1, curPix.G, curPix.B));
                        }
                        else if (curPix.R == significantPoints[(int)channel][0])
                        {
                            if (bitPayload.Length > payloadPos && bitPayload.Get(payloadPos))
                            {
                                image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R + 1, curPix.G, curPix.B));
                            }
                            payloadPos++;
                        }
                    }
                    else if (channel == EmbeddingChanel.G)
                    {
                        if ((curPix.G > significantPoints[(int)channel][0]) && (curPix.G < significantPoints[(int)channel][1]))
                        {
                            image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, curPix.G + 1, curPix.B));
                        }
                        else if (curPix.G == significantPoints[(int)channel][0])
                        {
                            if (bitPayload.Length > payloadPos && bitPayload.Get(payloadPos))
                            {
                                image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, curPix.G + 1, curPix.B));
                            }
                            payloadPos++;
                        }
                    }
                    else if (channel == EmbeddingChanel.B)
                    {
                        if ((curPix.B > significantPoints[(int)channel][0]) && (curPix.B < significantPoints[(int)channel][1]))
                        {
                            image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, curPix.G, curPix.B + 1));
                        }
                        else if (curPix.B == significantPoints[(int)channel][0])
                        {
                            if (bitPayload.Length > payloadPos && bitPayload.Get(payloadPos))
                            {
                                image.SetPixel(x, y, Color.FromArgb(curPix.A, curPix.R, curPix.G, curPix.B + 1));
                            }
                            payloadPos++;
                        }
                    }
                }
            }
            return image;
        }


        private List<BitArray> getPayloadsForEachChannel(BitArray payload, HashSet<EmbeddingChanel> channels, int[][] significantPoints, int[][] histogram)
        {
            int startIndex = 0;
            int remainingData = payload.Length;
            List<BitArray> output = new List<BitArray>();
            bool[] payloadArray = new bool[payload.Length];
            payload.CopyTo(payloadArray, 0);
            foreach (EmbeddingChanel channel in channels)
            {
                if (remainingData > 0)
                {
                    int a = significantPoints[(int)channel][0];
                    int b = significantPoints[(int)channel][1];
                    int capacity = histogram[(int)channel][a] - histogram[(int)channel][b];
                    bool[] payloadChannel = new bool[capacity];
                    if (capacity > remainingData)
                    {
                        Array.Copy(payloadArray, startIndex, payloadChannel, 0, remainingData);
                    }
                    else
                    {
                        Array.Copy(payloadArray, startIndex, payloadChannel, 0, capacity);
                    }
                    startIndex += capacity;
                    remainingData -= capacity;
                    output.Add(new BitArray(payloadChannel));
                }
                else
                {
                    output.Add(new BitArray(0));
                }
            }
            return output;
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

        private string GatherPayload(Bitmap encodedImage, RetrivalData retrivalData)
        {
            List<bool> payload = new List<bool>();

            foreach (var channel in retrivalData.embeddingChannels)
            {
                for (int x = 0; x < encodedImage.Width; x++)
                {
                    for (int y = 0; y < encodedImage.Height; y++)
                    {
                        int a = retrivalData.significantPoints[(int)channel][0];
                        var currentPixel = encodedImage.GetPixel(x, y);
                        switch (channel)
                        {
                            case EmbeddingChanel.R:
                                {
                                    if (currentPixel.R == a)
                                    {
                                        payload.Add(false);
                                    }
                                    else if (currentPixel.R == a + 1)
                                    {
                                        payload.Add(true);
                                    }
                                    break;
                                }
                            case EmbeddingChanel.G:
                                {
                                    if (currentPixel.G == a)
                                    {
                                        payload.Add(false);
                                    }
                                    else if (currentPixel.G == a + 1)
                                    {
                                        payload.Add(true);
                                    }
                                    break;
                                }
                            case EmbeddingChanel.B:
                                {
                                    if (currentPixel.B == a)
                                    {
                                        payload.Add(false);
                                    }
                                    else if (currentPixel.B == a + 1)
                                    {
                                        payload.Add(true);
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            BitArray bitArray = new BitArray(payload.ToArray());
            return BitArrayToString(bitArray);
        }

        private Bitmap reshiftImage(Bitmap encodedImage, RetrivalData retrivalData)
        {
            foreach (var channel in retrivalData.embeddingChannels)
            {
                for (int x = 0; x < encodedImage.Width; x++)
                {
                    for (int y = 0; y < encodedImage.Height; y++)
                    {
                        var currentPixel = encodedImage.GetPixel(x, y);
                        int a = retrivalData.significantPoints[(int)channel][0];
                        int b = retrivalData.significantPoints[(int)channel][1];
                        switch (channel)
                        {
                            case EmbeddingChanel.R:
                                {
                                    if (currentPixel.R > a || currentPixel.R <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R - 1, currentPixel.G, currentPixel.B));
                                    }
                                    break;
                                }
                            case EmbeddingChanel.G:
                                {
                                    if (currentPixel.G > a || currentPixel.G <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G - 1, currentPixel.B));
                                    }
                                    break;
                                }
                            case EmbeddingChanel.B:
                                {
                                    if (currentPixel.B > a || currentPixel.B <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G, currentPixel.B - 1));
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            foreach (var shiftPixel in retrivalData.overhead)
            {
                int b = retrivalData.significantPoints[(int)shiftPixel.Item3][1]; // value b for a given channel
                switch (shiftPixel.Item3)
                {
                    case EmbeddingChanel.R:
                        {
                            var currentPixel = encodedImage.GetPixel(shiftPixel.Item1, shiftPixel.Item2);
                            encodedImage.SetPixel(shiftPixel.Item1, shiftPixel.Item2, Color.FromArgb(currentPixel.A, b, currentPixel.G, currentPixel.B));
                            break;
                        }
                    case EmbeddingChanel.G:
                        {
                            var currentPixel = encodedImage.GetPixel(shiftPixel.Item1, shiftPixel.Item2);
                            encodedImage.SetPixel(shiftPixel.Item1, shiftPixel.Item2, Color.FromArgb(currentPixel.A, currentPixel.R, b, currentPixel.B));
                            break;
                        }
                    case EmbeddingChanel.B:
                        {
                            var currentPixel = encodedImage.GetPixel(shiftPixel.Item1, shiftPixel.Item2);
                            encodedImage.SetPixel(shiftPixel.Item1, shiftPixel.Item2, Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G, b));
                            break;
                        }
                }
            }
            return encodedImage;
        }

        private Object byteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }

        private Bitmap EncodeIteration(Bitmap inputImage, string payload, AlgorithmConfiguration algorithmConfiguration)
        {
            Bitmap newImage = new Bitmap(inputImage);
            var embeddingChanels = algorithmConfiguration.EmbeddingChanels;
            // Bitmap newImage = new Bitmap(inputImage);
            BitArray bitPayload = new BitArray(Encoding.ASCII.GetBytes(payload));
            int[][] histogram = GetHistogram(newImage);
            // Find Max 'a' and Min 'b' for all channels, aka significantPoints
            int[][] significantPoints = FindSignificantPoints(histogram);
            List<(int, int, EmbeddingChanel)> overhead = new List<(int, int, EmbeddingChanel)>();
            RetrivalData retrive = new RetrivalData(significantPoints, overhead, newImage.Height, embeddingChanels);

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, retrive);
            byte[] retrivalBytes = ms.ToArray();
            byte[] dataLengthBytes = BitConverter.GetBytes(retrivalBytes.Length);

            // calculate new image size with added columns

            byte[] fullData = new byte[4 + retrivalBytes.Length];
            Array.Copy(retrivalBytes, 0, fullData, 0, retrivalBytes.Length);
            Array.Copy(dataLengthBytes, 0, fullData, retrivalBytes.Length, 4);
            ms.Close();

            List<BitArray> channelPayloads = getPayloadsForEachChannel(bitPayload, embeddingChanels, significantPoints, histogram);
            // Gather overhead data, need to be stored for decoding
            for (int i = 0; i < embeddingChanels.Count; i++)
            {
                overhead.AddRange(RecodeMinPixels(newImage, significantPoints, embeddingChanels.ElementAt(i)));
                newImage = ShiftImageAndEncode(newImage, significantPoints, embeddingChanels.ElementAt(i), channelPayloads.ElementAt(i));
            }

            byte[] imageArr;
            using (var stream = new MemoryStream())
            {
                newImage.Save(stream, ImageFormat.Bmp);
                imageArr = stream.ToArray();
            }

            // calculate new image width with overhead data
            int newImageHeight = (int)Math.Ceiling((double)(4 + retrivalBytes.Length) / (newImage.Width * 4)) + newImage.Height;

            byte[] completeImage = new byte[4 * newImageHeight * newImage.Width + 54];
            Array.Copy(imageArr, 0, completeImage, 0, imageArr.Length);
            Array.Copy(fullData, 0, completeImage, completeImage.Length - fullData.Length, fullData.Length);

            // overwrite headers filesize and width

            byte[] newSize = BitConverter.GetBytes(completeImage.Length);
            for (int i = 2; i < 6; i++)
            {
                completeImage[i] = newSize[i - 2]; // write new image size to the picture
            }
            byte[] heightBytes = BitConverter.GetBytes(newImageHeight);
            for (int i = 22; i < 26; i++)
            {
                completeImage[i] = heightBytes[i - 22]; // write new image heigth to the picture
            }

            Bitmap ahaha = new Bitmap(newImage.Width, newImageHeight);
            // make new picture
            var memstream = new MemoryStream(completeImage);
            newImage = (Bitmap)Image.FromStream(memstream);

            return newImage;
        }

        private string[] getPayloadParts(string payload, AlgorithmConfiguration algorithmConfiguration)
        {
            int startIndex = 0;
            int baseLength = payload.Length / algorithmConfiguration.Iterations;
            int noLongerStrings = payload.Length % algorithmConfiguration.Iterations;
            string[] payloadParts = new string[algorithmConfiguration.Iterations];
            for (int i = 0; i < algorithmConfiguration.Iterations; i++)
            {
                if (noLongerStrings > 0)
                {
                    payloadParts[i] = payload.Substring(startIndex, baseLength + 1);
                    startIndex += baseLength + 1;
                    noLongerStrings--;
                }
                else
                {
                    payloadParts[i] = payload.Substring(startIndex, baseLength);
                    startIndex += baseLength;
                }
            }
            return payloadParts;
        }

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration algorithmConfiguration)
        {
            Bitmap newImage = inputImage;
            string[] payloadParts = getPayloadParts(payload, algorithmConfiguration);
            foreach (var part in payloadParts)
            {
                newImage = EncodeIteration(newImage, payload, algorithmConfiguration);
            }
            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration algorithmConfiguration)
        {
            Bitmap originalImage;
            string payload;
            ImageConverter converter = new ImageConverter();
            byte[] retrivalDataLengthBytes = new byte[4];
            byte[] imageArr = (byte[])converter.ConvertTo(encodedImage, typeof(byte[]));
            for (int i = 0; i < 4; i++)
            {
                retrivalDataLengthBytes[i] = imageArr[imageArr.Length - 4 + i];
            }
            // Get retrival data
            int retrivalDataLength = BitConverter.ToInt32(retrivalDataLengthBytes, 0);
            byte[] retrivalDataBytes = new byte[retrivalDataLength];
            for (int i = 0; i < retrivalDataLength; i++)
            {
                retrivalDataBytes[i] = imageArr[imageArr.Length - retrivalDataLength - 4 + i];
            }
            RetrivalData retrival = (RetrivalData)byteArrayToObject(retrivalDataBytes);
            // Get image without the overhead data embedded
            int originalImageSize = 4 * encodedImage.Width * retrival.originalImageHeigth + 54;
            byte[] imageWithoutOverhead = new byte[originalImageSize]; // length of this array is the size of original bitmap
            Array.Copy(imageArr, 0, imageWithoutOverhead, 0, originalImageSize);

            // Overwrite header to its original form
            byte[] oldSize = BitConverter.GetBytes(originalImageSize);
            for (int i = 2; i < 6; i++)
            {
                imageWithoutOverhead[i] = oldSize[i - 2]; // write new image size to the picture
            }
            byte[] heightBytes = BitConverter.GetBytes(retrival.originalImageHeigth);
            Array.Reverse(heightBytes);
            for (int i = 22; i < 26; i++)
            {
                imageWithoutOverhead[i] = heightBytes[i - 22]; // write new image heigth to the picture
            }

            var memstream = new MemoryStream(imageWithoutOverhead);
            originalImage = (Bitmap)Image.FromStream(memstream);
            // Get payload from the image
            payload = GatherPayload(originalImage, retrival).Replace("\0", string.Empty);
            // Return image to its original form
            originalImage = reshiftImage(originalImage, retrival);
            return new Tuple<Bitmap, string>(originalImage, payload);
        }
    }
}
