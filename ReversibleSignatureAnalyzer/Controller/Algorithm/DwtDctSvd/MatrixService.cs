using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class MatrixService
    {

        public double[][,] DecimalToDouble(decimal[][,] origin)
        {
            double[][,] newMatrix = new double[origin.GetLength(0)][,];

            for (int k = 0; k < origin.GetLength(0); k++)
            {
                newMatrix[k] = new double[origin[0].GetLength(0), origin[0].GetLength(1)];
                for (int j = 0; j < origin[0].GetLength(1); j++)
                {
                    for (int i = 0; i < origin[0].GetLength(0); i++)
                    {
                        newMatrix[k][i, j] = Convert.ToDouble(origin[k][i, j]);
                    }
                }
            }

            return newMatrix;
        }

        public decimal[][,] DoubleToDecimal(double[][,] origin)
        {
            decimal[][,] newMatrix = new decimal[origin.GetLength(0)][,];

            for (int k = 0; k < origin.GetLength(0); k++)
            {
                newMatrix[k] = new decimal[origin[0].GetLength(0), origin[0].GetLength(1)];
                for (int j = 0; j < origin[0].GetLength(1); j++)
                {
                    for (int i = 0; i < origin[0].GetLength(0); i++)
                    {
                        newMatrix[k][i, j] = Convert.ToDecimal(origin[k][i, j]);
                    }
                }
            }

            return newMatrix;
        }

    }
}
