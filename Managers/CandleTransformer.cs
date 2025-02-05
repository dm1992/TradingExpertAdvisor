using Microsoft.Extensions.Logging;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Managers.Options;
using System.Reflection.Metadata;

namespace TradingExpertAdvisor.Managers
{
    /// <summary>
    /// Transform received candle with all kinds of data. For now only with received trades and orderbook.
    /// </summary>
    public class CandleTransformer : ICandleTransformer
    {
        public event EventHandler<CandleTransformedEventArgs> CandleTransformedEventHandler;

        private readonly ILogger<CandleTransformer> _logger;
        private readonly IApiClient _apiClient;
        private readonly CandleTransformerOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, InternalCandle>> _timeframeCandles;

        public CandleTransformer(ILoggerFactory loggerFactory,
                                IApiClient apiClient,
                                CandleTransformerOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleTransformer>();
            _apiClient = apiClient;
            _option = option;

            _isInitialized = false;
            _timeframeCandles = new Dictionary<string, Dictionary<int, InternalCandle>>();
        }


        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                if (!_apiClient.StartCandleReceiverAsync(_option.Symbols, _option.Timeframes).Result)
                    return false;

                if (!_apiClient.StartTradeReceiverAsync(_option.Symbols).Result)
                    return false;

                if (!_apiClient.StartOrderbookReceiverAsync(_option.Symbols).Result)
                    return false;

                _apiClient.CandleReceivedEventHandler += CandleReceivedEventHandler;
                _apiClient.TradeReceivedEventHandler += TradeReceivedEventHandler;
                _apiClient.OrderbookReceivedEventHandler += OrderbookReceivedEventHandler;
                _apiClient.UnsolicitedMessageEventHandler += UnsolicitedMessageEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private bool AreCandlesReady(string symbol)
        {
            if (!_timeframeCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> timeframeCandles))
                return false;

            return timeframeCandles.Keys.Count == _option.Timeframes.Count;
        }

        private void CandleReceivedEventHandler(object? sender, CandleReceivedEventArgs e)
        {
            HandleReceivedCandle(e.Candle);
        }

        private void TradeReceivedEventHandler(object? sender, TradeReceivedEventArgs e)
        {
            SaveTradesToCandle(e.Trades);
        }

        private void OrderbookReceivedEventHandler(object? sender, OrderbookReceivedEventArgs e)
        {
            SaveOrderbookToCandle(e.Orderbook);
        }

        private void UnsolicitedMessageEventHandler(object? sender, UnsolicitedMessageEventArgs e)
        {
            switch (e.Type)
            {
                case MessageType.Info:
                    _logger.LogInformation(e.Message);
                    break;

                case MessageType.Warning:
                    _logger.LogWarning(e.Message);
                    break;

                case MessageType.Error:
                    _logger.LogError(e.Message);
                    break;

                default:
                    _logger.LogDebug(e.Message);
                    break;
            }
        }

        private void InvokeCandleTransformedEvent(InternalCandle candle)
        {
            if (candle == null) return;

            _logger.LogDebug($"Invoking candle transformed event with '{candle.Symbol}_{candle.Timeframe}' candle. ");

            this.CandleTransformedEventHandler?.Invoke(this, new CandleTransformedEventArgs(candle));
        }

        private void HandleReceivedCandle(InternalCandle candle)
        {
            lock (_timeframeCandles)
            {
                try
                {
                    if (!_timeframeCandles.TryGetValue(candle.Symbol, out Dictionary<int, InternalCandle> timeframeCandle))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' candle.");

                        timeframeCandle = new Dictionary<int, InternalCandle>();
                        timeframeCandle.Add(candle.Timeframe, candle);

                        _timeframeCandles.Add(candle.Symbol, timeframeCandle);
                    }
                    else if (!timeframeCandle.TryGetValue(candle.Timeframe, out InternalCandle transformedCandle))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' candle.");

                        timeframeCandle.Add(candle.Timeframe, candle);
                    }
                    else if (candle.IsClosed)
                    {
                        _logger.LogDebug($"Closing '{transformedCandle.Symbol}_{transformedCandle.Timeframe}' candle.");

                        InvokeCandleTransformedEvent(transformedCandle);

                        timeframeCandle.Remove(transformedCandle.Timeframe);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle received candle.");
                }
            }
        }

        private void SaveTradesToCandle(List<InternalTrade> trades)
        {
            lock (_timeframeCandles)
            {
                try
                {
                    string symbol = trades.First().Symbol;

                    if (!AreCandlesReady(symbol))
                        return;

                    if (!_timeframeCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> timeframeCandles))
                        return;

                    foreach (var timeframeCandle in timeframeCandles)
                    {
                        timeframeCandle.Value.Trades.AddRange(trades);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed save trades to candle.");
                }
            }
        }

        private void SaveOrderbookToCandle(InternalOrderbook orderbook)
        {
            lock (_timeframeCandles)
            {
                try
                {
                    if (!AreCandlesReady(orderbook.Symbol))
                        return;

                    if (!_timeframeCandles.TryGetValue(orderbook.Symbol, out Dictionary<int, InternalCandle> timeframeCandles))
                        return;

                    foreach (var timeframeCandle in timeframeCandles)
                    {
                        timeframeCandle.Value.Orderbook = orderbook;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed save orderbook to candle.");
                }
            }
        }
    }
}
