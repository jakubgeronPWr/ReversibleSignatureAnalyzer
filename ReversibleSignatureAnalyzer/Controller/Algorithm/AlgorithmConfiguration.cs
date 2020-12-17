using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm
{
    public class AlgorithmConfiguration
    {

        public HashSet<EmbeddingChanel> EmbeddingChanels { get; set; }

        public AlgorithmConfiguration(HashSet<EmbeddingChanel> embeddingChanels)
        {
            this.EmbeddingChanels = embeddingChanels;
        }

    }
}
