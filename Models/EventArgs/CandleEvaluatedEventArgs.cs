using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public class CandleEvaluatedEventArgs : BaseEventArgs
    {
        public MarketDirectionType DirectionType { get; set; }

        public CandleEvaluatedEventArgs(MarketDirectionType directionType)
        {
            this.DirectionType = directionType;
        }
    }
}
