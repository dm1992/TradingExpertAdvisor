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

    public enum InternalCandleDirection
    {
        Unknown = 0,
        Expected_Up = 1,
        Expected_Down = 2,
        Not_Expected_Up = 3,
        Not_Expected_Down = 4
    }

    public enum MarketDirection
    {
        Unknown = 0,
        Up = 1,
        Down = 2
    }

    public enum CandleMetric
    { 
        ActiveTotalVolume = 1,
        ActiveBuyVolume = 2,
        ActiveSellVolume = 3,
        PassiveBuyVolume = 4,
        PassiveSellVolume = 5,
        DeltaPrice = 6
    }

    public enum CandleFilter
    {
        AboveAverage = 1,
        BelowAverage = 2,
        AboveMedian = 3,
        BelowMedian = 4,
        MaxAverageDeviation = 5,
        MinAverageDeviation = 6,
        MaxMedianDeviation = 7,
        MinMedianDeviation = 8
    }
}
