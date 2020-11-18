using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Accord.Math;
using Accord.Math.Decompositions;
using Debug = System.Diagnostics.Debug;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class SvdAlgorithm
    {

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

        public double[,] ExtractPayloadSVD(double[][,] watermarkedMatrix, double[][,] originalMatrix, string channel)
        {
            double[,] payload = null;

            switch (channel.ToLower())
            {
                case "red":
                    payload = ExtractPayload(watermarkedMatrix[0], originalMatrix[0]);
                    break;
                case "green":
                    payload = ExtractPayload(watermarkedMatrix[1], originalMatrix[1]);
                    break;
                case "blue":
                    payload = ExtractPayload(watermarkedMatrix[2], originalMatrix[2]);
                    break;
                default:
                    for (int k = 0; k < watermarkedMatrix.Length; k++)
                    {
                        ExtractPayload(watermarkedMatrix[k], originalMatrix[k]);
                    }
                    break;
            }
            return payload;
        }

        private double[,] AddToEnd(double[,] actualPayload, double[,] addingContent)
        {
            double[,] result = actualPayload.Clone() as double[,];

            for (int i = 0; i < actualPayload.GetLength(1); i++)
            {
                for (int j = 0; j < actualPayload.GetLength(0); j++)
                {
                    
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
            var sh = svdHost.DiagonalMatrix.Clone() as double[,];
            //var sp = svdPayload.DiagonalMatrix;
            
            if (sh.GetLength(0) == payloadMatrix.GetLength(0) &&
                sh.GetLength(1) == payloadMatrix.GetLength(1))
            {
                //var sr = Accord.Math.Elementwise.Add(sh, payloadMatrix);
                //var sP = sh.Add(payloadMatrix);
                sh.Add(payloadMatrix);
                //var svdSP = new SingularValueDecomposition(sh, true, true, true, true);
                //var uSP = svdSP.DiagonalMatrix;

                //resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, svdSP.DiagonalMatrix), svdHost.RightSingularVectors.Transpose());
                //resultMatrix = Elementwise.Multiply(Elementwise.Multiply(svdHost.LeftSingularVectors, uSP), svdHost.RightSingularVectors.Transpose());
                resultMatrix = svdHost.Reverse();

                //newMatrix = s.Add(payloadMatrix);
            }
            else
            {
                Debug.WriteLine($"LL matrix S component dim: {sh.GetLength(0)} x {sh.GetLength(1)}");
                Debug.WriteLine($"Watermark matrix dim: {payloadMatrix.GetLength(0)} x {payloadMatrix.GetLength(1)}");
                throw new ArithmeticException("Payload matrix should have same dimensions as Image Host matrix");
            }

            if (resultMatrix.Equals(newMatrix))
            {
                throw new ApplicationException("BUG in adding payload");
            }

            return resultMatrix;
        }

        public double[,] ExtractPayload(double[,] watermarkedMatrix, double[,] originalMatrix)
        {
            //var result = new Dictionary<string, double[,]>();
            var svdWatermarked = new SingularValueDecomposition(watermarkedMatrix, true, true, true, true);
            var svdOriginal = new SingularValueDecomposition(originalMatrix, true, true, true, true);

            var newMatrixS = svdWatermarked.DiagonalMatrix;
            var originalMatrixS = svdOriginal.DiagonalMatrix;

            for (int i = 0; i < newMatrixS.GetLength(1); i++)
            {
                for (int j = 0; j < newMatrixS.GetLength(0); j++)
                {
                    newMatrixS[j, i] -= originalMatrixS[j, i];
                }
            }

            return newMatrixS;
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

        

    }
}
