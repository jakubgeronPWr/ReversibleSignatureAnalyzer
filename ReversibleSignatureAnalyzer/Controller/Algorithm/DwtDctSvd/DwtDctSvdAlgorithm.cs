using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtDctSvdAlgorithm: IReversibleWatermarkingAlgorithm
    {
        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}
