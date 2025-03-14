using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingExpertAdvisor.Interfaces;
using TradingExpertAdvisor.Managers;
using TradingExpertAdvisor.Models;

namespace TradingExpertAdvisor.Database
{
    public class MarketSignalEvaluationDatabaseManager : IManager
    {
        private readonly ILogger<MarketSignalEvaluationDatabaseManager> _logger;

        private bool _isInitialized = false;

        public MarketSignalEvaluationDatabaseManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MarketSignalEvaluationDatabaseManager>();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized) return true;

                _logger.LogInformation($"Initializing...");

                using (var dbContext = new MarketSignalEvaluationDatabaseContext())
                {
                    dbContext.Database.EnsureCreated();
                }


                return _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize.");
                return false;
            }
        }

        public MarketSignalEvaluation GetMarketSignalEvaluation(string marketSignalTag)
        {
            try
            {
                using (var dbContext = new MarketSignalEvaluationDatabaseContext())
                {
                    return dbContext.MarketSignalEvaluations.FirstOrDefault(x => x.Tag == marketSignalTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get market signal evaluation.");
                return null;
            }
        }

        public List<MarketSignalEvaluation> GetSimilarMarketSignalEvaluations(string marketSignalTag)
        {
            try
            {
                using (var dbContext = new MarketSignalEvaluationDatabaseContext())
                {
                    int underscoreIndex = Helpers.FindNthIndex(marketSignalTag, '_', 3);

                    string subTag = marketSignalTag.Substring(underscoreIndex);

                    return dbContext.MarketSignalEvaluations.Where(x => x.Tag.EndsWith(subTag)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get similar market signal evaluations.");
                return null;
            }
        }

        public bool SaveMarketSignalEvaluation(string marketSignalTag)
        {
            try
            {
                using (var dbContext = new MarketSignalEvaluationDatabaseContext())
                {
                    if (dbContext.MarketSignalEvaluations.FirstOrDefault(x => x.Tag == marketSignalTag) == null)
                    {
                        _logger.LogDebug($"Saving evaluation on market signal '{marketSignalTag}'...");

                        dbContext.MarketSignalEvaluations.Add(new MarketSignalEvaluation(marketSignalTag));

                        dbContext.SaveChanges();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save market signal evaluation.");
                return false;
            }
        }

        public void UpdateMarketSignalDirectionCounter(string marketSignalTag, MarketDirection marketDirection)
        {
            if (marketDirection == MarketDirection.Unknown)
                return;

            try
            {
                using (var dbContext = new MarketSignalEvaluationDatabaseContext())
                {
                    MarketSignalEvaluation marketSignalEvaluation = dbContext.MarketSignalEvaluations.FirstOrDefault(x => x.Tag == marketSignalTag);

                    if (marketSignalEvaluation != null)
                    {
                        _logger.LogDebug($"Updating market direction '{marketDirection}' counter on market signal '{marketSignalTag}'...");

                        if (marketDirection == MarketDirection.Up)
                        {
                            marketSignalEvaluation.Ups++;
                        }
                        else if (marketDirection == MarketDirection.Down)
                        {
                            marketSignalEvaluation.Downs++;
                        }

                        dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update market signal direction counter.");
            }
        }
    }
}
