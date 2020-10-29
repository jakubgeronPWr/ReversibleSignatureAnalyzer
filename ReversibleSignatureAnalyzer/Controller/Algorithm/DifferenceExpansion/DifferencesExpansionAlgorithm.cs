using ReversibleSignatureAnalyzer.Model.Algorithm;
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
        private const byte EOM = 255;

        public DifferencesExpansionAlgorithm(int treshold, int iterations, Direction direction)
        {
            this.treshold = treshold;
            this.iterations = iterations;
            this.direction = direction;
        }
        public Bitmap Encode(Bitmap inputImage, string payload)
        {
            Bitmap image = new Bitmap(inputImage);
            int[,] averages = Calculate(image, CalculateAverage, direction);
            int[,] differences = Calculate(image, CalculateDifference, direction);
            int[,] localityMap = GetEmptyArrayAdjustedToImageSize(image, direction);
            SetName[,] setsIds = new SetName[localityMap.GetLength(0), localityMap.GetLength(1)];
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
                            setsIds[i, j] = SetName.EZ;
                        }
                        else if (Math.Abs(differences[i, j]) <= treshold)
                        {
                            localityMap[i, j] = 1;
                            EN1Size++;
                            setsIds[i, j] = SetName.EN1;
                        }
                        else
                        {
                            EN2AndCN.Add(differences[i, j]);
                            localityMap[i, j] = 0;
                            setsIds[i, j] = SetName.EN2;
                        }
                    }
                    else if (IsChangeable(i, j, differences, averages))
                    {
                        EN2AndCN.Add(differences[i, j]);
                        localityMap[i, j] = 0;
                        setsIds[i, j] = SetName.CN;
                    }
                    else
                    {
                        localityMap[i, j] = 0;
                        setsIds[i, j] = SetName.NC;
                    }
                }
            }

            List<byte> localityVector = GetLeastSignificatBits(ConvertToList(localityMap));
            byte[] compressedLocalityVector = RLE<byte>.Encode(localityVector).ToArray();

            List<int> filteredEN2AndCN = EN2AndCN.Where(x => x != -2 && x != 1).ToList();
            List<byte> LSBs = GetLeastSignificatBits(filteredEN2AndCN);
            byte[] compressedLSBs = RLE<byte>.Encode(LSBs).ToArray();

            int embeddingCapacity = EZSize + EN1Size + EN2AndCN.Count;

            byte[] embeddingStream = ConcatArrays(compressedLocalityVector, new byte[] { EOM }, compressedLSBs, Encoding.ASCII.GetBytes(payload));
            bool[] bits = embeddingStream.SelectMany(GetBits).ToArray();

            int bitNumber = 0;
            for(int p = 0; p < setsIds.GetLength(0) && bitNumber < bits.Length; p++)
            {
                for(int q = 0; q < setsIds.GetLength(1) && bitNumber < bits.Length; q++)
                {
                    if (setsIds[p, q] == SetName.EZ || setsIds[p, q] == SetName.EN1)
                    {
                        differences[p, q] = 2 * differences[p, q] + GetIntValue(bits[bitNumber]);
                        bitNumber++;
                    }
                    else if (setsIds[p, q] == SetName.EN2 || setsIds[p, q] == SetName.CN)
                    {
                        differences[p, q] = 2 * (differences[p, q] / 2) + GetIntValue(bits[bitNumber]);
                        bitNumber++;
                    }
                }
            }

            return GenerateEmbeddedImage(image, averages, differences, direction);
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
            return (x + y) / 2;
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
            return Math.Abs(2 * (differences[i, j] / 2) + 0) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1) &&
                Math.Abs(2 * (differences[i, j] / 2) + 1) <= Math.Min(2 * (255 - averages[i, j]), 2 * averages[i, j] + 1);
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
            int count = values.Count / 4;
            int reminder = values.Count % 4;
            int i = 0;
            for(; i < count; i++)
            {
                leastSignificantBits.Add((byte)((values[i] & 1) + (values[i + 1] & 1) * 2 + (values[i + 2] & 1) * 4 + (values[i + 3] & 1) * 8));
            }
            switch(reminder)
            {
                case 1:
                    leastSignificantBits.Add((byte)(values[i] & 1));
                    break;
                case 2:
                    leastSignificantBits.Add((byte)((values[i] & 1) + (values[i + 1] & 1)));
                    break;
                case 3:
                    leastSignificantBits.Add((byte)((values[i] & 1) + (values[i + 1] & 1) * 2 + (values[i + 2] & 1) * 4));
                    break;
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
                    int XR = averages[y, x] + (newDifferences[y, x] + 1) / 2;
                    image.SetPixel(2 * x, y, Color.FromArgb(currentXColor.A, XR, currentXColor.G, currentXColor.B));
                    int YR = averages[y, x] - newDifferences[y, x] / 2;
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
                    int XR = averages[y, x] + (newDifferences[y, x] + 1) / 2;
                    image.SetPixel(x, 2 * y, Color.FromArgb(currentXColor.A, XR, currentXColor.G, currentXColor.B));
                    int YR = averages[y, x] - newDifferences[y, x] / 2;
                    Color currentYColor = image.GetPixel(x, 2 * y + 1);
                    image.SetPixel(x, 2 * y + 1, Color.FromArgb(currentYColor.A, YR, currentYColor.G, currentYColor.B));
                }
            }
            return image;
        }

        public Tuple<Bitmap, String> Decode(Bitmap encodedImage)
        {
            throw new NotImplementedException();
        }

    }

    enum Direction
    {
        Horizontal,
        Vertical
    }

    enum SetName
    {
        EZ,
        EN1,
        EN2,
        CN,
        NC
    }

}
