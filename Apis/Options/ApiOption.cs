using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Apis.Options
{
    public class ApiOption
    {
        public Api Api { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }
}
