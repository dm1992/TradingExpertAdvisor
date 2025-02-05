using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Models.EventArgs;

namespace TradingExpertAdvisor.Interfaces
{
    public interface ICandleCollector : IManager
    {
        event EventHandler<CandleCollectedEventArgs> CandleCollectedEventHandler;
    }
}
