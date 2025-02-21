using Microsoft.Extensions.Logging;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Managers
{
    public class TradeProcessorSimulator : ITradeProcessor
    {
        private readonly ILogger<TradeProcessorSimulator> _logger;
        //private readonly IMarketSignalValidator _marketSignalValidator;
        private readonly IMarketSignalGenerator _marketSignalGenerator;
        private readonly IExchangeApiClient _apiClient;
        private readonly TradeProcessorSimulatorOption _option;

        private bool _isInitialized;
        private List<SimulationTrade> _tradeBuffer;

        public TradeProcessorSimulator(ILoggerFactory loggerFactory,
                                       IMarketSignalGenerator marketSignalGenerator,
                                       IExchangeApiClient apiClient,
                                       TradeProcessorSimulatorOption option)
        {
            _logger = loggerFactory.CreateLogger<TradeProcessorSimulator>();
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

                _marketSignalGenerator.MarketSignalGeneratedEventHandler += MarketSignalEventHandler;
                _apiClient.PriceInfoReceivedEventHandler += PriceInfoReceivedEventHandler;

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
            HandleMarketSignal(e.MarketSignalMetadata);
        }

        private void PriceInfoReceivedEventHandler(object? sender, PriceInfoReceivedEventArgs e)
        {
            HandlePriceInfo(e.PriceInfo);
        }

        private void HandleMarketSignal(MarketSignalMetadata marketSignal)
        {
            lock (_tradeBuffer)
            {
                if (marketSignal == null) return;

                decimal? lastPrice = _apiClient.GetLastPrice(marketSignal.Symbol);
                if (lastPrice == null)
                    return;

                int activeTrades = _tradeBuffer.Where(x => x.Symbol == marketSignal.Symbol && !x.HasCompleted).Count();
                if (activeTrades >= _option.ActiveTradesLimit)
                    return;

                SimulationTrade trade = new SimulationTrade(_option.TakeProfitAmount, _option.StopLossAmount);
                trade.Time = DateTime.Now;
                trade.Symbol = marketSignal.Symbol;
                trade.EntryPrice = lastPrice.Value;
                trade.TradeDirection = marketSignal.MarketDirection == MarketDirection.Up ? TradeDirection.Buy : TradeDirection.Sell;
                trade.Volume = 1;

                _logger.LogInformation($"<<<<< Opening '{trade.Symbol}' trade in direction '{trade.TradeDirection}' @ price '{trade.EntryPrice}' <<<<<");

                _tradeBuffer.Add(trade);
            }
        }

        private void HandlePriceInfo(PriceInfo priceInfo)
        {
            lock (_tradeBuffer)
            {
                if (priceInfo == null) return;

                var trades = _tradeBuffer.Where(x => x.Symbol == priceInfo.Symbol && !x.HasCompleted);

                if (trades.IsNullOrEmpty())
                    return;

                foreach (var trade in trades)
                {
                    if (trade.TradeDirection == TradeDirection.Buy)
                    {
                        if (priceInfo.Price >= trade.TakeProfitPrice)
                        {
                            trade.Balance = priceInfo.Price - trade.EntryPrice;
                        }
                        else if (priceInfo.Price <= trade.StopLossPrice)
                        {
                            trade.Balance = -(trade.EntryPrice - priceInfo.Price);
                        }
                    }
                    else if (trade.TradeDirection == TradeDirection.Sell)
                    {
                        if (priceInfo.Price <= trade.TakeProfitPrice)
                        {
                            trade.Balance = trade.EntryPrice - priceInfo.Price;
                        }
                        else if (priceInfo.Price >= trade.StopLossPrice)
                        {
                            trade.Balance = -(priceInfo.Price - trade.EntryPrice);
                        }
                    }

                    if (trade.Balance.HasValue)
                    {
                        _logger.LogInformation($"!!!!! '{priceInfo.Symbol}' trade completed with balance '{trade.Balance.Value}'. Entry price: '{trade.EntryPrice}' and Exit price: '{trade.ExitPrice}' !!!!!");
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
