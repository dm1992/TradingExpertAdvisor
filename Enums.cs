using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor
{
    public enum TradeDirection
    {
        Buy = 1,
        Sell = 2,
    }

    public enum MessageType
    { 
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public enum Api
    {
        Bybit_Spot = 1
    }

    public enum InternalCandleDirectionType
    {
        Unknown = 0,
        Expected_Up = 1,
        Expected_Down = 2,
        Not_Expected_Up = 3,
        Not_Expected_Down = 4
    }

    public enum MarketDirectionType
    {
        Unknown = 0,
        Up = 1,
        Down = 2
    }
}
