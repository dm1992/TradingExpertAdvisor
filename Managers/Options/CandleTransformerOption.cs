using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Managers.Options
{
    public class CandleTransformerOption
    {
        public List<string> Symbols { get; set; }
        public List<int> Timeframes { get; set; }
    }
}
