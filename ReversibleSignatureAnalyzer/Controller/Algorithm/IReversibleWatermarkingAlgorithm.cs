using System;
using System.Drawing;

namespace ReversibleSignatureAnalyzer.Model
{
    public interface IReversibleWatermarkingAlgorithm
    {
        Bitmap Encode(Bitmap inputImage, String payload);

        Tuple<Bitmap, String> Decode(Bitmap encodedImage);

    }
}
