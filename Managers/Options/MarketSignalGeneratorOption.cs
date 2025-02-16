using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Managers.Options
{
    public class MarketSignalGeneratorOption : IOption
    {
        public Dictionary<int, int> CandleCollectionTimeframeThresholds { get; set; }

        public string Dump()
        {
            throw new NotImplementedException();
        }
    }
}
