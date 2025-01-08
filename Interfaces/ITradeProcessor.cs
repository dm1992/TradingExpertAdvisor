using Kraken.Net.Objects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Interfaces
{
    public interface ITradeProcessor : IManager
    {
        public bool OpenTrade();
    }
}
