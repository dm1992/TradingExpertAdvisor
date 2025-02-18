using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICandleCollector _candleCollector;
        private readonly IExchangeApiClient _exchangeApiClient;
        private readonly MarketSignalGeneratorOption _option;

        private Dictionary<string, Dictionary<int, List<CandleCollection>>> _symbolCandleCollections = new Dictionary<string, Dictionary<int, List<CandleCollection>>>();
        private bool _isInitialized = false;

        public MarketSignalGenerator(ILoggerFactory loggerFactory,
                                     ICandleCollector candleCollector,
                                     IExchangeApiClient exchangeApiClient,
                                     MarketSignalGeneratorOption option)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalGenerator>();
            _candleCollector = candleCollector;
            _exchangeApiClient = exchangeApiClient;
            _option = option;
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                _candleCollector.CandleCollectedEventHandler += CandleCollectedEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private void CandleCollectedEventHandler(object? sender, CandleCollectedEventArgs e)
        {
            SaveCandleCollection(e.CandleCollection);

            CreateMarketSignal(e.CandleCollection.Symbol, e.CandleCollection.Timeframe);
        }

        private void SaveCandleCollection(CandleCollection candleCollection)
        {
            try
            {
                _logger.LogDebug($"Saving '{candleCollection.Symbol}_{candleCollection.Timeframe}' candle collection.");


                if (!_symbolCandleCollections.TryGetValue(candleCollection.Symbol, out Dictionary<int, List<CandleCollection>> symbolCandleCollections))
                {
                    symbolCandleCollections = new Dictionary<int, List<CandleCollection>>();
                    symbolCandleCollections.Add(candleCollection.Timeframe, new List<CandleCollection>() { candleCollection });

                    _symbolCandleCollections.Add(candleCollection.Symbol, symbolCandleCollections);
                }
                else if (!symbolCandleCollections.TryGetValue(candleCollection.Timeframe, out List<CandleCollection> timeframeCandleCollections))
                {
                    symbolCandleCollections.Add(candleCollection.Timeframe, new List<CandleCollection>() { candleCollection });
                }
                else
                {
                    timeframeCandleCollections.Add(candleCollection); //xxx when to remove them, if any?
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save candle collection.");
            }
        }

        /// <summary>
        /// Draft method for creating market signal. To be implemented furthermore.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeframe"></param>
        private void CreateMarketSignal(string symbol, int timeframe)
        {
            try
            {
                if (!IsCandleCollectionTimeframeThresholdReached(symbol, timeframe))
                {
                    _logger.LogWarning($"Failed to create market signal. '{symbol}_{timeframe}' candle collection threashold not observed or not reached yet.");
                    return;
                }

                List<CandleCollection> candleCollections = GetCandleCollections(symbol, timeframe);

                if (candleCollections.IsNullOrEmpty())
                {
                    _logger.LogError($"Failed to create market signal. '{symbol}_{timeframe}' candle collections are empty, very strange because its threshold is reached!");
                    return;
                }

                MarketDirection marketDirection = GetCandleCollectionsMarketDirection(candleCollections, out decimal marketDirectionPercentage);

                if (marketDirection == MarketDirection.Unknown)
                {
                    _logger.LogInformation($"Unknown market direction on '{symbol}_{timeframe}' candle collection. Do nothing...");
                    return;
                }

                decimal? currentSymbolPrice = _exchangeApiClient.GetLastPrice(symbol);

                if (currentSymbolPrice == null)
                {
                    _logger.LogInformation($"Unknown '{symbol}' current price, despite '{symbol}_{timeframe}' market signal with direction '{marketDirection}'. Do nothing...");
                    return;
                }

                _logger.LogDebug($"Creating '{symbol}_{timeframe}' market signal with direction '{marketDirection}' and percentage '{marketDirectionPercentage}'% @ price '{currentSymbolPrice.Value}'$.");

                MarketSignalMetadata marketSignal = new MarketSignalMetadata();
                marketSignal.Symbol = symbol;
                marketSignal.Timeframe = timeframe;
                marketSignal.Timestamp = DateTime.Now;
                marketSignal.CurrentPrice = currentSymbolPrice.Value;
                marketSignal.MarketDirection = marketDirection;
                marketSignal.MarketDirectionPercentage = marketDirectionPercentage;
                marketSignal.CandleCollections = new List<CandleCollection>(candleCollections);

                InvokeMarketSignalGeneratedEvent(marketSignal);

                FlushCandleCollection(symbol, timeframe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create market signal.");
            }
        }

        private bool IsCandleCollectionTimeframeThresholdReached(string symbol, int timeframe)
        {
            if (_symbolCandleCollections.TryGetValue(symbol, out Dictionary<int, List<CandleCollection>> symbolCandleCollections))
            {
                if (symbolCandleCollections.TryGetValue(timeframe, out List<CandleCollection> timeframeCandleCollections)) 
                {
                    if (_option.CandleCollectionTimeframeThresholds.TryGetValue(timeframe, out int threshold))
                    {
                        return timeframeCandleCollections.Count >= threshold;
                    }
                }
            }

            return false;
        }

        private List<CandleCollection> GetCandleCollections(string symbol, int timeframe)
        {
            if (_symbolCandleCollections.TryGetValue(symbol, out Dictionary<int, List<CandleCollection>> symbolCandleCollections))
            {
                if (symbolCandleCollections.TryGetValue(timeframe, out List<CandleCollection> timeframeCandleCollections))
                {
                    return timeframeCandleCollections;
                }
            }

            return null;
        }

        private MarketDirection GetCandleCollectionsMarketDirection(List<CandleCollection> candleCollections, out decimal marketDirectionPercentage)
        {
            marketDirectionPercentage = 0;

            if (candleCollections.IsNullOrEmpty())
                return MarketDirection.Unknown;

            //xxx need to check this code!

            var ups = candleCollections.Where(x => x.DirectionType == InternalCandleDirection.Expected_Up || x.DirectionType == InternalCandleDirection.Not_Expected_Up);
            var downs = candleCollections.Where(x => x.DirectionType == InternalCandleDirection.Expected_Down || x.DirectionType == InternalCandleDirection.Not_Expected_Down);
            var unknows = candleCollections.Where(x => x.DirectionType == InternalCandleDirection.Unknown);

            if (unknows.Count() > ups.Count() + downs.Count())
                return MarketDirection.Unknown;

            decimal averageUpsPercentage = 0;
            decimal averageDownsPercentage = 0;

            if (!ups.IsNullOrEmpty())
                averageUpsPercentage = ups.Average(x => x.DirectionTypeGeneralPercentage);

            if (!downs.IsNullOrEmpty())
                averageDownsPercentage = downs.Average(x => x.DirectionTypeGeneralPercentage);

            if (averageUpsPercentage > averageDownsPercentage)
            {
                marketDirectionPercentage = averageUpsPercentage;
                return MarketDirection.Up;
            }
            else if (averageDownsPercentage > averageUpsPercentage)
            {
                marketDirectionPercentage = averageDownsPercentage;
                return MarketDirection.Down;
            }

            return MarketDirection.Unknown;
        }

        private void FlushCandleCollection(string symbol, int timeframe)
        {
            if (_symbolCandleCollections.TryGetValue(symbol, out Dictionary<int, List<CandleCollection>> symbolCandleCollections))
            {
                if (symbolCandleCollections.TryGetValue(timeframe, out List<CandleCollection> timeframeCandleCollections))
                {
                    _logger.LogInformation($"Flushing {symbol}_{timeframe}' candle collection. Total '{timeframeCandleCollections.Count}' candle collections.");

                    timeframeCandleCollections.Clear();
                }
            }
        }

        private void InvokeMarketSignalGeneratedEvent(MarketSignalMetadata marketSignal)
        {
            if (!Helpers.IsMarketSignalValid(marketSignal))
                return;

            _logger.LogDebug($"Invoking '{marketSignal.Symbol}_{marketSignal.Timeframe}' market signal generated event.");

            this.MarketSignalGeneratedEventHandler?.Invoke(this, new MarketSignalEventArgs(marketSignal));
        }
    }
}
