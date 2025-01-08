using Bybit.Net.Clients;
using Bybit.Net;
using CryptoExchange.Net.Authentication;
using TradingExpertAdvisor.Apis.Options;
using System.Runtime;

namespace TradingExpertAdvisor.Apis.BybitApi
{
    public abstract class BybitBaseApiClient
    {
        protected readonly BybitRestClient _restClient;
        protected readonly BybitSocketClient _socketClient;

        public BybitBaseApiClient(string apiKey, string apiSecret, BybitEnvironment environment)
        {
            _restClient = new BybitRestClient(optionsDelegate =>
            {
                optionsDelegate.Environment = environment;

                if (!string.IsNullOrEmpty(apiKey))
                {
                    if (!string.IsNullOrEmpty(apiSecret))
                    {
                        optionsDelegate.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
                    }
                }
            });

            _socketClient = new BybitSocketClient(optionsDelegate =>
            {
                optionsDelegate.Environment = environment;

                if (!string.IsNullOrEmpty(apiKey))
                {
                    if (!string.IsNullOrEmpty(apiSecret))
                    {
                        optionsDelegate.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
                    }
                }
            });
        }   
    }
}
