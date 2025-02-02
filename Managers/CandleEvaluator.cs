using CryptoCom.Net.Enums;
using CryptoExchange.Net.CommonObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers.Options;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;
using XT.Net.Objects.Models;

namespace TradingExpertAdvisor.Managers
{
    public class CandleEvaluator : ICandleEvaluator
    {
        public event EventHandler<CandleEvaluatedEventArgs> CandleEvaluatedEventHandler;

        private readonly ILogger<CandleEvaluator> _logger;
        private readonly ICandleTransformer _candleTransformer;
        private readonly CandleEvaluatorOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, List<InternalCandle>>> _timeframeCandles;

        public CandleEvaluator(ILoggerFactory loggerFactory,
                               ICandleTransformer candleTransformer,
                               CandleEvaluatorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleEvaluator>();
            _candleTransformer = candleTransformer;
            _option = option;

            _isInitialized = false;
            _timeframeCandles = new Dictionary<string, Dictionary<int, List<InternalCandle>>>();
        }


        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                _candleTransformer.CandleTransformedEventHandler += CandleTransformedEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private void CandleTransformedEventHandler(object? sender, CandleTransformedEventArgs e)
        {
            SaveCandle(e.Candle);

            ExecuteCandleEvaluation(e.Candle.Symbol, e.Candle.Timeframe);
        }

        private void SaveCandle(InternalCandle candle)
        {
            lock (_timeframeCandles)
            {
                try
                {
                    _logger.LogDebug($"Saving '{candle.Symbol}_{candle.Timeframe}' candle " +
                                     $"with price O = '{candle.OpenPrice}', H = '{candle.HighPrice}', L = '{candle.LowPrice}', C = '{candle.ClosePrice}'.");

                    if (!_timeframeCandles.TryGetValue(candle.Symbol, out Dictionary<int, List<InternalCandle>> timeframeCandles))
                    {
                        timeframeCandles = new Dictionary<int, List<InternalCandle>>();
                        timeframeCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });

                        _timeframeCandles.Add(candle.Symbol, timeframeCandles);
                    }
                    else if (!timeframeCandles.TryGetValue(candle.Timeframe, out List<InternalCandle> candles))
                    {
                        timeframeCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });
                    }
                    else
                    {
                        candles.Add(candle); //xxx when to remove them, if any?
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save candle.");
                }
            }
        }

        private void SetCandlesPosition(List<InternalCandle> candles)
        {
            if (candles.IsNullOrEmpty())
                return;

            for (int i = 0; i < candles.Count(); i++)
            {
                candles[i].Position = i + 1;
            }
        }

        private void ExecuteCandleEvaluation(string symbol, int timeframe)
        {
            lock (_timeframeCandles)
            {
                try
                {
                    if (!_option.Timeframes.Contains(timeframe))
                    {
                        _logger.LogWarning($"Not allowed to execute '{symbol}_{timeframe}' candle evaluation.");
                        return;
                    }

                    _logger.LogDebug($">>>>> EXECUTING '{symbol}_{timeframe}' CANDLE EVALUATION <<<<<");

                    if (!_timeframeCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> timeframeCandles) || timeframeCandles.IsNullOrEmpty())
                    {
                        _logger.LogWarning($"No '{symbol}' candles.");
                        return;
                    }

                    CandleMetricEvaluation candleEvaluation = new CandleMetricEvaluation();

                    foreach (var timeframeCandlesKvp in timeframeCandles.OrderBy(x => x.Key))
                    {
                        if (timeframe < timeframeCandlesKvp.Key)
                        {
                            _logger.LogWarning($"Found bigger timeframe '{timeframeCandlesKvp.Key}' than given timeframe '{timeframe}'. " +
                                               $"Will not execute '{symbol}_{timeframe}' candle evaluation on timeframe '{timeframeCandlesKvp.Key}'.");
                            continue;
                        }

                        int neededCandles = timeframe / timeframeCandlesKvp.Key;

                        if (neededCandles > timeframeCandlesKvp.Value.Count())
                        {
                            _logger.LogWarning($"Not enough '{symbol}' candles on timeframe '{timeframeCandlesKvp.Key}'. " +
                                               $"Needed candles: '{neededCandles}', Current candles: '{timeframeCandlesKvp.Value.Count()}'.");
                            continue;
                        }
                        else if (neededCandles > 1)
                        {
                            List<InternalCandle> lastCandles = timeframeCandlesKvp.Value.TakeLast(neededCandles).ToList();

                            SetCandlesPosition(lastCandles);

                            candleEvaluation.SubCandles.Add(timeframeCandlesKvp.Key, lastCandles);
                        }
                        else if (neededCandles == 1)
                        {
                            candleEvaluation.MainCandle = timeframeCandlesKvp.Value.Last();
                        }
                    }

                    //xxx for now log
                    if (candleEvaluation.MainCandle != null && !candleEvaluation.SubCandles.IsNullOrEmpty())
                    {
                        _logger.LogInformation(candleEvaluation.Dump());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to execute '{symbol}_{timeframe}' candle evalaution.");
                }
            }
        }

        //public void EnterMarket(string symbol)
        //{
        //    //xxx
        //    //InvokeMarketEvaluationEvent(new MarketEvaluationEventArgs(symbol, MarketDirectionType.Up));
        //}

        //private void InvokeMarketEvaluationEvent(CandleEvaluatedEventArgs args)
        //{
        //    this.CandleEvaluatedEventHandler?.Invoke(this, args);
        //}
    }
}
