using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Accord.IO;
using Accord.Math;
using Accord.Math.Decompositions;
using Debug = System.Diagnostics.Debug;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class SvdAlgorithm
    {
        private double[,] _inMemorySo;
        private double _alpha = 0.05;

        public double[][,] HidePayloadSVD(double[][,] hostMatrics, double[,] payloadMatrix, string channel)
        {
            switch (channel.ToLower())
            {
                case "red" :
                    hostMatrics[0] = HidePayload(hostMatrics[0], payloadMatrix);
                    break;
                case "green":
                    hostMatrics[1] = HidePayload(hostMatrics[1], payloadMatrix);
                    break;
                case "blue":
                    hostMatrics[2] = HidePayload(hostMatrics[2], payloadMatrix);
                    break;
                default:

                    for (int k = 0; k < hostMatrics.Length; k++)
                    {
                        HidePayload(hostMatrics[k], payloadMatrix);
                    }
                    break;
            }

            return hostMatrics;
        }

        public Tuple<double[][,], double[,]> ExtractPayloadSVD(double[][,] watermarkedMatrix, string channel)//double[][,] originalMatrix, string channel)
        {
            var newImage = watermarkedMatrix.DeepClone();
            Tuple<double[][,], double[,]> result = null;

            switch (channel.ToLower())
            {
                case "red":
                    var tempRed = ExtractPayload(watermarkedMatrix[0]);
                    newImage[0] = tempRed.Item1;
                    result = new Tuple<double[][,], double[,]>(newImage, tempRed.Item2);
                    break;
                case "green":
                    var tempGreen = ExtractPayload(watermarkedMatrix[1]);
                    newImage[0] = tempGreen.Item1;
                    result = new Tuple<double[][,], double[,]>(newImage, tempGreen.Item2);
                    break;
                case "blue":
                    var tempBlue = ExtractPayload(watermarkedMatrix[2]);
                    newImage[0] = tempBlue.Item1;
                    result = new Tuple<double[][,], double[,]>(newImage, tempBlue.Item2);
                    break;
                default:
                    Tuple<double[,], double[,]> temp;
                    double[,] payload = new double[watermarkedMatrix[0].GetLength(0), watermarkedMatrix[0].GetLength(1)];
                    for (int k = 0; k < watermarkedMatrix.GetLength(0); k++)
                    {
                        temp = ExtractPayload(watermarkedMatrix[k]);
                        newImage[k] = temp.Item1;
                        payload = CombinePayloads(payload, temp.Item2);
                        result = new Tuple<double[][,], double[,]>(newImage, payload);
                    }
                    break;
            }

            return result;
        }

        private double[,] CombinePayloads(double[,] actualPayload, double[,] addingPayload)
        {
            double[,] result = actualPayload.DeepClone();

            for (int i = 0; i < actualPayload.GetLength(1); i++)
            {
                for (int j = 0; j < actualPayload.GetLength(0); j++)
                {
                    //TODO !!!
                }
            }

            return result;
        }

        public double[,] HidePayload(double[,] matrix, double[,] payloadMatrix)
        {
            double[,] newMatrix = matrix.DeepClone();
            double[,] resultMatrix;// = new double[newMatrix.GetLength(0), newMatrix.GetLength(1)];
            var svdHost = new SingularValueDecomposition(newMatrix, true, true, false, false);
            //var svdPayload = new SingularValueDecomposition(payloadMatrix, true, true, true, true);
            var sO = svdHost.DiagonalMatrix.DeepClone();
            //var sp = svdPayload.DiagonalMatrix;
            
            if (sO.GetLength(0) == payloadMatrix.GetLength(0) &&
                sO.GetLength(1) == payloadMatrix.GetLength(1))
            {
                _inMemorySo = svdHost.DiagonalMatrix.DeepClone();

                //var alphaPayloadPrecise = payloadMatrix.Multiply(_alpha);
                //var alphaPayload = ApproximateEvery(alphaPayloadPrecise, 6);

                Debug.WriteLine("So diagonal Original inMemory: ");
                PrintMatrix(_inMemorySo);

                Debug.WriteLine("PayloadMatrix: ");
                PrintMatrix(payloadMatrix);

                //Debug.WriteLine("AlphaPayload: ");
                //PrintMatrix(alphaPayload);

                var sP = Code(_inMemorySo, payloadMatrix); //sO.Add(alphaPayload);

                /*

                var m = new double[,]
                {
                    {1.0, 0.0, 0.0, 0.0, 2.0},
                    {0.0, 0.0, 3.0, 0.0, 0.0},
                    {0.0, 0.0, 0.0, 0.0, 0.0},
                    {0.0, 4.0, 0.0, 0.0, 0.0},
                };

                var svdM = new SingularValueDecomposition(m, true, true, false, false);


                var u = new double[,]
                {
                    {0.0, 0.0, 1.0, 0.0},
                    {0.0, 1.0, 0.0, 0.0},
                    {0.0, 0.0, 0.0, -1.0},
                    {1.0, 0.0, 0.0, 0.0},
                };

                var s = new double[,]
                {
                    {4.0, 0.0, 0.0, 0.0, 0.0},
                    {0.0, 3.0, 0.0, 0.0, 0.0},
                    {0.0, 0.0, Math.Sqrt(5), 0.0, 0.0},
                    {0.0, 0.0, 0.0, 0.0, 0.0},
                };

                var vt = new double[,]
                {
                    {0.0, 1.0, 0.0, 0.0, 0.0},
                    {0.0, 0.0, 1.0, 0.0, 0.0},
                    {Math.Sqrt(0.2), 0.0, 0.0, 0.0, Math.Sqrt(0.8)},
                    {0.0, 0.0, 0.0, 1.0, 0.0},
                    {-Math.Sqrt(0.8), 0.0, 0.0, 0.0, Math.Sqrt(0.2)},
                };

                var calc_m = Matrix.Dot(Matrix.Dot(u,s), vt);
                var calc_mSVD = Matrix.Dot(Matrix.Dot(svdM.LeftSingularVectors,svdM.DiagonalMatrix), svdM.RightSingularVectors.Transpose());

                Debug.WriteLine("M:");
                PrintMatrix(m);

                Debug.WriteLine("Calc M:");
                PrintMatrix(calc_m);

                Debug.WriteLine("Calc svdM:");
                PrintMatrix(calc_mSVD);

                */

                //Debug.WriteLine("AlphaPayload: ");
                //PrintMatrix(alphaPayload);

                //var sM = Add2DMatrix(sO, alphaPayload);

                var u0 = svdHost.LeftSingularVectors;
                var v0t = svdHost.RightSingularVectors.Transpose();

                //var sP = sO;

                Debug.WriteLine("sP: ");
                PrintMatrix(sP);
                //Debug.WriteLine("Uo matrix: ");
                //PrintMatrix(u0);
                //Debug.WriteLine("Vot matrix: ");
                //PrintMatrix(v0t);

                //Debug.WriteLine("Sw: ");
                //PrintMatrix(sW);

                var uS = Matrix.Dot(u0, sP);
                var exactResult = Matrix.Dot(uS, v0t);
                resultMatrix = ApproximateEvery(exactResult, 6);
                //resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, sM), svdHost.RightSingularVectors.Transpose());
                //resultMatrix = Matrix.Dot(Matrix.Dot(svdHost.LeftSingularVectors, sM), svdHost.RightSingularVectors.Transpose());

                //Debug.WriteLine("Original Array after SVD->RSVD");
                //PrintMatrix(approximateResult);

                //var resSVD = new SingularValueDecomposition(resultMatrix);

                //Debug.WriteLine("sM after svd");
                //PrintMatrix(resSVD.DiagonalMatrix);
                //resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, uSP), svdHost.RightSingularVectors.Transpose());
                //resultMatrix = svdHost.Reverse();

                //Debug.WriteLine("Original Matrix: ");
                //PrintMatrix(matrix);

                //Debug.WriteLine("Matrix after svd and rsvd: ");
                //PrintMatrix(resultMatrix);

                //newMatrix = s.Add(payloadMatrix);
            }
            else
            {
                Debug.WriteLine($"LL matrix S component dim: {sO.GetLength(0)} x {sO.GetLength(1)}");
                Debug.WriteLine($"Watermark matrix dim: {payloadMatrix.GetLength(0)} x {payloadMatrix.GetLength(1)}");
                throw new ArithmeticException("Payload matrix should have same dimensions as Image Host matrix");
            }

            return resultMatrix;
        }

        public Tuple<double[,], double[,]> ExtractPayload(double[,] watermarkedMatrix) //, double[,] originalMatrix)
        {
            double[,] watermarked2D = watermarkedMatrix.DeepClone();
            //var result = new Dictionary<string, double[,]>();
            Debug.WriteLine("in memory S0");
            PrintMatrix(_inMemorySo);

            var svdWatermarked = new SingularValueDecomposition(watermarked2D, true, true, false, true);
            var sM = svdWatermarked.DiagonalMatrix;

            var decodeTuple = Decode(sM);

            Debug.WriteLine("Sm encoding matrix: ");
            //PrintMatrix(sM);
            PrintMatrix(decodeTuple.Item1);

            //var watermark2d = new double[sM.GetLength(0), sM.GetLength(1)];
            
            Debug.WriteLine("Extracted payload:");
            Debug.WriteLine("[");
            for (int i = 0; i < sM.GetLength(0); i++)
            {
                Debug.WriteLine("");
                for (int j = 0; j < sM.GetLength(1); j++)
                {
                    //decimal valueWatermarked = Convert.ToDecimal(Math.Round(sM[j, i], 4));
                    //decimal valueOrigin = Convert.ToDecimal(Math.Round(_inMemorySo[j, i], 4));
                    //decimal difference = Decimal.Subtract(valueWatermarked, valueOrigin);
                    //decimal alpha = Convert.ToDecimal(_alpha);
                    
                    //watermark2d[i, j] = sM[i, j]; //(sM[i, j] - _inMemorySo[i,j]) / _alpha;
                    //Debug.Write($"{watermark2d[i, j]} |");
                    Debug.Write($"{decodeTuple.Item2[i, j]} |");
                }
            }
            Debug.WriteLine("]");

            var origin2D =  Matrix.Dot(Matrix.Dot(svdWatermarked.LeftSingularVectors, _inMemorySo), svdWatermarked.RightSingularVectors.Transpose());
            return new Tuple<double[,], double[,]>(origin2D, decodeTuple.Item2);
        }

        public void MatricesSVD(double[][,] matrices)
        {
            for (int k = 0; k < matrices.Length; k++ )
            {
                MatrixSVD(matrices[k]);
            }
        }

        public Dictionary<char, double[,]> MatrixSVD(double[,] matrix)
        {
            var svd = new SingularValueDecomposition(matrix, true, true, true, true);
            var vectors = new Dictionary<char, double[,]>();
            var u = svd.LeftSingularVectors;
            var v = svd.RightSingularVectors;
            var s = svd.DiagonalMatrix;
            vectors['u'] = u;
            vectors['v'] = v;
            vectors['s'] = s;

            return vectors;
        }

        public void PrintMatrix(double[,] matrix){
            Debug.Write("[");
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                Debug.WriteLine("");
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Debug.Write($"{matrix[i, j]} |");
                }
            }
            Debug.WriteLine("]");
        }

        private double[,] Add2DMatrix(double[,] host, double[,] addition)
        {
            double[,] result = host.DeepClone();
            if (host.GetLength(0) != addition.GetLength(0) || host.GetLength(1) != addition.GetLength(1))
            {
                throw new ArithmeticException("Can't add 2 matrix of different size");
            }

            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    result[i, j] += addition[i, j];
                }
            }

            return result;
        }

        private double[,] ApproximateEvery(double[,] valuesToApproximate, int numberOfDecimalPlaces)
        {
            //string[] result = valueToApproximate.ToString().Split('.');
            for(int i = 0; i < valuesToApproximate.GetLength(0); i++)
            {
                for (int j = 0; j < valuesToApproximate.GetLength(1); j++)
                {
                    valuesToApproximate[i, j] = Math.Round(valuesToApproximate[i, j], numberOfDecimalPlaces);
                }
            }

            return valuesToApproximate;
        }

        private double[,] Code(double[,] s, double[,] payload)
        {
            var result = s.DeepClone();

            var iters = (s.GetLength(0) < s.GetLength(1)) ? s.GetLength(0) : s.GetLength(1);

            for (int i = 0; i < iters; i++)
            {
                result[i, i] = Math.Round(s[i, i], 0) + (payload[i, i] * 0.001);
            }
            return result;
        }

        private Tuple<double[,],double[,]> Decode(double[,] sp)
        {

            var iters = (sp.GetLength(0) < sp.GetLength(1)) ? sp.GetLength(0) : sp.GetLength(1);
            var originalSp = sp.DeepClone();
            var payload = sp.DeepClone();

            for (int i = 0; i < iters; i++)
            {
                var doubleString = sp[i, i].ToString().Split(',');
                originalSp[i, i] = double.Parse(doubleString[0]);
                if (doubleString.Length == 2)
                {
                    payload[i, i] = double.Parse(doubleString[1]);
                }
                else
                {
                    payload[i, i] = 0;
                }
                
            }
            return new Tuple<double[,], double[,]>(originalSp, payload);
        }



    }
}
