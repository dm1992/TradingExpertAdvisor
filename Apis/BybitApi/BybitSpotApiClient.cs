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
using TradingExpertAdvisor.Apis.Options;
using System.Diagnostics;
using CryptoExchange.Net.CommonObjects;
using Newtonsoft.Json;

namespace TradingExpertAdvisor.Apis.BybitApi
{
    public class BybitSpotApiClient : BybitBaseApiClient, IApiClient
    {
        private readonly ILogger<BybitSpotApiClient> _logger;

        public event EventHandler<TradeReceivedEventArgs> TradeReceivedEventHandler;
        public event EventHandler<OrderbookReceivedEventArgs> OrderbookReceivedEventHandler;
        public event EventHandler<CandleReceivedEventArgs> CandleReceivedEventHandler;
        public event EventHandler<PriceReceivedEventArgs> PriceReceivedEventHandler;
        public event EventHandler<UnsolicitedMessageEventArgs> UnsolicitedMessageEventHandler;

        private Dictionary<string, InternalOrderbook> _orderbooks;
        private Dictionary<string, decimal> _prices;

        public BybitSpotApiClient(ILoggerFactory loggerFactory, string apiKey, string apiSecret, BybitEnvironment environment) 
        : base(apiKey, apiSecret, environment)
        {
            _logger = loggerFactory.CreateLogger<BybitSpotApiClient>();

            _orderbooks = new Dictionary<string, InternalOrderbook>();
            _prices = new Dictionary<string, decimal>();
        }

        public Api GetApiName()
        {
            return Api.Bybit_Spot;
        }

        public decimal? GetLastPrice(string symbol)
        {
            if (!_prices.TryGetValue(symbol, out decimal price))
                return null;

            return price;
        }

        public async Task<bool> StartTradeReceiverAsync(IEnumerable<string> symbols)
        {
            try
            {
                _logger.LogDebug($"Starting trade receiver on symbols: '{String.Join(",", symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToTradeUpdatesAsync(symbols, TradeReceiver);

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

        public async Task<bool> StartOrderbookReceiverAsync(IEnumerable<string> symbols)
        {
            try
            {
                _logger.LogDebug($"Starting orderbook receiver on symbols: '{String.Join(",", symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToOrderbookUpdatesAsync(symbols, depth: 50, OrderbookReceiver);

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

        public async Task<bool> StartCandleReceiverAsync(IEnumerable<string> symbols, IEnumerable<int> timeframes)
        {
            try
            {
                _logger.LogDebug($"Starting candle receiver on symbols: '{String.Join(",", symbols)}' and timeframes: '{String.Join(",", timeframes)}'...");

                foreach (int timeframe in timeframes)
                {
                    KlineInterval? klineInterval = timeframe.GetKlineInterval();

                    if (klineInterval == null)
                    {
                        _logger.LogError($"Not supported kline interval on timeframe: {timeframe}.");
                        return false;
                    }

                    CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToKlineUpdatesAsync(symbols, klineInterval.Value, CandleReceiver);

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

        public async Task<bool> StartPriceReceiverAsync(IEnumerable<string> symbols)
        {
            try
            {
                _logger.LogDebug($"Starting price receiver on symbols: '{String.Join(",", symbols)}'...");

                CallResult<UpdateSubscription> response = await _socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(symbols, PriceReceiver);

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

        private void ConnectionRestored(TimeSpan obj)
        {
            InvokeUnsolicitedMesageEvent(MessageType.Info, "BybitSpotApiClient connection restored.");
        }

        private void ConnectionLost()
        {
            InvokeUnsolicitedMesageEvent(MessageType.Error, "BybitSpotApiClient connection lost.");
        }

        private void ConnectionClosed()
        {
            InvokeUnsolicitedMesageEvent(MessageType.Warning, "BybitSpotApiClient connection closed.");
        }


        #region API topics receivers

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

                    InvokePriceReceivedEvent(ticker.Symbol, ticker.Data.LastPrice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive price.");
            }
        }

        #endregion



        #region Event handlers

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

        public void InvokePriceReceivedEvent(string symbol, decimal price)
        {
            this.PriceReceivedEventHandler?.Invoke(this, new PriceReceivedEventArgs(symbol, price));
        }

        public void InvokeUnsolicitedMesageEvent(MessageType type, string message)
        {
            this.UnsolicitedMessageEventHandler?.Invoke(this, new UnsolicitedMessageEventArgs(type, message));
        }

        #endregion
    }
}
