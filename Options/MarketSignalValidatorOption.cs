using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Options
{
    public class MarketSignalValidatorOption : IOption
    {
        public Dictionary<int, int> MarketSignalTimeframeThresholds { get; set; }

        public string Dump()
        {
            if (MarketSignalTimeframeThresholds.IsNullOrEmpty())
                return "N/A";

            return $"MarketSignalTimeframeThresholds: {string.Join(", ", MarketSignalTimeframeThresholds.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
        }
    }
}
