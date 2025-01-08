using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class PriceReceivedEventArgs : BaseEventArgs
    {
        public decimal Price { get; set; }

        public PriceReceivedEventArgs(string symbol, decimal price) : base(symbol)
        {
            this.Price = price;
        }
    }
}
