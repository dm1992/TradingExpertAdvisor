using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class PriceInfo
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }

        public PriceInfo(string symbol, decimal price)
        {
            this.Symbol = symbol;
            this.Price = price;
        }
    }
}
