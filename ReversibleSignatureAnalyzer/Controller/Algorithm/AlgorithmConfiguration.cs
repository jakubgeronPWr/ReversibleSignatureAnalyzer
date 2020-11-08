using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm
{
    public class AlgorithmConfiguration
    {

        public int Iterations { get; set; }
        public EmbeddingChanel EmbeddingChanel { get; set; }

        public AlgorithmConfiguration(int iterations, EmbeddingChanel embeddingChanel)
        {
            this.Iterations = iterations;
            this.EmbeddingChanel = embeddingChanel;
        }

    }
}
