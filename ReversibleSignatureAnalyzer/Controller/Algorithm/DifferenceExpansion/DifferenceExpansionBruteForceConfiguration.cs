using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion
{
    public class DifferenceExpansionBruteForceConfiguration : AlgorithmConfiguration
    {
        public HashSet<Direction> EmbeddingDirections { get; set; }
        public DifferenceExpansionBruteForceConfiguration(HashSet<EmbeddingChanel> embeddingChanels, HashSet<Direction> embeddingDirections) : base(embeddingChanels)
        {
            this.EmbeddingDirections = embeddingDirections;
        }
    }
}
