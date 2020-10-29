using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ReversibleSignatureAnalyzer.Model
{
    interface IReversibleWatermarkingAlgorithm
    {
        Bitmap Encode(Bitmap inputImage, String payload);
    }
}
