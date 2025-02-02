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
    public class CandleMetricEvaluation
    {
        public InternalCandle MainCandle { get; set; }
        public Dictionary<int, List<InternalCandle>> SubCandles { get; set; } = new Dictionary<int, List<InternalCandle>>();

        public Dictionary<int, List<InternalCandle>> GetSubCandles(CandleMetricOperation candleMetricOperation, CandleMetric candleMetric)
        {
            Dictionary<int, List<InternalCandle>> subCandles = new Dictionary<int, List<InternalCandle>>();

            foreach (var subCandlesKvp in this.SubCandles)
            {
                switch (candleMetricOperation)
                {
                    case CandleMetricOperation.AboveAverage:
                    case CandleMetricOperation.BelowAverage:
                    {
                        var average = subCandlesKvp.Value.GetCandlesMetricAverage(candleMetric);

                        if (candleMetricOperation == CandleMetricOperation.AboveAverage)
                        {
                            subCandles.Add(subCandlesKvp.Key, subCandlesKvp.Value.GetCandlesWithCandleMetricAboveValue(candleMetric, average));
                            break;
                        }
  
                        subCandles.Add(subCandlesKvp.Key, subCandlesKvp.Value.GetCandlesWithCandleMetricBelowValue(candleMetric, average));
                        break;
                    }

                    case CandleMetricOperation.AboveMedian:
                    case CandleMetricOperation.BelowMedian:
                    {
                        var median = subCandlesKvp.Value.GetCandlesMetricMedian(candleMetric);

                        if (candleMetricOperation == CandleMetricOperation.AboveMedian)
                        {
                            subCandles.Add(subCandlesKvp.Key, subCandlesKvp.Value.GetCandlesWithCandleMetricAboveValue(candleMetric, median));
                            break;
                        }

                        subCandles.Add(subCandlesKvp.Key, subCandlesKvp.Value.GetCandlesWithCandleMetricBelowValue(candleMetric, median));
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"Not supported candle metric operation: '{candleMetricOperation}'.");
                }
            }

            return subCandles;
        }

        //public Dictionary<int, List<InternalCandle>> GetSubCandles(CandleFilter candleFilter, CandleMetric candleMetric)
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = new Dictionary<int, List<InternalCandle>>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        if (candleFilter == CandleFilterAction.AboveAverage || candleFilter == CandleFilterAction.BelowAverage)
        //        {
        //            var average = kvp.Value.GetAverage(candleMetric);

        //            if (candleMetric == CandleMetric.ActiveTotalVolume)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveTotalVolume > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveTotalVolume < average).ToList());
        //                }
        //            }
        //            else if (candleMetric == CandleMetric.ActiveBuyVolume)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveBuyVolume > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveBuyVolume < average).ToList());
        //                }
        //            }
        //            else if (candleMetric == CandleMetric.ActiveSellVolume)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveSellVolume > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ActiveSellVolume < average).ToList());
        //                }
        //            }
        //            else if (candleMetric == CandleMetric.PassiveBuyVolume)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.PassiveBuyVolumePercentage > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.PassiveBuyVolumePercentage < average).ToList());
        //                }
        //            }
        //            else if (candleMetric == CandleMetric.PassiveSellVolume)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.PassiveSellVolumePercentage > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.PassiveSellVolumePercentage < average).ToList());
        //                }
        //            }
        //            else if (candleMetric == CandleMetric.ClosePrice)
        //            {
        //                if (candleFilter == CandleFilterAction.AboveAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ClosePrice > average).ToList());
        //                }
        //                else if (candleFilter == CandleFilterAction.BelowAverage)
        //                {
        //                    subCandles.Add(kvp.Key, kvp.Value.Where(x => x.ClosePrice < average).ToList());
        //                }
        //            }
        //        }
        //        else if (candleFilter == CandleFilterAction.AboveMedian || candleFilter == CandleFilterAction.BelowMedian)
        //        {

        //        }

        //        else
        //        {
        //            throw new InvalidOperationException($"Not supported candle filter operation: '{candleFilter}'.");
        //        }
        //    }

        //    return subCandles;
        //}


        //#region Deviation

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationActiveTotalVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveTotalVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveTotalVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveTotalVolume > averageActiveTotalVolume).OrderBy(x => x.ActiveTotalVolume).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationActiveTotalVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveTotalVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveTotalVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveTotalVolume < averageActiveTotalVolume).OrderBy(x => x.ActiveTotalVolume).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationActiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveBuyVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveBuyVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveBuyVolume > averageActiveBuyVolume).OrderBy(x => x.ActiveBuyVolume).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationActiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveBuyVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveBuyVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveBuyVolume < averageActiveBuyVolume).OrderBy(x => x.ActiveBuyVolume).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationActiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveSellVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveSellVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveSellVolume > averageActiveSellVolume).OrderBy(x => x.ActiveSellVolume).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationActiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageActiveSellVolume = kvp.Value.GetAverage(CandleMetricOperation.ActiveSellVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveSellVolume < averageActiveSellVolume).OrderBy(x => x.ActiveSellVolume).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationPassiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averagePassiveBuyVolume = kvp.Value.GetAverage(CandleMetricOperation.PassiveBuyVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveBuyVolume > averagePassiveBuyVolume).OrderBy(x => x.PassiveBuyVolumePercentage).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationPassiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averagePassiveBuyVolume = kvp.Value.GetAverage(CandleMetricOperation.PassiveBuyVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveBuyVolume < averagePassiveBuyVolume).OrderBy(x => x.PassiveBuyVolumePercentage).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationPassiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averagePassiveSellVolume = kvp.Value.GetAverage(CandleMetricOperation.PassiveSellVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveSellVolume > averagePassiveSellVolume).OrderBy(x => x.PassiveSellVolumePercentage).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationPassiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averagePassiveSellVolume = kvp.Value.GetAverage(CandleMetricOperation.PassiveSellVolume);

        //        var subCandle = kvp.Value.Where(x => x.ActiveSellVolume < averagePassiveSellVolume).OrderBy(x => x.PassiveSellVolumePercentage).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMaxAverageDeviationClosePrice()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageClosePrice = kvp.Value.GetAverage(CandleMetricOperation.ClosePrice);

        //        var subCandle = kvp.Value.Where(x => x.ClosePrice > averageClosePrice).OrderBy(x => x.ClosePrice).LastOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //public Dictionary<int, InternalCandle> GetSubCandleWithMinAverageDeviationClosePrice()
        //{
        //    Dictionary<int, InternalCandle> subCandles = new Dictionary<int, InternalCandle>();

        //    foreach (var kvp in this.SubCandles)
        //    {
        //        var averageClosePrice = kvp.Value.GetAverage(CandleMetricOperation.ClosePrice);

        //        var subCandle = kvp.Value.Where(x => x.ClosePrice < averageClosePrice).OrderBy(x => x.ClosePrice).FirstOrDefault();

        //        subCandles.Add(kvp.Key, subCandle);
        //    }

        //    return subCandles;
        //}

        //#endregion


        //#region Average dump

        //public string DumpSubCandlePositionsWithAboveAverageActiveTotalVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAverageActiveTotalVolume();

        //    return $"ABOVE AVERAGE active total volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveAverageActiveBuyVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAverageActiveBuyVolume();

        //    return $"ABOVE AVERAGE active buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveAverageActiveSellVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAverageActiveSellVolume();

        //    return $"ABOVE AVERAGE active sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveAveragePassiveBuyVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAveragePassiveBuyVolume();

        //    return $"ABOVE AVERAGE passive buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveAveragePassiveSellVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAveragePassiveSellVolume();

        //    return $"ABOVE AVERAGE passive sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveAverageClosePrice()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveAverageClosePrice();

        //    return $"ABOVE AVERAGE close price candles: {subCandles.DumpCandlesPositions()}";
        //}

        //#endregion


        //#region Median dump

        //public string DumpSubCandlePositionsWithAboveMedianActiveTotalVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianActiveTotalVolume();

        //    return $"ABOVE MEDIAN active total volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveMedianActiveBuyVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianActiveBuyVolume();

        //    return $"ABOVE MEDIAN active buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveMedianActiveSellVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianActiveSellVolume();

        //    return $"ABOVE MEDIAN active sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveMedianPassiveBuyVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianPassiveBuyVolume();

        //    return $"ABOVE MEDIAN passive buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveMedianPassiveSellVolume()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianPassiveSellVolume();

        //    return $"ABOVE MEDIAN passive sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionsWithAboveMedianClosePrice()
        //{
        //    Dictionary<int, List<InternalCandle>> subCandles = GetSubCandlesWithAboveMedianClosePrice();

        //    return $"ABOVE MEDIAN close price candles: {subCandles.DumpCandlesPositions()}";
        //}

        //#endregion



        //#region Deviation dump

        //public string DumpSubCandlePositionWithMaxAverageDeviationActiveTotalVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationActiveTotalVolume();

        //    return $"MAX AVERAGE DEVIATION active total volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationActiveTotalVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationActiveTotalVolume();

        //    return $"MIN AVERAGE DEVIATION active total volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMaxAverageDeviationActiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationActiveBuyVolume();

        //    return $"MAX AVERAGE DEVIATION active buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationActiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationActiveBuyVolume();

        //    return $"MIN AVERAGE DEVIATION active buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMaxAverageDeviationActiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationActiveSellVolume();

        //    return $"MAX AVERAGE DEVIATION active sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationActiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationActiveSellVolume();

        //    return $"MIN AVERAGE DEVIATION deviation active sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMaxAverageDeviationPassiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationPassiveBuyVolume();

        //    return $"MAX AVERAGE DEVIATION passive buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationPassiveBuyVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationPassiveBuyVolume();

        //    return $"MIN AVERAGE DEVIATION passive buy volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMaxAverageDeviationPassiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationPassiveSellVolume();

        //    return $"MAX AVERAGE DEVIATION passive sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationPassiveSellVolume()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationPassiveSellVolume();

        //    return $"MIN AVERAGE DEVIATION deviation passive sell volume candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMaxAverageDeviationClosePrice()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMaxAverageDeviationClosePrice();

        //    return $"MAX AVERAGE DEVIATION close price candles: {subCandles.DumpCandlesPositions()}";
        //}

        //public string DumpSubCandlePositionWithMinAverageDeviationClosePrice()
        //{
        //    Dictionary<int, InternalCandle> subCandles = GetSubCandleWithMinAverageDeviationClosePrice();

        //    return $"MIN AVERAGE DEVIATION close price candles: {subCandles.DumpCandlesPositions()}";
        //}

        //#endregion

        public string Dump()
        {
            return $"\n\n{this.MainCandle.Dump()}" +
                   $"\n\n---------------------------------------------------------------" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveAverageActiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveAverageActiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveAveragePassiveBuyVolume()}" + 
                   $"\n\n{this.DumpSubCandlePositionsWithAboveAveragePassiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveAverageClosePrice()}" +
                   $"\n\n---------------------------------------------------------------" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveMedianActiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveMedianActiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveMedianPassiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveMedianPassiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionsWithAboveMedianClosePrice()}" +
                   $"\n\n---------------------------------------------------------------" +
                   $"\n\n{this.DumpSubCandlePositionWithMaxAverageDeviationActiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMaxAverageDeviationActiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMaxAverageDeviationPassiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMaxAverageDeviationPassiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMaxAverageDeviationClosePrice()}" +
                   $"\n\n---------------------------------------------------------------" +
                   $"\n\n{this.DumpSubCandlePositionWithMinAverageDeviationActiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMinAverageDeviationActiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMinAverageDeviationPassiveBuyVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMinAverageDeviationPassiveSellVolume()}" +
                   $"\n\n{this.DumpSubCandlePositionWithMinAverageDeviationClosePrice()}\n";
        }
    }
}
