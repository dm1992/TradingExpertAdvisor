using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;

namespace TradingExpertAdvisor.Managers
{
    public class NewsTracker : IManager
    {
        // triggerr event when pending market symbol received 

        private readonly ILogger<NewsTracker> _logger;
        private readonly IExchangeApiClient _exchangeApiClient;

        private List<SymbolInfo> _marketSymbols = new List<SymbolInfo>();
        private bool _isInitialized = false;

        public NewsTracker(ILoggerFactory loggerFactory,
                           IExchangeApiClient exchangeApiClient)
        {
            _logger = loggerFactory.CreateLogger<NewsTracker>();
            _exchangeApiClient = exchangeApiClient;
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                Task.Run(() => RunNewsTrackerInThread());

                _exchangeApiClient.UnsolicitedMessageEventHandler += UnsolicitedMessageEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private async void RunNewsTrackerInThread()
        {
            _logger.LogDebug("RunNewsTrackerInThread.");

            while(true)
            {
                CheckMarketSymbols();

                CheckMarketAnnouncements();

                await Task.Delay(60000 * 60); // run every hour
            }
        }

        private async void CheckMarketSymbols()
        {
            try
            {
                _logger.LogDebug("CheckMarketSymbols.");

                var symbols = await _exchangeApiClient.GetSymbolsAsync();

                if (symbols.IsNullOrEmpty())
                {
                    throw new Exception("No market symbols received.");
                }

                foreach (var ms in symbols.Where(x => x.Status == SymbolStatus.PreLaunch))
                {
                    SymbolInfo ms_old = _marketSymbols.Find(x => x.Name == ms.Name);

                    if (ms_old != null)
                    {
                        if (ms_old.Status == SymbolStatus.PreLaunch && ms_old.Status != ms.Status)
                        {
                            _logger.LogDebug($"PRELAUNCH SYMBOL >>> '{ms.Name}'"); //xxx invoke event
                        }
                    }                 
                }

                _marketSymbols = symbols;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check market symbols.");
            }
        }

        private async void CheckMarketAnnouncements()
        {
            try
            {
                _logger.LogDebug("CheckMarketAnnouncements.");

                var announcements = await _exchangeApiClient.GetAnnouncementsAsync();

                if (announcements.IsNullOrEmpty())
                {
                    throw new Exception("No market announcements received.");
                }

                foreach (var a in announcements)
                {
                        _logger.LogDebug(JsonConvert.SerializeObject(a, Formatting.Indented));
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check market announcements.");
            }
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

    }
}
