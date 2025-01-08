using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers.Options;
using TradingExpertAdvisor.Models;
using TradingExpertAdvisor.Models.EventArgs;

namespace TradingExpertAdvisor.Managers
{
    public class CandleEvaluator : ICandleEvaluator
    {
        public event EventHandler<CandleEvaluatedEventArgs> CandleEvaluatedEventHandler;

        private readonly ILogger<CandleEvaluator> _logger;
        private readonly ICandleTransformer _candleTransformer;
        private readonly CandleEvaluatorOption _option;

        private bool _isInitialized;
        private Dictionary<string, Dictionary<int, List<InternalCandle>>> _candles;
        //private List<CandleEvaluationResult> _candleEvaluationResults;    

        public CandleEvaluator(ILoggerFactory loggerFactory, 
                               ICandleTransformer candleTransformer,
                               CandleEvaluatorOption option)
        {
            _logger = loggerFactory.CreateLogger<CandleEvaluator>();
            _candleTransformer = candleTransformer;
            _option = option;

            _isInitialized = false;
            _candles = new Dictionary<string, Dictionary<int, List<InternalCandle>>>();
            //_candleEvaluationResults = new List<CandleEvaluationResult>();
        }


        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                _candleTransformer.CandleTransformedEventHandler += CandleTransformedEventHandler;

                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        private void CandleTransformedEventHandler(object? sender, CandleTransformedEventArgs e)
        {
            //xxx when max timeframe candle transformed received, dump all of previously received candles to log!
            HandleCandle(e.Candle);
        }

        private void HandleCandle(InternalCandle candle)
        {
            lock (_candles)
            {
                if (!_candles.TryGetValue(candle.Symbol, out Dictionary<int, List<InternalCandle>> dict))
                {
                    dict = new Dictionary<int, List<InternalCandle>>();
                    dict.Add(candle.Timeframe, new List<InternalCandle>() { candle });

                    _candles.Add(candle.Symbol, dict);
                }
                else if (!dict.TryGetValue(candle.Timeframe, out List<InternalCandle> candles))
                {
                    dict.Add(candle.Timeframe, new List<InternalCandle>() { candle });
                }
                else
                {
                    candles.Add(candle);
                }
            }
        }

        //private void EvaluateMarket(string symbol, int timeframe)
        //{
        //    if (!_candles.TryGetValue(symbol, out Dictionary<int, List<InternalCandle>> dict))
        //    {
        //        _logger.LogError($"Failed to get '{symbol}' candle buffer.");
        //        return;
        //    }

        //    if (!dict.TryGetValue(timeframe, out List<InternalCandle> candles))
        //    {
        //        _logger.LogError($"Failed to get '{symbol}_{timeframe}' candle.");
        //        return;
        //    }

        //    if (!_option.TimeframeThresholds.TryGetValue(timeframe, out int threshold))
        //    {
        //        _logger.LogError($"Failed to get '{symbol}_{timeframe}' threshold value.");
        //        return;
        //    }

        //    if (candles.Count() % threshold == 0)
        //    {
        //        _logger.LogInformation($"'{symbol}_{timeframe}' threshold of '{threshold}' candles reached. Evaluating market...");

        //        CandleEvaluationResult result = new CandleEvaluationResult(symbol, timeframe, new List<InternalCandle>(candles));

        //        _candleEvaluationResults.Add(result);

        //        _logger.LogInformation(result.DumpBase());

        //        candles.Clear(); // evaluation done, flush timeframe candles

        //        EnterMarket(symbol);
        //    }
        //}

        //public void EnterMarket(string symbol)
        //{
        //    //xxx
        //    //InvokeMarketEvaluationEvent(new MarketEvaluationEventArgs(symbol, MarketDirectionType.Up));
        //}

        //private void InvokeMarketEvaluationEvent(CandleEvaluatedEventArgs args)
        //{
        //    this.CandleEvaluatedEventHandler?.Invoke(this, args);
        //}
    }
}
