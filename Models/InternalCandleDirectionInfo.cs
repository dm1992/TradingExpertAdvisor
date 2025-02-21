using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class InternalCandleDirectionInfo
    {
        public List<InternalCandle> Candles { get; set; } = new List<InternalCandle>();

        public InternalCandleDirectionInfo(List<InternalCandle> candles)
        {
            this.Candles = candles;
        }


        public decimal AverageActiveBuyVolume
        {
            get
            {
                return this.Candles.GetCandlesMetricAverage(CandleMetric.ActiveBuyVolume);
            }
        }

        public List<InternalCandle> AboveAverageActiveBuyVolumeCandles
        {
            get
            {
                return this.Candles.GetCandlesWithCandleMetricAboveValue(CandleMetric.ActiveBuyVolume, this.AverageActiveBuyVolume);
            }
        }

        public decimal AverageActiveSellVolume
        {
            get
            {
                return this.Candles.GetCandlesMetricAverage(CandleMetric.ActiveSellVolume);
            }
        }

        public List<InternalCandle> AboveAverageActiveSellVolumeCandles
        {
            get
            {
                return this.Candles.GetCandlesWithCandleMetricAboveValue(CandleMetric.ActiveSellVolume, this.AverageActiveSellVolume);
            }
        }

        public decimal AverageDeltaPrice
        {
            get
            {
                return this.Candles.GetCandlesMetricAverage(CandleMetric.DeltaPrice);
            }
        }

        public List<InternalCandle> AboveAverageDeltaPriceCandles
        {
            get
            {
                return this.Candles.GetCandlesWithCandleMetricAboveValue(CandleMetric.DeltaPrice, this.AverageDeltaPrice);
            }
        }

        public InternalCandleDirection DirectionType
        {
            get
            {
                return GetDirectionType();
            }
        }

        private InternalCandleDirection GetDirectionType()
        {
            if (this.AverageActiveBuyVolume > this.AverageActiveSellVolume)
            {
                if (this.AverageDeltaPrice > 0)
                {
                    return InternalCandleDirection.Expected_Up;
                }
                else if (this.AverageDeltaPrice < 0)
                {
                    return InternalCandleDirection.Not_Expected_Down;
                }
            }
            else if (this.AverageActiveSellVolume > this.AverageActiveBuyVolume)
            {
                if (this.AverageDeltaPrice < 0)
                {
                    return InternalCandleDirection.Expected_Down;
                }
                else if (this.AverageDeltaPrice > 0)
                {
                    return InternalCandleDirection.Not_Expected_Up;
                }
            }

            return InternalCandleDirection.Unknown;
        }

        public string Dump()
        {
            return $"AverageActiveBuyVolume: {this.AverageActiveBuyVolume}, AboveAverageActiveBuyVolumeCandles: [ {String.Join(", ", this.AboveAverageActiveBuyVolumeCandles.Select(x => x.Position))} ]\n" +
                   $"AverageActiveSellVolume: {this.AverageActiveSellVolume}, AboveAverageActiveSellVolumeCandles: [ {String.Join(", ", this.AboveAverageActiveSellVolumeCandles.Select(x => x.Position))} ]\n" +
                   $"AverageDeltaPrice: {this.AverageDeltaPrice}, AboveAverageDeltaPriceCandles: [ {String.Join(", ", this.AboveAverageDeltaPriceCandles.Select(x => x.Position))} ]\n" +
                   $"DirectionType: {this.DirectionType}";
        }
    }
}
