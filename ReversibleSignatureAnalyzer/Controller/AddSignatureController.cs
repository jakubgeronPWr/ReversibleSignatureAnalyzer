using ReversibleSignatureAnalyzer.Controller.Algorithm;
using ReversibleSignatureAnalyzer.Model;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ReversibleSignatureAnalyzer.Controller
{
    public class AddSignatureController
    {
        public BitmapImage GetWatermarkedImage(BitmapImage bitmapImage, string payload, IReversibleWatermarkingAlgorithm algorithm, AlgorithmConfiguration configuration)
        {
            Bitmap originalBitmap = BitmapImageToBitmap(bitmapImage);
            Bitmap encodedBitmap = algorithm.Encode(originalBitmap, payload, configuration);
            return BitmapToBitmapImage(encodedBitmap);
        }

        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new System.Drawing.Bitmap(outStream);
                return new Bitmap(bitmap);
            }
        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public Tuple<BitmapImage, string> GetDecodedImage(BitmapImage bitmapImage, IReversibleWatermarkingAlgorithm algorithm, AlgorithmConfiguration configuration)
        {
            Bitmap encodedImage = BitmapImageToBitmap(bitmapImage);
            Tuple<Bitmap, string> decodingResult = algorithm.Decode(encodedImage, configuration);
            return new Tuple<BitmapImage, string>(BitmapToBitmapImage(decodingResult.Item1), decodingResult.Item2);
        }

    }
}
