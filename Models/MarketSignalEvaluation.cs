using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class MarketSignalEvaluation
    {
        [Key]
        public int Id { get; set; }

        [NotNull]
        public string Tag { get; set; }

        [NotNull]
        public int Ups { get; set; }

        public decimal UpsPercentage
        {
            get
            {
                if (this.Ups == 0)
                    return 0;

                return Math.Round(this.Ups / (decimal)this.Total * 100.0m, 2);
            }
        }

        [NotNull]
        public int Downs { get; set; }

        public decimal DownsPercentage
        {
            get
            {
                if (this.Downs == 0)
                    return 0;

                return Math.Round(this.Downs / (decimal)this.Total * 100.0m, 2);
            }
        }

        public int Total
        {
            get
            {
                return this.Ups + this.Downs;
            }
        }

        public MarketSignalEvaluation(string tag)
        {
            Tag = tag;
            Ups = 0;
            Downs = 0;
        }
    }
}
