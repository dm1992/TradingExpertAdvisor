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

        /// <summary>
        /// Determined by main candle.
        /// </summary>
        public string Symbol
        {
            get
            {
                return this.MainCandle?.Symbol ?? "N/A";
            }
        }

        /// <summary>
        /// Determined by main candle.
        /// </summary>
        public int Timeframe
        {
            get
            {
                return this.MainCandle?.Timeframe ?? -1;
            }
        }

        /// <summary>
        /// Determined by main candle.
        /// </summary>
        public InternalCandleDirection DirectionType
        {
            get
            {
                return this.MainCandle?.DirectionType ?? InternalCandleDirection.Unknown;
            }
        }

        /// <summary>
        /// Ignoring candle metrics and using all timeframe sub candles.
        /// </summary>
        public decimal DirectionTypeGeneralPercentage 
        { 
            get 
            { 
                return GetDirectionTypePercentage_TimeframeSubCandles(); 
            } 
        }

        public Dictionary<CandleFilter, Dictionary<CandleMetric, decimal>> DirectionTypeFilterMetricPercentages
        {
            get
            {
                return GetDirectionTypePercentage_FilterTimeframeSubCandlesMetric();
            }
        }


        private decimal GetDirectionTypePercentage_TimeframeSubCandles()
        {
            if (this.DirectionType == InternalCandleDirection.Unknown)
                return 0;

            if (this.TimeframeSubCandles.IsNullOrEmpty())
                return 0;

            return (this.TimeframeSubCandles.Count(x => x.Value.Count(y => y.DirectionType == this.DirectionType) > 0) /
                    this.TimeframeSubCandles.Count(x => x.Value.Count() > 0)) * 100.0m;
        }

        private Dictionary<CandleFilter, Dictionary<CandleMetric, decimal>> GetDirectionTypePercentage_FilterTimeframeSubCandlesMetric()
        {
            Dictionary<CandleFilter, Dictionary<CandleMetric, decimal>> candleFilterMetricPercentages = new Dictionary<CandleFilter, Dictionary<CandleMetric, decimal>>();

            foreach (CandleFilter candleFilter in Enum.GetValues(typeof(CandleFilter)))
            {
                candleFilterMetricPercentages.Add(candleFilter, GetDirectionTypePercentage_TimeframeSubCandlesMetricWithFilter(candleFilter));
            }

            return candleFilterMetricPercentages;
        }

        private Dictionary<CandleMetric, decimal> GetDirectionTypePercentage_TimeframeSubCandlesMetricWithFilter(CandleFilter candleFilter)
        {
            if (this.DirectionType == InternalCandleDirection.Unknown)
                return null;

            if (this.TimeframeSubCandles.IsNullOrEmpty())
                return null;

            Dictionary<CandleMetric, decimal> candleMetricPercentages = new Dictionary<CandleMetric, decimal>();

            foreach (CandleMetric candleMetric in Enum.GetValues(typeof(CandleMetric)))
            {
                var timeframeSubCandles = FilterTimeframeSubCandlesWithCandleMetric(candleFilter, candleMetric);
                var percentage = 0.0m;

                if (!timeframeSubCandles.IsNullOrEmpty())
                {
                    percentage = (timeframeSubCandles.Count(x => x.Value.Count(y => y.DirectionType == this.DirectionType) > 0) /
                                  timeframeSubCandles.Count(x => x.Value.Count() > 0)) * 100.0m;
                }

                candleMetricPercentages.Add(candleMetric, percentage);
            }

            return candleMetricPercentages;
        }


        #region Candle collection filter methods

        private Dictionary<int, List<InternalCandle>> FilterTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
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

        //public string Dump()
        //{
        //    string dump = $"\n\n{this.MainCandle.Dump()}";

        //    foreach(var candleFilter in Enum.GetValues(typeof(CandleFilter)))
        //    {
        //        foreach(var candleMetric in Enum.GetValues(typeof(CandleMetric)))
        //        {
        //            dump += DumpCandlesPositionsOnFilteredTimeframeSubCandlesWithCandleMetric((CandleFilter)candleFilter, (CandleMetric)candleMetric);
        //        }
        //    }

        //    return dump;
        //}

        //public string DumpCandlesPositionsOnFilteredTimeframeSubCandlesWithCandleMetric(CandleFilter candleFilter, CandleMetric candleMetric)
        //{
        //    string dump = $"\n------ Dumping timeframe subcandles positions with CANDLE FILTER: {candleFilter} on CANDLE METRIC: {candleMetric} --------";

        //    var timeframeSubCandles = FilterTimeframeSubCandlesWithCandleMetric((CandleFilter)candleFilter, (CandleMetric)candleMetric);

        //    dump += $"\n{timeframeSubCandles.DumpCandlesPositions()}";

        //    dump += $"\n\n----------------------------------------------";

        //    return dump;
        //}
    }
}
