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
        public int Timeframe { get; set; }
        public DateTime Timestamp { get; set; }
        public MarketDirection MarketDirection { get; set; }
        public decimal MarketDirectionPercentage { get; set; }

        public MarketSignalMetadata(string symbol, int timeframe, MarketDirection marketDirection, decimal marketDirectionPercentage)
        {
            this.Symbol = symbol;
            this.Timeframe = timeframe;
            this.MarketDirection = marketDirection;
            this.MarketDirectionPercentage = marketDirectionPercentage;
            this.Timestamp = DateTime.Now;
        }
    }
}
