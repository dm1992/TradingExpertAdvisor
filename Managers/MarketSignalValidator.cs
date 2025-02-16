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
    public class MarketSignalValidator : IMarketSignalValidator
    {
        public event EventHandler<MarketSignalEventArgs> MarketSignalEventHandler;

        private readonly ILogger<MarketSignalValidator> _logger;
        private readonly IMarketSignalGenerator _marketSignalGenerator;
        private readonly IApiClient _apiClient;
        private readonly MarketSignalGeneratorOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, List<MarketSignalMetadata>>> _symbolMarketSignals; // market signal validation buffer

        public MarketSignalValidator(ILoggerFactory loggerFactory,
                                     IMarketSignalGenerator marketSignalGenerator,
                                     IApiClient apiClient,
                                     MarketSignalGeneratorOption option)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalValidator>();
            _marketSignalGenerator = marketSignalGenerator;
            _apiClient = apiClient;
            _option = option;

            _isInitialized = false;
            _symbolMarketSignals = new Dictionary<string, Dictionary<int, List<MarketSignalMetadata>>>();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                _apiClient.PriceReceivedEventHandler += PriceReceivedEventHandler;
                _marketSignalGenerator.MarketSignalEventHandler += MarketSignalGeneratedEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private void SaveMarketSignal(MarketSignalMetadata marketSignal)
        {
            try
            {
                _logger.LogDebug($"Saving '{marketSignal.Symbol}_{marketSignal.Timeframe}' market signal.");


                if (!_symbolMarketSignals.TryGetValue(marketSignal.Symbol, out Dictionary<int, List<MarketSignalMetadata>> symbolMarketSignals))
                {
                    symbolMarketSignals = new Dictionary<int, List<MarketSignalMetadata>>();
                    symbolMarketSignals.Add(marketSignal.Timeframe, new List<MarketSignalMetadata>() { marketSignal });

                    _symbolMarketSignals.Add(marketSignal.Symbol, symbolMarketSignals);
                }
                else if (!symbolMarketSignals.TryGetValue(marketSignal.Timeframe, out List<MarketSignalMetadata> timeframeMarketSignals))
                {
                    symbolMarketSignals.Add(marketSignal.Timeframe, new List<MarketSignalMetadata>() { marketSignal });
                }
                else
                {
                    timeframeMarketSignals.Add(marketSignal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save market signal.");
            }
        }

        private void PriceReceivedEventHandler(object? sender, PriceReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MarketSignalGeneratedEventHandler(object? sender, MarketSignalEventArgs e)
        {
            SaveMarketSignal(e.MarketSignalMetadata);

            // track generated market signal price changes...
        }
    }
}
