using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class OrderbookReceivedEventArgs : BaseEventArgs
    {
        public InternalOrderbook Orderbook { get; set; }

        public OrderbookReceivedEventArgs(InternalOrderbook orderbook)
        {
            this.Orderbook = orderbook;
        }
    }
}
