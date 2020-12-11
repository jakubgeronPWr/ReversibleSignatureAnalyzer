using System.Collections.Generic;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.HistogramShifting
{
    class HistogramShiftingConfiguration : AlgorithmConfiguration
    {
        public bool bruteforce;
        public HistogramShiftingConfiguration(int iterations, bool brute, HashSet<EmbeddingChanel> embeddingChanels) : base(iterations, embeddingChanels) 
        {
            bruteforce = brute;
        }
    }
}
