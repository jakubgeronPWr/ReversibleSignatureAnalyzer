﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class DwtSvdConfiguration : AlgorithmConfiguration
    {

        public HashSet<DwtDctSvdAlgorithm.QuarterSymbol> QuarterSymbol { get; set; }

        public DwtSvdConfiguration(int iterations, HashSet<EmbeddingChanel> embeddingChannels, HashSet<DwtDctSvdAlgorithm.QuarterSymbol> quarterSymbol) : base(iterations, embeddingChannels)
        {
            QuarterSymbol = quarterSymbol;
        }
    }
}
