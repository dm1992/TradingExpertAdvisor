using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class MarketDirectionInfo
    {
        public string MarketTag { get; set; } // market signal symbol, timeframe,... etc
        public decimal TakeProfitAmount { get; set; }
        public decimal StopLossAmount { get; set; }
        public int Ups { get; set; }
        public int Downs { get; set; }
    }
}
