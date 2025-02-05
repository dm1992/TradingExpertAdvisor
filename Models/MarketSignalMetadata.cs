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
        public MarketDirection MarketDirection { get; set; }
        decimal ProbabilityPercentage { get; set; }

        public MarketSignalMetadata(string symbol, MarketDirection marketDirection, decimal probabilityPercentage)
        {
            this.Symbol = symbol;
            this.MarketDirection = marketDirection;
            this.ProbabilityPercentage = probabilityPercentage;
            this.Timestamp = DateTime.Now;
        }
    }
}
