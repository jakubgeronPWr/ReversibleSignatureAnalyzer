using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class StringToArrayConverter
    {
        public double[] StringPayloadToArray(string payload)
        {

            var payloadArray = payload.ToCharArray();
            var result = new double[payloadArray.Length];

            var asciiBytesArray = Encoding.ASCII.GetBytes(payload);


            for (int i = 0; i < payloadArray.Length; i++)
            {
                result[i] = (double) ((int) payloadArray[i]) ;
                Debug.WriteLine($"string to double: {result[i]}");
            }

            return result;
        }

        public string ArrayPayloadToString(double[] payload)
        {
            StringBuilder sb = new StringBuilder(payload.Length);

            //var asciiBytesArray = Encoding.ASCII.GetBytes(payload);

            foreach (var value in payload)
            {
                double oneElemet = value;
                if (value < 0)
                    oneElemet = 0;
                sb.Append(Convert.ToChar(Convert.ToInt32(oneElemet)));

            }

            return sb.ToString();
        }

        public double[,] ArrayPayloadTo2DArray(double[] payload, int width, int height)
        {
            var result2DArray = new double[width, height];
            for (int i = 0; i < result2DArray.GetLength(0); i++)
            {
                for (int j = 0; j < result2DArray.GetLength(1); j++)
                {
                    result2DArray[j, i] = 0;
                }
            }

            int payloadLenght = payload.Length;

            var numberOfRows = (int) Math.Ceiling((decimal)payloadLenght /width);

            int payloadRowLength = (payloadLenght < width) ? payloadLenght : width;

            if (numberOfRows > height)
            {
                throw new ArithmeticException("Message is too long");
            }
            else
            {
                Debug.WriteLine("2D Payload array: ");
                var ommit = 0;
                for (int k = 0; k < numberOfRows; k++)
                {
                    Debug.WriteLine("");
                    Debug.Write("[");
                    for (int i = 0; i < payloadRowLength ; i++)
                    {
                        if (payload.Length > i + (k * width))
                        {
                            result2DArray[i, k] = payload[i + (k * width)];
                        }
                        Debug.Write($"{result2DArray[i, k]}, ");
                    }
                    Debug.Write("]");
                }
            }

            return result2DArray;
        }


        public double[] Array2DPayloadToArray(double[,] payload)
        {
            int width = payload.GetLength(0);
            int height = payload.GetLength(1);

            double[] resultArray = new double[width*height];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    resultArray[j+(i*j)] = payload[i,j];
                }
            }

            //Debug.WriteLine("Payload extraction 1D array: ");
            //foreach (var value in resultArray)
            //{
            //    Debug.Write(value);
            //}

            return resultArray;
        }

    }
}
