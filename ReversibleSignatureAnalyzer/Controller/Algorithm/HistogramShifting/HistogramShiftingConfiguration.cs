using System.Collections.Generic;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.HistogramShifting
{
    class HistogramShiftingConfiguration : AlgorithmConfiguration
    {
        public bool bruteforce;
        public HistogramShiftingConfiguration(bool brute, HashSet<EmbeddingChanel> embeddingChanels) : base(embeddingChanels) 
        {
            bruteforce = brute;
        }
    }
}
