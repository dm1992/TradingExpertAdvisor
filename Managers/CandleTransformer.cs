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
        private Dictionary<string, Dictionary<int, InternalCandle>> _candles;

        public CandleTransformer(ILoggerFactory loggerFactory,
                                IApiClient apiClient,
                                CandleTransformerOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleTransformer>();
            _apiClient = apiClient;
            _option = option;

            _isInitialized = false;
            _candles = new Dictionary<string, Dictionary<int, InternalCandle>>();
        }


        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

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
            if (!_candles.TryGetValue(symbol, out Dictionary<int, InternalCandle> candles))
                return false;

            return candles.Keys.Count == _option.Timeframes.Count;
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
            this.CandleTransformedEventHandler?.Invoke(this, new CandleTransformedEventArgs(candle));
        }

        private void HandleReceivedCandle(InternalCandle candle)
        {
            try
            {
                lock (_candles)
                {
                    if (!_candles.TryGetValue(candle.Symbol, out Dictionary<int, InternalCandle> dict))
                    {
                        _logger.LogDebug($"Saving first '{candle.Symbol}_{candle.Timeframe}' candle.");

                        dict = new Dictionary<int, InternalCandle>();
                        dict.Add(candle.Timeframe, candle);

                        _candles.Add(candle.Symbol, dict);
                    }
                    else if (!dict.TryGetValue(candle.Timeframe, out InternalCandle pendingCandle))
                    {
                        _logger.LogDebug($"Saving first '{candle.Symbol}_{candle.Timeframe}' candle.");

                        dict.Add(candle.Timeframe, candle);
                    }
                    else if (candle.IsClosed)
                    {
                        _logger.LogDebug($"Closing pending '{pendingCandle.Symbol}_{pendingCandle.Timeframe}' candle.");

                        pendingCandle.IsClosed = true;

                        InvokeCandleTransformedEvent(pendingCandle);
                    }
                    else
                    {
                        _logger.LogDebug($"Using '{candle.Symbol}_{candle.Timeframe}' candle as new pending candle.");

                        pendingCandle = candle;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle received candle.");
            }
        }

        private void SaveTradesToCandle(List<InternalTrade> trades)
        {
            try
            {
                lock (_candles)
                {
                    string symbol = trades.First().Symbol;

                    if (!AreCandlesReady(symbol))
                        return;

                    if (!_candles.TryGetValue(symbol, out Dictionary<int, InternalCandle> dict))
                        return;

                    foreach (var dictItem in dict)
                    {
                        if (!dictItem.Value.IsClosed)
                        {
                            dictItem.Value.Trades.AddRange(trades);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed save trades to candle.");
            }
        }

        private void SaveOrderbookToCandle(InternalOrderbook orderbook)
        {
            try
            {
                lock (_candles)
                {
                    if (!AreCandlesReady(orderbook.Symbol))
                        return;

                    if (!_candles.TryGetValue(orderbook.Symbol, out Dictionary<int, InternalCandle> dict))
                        return;

                    foreach (var dictItem in dict)
                    {
                        if (!dictItem.Value.IsClosed)
                        {
                            dictItem.Value.Orderbook = orderbook;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed save orderbook to candle.");
            }
        }
    }
}
