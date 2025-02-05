using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor.Managers.Options
{
    public class CandleCollectorOption : IOption
    {
        public List<int> Timeframes { get; set; }

        public string Dump()
        {
            return $"Timeframes: '{String.Join(", ", this.Timeframes)}'";
        }
    }
}
