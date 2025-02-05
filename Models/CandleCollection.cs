using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    // ----- comments

    // get direction of last candle 1h and perform statistical evaluation on 2x30min, 4x15min, 12x5min and 60x1min candles.
    // calculate average total volume, buy and sell volume and price reaction and find where in the pattern are this candles above average located (position).
    // When location is found check if general 1h candle direction coralates to above average candle directions?

    // strong COMPONENT and vice versa weak component (volume, price...)
    public class CandleCollection
    {
        /// <summary>
        /// Main candle like 5 minutes candle.
        /// </summary>
        public InternalCandle MainCandle { get; set; }

        /// <summary>
        /// Sub candles like 5 times 1 minute candle.
        /// </summary>
        public Dictionary<int, List<InternalCandle>> TimeframeSubCandles { get; set; } = new Dictionary<int, List<InternalCandle>>();


        #region Candle collection filter methods

        public Dictionary<int, List<InternalCandle>> FilterTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
        {
            switch (candleFilter)
            {
                case CandleFilter.AboveAverage:
                case CandleFilter.BelowAverage:
                case CandleFilter.MaxAverageDeviation:
                case CandleFilter.MinAverageDeviation:
                    return AverageFilterTimeframeSubCandlesWithCandleMetric(candleFilter, candleMetric);

                case CandleFilter.AboveMedian:
                case CandleFilter.BelowMedian:
                case CandleFilter.MaxMedianDeviation:
                case CandleFilter.MinMedianDeviation:
                    return MedianFilterTimeframeSubCandlesWithCandleMetric(candleFilter, candleMetric);

                default:
                    throw new InvalidOperationException($"FilterTimeframeSubCandlesWithCandleMetric error! Not supported candle filter '{candleFilter}'.");
            }
        }

        private Dictionary<int, List<InternalCandle>> AverageFilterTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
        {
            Dictionary<int, List<InternalCandle>> timeframeSubCandles = new Dictionary<int, List<InternalCandle>>();

            foreach (var kvp in this.TimeframeSubCandles)
            {
                switch (candleFilter)
                {
                    case CandleFilter.AboveAverage:
                    case CandleFilter.BelowAverage:
                    case CandleFilter.MaxAverageDeviation:
                    case CandleFilter.MinAverageDeviation:
                    {
                        var average = kvp.Value.GetCandlesMetricAverage(candleMetric);

                        if (candleFilter == CandleFilter.AboveAverage)
                        {
                            var candles = kvp.Value.GetCandlesWithCandleMetricAboveValue(candleMetric, average);

                            timeframeSubCandles.Add(kvp.Key, candles);
                        }
                        else if (candleFilter == CandleFilter.BelowAverage)
                        {
                            var candles = kvp.Value.GetCandlesWithCandleMetricBelowValue(candleMetric, average);

                            timeframeSubCandles.Add(kvp.Key, candles);
                        }
                        else if (candleFilter == CandleFilter.MaxAverageDeviation)
                        {
                            var candle = kvp.Value.GetCandleWithMaxCandleMetricAboveValue(candleMetric, average);

                            if (candle != null)
                            {
                                timeframeSubCandles.Add(kvp.Key, new List<InternalCandle> { candle });
                            }
                        }
                        else if (candleFilter == CandleFilter.MinAverageDeviation)
                        {
                            var candle = kvp.Value.GetCandleWithMinCandleMetricBelowValue(candleMetric, average);

                            if (candle != null)
                            {
                                timeframeSubCandles.Add(kvp.Key, new List<InternalCandle> { candle });
                            }
                        }

                        break;
                    }

                    default:
                        throw new InvalidOperationException($"AverageFilterTimeframeSubCandlesWithCandleMetric error! Not supported candle filter '{candleFilter}'.");
                }
            }

            return timeframeSubCandles;
        }

        private Dictionary<int, List<InternalCandle>> MedianFilterTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
        {
            Dictionary<int, List<InternalCandle>> timeframeSubCandles = new Dictionary<int, List<InternalCandle>>();

            foreach (var kvp in this.TimeframeSubCandles)
            {
                switch (candleFilter)
                {
                    case CandleFilter.AboveMedian:
                    case CandleFilter.BelowMedian:
                    case CandleFilter.MaxMedianDeviation:
                    case CandleFilter.MinMedianDeviation:
                    {
                        var median = kvp.Value.GetCandlesMetricMedian(candleMetric);

                        if (candleFilter == CandleFilter.AboveMedian)
                        {
                            var candles = kvp.Value.GetCandlesWithCandleMetricAboveValue(candleMetric, median);

                            timeframeSubCandles.Add(kvp.Key, candles);
                        }
                        else if (candleFilter == CandleFilter.BelowMedian)
                        {
                            var candles = kvp.Value.GetCandlesWithCandleMetricBelowValue(candleMetric, median);

                            timeframeSubCandles.Add(kvp.Key, candles);
                        }
                        else if (candleFilter == CandleFilter.MaxMedianDeviation)
                        {
                            var candle = kvp.Value.GetCandleWithMaxCandleMetricAboveValue(candleMetric, median);

                            if (candle != null)
                            {
                                timeframeSubCandles.Add(kvp.Key, new List<InternalCandle> { candle });
                            }
                        }
                        else if (candleFilter == CandleFilter.MinMedianDeviation)
                        {
                            var candle = kvp.Value.GetCandleWithMinCandleMetricBelowValue(candleMetric, median);

                            if (candle != null)
                            {
                                timeframeSubCandles.Add(kvp.Key, new List<InternalCandle> { candle });
                            }
                        }

                        break;
                    }

                    default:
                        throw new InvalidOperationException($"MedianFilterTimeframeSubCandlesWithCandleMetric error! Not supported candle filter '{candleFilter}'.");
                }
            }

            return timeframeSubCandles;
        }


        #endregion

        public string Dump()
        {
            string dump = $"\n\n{this.MainCandle.Dump()}";

            foreach(var candleFilter in Enum.GetValues(typeof(CandleFilter)))
            {
                foreach(var candleMetric in Enum.GetValues(typeof(CandleMetric)))
                {
                    dump += DumpCandlesPositionsOnFilteredTimeframeSubCandlesWithCandleMetric((CandleFilter)candleFilter, (CandleMetric)candleMetric);
                }
            }

            return dump;
        }

        public string DumpCandlesPositionsOnFilteredTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
        {
            string dump = $"\n------ Dumping timeframe subcandles positions with CANDLE FILTER: {candleFilter} on CANDLE METRIC: {candleMetric} --------";

            var timeframeSubCandles = FilterTimeframeSubCandlesWithCandleMetric((CandleFilter)candleFilter, (CandleMetric)candleMetric);

            dump += $"\n{timeframeSubCandles.DumpCandlesPositions()}";

            dump += $"\n\n----------------------------------------------";

            return dump;
        }
    }
}
