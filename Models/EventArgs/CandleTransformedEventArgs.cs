using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class CandleTransformedEventArgs : BaseEventArgs
    {
        public InternalCandle Candle { get; set; }

        public CandleTransformedEventArgs(InternalCandle candle)
        {
            this.Candle = candle;
        }
    }
}
