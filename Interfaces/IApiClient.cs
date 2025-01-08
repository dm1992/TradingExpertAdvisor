using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Models.EventArgs;

namespace TradingExpertAdvisor.Interfaces
{
    public interface IApiClient
    {
        event EventHandler<TradeReceivedEventArgs> TradeReceivedEventHandler;
        event EventHandler<OrderbookReceivedEventArgs> OrderbookReceivedEventHandler;
        event EventHandler<CandleReceivedEventArgs> CandleReceivedEventHandler;
        event EventHandler<PriceReceivedEventArgs> PriceReceivedEventHandler;
        event EventHandler<UnsolicitedMessageEventArgs> UnsolicitedMessageEventHandler;

        Api GetApiName();

        decimal? GetLastPrice(string symbol);

        Task<bool> StartTradeReceiverAsync(IEnumerable<string> symbols);

        Task<bool> StartOrderbookReceiverAsync(IEnumerable<string> symbols);

        Task<bool> StartCandleReceiverAsync(IEnumerable<string> symbols, IEnumerable<int> timeframes);

        Task<bool> StartPriceReceiverAsync(IEnumerable<string> symbols);
    }
}
