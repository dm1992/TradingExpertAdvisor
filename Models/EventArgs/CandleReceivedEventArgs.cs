using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class CandleReceivedEventArgs : BaseEventArgs
    {
        public InternalCandle Candle { get; set; }

        public CandleReceivedEventArgs(InternalCandle candle)
        {
            this.Candle = candle;
        }
    }
}
