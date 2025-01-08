using Bybit.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Apis.BybitApi;
using TradingExpertAdvisor.Apis.Options;
using TradingExpertAdvisor.Interfaces;

namespace TradingExpertAdvisor
{
    public static class InstanceFactory
    {

        public static IApiClient CreateApiClient(ILoggerFactory loggerFactory, ApiOption option)
        {
            if (option == null)
                throw new Exception("Failed to create API client. API option not provided.");

            switch(option.Api)
            {
                case Api.Bybit_Spot:
                    return new BybitSpotApiClient(loggerFactory, option.ApiKey, option.ApiSecret, BybitEnvironment.Live);

                default:
                    throw new Exception($"Not supported API '{option.Api}'.");
            }
        }
    }
}
