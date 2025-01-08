using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class InternalTrade
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public TradeDirection TradeDirection { get; set; }
        public DateTime Time { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }

        public string Dump()
        {
            return $"{this.Id};{this.Symbol};{this.TradeDirection};{this.Time:dd.MM.yyyy HH:mm:ss};{this.Price};{this.Volume}";
        }
    }
}
