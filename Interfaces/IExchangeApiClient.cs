using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Interfaces
{
    public interface IExchangeApiClient
    {
        event EventHandler<TradeReceivedEventArgs> TradeReceivedEventHandler;
        event EventHandler<OrderbookReceivedEventArgs> OrderbookReceivedEventHandler;
        event EventHandler<CandleReceivedEventArgs> CandleReceivedEventHandler;
        event EventHandler<PriceInfoReceivedEventArgs> PriceInfoReceivedEventHandler;
        event EventHandler<UnsolicitedMessageEventArgs> UnsolicitedMessageEventHandler;

        Task<bool> Initialize();

        Task<List<SymbolInfo>> GetSymbolsAsync();

        Task<List<Announcement>> GetAnnouncementsAsync();

        ExchangeApiOption GetOption();

        decimal? GetLastPrice(string symbol);

        
    }
}
