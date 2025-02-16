using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Managers.Options
{
    public class MarketSignalValidatorOption : IOption
    {
        public Dictionary<int, int> MarketSignalTimeframeThresholds { get; set; }

        public string Dump()
        {
            throw new NotImplementedException();
        }
    }
}
