using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class SimulationTrade : InternalTrade
    {
        public decimal EntryPrice { get; set; }
        public decimal TakeProfitAmount { get; set; }
        public decimal TakeProfitPrice
        {
            get
            {
                if (TradeDirection == TradeDirection.Buy)
                {
                    return EntryPrice + TakeProfitAmount;
                }
                else if (TradeDirection == TradeDirection.Sell)
                {
                    return EntryPrice - TakeProfitAmount;
                }

                return 0;
            }

        }
        public decimal StopLossAmount { get; set; }

        public decimal StopLossPrice
        {
            get
            {
                if (TradeDirection == TradeDirection.Buy)
                {
                    return EntryPrice - StopLossAmount;
                }
                else if (TradeDirection == TradeDirection.Sell)
                {
                    return EntryPrice + StopLossAmount;
                }

                return 0;
            }

        }

        public decimal? Balance { get; set; }

        public decimal? ExitPrice
        {
            get
            {
                if (!Balance.HasValue)
                    return null;

                return EntryPrice + Balance;
            }
        }

        public decimal? MaxPrice { get; set; }
        public decimal? MinPrice { get; set; }

        public bool HasCompleted { get { return ExitPrice.HasValue; } }
    }
}
