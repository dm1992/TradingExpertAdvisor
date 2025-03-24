using CryptoCom.Net.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Database;
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
        private readonly MarketSignalEvaluationDatabaseManager _dbManager;
        private readonly MarketSignalGeneratorOption _option;

        private Dictionary<string, Dictionary<int, List<InternalCandle>>> _symbolCandles = new Dictionary<string, Dictionary<int, List<InternalCandle>>>();
        private List<MarketSignalMetadata> _marketSignalBuffer = new List<MarketSignalMetadata>();      
        private bool _isInitialized = false;

        public MarketSignalGenerator(ILoggerFactory loggerFactory,
                                     ICandleTransformer candleTransformer,
                                     IExchangeApiClient exchangeApiClient,
                                     MarketSignalEvaluationDatabaseManager dbManager,
                                     MarketSignalGeneratorOption option)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalGenerator>();
            _candleTransformer = candleTransformer;
            _exchangeApiClient = exchangeApiClient;
            _dbManager = dbManager;
            _option = option;
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                if (_option.TakeProfitAmounts.Count != _option.StopLossAmounts.Count)
                {
                    throw new Exception("Take profit and stop amounts lists not long enough.");
                }

                _candleTransformer.CandleTransformedEventHandler += CandleTransformedEventHandler;
                _exchangeApiClient.PriceInfoReceivedEventHandler += PriceInfoReceivedEventHandler;

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

            EvaluateMarket(e.Candle.Symbol);
        }

        private void PriceInfoReceivedEventHandler(object? sender, PriceInfoReceivedEventArgs e)
        {
            //EvaluateMarketSignal(e.PriceInfo);
        }

        private void EvaluateMarketSignal(PriceInfo priceInfo)
        {
            if (priceInfo == null) return;

            lock (_marketSignalBuffer)
            {
                List<MarketSignalMetadata> marketSignals =_marketSignalBuffer.Where(x => x.Symbol == priceInfo.Symbol).ToList();

                if (marketSignals.IsNullOrEmpty()) 
                    return;

                //_logger.LogInformation($"Evaluating '{marketSignals.Count}' '{priceInfo.Symbol}' market signals...");

                foreach (MarketSignalMetadata marketSignal in marketSignals)
                {
                    if (priceInfo.Price >= marketSignal.ExitPriceTakeProfitUp)
                    {
                        marketSignal.MarketDirection = MarketDirection.Up;
                        marketSignal.IsObsolete = true;
                    }
                    else if (priceInfo.Price <= marketSignal.ExitPriceTakeProfitDown)
                    {
                        marketSignal.MarketDirection = MarketDirection.Down;
                        marketSignal.IsObsolete = true;
                    }
                    else if (priceInfo.Price >= marketSignal.ExitPriceStopLossUp)
                    {
                        marketSignal.MarketDirection = MarketDirection.Up;
                        marketSignal.IsObsolete = true;
                    }
                    else if (priceInfo.Price <= marketSignal.ExitPriceStopLossDown)
                    {
                        marketSignal.MarketDirection = MarketDirection.Down;
                        marketSignal.IsObsolete = true;
                    }

                    if (marketSignal.IsObsolete)
                    {
                        _logger.LogInformation($"Evaluated '{marketSignal.Symbol}' market signal. " +
                                               $"Entry price: '{marketSignal.EntryPrice}$', exit price: '{priceInfo.Price}$', market direction: '{marketSignal.MarketDirection}'.");

                        if (_dbManager.SaveMarketSignalEvaluation(marketSignal.Tag))
                        {
                            _dbManager.UpdateMarketSignalDirectionCounter(marketSignal.Tag, marketSignal.MarketDirection);
                        }
                    }
                }

                int removed = _marketSignalBuffer.RemoveAll(x => x.IsObsolete);

                if (removed > 0)
                {
                    _logger.LogInformation($"Removed '{removed}' obsolete evaluated market signals.");
                }
            }
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

        private void EvaluateMarket(string symbol)
        {
            try
            {
                if (!IsEnoughMarketData(symbol))
                {
                    _logger.LogWarning($"Failed to evaluate '{symbol}' market. Not enough '{symbol}' market data.");
                    return;
                }

                HandleMarketSignal(symbol);
                
                FlushMarketData(symbol); // start over again
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate market.");
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
            
            foreach (var kvp in symbolCandles)
            {
                int expectedCandles = maxCandleTimeframe / kvp.Key;

                if (expectedCandles != kvp.Value.Count)
                {
                    _logger.LogWarning($"Not enough '{symbol}' candles. Expected '{expectedCandles}' candles, actual '{kvp.Value.Count}' candles.");
                    return false;
                }
            }

            return true;
        }

        private void HandleMarketSignal(string symbol)
        {
            lock (_marketSignalBuffer)
            {


                if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
                {
                    _logger.LogError($"Failed to create '{symbol}' market signal. No '{symbol}' candles.");
                    return;
                }

                decimal? currentSymbolPrice = _exchangeApiClient.GetLastPrice(symbol);

                if (currentSymbolPrice == null)
                {
                    _logger.LogError($"Failed to create '{symbol}' market signal. Unknown '{symbol}' current price.");
                    return;
                }

                string marketSignalTag = $"{symbol}";

                foreach (var kvp in symbolCandles)
                {
                    marketSignalTag += $"_{String.Join("_", kvp.Value.Select(x => x.Tag))}";
                }

                MarketSignalMetadata marketSignal = new MarketSignalMetadata();
                marketSignal.Tag = marketSignalTag;
                marketSignal.Symbol = symbol;
                marketSignal.Timestamp = DateTime.UtcNow;
                marketSignal.EntryPrice = currentSymbolPrice.Value;
                marketSignal.IsObsolete = false;
                marketSignal.MarketDirection = MarketDirection.Unknown;

                _logger.LogInformation($"Created '{marketSignal.Symbol}' market signal at entry price '{marketSignal.EntryPrice}$'.");

                _marketSignalBuffer.Add(marketSignal);

                //TryMarketSignal_V2(marketSignal);
            }
        }

        private void TryMarketSignal_V2(MarketSignalMetadata marketSignal)
        {
            if (marketSignal == null)
                return;

            var marketSignalEvaluations = _dbManager.GetSimilarMarketSignalEvaluations(marketSignal.Tag);

            if (marketSignalEvaluations.IsNullOrEmpty() || marketSignalEvaluations.Sum(x => x.Total) < 5)
                return;

            var totalUps = marketSignalEvaluations.Sum(x => x.Ups);
            var totalDowns = marketSignalEvaluations.Sum(x => x.Downs);
            var total = totalUps + totalDowns;

            var percentageUps = (totalUps / (decimal)(total)) * 100.0m;
            var percentageDowns = (totalDowns / (decimal)(total)) * 100.0m;

            if (percentageUps >= 90.0m)
            {
                marketSignal.MarketDirection = MarketDirection.Up;
            }
            else if (percentageDowns >= 90.0m)
            {
                marketSignal.MarketDirection = MarketDirection.Down;
            }


            if (marketSignal.MarketDirection != MarketDirection.Unknown)
            {
                _logger.LogDebug($">>>>> Trying '{marketSignal.MarketDirection}' market signal tag '{marketSignal.Tag}' " +
                                 $"UPS percentage '{percentageUps}' and DOWNS percentage '{percentageDowns}', total signals '{total}'.");

                MarketSignalGeneratedEventHandler?.Invoke(this, new MarketSignalEventArgs(marketSignal));
            }
            else
            {
                _logger.LogDebug($"Will skip UNKNOWN market signal with tag '{marketSignal.Tag}'...");
            }

        }

        //private void TryMarketSignal(MarketSignalMetadata marketSignal)
        //{
        //    if (marketSignal == null) 
        //        return;

        //    MarketSignalEvaluation marketSignalEvaluation = _dbManager.GetMarketSignalEvaluation(marketSignal.Tag);

        //    if (marketSignalEvaluation == null || marketSignalEvaluation.Total < 3)
        //    {
        //        _logger.LogWarning($"Invalid market evaluation. Will not try '{marketSignal.MarketDirection}' market signal tag '{marketSignal.Tag}'.");
        //        return;
        //    }

        //    if (marketSignalEvaluation.UpsPercentage == 100.0m) // marketSignalEvaluation.DownsPercentage
        //    {
        //        marketSignal.MarketDirection = MarketDirection.Up;
        //    }
        //    else if (marketSignalEvaluation.DownsPercentage == 100.0m) // marketSignalEvaluation.UpsPercentage
        //    {
        //        marketSignal.MarketDirection = MarketDirection.Down;
        //    }

        //    if (marketSignal.MarketDirection != MarketDirection.Unknown)
        //    {
        //        _logger.LogDebug($">>>>> Trying '{marketSignal.MarketDirection}' market signal tag '{marketSignal.Tag}' " +
        //                         $"UPS percentage '{marketSignalEvaluation.UpsPercentage}' and DOWNS percentage '{marketSignalEvaluation.DownsPercentage}', total signals '{marketSignalEvaluation.Total}'.");

        //        MarketSignalGeneratedEventHandler?.Invoke(this, new MarketSignalEventArgs(marketSignal));
        //    }
        //    else
        //    {
        //        _logger.LogDebug($"Will skip UNKNOWN market signal with tag '{marketSignal.Tag}'...");
        //    }
        //}

        private void FlushMarketData(string symbol)
        {
            if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> symbolCandles))
            {
                _logger.LogError($"No '{symbol}' candles.");
                return;
            }

            _logger.LogDebug($"Flushing '{symbol}' market data of total '{symbolCandles.Values.Sum(x => x.Count)}' entries.");

            symbolCandles.Clear();
        }
    }
}
