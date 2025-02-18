using Bybit.Net.Clients;
using Bybit.Net;
using CryptoExchange.Net.Authentication;
using System.Runtime;
using System;
using XT.Net;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor.Apis.BybitApi
{
    public abstract class BybitBaseApiClient
    {
        protected readonly ExchangeApiOption _option; 
        protected BybitRestClient _restClient;
        protected BybitSocketClient _socketClient;

        public BybitBaseApiClient(ExchangeApiOption option)
        {
            _option = option;

            SetupRestClient();

            SetupSocketClient();
        }   

        private void SetupRestClient()
        {
            _restClient = new BybitRestClient(optionsDelegate =>
            {
                optionsDelegate.Environment = _option.IsLive ? BybitEnvironment.Live : BybitEnvironment.DemoTrading;

                if (!string.IsNullOrEmpty(_option.ApiKey))
                {
                    if (!string.IsNullOrEmpty(_option.ApiSecret))
                    {
                        optionsDelegate.ApiCredentials = new ApiCredentials(_option.ApiKey, _option.ApiSecret);
                    }
                }
            });
        }

        private void SetupSocketClient()
        {
            _socketClient = new BybitSocketClient(optionsDelegate =>
            {
                optionsDelegate.Environment = _option.IsLive ? BybitEnvironment.Live : BybitEnvironment.DemoTrading;

                if (!string.IsNullOrEmpty(_option.ApiKey))
                {
                    if (!string.IsNullOrEmpty(_option.ApiSecret))
                    {
                        optionsDelegate.ApiCredentials = new ApiCredentials(_option.ApiKey, _option.ApiSecret);
                    }
                }
            });
        }
    }
}
