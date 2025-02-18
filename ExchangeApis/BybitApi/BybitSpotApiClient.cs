using Bybit.Net.Objects.Models.V5;
using Bybit.Net;
using Bybit.Net.Enums;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using CryptoExchange.Net.CommonObjects;
using Newtonsoft.Json;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Apis.BybitApi
{
    public class BybitSpotApiClient : BybitBaseApiClient, IExchangeApiClient
    {
        private readonly ILogger<BybitSpotApiClient> _logger;

        public event EventHandler<TradeReceivedEventArgs> TradeReceivedEventHandler;
        public event EventHandler<OrderbookReceivedEventArgs> OrderbookReceivedEventHandler;
        public event EventHandler<CandleReceivedEventArgs> CandleReceivedEventHandler;
        public event EventHandler<PriceInfoReceivedEventArgs> PriceInfoReceivedEventHandler;
        public event EventHandler<UnsolicitedMessageEventArgs> UnsolicitedMessageEventHandler;

        private Dictionary<string, InternalOrderbook> _orderbooks = new Dictionary<string, InternalOrderbook>();
        private Dictionary<string, decimal> _prices = new Dictionary<string, decimal>();
        private bool _isInitialized = false;

        public BybitSpotApiClient(ILoggerFactory loggerFactory, ExchangeApiOption option) : base(option)
        {
            _logger = loggerFactory.CreateLogger<BybitSpotApiClient>();
        }

        public ExchangeApiOption GetOption()
        {
            return _option;
        }

        public decimal? GetLastPrice(string symbol)
        {
            if (!_prices.TryGetValue(symbol, out decimal price))
                return null;

            return price;
        }

        public async Task<bool> Initialize()
        {
            try
            {

                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing with options '{_option.Dump()}'...");

                if (!await StartTradeReceiverAsync())
                    return false;

                if (!await StartOrderbookReceiverAsync())
                    return false;

                if (!await StartCandleReceiverAsync())
                    return false;

                if (!await StartPriceReceiverAsync())
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }



        #region Exchange api topic receivers

        private async Task<bool> StartTradeReceiverAsync()
        {
            try
            {
                _logger.LogDebug($"Starting trade receiver on symbols: '{String.Join(",", _option.Symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToTradeUpdatesAsync(_option.Symbols, TradeReceiver);

                if (!response.GetResultOrError(out var updateSubscription, out var error))
                {
                    _logger.LogError($"Failed to start trade receiver. Error: ({error?.Code}) {error?.Message}.");
                    return false;
                }

                updateSubscription.ConnectionRestored += ConnectionRestored;
                updateSubscription.ConnectionLost += ConnectionLost;
                updateSubscription.ConnectionClosed += ConnectionClosed;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start trade receiver.");
                return false;
            }
        }

        private async Task<bool> StartOrderbookReceiverAsync()
        {
            try
            {
                _logger.LogDebug($"Starting orderbook receiver on symbols: '{String.Join(",", _option.Symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToOrderbookUpdatesAsync(_option.Symbols, depth: 50, OrderbookReceiver);

                if (!response.GetResultOrError(out var updateSubscription, out var error))
                {
                    _logger.LogError($"Failed to start orderbook receiver. Error: ({error?.Code}) {error?.Message}.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start orderbook receiver.");
                return false;
            }
        }

        private async Task<bool> StartCandleReceiverAsync()
        {
            try
            {
                _logger.LogDebug($"Starting candle receiver on symbols: '{String.Join(",", _option.Symbols)}' and timeframes: '{String.Join(",", _option.Timeframes)}'...");

                foreach (int timeframe in _option.Timeframes)
                {
                    KlineInterval? klineInterval = timeframe.GetKlineInterval();

                    if (klineInterval == null)
                    {
                        _logger.LogError($"Not supported kline interval on timeframe: {timeframe}.");
                        return false;
                    }

                    CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToKlineUpdatesAsync(_option.Symbols, klineInterval.Value, CandleReceiver);

                    if (!response.GetResultOrError(out var updateSubscription, out var error))
                    {
                        _logger.LogError($"Failed to start candle receiver on timeframe {klineInterval.Value}. Error: ({error?.Code}) {error?.Message}.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start candle receiver.");
                return false;
            }
        }

        private async Task<bool> StartPriceReceiverAsync()
        {
            try
            {
                _logger.LogDebug($"Starting price receiver on symbols: '{String.Join(",", _option.Symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(_option.Symbols, PriceReceiver);

                if (!response.GetResultOrError(out var updateSubscription, out var error))
                {
                    _logger.LogError($"Failed to start price receiver. Error: ({error?.Code}) {error?.Message}.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start price receiver.");
                return false;
            }
        }

        private void TradeReceiver(DataEvent<IEnumerable<BybitTrade>> trades)
        {
            try
            {
                List<InternalTrade> internalTrades = new List<InternalTrade>();

                foreach (var trade in trades.Data)
                {
                    internalTrades.Add(new InternalTrade()
                    {
                        Id = trade.TradeId,
                        Symbol = trade.Symbol,
                        TradeDirection = trade.Side == OrderSide.Buy ? TradeDirection.Buy : TradeDirection.Sell,
                        Time = trade.Timestamp,
                        Price = trade.Price,
                        Volume = trade.Quantity
                    });
                }

                if (internalTrades.Count > 0)
                {
                    InvokeTradeReceivedEvent(internalTrades);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive trades.");
            }
        }

        private void OrderbookReceiver(DataEvent<BybitOrderbook> orderbook)
        {
            try
            {
                if (_orderbooks.TryGetValue(orderbook.Symbol, out InternalOrderbook localOrderbook))
                {
                    for (int i = 0; i < orderbook.Data.Asks.Count(); i++)
                    {
                        // in case more asks are sent from api endpoint.
                        if (i < localOrderbook.Asks.Count())
                        {
                            var remoteOrderbookItem = orderbook.Data.Asks.ElementAt(i);

                            localOrderbook.Asks.ElementAt(i).Price = remoteOrderbookItem.Price;
                            localOrderbook.Asks.ElementAt(i).Quantity = remoteOrderbookItem.Quantity;
                        }
                    }

                    for (int i = 0; i < orderbook.Data.Bids.Count(); i++)
                    {
                        // in case more bids are sent from api endpoint.
                        if (i < localOrderbook.Bids.Count())
                        {
                            var remoteOrderbookItem = orderbook.Data.Bids.ElementAt(i);

                            localOrderbook.Bids.ElementAt(i).Price = remoteOrderbookItem.Price;
                            localOrderbook.Bids.ElementAt(i).Quantity = remoteOrderbookItem.Quantity;
                        }
                    }
                }
                else
                {
                    List<InternalAsk> asks = new List<InternalAsk>();

                    foreach (var ask in orderbook.Data.Asks)
                    {
                        asks.Add(new InternalAsk(ask.Price, ask.Quantity));
                    }

                    List<InternalBid> bids = new List<InternalBid>();

                    foreach (var bid in orderbook.Data.Bids)
                    {
                        bids.Add(new InternalBid(bid.Price, bid.Quantity));
                    }

                    localOrderbook = new InternalOrderbook(orderbook.Symbol, asks, bids);

                    _orderbooks.Add(orderbook.Symbol, localOrderbook);
                }

                InvokeOrderbookReceivedEvent(localOrderbook.DeepCopy()); // copy instance NOT original instance
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive orderbook.");
            }
        }

        private void CandleReceiver(DataEvent<IEnumerable<BybitKlineUpdate>> candles)
        {
            try
            {
                foreach (var candle in candles.Data)
                {
                    int? timeframe = candle.Interval.GetTimeframe();
                    if (timeframe == null)
                    {
                        _logger.LogError($"Unable to convert kline interval: {candle.Interval} to minutes.");
                        continue;
                    }

                    InternalCandle internalCandle = new InternalCandle();
                    internalCandle.Symbol = candles.Symbol;
                    internalCandle.Timeframe = timeframe.Value;
                    internalCandle.StartTime = candle.StartTime;
                    internalCandle.CloseTime = candle.EndTime;
                    internalCandle.IsClosed = candle.Confirm;

                    InvokeCandleReceivedEvent(internalCandle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive candle.");
            }
        }

        private void PriceReceiver(DataEvent<BybitSpotTickerUpdate> ticker)
        {
            try
            {
                if (!_prices.TryGetValue(ticker.Symbol, out decimal price))
                {
                    _prices.Add(ticker.Symbol, ticker.Data.LastPrice);
                }
                else if (price != ticker.Data.LastPrice)
                {
                    _prices[ticker.Symbol] = ticker.Data.LastPrice;
      
                    InvokePriceInfoReceivedEvent(new PriceInfo(ticker.Symbol, ticker.Data.LastPrice));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive price.");
            }
        }

        private void ConnectionRestored(TimeSpan obj)
        {
            InvokeUnsolicitedMesageEvent(new UnsolicitedMessage(MessageType.Info, "BybitSpotApiClient connection restored."));
        }

        private void ConnectionLost()
        {
            InvokeUnsolicitedMesageEvent(new UnsolicitedMessage(MessageType.Error, "BybitSpotApiClient connection lost."));
        }

        private void ConnectionClosed()
        {
            InvokeUnsolicitedMesageEvent(new UnsolicitedMessage(MessageType.Warning, "BybitSpotApiClient connection closed."));
        }

        #endregion



        #region Exchange api event handlers

        public void InvokeTradeReceivedEvent(List<InternalTrade> trades)
        {
            this.TradeReceivedEventHandler?.Invoke(this, new TradeReceivedEventArgs(trades));
        }

        public void InvokeOrderbookReceivedEvent(InternalOrderbook orderbook)
        {
            this.OrderbookReceivedEventHandler?.Invoke(this, new OrderbookReceivedEventArgs(orderbook));
        }

        public void InvokeCandleReceivedEvent(InternalCandle candle)
        {
            this.CandleReceivedEventHandler?.Invoke(this, new CandleReceivedEventArgs(candle));
        }

        public void InvokePriceInfoReceivedEvent(PriceInfo price)
        {
            this.PriceInfoReceivedEventHandler?.Invoke(this, new PriceInfoReceivedEventArgs(price));
        }

        public void InvokeUnsolicitedMesageEvent(UnsolicitedMessage unsolicitedMessage)
        {
            this.UnsolicitedMessageEventHandler?.Invoke(this, new UnsolicitedMessageEventArgs(unsolicitedMessage));
        }

        #endregion
    }
}
