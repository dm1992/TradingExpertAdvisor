using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class InternalOrderbook
    {
        public DateTime Timestamp { get; set; }
        public string Symbol { get; set; }
        public List<InternalAsk> Asks { get; set; }
        public List<InternalBid> Bids { get; set; }

        public InternalOrderbook(string symbol, List<InternalAsk> asks, List<InternalBid> bids)
        {
            this.Timestamp = DateTime.Now;
            this.Symbol = symbol;
            this.Asks = asks;
            this.Bids = bids;
        }
    }

    public abstract class InternalOrderbookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public InternalOrderbookEntry(decimal price, decimal quantity)
        {
            this.Price = price;
            this.Quantity = quantity;
        }

        public string Dump()
        {
            return $"{this.Price}:{this.Quantity}";
        }
    }

    public class InternalAsk : InternalOrderbookEntry
    {
        public InternalAsk(decimal price, decimal quantity) : base(price, quantity) { }
    }

    public class InternalBid : InternalOrderbookEntry
    {
        public InternalBid(decimal price, decimal quantity) : base(price, quantity) { }
    }
}
