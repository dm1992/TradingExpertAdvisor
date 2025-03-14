using Microsoft.EntityFrameworkCore;
using TradingExpertAdvisor.Models;

namespace TradingExpertAdvisor.Database
{
    public class MarketSignalEvaluationDatabaseContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=app.db");
        }

        public DbSet<MarketSignalEvaluation> MarketSignalEvaluations { get; set; }
    }
}
