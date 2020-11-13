using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Decompositions;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class SvdAlgorithm
    {

        public void MatricesSVD(double[][,] matrices)
        {
            for (int k = 0; k < matrices.Length; k++ )
            {
                MatrixSVD(matrices[k]);
            }
        }

        public double[] MatrixSVD(double[,] matrix)
        {
            var svd = new SingularValueDecomposition(matrix, true, true, true, true);
            var vectors = new double[4];
            var u = svd.LeftSingularVectors;
            var v = svd.RightSingularVectors;
            var epsilon = svd.DiagonalMatrix;

            return vectors;
        }

    }
}
