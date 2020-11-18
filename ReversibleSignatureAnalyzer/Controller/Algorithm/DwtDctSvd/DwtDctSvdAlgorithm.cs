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
            
            var iterations = 4;
            _dwtHandler.OriginalImage = inputImage;
            _dwtHandler.ApplyHaarTransform(true, true, iterations);
            //_dwtHandler.OriginalImage = _dwtHandler.TransformedImage;
            //dwtHandler.ApplyHaarTransform(false, true, "2");

            var imageAfterDwt = _dwtHandler.TransformedImage;

            Bitmap smallImage = new Bitmap(imageAfterDwt.Width/(iterations*2), imageAfterDwt.Height/(iterations*2));

            for (int i = 0; i < imageAfterDwt.Height / (iterations*2); i++)
            {
                for (int j = 0; j < imageAfterDwt.Width / (iterations * 2); j++)
                {
                    Color color = imageAfterDwt.GetPixel(j, i);
                    //Debug.WriteLine($"Value of color{ color }");
                    smallImage.SetPixel(j, i, color);
                }
            }

            var payloadDoubleArray2D =
                _stringArrayConverter.ArrayPayloadTo2DArray(_stringArrayConverter.StringPayloadToArray(payload),
                    smallImage.Width, smallImage.Width);

            var smallImg2DMatrices = _imageArrayConverter.BitmapToMatrices(smallImage);

            var smallImageArrayWatermarked = _svdHandler.HidePayloadSVD(smallImg2DMatrices, payloadDoubleArray2D, "red");

            var smallImageWatermarked = _imageArrayConverter.MatricesToBitmap(smallImageArrayWatermarked);

            Debug.WriteLine("");
            Debug.WriteLine($"Value of image after dwt / 2* iterations Height { imageAfterDwt.Height / (iterations * 2) }");
            Debug.WriteLine($"Value of small image watermarked  Height { smallImageWatermarked.Height }");
            for (int i = 0; i < smallImageWatermarked.Height; i++)
            {
                for (int j = 0; j < smallImageWatermarked.Width; j++)
                {
                    Color color = smallImageWatermarked.GetPixel(j, i);
                    //Debug.WriteLine($"Value of color{ color }");
                    imageAfterDwt.SetPixel(j, i, color);
                }
            }


            _dwtHandler.TransformedImage = imageAfterDwt;
            _dwtHandler.ApplyHaarTransform(false, true, iterations);
            var imageWatermarked = _dwtHandler.OriginalImage;
            //_dctHandler = new DctAlgorithm(inputImage.Width, inputImage.Height);
            //var arrayAfterDwt = _dctHandler.BitmapToMatrices(_dwtHandler.OriginalImage);
            //var arrayAfterDct = _dctHandler.DCTMatrices(arrayAfterDwt);

            
            //var newImage = converter.Array2DToBitmap(doubleArray);
            //var newImage = _dwtHandler.OriginalImage;
            //var newImage = _dwtHandler.TransformedImage;
            //var newImage = _dctHandler.MatricesToBitmap(arrayAfterDct);
            var newImage = imageWatermarked;
            Debug.WriteLine($"image with water mark dimensions: {newImage.Width} x {newImage.Height}");

            //MessageBox.Show($"Meassures : {newImage.Width}, {newImage.Height}");

            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            if (OriginalImage != null)
            {
                var iterations = 4;
                var payload = "";
                _dwtHandler.OriginalImage = encodedImage;
                _dwtHandler.ApplyHaarTransform(true, true, iterations);
                Bitmap imageWatermarkedAfterDwt = _dwtHandler.TransformedImage.Clone() as Bitmap;
                _dwtHandler.OriginalImage = OriginalImage;
                _dwtHandler.ApplyHaarTransform(true, true, iterations);
                Bitmap imageOriginalAfterDwt = _dwtHandler.TransformedImage.Clone() as Bitmap;

                Bitmap smallOriginalImage = new Bitmap(imageOriginalAfterDwt.Width / (iterations * 2), imageOriginalAfterDwt.Height / (iterations * 2));

                for (int i = 0; i < imageOriginalAfterDwt.Height / (iterations * 2); i++)
                {
                    for (int j = 0; j < imageOriginalAfterDwt.Width / (iterations * 2); j++)
                    {
                        Color color = imageOriginalAfterDwt.GetPixel(j, i);
                        //Debug.WriteLine($"Value of color{ color }");
                        smallOriginalImage.SetPixel(j, i, color);
                    }
                }

                Bitmap smallWatermarkedImage = new Bitmap(imageWatermarkedAfterDwt.Width / (iterations * 2), imageWatermarkedAfterDwt.Height / (iterations * 2));

                for (int i = 0; i < imageWatermarkedAfterDwt.Height / (iterations * 2); i++)
                {
                    for (int j = 0; j < imageWatermarkedAfterDwt.Width / (iterations * 2); j++)
                    {
                        Color color = imageWatermarkedAfterDwt.GetPixel(j, i);
                        //Debug.WriteLine($"Value of color{ color }");
                        smallWatermarkedImage.SetPixel(j, i, color);
                    }
                }


                var smallImgWatermarked2DMatrices = _imageArrayConverter.BitmapToMatrices(smallWatermarkedImage);
                var smallImgOriginal2DMatrices = _imageArrayConverter.BitmapToMatrices(smallOriginalImage);

                var payloadArray =
                    _svdHandler.ExtractPayloadSVD(smallImgWatermarked2DMatrices, smallImgOriginal2DMatrices, "red");

                payload = _stringArrayConverter.ArrayPayloadToString(_stringArrayConverter.Array2DPayloadToArray(payloadArray));

                return new Tuple<Bitmap, string>(OriginalImage, payload);
            }
            else
            {
                throw new ArgumentNullException("OriginalImage", "No original image");
            }
        }



        


    }
}
