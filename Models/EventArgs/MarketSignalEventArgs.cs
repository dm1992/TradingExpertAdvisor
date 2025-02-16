using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class MarketSignalEventArgs
    {
        public MarketSignalMetadata MarketSignalMetadata { get; set; }

        public MarketSignalEventArgs(MarketSignalMetadata marketSignalMetadata)
        {
            this.MarketSignalMetadata = marketSignalMetadata;
        }
    }
}
