using CryptoCom.Net.Enums;
using CryptoExchange.Net.CommonObjects;
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
    public class MarketSignalValidator : IMarketSignalValidator
    {
        public event EventHandler<MarketSignalEventArgs> MarketSignalValidatedEventHandler;

        private readonly ILogger<MarketSignalValidator> _logger;
        private readonly IMarketSignalGenerator _marketSignalGenerator;
        private readonly IExchangeApiClient _exchangeApiClient;
        private readonly MarketSignalValidatorOption _option;

        private Dictionary<string, Dictionary<int, List<MarketSignalMetadata>>> _symbolMarketSignals = new Dictionary<string, Dictionary<int, List<MarketSignalMetadata>>>();
        private bool _isInitialized = false;

        public MarketSignalValidator(ILoggerFactory loggerFactory,
                                     IMarketSignalGenerator marketSignalGenerator,
                                     IExchangeApiClient exchangeApiClient,
                                     MarketSignalValidatorOption option)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalValidator>();
            _marketSignalGenerator = marketSignalGenerator;
            _exchangeApiClient = exchangeApiClient;
            _option = option;
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                _marketSignalGenerator.MarketSignalGeneratedEventHandler += MarketSignalGeneratedEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private void MarketSignalGeneratedEventHandler(object? sender, MarketSignalEventArgs e)
        {
            ValidateMarketSignal(e.MarketSignalMetadata);

            SaveMarketSignal(e.MarketSignalMetadata);
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

        /// <summary>
        /// Draft method for validating market signal. To be implemented furthermore.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeframe"></param>
        private void ValidateMarketSignal(MarketSignalMetadata marketSignal)
        {
            try
            {
                if (marketSignal == null) return;

                if (!IsMarketSignalThresholdReached(marketSignal.Symbol, marketSignal.Timeframe))
                {
                    _logger.LogWarning($"Failed to validate market signal. '{marketSignal.Symbol}_{marketSignal.Timeframe}' market signal threashold not observed or not reached yet.");
                    return;
                }

                List<MarketSignalMetadata> marketSignals = GetMarketSignals(marketSignal.Symbol, marketSignal.Timeframe);

                if (marketSignals.IsNullOrEmpty())
                {
                    _logger.LogError($"Failed to create market signal. '{marketSignal.Symbol}_{marketSignal.Timeframe}' market signals are empty, very strange because its threshold is reached!");
                    return;
                }

                if (marketSignal.MarketDirection == MarketDirection.Up)
                {
                    decimal marketDirectionPercentageAboveCurrent = (marketSignals.Where(x => x.MarketDirection == MarketDirection.Up && x.MarketDirectionPercentage < marketSignal.MarketDirectionPercentage).Count() /
                                                                     marketSignals.Count()) * 100.0m;

                    decimal pricePercentageAboveCurrent = (marketSignals.Where(x => x.MarketDirection == MarketDirection.Up && x.CurrentPrice < marketSignal.CurrentPrice).Count() /
                                                           marketSignals.Count()) * 100.0m;

                    if (marketDirectionPercentageAboveCurrent > 50.0m && pricePercentageAboveCurrent > 50.0m)
                    {
                        InvokeMarketSignalValidatedEvent(marketSignal);
                    }
                }
                else if (marketSignal.MarketDirection == MarketDirection.Down)
                {
                    decimal marketDirectionPercentageBelowCurrent = (marketSignals.Where(x => x.MarketDirection == MarketDirection.Down && x.MarketDirectionPercentage < marketSignal.MarketDirectionPercentage).Count() /
                                                                     marketSignals.Count()) * 100.0m;

                    decimal pricePercentageBelowCurrent = (marketSignals.Where(x => x.MarketDirection == MarketDirection.Down && x.CurrentPrice > marketSignal.CurrentPrice).Count() /
                                                           marketSignals.Count()) * 100.0m;

                    if (marketDirectionPercentageBelowCurrent > 50.0m && pricePercentageBelowCurrent > 50.0m)
                    {
                        InvokeMarketSignalValidatedEvent(marketSignal);
                    }
                } 

                FlushMarketSignals(marketSignal.Symbol, marketSignal.Timeframe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate market signal.");
            }
        }

        private List<MarketSignalMetadata> GetMarketSignals(string symbol, int timeframe)
        {
            if (_symbolMarketSignals.TryGetValue(symbol, out Dictionary<int, List<MarketSignalMetadata>> symbolMarketSignals))
            {
                if (symbolMarketSignals.TryGetValue(timeframe, out List<MarketSignalMetadata> timeframeMarketSignals))
                {
                    return timeframeMarketSignals;
                }
            }

            return null;
        }


        private bool IsMarketSignalThresholdReached(string symbol, int timeframe)
        {
            if (_symbolMarketSignals.TryGetValue(symbol, out Dictionary<int, List<MarketSignalMetadata>> symbolMarketSignals))
            {
                if (symbolMarketSignals.TryGetValue(timeframe, out List<MarketSignalMetadata> timeframeMarketSignals))
                {
                    if (_option.MarketSignalTimeframeThresholds.TryGetValue(timeframe, out int threshold))
                    {
                        return timeframeMarketSignals.Count >= threshold;
                    }
                }
            }

            return false;
        }

        private void FlushMarketSignals(string symbol, int timeframe)
        {
            if (_symbolMarketSignals.TryGetValue(symbol, out Dictionary<int, List<MarketSignalMetadata>> symbolMarketSignals))
            {
                if (symbolMarketSignals.TryGetValue(timeframe, out List<MarketSignalMetadata> timeframeMarketSignals))
                {
                    _logger.LogInformation($"Flushing {symbol}_{timeframe}' market signals. Total '{timeframeMarketSignals.Count}' market signals.");

                    timeframeMarketSignals.Clear();
                }
            }
        }

        private void InvokeMarketSignalValidatedEvent(MarketSignalMetadata marketSignalMetadata)
        {
            if (marketSignalMetadata == null) return;

            _logger.LogDebug($"Invoking '{marketSignalMetadata.Symbol}_{marketSignalMetadata.Timeframe}' market signal validated event.");

            this.MarketSignalValidatedEventHandler?.Invoke(this, new MarketSignalEventArgs(marketSignalMetadata));
        }
    }
}
