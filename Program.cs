using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Reflection;
using TradingExpertAdvisor;
using TradingExpertAdvisor.Database;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers;
using TradingExpertAdvisor.Options;

ILoggerFactory _loggerFactory = null;
ILogger _logger = null;

try
{
    ManualResetEvent stopApplication = new ManualResetEvent(false);

    _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddConsole();
        builder.AddNLog();
    });

    _logger = _loggerFactory.CreateLogger<Program>();

    _logger.LogInformation($"-------------------------------------------");
    _logger.LogInformation($"APP_NAME: {Assembly.GetExecutingAssembly().GetName().Name}");
    _logger.LogInformation($"VERSION: {Assembly.GetExecutingAssembly().GetName().Version}");
    _logger.LogInformation($"-------------------------------------------");

    //Mutex mutex = new Mutex(true, args[0]);

    //if (!mutex.WaitOne(TimeSpan.Zero, true))
    //    throw new Exception($"Second application instance detected on '{args[0]}'. This is not allowed.");

    IConfiguration configuration = new ConfigurationBuilder()
                                .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Configs"))
                                .AddJsonFile($"{args[0]}.json", optional: true, reloadOnChange: true)
                                .Build();

    if (configuration == null || configuration.AsEnumerable().IsNullOrEmpty())
        throw new ArgumentException($"Application missing '{args[0]}' configuration.", "ID configuration");

    List<ExchangeApiOption> exchangeApiOptions = configuration.GetSection("AppConfig:ExchangeApis").Get<List<ExchangeApiOption>>();

    foreach (var exchangeApiOption in exchangeApiOptions)
    {
        IExchangeApiClient exchangeApiClient = InstanceFactory.CreateExchangeApiClient(_loggerFactory, exchangeApiOption);

        if (!exchangeApiClient.Initialize().Result)
        {
            throw new Exception($"Failed to setup api '{exchangeApiClient.GetType().Name}'.");
        }

        //NewsTracker newsTracker = new NewsTracker(_loggerFactory, exchangeApiClient);

        //if (!newsTracker.Initialize())
        //{
        //    throw new Exception("Failed to start news tracker.");
        //}

        CandleTransformer candleTransformer = new CandleTransformer(_loggerFactory, exchangeApiClient);

        if (!candleTransformer.Initialize())
        {
            throw new Exception("Failed to start candle transformer.");
        }

        MarketSignalEvaluationDatabaseManager databaseManager = new MarketSignalEvaluationDatabaseManager(_loggerFactory);

        if (!databaseManager.Initialize())
        {
            throw new Exception("Failed to start database manager.");
        }

        MarketSignalGeneratorOption marketSignalGeneratorOption = new MarketSignalGeneratorOption();
        configuration.GetSection("AppConfig:MarketSignalGenerator").Bind(marketSignalGeneratorOption);

        MarketSignalGenerator marketSignalGenerator = new MarketSignalGenerator(_loggerFactory, candleTransformer, exchangeApiClient, databaseManager, marketSignalGeneratorOption);

        if (!marketSignalGenerator.Initialize())
        {
            throw new Exception("Failed to start market signal generator.");
        }

        TradeProcessorSimulatorOption tradeProcessorSimulatorOption = new TradeProcessorSimulatorOption();
        configuration.GetSection("AppConfig:TradeProcessorSimulator").Bind(tradeProcessorSimulatorOption);

        TradeProcessorSimulator tradeProcessorSimulator = new TradeProcessorSimulator(_loggerFactory, marketSignalGenerator, exchangeApiClient, tradeProcessorSimulatorOption);

        if (!tradeProcessorSimulator.Initialize())
        {
            throw new Exception("Failed to start trade processor simulator.");
        }
    }

    stopApplication.WaitOne();
}
catch (Exception ex)
{
    _logger?.LogError(ex, $"Fatal error occurred.");
}
finally
{
   _logger.LogInformation("Program exited.");
}

