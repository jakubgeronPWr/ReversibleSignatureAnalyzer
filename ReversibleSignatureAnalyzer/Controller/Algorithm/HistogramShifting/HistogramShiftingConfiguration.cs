using System.Collections.Generic;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.HistogramShifting
{
    class HistogramShiftingConfiguration : AlgorithmConfiguration
    {
        public HistogramShiftingConfiguration(int iterations, HashSet<EmbeddingChanel> embeddingChanels) : base(iterations, embeddingChanels) 
        {

        }
    }
}
