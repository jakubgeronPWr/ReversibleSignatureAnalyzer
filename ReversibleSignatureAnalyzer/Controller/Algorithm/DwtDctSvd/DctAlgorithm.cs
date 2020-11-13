using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DctAlgorithm
    {

        public double[][,] DCTMatrices(double[][,] matrices)
        {
            var outMatrices = new double[3][,];
            Parallel.For(0, 3, i =>
            {
                Debug.WriteLine($"number of processing matrix: {i}");
                outMatrices[i] = DCT2D(matrices[i]);
            });
            Debug.WriteLine($"End of matrices processing");
            return outMatrices;
        }

        //Run the inverse DCT2D on 3-channeled group of matrices
        public double[][,] IDCTMatrices(double[][,] matrices)
        {
            var outMatrices = new double[3][,];
            Parallel.For(0, 3, i =>
            {
                outMatrices[i] = IDCT2D(matrices[i]);
            });
            return outMatrices;
        }

        //Run a DCT2D on a single matrix
        public double[,] DCT2D(double[,] input)
        {
            double[,] coeffs = new double[input.GetLength(0), input.GetLength(1)];

            //To initialise every [u,v] value in the coefficient table...
            for (int u = 0; u < coeffs.GetLength(0); u++)
            {
                for (int v = 0; v < coeffs.GetLength(1); v++)
                {
                    //...sum the basisfunction for every [x,y] value in the bitmap input
                    double sum = 0d;



                    for (int x = 0; x < coeffs.GetLength(0); x++)
                    {
                        for (int y = 0; y < coeffs.GetLength(1); y++)
                        {
                            double a = input[x, y];
                            sum += BasisFunction(a, u, v, x, y, coeffs.GetLength(0), coeffs.GetLength(1));
                        }
                    }
                    coeffs[u, v] = sum * Beta(coeffs.GetLength(0), coeffs.GetLength(1)) * Alpha(u) * Alpha(v);
                }
            }
            return coeffs;
        }

        //Run an inverse DCT2D on a single matrix
        public double[,] IDCT2D(double[,] coeffs)
        {
            double[,] output = new double[coeffs.GetLength(0), coeffs.GetLength(1)];

            //To initialise every [x,y] value in the bitmap output...
            for (int x = 0; x < coeffs.GetLength(0); x++)
            {
                for (int y = 0; y < coeffs.GetLength(1); y++)
                {
                    //...sum the basisfunction for every [u,v] value in the coefficient table
                    double sum = 0d;

                    for (int u = 0; u < coeffs.GetLength(0); u++)
                    {
                        for (int v = 0; v < coeffs.GetLength(1); v++)
                        {
                            double a = coeffs[u, v];
                            sum += BasisFunction(a, u, v, x, y, coeffs.GetLength(0), coeffs.GetLength(1)) * Alpha(u) * Alpha(v);
                        }
                    }

                    output[x, y] = sum * Beta(coeffs.GetLength(0), coeffs.GetLength(1));
                }
            }
            return output;
        }

        public double BasisFunction(double a, double u, double v, double x, double y, int width, int height)
        {
            double b = Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * width));
            double c = Math.Cos(((2d * y + 1d) * v * Math.PI) / (2 * height));

            return a * b * c;
        }

        //return 1/sqrt(2) if u is not 0
        private double Alpha(int u)
        {
            if (u == 0)
                return 1 / Math.Sqrt(2);
            return 1;
        }

        //normalising value
        private double Beta(int width, int height)
        {
            return (1d / width + 1d / height); 
        }

    }
}
