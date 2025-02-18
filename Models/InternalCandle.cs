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
        public int Position { get; set; }
        public string Symbol { get; set; }
        public int Timeframe { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public List<InternalTrade> Trades { get; set; } = new List<InternalTrade>();
        public InternalOrderbook Orderbook { get; set; }
        public decimal ActiveTotalVolume 
        { 
            get 
            { 
                return GetActiveTotalVolume(); 
            } 
        }
        public decimal ActiveBuyVolume 
        { 
            get 
            {  
                return GetActiveBuyVolume(); 
            } 
        }
        public decimal ActiveSellVolume 
        { 
            get 
            { 
                return GetActiveSellVolume(); 
            } 
        }
        public decimal ActiveBuyVolumePercentage 
        { 
            get 
            { 
                return GetActiveBuyVolumePercentage(); 
            } 
        }
        public decimal ActiveSellVolumePercentage 
        { 
            get 
            { 
                return GetActiveSellVolumePercentage(); 
            } 
        }
        public decimal PassiveBuyVolumePercentage 
        { 
            get 
            { 
                return GetPassiveBuyVolumePercentage(); 
            } 
        }
        public decimal PassiveSellVolumePercentage 
        { 
            get 
            { 
                return GetPassiveSellVolumePercentage(); 
            } 
        }
        public decimal OpenPrice 
        { 
            get 
            { 
                return GetOpenPrice(); 
            } 
        }
        public decimal HighPrice 
        { 
            get 
            { 
                return GetHighPrice(); 
            } 
        }
        public decimal LowPrice 
        { 
            get 
            { 
                return GetLowPrice(); 
            } 
        }
        public decimal ClosePrice 
        { 
            get 
            { 
                return GetClosePrice(); 
            } 
        }
        public decimal DeltaPrice 
        { 
            get 
            { 
                return GetDeltaPrice(); 
            } 
        }
        public InternalCandleDirection DirectionType 
        { 
            get 
            { 
                return GetDirectionType(); 
            } 
        }

        public string Dump()
        {
            return $"\n-------------------------------------------------------------CANDLE INFO-------------------------------------------------------------\n" +
                   $"----- {this.Symbol} / {this.Timeframe}min / StartTime {this.StartTime} / CloseTime {this.CloseTime} / Trades: {this.Trades.Count} -----\n" +
                   $"--------------------------------------------------------------ACTIVE VOLUME------------------------------------------------------------\n" +
                   $"ABV: {this.ActiveBuyVolume} ({this.ActiveBuyVolumePercentage}%), ASV: {this.ActiveSellVolume} ({this.ActiveSellVolumePercentage}%), TAV: {this.ActiveTotalVolume}\n" +
                   $"--------------------------------------------------------------PASSIVE VOLUME-----------------------------------------------------------\n" +
                   $"PBV_5: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 5)}%, PSV_5: {this.GetPassiveSellVolumePercentage(orderbookDepth: 5)}%\n" +
                   $"PBV_10: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 10)}%, PSV_10: {this.GetPassiveSellVolumePercentage(orderbookDepth: 10)}%\n" +
                   $"PBV_20: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 20)}%, PSV_20: {this.GetPassiveSellVolumePercentage(orderbookDepth: 20)}%\n" +
                   $"PBV_30: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 30)}%, PSV_30: {this.GetPassiveSellVolumePercentage(orderbookDepth: 30)}%\n" +
                   $"PBV_40: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 40)}%, PSV_40: {this.GetPassiveSellVolumePercentage(orderbookDepth: 40)}%\n" +
                   $"PBV_50: {this.GetPassiveBuyVolumePercentage(orderbookDepth: 50)}%, PSV_50: {this.GetPassiveSellVolumePercentage(orderbookDepth: 50)}%\n" +
                   $"-----------------------------------------------------------------PRICE-----------------------------------------------------------------\n" +
                   $"O: {this.OpenPrice} H: {this.HighPrice}, L: {this.LowPrice}, C: {this.ClosePrice}, Delta: {this.DeltaPrice}\n" +
                   $"---------------------------------------------------------------PREDICTION--------------------------------------------------------------\n" +
                   $"DIRECTION: {this.DirectionType}\n" +
                   $"---------------------------------------------------------------------------------------------------------------------------------------\n";
        }


        #region Market metric calculations

        private InternalCandleDirection GetDirectionType()
        {
            decimal activeBuyVolumePercentage = this.GetActiveBuyVolumePercentage();
            decimal activeSellVolumePercentage = this.GetActiveSellVolumePercentage();
            decimal deltaPrice = this.GetDeltaPrice();

            if (activeBuyVolumePercentage > activeSellVolumePercentage)
            {
                if (deltaPrice > 0)
                {
                    return InternalCandleDirection.Expected_Up;
                }
                else if (deltaPrice < 0)
                {
                    return InternalCandleDirection.Not_Expected_Down;
                }
            }
            else if (activeSellVolumePercentage > activeBuyVolumePercentage)
            {
                if (deltaPrice < 0)
                {
                    return InternalCandleDirection.Expected_Down;
                }
                else if (deltaPrice > 0)
                {
                    return InternalCandleDirection.Not_Expected_Up;
                }
            }

            return InternalCandleDirection.Unknown;
        }

        private decimal GetActiveBuyVolume()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Where(x => x.TradeDirection == TradeDirection.Buy).Sum(x => x.Volume);
        }

        private decimal GetActiveSellVolume()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Where(x => x.TradeDirection == TradeDirection.Sell).Sum(x => x.Volume);
        }

        private decimal GetActiveTotalVolume()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Sum(x => x.Volume);
        }

        private decimal GetActiveBuyVolumePercentage()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            decimal totalVolume = this.Trades.Sum(x => x.Volume);
            decimal buyVolume = this.Trades.Where(x => x.TradeDirection == TradeDirection.Buy).Sum(x => x.Volume);

            return Math.Round((buyVolume / totalVolume) * 100.0m, 2);
        }

        private decimal GetActiveSellVolumePercentage()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            decimal totalVolume = this.Trades.Sum(x => x.Volume);
            decimal sellVolume = this.Trades.Where(x => x.TradeDirection == TradeDirection.Sell).Sum(x => x.Volume);

            return Math.Round((sellVolume / totalVolume) * 100.0m, 2);
        }

        private decimal GetPassiveBuyVolumePercentage(int orderbookDepth = 5)
        {
            if (this.Orderbook == null)
                return 0;

            decimal askVolume = this.Orderbook.Asks.Take(orderbookDepth).Sum(x => x.Quantity);
            decimal bidVolume = this.Orderbook.Bids.Take(orderbookDepth).Sum(x => x.Quantity);

            return Math.Round((bidVolume / (askVolume + bidVolume)) * 100, 2);
        }

        private decimal GetPassiveSellVolumePercentage(int orderbookDepth = 5)
        {
            if (this.Orderbook == null)
                return 0;

            decimal askVolume = this.Orderbook.Asks.Take(orderbookDepth).Sum(x => x.Quantity);
            decimal bidVolume = this.Orderbook.Bids.Take(orderbookDepth).Sum(x => x.Quantity);

            return Math.Round((askVolume / (askVolume + bidVolume)) * 100, 2);
        }

        private decimal GetOpenPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.First().Price;
        }

        private decimal GetHighPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Max(x => x.Price);
        }

        private decimal GetLowPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Min(x => x.Price);
        }

        private decimal GetClosePrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.Trades.Last().Price;
        }

        private decimal GetDeltaPrice()
        {
            if (this.Trades.IsNullOrEmpty())
                return 0;

            return this.GetClosePrice() - this.GetOpenPrice();
        }

        #endregion
    }
}
