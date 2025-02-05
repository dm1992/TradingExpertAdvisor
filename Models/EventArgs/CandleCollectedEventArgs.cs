using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class CandleCollectedEventArgs : BaseEventArgs
    {
        public CandleCollection CandleCollection { get; set; }

        public CandleCollectedEventArgs(CandleCollection candleCollection)
        {
            this.CandleCollection = candleCollection;
        }
    }
}
