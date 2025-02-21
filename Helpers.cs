using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Models;

namespace TradingExpertAdvisor
{
    public static class Helpers
    {
        public static bool IsMarketSignalValid(MarketSignalMetadata marketSignal)
        {
            return marketSignal != null && marketSignal.MarketDirection != MarketDirection.Unknown;
        }
    }
}
