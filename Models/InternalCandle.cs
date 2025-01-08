using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    /// <summary>
    /// Candle data internal abstraction model.
    /// </summary>
    public class InternalCandle
    {
        public string Symbol { get; set; }
        public int Timeframe { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public InternalCandleDirectionType DirectionType { get { return GetDirectionType(); } }
        public List<InternalTrade> Trades { get; set; } = new List<InternalTrade>();
        public InternalOrderbook Orderbook { get; set; }

        public decimal GetActiveBuyVolumePercentage()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            decimal totalVolume = this.Trades.Sum(x => x.Volume);
            decimal buyVolume = this.Trades.Where(x => x.TradeDirection == TradeDirection.Buy).Sum(x => x.Volume);

            return Math.Round((buyVolume / totalVolume) * 100.0m, 2);
        }

        public decimal GetActiveSellVolumePercentage()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            decimal totalVolume = this.Trades.Sum(x => x.Volume);
            decimal sellVolume = this.Trades.Where(x => x.TradeDirection == TradeDirection.Sell).Sum(x => x.Volume);

            return Math.Round((sellVolume / totalVolume) * 100.0m, 2);
        }

        public decimal GetPassiveBuyVolumePercentage(int orderbookDepth = 5)
        {
            if (this.Orderbook == null)
                return 0;

            decimal askVolume = this.Orderbook.Asks.Take(orderbookDepth).Sum(x => x.Quantity);
            decimal bidVolume = this.Orderbook.Bids.Take(orderbookDepth).Sum(x => x.Quantity);

            return Math.Round((bidVolume / (askVolume + bidVolume)) * 100, 2);
        }

        public decimal GetPassiveSellVolumePercentage(int orderbookDepth = 5)
        {
            if (this.Orderbook == null)
                return 0;

            decimal askVolume = this.Orderbook.Asks.Take(orderbookDepth).Sum(x => x.Quantity);
            decimal bidVolume = this.Orderbook.Bids.Take(orderbookDepth).Sum(x => x.Quantity);

            return Math.Round((askVolume / (askVolume + bidVolume)) * 100, 2);
        }

        public decimal GetOpenPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.First().Price;
        }

        public decimal GetHighPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Max(x => x.Price);
        }

        public decimal GetLowPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Min(x => x.Price);
        }

        public decimal GetClosePrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Last().Price;
        }

        public decimal GetDeltaPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.GetClosePrice() - this.GetOpenPrice();
        }

        public InternalCandleDirectionType GetDirectionType()
        {
            decimal activeBuyVolumePercentage = this.GetActiveBuyVolumePercentage();
            decimal activeSellVolumePercentage = this.GetActiveSellVolumePercentage();
            decimal deltaPrice = this.GetDeltaPrice();

            if (activeBuyVolumePercentage > activeSellVolumePercentage)
            {
                if (deltaPrice > 0)
                {
                    return InternalCandleDirectionType.Expected_Up;
                }
                else if (deltaPrice < 0)
                {
                    return InternalCandleDirectionType.Not_Expected_Down;
                }
            }
            else if (activeSellVolumePercentage > activeBuyVolumePercentage)
            {
                if (deltaPrice < 0)
                {
                    return InternalCandleDirectionType.Expected_Down;
                }
                else if (deltaPrice > 0)
                {
                    return InternalCandleDirectionType.Not_Expected_Up;
                }
            }

            return InternalCandleDirectionType.Unknown;
        }

        public string DumpOrderbookItems(int orderbookDepth = 5)
        {
            if (this.Orderbook == null)
                return null;

            string dump = null;

            if (!this.Orderbook.Asks.IsNullOrEmpty())
            {
                dump += $"Asks: ({String.Join(",", this.Orderbook.Asks.Take(orderbookDepth).Select(x => x.Dump()))})";
            }
            else if (!this.Orderbook.Bids.IsNullOrEmpty())
            {
                if (!dump.IsNullOrEmpty())
                {
                    dump += Environment.NewLine;
                }

                dump += $"Bids: ({String.Join(",", this.Orderbook.Bids.Take(orderbookDepth).Select(x => x.Dump()))})";
            }

            return dump;
        }

        public string Dump()
        {
            return $"----- {this.Symbol} / {this.Timeframe}min / StartTime {this.StartTime} / CloseTime {this.CloseTime} / Trades: {this.Trades.Count} -----\n" +
                   $"ABV%: {this.GetActiveBuyVolumePercentage()}, ASV%: {this.GetActiveSellVolumePercentage()}\n" +
                   $"PBV_5%: {this.GetPassiveBuyVolumePercentage()}, PSV_5%: {this.GetPassiveSellVolumePercentage()}, PBV_10%: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 10)}, PSV_10%: {this.GetPassiveSellVolumePercentage(orderbookDepth: 10)}\n" +
                   $"{this.DumpOrderbookItems()}\n" +
                   $"O: {this.GetOpenPrice()} H: {this.GetHighPrice()}, L: {this.GetLowPrice()}, C: {this.GetClosePrice()}, Delta: {this.GetDeltaPrice()}\n" +
                   $"Direction: {this.GetDirectionType()}";
        }
    }
}
