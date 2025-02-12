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
    public class CandleCollector : ICandleCollector
    {
        public event EventHandler<CandleCollectedEventArgs> CandleCollectedEventHandler;

        private readonly ILogger<CandleCollector> _logger;
        private readonly ICandleTransformer _candleTransformer;
        private readonly CandleCollectorOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, List<InternalCandle>>> _symbolCandles;

        public CandleCollector(ILoggerFactory loggerFactory,
                               ICandleTransformer candleTransformer,
                               CandleCollectorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleCollector>();
            _candleTransformer = candleTransformer;
            _option = option;

            _isInitialized = false;
            _symbolCandles = new Dictionary<string, Dictionary<int, List<InternalCandle>>>();
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

            CreateCandleCollection(e.Candle.Symbol, e.Candle.Timeframe);
        }

        private void SaveCandle(InternalCandle candle)
        {
            lock (_symbolCandles)
            {
                try
                {
                    _logger.LogDebug($"Saving '{candle.Symbol}_{candle.Timeframe}' candle " +
                                     $"with price O = '{candle.OpenPrice}', H = '{candle.HighPrice}', L = '{candle.LowPrice}', C = '{candle.ClosePrice}'.");

                    if (!_symbolCandles.TryGetValue(candle.Symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
                    {
                        symbolCandles = new Dictionary<int, List<InternalCandle>>();
                        symbolCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });

                        _symbolCandles.Add(candle.Symbol, symbolCandles);
                    }
                    else if (!symbolCandles.TryGetValue(candle.Timeframe, out List<InternalCandle> timeframeCandles))
                    {
                        symbolCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });
                    }
                    else
                    {
                        timeframeCandles.Add(candle); //xxx when to remove them, if any?
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

        private void CreateCandleCollection(string symbol, int timeframe)
        {
            lock (_symbolCandles)
            {
                try
                {
                    if (!_option.Timeframes.Contains(timeframe))
                    {
                        _logger.LogWarning($"Not allowed to create '{symbol}_{timeframe}' candle collection.");
                        return;
                    }

                    _logger.LogDebug($">>>>> CREATING '{symbol}_{timeframe}' CANDLE COLLECTION <<<<<");

                    if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles) || symbolCandles.IsNullOrEmpty())
                    {
                        _logger.LogWarning($"No '{symbol}' candles.");
                        return;
                    }

                    CandleCollection candleCollection = new CandleCollection();

                    foreach (var kvp in symbolCandles.OrderBy(x => x.Key))
                    {
                        if (timeframe < kvp.Key)
                        {
                            _logger.LogWarning($"Found bigger timeframe '{kvp.Key}' than given timeframe '{timeframe}'. " +
                                               $"Will not create '{symbol}_{timeframe}' candle collection on timeframe '{kvp.Key}'.");
                            continue;
                        }

                        int neededCandles = timeframe / kvp.Key;

                        if (neededCandles > kvp.Value.Count())
                        {
                            _logger.LogWarning($"Not enough '{symbol}' candles on timeframe '{kvp.Key}'. " +
                                               $"Needed candles: '{neededCandles}', Current candles: '{kvp.Value.Count()}'.");
                            continue;
                        }
                        else if (neededCandles > 1)
                        {
                            List<InternalCandle> lastCandles = kvp.Value.TakeLast(neededCandles).ToList();

                            SetCandlesPosition(lastCandles);

                            candleCollection.TimeframeSubCandles.Add(kvp.Key, lastCandles);
                        }
                        else if (neededCandles == 1)
                        {
                            candleCollection.MainCandle = kvp.Value.Last();
                        }
                    }

                    InvokeCandleCollectedEvent(candleCollection);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create '{symbol}_{timeframe}' candle collection.");
                }
            }
        }

        private void InvokeCandleCollectedEvent(CandleCollection candleCollection)
        {
            if (!Helpers.IsCandleCollectionValid(candleCollection))
                return;

            _logger.LogDebug($"Invoking candle collected event with '{candleCollection.Symbol}_{candleCollection.Timeframe}' candle. ");

            this.CandleCollectedEventHandler?.Invoke(this, new CandleCollectedEventArgs(candleCollection));
        }
    }
}
