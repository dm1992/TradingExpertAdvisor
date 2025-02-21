using Bybit.Net.Enums;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Models;

namespace TradingExpertAdvisor
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            if (list == null || !list.Any())
                return true;

            return false;
        }

        public static int? GetTimeframe(this KlineInterval interval)
        {
            switch (interval)
            {
                case KlineInterval.OneMinute:
                    return 1;
                case KlineInterval.ThreeMinutes:
                    return 3;
                case KlineInterval.FiveMinutes:
                    return 5;
                case KlineInterval.FifteenMinutes:
                    return 15;
                case KlineInterval.ThirtyMinutes:
                    return 30;
                case KlineInterval.OneHour:
                    return 60;
                case KlineInterval.TwoHours:
                    return 120;
                case KlineInterval.FourHours:
                    return 240;
                case KlineInterval.SixHours:
                    return 360;
                case KlineInterval.TwelveHours:
                    return 720;
                case KlineInterval.OneDay:
                    return 1440;
                case KlineInterval.OneWeek:
                    return 10080;
                case KlineInterval.OneMonth:
                    return 43829;

                default:
                    return null;

            }
        }

        public static KlineInterval? GetKlineInterval(this int value)
        {
            switch (value)
            {
                case 1:
                    return KlineInterval.OneMinute;
                case 3:
                    return KlineInterval.ThreeMinutes;
                case 5:
                    return KlineInterval.FiveMinutes;
                case 15:
                    return KlineInterval.FifteenMinutes;
                case 30:
                    return KlineInterval.ThirtyMinutes;
                case 60:
                    return KlineInterval.OneHour;
                case 120:
                    return KlineInterval.TwoHours;
                case 240:
                    return KlineInterval.FourHours;
                case 360:
                    return KlineInterval.SixHours;
                case 720:
                    return KlineInterval.TwelveHours;
                case 1440:
                    return KlineInterval.OneDay;
                case 10080:
                    return KlineInterval.OneWeek;
                case 43829:
                    return KlineInterval.OneMonth;

                default:
                    return null;

            }
        }

        public static List<InternalCandle> GetCandlesWithCandleMetricAboveValue(this List<InternalCandle> candles, CandleMetric candleMetric, decimal value)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            switch (candleMetric)
            {
                case CandleMetric.ActiveTotalVolume:
                    return candles.Where(x => x.ActiveTotalVolume > value).ToList();

                case CandleMetric.ActiveBuyVolume:
                    return candles.Where(x => x.ActiveBuyVolume > value).ToList();

                case CandleMetric.ActiveSellVolume:
                    return candles.Where(x => x.ActiveSellVolume > value).ToList();

                case CandleMetric.PassiveBuyVolume:
                    return candles.Where(x => x.PassiveBuyVolumePercentage > value).ToList();

                case CandleMetric.PassiveSellVolume:
                    return candles.Where(x => x.PassiveSellVolumePercentage > value).ToList();

                case CandleMetric.DeltaPrice:
                    return candles.Where(x => x.DeltaPrice > value).ToList();

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{candleMetric}'.");
            }
        }

        public static List<InternalCandle> GetCandlesWithCandleMetricBelowValue(this List<InternalCandle> candles, CandleMetric candleMetric, decimal value)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            switch (candleMetric)
            {
                case CandleMetric.ActiveTotalVolume:
                    return candles.Where(x => x.ActiveTotalVolume < value).ToList();

                case CandleMetric.ActiveBuyVolume:
                    return candles.Where(x => x.ActiveBuyVolume < value).ToList();

                case CandleMetric.ActiveSellVolume:
                    return candles.Where(x => x.ActiveSellVolume < value).ToList();

                case CandleMetric.PassiveBuyVolume:
                    return candles.Where(x => x.PassiveBuyVolumePercentage < value).ToList();

                case CandleMetric.PassiveSellVolume:
                    return candles.Where(x => x.PassiveSellVolumePercentage < value).ToList();

                case CandleMetric.DeltaPrice:
                    return candles.Where(x => x.DeltaPrice < value).ToList();

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{candleMetric}'.");
            }
        }

        public static InternalCandle GetCandleWithMaxCandleMetricAboveValue(this List<InternalCandle> candles, CandleMetric candleMetric, decimal value)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            switch (candleMetric)
            {
                case CandleMetric.ActiveTotalVolume:
                    return candles.Where(x => x.ActiveTotalVolume > value).OrderBy(x => x.ActiveTotalVolume).LastOrDefault();

                case CandleMetric.ActiveBuyVolume:
                    return candles.Where(x => x.ActiveBuyVolume > value).OrderBy(x => x.ActiveBuyVolume).LastOrDefault();

                case CandleMetric.ActiveSellVolume:
                    return candles.Where(x => x.ActiveSellVolume > value).OrderBy(x => x.ActiveSellVolume).LastOrDefault();

                case CandleMetric.PassiveBuyVolume:
                    return candles.Where(x => x.PassiveBuyVolumePercentage > value).OrderBy(x => x.PassiveBuyVolumePercentage).LastOrDefault();

                case CandleMetric.PassiveSellVolume:
                    return candles.Where(x => x.PassiveSellVolumePercentage > value).OrderBy(x => x.PassiveSellVolumePercentage).LastOrDefault();

                case CandleMetric.DeltaPrice:
                    return candles.Where(x => x.DeltaPrice > value).OrderBy(x => x.DeltaPrice).LastOrDefault();

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{candleMetric}'.");
            }
        }

        public static InternalCandle GetCandleWithMinCandleMetricBelowValue(this List<InternalCandle> candles, CandleMetric candleMetric, decimal value)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            switch (candleMetric)
            {
                case CandleMetric.ActiveTotalVolume:
                    return candles.Where(x => x.ActiveTotalVolume < value).OrderBy(x => x.ActiveTotalVolume).FirstOrDefault();

                case CandleMetric.ActiveBuyVolume:
                    return candles.Where(x => x.ActiveBuyVolume < value).OrderBy(x => x.ActiveBuyVolume).FirstOrDefault();

                case CandleMetric.ActiveSellVolume:
                    return candles.Where(x => x.ActiveSellVolume < value).OrderBy(x => x.ActiveSellVolume).FirstOrDefault();

                case CandleMetric.PassiveBuyVolume:
                    return candles.Where(x => x.PassiveBuyVolumePercentage < value).OrderBy(x => x.PassiveBuyVolumePercentage).FirstOrDefault();

                case CandleMetric.PassiveSellVolume:
                    return candles.Where(x => x.PassiveSellVolumePercentage < value).OrderBy(x => x.PassiveSellVolumePercentage).FirstOrDefault();

                case CandleMetric.DeltaPrice:
                    return candles.Where(x => x.DeltaPrice < value).OrderBy(x => x.DeltaPrice).FirstOrDefault();

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{candleMetric}'.");
            }
        }

        public static decimal GetCandlesMetricAverage(this List<InternalCandle> candles, CandleMetric metric)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            switch (metric)
            {
                case CandleMetric.ActiveTotalVolume:
                    return candles.Average(x => x.ActiveTotalVolume);

                case CandleMetric.ActiveBuyVolume:
                    return candles.Average(x => x.ActiveBuyVolume);

                case CandleMetric.ActiveSellVolume:
                    return candles.Average(x => x.ActiveSellVolume);

                case CandleMetric.PassiveBuyVolume:
                    return candles.Average(x => x.PassiveBuyVolumePercentage);

                case CandleMetric.PassiveSellVolume:
                    return candles.Average(x => x.PassiveSellVolumePercentage);

                case CandleMetric.DeltaPrice:
                    return candles.Average(x => x.DeltaPrice);

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{metric}'.");
            }
        }

        public static decimal GetCandlesMetricMedian(this List<InternalCandle> candles, CandleMetric metric)
        {
            if (candles.IsNullOrEmpty())
                throw new ArgumentNullException("Candles cannot be empty.");

            List<decimal> metrics;

            switch (metric)
            {
                case CandleMetric.ActiveTotalVolume:
                    metrics = candles.OrderBy(x => x.ActiveTotalVolume).Select(x => x.ActiveTotalVolume).ToList();
                    break;

                case CandleMetric.ActiveBuyVolume:
                    metrics = candles.OrderBy(x => x.ActiveBuyVolume).Select(x => x.ActiveBuyVolume).ToList();
                    break;

                case CandleMetric.ActiveSellVolume:
                    metrics = candles.OrderBy(x => x.ActiveSellVolume).Select(x => x.ActiveSellVolume).ToList();
                    break;

                case CandleMetric.PassiveBuyVolume:
                    metrics = candles.OrderBy(x => x.PassiveBuyVolumePercentage).Select(x => x.PassiveBuyVolumePercentage).ToList();
                    break;

                case CandleMetric.PassiveSellVolume:
                    metrics = candles.OrderBy(x => x.PassiveSellVolumePercentage).Select(x => x.PassiveSellVolumePercentage).ToList();
                    break;

                case CandleMetric.DeltaPrice:
                    metrics = candles.OrderBy(x => x.DeltaPrice).Select(x => x.DeltaPrice).ToList();
                    break;

                default:
                    throw new InvalidOperationException($"Not supported candle metric: '{metric}'.");

            }

            int count = metrics.Count;
            int middle = count / 2;

            if (count % 2 == 0)
                return (metrics[middle - 1] + metrics[middle]) / 2.0m;

            return metrics[middle];
        }
    }
}
