using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Managers.Options
{
    public class CandleEvaluatorOption
    {
        public List<int> Timeframes { get; set; }

        public string Dump()
        {
            return $"Timeframes: '{String.Join(", ", this.Timeframes)}'";
        }
    }
}
