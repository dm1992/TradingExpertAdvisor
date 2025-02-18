using Bybit.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Apis.BybitApi;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Options;

namespace TradingExpertAdvisor
{
    public static class InstanceFactory
    {

        public static IExchangeApiClient CreateExchangeApiClient(ILoggerFactory loggerFactory, ExchangeApiOption option)
        {
            if (option == null)
                throw new Exception("Failed to create exchange api client. Exchange api option not provided.");

            switch(option.ApiName)
            {
                case ExchangeApi.Bybit_Spot:
                    return new BybitSpotApiClient(loggerFactory, option);

                default:
                    throw new Exception($"Not supported exchange api '{option.ApiName}'.");
            }
        }
    }
}
