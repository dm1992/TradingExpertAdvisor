using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Reflection;
using TradingExpertAdvisor;
using TradingExpertAdvisor.Apis.Options;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers;
using TradingExpertAdvisor.Managers.Options;

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

    Mutex mutex = new Mutex(true, args[0]);

    if (!mutex.WaitOne(TimeSpan.Zero, true))
        throw new Exception($"Second application instance detected on '{args[0]}'. This is not allowed.");

    IConfiguration configuration = new ConfigurationBuilder()
                                .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Configs"))
                                .AddJsonFile($"{args[0]}.json", optional: true, reloadOnChange: true)
                                .Build();

    if (configuration == null || configuration.AsEnumerable().IsNullOrEmpty())
        throw new ArgumentException($"Application missing '{args[0]}' configuration.", "ID configuration");

    List<ApiOption> apiOptions = configuration.GetSection("AppConfig:Apis").Get<List<ApiOption>>();
    foreach (var apiOption in apiOptions)
    {
        IApiClient apiClient = InstanceFactory.CreateApiClient(_loggerFactory, apiOption);

        CandleTransformerOption candleTransformerOption = new CandleTransformerOption();
        configuration.GetSection("AppConfig:CandleTransformer").Bind(candleTransformerOption);
        CandleTransformer candleTransformer = new CandleTransformer(_loggerFactory, apiClient, candleTransformerOption);

        if (!candleTransformer.Initialize())
        {
            throw new Exception("Failed to start candle transformer.");
        }

        CandleCollectorOption candleCollectorOption = new CandleCollectorOption();
        configuration.GetSection("AppConfig:CandleCollector").Bind(candleCollectorOption);
        CandleCollector candleCollector = new CandleCollector(_loggerFactory, candleTransformer, candleCollectorOption);

        if (!candleCollector.Initialize())
        {
            throw new Exception("Failed to start candle collector.");
        }

        MarketSignalGeneratorOption marketSignalGeneratorOption = new MarketSignalGeneratorOption();
        configuration.GetSection("AppConfig:MarketSignalGenerator").Bind(marketSignalGeneratorOption);
        MarketSignalGenerator marketSignalGenerator = new MarketSignalGenerator(_loggerFactory, candleCollector, marketSignalGeneratorOption);

        if (!marketSignalGenerator.Initialize())
        {
            throw new Exception("Failed to start market signal generator.");
        }

        TradeProcessorSimulatorOption tradeProcessorSimulatorOption = new TradeProcessorSimulatorOption();
        configuration.GetSection("AppConfig:TradeProcessorSimulator").Bind(tradeProcessorSimulatorOption);

        TradeProcessorSimulator tradeProcessorSimulator = new TradeProcessorSimulator(_loggerFactory, marketSignalGenerator, apiClient, tradeProcessorSimulatorOption);

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

