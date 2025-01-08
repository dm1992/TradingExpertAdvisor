using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models
{
    public class CandleEvaluationResult
    {
        public string Symbol { get; set; }
        public int Timeframe { get; set; }
        public List<InternalCandle> Candles { get; set; } = new List<InternalCandle>();
        public DateTime CreatedAt { get; private set; }

        public CandleEvaluationResult(string symbol, int timeframe, List<InternalCandle> candles)
        {
            this.Symbol = symbol;
            this.Timeframe = timeframe;
            this.Candles = candles;
            this.CreatedAt = DateTime.Now;
        }

        public decimal GetOpenMarketPrice()
        {
            return this.Candles.OrderBy(x => x.StartTime).First().GetOpenPrice();
        }

        public decimal GetHighMarketPrice()
        {
            return this.Candles.OrderBy(x => x.StartTime).Max(x => x.GetHighPrice());
        }

        public decimal GetLowMarketPrice()
        {
            return this.Candles.OrderBy(x => x.StartTime).Min(x => x.GetLowPrice());
        }

        public decimal GetCloseMarketPrice()
        {
            return this.Candles.OrderBy(x => x.StartTime).Last().GetClosePrice();
        }

        public Dictionary<InternalCandleDirectionType, decimal> GetCandleDirectionPercentages()
        {
            Dictionary<InternalCandleDirectionType, decimal> dict = new Dictionary<InternalCandleDirectionType, decimal>();

            dict.Add(InternalCandleDirectionType.Expected_Up, Math.Round((this.Candles.Where(x => x.GetDirectionType() == InternalCandleDirectionType.Expected_Up).Count() / (decimal)this.Candles.Count()) * 100.0m, 2));
            dict.Add(InternalCandleDirectionType.Expected_Down, Math.Round((this.Candles.Where(x => x.GetDirectionType() == InternalCandleDirectionType.Expected_Down).Count() / (decimal)this.Candles.Count()) * 100.0m, 2));
            dict.Add(InternalCandleDirectionType.Not_Expected_Up, Math.Round((this.Candles.Where(x => x.GetDirectionType() == InternalCandleDirectionType.Not_Expected_Up).Count() / (decimal)this.Candles.Count()) * 100.0m, 2));
            dict.Add(InternalCandleDirectionType.Not_Expected_Down, Math.Round((this.Candles.Where(x => x.GetDirectionType() == InternalCandleDirectionType.Not_Expected_Down).Count() / (decimal)this.Candles.Count()) * 100.0m, 2));
            dict.Add(InternalCandleDirectionType.Unknown, Math.Round((this.Candles.Where(x => x.GetDirectionType() == InternalCandleDirectionType.Unknown).Count() / (decimal)this.Candles.Count()) * 100.0m, 2));

            return dict;
        }

        public MarketDirectionType GetMarketDirection()
        {
            var candleDirectionPercentages = this.GetCandleDirectionPercentages();

            if (candleDirectionPercentages[InternalCandleDirectionType.Expected_Up] + candleDirectionPercentages[InternalCandleDirectionType.Not_Expected_Up] >
                candleDirectionPercentages[InternalCandleDirectionType.Expected_Down] + candleDirectionPercentages[InternalCandleDirectionType.Not_Expected_Down])
            {
                return MarketDirectionType.Up;
            }
            else if (candleDirectionPercentages[InternalCandleDirectionType.Expected_Down] + candleDirectionPercentages[InternalCandleDirectionType.Not_Expected_Down] >
                candleDirectionPercentages[InternalCandleDirectionType.Expected_Up] + candleDirectionPercentages[InternalCandleDirectionType.Not_Expected_Up])
            {
                return MarketDirectionType.Down;
            }

            return MarketDirectionType.Unknown;
        }

        public string DumpCandleDirectionPercentages()
        {
            return String.Join(", ", this.GetCandleDirectionPercentages().Select(x => $"{x.Key} = {x.Value}%"));
        }

        public string DumpBase()
        {
            return $"---- MARKET EVALUATION {this.Symbol}_{this.Timeframe} ----\n" +
                   $"{this.DumpCandleDirectionPercentages()}\n" +
                   $"Open market price: '{this.GetOpenMarketPrice()}'\n" +
                   $"High market price: '{this.GetHighMarketPrice()}'\n" +
                   $"Low market price: '{this.GetLowMarketPrice()}'\n" +
                   $"Close market price: '{this.GetCloseMarketPrice()}'\n" +
                   $"Market direction: '{this.GetMarketDirection()}'";
        }
    }
}
