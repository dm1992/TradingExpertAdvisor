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
    public class TradeProcessorSimulator : ITradeProcessor
    {
        private readonly ILogger<CandleCollector> _logger;
        private readonly IMarketSignalGenerator _marketSignalGenerator;
        private readonly IApiClient _apiClient;
        private readonly TradeProcessorSimulatorOption _option;

        private bool _isInitialized;
        private List<SimulationTrade> _tradeBuffer;

        public TradeProcessorSimulator(ILoggerFactory loggerFactory,
                                       IMarketSignalGenerator marketSignalGenerator,
                                       IApiClient apiClient,
                                       TradeProcessorSimulatorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleCollector>();
            _marketSignalGenerator = marketSignalGenerator;
            _apiClient = apiClient;
            _option = option;

            _isInitialized = false;
            _tradeBuffer = new List<SimulationTrade>();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                if (!_apiClient.StartPriceReceiverAsync(_option.Symbols).Result)
                    return false;

                _marketSignalGenerator.MarketSignalEventHandler += MarketSignalEventHandler;
                _apiClient.PriceReceivedEventHandler += PriceChangedEventHandler;

                Task.Run(() => RunBalanceTrackerInThread());

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        public bool OpenTrade()
        {
            throw new NotImplementedException();
        }

        private void MarketSignalEventHandler(object? sender, MarketSignalEventArgs e)
        {
            //HandleMarketEvaluation(e.Symbol, e.DirectionType);
        }

        private void PriceChangedEventHandler(object? sender, PriceReceivedEventArgs e)
        {
            HandlePriceChange(e.Symbol, e.Price);
        }

        private void HandleMarketEvaluation(string symbol, MarketDirection directionType)
        {
            lock (_tradeBuffer)
            {
                if (directionType == MarketDirection.Unknown)
                    return;

                decimal? lastPrice = _apiClient.GetLastPrice(symbol);
                if (lastPrice == null)
                    return;

                int activeTrades = _tradeBuffer.Where(x => x.Symbol == symbol && !x.HasCompleted).Count();
                if (activeTrades >= _option.ActiveTradesLimit)
                    return;

                SimulationTrade trade = new SimulationTrade(_option.TakeProfitAmount, _option.StopLossAmount);
                trade.Time = DateTime.Now;
                trade.Symbol = symbol;
                trade.EntryPrice = lastPrice.Value;
                trade.TradeDirection = directionType == MarketDirection.Up ? TradeDirection.Buy : TradeDirection.Sell;
                trade.Volume = 1;

                _logger.LogInformation($"<<<<< Opening '{trade.Symbol}' trade in direction '{trade.TradeDirection}' @ price '{trade.EntryPrice}' <<<<<");

                _tradeBuffer.Add(trade);
            }
        }

        private void HandlePriceChange(string symbol, decimal price)
        {
            lock (_tradeBuffer)
            {
                var trades = _tradeBuffer.Where(x => x.Symbol == symbol && !x.HasCompleted);

                if (trades.IsNullOrEmpty())
                    return;

                foreach (var trade in trades)
                {
                    if (trade.TradeDirection == TradeDirection.Buy)
                    {
                        if (price >= trade.TakeProfitPrice)
                        {
                            trade.Balance = price - trade.EntryPrice;
                        }
                        else if (price <= trade.StopLossPrice)
                        {
                            trade.Balance = -(trade.EntryPrice - price);
                        }
                    }
                    else if (trade.TradeDirection == TradeDirection.Sell)
                    {
                        if (price <= trade.TakeProfitPrice)
                        {
                            trade.Balance = trade.EntryPrice - price;
                        }
                        else if (price >= trade.StopLossPrice)
                        {
                            trade.Balance = -(price - trade.EntryPrice);
                        }
                    }

                    if (trade.Balance.HasValue)
                    {
                        _logger.LogInformation($"!!!!! '{symbol}' trade completed with balance '{trade.Balance.Value}'. Entry price: '{trade.EntryPrice}' and Exit price: '{trade.ExitPrice}' !!!!!");
                    }
                }
            }
        }

        private void RunBalanceTrackerInThread()
        {
            while (true)
            {
                var completedTrades = _tradeBuffer.Where(x => x.HasCompleted);

                if (!completedTrades.IsNullOrEmpty())
                {
                    _logger.LogInformation($">>>>> Total balance: '{completedTrades.Sum(x => x.Balance)}' <<<<<");
                }

                Thread.Sleep(10000);
            }
        }
    }
}
