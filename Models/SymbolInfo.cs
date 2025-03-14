using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class SymbolInfo
    {
        public string Name { get; set; } 

        public string BaseAsset { get; set; }

        public string QuoteAsset { get; set; }

        public SymbolStatus Status { get; set; }
    }
}
