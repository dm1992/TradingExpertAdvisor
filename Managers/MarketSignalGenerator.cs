using CryptoCom.Net.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Managers
{
    public class MarketSignalGenerator : IMarketSignalGenerator
    {
        public event EventHandler<MarketSignalEventArgs> MarketSignalGeneratedEventHandler;

        private readonly ILogger<MarketSignalGenerator> _logger;
        private readonly ICandleTransformer _candleTransformer;
        private readonly IExchangeApiClient _exchangeApiClient;

        private Dictionary<string, Dictionary<int, List<InternalCandle>>> _symbolCandles = new Dictionary<string, Dictionary<int, List<InternalCandle>>>();
        private bool _isInitialized = false;

        public MarketSignalGenerator(ILoggerFactory loggerFactory,
                                     ICandleTransformer candleTransformer,
                                     IExchangeApiClient exchangeApiClient)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalGenerator>();
            _candleTransformer = candleTransformer;
            _exchangeApiClient = exchangeApiClient;
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

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

            InvokeMarketEntry(e.Candle.Symbol);
        }

        private void SaveCandle(InternalCandle candle)
        {
            try
            {
                _logger.LogDebug($"Saving '{candle.Symbol}_{candle.Timeframe}' candle.");


                if (!_symbolCandles.TryGetValue(candle.Symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
                {
                    symbolCandles = new Dictionary<int, List<InternalCandle>>();
                    symbolCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });

                    _symbolCandles.Add(candle.Symbol, symbolCandles);
                }
                else if (!symbolCandles.TryGetValue(candle.Timeframe, out List<InternalCandle> candles))
                {
                    symbolCandles.Add(candle.Timeframe, new List<InternalCandle>() { candle });
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

        private void InvokeMarketEntry(string symbol)
        {
            try
            {
                if (!IsEnoughMarketData(symbol))
                {
                    _logger.LogWarning($"Failed to invoke '{symbol}' market entry. Not enough '{symbol}' market data.");
                    return;
                }

                CreateMarketSignal(symbol);

                FlushMarketData(symbol); // start over again
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invoke market entry.");
            }
        }

        private bool IsEnoughMarketData(string symbol)
        {
            if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
            {
                _logger.LogError($"No '{symbol}' candles.");
                return false;
            }

            int maxCandleTimeframe = _exchangeApiClient.GetOption().Timeframes.Max();

            if (!symbolCandles.TryGetValue(maxCandleTimeframe, out _))
            {
                _logger.LogWarning($"'{symbol}_{maxCandleTimeframe}' candle not present yet.");
                return false;
            }

            return true;
        }

        private void FlushMarketData(string symbol)
        {
            if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
            {
                _logger.LogError($"No '{symbol}' candles.");
                return;
            }

            _logger.LogDebug($"Flushing '{symbol}' market data of total '{symbolCandles.Values.Count}' entries.");

            symbolCandles.Clear();
        }

        private void CreateMarketSignal(string symbol)
        {
            Dictionary<int, InternalCandleDirectionInfo> timeframeCandleDirectionInfos = GetTimeframeCandleDirectionInfos(symbol);

            MarketDirection marketDirection = EvaluateTimeframeCandleDirectionInfos(timeframeCandleDirectionInfos);
            
            if (marketDirection == MarketDirection.Unknown)
            {
                _logger.LogError($"Failed to create '{symbol}' market signal.");
                return;
            }

            decimal? currentSymbolPrice = _exchangeApiClient.GetLastPrice(symbol);

            if (currentSymbolPrice == null)
            {
                _logger.LogError($"Failed to create '{symbol}' market signal. Unknown '{symbol}' current price.");
                return;
            }

            _logger.LogDebug($">>> Creating '{symbol}' market signal with direction '{marketDirection}' @ price '{currentSymbolPrice.Value}'$ with timeframe candle direction infos: \n" +
                             $"[ {string.Join("\n", timeframeCandleDirectionInfos.Select(kvp => $"{kvp.Key}: {kvp.Value.Dump()}"))} ]");

            MarketSignalMetadata marketSignal = new MarketSignalMetadata();
            marketSignal.Symbol = symbol;
            marketSignal.Timestamp = DateTime.Now;
            marketSignal.CurrentPrice = currentSymbolPrice.Value;
            marketSignal.MarketDirection = marketDirection;
            marketSignal.TimeframeCandleDirectionInfos = timeframeCandleDirectionInfos;

            InvokeMarketSignalEvent(marketSignal);

            //FlushMarketData(symbol); // start over again
        }

        private Dictionary<int, InternalCandleDirectionInfo> GetTimeframeCandleDirectionInfos(string symbol, int? timeframe = null, int? useTotalCandles = null)
        {
            if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
            {
                _logger.LogError($"No '{symbol}' candles.");
                return null;
            }

            Dictionary<int, InternalCandleDirectionInfo> timeframeCandleDirectionInfos = new Dictionary<int, InternalCandleDirectionInfo>();

            if (timeframe.HasValue)
            {
                if (!symbolCandles.TryGetValue(timeframe.Value, out List<InternalCandle> candles))
                {
                    _logger.LogError($"No '{symbol}_{timeframe.Value}' candles.");
                    return null;
                }

                int totalCandles = useTotalCandles.HasValue ? useTotalCandles.Value : candles.Count();

                List<InternalCandle> lastCandles = candles.TakeLast(totalCandles).ToList();

                SetCandlesPosition(lastCandles);

                timeframeCandleDirectionInfos.Add(totalCandles, new InternalCandleDirectionInfo(lastCandles));
            }
            else
            {
                foreach (var kvp in symbolCandles)
                {
                    int totalCandles = useTotalCandles.HasValue ? useTotalCandles.Value : kvp.Value.Count();

                    List<InternalCandle> lastCandles = kvp.Value.TakeLast(totalCandles).ToList();

                    SetCandlesPosition(lastCandles);;

                    timeframeCandleDirectionInfos.Add(kvp.Key, new InternalCandleDirectionInfo(lastCandles));
                }
            }

            return timeframeCandleDirectionInfos;
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

        private MarketDirection EvaluateTimeframeCandleDirectionInfos(Dictionary<int, InternalCandleDirectionInfo> timeframeCandleDirectionInfos)
        {
            if (timeframeCandleDirectionInfos.IsNullOrEmpty())
                return MarketDirection.Unknown;

            //xxx hardcoded, change!

            if (timeframeCandleDirectionInfos[15].DirectionType == InternalCandleDirection.Expected_Down)
            {
                if (timeframeCandleDirectionInfos[5].DirectionType == InternalCandleDirection.Expected_Down)
                {
                    if (timeframeCandleDirectionInfos[1].DirectionType == InternalCandleDirection.Expected_Up || timeframeCandleDirectionInfos[1].DirectionType == InternalCandleDirection.Not_Expected_Up)
                    {
                        return MarketDirection.Up;
                    }
                }
            }
            else if (timeframeCandleDirectionInfos[15].DirectionType == InternalCandleDirection.Expected_Up)
            {
                if (timeframeCandleDirectionInfos[5].DirectionType == InternalCandleDirection.Expected_Up)
                {
                    if (timeframeCandleDirectionInfos[1].DirectionType == InternalCandleDirection.Expected_Down || timeframeCandleDirectionInfos[1].DirectionType == InternalCandleDirection.Not_Expected_Down)
                    {
                        return MarketDirection.Down;
                    }
                }
            }

            return MarketDirection.Unknown;
        }

        private void InvokeMarketSignalEvent(MarketSignalMetadata marketSignal)
        {
            if (!Helpers.IsMarketSignalValid(marketSignal))
                return;

            _logger.LogDebug($"Invoking '{marketSignal.Symbol}' market signal event.");

            this.MarketSignalGeneratedEventHandler?.Invoke(this, new MarketSignalEventArgs(marketSignal));
        }
    }
}
