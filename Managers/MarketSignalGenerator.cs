using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers.Options;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;

namespace TradingExpertAdvisor.Managers
{
    public class MarketSignalGenerator : IMarketSignalGenerator
    {
        public event EventHandler<MarketSignalEventArgs> MarketSignalEventHandler;

        private readonly ILogger<CandleCollector> _logger;
        private readonly ICandleCollector _candleCollector;
        private readonly MarketSignalGeneratorOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, List<CandleCollection>>> _symbolCandleCollections;

        public MarketSignalGenerator(ILoggerFactory loggerFactory,
                                     ICandleCollector candleCollector,
                                     MarketSignalGeneratorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleCollector>();
            _candleCollector = candleCollector;
            _option = option;

            _isInitialized = false;
            _symbolCandleCollections = new Dictionary<string, Dictionary<int, List<CandleCollection>>>();
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

            EvaluateCandleCollections(e.CandleCollection.Symbol, e.CandleCollection.Timeframe);
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

        private void EvaluateCandleCollections(string symbol, int timeframe)
        {
            try
            {
                //xxx evaluate last N main candles and its subcandles,...
                // compare this to bigger picture

                // TBD: IMPLEMENT STRATEGY LOGIC HERE !!!!

                if (!IsTimeframeCandleCollectionReached(symbol, timeframe))
                    return;


                FlushTimeframeCandleCollection(symbol, timeframe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate candle collection.");
            }
        }

        private bool IsTimeframeCandleCollectionReached(string symbol, int timeframe)
        {
            if (_symbolCandleCollections.TryGetValue(symbol, out Dictionary<int, List<CandleCollection>> symbolCandleCollections))
            {
                if (symbolCandleCollections.TryGetValue(timeframe, out List<CandleCollection> timeframeCandleCollections)) 
                {
                    if (_option.TimeframeThresholds.TryGetValue(timeframe, out int threshold))
                    {
                        return timeframeCandleCollections.Count >= threshold;
                    }
                }
            }

            return false;
        }

        private void FlushTimeframeCandleCollection(string symbol, int timeframe)
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

        private void InvokeMarketSignalEvent(MarketSignalMetadata marketSignalMetadata)
        {
            if (marketSignalMetadata == null) return;

            // validate invoked market signal against similar previous patterns (introduce DB)
        }
    }
}
