using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtDctSvdAlgorithm : IReversibleWatermarkingAlgorithm
    {
        private readonly ImageArrayConverter _imageArrayConverter;
        private readonly StringToArrayConverter _stringArrayConverter;
        private readonly DwtAlgorithm _dwtHandler;
        private DctAlgorithm _dctHandler;
        private SvdAlgorithm _svdHandler;
        private int _iterations;
        private QuarterSymbol _quarter;

        public Bitmap OriginalImage { get; set; }

        public DwtDctSvdAlgorithm()
        {
            _imageArrayConverter = new ImageArrayConverter();
            _stringArrayConverter = new StringToArrayConverter();
            _dwtHandler = new DwtAlgorithm();
            _svdHandler = new SvdAlgorithm();
        }

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration algconfig)
        {

            _iterations = 2;
            _quarter = QuarterSymbol.HH;
            _dwtHandler.OriginalImage = inputImage;
            _dwtHandler.ApplyHaarTransform(true, true, _iterations);
            //_dwtHandler.OriginalImage = _dwtHandler.TransformedImage;
            //dwtHandler.ApplyHaarTransform(false, true, "2");

            var imageAfterDwt = _dwtHandler.TransformedImage;

            //Bitmap smallImage = new Bitmap(imageAfterDwt.Width/(iterations*2), imageAfterDwt.Height/(iterations*2));

            //for (int i = (imageAfterDwt.Height / (iterations * 2)) ; i < imageAfterDwt.Height / ((iterations * 2)-1*iterations) ; i++)
            //{
            //    for (int j = (imageAfterDwt.Width / (iterations * 2)) ; j < imageAfterDwt.Width / ((iterations * 2)-1*iterations); j++)
            //    {
            //        var width = j - (imageAfterDwt.Width / (iterations * 2));
            //        var height = i - (imageAfterDwt.Height / (iterations * 2));
            //        Color color = imageAfterDwt.GetPixel(j, i);
            //        //Debug.WriteLine($"Value of color{ color }");
            //        smallImage.SetPixel(width, height, color);
            //    }
            //}

            Bitmap smallImage = ExtractQuarter(imageAfterDwt, _iterations, _quarter);

            var payloadDoubleArray2D =
                _stringArrayConverter.ArrayPayloadTo2DArray(_stringArrayConverter.StringPayloadToArray(payload),
                    smallImage.Width, smallImage.Width);

            var smallImg2DMatrices = _imageArrayConverter.BitmapToMatrices(smallImage);

            var smallImageArrayWatermarked = _svdHandler.HidePayloadSVD(smallImg2DMatrices, payloadDoubleArray2D, "red");

            var smallImageWatermarked = _imageArrayConverter.MatricesToBitmap(smallImageArrayWatermarked);

            Debug.WriteLine("");
            Debug.WriteLine($"Value of image Height after dwt / 2* iterations Height { imageAfterDwt.Height / Convert.ToInt32(Math.Pow(2, _iterations)) }");
            Debug.WriteLine($"Value of small image watermarked  Height { smallImageWatermarked.Height }");
            for (int i = 0; i < smallImageWatermarked.Height; i++)
            {
                for (int j = 0; j < smallImageWatermarked.Width; j++)
                {
                    Color color = smallImageWatermarked.GetPixel(j, i);
                    //Debug.WriteLine($"Value of color{ color }");
                    imageAfterDwt.SetPixel(j + (imageAfterDwt.Width / Convert.ToInt32(Math.Pow(2, _iterations))), i + (imageAfterDwt.Height / Convert.ToInt32(Math.Pow(2, _iterations))), color);
                }
            }


            _dwtHandler.TransformedImage = imageAfterDwt;
            _dwtHandler.ApplyHaarTransform(false, true, _iterations);
            var imageWatermarked = _dwtHandler.OriginalImage;
            //_dctHandler = new DctAlgorithm(inputImage.Width, inputImage.Height);
            //var arrayAfterDwt = _dctHandler.BitmapToMatrices(_dwtHandler.OriginalImage);
            //var arrayAfterDct = _dctHandler.DCTMatrices(arrayAfterDwt);


            //var newImage = converter.Array2DToBitmap(doubleArray);
            //var newImage = _dwtHandler.OriginalImage;
            //var newImage = _dwtHandler.TransformedImage;
            //var newImage = _dctHandler.MatricesToBitmap(arrayAfterDct);
            //var newImage = smallImage;
            var newImage = imageWatermarked;
            Debug.WriteLine($"image with water mark dimensions: {newImage.Width} x {newImage.Height}");

            //MessageBox.Show($"Meassures : {newImage.Width}, {newImage.Height}");

            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            _iterations = 2;
            var quarter = QuarterSymbol.HH;
            var channel = "red";
            var payload = "";
            _dwtHandler.OriginalImage = encodedImage;
            _dwtHandler.ApplyHaarTransform(true, true, _iterations);
            Bitmap imageWatermarkedAfterDwt = _dwtHandler.TransformedImage.Clone() as Bitmap;
            //_dwtHandler.OriginalImage = OriginalImage;
            //_dwtHandler.ApplyHaarTransform(true, true, _iterations);
            //Bitmap imageOriginalAfterDwt = _dwtHandler.TransformedImage.Clone() as Bitmap;

            //Bitmap smallOriginalImage = ExtractQuarter(imageOriginalAfterDwt, _iterations, quarter);

            Bitmap smallWatermarkedImage = ExtractQuarter(imageWatermarkedAfterDwt, _iterations, quarter);


            var smallImgWatermarked2DMatrices = _imageArrayConverter.BitmapToMatrices(smallWatermarkedImage);
            //var smallImgOriginal2DMatrices = _imageArrayConverter.BitmapToMatrices(smallOriginalImage);

            var originalImage2dAndPayload =
                _svdHandler.ExtractPayloadSVD(smallImgWatermarked2DMatrices, channel);

            var payload2d = originalImage2dAndPayload.Item2;

            payload = _stringArrayConverter.ArrayPayloadToString(_stringArrayConverter.Array2DPayloadToArray(originalImage2dAndPayload.Item2));

            //var origin

            //var originImg = _imageArrayConverter.MatricesToBitmap();
            Debug.WriteLine($"payload : {payload}");

            return new Tuple<Bitmap, string>(encodedImage, payload);
        }

        private Bitmap ExtractQuarter(Bitmap imageToSplit, int iterations, QuarterSymbol quarterSymbol)
        {
            var quarterImage = new Bitmap(imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations)), imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations)));
            int widthShift = 0;
            int heightShift = 0;
            int widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations));
            int heightShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations));

            switch (quarterSymbol)
            {
                case QuarterSymbol.HH:
                    widthShift = Convert.ToInt32(Math.Ceiling(imageToSplit.Width / Math.Pow(2, _iterations)));
                    heightShift = Convert.ToInt32(Math.Ceiling(imageToSplit.Height / Math.Pow(2, _iterations)));
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    break;
                case QuarterSymbol.HL:
                    widthShift = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShift = 0;
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations));
                    break;
                case QuarterSymbol.LH:
                    widthShift = 0;
                    heightShift = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations));
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    break;
                case QuarterSymbol.LL:
                    widthShift = 0;
                    heightShift = 0;
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations));
                    break;
            }

            for (int i = heightShift; i < heightShiftEnd; i++)
            {
                for (int j = widthShift; j < widthShiftEnd; j++)
                {
                    var width = j - widthShift;
                    var height = i - heightShift;
                    Color color = imageToSplit.GetPixel(j, i);
                    quarterImage.SetPixel(width, height, color);
                }
            }

            return quarterImage;

        }

        public enum QuarterSymbol
        {
            LL,
            HH,
            LH,
            HL,
        }
    }
}
