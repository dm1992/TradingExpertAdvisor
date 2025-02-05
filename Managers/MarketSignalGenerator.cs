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
        private List<CandleCollection> _candleCollections;

        public MarketSignalGenerator(ILoggerFactory loggerFactory,
                                     ICandleCollector candleCollector,
                                     MarketSignalGeneratorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleCollector>();
            _candleCollector = candleCollector;
            _option = option;

            _isInitialized = false;
            _candleCollections = new List<CandleCollection>();
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

            EvaluateCandleCollections();
        }

        private void SaveCandleCollection(CandleCollection candleCollection)
        {
            try
            {
                _logger.LogDebug($"Saving '{candleCollection.MainCandle.Symbol}_{candleCollection.MainCandle.Timeframe}' candle collection.");

                _candleCollections.Add(candleCollection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save candle collection.");
            }
        }

        private void EvaluateCandleCollections()
        {
            //xxx evaluate last N main candles and its subcandles,...
            // compare this to bigger picture

            // TBD: IMPLEMENT STRATEGY LOGIC HERE !!!!
        }

        private void InvokeMarketSignalEvent(MarketSignalMetadata marketSignalMetadata)
        {
            if (marketSignalMetadata == null) return;
        }
    }
}
