using Microsoft.Extensions.Logging;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Managers
{
    /// <summary>
    /// Transform received candle with all kinds of data. For now only with received trades and orderbook.
    /// </summary>
    public class CandleTransformer : ICandleTransformer
    {
        public event EventHandler<CandleTransformedEventArgs> CandleTransformedEventHandler;

        private readonly ILogger<CandleTransformer> _logger;
        private readonly IExchangeApiClient _exchangeApiClient;

        private Dictionary<string, Dictionary<int, InternalCandle>> _symbolCandles = new Dictionary<string, Dictionary<int, InternalCandle>>();
        private ExchangeApiOption _option = null;
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

                _option = _exchangeApiClient.GetOption();

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

        private bool AreCandlesReady(string symbol)
        {
            if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> timeframeCandles))
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
            lock (_symbolCandles)
            {
                try
                {
                    if (!_symbolCandles.TryGetValue(candle.Symbol, out Dictionary<int, InternalCandle> symbolCandles))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' candle.");

                        symbolCandles = new Dictionary<int, InternalCandle>();
                        symbolCandles.Add(candle.Timeframe, candle);

                        _symbolCandles.Add(candle.Symbol, symbolCandles);
                    }
                    else if (!symbolCandles.TryGetValue(candle.Timeframe, out InternalCandle timeframeCandle))
                    {
                        _logger.LogDebug($"Received new '{candle.Symbol}_{candle.Timeframe}' candle.");

                        symbolCandles.Add(candle.Timeframe, candle);
                    }
                    else if (candle.IsClosed)
                    {
                        _logger.LogDebug($"Closing '{timeframeCandle.Symbol}_{timeframeCandle.Timeframe}' candle.");

                        InvokeCandleTransformedEvent(timeframeCandle);

                        symbolCandles.Remove(timeframeCandle.Timeframe);
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
            lock (_symbolCandles)
            {
                try
                {
                    string symbol = trades.First().Symbol;

                    if (!AreCandlesReady(symbol))
                        return;

                    if (!_symbolCandles.TryGetValue(symbol, out Dictionary<int, InternalCandle> timeframeCandles))
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
            lock (_symbolCandles)
            {
                try
                {
                    if (!AreCandlesReady(orderbook.Symbol))
                        return;

                    if (!_symbolCandles.TryGetValue(orderbook.Symbol, out Dictionary<int, InternalCandle> timeframeCandles))
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

        private void InvokeCandleTransformedEvent(InternalCandle candle)
        {
            if (candle == null) return;

            _logger.LogDebug($"Invoking candle transformed event with '{candle.Symbol}_{candle.Timeframe}' candle. ");

            this.CandleTransformedEventHandler?.Invoke(this, new CandleTransformedEventArgs(candle));
        }
    }
}
