
﻿using ReversibleSignatureAnalyzer.Model.Algorithm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ReversibleSignatureAnalyzer.Model
{
    class DifferencesExpansionAlgorithm : IReversibleWatermarkingAlgorithm
    {
        private int treshold;
        private int iterations;
        private Direction direction;

        public DifferencesExpansionAlgorithm(int treshold, int iterations, Direction direction)
        {
            this.treshold = treshold;
            this.iterations = iterations;
            this.direction = direction;
        }
        public Bitmap Encode(Bitmap inputImage, string payload)
        {
            Bitmap image = new Bitmap(inputImage);
            int[,] pixelValues = GetPixelValues(image);
            int[,] averages = Calculate(image, CalculateAverage, direction);
            int[,] differences = Calculate(image, CalculateDifference, direction);
            int[,] localityMap = GetEmptyArrayAdjustedToImageSize(image, direction);
            EncodingSetName[,] setsIds = new EncodingSetName[localityMap.GetLength(0), localityMap.GetLength(1)];
            int EZSize = 0;
            int EN1Size = 0;
            List<int> EN2AndCN = new List<int>();
            for (int i = 0; i < setsIds.GetLength(0); i++)
            {
                for (int j = 0; j < setsIds.GetLength(1); j++)
                {
                    if (IsExpandable(i, j, differences, averages))
                    {
                        if(differences[i, j] == 0 || differences[i, j] == -1)
                        {
                            localityMap[i, j] = 1;
                            EZSize++;
                            setsIds[i, j] = EncodingSetName.EZ;
                        }
                        else if (Math.Abs(differences[i, j]) <= treshold)
                        {
                            localityMap[i, j] = 1;
                            EN1Size++;
                            setsIds[i, j] = EncodingSetName.EN1;
                        }
                        else
                        {
                            EN2AndCN.Add(differences[i, j]);
                            localityMap[i, j] = 0;
                            setsIds[i, j] = EncodingSetName.EN2;
                        }
                    }
                    else if (IsChangeable(i, j, differences, averages))
                    {
                        EN2AndCN.Add(differences[i, j]);
                        localityMap[i, j] = 0;
                        setsIds[i, j] = EncodingSetName.CN;
                    }
                    else
                    {
                        localityMap[i, j] = 0;
                        setsIds[i, j] = EncodingSetName.NC;
                    }
                }
            }

            List<byte> localityVector = GetLeastSignificatBits(ConvertToList(localityMap));
//            byte[] compressedLocalityVector = RLE<byte>.Encode(localityVector).ToArray();

            List<int> filteredEN2AndCN = EN2AndCN.Where(x => x != -2 && x != 1).ToList();
            List<byte> LSBs = GetLeastSignificatBits(filteredEN2AndCN);
 //           byte[] compressedLSBs = RLE<byte>.Encode(LSBs).ToArray();

            int embeddingCapacity = EZSize + EN1Size + EN2AndCN.Count;

            byte[] localityVectorSize = BitConverter.GetBytes((uint)localityVector.Count);
            byte[] LSBsSize = BitConverter.GetBytes((uint)LSBs.Count);
            byte[] payloadSize = BitConverter.GetBytes(payload.Length);
            byte[] header = ConcatArrays(localityVectorSize, LSBsSize, payloadSize);
            byte[] data = ConcatArrays(localityVector.ToArray(), LSBs.ToArray(), Encoding.ASCII.GetBytes(payload));
            byte[] compressedData = RLE<byte>.Encode(new List<byte>(data)).ToArray(); 
            byte[] embeddingStream = ConcatArrays(header, compressedData);
            bool[] bits = embeddingStream.SelectMany(GetBits).ToArray();

            int bitNumber = 0;
            for(int p = 0; p < setsIds.GetLength(0) && bitNumber < bits.Length; p++)
            {
                for(int q = 0; q < setsIds.GetLength(1) && bitNumber < bits.Length; q++)
                {
                    if (setsIds[p, q] == EncodingSetName.EZ || setsIds[p, q] == EncodingSetName.EN1)
                    {
                        differences[p, q] = 2 * differences[p, q] + GetIntValue(bits[bitNumber]);
                        bitNumber++;
                    }
                    else if (setsIds[p, q] == EncodingSetName.EN2 || setsIds[p, q] == EncodingSetName.CN)
                    {
                        differences[p, q] = 2 * DivideAndFloor(differences[p, q], 2) + GetIntValue(bits[bitNumber]);
                        bitNumber++;
                    }
                }
            }

            Bitmap embedded = GenerateEmbeddedImage(image, averages, differences, direction);
            int[,] pixelValuesEmbedded = GetPixelValues(embedded);
            return embedded;
        }

        private int DivideAndFloor(double number, double divisor)
        {
            return (int) Math.Floor(number / divisor);
        }

        private int [,] GetPixelValues(Bitmap image)
        {
            int[,] values = new int[image.Height, image.Width];
            for(int y = 0; y < image.Height; y++)
            {
                for(int x = 0; x < image.Width; x++)
                {
                    values[y, x] = image.GetPixel(x, y).R;
                }
            }
            return values;
        }

        private int [,] Calculate(Bitmap image, Func<int, int, int> operation, Direction direction)
        {
            if (direction == Direction.Horizontal)
            {
                return CalculateHorizontally(image, operation);
            }
            return CalculateVertically(image, operation);
        }

        private int [,] CalculateHorizontally(Bitmap image, Func<int, int, int> operation)
        {
            int[,] result = new int[image.Height, image.Width / 2];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width / 2; x++)
                {
                    result[y, x] = operation(image.GetPixel(2 * x, y).R, image.GetPixel(2 * x + 1, y).R);
                }
            }
            return result;
        }

        private int[,] CalculateVertically(Bitmap image, Func<int, int, int> operation)
        {
            int[,] result = new int[image.Height / 2, image.Width];
            for (int y = 0; y < image.Height / 2; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    result[y, x] = operation(image.GetPixel(x, 2 * y).R, image.GetPixel(x, 2 * y + 1).R);
                }
            }
            return result;
        }
        private int CalculateAverage(int x, int y)
        {
            return (int) Math.Floor((((double) x) + y) / 2);
        }

        private int CalculateDifference(int x, int y)
        {
            return x - y;
        }

        private int[,] GetEmptyArrayAdjustedToImageSize(Bitmap image, Direction direction)
        {
            if (direction == Direction.Horizontal)
            {
                return new int[image.Height, image.Width / 2];
            }
            return new int[image.Height / 2, image.Width];
        }

        private bool IsExpandable(int i, int j, int[,] differences, int[,] averages)
        {
            return Math.Abs(2 * differences[i, j] + 0) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1) && 
                Math.Abs(2 * differences[i, j] + 1) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1);
        }

        private bool IsChangeable(int i, int j, int[,] differences, int[,] averages)
        {
            return Math.Abs(2 * DivideAndFloor(differences[i, j], 2) + 0) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1) &&
                Math.Abs(2 * DivideAndFloor(differences[i, j], 2) + 1) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1);
        }

        static List<int> ConvertToList(int[,] array)
        {
            int[] tmp = new int[array.GetLength(0) * array.GetLength(1)];
            Buffer.BlockCopy(array, 0, tmp, 0, tmp.Length * sizeof(int));
            List<int> list = new List<int>(tmp);
            return list;
        }

        private List<byte> GetLeastSignificatBits(List<int> values)
        {
            List<byte> leastSignificantBits = new List<byte>();
            int count = values.Count;
            int i;
            int pos;
            int currentValue = 0;
            for(i = 0; i < count; i+=pos)
            {
                currentValue = 0;
                for(pos = 0; pos < 8 && (i + pos) < count; pos++)
                {
                    currentValue += (values[i + pos] & 1) * (int)(Math.Pow(2, 7 - pos));
                }
                leastSignificantBits.Add((byte) currentValue);
            }
            return leastSignificantBits;
        }

        public static T[] ConcatArrays<T>(params T[][] list)
        {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++)
            {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }
            return result;
        }

        IEnumerable<bool> GetBits(byte b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return (b & 0x80) != 0;
                b *= 2;
            }
        }

        private int GetIntValue(bool boolean)
        {
            return boolean ? 1 : 0;
        }

        private Bitmap GenerateEmbeddedImage(Bitmap image, int[,] averages, int[,] newDifferences, Direction direction)
        {
            Bitmap newImage = new Bitmap(image);
            return Embed(newImage, averages, newDifferences, direction);
        }

        private Bitmap Embed(Bitmap image, int[,] averages, int[,] newDifferences, Direction direction)
        {
            if (direction == Direction.Horizontal)
            {
                return EmbedHorizontally(image, averages, newDifferences);
            }
            return EmbedVertically(image, averages, newDifferences);
        }

        private Bitmap EmbedHorizontally(Bitmap image, int[,] averages, int[,] newDifferences)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width / 2; x++)
                {
                    Color currentXColor = image.GetPixel(2 * x, y);
                    int XR = averages[y, x] + DivideAndFloor(newDifferences[y, x] + 1, 2);
                    image.SetPixel(2 * x, y, Color.FromArgb(currentXColor.A, XR, currentXColor.G, currentXColor.B));
                    int YR = averages[y, x] - DivideAndFloor(newDifferences[y, x], 2);
                    Color currentYColor = image.GetPixel(2 * x + 1, y);
                    image.SetPixel(2 * x + 1, y, Color.FromArgb(currentYColor.A, YR, currentYColor.G, currentYColor.B));
                }
            }
            return image;
        }

        private Bitmap EmbedVertically(Bitmap image, int[,] averages, int[,] newDifferences)
        {
            for (int y = 0; y < image.Height / 2; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color currentXColor = image.GetPixel(x, 2 * y);
                    int XR = averages[y, x] + DivideAndFloor(newDifferences[y, x] + 1, 2);
                    image.SetPixel(x, 2 * y, Color.FromArgb(currentXColor.A, XR, currentXColor.G, currentXColor.B));
                    int YR = averages[y, x] - DivideAndFloor(newDifferences[y, x], 2);
                    Color currentYColor = image.GetPixel(x, 2 * y + 1);
                    image.SetPixel(x, 2 * y + 1, Color.FromArgb(currentYColor.A, YR, currentYColor.G, currentYColor.B));
                }
            }
            return image;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage)
        {
            Bitmap image = new Bitmap(encodedImage);
            int[,] pixelValues = GetPixelValues(image);
            int[,] averages = Calculate(image, CalculateAverage, direction);
            int[,] differences = Calculate(image, CalculateDifference, direction);
            int[,] localityMap = GetEmptyArrayAdjustedToImageSize(image, direction);
            DecodingSetName[,] setsIds = new DecodingSetName[localityMap.GetLength(0), localityMap.GetLength(1)];
            List<int> changeableDifferences = new List<int>();

            for (int i = 0; i < setsIds.GetLength(0); i++)
            {
                for (int j = 0; j < setsIds.GetLength(1); j++)
                {
                    if (IsChangeable(i, j, differences, averages))
                    {
                        changeableDifferences.Add(differences[i, j]);
                        setsIds[i, j] = DecodingSetName.CH;
                    }
                    else
                    {
                        setsIds[i, j] = DecodingSetName.NC;
                    }
                }
            }
            List<byte> LSBs = GetLeastSignificatBits(changeableDifferences);
            byte[] localityVectorSizeBytes = LSBs.GetRange(0, 4).ToArray();
            byte[] originalLSBsSizeBytes = LSBs.GetRange(4, 4).ToArray();
            byte[] payloadSizeBytes = LSBs.GetRange(8, 4).ToArray();
            byte[] compressedData = LSBs.GetRange(12, LSBs.Count - 12).ToArray();
            int localityVectorSize = BitConverter.ToInt32(localityVectorSizeBytes, 0);
            int originalLSBsSize = BitConverter.ToInt32(originalLSBsSizeBytes, 0);
            int payloadSize = BitConverter.ToInt32(payloadSizeBytes, 0);
            byte[] originalData = RLE<byte>.Decode(compressedData).ToArray();
            byte[] localityVector = new List<byte>(originalData).GetRange(0, localityVectorSize).ToArray();
            byte[] originalLSBsVector = new List<byte>(originalData).GetRange(localityVectorSize, originalLSBsSize).ToArray();
            byte[] payload = new List<byte>(originalData).GetRange(localityVectorSize + originalLSBsSize, payloadSize).ToArray();
            bool[] localityVectorBits = new List<byte>(localityVector).SelectMany(GetBits).ToArray();
            bool[] originalLSBsVectorBits = new List<byte>(originalLSBsVector).SelectMany(GetBits).ToArray();
            bool[] payloadBits = new List<byte>(payload).SelectMany(GetBits).ToArray();
            int bitNumber = 0;
            for(int i = 0; i < differences.GetLength(0); i++)
            {
                for(int j = 0; j < differences.GetLength(1); j++)
                {
                    if (setsIds[i, j] == DecodingSetName.CH)
                    {
                        if(localityVectorBits[i * differences.GetLength(1) + j])
                        {
                            differences[i, j] = DivideAndFloor(differences[i, j], 2);
                        }
                        else
                        {
                            if (differences[i, j] == 0 || differences[i, j] == 1)
                            {
                                differences[i, j] = 1;
                            }
                            else if (differences[i, j] == -2 || differences[i, j] == -1)
                            {
                                differences[i, j] = -2;
                            }
                            else
                            {
                                differences[i, j] = 2 * DivideAndFloor(differences[i, j], 2) + GetIntValue(originalLSBsVectorBits[bitNumber]);
                                bitNumber++;
                            }
                        }
                    }
                }
            }
            Bitmap originalImage = GenerateEmbeddedImage(image, averages, differences, direction);
            string payloadString = Encoding.ASCII.GetString(payload);
            return new Tuple<Bitmap, string>(originalImage, payloadString);
        }

    }

    enum Direction
    {
        Horizontal,
        Vertical
    }

    enum EncodingSetName
    {
        EZ,
        EN1,
        EN2,
        CN,
        NC
    }

    enum DecodingSetName
    {
        CH,
        NC
    }

}
