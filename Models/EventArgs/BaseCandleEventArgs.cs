using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public abstract class BaseCandleEventArgs : BaseEventArgs
    {
        public InternalCandle Candle { get; set; }

        public BaseCandleEventArgs(InternalCandle candle)
        {
            this.Candle = candle;
        }
    }


    public class CandleReceivedEventArgs : BaseCandleEventArgs
    {
        public CandleReceivedEventArgs(InternalCandle candle) : base(candle) { }
    }

    public class CandleTransformedEventArgs : BaseCandleEventArgs
    {
        public CandleTransformedEventArgs(InternalCandle candle) : base(candle) { }
    }
}
