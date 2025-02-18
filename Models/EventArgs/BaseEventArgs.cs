using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingExpertAdvisor.Models.EventArgs
{
    public abstract class BaseEventArgs
    {
        public DateTime CreatedAt { get; private set; }
        public string Symbol { get; set; }

        public BaseEventArgs()
        {
            this.CreatedAt = DateTime.Now;
        }
    }
}
