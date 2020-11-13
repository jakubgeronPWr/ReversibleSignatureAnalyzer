using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtDctSvdAlgorithm : IReversibleWatermarkingAlgorithm
    {
        private readonly ImageArrayConverter _converter;
        private readonly DwtAlgorithm _dwtHandler;
        private readonly DctAlgorithm _dctHandler;

        public DwtDctSvdAlgorithm()
        {
            _converter = new ImageArrayConverter();
            _dwtHandler = new DwtAlgorithm();
            _dctHandler = new DctAlgorithm();
        }

        public Bitmap Encode(Bitmap inputImage, string payload)
        {
            
            //Bitmap newImage = new Bitmap(inputImage);
            BitArray bitPayload = new BitArray(Encoding.ASCII.GetBytes(payload));
            var doubleArray = _converter.BitmapToArray2D(inputImage);

            _dwtHandler.OriginalImage = inputImage;
            _dwtHandler.ApplyHaarTransform(true, true, "2");
            _dwtHandler.OriginalImage = _dwtHandler.TransformedImage;
            //dwtHandler.ApplyHaarTransform(false, true, "2");

            var arrayAfterDwt = _converter.BitmapToMatrices(_dwtHandler.OriginalImage);

            var arrayAfterDct = _dctHandler.DCTMatrices(arrayAfterDwt);



            //var newImage = converter.Array2DToBitmap(doubleArray);
            //var newImage = _dwtHandler.OriginalImage;
            var newImage = _converter.MatricesToBitmap(arrayAfterDct);

            return newImage;
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        


    }
}
