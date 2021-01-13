using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion
{
    public class DifferencesExpansionConfiguration : AlgorithmConfiguration
    {
        public int Threeshold { get; set; }
        public Direction EmbeddingDirection { get; set; }

        public DifferencesExpansionConfiguration(int threeshold, Direction embeddingDirection, HashSet<EmbeddingChanel> embeddingChanels) : base(embeddingChanels)
        {
            this.EmbeddingDirection = embeddingDirection;
            this.Threeshold = threeshold;        
        }

    }
}
