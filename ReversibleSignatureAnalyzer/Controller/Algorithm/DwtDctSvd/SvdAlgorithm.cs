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

        private double[,] _inMemoryUw;
        private double[,] _inMemoryVwt;
        private double[,] _inMemorySo;

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

        public Tuple<double[,], double[,]> ExtractPayloadSVD(double[][,] watermarkedMatrix, string channel)//double[][,] originalMatrix, string channel)
        {
            Tuple<double[,], double[,]> payload = null;

            switch (channel.ToLower())
            {
                case "red":
                    payload = ExtractPayload(watermarkedMatrix[0]);
                    break;
                case "green":
                    payload = ExtractPayload(watermarkedMatrix[1]);
                    break;
                case "blue":
                    payload = ExtractPayload(watermarkedMatrix[2]);
                    break;
                default:
                    for (int k = 0; k < watermarkedMatrix.Length; k++)
                    {
                        ExtractPayload(watermarkedMatrix[k]);
                    }
                    break;
            }
            return payload;
        }

        private double[,] CombinePayloads(double[,] actualPayload, double[,] addingPayload)
        {
            double[,] result = actualPayload.Clone() as double[,];

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
            double[,] newMatrix = matrix.Clone() as double[,];
            double[,] resultMatrix;// = new double[newMatrix.GetLength(0), newMatrix.GetLength(1)];
            var svdHost = new SingularValueDecomposition(newMatrix, true, true, true, true);
            //var svdPayload = new SingularValueDecomposition(payloadMatrix, true, true, true, true);
            var sO = svdHost.DiagonalMatrix.Clone() as double[,];
            //var sp = svdPayload.DiagonalMatrix;
            
            if (sO.GetLength(0) == payloadMatrix.GetLength(0) &&
                sO.GetLength(1) == payloadMatrix.GetLength(1))
            {
                //var sr = Accord.Math.Elementwise.Add(sh, payloadMatrix);
                //var sP = sh.Add(payloadMatrix);

                //Debug.WriteLine("So diagonal origin: ");
                //PrintMatrix(svdHost.DiagonalMatrix);

                _inMemorySo = svdHost.DiagonalMatrix.DeepClone();

                var alphaPayload = payloadMatrix.Multiply(0.1);

                //Debug.WriteLine("AlphaPayload: ");
                //PrintMatrix(alphaPayload);

                var sM = Add2DMatixes(sO, alphaPayload);

                //Debug.WriteLine("Sm: ");
                //PrintMatrix(sM);

                var svdSM = new SingularValueDecomposition(sM, true, true, true, true);
                var sW = svdSM.DiagonalMatrix.DeepClone();
                _inMemoryUw = svdSM.LeftSingularVectors.DeepClone();
                _inMemoryVwt = svdSM.RightSingularVectors.Transpose().DeepClone();

                //Debug.WriteLine("Uo matrix: ");
                //PrintMatrix(svdHost.LeftSingularVectors);
                //Debug.WriteLine("Vot matrix: ");
                //PrintMatrix(svdHost.RightSingularVectors.Transpose());
                //Debug.WriteLine("Sw: ");
                //PrintMatrix(sW);


                resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, sW), svdHost.RightSingularVectors.Transpose());
                //resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, uSP), svdHost.RightSingularVectors.Transpose());
                //resultMatrix = svdHost.Reverse();

                //newMatrix = s.Add(payloadMatrix);
            }
            else
            {
                Debug.WriteLine($"LL matrix S component dim: {sO.GetLength(0)} x {sO.GetLength(1)}");
                Debug.WriteLine($"Watermark matrix dim: {payloadMatrix.GetLength(0)} x {payloadMatrix.GetLength(1)}");
                throw new ArithmeticException("Payload matrix should have same dimensions as Image Host matrix");
            }

            if (resultMatrix.Equals(newMatrix))
            {
                throw new ApplicationException("BUG in adding payload");
            }

            return resultMatrix;
        }

        public Tuple<double[,], double[,]> ExtractPayload(double[,] watermarkedMatrix) //, double[,] originalMatrix)
        {
            //var result = new Dictionary<string, double[,]>();
            var svdWatermarked = new SingularValueDecomposition(watermarkedMatrix, true, true, true, true);
            //var svdOriginal = new SingularValueDecomposition(originalMatrix, true, true, true, true);

            var watermarkedS = svdWatermarked.DiagonalMatrix;
            //var originalMatrixS = svdOriginal.DiagonalMatrix;
            var sM = Elementwise.Multiply(Elementwise.Multiply(_inMemoryUw, watermarkedS), _inMemoryVwt);

            var watermark2d = new double[sM.GetLength(0), sM.GetLength(1)];

            //Debug.WriteLine("[");
            for (int i = 0; i < sM.GetLength(1); i++)
            {
                Debug.WriteLine("");
                for (int j = 0; j < sM.GetLength(0); j++)
                {
                    watermark2d[j, i] = sM[j, i] - _inMemorySo[j, i];
                    //Debug.Write($"{sM[j, i]} |");
                }
            }
            //Debug.WriteLine("]");

            var origin2D =  Elementwise.Multiply(Elementwise.Multiply(svdWatermarked.LeftSingularVectors, _inMemorySo), svdWatermarked.RightSingularVectors);
            return new Tuple<double[,], double[,]>(origin2D, watermark2d);
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
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                Debug.WriteLine("");
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    Debug.Write($"{matrix[j, i]} |");
                }
            }
            Debug.WriteLine("]");
        }

        private double[,] Add2DMatixes(double[,] host, double[,] addition)
        {
            double[,] result = host.DeepClone();
            if (host.GetLength(0) != addition.GetLength(0) || host.GetLength(1) != addition.GetLength(1))
            {
                throw new ArithmeticException("Can't add 2 matrix of different size");
            }

            for (int i = 0; i < result.GetLength(1); i++)
            {
                for (int j = 0; j < result.GetLength(0); j++)
                {
                    result[j, i] += addition[j, i];
                }
            }

            return result;
        }

        

    }
}
