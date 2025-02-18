using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Options
{
    public class MarketSignalGeneratorOption : IOption
    {
        public Dictionary<int, int> CandleCollectionTimeframeThresholds { get; set; }

        public string Dump()
        {
            if (CandleCollectionTimeframeThresholds.IsNullOrEmpty())
                return "N/A";

            return $"CandleCollectionTimeframeThresholds: {string.Join(", ", CandleCollectionTimeframeThresholds.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
        }
    }
}
