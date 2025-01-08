using Bybit.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            if (list == null || !list.Any())
                return true;

            return false;
        }

        public static int? GetTimeframe(this KlineInterval interval)
        {
            switch (interval)
            {
                case KlineInterval.OneMinute:
                    return 1;
                case KlineInterval.ThreeMinutes:
                    return 3;
                case KlineInterval.FiveMinutes:
                    return 5;
                case KlineInterval.FifteenMinutes:
                    return 15;
                case KlineInterval.ThirtyMinutes:
                    return 30;
                case KlineInterval.OneHour:
                    return 60;
                case KlineInterval.TwoHours:
                    return 120;
                case KlineInterval.FourHours:
                    return 240;
                case KlineInterval.SixHours:
                    return 360;
                case KlineInterval.TwelveHours:
                    return 720;
                case KlineInterval.OneDay:
                    return 1440;
                case KlineInterval.OneWeek:
                    return 10080;
                case KlineInterval.OneMonth:
                    return 43829;

                default:
                    return null;

            }
        }

        public static KlineInterval? GetKlineInterval(this int value)
        {
            switch (value)
            {
                case 1:
                    return KlineInterval.OneMinute;
                case 3:
                    return KlineInterval.ThreeMinutes;
                case 5:
                    return KlineInterval.FiveMinutes;
                case 15:
                    return KlineInterval.FifteenMinutes;
                case 30:
                    return KlineInterval.ThirtyMinutes;
                case 60:
                    return KlineInterval.OneHour;
                case 120:
                    return KlineInterval.TwoHours;
                case 240:
                    return KlineInterval.FourHours;
                case 360:
                    return KlineInterval.SixHours;
                case 720:
                    return KlineInterval.TwelveHours;
                case 1440:
                    return KlineInterval.OneDay;
                case 10080:
                    return KlineInterval.OneWeek;
                case 43829:
                    return KlineInterval.OneMonth;

                default:
                    return null;

            }
        }
    }
}
