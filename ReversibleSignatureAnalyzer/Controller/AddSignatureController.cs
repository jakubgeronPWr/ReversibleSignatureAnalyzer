using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ReversibleSignatureAnalyzer.Controller
{
    public class AddSignatureController
    {
        public BitmapImage GetWatermarkedImage(BitmapImage bitmapImage, String payload, IReversibleWatermarkingAlgorithm algorithm)
        {
            Bitmap originalBitmap = BitmapImageToBitmap(bitmapImage);
            Bitmap encodedBitmap = algorithm.Encode(originalBitmap, payload);
            return BitmapToBitmapImage(encodedBitmap);
        }

        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);
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
    }
}
