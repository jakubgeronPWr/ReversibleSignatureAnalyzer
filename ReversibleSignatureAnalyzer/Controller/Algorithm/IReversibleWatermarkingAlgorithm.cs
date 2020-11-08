using ReversibleSignatureAnalyzer.Controller.Algorithm;
using System;
using System.Drawing;

namespace ReversibleSignatureAnalyzer.Model
{
    public interface IReversibleWatermarkingAlgorithm
    {
        Bitmap Encode(Bitmap inputImage, String payload, AlgorithmConfiguration configuration);

        Tuple<Bitmap, String> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration);

    }

    public enum EmbeddingChanel
    {
        R,
        G,
        B,
        All
    }

}
