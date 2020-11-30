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
            public byte a { get; set; }
            public byte r { get; set; }
            public byte g { get; set; }
            public byte b { get; set; }

            // public Color retrivalPixel { get; set; }
            public int[][] significantPoints { get; set; }
            // public List<(int, int, EmbeddingChanel)> overhead { get; set; }
            // public HashSet<EmbeddingChanel> embeddingChannels { get; set; }
            public byte embeddingChannels { get; set; }

            public RetrivalData() { }
            public RetrivalData(int[][] significantPoints, /*List<(int, int, EmbeddingChanel)> overhead,*/ byte embeddingChannels, byte a, byte r, byte g, byte b)
            {
                this.significantPoints = significantPoints;
                this.embeddingChannels = embeddingChannels;
                this.a = a;
                this.r = r;
                this.g = g;
                this.b = b;
            }

        }

        static private int[][] GetHistogram(Bitmap inputImage)
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

        static private int[][] FindSignificantPoints(int[][] histogram)
        {
            int[][] significantPoints = new int[histogram.Length][];
            for (int i = 0; i < histogram.Length; i++)
            {
                significantPoints[i] = new int[2];
                int a = Array.IndexOf(histogram[i], histogram[i].Max());
                int b = 0;
                int minVal = int.MaxValue;
                for (int x = 254; x > a; x--)
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

        static private (int, int, EmbeddingChanel)[] RecodeMinPixels(Bitmap image, int[][] significantPoints, EmbeddingChanel channel)
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

        static private Bitmap ShiftImageAndEncode(Bitmap image, int[][] significantPoints, EmbeddingChanel channel, BitArray bitPayload)
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

        static private List<BitArray> getPayloadsForEachChannel(BitArray payload, HashSet<EmbeddingChanel> channels, int[][] significantPoints, int[][] histogram)
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

            //output.Add(payload);
            return output;
        }

        static private HashSet<EmbeddingChanel> retriveChannels(Color pix)
        {
            Boolean r = false;
            Boolean g = false;
            Boolean b = false;
            int enc = pix.A;
            HashSet<EmbeddingChanel> embeddingChannels = new HashSet<EmbeddingChanel>();
            if (enc >= 4)
            {
                b = true;
                enc -= 4;
            }
            if (enc >= 2)
            {
                g = true;
                enc -= 2;
            }
            if (enc >= 1)
            {
                r = true;
                enc -= 1;
            }
            if (r)
                embeddingChannels.Add(EmbeddingChanel.R);
            if (g)
                embeddingChannels.Add(EmbeddingChanel.G);
            if (b)
                embeddingChannels.Add(EmbeddingChanel.B);
            return embeddingChannels;
        }

        static private HashSet<EmbeddingChanel> retriveChannelsByte(byte enc)
        {
            Boolean r = false;
            Boolean g = false;
            Boolean b = false;
            HashSet<EmbeddingChanel> embeddingChannels = new HashSet<EmbeddingChanel>();
            if (enc >= 4)
            {
                b = true;
                enc -= 4;
            }
            if (enc >= 2)
            {
                g = true;
                enc -= 2;
            }
            if (enc >= 1)
            {
                r = true;
                enc -= 1;
            }
            if (r)
                embeddingChannels.Add(EmbeddingChanel.R);
            if (g)
                embeddingChannels.Add(EmbeddingChanel.G);
            if (b)
                embeddingChannels.Add(EmbeddingChanel.B);
            return embeddingChannels;
        }

        static private byte aquireChannelsByte(HashSet<EmbeddingChanel> embeddingChanels)
        {
            byte encoding = 0;
            if (embeddingChanels.Contains(EmbeddingChanel.R))
            {
                encoding += 1;
            }
            if (embeddingChanels.Contains(EmbeddingChanel.G))
            {
                encoding += 2;
            }
            if (embeddingChanels.Contains(EmbeddingChanel.B))
            {
                encoding += 4;
            }
            return encoding;
        }

        static private Bitmap setRetrivalPixel(Bitmap encodedImage, int[][] significantPoints, HashSet<EmbeddingChanel> embeddingChanels)
        {
            int encoding = 0;
            if (embeddingChanels.Contains(EmbeddingChanel.R))
            {
                encoding += 1;
            }
            if (embeddingChanels.Contains(EmbeddingChanel.G))
            {
                encoding += 2;
            }
            if (embeddingChanels.Contains(EmbeddingChanel.B))
            {
                encoding += 4;
            }
            encodedImage.SetPixel(0, 0, Color.FromArgb(encoding, significantPoints[0][0], significantPoints[1][0], significantPoints[2][0]));
            return encodedImage;
        }

        static private string BitArrayToString(BitArray bitArray)
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

        static private byte[] gatherSingleChannelPayload(Bitmap encodedImage, EmbeddingChanel channel, int a)
        {
            List<bool> payload = new List<bool>();

            for (int x = 0; x < encodedImage.Width; x++)
            {
                for (int y = 0; y < encodedImage.Height; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
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
            BitArray arr = new BitArray(payload.ToArray());
            byte[] bytes = new byte[arr.Length];
            arr.CopyTo(bytes, 0);
            return bytes;
        }

        static private BitArray gatherPayload(Bitmap encodedImage, HashSet<EmbeddingChanel> embeddingChannels, Color retrivalPixel)
        {
            List<byte[]> data = new List<byte[]>();
            int size = 0;
            int pointer = 0;
            byte[] final;
            foreach (var channel in embeddingChannels)
            {
                if (channel == EmbeddingChanel.R)
                {
                    data.Add(gatherSingleChannelPayload(encodedImage, channel, retrivalPixel.R));
                }
                else if (channel == EmbeddingChanel.G)
                {
                    data.Add(gatherSingleChannelPayload(encodedImage, channel, retrivalPixel.G));
                }
                else if (channel == EmbeddingChanel.B)
                {
                    data.Add(gatherSingleChannelPayload(encodedImage, channel, retrivalPixel.B));
                }
            }
            foreach (var arr in data)
            {
                size += arr.Length;
            }
            final = new byte[size];
            foreach (var arr in data)
            {
                Array.Copy(arr, 0, final, pointer, arr.Length);
                pointer += arr.Length;
            }
            return new BitArray(final);
        }

        static private Bitmap reshiftImage(Bitmap encodedImage, RetrivalData retrivalData)
        {
            foreach (var channel in retriveChannelsByte(retrivalData.embeddingChannels))
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
                                    if (currentPixel.R > a && currentPixel.R <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R - 1, currentPixel.G, currentPixel.B));
                                    }
                                    break;
                                }
                            case EmbeddingChanel.G:
                                {
                                    if (currentPixel.G > a && currentPixel.G <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G - 1, currentPixel.B));
                                    }
                                    break;
                                }
                            case EmbeddingChanel.B:
                                {
                                    if (currentPixel.B > a && currentPixel.B <= b)
                                    {
                                        encodedImage.SetPixel(x, y, Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G, currentPixel.B - 1));
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            /*foreach (var shiftPixel in retrivalData.overhead)
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
            }*/
            return encodedImage;
        }

        static private Object byteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);

            return obj;
        }

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration algorithmConfiguration)
        {
            Bitmap newImage = new Bitmap(inputImage);
            HashSet<EmbeddingChanel> embeddingChanels = algorithmConfiguration.EmbeddingChanels;

            // get histogram
            int[][] histogram = GetHistogram(newImage);

            // gather significant points
            int[][] significantPoints = FindSignificantPoints(histogram);

            // gather overhead data
            List<(int, int, EmbeddingChanel)> overhead = new List<(int, int, EmbeddingChanel)>();
            for (int i = 0; i < embeddingChanels.Count; i++)
            {
                overhead.AddRange(RecodeMinPixels(newImage, significantPoints, embeddingChanels.ElementAt(i)));
            }
            // gather retrival pixel data
            Color retPix = newImage.GetPixel(0, 0);

            // create rertival data
            RetrivalData retrivalData = new RetrivalData(significantPoints, aquireChannelsByte(embeddingChanels), retPix.A, retPix.R, retPix.G, retPix.B);

            // shift and encode with whole data
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, retrivalData);
            byte[] retrivalBytes = ms.ToArray();
            byte[] dataLengthBytes = BitConverter.GetBytes(retrivalBytes.Length);
            byte[] bitPayload = Encoding.ASCII.GetBytes(payload);
            byte[] fullData = new byte[4 + retrivalBytes.Length + bitPayload.Length];
            Array.Copy(dataLengthBytes, 0, fullData, 0, 4);
            Array.Copy(retrivalBytes, 0, fullData, 4, retrivalBytes.Length);
            Array.Copy(bitPayload, 0, fullData, 4 + retrivalBytes.Length, bitPayload.Length);
            ms.Close();

            BitArray bitArray = new BitArray(fullData);
            List<BitArray> channelPayloads = getPayloadsForEachChannel(bitArray, embeddingChanels, significantPoints, histogram);
            //newImage = ShiftImageAndEncode(newImage, significantPoints, embeddingChanels.ElementAt(0), bitArray);
            for (int i = 0; i < embeddingChanels.Count; i++)
            {
                newImage = ShiftImageAndEncode(newImage, significantPoints, embeddingChanels.ElementAt(i), channelPayloads.ElementAt(i));
            }

            // change retrival pixel
            newImage = setRetrivalPixel(newImage, significantPoints, embeddingChanels);
            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration algorithmConfiguration)
        {
            Bitmap originalImage;
            string payload;

            // read retrival pixel
            Color retrivalPixel = encodedImage.GetPixel(0, 0);

            // gather retrival data
            HashSet<EmbeddingChanel> embeddingChannels = retriveChannels(retrivalPixel);
            BitArray dataBitArray = gatherPayload(encodedImage, embeddingChannels, retrivalPixel);
            byte[] fullData = new byte[dataBitArray.Length];
            dataBitArray.CopyTo(fullData, 0);
            int retrivalDataLength = BitConverter.ToInt32(fullData, 0);
            byte[] retrivalDataBytes = new byte[retrivalDataLength];
            for (int i = 0; i < retrivalDataLength; i++)
            {
                retrivalDataBytes[i] = fullData[i + 4];
            }
            RetrivalData retrival = (RetrivalData)byteArrayToObject(retrivalDataBytes);

            // separate payload
            byte[] payloadBytes = new byte[fullData.Length - 4 - retrivalDataLength];
            for (int i = 0; i < retrivalDataLength; i++)
            {
                payloadBytes[i] = fullData[i + 4 + retrivalDataLength];
            }
            payload = BitArrayToString(new BitArray(payloadBytes)).Replace("\0", string.Empty);

            // set retrival pixel
            originalImage = encodedImage;
            originalImage.SetPixel(0, 0, Color.FromArgb(retrival.a, retrival.r, retrival.g, retrival.b));

            // retrive original image
            originalImage = reshiftImage(encodedImage, retrival);

            return new Tuple<Bitmap, string>(originalImage, payload);
        }
    }
}
