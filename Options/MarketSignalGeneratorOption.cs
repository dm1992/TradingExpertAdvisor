using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Options
{
    public class MarketSignalGeneratorOption
    {
        public List<decimal> TakeProfitAmounts { get; set; }
        public List<decimal> StopLossAmounts { get; set; }
    }
}
