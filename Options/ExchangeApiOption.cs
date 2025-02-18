using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Options
{
    public class ExchangeApiOption : IOption
    {
        public ExchangeApi ApiName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public List<string> Symbols { get; set; }
        public List<int> Timeframes { get; set; }
        public bool IsLive { get; set; }

        public string Dump()
        {
            return $"ExchangeApi: {this.ApiName}, Symbols: {string.Join(", ", this.Symbols)}, Timeframes: {string.Join(", ", this.Timeframes)}, IsLive: {this.IsLive}";
        }
    }
}
