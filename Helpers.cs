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
        public static bool IsCandleCollectionValid(CandleCollection candleCollection)
        {
            return candleCollection != null && candleCollection.MainCandle != null && !candleCollection.TimeframeSubCandles.IsNullOrEmpty();
        }
    }
}
