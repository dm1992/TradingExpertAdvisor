using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class MarketSignalMetadata
    {
        public string Symbol { get; set; }

        public DateTime Timestamp { get; set; }

        public decimal CurrentPrice { get; set; }

        public MarketDirection MarketDirection { get; set; }

        public Dictionary<int, InternalCandleDirectionInfo> TimeframeCandleDirectionInfos { get; set; }
    }
}
