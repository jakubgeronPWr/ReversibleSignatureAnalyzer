
using ReversibleSignatureAnalyzer.Controller.Algorithm;
using ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion;
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

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration configuration)
        {
            DifferencesExpansionConfiguration config = (DifferencesExpansionConfiguration) configuration;
            Bitmap image = new Bitmap(inputImage);
            // For multiple embedding channels payload to encode is divided into chunks - one for each RGB channel
            List<string> payloadChunks = GetPayloadChunks(payload, config.EmbeddingChanels);
            int chunkNumber = 0;
            foreach(EmbeddingChanel embeddingChanel in Enum.GetValues(typeof(EmbeddingChanel))) {
                if (config.EmbeddingChanels.Contains(embeddingChanel))
                {
                    // Encoding of payload chunk performed for given channel of RGB
                    EncodePayloadIntoImage(image, payloadChunks[chunkNumber++], config.EmbeddingDirection, config.Threeshold, embeddingChanel);
                }
            }
            return image;
        }

        private List<string> GetPayloadChunks(String payload, HashSet<EmbeddingChanel> embeddingChanels)
        {
            int numberOfChannels = embeddingChanels.Count;
            if (numberOfChannels == 0)
            {
                throw new ArgumentException("At least one embedding channel have to be selected");
            }
            // Calculation of base chunks size, note that for non-divisable payload size chunks can have differnet sizes
            // For very short payloads chunks for second and third channel can be empty
            int chunkSizeForChannel = payload.Length / numberOfChannels;
            List<string> payloadChunks = new List<string>(numberOfChannels);
            int i = 0;
            for (; i < numberOfChannels - 1; i++)
            {
                payloadChunks.Add(payload.Substring(i * chunkSizeForChannel, chunkSizeForChannel));
            }
            payloadChunks.Add(payload.Substring(i * chunkSizeForChannel));
            return payloadChunks;
        }

        static IEnumerable<string> GetChunks(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        private void EncodePayloadIntoImage(Bitmap image, string payload, Direction embeddingDirection, int threeshold, EmbeddingChanel embeddingChanel)
        {
            // Calculation of averages for pixel pairs l = (x + y) / 2
            // Depending on embedding direction below arrays has size [image.Height, image.Width / 2] or [image.Height / 2, image.Width]
            int[,] averages = Calculate(image, CalculateAverage, embeddingDirection, embeddingChanel);
            // Calcualtion of differences for pixel pairs h = x - y
            int[,] differences = Calculate(image, CalculateDifference, embeddingDirection, embeddingChanel);
            // Preparing array for locality map containting 1 if pixel pair belongs to set EZ and 0 if it belongs to set EN1
            int[,] localityMap = GetEmptyArrayAdjustedToImageSize(image, embeddingDirection);
            // Indicates to which encoding set pixel pair belongs
            EncodingSetName[,] setsIds = new EncodingSetName[localityMap.GetLength(0), localityMap.GetLength(1)];
            int EZSize = 0;
            int EN1Size = 0;
            List<int> EN2AndCN = new List<int>();
            // Divison of pixel pairs into 5 encoding sets (EZ, EN1, EN2, CN, NC) and construction of locality map.
            // We also store difference values for pixel pairs belonging to EN2 and CN.
            for (int i = 0; i < setsIds.GetLength(0); i++)
            {
                for (int j = 0; j < setsIds.GetLength(1); j++)
                {
                    if (IsExpandable(i, j, differences, averages))
                    {
                        if (differences[i, j] == 0 || differences[i, j] == -1)
                        {
                            // Pixel pairs with expandable differences with values equal 0 or -1 are 
                            // assigned to EZ - expandable zeros set
                            localityMap[i, j] = 1;
                            EZSize++;
                            setsIds[i, j] = EncodingSetName.EZ;
                        }
                        else if (Math.Abs(differences[i, j]) <= threeshold)
                        {
                            // Pixel pairs with expandable differences with values other than 0 or -1 
                            // but with abs less than configured threeshold are assigned to EN1 set
                            localityMap[i, j] = 1;
                            EN1Size++;
                            setsIds[i, j] = EncodingSetName.EN1;
                        }
                        else
                        {
                            // Pixel pairs with expandable differences with values other than 0 or -1
                            // and with abs greater than configured threeshold are assigned to EN2 set
                            EN2AndCN.Add(differences[i, j]);
                            localityMap[i, j] = 0;
                            setsIds[i, j] = EncodingSetName.EN2;
                        }
                    }
                    else if (IsChangeable(i, j, differences, averages))
                    {
                        // Pixel pairs with non-expandable but changable differences are assigned to CN set
                        EN2AndCN.Add(differences[i, j]);
                        localityMap[i, j] = 0;
                        setsIds[i, j] = EncodingSetName.CN;
                    }
                    else
                    {
                        // Non-expandable and non-changeable pixel pairs are assigned to NC set
                        localityMap[i, j] = 0;
                        setsIds[i, j] = EncodingSetName.NC;
                    }
                }
            }

            // For convinience of performing operations localityMap is hold as int array, but their values are
            // always 0 or 1 so convert int array to stream of last signifacant bits and divide those bits into bytes
            // so after operation one byte holds values of 8 subsequent LSBs
            List<byte> localityVector = GetLeastSignificatBits(ConvertToList(localityMap));

            // differnece with values -2 and 1 can be restored on decoding and there is no need to save them
            List<int> filteredEN2AndCN = EN2AndCN.Where(x => x != -2 && x != 1).ToList();
            // We obtain lsb of differences of pixel pairs from set EN2 and CN
            List<byte> LSBs = GetLeastSignificatBits(filteredEN2AndCN);

            int embeddingCapacity = EZSize + EN1Size + EN2AndCN.Count;

            // Calculation of locality map bits stream size
            byte[] localityVectorSize = BitConverter.GetBytes((uint)localityVector.Count);
            // Calculation of bit stream size for original LSBs of differences for pixel pairs from EN2 and CN
            byte[] LSBsSize = BitConverter.GetBytes((uint)LSBs.Count);
            // Calculation of payload size
            byte[] payloadSize = BitConverter.GetBytes(payload.Length);
            // Stroing information about sizes in header, this informations will be retrived on decoding
            byte[] header = ConcatArrays(localityVectorSize, LSBsSize, payloadSize);
            // Bits of locality map, LSBs and payload represented as byte array
            byte[] data = ConcatArrays(localityVector.ToArray(), LSBs.ToArray(), Encoding.ASCII.GetBytes(payload));
            // Compresion of data with run length encoding
            byte[] compressedData = RLE<byte>.Encode(new List<byte>(data)).ToArray();
            // Non-compressed header bits are added at the begining of embedding stream 
            byte[] embeddingStream = ConcatArrays(header, compressedData);
            // For convenience array of bytes (where each byte contains 8 subsequent LSBs) is converted to array of
            // bool, where each value represents LSB 
            bool[] bits = embeddingStream.SelectMany(GetBits).ToArray();

            // Calculation of new difference values - data embedding
            int bitNumber = 0;
            for (int p = 0; p < setsIds.GetLength(0) && bitNumber < bits.Length; p++)
            {
                for (int q = 0; q < setsIds.GetLength(1) && bitNumber < bits.Length; q++)
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
            // Recalculation of pixel values based on new differences values
            RecalculateImagePixels(image, averages, differences, embeddingDirection, embeddingChanel);
        }

        private int[,] GetPixelValues(Bitmap image, EmbeddingChanel embeddingChannel)
        {
            int[,] values = new int[image.Height, image.Width];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    values[y, x] = getValueOfRgbChannel(image.GetPixel(x, y), embeddingChannel);
                }
            }
            return values;
        }

        private byte getValueOfRgbChannel(Color pixel, EmbeddingChanel rgbChannel)
        {
            switch(rgbChannel)
            {
                case EmbeddingChanel.R:
                    return pixel.R;
                case EmbeddingChanel.G:
                    return pixel.G;
                case EmbeddingChanel.B:
                    return pixel.B;
                default:
                    throw new Exception("Assertion error! Illegal RGB channel!");
            }
        }

        private int DivideAndFloor(double number, double divisor)
        {
            return (int) Math.Floor(number / divisor);
        }

        private int [,] Calculate(Bitmap image, Func<int, int, int> operation, Direction direction, EmbeddingChanel embeddingChanel)
        {
            if (direction == Direction.Horizontal)
            {
                return CalculateHorizontally(image, operation, embeddingChanel);
            }
            return CalculateVertically(image, operation, embeddingChanel);
        }

        private int [,] CalculateHorizontally(Bitmap image, Func<int, int, int> operation, EmbeddingChanel embeddingChannel)
        {
            int[,] result = new int[image.Height, image.Width / 2];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width / 2; x++)
                {
                    result[y, x] = operation(getValueOfRgbChannel(image.GetPixel(2 * x, y), embeddingChannel), getValueOfRgbChannel(image.GetPixel(2 * x + 1, y), embeddingChannel));
                }
            }
            return result;
        }

        private int[,] CalculateVertically(Bitmap image, Func<int, int, int> operation, EmbeddingChanel embeddingChanel)
        {
            int[,] result = new int[image.Height / 2, image.Width];
            for (int y = 0; y < image.Height / 2; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    result[y, x] = operation(getValueOfRgbChannel(image.GetPixel(x, 2 * y), embeddingChanel), getValueOfRgbChannel(image.GetPixel(x, 2 * y + 1), embeddingChanel));
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

        private void RecalculateImagePixels(Bitmap image, int[,] averages, int[,] newDifferences, Direction direction, EmbeddingChanel embeddingChanel)
        {
            if (direction == Direction.Horizontal)
            {
                RecalculatePixelsHorizontally(image, averages, newDifferences, embeddingChanel);
            } else
            {
                RecalculatePixelsVertically(image, averages, newDifferences, embeddingChanel);
            }
        }

        private void RecalculatePixelsHorizontally(Bitmap image, int[,] averages, int[,] newDifferences, EmbeddingChanel embeddingChanel)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width / 2; x++)
                {
                    Color currentPixelX = image.GetPixel(2 * x, y);
                    int newX = averages[y, x] + DivideAndFloor(newDifferences[y, x] + 1, 2);
                    image.SetPixel(2 * x, y, getPixelWithNewValueForSpecifiedRgbChannel(currentPixelX, newX, embeddingChanel));
                    int newY = averages[y, x] - DivideAndFloor(newDifferences[y, x], 2);
                    Color currentPixelY = image.GetPixel(2 * x + 1, y);
                    image.SetPixel(2 * x + 1, y, getPixelWithNewValueForSpecifiedRgbChannel(currentPixelY, newY, embeddingChanel));
                }
            }
        }

        private Color getPixelWithNewValueForSpecifiedRgbChannel(Color oldPixel, int newValueForChannel, EmbeddingChanel embeddingChanel)
        {
            switch(embeddingChanel)
            {
                case EmbeddingChanel.R:
                    return Color.FromArgb(oldPixel.A, newValueForChannel, oldPixel.G, oldPixel.B);
                case EmbeddingChanel.G:
                    return Color.FromArgb(oldPixel.A, oldPixel.R, newValueForChannel, oldPixel.B);
                case EmbeddingChanel.B:
                    return Color.FromArgb(oldPixel.A, oldPixel.R, oldPixel.G, newValueForChannel);
                default:
                    throw new Exception("Assertion error! Illegal RGB channel!");
            }
        }

        private void RecalculatePixelsVertically(Bitmap image, int[,] averages, int[,] newDifferences, EmbeddingChanel embeddingChannel)
        {
            for (int y = 0; y < image.Height / 2; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color currentPixelX = image.GetPixel(x, 2 * y);
                    int newX = averages[y, x] + DivideAndFloor(newDifferences[y, x] + 1, 2);
                    image.SetPixel(x, 2 * y, getPixelWithNewValueForSpecifiedRgbChannel(currentPixelX, newX, embeddingChannel));
                    int newY = averages[y, x] - DivideAndFloor(newDifferences[y, x], 2);
                    Color currentPixelY = image.GetPixel(x, 2 * y + 1);
                    image.SetPixel(x, 2 * y + 1, getPixelWithNewValueForSpecifiedRgbChannel(currentPixelY, newY, embeddingChannel));
                }
            }
        }

        private Direction GetChangedEmbeddingDirection(Direction currentDirection)
        {
            return currentDirection == Direction.Horizontal ? Direction.Vertical : Direction.Horizontal;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            Bitmap image = new Bitmap(encodedImage);
            // Depending on configuration standard or brute force decoding is performed
            if (configuration is DifferencesExpansionConfiguration)
            {
                return Decode(image, (DifferencesExpansionConfiguration)configuration);
            }
            if(configuration is DifferenceExpansionBruteForceConfiguration)
            {
                return BruteforceDecode(image, (DifferenceExpansionBruteForceConfiguration)configuration);
            }
            return null;
        }

        private Tuple<Bitmap, string> Decode(Bitmap image, DifferencesExpansionConfiguration config)
        {
            try
            {
                List<string> payloadChunks = new List<string>();
                foreach(EmbeddingChanel embeddingChanel in Enum.GetValues(typeof(EmbeddingChanel))) {
                    if (config.EmbeddingChanels.Contains(embeddingChanel))
                    {
                        // For each RGB elected in configuration chunk of embedded payload is retrieved
                        payloadChunks.Add(DecodeImage(image, config.EmbeddingDirection, embeddingChanel));
                    }
                }
                // Payload from all selected channels are concatenated to build whole message
                string payload = String.Join("", payloadChunks);
                // Payload and oryginal image are returned
                return new Tuple<Bitmap, string>(image, payload);
            }
            catch
            {

            }
            return null;
        }

        private string DecodeImage(Bitmap image, Direction embeddingDirection, EmbeddingChanel embeddingChannel)
        {
            // Calculation of averages for pixel pairs l = (x + y) / 2
            // Depending on embedding direction below arrays has size [image.Height, image.Width / 2] or [image.Height / 2, image.Width]
            int[,] averages = Calculate(image, CalculateAverage, embeddingDirection, embeddingChannel);
            // Calcualtion of differences for pixel pairs h = x - y
            int[,] differences = Calculate(image, CalculateDifference, embeddingDirection, embeddingChannel);
            // Preparing array for locality map containting 1 if pixel pair belongs to set EZ and 0 if it belongs to set EN1
            int[,] localityMap = GetEmptyArrayAdjustedToImageSize(image, embeddingDirection);
            // Indicates to which of two decoding set pixel pair belongs
            DecodingSetName[,] setsIds = new DecodingSetName[localityMap.GetLength(0), localityMap.GetLength(1)];
            List<int> changeableDifferences = new List<int>();

            // Pixel pairs are dividen into two sets - pixel pairs with changeable differences (CH)
            // and non-changeable differences (NC). Changeable differences contains data embedded during
            // encoding (it corresponds to encoding sets EZ, EN1, EN2, CN)
            for (int i = 0; i < setsIds.GetLength(0); i++)
            {
                for (int j = 0; j < setsIds.GetLength(1); j++)
                {
                    if (IsChangeable(i, j, differences, averages))
                    {
                        // Pixel pairs with changeable differences are assigned to CH set and
                        // its difference value is collected
                        changeableDifferences.Add(differences[i, j]);
                        setsIds[i, j] = DecodingSetName.CH;
                    }
                    else
                    {
                        // Pixel pairs with non-changeable differences are assigned to NC set
                        setsIds[i, j] = DecodingSetName.NC;
                    }
                }
            }
            // Changeable differences are converted to stream of LSBs represented as byte list
            // each byte holds 8 subsequent LSBs. This list contains data embedded during encoding.
            List<byte> LSBs = GetLeastSignificatBits(changeableDifferences);
            // At the beginging of data there is header with sizes of locality map
            // oryginal LSBs and payload
            byte[] localityVectorSizeBytes = LSBs.GetRange(0, 4).ToArray();
            byte[] originalLSBsSizeBytes = LSBs.GetRange(4, 4).ToArray();
            byte[] payloadSizeBytes = LSBs.GetRange(8, 4).ToArray();
            // locality map, original LSBs, and payload are compressed and placed 12 bits after header
            byte[] compressedData = LSBs.GetRange(12, LSBs.Count - 12).ToArray();
            // bytes are interpreted as int values representing size
            int localityVectorSize = BitConverter.ToInt32(localityVectorSizeBytes, 0);
            int originalLSBsSize = BitConverter.ToInt32(originalLSBsSizeBytes, 0);
            int payloadSize = BitConverter.ToInt32(payloadSizeBytes, 0);
            // to retrieve locality map, original LSBs and payload we perform decompression
            byte[] originalData = RLE<byte>.Decode(compressedData).ToArray();
            // after decporession original data is dividen into locality map, orginal LSBs and payload
            byte[] localityVector = new List<byte>(originalData).GetRange(0, localityVectorSize).ToArray();
            byte[] originalLSBsVector = new List<byte>(originalData).GetRange(localityVectorSize, originalLSBsSize).ToArray();
            byte[] payload = new List<byte>(originalData).GetRange(localityVectorSize + originalLSBsSize, payloadSize).ToArray();
            // For convenience locality map, original LSBs and paload bits are represented as bool 
            // conversion from byte array (each element containing 8 subsequent LSBs) is performed
            bool[] localityVectorBits = new List<byte>(localityVector).SelectMany(GetBits).ToArray();
            bool[] originalLSBsVectorBits = new List<byte>(originalLSBsVector).SelectMany(GetBits).ToArray();
            bool[] payloadBits = new List<byte>(payload).SelectMany(GetBits).ToArray();
            // Calculation of original differences values
            int bitNumber = 0;
            for (int i = 0; i < differences.GetLength(0); i++)
            {
                for (int j = 0; j < differences.GetLength(1); j++)
                {
                    if (setsIds[i, j] == DecodingSetName.CH)
                    {
                        if (localityVectorBits[i * differences.GetLength(1) + j])
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
            // Recalculation of pixel values based on new values of differences
            RecalculateImagePixels(image, averages, differences, embeddingDirection, embeddingChannel);
            return Encoding.ASCII.GetString(payload);
        }

        public Tuple<Bitmap, string> BruteforceDecode(Bitmap inputImage, DifferenceExpansionBruteForceConfiguration bruteForceConfiguration)
        {
            Bitmap processedImage = inputImage;
            List<string> payloadChunks = new List<string>();
            foreach (EmbeddingChanel embeddingChannel in Enum.GetValues(typeof(EmbeddingChanel)))
            {
                if(bruteForceConfiguration.EmbeddingChanels.Contains(embeddingChannel))
                {
                    // For each RGB channel specified in configuration algorithm tries to perform decoding
                    Tuple<Bitmap, string> imageAndPayload = TryDecodeImageForChannel(processedImage, embeddingChannel, bruteForceConfiguration);
                    if (isDecodingSuccessfull(imageAndPayload))
                    {
                        // If decoding was successfull then retrived chunk of payload from given channel is saved
                        // If there was no success next embedding channel is used
                        processedImage = imageAndPayload.Item1;
                        payloadChunks.Add(imageAndPayload.Item2);
                    }
                }
            }
            return payloadChunks.Count > 0 ? new Tuple<Bitmap, string>(processedImage, String.Join("", payloadChunks)) : null;
        }

        public Tuple<Bitmap, string> TryDecodeImageForChannel(Bitmap inputImage, EmbeddingChanel embeddingChanel, DifferenceExpansionBruteForceConfiguration bruteForceConfiguration)
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if(bruteForceConfiguration.EmbeddingDirections.Contains(direction))
                {
                    // For given RGB channel algorith tries to decode payload with each embedding direction allowed in configuration
                    Tuple<Bitmap, string> imageAndPayload = TryDecodeImageForDirectionAndChannel(inputImage, Direction.Horizontal, embeddingChanel);
                    if (isDecodingSuccessfull(imageAndPayload))
                    {
                        // If decoding with given embedding channel and direction was successfull then image and payload chunk is returned
                        // If there was no success other embedding direction for given channel is used
                        return imageAndPayload;
                    }
                }
            }
            return null;
        }

        private bool isDecodingSuccessfull(Tuple<Bitmap, string> imageAndPayload)
        {
            return imageAndPayload != null;
        }

        public Tuple<Bitmap, string> TryDecodeImageForDirectionAndChannel(Bitmap inputImage, Direction embeddingDirection, EmbeddingChanel embeddingChannel)
        {
            Bitmap image = new Bitmap(inputImage);
            try
            {
                // If no error occurs then decoding is successfull and image and payload chunk is returned
                return new Tuple<Bitmap, string>(image, DecodeImage(image, embeddingDirection, embeddingChannel));
            }
            catch
            {

            }
            // In case of failure exception is supressed and null value (failure indicator) is returned
            return null;
        }

    }

    public enum Direction
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
