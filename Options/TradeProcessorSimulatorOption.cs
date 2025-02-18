using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Options
{
    public class TradeProcessorSimulatorOption : TradeProcessorOption
    {
        public decimal TakeProfitAmount { get; set; }
        public decimal StopLossAmount { get; set; }
        public int ActiveTradesLimit { get; set; }
    }
}
