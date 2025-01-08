using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class TradeReceivedEventArgs : BaseEventArgs
    {
        public List<InternalTrade> Trades { get; set; }

        public TradeReceivedEventArgs(List<InternalTrade> trades)
        {
            this.Trades = trades;
        }
    }
}
