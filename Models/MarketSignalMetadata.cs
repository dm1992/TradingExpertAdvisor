using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class MarketSignalMetadata
    {
        public string Tag { get; set; }

        public string Symbol { get; set; }

        public DateTime Timestamp { get; set; }

        public decimal TakeProfitAmount { get; set; }

        public decimal StopLossAmount { get; set; }

        public decimal EntryPrice { get; set; }

        public decimal ExitPriceTakeProfitUp
        {
            get
            {
                return this.EntryPrice + this.TakeProfitAmount;
            }
        }

        public decimal ExitPriceTakeProfitDown
        {
            get
            {
                return this.EntryPrice - this.TakeProfitAmount;
            }
        }

        public decimal ExitPriceStopLossUp
        {
            get
            {
                return this.EntryPrice + this.StopLossAmount;
            }
        }

        public decimal ExitPriceStopLossDown
        {
            get
            {
                return this.EntryPrice - this.StopLossAmount;
            }
        }

        public MarketDirection MarketDirection { get; set; }

        public bool IsObsolete { get; set; }
    }
}
