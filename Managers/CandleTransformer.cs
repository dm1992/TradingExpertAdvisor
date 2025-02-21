using Microsoft.Extensions.Logging;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Managers
{
    /// <summary>
    /// Transform received raw candle with relevant market data.
    /// </summary>
    public class CandleTransformer : ICandleTransformer
    {
        public event EventHandler<CandleTransformedEventArgs> CandleTransformedEventHandler;

        private readonly ILogger<CandleTransformer> _logger;
        private readonly IExchangeApiClient _exchangeApiClient;

        private Dictionary<string, Dictionary<int, InternalCandle>> _symbolPendingCandles = new Dictionary<string, Dictionary<int, InternalCandle>>();
        private bool _isInitialized = false; 

        public CandleTransformer(ILoggerFactory loggerFactory,
                                 IExchangeApiClient exchangeApiClient)
        {
            _logger = loggerFactory.CreateLogger<CandleTransformer>();
            _exchangeApiClient = exchangeApiClient;
        }


        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                _exchangeApiClient.CandleReceivedEventHandler += CandleReceivedEventHandler;
                _exchangeApiClient.TradeReceivedEventHandler += TradeReceivedEventHandler;
                _exchangeApiClient.OrderbookReceivedEventHandler += OrderbookReceivedEventHandler;
                _exchangeApiClient.UnsolicitedMessageEventHandler += UnsolicitedMessageEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private bool ArePendingCandlesReady(string symbol)
        {
            if (!_symbolPendingCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> symbolPendingCandles))
                return false;

            return symbolPendingCandles.Keys.Count == _exchangeApiClient.GetOption().Timeframes.Count;
        }

        private void CandleReceivedEventHandler(object? sender, CandleReceivedEventArgs e)
        {
            HandleReceivedCandle(e.Candle);
        }

        private void TradeReceivedEventHandler(object? sender, TradeReceivedEventArgs e)
        {
            SaveTradesToPendingCandle(e.Trades);
        }

        private void OrderbookReceivedEventHandler(object? sender, OrderbookReceivedEventArgs e)
        {
            SaveOrderbookToPendingCandle(e.Orderbook);
        }

        private void UnsolicitedMessageEventHandler(object? sender, UnsolicitedMessageEventArgs e)
        {
            switch (e.UnsolicitedMessage.Type)
            {
                case MessageType.Info:
                    _logger.LogInformation(e.UnsolicitedMessage.Message);
                    break;

                case MessageType.Warning:
                    _logger.LogWarning(e.UnsolicitedMessage.Message);
                    break;

                case MessageType.Error:
                    _logger.LogError(e.UnsolicitedMessage.Message);
                    break;

                default:
                    _logger.LogDebug(e.UnsolicitedMessage.Message);
                    break;
            }
        }

        private void HandleReceivedCandle(InternalCandle candle)
        {
            lock (_symbolPendingCandles)
            {
                try
                {
                    if (!_symbolPendingCandles.TryGetValue(candle.Symbol, out Dictionary<int, InternalCandle> symbolPendingCandles))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' pending candle.");

                        symbolPendingCandles = new Dictionary<int, InternalCandle>();
                        symbolPendingCandles.Add(candle.Timeframe, candle);

                        _symbolPendingCandles.Add(candle.Symbol, symbolPendingCandles);
                    }
                    else if (!symbolPendingCandles.TryGetValue(candle.Timeframe, out InternalCandle pendingCandle))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' pending candle.");

                        symbolPendingCandles.Add(candle.Timeframe, candle);
                    }
                    else if (candle.IsClosed)
                    {
                        _logger.LogDebug($"Closing '{pendingCandle.Symbol}_{pendingCandle.Timeframe}' pending candle.");

                        InvokeCandleTransformedEvent(pendingCandle);

                        symbolPendingCandles.Remove(pendingCandle.Timeframe);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle received candle.");
                }
            }
        }

        private void SaveTradesToPendingCandle(List<InternalTrade> trades)
        {
            lock (_symbolPendingCandles)
            {
                try
                {
                    string symbol = trades.First().Symbol;

                    if (!ArePendingCandlesReady(symbol))
                        return;

                    if (!_symbolPendingCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> timeframeCandles))
                        return;

                    foreach (var timeframeCandle in timeframeCandles)
                    {
                        timeframeCandle.Value.Trades.AddRange(trades);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed save trades to pending candle.");
                }
            }
        }

        private void SaveOrderbookToPendingCandle(InternalOrderbook orderbook)
        {
            lock (_symbolPendingCandles)
            {
                try
                {
                    if (!ArePendingCandlesReady(orderbook.Symbol))
                        return;

                    if (!_symbolPendingCandles.TryGetValue(orderbook.Symbol, out Dictionary<int, InternalCandle> timeframeCandles))
                        return;

                    foreach (var timeframeCandle in timeframeCandles)
                    {
                        timeframeCandle.Value.Orderbook = orderbook;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed save orderbook to pending candle.");
                }
            }
        }

        private void InvokeCandleTransformedEvent(InternalCandle candle)
        {
            if (candle == null) return;

            _logger.LogDebug($"Invoking candle transformed event with '{candle.Symbol}_{candle.Timeframe}' candle. ");

            this.CandleTransformedEventHandler?.Invoke(this, new CandleTransformedEventArgs(candle));
        }
    }
}
