﻿using System;
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
using Accord.IO;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtDctSvdAlgorithm : IReversibleWatermarkingAlgorithm
    {
        private readonly ImageArrayConverter _imageArrayConverter;
        private readonly StringToArrayConverter _stringArrayConverter;
        private readonly MatrixService _matrixService;
        private readonly DwtPrecisionAlgorithm _dwtHandler;
        private DctAlgorithm _dctHandler;
        private SvdAlgorithm _svdHandler;
        private int _iterations;
        private QuarterSymbol _quarter;
        private EmbeddingChanel _channel;

        public Bitmap OriginalImage { get; set; }

        public DwtDctSvdAlgorithm()
        {
            _imageArrayConverter = new ImageArrayConverter();
            _stringArrayConverter = new StringToArrayConverter();
            _matrixService = new MatrixService();
            _dwtHandler = new DwtPrecisionAlgorithm();
            _svdHandler = new SvdAlgorithm();
            _iterations = 1;
            _quarter = QuarterSymbol.HH;
            _channel = EmbeddingChanel.R;
        }

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration algconfig)
        {
            var config = algconfig as DwtSvdConfiguration;
            SetChannelFromConfiguration(config);
            SetQuarterFromConfiguration(config);
            decimal[][,] inputDoubles = _imageArrayConverter.BitmapToPrecisionMatrices(inputImage.DeepClone());
            _dwtHandler.OriginalImage = inputDoubles.DeepClone();
            _dwtHandler.ApplyHaarTransform(true, true, _iterations);

            var imageAfterDwt = _dwtHandler.TransformedImage.DeepClone();
            decimal[][,] quarter = ExtractQuarter(imageAfterDwt, _iterations, _quarter);

            var payloadDoubleVector = _stringArrayConverter.StringPayloadToArray(payload);

            //var smallImg2DMatrices = _imageArrayConverter.BitmapToMatrices(smallImage);
            var quarterDouble = _matrixService.DecimalToDouble(quarter);

            var smallMatrixWatermarked = _svdHandler.HidePayloadSVD(quarterDouble, payloadDoubleVector, _channel);

           var smallMatrixPrecisionWatermarked = _matrixService.DoubleToDecimal(smallMatrixWatermarked);

            var bigImageWatermarked = MergeQuarter(smallMatrixPrecisionWatermarked, imageAfterDwt, _iterations, _quarter);

            //var smallImageWatermarked = _imageArrayConverter.MatricesToBitmap(smallArrayWatermarked);

            Debug.WriteLine("");
            Debug.WriteLine($"Value of image Height after dwt / 2* iterations Height: { imageAfterDwt[0].GetLength(1) / Convert.ToInt32(Math.Pow(2, _iterations)) }");
            //Debug.WriteLine($"Value of small image watermarked  Height: { smallImageWatermarked.Height }");
            //for (int i = 0; i < smallImageWatermarked.Height; i++)
            //{
            //    for (int j = 0; j < smallImageWatermarked.Width; j++)
            //    {
            //        Color color = smallImageWatermarked.GetPixel(j, i);
            //        //Debug.WriteLine($"Value of color{ color }");
            //        imageAfterDwt.SetPixel(j + (imageAfterDwt.Width / Convert.ToInt32(Math.Pow(2, _iterations))), i + (imageAfterDwt.Height / Convert.ToInt32(Math.Pow(2, _iterations))), color);
            //    }
            //}

            _dwtHandler.OriginalImage = null;
            _dwtHandler.TransformedImage = null;
            _dwtHandler.TransformedImage = bigImageWatermarked;
            _dwtHandler.ApplyHaarTransform(false, true, _iterations);
            var newMatrix = _dwtHandler.OriginalImage;

            //_dctHandler = new DctAlgorithm(inputImage.Width, inputImage.Height);
            //var arrayAfterDwt = _dctHandler.BitmapToMatrices(_dwtHandler.OriginalImage);
            //var arrayAfterDct = _dctHandler.DCTMatrices(arrayAfterDwt);

            //var newImage = imageAfterDwt;
            //var newImage = converter.Array2DToBitmap(doubleArray);
            var newImage = _imageArrayConverter.MatricesPrecisionToBitmap(newMatrix);  //IMAGE WATERMARKED
            //var newImage = _dwtHandler.TransformedImage;
            //var newImage = _dctHandler.MatricesToBitmap(arrayAfterDct);
            //var newImage = smallImage;

            Debug.WriteLine($"image with water mark dimensions: {newImage.Width} x {newImage.Height}");

            var areEquals = AreEquals(inputImage, newImage);

            Debug.WriteLine($"Are before and after equals (0 mean equal, -1 different sizes, >0 differences count): {areEquals}");

            //MessageBox.Show($"Meassures : {newImage.Width}, {newImage.Height}");
            _dwtHandler.OriginalImage = null;
            _dwtHandler.TransformedImage = null;

            return newImage;
        }

        private void SetChannelFromConfiguration(DwtSvdConfiguration config)
        {
            if (config.EmbeddingChanels.Contains(EmbeddingChanel.R))
            {
                _channel = EmbeddingChanel.R;
            }
            if (config.EmbeddingChanels.Contains(EmbeddingChanel.G))
            {
                _channel = EmbeddingChanel.G;
            }
            if (config.EmbeddingChanels.Contains(EmbeddingChanel.B))
            {
                _channel = EmbeddingChanel.B;
            }
        }

        private void SetQuarterFromConfiguration(DwtSvdConfiguration config)
        {
            if (config.QuarterSymbol.Contains(QuarterSymbol.HH))
            {
               _quarter = QuarterSymbol.HH;
            }
            if (config.QuarterSymbol.Contains(QuarterSymbol.LL))
            {
                _quarter = QuarterSymbol.HH;
            }
            if (config.QuarterSymbol.Contains(QuarterSymbol.LH))
            {
                _quarter = QuarterSymbol.HH;
            }
            if (config.QuarterSymbol.Contains(QuarterSymbol.HL))
            {
                _quarter = QuarterSymbol.HL;
            }
        }


        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            var payload = "";
            decimal[][,] inputDoubles = _imageArrayConverter.BitmapToPrecisionMatrices(encodedImage.DeepClone());
            _dwtHandler.OriginalImage = inputDoubles.DeepClone();
            _dwtHandler.ApplyHaarTransform(true, true, _iterations);

            var imageArrayAfterDwt = _dwtHandler.TransformedImage.DeepClone();
            decimal[][,] quarter = ExtractQuarter(imageArrayAfterDwt, _iterations, _quarter);
            var quarterDouble = _matrixService.DecimalToDouble(quarter);

            var originalImage2dAndPayload =
                _svdHandler.ExtractPayloadSVD(quarterDouble, _channel);

            var payload2d = originalImage2dAndPayload.Item2;

            var originalImage = originalImage2dAndPayload.Item1;

            var smallMatrixPrecisionOrigin = _matrixService.DoubleToDecimal(originalImage);

            var bigImageOrigin = MergeQuarter(smallMatrixPrecisionOrigin, imageArrayAfterDwt, _iterations, _quarter);

            _dwtHandler.OriginalImage = null;
            _dwtHandler.TransformedImage = null;
            _dwtHandler.TransformedImage = bigImageOrigin;
            _dwtHandler.ApplyHaarTransform(false, true, _iterations);
            var newMatrix = _dwtHandler.OriginalImage;
            //TODO make original matrix 
            var originImage = _imageArrayConverter.MatricesPrecisionToBitmap(newMatrix);
            payload = _stringArrayConverter.ArrayPayloadToString(_stringArrayConverter.ArrayDiagonalToArray(payload2d));

            Debug.WriteLine("");
            Debug.WriteLine($"payload : {payload}");
            Debug.WriteLine("");
            return new Tuple<Bitmap, string>(originImage, payload);
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
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    break;
                case QuarterSymbol.LH:
                    widthShift = 0;
                    heightShift = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations));
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations -1));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations - 1));
                    break;
                case QuarterSymbol.LL:
                    widthShift = 0;
                    heightShift = 0;
                    widthShiftEnd = imageToSplit.Width / Convert.ToInt32(Math.Pow(2, _iterations-1));
                    heightShiftEnd = imageToSplit.Height / Convert.ToInt32(Math.Pow(2, _iterations-1));
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

        private decimal[][,] ExtractQuarter(decimal[][,] matrixToSplit, int iterations, QuarterSymbol quarterSymbol)
        {
            decimal[][,] origin = matrixToSplit.DeepClone();
            var countOfMatrix = matrixToSplit.GetLength(0);
            var matrixWidth = matrixToSplit[0].GetLength(0);
            var matrixHeight = matrixToSplit[0].GetLength(1);
            //var quarterImage = new decimal[countOfMatrixes][matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations)), matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations))];
            var quarterImage = new decimal[countOfMatrix][,];
            
            int widthShift = 0;
            int heightShift = 0;
            int widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, iterations));
            int heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, iterations));
            for(int i = 0; i < countOfMatrix; i++)
            {
                quarterImage[i] = new decimal[widthShiftEnd, heightShiftEnd];
            }

            switch (quarterSymbol)
            {
                case QuarterSymbol.HH:
                    widthShift = Convert.ToInt32(Math.Ceiling(matrixWidth / Math.Pow(2, iterations)));
                    heightShift = Convert.ToInt32(Math.Ceiling(matrixHeight / Math.Pow(2, iterations)));
                    widthShiftEnd = matrixWidth;
                    heightShiftEnd = matrixHeight;
                    break;
                case QuarterSymbol.HL:
                    widthShift = matrixWidth / Convert.ToInt32(Math.Pow(2, iterations));
                    heightShift = 0;
                    widthShiftEnd = matrixWidth;
                    heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, iterations));
                    break;
                case QuarterSymbol.LH:
                    widthShift = 0;
                    heightShift = matrixHeight / Convert.ToInt32(Math.Pow(2, iterations));
                    widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, iterations));
                    heightShiftEnd = matrixHeight;
                    break;
                case QuarterSymbol.LL:
                    widthShift = 0;
                    heightShift = 0;
                    widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, iterations));
                    heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, iterations));
                    break;
            }

            for (int k = 0; k < countOfMatrix; k++)
            {
                for (int j = heightShift; j < heightShiftEnd; j++)
                {
                    for (int i = widthShift; i < widthShiftEnd; i++)
                    {
                        var width = i - widthShift;
                        var height = j - heightShift;
                        quarterImage[k][width, height] = origin[k][i, j];
                    }
                }
            }
            return quarterImage;

        }


        private decimal[][,] MergeQuarter(decimal[][,] matrixToMerge, decimal[][,] originMatrix, int iterations, QuarterSymbol quarterSymbol)
        {
            var countOfMatrix = originMatrix.GetLength(0);
            var matrixWidth = originMatrix[0].GetLength(0);
            var matrixHeight = originMatrix[0].GetLength(1);
            var quarterImage = new decimal[countOfMatrix][,];
            int widthShift = 0;
            int heightShift = 0;
            int widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations));
            int heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations));

            switch (quarterSymbol)
            {
                case QuarterSymbol.HH:
                    widthShift = Convert.ToInt32(Math.Ceiling(matrixWidth / Math.Pow(2, _iterations)));
                    heightShift = Convert.ToInt32(Math.Ceiling(matrixHeight / Math.Pow(2, _iterations)));
                    widthShiftEnd = matrixWidth;
                    heightShiftEnd = matrixHeight;
                    break;
                case QuarterSymbol.HL:
                    widthShift = matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShift = 0;
                    widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations));
                    break;
                case QuarterSymbol.LH:
                    widthShift = 0;
                    heightShift = matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations));
                    widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations));
                    break;
                case QuarterSymbol.LL:
                    widthShift = 0;
                    heightShift = 0;
                    widthShiftEnd = matrixWidth / Convert.ToInt32(Math.Pow(2, _iterations));
                    heightShiftEnd = matrixHeight / Convert.ToInt32(Math.Pow(2, _iterations));
                    break;
            }

            for (int k = 0; k < countOfMatrix; k++)
            {
                for (int j = heightShift; j < heightShiftEnd; j++)
                {
                    for (int i = widthShift; i < widthShiftEnd; i++)
                    {
                        var width = i - widthShift;
                        var height = j - heightShift;
                        originMatrix[k][i,j] = matrixToMerge[k][width, height];
                    }
                }
            }
            return originMatrix;

        }

        public enum QuarterSymbol
        {
            LL,
            HH,
            LH,
            HL,
        }

        private int AreEquals(Bitmap first, Bitmap second)
        {
            var alpha = false;
            var r = false;
            var g = false;
            var b = false;
            var differences = 0;

            if (first.Height != second.Height || first.Width != second.Width)
            {
                return -1;
            }
            else
            {
                for (int j = 0; j < first.Height; j++)
                {
                    for (int i = 0; i < first.Width; i++)
                    {
                        alpha = first.GetPixel(i, j).A == second.GetPixel(i, j).A;
                        r = first.GetPixel(i, j).R == second.GetPixel(i, j).R;
                        g = first.GetPixel(i, j).G == second.GetPixel(i, j).G;
                        b = first.GetPixel(i, j).B == second.GetPixel(i, j).B;

                        if (!(alpha && r && g && b))
                        {
                            differences++;
                            //Debug.WriteLine($"for pixel on position [{i}, {j}] values are not equal for : a({alpha}), r({r})({first.GetPixel(i, j).R})({second.GetPixel(i, j).R}), g({g})({first.GetPixel(i, j).G})({second.GetPixel(i, j).G}), b({b})({first.GetPixel(i, j).B})({second.GetPixel(i, j).B})");
                        }
                    }
                }

                return differences;
            }
        }

    }
}
