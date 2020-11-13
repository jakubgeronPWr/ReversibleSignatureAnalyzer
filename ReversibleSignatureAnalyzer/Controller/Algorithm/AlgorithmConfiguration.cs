using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm
{
    public class AlgorithmConfiguration
    {

        public int Iterations { get; set; }
        public HashSet<EmbeddingChanel> EmbeddingChanels { get; set; }

        public AlgorithmConfiguration(int iterations, HashSet<EmbeddingChanel> embeddingChanels)
        {
            this.Iterations = iterations;
            this.EmbeddingChanels = embeddingChanels;
        }

    }
}
