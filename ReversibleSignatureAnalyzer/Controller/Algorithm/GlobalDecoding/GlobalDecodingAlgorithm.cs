using ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion;
using ReversibleSignatureAnalyzer.Controller.Algorithm.HistogramShifting;
using ReversibleSignatureAnalyzer.Model;
using ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.GlobalDecoding
{
    class GlobalDecodingAlgorithm : IReversibleWatermarkingAlgorithm
    {
        public Tuple<Bitmap, string> Decode(Bitmap encodedImage, AlgorithmConfiguration configuration)
        {
            // Algorithm instances
            DifferencesExpansionAlgorithm de = new DifferencesExpansionAlgorithm();
            HistogramShiftingAlgorithm hs = new HistogramShiftingAlgorithm();
            // Algorithm configs
            DifferenceExpansionBruteForceConfiguration deConfig = new DifferenceExpansionBruteForceConfiguration(
                new HashSet<EmbeddingChanel>() { EmbeddingChanel.R, EmbeddingChanel.G, EmbeddingChanel.B }, new HashSet<Direction>() { Direction.Horizontal, Direction.Vertical });
            HistogramShiftingConfiguration hsConfig = new HistogramShiftingConfiguration(true, 
                new HashSet<EmbeddingChanel>() { EmbeddingChanel.R, EmbeddingChanel.G, EmbeddingChanel.B });
            // Decoding
            var deResult = de.BruteforceDecode(encodedImage, deConfig);
            if (deResult != null)
            {
                return deResult;
            }
            var hsResult = hs.Decode(encodedImage, hsConfig);
            if (hsResult != null)
            {
                return hsResult;
            }
            return null;
        }

        public Bitmap Encode(Bitmap inputImage, string payload, AlgorithmConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}
