using System;
using System.Windows;
using Accord.IO;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtPrecisionAlgorithm
    {
        //private readonly decimal root2 = 1m / Convert.ToDecimal(Math.Sqrt(2));
        private readonly decimal w0 = 0.5m;
        private readonly decimal w1 = -0.5m;
        private readonly decimal s0 = 0.5m;
        private readonly decimal s1 = 0.5m;

        public decimal[][,] OriginalImage { get; set; }
        public decimal[][,] TransformedImage { get; set; }

        public decimal[][,] TransformedArraysDoubles { get; set; }


        public DwtPrecisionAlgorithm()
        {
            TransformedArraysDoubles = new decimal[3][,];
            //w0 = root2;
            //w1 = -root2;
            //s0 = root2;
            //s1 = root2;
        }

        public void DwtF(decimal[] data)
        {
            decimal[] temp = new decimal[data.Length];

            int h = data.Length >> 1;

            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        public void DwtF(decimal[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            decimal[] row;
            decimal[] col;

            for (int k = 0; k < iterations; k++)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                row = new decimal[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    DwtF(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }


                col = new decimal[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    DwtF(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }
            }
        }

        public void IDwtF(decimal[] data)
        {
            decimal[] temp = new decimal[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
                temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        public void IDwtF(decimal[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            decimal[] col;
            decimal[] row;

            for (int k = iterations - 1; k >= 0; k--)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                col = new decimal[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    IDwtF(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }

                row = new decimal[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    IDwtF(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }
            }
        }

        public void ApplyHaarTransform(bool Forward, bool Safe, int iterations)
        {
            decimal[][,] temp = Forward ? OriginalImage.DeepClone() : TransformedImage.DeepClone();

            //int Iterations = 0;
            //int.TryParse(sIterations, out Iterations);
            decimal[,] Red = temp[0];
            decimal[,] Green = temp[1];
            decimal[,] Blue = temp[2];

            int maxScale = (int)(Math.Log(Red.GetLength(0) < Red.GetLength(1) ? Red.GetLength(0) : Red.GetLength(1)) / Math.Log(2));
            if (iterations < 1 || iterations > maxScale)
            {
                MessageBox.Show("Iteration must be Integer from 1 to " + maxScale);
                return;
            }

            int time = Environment.TickCount;


            if (Forward)
            {
                DwtF(Red, iterations);
                DwtF(Green, iterations);
                DwtF(Blue, iterations);
            }
            else
            {
                IDwtF(Red, iterations);
                IDwtF(Green, iterations);
                IDwtF(Blue, iterations);
            }

            if (Forward)
            {
                TransformedImage = temp.DeepClone();
            }
            else
            {
                OriginalImage = temp.DeepClone();
            }

            string transformationTime = ((int)(Environment.TickCount - time)).ToString() + " milis.";
        }
    }
}
