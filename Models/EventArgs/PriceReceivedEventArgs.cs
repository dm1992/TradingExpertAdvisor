using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class PriceInfoReceivedEventArgs : BaseEventArgs
    {
        public PriceInfo PriceInfo { get; set; }

        public PriceInfoReceivedEventArgs(PriceInfo priceInfo)
        {
            this.PriceInfo = priceInfo;
        }
    }
}
