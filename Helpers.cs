using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor
{
    public static class Helpers
    {
        public static int FindNthIndex(string str, char c, int n)
        {
            int index = -1;
            for (int i = 0; i < n; i++)
            {
                index = str.IndexOf(c, index + 1);
                if (index == -1)
                    return -1; // Če ni dovolj pojavitev, vrne -1
            }
            return index;
        }
    }
}
